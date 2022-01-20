using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            long Max,
            double Average,
            double GoodPercent,
            double AboveGood,
            double? AverageAboveGood);

        private static readonly TimeSpan MeasurementDuration = TimeSpan.FromMinutes(3);
        private static readonly int[] IntervalsToCheck = { 1, 10, 100 };

        private const int GoodAreaSize = 5;

        public static void Check(ITimer timer)
        {
            Console.WriteLine("Starting measuring...");
            Console.WriteLine($"OS: {Environment.OSVersion}");
            Console.WriteLine("--------------------------------");

            foreach (var intervalMs in IntervalsToCheck)
            {
                Console.WriteLine($"Measuring interval of {intervalMs} ms...");
                
                var result = CheckInterval(timer, intervalMs);

                Console.WriteLine($"Results on {result.TimesCount} times:");
                Console.WriteLine($"    first      = {result.First}");
                Console.WriteLine($"    min/max    = {result.Min}/{result.Max}");
                Console.WriteLine($"    average    = {result.Average:0.##}");
                Console.WriteLine($"    good       = {result.GoodPercent:0.##} %");
                Console.WriteLine($"    above good = {result.AboveGood:0.##} % (average {result.AverageAboveGood:0.##})");
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

            foreach (var time in times.ToArray())
            {
                var delta = time - lastTime;
                deltas.Add(delta);
                lastTime = time;
            }

            File.WriteAllLines($"deltas_{intervalMs}.txt", deltas.Select(d => d.ToString()));

            var min = deltas.Min();
            var max = deltas.Max();
            var average = deltas.Average();

            double GetPercent(Func<long, bool> selector) =>
                deltas.Count(selector) / (double)deltas.Count * 100;

            var goodPercent = GetPercent(d => d >= intervalMs - GoodAreaSize && d <= intervalMs + GoodAreaSize);
            var aboveGoodPercent = GetPercent(d => d > intervalMs + GoodAreaSize);
            var aboveGood = deltas.Where(d => d > intervalMs + GoodAreaSize).ToArray();
            var averageAboveGood = aboveGood.Any() ? (double?)aboveGood.Average() : null;

            return new Result(
                times.Count,
                times[0],
                min,
                max,
                average,
                goodPercent,
                aboveGoodPercent,
                averageAboveGood);
        }
    }
}
