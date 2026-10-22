using CompMs.App.RawDataViewer.ViewModel;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.App.RawDataViewer.Tests
{
    /// <summary>
    /// The unobserved task exceptions of the test run, from the first call to <see cref="Listen"/> on.
    /// </summary>
    internal static class UnobservedTaskExceptions
    {
        private static readonly ConcurrentQueue<Exception> Reported = new ConcurrentQueue<Exception>();

        static UnobservedTaskExceptions() {
            TaskScheduler.UnobservedTaskException += (s, e) => Reported.Enqueue(e.Exception);
        }

        /// <summary>
        /// Starts listening, if no test has yet. Call it before the tasks to be checked are made.
        /// </summary>
        public static void Listen() {
            // The static constructor does it, and runs once, before this.
        }

        /// <summary>
        /// Finalizes every faulted task that nothing refers to any more, which is when a fault nobody observed is reported.
        /// </summary>
        public static void Collect() {
            for (int i = 0; i < 3; i++) {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        /// <summary>
        /// Whether an unobserved task exception reported so far mentions <paramref name="text"/>.
        /// </summary>
        public static bool AnyWith(string text) {
            return Reported.Any(e => PanelLoadResult.Describe(e).Contains(text));
        }
    }
}
