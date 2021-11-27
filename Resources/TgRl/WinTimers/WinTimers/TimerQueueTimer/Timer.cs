using Common;
using System;

namespace TimerQueueTimer
{
    internal sealed class Timer : ITimer
    {
        private IntPtr _timer;
        private NativeTimeApi.WaitOrTimerCallback _waitOrTimerCallback;
        private Action _callback;

        public void Start(int intervalMs, Action callback)
        {
            _callback = callback;
            _waitOrTimerCallback = WaitOrTimerCallback;

            NativeTimeApi.CreateTimerQueueTimer(
                ref _timer,
                IntPtr.Zero,
                _waitOrTimerCallback,
                IntPtr.Zero,
                (uint)intervalMs,
                (uint)intervalMs,
                NativeTimeApi.WT_EXECUTEINTIMERTHREAD);
        }

        public void Stop()
        {
            NativeTimeApi.DeleteTimerQueueTimer(IntPtr.Zero, _timer, IntPtr.Zero);
        }

        private void WaitOrTimerCallback(IntPtr lpParameter, bool TimerOrWaitFired)
        {
            _callback();
        }
    }
}
