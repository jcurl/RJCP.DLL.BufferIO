namespace RJCP.IO
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class AsyncTestResult
    {
        [Test]
        public void SynchronousAsyncTest()
        {
            SynchronousTestAsync test = new SynchronousTestAsync();
            IAsyncResult ar = test.BeginTest(null, null);
            Assert.That(ar.IsCompleted, Is.True);
            Assert.That(ar.AsyncState, Is.Null);
            Assert.That(ar.CompletedSynchronously, Is.True);

            test.EndTest(ar);
        }

        [Test]
        public void SynchronousAsyncTestWithObject()
        {
            object state = new object();
            SynchronousTestAsync test = new SynchronousTestAsync();
            IAsyncResult ar = test.BeginTest(null, state);
            Assert.That(ar.IsCompleted, Is.True);
            Assert.That(ar.AsyncState, Is.EqualTo(state));
            Assert.That(ar.CompletedSynchronously, Is.True);

            test.EndTest(ar);
        }

        [Test]
        public void SynchronousAsyncTestWithCallback()
        {
            object state = new object();
            bool callbackComplete = false;
            SynchronousTestAsync test = new SynchronousTestAsync();
            IAsyncResult ar = test.BeginTest((iar) => {
                callbackComplete = true;
            }, state);

            Assert.That(callbackComplete, Is.True);
            Assert.That(ar.IsCompleted, Is.True);
            Assert.That(ar.AsyncState, Is.EqualTo(state));
            Assert.That(ar.CompletedSynchronously, Is.True);

            test.EndTest(ar);
        }

        [Test]
        public void SynchronousAsyncTestWait()
        {
            object state = new object();
            bool callbackComplete = false;
            SynchronousTestAsync test = new SynchronousTestAsync();
            IAsyncResult ar = test.BeginTest((iar) => {
                callbackComplete = true;
            }, state);

            Assert.That(callbackComplete, Is.True);
            Assert.That(ar.IsCompleted, Is.True);
            Assert.That(ar.AsyncState, Is.EqualTo(state));
            Assert.That(ar.CompletedSynchronously, Is.True);

            ar.AsyncWaitHandle.WaitOne();
            Assert.That(ar.IsCompleted, Is.True);

            test.EndTest(ar);
        }

        [Test]
        public void SynchronousStartTwice()
        {
            // It's allowed to start the BeginXXX operation twice, if the object itself doesn't prevent this. The
            // implementation, not AsyncResult, is responsible to ensure that two operations don't run in parallel if
            // this is not desired.
            SynchronousTestAsync test = new SynchronousTestAsync();
            IAsyncResult ar1 = test.BeginTest(null, null);
            IAsyncResult ar2 = test.BeginTest(null, null);
            test.EndTest(ar1);
            test.EndTest(ar2);
        }

        [Test]
        public void SynchronousStartMismatchedEnd()
        {
            SynchronousTestAsync test = new SynchronousTestAsync();
            IAsyncResult ar1 = test.BeginTest(null, null);
            Assert.That(() => {
                test.EndSecondTest(ar1);
            }, Throws.TypeOf<ArgumentException>());
            test.EndTest(ar1);
        }

        [Test]
        public void SynchronousStartMismatchedObject()
        {
            SynchronousTestAsync test1 = new SynchronousTestAsync();
            SynchronousTestAsync test2 = new SynchronousTestAsync();

            IAsyncResult ar1 = test1.BeginTest(null, null);
            IAsyncResult ar2 = test2.BeginTest(null, null);

            Assert.That(() => {
                test2.EndTest(ar1);
            }, Throws.TypeOf<InvalidOperationException>());

            test1.EndTest(ar1);
            test2.EndTest(ar2);
        }

        [Test]
        [Timeout(2000)]
        public void AsynchronousAsyncTest()
        {
            AsynchronousTestAsync test = new AsynchronousTestAsync();
            IAsyncResult ar = test.BeginTest(null, null);
            Assert.That(ar.CompletedSynchronously, Is.False);
            Assert.That(ar.AsyncState, Is.Null);

            // Should block until the test is actually completed.
            test.EndTest(ar);
            Assert.That(ar.IsCompleted, Is.True);
        }

        [Test]
        [Timeout(2000)]
        public void AsynchronousAsyncTestWithObject()
        {
            object state = new object();
            AsynchronousTestAsync test = new AsynchronousTestAsync();
            IAsyncResult ar = test.BeginTest(null, state);
            Assert.That(ar.CompletedSynchronously, Is.False);
            Assert.That(ar.AsyncState, Is.EqualTo(state));

            // Should block until the test is actually completed.
            test.EndTest(ar);
            Assert.That(ar.IsCompleted, Is.True);
        }

        [Test]
        [Timeout(2000)]
        public void AsynchronousAsyncTestWithCallback()
        {
            object state = new object();
            bool callbackComplete = false;
            AsynchronousTestAsync test = new AsynchronousTestAsync();
            IAsyncResult ar = test.BeginTest((iar) => {
                callbackComplete = true;
            }, state);

            Assert.That(callbackComplete, Is.False);
            Assert.That(ar.IsCompleted, Is.False);
            Assert.That(ar.CompletedSynchronously, Is.False);
            Assert.That(ar.AsyncState, Is.EqualTo(state));

            // Should block until the test is actually completed.
            test.EndTest(ar);
            Assert.That(ar.IsCompleted, Is.True);
        }

        [Test]
        [Timeout(2000)]
        public void AsynchronousAsyncTestWait()
        {
            object state = new object();
            bool callbackComplete = false;
            AsynchronousTestAsync test = new AsynchronousTestAsync();
            IAsyncResult ar = test.BeginTest((iar) => {
                callbackComplete = true;
            }, state);

            Assert.That(callbackComplete, Is.False);
            Assert.That(ar.IsCompleted, Is.False);
            Assert.That(ar.CompletedSynchronously, Is.False);
            Assert.That(ar.AsyncState, Is.EqualTo(state));

            // Should block until the test is actually completed.
            ar.AsyncWaitHandle.WaitOne();
            Assert.That(ar.IsCompleted, Is.True);

            test.EndTest(ar);
        }

        [Test]
        public void AsynchronousProcessCallback()
        {
            // For the purpose of this test, see the code "ProcessDelAsync". This shows how you might want to use a
            // delegate or a lambda to combine the AsyncResult to the class instantiating it.

            ProcessDelAsync op = new ProcessDelAsync();
            IAsyncResult ar = op.BeginOp(null, null);
            op.EndOp(ar);
        }
    }
}
