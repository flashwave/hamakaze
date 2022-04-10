using System;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography;
using System.Text;

namespace Hamakaze.WebSocket {
    public class WsConnection : IDisposable {
        public Stream Stream { get; }

        public bool IsSecure { get; }
        public bool IsClosed { get; private set; }

        private const byte MASK_FLAG = 0x80;
        private const int MASK_SIZE = 4;

        private WsOpcode FragmentType = 0;
        private MemoryStream FragmentStream;

        private WsBufferedSend BufferedSend;

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

        private (WsOpcode opcode, int length, bool isFinal, byte[] mask) ReadFrameHeader() {
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
            // UPDATE: decided to put the max at 2^32-1
            // it's still more than you should ever need for a single frame
            //  and it makes working with the number within a .NET context
            //  less of a bother.
            if(length < 0 || length > int.MaxValue)
                throw new WsInvalidFrameSizeException(length);

            byte[] mask = null;

            if(isMasked) {
                StrictRead(buffer, 0, MASK_SIZE);
                mask = buffer;
            }

            return (opcode, (int)length, isFinal, mask);
        }

        private int ReadFrameBody(byte[] target, int length, byte[] mask, int offset = 0) {
            if(target == null)
                throw new ArgumentNullException(nameof(target));

            bool isMasked = mask != null;

            int read;
            const int bufferSize = 0x1000;
            int take = length > bufferSize ? bufferSize : (int)length;

            while(length > 0) {
                read = Stream.Read(target, offset, take);

                if(isMasked)
                    for(int i = 0; i < read; ++i) {
                        int o = offset + i;
                        target[o] ^= mask[o % MASK_SIZE];
                    }

                length -= read;
                offset += read;

                if(take > length)
                    take = (int)length;
            }

            return offset;
        }

        private WsMessage ReadFrame() {
            (WsOpcode opcode, int length, bool isFinal, byte[] mask) = ReadFrameHeader();

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

            byte[] body = length < 1 ? null : new byte[length];

            if(hasBody) {
                ReadFrameBody(body, length, mask);

                if(canFragment) {
                    if(isContinue) {
                        if(FragmentType == 0)
                            throw new WsUnexpectedContinueException();

                        opcode = FragmentType;

                        FragmentStream ??= new();
                        FragmentStream.Write(body, 0, length);
                    } else {
                        if(FragmentType != 0)
                            throw new WsUnexpectedDataException();

                        if(!isFinal) {
                            FragmentType = opcode;
                            FragmentStream = new();
                            FragmentStream.Write(body, 0, length);
                        }
                    }
                }
            }

            WsMessage msg;

            if(isFinal) {
                if(canFragment && isContinue) {
                    FragmentType = 0;

                    body = FragmentStream.ToArray();
                    FragmentStream.Dispose();
                    FragmentStream = null;
                }

                msg = opcode switch {
                    WsOpcode.DataText => new WsTextMessage(body),
                    WsOpcode.DataBinary => new WsBinaryMessage(body),

                    WsOpcode.CtrlClose => new WsCloseMessage(body),
                    WsOpcode.CtrlPing => new WsPingMessage(body),
                    WsOpcode.CtrlPong => new WsPongMessage(body),

                    // fallback, if we end up here something is very fucked
                    _ => throw new WsUnsupportedOpcodeException(opcode),
                };
            } else msg = null;

            return msg;
        }

        public WsMessage Receive() {
            WsMessage msg;
            while((msg = ReadFrame()) == null);
            return msg;
        }

        private void WriteFrameHeader(WsOpcode opcode, int length, bool isFinal, byte[] mask = null) {
            if(length < 0 || length > int.MaxValue)
                throw new WsInvalidFrameSizeException(length);

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
            Stream.Flush();
        }

        private int WriteFrameBody(ReadOnlySpan<byte> body, byte[] mask = null, int offset = 0) {
            if(body == null)
                throw new ArgumentNullException(nameof(body));

            if(mask != null) {
                byte[] masked = new byte[body.Length];

                for(int i = 0; i < body.Length; ++i)
                    masked[i] = (byte)(body[i] ^ mask[offset++ % MASK_SIZE]);

                body = masked;
            }

            Stream.Write(body);
            Stream.Flush();

            return offset;
        }

        internal void WriteFrame(WsOpcode opcode, ReadOnlySpan<byte> body, bool isFinal) {
            if(body == null)
                throw new ArgumentNullException(nameof(body));

            byte[] mask = GenerateMask();
            WriteFrameHeader(opcode, body.Length, isFinal, mask);
            if(body.Length > 0)
                WriteFrameBody(body, mask);
        }

        private void WriteData(WsOpcode opcode, ReadOnlySpan<byte> body) {
            if(body == null)
                throw new ArgumentNullException(nameof(body));
            if(BufferedSend != null)
                throw new WsBufferedSendInSessionException();

            if(body.Length > ushort.MaxValue) {
                WriteFrame(opcode, body.Slice(0, ushort.MaxValue), false);
                body = body.Slice(ushort.MaxValue);

                while(body.Length > ushort.MaxValue) {
                    WriteFrame(WsOpcode.DataContinue, body.Slice(0, ushort.MaxValue), false);
                    body = body.Slice(ushort.MaxValue);
                }

                WriteFrame(WsOpcode.DataContinue, body, true);
            } else
                WriteFrame(opcode, body, true);
        }

        public void Send(string text)
            => WriteData(WsOpcode.DataText, Encoding.UTF8.GetBytes(text));

        public void Send(ReadOnlySpan<byte> buffer)
            => WriteData(WsOpcode.DataBinary, buffer);

        public WsBufferedSend BeginBufferedSend() {
            if(BufferedSend != null)
                throw new WsBufferedSendAlreadyActiveException();
            return BufferedSend = new(this);
        }

        // this method should only be called from within WsBufferedSend.Dispose
        internal void EndBufferedSend() {
            BufferedSend = null;
        }

        private void WriteControl(WsOpcode opcode)
            => WriteFrameHeader(opcode, 0, true, GenerateMask());

        private void WriteControl(WsOpcode opcode, ReadOnlySpan<byte> buffer) {
            if(buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if(buffer.Length > 125)
                throw new ArgumentException(@"Data may not be more than 125 bytes.", nameof(buffer));

            byte[] mask = GenerateMask();
            WriteFrameHeader(opcode, buffer.Length, true, mask);
            WriteFrameBody(buffer, mask);
        }

        public void Ping()
            => WriteControl(WsOpcode.CtrlPing);

        public void Ping(ReadOnlySpan<byte> buffer)
            => WriteControl(WsOpcode.CtrlPing, buffer);

        public void Pong()
            => WriteControl(WsOpcode.CtrlPong);

        public void Pong(ReadOnlySpan<byte> buffer)
            => WriteControl(WsOpcode.CtrlPong, buffer);

        public void CloseEmpty() {
            if(IsClosed)
                return;
            IsClosed = true;

            WriteControl(WsOpcode.CtrlClose);
        }

        public void Close(ReadOnlySpan<byte> buffer) {
            if(buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if(IsClosed)
                return;
            IsClosed = true;

            WriteControl(WsOpcode.CtrlClose, buffer);
        }

        public void Close(WsCloseReason code)
            => Close(WsUtils.FromU16((ushort)code));

        public void Close(WsCloseReason code, ReadOnlySpan<byte> reason) {
            if(reason == null)
                throw new ArgumentNullException(nameof(reason));
            if(reason.Length > 123)
                throw new ArgumentException(@"Reason may not be more than 123 bytes.", nameof(reason));

            if(IsClosed)
                return;
            IsClosed = true;

            byte[] mask = GenerateMask();
            WriteFrameHeader(WsOpcode.CtrlClose, 2 + reason.Length, true, mask);
            WriteFrameBody(WsUtils.FromU16((ushort)code), mask);
            WriteFrameBody(reason, mask, 2);
        }

        public void Close(WsCloseReason code, string reason) {
            if(reason == null)
                throw new ArgumentNullException(nameof(reason));

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

            BufferedSend?.Dispose();
            FragmentStream?.Dispose();
            Stream.Dispose();
        }
    }
}
