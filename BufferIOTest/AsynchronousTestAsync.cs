namespace RJCP.IO
{
    using System;
    using System.Threading;

    internal class AsynchronousTestAsync
    {
        private class AsynchronousTestAsyncResult : AsyncResult
        {
            private Thread m_BackgroundThread;

            public AsynchronousTestAsyncResult(AsyncCallback callback, object state, object owner, string operationId)
                : base(callback, state, owner, operationId) { }

            public override void Process()
            {
                if (m_BackgroundThread is not null)
                    throw new InvalidOperationException("Test still running");

                m_BackgroundThread = new Thread(() => {
                    Thread.Sleep(100);
                    Complete(null, false);
                });

                m_BackgroundThread.Start();
            }

            protected override void Completed(Exception exception, bool completedSynchronously)
            {
                m_BackgroundThread.Join();
                m_BackgroundThread = null;
            }
        }

        public IAsyncResult BeginTest(AsyncCallback callback, object state)
        {

            AsynchronousTestAsyncResult result = new(callback, state, this, nameof(BeginTest));
            result.Process();
            return result;
        }

        public void EndTest(IAsyncResult result)
        {
            AsyncResult.End(result, this, nameof(BeginTest));
        }
    }
}
