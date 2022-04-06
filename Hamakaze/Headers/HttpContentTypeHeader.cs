using System;

namespace Hamakaze.Headers {
    public class HttpContentTypeHeader : HttpHeader {
        public const string NAME = @"Content-Type";

        public override string Name => NAME;
        public override object Value => MediaType.ToString();

        public HttpMediaType MediaType { get; }

        public HttpContentTypeHeader(string mediaType) {
            MediaType = HttpMediaType.Parse(mediaType ?? throw new ArgumentNullException(nameof(mediaType)));
        }

        public HttpContentTypeHeader(HttpMediaType mediaType) {
            MediaType = mediaType;
        }
    }
}
