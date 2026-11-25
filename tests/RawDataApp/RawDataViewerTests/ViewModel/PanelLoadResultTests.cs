using CompMs.App.RawDataViewer.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.RawDataViewer.ViewModel.Tests
{
    [TestClass]
    public class PanelLoadResultTests
    {
        [ClassInitialize]
        public static void Listen(TestContext _) {
            UnobservedTaskExceptions.Listen();
        }

        [TestMethod]
        public void ACompletedTaskGivesItsViewModel() {
            using var panel = PanelLoadResult.FromTask(Task.FromResult(3), m => $"view model of {m}").ToReadOnlyReactivePropertySlim();

            Assert.AreEqual("view model of 3", panel.Value.Value);
            Assert.IsFalse(panel.Value.IsFailed);
            Assert.IsNull(panel.Value.ErrorMessage);
        }

        [TestMethod]
        public void APropertyDerivedFromThePanelOfAnEndedTaskGetsItsValue() {
            // Task.ToObservable emits the result of a task that has already ended while the panel subscribes, so the panel
            // has its value before anything subscribes to it, as the view model properties derived from it do later.
            using var panel = PanelLoadResult.FromTask(Task.FromResult(3), m => $"view model of {m}").ToReadOnlyReactivePropertySlim();
            using var derived = panel.Select(p => p?.Value).ToReadOnlyReactivePropertySlim();

            Assert.AreEqual("view model of 3", derived.Value);
        }

        [TestMethod]
        public void AFaultedTaskGivesItsErrorAndLeavesNoUnobservedTaskException() {
            var tag = Guid.NewGuid().ToString("N");
            var ready = new TaskCompletionSource<bool>();
            // Faults after the panel subscribes, as a model task does while the raw data is still being read.
            var model = Task.Run<int>(async () => {
                await ready.Task.ConfigureAwait(false);
                throw new KeyNotFoundException($"The given key was not present in the dictionary. {tag}");
            });

            using (var panel = PanelLoadResult.FromTask(model, m => $"view model of {m}").ToReadOnlyReactivePropertySlim()) {
                Assert.IsNull(panel.Value, "nothing to show while the model is being built");
                ready.SetResult(true);
                Assert.IsTrue(SpinWait.SpinUntil(() => !(panel.Value is null), TimeSpan.FromSeconds(10)));

                Assert.IsTrue(panel.Value.IsFailed);
                Assert.IsNull(panel.Value.Value);
                Assert.AreEqual($"System.Collections.Generic.KeyNotFoundException: The given key was not present in the dictionary. {tag}", panel.Value.ErrorMessage);
            }

            UnobservedTaskExceptions.Collect();
            Assert.IsFalse(UnobservedTaskExceptions.AnyWith(tag), "the fault is observed");
        }

        [TestMethod]
        public void AFailureToBuildTheViewModelIsAnErrorToo() {
            using var panel = PanelLoadResult.FromTask<int, string>(Task.FromResult(3), m => throw new InvalidOperationException("no view model")).ToReadOnlyReactivePropertySlim();

            Assert.IsTrue(panel.Value.IsFailed);
            Assert.AreEqual("System.InvalidOperationException: no view model", panel.Value.ErrorMessage);
        }

        [TestMethod]
        public void ACanceledTaskIsAnError() {
            using var panel = PanelLoadResult.FromTask(Task.FromCanceled<int>(new CancellationToken(true)), m => $"view model of {m}").ToReadOnlyReactivePropertySlim();

            Assert.IsTrue(panel.Value.IsFailed);
            StringAssert.StartsWith(panel.Value.ErrorMessage, "System.Threading.Tasks.TaskCanceledException: ");
        }

        [TestMethod]
        public void ControlTheUnguardedPipelineLosesTheFaultAsAnUnobservedTaskException() {
            // The pipeline the summary window used before PanelLoadResult. This shows that the test above can see an
            // unobserved task exception when there is one: if this starts failing, that test proves nothing any more.
            var tag = Guid.NewGuid().ToString("N");
            var ready = new TaskCompletionSource<bool>();
            var model = Task.Run<int>(async () => {
                await ready.Task.ConfigureAwait(false);
                throw new KeyNotFoundException(tag);
            });

            using (var panel = model.ToObservable().Select(m => $"view model of {m}").ToReadOnlyReactivePropertySlim()) {
                ready.SetResult(true);
                Assert.IsTrue(SpinWait.SpinUntil(() => {
                    UnobservedTaskExceptions.Collect();
                    return UnobservedTaskExceptions.AnyWith(tag);
                }, TimeSpan.FromSeconds(10)));
                Assert.IsNull(panel.Value, "the panel never leaves \"Loading...\"");
            }
        }

        [TestMethod]
        public void DescribeListsTheInnermostCauseFirst() {
            var exception = new InvalidOperationException("outer", new IOException("middle", new KeyNotFoundException("inner")));

            var lines = PanelLoadResult.Describe(exception).Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            CollectionAssert.AreEqual(new[]
            {
                "System.Collections.Generic.KeyNotFoundException: inner",
                "System.IO.IOException: middle",
                "System.InvalidOperationException: outer",
            }, lines);
        }

        [TestMethod]
        public void DescribeUnwrapsAggregateExceptions() {
            var exception = new AggregateException(
                new AggregateException(new KeyNotFoundException("first")),
                new InvalidOperationException("second", new IOException("cause")));

            var description = PanelLoadResult.Describe(exception);

            var nl = Environment.NewLine;
            Assert.AreEqual(
                $"System.Collections.Generic.KeyNotFoundException: first{nl}{nl}System.IO.IOException: cause{nl}System.InvalidOperationException: second",
                description);
        }
    }
}
