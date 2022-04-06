using System;

namespace Hamakaze.Headers {
    public class HttpTransferEncodingHeader : HttpHeader {
        public const string NAME = @"Transfer-Encoding";

        public override string Name => NAME;
        public override object Value => string.Join(@", ", Encodings);

        public string[] Encodings { get; }

        public HttpTransferEncodingHeader(string encodings) : this(
            (encodings ?? throw new ArgumentNullException(nameof(encodings))).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        ) {}

        public HttpTransferEncodingHeader(string[] encodings) {
            Encodings = encodings ?? throw new ArgumentNullException(nameof(encodings));
        }
    }
}
