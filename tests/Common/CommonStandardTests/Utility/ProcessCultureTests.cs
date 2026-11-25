using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.Common.Utility.Tests
{
    // These tests change the process-wide default culture while they run, so nothing may run beside them.
    [TestClass, DoNotParallelize]
    public class ProcessCultureTests
    {
        /// <summary>
        /// Emulates a PC whose regional format is fr-FR, as every thread of the process would see it,
        /// then checks that after the call each way MS-DIAL runs work reads "1.5" as one and a half,
        /// and that the UI culture, which only sets the language of messages, is left alone.
        /// </summary>
        /// <remarks>
        /// RegionalCulture is set by the first call in the process and cannot be reset, so the test
        /// only checks that it is set.
        /// </remarks>
        [TestMethod]
        public void UseInvariantCulture_OnADecimalCommaPc_ReachesEveryKindOfThread() {
            var savedDefault = CultureInfo.DefaultThreadCurrentCulture;
            var savedCurrent = CultureInfo.CurrentCulture;
            var savedUi = CultureInfo.CurrentUICulture;
            var french = CultureInfo.GetCultureInfo("fr-FR");
            var seen = new ConcurrentDictionary<string, string>();
            void Record(string context) {
                var culture = CultureInfo.CurrentCulture;
                var parsed = double.TryParse("1.5", out var value) && value == 1.5;
                seen[context] = (culture.Name == "" ? "invariant" : culture.Name) + (parsed ? "" : " (cannot read 1.5)");
            }

            string uiCultureAfter;
            using var release = new ManualResetEventSlim(false);
            Thread earlier = null;
            try {
                CultureInfo.DefaultThreadCurrentCulture = french;
                CultureInfo.CurrentCulture = french;
                CultureInfo.CurrentUICulture = french;
                // A thread that exists before the call and has no culture of its own, like the
                // runtime's worker threads: it must not inherit the calling thread's culture.
                earlier = new Thread(() => { release.Wait(); Record("thread started before the call"); });
                using (ExecutionContext.SuppressFlow()) {
                    earlier.Start();
                }

                ProcessCulture.UseInvariantCulture();
                uiCultureAfter = CultureInfo.CurrentUICulture.Name;

                Record("calling thread");
                Task.Run(() => Record("Task.Run")).Wait();
                Task.Factory.StartNew(() => Record("long-running task"), TaskCreationOptions.LongRunning).Wait();
                Parallel.For(0, 8, i => Record($"Parallel.For {i}"));
                var thread = new Thread(() => Record("new Thread"));
                thread.Start();
                thread.Join();
                using (var done = new ManualResetEventSlim(false)) {
                    ThreadPool.UnsafeQueueUserWorkItem(_ => { Record("ThreadPool.UnsafeQueueUserWorkItem"); done.Set(); }, null);
                    done.Wait();
                }
                release.Set();
                earlier.Join();
            }
            finally {
                release.Set();
                earlier?.Join();
                CultureInfo.DefaultThreadCurrentCulture = savedDefault;
                CultureInfo.CurrentCulture = savedCurrent;
                CultureInfo.CurrentUICulture = savedUi;
            }

            var wrong = seen.Where(kv => kv.Value != "invariant").Select(kv => $"{kv.Key}: {kv.Value}").ToArray();
            Assert.AreEqual(0, wrong.Length, string.Join(Environment.NewLine, wrong));
            Assert.AreEqual(14, seen.Count, string.Join(", ", seen.Keys));
            Assert.AreEqual("fr-FR", uiCultureAfter);
            Assert.IsNotNull(ProcessCulture.RegionalCulture);
        }

        [TestMethod]
        public void Describe_NamesTheRegionalFormatItsSeparatorsAndTheProcessCulture() {
            Assert.AreEqual(
                "de-DE (decimal ',', grouping '.'); process: invariant",
                ProcessCulture.Describe(CultureInfo.GetCultureInfo("de-DE"), CultureInfo.InvariantCulture));
            Assert.AreEqual(
                "en-US (decimal '.', grouping ','); process: en-US",
                ProcessCulture.Describe(CultureInfo.GetCultureInfo("en-US"), CultureInfo.GetCultureInfo("en-US")));
        }

        [TestMethod]
        public void Describe_ShowsAnInvisibleGroupingSymbolByItsCodePoint() {
            var format = (CultureInfo)CultureInfo.GetCultureInfo("fr-FR").Clone();
            format.NumberFormat.NumberGroupSeparator = " ";

            Assert.AreEqual(
                "fr-FR (decimal ',', grouping U+202F); process: invariant",
                ProcessCulture.Describe(format, CultureInfo.InvariantCulture));
        }
    }
}
