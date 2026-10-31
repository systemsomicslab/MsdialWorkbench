using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Utility.Tests;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MsdialGuiAppTests.Model.Search;

[TestClass]
public sealed class CompoundSearchLifecycleTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task ClosingModelCompletesItsObservableWithoutReportingAnError(bool closeBeforeStarting) {
        using var file = new AnalysisFileBeanModel(new AnalysisFileBean());
        using var plot = new PlotComparedMsSpectrumUsecase(new MSScanProperty { Spectrum = [] });
        var search = new CancellableSearch();
        using var model = new CompoundSearchModel<int>(file, 0, search, plot, null!);
        var terminal = new TaskCompletionSource<Exception?>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (closeBeforeStarting) { model.Dispose(); }
        using var subscription = Observable.FromAsync(model.SearchAsync).Subscribe(
            _ => { }, error => terminal.TrySetResult(error), () => terminal.TrySetResult(null));
        if (!closeBeforeStarting) {
            await search.Started.Task;
            model.Dispose();
        }
        Assert.AreSame(terminal.Task, await Task.WhenAny(terminal.Task, Task.Delay(TimeSpan.FromSeconds(5))));
        Assert.IsNull(await terminal.Task, "Closing a search window is normal completion, not an error for its observable subscriber.");
        Assert.IsNull(model.CompoundResults);
    }

    [TestMethod]
    public async Task ClosingModelCancelsScoringBeforeDeferredWpfResourceDisposal() {
        TestApplication.Ensure();
        using var file = new AnalysisFileBeanModel(new AnalysisFileBean());
        using var plot = new PlotComparedMsSpectrumUsecase(new MSScanProperty { Spectrum = [] });
        var search = new CancellableSearch();
        using var model = new CompoundSearchModel<int>(file, 0, search, plot, null!);
        var pending = model.SearchAsync(default);
        await search.Started.Task;
        using var blocked = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        var dispatcherWork = System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
            blocked.Set();
            if (!release.Wait(TimeSpan.FromSeconds(10))) { throw new TimeoutException("Dispatcher release timed out."); }
        });
        try {
            Assert.IsTrue(blocked.Wait(TimeSpan.FromSeconds(5)));
            model.Dispose();
            Assert.IsTrue(search.Token.IsCancellationRequested,
                "Closing a window must cancel scoring even while its dispatcher cannot dispose queued resources.");
        }
        finally {
            release.Set();
            await dispatcherWork.Task;
            TestApplication.WaitForBackgroundWork();
            await pending;
        }
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task SearchPublishesOnlyWhileItsModelIsOpen(bool closeModel) {
        using var file = new AnalysisFileBeanModel(new AnalysisFileBean());
        using var plot = new PlotComparedMsSpectrumUsecase(new MSScanProperty { Spectrum = [] });
        using var search = new DelayedSearch();
        using var model = new CompoundSearchModel<int>(file, 0, search, plot, null!);
        var notifications = 0;
        model.PropertyChanged += (_, e) => {
            if (e.PropertyName == nameof(model.CompoundResults)) { notifications++; }
        };
        var pending = model.SearchAsync(default);
        await search.Started.Task;

        if (closeModel) { model.Dispose(); }
        search.Release.Set();
        await pending;

        Assert.AreEqual(closeModel ? 0 : 1, notifications);
        Assert.AreEqual(closeModel ? 0 : 1, model.CompoundResults?.Count ?? 0);
    }

    [TestMethod]
    public async Task ClosingModelStopsCancellableSearchWithoutWaitingForItsWork() {
        using var file = new AnalysisFileBeanModel(new AnalysisFileBean());
        using var plot = new PlotComparedMsSpectrumUsecase(new MSScanProperty { Spectrum = [] });
        var search = new CancellableSearch();
        using var model = new CompoundSearchModel<int>(file, 0, search, plot, null!);
        var pending = model.SearchAsync(default);
        await search.Started.Task;
        model.Dispose();
        var completed = await Task.WhenAny(pending, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.AreSame(pending, completed, "Closing the model must reach the scoring loop's cancellation token.");
        await pending;
        Assert.IsNull(model.CompoundResults);
    }

    private sealed class CancellableSearch : BindableBase, ICancellableCompoundSearchUsecase<ICompoundResult, int>
    {
        public CancellationToken Token { get; private set; }
        public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public IList SearchMethods { get; } = Array.Empty<object>();
        public object? SearchMethod { get; set; }
        public MsRefSearchParameterBase? SearchParameter => null;
        public IReadOnlyList<ICompoundResult> Search(int target) => throw new InvalidOperationException("Use cancellable search.");
        public IReadOnlyList<ICompoundResult> Search(int target, CancellationToken token) {
            Token = token;
            Started.TrySetResult(true);
            token.WaitHandle.WaitOne(TimeSpan.FromSeconds(5));
            token.ThrowIfCancellationRequested();
            throw new TimeoutException("Cancellation did not reach the search.");
        }
    }

    [TestMethod]
    public async Task CancelledSearchDoesNotPublishLateResults() {
        using var file = new AnalysisFileBeanModel(new AnalysisFileBean());
        using var plot = new PlotComparedMsSpectrumUsecase(new MSScanProperty { Spectrum = [] });
        using var search = new DelayedSearch();
        using var model = new CompoundSearchModel<int>(file, 0, search, plot, null!);
        using var cancellation = new CancellationTokenSource();
        var pending = model.SearchAsync(cancellation.Token);
        await search.Started.Task;

        cancellation.Cancel();
        search.Release.Set();
        try {
            await pending;
            Assert.Fail("A cancelled search must not publish its result.");
        }
        catch (OperationCanceledException) {
            Assert.IsNull(model.CompoundResults);
        }
    }

    private sealed class DelayedSearch : BindableBase, ICompoundSearchUsecase<ICompoundResult, int>, IDisposable
    {
        public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ManualResetEventSlim Release { get; } = new(false);
        public IList SearchMethods { get; } = Array.Empty<object>();
        public object? SearchMethod { get; set; }
        public MsRefSearchParameterBase? SearchParameter => null;
        public IReadOnlyList<ICompoundResult> Search(int target) {
            Started.TrySetResult(true);
            if (!Release.Wait(TimeSpan.FromSeconds(5))) { throw new TimeoutException("Search release timed out."); }
            return [new CompoundResult(new MoleculeMsReference(), new MsScanMatchResult())];
        }
        public void Dispose() => Release.Dispose();
    }
}
