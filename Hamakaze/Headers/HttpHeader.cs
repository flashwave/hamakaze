using System;
using System.Globalization;

namespace Hamakaze.Headers {
    public abstract class HttpHeader {
        public abstract string Name { get; }
        public abstract object Value { get; }

        public override string ToString() {
            return string.Format(@"{0}: {1}", Name, Value);
        }

        public static string NormaliseName(string name) {
            if(string.IsNullOrWhiteSpace(name))
                return string.Empty;

            string[] parts = name.ToLowerInvariant().Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for(int i = 0; i < parts.Length; ++i)
                parts[i] = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(parts[i]);
            return string.Join('-', parts);
        }

        public static HttpHeader Create(string name, object value) {
            return name switch {
                HttpTeHeader.NAME => new HttpTeHeader(value.ToString()),
                HttpDateHeader.NAME => new HttpDateHeader(value.ToString()),
                HttpHostHeader.NAME => new HttpHostHeader(value.ToString()),
                HttpServerHeader.NAME => new HttpServerHeader(value.ToString()),
                HttpUserAgentHeader.NAME => new HttpUserAgentHeader(value.ToString()),
                HttpKeepAliveHeader.NAME => new HttpKeepAliveHeader(value.ToString()),
                HttpConnectionHeader.NAME => new HttpConnectionHeader(value.ToString()),
                HttpContentTypeHeader.NAME => new HttpContentTypeHeader(value.ToString()),
                HttpContentLengthHeader.NAME => new HttpContentLengthHeader(value.ToString()),
                HttpAcceptEncodingHeader.NAME => new HttpAcceptEncodingHeader(value.ToString()),
                HttpContentEncodingHeader.NAME => new HttpContentEncodingHeader(value.ToString()),
                HttpTransferEncodingHeader.NAME => new HttpTransferEncodingHeader(value.ToString()),
                _ => new HttpCustomHeader(name, value),
            };
        }
    }
}
