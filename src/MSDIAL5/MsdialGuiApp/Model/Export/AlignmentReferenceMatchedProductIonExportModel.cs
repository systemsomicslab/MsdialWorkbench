using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Spectra;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Export;

internal sealed class AlignmentReferenceMatchedProductIonExportModel : BindableBase, IAlignmentResultExportModel
{
    private readonly AlignmentPeakSpotSupplyer _peakSpotSupplyer;
    private readonly AnalysisFileBeanModelCollection _fileCollection;
    private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;
    private readonly DataBaseMapper _dataBaseMapper;
    private readonly TargetOmics _omics;

    public AlignmentReferenceMatchedProductIonExportModel(AlignmentPeakSpotSupplyer peakSpotSupplyer, AnalysisFileBeanModelCollection fileCollection, IMatchResultEvaluator<MsScanMatchResult> evaluator, DataBaseMapper dataBaseMapper, TargetOmics omics) {
        _peakSpotSupplyer = peakSpotSupplyer;
        _fileCollection = fileCollection;
        _evaluator = evaluator;
        _dataBaseMapper = dataBaseMapper;
        _omics = omics;
    }

    public bool IsSelected {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    private bool _isSelected = false;

    public int CountExportFiles(AlignmentFileBeanModel alignmentFile) {
        return IsSelected ? 1 : 0;
    }

    public void Export(AlignmentFileBeanModel alignmentFile, string exportDirectory, Action<string> notification) {
        Task.Run(() => ExportAsync(alignmentFile, exportDirectory, notification)).Wait();
    }

    public async Task ExportAsync(AlignmentFileBeanModel alignmentFile, string exportDirectory, Action<string> notification, CancellationToken token = default) {
        var alignmentPeaksSpectraLoader = new AlignmentPeaksSpectraLoader(_fileCollection);
        var mapper = new MzMapper();
        var quantifier = new Ms2Quantifier();
        AnalysisFileBeanModel[] samples = [.. _fileCollection.AnalysisFiles];
        double mzTolerance = .05d;
        var spots = _peakSpotSupplyer.Supply(alignmentFile, token).Select(s => new AlignmentSpotPropertyModel(s));

        var objects = new List<object>();
        foreach (var spot in spots) {
            token.ThrowIfCancellationRequested();
            if (!spot.IsRefMatched(_evaluator)) {
                continue;
            }

            token.ThrowIfCancellationRequested();
            var scans = await alignmentPeaksSpectraLoader.GetCurrentScansAsync(_fileCollection.AnalysisFiles, spot).ConfigureAwait(false);
            if (scans is null) {
                continue;
            }

            token.ThrowIfCancellationRequested();

            var task = spot.AlignedPeakPropertiesModelProperty.ToTask();
            var peaks = spot.AlignedPeakPropertiesModelProperty.Value ?? await task;

            var referencePairs = peaks.SelectMany(p => p.MatchResults.MatchResults.Where(_evaluator.IsReferenceMatched).Select(r => (_dataBaseMapper.MoleculeMsRefer(r), r))).OfType<(MoleculeMsReference, MsScanMatchResult)>().ToArray();
            if (_omics == TargetOmics.Lipidomics) {
                referencePairs = referencePairs.Where(pair => pair.Item2.MatchedPeaksPercentage >= 2d).ToArray();
            } 

            var reference2Score = referencePairs.GroupBy(pair => pair.Item1, pair => pair.Item2.TotalScore).ToDictionary(g => g.Key, g => g.Max());
            var references = referencePairs.Select(pair => pair.Item1).ToArray();
            var groups = mapper.MapMzByReferenceGroups(references, mzTolerance)
                .Select(pair => new MoleculeGroupModel {
                    Name = string.Join(",", pair.Item1.Select(r => r.Name)),
                    References = pair.Item1,
                    UniqueMzList = pair.Item2,
                }).OrderByDescending(g => g.References.Max(r => reference2Score[r])).ThenBy(g => g.References.Length);

            var groupObjects = new List<object>();
            foreach (var group in groups) {
                token.ThrowIfCancellationRequested();
                var quantResults = quantifier.Quantify(group.UniqueMzList, scans, samples, mzTolerance);
                var productIonAbundances = quantResults.Select(
                    r => new GroupProductIonAbundancesModel {
                        Abundances = r.Abundances.Select(
                            abundance => new ProductIonAbundanceModel {
                                SampleName = abundance.SampleName,
                                Intensity = abundance.Abundance,
                            }
                        ).ToArray(),
                        Mz = r.QuantMass,
                    }
                ).ToArray();
                groupObjects.Add(new
                {
                    ReferenceGroup = group.References.Select(r => r.Name).ToArray(),
                    UniqueIonMz = group.UniqueMzList,
                    Abundances = productIonAbundances,
                });
            }

            var ontology = _dataBaseMapper.MoleculeMsRefer(spot.MatchResultsModel.Representative)?.OntologyOrCompoundClass;
            objects.Add(new
            {
                MasterAlignemntID = spot.MasterAlignmentID,
                RepresentativeReference = spot.MatchResultsModel.Representative.Name,
                RepresentativeOntology = ontology,
                ProductIonAbundances = scans.Zip(samples, (s, sample) => new {
                    Sample = sample.AnalysisFileName,
                    SumOfProductIonAbundance = s?.Spectrum.Select(p => p.Intensity).DefaultIfEmpty().Sum() ?? 0d
                }).ToList(),
                References = reference2Score.Select(kvp => new { name = kvp.Key.Name, score = kvp.Value, ontology = kvp.Key.OntologyOrCompoundClass }).ToArray(),
                Groups = groupObjects,
            });
        }

        var dt = DateTime.Now;
        var outFilePath = Path.Combine(exportDirectory, $"{dt:yyyy_MM_dd_HH_mm_ss}_{alignmentFile.FileName}.json");
        using var writer = new StreamWriter(outFilePath);
        await writer.WriteAsync(Newtonsoft.Json.JsonConvert.SerializeObject(objects, Newtonsoft.Json.Formatting.Indented)).ConfigureAwait(false);
    }
}
