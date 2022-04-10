using System;
using System.Threading;

// todo: sending errors as fake close messages

namespace Hamakaze.WebSocket {
    public class WsClient : IDisposable {
        public WsConnection Connection { get; }
        public bool IsRunning { get; private set; } = true;

        private Thread ReadThread { get; }
        private Action<WsMessage> MessageHandler { get; }
        private Action<Exception> ExceptionHandler { get; }

        private Mutex SendLock { get; }
        private const int TIMEOUT = 60000;

        public WsClient(
            WsConnection connection,
            Action<WsMessage> messageHandler,
            Action<Exception> exceptionHandler
        ) {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            MessageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
            ExceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));

            SendLock = new();

            ReadThread = new(ReadThreadBody) { IsBackground = true };
            ReadThread.Start();
        }

        private void ReadThreadBody() {
            try {
                while(IsRunning)
                    MessageHandler(Connection.Receive());
            } catch(Exception ex) {
                IsRunning = false;
                ExceptionHandler(ex);
            }
        }

        public void Send(string text) {
            try {
                if(!SendLock.WaitOne(TIMEOUT))
                    throw new WsClientMutexFailedException();
                Connection.Send(text);
            } finally {
                SendLock.ReleaseMutex();
            }
        }

        public void Send(object obj) {
            if(obj == null)
                throw new ArgumentNullException(nameof(obj));

            try {
                if(!SendLock.WaitOne(TIMEOUT))
                    throw new WsClientMutexFailedException();
                Connection.Send(obj.ToString());
            } finally {
                SendLock.ReleaseMutex();
            }
        }

        public void Send(ReadOnlySpan<byte> data) {
            if(data == null)
                throw new ArgumentNullException(nameof(data));

            try {
                if(!SendLock.WaitOne(TIMEOUT))
                    throw new WsClientMutexFailedException();
                Connection.Send(data);
            } finally {
                SendLock.ReleaseMutex();
            }
        }

        public void Send(byte[] buffer, int offset, int count) {
            if(buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            try {
                if(!SendLock.WaitOne(TIMEOUT))
                    throw new WsClientMutexFailedException();
                Connection.Send(buffer.AsSpan(offset, count));
            } finally {
                SendLock.ReleaseMutex();
            }
        }

        public void Send(Action<WsBufferedSend> handler) {
            if(handler == null)
                throw new ArgumentNullException(nameof(handler));

            try {
                if(!SendLock.WaitOne(TIMEOUT))
                    throw new WsClientMutexFailedException();
                using(WsBufferedSend bs = Connection.BeginBufferedSend())
                    handler(bs);
            } finally {
                SendLock.ReleaseMutex();
            }
        }

        public void Ping() {
            try {
                if(!SendLock.WaitOne(TIMEOUT))
                    throw new WsClientMutexFailedException();
                Connection.Ping();
            } finally {
                SendLock.ReleaseMutex();
            }
        }

        public void Ping(ReadOnlySpan<byte> data) {
            if(data == null)
                throw new ArgumentNullException(nameof(data));

            try {
                if(!SendLock.WaitOne(TIMEOUT))
                    throw new WsClientMutexFailedException();
                Connection.Ping(data);
            } finally {
                SendLock.ReleaseMutex();
            }
        }

        public void Ping(byte[] buffer, int offset, int length) {
            if(buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            try {
                if(!SendLock.WaitOne(TIMEOUT))
                    throw new WsClientMutexFailedException();
                Connection.Ping(buffer.AsSpan(offset, length));
            } finally {
                SendLock.ReleaseMutex();
            }
        }

        public void Pong() {
            try {
                if(!SendLock.WaitOne(TIMEOUT))
                    throw new WsClientMutexFailedException();
                Connection.Pong();
            } finally {
                SendLock.ReleaseMutex();
            }
        }

        public void Pong(ReadOnlySpan<byte> data) {
            if(data == null)
                throw new ArgumentNullException(nameof(data));

            try {
                if(!SendLock.WaitOne(TIMEOUT))
                    throw new WsClientMutexFailedException();
                Connection.Pong(data);
            } finally {
                SendLock.ReleaseMutex();
            }
        }

        public void Pong(byte[] buffer, int offset, int length) {
            if(buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            try {
                if(!SendLock.WaitOne(TIMEOUT))
                    throw new WsClientMutexFailedException();
                Connection.Pong(buffer.AsSpan(offset, length));
            } finally {
                SendLock.ReleaseMutex();
            }
        }

        public void Close() {
            try {
                if(!SendLock.WaitOne(TIMEOUT))
                    throw new WsClientMutexFailedException();
                Connection.Close(WsCloseReason.NormalClosure);
            } finally {
                SendLock.ReleaseMutex();
            }
        }

        public void CloseEmpty() {
            try {
                if(!SendLock.WaitOne(TIMEOUT))
                    throw new WsClientMutexFailedException();
                Connection.CloseEmpty();
            } finally {
                SendLock.ReleaseMutex();
            }
        }

        public void Close(WsCloseReason opcode) {
            try {
                if(!SendLock.WaitOne(TIMEOUT))
                    throw new WsClientMutexFailedException();
                Connection.Close(opcode);
            } finally {
                SendLock.ReleaseMutex();
            }
        }

        public void Close(string reason) {
            if(reason == null)
                throw new ArgumentNullException(nameof(reason));

            try {
                if(!SendLock.WaitOne(TIMEOUT))
                    throw new WsClientMutexFailedException();
                Connection.Close(WsCloseReason.NormalClosure, reason);
            } finally {
                SendLock.ReleaseMutex();
            }
        }

        public void Close(WsCloseReason opcode, string reason) {
            if(reason == null)
                throw new ArgumentNullException(nameof(reason));

            try {
                if(!SendLock.WaitOne(TIMEOUT))
                    throw new WsClientMutexFailedException();
                Connection.Close(opcode, reason);
            } finally {
                SendLock.ReleaseMutex();
            }
        }

        public void Close(ReadOnlySpan<byte> data) {
            if(data == null)
                throw new ArgumentNullException(nameof(data));

            try {
                if(!SendLock.WaitOne(TIMEOUT))
                    throw new WsClientMutexFailedException();
                Connection.Close(data);
            } finally {
                SendLock.ReleaseMutex();
            }
        }

        public void Close(byte[] buffer, int offset, int length) {
            if(buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            try {
                if(!SendLock.WaitOne(TIMEOUT))
                    throw new WsClientMutexFailedException();
                Connection.Close(buffer.AsSpan(offset, length));
            } finally {
                SendLock.ReleaseMutex();
            }
        }

        public void Close(WsCloseReason opcode, ReadOnlySpan<byte> data) {
            if(data == null)
                throw new ArgumentNullException(nameof(data));

            try {
                if(!SendLock.WaitOne(TIMEOUT))
                    throw new WsClientMutexFailedException();
                Connection.Close(opcode, data);
            } finally {
                SendLock.ReleaseMutex();
            }
        }

        public void Close(WsCloseReason code, byte[] buffer, int offset, int length) {
            if(buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            try {
                if(!SendLock.WaitOne(TIMEOUT))
                    throw new WsClientMutexFailedException();
                Connection.Close(code, buffer.AsSpan(offset, length));
            } finally {
                SendLock.ReleaseMutex();
            }
        }

        private bool IsDisposed;

        ~WsClient() {
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

            SendLock.Dispose();
            Connection.Dispose();
        }
    }
}
