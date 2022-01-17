using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;

namespace CompMs.MsdialCore.Utility
{
    public class DataObjConverter
    {

        public static AlignmentChromPeakFeature ConvertToAlignmentChromPeakFeature(IMSScanProperty peakobj, MachineCategory category) {
            var result = new AlignmentChromPeakFeature();
            SetAlignmentChromPeakFeature(result, peakobj, category);
            return result;
        }
            
        public static void SetAlignmentChromPeakFeatureFromMSDecResult(AlignmentChromPeakFeature alignmentPeak, MSDecResult peak) {
            alignmentPeak.MasterPeakID = peak.ScanID;
            alignmentPeak.PeakID = peak.ScanID;
            alignmentPeak.SeekPointToDCLFile = peak.SeekPoint;
            alignmentPeak.MS1RawSpectrumID = peak.RawSpectrumID;
            alignmentPeak.MS1RawSpectrumIdTop = peak.RawSpectrumID;
            alignmentPeak.ChromXsTop = peak.ChromXs;
            alignmentPeak.ChromXsLeft = peak.ModelPeakChromatogram[0].ChromXs;
            alignmentPeak.ChromXsRight = peak.ModelPeakChromatogram[peak.ModelPeakChromatogram.Count - 1].ChromXs;
            alignmentPeak.PeakHeightTop = peak.ModelPeakHeight;
            alignmentPeak.PeakAreaAboveZero = peak.ModelPeakArea;
            alignmentPeak.Mass = peak.ModelPeakMz;
            alignmentPeak.IonMode = peak.IonMode;
            alignmentPeak.MSRawID2MspIDs = new Dictionary<int, List<int>>() { { peak.RawSpectrumID, peak.MspIDs } };
            alignmentPeak.MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>() { { peak.RawSpectrumID, peak.MspBasedMatchResult } };
            alignmentPeak.MatchResults.AddMspResult(peak.RawSpectrumID, peak.MspBasedMatchResult);
            alignmentPeak.PeakShape = new ChromatogramPeakShape()
            {
                EstimatedNoise = peak.EstimatedNoise, SignalToNoise = peak.SignalNoiseRatio, AmplitudeScoreValue = peak.AmplitudeScore,
                PeakPureValue = peak.ModelPeakPurity, IdealSlopeValue = peak.ModelPeakQuality
            };
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
            alignmentPeak.ChromScanIdLeft = peak.ChromScanIdLeft;
            alignmentPeak.ChromScanIdRight = peak.ChromScanIdRight;
            alignmentPeak.ChromScanIdTop = peak.ChromScanIdTop;
            alignmentPeak.MS1RawSpectrumIdTop = peak.MS1RawSpectrumIdTop;
            alignmentPeak.MS1RawSpectrumIdLeft = peak.MS1RawSpectrumIdLeft;
            alignmentPeak.MS1RawSpectrumIdRight = peak.MS1RawSpectrumIdRight;
            alignmentPeak.MS1AccumulatedMs1RawSpectrumIdTop = peak.MS1AccumulatedMs1RawSpectrumIdTop;
            alignmentPeak.MS1AccumulatedMs1RawSpectrumIdLeft = peak.MS1AccumulatedMs1RawSpectrumIdLeft;
            alignmentPeak.MS1AccumulatedMs1RawSpectrumIdRight = peak.MS1AccumulatedMs1RawSpectrumIdRight;
            alignmentPeak.ChromXsLeft = peak.ChromXsLeft;
            alignmentPeak.ChromXsTop = peak.ChromXsTop;
            alignmentPeak.ChromXsRight = peak.ChromXsRight;
            alignmentPeak.PeakHeightLeft = peak.PeakHeightLeft;
            alignmentPeak.PeakHeightTop = peak.PeakHeightTop;
            alignmentPeak.PeakHeightRight = peak.PeakHeightRight;
            alignmentPeak.PeakAreaAboveZero = peak.PeakAreaAboveZero;
            alignmentPeak.PeakAreaAboveBaseline = peak.PeakAreaAboveBaseline;
            alignmentPeak.Mass = peak.Mass;
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
            //if (peak.IsReferenceMatched) {
            //    Console.WriteLine(peak.Name + "\t" + peak.PeakCharacter.AdductType.AdductIonName + "\t" + peak.AdductType.AdductIonName);
            //}
           // Console.WriteLine(alignmentPeak.Name + "\t" + alignmentPeak.PeakCharacter.AdductType.AdductIonName);
        }

        public static void SetAlignmentChromPeakFeature(AlignmentChromPeakFeature alignmentPeak, IMSScanProperty peakobj, MachineCategory category) {
            if (category == MachineCategory.GCMS) {
                var peak = (MSDecResult)peakobj;
                SetAlignmentChromPeakFeatureFromMSDecResult(alignmentPeak, peak);
            }
            else {
                var peak = (ChromatogramPeakFeature)peakobj;
                SetAlignmentChromPeakFeatureFromChromatogramPeakFeature(alignmentPeak, peak);
            }
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
            spot.AdductType = Common.Parser.AdductIonParser.GetAdductIonBean(representative.PeakCharacter.AdductType.AdductIonName);
            spot.PeakCharacter = new IonFeatureCharacter
            {
                AdductType = spot.AdductType,
                AdductTypeByAmalgamationProgram = representative.PeakCharacter.AdductTypeByAmalgamationProgram, 
                Charge = representative.PeakCharacter.Charge,
                PeakLinks = representative.PeakCharacter.PeakLinks.ToList(),
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

            spot.MassMin = (float)alignedPeaks.Min(peak => peak.Mass);
            spot.MassMax = (float)alignedPeaks.Max(peak => peak.Mass);

            spot.FillParcentage = (float)alignedPeaks.Length / alignment.Count;
            spot.MonoIsotopicPercentage = alignedPeaks.Count(peak => peak.PeakCharacter.IsotopeWeightNumber == 0) / (float)alignedPeaks.Length;
            spot.TimesCenter = new ChromXs() {
                MainType = chromXType,
                RT = new RetentionTime(alignedPeaks.Average(peak => peak.ChromXsTop.RT.Value), representative.ChromXsTop.RT.Unit),
                RI = new RetentionIndex(alignedPeaks.Average(peak => peak.ChromXsTop.RI.Value), representative.ChromXsTop.RI.Unit),
                Mz = new MzValue(alignedPeaks.Average(peak => peak.ChromXsTop.Mz.Value), representative.ChromXsTop.Mz.Unit),
                Drift = new DriftTime(alignedPeaks.Average(peak => peak.ChromXsTop.Drift.Value), representative.ChromXsTop.Drift.Unit),
            };
            spot.MassCenter = alignedPeaks.Max(peak => (peak.ChromXsTop.Value, peak.Mass)).Mass;
        }

        public static int GetRepresentativeFileID(IReadOnlyList<AlignmentChromPeakFeature> alignment) {
            if (alignment.Count == 0) return -1;
            var alignmentWithMSMS = alignment.Where(align => !align.MS2RawSpectrumID2CE.IsEmptyOrNull()).ToArray(); // ms2 contained
            if (alignmentWithMSMS.Length != 0) {
                return alignmentWithMSMS.Argmax(peak =>
                    // highest total score then highest intensity
                    (peak.MatchResults.MatchResults.DefaultIfEmpty().Max(val => val.TotalScore), peak.PeakHeightTop)
                ).FileID;
            }
            return alignment.Argmax(peak =>
                // highest total score then highest intensity
                (peak.MatchResults.MatchResults.DefaultIfEmpty().Max(val => val.TotalScore), peak.PeakHeightTop)
            ).FileID;
        }

        public static string GetIsotopesListContent(
            ChromatogramPeakFeature feature, 
            IReadOnlyList<RawSpectrum> spectrumList, 
            ParameterBase param) {
            var spectrum = spectrumList.FirstOrDefault(spec => spec.OriginalIndex == feature.MS1RawSpectrumIdTop);
            if (spectrum is null) {
                return "null";
            }
            var isotopes = DataAccess.GetIsotopicPeaks(spectrum.Spectrum, (float)feature.PrecursorMz, param.CentroidMs1Tolerance);
            if (isotopes.IsEmptyOrNull()) {
                return "null";
            }
            return string.Join(";", isotopes.Select(isotope => string.Format("{0:F5} {1:F0}", isotope.Mass, isotope.AbsoluteAbundance)));
        }

        public static string GetSpectrumListContent(
            MSDecResult msdec, 
            IReadOnlyList<RawSpectrum> spectrumList,
            ParameterBase param) {
            var spectrum = DataAccess.GetMassSpectrum(spectrumList, msdec, param.ExportSpectraType, msdec.RawSpectrumID, param);
            if (spectrum.IsEmptyOrNull()) {
                return "null";
            }
            return string.Join(";", spectrum.Select(peak => string.Format("{0:F5} {1:F0}", peak.Mass, peak.Intensity)));
        }

    }
}
