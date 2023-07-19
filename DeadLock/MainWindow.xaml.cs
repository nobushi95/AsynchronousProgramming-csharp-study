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
            Task.Run(() =>
            {
                var s2 = $"On Task: {Environment.CurrentManagedThreadId}"; // ID: ワーカースレッド
                Debug.WriteLine(s2);
            }).Wait(); // UIスレッドをロックしない？
            var s3 = $"After Task: {Environment.CurrentManagedThreadId}"; // ID: UIスレッド
            Debug.WriteLine(s3);
        }

        private void DeadLockAsyncMethodButton_click(object sender, RoutedEventArgs e)
        {
            // 1. AsyncMethodを呼び出し
            //    Wait関数を使用しているので、呼び出し元のスレッド(今回はUIスレッド)から他のスレッドに遷移した際に、
            //    呼び出し元のスレッドをロックする(Task終了をWaitする)
            // 2. s1への代入はUIスレッドで実行される
            // 3. s2への代入はワーカースレッドで実行される
            //    呼び出し元のスレッド(UIスレッド)から他のスレッドに遷移するので、呼び出し元のスレッド(UIスレッド)は
            //    ロックされる(待機状態になる)
            //    * AsyncMethod関数が返すTaskの終了を待機するイメージ
            // 4. s3への代入はワーカースレッドからUIスレッドに戻って実行しようとするが、
            //    3.でUIスレッドは待機状態に移行している(Taskの終了を待っている)ので、
            //    処理を実行することができない(デッドロック)
            //    => UIスレッドでWaitしたTask内の処理でワーカースレッドで処理をしたのちに、
            //       UIスレッドで処理を行おうとするとデッドロックする
            //       * Wait()ではなくConfigureAwait(false)とするとUIスレッドに戻らず、
            //         そのままワーカースレッドで処理が実行されるのでデッドロックしない？？
            AsyncMethod().Wait();
        }

        private void NotDeadLockAsyncMethodButton_Click(object sender, RoutedEventArgs e)
        {
            // ThreadCheck_Click関数と同様に、UIスレッドはTask.Run()の処理がワーカースレッドで実行されるのを待ち、
            // ワーカースレッドで処理が完了し、待機していたUIスレッドを開放するのでデッドロックしない
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
