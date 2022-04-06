using System;

namespace Hamakaze.Headers {
    public class HttpConnectionHeader : HttpHeader {
        public const string NAME = @"Connection";

        public override string Name => NAME;
        public override object Value { get; }

        public const string CLOSE = @"close";
        public const string KEEP_ALIVE = @"keep-alive";

        public HttpConnectionHeader(string mode) {
            Value = mode ?? throw new ArgumentNullException(nameof(mode));
        }
    }
}
