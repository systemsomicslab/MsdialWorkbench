using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using CompMs.Common.Components;
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
        PeakComparer Comparer { get; set; }
        PeakJoiner Joiner { get; set; }
        AlignmentRefiner Refiner { get; set; }
        AlignmentProcessFactory AProcessFactory { get; set; }
        ParameterBase Param { get; set; }
        IupacDatabase Iupac { get; set; }

        public PeakAligner(PeakComparer comparer, PeakJoiner joiner, AlignmentProcessFactory factory, AlignmentRefiner refiner, ParameterBase param, IupacDatabase iupac) {
            Comparer = comparer;
            Joiner = joiner;
            AProcessFactory = factory;
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
            var quantMasses = GetQuantmassDictionary(analysisFiles, spots);

            foreach ((var analysisFile, var idx) in analysisFiles.WithIndex()) {
                var peaks = new List<AlignmentChromPeakFeature>(spots.Count);
                foreach (var spot in spots)
                    peaks.Add(spot.AlignedPeakProperties[idx]);
                var file = CollectAlignmentPeaks(analysisFile, peaks, spots, quantMasses, chromPeakInfoSerializer);
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

        private List<double> GetQuantmassDictionary(IReadOnlyList<AnalysisFileBean> analysisFiles, List<AlignmentSpotProperty> spots) {
            var sampleNum = analysisFiles.Count;
            var peakNum = spots.Count;
            var quantmasslist = Enumerable.Repeat<double>(-1, peakNum).ToList();

            if (Param.MachineCategory != MachineCategory.GCMS) return quantmasslist;

            var isReplaceMode = !this.AProcessFactory.MspDB.IsEmptyOrNull();
            var bin = this.Param.AccuracyType == AccuracyType.IsAccurate ? 2 : 0;
            for (int i = 0; i < peakNum; i++) {
                var alignment = spots[i].AlignedPeakProperties;
                var repFileID = DataObjConverter.GetRepresentativeFileID(alignment);
                
                if (isReplaceMode && alignment[repFileID].MspID() >= 0) {
                    var refQuantMass = this.AProcessFactory.MspDB[alignment[repFileID].MspID()].QuantMass;
                    if (refQuantMass >= this.Param.MassRangeBegin && refQuantMass <= this.Param.MassRangeEnd) {
                        quantmasslist[i] = refQuantMass; continue;
                    }
                }

                var dclFile = analysisFiles[repFileID].DeconvolutionFilePath;
                var msdecResult = MsdecResultsReader.ReadMSDecResult(dclFile, alignment[repFileID].SeekPointToDCLFile);
                var spectrum = msdecResult.Spectrum;
                var quantMassDict = new Dictionary<double, List<double>>();
                var maxPeakHeight = alignment.Max(n => n.PeakHeightTop);
                var repQuantMass = alignment[repFileID].Mass;

                foreach (var feature in alignment.Where(n => n.PeakHeightTop > maxPeakHeight * 0.1)) {
                    var quantMass = Math.Round(feature.Mass, bin);
                    if (!quantMassDict.Keys.Contains(quantMass))
                        quantMassDict[quantMass] = new List<double>() { feature.Mass };
                    else
                        quantMassDict[quantMass].Add(feature.Mass);
                }
                var maxQuant = 0.0; var maxCount = 0;
                foreach (var pair in quantMassDict)
                    if (pair.Value.Count > maxCount) { maxCount = pair.Value.Count; maxQuant = pair.Key; }

                var quantMassCandidate = quantMassDict[maxQuant].Average();
                var basepeakMz = 0.0;
                var basepeakIntensity = 0.0;
                var isQuantMassExist = isQuantMassExistInSpectrum(quantMassCandidate, spectrum, this.Param.CentroidMs1Tolerance, 10.0F, out basepeakMz, out basepeakIntensity);
                if (AProcessFactory.IsRepresentativeQuantMassBasedOnBasePeakMz) {
                    quantmasslist[i] = basepeakMz; continue;
                }
                if (isQuantMassExist) {
                    quantmasslist[i] = quantMassCandidate; continue;
                }

                var isSuitableQuantMassExist = false;
                foreach (var peak in spectrum) {
                    if (peak.Mass < repQuantMass - bin) continue;
                    if (peak.Mass > repQuantMass + bin) break;
                    var diff = Math.Abs(peak.Mass - repQuantMass);
                    if (diff <= bin && peak.Intensity > basepeakIntensity * 10.0 * 0.01) {
                        isSuitableQuantMassExist = true;
                        break;
                    }
                }
                if (isSuitableQuantMassExist)
                    quantmasslist[i] = repQuantMass;
                else
                    quantmasslist[i] = basepeakMz;
            }
            return quantmasslist;
        }

        // spectrum should be ordered by m/z value
        private static bool isQuantMassExistInSpectrum(double quantMass, List<SpectrumPeak> spectrum, float bin, float threshold,
            out double basepeakMz, out double basepeakIntensity) {

            basepeakMz = 0.0;
            basepeakIntensity = 0.0;
            foreach (var peak in spectrum) {
                if (peak.Intensity > basepeakIntensity) {
                    basepeakIntensity = peak.Intensity;
                    basepeakMz = peak.Mass;
                }
            }

            var maxIntensity = basepeakIntensity;
            foreach (var peak in spectrum) {
                if (peak.Mass < quantMass - bin) continue;
                if (peak.Mass > quantMass + bin) break;
                var diff = Math.Abs(peak.Mass - quantMass);
                if (diff <= bin && peak.Intensity > maxIntensity * threshold * 0.01) {
                    return true;
                }
            }
            return false;
        }


        private string CollectAlignmentPeaks(
            AnalysisFileBean analysisFile, List<AlignmentChromPeakFeature> peaks,
            List<AlignmentSpotProperty> spots, List<double> quantMasses,
            ChromatogramSerializer<ChromatogramPeakInfo> serializer = null) {

            var peakInfos = new List<ChromatogramPeakInfo>();
            using (var rawDataAccess = new RawDataAccess(analysisFile.AnalysisFilePath, 0, true, analysisFile.RetentionTimeCorrectionBean.PredictedRt)) {
                var spectra = DataAccess.GetAllSpectra(rawDataAccess);
                foreach ((var peak, var spot, var quantmass) in peaks.Zip(spots, quantMasses)) {
                    if (peak.PeakID < 0) {
                        GapFilling(spectra, spot.AlignedPeakProperties, quantmass, analysisFile.AnalysisFileId);
                    }

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

        private void GapFilling(List<RawSpectrum> spectra, List<AlignmentChromPeakFeature> peaks, double quantmass, int fileID) {
            var features = peaks.Where(p => p.PeakID >= 0);
            var chromXCenter = Comparer.GetCenter(features);
            if (quantmass > 0) chromXCenter.Mz = new MzValue(quantmass);

            foreach (var peak in peaks.Where(p => p.PeakID < 0))
                GapFiller.GapFilling(this.AProcessFactory, spectra, chromXCenter, Comparer.GetAveragePeakWidth(features), fileID, peak);
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
