using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using CompMs.RawDataHandler.Core;

namespace CompMs.MsdialDimsCore.Common
{
    public class AlignmentProcess
    {
        public static AlignmentResultContainer Alignment(
            IReadOnlyList<AnalysisFileBean> analysisFiles, AlignmentFileBean alignmentFile,
            ChromatogramSerializer<ChromatogramSpotInfo> chromPeakSpotSerializer, ParameterBase param) {
            var master = GetMasterList(analysisFiles, param.AlignmentReferenceFileID, param.Ms1AlignmentTolerance);
            var alignments = AlignAll(master, analysisFiles, param.Ms1AlignmentTolerance);
            var alignmentSpots = CollectPeakSpots(analysisFiles, alignmentFile, alignments, chromPeakSpotSerializer, param);

            return PackingSpots(alignmentSpots, param);
        }

        private static List<List<AlignmentChromPeakFeature>> AlignAll(IReadOnlyList<ChromatogramPeakFeature> master, IEnumerable<AnalysisFileBean> analysisFiles, double tolerance) {
            return analysisFiles.Select(file => AlignFileToMaster(file, master, tolerance)).ToList();
        }

        private static List<AlignmentChromPeakFeature> AlignFileToMaster(AnalysisFileBean analysisFile, IReadOnlyList<ChromatogramPeakFeature> master, double tolerance) {
            var results = AlignPeaksToMaster(GetChromatogramPeakFeatures(analysisFile), master, tolerance);
            foreach(var result in results) {
                if (result == null) continue;
                result.FileID = analysisFile.AnalysisFileId;
                result.FileName = analysisFile.AnalysisFileName;
            }
                
            return results;
        }

        private static List<AlignmentChromPeakFeature> AlignPeaksToMaster(IEnumerable<ChromatogramPeakFeature> peaks, IReadOnlyList<ChromatogramPeakFeature> master, double tolerance) {
            var n = master.Count;
            var result = Enumerable.Repeat<AlignmentChromPeakFeature>(null, n).ToList();
            var target = new ChromatogramPeakFeature { ChromXsTop = new ChromXs { Mz = new MzValue() } };
            var maxMatchs = new double[n];

            foreach (var peak in peaks) {
                target.ChromXsTop.Mz.Value = peak.ChromXs.Mz.Value - tolerance;
                var idx = SearchCollection.LowerBound(master, target, MassComparer.Comparer);
                int? matchIdx = null;
                double matchFactor = double.MinValue;
                for (; idx < n; idx++) {
                    if (master[idx].ChromXsTop.Mz.Value < peak.ChromXsTop.Mz.Value - tolerance) continue;
                    if (master[idx].ChromXsTop.Mz.Value > peak.ChromXsTop.Mz.Value + tolerance) break;
                    var factor = CalculateMatchFactor(master[idx].ChromXsTop.Mz.Value - peak.ChromXsTop.Mz.Value, tolerance);
                    if (factor > maxMatchs[idx] && factor > matchFactor) {
                        matchIdx = idx;
                        matchFactor = factor;
                    }
                }
                if (matchIdx.HasValue)
                    result[matchIdx.Value] = CreateAlignmentChromPeakFeature(peak);
            }

            return result;
        }

        private static double CalculateMatchFactor(double width, double tolerance) {
            return Math.Exp(-.5 * Math.Pow(width / tolerance, 2));
        }

        private static (List<AlignmentChromPeakFeature>, string) CollectAlignmentPeaks(
            AnalysisFileBean analysisFile, List<ChromXs> points, List<float> widths,
            List<AlignmentChromPeakFeature> alignment, ParameterBase param, ChromatogramSerializer<ChromatogramPeakInfo> serializer = null) {
            var results = new List<AlignmentChromPeakFeature>();
            var peakInfos = new List<ChromatogramPeakInfo>();
            using (var rawDataAccess = new RawDataAccess(analysisFile.AnalysisFilePath, analysisFile.AnalysisFileId, true, analysisFile.RetentionTimeCorrectionBean.PredictedRt)) {
                var spectra = DataAccess.GetAllSpectra(rawDataAccess);
                foreach ((var point, var width, var align_) in points.Zip(widths, alignment)) {
                    var align = align_;
                    if (align == null) {
                        align = GapFilling(spectra, point, width, param);
                    }
                    results.Add(align);

                    var peaklist = DataAccess.GetMs1Peaklist(spectra, (float)point.Mz.Value, width * 1.5f, align.IonMode);
                    var peakInfo = new ChromatogramPeakInfo(
                        align.FileID, peaklist,
                        (float)align.ChromXsTop.Value,
                        (float)align.ChromXsLeft.Value,
                        (float)align.ChromXsRight.Value
                        );
                    peakInfos.Add(peakInfo);
                }
            }
            var file = System.IO.Path.GetTempFileName();
            serializer?.SerializeAllToFile(file, peakInfos);
            return (results, file);
        }

        private static List<AlignmentSpotProperty> CollectPeakSpots(
            IEnumerable<AnalysisFileBean> analysisFiles, AlignmentFileBean alignmentFile,
            List<List<AlignmentChromPeakFeature>> alignments,
            ChromatogramSerializer<ChromatogramSpotInfo> chromPeakSpotSerializer, ParameterBase param) {
            var transpose = Sequence(alignments);
            var points = GetAlignmentPointCore(transpose);
            var widths = GetPeakWidths(transpose);
            var aligns = new List<List<AlignmentChromPeakFeature>>();
            var files = new List<string>();
            var chromPeakInfoSerializer = ChromatogramSerializerFactory.CreatePeakSerializer("CPSTMP");
            foreach ((var analysisFile, var alignment) in analysisFiles.Zip(alignments)) {
                (var align, var file) = CollectAlignmentPeaks(analysisFile, points, widths, alignment, param, chromPeakInfoSerializer);
                aligns.Add(align);
                files.Add(file);
            }
            var spots = PackingAlignmentsToSpots(Sequence(aligns), param);
            SerializeSpotInfo(spots, files, alignmentFile, chromPeakSpotSerializer, chromPeakInfoSerializer);
            return spots;
        }

        private static AlignmentChromPeakFeature CreateAlignmentChromPeakFeature(ChromatogramPeakFeature peak) {
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

        private static AlignmentChromPeakFeature GapFilling(List<RawSpectrum> spectrum, ChromXs point, float peakWidth, ParameterBase param) {
            return GapFiller.GapFilling(
                spectrum, point,
                param.Ms1AlignmentTolerance, peakWidth, param.IonMode,
                param.SmoothingMethod, param.SmoothingLevel,
                param.IsForceInsertForGapFilling);
        }

        private static List<ChromXs> GetAlignmentPointCore(List<List<AlignmentChromPeakFeature>> alignments) {
            var results = new List<ChromXs>(alignments.Count);
            foreach (var alignment in alignments) {
                var detects = alignment.Where(align => align != null).ToArray();
                var n = detects.Length;

                if (n == 0) {
                    results.Add(null);
                    continue;
                }

                var rt = detects.Sum(detect => detect.ChromXsTop.RT.Value) / n;
                var ri = detects.Sum(detect => detect.ChromXsTop.RI.Value) / n;
                var mz = detects.Sum(detect => detect.ChromXsTop.Mz.Value) / n;
                var drift = detects.Sum(detect => detect.ChromXsTop.Drift.Value) / n;
                var tmp = detects[0].ChromXsTop;

                results.Add(new ChromXs {
                    RT = new RetentionTime(rt, tmp.RT.Unit),
                    RI = new RetentionIndex(ri, tmp.RI.Unit),
                    Mz = new MzValue(mz, tmp.Mz.Unit),
                    Drift = new DriftTime(drift, tmp.Drift.Unit),
                    MainType = tmp.MainType
                });
            }

            return results;
        }

        private static List<ChromatogramPeakFeature> GetChromatogramPeakFeatures(AnalysisFileBean analysisFile) {
            var chromatogram = MsdialSerializer.LoadChromatogramPeakFeatures(analysisFile.PeakAreaBeanInformationFilePath);
            chromatogram.Sort(MassComparer.Comparer);
            return chromatogram;
        }

        private static List<ChromatogramPeakFeature> GetMasterList(IReadOnlyList<AnalysisFileBean> analysisFiles, int referenceId, double tolerance) {
            var master = new LinkedList<ChromatogramPeakFeature>();
            master.AddFirst(new ChromatogramPeakFeature { ChromXsTop = new ChromXs(double.MinValue, ChromXType.Mz, ChromXUnit.Mz) }); // Add Sentinel
            master.AddLast(new ChromatogramPeakFeature { ChromXsTop = new ChromXs(double.MaxValue, ChromXType.Mz, ChromXUnit.Mz) });  // Add Sentinel

            MergeFileToMasterList(analysisFiles.FirstOrDefault(file => file.AnalysisFileId == referenceId), master, tolerance);
            foreach (var file in analysisFiles) {
                if (file.AnalysisFileId == referenceId) continue;
                MergeFileToMasterList(file, master, tolerance);
            }

            master.RemoveFirst(); // Remove Sentinel
            master.RemoveLast();  // Remove Sentinel

            return new List<ChromatogramPeakFeature>(master);
        }

        private static float GetPeakWidth(List<AlignmentChromPeakFeature> alignment) {
            var width = alignment.Where(align => align != null).DefaultIfEmpty(null).Average(align => align?.PeakWidth(ChromXType.Mz));
            if (width.HasValue)
                return (float)width.Value;
            return 0f;
        }

        private static List<float> GetPeakWidths(List<List<AlignmentChromPeakFeature>> alignments) {
            return alignments.Select(alignment => GetPeakWidth(alignment)).ToList();
        }

        private static int GetRepresentativeFileID(IReadOnlyList<AlignmentChromPeakFeature> alignment) {
            if (alignment.Count == 0) return -1;
            var alignmentWithMSMS = alignment.Where(align => !align.MS2RawSpectrumID2CE.IsEmptyOrNull()).ToArray();
            if (alignmentWithMSMS.Length != 0) {
                return alignmentWithMSMS.Max(align =>
                    // UNDONE: MSRawID2MspBasedMatchResult has no element.
                    (align.MSRawID2MspBasedMatchResult?.Values?.DefaultIfEmpty().Max(val => val?.TotalScore), align.PeakHeightTop, align.FileID)
                    ).FileID;
            }
            return alignment.Max(align => (align.TextDbBasedMatchResult?.TotalScore, align.FileID)).FileID;
        }

        private static void MergeFileToMasterList(AnalysisFileBean analysisFile, LinkedList<ChromatogramPeakFeature> master, double tolerance) {
            MergePeaksToMasterList(GetChromatogramPeakFeatures(analysisFile), master, tolerance);
        }

        private static void MergePeaksToMasterList(IEnumerable<ChromatogramPeakFeature> peaks, LinkedList<ChromatogramPeakFeature> master, double tolerance) {
            var itr = master.First;
            foreach (var peak in peaks) {
                while (itr.Next.Value.ChromXsTop.Mz.Value < peak.ChromXsTop.Mz.Value) itr = itr.Next;
                if (itr.Value.ChromXsTop.Mz.Value + tolerance < peak.ChromXsTop.Mz.Value && peak.ChromXsTop.Mz.Value < itr.Next.Value.ChromXsTop.Mz.Value - tolerance)
                    master.AddAfter(itr, peak);
            }
        }

        private static List<List<AlignmentChromPeakFeature>> Sequence(List<List<AlignmentChromPeakFeature>> alignments) {
            var n = alignments[0].Count;
            var m = alignments.Count;
            var results = new List<List<AlignmentChromPeakFeature>>(n);
            for (int i = 0; i < n; i++)
                results.Add(new List<AlignmentChromPeakFeature>(m));

            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    results[i].Add(alignments[j][i]);

            return results;
        }

        private static IEnumerable<List<T>> Sequence<T>(IReadOnlyList<IEnumerable<T>> xss) {
            var n = xss.Count;
            var enumerators = xss.Select(xs => xs.GetEnumerator()).ToList();
            var remain = true;
            while (remain) {
                var result = new List<T>(n);
                foreach (var enumerator in enumerators) {
                    if (enumerator.MoveNext()) {
                        result.Add(enumerator.Current);
                    }
                    else {
                        remain = false;
                        break;
                    }
                }
                if (remain) yield return result;
            }
        }

        private static List<AlignmentSpotProperty> PackingAlignmentsToSpots(List<List<AlignmentChromPeakFeature>> alignments, ParameterBase param) {
            var results = new List<AlignmentSpotProperty>(alignments.Count);
            foreach ((var alignment, var idx) in alignments.WithIndex()) {
                var spot = PackingFeatureToSpot(alignment, param);
                spot.AlignmentID = idx;
                spot.MasterAlignmentID = idx;
                spot.ParentAlignmentID = -1;
                spot.InternalStandardAlignmentID = idx;
                results.Add(spot);
            }
            return results;
        }

        private static AlignmentSpotProperty PackingFeatureToSpot(List<AlignmentChromPeakFeature> alignment, ParameterBase param) {
            var alignedPeaks = alignment.Where(align => align.PeakID >= 0).ToArray();
            if (alignedPeaks.Length == 0) {
                return new AlignmentSpotProperty();
            }
            var repId = GetRepresentativeFileID(alignment);
            var representative = alignment[repId];
            var result = new AlignmentSpotProperty() {
                RepresentativeFileID = repId,

                AlignedPeakProperties = alignment,

                PeakCharacter = representative.PeakCharacter,
                IonMode = param.IonMode,

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
                PeakWidthAverage = (float)alignedPeaks.Average(peak => peak.PeakWidth(ChromXType.Mz)),

                SignalToNoiseAve = alignedPeaks.Average(peak => peak.PeakShape.SignalToNoise),
                SignalToNoiseMax = alignedPeaks.Max(peak => peak.PeakShape.SignalToNoise),
                SignalToNoiseMin = alignedPeaks.Min(peak => peak.PeakShape.SignalToNoise),

                EstimatedNoiseAve = alignedPeaks.Average(peak => peak.PeakShape.EstimatedNoise),
                EstimatedNoiseMax = alignedPeaks.Max(peak => peak.PeakShape.EstimatedNoise),
                EstimatedNoiseMin = alignedPeaks.Min(peak => peak.PeakShape.EstimatedNoise),

                TimesMin = alignedPeaks.Min(peak => (peak.ChromXsTop.Value, peak.ChromXsTop)).ChromXsTop,
                TimesMax = alignedPeaks.Max(peak => (peak.ChromXsTop.Value, peak.ChromXsTop)).ChromXsTop,

                MassMin = (float)alignedPeaks.Min(peak => peak.Mass),
                MassMax = (float)alignedPeaks.Max(peak => peak.Mass),

                FillParcentage = (float)alignedPeaks.Length / alignment.Count,
                MonoIsotopicPercentage = alignedPeaks.Count(peak => peak.PeakCharacter.IsotopeWeightNumber == 0) / (float)alignedPeaks.Length,
            };
            result.TimesCenter = new ChromXs() {
                MainType = representative.ChromXsTop.MainType,
                RT = new RetentionTime(alignedPeaks.Average(peak => peak.ChromXsTop.RT.Value), representative.ChromXsTop.RT.Unit),
                RI = new RetentionIndex(alignedPeaks.Average(peak => peak.ChromXsTop.RI.Value), representative.ChromXsTop.RI.Unit),
                Mz = new MzValue(alignedPeaks.Average(peak => peak.ChromXsTop.Mz.Value), representative.ChromXsTop.Mz.Unit),
                Drift = new DriftTime(alignedPeaks.Average(peak => peak.ChromXsTop.Drift.Value), representative.ChromXsTop.Drift.Unit),
            };
            result.MassCenter = alignedPeaks.Max(peak => (peak.ChromXsTop.Value, peak.Mass)).Mass;
            return result;
        }

        private static AlignmentResultContainer PackingSpots(IEnumerable<AlignmentSpotProperty> alignmentSpots, ParameterBase param) {
            var props = new System.Collections.ObjectModel.ObservableCollection<AlignmentSpotProperty>(alignmentSpots);
            return new AlignmentResultContainer {
                Ionization = param.Ionization,
                AlignmentResultFileID = -1,
                TotalAlignmentSpotCount = props.Count,
                AlignmentSpotProperties = props,
            };
        }

        private static void SerializeSpotInfo(
            List<AlignmentSpotProperty> spots, List<string> files,
            AlignmentFileBean alignmentFile,
            ChromatogramSerializer<ChromatogramSpotInfo> spotSerializer,
            ChromatogramSerializer<ChromatogramPeakInfo> peakSerializer) {
            // UNDONE: already close files
            var pss = files.Select(file => peakSerializer.DeserializeAllFromFile(file)).ToList();
            var qss = Sequence(pss);

            using (var fs = File.OpenWrite(alignmentFile.EicFilePath)) {
                spotSerializer.SerializeN(fs, spots.Zip(qss, (spot, qs) => new ChromatogramSpotInfo(qs, spot.TimesCenter)), spots.Count);
            }
        }

        private class MassComparer : IComparer<IChromatogramPeakFeature>
        {
            public int Compare(IChromatogramPeakFeature x, IChromatogramPeakFeature y) {
                return x.ChromXsTop.Mz.Value.CompareTo(y.ChromXsTop.Mz.Value);
            }

            public static MassComparer Comparer = new MassComparer();
        }
    }
}
