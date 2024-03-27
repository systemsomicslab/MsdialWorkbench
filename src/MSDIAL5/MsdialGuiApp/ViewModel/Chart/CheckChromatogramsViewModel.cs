using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.ViewModel.MsResult;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Common;
using CompMs.Graphics.UI;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.ViewModel.Chart;

internal sealed class CheckChromatogramsViewModel : ViewModelBase, IDialogPropertiesViewModel
{
    private readonly CheckChromatogramsModel _model;
    private readonly IMessageBroker _broker;

    public CheckChromatogramsViewModel(CheckChromatogramsModel model, IMessageBroker? broker = null) {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _broker = broker ?? MessageBroker.Default;

        RangeSelectableChromatogramViewModel = model.ObserveProperty(m => m.RangeSelectableChromatogramModel)
            .Select(m => m is null ? null : new RangeSelectableChromatogramViewModel(m))
            .DisposePreviousValue()
            .ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        ChromatogramsViewModel = RangeSelectableChromatogramViewModel.Select(vm => vm?.ChromatogramsViewModel).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        AccumulatedMs1SpectrumViewModel = model.ObserveProperty(m => m.AccumulatedMs1SpectrumModel)
            .DefaultIfNull(m => new AccumulatedMs1SpectrumViewModel(m))
            .DisposePreviousValue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        AccumulatedMs2SpectrumViewModel = model.ObserveProperty(m => m.AccumulatedMs2SpectrumModel)
            .DefaultIfNull(m => new AccumulatedMs2SpectrumViewModel(m))
            .DisposePreviousValue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        AccumulatedMs2SpectrumViewModels = model.ObserveProperty(m => m.AccumulatedMs2SpectrumModels)
            .Select(ms => ms.Select(m => new AccumulatedExtractedMs2SpectrumViewModel(m)).ToArray())
            .DisposePreviousValue()
            .ToReadOnlyReactivePropertySlim(Array.Empty<AccumulatedExtractedMs2SpectrumViewModel>())
            .AddTo(Disposables);
        AccumulatedSpecificExperimentMS2SpectrumViewModels = model.ObserveProperty(m => m.AccumulatedSpecificExperimentMS2SpectrumModels)
            .Select(ms => ms.Select(m => new AccumulatedSpecificExperimentMS2SpectrumViewModel(m)).ToArray())
            .DisposePreviousValue()
            .ToReadOnlyReactivePropertySlim(Array.Empty<AccumulatedSpecificExperimentMS2SpectrumViewModel>())
            .AddTo(Disposables);

        InsertTic = model.LoadChromatogramsUsecase.ToReactivePropertySlimAsSynchronized(m => m.InsertTic).AddTo(Disposables);
        InsertBpc = model.LoadChromatogramsUsecase.ToReactivePropertySlimAsSynchronized(m => m.InsertBpc).AddTo(Disposables);
        InsertHighestEic = model.LoadChromatogramsUsecase.ToReactivePropertySlimAsSynchronized(m => m.InsertHighestEic).AddTo(Disposables);
        InsertMS2Tic = model.LoadChromatogramsUsecase.ToReactivePropertySlimAsSynchronized(m => m.InsertMS2Tic).AddTo(Disposables);

        CopyAsTableCommand = new ReactiveCommand()
            .WithSubscribe(CopyAsTable)
            .AddTo(Disposables);
        SaveAsTableCommand = new AsyncReactiveCommand()
            .WithSubscribe(SaveAsTableAsync)
            .AddTo(Disposables);

        DiplayEicSettingValues = model.DisplayEicSettingValues
            .ToReadOnlyReactiveCollection(x => new PeakFeatureSearchValueViewModel(x))
            .AddTo(Disposables);

        var settingHasError = new[] {
            DiplayEicSettingValues.ObserveAddChanged().ToUnit(),
            DiplayEicSettingValues.ObserveRemoveChanged().ToUnit(),
            DiplayEicSettingValues.ObserveElementObservableProperty(vm => vm.HasErrors).ToUnit(),
        }.Merge()
        .Select(_ => DiplayEicSettingValues.Any(vm => vm.HasErrors.Value));

        ObserveHasErrors = new[]
        {
            settingHasError
        }.CombineLatestValuesAreAllFalse()
        .Inverse()
        .ToReadOnlyReactivePropertySlim(true)
        .AddTo(Disposables);

        ApplyCommand = ObserveHasErrors.Inverse()
           .ToReactiveCommand()
           .WithSubscribe(Apply)
           .AddTo(Disposables);
        ClearCommand = new ReactiveCommand()
            .WithSubscribe(model.Clear)
            .AddTo(Disposables);

        ShowAccumulatedMs1SpectrumCommand = RangeSelectableChromatogramViewModel.Select(vm => vm is not null)
            .ToAsyncReactiveCommand()
            .WithSubscribe(() => ShowAccumulatedSpectrumAsync(default)).AddTo(Disposables);
        ShowAccumulatedSpectrumCommand = RangeSelectableChromatogramViewModel.Select(vm => vm is not null)
            .ToAsyncReactiveCommand<ViewModelBase>()
            .WithSubscribe(vm => ShowAccumulatedSpectrumAsync(vm, default)).AddTo(Disposables);

        VisiblePeakLabel = model.ToReactivePropertySlimAsSynchronized(m => m.VisiblePeakLabel).AddTo(Disposables);
        DetectPeaksCommand = new ReactiveCommand().WithSubscribe(model.DetectPeaks).AddTo(Disposables);
        AddPeaksCommand = RangeSelectableChromatogramViewModel.Select(vm => vm is { SelectedRange: not null })
            .ToReactiveCommand().WithSubscribe(model.AddPeak).AddTo(Disposables);
        ResetPeaksCommand = new ReactiveCommand().WithSubscribe(model.ResetPeaks).AddTo(Disposables);
        RemovePeakCommand = new ReactiveCommand<DisplayPeakOfChromatogram>().WithSubscribe(model.RemovePeak).AddTo(Disposables);
        ExportPeaksCommand = new ReactiveCommand().WithSubscribe(model.ExportPeaks).AddTo(Disposables);

        Layout = model.ObserveProperty(m => m.Layout).ToReadOnlyReactivePropertySlim<IDockLayoutElement>().AddTo(Disposables);
        SerializeLayoutCommand = new ReactiveCommand<NodeContainers>().WithSubscribe(model.SerializeLayout).AddTo(Disposables);
        DeserializeLayoutCommand = new ReactiveCommand().WithSubscribe(model.DeserializeLayout).AddTo(Disposables);

        ViewModels = [
            new ChromatogramViewModel(ChromatogramsViewModel, RangeSelectableChromatogramViewModel, AccumulatedMs2SpectrumViewModel, AccumulatedMs2SpectrumViewModels, AccumulatedSpecificExperimentMS2SpectrumViewModels, VisiblePeakLabel, ShowAccumulatedMs1SpectrumCommand, ShowAccumulatedSpectrumCommand, CopyAsTableCommand, SaveAsTableCommand),
            new EicSettingViewModel(DiplayEicSettingValues, InsertTic, InsertBpc, InsertHighestEic, InsertMS2Tic, ApplyCommand, ClearCommand),
            new PeakPickViewModel(ChromatogramsViewModel, VisiblePeakLabel, DetectPeaksCommand, AddPeaksCommand, ResetPeaksCommand, RemovePeakCommand, ExportPeaksCommand),
        ];
    }

    public AnalysisFileBeanModel FileModel => _model.File;

    public ReadOnlyReactivePropertySlim<ChromatogramsViewModel?> ChromatogramsViewModel { get; }
    public ReadOnlyReactivePropertySlim<RangeSelectableChromatogramViewModel?> RangeSelectableChromatogramViewModel { get; }
    public ReadOnlyReactivePropertySlim<AccumulatedMs1SpectrumViewModel?> AccumulatedMs1SpectrumViewModel { get; }
    public ReadOnlyReactivePropertySlim<AccumulatedMs2SpectrumViewModel?> AccumulatedMs2SpectrumViewModel { get; }
    public ReadOnlyReactivePropertySlim<AccumulatedExtractedMs2SpectrumViewModel[]> AccumulatedMs2SpectrumViewModels { get; }
    public ReadOnlyReactivePropertySlim<AccumulatedSpecificExperimentMS2SpectrumViewModel[]> AccumulatedSpecificExperimentMS2SpectrumViewModels { get; }

    public AsyncReactiveCommand ShowAccumulatedMs1SpectrumCommand { get; }
    public AsyncReactiveCommand<ViewModelBase> ShowAccumulatedSpectrumCommand { get; }

    private async Task ShowAccumulatedSpectrumAsync(CancellationToken token) {
        if (AccumulatedMs1SpectrumViewModel.Value is not null) {
            var task = _model.AccumulateAsync(token);
            _broker.Publish(AccumulatedMs1SpectrumViewModel.Value);
            await task.ConfigureAwait(false);
        }
    }

    private async Task ShowAccumulatedSpectrumAsync(ViewModelBase vm, CancellationToken token) {
        Task task = Task.CompletedTask;
        switch (vm)
        {
            case AccumulatedMs2SpectrumViewModel ms2:
                task = _model.AccumulateAsync(ms2.Model, token);
                _broker.Publish(ms2);
                break;
            case AccumulatedExtractedMs2SpectrumViewModel ms2:
                task = _model.AccumulateAsync(ms2.Model, token);
                _broker.Publish(ms2);
                break;
            case AccumulatedSpecificExperimentMS2SpectrumViewModel expms2:
                task = _model.AccumulateAsync(expms2.Model, token);
                _broker.Publish(expms2);
                break;
        };
        await task.ConfigureAwait(false);
    }

    public ReactiveCommand CopyAsTableCommand { get; }

    private void CopyAsTable() {
        using var stream = new MemoryStream();
        _model.ExportAsync(stream, "\t").Wait();
        Clipboard.SetDataObject(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetString(stream.ToArray()));
    }

    public AsyncReactiveCommand SaveAsTableCommand { get; }

    private async Task SaveAsTableAsync() {
        var fileName = string.Empty;
        var request = new SaveFileNameRequest(name => fileName = name)
        {
            AddExtension = true,
            Filter = "tab separated values|*.txt",
            RestoreDirectory = true,
        };
        _broker.Publish(request);
        if (request.Result == true) {
            using var stream = File.Open(fileName, FileMode.Create);
            await _model.ExportAsync(stream, "\t").ConfigureAwait(false);
        }
    }

    public ReadOnlyReactiveCollection<PeakFeatureSearchValueViewModel> DiplayEicSettingValues { get; }
    public ReactivePropertySlim<bool> InsertTic { get; }
    public ReactivePropertySlim<bool> InsertBpc { get; }
    public ReactivePropertySlim<bool> InsertHighestEic { get; }
    public ReactivePropertySlim<bool> InsertMS2Tic { get; }

    public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }

    public ReactivePropertySlim<bool> VisiblePeakLabel { get; }

    public ReactiveCommand DetectPeaksCommand { get; }
    public ReactiveCommand AddPeaksCommand { get; }
    public ReactiveCommand ResetPeaksCommand { get; }
    public ReactiveCommand<DisplayPeakOfChromatogram> RemovePeakCommand { get; }
    public ReactiveCommand ExportPeaksCommand { get; }

    public ReactiveCommand ApplyCommand { get; }
    public ReactiveCommand ClearCommand { get; }

    private void Apply() {
        foreach (var value in DiplayEicSettingValues) {
            value.Commit();
        }
        _model.Update();
    }

    public ReadOnlyReactivePropertySlim<IDockLayoutElement> Layout { get; }
    public ReactiveCommand<NodeContainers> SerializeLayoutCommand { get; }
    public ReactiveCommand DeserializeLayoutCommand { get; }

    public ObservableCollection<BindableBase> ViewModels { get; }
    string? IDialogPropertiesViewModel.Title => $"Display chromatograms {_model.File.AnalysisFileName}";
    double? IDialogPropertiesViewModel.Width => null;
    double? IDialogPropertiesViewModel.Height => null;
}

internal sealed class ChromatogramViewModel(
    ReadOnlyReactivePropertySlim<ChromatogramsViewModel?> chromatogramsViewModel,
    ReadOnlyReactivePropertySlim<RangeSelectableChromatogramViewModel?> rangeSelectableChromatogramViewModel,
    ReadOnlyReactivePropertySlim<AccumulatedMs2SpectrumViewModel?> accumulatedMs2SpectrumViewModel,
    ReadOnlyReactivePropertySlim<AccumulatedExtractedMs2SpectrumViewModel[]> accumulatedExtractedMs2SpectrumViewModels,
    ReadOnlyReactivePropertySlim<AccumulatedSpecificExperimentMS2SpectrumViewModel[]> accumulatedSpecificExperimentMS2SpectrumViewModels,
    ReactivePropertySlim<bool> visiblePeakLabel,
    AsyncReactiveCommand showAccumulatedMs1SpectrumCommand,
    AsyncReactiveCommand<ViewModelBase> showAccumulatedSpectrumCommand,
    ReactiveCommand copyAsTableCommand,
    AsyncReactiveCommand saveAsTableCommand) : BindableBase
{
    public string Title { get; } = "Chromatograms";
    public ReadOnlyReactivePropertySlim<ChromatogramsViewModel?> ChromatogramsViewModel { get; } = chromatogramsViewModel;
    public ReadOnlyReactivePropertySlim<RangeSelectableChromatogramViewModel?> RangeSelectableChromatogramViewModel { get; } = rangeSelectableChromatogramViewModel;

    public ReadOnlyReactivePropertySlim<ViewModelBase[]> AccumulatedMs2SpectrumViewModels { get; } = new IObservable<ViewModelBase[]>[] {
        accumulatedMs2SpectrumViewModel.Select(x => x is null ? Array.Empty<ViewModelBase>() : [x]),
        accumulatedExtractedMs2SpectrumViewModels,
        accumulatedSpecificExperimentMS2SpectrumViewModels,
    }.CombineLatest<ViewModelBase[], ViewModelBase[]>(xs => [..xs[0], ..xs[1], ..xs[2]]).ToReadOnlyReactivePropertySlim([]);

    public ReactivePropertySlim<bool> VisiblePeakLabel { get; } = visiblePeakLabel;
    public AsyncReactiveCommand ShowAccumulatedMs1SpectrumCommand { get; } = showAccumulatedMs1SpectrumCommand;
    public AsyncReactiveCommand<ViewModelBase> ShowAccumulatedSpectrumCommand { get; } = showAccumulatedSpectrumCommand;
    public ReactiveCommand CopyAsTableCommand { get; } = copyAsTableCommand;
    public AsyncReactiveCommand SaveAsTableCommand { get; } = saveAsTableCommand;
}

internal sealed class EicSettingViewModel(
    ReadOnlyReactiveCollection<PeakFeatureSearchValueViewModel> diplayEicSettingValues,
    ReactivePropertySlim<bool> insertTic,
    ReactivePropertySlim<bool> insertBpc,
    ReactivePropertySlim<bool> insertHighestEic,
    ReactivePropertySlim<bool> insertMS2Tic,
    ReactiveCommand applyCommand,
    ReactiveCommand clearCommand) : BindableBase
{
    public string Title { get; } = "Setting";
    public ReadOnlyReactiveCollection<PeakFeatureSearchValueViewModel> DiplayEicSettingValues { get; } = diplayEicSettingValues;
    public ReactivePropertySlim<bool> InsertTic { get; } = insertTic;
    public ReactivePropertySlim<bool> InsertBpc { get; } = insertBpc;
    public ReactivePropertySlim<bool> InsertHighestEic { get; } = insertHighestEic;
    public ReactivePropertySlim<bool> InsertMS2Tic { get; } = insertMS2Tic;
    public ReactiveCommand ApplyCommand { get; } = applyCommand;
    public ReactiveCommand ClearCommand { get; } = clearCommand;
}

internal sealed class PeakPickViewModel(
    ReadOnlyReactivePropertySlim<ChromatogramsViewModel?> chromatogramsViewModel,
    ReactivePropertySlim<bool> visiblePeakLabel,
    ReactiveCommand detectPeaksCommand,
    ReactiveCommand addPeaksCommand,
    ReactiveCommand resetPeaksCommand,
    ReactiveCommand<DisplayPeakOfChromatogram> removePeakCommand,
    ReactiveCommand exportPeaksCommand) : BindableBase
{
    public string Title { get; } = "Peaks";
    public ReadOnlyReactivePropertySlim<ChromatogramsViewModel?> ChromatogramsViewModel { get; } = chromatogramsViewModel;
    public ReactivePropertySlim<bool> VisiblePeakLabel { get; } = visiblePeakLabel;
    public ReactiveCommand DetectPeaksCommand { get; } = detectPeaksCommand;
    public ReactiveCommand AddPeaksCommand { get; } = addPeaksCommand;
    public ReactiveCommand ResetPeaksCommand { get; } = resetPeaksCommand;
    public ReactiveCommand<DisplayPeakOfChromatogram> RemovePeakCommand { get; } = removePeakCommand;
    public ReactiveCommand ExportPeaksCommand { get; } = exportPeaksCommand;
}
