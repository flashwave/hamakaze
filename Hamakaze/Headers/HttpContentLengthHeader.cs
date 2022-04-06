using System;
using System.IO;

namespace Hamakaze.Headers {
    public class HttpContentLengthHeader : HttpHeader {
        public const string NAME = @"Content-Length";

        public override string Name => NAME;
        public override object Value => Stream?.Length ?? Length;

        private Stream Stream { get; }
        private long Length { get; }

        public HttpContentLengthHeader(Stream stream) {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            if(!stream.CanRead || !stream.CanSeek)
                throw new ArgumentException(@"Body must readable and seekable.", nameof(stream));
        }

        public HttpContentLengthHeader(long length) {
            Length = length;
        }

        public HttpContentLengthHeader(string length) {
            if(!long.TryParse(length, out long ll))
                throw new ArgumentException(@"Invalid length value.", nameof(length));
            Length = ll;
        }
    }
}
