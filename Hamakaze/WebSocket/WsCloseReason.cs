namespace Hamakaze.WebSocket {
    public enum WsCloseReason : ushort {
        NormalClosure = 1000,
        GoingAway = 1001,
        ProtocolError = 1002,
        InvalidData = 1003,
        NoStatus = 1005, // virtual -> no data in close frame
        AbnormalClosure = 1006, // virtual -> connection dropped
        MalformedData = 1007,
        PolicyViolation = 1008,
        FrameTooLarge = 1009,
        MissingExtension = 1010,
        UnexpectedCondition = 1011,
        TlsHandshakeFailed = 1015, // virtual -> obvious
    }
}
