﻿namespace RJCP.IO.Timer
{
    using System;
    using System.Threading;
    using NUnit.Framework;

    [TestFixture]
    public class TimerExpiryTest
    {
        [Test]
        [CancelAfter(1000)]
        public void TimerExpiry_Basic()
        {
            TimerExpiry te = new(200);
            int t = te.RemainingTime();
            Assert.That(t, Is.GreaterThan(140), $"Timer is less than 140ms (should be close to 200ms), remaining={t}");

            int[] rea = new int[1000];
            int c = 0;

            // Note, you should put your delays in loops like this one, as the OS doesn't guarantee that you will
            // actually wait this long. Normally, this kind of loop is exactly the type you want, as you wait for
            // another event to occur, and if it doesn't occur, you keep waiting until the timeout is zero.
            //
            // On some systems, a signal may cause a timeout to abort early also.
            int re = te.RemainingTime();
            do {
                rea[c++] = re;
                Thread.Sleep(re);
                re = te.RemainingTime();
            } while (re > 0);
            Assert.That(te.RemainingTime(), Is.EqualTo(0));
            Assert.That(te.Expired, Is.True);

            for (int i = 0; i < c; i++) {
                Console.WriteLine("Wait {0}: {1}", i, rea[i]);
            }

            // Ensure that resetting will reenact the timer.
            te.Reset();
            Assert.That(te.Expired, Is.False);
            Assert.That(t, Is.GreaterThan(140));
        }

        [Test]
        [CancelAfter(5000)]
        public void TimerExpiry_Reset()
        {
            TimerExpiry te = new(1000);
            Thread.Sleep(250);
            Assert.That(te.RemainingTime(), Is.GreaterThan(600));
            te.Reset();

            Thread.Sleep(250);
            Assert.That(te.RemainingTime(), Is.GreaterThan(600));
            te.Reset();

            Thread.Sleep(250);
            Assert.That(te.RemainingTime(), Is.GreaterThan(600));
            te.Reset();

            Thread.Sleep(250);
            Assert.That(te.RemainingTime(), Is.GreaterThan(600));
            te.Reset();

            Thread.Sleep(250);
            Assert.That(te.RemainingTime(), Is.GreaterThan(600));
            te.Reset();

            Thread.Sleep(250);
            Assert.That(te.RemainingTime(), Is.GreaterThan(600));
        }

        [Test]
        [CancelAfter(5000)]
        public void TimerExpiry_Reset2()
        {
            TimerExpiry te = new(1000);
            Thread.Sleep(250);
            Assert.That(te.RemainingTime(), Is.GreaterThan(600));
            Thread.Sleep(250);
            Assert.That(te.RemainingTime(), Is.LessThan(550));

            te.Reset();
            Thread.Sleep(250);
            Assert.That(te.RemainingTime(), Is.GreaterThan(600));
            Thread.Sleep(250);
            Assert.That(te.RemainingTime(), Is.LessThan(550));
        }

        [Test]
        [CancelAfter(2000)]
        public void TimerExpiry_Negative()
        {
            TimerExpiry te = new(-1);
            Assert.That(te.RemainingTime(), Is.EqualTo(Timeout.Infinite));
            Thread.Sleep(100);
            Assert.That(te.RemainingTime(), Is.EqualTo(Timeout.Infinite));

            TimerExpiry te2 = new(100);
            Assert.That(te2.RemainingTime(), Is.GreaterThan(0));
            te2.Timeout = -1;
            Assert.That(te.RemainingTime(), Is.EqualTo(Timeout.Infinite));
            Thread.Sleep(300);
            Assert.That(te.RemainingTime(), Is.EqualTo(Timeout.Infinite));
        }

        [Test]
        public void TimerExpiry_Zero()
        {
            TimerExpiry te = new(0);
            Assert.That(te.Expired, Is.True);
            Assert.That(te.RemainingTime(), Is.EqualTo(0));
        }

        [Test]
        public void TimerExpiry_InvalidValue()
        {
            Assert.That(() => { _ = new TimerExpiry(-2); },
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TimerExpiry_TimeoutInvalidValue()
        {
            TimerExpiry te = new(200);
            Assert.That(te.Expired, Is.False);
            Assert.That(te.Timeout, Is.EqualTo(200));
            Assert.That(te.RemainingTime(), Is.GreaterThan(0).And.LessThanOrEqualTo(200));

            Assert.That(() => { te.Timeout = -2; },
                Throws.TypeOf<ArgumentOutOfRangeException>());

            Assert.That(te.Expired, Is.False);
            Assert.That(te.Timeout, Is.EqualTo(200));
            Assert.That(te.RemainingTime(), Is.GreaterThan(0).And.LessThanOrEqualTo(200));
        }

        [Test]
        public void TimerExpiry_LargeTimeout()
        {
            TimerExpiry te = new(0x7FFFFFFF);
            Assert.That(te.Expired, Is.False);
            Assert.That(te.Timeout, Is.EqualTo(0x7FFFFFFF));
            Assert.That(te.RemainingTime(), Is.GreaterThan(0x7FFFFFFF - 100).And.LessThanOrEqualTo(0x7FFFFFFF));

            int start = Environment.TickCount;
            Thread.Sleep(100);
            int end = Environment.TickCount;
            int elapsed = unchecked(end - start);
            Assert.That(te.Expired, Is.False);
            Assert.That(te.Timeout, Is.EqualTo(0x7FFFFFFF));
            Assert.That(te.RemainingTime(), Is.GreaterThan(0x7FFFFFFF - 200).And.LessThanOrEqualTo(0x7FFFFFFF - elapsed));

            Console.WriteLine($"Elapsed {elapsed}, TimerExpiry {unchecked(te.Timeout - te.RemainingTime())}");
        }
    }
}
