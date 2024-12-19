using System;
using System.Threading;
using System.Threading.Tasks;

namespace pylorak.Utilities
{
    public sealed class BackgroundTask : Disposable
    {
        private CancellationTokenSource CancellationSource;
        private Task? UserTask;

        public BackgroundTask()
        {
            CancellationSource = new CancellationTokenSource();
            CancellationToken = CancellationSource.Token;
        }

        public CancellationToken CancellationToken { private set; get; }

        private void CancelTask(bool rearm)
        {
            try
            {
                CancellationSource.Cancel();
                UserTask?.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            { }
            finally
            {
                UserTask?.Dispose();
                CancellationSource.Dispose();
            }

            if (rearm)
            {
                CancellationSource = new CancellationTokenSource();
                CancellationToken = CancellationSource.Token;
            }
        }

        public void CancelTask()
        {
            CancelTask(true);
        }

        public void Restart(Action action)
        {
            CancelTask(true);
            UserTask = Task.Run(action, CancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (disposing)
                CancelTask(false);

            base.Dispose(disposing);
        }
    }
}
