using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace TestApp
{
    class Program
    {
        private delegate void Callback();

        [DllImport("tgrl")]
        private static extern void CreateTimer(Callback callback);

        private static Callback _callback;

        static void Main(string[] args)
        {
            _callback = CallbackImpl;
            CreateTimer(_callback);

            Thread.Sleep(10000);
        }

        private static void CallbackImpl()
        {
            Console.WriteLine("AAA");
        }
    }
}
