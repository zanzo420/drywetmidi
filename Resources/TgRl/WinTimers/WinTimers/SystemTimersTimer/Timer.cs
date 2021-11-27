using Common;
using System;

namespace SystemTimersTimer
{
    internal sealed class Timer : ITimer
    {
        private System.Timers.Timer _timer;

        public void Start(int intervalMs, Action callback)
        {
            _timer = new System.Timers.Timer(intervalMs);
            _timer.Elapsed += (_, __) => callback();
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }
    }
}
