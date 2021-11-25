using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace TestApp
{
    class Program
    {
        private delegate void Callback();

        [DllImport("tgrl")]
        private static extern IntPtr CreateSession();

        [DllImport("tgrl")]
        private static extern IntPtr StartTimer(IntPtr sessionInfo, int ms, Callback callback);

        [DllImport("tgrl")]
        private static extern void StopTimer(IntPtr sessionInfo, IntPtr tickGeneratorInfo);

        private static Callback _callback;
        private static Stopwatch _stopwatch;
        private static List<long> _times;

        static void Main(string[] args)
        {
            _callback = CallbackImpl;

            _times = new List<long>((int)Math.Round(MeasurementDuration.TotalMilliseconds) + 1);
            _stopwatch = new Stopwatch();

            var si = CreateSession();
            var tgi = StartTimer(si, 1, _callback);
            _stopwatch.Start();

            Thread.Sleep(MeasurementDuration);

            StopTimer(si, tgi);
            _stopwatch.Stop();

            var deltas = new List<long>();
            var lastTime = 0L;

            foreach (var time in _times)
            {
                var delta = time - lastTime;
                deltas.Add(delta);

                lastTime = time;
            }

            var min = deltas.Min();
            var max = deltas.Max();
            var average = deltas.Average();

            var areaSize = 1 * GoodAreaPercents / 100;

            var result = new Result(
                _times.Count,
                _times[0],
                min,
                deltas.Count(d => d == min) / (double)deltas.Count * 100,
                max,
                deltas.Count(d => d == max) / (double)deltas.Count * 100,
                average,
                deltas.Count(d => d >= 1 - areaSize && d <= 1 + areaSize) / (double)deltas.Count * 100);

            Console.WriteLine($"Results on {result.TimesCount} times:");
            Console.WriteLine($"    first   = {result.First}");
            Console.WriteLine($"    min     = {result.Min} ({result.MinPercent:0.##} %)");
            Console.WriteLine($"    max     = {result.Max} ({result.MaxPercent:0.##} %)");
            Console.WriteLine($"    average = {result.Average:0.##}");
            Console.WriteLine($"    good    = {result.FivePercentsIntervalAreaPercent:0.##} %");
        }

        private static void CallbackImpl()
        {
            if (_stopwatch.IsRunning)
                _times.Add(_stopwatch.ElapsedMilliseconds);
        }

        private record Result(
            int TimesCount,
            long First,
            long Min,
            double MinPercent,
            long Max,
            double MaxPercent,
            double Average,
            double FivePercentsIntervalAreaPercent);

        private const double GoodAreaPercents = 10.0;
        private static readonly TimeSpan MeasurementDuration = TimeSpan.FromSeconds(30);
        private static readonly int[] IntervalsToCheck = { 1, 10, 100 };
    }
}
