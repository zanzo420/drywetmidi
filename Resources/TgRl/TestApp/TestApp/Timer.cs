using Common;
using System;
using System.Runtime.InteropServices;

namespace TestApp
{
    internal sealed class Timer : ITimer
    {
        private delegate void Callback();

        [DllImport("tgrl")]
        private static extern IntPtr StartTimer(IntPtr sessionInfo, int ms, Callback callback);

        [DllImport("tgrl")]
        private static extern void StopTimer(IntPtr sessionInfo, IntPtr tickGeneratorInfo);

        private IntPtr _sessionInfo;
        private Callback _callback;
        private Action _timerCallback;
        private IntPtr _timerInfo;

        public Timer(IntPtr sessionInfo)
        {
            _sessionInfo = sessionInfo;
        }

        public void Start(int intervalMs, Action callback)
        {
            _timerCallback = callback;
            _callback = CallbackImpl;

            _timerInfo = StartTimer(_sessionInfo, intervalMs, _callback);
        }

        public void Stop()
        {
            StopTimer(_sessionInfo, _timerInfo);
        }

        private void CallbackImpl()
        {
            _timerCallback();
        }
    }
}
