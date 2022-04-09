using Hamakaze.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Hamakaze {
    public class HttpTask {
        public TaskState State { get; private set; } = TaskState.Initial;

        public bool IsStarted
            => State != TaskState.Initial;
        public bool IsFinished
            => State == TaskState.Finished;
        public bool IsCancelled
            => State == TaskState.Cancelled;
        public bool IsErrored
            => Exception != null;

        public Exception Exception { get; private set; }

        public HttpRequestMessage Request { get; }
        public HttpResponseMessage Response { get; private set; }
        private HttpConnectionManager Connections { get; }

        private IEnumerable<IPAddress> Addresses { get; set; }
        public HttpConnection Connection { get; private set; }

        public bool DisposeRequest { get; set; }
        public bool DisposeResponse { get; set; }

        public event Action<HttpTask, HttpResponseMessage> OnComplete;
        public event Action<HttpTask, Exception> OnError;
        public event Action<HttpTask> OnCancel;
        public event Action<HttpTask, long, long> OnUploadProgress;
        public event Action<HttpTask, long, long> OnDownloadProgress;
        public event Action<HttpTask, TaskState> OnStateChange;

        public HttpTask(HttpConnectionManager conns, HttpRequestMessage request, bool disposeRequest, bool disposeResponse) {
            Connections = conns ?? throw new ArgumentNullException(nameof(conns));
            Request = request ?? throw new ArgumentNullException(nameof(request));
            DisposeRequest = disposeRequest;
            DisposeResponse = disposeResponse;
        }

        public void Run() {
            if(IsStarted)
                throw new HttpTaskAlreadyStartedException();
            while(NextStep());
        }

        public void Cancel() {
            State = TaskState.Cancelled;
            OnStateChange?.Invoke(this, State);
            OnCancel?.Invoke(this);
            if(DisposeResponse)
                Response?.Dispose();
            if(DisposeRequest)
                Request?.Dispose();
        }

        private void Error(Exception ex) {
            Exception = ex;
            OnError?.Invoke(this, ex);
            Cancel();
        }

        public bool NextStep() {
            if(IsCancelled)
                return false;

            try {
                switch(State) {
                    case TaskState.Initial:
                        State = TaskState.Lookup;
                        OnStateChange?.Invoke(this, State);
                        DoLookup();
                        break;
                    case TaskState.Lookup:
                        State = TaskState.Request;
                        OnStateChange?.Invoke(this, State);
                        DoRequest();
                        break;
                    case TaskState.Request:
                        State = TaskState.Response;
                        OnStateChange?.Invoke(this, State);
                        DoResponse();
                        break;
                    case TaskState.Response:
                        State = TaskState.Finished;
                        OnStateChange?.Invoke(this, State);
                        OnComplete?.Invoke(this, Response);
                        if(DisposeResponse)
                            Response?.Dispose();
                        if(DisposeRequest)
                            Request?.Dispose();
                        return false;
                    default:
                        throw new HttpTaskInvalidStateException();
                }
            } catch(Exception ex) {
                Error(ex);
                return false;
            }

            return true;
        }

        private void DoLookup() {
            Addresses = Dns.GetHostAddresses(Request.Host);

            if(!Addresses.Any())
                throw new HttpTaskNoAddressesException();
        }

        private void DoRequest() {
            Queue<IPAddress> addresses = new(Addresses);

            while(addresses.TryDequeue(out IPAddress addr)) {
                int tries = 0;
                IPEndPoint endPoint = new(addr, Request.Port);

                Connection = Connections.GetConnection(Request.Host, endPoint, Request.IsSecure);

            retry:
                ++tries;
                try {
                    Request.WriteTo(Connection.Stream, (p, t) => OnUploadProgress?.Invoke(this, p, t));
                    break;
                } catch(HttpRequestMessageStreamException) {
                    Connection.Dispose();
                    Connection = Connections.GetConnection(Request.Host, endPoint, Request.IsSecure);

                    if(tries < 2)
                        goto retry;

                    if(!addresses.Any())
                        throw;
                } finally {
                    Connection.MarkUsed();
                }
            }

            if(Connection == null)
                throw new HttpTaskNoConnectionException();
        }

        private void DoResponse() {
            Response = HttpResponseMessage.ReadFrom(Connection.Stream, (p, t) => OnDownloadProgress?.Invoke(this, p, t));

            if(Response.Connection == HttpConnectionHeader.CLOSE
                || Response.ProtocolVersion.CompareTo(@"1.1") < 0)
                Connection.Dispose();
            if(Response == null)
                throw new HttpTaskRequestFailedException();

            HttpKeepAliveHeader hkah = Response.Headers.Where(x => x.Name == HttpKeepAliveHeader.NAME).Cast<HttpKeepAliveHeader>().FirstOrDefault();
            if(hkah != null) {
                Connection.MaxIdle = hkah.MaxIdle;
                Connection.MaxRequests = hkah.MaxRequests;
            }

            Connection.Release();
        }

        public enum TaskState {
            Initial = 0,
            Lookup = 10,
            Request = 20,
            Response = 30,
            Finished = 40,

            Cancelled = -1,
        }
    }
}
