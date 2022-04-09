using System;

namespace Hamakaze {
    public class HttpException : Exception {
        public HttpException(string message) : base(message) { }
    }

    public class HttpUpgradeException : HttpException {
        public HttpUpgradeException(string message) : base(message) { }
    }
    public class HttpUpgradeProtocolVersionException : HttpUpgradeException {
        public HttpUpgradeProtocolVersionException(string expectedVersion, string givenVersion)
            : base($@"Server HTTP version ({givenVersion}) is lower than what is expected {expectedVersion}.") { }
    }
    public class HttpUpgradeUnexpectedStatusException : HttpUpgradeException {
        public HttpUpgradeUnexpectedStatusException(int statusCode) : base($@"Expected HTTP status code 101, got {statusCode}.") { }
    }
    public class HttpUpgradeUnexpectedHeaderException : HttpUpgradeException {
        public HttpUpgradeUnexpectedHeaderException(string header, string expected, string given)
            : base($@"Unexpected {header} header value ""{given}"", expected ""{expected}"".") { }
    }
    public class HttpUpgradeInvalidHashException : HttpUpgradeException {
        public HttpUpgradeInvalidHashException(string expected, string given)
            : base($@"Server sent invalid hash ""{given}"", expected ""{expected}"".") { }
    }

    public class HttpConnectionException : HttpException {
        public HttpConnectionException(string message) : base(message) { }
    }
    public class HttpConnectionAlreadyUpgradedException : HttpConnectionException {
        public HttpConnectionAlreadyUpgradedException() : base(@"This connection has already been upgraded.") { }
    }

    public class HttpConnectionManagerException : HttpException {
        public HttpConnectionManagerException(string message) : base(message) { }
    }
    public class HttpConnectionManagerLockException : HttpConnectionManagerException {
        public HttpConnectionManagerLockException() : base(@"Failed to lock the connection manager in time.") { }
    }

    public class HttpRequestMessageException : HttpException {
        public HttpRequestMessageException(string message) : base(message) { }
    }
    public class HttpRequestMessageStreamException : HttpRequestMessageException {
        public HttpRequestMessageStreamException() : base(@"Provided Stream is not writable.") { }
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
