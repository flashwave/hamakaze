using System.Text;

namespace Hamakaze.WebSocket {
    public class WsTextMessage : WsMessage {
        public string Text { get; }

        public WsTextMessage(byte[] data) {
            if(data?.Length > 0)
                Text = Encoding.UTF8.GetString(data);
            else
                Text = string.Empty;
        }

        public static implicit operator string(WsTextMessage msg) => msg.Text;

        public override string ToString() {
            return Text;
        }
    }
}
