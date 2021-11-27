using Common;
using System;

namespace SystemThreadingTimerWithPeriod
{
    internal sealed class Timer : ITimer
    {
        private System.Threading.Timer _timer;
        private uint _resolution;

        public void Start(int intervalMs, Action callback)
        {
            _resolution = NativeTimeApi.BeginPeriod(intervalMs);
            _timer = new System.Threading.Timer(_ => callback(), null, intervalMs, intervalMs);
        }

        public void Stop()
        {
            _timer.Dispose();
            NativeTimeApi.EndPeriod(_resolution);
        }
    }
}
