namespace RJCP.IO.Buffer
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class CircularBufferCopyTest
    {
        [TestCase(false, TestName = "MoveToZeroOffsetPartial")]
        [TestCase(true, TestName = "CopyToZeroOffsetPartial")]
        public void CopyToZeroOffsetPartial(bool copy)
        {
            byte[] m = {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            };

            CircularBuffer<byte> cb = new(m);

            byte[] result = new byte[8];
            if (copy) {
                Assert.That(cb.CopyTo(result, 0, result.Length), Is.EqualTo(result.Length));
                Assert.That(cb.Length, Is.EqualTo(m.Length));
            } else {
                Assert.That(cb.MoveTo(result, 0, result.Length), Is.EqualTo(result.Length));
                Assert.That(cb.Length, Is.EqualTo(m.Length - result.Length));
            }
            Assert.That(result, Is.EqualTo(new byte[] { 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48 }));
        }

        [TestCase(false, TestName = "MoveToZeroOffsetFull")]
        [TestCase(true, TestName = "CopyToZeroOffsetFull")]
        public void CopyToZeroOffsetFull(bool copy)
        {
            byte[] m = {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            };

            CircularBuffer<byte> cb = new(m);

            byte[] result = new byte[m.Length];
            if (copy) {
                Assert.That(cb.CopyTo(result, 0, result.Length), Is.EqualTo(result.Length));
                Assert.That(cb.Length, Is.EqualTo(m.Length));
            } else {
                Assert.That(cb.MoveTo(result, 0, result.Length), Is.EqualTo(result.Length));
                Assert.That(cb.Length, Is.EqualTo(m.Length - result.Length));
            }
            Assert.That(result, Is.EqualTo(m));
        }

        [TestCase(false, TestName = "MoveToNonZeroOffsetNoWrap")]
        [TestCase(true, TestName = "CopyToNonZeroOffsetNoWrap")]
        public void CopyToNonZeroOffsetNoWrap(bool copy)
        {
            byte[] m = {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            };

            CircularBuffer<byte> cb = new(m, 8, m.Length);

            byte[] result = new byte[8];
            if (copy) {
                Assert.That(cb.CopyTo(result, 0, result.Length), Is.EqualTo(result.Length));
                Assert.That(cb.Length, Is.EqualTo(m.Length));
            } else {
                Assert.That(cb.MoveTo(result, 0, result.Length), Is.EqualTo(result.Length));
                Assert.That(cb.Length, Is.EqualTo(m.Length - result.Length));
            }
            Assert.That(result, Is.EqualTo(new byte[] { 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50 }));
        }

        [TestCase(false, TestName = "MoveToNonZeroOffsetWrap")]
        [TestCase(true, TestName = "CopyToNonZeroOffsetWrap")]
        public void CopyToNonZeroOffsetWrap(bool copy)
        {
            byte[] m = {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            };

            CircularBuffer<byte> cb = new(m, 16, m.Length);

            byte[] result = new byte[12];
            if (copy) {
                Assert.That(cb.CopyTo(result, 0, result.Length), Is.EqualTo(result.Length));
                Assert.That(cb.Length, Is.EqualTo(m.Length));
            } else {
                Assert.That(cb.MoveTo(result, 0, result.Length), Is.EqualTo(result.Length));
                Assert.That(cb.Length, Is.EqualTo(m.Length - result.Length));
            }
            Assert.That(result, Is.EqualTo(new byte[] { 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x41, 0x42, 0x43, 0x44 }));
        }

        [TestCase(false, TestName = "MoveToNonZeroOffsetFull")]
        [TestCase(true, TestName = "CopyToNonZeroOffsetFull")]
        public void CopyToNonZeroOffsetFull(bool copy)
        {
            byte[] m = {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            };

            CircularBuffer<byte> cb = new(m, 16, m.Length);

            byte[] result = new byte[m.Length];
            if (copy) {
                Assert.That(cb.CopyTo(result, 0, result.Length), Is.EqualTo(result.Length));
                Assert.That(cb.Length, Is.EqualTo(m.Length));
            } else {
                Assert.That(cb.MoveTo(result, 0, result.Length), Is.EqualTo(result.Length));
                Assert.That(cb.Length, Is.EqualTo(m.Length - result.Length));
            }
            Assert.That(result, Is.EqualTo(new byte[] {
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50
            }));
        }

        [TestCase(false, TestName = "MoveToLargeWrap")]
        [TestCase(true, TestName = "CopyToLargeWrap")]
        public void CopyToLargeWrap(bool copy)
        {
            byte[] m = {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            };

            CircularBuffer<byte> cb = new(m, 16, m.Length);

            byte[] result = new byte[m.Length + 1];
            if (copy) {
                Assert.That(cb.CopyTo(result, 0, result.Length), Is.EqualTo(m.Length));
                Assert.That(cb.Length, Is.EqualTo(m.Length));
            } else {
                Assert.That(cb.MoveTo(result, 0, result.Length), Is.EqualTo(m.Length));
                Assert.That(cb.Length, Is.EqualTo(0));
            }
            Assert.That(result, Is.EqualTo(new byte[] {
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0
            }));
        }

        [TestCase(false, TestName = "MoveToLargeWrap")]
        [TestCase(true, TestName = "CopyToLargeWrap")]
        public void CopyToLargeNoWrap(bool copy)
        {
            byte[] m = {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            };

            CircularBuffer<byte> cb = new(m, 0, m.Length);

            byte[] result = new byte[m.Length + 1];
            if (copy) {
                Assert.That(cb.CopyTo(result, 0, result.Length), Is.EqualTo(m.Length));
                Assert.That(cb.Length, Is.EqualTo(m.Length));
            } else {
                Assert.That(cb.MoveTo(result, 0, result.Length), Is.EqualTo(m.Length));
                Assert.That(cb.Length, Is.EqualTo(0));
            }
            Assert.That(result, Is.EqualTo(new byte[] {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
                0
            }));
        }

        [TestCase(false, TestName = "MoveToSubSetNoWrap")]
        [TestCase(true, TestName = "CopyToSubSetNoWrap")]
        public void CopyToSubSetNoWrap(bool copy)
        {
            byte[] m = {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            };

            CircularBuffer<byte> cb = new(m, 8, 8);

            byte[] result = new byte[8];
            if (copy) {
                Assert.That(cb.CopyTo(result, 0, result.Length), Is.EqualTo(result.Length));
                Assert.That(cb.Length, Is.EqualTo(8));
            } else {
                Assert.That(cb.MoveTo(result, 0, result.Length), Is.EqualTo(result.Length));
                Assert.That(cb.Length, Is.EqualTo(0));
            }
            Assert.That(result, Is.EqualTo(new byte[] {
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50
            }));
        }

        [TestCase(false, TestName = "MoveToSubSetWrap")]
        [TestCase(true, TestName = "CopyToSubSetWrap")]
        public void CopyToSubSetWrap(bool copy)
        {
            byte[] m = {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            };

            CircularBuffer<byte> cb = new(m, 20, 8);

            byte[] result = new byte[8];
            if (copy) {
                Assert.That(cb.CopyTo(result, 0, result.Length), Is.EqualTo(result.Length));
                Assert.That(cb.Length, Is.EqualTo(8));
            } else {
                Assert.That(cb.MoveTo(result, 0, result.Length), Is.EqualTo(result.Length));
                Assert.That(cb.Length, Is.EqualTo(0));
            }
            Assert.That(result, Is.EqualTo(new byte[] {
                0x55, 0x56, 0x57, 0x58, 0x41, 0x42, 0x43, 0x44
            }));
        }

        [Test]
        public void AppendFull()
        {
            CircularBuffer<byte> cb = new(24);

            byte[] m = {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            };
            Assert.That(cb.Append(m, 0, m.Length), Is.EqualTo(m.Length));
            Assert.That(cb.Length, Is.EqualTo(m.Length));

            byte[] r = new byte[m.Length];
            Assert.That(cb.CopyTo(r, 0, r.Length), Is.EqualTo(m.Length));
            Assert.That(r, Is.EqualTo(m));
        }

        [Test]
        public void AppendFullWrap()
        {
            CircularBuffer<byte> cb = new(new byte[24], 12, 0);

            byte[] m = {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            };
            Assert.That(cb.Append(m, 0, m.Length), Is.EqualTo(m.Length));
            Assert.That(cb.Length, Is.EqualTo(m.Length));

            byte[] r = new byte[m.Length];
            Assert.That(cb.CopyTo(r, 0, r.Length), Is.EqualTo(m.Length));
            Assert.That(r, Is.EqualTo(new byte[] {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            }));
        }

        [Test]
        public void Append()
        {
            CircularBuffer<byte> cb = new(new byte[24], 12, 0);

            byte[] m = {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            };
            Assert.That(cb.Append(m, 0, 8), Is.EqualTo(8));
            Assert.That(cb.Length, Is.EqualTo(8));

            byte[] r = new byte[8];
            Assert.That(cb.CopyTo(r, 0, r.Length), Is.EqualTo(r.Length));
            Assert.That(r, Is.EqualTo(new byte[] {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
            }));
        }

        [Test]
        public void AppendWrap()
        {
            CircularBuffer<byte> cb = new(new byte[24], 12, 0);

            byte[] m = {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            };
            Assert.That(cb.Append(m, 0, 16), Is.EqualTo(16));
            Assert.That(cb.Length, Is.EqualTo(16));

            byte[] r = new byte[16];
            Assert.That(cb.CopyTo(r, 0, r.Length), Is.EqualTo(r.Length));
            Assert.That(r, Is.EqualTo(new byte[] {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
            }));
        }

        [Test]
        public void AppendOverflow()
        {
            byte[] s = {
                0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
                0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F, 0x70,
                0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78,
            };
            CircularBuffer<byte> cb = new(s, 12, 8);

            byte[] m = {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            };
            Assert.That(cb.Append(m, 0, 24), Is.EqualTo(16));
            Assert.That(cb.Length, Is.EqualTo(24));

            byte[] r = new byte[24];
            Assert.That(cb.CopyTo(r, 0, r.Length), Is.EqualTo(r.Length));
            Assert.That(r, Is.EqualTo(new byte[] {
                0x6D, 0x6E, 0x6F, 0x70, 0x71, 0x72, 0x73, 0x74,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
            }));
        }

#if NET6_0_OR_GREATER
        [TestCase(false, TestName = "MoveToSpanZeroOffsetPartial")]
        [TestCase(true, TestName = "CopyToSpanZeroOffsetPartial")]
        public void CopyToSpanZeroOffsetPartial(bool copy)
        {
            byte[] m = {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            };

            CircularBuffer<byte> cb = new(m);

            byte[] result = new byte[8];
            Span<byte> span = new(result);
            if (copy) {
                Assert.That(cb.CopyTo(span), Is.EqualTo(result.Length));
                Assert.That(cb.Length, Is.EqualTo(m.Length));
            } else {
                Assert.That(cb.MoveTo(span), Is.EqualTo(result.Length));
                Assert.That(cb.Length, Is.EqualTo(m.Length - result.Length));
            }
            Assert.That(result, Is.EqualTo(new byte[] { 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48 }));
        }

        [TestCase(false, TestName = "MoveToSpanZeroOffsetFull")]
        [TestCase(true, TestName = "CopyToSpanZeroOffsetFull")]
        public void CopyToSpanZeroOffsetFull(bool copy)
        {
            byte[] m = {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            };

            CircularBuffer<byte> cb = new(m);

            byte[] result = new byte[m.Length];
            Span<byte> span = new(result);
            if (copy) {
                Assert.That(cb.CopyTo(span), Is.EqualTo(result.Length));
                Assert.That(cb.Length, Is.EqualTo(m.Length));
            } else {
                Assert.That(cb.MoveTo(span), Is.EqualTo(result.Length));
                Assert.That(cb.Length, Is.EqualTo(m.Length - result.Length));
            }
            Assert.That(result, Is.EqualTo(m));
        }

        [TestCase(false, TestName = "MoveToSpanNonZeroOffsetNoWrap")]
        [TestCase(true, TestName = "CopyToSpanNonZeroOffsetNoWrap")]
        public void CopyToSpanNonZeroOffsetNoWrap(bool copy)
        {
            byte[] m = {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            };

            CircularBuffer<byte> cb = new(m, 8, m.Length);

            byte[] result = new byte[8];
            Span<byte> span = new(result);
            if (copy) {
                Assert.That(cb.CopyTo(span), Is.EqualTo(result.Length));
                Assert.That(cb.Length, Is.EqualTo(m.Length));
            } else {
                Assert.That(cb.MoveTo(span), Is.EqualTo(result.Length));
                Assert.That(cb.Length, Is.EqualTo(m.Length - result.Length));
            }
            Assert.That(result, Is.EqualTo(new byte[] { 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50 }));
        }

        [TestCase(false, TestName = "MoveToSpanNonZeroOffsetWrap")]
        [TestCase(true, TestName = "CopyToSpanNonZeroOffsetWrap")]
        public void CopyToSpanNonZeroOffsetWrap(bool copy)
        {
            byte[] m = {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            };

            CircularBuffer<byte> cb = new(m, 16, m.Length);

            byte[] result = new byte[12];
            Span<byte> span = new(result);
            if (copy) {
                Assert.That(cb.CopyTo(span), Is.EqualTo(result.Length));
                Assert.That(cb.Length, Is.EqualTo(m.Length));
            } else {
                Assert.That(cb.MoveTo(span), Is.EqualTo(result.Length));
                Assert.That(cb.Length, Is.EqualTo(m.Length - result.Length));
            }
            Assert.That(result, Is.EqualTo(new byte[] { 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x41, 0x42, 0x43, 0x44 }));
        }

        [TestCase(false, TestName = "MoveToSpanNonZeroOffsetFull")]
        [TestCase(true, TestName = "CopyToSpanNonZeroOffsetFull")]
        public void CopyToSpanNonZeroOffsetFull(bool copy)
        {
            byte[] m = {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            };

            CircularBuffer<byte> cb = new(m, 16, m.Length);

            byte[] result = new byte[m.Length];
            Span<byte> span = new(result);
            if (copy) {
                Assert.That(cb.CopyTo(span), Is.EqualTo(result.Length));
                Assert.That(cb.Length, Is.EqualTo(m.Length));
            } else {
                Assert.That(cb.MoveTo(span), Is.EqualTo(result.Length));
                Assert.That(cb.Length, Is.EqualTo(m.Length - result.Length));
            }
            Assert.That(result, Is.EqualTo(new byte[] {
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50
            }));
        }

        [TestCase(false, TestName = "MoveToSpanLargeWrap")]
        [TestCase(true, TestName = "CopyToSpanLargeWrap")]
        public void CopyToSpanLargeWrap(bool copy)
        {
            byte[] m = {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            };

            CircularBuffer<byte> cb = new(m, 16, m.Length);

            byte[] result = new byte[m.Length + 1];
            Span<byte> span = new(result);
            if (copy) {
                Assert.That(cb.CopyTo(span), Is.EqualTo(m.Length));
                Assert.That(cb.Length, Is.EqualTo(m.Length));
            } else {
                Assert.That(cb.MoveTo(span), Is.EqualTo(m.Length));
                Assert.That(cb.Length, Is.EqualTo(0));
            }
            Assert.That(result, Is.EqualTo(new byte[] {
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0
            }));
        }

        [TestCase(false, TestName = "MoveToSpanLargeNoWrap")]
        [TestCase(true, TestName = "CopyToSpanLargeNoWrap")]
        public void CopyToSpanLargeNoWrap(bool copy)
        {
            byte[] m = {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            };

            CircularBuffer<byte> cb = new(m, 0, m.Length);

            byte[] result = new byte[m.Length + 1];
            Span<byte> span = new(result);
            if (copy) {
                Assert.That(cb.CopyTo(span), Is.EqualTo(m.Length));
                Assert.That(cb.Length, Is.EqualTo(m.Length));
            } else {
                Assert.That(cb.MoveTo(span), Is.EqualTo(m.Length));
                Assert.That(cb.Length, Is.EqualTo(0));
            }
            Assert.That(result, Is.EqualTo(new byte[] {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
                0
            }));
        }

        [TestCase(false, TestName = "MoveToSpanSubSetNoWrap")]
        [TestCase(true, TestName = "CopyToSpanSubSetNoWrap")]
        public void CopyToSpanSubSetNoWrap(bool copy)
        {
            byte[] m = {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            };

            CircularBuffer<byte> cb = new(m, 8, 8);

            byte[] result = new byte[8];
            Span<byte> span = new(result);
            if (copy) {
                Assert.That(cb.CopyTo(span), Is.EqualTo(result.Length));
                Assert.That(cb.Length, Is.EqualTo(8));
            } else {
                Assert.That(cb.MoveTo(span), Is.EqualTo(result.Length));
                Assert.That(cb.Length, Is.EqualTo(0));
            }
            Assert.That(result, Is.EqualTo(new byte[] {
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50
            }));
        }

        [TestCase(false, TestName = "MoveToSpanSubSetWrap")]
        [TestCase(true, TestName = "CopyToSpanSubSetWrap")]
        public void CopyToSpanSubSetWrap(bool copy)
        {
            byte[] m = {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            };

            CircularBuffer<byte> cb = new(m, 20, 8);

            byte[] result = new byte[8];
            Span<byte> span = new(result);
            if (copy) {
                Assert.That(cb.CopyTo(span), Is.EqualTo(result.Length));
                Assert.That(cb.Length, Is.EqualTo(8));
            } else {
                Assert.That(cb.MoveTo(span), Is.EqualTo(result.Length));
                Assert.That(cb.Length, Is.EqualTo(0));
            }
            Assert.That(result, Is.EqualTo(new byte[] {
                0x55, 0x56, 0x57, 0x58, 0x41, 0x42, 0x43, 0x44
            }));
        }

        [Test]
        public void AppendSpanFull()
        {
            CircularBuffer<byte> cb = new(24);

            byte[] m = {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            };
            Span<byte> span = new(m);
            Assert.That(cb.Append(span), Is.EqualTo(m.Length));
            Assert.That(cb.Length, Is.EqualTo(m.Length));

            byte[] r = new byte[m.Length];
            Assert.That(cb.CopyTo(r, 0, r.Length), Is.EqualTo(m.Length));
            Assert.That(r, Is.EqualTo(m));
        }

        [Test]
        public void AppendSpanFullWrap()
        {
            CircularBuffer<byte> cb = new(new byte[24], 12, 0);

            byte[] m = {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            };
            Span<byte> span = new(m);
            Assert.That(cb.Append(span), Is.EqualTo(m.Length));
            Assert.That(cb.Length, Is.EqualTo(m.Length));

            byte[] r = new byte[m.Length];
            Assert.That(cb.CopyTo(r, 0, r.Length), Is.EqualTo(m.Length));
            Assert.That(r, Is.EqualTo(new byte[] {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            }));
        }

        [Test]
        public void AppendSpan()
        {
            CircularBuffer<byte> cb = new(new byte[24], 12, 0);

            byte[] m = {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            };
            Span<byte> span = new(m);
            Assert.That(cb.Append(span[0..8]), Is.EqualTo(8));
            Assert.That(cb.Length, Is.EqualTo(8));

            byte[] r = new byte[8];
            Assert.That(cb.CopyTo(r, 0, r.Length), Is.EqualTo(r.Length));
            Assert.That(r, Is.EqualTo(new byte[] {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
            }));
        }

        [Test]
        public void AppendSpanWrap()
        {
            CircularBuffer<byte> cb = new(new byte[24], 12, 0);

            byte[] m = {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            };
            Span<byte> span = new(m);
            Assert.That(cb.Append(span[0..16]), Is.EqualTo(16));
            Assert.That(cb.Length, Is.EqualTo(16));

            byte[] r = new byte[16];
            Assert.That(cb.CopyTo(r, 0, r.Length), Is.EqualTo(r.Length));
            Assert.That(r, Is.EqualTo(new byte[] {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
            }));
        }

        [Test]
        public void AppendSpanOverflow()
        {
            byte[] s = {
                0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68,
                0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F, 0x70,
                0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78,
            };
            CircularBuffer<byte> cb = new(s, 12, 8);

            byte[] m = {
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
                0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58,
            };
            Span<byte> span = new(m);
            Assert.That(cb.Append(span), Is.EqualTo(16));
            Assert.That(cb.Length, Is.EqualTo(24));

            byte[] r = new byte[24];
            Assert.That(cb.CopyTo(r, 0, r.Length), Is.EqualTo(r.Length));
            Assert.That(r, Is.EqualTo(new byte[] {
                0x6D, 0x6E, 0x6F, 0x70, 0x71, 0x72, 0x73, 0x74,
                0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48,
                0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F, 0x50,
            }));
        }
#endif
    }
}
