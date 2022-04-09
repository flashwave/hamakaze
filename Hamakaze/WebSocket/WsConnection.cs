using System;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography;
using System.Text;

// TODO: optimisations with newer .net feature to reduce memory copying
//       i think we're generally aware of how much data we're shoving around
//        so memorystream can be considered overkill

// Should there be internal mutexing on the socket? (leaning towards no)

// Should all external stream handling be moved to WsClient?
//  - IDEA: Buffered send "session" class.
//          Would require exposing the raw Write methods
//           but i suppose that's what "internal" exists for

namespace Hamakaze.WebSocket {
    public class WsConnection : IDisposable {
        public Stream Stream { get; }

        public bool IsSecure { get; }
        public bool IsClosed { get; private set; }

        private const int BUFFER_SIZE = 0x2000;
        private const byte MASK_FLAG = 0x80;
        private const int MASK_SIZE = 4;

        private WsOpcode FragmentedType = 0;
        private MemoryStream FragmentedStream;

        public WsConnection(Stream stream) {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            IsSecure = stream is SslStream;
        }

        private static byte[] GenerateMask() {
            return RandomNumberGenerator.GetBytes(MASK_SIZE);
        }

        private void StrictRead(byte[] buffer, int offset, int length) {
            int read = Stream.Read(buffer, offset, length);
            if(read < length)
                throw new Exception(@"Was unable to read the requested amount of data.");
        }

        private (WsOpcode opcode, long length, bool isFinal, byte[] mask) ReadFrameHeader() {
            byte[] buffer = new byte[8];
            StrictRead(buffer, 0, 2);

            WsOpcode opcode = (WsOpcode)(buffer[0] & 0x0F);
            bool isFinal = (buffer[0] & (byte)WsOpcode.FlagFinal) > 0;

            if(opcode >= WsOpcode.CtrlClose && !isFinal)
                throw new WsInvalidOpcodeException((WsOpcode)buffer[0]);

            bool isControl = (opcode & WsOpcode.CtrlClose) > 0;

            if(isControl && !isFinal)
                throw new WsInvalidControlFrameException(@"fragmented");

            bool isMasked = (buffer[1] & MASK_FLAG) > 0;

            // this may look stupid and you'd be correct but it's better than the stack of casts
            // i'd otherwise have to do otherwise because c# converts everything back to int32
            buffer[1] &= 0x7F;
            long length = buffer[1];

            if(length == 126) {
                StrictRead(buffer, 0, 2);
                length = WsUtils.ToU16(buffer);
            } else if(length == 127) {
                StrictRead(buffer, 0, 8);
                length = WsUtils.ToI64(buffer);
            }

            if(isControl && length > 125)
                throw new WsInvalidControlFrameException(@"too large");

            // should there be a sanity check on the length of frames?
            // i seriously don't understand the rationale behind both
            //  having a framing system but then also supporting frame lengths
            //  of 2^63, feels like 2^16 per frame would be a fine max.
            if(length < 0 || length > long.MaxValue)
                throw new WsInvalidFrameSizeException(length);

            byte[] mask = null;

            if(isMasked) {
                StrictRead(buffer, 0, MASK_SIZE);
                mask = buffer;
            }

            return (opcode, length, isFinal, mask);
        }

        private long ReadFrameBody(Stream target, long length, byte[] mask, long offset = 0) {
            if(target == null)
                throw new ArgumentNullException(nameof(target));
            if(!target.CanWrite)
                throw new ArgumentException(@"Target stream is not writable.", nameof(target));

            bool isMasked = mask != null;

            int read;
            int take = length > BUFFER_SIZE ? BUFFER_SIZE : (int)length;
            byte[] buffer = new byte[take];

            while(length > 0) {
                read = Stream.Read(buffer, 0, take);

                if(isMasked)
                    for(int i = 0; i < read; ++i)
                        buffer[i] ^= mask[offset++ % MASK_SIZE];

                target.Write(buffer, 0, read);

                offset += read;
                length -= read;

                if(take > length)
                    take = (int)length;
            }

            return offset;
        }

        private WsMessage ReadFrame() {
            (WsOpcode opcode, long length, bool isFinal, byte[] mask) = ReadFrameHeader();

            if(opcode is not WsOpcode.DataContinue
                and not WsOpcode.DataBinary
                and not WsOpcode.DataText
                and not WsOpcode.CtrlClose
                and not WsOpcode.CtrlPing
                and not WsOpcode.CtrlPong)
                throw new WsUnsupportedOpcodeException(opcode);

            bool hasBody = length > 0;
            bool isContinue = opcode == WsOpcode.DataContinue;
            bool canFragment = (opcode & WsOpcode.CtrlClose) == 0;

            MemoryStream bodyStream = null;

            if(hasBody) {
                if(canFragment) {
                    if(isContinue) {
                        if(FragmentedType == 0)
                            throw new WsUnexpectedContinueException();

                        opcode = FragmentedType;

                        if(FragmentedStream == null)
                            FragmentedStream = bodyStream = new();
                        else
                            bodyStream = FragmentedStream;
                    } else {
                        if(FragmentedType != 0)
                            throw new WsUnexpectedDataException();

                        if(isFinal)
                            bodyStream = new();
                        else {
                            FragmentedType = opcode;
                            FragmentedStream = bodyStream = new();
                        }
                    }
                } else
                    bodyStream = new();

                ReadFrameBody(bodyStream, length, mask);
            }

            WsMessage msg;

            if(isFinal) {
                if(canFragment && isContinue) {
                    FragmentedType = 0;
                    FragmentedStream = null;
                }

                byte[] body = null;

                if(bodyStream != null) {
                    if(bodyStream.Length > 0)
                        body = bodyStream.ToArray();
                    bodyStream.Dispose();
                }

                switch(opcode) {
                    case WsOpcode.DataText:
                        msg = new WsTextMessage(body);
                        break;

                    case WsOpcode.DataBinary:
                        msg = new WsBinaryMessage(body);
                        break;

                    case WsOpcode.CtrlClose:
                        msg = new WsCloseMessage(body);
                        break;

                    case WsOpcode.CtrlPing:
                        msg = new WsPingMessage(body);
                        break;

                    case WsOpcode.CtrlPong:
                        msg = new WsPongMessage(body);
                        break;

                    default: // fallback, if we end up here something is very fucked
                        throw new WsUnsupportedOpcodeException(opcode);
                }
            } else msg = null;

            return msg;
        }

        public WsMessage Receive() {
            WsMessage msg;
            while((msg = ReadFrame()) == null);
            return msg;
        }

        private void WriteFrameHeader(WsOpcode opcode, long length, bool isFinal, byte[] mask = null) {
            bool shouldMask = mask != null;

            if(isFinal)
                opcode |= WsOpcode.FlagFinal;

            Stream.WriteByte((byte)opcode);

            byte bLen1 = 0;
            if(shouldMask)
                bLen1 |= MASK_FLAG;

            byte[] bLenBuff = WsUtils.FromI64(length);
            if(length < 126) {
                Stream.WriteByte((byte)(bLen1 | bLenBuff[7]));
            } else if(length <= ushort.MaxValue) {
                Stream.WriteByte((byte)(bLen1 | 126));
                Stream.Write(bLenBuff, 6, 2);
            } else {
                Stream.WriteByte((byte)(bLen1 | 127));
                Stream.Write(bLenBuff, 0, 8);
            }

            if(shouldMask)
                Stream.Write(mask, 0, MASK_SIZE);
        }

        private long WriteFrameBody(ReadOnlySpan<byte> body, byte[] mask = null, long offset = 0) {
            if(mask != null) {
                byte[] masked = new byte[body.Length];

                for(int i = 0; i < body.Length; ++i)
                    masked[i] = (byte)(body[i] ^ mask[offset++ % MASK_SIZE]);

                body = masked;
            }

            Stream.Write(body);

            return offset;
        }

        private long WriteFrameBody(Stream body, byte[] mask = null, long offset = 0) {
            bool shouldMask = mask != null;

            int read;
            byte[] buffer = new byte[BUFFER_SIZE];
            while((read = body.Read(buffer, 0, BUFFER_SIZE)) > 0)
                offset = WriteFrameBody(buffer.AsSpan(0, read), mask, offset);

            return offset;
        }

        private void WriteFrame(WsOpcode opcode, ReadOnlySpan<byte> body, bool isFinal) {
            byte[] mask = GenerateMask();
            WriteFrameHeader(opcode, body.Length, isFinal, mask);
            if(body.Length > 0)
                WriteFrameBody(body, mask);
            Stream.Flush();
        }

        private void Write(WsOpcode opcode, ReadOnlySpan<byte> body) {
            if(body.Length > 0xFFFF) {
                WriteFrame(opcode, body.Slice(0, 0xFFFF), false);
                body = body.Slice(0xFFFF);

                while(body.Length > 0xFFFF) {
                    WriteFrame(WsOpcode.DataContinue, body.Slice(0, 0xFFFF), false);
                    body = body.Slice(0xFFFF);
                }

                WriteFrame(WsOpcode.DataContinue, body, true);
            } else
                WriteFrame(opcode, body, true);
        }

        private void Write(WsOpcode opcode, Stream stream) {
            if(stream == null)
                throw new ArgumentNullException(nameof(stream));
            if(!stream.CanRead)
                throw new ArgumentException(@"Provided stream cannot be read.", nameof(stream));

            int read;
            byte[] buffer = new byte[BUFFER_SIZE];

            while((read = stream.Read(buffer, 0, BUFFER_SIZE)) > 0) {
                WriteFrame(opcode, buffer.AsSpan(0, read), false);

                if(opcode != WsOpcode.DataContinue)
                    opcode = WsOpcode.DataContinue;
            }

            // this kinda fucking sucks
            WriteFrame(WsOpcode.CtrlClose, ReadOnlySpan<byte>.Empty, true);
        }

        private void Write(WsOpcode opcode, Stream stream, int length) {
            if(stream == null)
                throw new ArgumentNullException(nameof(stream));
            if(!stream.CanRead)
                throw new ArgumentException(@"Provided stream cannot be read.", nameof(stream));

            int read;
            byte[] buffer = new byte[BUFFER_SIZE];

            if(length > BUFFER_SIZE) {
                int take = BUFFER_SIZE;

                while((read = stream.Read(buffer, 0, take)) > 0) {
                    WriteFrame(opcode, buffer.AsSpan(0, read), false);

                    if(opcode != WsOpcode.DataContinue)
                        opcode = WsOpcode.DataContinue;

                    length -= read;
                    if(take > length)
                        take = length;
                }

                // feel like there'd be a better way to do this
                //  but i feel like assuming that any successful read with something
                //  still coming (read == BUFFER_SIZE) will bite me in the ass later somehow
                WriteFrame(WsOpcode.CtrlClose, Span<byte>.Empty, true);
            } else {
                read = stream.Read(buffer, 0, BUFFER_SIZE);
                if(read > 0)
                    WriteFrame(WsOpcode.DataBinary, buffer.AsSpan(0, read), true);
            }
        }

        public void Send(string text)
            => Write(WsOpcode.DataText, Encoding.UTF8.GetBytes(text));

        public void Send(ReadOnlySpan<byte> buffer)
            => Write(WsOpcode.DataBinary, buffer);

        public void Send(Stream source)
            => Write(WsOpcode.DataBinary, source);

        public void Send(Stream source, int count)
            => Write(WsOpcode.DataBinary, source, count);

        private void WriteControlFrame(WsOpcode opcode) {
            WriteFrameHeader(opcode, 0, true, GenerateMask());
            Stream.Flush();
        }

        private void WriteControlFrame(WsOpcode opcode, ReadOnlySpan<byte> buffer) {
            if(buffer.Length > 125)
                throw new ArgumentException(@"Data may not be more than 125 bytes.", nameof(buffer));

            byte[] mask = GenerateMask();
            WriteFrameHeader(opcode, buffer.Length, true, mask);
            WriteFrameBody(buffer, mask);
            Stream.Flush();
        }

        public void Ping()
            => WriteControlFrame(WsOpcode.CtrlPing);

        public void Ping(ReadOnlySpan<byte> buffer)
            => WriteControlFrame(WsOpcode.CtrlPing, buffer);

        public void Pong()
            => WriteControlFrame(WsOpcode.CtrlPong);

        public void Pong(ReadOnlySpan<byte> buffer)
            => WriteControlFrame(WsOpcode.CtrlPong, buffer);

        public void CloseEmpty() {
            if(IsClosed)
                return;
            IsClosed = true;

            WriteControlFrame(WsOpcode.CtrlClose);
        }

        public void Close(ReadOnlySpan<byte> buffer) {
            if(IsClosed)
                return;
            IsClosed = true;

            WriteControlFrame(WsOpcode.CtrlClose, buffer);
        }

        public void Close(WsCloseReason code)
            => Close(WsUtils.FromU16((ushort)code));

        public void Close(WsCloseReason code, ReadOnlySpan<byte> reason) {
            if(reason.Length > 123)
                throw new ArgumentException(@"Reason may not be more than 123 bytes.", nameof(reason));

            if(IsClosed)
                return;
            IsClosed = true;

            byte[] mask = GenerateMask();
            WriteFrameHeader(WsOpcode.CtrlClose, 2 + reason.Length, true, mask);
            WriteFrameBody(WsUtils.FromU16((ushort)code), mask);
            WriteFrameBody(reason, mask, 2);
            Stream.Flush();
        }

        public void Close(WsCloseReason code, string reason) {
            if(string.IsNullOrEmpty(reason)) {
                Close(code);
                return;
            }

            int length = Encoding.UTF8.GetByteCount(reason);
            if(length > 123)
                throw new ArgumentException(@"Reason string may not exceed 123 bytes in length.", nameof(reason));

            if(IsClosed)
                return;
            IsClosed = true;

            byte[] mask = GenerateMask();
            WriteFrameHeader(WsOpcode.CtrlClose, 2 + reason.Length, true, mask);
            WriteFrameBody(WsUtils.FromU16((ushort)code), mask);
            WriteFrameBody(Encoding.UTF8.GetBytes(reason), mask, 2);
            Stream.Flush();
        }

        private bool IsDisposed;

        ~WsConnection() {
            DoDispose();
        }

        public void Dispose() {
            DoDispose();
            GC.SuppressFinalize(this);
        }

        private void DoDispose() {
            if(IsDisposed)
                return;
            IsDisposed = true;

            Stream.Dispose();
        }
    }
}
