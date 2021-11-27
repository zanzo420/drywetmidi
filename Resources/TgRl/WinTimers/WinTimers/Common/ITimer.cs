using System;
using System.Collections.Generic;

namespace Common
{
    public interface ITimer
    {
        void Start(int intervalMs, Action callback);

        void Stop();
    }
}
