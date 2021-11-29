using System;
using System.Runtime.InteropServices;
using Common;

namespace TestApp
{
    class Program
    {
        private delegate void Callback();

        [DllImport("tgrl")]
        private static extern IntPtr CreateSession();

        static void Main(string[] args)
        {
            var si = CreateSession();
            TimerChecker.Check(new Timer(si));
        }
    }
}
