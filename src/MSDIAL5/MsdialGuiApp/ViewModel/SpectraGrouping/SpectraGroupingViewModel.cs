using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.ViewModel.SpectraGrouping; 

/// <summary>
/// ViewModel for Spectra Grouping View.
/// </summary>
public class SpectraGroupingViewModel : ViewModelBase {
    public ObservableCollection<MoleculeGroupModel> MoleculeGroups { get; } = [];
    public ReactivePropertySlim<MoleculeGroupModel?> SelectedMoleculeGroup { get; } = new();

    public ObservableCollection<double> MzList { get; } = [];
    public ReactivePropertySlim<double> SelectedMz { get; } = new();

    public ReadOnlyReactivePropertySlim<ProductIonAbundanceModel[][]> ProductIonAbundances { get; } = default!;
    public ReadOnlyReactivePropertySlim<MsSpectrum?> TheoreticalSpectra { get; } = default!;
    public AnalysisFileBeanModel[] Samples { get; } = [];
    public ReactivePropertySlim<AnalysisFileBean?> SelectedSample { get; } = default!;
    public ReadOnlyReactivePropertySlim<MsSpectrum?> MeasuredSpectra { get; } = default!;
}

/// <summary>
/// Model for a molecule group.
/// </summary>
public class MoleculeGroupModel {
    public string Name { get; set; } = string.Empty;
    public ObservableCollection<double> UniqueMzList { get; } = new();
    public MoleculeMsReference[] References { get; } = [];
}

/// <summary>
/// Model for product ion abundance (for chart/table).
/// </summary>
public class ProductIonAbundanceModel {
    public string SampleName { get; set; } = string.Empty;
    public double Intensity { get; set; }
}
