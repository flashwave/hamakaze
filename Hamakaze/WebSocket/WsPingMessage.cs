using System;

namespace Hamakaze.WebSocket {
    public class WsPingMessage : WsMessage {
        public byte[] Data { get; }

        public WsPingMessage(byte[] data) {
            Data = data ?? Array.Empty<byte>();
        }
    }
}
