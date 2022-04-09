using System;
using System.Threading;

namespace Hamakaze.WebSocket {
    public class WsClient : IDisposable {
        public WsConnection Connection { get; }
        public bool IsRunning { get; private set; } = true;

        private Thread ReadThread { get; }
        private Action<WsMessage> MessageHandler { get; }
        private Action<Exception> ExceptionHandler { get; }

        private Mutex SendLock { get; }

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
            Connection.Send(text);
        }

        public void Send(object obj) {
            if(obj == null)
                throw new ArgumentNullException(nameof(obj));

            Connection.Send(obj.ToString());
        }

        public void Send(ReadOnlySpan<byte> data) {
            Connection.Send(data);
        }

        public void Send(byte[] buffer, int offset, int count) {
            if(buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            Connection.Send(buffer.AsSpan(offset, count));
        }

        public void Ping() {
            Connection.Ping();
        }

        public void Ping(ReadOnlySpan<byte> data) {
            Connection.Ping(data);
        }

        public void Ping(byte[] buffer, int offset, int length) {
            if(buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            Connection.Ping(buffer.AsSpan(offset, length));
        }

        public void Pong() {
            Connection.Pong();
        }

        public void Pong(ReadOnlySpan<byte> data) {
            Connection.Pong(data);
        }

        public void Pong(byte[] buffer, int offset, int length) {
            if(buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            Pong(buffer.AsSpan(offset, length));
        }

        public void Close() {
            Connection.Close(WsCloseReason.NormalClosure);
        }

        public void CloseEmpty() {
            Connection.CloseEmpty();
        }

        public void Close(string reason) {
            Connection.Close(WsCloseReason.NormalClosure, reason);
        }

        public void Close(byte[] buffer, int offset, int length) {
            if(buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            Connection.Close(buffer.AsSpan(offset, length));
        }

        public void Close(WsCloseReason code, byte[] buffer, int offset, int length) {
            if(buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            Connection.Close(code, buffer.AsSpan(offset, length));
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
