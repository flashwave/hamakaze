using Hamakaze.Headers;
using Hamakaze.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Hamakaze {
    public class HttpClient : IDisposable {
        public const string PRODUCT_STRING = @"HMKZ";
        public const string VERSION_MAJOR = @"1";
        public const string VERSION_MINOR = @"1";
        public const string USER_AGENT = PRODUCT_STRING + @"/" + VERSION_MAJOR + @"." + VERSION_MINOR;

        private const string WS_GUID = @"258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        private const string WS_PROTO = @"websocket";
        private const int WS_RNG = 16;

        private static HttpClient InstanceValue { get; set; }
        public static HttpClient Instance {
            get {
                if(InstanceValue == null)
                    InstanceValue = new HttpClient();
                return InstanceValue;
            }
        }

        private HttpConnectionManager Connections { get; }
        private HttpTaskManager Tasks { get; }

        public string DefaultUserAgent { get; set; } = USER_AGENT;
        public bool ReuseConnections { get; set; } = true;
        public IEnumerable<HttpEncoding> AcceptedEncodings { get; set; } = new[] { HttpEncoding.GZip, HttpEncoding.Deflate, HttpEncoding.Brotli };

        public HttpClient() {
            Connections = new HttpConnectionManager();
            Tasks = new HttpTaskManager();
        }

        public HttpTask CreateTask(
            HttpRequestMessage request,
            Action<HttpTask, HttpResponseMessage> onComplete = null,
            Action<HttpTask, Exception> onError = null,
            Action<HttpTask> onCancel = null,
            Action<HttpTask, long, long> onDownloadProgress = null,
            Action<HttpTask, long, long> onUploadProgress = null,
            Action<HttpTask, HttpTask.TaskState> onStateChange = null,
            bool disposeRequest = true,
            bool disposeResponse = true
        ) {
            if(request == null)
                throw new ArgumentNullException(nameof(request));
            if(string.IsNullOrWhiteSpace(request.UserAgent))
                request.UserAgent = DefaultUserAgent;
            if(!request.HasHeader(HttpAcceptEncodingHeader.NAME))
                request.AcceptedEncodings = AcceptedEncodings;
            if(!request.HasHeader(HttpConnectionHeader.NAME))
                request.Connection = ReuseConnections ? HttpConnectionHeader.KEEP_ALIVE : HttpConnectionHeader.CLOSE;

            HttpTask task = new(Connections, request, disposeRequest, disposeResponse);

            if(onComplete != null)
                task.OnComplete += onComplete;
            if(onError != null)
                task.OnError += onError;
            if(onCancel != null)
                task.OnCancel += onCancel;
            if(onDownloadProgress != null)
                task.OnDownloadProgress += onDownloadProgress;
            if(onUploadProgress != null)
                task.OnUploadProgress += onUploadProgress;
            if(onStateChange != null)
                task.OnStateChange += onStateChange;

            return task;
        }

        public void RunTask(HttpTask task) {
            Tasks.RunTask(task);
        }

        public void SendRequest(
            HttpRequestMessage request,
            Action<HttpTask, HttpResponseMessage> onComplete = null,
            Action<HttpTask, Exception> onError = null,
            Action<HttpTask> onCancel = null,
            Action<HttpTask, long, long> onDownloadProgress = null,
            Action<HttpTask, long, long> onUploadProgress = null,
            Action<HttpTask, HttpTask.TaskState> onStateChange = null,
            bool disposeRequest = true,
            bool disposeResponse = true
        ) {
            RunTask(CreateTask(request, onComplete, onError, onCancel, onDownloadProgress, onUploadProgress, onStateChange, disposeRequest, disposeResponse));
        }

        public void CreateWsClient(
            string url,
            Action<WsClient> onOpen,
            Action<WsMessage> onMessage,
            Action<Exception> onError,
            IEnumerable<string> protocols = null,
            Action<HttpResponseMessage> onResponse = null,
            bool disposeRequest = true,
            bool disposeResponse = true
        ) => CreateWsConnection(
                url,
                conn => onOpen(new WsClient(conn, onMessage, onError)),
                onError,
                protocols,
                onResponse,
                disposeRequest,
                disposeResponse
            );

        public void CreateWsClient(
            HttpRequestMessage request,
            Action<WsClient> onOpen,
            Action<WsMessage> onMessage,
            Action<Exception> onError,
            IEnumerable<string> protocols = null,
            Action<HttpResponseMessage> onResponse = null,
            bool disposeRequest = true,
            bool disposeResponse = true
        ) => CreateWsConnection(
                request,
                conn => onOpen(new WsClient(conn, onMessage, onError)),
                onError,
                protocols,
                onResponse,
                disposeRequest,
                disposeResponse
            );

        public void CreateWsConnection(
            string url,
            Action<WsConnection> onOpen,
            Action<Exception> onError,
            IEnumerable<string> protocols = null,
            Action<HttpResponseMessage> onResponse = null,
            bool disposeRequest = true,
            bool disposeResponse = true
        ) => CreateWsConnection(
                new HttpRequestMessage(@"GET", url),
                onOpen,
                onError,
                protocols,
                onResponse,
                disposeRequest,
                disposeResponse
            );

        public void CreateWsConnection(
            HttpRequestMessage request,
            Action<WsConnection> onOpen,
            Action<Exception> onError,
            IEnumerable<string> protocols = null,
            Action<HttpResponseMessage> onResponse = null,
            bool disposeRequest = true,
            bool disposeResponse = true
        ) {
            string key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(WS_RNG));

            request.Connection = HttpConnectionHeader.UPGRADE;
            request.SetHeader(@"Cache-Control", @"no-cache");
            request.SetHeader(@"Upgrade", WS_PROTO);
            request.SetHeader(@"Sec-WebSocket-Key", key);
            request.SetHeader(@"Sec-WebSocket-Version", @"13");

            if(protocols?.Any() == true)
                request.SetHeader(@"Sec-WebSocket-Protocol", string.Join(@", ", protocols));

            SendRequest(
                request,
                (t, res) => {
                    try {
                        onResponse?.Invoke(res);

                        if(res.ProtocolVersion.CompareTo(@"1.1") < 0)
                            throw new HttpUpgradeProtocolVersionException(@"1.1", res.ProtocolVersion);

                        if(res.StatusCode != 101)
                            throw new HttpUpgradeUnexpectedStatusException(res.StatusCode);

                        if(res.Connection != HttpConnectionHeader.UPGRADE)
                            throw new HttpUpgradeUnexpectedHeaderException(
                                @"Connection",
                                HttpConnectionHeader.UPGRADE,
                                res.Connection
                            );

                        string hUpgrade = res.GetHeaderLine(@"Upgrade");
                        if(hUpgrade != WS_PROTO)
                            throw new HttpUpgradeUnexpectedHeaderException(@"Upgrade", WS_PROTO, hUpgrade);

                        string serverHashStr = res.GetHeaderLine(@"Sec-WebSocket-Accept");
                        byte[] expectHash = SHA1.HashData(Encoding.ASCII.GetBytes(key + WS_GUID));

                        if(string.IsNullOrWhiteSpace(serverHashStr))
                            throw new HttpUpgradeUnexpectedHeaderException(
                                @"Sec-WebSocket-Accept",
                                Convert.ToBase64String(expectHash),
                                serverHashStr
                            );

                        byte[] givenHash = Convert.FromBase64String(serverHashStr.Trim());

                        if(!expectHash.SequenceEqual(givenHash))
                            throw new HttpUpgradeInvalidHashException(Convert.ToBase64String(expectHash), serverHashStr);

                        onOpen(t.Connection.ToWebSocket());
                    } catch(Exception ex) {
                        onError(ex);
                    }
                },
                (t, ex) => onError(ex),
                disposeRequest: disposeRequest,
                disposeResponse: disposeResponse
            );
        }

        public static void Send(
            HttpRequestMessage request,
            Action<HttpTask, HttpResponseMessage> onComplete = null,
            Action<HttpTask, Exception> onError = null,
            Action<HttpTask> onCancel = null,
            Action<HttpTask, long, long> onDownloadProgress = null,
            Action<HttpTask, long, long> onUploadProgress = null,
            Action<HttpTask, HttpTask.TaskState> onStateChange = null,
            bool disposeRequest = true,
            bool disposeResponse = true
        ) => Instance.SendRequest(
                request,
                onComplete,
                onError,
                onCancel,
                onDownloadProgress,
                onUploadProgress,
                onStateChange,
                disposeRequest,
                disposeResponse
            );

        public static void Connect(
            string url,
            Action<WsClient> onOpen,
            Action<WsMessage> onMessage,
            Action<Exception> onError,
            IEnumerable<string> protocols = null,
            Action<HttpResponseMessage> onResponse = null,
            bool disposeRequest = true,
            bool disposeResponse = true
        ) => Instance.CreateWsClient(
                url,
                onOpen,
                onMessage,
                onError,
                protocols,
                onResponse,
                disposeRequest,
                disposeResponse
            );

        public static void Connect(
            HttpRequestMessage request,
            Action<WsClient> onOpen,
            Action<WsMessage> onMessage,
            Action<Exception> onError,
            IEnumerable<string> protocols = null,
            Action<HttpResponseMessage> onResponse = null,
            bool disposeRequest = true,
            bool disposeResponse = true
        ) => Instance.CreateWsClient(
                request,
                onOpen,
                onMessage,
                onError,
                protocols,
                onResponse,
                disposeRequest,
                disposeResponse
            );

        private bool IsDisposed;
        ~HttpClient()
            => DoDispose();
        public void Dispose() {
            DoDispose();
            GC.SuppressFinalize(this);
        }
        private void DoDispose() {
            if(IsDisposed)
                return;
            IsDisposed = true;

            Tasks.Dispose();
            Connections.Dispose();
        }
    }
}
