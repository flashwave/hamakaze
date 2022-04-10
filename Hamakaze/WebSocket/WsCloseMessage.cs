using System;
using System.Text;

namespace Hamakaze.WebSocket {
    public class WsCloseMessage : WsMessage, IHasBinaryData {
        public WsCloseReason Reason { get; }
        public string ReasonPhrase { get; }
        public byte[] Data { get; }

        public WsCloseMessage(WsCloseReason reason) {
            Reason = reason;
            ReasonPhrase = string.Empty;
            Data = Array.Empty<byte>();
        }

        public WsCloseMessage(byte[] data) {
            if(data == null) {
                Reason = WsCloseReason.NoStatus;
                ReasonPhrase = string.Empty;
                Data = Array.Empty<byte>();
            } else {
                Reason = (WsCloseReason)WsUtils.ToU16(data);
                Data = data;

                if(data.Length > 2)
                    try {
                        ReasonPhrase = Encoding.UTF8.GetString(data, 2, data.Length - 2);
                    } catch {
                        ReasonPhrase = string.Empty;
                    }
                else
                    ReasonPhrase = string.Empty;
            }
        }
    }
}
