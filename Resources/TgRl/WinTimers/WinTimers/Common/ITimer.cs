using System;

namespace Common
{
    public interface ITimer
    {
        void Start(int intervalMs, Action callback);

        void Stop();
    }
}
