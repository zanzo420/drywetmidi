using Common;
using System;
using System.Diagnostics;

namespace InfiniteLoopTimer
{
    internal sealed class Timer : ITimer
    {
        private bool _running;

        public void Start(int intervalMs, Action callback)
        {
            var lastTime = 0L;
            var stopwatch = new Stopwatch();

            _running = true;
            stopwatch.Start();

            while (_running)
            {
                if (stopwatch.ElapsedMilliseconds - lastTime < intervalMs)
                    continue;

                callback();
                lastTime = stopwatch.ElapsedMilliseconds;
            }
        }

        public void Stop()
        {
            _running = false;
        }
    }
}
