using System;

namespace Hamakaze.Headers {
    public class HttpCustomHeader : HttpHeader {
        public override string Name { get; }
        public override object Value { get; }

        public HttpCustomHeader(string name, object value) {
            Name = NormaliseName(name ?? throw new ArgumentNullException(nameof(name)));
            Value = value;
        }
    }
}
