using CompMs.App.Msdial.Model.MsResult;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.MsResult;

internal sealed class AccumulatedMs2SpectrumViewModel : ViewModelBase
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
    }

    public AccumulatedMs2SpectrumModel Model { get; }

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
}
