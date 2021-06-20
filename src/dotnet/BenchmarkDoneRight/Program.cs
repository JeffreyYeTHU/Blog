using System;
using System.Text;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace BenchmarkDoneRight
{
    class Program
    {
        static void Main(string[] args)
        {
            // WrongWayToTiming();
            // SlightlyBetterWayToTiming();
            BenchmarkRunner.Run<MyStringContactor>();
        }

        private static void WrongWayToTiming()
        {
            var before = DateTime.UtcNow;
            DoWork();
            var after = DateTime.UtcNow;
            var timeTake = after - before;
            Console.WriteLine($"Compute Time take to DoWork using DateTime.UtcNow: {timeTake.TotalMilliseconds}ms");
        }

        private static void SlightlyBetterWayToTiming()
        {
            var stopWatch = Stopwatch.StartNew();
            stopWatch.Start();
            DoWork();
            stopWatch.Stop();
            Console.WriteLine($"Compute Time take to DoWork using StopWatch: {stopWatch.ElapsedMilliseconds}ms");
        }

        private static void DoWork()
        {
            string res = string.Empty;
            for (int i = 0; i < 10000; i++)
            {
                res += i;
            }
        }
    }

    public class MyStringContactor
    {
        private int count = 10000;

        [Benchmark]
        public string UsingPlus()
        {
            string res = string.Empty;
            for (int i = 0; i < count; i++)
            {
                res += i;
            }
            return res;
        }

        [Benchmark]
        public string UsingBuilder()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                sb.Append(i);
            }
            return sb.ToString();
        }
    }
}
