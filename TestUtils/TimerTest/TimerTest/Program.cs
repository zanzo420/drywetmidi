using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Timer = System.Timers.Timer;

namespace TimerTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var interval = int.Parse(args[0]);

            var stopwatch = new Stopwatch();
            var times = new List<long>();
            
            var timer = new Timer(interval);
            timer.Elapsed += (_, _) => times.Add(stopwatch.ElapsedMilliseconds);

            stopwatch.Start();
            timer.Start();
            
            //
            
            SpinWait.SpinUntil(() => times.Count >= 1000);
            timer.Stop();
            stopwatch.Stop();
            
            //

            var time = times[0];
            var deltas = new List<long>();

            for (var i = 1; i < times.Count; i++)
            {
                var delta = times[i] - time;
                deltas.Add(delta);
                time = times[i];
            }
            
            //

            Console.WriteLine($"Statistics for {times.Count} measures by interval of {interval}ms:");
            Console.WriteLine();
            Console.WriteLine($"min = {deltas.Min()}");
            Console.WriteLine($"max = {deltas.Max()}");
            Console.WriteLine($"avg = {deltas.Average()}");
            Console.WriteLine();
            Console.WriteLine("Deltas clusters:");
            Console.WriteLine();
            
            var groupedDeltas = deltas
                .GroupBy(d => d)
                .OrderByDescending(g => g.Count())
                .ToArray();

            foreach (var g in groupedDeltas)
            {
                Console.WriteLine($"{g.Key.ToString().PadRight(3)} : {g.Count() / (double)deltas.Count * 100:0.#}");
            }
        }
    }
}