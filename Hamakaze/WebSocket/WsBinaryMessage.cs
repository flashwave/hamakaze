using System;

namespace Hamakaze.WebSocket {
    public class WsBinaryMessage : WsMessage {
        public byte[] Data { get; }

        public WsBinaryMessage(byte[] data) {
            Data = data ?? Array.Empty<byte>();
        }
    }
}
