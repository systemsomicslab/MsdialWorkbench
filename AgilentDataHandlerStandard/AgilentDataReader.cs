using Agilent.MassSpectrometry.CommonControls.AgtFolderSelectionDialog;
using Agilent.MassSpectrometry.MIDAC;
using Riken.Metabolomics.RawData;
using Riken.Metabolomics.RawDataHandlerCommon.Parser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Riken.Metabolomics.AgilentDataHandler.Parser {
    public class AgilentMidacDataReader {

        public string RawfilePath { get; private set; }
        public int SpectraCount { get; private set; }
        public int TotalSpectraCount { get; private set; }
        public List<RAW_SourceFileInfo> SourceFiles { get; private set; }
        public List<RAW_Sample> Samples { get; private set; }
        public List<Raw_CalibrationInfo> CalibrantInfos { get; private set; }
        public DateTime StartTimeStamp { get; private set; }
        public List<RAW_Spectrum> SpectraList { get; private set; }
        public List<RAW_Spectrum> AccumulatedMs1SpectrumList { get; private set; }
        public List<RAW_Chromatogram> ChromatogramsList { get; private set; }
        public bool IsMs1Profile { get; private set; }
        public bool IsMs2Profile { get; private set; }

        IMidacImsReader m_reader;

        // Items used in browsing for data files
        private string m_currentFilePath;
        public RAW_Measurement ReadAgilentMidacDotD(string filepath, int fileid, double peakCutOff, out string errorString, BackgroundWorker bgWorker = null) {

            this.SourceFiles = new List<RAW_SourceFileInfo>();
            this.Samples = new List<RAW_Sample>();
            this.CalibrantInfos = new List<Raw_CalibrationInfo>();
            this.StartTimeStamp = new DateTime();
            this.SpectraList = new List<RAW_Spectrum>();
            this.AccumulatedMs1SpectrumList = new List<RAW_Spectrum>();
            this.ChromatogramsList = null;
            this.IsMs1Profile = true;
            this.IsMs2Profile = true;
            this.SpectraCount = 0;

            errorString = string.Empty;
            var filename = System.IO.Path.GetFileName(filepath);
            if (!MidacFileAccess.FileHasImsData(filepath)) {
                errorString += filename + " does not contain ion mobility data.";
                return null;
            }
            if (m_reader != null)
                m_reader.Close();
            m_reader = null;
            // Returns a reader alredy opened to the specified file path
            m_reader = MidacFileAccess.ImsDataReader(filepath);
            m_currentFilePath = filepath;

            // Get overall file metadata
            IMidacFileInfo fileInfo = m_reader.FileInfo;
            var isCentroidExist = m_reader.HasPeakTfsSpectra ? true : false;
            var totalFrameCount = fileInfo.NumFrames;
            this.TotalSpectraCount = totalFrameCount;
            var maxDriftBinsPerFrame = fileInfo.MaxNonTfsMsPerFrame;
            var isHasSingleFieldCcsInformation = m_reader.HasSingleFieldCcsInformation ? true : false;
            var isAllIonData = fileInfo.HasHiLoFragData ? true : false;
            // Force read of some data before we start; gets some assertions out of the way.
            IMidacUnitConverter converter = m_reader.FrameInfo(1).FrameUnitConverter;
            int minDBin = 0;
            int maxDBin = maxDriftBinsPerFrame - 1;
            var midacspecFormat = isCentroidExist ? MidacSpecFormat.Peak : MidacSpecFormat.Profile;

            var singleFieldBeta = -1.0;
            var singleFieldTFix = -1.0;
            if (isHasSingleFieldCcsInformation) {
                IImsCcsInfoReader imsCcsCalInfo = new ImsCcsInfoReader();
                imsCcsCalInfo.Read(m_currentFilePath);
                singleFieldBeta = imsCcsCalInfo.Beta;
                singleFieldTFix = imsCcsCalInfo.TFix;
            }

            RAW_Spectrum specObject = null;

            if (isAllIonData) {
                IMidacMsFiltersChrom chromFilters = MidacFileAccess.DefaultMsChromFilters;
                chromFilters.ApplicableFilters = ApplicableFilters.FragmentationClass;
                chromFilters.FragmentationClass = FragmentationClass.HighEnergy;
                var numHighFrames = m_reader.FilteredFrameNumbers(chromFilters);

                chromFilters.FragmentationClass = FragmentationClass.LowEnergy;
                var numLowFrames = m_reader.FilteredFrameNumbers(chromFilters);

                for (int i = 0; i < numLowFrames.Length; i++) {
                    var frame = numLowFrames[i];
                    //IMidacSpecDataMs totalFrameSpec = null;
                    //if (isCentroidExist)
                    //    totalFrameSpec = m_reader.PeakDetectedTotalFrameMs(frame, filter, true); //
                    //else
                    //    totalFrameSpec = m_reader.ProfileTotalFrameMs(midacspecFormat, frame); //
                    //readSpectra(frame, totalFrameSpec, out specObject);
                    //if (specObject != null) {
                    //    specObject.MsLevel = 1; // just in case
                    //    this.AccumulatedMs1SpectrumList.Add(specObject);
                    //}
                    var accumulatedMassBin = new Dictionary<int, double[]>();
                    //var accumulatedMassIntensityArray = new double[300000000];
                    for (int dbin = minDBin; dbin <= maxDBin; dbin++) {
                        var driftSpec = m_reader.FrameMs(frame, dbin, midacspecFormat, true) as IMidacSpecDataMs;
                        //readSpectra(frame, dbin, driftSpec, peakCutOff, ref accumulatedMassIntensityArray, out specObject);
                        readSpectra(frame, dbin, driftSpec, peakCutOff, accumulatedMassBin, out specObject);
                        if (specObject != null && specObject.Spectrum.Length > 0) {
                            specObject.MsLevel = 1;
                            this.SpectraList.Add(specObject);
                        }
                    }
                    setAccumulatedMs1Spectra(m_reader, frame, accumulatedMassBin);
                    this.SpectraCount++;
                    if (bgWorker != null)
                        progressReports(this.SpectraCount, this.TotalSpectraCount, bgWorker);
                    else {
                        if (!Console.IsOutputRedirected) {
                            Console.Write("{0} / {1}", this.SpectraCount, this.TotalSpectraCount);
                            Console.SetCursorPosition(0, Console.CursorTop);
                        }
                        else {
                            Console.WriteLine("{0} / {1}", this.SpectraCount, this.TotalSpectraCount);
                        }
                    }
                }

                // for high energy frames, it will be recognized as MS2
                for (int i = 0; i < numHighFrames.Length; i++) {

                    var frame = numHighFrames[i];
                    for (int dbin = minDBin; dbin <= maxDBin; dbin++) {
                        var driftSpec = m_reader.FrameMs(frame, dbin, midacspecFormat, true) as IMidacSpecDataMs;
                        readSpectra(frame, dbin, driftSpec, peakCutOff, out specObject);
                        if (specObject != null && specObject.Spectrum.Length > 0) {
                            // To access the CE range:
                            //      IDoubleRange ceRange = specData.FragmentationEnergyRange;
                            //driftSpec.
                            specObject.MsLevel = 2;
                            var precurosr = new RAW_PrecursorIon() {
                                SelectedIonMz = 1000, //temp
                                IsolationTargetMz = 1000, //temp
                                IsolationWindowLowerOffset = 1000, //temp
                                IsolationWindowUpperOffset = 1000, //temp
                                CollisionEnergy = driftSpec.FragmentationEnergyRange.Center,
                                CollisionEnergyUnit = Units.ElectronVolt,
                                Dissociationmethod = DissociationMethods.CID
                            };
                            specObject.Precursor = precurosr;

                            this.SpectraList.Add(specObject);
                        }
                    }
                    this.SpectraCount++;
                    if (bgWorker != null)
                        progressReports(this.SpectraCount, this.TotalSpectraCount, bgWorker);
                    else {
                        if (!Console.IsOutputRedirected) {
                            Console.Write("{0} / {1}", this.SpectraCount, this.TotalSpectraCount);
                            Console.SetCursorPosition(0, Console.CursorTop);
                        }
                        else {
                            Console.WriteLine("{0} / {1}", this.SpectraCount, this.TotalSpectraCount);
                        }
                    }
                }
            }
            else {
                // parse conventional LC-IM-MS data
                var frameNumbers = new int[totalFrameCount];
                for (int i = 0; i < frameNumbers.Length; i++)
                    frameNumbers[i] = i + 1;
                for (int i = 0; i < frameNumbers.Length; i++) {
                    var frame = frameNumbers[i];
                    //var accumulatedMassIntensityArray = new double[300000000];
                    var accumulatedMassBin = new Dictionary<int, double[]>();
                    //var totalFrameSpec = m_reader.ProfileTotalFrameMs(midacspecFormat, frame); //
                    //readSpectra(frame, totalFrameSpec, out specObject);
                    //if (specObject != null && specObject.MsLevel == 1) {
                    //    this.AccumulatedMs1SpectrumList.Add(specObject);
                    //}

                    for (int dbin = minDBin; dbin <= maxDBin; dbin++) {
                        var driftSpec = m_reader.FrameMs(frame, dbin, midacspecFormat, true) as IMidacSpecDataMs;
                        readSpectra(frame, dbin, driftSpec, peakCutOff, accumulatedMassBin, out specObject);
                        if (specObject != null && specObject.Spectrum.Length > 0)
                            this.SpectraList.Add(specObject);
                    }

                    if (m_reader.FrameInfo(frame).SpectrumDetails.MsLevel == MsLevel.MS) {
                        setAccumulatedMs1Spectra(m_reader, frame, accumulatedMassBin);
                    }

                    this.SpectraCount++;
                    if (bgWorker != null)
                        progressReports(this.SpectraCount, this.TotalSpectraCount, bgWorker);
                    else {
                        if (!Console.IsOutputRedirected) {
                            Console.Write("{0} / {1}", this.SpectraCount, this.TotalSpectraCount);
                            Console.SetCursorPosition(0, Console.CursorTop);
                        }
                        else {
                            Console.WriteLine("{0} / {1}", this.SpectraCount, this.TotalSpectraCount);
                        }
                    }
                }
            }
            m_reader.Close(); //file close

            finalizationRawObject();

            if (this.SourceFiles.Count == 0) {
                this.SourceFiles.Add(new RAW_SourceFileInfo() { Id = fileid.ToString(),
                    Location = filepath, Name = System.IO.Path.GetFileNameWithoutExtension(filepath) });
            }
            if (this.Samples.Count == 0) {
                this.Samples.Add(new RAW_Sample() { Id = fileid.ToString(),
                    Name = System.IO.Path.GetFileNameWithoutExtension(filepath) });
            }
            return new RAW_Measurement() {
                SourceFileInfo = this.SourceFiles[0],
                Sample = this.Samples[0],
                SpectrumList = this.SpectraList,
                CalibrantInfo = new Raw_CalibrationInfo() { AgilentBeta = singleFieldBeta, AgilentTFix = singleFieldTFix, IsAgilentIM = true },
                AccumulatedSpectrumList = this.AccumulatedMs1SpectrumList,
                ChromatogramList = this.ChromatogramsList,
                Method = isAllIonData == true ? MeasurmentMethod.IM_ALLIONS : MeasurmentMethod.IM_DDA
            };
        }

        private void setAccumulatedMs1Spectra(IMidacImsReader m_reader, int frame, Dictionary<int, double[]> specMassBin) {
            var frameInfo = m_reader.FrameInfo(frame);
            var rt = frameInfo.AcqTimeRange.Center;
            var mslevel = 1;
            var polarity = frameInfo.SpectrumDetails.IonPolarity == Polarity.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;
            var accuSpec = new RAW_Spectrum() {
                ScanNumber = frame,
                ScanStartTime = rt,
                ScanStartTimeUnit = Units.Minute,
                MsLevel = mslevel,
                CollisionEnergy = 0.0,
                ScanPolarity = polarity,
                Precursor = null
            };
            SpectrumParser.setSpectrumProperties(accuSpec, specMassBin);
            this.AccumulatedMs1SpectrumList.Add(accuSpec);
        }

        private void progressReports(int currentProgress, int maxProgress, BackgroundWorker bgWorker) {
            var progress = (double)currentProgress / (double)maxProgress * 100.0;
            if (bgWorker != null)
                bgWorker.ReportProgress((int)progress);
        }

        private void finalizationRawObject() {

            this.SpectraList = this.SpectraList.OrderBy(n => n.ScanNumber).ThenBy(n => n.DriftScanNumber).ToList();
            for (int i = 0; i < this.SpectraList.Count; i++) {
                this.SpectraList[i].Index = i;
                this.SpectraList[i].OriginalIndex = i;
            }

            // finalize for storing accumulated spectra
            if (this.AccumulatedMs1SpectrumList != null && this.AccumulatedMs1SpectrumList.Count > 0) {
                this.AccumulatedMs1SpectrumList = this.AccumulatedMs1SpectrumList.OrderBy(n => n.ScanNumber).ToList();
                var tempSpeclist = new List<RAW_Spectrum>();
                var lastFrameID = -1;
                foreach (var spec in this.SpectraList.Where(n => n.MsLevel == 1)) {
                    if (spec.ScanNumber == lastFrameID) {

                    }
                    else {
                        tempSpeclist.Add(spec);
                        lastFrameID = spec.ScanNumber;
                    }
                }

                var counter = 0;
                foreach (var spec in this.AccumulatedMs1SpectrumList) {
                    // Console.WriteLine("Scan num {0}, RT {1}, Spec num {2}, ms level {3}", spec.ScanNumber,
                    // spec.ScanStartTime, spec.MsLevel, spec.Spectrum.Length);
                    spec.Index = counter;
                    counter++;
                    foreach (var tSpec in tempSpeclist) {
                        if (spec.ScanNumber == tSpec.ScanNumber) {
                            spec.OriginalIndex = tSpec.Index;
                            //Console.WriteLine("Spec index {0}, spec original index {1}", spec.Index, spec.OriginalIndex);
                            break;
                        }
                    }
                }
            }

        }

        //private void readSpectra(int frame, IMidacSpecDataMs spec, ref double[] accSpec, out RAW_Spectrum specObject) {
        //    specObject = null;

        //    if (spec == null) return;
        //    if (spec.YArray == null || spec.NonZeroPoints <= 0) return;

        //    var mzArray = spec.XArray;
        //    var intensityArray = spec.YArray;
        //    var rt = spec.AcquiredTimeRanges[0].Center;
        //    var mslevel = spec.MsLevel == MsLevel.MS ? 1 : 2;
        //    var ionmode = spec.IonPolarity == Polarity.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;
        //    specObject = new RAW_Spectrum() {
        //        ScanNumber = frame,
        //        ScanStartTime = rt,
        //        ScanStartTimeUnit = Units.Minute,
        //        MsLevel = mslevel,
        //        CollisionEnergy = 0,
        //        ScanPolarity = ionmode,
        //        Precursor = null
        //    };
        //    SpectrumParser.setSpectrumProperties(specObject, mzArray, intensityArray, ref accSpec);
        //}

        private void readSpectra(int frame, int dbin, IMidacSpecDataMs spec, double peakCutOff, Dictionary<int, double[]> accSpec, out RAW_Spectrum specObject) {
            specObject = null;

            if (spec == null) return;
            if (spec.YArray == null || spec.NonZeroPoints <= 0) return;

            var mzList = new List<double>();
            var intensityList = new List<double>();
            parseMzIntensityArray(spec, out mzList, out intensityList);

            var rt = spec.AcquiredTimeRanges[0].Center;
            var dt = spec.DriftTimeRanges[0].Center;
            var mslevel = spec.MsLevel == MsLevel.MS ? 1 : 2;
            var ionmode = spec.IonPolarity == Polarity.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;
            specObject = new RAW_Spectrum() {
                ScanNumber = frame,
                ScanStartTime = rt,
                ScanStartTimeUnit = Units.Minute,
                MsLevel = mslevel,
                ScanPolarity = ionmode,
                CollisionEnergy = 0.0,
                Precursor = null,
                DriftTime = dt,
                DriftTimeUnit = Units.Milliseconds,
                DriftScanNumber = dbin
            };
            SpectrumParser.setSpectrumProperties(specObject, mzList, intensityList, peakCutOff, accSpec);
        }

        private void parseMzIntensityArray(IMidacSpecDataMs spec, out List<double> mzList, out List<double> intensityList) {
            var mzArray = spec.XArray;
            var intensityArray = spec.YArray;

            mzList = new List<double>();
            intensityList = new List<double>();
            for (int i = 0; i < intensityArray.Length; i++) {
                if (intensityArray[i] > 0.0) {
                    mzList.Add(mzArray[i]);
                    intensityList.Add(intensityArray[i]);
                }
            }
        }

        private void readSpectra(int frame, int dbin, IMidacSpecDataMs spec, double peakCutOff, out RAW_Spectrum specObject) {
            specObject = null;

            if (spec == null) return;
            if (spec.YArray == null || spec.NonZeroPoints <= 0) return;

            var mzList = new List<double>();
            var intensityList = new List<double>();
            parseMzIntensityArray(spec, out mzList, out intensityList);

            var rt = spec.AcquiredTimeRanges[0].Center;
            var dt = spec.DriftTimeRanges[0].Center;
            var mslevel = spec.MsLevel == MsLevel.MS ? 1 : 2;
            var ionmode = spec.IonPolarity == Polarity.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;
            specObject = new RAW_Spectrum() {
                ScanNumber = frame,
                ScanStartTime = rt,
                ScanStartTimeUnit = Units.Minute,
                MsLevel = mslevel,
                ScanPolarity = ionmode,
                CollisionEnergy = 0.0,
                Precursor = null,
                DriftTime = dt,
                DriftTimeUnit = Units.Milliseconds,
                DriftScanNumber = dbin
            };
            SpectrumParser.setSpectrumProperties(specObject, mzList, intensityList, peakCutOff);
        }
    }
}
