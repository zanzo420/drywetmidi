using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Common
{
    public static class TimerChecker
    {
        private record Result(
            int TimesCount,
            long First,
            long Min,
            double MinPercent,
            long Max,
            double MaxPercent,
            double Average,
            double GoodPercent,
            double AboveGood,
            double BelowGood);

        private const double GoodAreaPercents = 10.0;
        private static readonly TimeSpan MeasurementDuration = TimeSpan.FromSeconds(30);
        private static readonly int[] IntervalsToCheck = { 1, 10, 100 };

        public static void Check(ITimer timer)
        {
            foreach (var intervalMs in IntervalsToCheck)
            {
                Console.WriteLine($"Measuring interval of {intervalMs} ms...");
                
                var result = CheckInterval(timer, intervalMs);

                Console.WriteLine($"Results on {result.TimesCount} times:");
                Console.WriteLine($"    first      = {result.First}");
                Console.WriteLine($"    min        = {result.Min} ({result.MinPercent:0.##} %)");
                Console.WriteLine($"    max        = {result.Max} ({result.MaxPercent:0.##} %)");
                Console.WriteLine($"    average    = {result.Average:0.##}");
                Console.WriteLine($"    good       = {result.GoodPercent:0.##} %");
                Console.WriteLine($"    above good = {result.AboveGood:0.##} %");
                Console.WriteLine($"    below good = {result.BelowGood:0.##} %");
            }

            Console.WriteLine("All done.");
        }

        private static Result CheckInterval(ITimer timer, int intervalMs)
        {
            var times = new List<long>((int)Math.Round(MeasurementDuration.TotalMilliseconds) + 1);
            var stopwatch = new Stopwatch();
            Action callback = () => times.Add(stopwatch.ElapsedMilliseconds);

            timer.Start(intervalMs, callback);
            stopwatch.Start();

            Thread.Sleep(MeasurementDuration);

            timer.Stop();
            stopwatch.Stop();

            var deltas = new List<long>();
            var lastTime = 0L;

            foreach (var time in times)
            {
                var delta = time - lastTime;
                deltas.Add(delta);

                lastTime = time;
            }

            var min = deltas.Min();
            var max = deltas.Max();
            var average = deltas.Average();

            var areaSize = intervalMs * GoodAreaPercents / 100;

            double GetPercent(Func<long, bool> selector) =>
                deltas.Count(selector) / (double)deltas.Count * 100;

            var minPercent = GetPercent(d => d == min);
            var maxPercent = GetPercent(d => d == max);
            var goodPercent = GetPercent(d => d >= intervalMs - areaSize && d <= intervalMs + areaSize);
            var aboveGoodPercent = GetPercent(d => d > intervalMs + areaSize);
            var belowGoodPercent = GetPercent(d => d < intervalMs - areaSize);

            return new Result(
                times.Count,
                times[0],
                min,
                minPercent,
                max,
                maxPercent,
                average,
                goodPercent,
                aboveGoodPercent,
                belowGoodPercent);
        }
    }
}
