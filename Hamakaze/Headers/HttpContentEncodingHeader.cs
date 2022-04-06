using System;

namespace Hamakaze.Headers {
    public class HttpContentEncodingHeader : HttpHeader {
        public const string NAME = @"Content-Encoding";

        public override string Name => NAME;
        public override object Value => string.Join(@", ", Encodings);

        public string[] Encodings { get; }

        public HttpContentEncodingHeader(string encodings) : this(
            (encodings ?? throw new ArgumentNullException(nameof(encodings))).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        ) { }

        public HttpContentEncodingHeader(string[] encodings) {
            Encodings = encodings ?? throw new ArgumentNullException(nameof(encodings));
        }
    }
}
