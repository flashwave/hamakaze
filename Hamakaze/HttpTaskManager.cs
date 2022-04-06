using System;
using System.Threading;

namespace Hamakaze {
    public class HttpTaskManager : IDisposable {
        private Semaphore Lock { get; set; }

        public HttpTaskManager(int maxThreads = 5) {
            Lock = new Semaphore(maxThreads, maxThreads);
        }

        public void RunTask(HttpTask task) {
            if(task == null)
                throw new ArgumentNullException(nameof(task));
            if(!Lock.WaitOne())
                throw new HttpTaskManagerLockException();
            new Thread(() => {
                try {
                    task.Run();
                } finally {
                    Lock?.Release();
                }
            }).Start();
        }

        private bool IsDisposed;
        ~HttpTaskManager()
            => DoDispose();
        public void Dispose() {
            DoDispose();
            GC.SuppressFinalize(this);
        }
        private void DoDispose() {
            if(IsDisposed)
                return;
            IsDisposed = true;
            Lock.Dispose();
            Lock = null;
        }
    }
}
