using System;

namespace Hamakaze.WebSocket {
    public class WsBufferedSend : IDisposable {
        private WsConnection Connection { get; }

        internal WsBufferedSend(WsConnection conn) {
            Connection = conn ?? throw new ArgumentNullException(nameof(conn));
        }

        //

        private bool IsDisposed;

        ~WsBufferedSend() {
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

        }
    }
}
