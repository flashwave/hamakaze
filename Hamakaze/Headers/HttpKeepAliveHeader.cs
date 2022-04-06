using System;
using System.Collections.Generic;

namespace Hamakaze.Headers {
    public class HttpKeepAliveHeader : HttpHeader {
        public const string NAME = @"Keep-Alive";

        public override string Name => NAME;
        public override object Value {
            get {
                List<string> parts = new();
                if(MaxIdle != TimeSpan.MaxValue)
                    parts.Add(string.Format(@"timeout={0}", MaxIdle.TotalSeconds));
                if(MaxRequests >= 0)
                    parts.Add(string.Format(@"max={0}", MaxRequests));
                return string.Join(@", ", parts);
            }
        }

        public TimeSpan MaxIdle { get; } = TimeSpan.MaxValue;
        public int MaxRequests { get; } = -1;

        public HttpKeepAliveHeader(string value) {
            IEnumerable<string> kvps = (value ?? throw new ArgumentNullException(nameof(value))).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach(string kvp in kvps) {
                string[] parts = kvp.Split('=', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if(parts[0] == @"timeout" && int.TryParse(parts[1], out int timeout))
                    MaxIdle = TimeSpan.FromSeconds(timeout);
                else if(parts[0] == @"max" && int.TryParse(parts[1], out int max))
                    MaxRequests = max;
            }
        }
    }
}
