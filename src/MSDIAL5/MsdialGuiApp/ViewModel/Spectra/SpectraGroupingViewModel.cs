using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Spectra;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;

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
        SelectedReference = model.ToReactivePropertySlimAsSynchronized(m => m.SelectedReference).AddTo(Disposables);
        ProductIonAbundances = model.ObserveProperty(m => m.ProductIonAbundances).ToReadOnlyReactivePropertySlim(initialValue: []).AddTo(Disposables);
        SelectedSample = model.ToReactivePropertySlimAsSynchronized(m => m.SelectedSample).AddTo(Disposables);
        MeasuredSpectra = model.ObserveProperty(m => m.MeasuredSpectra).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
    }

    public ReadOnlyObservableCollection<MoleculeGroupModel> MoleculeGroups { get; }
    public ReactivePropertySlim<MoleculeGroupModel?> SelectedMoleculeGroup { get; }
    public ReactivePropertySlim<MoleculeMsReference?> SelectedReference { get; }

    public ReadOnlyReactivePropertySlim<GroupProductIonAbundancesModel[]> ProductIonAbundances { get; }
    public AnalysisFileBeanModel[] Samples => _model.Samples;
    public ReactivePropertySlim<AnalysisFileBeanModel?> SelectedSample { get; }
    public ReadOnlyReactivePropertySlim<MsSpectrum?> MeasuredSpectra { get; }
}
