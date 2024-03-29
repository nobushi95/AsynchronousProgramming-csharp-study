// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DeadLock
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
        }

        private void ThreadCheck_Click(object sender, RoutedEventArgs e)
        {
            // 1. s1への代入はUIスレッドで実行される
            // 2. s2への代入はワーカスレッドで実行される
            //    UIスレッドはTask(ワーカスレッドで実行されるs2の代入)の終了をWaitする
            //    ワーカースレッドでの処理が終了すると、UIスレッドのロックを開放（Taskが終了するのでWaitを解除）
            //    * UIスレッドからワーカスレッドへ遷移した場合は、戻り先のスレッドはワーカースレッド
            //      ワーカースレッドからワーカースレッドへ遷移した場合の戻り先は呼び出し元のワーカースレッドとは限らない
            // 3. s3への代入はUIスレッドで実行される
            var s1 = $"Before Task: {Environment.CurrentManagedThreadId}"; // ID: UIスレッド
            Debug.WriteLine(s1);
            Task.Run(async () =>
            {
                var s2 = $"On Task: {Environment.CurrentManagedThreadId}"; // ID: ワーカースレッド
                await Task.Delay(100);
                Debug.WriteLine(s2);
            }).Wait();
            // Wait関数によってTask.Runの中身が実行される際はUIスレッドはロックされる
            // Task.Runの中にUIスレッドで実行されるものはないため、デッドロックしない
            var s3 = $"After Task: {Environment.CurrentManagedThreadId}"; // ID: UIスレッド
            Debug.WriteLine(s3);
        }

        private void DeadLockAsyncMethodButton_click(object sender, RoutedEventArgs e)
        {
            // 1. AsyncMethodを呼び出し
            //    Wait関数を使用しているので、呼び出し元のスレッド(今回はUIスレッド)から他のスレッドに遷移した際に、
            //    呼び出し元のスレッドをロックする(Task終了を待機する)
            // 2. s1への代入はそのままUIスレッドで実行される
            // 3. s2への代入はTask.Runで記載しているため、処理はワーカースレッドで実行される
            //    1.でWait関数を実行しているため、処理がワーカースレッドに移るこのタイミングで、
            //    呼び出し元のスレッド(UIスレッド)はロックされる
            // 4. s2のTaskをawaitしているため、s3への代入はワーカースレッドからUIスレッドに戻って実行しようとするが、
            //    3.でUIスレッドはロックされているので、処理を実行することができない(デッドロック)
            //    * awaitは処理が完了したときに、キャプチャしたコンテキストで後続の処理を実行しようとする
            //    * UIスレッドからワーカースレッドに遷移し処理が完了した場合、スレッドは必ずUIスレッドに戻る
            //    * ワーカースレッドからワーカスレッドに遷移し処理が完了した場合、スレッドはもとのワーカースレッドに戻るとは限らない
            //    => UIスレッドでWaitしたTask内の処理でワーカースレッドでの処理(Task.Runが含まれる処理)をawaitし、
            //       awaitの後に処理が残っていると、
            //       ワーカースレッドからキャプチャしたコンテキスト(ロックされたUIスレッド)に戻って処理を行おうとして、デッドロックする
            //       * Wait()ではなくConfigureAwait(false)とするとUIスレッドに戻らず、
            //         そのままワーカースレッドで処理が実行されるのでデッドロックしない（同一のワーカースレッドとは限らない）
            //         => await == ConfigureWait(true)
            AsyncMethod().Wait();
        }

        private void NotDeadLockAsyncMethodButton_Click(object sender, RoutedEventArgs e)
        {
            // ThreadCheck_Click関数と同様に、UIスレッドはTask.Run()の処理がワーカースレッドで実行されるのを待ち、
            // ワーカースレッドで処理が完了し、Task.Run内でUIスレッドで実行されるものはないため、デッドロックしない
            Task.Run(async () => await AsyncMethod()).Wait();
        }

        private void NotDeadLockAsyncMethodConfigureAwaitFalseButton_Click(object sender, RoutedEventArgs e)
        {
            AsyncMethod().ConfigureAwait(false);
        }

        private void DeadLockMethodButton_click(object sender, RoutedEventArgs e)
        {
            DeadLockMethod();
        }

        private void NotDeadLockMethodButton_Click(object sender, RoutedEventArgs e)
        {
            NotDeadLockMethod();
        }

        private void NotDeadLockMethodConfigureAwaitFalseButton_Click(object sender, RoutedEventArgs e)
        {
            NotDeadLockMethodConfigureAwaitFalse();
        }

        private static async Task AsyncMethod()
        {
            var s1 = $"Before Task: {Environment.CurrentManagedThreadId}";
            Debug.WriteLine(s1);
            await Task.Run(() =>
            {
                var s2 = $"On Task: {Environment.CurrentManagedThreadId}";
                Debug.WriteLine(s2);
            });
            var s3 = $"After Task: {Environment.CurrentManagedThreadId}";
            Debug.WriteLine(s3);
        }

        // Waitを行うためスレッドセーフでないメソッド
        private static void DeadLockMethod()
        {
            // DeadLockAsyncMethodButton_click関数と同様の動作となる
            AsyncMethod().Wait();
        }

        // UIスレッドで呼び出されてもデッドロックしないスレッドセーフなメソッド
        private static void NotDeadLockMethod()
        {
            // NotDeadLockAsyncMethodButton_Click関数と同様の動作となる
            Task.Run(async () => await AsyncMethod()).Wait();
        }

        private static void NotDeadLockMethodConfigureAwaitFalse()
        {
            // NotDeadLockAsyncMethodConfigureAwaitFalseButton_Click関数と同様の動作となる
            AsyncMethod().ConfigureAwait(false);
        }

    }
}
