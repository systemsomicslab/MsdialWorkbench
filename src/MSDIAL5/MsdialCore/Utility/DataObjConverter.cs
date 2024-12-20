using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.MsdialCore.DataObj;
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

        public static void SetRepresentativeProperty(AlignmentSpotProperty spot) {
            var alignment = spot.AlignedPeakProperties;
            var alignedPeaks = alignment.Where(align => align.PeakID >= 0).ToArray();

            var repId = GetRepresentativeFileID(alignedPeaks);
            var representative = repId >= 0 ? alignment[repId] : alignedPeaks.First();
            var chromXType = representative.ChromXsTop.MainType;
            spot.RepresentativeFileID = representative.FileID;

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
                RI = new RetentionIndex(alignedPeaks.Average(peak => peak.ChromXsTop.RI.Value), representative.ChromXsTop.RI.Unit),
                Mz = new MzValue(spot.MassCenter, representative.ChromXsTop.Mz.Unit),
                Drift = new DriftTime(alignedPeaks.Average(peak => peak.ChromXsTop.Drift.Value), representative.ChromXsTop.Drift.Unit),
            };
        }

        public static AlignmentChromPeakFeature? GetRepresentativePeak(IReadOnlyList<AlignmentChromPeakFeature> alignment) {
            if (alignment.Count == 0) return null;
            var alignmentWithMSMS = alignment.Where(align => !align.MS2RawSpectrumID2CE.IsEmptyOrNull()).ToArray(); // ms2 contained
            if (alignmentWithMSMS.Length != 0) {
                return alignmentWithMSMS.Argmax(peak =>
                    // highest total score then highest intensity
                    (peak.MatchResults.MatchResults.Max(val => val.TotalScore), peak.PeakHeightTop)
                );
            }
            return alignment.Argmax(peak =>
                // highest total score then highest intensity
                (peak.MatchResults.MatchResults.Max(val => val.TotalScore), peak.PeakHeightTop)
            );
        }

        public static int GetRepresentativeFileID(IReadOnlyList<AlignmentChromPeakFeature> alignment) {
            return GetRepresentativePeak(alignment)?.FileID ?? -1;
        }
    }
}
