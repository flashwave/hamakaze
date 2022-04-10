using System;

namespace Hamakaze.WebSocket {
    public class WsPingMessage : WsMessage, IHasBinaryData {
        public byte[] Data { get; }

        public WsPingMessage(byte[] data) {
            Data = data ?? Array.Empty<byte>();
        }
    }
}
