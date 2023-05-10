namespace RJCP.IO.Buffer
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    internal static class MemoryWriteBufferTestExtensions
    {
        public static bool WaitForWrite(this MemoryWriteBuffer buffer, WriteOverload overload, int count, int timeout, CancellationTokenSource cts)
        {
            switch (overload) {
            case WriteOverload.Normal:
                return buffer.WaitForWrite(count, timeout);
            case WriteOverload.Token:
                return buffer.WaitForWrite(count, timeout, cts.Token);
            case WriteOverload.TokenNone:
                return buffer.WaitForWrite(count, timeout, CancellationToken.None);
            default:
                throw new ArgumentException("Invalid overload", nameof(overload));
            }
        }

        public static bool WaitForEmpty(this MemoryWriteBuffer buffer, WriteOverload overload, int timeout, CancellationTokenSource cts)
        {
            switch (overload) {
            case WriteOverload.Normal:
                return buffer.WaitForEmpty(timeout);
            case WriteOverload.Token:
                return buffer.WaitForEmpty(timeout, cts.Token);
            case WriteOverload.TokenNone:
                return buffer.WaitForEmpty(timeout, CancellationToken.None);
            default:
                throw new ArgumentException("Invalid overload", nameof(overload));
            }
        }
    }

    [TestFixture]
    [Timeout(2000)]
    public class MemoryWriteBufferTest
    {
        [Test]
        public void CreateDefault()
        {
            using (MemoryWriteBuffer buffer = new MemoryWriteBuffer(4096)) {
                Assert.That(buffer.Lock, Is.Not.Null);
                Assert.That(buffer.BufferPtr, Is.EqualTo(IntPtr.Zero));      // Not pinned, so is zero
                Assert.That(buffer.BytesFree, Is.EqualTo(4096));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(0));
                Assert.That(buffer.IsBufferNotEmpty, Is.False);
                Assert.That(buffer.Buffer, Has.Length.EqualTo(4096));
                Assert.That(buffer.BufferReadLength, Is.EqualTo(0));
                Assert.That(buffer.IsDeviceDead, Is.False);
            }
        }

        [Test]
        public void CreatePinned()
        {
            using (MemoryWriteBuffer buffer = new MemoryWriteBuffer(4096, true)) {
                Assert.That(buffer.Lock, Is.Not.Null);
                Assert.That(buffer.BufferPtr, Is.Not.EqualTo(IntPtr.Zero));  // Pinned, so is real
                Assert.That(buffer.BytesFree, Is.EqualTo(4096));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(0));
                Assert.That(buffer.IsBufferNotEmpty, Is.False);
                Assert.That(buffer.Buffer, Has.Length.EqualTo(4096));
                Assert.That(buffer.BufferReadLength, Is.EqualTo(0));
                Assert.That(buffer.IsDeviceDead, Is.False);
            }
        }

        [TestCase(WriteOverload.Normal, TestName = "WaitForWrite")]
        [TestCase(WriteOverload.Token, TestName = "WaitForWriteCancellationToken")]
        [TestCase(WriteOverload.TokenNone, TestName = "WaitForWriteCancellationTokenNone")]
        public void WaitForWrite(WriteOverload overload)
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (MemoryWriteBuffer buffer = new MemoryWriteBuffer(4096)) {
                Assert.That(buffer.WaitForWrite(overload, 1, Timeout.Infinite, cts), Is.True);
            }
        }

        [TestCase(WriteOverload.Normal, TestName = "WaitForWriteFull")]
        [TestCase(WriteOverload.Token, TestName = "WaitForWriteFullCancellationToken")]
        [TestCase(WriteOverload.TokenNone, TestName = "WaitForWriteFullCancellationTokenNone")]
        [Repeat(200)]
        public void WaitForWriteFull(WriteOverload overload)
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (MemoryWriteBuffer buffer = new MemoryWriteBuffer(4096)) {
                buffer.Write(new byte[4096], 0, 4096);
                Assert.That(buffer.BytesFree, Is.EqualTo(0));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(4096));

                Task producer = new TaskFactory().StartNew(() => {
                    // Try to randomize the order in which things run to look for race conditions.
                    Random r1 = new Random();
                    if (r1.Next(2) == 0) Thread.Sleep(r1.Next(2));

                    lock (buffer.Lock) {
                        buffer.Consume(100);
                    }
                });

                // Try to randomize the order in which things run to look for race conditions.
                Random r2 = new Random();
                if (r2.Next(2) == 0) Thread.Sleep(r2.Next(2));
                Assert.That(buffer.WaitForWrite(overload, 1, Timeout.Infinite, cts), Is.True);
                Assert.That(buffer.BytesFree, Is.GreaterThan(0));
                Assert.That(buffer.BytesToWrite, Is.LessThan(4096));
                producer.Wait();
            }
        }

        [Test]
        public void WaitForWriteFullCancel()
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (MemoryWriteBuffer buffer = new MemoryWriteBuffer(4096)) {
                buffer.Write(new byte[4096], 0, 4096);
                Assert.That(buffer.BytesFree, Is.EqualTo(0));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(4096));

                Task user = new TaskFactory().StartNew(() => {
                    // User code would at some time cancel the operation
                    Thread.Sleep(100);
                    cts.Cancel();
                });

                Assert.That(buffer.WaitForWrite(1, Timeout.Infinite, cts.Token), Is.False);
                Assert.That(buffer.BytesFree, Is.EqualTo(0));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(4096));
                user.Wait();
            }
        }

        [Test]
        public void WaitForWriteFullCancelPrior()
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (MemoryWriteBuffer buffer = new MemoryWriteBuffer(4096)) {
                buffer.Write(new byte[4096], 0, 4096);
                Assert.That(buffer.BytesFree, Is.EqualTo(0));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(4096));
                cts.Cancel();

                Assert.That(buffer.WaitForWrite(1, Timeout.Infinite, cts.Token), Is.False);
                Assert.That(buffer.BytesFree, Is.EqualTo(0));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(4096));
            }
        }

        [TestCase(0, false, WriteOverload.Normal, TestName = "WaitForWriteZeroTimeoutFull")]
        [TestCase(100, true, WriteOverload.Normal, TestName = "WaitForWriteZeroTimeoutNotFull")]
        [TestCase(0, false, WriteOverload.Token, TestName = "WaitForWriteZeroTimeoutFullCancellationToken")]
        [TestCase(100, true, WriteOverload.Token, TestName = "WaitForWriteZeroTimeoutNotFullCancellationToken")]
        [TestCase(0, false, WriteOverload.TokenNone, TestName = "WaitForWriteZeroTimeoutFullCancellationTokenNone")]
        [TestCase(100, true, WriteOverload.TokenNone, TestName = "WaitForWriteZeroTimeoutNotFullCancellationTokenNone")]
        public void WaitForWriteZeroTimeout(int free, bool result, WriteOverload overload)
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (MemoryWriteBuffer buffer = new MemoryWriteBuffer(4096)) {
                if (free < 4096) buffer.Write(new byte[4096], 0, 4096 - free);
                Assert.That(buffer.WaitForWrite(overload, 1, 0, cts), Is.EqualTo(result));
                Assert.That(buffer.BytesFree, Is.EqualTo(free));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(4096 - free));
            }
        }

        [Test]
        public void WaitForWriteToFull()
        {
            using (MemoryWriteBuffer buffer = new MemoryWriteBuffer(4096)) {
                Assert.That(buffer.WaitForWrite(1, 0), Is.True);
                Assert.That(buffer.WaitForWrite(1, 1), Is.True);
                buffer.Write(new byte[4096], 0, 4096);
                Assert.That(buffer.WaitForWrite(1, 0), Is.False);
                Assert.That(buffer.WaitForWrite(1, 1), Is.False);
                buffer.Consume(1);
                Assert.That(buffer.WaitForWrite(1, 0), Is.True);
                Assert.That(buffer.WaitForWrite(1, 1), Is.True);
                Assert.That(buffer.WaitForWrite(2, 0), Is.False);
                Assert.That(buffer.WaitForWrite(2, 1), Is.False);
            }
        }

        [TestCase(WriteOverload.Normal, TestName = "WaitForWriteDeviceDead")]
        [TestCase(WriteOverload.Token, TestName = "WaitForWriteDeviceDeadCancellationToken")]
        [TestCase(WriteOverload.TokenNone, TestName = "WaitForWriteDeviceDeadCancellationTokenNone")]
        [Repeat(200)]
        public void WaitForWriteDeviceDead(WriteOverload overload)
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (MemoryWriteBuffer buffer = new MemoryWriteBuffer(4096)) {
                buffer.Write(new byte[4096], 0, 4096);
                Assert.That(buffer.BytesFree, Is.EqualTo(0));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(4096));

                Task driver = new TaskFactory().StartNew(() => {
                    // Try to randomize the order in which things run to look for race conditions.
                    Random r1 = new Random();
                    if (r1.Next(2) == 0) Thread.Sleep(r1.Next(2));

                    buffer.DeviceDead();
                });

                // Try to randomize the order in which things run to look for race conditions.
                Random r2 = new Random();
                if (r2.Next(2) == 0) Thread.Sleep(r2.Next(2));

                Assert.That(buffer.WaitForWrite(overload, 1, Timeout.Infinite, cts), Is.False);
                Assert.That(buffer.BytesFree, Is.EqualTo(4096));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(0));
                Assert.That(buffer.IsDeviceDead, Is.True);
                driver.Wait();
            }
        }

        [TestCase(false, WriteOverload.Normal, TestName = "WaitForWriteDeadTryWriteEmpty")]
        [TestCase(false, WriteOverload.Token, TestName = "WaitForWriteDeadTryWriteEmptyCancellationToken")]
        [TestCase(false, WriteOverload.TokenNone, TestName = "WaitForWriteDeadTryWriteEmptyCancellationTokenNone")]
        [TestCase(true, WriteOverload.Normal, TestName = "WaitForWriteDeadTryWriteFull")]
        [TestCase(true, WriteOverload.Token, TestName = "WaitForWriteDeadTryWriteFullCancellationToken")]
        [TestCase(true, WriteOverload.TokenNone, TestName = "WaitForWriteDeadTryWriteFullCancellationTokenNone")]
        public void WaitForWriteDeadTryWrite(bool full, WriteOverload overload)
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (MemoryWriteBuffer buffer = new MemoryWriteBuffer(4096)) {
                // When writing to a dead device, the write operation does nothing.
                if (full) buffer.Write(new byte[4096], 0, 4096);

                buffer.DeviceDead();
                Assert.That(buffer.WaitForWrite(overload, 1, Timeout.Infinite, cts), Is.False);
                Assert.That(buffer.BytesFree, Is.EqualTo(4096));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(0));
                Assert.That(buffer.IsDeviceDead, Is.True);
            }
        }

        [TestCase(WriteOverload.Normal, TestName = "WaitForWriteDispose")]
        [TestCase(WriteOverload.Token, TestName = "WaitForWriteDisposeCancellationToken")]
        [TestCase(WriteOverload.TokenNone, TestName = "WaitForWriteDisposeCancellationTokenNone")]
        [Repeat(200)]
        public void WaitForWriteDispose(WriteOverload overload)
        {
            MemoryWriteBuffer buffer = null;
            CancellationTokenSource cts = new CancellationTokenSource();
            try {
                buffer = new MemoryWriteBuffer(4096);
                buffer.Write(new byte[4096], 0, 4096);
                Task driver = new TaskFactory().StartNew(() => {
                    for (int i = 0; i < 2; i++) {
                        // Try to randomize the order in which things run to look for race conditions.
                        Random r1 = new Random();
                        if (r1.Next(2) == 0) Thread.Sleep(r1.Next(2));

                        lock (buffer.Lock) {
                            buffer.Consume(100);
                        }
                    }

                    buffer.Dispose();
                });

                Random r2 = new Random();
                if (r2.Next(2) == 0) Thread.Sleep(r2.Next(2));

                bool wait = false;
                try {
                    wait = buffer.WaitForWrite(overload, 250, Timeout.Infinite, cts);
                } catch (ObjectDisposedException) {
                    // The exception is expected when the buffer is disposed before WaitForRead is called.
                    wait = false;
                }
                Assert.That(wait, Is.False);
                Assert.That(buffer.IsDeviceDead, Is.True);
                Assert.That(buffer.BytesFree, Is.EqualTo(4096));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(0));
                driver.Wait();
            } finally {
                // A double dispose shouldn't cause a problem.
                if (buffer != null) buffer.Dispose();
                cts.Dispose();
            }
        }

        [Test]
        [Repeat(10)]
        public void WaitForWriteBufferDriver()
        {
            using (MemoryWriteBuffer buffer = new MemoryWriteBuffer(4096)) {
                Task user = new TaskFactory().StartNew(() => {
                    Thread.Sleep(50);
                    buffer.Write(new byte[10], 0, 10);
                });

                Assert.That(buffer.BufferNotEmpty.WaitOne(), Is.True);
                Assert.That(buffer.IsBufferNotEmpty, Is.True);
                Assert.That(buffer.BytesToWrite, Is.EqualTo(10));
                user.Wait();
            }
        }

        [Test]
        public void WaitForWriteToEmpty()
        {
            using (MemoryWriteBuffer buffer = new MemoryWriteBuffer(4096)) {
                Assert.That(buffer.IsBufferNotEmpty, Is.False);
                Assert.That(buffer.BytesToWrite, Is.EqualTo(0));
                Assert.That(buffer.WaitForWrite(1, 0), Is.True);
                Assert.That(buffer.WaitForWrite(1, 1), Is.True);

                buffer.Write(new byte[100], 0, 100);
                Assert.That(buffer.IsBufferNotEmpty, Is.True);
                Assert.That(buffer.BytesFree, Is.EqualTo(4096 - 100));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(100));
                Assert.That(buffer.WaitForWrite(1, 0), Is.True);
                Assert.That(buffer.WaitForWrite(1, 1), Is.True);

                buffer.Write(new byte[3996], 0, 3996);
                Assert.That(buffer.IsBufferNotEmpty, Is.True);
                Assert.That(buffer.BytesFree, Is.EqualTo(0));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(4096));
                Assert.That(buffer.WaitForWrite(1, 0), Is.False);
                Assert.That(buffer.WaitForWrite(1, 1), Is.False);

                buffer.Consume(100);
                Assert.That(buffer.IsBufferNotEmpty, Is.True);
                Assert.That(buffer.BytesFree, Is.EqualTo(100));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(3996));
                Assert.That(buffer.WaitForWrite(1, 0), Is.True);
                Assert.That(buffer.WaitForWrite(1, 1), Is.True);

                buffer.Consume(3996);
                Assert.That(buffer.IsBufferNotEmpty, Is.False);
                Assert.That(buffer.BytesFree, Is.EqualTo(4096));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(0));
                Assert.That(buffer.WaitForWrite(1, 0), Is.True);
                Assert.That(buffer.WaitForWrite(1, 1), Is.True);
            }
        }

        [TestCase(WriteOverload.Normal, TestName = "WaitForWriteInvalidCount")]
        [TestCase(WriteOverload.Token, TestName = "WaitForWriteInvalidCountCancellationToken")]
        [TestCase(WriteOverload.TokenNone, TestName = "WaitForWriteInvalidCountCancellationTokenNone")]
        public void WaitForWriteInvalidCount(WriteOverload overload)
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (MemoryWriteBuffer buffer = new MemoryWriteBuffer(4096)) {
                Assert.That(() => {
                    buffer.WaitForWrite(overload, -1, Timeout.Infinite, cts);
                }, Throws.TypeOf<ArgumentOutOfRangeException>());
            }
        }

        [TestCase(WriteOverload.Normal, TestName = "WaitForWriteInvalidTimeout")]
        [TestCase(WriteOverload.Token, TestName = "WaitForWriteInvalidTimeoutCancellationToken")]
        [TestCase(WriteOverload.TokenNone, TestName = "WaitForWriteInvalidTimeoutCancellationTokenNone")]
        public void WaitForWriteInvalidTimeout(WriteOverload overload)
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (MemoryWriteBuffer buffer = new MemoryWriteBuffer(4096)) {
                Assert.That(() => {
                    buffer.WaitForWrite(overload, 1, -2, cts);
                }, Throws.TypeOf<ArgumentOutOfRangeException>());
            }
        }

        [TestCase(WriteOverload.Normal, TestName = "WaitForWriteCapacity")]
        [TestCase(WriteOverload.Token, TestName = "WaitForWriteCapacityCancellationToken")]
        [TestCase(WriteOverload.TokenNone, TestName = "WaitForWriteCapacityCancellationTokenNone")]
        public void WaitForWriteCapacity(WriteOverload overload)
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (MemoryWriteBuffer buffer = new MemoryWriteBuffer(4096)) {
                Assert.That(buffer.WaitForWrite(overload, 4097, Timeout.Infinite, cts), Is.False);
            }
        }

        [TestCase(WriteOverload.Normal, false, TestName = "WaitForWriteZeroCount")]
        [TestCase(WriteOverload.Token, false, TestName = "WaitForWriteZeroCountCancellationToken")]
        [TestCase(WriteOverload.TokenNone, false, TestName = "WaitForWriteZeroCountCancellationTokenNone")]
        [TestCase(WriteOverload.Normal, true, TestName = "WaitForWriteZeroCountFull")]
        [TestCase(WriteOverload.Token, true, TestName = "WaitForWriteZeroCountFullCancellationToken")]
        [TestCase(WriteOverload.TokenNone, true, TestName = "WaitForWriteZeroCountFullCancellationTokenNone")]
        public void WaitForWriteZeroCount(WriteOverload overload, bool full)
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (MemoryWriteBuffer buffer = new MemoryWriteBuffer(4096)) {
                if (full) buffer.Write(new byte[4096], 0, 4096);
                Assert.That(buffer.WaitForWrite(overload, 0, Timeout.Infinite, cts), Is.True);
            }
        }

        [TestCase(WriteOverload.Normal, TestName = "WaitForEmptyFromFull")]
        [TestCase(WriteOverload.Token, TestName = "WaitForEmptyFromFullCancellationToken")]
        [TestCase(WriteOverload.TokenNone, TestName = "WaitForEmptyFromFullCancellationTokenNone")]
        [Repeat(5)]
        public void WaitForEmptyFromFull(WriteOverload overload)
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (MemoryWriteBuffer buffer = new MemoryWriteBuffer(4096)) {
                buffer.Write(new byte[4096], 0, 4096);
                Assert.That(buffer.BytesFree, Is.EqualTo(0));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(4096));

                Task driver = new TaskFactory().StartNew(() => {
                    Thread.Sleep(50);
                    for (int i = 0; i < 2; i++) {
                        // Try to randomize the order in which things run to look for race conditions.
                        Random r1 = new Random();
                        if (r1.Next(2) == 0) Thread.Sleep(r1.Next(2));

                        lock (buffer.Lock) {
                            buffer.Consume(2048);
                        }
                    }
                });

                Assert.That(buffer.WaitForEmpty(overload, Timeout.Infinite, cts), Is.True);
                driver.Wait();
            }
        }

        [TestCase(WriteOverload.Normal, TestName = "WaitForEmpty")]
        [TestCase(WriteOverload.Token, TestName = "WaitForEmptyCancellationToken")]
        [TestCase(WriteOverload.TokenNone, TestName = "WaitForEmptyCancellationTokenNone")]
        public void WaitForEmpty(WriteOverload overload)
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (MemoryWriteBuffer buffer = new MemoryWriteBuffer(4096)) {
                Assert.That(buffer.BytesFree, Is.EqualTo(4096));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(0));
                Assert.That(buffer.WaitForEmpty(overload, Timeout.Infinite, cts), Is.True);
            }
        }

        [Test]
        public void WaitForEmptyCancelled()
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (MemoryWriteBuffer buffer = new MemoryWriteBuffer(4096)) {
                buffer.Write(new byte[2048], 0, 2048);
                Assert.That(buffer.BytesFree, Is.EqualTo(2048));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(2048));

                Task user = new TaskFactory().StartNew(() => {
                    Thread.Sleep(50);
                    cts.Cancel();
                });

                Assert.That(buffer.WaitForEmpty(Timeout.Infinite, cts.Token), Is.False);
                user.Wait();
            }
        }

        [Test]
        public void WaitForEmptyCancelledPrior()
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            using (MemoryWriteBuffer buffer = new MemoryWriteBuffer(4096)) {
                buffer.Write(new byte[2048], 0, 2048);
                Assert.That(buffer.BytesFree, Is.EqualTo(2048));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(2048));

                cts.Cancel();
                Assert.That(buffer.WaitForEmpty(Timeout.Infinite, cts.Token), Is.False);
            }
        }

        [Test]
        public void Purge()
        {
            using (MemoryWriteBuffer buffer = new MemoryWriteBuffer(4096)) {
                buffer.Write(new byte[2048], 0, 2048);
                Assert.That(buffer.BytesFree, Is.EqualTo(2048));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(2048));

                Assert.That(buffer.IsDeviceDead, Is.False);

                buffer.Purge();
                Assert.That(buffer.BytesFree, Is.EqualTo(4096));
                Assert.That(buffer.BytesToWrite, Is.EqualTo(0));
                Assert.That(buffer.IsDeviceDead, Is.False);
            }
        }

        [Test]
        public void Write()
        {
            Random r = new Random();
            byte[] data = new byte[256];
            r.NextBytes(data);

            using (MemoryWriteBuffer buffer = new MemoryWriteBuffer(4096)) {
                buffer.Write(data, 0, data.Length);
                Assert.That(buffer.BytesFree, Is.EqualTo(3840));

                // Read the data and check the contents
                byte[] result = new byte[256];
                Array.Copy(buffer.Buffer, buffer.BufferStart, result, 0, buffer.BufferReadLength);
                Assert.That(result, Is.EqualTo(data));
            }
        }

        [Test]
        public void WriteWrap()
        {
            Random r = new Random();
            byte[] data = new byte[256];
            r.NextBytes(data);

            using (MemoryWriteBuffer buffer = new MemoryWriteBuffer(4096)) {
                buffer.Write(new byte[3968], 0, 3968);            // Shift the offset to position 3968 of 4096
                buffer.Consume(128);                              // Make 256 bytes free
                buffer.Write(data, 0, data.Length);
                buffer.Consume(3840);

                // Read the data and check the contents
                byte[] result = new byte[256];

                Assert.That(buffer.BytesFree, Is.EqualTo(3840));
                Assert.That(buffer.BufferStart, Is.EqualTo(3968));
                Assert.That(buffer.BufferReadLength, Is.EqualTo(128));

                Array.Copy(buffer.Buffer, buffer.BufferStart, result, 0, buffer.BufferReadLength);
                buffer.Consume(buffer.BufferReadLength);
                Array.Copy(buffer.Buffer, buffer.BufferStart, result, 128, buffer.BufferReadLength);
                buffer.Consume(buffer.BufferReadLength);

                Assert.That(result, Is.EqualTo(data));
            }
        }

#if NETCOREAPP
        [Test]
        public void WriteSpan()
        {
            Random r = new Random();
            byte[] data = new byte[256];
            r.NextBytes(data);

            using (MemoryWriteBuffer buffer = new MemoryWriteBuffer(4096)) {
                Span<byte> span = data;
                buffer.Write(span);
                Assert.That(buffer.BytesFree, Is.EqualTo(3840));

                // Read the data and check the contents
                byte[] result = new byte[256];
                Array.Copy(buffer.Buffer, buffer.BufferStart, result, 0, buffer.BufferReadLength);
                Assert.That(result, Is.EqualTo(data));
            }
        }

        [Test]
        public void WriteWrapSpan()
        {
            Random r = new Random();
            byte[] data = new byte[256];
            r.NextBytes(data);

            using (MemoryWriteBuffer buffer = new MemoryWriteBuffer(4096)) {
                buffer.Write(new byte[3968], 0, 3968);            // Shift the offset to position 3968 of 4096
                buffer.Consume(128);                              // Make 256 bytes free
                Span<byte> span = data;
                buffer.Write(span);
                buffer.Consume(3840);

                // Read the data and check the contents
                byte[] result = new byte[256];

                Assert.That(buffer.BytesFree, Is.EqualTo(3840));
                Assert.That(buffer.BufferStart, Is.EqualTo(3968));
                Assert.That(buffer.BufferReadLength, Is.EqualTo(128));

                Array.Copy(buffer.Buffer, buffer.BufferStart, result, 0, buffer.BufferReadLength);
                buffer.Consume(buffer.BufferReadLength);
                Array.Copy(buffer.Buffer, buffer.BufferStart, result, 128, buffer.BufferReadLength);
                buffer.Consume(buffer.BufferReadLength);

                Assert.That(result, Is.EqualTo(data));
            }
        }

        [Test]
        public void WriteSpanRetrieveViaSpan()
        {
            Random r = new Random();
            byte[] data = new byte[256];
            r.NextBytes(data);

            using (MemoryWriteBuffer buffer = new MemoryWriteBuffer(4096)) {
                Span<byte> span = data;
                buffer.Write(span);
                Assert.That(buffer.BytesFree, Is.EqualTo(3840));

                // Read the data and check the contents
                byte[] result = new byte[256];
                buffer.BufferSpan.CopyTo(result.AsSpan());
                Assert.That(result, Is.EqualTo(data));
            }
        }

        [Test]
        public void WriteWrapSpanRetrieveViaSpan()
        {
            Random r = new Random();
            byte[] data = new byte[256];
            r.NextBytes(data);

            using (MemoryWriteBuffer buffer = new MemoryWriteBuffer(4096)) {
                buffer.Write(new byte[3968], 0, 3968);            // Shift the offset to position 3968 of 4096
                buffer.Consume(128);                              // Make 256 bytes free
                Span<byte> span = data;
                buffer.Write(span);
                buffer.Consume(3840);

                // Read the data and check the contents
                byte[] result = new byte[256];

                Assert.That(buffer.BytesFree, Is.EqualTo(3840));
                Assert.That(buffer.BufferStart, Is.EqualTo(3968));
                Assert.That(buffer.BufferReadLength, Is.EqualTo(128));

                buffer.BufferSpan.CopyTo(result.AsSpan());
                buffer.Consume(buffer.BufferReadLength);
                buffer.BufferSpan.CopyTo(result.AsSpan(128));
                buffer.Consume(buffer.BufferReadLength);

                Assert.That(result, Is.EqualTo(data));
            }
        }
#endif
    }
}
