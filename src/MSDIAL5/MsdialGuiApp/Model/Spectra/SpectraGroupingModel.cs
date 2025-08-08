using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Spectra;

public sealed class SpectraGroupingModel : BindableBase
{
    private readonly AlignmentSpotPropertyModel _spot;
    private readonly IMSScanProperty?[] _scans;
    private readonly IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> _refer;
    private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;
    private readonly double _mzTolerance;
    private readonly Ms2Quantifier _quantifier;

    public SpectraGroupingModel(AnalysisFileBeanModelCollection fileCollection, AlignmentSpotPropertyModel spot, IMSScanProperty?[] scans, IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer, IMatchResultEvaluator<MsScanMatchResult> evaluator, double mzTolerance) {
        Samples = [.. fileCollection.AnalysisFiles];
        _spot = spot;
        _scans = scans;
        _refer = refer;
        _evaluator = evaluator;
        _mzTolerance = mzTolerance;
        _quantifier = new Ms2Quantifier();

        SelectedSample = Samples.FirstOrDefault();
    }

    public ObservableCollection<MoleculeGroupModel> MoleculeGroups { get; } = [];
    public MoleculeGroupModel? SelectedMoleculeGroup {
        get => _selectedMoleculeGroup;
        set {
            if (SetProperty(ref _selectedMoleculeGroup, value)) {
                if (_selectedMoleculeGroup?.References is not { Length: > 0 }) {
                    ProductIonAbundances = [];
                    SelectedReference = null;
                    return;
                }
                var references = _selectedMoleculeGroup.References;
                SelectedReference = references[0];

                var quantResults = _quantifier.Quantify(_selectedMoleculeGroup.UniqueMzList, _scans, Samples, _mzTolerance);
                ProductIonAbundances = quantResults.Zip(_selectedMoleculeGroup.UniqueMzList,
                    (r, mz) => new GroupProductIonAbundancesModel {
                        Abundances = r.Abundances.Select(
                            abundance => new ProductIonAbundanceModel {
                                SampleName = abundance.SampleName,
                                Intensity = abundance.Abundance,
                            }
                        ).ToArray(),
                        Mz = mz,
                    }
                ).ToArray();
            }
        }
    }
    private MoleculeGroupModel? _selectedMoleculeGroup;

    public MoleculeMsReference? SelectedReference {
        get => _selectedReference;
        set => SetProperty(ref _selectedReference, value);
    }
    private MoleculeMsReference? _selectedReference;

    public GroupProductIonAbundancesModel[] ProductIonAbundances {
        get => _productIonAbundances;
        private set => SetProperty(ref _productIonAbundances, value);
    }
    private GroupProductIonAbundancesModel[] _productIonAbundances = [];

    public AnalysisFileBeanModel[] Samples { get; } = [];
    public AnalysisFileBeanModel? SelectedSample {
        get => _selectedSample;
        set {
            if (SetProperty(ref _selectedSample, value)) {
                if (_selectedSample is null) {
                    MeasuredSpectra = null;
                    return;
                }
                var spectrum = _scans[_selectedSample.AnalysisFileId]?.Spectrum;
                MeasuredSpectra = spectrum is not null ? new MsSpectrum(spectrum) : null;
            }
        }
    }
    private AnalysisFileBeanModel? _selectedSample;

    public MsSpectrum? MeasuredSpectra {
        get => _measuredSpectra;
        set => SetProperty(ref _measuredSpectra, value);
    }
    private MsSpectrum? _measuredSpectra;

    public async Task UpdateMoleculeGroupsAsync(CancellationToken token = default) {
        var mapper = new MzMapper();

        var task = _spot.AlignedPeakPropertiesModelProperty.ToTask();
        var peaks = _spot.AlignedPeakPropertiesModelProperty.Value ?? await task;

        var referencePairs = peaks.SelectMany(p => p.MatchResults.MatchResults.Where(_evaluator.IsReferenceMatched).Select(r => (_refer.Refer(r), r))).OfType<(MoleculeMsReference, MsScanMatchResult)>();
        var reference2Score = referencePairs.GroupBy(pair => pair.Item1, pair => pair.Item2.TotalScore).ToDictionary(g => g.Key, g => g.Max());
        var references = referencePairs.Select(pair => pair.Item1).ToArray();
        var groups = mapper.MapMzByReferenceGroups(references, _mzTolerance)
            .Select(pair => new MoleculeGroupModel {
                Name = string.Join(",", pair.Item1.Select(r => r.Name)),
                References = pair.Item1,
                UniqueMzList = pair.Item2,
            }).OrderByDescending(g => g.References.Max(r => reference2Score[r])).ThenBy(g => g.References.Length);

        token.ThrowIfCancellationRequested();
        MoleculeGroups.Clear();
        foreach (var group in groups) {
            MoleculeGroups.Add(group);
        }
        SelectedMoleculeGroup = MoleculeGroups.FirstOrDefault();
    }
}

/// <summary>
/// Model for a molecule group.
/// </summary>
public class MoleculeGroupModel {
    public string Name { get; set; } = string.Empty;
    public double[] UniqueMzList { get; set; } = [];
    public MoleculeMsReference[] References { get; set; } = [];
}

/// <summary>
/// Model for product ion abundance (for chart/table).
/// </summary>
public class ProductIonAbundanceModel {
    public string SampleName { get; set; } = string.Empty;
    public double Intensity { get; set; }
}

public class GroupProductIonAbundancesModel {
    public ProductIonAbundanceModel[] Abundances { get; set; } = [];
    public double Mz { get; set; } = 0d;
}
