using System;

namespace Hamakaze.WebSocket {
    public class WsBufferedSend : IDisposable {
        private WsConnection Connection { get; }

        internal WsBufferedSend(WsConnection conn) {
            Connection = conn ?? throw new ArgumentNullException(nameof(conn));
        }

        public void SendPart(ReadOnlySpan<byte> data)
            => Connection.WriteFrame(WsOpcode.DataBinary, data, false);

        public void SendFinalPart(ReadOnlySpan<byte> data)
            => Connection.WriteFrame(WsOpcode.DataBinary, data, true);

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

            Connection.EndBufferedSend();
        }
    }
}
