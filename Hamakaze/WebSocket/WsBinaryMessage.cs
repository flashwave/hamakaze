using System;

namespace Hamakaze.WebSocket {
    public class WsBinaryMessage : WsMessage, IHasBinaryData {
        public byte[] Data { get; }

        public WsBinaryMessage(byte[] data) {
            Data = data ?? Array.Empty<byte>();
        }
    }
}
