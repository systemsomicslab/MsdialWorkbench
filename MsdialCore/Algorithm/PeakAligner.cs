using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Database;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using CompMs.RawDataHandler.Core;

namespace CompMs.MsdialCore.Algorithm
{
    public class PeakAligner
    {
        protected PeakComparer Comparer { get; set; }
        protected PeakJoiner Joiner { get; set; }
        protected GapFiller Filler { get; set; }
        protected AlignmentRefiner Refiner { get; set; }
        protected ParameterBase Param { get; set; }
        protected IupacDatabase Iupac { get; set; }

        public PeakAligner(PeakComparer comparer, PeakJoiner joiner, GapFiller filler, AlignmentRefiner refiner, ParameterBase param, IupacDatabase iupac) {
            Comparer = comparer;
            Joiner = joiner;
            Filler = filler;
            Refiner = refiner;
            Param = param;
            Iupac = iupac;
        }

        public AlignmentResultContainer Alignment(
            IReadOnlyList<AnalysisFileBean> analysisFiles, AlignmentFileBean alignmentFile,
            ChromatogramSerializer<ChromatogramSpotInfo> spotSerializer) {

            var master = GetMasterList(analysisFiles);
            var spots = AlignAll(master, analysisFiles);
            spots = FilterAlignments(spots, analysisFiles);

            spots = CollectPeakSpots(analysisFiles, alignmentFile, spots, spotSerializer);
            IsotopeAnalysis(spots);
            spots = GetRefinedAlignmentSpotProperties(spots);

            return PackingSpots(spots);
        }

        protected virtual List<IMSScanProperty> GetMasterList(IReadOnlyList<AnalysisFileBean> analysisFiles) {

            var referenceId = Param.AlignmentReferenceFileID;
            var referenceFile = analysisFiles.FirstOrDefault(file => file.AnalysisFileId == referenceId);
            if (referenceFile == null) return new List<IMSScanProperty>();

            var master = GetChromatogramPeakFeatures(referenceFile, Param.MachineCategory);
            foreach (var analysisFile in analysisFiles) {
                if (analysisFile.AnalysisFileId == referenceFile.AnalysisFileId)
                    continue;
                var target = GetChromatogramPeakFeatures(analysisFile, Param.MachineCategory);
                master = Joiner.MergeChromatogramPeaks(master, target);
            }

            return master;
        }

        private List<AlignmentSpotProperty> GetSpots(IReadOnlyCollection<IMSScanProperty> masters, IEnumerable<AnalysisFileBean> analysisFiles) {
            var masterId = 0;
            return InitSpots(masters, analysisFiles, ref masterId);
        }

        private List<AlignmentSpotProperty> InitSpots(IEnumerable<IMSScanProperty> scanProps,
            IEnumerable<AnalysisFileBean> analysisFiles, ref int masterId, int parentId = -1) {

            if (scanProps == null) return new List<AlignmentSpotProperty>();

            var spots = new List<AlignmentSpotProperty>();
            foreach ((var scanProp, var localId) in scanProps.WithIndex()) {
                var spot = new AlignmentSpotProperty
                {
                    MasterAlignmentID = masterId++,
                    AlignmentID = localId,
                    ParentAlignmentID = parentId,
                    TimesCenter = scanProp.ChromXs,
                    MassCenter = scanProp.PrecursorMz,
                };
                spot.InternalStandardAlignmentID = spot.MasterAlignmentID;

                var peaks = new List<AlignmentChromPeakFeature>();
                foreach (var file in analysisFiles) {
                    peaks.Add(new AlignmentChromPeakFeature
                    {
                        MasterPeakID = -1,
                        PeakID = -1,
                        FileID = file.AnalysisFileId,
                        FileName = file.AnalysisFileName,
                    });
                }
                spot.AlignedPeakProperties = peaks;

                if (scanProp is ChromatogramPeakFeature chrom)
                    spot.AlignmentDriftSpotFeatures = InitSpots(chrom.DriftChromFeatures, analysisFiles, ref masterId, spot.MasterAlignmentID);

                spots.Add(spot);
            }

            return spots;
        }

        protected virtual List<AlignmentSpotProperty> AlignAll(List<IMSScanProperty> master, IReadOnlyList<AnalysisFileBean> analysisFiles) {
            var result = GetSpots(master, analysisFiles);
            
            foreach (var analysisFile in analysisFiles) {
                var chromatogram = GetChromatogramPeakFeatures(analysisFile, Param.MachineCategory);
                Joiner.AlignPeaksToMaster(result, master, chromatogram, analysisFile.AnalysisFileId);
            }
            
            return result;
        }

        protected virtual List<AlignmentSpotProperty> FilterAlignments(
            List<AlignmentSpotProperty> spots, IReadOnlyList<AnalysisFileBean> analysisFiles ) {
            List<AlignmentSpotProperty> result = spots;
            result = result.Where(spot => spot.AlignedPeakProperties.Any(peak => peak.MasterPeakID >= 0)).ToList();

            var peakCountThreshold = Param.PeakCountFilter / 100 * analysisFiles.Count;
            result = result.Where(spot => spot.AlignedPeakProperties.Count(peak => peak.MasterPeakID >= 0) >= peakCountThreshold).ToList();

            if (Param.QcAtLeastFilter) {
                var qcidx = analysisFiles.WithIndex().Where(fi => fi.Item1.AnalysisFileType == AnalysisFileType.QC).Select(fi => fi.Item2).ToArray();
                result = result.Where(spot => qcidx.All(idx => spot.AlignedPeakProperties[idx].MasterPeakID >= 0)).ToList();
            }

            Func<AlignmentSpotProperty, bool> IsNPercentDetectedInOneGroup = GetNPercentDetectedInOneGroupFilter(analysisFiles);
            result = result.Where(IsNPercentDetectedInOneGroup).ToList();

            return result.ToList();
        }

        private Func<AlignmentSpotProperty, bool> GetNPercentDetectedInOneGroupFilter(IReadOnlyList<AnalysisFileBean> files) {
            var groupDic = new Dictionary<string, List<int>>();
            foreach ((var file, var idx) in files.WithIndex()) {
                if (!groupDic.ContainsKey(file.AnalysisFileClass))
                    groupDic[file.AnalysisFileClass] = new List<int>();
                groupDic[file.AnalysisFileClass].Add(idx);
            }

            double threshold = Param.NPercentDetectedInOneGroup / 100d;

            bool isNPercentDetected(AlignmentSpotProperty spot) {
                return groupDic.Any(kvp => kvp.Value.Count(idx => spot.AlignedPeakProperties[idx].MasterPeakID >= 0) >= threshold * kvp.Value.Count);
            }

            return isNPercentDetected;
        }

        private List<AlignmentSpotProperty> CollectPeakSpots(IReadOnlyList<AnalysisFileBean> analysisFiles, AlignmentFileBean alignmentFile,
            List<AlignmentSpotProperty> spots, ChromatogramSerializer<ChromatogramSpotInfo> spotSerializer) {

            var files = new List<string>();
            var chromPeakInfoSerializer = ChromatogramSerializerFactory.CreatePeakSerializer("CPSTMP");

            foreach (var analysisFile in analysisFiles) {
                var peaks = new List<AlignmentChromPeakFeature>(spots.Count);
                foreach (var spot in spots)
                    peaks.Add(spot.AlignedPeakProperties.FirstOrDefault(peak => peak.FileID == analysisFile.AnalysisFileId));
                var file = CollectAlignmentPeaks(analysisFile, peaks, spots, chromPeakInfoSerializer);
                files.Add(file);
            }
            var result = new List<AlignmentSpotProperty>();
            foreach (var spot in spots)
                result.Add(PackingSpot(spot));

            SerializeSpotInfo(result, files, alignmentFile, spotSerializer, chromPeakInfoSerializer);
            foreach (var f in files)
                if (File.Exists(f))
                    File.Delete(f);

            return result;
        }

        protected virtual string CollectAlignmentPeaks(
            AnalysisFileBean analysisFile, List<AlignmentChromPeakFeature> peaks,
            List<AlignmentSpotProperty> spots,
            ChromatogramSerializer<ChromatogramPeakInfo> serializer = null) {

            var peakInfos = new List<ChromatogramPeakInfo>();
            using (var rawDataAccess = new RawDataAccess(analysisFile.AnalysisFilePath, 0, true, analysisFile.RetentionTimeCorrectionBean.PredictedRt)) {
                var spectra = DataAccess.GetAllSpectra(rawDataAccess);
                foreach ((var peak, var spot) in peaks.Zip(spots)) {
                    if (spot.AlignedPeakProperties.FirstOrDefault(p => p.FileID == analysisFile.AnalysisFileId).MasterPeakID < 0) {
                        Filler.GapFill(spectra, spot, analysisFile.AnalysisFileId);
                    }

                    // UNDONE: retrieve spectrum data
                    var detected = spot.AlignedPeakProperties.Where(x => x.MasterPeakID >= 0);
                    var peaklist = DataAccess.GetMs1Peaklist(
                        spectra, (float)peak.Mass,
                        (float)(detected.Max(x => x.Mass) - detected.Min(x => x.Mass)) * 1.5f,
                        peak.IonMode);
                    var peakInfo = new ChromatogramPeakInfo(
                        peak.FileID, peaklist,
                        (float)peak.ChromXsTop.Value,
                        (float)peak.ChromXsLeft.Value,
                        (float)peak.ChromXsRight.Value
                        );
                    peakInfos.Add(peakInfo);
                }
            }
            var file = Path.GetTempFileName();
            serializer?.SerializeAllToFile(file, peakInfos);
            return file;
        }

        private void IsotopeAnalysis(IReadOnlyList<AlignmentSpotProperty> alignmentSpots) {
            foreach (var spot in alignmentSpots) {
                if (Param.TrackingIsotopeLabels || spot.IsReferenceMatched) {
                    spot.PeakCharacter.IsotopeParentPeakID = spot.AlignmentID;
                    spot.PeakCharacter.IsotopeWeightNumber = 0;
                }
                if (!spot.IsReferenceMatched) {
                    spot.AdductType.AdductIonName = string.Empty;
                }
            }
            if (Param.TrackingIsotopeLabels) return;

            IsotopeEstimator.Process(alignmentSpots, Param, Iupac);
        }

        private List<AlignmentSpotProperty> GetRefinedAlignmentSpotProperties(List<AlignmentSpotProperty> alignmentSpots) {
            if (alignmentSpots.Count <= 1) return alignmentSpots;
            return Refiner.Refine(alignmentSpots);
        }
        private AlignmentResultContainer PackingSpots(List<AlignmentSpotProperty> alignmentSpots) {
            if (alignmentSpots.IsEmptyOrNull()) return null;

            var minInt = (double)alignmentSpots.Min(spot => spot.HeightMin);
            var maxInt = (double)alignmentSpots.Max(spot => spot.HeightMax);

            maxInt = maxInt > 1 ? Math.Log(maxInt, 2) : 1;
            minInt = minInt > 1 ? Math.Log(minInt, 2) : 0;

            for (int i = 0; i < alignmentSpots.Count; i++) {
                var relativeValue = (float)((Math.Log(alignmentSpots[i].HeightMax, 2) - minInt) / (maxInt - minInt));
                alignmentSpots[i].RelativeAmplitudeValue = Math.Min(1, Math.Max(0, relativeValue));
            }

            var spots = new ObservableCollection<AlignmentSpotProperty>(alignmentSpots);
            return new AlignmentResultContainer {
                Ionization = Param.Ionization,
                AlignmentResultFileID = -1,
                TotalAlignmentSpotCount = spots.Count,
                AlignmentSpotProperties = spots,
            };
        }


        private List<IMSScanProperty> GetChromatogramPeakFeatures(AnalysisFileBean analysisFile, MachineCategory category) {
            if (category == MachineCategory.GCMS) {
                var msdecResults = MsdecResultsReader.ReadMSDecResults(analysisFile.DeconvolutionFilePath, out int dcl_version, out List<long> seekPoints);
                msdecResults.Sort(Comparer);
                return new List<IMSScanProperty>(msdecResults);
            }
            else {
                var chromatogram = MsdialSerializer.LoadChromatogramPeakFeatures(analysisFile.PeakAreaBeanInformationFilePath);
                chromatogram.Sort(Comparer);
                return new List<IMSScanProperty>(chromatogram);
            }
        }

        private AlignmentSpotProperty PackingSpot(AlignmentSpotProperty spot) {
            var childs = new List<AlignmentSpotProperty>(spot.AlignmentDriftSpotFeatures.Count);
            foreach (var child in spot.AlignmentDriftSpotFeatures)
                childs.Add(PackingSpot(child));

            var result = DataObjConverter.ConvertFeatureToSpot(spot.AlignedPeakProperties);
            result.AlignmentDriftSpotFeatures = childs;
            return result;
        }

        private void SerializeSpotInfo(
            IReadOnlyCollection<AlignmentSpotProperty> spots, IEnumerable<string> files,
            AlignmentFileBean alignmentFile,
            ChromatogramSerializer<ChromatogramSpotInfo> spotSerializer,
            ChromatogramSerializer<ChromatogramPeakInfo> peakSerializer) {
            var pss = files.Select(file => peakSerializer.DeserializeAllFromFile(file)).ToList();
            var qss = pss.Sequence();

            using (var fs = File.OpenWrite(alignmentFile.EicFilePath)) {
                spotSerializer.SerializeN(fs, spots.Zip(qss, (spot, qs) => new ChromatogramSpotInfo(qs, spot.TimesCenter)), spots.Count);
            }

            pss.ForEach(ps => ((IDisposable)ps).Dispose());
        }
    }
}
