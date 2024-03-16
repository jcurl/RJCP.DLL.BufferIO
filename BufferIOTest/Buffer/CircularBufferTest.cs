namespace RJCP.IO.Buffer
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class CircularBufferTest
    {
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(int.MinValue)]
        public void CircularBuffer_InvalidCapacity(int capacity)
        {
            Assert.That(() => { _ = new CircularBuffer<byte>(capacity); }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void CircularBuffer_NullArray()
        {
            Assert.That(() => { _ = new CircularBuffer<byte>(null); }, Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void CircularBuffer_EmptyArray()
        {
            Assert.That(() => { _ = new CircularBuffer<byte>(new byte[0]); }, Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void CircularBuffer_NullArrayCount()
        {
            Assert.That(() => { _ = new CircularBuffer<byte>(null, 0); }, Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void CircularBuffer_EmptyArrayCount()
        {
            Assert.That(() => { _ = new CircularBuffer<byte>(new byte[0], 1); }, Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void CircularBuffer_CountOutOfBounds()
        {
            Assert.That(() => { _ = new CircularBuffer<byte>(new byte[10], 11); }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void CircularBuffer_NullArrayCountOffset()
        {
            Assert.That(() => { _ = new CircularBuffer<byte>(null, 0, 0); }, Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void CircularBuffer_EmptyArrayCountOffset()
        {
            Assert.That(() => { _ = new CircularBuffer<byte>(new byte[0], 0, 0); }, Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void CircularBuffer_CountOutOfBoundsCountWithOffset()
        {
            Assert.That(() => { _ = new CircularBuffer<byte>(new byte[10], 0, 11); }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void CircularBuffer_CountOutOfBoundsOffset()
        {
            Assert.That(() => { _ = new CircularBuffer<byte>(new byte[10], 11, 0); }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void CircularBuffer_ProduceConsume()
        {
            CircularBuffer<byte> cb = new(50);
            Assert.That(cb.Capacity, Is.EqualTo(50));

            // Initial state
            Assert.That(cb.Start, Is.EqualTo(0));
            Assert.That(cb.Length, Is.EqualTo(0));
            Assert.That(cb.ReadLength, Is.EqualTo(0));
            Assert.That(cb.End, Is.EqualTo(0));
            Assert.That(cb.Free, Is.EqualTo(50));
            Assert.That(cb.WriteLength, Is.EqualTo(50));

            // Test 1: Allocate 50 bytes
            cb.Produce(50);
            Assert.That(cb.Start, Is.EqualTo(0));
            Assert.That(cb.Length, Is.EqualTo(50));
            Assert.That(cb.ReadLength, Is.EqualTo(50));
            Assert.That(cb.End, Is.EqualTo(0));
            Assert.That(cb.Free, Is.EqualTo(0));
            Assert.That(cb.WriteLength, Is.EqualTo(0));

            // Test 2: Free 50 bytes
            cb.Consume(50);
            Assert.That(cb.Start, Is.EqualTo(0));
            Assert.That(cb.Length, Is.EqualTo(0));
            Assert.That(cb.ReadLength, Is.EqualTo(0));
            Assert.That(cb.End, Is.EqualTo(0));
            Assert.That(cb.Free, Is.EqualTo(50));
            Assert.That(cb.WriteLength, Is.EqualTo(50));

            // Test 3: Allocate 25 bytes
            cb.Produce(25);
            Assert.That(cb.Start, Is.EqualTo(0));
            Assert.That(cb.Length, Is.EqualTo(25));
            Assert.That(cb.ReadLength, Is.EqualTo(25));
            Assert.That(cb.End, Is.EqualTo(25));
            Assert.That(cb.Free, Is.EqualTo(25));
            Assert.That(cb.WriteLength, Is.EqualTo(25));

            // Test 4: Free 24 bytes
            cb.Consume(24);
            Assert.That(cb.Start, Is.EqualTo(24));
            Assert.That(cb.Length, Is.EqualTo(1));
            Assert.That(cb.ReadLength, Is.EqualTo(1));
            Assert.That(cb.End, Is.EqualTo(25));
            Assert.That(cb.Free, Is.EqualTo(49));
            Assert.That(cb.WriteLength, Is.EqualTo(25));

            // Test 5: Allocate 49 bytes
            cb.Produce(49);
            Assert.That(cb.Start, Is.EqualTo(24));
            Assert.That(cb.Length, Is.EqualTo(50));
            Assert.That(cb.ReadLength, Is.EqualTo(26));
            Assert.That(cb.End, Is.EqualTo(24));
            Assert.That(cb.Free, Is.EqualTo(0));
            Assert.That(cb.WriteLength, Is.EqualTo(0));

            // Test 6: Reset
            cb.Reset();
            Assert.That(cb.Start, Is.EqualTo(0));
            Assert.That(cb.Length, Is.EqualTo(0));
            Assert.That(cb.ReadLength, Is.EqualTo(0));
            Assert.That(cb.End, Is.EqualTo(0));
            Assert.That(cb.Free, Is.EqualTo(50));
            Assert.That(cb.WriteLength, Is.EqualTo(50));

            // Test 7: Test full wrapping around
            cb.Produce(25);
            cb.Consume(25);
            cb.Produce(50);
            Assert.That(cb.Start, Is.EqualTo(25));
            Assert.That(cb.Length, Is.EqualTo(50));
            Assert.That(cb.ReadLength, Is.EqualTo(25));
            Assert.That(cb.End, Is.EqualTo(25));
            Assert.That(cb.Free, Is.EqualTo(0));
            Assert.That(cb.WriteLength, Is.EqualTo(0));

            // Test 8: Free all data
            cb.Consume(50);
            Assert.That(cb.Start, Is.EqualTo(25));
            Assert.That(cb.Length, Is.EqualTo(0));
            Assert.That(cb.ReadLength, Is.EqualTo(0));
            Assert.That(cb.End, Is.EqualTo(25));
            Assert.That(cb.Free, Is.EqualTo(50));
            Assert.That(cb.WriteLength, Is.EqualTo(25));
        }

        [Test]
        public void CircularBuffer_Indexing()
        {
            CircularBuffer<byte> cb = new(50);

            // Write into the array directly
            for (int i = 0; i < cb.Array.Length; i++) {
                cb.Array[i] = (byte)i;
            }
            cb.Produce(50);
            Assert.That(cb.Length, Is.EqualTo(50));
            Assert.That(cb.Start, Is.EqualTo(0));

            // Access the array using the indexer
            for (int i = 0; i < cb.Length; i++) {
                Assert.That(cb[i], Is.EqualTo(i));
            }

            cb.Consume(25);
            cb.Produce(25);

            // Now the start is in the middle
            Assert.That(cb.Start, Is.EqualTo(25));
            Assert.That(cb.Length, Is.EqualTo(50));
            for (int i = 0; i < cb.Length; i++) {
                Assert.That(cb[i], Is.EqualTo((i + 25) % 50), $"Index {i}");
            }

            for (int i = 0; i < cb.Length; i++) {
                Assert.That(cb.Array[cb.ToArrayIndex(i)], Is.EqualTo((i + 25) % 50), $"Index {i}");
            }
        }

        [Test]
        public void CircularBuffer_ReadBlock()
        {
            CircularBuffer<byte> cb = new(50);

            // Move the pointer to the middle
            cb.Produce(25);
            cb.Consume(25);

            // Now allocate all space
            cb.Produce(25);
            cb.Produce(25);

            Assert.That(cb.ReadLength, Is.EqualTo(25));
            Assert.That(cb.GetReadBlock(0), Is.EqualTo(25));
            Assert.That(cb.GetReadBlock(5), Is.EqualTo(20));
            Assert.That(cb.GetReadBlock(24), Is.EqualTo(1));
            Assert.That(cb.GetReadBlock(25), Is.EqualTo(25));
            Assert.That(cb.GetReadBlock(30), Is.EqualTo(20));
            Assert.That(cb.GetReadBlock(49), Is.EqualTo(1));
            Assert.That(cb.GetReadBlock(50), Is.EqualTo(0));
        }

        [Test]
        public void CircularBuffer_Revert()
        {
            CircularBuffer<byte> cb = new(50);

            // Move the pointer to the middle
            cb.Produce(25);
            cb.Consume(25);

            // Now allocate all space
            cb.Produce(25);
            cb.Produce(25);

            Assert.That(cb.Start, Is.EqualTo(25));
            Assert.That(cb.ReadLength, Is.EqualTo(25));
            Assert.That(cb.End, Is.EqualTo(25));
            Assert.That(cb.WriteLength, Is.EqualTo(0));

            cb.Revert(5);
            Assert.That(cb.Start, Is.EqualTo(25));
            Assert.That(cb.ReadLength, Is.EqualTo(25));
            Assert.That(cb.End, Is.EqualTo(20));
            Assert.That(cb.WriteLength, Is.EqualTo(5));

            cb.Revert(20);
            Assert.That(cb.Start, Is.EqualTo(25));
            Assert.That(cb.ReadLength, Is.EqualTo(25));
            Assert.That(cb.End, Is.EqualTo(0));
            Assert.That(cb.WriteLength, Is.EqualTo(25));

            cb.Revert(20);
            Assert.That(cb.Start, Is.EqualTo(25));
            Assert.That(cb.ReadLength, Is.EqualTo(5));
            Assert.That(cb.End, Is.EqualTo(30));
            Assert.That(cb.WriteLength, Is.EqualTo(20));
        }

        [Test]
        public void CircularBuffer_ReadWrite()
        {
            CircularBuffer<byte> cb = new(50);
            cb.Produce(25);
            cb.Consume(25);
            cb.Produce(50);

            byte[] rd = new byte[50];
            Random r = new();
            r.NextBytes(rd);

            for (int i = 0; i < rd.Length; i++) {
                cb[i] = rd[i];
            }

            for (int i = 0; i < rd.Length; i++) {
                Assert.That(cb[i], Is.EqualTo(rd[i]), $"Index {i} doesn't match");
            }
        }

        [Test]
        public void CircularBuffer_ConstructorArray()
        {
            byte[] m = {
                0x80, 0x00, 0x2F,
                0x11, 0x40, 0x2B, 0x00, 0xCD, 0xC0, 0x27, 0x90,
                0x22, 0x30, 0x02, 0x00, 0x02, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x94, 0x00, 0x6D, 0x6F, 0x73, 0x74,
                0x20, 0x74, 0x65, 0x72, 0x6D, 0x69, 0x6E, 0x61,
                0x6C, 0x20, 0x73, 0x74, 0x61, 0x74, 0x75, 0x73,
                0x20, 0x3D, 0x20, 0x31, 0x30, 0x29, 0x00,
                0xA6, 0x73 };

            CircularBuffer<byte> cb1 = new(m);
            Assert.That(cb1.Length, Is.EqualTo(m.Length));
            Assert.That(cb1.Free, Is.EqualTo(0));
            Assert.That(cb1.Start, Is.EqualTo(0));
            Assert.That(cb1[0], Is.EqualTo(0x80));

            CircularBuffer<byte> cb2 = new(m, 10);
            Assert.That(cb2.Length, Is.EqualTo(10));
            Assert.That(cb2.Free, Is.EqualTo(m.Length - 10));
            Assert.That(cb2.Start, Is.EqualTo(0));
            Assert.That(cb2[0], Is.EqualTo(0x80));

            CircularBuffer<byte> cb3 = new(m, m.Length);
            Assert.That(cb3.Length, Is.EqualTo(m.Length));
            Assert.That(cb3.Free, Is.EqualTo(0));
            Assert.That(cb3.Start, Is.EqualTo(0));
            Assert.That(cb3[0], Is.EqualTo(0x80));

            CircularBuffer<byte> cb4 = new(m, 15, 10);
            Assert.That(cb4.Length, Is.EqualTo(10));
            Assert.That(cb4.Free, Is.EqualTo(m.Length - 10));
            Assert.That(cb4.Start, Is.EqualTo(15));
            Assert.That(cb4[0], Is.EqualTo(0x02));
        }

        [Test]
        public void CircularBuffer_SubstringSimple()
        {
            byte[] m = {
                0x80, 0x00, 0x2F,
                0x11, 0x40, 0x2B, 0x00, 0xCD, 0xC0, 0x27, 0x90,
                0x22, 0x30, 0x02, 0x00, 0x02, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x94, 0x00, 0x6D, 0x6F, 0x73, 0x74,
                0x20, 0x74, 0x65, 0x72, 0x6D, 0x69, 0x6E, 0x61,
                0x6C, 0x20, 0x73, 0x74, 0x61, 0x74, 0x75, 0x73,
                0x20, 0x3D, 0x20, 0x31, 0x30, 0x29, 0x00,
                0xA6, 0x76 };

            CircularBuffer<byte> cb1 = new(m);
            Assert.That(cb1.Substring(0x00), Is.EqualTo(1));
            Assert.That(cb1.Substring(0x2F), Is.EqualTo(2));
            Assert.That(cb1.Substring(0x80), Is.EqualTo(0));
            Assert.That(cb1.Substring(0xFF), Is.EqualTo(-1));
            Assert.That(cb1.Substring(0x76), Is.EqualTo(51));

            cb1.Consume(3);
            Assert.That(cb1.Substring(0x00), Is.EqualTo(3));
            Assert.That(cb1.Substring(0x2F), Is.EqualTo(-1));
            Assert.That(cb1.Substring(0x80), Is.EqualTo(-1));
            Assert.That(cb1.Substring(0xFF), Is.EqualTo(-1));

            cb1.Append(0xFE);
            Assert.That(cb1.Substring(0x00), Is.EqualTo(3));
            Assert.That(cb1.Substring(0xFE), Is.EqualTo(49));
            Assert.That(cb1[49], Is.EqualTo(0xFE));
            Assert.That(cb1.Substring(0x80), Is.EqualTo(-1));
            Assert.That(cb1.Substring(0xFF), Is.EqualTo(-1));
            Assert.That(cb1.Substring(0x76), Is.EqualTo(48));
        }

        [Test]
        public void CircularBuffer_SubstringWrap()
        {
            byte[] m = {
               0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
               0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
               0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F,
               0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F,
            };

            CircularBuffer<byte> cb1 = new(m);
            cb1.Consume(0x20);
            cb1.Append(new byte[] { 0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47 });
            Assert.That(cb1.Substring(0x20), Is.EqualTo(0));
            Assert.That(cb1.Substring(0x30), Is.EqualTo(16));
            Assert.That(cb1.Substring(0x30, 0x10), Is.EqualTo(16));
            Assert.That(cb1.Substring(0x40, 0x10), Is.EqualTo(32));
            Assert.That(cb1.Substring(0x44, 0x10), Is.EqualTo(36));
            Assert.That(cb1.Substring(0x44, 0x20), Is.EqualTo(36));
            Assert.That(cb1.Substring(0x40, 0x20), Is.EqualTo(32));
        }

        [Test]
        public void CircularBuffer_SubstringNegativeOffset()
        {
            byte[] m = {
                0x80, 0x00, 0x2F,
                0x11, 0x40, 0x2B, 0x00, 0xCD, 0xC0, 0x27, 0x90,
                0x22, 0x30, 0x02, 0x00, 0x02, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x94, 0x00, 0x6D, 0x6F, 0x73, 0x74,
                0x20, 0x74, 0x65, 0x72, 0x6D, 0x69, 0x6E, 0x61,
                0x6C, 0x20, 0x73, 0x74, 0x61, 0x74, 0x75, 0x73,
                0x20, 0x3D, 0x20, 0x31, 0x30, 0x29, 0x00,
                0xA6, 0x76 };

            CircularBuffer<byte> cb1 = new(m);

            Assert.That(() => { _ = cb1.Substring(0x00, -1); }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void CircularBuffer_SubstringOutsideOfBounds()
        {
            byte[] m = {
                0x80, 0x00, 0x2F,
                0x11, 0x40, 0x2B, 0x00, 0xCD, 0xC0, 0x27, 0x90,
                0x22, 0x30, 0x02, 0x00, 0x02, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x94, 0x00, 0x6D, 0x6F, 0x73, 0x74,
                0x20, 0x74, 0x65, 0x72, 0x6D, 0x69, 0x6E, 0x61,
                0x6C, 0x20, 0x73, 0x74, 0x61, 0x74, 0x75, 0x73,
                0x20, 0x3D, 0x20, 0x31, 0x30, 0x29, 0x00,
                0xA6, 0x76 };

            CircularBuffer<byte> cb1 = new(m);

            // Note, in .NET, the string.SubString will also work for x.SubString(x.Length) and return empty.
            Assert.That(() => { cb1.Substring(0x00, m.Length + 1); }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void CircularBuffer_SubstringLastElement()
        {
            byte[] m = {
                0x80, 0x00, 0x2F,
                0x11, 0x40, 0x2B, 0x00, 0xCD, 0xC0, 0x27, 0x90,
                0x22, 0x30, 0x02, 0x00, 0x02, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x94, 0x00, 0x6D, 0x6F, 0x73, 0x74,
                0x20, 0x74, 0x65, 0x72, 0x6D, 0x69, 0x6E, 0x61,
                0x6C, 0x20, 0x73, 0x74, 0x61, 0x74, 0x75, 0x73,
                0x20, 0x3D, 0x20, 0x31, 0x30, 0x29, 0x00,
                0xA6, 0x76 };

            CircularBuffer<byte> cb1 = new(m);

            // Note, in .NET, the string.SubString will also work for x.SubString(x.Length) and return empty.
            Assert.That(cb1.Substring(0x76, m.Length), Is.EqualTo(-1));
        }

        [Test]
        public void CircularBuffer_SubstringOffset()
        {
            byte[] m = {
                0x80, 0x00, 0x2F,
                0x11, 0x40, 0x2B, 0x00, 0xCD, 0xC0, 0x27, 0x90,
                0x22, 0x30, 0x02, 0x00, 0x02, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x94, 0x00, 0x6D, 0x6F, 0x73, 0x74,
                0x20, 0x74, 0x65, 0x72, 0x6D, 0x69, 0x6E, 0x61,
                0x6C, 0x20, 0x73, 0x74, 0x61, 0x74, 0x75, 0x73,
                0x20, 0x3D, 0x20, 0x31, 0x30, 0x29, 0x00,
                0xA6, 0x76 };

            CircularBuffer<byte> cb1 = new(m);
            Assert.That(cb1.Substring(0x00, 1), Is.EqualTo(1));
            Assert.That(cb1.Substring(0x00, 2), Is.EqualTo(6));
            Assert.That(cb1.Substring(0x2F, 2), Is.EqualTo(2));
            Assert.That(cb1.Substring(0x2F, 3), Is.EqualTo(-1));
        }

        [Test]
        public void CircularBuffer_SubstringMultiple()
        {
            byte[] m = {
                0x41, 0x42, 0x43, 0x0d, 0x0a, 0x44, 0x45, 0x46, 0x0d, 0x0a
            };

            CircularBuffer<byte> cb = new(m);
            Assert.That(cb.Substring(new byte[] { 0x0A, 0x0D }), Is.EqualTo(3));
            cb.Consume(1);
            Assert.That(cb.Substring(new byte[] { 0x0A, 0x0D }), Is.EqualTo(2));
            cb.Consume(1);
            Assert.That(cb.Substring(new byte[] { 0x0A, 0x0D }), Is.EqualTo(1));
            cb.Consume(1);
            Assert.That(cb.Substring(new byte[] { 0x0A, 0x0D }), Is.EqualTo(0));
            cb.Consume(1);
            Assert.That(cb.Substring(new byte[] { 0x0A, 0x0D }), Is.EqualTo(0));
            cb.Consume(1);
            Assert.That(cb.Substring(new byte[] { 0x0A, 0x0D }), Is.EqualTo(3));
        }

        [Test]
        public void CircularBuffer_SubstringMultipleOffset()
        {
            byte[] m = {
                0x41, 0x42, 0x43, 0x0d, 0x0a, 0x44, 0x45, 0x46, 0x0d, 0x0a
            };

            CircularBuffer<byte> cb = new(m);
            Assert.That(cb.Substring(new byte[] { 0x0A, 0x0D }, 0), Is.EqualTo(3));
            Assert.That(cb.Substring(new byte[] { 0x0A, 0x0D }, 1), Is.EqualTo(3));
            Assert.That(cb.Substring(new byte[] { 0x0A, 0x0D }, 2), Is.EqualTo(3));
            Assert.That(cb.Substring(new byte[] { 0x0A, 0x0D }, 3), Is.EqualTo(3));
            Assert.That(cb.Substring(new byte[] { 0x0A, 0x0D }, 4), Is.EqualTo(4));
            Assert.That(cb.Substring(new byte[] { 0x0A, 0x0D }, 5), Is.EqualTo(8));
            Assert.That(cb.Substring(new byte[] { 0x0A, 0x0D }, 6), Is.EqualTo(8));
            Assert.That(cb.Substring(new byte[] { 0x0A, 0x0D }, 7), Is.EqualTo(8));
            Assert.That(cb.Substring(new byte[] { 0x0A, 0x0D }, 8), Is.EqualTo(8));
            Assert.That(cb.Substring(new byte[] { 0x0A, 0x0D }, 9), Is.EqualTo(9));
        }

        [Test]
        public void CircularBuffer_SubstringMultipleNegativeOffset()
        {
            byte[] m = {
                0x80, 0x00, 0x2F,
                0x11, 0x40, 0x2B, 0x00, 0xCD, 0xC0, 0x27, 0x90,
                0x22, 0x30, 0x02, 0x00, 0x02, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x94, 0x00, 0x6D, 0x6F, 0x73, 0x74,
                0x20, 0x74, 0x65, 0x72, 0x6D, 0x69, 0x6E, 0x61,
                0x6C, 0x20, 0x73, 0x74, 0x61, 0x74, 0x75, 0x73,
                0x20, 0x3D, 0x20, 0x31, 0x30, 0x29, 0x00,
                0xA6, 0x76 };

            CircularBuffer<byte> cb1 = new(m);

            Assert.That(() => { cb1.Substring(new byte[] { 0x00, 0x02 }, -1); }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void CircularBuffer_SubstringMultipleOutsideOfBounds()
        {
            byte[] m = {
                0x80, 0x00, 0x2F,
                0x11, 0x40, 0x2B, 0x00, 0xCD, 0xC0, 0x27, 0x90,
                0x22, 0x30, 0x02, 0x00, 0x02, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x94, 0x00, 0x6D, 0x6F, 0x73, 0x74,
                0x20, 0x74, 0x65, 0x72, 0x6D, 0x69, 0x6E, 0x61,
                0x6C, 0x20, 0x73, 0x74, 0x61, 0x74, 0x75, 0x73,
                0x20, 0x3D, 0x20, 0x31, 0x30, 0x29, 0x00,
                0xA6, 0x76 };

            CircularBuffer<byte> cb1 = new(m);

            // Note, in .NET, the string.SubString will also work for x.SubString(x.Length) and return empty.
            Assert.That(() => { cb1.Substring(new byte[] { 0x00, 0x02 }, m.Length + 1); }, Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void CircularBuffer_SubstringMultipleLastElement()
        {
            byte[] m = {
                0x80, 0x00, 0x2F,
                0x11, 0x40, 0x2B, 0x00, 0xCD, 0xC0, 0x27, 0x90,
                0x22, 0x30, 0x02, 0x00, 0x02, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x94, 0x00, 0x6D, 0x6F, 0x73, 0x74,
                0x20, 0x74, 0x65, 0x72, 0x6D, 0x69, 0x6E, 0x61,
                0x6C, 0x20, 0x73, 0x74, 0x61, 0x74, 0x75, 0x73,
                0x20, 0x3D, 0x20, 0x31, 0x30, 0x29, 0x00,
                0xA6, 0x76 };

            CircularBuffer<byte> cb1 = new(m);

            // Note, in .NET, the string.SubString will also work for x.SubString(x.Length) and return empty.
            Assert.That(cb1.Substring(new byte[] { 0xA6, 0x76 }, m.Length), Is.EqualTo(-1));
        }
    }
}
