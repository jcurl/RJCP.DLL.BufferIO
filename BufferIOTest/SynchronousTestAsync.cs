namespace RJCP.IO
{
    using System;

    internal class SynchronousTestAsync
    {
        private class SynchronousTestAsyncResult : AsyncResult
        {
            public SynchronousTestAsyncResult(AsyncCallback callback, object state, object owner, string operationId)
                : base(callback, state, owner, operationId) { }

            public override void Process()
            {
                Complete(null, true);
            }
        }

        public IAsyncResult BeginTest(AsyncCallback callback, object state)
        {
            SynchronousTestAsyncResult result = new(callback, state, this, nameof(BeginTest));
            result.Process();
            return result;
        }

        public void EndTest(IAsyncResult result)
        {
            AsyncResult.End(result, this, nameof(BeginTest));
        }

        public IAsyncResult BeginSecondTest(AsyncCallback callback, object state)
        {
            SynchronousTestAsyncResult result = new(callback, state, this, nameof(BeginSecondTest));
            result.Process();
            return result;
        }

        public void EndSecondTest(IAsyncResult result)
        {
            AsyncResult.End(result, this, nameof(BeginSecondTest));
        }
    }
}
