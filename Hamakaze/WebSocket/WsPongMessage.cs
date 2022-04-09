using System;

namespace Hamakaze.WebSocket {
    public class WsPongMessage : WsMessage {
        public byte[] Data { get; }

        public WsPongMessage(byte[] data) {
            Data = data ?? Array.Empty<byte>();
        }
    }
}
