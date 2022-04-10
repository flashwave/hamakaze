using System;

namespace Hamakaze.WebSocket {
    public class WsPongMessage : WsMessage, IHasBinaryData {
        public byte[] Data { get; }

        public WsPongMessage(byte[] data) {
            Data = data ?? Array.Empty<byte>();
        }
    }
}
