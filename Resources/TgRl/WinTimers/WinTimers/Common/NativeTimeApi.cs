using System;
using System.Runtime.InteropServices;

namespace Common
{
    public static class NativeTimeApi
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct TIMECAPS
        {
            public uint wPeriodMin;
            public uint wPeriodMax;
        }

        public const uint TIME_PERIODIC = 1;
        public const uint WT_EXECUTEINTIMERTHREAD = 0x00000020;
        public const uint WT_EXECUTEDEFAULT = 0x00000000;

        public delegate void TimeProc(uint uID, uint uMsg, uint dwUser, uint dw1, uint dw2);
        public delegate void WaitOrTimerCallback(IntPtr lpParameter, bool TimerOrWaitFired);

        [DllImport("winmm.dll")]
        public static extern uint timeGetDevCaps(ref TIMECAPS timeCaps, uint sizeTimeCaps);

        [DllImport("winmm.dll")]
        public static extern uint timeBeginPeriod(uint uPeriod);

        [DllImport("winmm.dll")]
        public static extern uint timeEndPeriod(uint uPeriod);

        [DllImport("winmm.dll")]
        public static extern uint timeSetEvent(uint uDelay, uint uResolution, TimeProc lpTimeProc, IntPtr dwUser, uint fuEvent);

        [DllImport("winmm.dll")]
        public static extern uint timeKillEvent(uint uTimerID);

        [DllImport("kernel32.dll")]
        public static extern bool CreateTimerQueueTimer(
            ref IntPtr phNewTimer,
            IntPtr TimerQueue,
            WaitOrTimerCallback Callback,
            IntPtr Parameter,
            uint DueTime,
            uint Period,
            uint Flags);

        [DllImport("kernel32.dll")]
        public static extern bool DeleteTimerQueueTimer(IntPtr TimerQueue, IntPtr Timer, IntPtr CompletionEvent);

        public static uint BeginPeriod(int intervalMs)
        {
            var timeCaps = default(TIMECAPS);
            timeGetDevCaps(ref timeCaps, (uint)Marshal.SizeOf(timeCaps));

            var resolution = Math.Min(Math.Max(timeCaps.wPeriodMin, (uint)intervalMs), timeCaps.wPeriodMax);
            timeBeginPeriod(resolution);

            return resolution;
        }

        public static void EndPeriod(uint resolution)
        {
            timeEndPeriod(resolution);
        }
    }
}
