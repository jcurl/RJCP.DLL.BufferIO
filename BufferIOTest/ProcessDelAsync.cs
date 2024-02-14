namespace RJCP.IO
{
    using System;
    using System.Threading;

    internal class ProcessDelAsync
    {
        // This sample shows how the logic remains in the main class ProcessDelAsync. where it likely makes more sense,
        // and allow the OpAsyncResult to call its Complete() method when done (which is protected and can't be called
        // from ProcessDelAsync).
        //
        // The ProcessDelAsync.BeginOp() creates it's OpAsyncResult object. It calls Process() giving it a reference to
        // itself. As the OpAsyncResult is a nested class of ProcessDelAsync, it has access to the private methods (it
        // just needs the reference then), as well as being able to complete.

        private class OpAsyncResult : AsyncResult
        {
            public OpAsyncResult(AsyncCallback callback, object state, object owner, string operation)
                : base(callback, state, owner, operation) { }

            public void Process(ProcessDelAsync op)
            {
                op.DoOp((ex) => {
                    Complete(ex, false);
                });
            }
        }

        private Thread m_BackgroundThread;

        public IAsyncResult BeginOp(AsyncCallback callback, object state)
        {
            OpAsyncResult ar = new(callback, state, this, nameof(BeginOp));
            ar.Process(this);
            return ar;
        }

        private void DoOp(Action<Exception> onComplete)
        {
            if (m_BackgroundThread is not null)
                throw new InvalidOperationException("Test still running");

            m_BackgroundThread = new Thread(() => {
                Thread.Sleep(100);
                onComplete(null);
            });

            m_BackgroundThread.Start();
        }

        public void EndOp(IAsyncResult ar)
        {
            AsyncResult.End(ar, this, nameof(BeginOp));
        }
    }
}
