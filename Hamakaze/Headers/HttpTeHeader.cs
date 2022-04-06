using System;
using System.Collections.Generic;
using System.Linq;

namespace Hamakaze.Headers {
    public class HttpTeHeader : HttpHeader {
        public const string NAME = @"TE";

        public override string Name => NAME;
        public override object Value => string.Join(@", ", Encodings);

        public HttpEncoding[] Encodings { get; }

        public HttpTeHeader(string encodings) : this(
            (encodings ?? throw new ArgumentNullException(nameof(encodings))).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        ) { }

        public HttpTeHeader(string[] encodings) : this(
            (encodings ?? throw new ArgumentNullException(nameof(encodings))).Select(HttpEncoding.Parse)
        ) { }

        public HttpTeHeader(IEnumerable<HttpEncoding> encodings) {
            Encodings = (encodings ?? throw new ArgumentNullException(nameof(encodings))).ToArray();
        }
    }
}
