using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Utility
{
    public static class DataObjConverter {
        public static void SetAlignmentChromPeakFeatureFromSpectrumFeature(AlignmentChromPeakFeature alignmentPeak, SpectrumFeature spectrum, ChromXType mainType) {
            var scan = spectrum.AnnotatedMSDecResult.MSDecResult;
            var peak = spectrum.QuantifiedChromatogramPeak;
            var molecule = spectrum.AnnotatedMSDecResult.Molecule;
            alignmentPeak.MasterPeakID = scan.ScanID;
            alignmentPeak.PeakID = scan.ScanID;
            alignmentPeak.SeekPointToDCLFile = scan.SeekPoint;
            alignmentPeak.MS1RawSpectrumID = scan.RawSpectrumID;
            alignmentPeak.MS1RawSpectrumIdTop = peak.MS1RawSpectrumIdTop;
            alignmentPeak.MS1RawSpectrumIdLeft = peak.MS1RawSpectrumIdLeft;
            alignmentPeak.MS1RawSpectrumIdRight = peak.MS1RawSpectrumIdRight;
            alignmentPeak.ChromXsTop = peak.PeakFeature.ChromXsTop;
            alignmentPeak.ChromXsTop.MainType = mainType;
            alignmentPeak.ChromXsLeft = peak.PeakFeature.ChromXsLeft;
            alignmentPeak.ChromXsLeft.MainType = mainType;
            alignmentPeak.ChromXsRight = peak.PeakFeature.ChromXsRight;
            alignmentPeak.ChromXsRight.MainType = mainType;
            alignmentPeak.PeakHeightTop = peak.PeakFeature.PeakHeightTop;
            alignmentPeak.PeakHeightLeft = peak.PeakFeature.PeakHeightLeft;
            alignmentPeak.PeakHeightRight = peak.PeakFeature.PeakHeightRight;
            alignmentPeak.PeakAreaAboveZero = peak.PeakFeature.PeakAreaAboveZero;
            alignmentPeak.PeakAreaAboveBaseline = peak.PeakFeature.PeakAreaAboveBaseline;
            alignmentPeak.PeakShape = peak.PeakShape;
            alignmentPeak.Mass = spectrum.AnnotatedMSDecResult.QuantMass;
            alignmentPeak.IonMode = scan.IonMode;
            alignmentPeak.Name = molecule.Name;
            alignmentPeak.Formula = molecule.Formula;
            alignmentPeak.Ontology = molecule.Ontology;
            alignmentPeak.SMILES = molecule.SMILES;
            alignmentPeak.InChIKey = molecule.InChIKey;
            alignmentPeak.MSDecResultIdUsed = scan.ScanID;

            alignmentPeak.MatchResults.ClearResults();
            alignmentPeak.MatchResults.ClearMspResults();
            alignmentPeak.MatchResults.ClearTextDbResults();
            alignmentPeak.MSRawID2MspIDs = new Dictionary<int, List<int>> { [scan.RawSpectrumID] = scan.MspIDs };
            alignmentPeak.MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult> { [scan.RawSpectrumID] = scan.MspBasedMatchResult };
            alignmentPeak.MatchResults.MergeContainers(spectrum.AnnotatedMSDecResult.MatchResults);
        }

        public static void SetAlignmentChromPeakFeatureFromChromatogramPeakFeature(AlignmentChromPeakFeature alignmentPeak, ChromatogramPeakFeature peak) {
            alignmentPeak.MasterPeakID = peak.MasterPeakID;
            alignmentPeak.PeakID = peak.PeakID;
            alignmentPeak.ParentPeakID = peak.ParentPeakID;
            alignmentPeak.SeekPointToDCLFile = peak.SeekPointToDCLFile;
            alignmentPeak.MS1RawSpectrumID = peak.ScanID;
            alignmentPeak.MS1RawSpectrumIDatAccumulatedMS1 = peak.MS1AccumulatedMs1RawSpectrumIdTop;
            alignmentPeak.MS2RawSpectrumID = peak.MS2RawSpectrumID;
            alignmentPeak.MS2RawSpectrumID2CE = peak.MS2RawSpectrumID2CE;
            alignmentPeak.ChromScanIdLeft = peak.PeakFeature.ChromScanIdLeft;
            alignmentPeak.ChromScanIdRight = peak.PeakFeature.ChromScanIdRight;
            alignmentPeak.ChromScanIdTop = peak.PeakFeature.ChromScanIdTop;
            alignmentPeak.MS1RawSpectrumIdTop = peak.MS1RawSpectrumIdTop;
            alignmentPeak.MS1RawSpectrumIdLeft = peak.MS1RawSpectrumIdLeft;
            alignmentPeak.MS1RawSpectrumIdRight = peak.MS1RawSpectrumIdRight;
            alignmentPeak.MS1AccumulatedMs1RawSpectrumIdTop = peak.MS1AccumulatedMs1RawSpectrumIdTop;
            alignmentPeak.MS1AccumulatedMs1RawSpectrumIdLeft = peak.MS1AccumulatedMs1RawSpectrumIdLeft;
            alignmentPeak.MS1AccumulatedMs1RawSpectrumIdRight = peak.MS1AccumulatedMs1RawSpectrumIdRight;
            alignmentPeak.ChromXsLeft = peak.PeakFeature.ChromXsLeft;
            alignmentPeak.ChromXsTop = peak.PeakFeature.ChromXsTop;
            alignmentPeak.ChromXsRight = peak.PeakFeature.ChromXsRight;
            alignmentPeak.PeakHeightLeft = peak.PeakFeature.PeakHeightLeft;
            alignmentPeak.PeakHeightTop = peak.PeakFeature.PeakHeightTop;
            alignmentPeak.PeakHeightRight = peak.PeakFeature.PeakHeightRight;
            alignmentPeak.PeakAreaAboveZero = peak.PeakFeature.PeakAreaAboveZero;
            alignmentPeak.PeakAreaAboveBaseline = peak.PeakFeature.PeakAreaAboveBaseline;
            alignmentPeak.Mass = peak.PeakFeature.Mass;
            alignmentPeak.IonMode = peak.IonMode;
            alignmentPeak.Name = peak.Name;
            alignmentPeak.Protein = peak.Protein;
            alignmentPeak.ProteinGroupID = peak.ProteinGroupID;
            alignmentPeak.Formula = peak.Formula;
            alignmentPeak.Ontology = peak.Ontology;
            alignmentPeak.SMILES = peak.SMILES;
            alignmentPeak.InChIKey = peak.InChIKey;
            alignmentPeak.CollisionCrossSection = peak.CollisionCrossSection;
            alignmentPeak.MSDecResultIdUsed = peak.MSDecResultIdUsed;

            alignmentPeak.MatchResults.ClearResults();
            alignmentPeak.MatchResults.ClearMspResults();
            alignmentPeak.MatchResults.ClearTextDbResults();
            alignmentPeak.MSRawID2MspIDs = peak.MSRawID2MspIDs;
            alignmentPeak.TextDbIDs = peak.TextDbIDs;
            alignmentPeak.MSRawID2MspBasedMatchResult = peak.MSRawID2MspBasedMatchResult;
            alignmentPeak.TextDbBasedMatchResult = peak.TextDbBasedMatchResult;
            alignmentPeak.MatchResults.MergeContainers(peak.MatchResults);

            alignmentPeak.PeakCharacter = peak.PeakCharacter;
            alignmentPeak.PeakShape = peak.PeakShape;
        }

        public static void SetRepresentativeFileID(AlignmentSpotProperty spot) {
            var alignment = spot.AlignedPeakProperties;
            var alignedPeaks = alignment.Where(align => align.PeakID >= 0).ToArray();

            var repId = GetRepresentativeFileID(alignedPeaks);
            var representative = repId >= 0 ? alignment[repId] : alignedPeaks.First();
            spot.RepresentativeFileID = representative.FileID;
        }

        public static void SetRepresentativeProperty(AlignmentSpotProperty spot) {
            var alignment = spot.AlignedPeakProperties;
            var alignedPeaks = alignment.Where(align => align.PeakID >= 0).ToArray();

            var repId = spot.RepresentativeFileID;
            var representative = alignment[repId];
            var chromXType = representative.ChromXsTop.MainType;

            spot.IonMode = representative.IonMode;
            spot.Name = representative.Name;
            spot.Protein = representative.Protein;
            spot.ProteinGroupID = representative.ProteinGroupID;
            spot.Ontology = representative.Ontology;
            spot.SMILES = representative.SMILES;
            spot.InChIKey = representative.InChIKey;
            spot.SetAdductType(Common.DataObj.Property.AdductIon.GetAdductIon(representative.PeakCharacter.AdductType.AdductIonName));
            spot.PeakCharacter = new IonFeatureCharacter
            {
                AdductType = spot.AdductType,
                AdductTypeByAmalgamationProgram = representative.PeakCharacter.AdductTypeByAmalgamationProgram, 
                Charge = representative.PeakCharacter.Charge,
                IsotopeWeightNumber = representative.PeakCharacter.IsotopeWeightNumber,
                IsotopeParentPeakID = representative.PeakCharacter.IsotopeParentPeakID,
                PeakGroupID = representative.PeakCharacter.PeakGroupID,
                IsLinked = representative.PeakCharacter.IsLinked,
                AdductParent = representative.PeakCharacter.AdductParent,
            };
            spot.Formula = representative.Formula;

            spot.CollisionCrossSection = representative.CollisionCrossSection;
            spot.MSRawID2MspIDs = representative.MSRawID2MspIDs;
            spot.TextDbIDs = new List<int>(representative.TextDbIDs);
            spot.MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>(representative.MSRawID2MspBasedMatchResult);
            spot.TextDbBasedMatchResult = representative.TextDbBasedMatchResult;
            spot.MatchResults.MergeContainers(representative.MatchResults);
            spot.MSDecResultIdUsed = representative.MSDecResultIdUsed;

            spot.HeightAverage = (float)alignedPeaks.Average(peak => peak.PeakHeightTop);
            spot.HeightMax = (float)alignedPeaks.Max(peak => peak.PeakHeightTop);
            spot.HeightMin = (float)alignedPeaks.Min(peak => peak.PeakHeightTop);
            spot.PeakWidthAverage = (float)alignedPeaks.Average(peak => peak.PeakWidth(chromXType));

            spot.SignalToNoiseAve = alignedPeaks.Average(peak => peak.PeakShape.SignalToNoise);
            spot.SignalToNoiseMax = alignedPeaks.Max(peak => peak.PeakShape.SignalToNoise);
            spot.SignalToNoiseMin = alignedPeaks.Min(peak => peak.PeakShape.SignalToNoise);

            spot.EstimatedNoiseAve = alignedPeaks.Average(peak => peak.PeakShape.EstimatedNoise);
            spot.EstimatedNoiseMax = alignedPeaks.Max(peak => peak.PeakShape.EstimatedNoise);
            spot.EstimatedNoiseMin = alignedPeaks.Min(peak => peak.PeakShape.EstimatedNoise);

            spot.TimesMin = alignedPeaks.Argmin(peak => peak.ChromXsTop.Value).ChromXsTop;
            spot.TimesMax = alignedPeaks.Argmax(peak => peak.ChromXsTop.Value).ChromXsTop;

            spot.MassCenter = alignedPeaks.Argmax(peak => peak.PeakHeightTop).Mass;
            spot.MassMin = (float)alignedPeaks.Min(peak => peak.Mass);
            spot.MassMax = (float)alignedPeaks.Max(peak => peak.Mass);

            spot.FillParcentage = (float)alignedPeaks.Length / alignment.Count;
            spot.MonoIsotopicPercentage = alignedPeaks.Count(peak => peak.PeakCharacter.IsotopeWeightNumber == 0) / (float)alignedPeaks.Length;
            spot.TimesCenter = new ChromXs() {
                MainType = chromXType,
                RT = new RetentionTime(alignedPeaks.Average(peak => peak.ChromXsTop.RT.Value), representative.ChromXsTop.RT.Unit),
                RI = new RetentionIndex(alignedPeaks.Select(peak => peak.ChromXsTop.RI.Value).Where(v => v >= 0).DefaultIfEmpty().Average(), representative.ChromXsTop.RI.Unit),
                Mz = new MzValue(spot.MassCenter, representative.ChromXsTop.Mz.Unit),
                Drift = new DriftTime(alignedPeaks.Average(peak => peak.ChromXsTop.Drift.Value), representative.ChromXsTop.Drift.Unit),
            };

            if (spot.QuantMass > 0) {
                spot.MassCenter = spot.QuantMass;
            }
        }

        /// <summary>
        /// The peak whose file represents an alignment spot: the spot takes its name, its annotation
        /// candidates, its MS/MS spectrum and its isotope pattern from this peak.
        /// </summary>
        /// <remarks>
        /// The order, best first, is <see cref="CompareRepresentativePrecedence"/>: a peak with a
        /// product-ion spectrum, then the precedence of each peak's own representative annotation,
        /// then peak height. The first peak in the list wins a complete tie, as before.
        /// </remarks>
        public static AlignmentChromPeakFeature? GetRepresentativePeak(IReadOnlyList<AlignmentChromPeakFeature> alignment) {
            if (alignment.Count == 0) return null;
            return alignment.Argmax(RepresentativePrecedence);
        }

        /// <summary>
        /// Which of two aligned peaks has the stronger claim to represent their spot: positive when
        /// <paramref name="x"/> does, negative when <paramref name="y"/> does, 0 for a complete tie.
        /// </summary>
        /// <remarks>
        /// Three keys, in this order.
        ///
        /// 1. Whether the peak carries a product-ion spectrum. Unchanged: a peak with one has always
        ///    been preferred over every peak without one, whatever their scores. It stays above the
        ///    manual modification of the next key, as it was above the score: the spot's MS/MS
        ///    spectrum comes from the representative file.
        ///
        /// 2. The precedence of the peak's own representative annotation -- the candidate
        ///    <see cref="MsScanMatchResultContainer.Representative"/> already chose for that peak --
        ///    by <see cref="MsScanMatchResultContainer.ResultOrder"/>, the same key that chose it:
        ///    manual modification, reference match, suggestion, evidence rank, annotator priority,
        ///    total score. This is the author's decision of 2026-10-28 (D-A), for every project:
        ///    first whether the annotation is reference-matched, then the annotator's tier, then the
        ///    score. It replaces the highest total score over ALL of a peak's candidates, which let a
        ///    candidate that did not represent its own peak decide which file represented the spot:
        ///    adding an in-silico library moved a spot to the file whose in-silico candidate scored
        ///    highest, and the spot then showed that file's acquired-library name. A candidate can now
        ///    only influence this choice through the peak it represents.
        ///
        ///    A peak with no candidate at all reports the synthetic unknown result. That placeholder is
        ///    not a candidate and loses this key to any real one, for the reason
        ///    <see cref="AnnotationEvidence.RankOf"/> gives; without that it would take the neutral
        ///    evidence rank and could outrank a real candidate whose spectrum was compared and failed.
        ///    An unknown a person set is a real result: it is manually modified and is compared as such.
        ///
        /// 3. Peak height, higher first. Unchanged.
        ///
        /// Where a modality has no tiers -- one annotator, no evidence record, or no annotation at all
        /// -- the second key reduces to the representative's verdicts and total score, and with no
        /// candidates anywhere to peak height alone, as before.
        /// </remarks>
        public static int CompareRepresentativePrecedence(AlignmentChromPeakFeature x, AlignmentChromPeakFeature y) {
            return RepresentativePrecedence(x).CompareTo(RepresentativePrecedence(y));
        }

        private static (bool, Tuple<bool, bool, bool, int, int, float>, double) RepresentativePrecedence(AlignmentChromPeakFeature peak) {
            return (HasProductIonSpectrum(peak), AnnotationPrecedence(peak), peak.PeakHeightTop);
        }

        private static bool HasProductIonSpectrum(AlignmentChromPeakFeature peak) {
            return !peak.MS2RawSpectrumID2CE.IsEmptyOrNull();
        }

        private static readonly Tuple<bool, bool, bool, int, int, float> NoCandidate
            = Tuple.Create(false, false, false, int.MinValue, int.MinValue, float.MinValue);

        private static Tuple<bool, bool, bool, int, int, float> AnnotationPrecedence(AlignmentChromPeakFeature peak) {
            var representative = peak.MatchResults?.Representative;
            if (representative is null || (representative.IsUnknown && !representative.IsManuallyModified)) {
                return NoCandidate;
            }
            return MsScanMatchResultContainer.ResultOrder(representative);
        }

        public static int GetRepresentativeFileID(IReadOnlyList<AlignmentChromPeakFeature> alignment) {
            return GetRepresentativePeak(alignment)?.FileID ?? -1;
        }
    }
}
