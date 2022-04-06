using System;

namespace Hamakaze {
    public class HttpException : Exception {
        public HttpException(string message) : base(message) { }
    }

    public class HttpConnectionManagerException : HttpException {
        public HttpConnectionManagerException(string message) : base(message) { }
    }
    public class HttpConnectionManagerLockException : HttpConnectionManagerException {
        public HttpConnectionManagerLockException() : base(@"Failed to lock the connection manager in time.") { }
    }

    public class HttpTaskException : HttpException {
        public HttpTaskException(string message) : base(message) { }
    }
    public class HttpTaskAlreadyStartedException : HttpTaskException {
        public HttpTaskAlreadyStartedException() : base(@"Task has already started.") { }
    }
    public class HttpTaskInvalidStateException : HttpTaskException {
        public HttpTaskInvalidStateException() : base(@"Task has ended up in an invalid state.") { }
    }
    public class HttpTaskNoAddressesException : HttpTaskException {
        public HttpTaskNoAddressesException() : base(@"Could not find any addresses for this host.") { }
    }
    public class HttpTaskNoConnectionException : HttpTaskException {
        public HttpTaskNoConnectionException() : base(@"Was unable to create a connection with this host.") { }
    }
    public class HttpTaskRequestFailedException : HttpTaskException {
        public HttpTaskRequestFailedException() : base(@"Request failed for unknown reasons.") { }
    }

    public class HttpTaskManagerException : HttpException {
        public HttpTaskManagerException(string message) : base(message) { }
    }
    public class HttpTaskManagerLockException : HttpTaskManagerException {
        public HttpTaskManagerLockException() : base(@"Failed to reserve a thread.") { }
    }
}
