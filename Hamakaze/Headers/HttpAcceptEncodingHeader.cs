using System;
using System.Collections.Generic;
using System.Linq;

namespace Hamakaze.Headers {
    public class HttpAcceptEncodingHeader : HttpHeader {
        public const string NAME = @"Accept-Encoding";

        public override string Name => NAME;
        public override object Value => string.Join(@", ", Encodings);

        public HttpEncoding[] Encodings { get; }

        public HttpAcceptEncodingHeader(string encodings) : this(
            (encodings ?? throw new ArgumentNullException(nameof(encodings))).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        ) { }

        public HttpAcceptEncodingHeader(string[] encodings) : this(
            (encodings ?? throw new ArgumentNullException(nameof(encodings))).Select(HttpEncoding.Parse)
        ) {}

        public HttpAcceptEncodingHeader(IEnumerable<HttpEncoding> encodings) {
            Encodings = (encodings ?? throw new ArgumentNullException(nameof(encodings))).ToArray();
        }
    }
}
