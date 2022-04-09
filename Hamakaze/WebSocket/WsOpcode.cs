namespace Hamakaze.WebSocket {
    public enum WsOpcode : byte {
        DataContinue = 0x00,
        DataText = 0x01,
        DataBinary = 0x02,

        CtrlClose = 0x08,
        CtrlPing = 0x09,
        CtrlPong = 0x0A,

        FlagFinal = 0x80,
    }
}
