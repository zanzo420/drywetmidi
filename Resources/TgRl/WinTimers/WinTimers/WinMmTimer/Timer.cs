using Common;
using System;

namespace WinMmTimer
{
    internal sealed class Timer : ITimer
    {
        private uint _resolution;
        private NativeTimeApi.TimeProc _timeProc;
        private Action _callback;
        private uint _timerId;

        public void Start(int intervalMs, Action callback)
        {
            _callback = callback;

            _resolution = NativeTimeApi.BeginPeriod(intervalMs);
            _timeProc = TimeProc;
            _timerId = NativeTimeApi.timeSetEvent((uint)intervalMs, _resolution, _timeProc, IntPtr.Zero, NativeTimeApi.TIME_PERIODIC);
        }

        public void Stop()
        {
            NativeTimeApi.timeKillEvent(_timerId);
            NativeTimeApi.EndPeriod(_resolution);
        }

        private void TimeProc(uint uID, uint uMsg, uint dwUser, uint dw1, uint dw2)
        {
            _callback();
        }
    }
}
