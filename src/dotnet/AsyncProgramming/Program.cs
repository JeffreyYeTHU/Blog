using System.Threading;
using System.Diagnostics;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System;


namespace AsyncProgramming
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            //await RunTaskParallel();
            //await RunTaskFakeParallel();
            try
            {
                TaskThrowException();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine("Exception handled!");
            }
            return 0;
        }

        private static async Task RunTaskParallel()
        {
            var stopWatch = Stopwatch.StartNew();
            stopWatch.Start();

            var task1 = FirstTaskAsync();  // start task 1
            var task2 = SecondTaskAsync();  // start task 2
            var task3 = ThirdTaskAsync();  // start task 3
            await Task.WhenAll(task1, task2, task3);  // wait tasks to be down
            Console.WriteLine("All tasks down!");

            stopWatch.Stop();
            Console.WriteLine($"Time take in RunTaskParallel: {stopWatch.ElapsedMilliseconds / 1000}s");
        }

        private static async Task RunTaskFakeParallel()
        {
            var stopWatch = Stopwatch.StartNew();
            stopWatch.Start();

            await FirstTaskAsync();
            await SecondTaskAsync();
            await ThirdTaskAsync();
            Console.WriteLine("All tasks down!");

            stopWatch.Stop();
            Console.WriteLine($"Time take in RunTaskFakeParallel: {stopWatch.ElapsedMilliseconds / 1000}s");
        }

        static async Task WorkHaveNoReturnAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        static async Task<int> WorkHaveReturnAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            return 0;
        }

        static int WorkHaveReturn()
        {
            Thread.Sleep(TimeSpan.FromSeconds(1));
            return 0;
        }

        static async Task FirstTaskAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        static async Task SecondTaskAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        static async Task ThirdTaskAsync()
        {
            await Task.Delay(TimeSpan.FromSeconds(3));
        }

        static async Task TaskThrowException()
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            throw new Exception("Something is wrong");
        }

        private async Task MethodCanBeCanceled(CancellationToken token = default)
        {
            for (int i = 0; i < 1000; i++)
            {
                Thread.Sleep(100); // simulate cpu bound work
                if (i % 100 == 0)  // control pooling frequency
                    token.ThrowIfCancellationRequested();
            }
        }

        private async Task MethodCanBeCanceledAsync(CancellationToken token = default)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), token);
        }
    }
}