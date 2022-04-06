using Hamakaze.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Hamakaze {
    public abstract class HttpMessage : IDisposable {
        public abstract string ProtocolVersion { get; }
        public abstract IEnumerable<HttpHeader> Headers { get; }
        public abstract Stream Body { get; }

        public virtual bool HasBody => Body != null;

        protected bool OwnsBodyStream { get; set; }

        public virtual IEnumerable<HttpHeader> GetHeader(string header) {
            header = HttpHeader.NormaliseName(header);
            return Headers.Where(h => h.Name == header);
        }

        public virtual bool HasHeader(string header) {
            header = HttpHeader.NormaliseName(header);
            return Headers.Any(h => h.Name == header);
        }

        public virtual string GetHeaderLine(string header) {
            return string.Join(@", ", GetHeader(header).Select(h => h.Value));
        }

        private bool IsDisposed;
        ~HttpMessage()
            => DoDispose();
        public void Dispose() {
            DoDispose();
            GC.SuppressFinalize(this);
        }
        protected void DoDispose() {
            if(IsDisposed)
                return;
            IsDisposed = true;
            if(OwnsBodyStream && Body != null)
                Body.Dispose();
        }
    }
}
