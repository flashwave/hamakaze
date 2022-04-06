using System;
using System.Globalization;

namespace Hamakaze.Headers {
    public class HttpDateHeader : HttpHeader {
        public const string NAME = @"Date";

        public override string Name => NAME;
        public override object Value { get; }

        public DateTimeOffset DateTime { get; }

        public HttpDateHeader(string dateString) {
            Value = dateString ?? throw new ArgumentNullException(nameof(dateString));
            DateTime = DateTimeOffset.ParseExact(dateString, @"r", CultureInfo.InvariantCulture);
        }
    }
}
