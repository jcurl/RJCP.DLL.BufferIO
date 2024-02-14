namespace RJCP.IO.Buffer
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    // These test cases are a copy of MemoryReadBufferTest, WaitForRead.

    [TestFixture]
    [Timeout(2000)]
    public class MemoryReadBufferAsyncTest
    {
        [Test]
        [Repeat(200)]
        public async Task WaitForReadAsync()
        {
            using (CancellationTokenSource cts = new())
            using (MemoryReadBuffer buffer = new(4096)) {
                Task producer = new TaskFactory().StartNew(() => {
                    // Try to randomize the order in which things run to look for race conditions.
                    Random r1 = new();
                    if (r1.Next(2) == 0) Thread.Sleep(r1.Next(2));

                    lock (buffer.Lock) {
                        buffer.Produce(100);
                    }
                });

                // Try to randomize the order in which things run to look for race conditions.
                Random r2 = new();
                if (r2.Next(2) == 0) Thread.Sleep(r2.Next(2));

                Assert.That(await buffer.WaitForReadAsync(Timeout.Infinite), Is.True);
                Assert.That(buffer.BytesToRead, Is.EqualTo(100));
                await producer;
            }
        }

        [TestCase(0, false, TestName = "WaitForReadAsyncZeroTimeoutNoData")]
        [TestCase(100, true, TestName = "WaitForReadAsyncZeroTimeoutWithData")]
        public async Task WaitForReadAsyncZeroTimeout(int produce, bool result)
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                if (produce > 0) buffer.Produce(produce);
                Assert.That(await buffer.WaitForReadAsync(0), Is.EqualTo(result));
                Assert.That(buffer.BytesToRead, Is.EqualTo(produce));
            }
        }

        [Test]
        public async Task WaitForReadAsyncByteToEmpty()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                buffer.Produce(1);
                Assert.That(await buffer.WaitForReadAsync(0), Is.True);
                Assert.That(await buffer.WaitForReadAsync(1), Is.True);
                buffer.ReadByte();
                Assert.That(await buffer.WaitForReadAsync(0), Is.False);
                Assert.That(await buffer.WaitForReadAsync(1), Is.False);
                buffer.Produce(1);
                Assert.That(await buffer.WaitForReadAsync(0), Is.True);
                Assert.That(await buffer.WaitForReadAsync(1), Is.True);
            }
        }

        [Test]
        public async Task WaitForReadAsyncToEmpty()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                byte[] data = new byte[1];

                buffer.Produce(1);
                Assert.That(await buffer.WaitForReadAsync(0), Is.True);
                Assert.That(await buffer.WaitForReadAsync(1), Is.True);
                Assert.That(buffer.Read(data, 0, 1), Is.EqualTo(1));
                Assert.That(await buffer.WaitForReadAsync(0), Is.False);
                Assert.That(await buffer.WaitForReadAsync(1), Is.False);
                buffer.Produce(1);
                Assert.That(await buffer.WaitForReadAsync(0), Is.True);
                Assert.That(await buffer.WaitForReadAsync(1), Is.True);
            }
        }

        [Test]
        [Repeat(200)]
        public async Task WaitForReadAsyncDeviceDead()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                Task driver = new TaskFactory().StartNew(() => {
                    // Try to randomize the order in which things run to look for race conditions.
                    Random r1 = new();
                    if (r1.Next(2) == 0) Thread.Sleep(r1.Next(2));

                    buffer.DeviceDead();
                });

                // Try to randomize the order in which things run to look for race conditions.
                Random r2 = new();
                if (r2.Next(2) == 0) Thread.Sleep(r2.Next(2));

                Assert.That(await buffer.WaitForReadAsync(Timeout.Infinite), Is.False);
                Assert.That(buffer.BytesToRead, Is.EqualTo(0));
                Assert.That(buffer.IsDeviceDead, Is.True);
                await driver;
            }
        }

        [Test]
        public async Task WaitForReadAsyncDeadProducedData()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                // Even though the device died, there is still data in the buffer, so this takes precedence.
                buffer.Produce(100);
                buffer.DeviceDead();
                Assert.That(buffer.IsDeviceDead, Is.True);
                Assert.That(await buffer.WaitForReadAsync(Timeout.Infinite), Is.True);

                // And once all data is read, then timeouts do not apply.
                byte[] data = new byte[100];
                Assert.That(buffer.Read(data, 0, 100), Is.EqualTo(100));
                Assert.That(await buffer.WaitForReadAsync(Timeout.Infinite), Is.False);
            }
        }

        [Test]
        public async Task WaitForReadAsyncCountDeadProducedData()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                // Even though the device died, there is still data in the buffer, so this takes precedence.
                buffer.Produce(100);
                buffer.DeviceDead();
                Assert.That(buffer.IsDeviceDead, Is.True);
                Assert.That(await buffer.WaitForReadAsync(100, Timeout.Infinite), Is.True);

                // And once all data is read, then timeouts do not apply.
                byte[] data = new byte[100];
                Assert.That(buffer.Read(data, 0, 100), Is.EqualTo(100));
                Assert.That(await buffer.WaitForReadAsync(1, Timeout.Infinite), Is.False);
            }
        }

        [Test]
        public async Task WaitForReadAsyncCancellationTokenDeadProducedData()
        {
            using (CancellationTokenSource cts = new())
            using (MemoryReadBuffer buffer = new(4096)) {
                // Even though the device died, there is still data in the buffer, so this takes precedence.
                buffer.Produce(100);
                buffer.DeviceDead();
                Assert.That(buffer.IsDeviceDead, Is.True);
                Assert.That(await buffer.WaitForReadAsync(Timeout.Infinite, cts.Token), Is.True);

                // And once all data is read, then timeouts do not apply.
                byte[] data = new byte[100];
                Assert.That(buffer.Read(data, 0, 100), Is.EqualTo(100));
                Assert.That(await buffer.WaitForReadAsync(Timeout.Infinite, cts.Token), Is.False);
            }
        }

        [Test]
        public async Task WaitForReadCountCancellationTokenDeadProducedData()
        {
            using (CancellationTokenSource cts = new())
            using (MemoryReadBuffer buffer = new(4096)) {
                // Even though the device died, there is still data in the buffer, so this takes precedence.
                buffer.Produce(100);
                buffer.DeviceDead();
                Assert.That(buffer.IsDeviceDead, Is.True);
                Assert.That(await buffer.WaitForReadAsync(100, Timeout.Infinite, cts.Token), Is.True);

                // And once all data is read, then timeouts do not apply.
                byte[] data = new byte[100];
                Assert.That(buffer.Read(data, 0, 100), Is.EqualTo(100));
                Assert.That(await buffer.WaitForReadAsync(1, Timeout.Infinite, cts.Token), Is.False);
            }
        }

        [Test]
        [Repeat(200)]
        public async Task WaitForReadAsyncDispose()
        {
            MemoryReadBuffer buffer = null;
            try {
                buffer = new MemoryReadBuffer(4096);
                Task driver = new TaskFactory().StartNew(() => {
                    // Try to randomize the order in which things run to look for race conditions.
                    Random r1 = new();
                    if (r1.Next(2) == 0) Thread.Sleep(10);

                    buffer.Dispose();
                });

                Random r2 = new();
                if (r2.Next(2) == 0) Thread.Sleep(r2.Next(2));

                bool wait;
                try {
                    wait = await buffer.WaitForReadAsync(Timeout.Infinite);
                    Assert.That(buffer.BytesToRead, Is.EqualTo(0));
                } catch (ObjectDisposedException) {
                    // The exception is expected when the buffer is disposed before WaitForRead is called.
                    wait = false;
                }
                Assert.That(wait, Is.False);
                Assert.That(buffer.IsDeviceDead, Is.True);
                await driver;
            } finally {
                if (buffer is not null) buffer.Dispose();
            }
        }

        [Test]
        public async Task WaitForReadAsyncCancellationToken()
        {
            using (CancellationTokenSource cts = new())
            using (MemoryReadBuffer buffer = new(4096)) {
                Task user = new TaskFactory().StartNew(() => {
                    Thread.Sleep(100);
                    cts.Cancel();
                });
                Assert.That(await buffer.WaitForReadAsync(Timeout.Infinite, cts.Token), Is.False);
                Assert.That(buffer.BytesToRead, Is.EqualTo(0));
                await user;
            }
        }

        [Test]
        public async Task WaitForReadAsyncCancellationTokenNone()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                Assert.That(await buffer.WaitForReadAsync(100, CancellationToken.None), Is.False);
                Assert.That(buffer.BytesToRead, Is.EqualTo(0));
            }
        }

        [TestCase(false, 0, false, TestName = "WaitForReadAsyncCancellationTokenZeroTimeoutNoData")]
        [TestCase(false, 100, true, TestName = "WaitForReadAsyncCancellationTokenZeroTimeoutWithData")]
        [TestCase(true, 0, false, TestName = "WaitForReadAsyncCancellationTokenZeroTimeoutNoDataCancelled")]
        [TestCase(true, 100, false, TestName = "WaitForReadAsyncCancellationTokenZeroTimeoutWithDataCancelled")]
        public async Task WaitForReadAsyncCancellationTokenZeroTimeout(bool cancelled, int produce, bool result)
        {
            using (CancellationTokenSource cts = new())
            using (MemoryReadBuffer buffer = new(4096)) {
                if (cancelled) cts.Cancel();
                if (produce > 0) buffer.Produce(produce);
                Assert.That(await buffer.WaitForReadAsync(0, cts.Token), Is.EqualTo(result));
                Assert.That(buffer.BytesToRead, Is.EqualTo(produce));
            }
        }

        [TestCase(0, false, TestName = "WaitForReadAsyncCancellationTokenNoneZeroTimeoutNoData")]
        [TestCase(100, true, TestName = "WaitForReadAsyncCancellationTokenNoneZeroTimeoutWithData")]
        public async Task WaitForReadAsyncCancellationTokenNoneZeroTimeout(int produce, bool result)
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                if (produce > 0) buffer.Produce(produce);
                Assert.That(await buffer.WaitForReadAsync(0, CancellationToken.None), Is.EqualTo(result));
                Assert.That(buffer.BytesToRead, Is.EqualTo(produce));
            }
        }

        [Test]
        [Repeat(200)]
        public async Task WaitForReadAsyncCount()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                Task producer = new TaskFactory().StartNew(() => {
                    for (int i = 0; i < 10; i++) {
                        // Try to randomize the order in which things run to look for race conditions.
                        Random r1 = new();
                        if (r1.Next(2) == 0) Thread.Sleep(r1.Next(2));

                        lock (buffer.Lock) {
                            buffer.Produce(100);
                        }
                    }
                });

                // Try to randomize the order in which things run to look for race conditions.
                Random r2 = new();
                if (r2.Next(2) == 0) Thread.Sleep(r2.Next(2));

                Assert.That(await buffer.WaitForReadAsync(950, Timeout.Infinite), Is.True);
                Assert.That(buffer.BytesToRead, Is.EqualTo(1000));
                await producer;
            }
        }

        [Test]
        public async Task WaitForReadAsyncCountTimeout()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                Assert.That(await buffer.WaitForReadAsync(100, 100), Is.False);
            }
        }

        [TestCase(0, 100, false, TestName = "WaitForReadAsyncCountZeroTimeoutNoData1")]
        [TestCase(0, 0, true, TestName = "WaitForReadAsyncCountZeroTimeoutNoData2")]
        [TestCase(100, 50, true, TestName = "WaitForReadAsyncCountZeroTimeoutWithData1")]
        [TestCase(100, 0, true, TestName = "WaitForReadAsyncCountZeroTimeoutWithData2")]
        [TestCase(100, 150, false, TestName = "WaitForReadAsyncCountZeroTimeoutWithInsufficientData")]
        public async Task WaitForReadAsyncCountZeroTimeout(int produce, int count, bool result)
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                if (produce > 0) buffer.Produce(produce);
                Assert.That(await buffer.WaitForReadAsync(count, 0), Is.EqualTo(result));
                Assert.That(buffer.BytesToRead, Is.EqualTo(produce));
            }
        }

        [Test]
        [Repeat(200)]
        public async Task WaitForReadAsyncCountDeviceDead()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                Task driver = new TaskFactory().StartNew(() => {
                    for (int i = 0; i < 2; i++) {
                        // Try to randomize the order in which things run to look for race conditions.
                        Random r1 = new();
                        if (r1.Next(2) == 0) Thread.Sleep(r1.Next(2));

                        lock (buffer.Lock) {
                            buffer.Produce(100);
                        }
                    }

                    buffer.DeviceDead();
                });

                // Try to randomize the order in which things run to look for race conditions.
                Random r2 = new();
                if (r2.Next(2) == 0) Thread.Sleep(r2.Next(2));

                Assert.That(await buffer.WaitForReadAsync(950, Timeout.Infinite), Is.False);
                Assert.That(buffer.BytesToRead, Is.EqualTo(200));
                await driver;
            }
        }

        [Test]
        [Repeat(200)]
        public async Task WaitForReadCountDispose()
        {
            MemoryReadBuffer buffer = null;
            try {
                buffer = new MemoryReadBuffer(4096);
                Task driver = new TaskFactory().StartNew(() => {
                    for (int i = 0; i < 2; i++) {
                        // Try to randomize the order in which things run to look for race conditions.
                        Random r1 = new();
                        if (r1.Next(2) == 0) Thread.Sleep(r1.Next(2));

                        lock (buffer.Lock) {
                            buffer.Produce(100);
                        }
                    }

                    buffer.Dispose();
                });

                Random r2 = new();
                if (r2.Next(2) == 0) Thread.Sleep(r2.Next(2));

                bool wait;
                try {
                    wait = await buffer.WaitForReadAsync(950, Timeout.Infinite);

                    // Will probably never get here.
                    Assert.That(buffer.BytesToRead, Is.EqualTo(200));
                } catch (ObjectDisposedException) {
                    // The exception is expected when the buffer is disposed before WaitForRead is called.
                    wait = false;
                }
                Assert.That(wait, Is.False);
                Assert.That(buffer.IsDeviceDead, Is.True);
                await driver;
            } finally {
                // A double dispose shouldn't cause a problem.
                if (buffer is not null) buffer.Dispose();
            }
        }

        [Test]
        public async Task WaitForReadAsyncCountCancellationToken()
        {
            using (CancellationTokenSource cts = new())
            using (MemoryReadBuffer buffer = new(4096)) {
                Task user = new TaskFactory().StartNew(() => {
                    Thread.Sleep(100);
                    cts.Cancel();
                });
                Assert.That(await buffer.WaitForReadAsync(1000, Timeout.Infinite, cts.Token), Is.False);
                Assert.That(buffer.BytesToRead, Is.EqualTo(0));
                await user;
            }
        }

        [Test]
        public async Task WaitForReadAsyncCountWithDataCancellationToken()
        {
            using (CancellationTokenSource cts = new())
            using (MemoryReadBuffer buffer = new(4096)) {
                Task t = new TaskFactory().StartNew(() => {
                    Thread.Sleep(100);
                    for (int i = 0; i < 2; i++) {
                        // Try to randomize the order in which things run to look for race conditions.
                        Random r1 = new();
                        if (r1.Next(2) == 0) Thread.Sleep(r1.Next(2));

                        lock (buffer.Lock) {
                            buffer.Produce(100);
                        }
                    }
                    cts.Cancel();
                });
                Assert.That(await buffer.WaitForReadAsync(1000, Timeout.Infinite, cts.Token), Is.False);
                Assert.That(buffer.BytesToRead, Is.EqualTo(200));
                await t;
            }
        }

        [Test]
        public async Task WaitForReadAsyncCountCancellationTokenNone()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                Assert.That(await buffer.WaitForReadAsync(1000, 100, CancellationToken.None), Is.False);
                Assert.That(buffer.BytesToRead, Is.EqualTo(0));
            }
        }

        [Test]
        public async Task WaitForReadAsyncCountWithDataCancellationTokenNone()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                Task t = new TaskFactory().StartNew(() => {
                    Thread.Sleep(50);
                    for (int i = 0; i < 2; i++) {
                        // Try to randomize the order in which things run to look for race conditions.
                        Random r1 = new();
                        if (r1.Next(2) == 0) Thread.Sleep(r1.Next(2));

                        lock (buffer.Lock) {
                            buffer.Produce(100);
                        }
                    }
                });

                Assert.That(await buffer.WaitForReadAsync(1000, 1000, CancellationToken.None), Is.False);
                Assert.That(buffer.BytesToRead, Is.EqualTo(200));
                await t;
            }
        }

        [TestCase(false, 0, 100, false, TestName = "WaitForReadAsyncCountCancellationTokenZeroTimeoutNoData1")]
        [TestCase(false, 0, 0, true, TestName = "WaitForReadAsyncCountCancellationTokenZeroTimeoutNoData2")]
        [TestCase(false, 100, 50, true, TestName = "WaitForReadAsyncCountCancellationTokenZeroTimeoutWithData1")]
        [TestCase(false, 100, 0, true, TestName = "WaitForReadAsyncCountCancellationTokenZeroTimeoutWithData2")]
        [TestCase(false, 100, 150, false, TestName = "WaitForReadAsyncCountCancellationTokenZeroTimeoutWithInsufficientData")]
        [TestCase(true, 0, 100, false, TestName = "WaitForReadAsyncCountCancellationTokenZeroTimeoutNoData1Cancelled")]
        [TestCase(true, 0, 0, false, TestName = "WaitForReadAsyncCountCancellationTokenZeroTimeoutNoData2Cancelled")]
        [TestCase(true, 100, 50, false, TestName = "WaitForReadCAsyncountCancellationTokenZeroTimeoutWithData1Cancelled")]
        [TestCase(true, 100, 0, false, TestName = "WaitForReadAsyncCountCancellationTokenZeroTimeoutWithData2Cancelled")]
        [TestCase(true, 100, 150, false, TestName = "WaitForReadAsyncCountCancellationTokenZeroTimeoutWithInsufficientDataCancelled")]
        public async Task WaitForReadAsyncCountCancellationTokenZeroTimeout(bool cancelled, int produce, int count, bool result)
        {
            using (CancellationTokenSource cts = new())
            using (MemoryReadBuffer buffer = new(4096)) {
                if (cancelled) cts.Cancel();
                if (produce > 0) buffer.Produce(produce);
                Assert.That(await buffer.WaitForReadAsync(count, 0, cts.Token), Is.EqualTo(result));
                Assert.That(buffer.BytesToRead, Is.EqualTo(produce));
            }
        }

        [TestCase(0, 100, false, TestName = "WaitForReadAsyncCountCancellationTokenNoneZeroTimeoutNoData1")]
        [TestCase(0, 0, true, TestName = "WaitForReadAsyncCountCancellationTokenNoneZeroTimeoutNoData2")]
        [TestCase(100, 50, true, TestName = "WaitForReadAsyncCountCancellationTokenNoneZeroTimeoutWithData1")]
        [TestCase(100, 0, true, TestName = "WaitForReadAsyncCountCancellationTokenNoneZeroTimeoutWithData2")]
        [TestCase(100, 150, false, TestName = "WaitForReadAsyncCountCancellationTokenNoneZeroTimeoutWithInsufficientData")]
        public async Task WaitForReadAsyncCountCancellationNoneTokenZeroTimeout(int produce, int count, bool result)
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                if (produce > 0) buffer.Produce(produce);
                Assert.That(await buffer.WaitForReadAsync(count, 0, CancellationToken.None), Is.EqualTo(result));
                Assert.That(buffer.BytesToRead, Is.EqualTo(produce));
            }
        }

        [Test]
        public async Task WaitForReadAsyncCountAtCapacity()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                Assert.That(await buffer.WaitForReadAsync(4097, Timeout.Infinite), Is.False);
            }
        }

        [TestCase(false, TestName = "WaitForReadAsyncCountAtCapacityCancellationToken")]
        [TestCase(true, TestName = "WaitForReadAsyncCountAtCapacityCancellationTokenCancelled")]
        public async Task WaitForReadAsyncCountAtCapacityCancellationToken(bool cancelled)
        {
            using (CancellationTokenSource cts = new())
            using (MemoryReadBuffer buffer = new(4096)) {
                if (cancelled) cts.Cancel();
                Assert.That(await buffer.WaitForReadAsync(4097, Timeout.Infinite, cts.Token), Is.False);
            }
        }

        [Test]
        public async Task WaitForReadAsyncCountAtCapacityCancellationTokenNone()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                Assert.That(await buffer.WaitForReadAsync(4097, Timeout.Infinite, CancellationToken.None), Is.False);
            }
        }

        [Test]
        public void WaitForReadAsyncNegativeTimeout()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                Assert.That(async () => { await buffer.WaitForReadAsync(-2); },
                    Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(buffer.BytesToRead, Is.EqualTo(0));
            }
        }

        [Test]
        public void WaitForReadAsyncNegativeTimeoutCancellationToken()
        {
            using (CancellationTokenSource cts = new())
            using (MemoryReadBuffer buffer = new(4096)) {
                Assert.That(async () => { await buffer.WaitForReadAsync(-2, cts.Token); },
                    Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(buffer.BytesToRead, Is.EqualTo(0));
            }
        }

        [Test]
        public void WaitForReadAsyncCountNegativeTimeout()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                Assert.That(async () => { await buffer.WaitForReadAsync(500, -2); },
                    Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(buffer.BytesToRead, Is.EqualTo(0));
            }
        }

        [Test]
        public void WaitForReadAsyncCountNegativeTimeoutCancellationToken()
        {
            using (CancellationTokenSource cts = new())
            using (MemoryReadBuffer buffer = new(4096)) {
                Assert.That(async () => { await buffer.WaitForReadAsync(500, -2, cts.Token); },
                    Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(buffer.BytesToRead, Is.EqualTo(0));
            }
        }

        [Test]
        public void WaitForReadAsyncCountNegativeTimeoutCancellationTokenNone()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                Assert.That(async () => { await buffer.WaitForReadAsync(500, -2, CancellationToken.None); },
                    Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(buffer.BytesToRead, Is.EqualTo(0));
            }
        }

        [Test]
        public void WaitForReadAsyncNegativeCount()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                Assert.That(async () => { await buffer.WaitForReadAsync(-100, Timeout.Infinite); },
                    Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(buffer.BytesToRead, Is.EqualTo(0));
            }
        }

        [Test]
        public void WaitForReadAsyncNegativeCountCancellationToken()
        {
            using (CancellationTokenSource cts = new())
            using (MemoryReadBuffer buffer = new(4096)) {
                Assert.That(async () => { await buffer.WaitForReadAsync(-100, Timeout.Infinite, cts.Token); },
                    Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(buffer.BytesToRead, Is.EqualTo(0));
            }
        }

        [Test]
        public void WaitForReadAsyncNegativeCountCancellationTokenNone()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                Assert.That(async () => { await buffer.WaitForReadAsync(-100, Timeout.Infinite, CancellationToken.None); },
                    Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(buffer.BytesToRead, Is.EqualTo(0));
            }
        }
    }
}
