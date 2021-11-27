using Common;
using System;

namespace SystemThreadingTimer
{
    internal sealed class Timer : ITimer
    {
        private System.Threading.Timer _timer;

        public void Start(int intervalMs, Action callback)
        {
            _timer = new System.Threading.Timer(_ => callback(), null, intervalMs, intervalMs);
        }

        public void Stop()
        {
            _timer.Dispose();
        }
    }
}
