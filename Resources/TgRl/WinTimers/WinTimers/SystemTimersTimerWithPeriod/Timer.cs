using Common;
using System;

namespace SystemTimersTimerWithPeriod
{
    internal sealed class Timer : ITimer
    {
        private System.Timers.Timer _timer;
        private uint _resolution;

        public void Start(int intervalMs, Action callback)
        {
            _timer = new System.Timers.Timer(intervalMs);
            _timer.Elapsed += (_, __) => callback();

            _resolution = NativeTimeApi.BeginPeriod(intervalMs);
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
            NativeTimeApi.EndPeriod(_resolution);
        }
    }
}
