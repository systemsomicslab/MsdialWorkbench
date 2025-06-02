using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using System;
using System.IO;
using System.Text;

namespace CompMs.App.Msdial.Model.Export;

internal sealed class AnnotationResultExportModel : BindableBase, IAlignmentResultExportModel
{
    private readonly AlignmentPeakSpotSupplyer _peakSpotSupplyer;
    private readonly IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> _refer;

    public AnnotationResultExportModel(AlignmentPeakSpotSupplyer peakSpotSupplyer, IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer) {
        _peakSpotSupplyer = peakSpotSupplyer;
        _refer = refer;
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
        if (!IsSelected) {
            return;
        }

        var filename = $"AlignmentAnnotationResult_{alignmentFile.FileName}_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}.csv";
        notification?.Invoke($"Export {filename}");
        var spots = _peakSpotSupplyer.Supply(alignmentFile, default); // TODO: cancellation
        using var writer = new StreamWriter(Path.Combine(exportDirectory, filename), false, Encoding.UTF8);
        writer.WriteLine(
            string.Join(",",
            [
                "spot.MasterAlignmentID",
                "result.Name",
                "reference?.Name ?? string.Empty",
                "result.InChIKey",
                "result.TotalScore",
                "result.WeightedDotProduct",
                "result.SimpleDotProduct",
                "result.ReverseDotProduct",
                "result.MatchedPeaksCount",
                "result.MatchedPeaksPercentage",
                "result.EssentialFragmentMatchedScore",
                "result.AndromedaScore",
                "result.PEPScore",
                "result.RtSimilarity",
                "result.RiSimilarity",
                "result.CcsSimilarity",
                "result.IsotopeSimilarity",
                "result.AcurateMassSimilarity",
                "result.IsPrecursorMzMatch",
                "result.IsSpectrumMatch",
                "result.IsRtMatch",
                "result.IsRiMatch",
                "result.IsCcsMatch",
                "result.IsLipidClassMatch",
                "result.IsLipidChainsMatch",
                "result.IsLipidPositionMatch",
                "result.IsLipidDoubleBondPositionMatch",
                "result.IsOtherLipidMatch",
                "result.Source",
                "result.AnnotatorID",
                "result.SpectrumID",
                "result.IsDecoy",
                "result.Priority",
                "result.IsReferenceMatched",
                "result.IsAnnotationSuggested",
                "result.CollisionEnergy",
                "reference?.ScanID",
                "reference?.PrecursorMz",
                "reference?.ChromXs",
                "reference?.IonMode",
                "reference?.Formula",
                "reference?.Ontology",
                "reference?.SMILES",
                "reference?.InChIKey",
                "reference?.AdductType",
                "reference?.CollisionCrossSection",
                "reference?.QuantMass",
                "reference?.CompoundClass",
                "reference?.Comment",
                "reference?.InstrumentModel",
                "reference?.InstrumentType",
                "reference?.Links",
                "reference?.CollisionEnergy",
                "reference?.DatabaseID",
                "reference?.DatabaseUniqueIdentifier",
                "reference?.Charge",
                "reference?.MsLevel",
                "reference?.RetentionTimeTolerance",
                "reference?.MassTolerance",
                "reference?.MinimumPeakHeight",
                "reference?.IsTargetMolecule",
                "reference?.FragmentationCondition",
                ]
            ));
        foreach (var spot in spots) {
            foreach (var result in spot.MatchResults.MatchResults) {
                if (result is null || result.IsUnknown) {
                    continue;
                }
                var reference = _refer.Refer(result);
                writer.WriteLine(
                    string.Join(",", [
                        spot.MasterAlignmentID,
                        result.Name,
                        reference?.Name ?? string.Empty,
                        result.InChIKey,
                        result.TotalScore,
                        result.WeightedDotProduct,
                        result.SimpleDotProduct,
                        result.ReverseDotProduct,
                        result.MatchedPeaksCount,
                        result.MatchedPeaksPercentage,
                        result.EssentialFragmentMatchedScore,
                        result.AndromedaScore,
                        result.PEPScore,
                        result.RtSimilarity,
                        result.RiSimilarity,
                        result.CcsSimilarity,
                        result.IsotopeSimilarity,
                        result.AcurateMassSimilarity,
                        result.IsPrecursorMzMatch,
                        result.IsSpectrumMatch,
                        result.IsRtMatch,
                        result.IsRiMatch,
                        result.IsCcsMatch,
                        result.IsLipidClassMatch,
                        result.IsLipidChainsMatch,
                        result.IsLipidPositionMatch,
                        result.IsLipidDoubleBondPositionMatch,
                        result.IsOtherLipidMatch,
                        result.Source,
                        result.AnnotatorID,
                        result.SpectrumID,
                        result.IsDecoy,
                        result.Priority,
                        result.IsReferenceMatched,
                        result.IsAnnotationSuggested,
                        result.CollisionEnergy,
                        reference?.ScanID,
                        reference?.PrecursorMz,
                        reference?.ChromXs,
                        reference?.IonMode,
                        reference?.Formula,
                        reference?.Ontology,
                        reference?.SMILES,
                        reference?.InChIKey,
                        reference?.AdductType,
                        reference?.CollisionCrossSection,
                        reference?.QuantMass,
                        reference?.CompoundClass,
                        reference?.Comment,
                        reference?.InstrumentModel,
                        reference?.InstrumentType,
                        reference?.Links,
                        reference?.CollisionEnergy,
                        reference?.DatabaseID,
                        reference?.DatabaseUniqueIdentifier,
                        reference?.Charge,
                        reference?.MsLevel,
                        reference?.RetentionTimeTolerance,
                        reference?.MassTolerance,
                        reference?.MinimumPeakHeight,
                        reference?.IsTargetMolecule,
                        reference?.FragmentationCondition,
                    ])
                );
            }
        }
    }
}
