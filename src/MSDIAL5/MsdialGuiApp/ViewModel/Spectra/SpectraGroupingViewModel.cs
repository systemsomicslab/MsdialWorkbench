using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Spectra; 
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Spectra; 

/// <summary>
/// ViewModel for Spectra Grouping View.
/// </summary>
public class SpectraGroupingViewModel : ViewModelBase {
    private readonly SpectraGroupingModel _model;

    public SpectraGroupingViewModel(SpectraGroupingModel model) {
        _model = model;
        MoleculeGroups = new ReadOnlyObservableCollection<MoleculeGroupModel>(model.MoleculeGroups);
        SelectedMoleculeGroup = model.ToReactivePropertySlimAsSynchronized(m => m.SelectedMoleculeGroup).AddTo(Disposables);
        ProductIonAbundances = model.ObserveProperty(m => m.ProductIonAbundances).ToReadOnlyReactivePropertySlim(initialValue: []).AddTo(Disposables);
        TheoreticalSpectra = model.ObserveProperty(m => m.TheoreticalSpectra).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        SelectedSample = model.ToReactivePropertySlimAsSynchronized(m => m.SelectedSample).AddTo(Disposables);
        MeasuredSpectra = model.ObserveProperty(m => m.MeasuredSpectra).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

        TheoreticalHorizontalAxis = TheoreticalSpectra.Select(s => s?.Spectrum.Select(p => p.Mass).DefaultIfEmpty() ?? [0d])
            .Select(mzs => (mzs.Min(), mzs.Max()))
            .ToReactiveContinuousAxisManager(new ConstantMargin(20d)).AddTo(Disposables);
        TheoreticalVerticalAxis = TheoreticalSpectra.Select(s => s?.Spectrum.Select(p => p.Intensity).DefaultIfEmpty() ?? [0d])
            .Select(ints => (ints.Min(), ints.Max()))
            .ToReactiveContinuousAxisManager(new ConstantMargin(0d, 20d), new AxisRange(0d, 0d)).AddTo(Disposables);
        MeasuredHorizontalAxis = MeasuredSpectra.Select(s => s?.Spectrum.Select(p => p.Mass).DefaultIfEmpty() ?? [0d])
            .Select(mzs => (mzs.Min(), mzs.Max()))
            .ToReactiveContinuousAxisManager(new ConstantMargin(20d)).AddTo(Disposables);
        MeasuredVerticalAxis = MeasuredSpectra.Select(s => s?.Spectrum.Select(p => p.Intensity).DefaultIfEmpty() ?? [0d])
            .Select(ints => (ints.Min(), ints.Max()))
            .ToReactiveContinuousAxisManager(new ConstantMargin(0d, 20d), new AxisRange(0d, 0d)).AddTo(Disposables);
    }

    public ReadOnlyObservableCollection<MoleculeGroupModel> MoleculeGroups { get; }
    public ReactivePropertySlim<MoleculeGroupModel?> SelectedMoleculeGroup { get; }

    public ReadOnlyReactivePropertySlim<ProductIonAbundanceModel[][]> ProductIonAbundances { get; }
    public ReadOnlyReactivePropertySlim<MsSpectrum?> TheoreticalSpectra { get; }
    public AnalysisFileBeanModel[] Samples => _model.Samples;
    public ReactivePropertySlim<AnalysisFileBeanModel?> SelectedSample { get; }
    public ReadOnlyReactivePropertySlim<MsSpectrum?> MeasuredSpectra { get; }

    public IAxisManager<double> TheoreticalHorizontalAxis { get; }
    public IAxisManager<double> TheoreticalVerticalAxis { get; }
    public IAxisManager<double> MeasuredHorizontalAxis { get; }
    public IAxisManager<double> MeasuredVerticalAxis { get; }
}
