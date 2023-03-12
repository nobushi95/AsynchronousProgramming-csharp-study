// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;

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
            var s1 = $"Before Task: {Environment.CurrentManagedThreadId}"; // ID: UIスレッド
            Task.Run(() =>
            {
                var s2 = $"On Task: {Environment.CurrentManagedThreadId}"; // ID: ワーカースレッド
            }).Wait(); // UIスレッドをロックしない？
            var s3 = $"After Task: {Environment.CurrentManagedThreadId}"; // ID: UIスレッド
        }

        private void DeadLockAsyncMethodButton_click(object sender, RoutedEventArgs e)
        {
            AsyncMethod().Wait();
        }

        private void NotDeadLockAsyncMethodButton_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(async () => await AsyncMethod()).Wait();
        }

        private void DeadLockMethodButton_click(object sender, RoutedEventArgs e)
        {
            DeadLockMethod();
        }

        private void NotDeadLockMethodButton_Click(object sender, RoutedEventArgs e)
        {
            NotDeadLockMethod();
        }

        private static async Task AsyncMethod()
        {
            var s1 = $"Before Task: {Environment.CurrentManagedThreadId}";
            await Task.Run(() =>
            {
                var s2 = $"On Task: {Environment.CurrentManagedThreadId}";
            });
            var s3 = $"After Task: {Environment.CurrentManagedThreadId}";
        }

        // Waitを行うためスレッドセーフでないメソッド
        private static void DeadLockMethod()
        {
            AsyncMethod().Wait();
        }

        // UIスレッドで呼び出されてもデッドロックしないスレッドセーフなメソッド
        private static void NotDeadLockMethod()
        {
            Task.Run(async () => await AsyncMethod()).Wait();
        }
    }
}
