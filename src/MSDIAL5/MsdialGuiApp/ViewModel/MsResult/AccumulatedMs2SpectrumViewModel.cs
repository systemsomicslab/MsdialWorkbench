using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.MsResult;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Common;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.UI;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.MsResult;

internal sealed class AccumulatedMs2SpectrumViewModel : ViewModelBase, IDialogPropertiesViewModel
{
    public AccumulatedMs2SpectrumViewModel(AccumulatedMs2SpectrumModel model)
    {
        Model = model;
        MsSpectrumViewModel = model.ObserveProperty(m => m.PlotComparedSpectrum)
            .DefaultIfNull(m => new MsSpectrumViewModel(m.MsSpectrumModel))
            .DisposePreviousValue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

        Compounds = model.ObserveProperty(m => m.Compounds).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        SelectedCompound = model.ToReactivePropertySlimAsSynchronized(m => m.SelectedCompound).AddTo(Disposables);
        SelectedRange = model.ToReactivePropertySlimAsSynchronized(m => m.SelectedRange).AddTo(Disposables);

        ExtractedIonChromatogram = model.ObserveProperty(m => m.ExtractedIonChromatogram)
            .DefaultIfNull(m => new ChromatogramsViewModel(m))
            .DisposePreviousValue()
            .ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        ProductIonRange = model.ToReactivePropertySlimAsSynchronized(m => m.ExtractIonRange).AddTo(Disposables);

        SearchMethod = model.ToReactivePropertySlimAsSynchronized(m => m.SearchMethod).AddTo(Disposables);
        ParameterViewModel = model.SearchParameter
            .Select(parameter => parameter is null ? null : new MsRefSearchParameterBaseViewModel(parameter))
            .DisposePreviousValue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

        SearchCompoundCommand = new ReactiveCommand().WithSubscribe(model.SearchCompound).AddTo(Disposables);
        ImportDataBaseCommand = new ReactiveCommand().WithSubscribe(model.ImportDatabase).AddTo(Disposables);
        ExportCompoundCommand = new ReactiveCommand().WithSubscribe(model.ExportCompounds).AddTo(Disposables);
        CalculateExtractedIonChromatogramCommand = SelectedRange.Select(r => r is not null).ToReactiveCommand().WithSubscribe(model.CalculateExtractedIonChromatogram).AddTo(Disposables);

        DetectPeaksCommand = new ReactiveCommand().WithSubscribe(model.DetectPeaks).AddTo(Disposables);
        AddPeakCommand = new ReactiveCommand().WithSubscribe(model.AddPeak).AddTo(Disposables);
        ResetPeaksCommand = new ReactiveCommand().WithSubscribe(model.ResetPeaks).AddTo(Disposables);

        SaveAsNistCommand = model.ObserveProperty(m => m.Scan).Select(m => m is not null)
            .ToReactiveCommand().WithSubscribe(model.Export).AddTo(Disposables);

        Layout = model.ObserveProperty(m => m.Layout).ToReadOnlyReactivePropertySlim<IDockLayoutElement>().AddTo(Disposables);
        SerializeLayoutCommand = new ReactiveCommand<NodeContainers>().WithSubscribe(model.SerializeLayout).AddTo(Disposables);
        DeserializeLayoutCommand = new ReactiveCommand().WithSubscribe(model.DeserializeLayout).AddTo(Disposables);

        ViewModels = [
            new AccumulatedMS2SpectrumViewModel_Spectrum(MsSpectrumViewModel, SelectedRange, SaveAsNistCommand, CalculateExtractedIonChromatogramCommand),
            new AccumulatedMS2SpectrumViewModel_Search(model.SearchMethods, SearchMethod, ParameterViewModel, Compounds, SelectedCompound, SearchCompoundCommand, ImportDataBaseCommand, ExportCompoundCommand),
            new AccumulatedMS2SpectrumViewModel_Chromatogram(ExtractedIonChromatogram, ProductIonRange, DetectPeaksCommand, AddPeakCommand, ResetPeaksCommand),
        ];
    }

    public AccumulatedMs2SpectrumModel Model { get; }

    public AnalysisFileBeanModel FileModel => Model.FileModel;

    public ReadOnlyReactivePropertySlim<MsSpectrumViewModel?> MsSpectrumViewModel { get; }
    public ReactivePropertySlim<AxisRange?> SelectedRange { get; }

    public ReactiveCommand SaveAsNistCommand { get; }

    public ReadOnlyReactivePropertySlim<ChromatogramsViewModel?> ExtractedIonChromatogram { get; }
    public ReactivePropertySlim<AxisRange?> ProductIonRange { get; }

    public IList SearchMethods => Model.SearchMethods;

    public ReactivePropertySlim<object?> SearchMethod { get; }

    public ReadOnlyReactivePropertySlim<MsRefSearchParameterBaseViewModel?> ParameterViewModel { get; }

    public ReadOnlyReactivePropertySlim<IReadOnlyList<ICompoundResult>?> Compounds { get; }
    public ReactivePropertySlim<ICompoundResult?> SelectedCompound { get; }

    public ReactiveCommand SearchCompoundCommand { get; }

    public ReactiveCommand ImportDataBaseCommand { get; }

    public ReactiveCommand ExportCompoundCommand { get; }

    public ReactiveCommand CalculateExtractedIonChromatogramCommand { get; }

    public ReactiveCommand DetectPeaksCommand { get; }
    public ReactiveCommand AddPeakCommand { get; }
    public ReactiveCommand ResetPeaksCommand { get; }

    public ReadOnlyReactivePropertySlim<IDockLayoutElement> Layout { get; }
    public ReactiveCommand<NodeContainers> SerializeLayoutCommand { get; }
    public ReactiveCommand DeserializeLayoutCommand { get; }

    public ObservableCollection<BindableBase> ViewModels { get; }

    string? IDialogPropertiesViewModel.Title => $"Accumulated MS2 Spectrum, {Model.FileModel.AnalysisFileName}";
    double? IDialogPropertiesViewModel.Width => null;
    double? IDialogPropertiesViewModel.Height => null;
}

internal class AccumulatedMS2SpectrumViewModel_Spectrum(
    ReadOnlyReactivePropertySlim<MsSpectrumViewModel?> msSpectrumViewModel,
    ReactivePropertySlim<AxisRange?> selectedRange,
    ReactiveCommand saveAsNistCommand,
    ReactiveCommand calculateExtractedIonChromatogramCommand) : BindableBase
{
    public string Title { get; } = "Spectrum";
    public ReadOnlyReactivePropertySlim<MsSpectrumViewModel?> MsSpectrumViewModel { get; } = msSpectrumViewModel;
    public ReactivePropertySlim<AxisRange?> SelectedRange { get; } = selectedRange;
    public ReactiveCommand SaveAsNistCommand { get; } = saveAsNistCommand;
    public ReactiveCommand CalculateExtractedIonChromatogramCommand { get; } = calculateExtractedIonChromatogramCommand;
}

internal class AccumulatedMS2SpectrumViewModel_Search(
    IList searchMethods,
    ReactivePropertySlim<object?> searchMethod,
    ReadOnlyReactivePropertySlim<MsRefSearchParameterBaseViewModel?> parameterViewModel,
    ReadOnlyReactivePropertySlim<IReadOnlyList<ICompoundResult>?> compounds,
    ReactivePropertySlim<ICompoundResult?> selectedCompound,
    ReactiveCommand searchCompoundCommand,
    ReactiveCommand importDataBaseCommand,
    ReactiveCommand exportCompoundCommand) : BindableBase
{
    public string Title { get; } = "Search";
    public IList SearchMethods { get; } = searchMethods;
    public ReactivePropertySlim<object?> SearchMethod { get; } = searchMethod;
    public ReadOnlyReactivePropertySlim<MsRefSearchParameterBaseViewModel?> ParameterViewModel { get; } = parameterViewModel;
    public ReadOnlyReactivePropertySlim<IReadOnlyList<ICompoundResult>?> Compounds { get; } = compounds;
    public ReactivePropertySlim<ICompoundResult?> SelectedCompound { get; } = selectedCompound;
    public ReactiveCommand SearchCompoundCommand { get; } = searchCompoundCommand;
    public ReactiveCommand ImportDataBaseCommand { get; } = importDataBaseCommand;
    public ReactiveCommand ExportCompoundCommand { get; } = exportCompoundCommand;
}

internal class AccumulatedMS2SpectrumViewModel_Chromatogram(
    ReadOnlyReactivePropertySlim<ChromatogramsViewModel?> extractedIonChromatogram,
    ReactivePropertySlim<AxisRange?> productIonRange,
    ReactiveCommand detectPeaksCommand,
    ReactiveCommand addPeakCommand,
    ReactiveCommand resetPeaksCommand) : BindableBase
{
    public string Title { get; } = "Chromatogram";
    public ReadOnlyReactivePropertySlim<ChromatogramsViewModel?> ExtractedIonChromatogram { get; } = extractedIonChromatogram;
    public ReactivePropertySlim<AxisRange?> ProductIonRange { get; } = productIonRange;
    public ReactiveCommand DetectPeaksCommand { get; } = detectPeaksCommand;
    public ReactiveCommand AddPeakCommand { get; } = addPeakCommand;
    public ReactiveCommand ResetPeaksCommand { get; } = resetPeaksCommand;
}
