namespace RJCP.IO.Timer
{
    using System.Diagnostics;
    using System.Threading;

    /// <summary>
    /// A class to maintain how much time is remaining since the last reset, until expiry.
    /// </summary>
    /// <remarks>
    /// This class is useful when implementing time outs in other methods. It can provide the remaining time, in units
    /// of milliseconds, that can be used with many Operating System calls as an expiry time.
    /// <para>
    /// One example is the <see cref="WaitHandle.WaitOne()"/> method which expects a time out parameter. Either
    /// instantiate the <see cref="TimerExpiry"/> class at the beginning immediately before its use, or call the
    /// <see cref="Reset"/> method at the beginning of the time out operation. Then on return of the function, if no
    /// other operation occurred, the method <see cref="RemainingTime"/> should return 0 indicating that the timer has
    /// expired.
    /// </para>
    /// <para>
    /// Another thread can be programmed to <see cref="Reset"/> the timer class during a time out operation, so that
    /// even if the result of Wait operation by the Operating system resulted in a time out, a <see cref="Reset"/>,
    /// which results in the <see cref="RemainingTime"/> being more than 0 milliseconds, indicates that another wait
    /// operation should occur.
    /// </para>
    /// <para>
    /// Even if no expiry is to occur, but the Operating System function returns early, you can opt to restart the time
    /// out operation which will then take into account the current time and reduce the time out so that the operation
    /// ends as expected.
    /// </para>
    /// <para>
    /// As an example, say you need to wait for data by calling a method which waits for the first set of data within a
    /// time out. But your method must wait for at least two elements of data within the time out. This can be
    /// implemented as follows:
    /// </para>
    /// <example>
    /// <code lang="csharp">
    /// <![CDATA[
    /// public bool MyFunc(int timeOut) {
    ///     TimerExpiry myExpiry = new TimerExpiry(timeOut);
    ///     int elements = 0;
    ///     do {
    ///         elements += GetData(myExpiry.RemainingTime());
    ///     } while (elements < 2 && !myExpiry.Expired);
    ///
    ///     if (elements >= 2)
    ///         return true;
    ///     return false;
    /// }
    /// ]]>
    /// </code>
    /// </example>
    /// </remarks>
    public sealed class TimerExpiry
    {
        private readonly Stopwatch m_StopWatch = new Stopwatch();
        private int m_Milliseconds;

        /// <summary>
        /// Constructor. Initialise expiry based on the current time.
        /// </summary>
        /// <param name="milliseconds">The initial time out in milliseconds.</param>
        /// <remarks>
        /// The constructor sets the initial time out that should be used. On construction of the new object, the timer
        /// is automatically started.
        /// </remarks>
        public TimerExpiry(int milliseconds)
        {
            Timeout = milliseconds;
        }

        /// <summary>
        /// The time for expiry on the next reset. <see cref="Timeout.Infinite"/> indicates no expiry.
        /// </summary>
        public int Timeout
        {
            get { return m_Milliseconds; }
            set
            {
                m_StopWatch.Reset();
                if (value < 0) {
                    m_Milliseconds = System.Threading.Timeout.Infinite;
                } else {
                    m_Milliseconds = value;
                    if (value > 0) m_StopWatch.Start();
                }
            }
        }

        /// <summary>
        /// Estimate the amount of time (ms) remaining from when this function is called until expiry.
        /// </summary>
        /// <returns>The time to expiry in milliseconds.</returns>
        public int RemainingTime()
        {
            if (m_Milliseconds < 0) return System.Threading.Timeout.Infinite;

            long elapsed = m_StopWatch.ElapsedMilliseconds;
            if (elapsed >= m_Milliseconds) return 0;
            return (int)(m_Milliseconds - elapsed);
        }

        /// <summary>
        /// Test if the timer expiry has expired.
        /// </summary>
        public bool Expired
        {
            get { return RemainingTime() == 0; }
        }

        /// <summary>
        /// Reset the time out so it occurs with the given <see cref="Timeout"/>.
        /// </summary>
        public void Reset()
        {
            if (m_Milliseconds > 0) {
                m_StopWatch.Reset();
                m_StopWatch.Start();
            }
        }
    }
}
