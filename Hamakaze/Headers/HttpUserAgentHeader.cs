using System;

namespace Hamakaze.Headers {
    public class HttpUserAgentHeader : HttpHeader {
        public const string NAME = @"User-Agent";

        public override string Name => NAME;
        public override object Value { get; }
        
        public HttpUserAgentHeader(string userAgent) {
            if(userAgent == null)
                throw new ArgumentNullException(nameof(userAgent));

            if(string.IsNullOrWhiteSpace(userAgent) || userAgent.Equals(HttpClient.USER_AGENT))
                Value = HttpClient.USER_AGENT;
            else
                Value = string.Format(@"{0} {1}", userAgent, HttpClient.USER_AGENT);
        }
    }
}
