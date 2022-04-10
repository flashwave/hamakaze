namespace Hamakaze.WebSocket {
    public class WsException : HttpException {
        public WsException(string message) : base(message) { }
    }

    public class WsInvalidOpcodeException : WsException {
        public WsInvalidOpcodeException(WsOpcode opcode) : base($@"An invalid WebSocket opcode was encountered: {opcode}.") { }
    }

    public class WsUnsupportedOpcodeException : WsException {
        public WsUnsupportedOpcodeException(WsOpcode opcode) : base($@"An unsupported WebSocket opcode was encountered: {opcode}.") { }
    }

    public class WsInvalidFrameSizeException : WsException {
        public WsInvalidFrameSizeException(long size) : base($@"WebSocket frame size is too large: {size} bytes.") { }
    }

    public class WsUnexpectedContinueException : WsException {
        public WsUnexpectedContinueException() : base(@"A WebSocket continue frame was issued but there is nothing to continue.") { }
    }

    public class WsUnexpectedDataException : WsException {
        public WsUnexpectedDataException() : base(@"A WebSocket data frame was issued while a fragmented frame is being constructed.") { }
    }

    public class WsInvalidControlFrameException : WsException {
        public WsInvalidControlFrameException(string variant) : base($@"An invalid WebSocket control frame was encountered: {variant}") { }
    }

    public class WsClientMutexFailedException : WsException {
        public WsClientMutexFailedException() : base(@"Failed to acquire send mutex.") { }
    }

    public class WsBufferedSendAlreadyActiveException : WsException {
        public WsBufferedSendAlreadyActiveException() : base(@"A buffered websocket send is already in session.") { }
    }

    public class WsBufferedSendInSessionException : WsException {
        public WsBufferedSendInSessionException() : base(@"Cannot send data while a buffered send is in session.") { }
    }
}
