using System.Diagnostics;

namespace AsyncProgramming_console
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var sw = new Stopwatch();
            sw.Start();
            await AsyncMethod();
            sw.Stop();
            Console.WriteLine($"AsyncMethod Elapsed:\t{sw.Elapsed}");

            sw.Restart();
            await NotAsyncMethod();
            sw.Stop();
            Console.WriteLine($"NotAsyncMethod Elapsed:\t{sw.Elapsed}");
        }

        private static async Task AsyncMethod()
        {
            var list = new List<Task>
            {
                Task.Delay(1000),
                Task.Delay(1000),
                Task.Run(() => Thread.Sleep(1000)),
            };
            await Task.WhenAll(list);
        }

        // async Taskになっているが非同期に動作していない関数
        private static async Task NotAsyncMethod()
        {
            await Task.Delay(1000);
            await Task.Delay(1000);
            await Task.Run(() => Thread.Sleep(1000));
        }

    }
}