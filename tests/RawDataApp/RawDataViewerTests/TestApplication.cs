using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace CompMs.App.RawDataViewer.Tests
{
    /// <summary>
    /// A WPF application whose dispatcher runs on its own STA thread, as the viewer's UI thread would be.
    /// An AppDomain holds one application at most, so once a test makes it, every later test in the run has it too.
    /// </summary>
    internal static class TestApplication
    {
        public static void Ensure() {
            if (Application.Current is not null) {
                return;
            }
            using var ready = new ManualResetEventSlim();
            var thread = new Thread(() => {
                _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown, };
                ready.Set();
                Dispatcher.Run();
            })
            {
                IsBackground = true,
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            ready.Wait();
        }

        /// <summary>
        /// Returns once the dispatcher has run everything queued at Background priority or above, which is where
        /// DisposableCollection disposes its items while an application runs. Call it from a thread other than the dispatcher's.
        /// </summary>
        public static void WaitForBackgroundWork() {
            Application.Current?.Dispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
        }
    }
}
