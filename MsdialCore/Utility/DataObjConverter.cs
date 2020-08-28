using System;
using System.Collections.Generic;
using System.Linq;

using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.MsdialCore.DataObj;

namespace CompMs.MsdialCore.Utility
{
    public class DataObjConverter
    {
        public static AlignmentChromPeakFeature ConvertToAlignmentChromPeakFeature(ChromatogramPeakFeature peak) {
            return new AlignmentChromPeakFeature {
                MasterPeakID = peak.MasterPeakID,
                PeakID = peak.PeakID,
                ParentPeakID = peak.ParentPeakID,
                SeekPointToDCLFile = peak.SeekPointToDCLFile,
                MS1RawSpectrumID = peak.ScanID,
                MS1RawSpectrumIDatAccumulatedMS1 = peak.MS1AccumulatedMs1RawSpectrumIdTop,
                MS2RawSpectrumID = peak.MS2RawSpectrumID,
                MS2RawSpectrumID2CE = peak.MS2RawSpectrumID2CE,
                ChromScanIdLeft = peak.ChromScanIdLeft,
                ChromScanIdRight = peak.ChromScanIdRight,
                ChromScanIdTop = peak.ChromScanIdTop,
                MS1RawSpectrumIdTop = peak.MS1RawSpectrumIdTop,
                MS1RawSpectrumIdLeft = peak.MS1RawSpectrumIdLeft,
                MS1RawSpectrumIdRight = peak.MS1RawSpectrumIdRight,
                MS1AccumulatedMs1RawSpectrumIdTop = peak.MS1AccumulatedMs1RawSpectrumIdTop,
                MS1AccumulatedMs1RawSpectrumIdLeft = peak.MS1AccumulatedMs1RawSpectrumIdLeft,
                MS1AccumulatedMs1RawSpectrumIdRight = peak.MS1AccumulatedMs1RawSpectrumIdRight,
                ChromXsLeft = peak.ChromXsLeft,
                ChromXsTop = peak.ChromXsTop,
                ChromXsRight = peak.ChromXsRight,
                PeakHeightLeft = peak.PeakHeightLeft,
                PeakHeightTop = peak.PeakHeightTop,
                PeakHeightRight = peak.PeakHeightRight,
                PeakAreaAboveZero = peak.PeakAreaAboveZero,
                PeakAreaAboveBaseline = peak.PeakAreaAboveBaseline,
                Mass = peak.Mass,
                IonMode = peak.IonMode,
                Name = peak.Name,
                Formula = peak.Formula,
                Ontology = peak.Ontology,
                SMILES = peak.SMILES,
                InChIKey = peak.InChIKey,
                CollisionCrossSection = peak.CollisionCrossSection,
                MSRawID2MspIDs = peak.MSRawID2MspIDs,
                TextDbIDs = peak.TextDbIDs,
                MSRawID2MspBasedMatchResult = peak.MSRawID2MspBasedMatchResult,
                TextDbBasedMatchResult = peak.TextDbBasedMatchResult,
                PeakCharacter = peak.PeakCharacter,
                PeakShape = peak.PeakShape,
            };
        }

        public static AlignmentSpotProperty ConvertFeatureToSpot(List<AlignmentChromPeakFeature> alignment) {
            var alignedPeaks = alignment.Where(align => align.PeakID >= 0).ToArray();
            if (alignedPeaks.Length == 0) {
                return new AlignmentSpotProperty();
            }
            var repId = GetRepresentativeFileID(alignment);
            var representative = repId >= 0 ? alignment[repId] : alignedPeaks.First();
            var chromXType = representative.ChromXsTop.MainType;
            var result = new AlignmentSpotProperty() {
                RepresentativeFileID = repId,

                AlignedPeakProperties = alignment,

                PeakCharacter = representative.PeakCharacter,
                IonMode = representative.IonMode,

                Name = representative.Name,
                Formula = representative.Formula,
                Ontology = representative.Ontology,
                SMILES = representative.SMILES,
                InChIKey = representative.InChIKey,

                CollisionCrossSection = representative.CollisionCrossSection,

                MSRawID2MspIDs = representative.MSRawID2MspIDs,
                TextDbIDs = representative.TextDbIDs,
                MSRawID2MspBasedMatchResult = representative.MSRawID2MspBasedMatchResult,
                TextDbBasedMatchResult = representative.TextDbBasedMatchResult,

                HeightAverage = (float)alignedPeaks.Average(peak => peak.PeakHeightTop),
                HeightMax = (float)alignedPeaks.Max(peak => peak.PeakHeightTop),
                HeightMin = (float)alignedPeaks.Min(peak => peak.PeakHeightTop),
                PeakWidthAverage = (float)alignedPeaks.Average(peak => peak.PeakWidth(chromXType)),

                SignalToNoiseAve = alignedPeaks.Average(peak => peak.PeakShape.SignalToNoise),
                SignalToNoiseMax = alignedPeaks.Max(peak => peak.PeakShape.SignalToNoise),
                SignalToNoiseMin = alignedPeaks.Min(peak => peak.PeakShape.SignalToNoise),

                EstimatedNoiseAve = alignedPeaks.Average(peak => peak.PeakShape.EstimatedNoise),
                EstimatedNoiseMax = alignedPeaks.Max(peak => peak.PeakShape.EstimatedNoise),
                EstimatedNoiseMin = alignedPeaks.Min(peak => peak.PeakShape.EstimatedNoise),

                TimesMin = alignedPeaks.Argmin(peak => peak.ChromXsTop.Value).ChromXsTop,
                TimesMax = alignedPeaks.Argmax(peak => peak.ChromXsTop.Value).ChromXsTop,

                MassMin = (float)alignedPeaks.Min(peak => peak.Mass),
                MassMax = (float)alignedPeaks.Max(peak => peak.Mass),

                FillParcentage = (float)alignedPeaks.Length / alignment.Count,
                MonoIsotopicPercentage = alignedPeaks.Count(peak => peak.PeakCharacter.IsotopeWeightNumber == 0) / (float)alignedPeaks.Length,
            };
            result.TimesCenter = new ChromXs() {
                MainType = chromXType,
                RT = new RetentionTime(alignedPeaks.Average(peak => peak.ChromXsTop.RT.Value), representative.ChromXsTop.RT.Unit),
                RI = new RetentionIndex(alignedPeaks.Average(peak => peak.ChromXsTop.RI.Value), representative.ChromXsTop.RI.Unit),
                Mz = new MzValue(alignedPeaks.Average(peak => peak.ChromXsTop.Mz.Value), representative.ChromXsTop.Mz.Unit),
                Drift = new DriftTime(alignedPeaks.Average(peak => peak.ChromXsTop.Drift.Value), representative.ChromXsTop.Drift.Unit),
            };
            result.MassCenter = alignedPeaks.Max(peak => (peak.ChromXsTop.Value, peak.Mass)).Mass;
            return result;
        }

        public static int GetRepresentativeFileID(IReadOnlyList<AlignmentChromPeakFeature> alignment) {
            if (alignment.Count == 0) return -1;
            var alignmentWithMSMS = alignment.Where(align => !align.MS2RawSpectrumID2CE.IsEmptyOrNull()).ToArray();
            if (alignmentWithMSMS.Length != 0) {
                return alignmentWithMSMS.Argmax(align =>
                    // UNDONE: MSRawID2MspBasedMatchResult has no element.
                    (align.MSRawID2MspBasedMatchResult?.Values?.DefaultIfEmpty().Max(val => val?.TotalScore), align.PeakHeightTop)
                    ).FileID;
            }
            return alignment.Max(align => (align.TextDbBasedMatchResult?.TotalScore, align.FileID)).FileID;
        }

        public static void SetDefaultCompoundInformation(AlignmentSpotProperty alignmentSpot) {
            alignmentSpot.AdductType.AdductIonName = string.Empty;
            alignmentSpot.PeakCharacter.Charge = 1;
            alignmentSpot.Name = string.Empty;

            // reset text db
            SetDefaultCompoundInformation(alignmentSpot.TextDbBasedMatchResult);

            // reset msp db
            alignmentSpot.MSRawID2MspBasedMatchResult.Select(kvp => kvp.Value).ToList().ForEach(result => SetDefaultCompoundInformation(result));
        }

        public static void SetDefaultCompoundInformation(MsScanMatchResult scanMatchResult) {
            scanMatchResult.LibraryID = -1;
            scanMatchResult.TotalScore = -1;

            scanMatchResult.WeightedDotProduct = -1;
            scanMatchResult.SimpleDotProduct = -1;
            scanMatchResult.ReverseDotProduct = -1;
            scanMatchResult.MatchedPeaksCount = -1;
            scanMatchResult.MatchedPeaksPercentage = -1;
            scanMatchResult.EssentialFragmentMatchedScore = -1;

            scanMatchResult.RtSimilarity = -1;
            scanMatchResult.RiSimilarity = -1;
            scanMatchResult.CcsSimilarity = -1;
            scanMatchResult.IsotopeSimilarity = -1;
            scanMatchResult.AcurateMassSimilarity = -1;

            scanMatchResult.IsPrecursorMzMatch = false;
            scanMatchResult.IsSpectrumMatch = false;
            scanMatchResult.IsRtMatch = false;
            scanMatchResult.IsRiMatch = false;
            scanMatchResult.IsCcsMatch = false;
            scanMatchResult.IsLipidClassMatch = false;
            scanMatchResult.IsLipidChainsMatch = false;
            scanMatchResult.IsLipidPositionMatch = false;
            scanMatchResult.IsOtherLipidMatch = false;
        }
    }
}
