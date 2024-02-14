namespace RJCP.IO.Buffer
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    [Timeout(2000)]
    public class MemoryReadBufferTest
    {
        [Test]
        public void CreateDefault()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                Assert.That(buffer.Lock, Is.Not.Null);
                Assert.That(buffer.BufferPtr, Is.EqualTo(IntPtr.Zero));      // Not pinned, so is zero
                Assert.That(buffer.BytesToRead, Is.EqualTo(0));
                Assert.That(buffer.IsBufferNotFull, Is.True);
                Assert.That(buffer.Buffer, Has.Length.EqualTo(4096));
                Assert.That(buffer.BufferWriteLength, Is.EqualTo(4096));
                Assert.That(buffer.IsDeviceDead, Is.False);
            }
        }

        [Test]
        public void CreatePinned()
        {
            using (MemoryReadBuffer buffer = new(4096, true)) {
                Assert.That(buffer.Lock, Is.Not.Null);
                Assert.That(buffer.BufferPtr, Is.Not.EqualTo(IntPtr.Zero));  // Pinned, so is real
                Assert.That(buffer.BytesToRead, Is.EqualTo(0));
                Assert.That(buffer.IsBufferNotFull, Is.True);
                Assert.That(buffer.Buffer, Has.Length.EqualTo(4096));
                Assert.That(buffer.BufferWriteLength, Is.EqualTo(4096));
                Assert.That(buffer.IsDeviceDead, Is.False);
            }
        }

        [Test]
        [Repeat(200)]
        public void WaitForRead()
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

                Assert.That(buffer.WaitForRead(Timeout.Infinite), Is.True);
                Assert.That(buffer.BytesToRead, Is.EqualTo(100));
                producer.Wait();
            }
        }

        [TestCase(0, false, TestName = "WaitForReadZeroTimeoutNoData")]
        [TestCase(100, true, TestName = "WaitForReadZeroTimeoutWithData")]
        public void WaitForReadZeroTimeout(int produce, bool result)
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                if (produce > 0) buffer.Produce(produce);
                Assert.That(buffer.WaitForRead(0), Is.EqualTo(result));
                Assert.That(buffer.BytesToRead, Is.EqualTo(produce));
            }
        }

        [Test]
        public void WaitForReadByteToEmpty()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                buffer.Produce(1);
                Assert.That(buffer.WaitForRead(0), Is.True);
                Assert.That(buffer.WaitForRead(1), Is.True);
                buffer.ReadByte();
                Assert.That(buffer.WaitForRead(0), Is.False);
                Assert.That(buffer.WaitForRead(1), Is.False);
                buffer.Produce(1);
                Assert.That(buffer.WaitForRead(0), Is.True);
                Assert.That(buffer.WaitForRead(1), Is.True);
            }
        }

        [Test]
        public void WaitForReadToEmpty()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                byte[] data = new byte[1];

                buffer.Produce(1);
                Assert.That(buffer.WaitForRead(0), Is.True);
                Assert.That(buffer.WaitForRead(1), Is.True);
                Assert.That(buffer.Read(data, 0, 1), Is.EqualTo(1));
                Assert.That(buffer.WaitForRead(0), Is.False);
                Assert.That(buffer.WaitForRead(1), Is.False);
                buffer.Produce(1);
                Assert.That(buffer.WaitForRead(0), Is.True);
                Assert.That(buffer.WaitForRead(1), Is.True);
            }
        }

        [Test]
        [Repeat(200)]
        public void WaitForReadDeviceDead()
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

                Assert.That(buffer.WaitForRead(Timeout.Infinite), Is.False);
                Assert.That(buffer.BytesToRead, Is.EqualTo(0));
                Assert.That(buffer.IsDeviceDead, Is.True);
                driver.Wait();
            }
        }

        [Test]
        public void WaitForReadDeadProducedData()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                // Even though the device died, there is still data in the buffer, so this takes precedence.
                buffer.Produce(100);
                buffer.DeviceDead();
                Assert.That(buffer.IsDeviceDead, Is.True);
                Assert.That(buffer.WaitForRead(Timeout.Infinite), Is.True);

                // And once all data is read, then timeouts do not apply.
                byte[] data = new byte[100];
                Assert.That(buffer.Read(data, 0, 100), Is.EqualTo(100));
                Assert.That(buffer.WaitForRead(Timeout.Infinite), Is.False);
            }
        }

        [Test]
        public void WaitForReadCountDeadProducedData()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                // Even though the device died, there is still data in the buffer, so this takes precedence.
                buffer.Produce(100);
                buffer.DeviceDead();
                Assert.That(buffer.IsDeviceDead, Is.True);
                Assert.That(buffer.WaitForRead(100, Timeout.Infinite), Is.True);

                // And once all data is read, then timeouts do not apply.
                byte[] data = new byte[100];
                Assert.That(buffer.Read(data, 0, 100), Is.EqualTo(100));
                Assert.That(buffer.WaitForRead(1, Timeout.Infinite), Is.False);
            }
        }

        [Test]
        public void WaitForReadCancellationTokenDeadProducedData()
        {
            using (CancellationTokenSource cts = new())
            using (MemoryReadBuffer buffer = new(4096)) {
                // Even though the device died, there is still data in the buffer, so this takes precedence.
                buffer.Produce(100);
                buffer.DeviceDead();
                Assert.That(buffer.IsDeviceDead, Is.True);
                Assert.That(buffer.WaitForRead(Timeout.Infinite, cts.Token), Is.True);

                // And once all data is read, then timeouts do not apply.
                byte[] data = new byte[100];
                Assert.That(buffer.Read(data, 0, 100), Is.EqualTo(100));
                Assert.That(buffer.WaitForRead(Timeout.Infinite, cts.Token), Is.False);
            }
        }

        [Test]
        public void WaitForReadCountCancellationTokenDeadProducedData()
        {
            using (CancellationTokenSource cts = new())
            using (MemoryReadBuffer buffer = new(4096)) {
                // Even though the device died, there is still data in the buffer, so this takes precedence.
                buffer.Produce(100);
                buffer.DeviceDead();
                Assert.That(buffer.IsDeviceDead, Is.True);
                Assert.That(buffer.WaitForRead(100, Timeout.Infinite, cts.Token), Is.True);

                // And once all data is read, then timeouts do not apply.
                byte[] data = new byte[100];
                Assert.That(buffer.Read(data, 0, 100), Is.EqualTo(100));
                Assert.That(buffer.WaitForRead(1, Timeout.Infinite, cts.Token), Is.False);
            }
        }

        [Test]
        [Repeat(200)]
        public void WaitForReadDispose()
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
                    wait = buffer.WaitForRead(Timeout.Infinite);
                    Assert.That(buffer.BytesToRead, Is.EqualTo(0));
                } catch (ObjectDisposedException) {
                    // The exception is expected when the buffer is disposed before WaitForRead is called.
                    wait = false;
                }
                Assert.That(wait, Is.False);
                Assert.That(buffer.IsDeviceDead, Is.True);
                driver.Wait();
            } finally {
                if (buffer is not null) buffer.Dispose();
            }
        }

        [Test]
        public void BufferFullReadByte()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                buffer.Produce(4096);
                Assert.That(buffer.IsBufferNotFull, Is.False);
                Assert.That(buffer.BufferWriteLength, Is.EqualTo(0));

                Task user = new TaskFactory().StartNew(() => {
                    Thread.Sleep(100);
                    buffer.ReadByte();
                });

                Assert.That(buffer.BufferNotFull.WaitOne(500), Is.True);
                Assert.That(buffer.IsBufferNotFull, Is.True);
                Assert.That(buffer.BufferWriteLength, Is.EqualTo(1));
                Assert.That(buffer.BytesToRead, Is.EqualTo(4095));
                user.Wait();
            }
        }

        [Test]
        public void BufferFullRead()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                buffer.Produce(4096);
                Assert.That(buffer.IsBufferNotFull, Is.False);
                Assert.That(buffer.BufferWriteLength, Is.EqualTo(0));

                Task user = new TaskFactory().StartNew(() => {
                    Thread.Sleep(100);
                    byte[] data = new byte[1];
                    buffer.Read(data, 0, 1);
                });

                Assert.That(buffer.BufferNotFull.WaitOne(500), Is.True);
                Assert.That(buffer.IsBufferNotFull, Is.True);
                Assert.That(buffer.BufferWriteLength, Is.EqualTo(1));
                Assert.That(buffer.BytesToRead, Is.EqualTo(4095));
                user.Wait();
            }
        }

        [Test]
        public void WaitForReadCancellationToken()
        {
            using (CancellationTokenSource cts = new())
            using (MemoryReadBuffer buffer = new(4096)) {
                Task user = new TaskFactory().StartNew(() => {
                    Thread.Sleep(100);
                    cts.Cancel();
                });
                Assert.That(buffer.WaitForRead(Timeout.Infinite, cts.Token), Is.False);
                Assert.That(buffer.BytesToRead, Is.EqualTo(0));
                user.Wait();
            }
        }

        [Test]
        public void WaitForReadCancellationTokenNone()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                Assert.That(buffer.WaitForRead(100, CancellationToken.None), Is.False);
                Assert.That(buffer.BytesToRead, Is.EqualTo(0));
            }
        }

        [TestCase(false, 0, false, TestName = "WaitForReadCancellationTokenZeroTimeoutNoData")]
        [TestCase(false, 100, true, TestName = "WaitForReadCancellationTokenZeroTimeoutWithData")]
        [TestCase(true, 0, false, TestName = "WaitForReadCancellationTokenZeroTimeoutNoDataCancelled")]
        [TestCase(true, 100, false, TestName = "WaitForReadCancellationTokenZeroTimeoutWithDataCancelled")]
        public void WaitForReadCancellationTokenZeroTimeout(bool cancelled, int produce, bool result)
        {
            using (CancellationTokenSource cts = new())
            using (MemoryReadBuffer buffer = new(4096)) {
                if (cancelled) cts.Cancel();
                if (produce > 0) buffer.Produce(produce);
                Assert.That(buffer.WaitForRead(0, cts.Token), Is.EqualTo(result));
                Assert.That(buffer.BytesToRead, Is.EqualTo(produce));
            }
        }

        [TestCase(0, false, TestName = "WaitForReadCancellationTokenNoneZeroTimeoutNoData")]
        [TestCase(100, true, TestName = "WaitForReadCancellationTokenNoneZeroTimeoutWithData")]
        public void WaitForReadCancellationTokenNoneZeroTimeout(int produce, bool result)
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                if (produce > 0) buffer.Produce(produce);
                Assert.That(buffer.WaitForRead(0, CancellationToken.None), Is.EqualTo(result));
                Assert.That(buffer.BytesToRead, Is.EqualTo(produce));
            }
        }

        [Test]
        [Repeat(200)]
        public void WaitForReadCount()
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

                Assert.That(buffer.WaitForRead(950, Timeout.Infinite), Is.True);
                Assert.That(buffer.BytesToRead, Is.EqualTo(1000));
                producer.Wait();
            }
        }

        [Test]
        public void WaitForReadCountTimeout()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                Assert.That(buffer.WaitForRead(100, 100), Is.False);
            }
        }

        [TestCase(0, 100, false, TestName = "WaitForReadCountZeroTimeoutNoData1")]
        [TestCase(0, 0, true, TestName = "WaitForReadCountZeroTimeoutNoData2")]
        [TestCase(100, 50, true, TestName = "WaitForReadCountZeroTimeoutWithData1")]
        [TestCase(100, 0, true, TestName = "WaitForReadCountZeroTimeoutWithData2")]
        [TestCase(100, 150, false, TestName = "WaitForReadCountZeroTimeoutWithInsufficientData")]
        public void WaitForReadCountZeroTimeout(int produce, int count, bool result)
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                if (produce > 0) buffer.Produce(produce);
                Assert.That(buffer.WaitForRead(count, 0), Is.EqualTo(result));
                Assert.That(buffer.BytesToRead, Is.EqualTo(produce));
            }
        }

        [Test]
        [Repeat(200)]
        public void WaitForReadCountDeviceDead()
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

                Assert.That(buffer.WaitForRead(950, Timeout.Infinite), Is.False);
                Assert.That(buffer.BytesToRead, Is.EqualTo(200));
                driver.Wait();
            }
        }

        [Test]
        [Repeat(200)]
        public void WaitForReadCountDispose()
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
                    wait = buffer.WaitForRead(950, Timeout.Infinite);

                    // Will probably never get here.
                    Assert.That(buffer.BytesToRead, Is.EqualTo(200));
                } catch (ObjectDisposedException) {
                    // The exception is expected when the buffer is disposed before WaitForRead is called.
                    wait = false;
                }
                Assert.That(wait, Is.False);
                Assert.That(buffer.IsDeviceDead, Is.True);
                driver.Wait();
            } finally {
                // A double dispose shouldn't cause a problem.
                if (buffer is not null) buffer.Dispose();
            }
        }

        [Test]
        public void WaitForReadCountCancellationToken()
        {
            using (CancellationTokenSource cts = new())
            using (MemoryReadBuffer buffer = new(4096)) {
                Task user = new TaskFactory().StartNew(() => {
                    Thread.Sleep(100);
                    cts.Cancel();
                });
                Assert.That(buffer.WaitForRead(1000, Timeout.Infinite, cts.Token), Is.False);
                Assert.That(buffer.BytesToRead, Is.EqualTo(0));
                user.Wait();
            }
        }

        [Test]
        public void WaitForReadCountWithDataCancellationToken()
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
                Assert.That(buffer.WaitForRead(1000, Timeout.Infinite, cts.Token), Is.False);
                Assert.That(buffer.BytesToRead, Is.EqualTo(200));
                t.Wait();
            }
        }

        [Test]
        public void WaitForReadCountCancellationTokenNone()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                Assert.That(buffer.WaitForRead(1000, 100, CancellationToken.None), Is.False);
                Assert.That(buffer.BytesToRead, Is.EqualTo(0));
            }
        }

        [Test]
        public void WaitForReadCountWithDataCancellationTokenNone()
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

                Assert.That(buffer.WaitForRead(1000, 1000, CancellationToken.None), Is.False);
                Assert.That(buffer.BytesToRead, Is.EqualTo(200));
                t.Wait();
            }
        }

        [TestCase(false, 0, 100, false, TestName = "WaitForReadCountCancellationTokenZeroTimeoutNoData1")]
        [TestCase(false, 0, 0, true, TestName = "WaitForReadCountCancellationTokenZeroTimeoutNoData2")]
        [TestCase(false, 100, 50, true, TestName = "WaitForReadCountCancellationTokenZeroTimeoutWithData1")]
        [TestCase(false, 100, 0, true, TestName = "WaitForReadCountCancellationTokenZeroTimeoutWithData2")]
        [TestCase(false, 100, 150, false, TestName = "WaitForReadCountCancellationTokenZeroTimeoutWithInsufficientData")]
        [TestCase(true, 0, 100, false, TestName = "WaitForReadCountCancellationTokenZeroTimeoutNoData1Cancelled")]
        [TestCase(true, 0, 0, false, TestName = "WaitForReadCountCancellationTokenZeroTimeoutNoData2Cancelled")]
        [TestCase(true, 100, 50, false, TestName = "WaitForReadCountCancellationTokenZeroTimeoutWithData1Cancelled")]
        [TestCase(true, 100, 0, false, TestName = "WaitForReadCountCancellationTokenZeroTimeoutWithData2Cancelled")]
        [TestCase(true, 100, 150, false, TestName = "WaitForReadCountCancellationTokenZeroTimeoutWithInsufficientDataCancelled")]
        public void WaitForReadCountCancellationTokenZeroTimeout(bool cancelled, int produce, int count, bool result)
        {
            using (CancellationTokenSource cts = new())
            using (MemoryReadBuffer buffer = new(4096)) {
                if (cancelled) cts.Cancel();
                if (produce > 0) buffer.Produce(produce);
                Assert.That(buffer.WaitForRead(count, 0, cts.Token), Is.EqualTo(result));
                Assert.That(buffer.BytesToRead, Is.EqualTo(produce));
            }
        }

        [TestCase(0, 100, false, TestName = "WaitForReadCountCancellationTokenNoneZeroTimeoutNoData1")]
        [TestCase(0, 0, true, TestName = "WaitForReadCountCancellationTokenNoneZeroTimeoutNoData2")]
        [TestCase(100, 50, true, TestName = "WaitForReadCountCancellationTokenNoneZeroTimeoutWithData1")]
        [TestCase(100, 0, true, TestName = "WaitForReadCountCancellationTokenNoneZeroTimeoutWithData2")]
        [TestCase(100, 150, false, TestName = "WaitForReadCountCancellationTokenNoneZeroTimeoutWithInsufficientData")]
        public void WaitForReadCountCancellationNoneTokenZeroTimeout(int produce, int count, bool result)
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                if (produce > 0) buffer.Produce(produce);
                Assert.That(buffer.WaitForRead(count, 0, CancellationToken.None), Is.EqualTo(result));
                Assert.That(buffer.BytesToRead, Is.EqualTo(produce));
            }
        }

        [Test]
        public void WaitForReadCountAtCapacity()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                Assert.That(buffer.WaitForRead(4097, Timeout.Infinite), Is.False);
            }
        }

        [TestCase(false, TestName = "WaitForReadCountAtCapacityCancellationToken")]
        [TestCase(true, TestName = "WaitForReadCountAtCapacityCancellationTokenCancelled")]
        public void WaitForReadCountAtCapacityCancellationToken(bool cancelled)
        {
            using (CancellationTokenSource cts = new())
            using (MemoryReadBuffer buffer = new(4096)) {
                if (cancelled) cts.Cancel();
                Assert.That(buffer.WaitForRead(4097, Timeout.Infinite, cts.Token), Is.False);
            }
        }

        [Test]
        public void WaitForReadCountAtCapacityCancellationTokenNone()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                Assert.That(buffer.WaitForRead(4097, Timeout.Infinite, CancellationToken.None), Is.False);
            }
        }

        [Test]
        public void WaitForReadNegativeTimeout()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                Assert.That(() => { buffer.WaitForRead(-2); },
                    Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(buffer.BytesToRead, Is.EqualTo(0));
            }
        }

        [Test]
        public void WaitForReadNegativeTimeoutCancellationToken()
        {
            using (CancellationTokenSource cts = new())
            using (MemoryReadBuffer buffer = new(4096)) {
                Assert.That(() => { buffer.WaitForRead(-2, cts.Token); },
                    Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(buffer.BytesToRead, Is.EqualTo(0));
            }
        }

        [Test]
        public void WaitForReadCountNegativeTimeout()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                Assert.That(() => { buffer.WaitForRead(500, -2); },
                    Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(buffer.BytesToRead, Is.EqualTo(0));
            }
        }

        [Test]
        public void WaitForReadCountNegativeTimeoutCancellationToken()
        {
            using (CancellationTokenSource cts = new())
            using (MemoryReadBuffer buffer = new(4096)) {
                Assert.That(() => { buffer.WaitForRead(500, -2, cts.Token); },
                    Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(buffer.BytesToRead, Is.EqualTo(0));
            }
        }

        [Test]
        public void WaitForReadCountNegativeTimeoutCancellationTokenNone()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                Assert.That(() => { buffer.WaitForRead(500, -2, CancellationToken.None); },
                    Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(buffer.BytesToRead, Is.EqualTo(0));
            }
        }

        [Test]
        public void WaitForReadNegativeCount()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                Assert.That(() => { buffer.WaitForRead(-100, Timeout.Infinite); },
                    Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(buffer.BytesToRead, Is.EqualTo(0));
            }
        }

        [Test]
        public void WaitForReadNegativeCountCancellationToken()
        {
            using (CancellationTokenSource cts = new())
            using (MemoryReadBuffer buffer = new(4096)) {
                Assert.That(() => { buffer.WaitForRead(-100, Timeout.Infinite, cts.Token); },
                    Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(buffer.BytesToRead, Is.EqualTo(0));
            }
        }

        [Test]
        public void WaitForReadNegativeCountCancellationTokenNone()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                Assert.That(() => { buffer.WaitForRead(-100, Timeout.Infinite, CancellationToken.None); },
                    Throws.TypeOf<ArgumentOutOfRangeException>());
                Assert.That(buffer.BytesToRead, Is.EqualTo(0));
            }
        }

        [Test]
        public void Clear()
        {
            using (MemoryReadBuffer buffer = new(4096)) {
                buffer.Produce(3072);
                Assert.That(buffer.BytesToRead, Is.EqualTo(3072));

                buffer.Clear();
                Assert.That(buffer.BytesToRead, Is.EqualTo(0));
                Assert.That(buffer.IsBufferNotFull, Is.True);

                buffer.Produce(4096);
                Assert.That(buffer.BytesToRead, Is.EqualTo(4096));
                Assert.That(buffer.IsBufferNotFull, Is.False);

                buffer.Clear();
                Assert.That(buffer.BytesToRead, Is.EqualTo(0));
                Assert.That(buffer.IsBufferNotFull, Is.True);
            }
        }

        [Test]
        public void Read()
        {
            Random r = new();
            byte[] data = new byte[256];
            r.NextBytes(data);

            using (MemoryReadBuffer buffer = new(4096)) {
                // Fill the MemoryReadBuffer with data. It uses a CircularBuffer and so the index it starts at is zero.
                Array.Copy(data, 0, buffer.Buffer, buffer.BufferEnd, data.Length);
                buffer.Produce(data.Length);

                // Read the data and check the contents
                byte[] result = new byte[256];
                Assert.That(buffer.Read(result, 0, result.Length), Is.EqualTo(result.Length));
                Assert.That(result, Is.EqualTo(data));
            }
        }

        [Test]
        public void ReadWrap()
        {
            Random r = new();
            byte[] data = new byte[256];
            r.NextBytes(data);

            using (MemoryReadBuffer buffer = new(4096)) {
                // Fill the MemoryReadBuffer with data. It uses a CircularBuffer and so the index it starts at is zero.
                buffer.Produce(3968);                             // Shift the offset to position 3968 of 4096
                Array.Copy(data, 0, buffer.Buffer, buffer.BufferEnd, 128);
                buffer.Produce(128);                              // Fill the last 128 bytes
                buffer.Read(new byte[3968], 0, 3968);             // Free up the 3968 bytes to write the next block
                Array.Copy(data, 128, buffer.Buffer, 0, 128);     // Write the second half of the data
                buffer.Produce(128);

                Assert.That(buffer.BufferEnd, Is.EqualTo(128));
                Assert.That(buffer.BufferWriteLength, Is.EqualTo(3840));

                // Read the data and check the contents
                byte[] result = new byte[256];
                Assert.That(buffer.Read(result, 0, result.Length), Is.EqualTo(result.Length));
                Assert.That(result, Is.EqualTo(data));
            }
        }

#if NET6_0_OR_GREATER
        [Test]
        public void ReadSpan()
        {
            Random r = new();
            byte[] data = new byte[256];
            r.NextBytes(data);

            using (MemoryReadBuffer buffer = new(4096)) {
                // Fill the MemoryReadBuffer with data. It uses a CircularBuffer and so the index it starts at is zero.
                Array.Copy(data, 0, buffer.Buffer, buffer.BufferEnd, data.Length);
                buffer.Produce(data.Length);

                // Read the data and check the contents
                byte[] result = new byte[256];
                Span<byte> span = result;
                Assert.That(buffer.Read(span), Is.EqualTo(result.Length));
                Assert.That(result, Is.EqualTo(data));
            }
        }

        [Test]
        public void ReadWrapSpan()
        {
            Random r = new();
            byte[] data = new byte[256];
            r.NextBytes(data);

            using (MemoryReadBuffer buffer = new(4096)) {
                // Fill the MemoryReadBuffer with data. It uses a CircularBuffer and so the index it starts at is zero.
                buffer.Produce(3968);                             // Shift the offset to position 3968 of 4096
                Array.Copy(data, 0, buffer.Buffer, buffer.BufferEnd, 128);
                buffer.Produce(128);                              // Fill the last 128 bytes
                buffer.Read(new byte[3968], 0, 3968);             // Free up the 3968 bytes to write the next block
                Array.Copy(data, 128, buffer.Buffer, 0, 128);     // Write the second half of the data
                buffer.Produce(128);

                Assert.That(buffer.BufferEnd, Is.EqualTo(128));
                Assert.That(buffer.BufferWriteLength, Is.EqualTo(3840));

                // Read the data and check the contents
                byte[] result = new byte[256];
                Span<byte> span = result;
                Assert.That(buffer.Read(span), Is.EqualTo(result.Length));
                Assert.That(result, Is.EqualTo(data));
            }
        }

        [Test]
        public void ReadSpanInsertViaSpan()
        {
            Random r = new();
            byte[] data = new byte[256];
            r.NextBytes(data);

            using (MemoryReadBuffer buffer = new(4096)) {
                // We could just use r.NextBytes(buffer.BufferSpan[0..256]) but then we need to compare later.
                data.AsSpan().CopyTo(buffer.BufferSpan);
                buffer.Produce(data.Length);

                // Read the data and check the contents
                byte[] result = new byte[256];
                Span<byte> span = result;
                Assert.That(buffer.Read(span), Is.EqualTo(result.Length));
                Assert.That(result, Is.EqualTo(data));
            }
        }

        [Test]
        public void ReadWrapSpanInsertViaSpan()
        {
            Random r = new();
            byte[] data = new byte[256];
            r.NextBytes(data);

            using (MemoryReadBuffer buffer = new(4096)) {
                buffer.Produce(3968);                             // Shift the offset to position 3968 of 4096
                data.AsSpan(0, 128).CopyTo(buffer.BufferSpan);
                buffer.Produce(128);                              // Fill the last 128 bytes
                buffer.Read(new byte[3968], 0, 3968);             // Free up the 3968 bytes to write the next block
                data.AsSpan(128, 128).CopyTo(buffer.BufferSpan);  // Write the second half of the data
                buffer.Produce(128);

                Assert.That(buffer.BufferEnd, Is.EqualTo(128));
                Assert.That(buffer.BufferWriteLength, Is.EqualTo(3840));

                // Read the data and check the contents
                byte[] result = new byte[256];
                Span<byte> span = result;
                Assert.That(buffer.Read(span), Is.EqualTo(result.Length));
                Assert.That(result, Is.EqualTo(data));
            }
        }
#endif
    }
}
