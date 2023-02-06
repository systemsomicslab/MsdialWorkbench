using Msdial.Gcms.Dataprocess.Algorithm;
using Rfx.Riken.OsakaUniv;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using CompMs.Common.DataObj;

namespace Msdial.Gcms.Dataprocess.Utility
{
    public sealed class DataAccessGcUtility
    {
        private DataAccessGcUtility() { }

        #region raw data access
        public static List<RawSpectrum> GetRdamSpectrumList(RawDataAccess rawDataAccess)
        {
            var mes = rawDataAccess.GetMeasurement();
            if (mes == null) return null;
            return mes.SpectrumList;
        }

        public static List<double[]> GetChromatogramPeaklist(AnalysisFileBean file, float rtBegin, float rtEnd, float targetMz, float mzTolerance, IonMode ionmode, RdamPropertyBean rdamProperty)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var peaklist = new List<double[]>();
           
            var filepath = file.AnalysisFilePropertyBean.AnalysisFilePath;
            var fileID = rdamProperty.RdamFilePath_RdamFileID[filepath];
            var measurementID = rdamProperty.RdamFileContentBeanCollection[fileID].FileID_MeasurementID[file.AnalysisFilePropertyBean.AnalysisFileId];


            using (var rawDataAccess = new RawDataAccess(filepath, measurementID, false, false, true)) {
                //Console.WriteLine("open stream: {0}", sw.ElapsedMilliseconds);
                var mes = rawDataAccess.GetMeasurement();
                //Console.WriteLine("got functions: {0}", sw.ElapsedMilliseconds);
                peaklist = GetMs1SlicePeaklist(mes.SpectrumList, targetMz, mzTolerance, rtBegin, rtEnd, ionmode);
            }
            sw.Stop();

            return peaklist;
        }

        public static List<double[]> GetTicPeaklist(List<RawSpectrum> spectrumList, IonMode ionmode)
        {
            var peaklist = new List<double[]>();
            RawSpectrum spectrum;
            RawPeakElement[] massSpectra;
            double sum = 0, maxIntensityMz, maxMass;
            var scanPolarity = ionmode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;

            for (int i = 0; i < spectrumList.Count; i++)
            {
                spectrum = spectrumList[i];

                if (spectrum.MsLevel > 1) continue;
                if (spectrum.ScanPolarity != scanPolarity) continue;

                sum = 0;
                massSpectra = spectrum.Spectrum;
                maxIntensityMz = double.MinValue;
                maxMass = -1;

                for (int j = 0; j < massSpectra.Length; j++)
                {
                    sum += massSpectra[j].Intensity; if (maxIntensityMz < massSpectra[j].Intensity) { maxIntensityMz = massSpectra[j].Intensity; maxMass = massSpectra[j].Mz; }
                }
                peaklist.Add(new double[] { i, spectrum.ScanStartTime, maxMass, sum });
            }
            return peaklist;
        }

        public static List<double[]> GetMs1PeaklistAsBPC(List<RawSpectrum> spectrumList, float focusedMass, float ms1Tolerance, float retentionTimeBegin, float retentionTimeEnd, IonMode ionmode)
        {
            var peaklist = new List<double[]>();
            RawSpectrum spectrum;
            RawPeakElement[] massSpectra;
            int startIndex = 0;
            double sum = 0, maxIntensityMz, maxMass;

            var scanPolarity = ionmode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;

            for (int i = 0; i < spectrumList.Count; i++)
            {
                spectrum = spectrumList[i];

                if (spectrum.MsLevel > 1) continue;
                if (spectrum.ScanPolarity != scanPolarity) continue;
                if (spectrum.ScanStartTime < retentionTimeBegin) continue;
                if (spectrum.ScanStartTime > retentionTimeEnd) break;

                sum = 0;
                massSpectra = spectrum.Spectrum;
                maxIntensityMz = double.MinValue;
                maxMass = focusedMass;
                startIndex = GetMs1StartIndex(focusedMass, ms1Tolerance, massSpectra);

                for (int j = startIndex; j < massSpectra.Length; j++)
                {
                    if (massSpectra[j].Mz < focusedMass - ms1Tolerance) continue;
                    else if (focusedMass - ms1Tolerance <= massSpectra[j].Mz && massSpectra[j].Mz <= focusedMass + ms1Tolerance) { sum += massSpectra[j].Intensity; if (maxIntensityMz < massSpectra[j].Intensity) { maxIntensityMz = massSpectra[j].Intensity; maxMass = massSpectra[j].Mz; } }
                    else if (massSpectra[j].Mz > focusedMass + ms1Tolerance) break;
                }

                if (maxIntensityMz < 0) maxIntensityMz = 0;
                peaklist.Add(new double[] { i, spectrum.ScanStartTime, maxMass, sum });
            }
            return peaklist;
        }

        public static List<double[]> GetMs1PeaklistAsBPC(List<RawSpectrum> spectrumList, float ms1Tolerance, IonMode ionmode) {
            var peaklist = new List<double[]>();
            RawSpectrum spectrum;
            RawPeakElement[] massSpectra;
            int startIndex = 0;
            double sum = 0, maxIntensityMz, maxMass;

            var scanPolarity = ionmode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;

            for (int i = 0; i < spectrumList.Count; i++) {
                spectrum = spectrumList[i];

                if (spectrum.MsLevel > 1) continue;
                if (spectrum.ScanPolarity != scanPolarity) continue;

                sum = 0;
                massSpectra = spectrum.Spectrum;
                maxIntensityMz = double.MinValue;
                var focusedMass = (float)spectrum.BasePeakMz;
                startIndex = GetMs1StartIndex(focusedMass, ms1Tolerance, massSpectra);

                for (int j = startIndex; j < massSpectra.Length; j++) {
                    if (massSpectra[j].Mz < focusedMass - ms1Tolerance) continue;
                    else if (focusedMass - ms1Tolerance <= massSpectra[j].Mz && massSpectra[j].Mz <= focusedMass + ms1Tolerance) { sum += massSpectra[j].Intensity; if (maxIntensityMz < massSpectra[j].Intensity) { maxIntensityMz = massSpectra[j].Intensity; maxMass = massSpectra[j].Mz; } }
                    else if (massSpectra[j].Mz > focusedMass + ms1Tolerance) break;
                }

                if (maxIntensityMz < 0) maxIntensityMz = 0;
                peaklist.Add(new double[] { i, spectrum.ScanStartTime, focusedMass, sum });
            }
            return peaklist;
        }
        #endregion

        #region // for survey MS1 scan data
        public static float[] GetMs1ScanRange(List<RawSpectrum> spectrumList, IonMode ionmode)
        {
            float minMz = float.MaxValue, maxMz = float.MinValue;
            var scanPolarity = ionmode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;

            for (int i = 0; i < spectrumList.Count; i++)
            {
                if (spectrumList[i].MsLevel > 1) continue;
                if (spectrumList[i].ScanPolarity != scanPolarity) continue;
                if (spectrumList[i].LowestObservedMz < minMz) minMz = (float)spectrumList[i].LowestObservedMz;
                if (spectrumList[i].HighestObservedMz > maxMz) maxMz = (float)spectrumList[i].HighestObservedMz;
            }
            return new float[] { minMz, maxMz };
        }

        public static List<Peak> GetBaselineCorrectedPeaklistByMassAccuracy(List<RawSpectrum> spectrumList, float centralRt, float rtBegin, float rtEnd,
            float quantMass, AnalysisParamOfMsdialGcms param) {
            var peaklist = new List<Peak>();
            var scanPolarity = param.IonMode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;
            var sliceWidth = param.MassAccuracy;

            for (int i = 0; i < spectrumList.Count; i++) {
                var spectrum = spectrumList[i];

                if (spectrum.MsLevel > 1) continue;
                if (spectrum.ScanPolarity != scanPolarity) continue;
                if (spectrum.ScanStartTime < rtBegin) continue;
                if (spectrum.ScanStartTime > rtEnd) break;

                var massSpectra = spectrum.Spectrum;

                var sum = 0.0;
                var maxIntensityMz = double.MinValue;
                var maxMass = quantMass;
                var startIndex = DataAccessGcUtility.GetMs1StartIndex(quantMass, sliceWidth, massSpectra);

                for (int j = startIndex; j < massSpectra.Length; j++) {
                    if (massSpectra[j].Mz < quantMass - sliceWidth) continue;
                    else if (quantMass - sliceWidth <= massSpectra[j].Mz && massSpectra[j].Mz < quantMass + sliceWidth) {
                        sum += massSpectra[j].Intensity;
                        if (maxIntensityMz < massSpectra[j].Intensity) {
                            maxIntensityMz = massSpectra[j].Intensity; maxMass = (float)massSpectra[j].Mz;
                        }
                    }
                    else if (massSpectra[j].Mz >= quantMass + sliceWidth) break;
                }
                peaklist.Add(new Peak() { ScanNumber = spectrum.ScanNumber, RetentionTime = spectrum.ScanStartTime, Mz = maxMass, Intensity = sum });
            }

            var minLeftIntensity = double.MaxValue;
            var minRightIntensity = double.MaxValue;
            var minLeftID = 0;
            var minRightID = peaklist.Count - 1;

            //searching local left minimum
            for (int i = 0; i < peaklist.Count - 1; i++) {
                var peak = peaklist[i];
                if (peak.RetentionTime >= centralRt) break;
                if (peak.Intensity < minLeftIntensity) {
                    minLeftIntensity = peak.Intensity;
                    minLeftID = i;
                }
            }

            //searching local right minimum
            for (int i = peaklist.Count - 1; i >= 0; i--) {
                var peak = peaklist[i];
                if (peak.RetentionTime <= centralRt) break;
                if (peak.Intensity < minRightIntensity) {
                    minRightIntensity = peak.Intensity;
                    minRightID = i;
                }
            }

            var baselineCorrectedPeaklist = GetBaselineCorrectedPeaklist(peaklist, minLeftID, minRightID);

            return baselineCorrectedPeaklist;
        }

        public static List<Peak> GetBaselineCorrectedPeaklist(List<Peak> peaklist, int minLeftID, int minRightID) {
            var baselineCorrectedPeaklist = new List<Peak>();
            if (peaklist == null || peaklist.Count == 0) return baselineCorrectedPeaklist;

            double coeff = (peaklist[minRightID].Intensity - peaklist[minLeftID].Intensity) /
                (peaklist[minRightID].RetentionTime - peaklist[minLeftID].RetentionTime);
            double intercept = (peaklist[minRightID].RetentionTime * peaklist[minLeftID].Intensity -
                peaklist[minLeftID].RetentionTime * peaklist[minRightID].Intensity) /
                (peaklist[minRightID].RetentionTime - peaklist[minLeftID].RetentionTime);
            double correctedIntensity = 0;
            for (int i = 0; i < peaklist.Count; i++) {
                correctedIntensity = peaklist[i].Intensity - (int)(coeff * peaklist[i].RetentionTime + intercept);
                if (correctedIntensity >= 0)
                    baselineCorrectedPeaklist.Add(
                        new Peak() {
                            ScanNumber = peaklist[i].ScanNumber,
                            RetentionTime = peaklist[i].RetentionTime,
                            Mz = peaklist[i].Mz,
                            Intensity = correctedIntensity
                        });
                else
                    baselineCorrectedPeaklist.Add(
                        new Peak() {
                            ScanNumber = peaklist[i].ScanNumber,
                            RetentionTime = peaklist[i].RetentionTime,
                            Mz = peaklist[i].Mz,
                            Intensity = 0
                        });
            }

            return baselineCorrectedPeaklist;
        }


        public static List<double[]> GetMs1SlicePeaklist(List<RawSpectrum> spectrumList, float focusedMass, float sliceWidth, 
            float rtBegin, float rtEnd, IonMode ionmode)
        {
            if (spectrumList == null) return null;
            var peaklist = new List<double[]>();
            
            var startIndex = 0;
            double sum = 0, maxIntensityMz, maxMass;
            var scanPolarity = ionmode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;

            for (int i = 0; i < spectrumList.Count; i++)
            {
                var spectrum = spectrumList[i];

                if (spectrum.MsLevel > 1) continue;
                if (spectrum.ScanPolarity != scanPolarity) continue;
                if (spectrum.ScanStartTime < rtBegin) continue;
                if (spectrum.ScanStartTime > rtEnd) break;

                var massSpectra = spectrum.Spectrum;

                sum = 0;
                maxIntensityMz = double.MinValue;
                maxMass = focusedMass;
                startIndex = GetMs1StartIndex(focusedMass, sliceWidth, massSpectra);


                for (int j = startIndex; j < massSpectra.Length; j++)
                {
                    if (massSpectra[j].Mz < focusedMass - sliceWidth) continue;
                    else if (focusedMass - sliceWidth <= massSpectra[j].Mz && massSpectra[j].Mz < focusedMass + sliceWidth) { sum += massSpectra[j].Intensity; if (maxIntensityMz < massSpectra[j].Intensity) { maxIntensityMz = massSpectra[j].Intensity; maxMass = massSpectra[j].Mz; } }
                    else if (massSpectra[j].Mz >= focusedMass + sliceWidth) break;
                }
                peaklist.Add(new double[] { i, spectrum.ScanStartTime, maxMass, sum });
            }
            return peaklist;
        }

        public static int GetMs1StartIndex(float focusedMass, float ms1Tolerance, RawPeakElement[] massSpectra)
        {
            if (massSpectra == null || massSpectra.Length == 0) return 0;
            float targetMass = focusedMass - ms1Tolerance;
            int startIndex = 0, endIndex = massSpectra.Length - 1;
            int counter = 0;
            while (counter < 10)
            {
                if (massSpectra[startIndex].Mz <= targetMass && targetMass < massSpectra[(startIndex + endIndex) / 2].Mz)
                {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (massSpectra[(startIndex + endIndex) / 2].Mz <= targetMass && targetMass < massSpectra[endIndex].Mz)
                {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        public static int GetMs1StartIndex(float focusedMass, float ms1Tolerance, List<double[]> ms1Spectra)
        {
            if (ms1Spectra.Count == 0) return 0;
            float targetMass = focusedMass - ms1Tolerance;
            int startIndex = 0, endIndex = ms1Spectra.Count - 1;
            int counter = 0;
            while (counter < 10)
            {
                if (ms1Spectra[startIndex][0] <= targetMass && targetMass < ms1Spectra[(startIndex + endIndex) / 2][0])
                {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (ms1Spectra[(startIndex + endIndex) / 2][0] <= targetMass && targetMass < ms1Spectra[endIndex][0])
                {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        public static int GetMs1StartIndex(int targetScan, List<PeakAreaBean> peakAreaBeanList)
        {
            int startIndex = 0, endIndex = peakAreaBeanList.Count - 1;

            int counter = 0;
            while (counter < 5)
            {
                if (peakAreaBeanList[startIndex].ScanNumberAtPeakTop <= targetScan && targetScan < peakAreaBeanList[(startIndex + endIndex) / 2].ScanNumberAtPeakTop)
                {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (peakAreaBeanList[(startIndex + endIndex) / 2].ScanNumberAtPeakTop <= targetScan && targetScan < peakAreaBeanList[endIndex].ScanNumberAtPeakTop)
                {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        /// <summary>
        /// the collection must be sorted by accurate mass
        /// </summary>
        public static int GetScanStartIndexByMz(float targetMass, List<PeakAreaBean> peakAreaBeans)
        {
            int startIndex = 0, endIndex = peakAreaBeans.Count - 1;

            int counter = 0;
            while (counter < 5) {
                if (peakAreaBeans[startIndex].AccurateMass <= targetMass &&
                    targetMass < peakAreaBeans[(startIndex + endIndex) / 2].AccurateMass) {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (peakAreaBeans[(startIndex + endIndex) / 2].AccurateMass <= targetMass &&
                    targetMass < peakAreaBeans[endIndex].AccurateMass) {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }
        #endregion

        #region // for smoothing
        public static List<double[]> GetSmoothedPeaklist(List<double[]> peaklist, SmoothingMethod smoothingMethod, int smoothingLevel)
        {
            var smoothedPeaklist = new List<double[]>();

            switch (smoothingMethod)
            {
                case SmoothingMethod.SimpleMovingAverage:
                    smoothedPeaklist = Smoothing.SimpleMovingAverage(peaklist, smoothingLevel);
                    break;
                case SmoothingMethod.LinearWeightedMovingAverage:
                    smoothedPeaklist = Smoothing.LinearWeightedMovingAverage(peaklist, smoothingLevel);
                    break;
                case SmoothingMethod.SavitzkyGolayFilter:
                    smoothedPeaklist = Smoothing.SavitxkyGolayFilter(peaklist, smoothingLevel);
                    break;
                case SmoothingMethod.BinomialFilter:
                    smoothedPeaklist = Smoothing.BinomialFilter(peaklist, smoothingLevel);
                    break;
                case SmoothingMethod.LowessFilter:
                    smoothedPeaklist = Smoothing.LowessFilter(peaklist, smoothingLevel);
                    break;
                case SmoothingMethod.LoessFilter:
                    smoothedPeaklist = Smoothing.LoessFilter(peaklist, smoothingLevel);
                    break;
                default:
                    smoothedPeaklist = Smoothing.LinearWeightedMovingAverage(peaklist, smoothingLevel);
                    break;
            }

            return smoothedPeaklist;
        }

        public static List<Peak> GetSmoothedPeaklist(List<Peak> peaklist, SmoothingMethod smoothingMethod, int smoothingLevel)
        {
            var smoothedPeaklist = new List<Peak>();

            switch (smoothingMethod)
            {
                case SmoothingMethod.SimpleMovingAverage:
                    smoothedPeaklist = Smoothing.SimpleMovingAverage(peaklist, smoothingLevel);
                    break;
                case SmoothingMethod.LinearWeightedMovingAverage:
                    smoothedPeaklist = Smoothing.LinearWeightedMovingAverage(peaklist, smoothingLevel);
                    break;
                case SmoothingMethod.SavitzkyGolayFilter:
                    smoothedPeaklist = Smoothing.SavitxkyGolayFilter(peaklist, smoothingLevel);
                    break;
                case SmoothingMethod.BinomialFilter:
                    smoothedPeaklist = Smoothing.BinomialFilter(peaklist, smoothingLevel);
                    break;
                case SmoothingMethod.LowessFilter:
                    smoothedPeaklist = Smoothing.LowessFilter(peaklist, smoothingLevel);
                    break;
                case SmoothingMethod.LoessFilter:
                    smoothedPeaklist = Smoothing.LoessFilter(peaklist, smoothingLevel);
                    break;
                default:
                    smoothedPeaklist = Smoothing.LinearWeightedMovingAverage(peaklist, smoothingLevel);
                    break;
            }

            return smoothedPeaklist;
        }
        #endregion

        #region // for peak detection
        public static PeakAreaBean GetPeakAreaBean(PeakDetectionResult peakResult)
        {
            if (peakResult == null) return null;

            var peakAreaBean = new PeakAreaBean() {
                AmplitudeOrderValue = peakResult.AmplitudeOrderValue,
                AmplitudeScoreValue = peakResult.AmplitudeScoreValue,
                AreaAboveBaseline = peakResult.AreaAboveBaseline,
                AreaAboveZero = peakResult.AreaAboveZero,
                BasePeakValue = peakResult.BasePeakValue,
                GaussianSimilarityValue = peakResult.GaussianSimilarityValue,
                IdealSlopeValue = peakResult.IdealSlopeValue,
                IntensityAtLeftPeakEdge = peakResult.IntensityAtLeftPeakEdge,
                IntensityAtPeakTop = peakResult.IntensityAtPeakTop,
                IntensityAtRightPeakEdge = peakResult.IntensityAtRightPeakEdge,
                PeakID = peakResult.PeakID,
                PeakPureValue = peakResult.PeakPureValue,
                RtAtLeftPeakEdge = peakResult.RtAtLeftPeakEdge,
                RtAtPeakTop = peakResult.RtAtPeakTop,
                RtAtRightPeakEdge = peakResult.RtAtRightPeakEdge,
                ScanNumberAtLeftPeakEdge = peakResult.ScanNumAtLeftPeakEdge,
                ScanNumberAtPeakTop = peakResult.ScanNumAtPeakTop,
                ScanNumberAtRightPeakEdge = peakResult.ScanNumAtRightPeakEdge,
                ShapenessValue = peakResult.ShapnessValue,
                SymmetryValue = peakResult.SymmetryValue,
                EstimatedNoise = peakResult.EstimatedNoise,
                SignalToNoise = peakResult.SignalToNoise,
                NormalizedValue = -1,
                AlignedRetentionTime = -1,
                TotalScore = -1,
                AccurateMass = -1,
                MetaboliteName = string.Empty,
                AdductIonName = string.Empty,
                LibraryID = -1,
                IsotopeWeightNumber = -1,
                IsotopeParentPeakID = -1,
                AdductParent = -1,
                RtSimilarityValue = -1,
                IsotopeSimilarityValue = -1,
                MassSpectraSimilarityValue = -1,
                ReverseSearchSimilarityValue = -1,
                PresenseSimilarityValue = -1,
                Ms1LevelDatapointNumber = -1,
                Ms2LevelDatapointNumber = -1,
                AdductIonAccurateMass = -1,
                AdductIonXmer = -1,
                AdductIonChargeNumber = -1,
            };

            return peakAreaBean;
        }
        #endregion

        #region // for centroiding

        public static List<double[]> GetCentroidMasasSpectra(List<RawSpectrum> spectrumList, DataType dataType, int msScanPoint, float massBin, float amplitudeThresh, float mzBegin, float mzEnd)
        {
            if (msScanPoint < 0) return new List<double[]>();

            var spectra = new List<double[]>();
            var spectrum = spectrumList[msScanPoint];
            var massSpectra = spectrum.Spectrum;

            for (int i = 0; i < massSpectra.Length; i++)
            {
                if (massSpectra[i].Mz < mzBegin) continue;
                if (massSpectra[i].Mz > mzEnd) continue;
                spectra.Add(new double[] { massSpectra[i].Mz, massSpectra[i].Intensity });
            }

            if (dataType == DataType.Centroid) return spectra.Where(n => n[1] > amplitudeThresh).ToList();

            if (spectra.Count == 0) return new List<double[]>();

            var centroidedSpectra = SpectralCentroiding.Centroid(spectra, massBin, amplitudeThresh);

            if (centroidedSpectra != null && centroidedSpectra.Count != 0)
                return centroidedSpectra;
            else
                return spectra;
        }

        #endregion

        #region
        public static int GetMS1DecResultListStartIndex(List<MS1DecResult> masterMs1DecResults, float targetRT, float rtTol, bool isRiIndex)
        {
            int startIndex = 0, endIndex = masterMs1DecResults.Count - 1;
            float focusedRT = targetRT - rtTol;
            int counter = 0;

            if (isRiIndex == true)
            {
                while (counter < 5)
                {
                    if (masterMs1DecResults[startIndex].RetentionIndex <= focusedRT && focusedRT < masterMs1DecResults[(startIndex + endIndex) / 2].RetentionIndex)
                    {
                        endIndex = (startIndex + endIndex) / 2;
                    }
                    else if (masterMs1DecResults[(startIndex + endIndex) / 2].RetentionIndex <= focusedRT && focusedRT < masterMs1DecResults[endIndex].RetentionIndex)
                    {
                        startIndex = (startIndex + endIndex) / 2;
                    }
                    counter++;
                }
            }
            else
            {
                while (counter < 5)
                {
                    if (masterMs1DecResults[startIndex].RetentionTime <= focusedRT && focusedRT < masterMs1DecResults[(startIndex + endIndex) / 2].RetentionTime)
                    {
                        endIndex = (startIndex + endIndex) / 2;
                    }
                    else if (masterMs1DecResults[(startIndex + endIndex) / 2].RetentionTime <= focusedRT && focusedRT < masterMs1DecResults[endIndex].RetentionTime)
                    {
                        startIndex = (startIndex + endIndex) / 2;
                    }
                    counter++;
                }
            }
            
            return startIndex;
        }
        #endregion

    }
}
