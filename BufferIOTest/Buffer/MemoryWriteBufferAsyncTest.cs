namespace RJCP.IO.Buffer
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    // These test cases are a copy of MemoryWriteBufferTest, WaitForWrite and WaitForEmpty.

    internal static class MemoryWriteBufferAsyncTestExtensions
    {
        public static Task<bool> WaitForWriteAsync(this MemoryWriteBuffer buffer, WriteOverload overload, int count, int timeout, CancellationTokenSource cts)
        {
            switch (overload) {
            case WriteOverload.Normal:
                return buffer.WaitForWriteAsync(count, timeout);
            case WriteOverload.Token:
                return buffer.WaitForWriteAsync(count, timeout, cts.Token);
            case WriteOverload.TokenNone:
                return buffer.WaitForWriteAsync(count, timeout, CancellationToken.None);
            default:
                throw new ArgumentException("Invalid overload", nameof(overload));
            }
        }

        public static Task<bool> WaitForEmptyAsync(this MemoryWriteBuffer buffer, WriteOverload overload, int timeout, CancellationTokenSource cts)
        {
            switch (overload) {
            case WriteOverload.Normal:
                return buffer.WaitForEmptyAsync(timeout);
            case WriteOverload.Token:
                return buffer.WaitForEmptyAsync(timeout, cts.Token);
            case WriteOverload.TokenNone:
                return buffer.WaitForEmptyAsync(timeout, CancellationToken.None);
            default:
                throw new ArgumentException("Invalid overload", nameof(overload));
            }
        }
    }

    [TestFixture]
    [Timeout(2000)]
    public class MemoryWriteBufferAsyncTest
    {
        [TestCase(WriteOverload.Normal, TestName = "WaitForWriteAsync")]
        [TestCase(WriteOverload.Token, TestName = "WaitForWriteAsyncCancellationToken")]
        [TestCase(WriteOverload.TokenNone, TestName = "WaitForWriteAsyncCancellationTokenNone")]
        public async Task WaitForWriteAsync(WriteOverload overload)
        {
            using (CancellationTokenSource cts = new())
            using (MemoryWriteBuffer buffer = new(4096)) {
                Assert.That(await buffer.WaitForWriteAsync(overload, 1, Timeout.Infinite, cts), Is.True);
            }
        }

        [TestCase(WriteOverload.Normal, TestName = "WaitForWriteAsyncFull")]
        [TestCase(WriteOverload.Token, TestName = "WaitForWriteAsyncFullCancellationToken")]
        [TestCase(WriteOverload.TokenNone, TestName = "WaitForWriteAsyncFullCancellationTokenNone")]
        [Repeat(200)]
        public async Task WaitForWriteAsyncFull(WriteOverload overload)
        {
            using (CancellationTokenSource cts = new())
            using (MemoryWriteBuffer buffer = new(4096)) {
                buffer.Write(new byte[4096], 0, 4096);
                Assert.That(buffer.BytesFree, Is.EqualTo(0));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(4096));

                Task producer = new TaskFactory().StartNew(() => {
                    // Try to randomize the order in which things run to look for race conditions.
                    Random r1 = new();
                    if (r1.Next(2) == 0) Thread.Sleep(r1.Next(2));

                    lock (buffer.Lock) {
                        buffer.Consume(100);
                    }
                });

                // Try to randomize the order in which things run to look for race conditions.
                Random r2 = new();
                if (r2.Next(2) == 0) Thread.Sleep(r2.Next(2));
                Assert.That(await buffer.WaitForWriteAsync(overload, 1, Timeout.Infinite, cts), Is.True);
                Assert.That(buffer.BytesFree, Is.GreaterThan(0));
                Assert.That(buffer.BytesToWrite, Is.LessThan(4096));
                await producer;
            }
        }

        [Test]
        public async Task WaitForWriteAsyncFullCancel()
        {
            using (CancellationTokenSource cts = new())
            using (MemoryWriteBuffer buffer = new(4096)) {
                buffer.Write(new byte[4096], 0, 4096);
                Assert.That(buffer.BytesFree, Is.EqualTo(0));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(4096));

                Task user = new TaskFactory().StartNew(() => {
                    // User code would at some time cancel the operation
                    Thread.Sleep(100);
                    cts.Cancel();
                });

                Assert.That(await buffer.WaitForWriteAsync(1, Timeout.Infinite, cts.Token), Is.False);
                Assert.That(buffer.BytesFree, Is.EqualTo(0));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(4096));
                await user;
            }
        }

        [Test]
        public async Task WaitForWriteAsyncFullCancelPrior()
        {
            using (CancellationTokenSource cts = new())
            using (MemoryWriteBuffer buffer = new(4096)) {
                buffer.Write(new byte[4096], 0, 4096);
                Assert.That(buffer.BytesFree, Is.EqualTo(0));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(4096));
                cts.Cancel();

                Assert.That(await buffer.WaitForWriteAsync(1, Timeout.Infinite, cts.Token), Is.False);
                Assert.That(buffer.BytesFree, Is.EqualTo(0));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(4096));
            }
        }

        [TestCase(0, false, WriteOverload.Normal, TestName = "WaitForWritAsynceZeroTimeoutFull")]
        [TestCase(100, true, WriteOverload.Normal, TestName = "WaitForWriteAsyncZeroTimeoutNotFull")]
        [TestCase(0, false, WriteOverload.Token, TestName = "WaitForWriteAsyncZeroTimeoutFullCancellationToken")]
        [TestCase(100, true, WriteOverload.Token, TestName = "WaitForWriteAsyncZeroTimeoutNotFullCancellationToken")]
        [TestCase(0, false, WriteOverload.TokenNone, TestName = "WaitForWriteAsyncZeroTimeoutFullCancellationTokenNone")]
        [TestCase(100, true, WriteOverload.TokenNone, TestName = "WaitForWriteAsyncZeroTimeoutNotFullCancellationTokenNone")]
        public async Task WaitForWriteAsyncZeroTimeout(int free, bool result, WriteOverload overload)
        {
            using (CancellationTokenSource cts = new())
            using (MemoryWriteBuffer buffer = new(4096)) {
                if (free < 4096) buffer.Write(new byte[4096], 0, 4096 - free);
                Assert.That(await buffer.WaitForWriteAsync(overload, 1, 0, cts), Is.EqualTo(result));
                Assert.That(buffer.BytesFree, Is.EqualTo(free));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(4096 - free));
            }
        }

        [Test]
        public async Task WaitForWriteAsyncToFull()
        {
            using (MemoryWriteBuffer buffer = new(4096)) {
                Assert.That(await buffer.WaitForWriteAsync(1, 0), Is.True);
                Assert.That(await buffer.WaitForWriteAsync(1, 1), Is.True);
                buffer.Write(new byte[4096], 0, 4096);
                Assert.That(await buffer.WaitForWriteAsync(1, 0), Is.False);
                Assert.That(await buffer.WaitForWriteAsync(1, 1), Is.False);
                buffer.Consume(1);
                Assert.That(await buffer.WaitForWriteAsync(1, 0), Is.True);
                Assert.That(await buffer.WaitForWriteAsync(1, 1), Is.True);
                Assert.That(await buffer.WaitForWriteAsync(2, 0), Is.False);
                Assert.That(await buffer.WaitForWriteAsync(2, 1), Is.False);
            }
        }

        [TestCase(WriteOverload.Normal, TestName = "WaitForWriteAsyncDeviceDead")]
        [TestCase(WriteOverload.Token, TestName = "WaitForWriteAsyncDeviceDeadCancellationToken")]
        [TestCase(WriteOverload.TokenNone, TestName = "WaitForWriteAsyncDeviceDeadCancellationTokenNone")]
        [Repeat(200)]
        public async Task WaitForWriteAsyncDeviceDead(WriteOverload overload)
        {
            using (CancellationTokenSource cts = new())
            using (MemoryWriteBuffer buffer = new(4096)) {
                buffer.Write(new byte[4096], 0, 4096);
                Assert.That(buffer.BytesFree, Is.EqualTo(0));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(4096));

                Task driver = new TaskFactory().StartNew(() => {
                    // Try to randomize the order in which things run to look for race conditions.
                    Random r1 = new();
                    if (r1.Next(2) == 0) Thread.Sleep(r1.Next(2));

                    buffer.DeviceDead();
                });

                // Try to randomize the order in which things run to look for race conditions.
                Random r2 = new();
                if (r2.Next(2) == 0) Thread.Sleep(r2.Next(2));

                Assert.That(await buffer.WaitForWriteAsync(overload, 1, Timeout.Infinite, cts), Is.False);
                Assert.That(buffer.BytesFree, Is.EqualTo(4096));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(0));
                Assert.That(buffer.IsDeviceDead, Is.True);
                await driver;
            }
        }

        [TestCase(false, WriteOverload.Normal, TestName = "WaitForWriteAsyncDeadTryWriteEmpty")]
        [TestCase(false, WriteOverload.Token, TestName = "WaitForWriteAsyncDeadTryWriteEmptyCancellationToken")]
        [TestCase(false, WriteOverload.TokenNone, TestName = "WaitForWriteAsyncDeadTryWriteEmptyCancellationTokenNone")]
        [TestCase(true, WriteOverload.Normal, TestName = "WaitForWriteAsyncDeadTryWriteFull")]
        [TestCase(true, WriteOverload.Token, TestName = "WaitForWriteAsyncDeadTryWriteFullCancellationToken")]
        [TestCase(true, WriteOverload.TokenNone, TestName = "WaitForWriteAsyncDeadTryWriteFullCancellationTokenNone")]
        public async Task WaitForWriteAsyncDeadTryWrite(bool full, WriteOverload overload)
        {
            using (CancellationTokenSource cts = new())
            using (MemoryWriteBuffer buffer = new(4096)) {
                // When writing to a dead device, the write operation does nothing.
                if (full) buffer.Write(new byte[4096], 0, 4096);

                buffer.DeviceDead();
                Assert.That(await buffer.WaitForWriteAsync(overload, 1, Timeout.Infinite, cts), Is.False);
                Assert.That(buffer.BytesFree, Is.EqualTo(4096));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(0));
                Assert.That(buffer.IsDeviceDead, Is.True);
            }
        }

        [TestCase(WriteOverload.Normal, TestName = "WaitForWriteAsyncDispose")]
        [TestCase(WriteOverload.Token, TestName = "WaitForWriteAsyncDisposeCancellationToken")]
        [TestCase(WriteOverload.TokenNone, TestName = "WaitForWriteAsyncDisposeCancellationTokenNone")]
        [Repeat(200)]
        public async Task WaitForWriteAsyncDispose(WriteOverload overload)
        {
            MemoryWriteBuffer buffer = null;
            CancellationTokenSource cts = new();
            try {
                buffer = new MemoryWriteBuffer(4096);
                buffer.Write(new byte[4096], 0, 4096);
                Task driver = new TaskFactory().StartNew(() => {
                    for (int i = 0; i < 2; i++) {
                        // Try to randomize the order in which things run to look for race conditions.
                        Random r1 = new();
                        if (r1.Next(2) == 0) Thread.Sleep(r1.Next(2));

                        lock (buffer.Lock) {
                            buffer.Consume(100);
                        }
                    }

                    buffer.Dispose();
                });

                Random r2 = new();
                if (r2.Next(2) == 0) Thread.Sleep(r2.Next(2));

                bool wait = false;
                try {
                    wait = await buffer.WaitForWriteAsync(overload, 250, Timeout.Infinite, cts);
                } catch (ObjectDisposedException) {
                    // The exception is expected when the buffer is disposed before WaitForRead is called.
                    wait = false;
                }
                Assert.That(wait, Is.False);
                Assert.That(buffer.IsDeviceDead, Is.True);
                Assert.That(buffer.BytesFree, Is.EqualTo(4096));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(0));
                await driver;
            } finally {
                // A double dispose shouldn't cause a problem.
                if (buffer is not null) buffer.Dispose();
                cts.Dispose();
            }
        }

        [Test]
        public async Task WaitForWriteAsyncToEmpty()
        {
            using (MemoryWriteBuffer buffer = new(4096)) {
                Assert.That(buffer.IsBufferNotEmpty, Is.False);
                Assert.That(buffer.BytesToWrite, Is.EqualTo(0));
                Assert.That(await buffer.WaitForWriteAsync(1, 0), Is.True);
                Assert.That(await buffer.WaitForWriteAsync(1, 1), Is.True);

                buffer.Write(new byte[100], 0, 100);
                Assert.That(buffer.IsBufferNotEmpty, Is.True);
                Assert.That(buffer.BytesFree, Is.EqualTo(4096 - 100));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(100));
                Assert.That(await buffer.WaitForWriteAsync(1, 0), Is.True);
                Assert.That(await buffer.WaitForWriteAsync(1, 1), Is.True);

                buffer.Write(new byte[3996], 0, 3996);
                Assert.That(buffer.IsBufferNotEmpty, Is.True);
                Assert.That(buffer.BytesFree, Is.EqualTo(0));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(4096));
                Assert.That(await buffer.WaitForWriteAsync(1, 0), Is.False);
                Assert.That(await buffer.WaitForWriteAsync(1, 1), Is.False);

                buffer.Consume(100);
                Assert.That(buffer.IsBufferNotEmpty, Is.True);
                Assert.That(buffer.BytesFree, Is.EqualTo(100));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(3996));
                Assert.That(await buffer.WaitForWriteAsync(1, 0), Is.True);
                Assert.That(await buffer.WaitForWriteAsync(1, 1), Is.True);

                buffer.Consume(3996);
                Assert.That(buffer.IsBufferNotEmpty, Is.False);
                Assert.That(buffer.BytesFree, Is.EqualTo(4096));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(0));
                Assert.That(await buffer.WaitForWriteAsync(1, 0), Is.True);
                Assert.That(await buffer.WaitForWriteAsync(1, 1), Is.True);
            }
        }

        [TestCase(WriteOverload.Normal, TestName = "WaitForWriteAsyncInvalidCount")]
        [TestCase(WriteOverload.Token, TestName = "WaitForWriteAsyncInvalidCountCancellationToken")]
        [TestCase(WriteOverload.TokenNone, TestName = "WaitForWriteAsyncInvalidCountCancellationTokenNone")]
        public void WaitForWriteInvalidCount(WriteOverload overload)
        {
            using (CancellationTokenSource cts = new())
            using (MemoryWriteBuffer buffer = new(4096)) {
                Assert.That(async () => {
                    await buffer.WaitForWriteAsync(overload, -1, Timeout.Infinite, cts);
                }, Throws.TypeOf<ArgumentOutOfRangeException>());
            }
        }

        [TestCase(WriteOverload.Normal, TestName = "WaitForWriteAsyncInvalidTimeout")]
        [TestCase(WriteOverload.Token, TestName = "WaitForWriteAsyncInvalidTimeoutCancellationToken")]
        [TestCase(WriteOverload.TokenNone, TestName = "WaitForWriteAsyncInvalidTimeoutCancellationTokenNone")]
        public void WaitForWriteInvalidTimeout(WriteOverload overload)
        {
            using (CancellationTokenSource cts = new())
            using (MemoryWriteBuffer buffer = new(4096)) {
                Assert.That(async () => {
                    await buffer.WaitForWriteAsync(overload, 1, -2, cts);
                }, Throws.TypeOf<ArgumentOutOfRangeException>());
            }
        }

        [TestCase(WriteOverload.Normal, TestName = "WaitForWriteAsyncCapacity")]
        [TestCase(WriteOverload.Token, TestName = "WaitForWriteAsyncCapacityCancellationToken")]
        [TestCase(WriteOverload.TokenNone, TestName = "WaitForWriteAsyncCapacityCancellationTokenNone")]
        public async Task WaitForWriteCapacity(WriteOverload overload)
        {
            using (CancellationTokenSource cts = new())
            using (MemoryWriteBuffer buffer = new(4096)) {
                Assert.That(await buffer.WaitForWriteAsync(overload, 4097, Timeout.Infinite, cts), Is.False);
            }
        }

        [TestCase(WriteOverload.Normal, false, TestName = "WaitForWriteAsyncZeroCount")]
        [TestCase(WriteOverload.Token, false, TestName = "WaitForWriteAsyncZeroCountCancellationToken")]
        [TestCase(WriteOverload.TokenNone, false, TestName = "WaitForWriteAsyncZeroCountCancellationTokenNone")]
        [TestCase(WriteOverload.Normal, true, TestName = "WaitForWriteAsyncZeroCountFull")]
        [TestCase(WriteOverload.Token, true, TestName = "WaitForWriteAsyncZeroCountFullCancellationToken")]
        [TestCase(WriteOverload.TokenNone, true, TestName = "WaitForWriteAsyncZeroCountFullCancellationTokenNone")]
        public async Task WaitForWriteAsyncZeroCount(WriteOverload overload, bool full)
        {
            using (CancellationTokenSource cts = new())
            using (MemoryWriteBuffer buffer = new(4096)) {
                if (full) buffer.Write(new byte[4096], 0, 4096);
                Assert.That(await buffer.WaitForWriteAsync(overload, 0, Timeout.Infinite, cts), Is.True);
            }
        }

        [TestCase(WriteOverload.Normal, TestName = "WaitForEmptyAsyncFromFull")]
        [TestCase(WriteOverload.Token, TestName = "WaitForEmptyAsyncFromFullCancellationToken")]
        [TestCase(WriteOverload.TokenNone, TestName = "WaitForEmptyAsyncFromFullCancellationTokenNone")]
        [Repeat(5)]
        public async Task WaitForEmptyAsyncFromFull(WriteOverload overload)
        {
            using (CancellationTokenSource cts = new())
            using (MemoryWriteBuffer buffer = new(4096)) {
                buffer.Write(new byte[4096], 0, 4096);
                Assert.That(buffer.BytesFree, Is.EqualTo(0));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(4096));

                Task driver = new TaskFactory().StartNew(() => {
                    Thread.Sleep(50);
                    for (int i = 0; i < 2; i++) {
                        // Try to randomize the order in which things run to look for race conditions.
                        Random r1 = new();
                        if (r1.Next(2) == 0) Thread.Sleep(r1.Next(2));

                        lock (buffer.Lock) {
                            buffer.Consume(2048);
                        }
                    }
                });

                Assert.That(await buffer.WaitForEmptyAsync(overload, Timeout.Infinite, cts), Is.True);
                await driver;
            }
        }

        [TestCase(WriteOverload.Normal, TestName = "WaitForEmptyAsync")]
        [TestCase(WriteOverload.Token, TestName = "WaitForEmptyAsyncCancellationToken")]
        [TestCase(WriteOverload.TokenNone, TestName = "WaitForEmptyAsyncCancellationTokenNone")]
        public async Task WaitForEmptyAsync(WriteOverload overload)
        {
            using (CancellationTokenSource cts = new())
            using (MemoryWriteBuffer buffer = new(4096)) {
                Assert.That(buffer.BytesFree, Is.EqualTo(4096));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(0));
                Assert.That(await buffer.WaitForEmptyAsync(overload, Timeout.Infinite, cts), Is.True);
            }
        }

        [Test]
        public async Task WaitForEmptyAsyncCancelled()
        {
            using (CancellationTokenSource cts = new())
            using (MemoryWriteBuffer buffer = new(4096)) {
                buffer.Write(new byte[2048], 0, 2048);
                Assert.That(buffer.BytesFree, Is.EqualTo(2048));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(2048));

                Task user = new TaskFactory().StartNew(() => {
                    Thread.Sleep(50);
                    cts.Cancel();
                });

                Assert.That(await buffer.WaitForEmptyAsync(Timeout.Infinite, cts.Token), Is.False);
                await user;
            }
        }

        [Test]
        public async Task WaitForEmptyAsyncCancelledPrior()
        {
            using (CancellationTokenSource cts = new())
            using (MemoryWriteBuffer buffer = new(4096)) {
                buffer.Write(new byte[2048], 0, 2048);
                Assert.That(buffer.BytesFree, Is.EqualTo(2048));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(2048));

                cts.Cancel();
                Assert.That(await buffer.WaitForEmptyAsync(Timeout.Infinite, cts.Token), Is.False);
            }
        }
    }
}
