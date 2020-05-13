using CompMs.Common.Algorithm.ChromSmoothing;
using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Utility {
    public sealed class DataAccess {
        private DataAccess() { }

        // raw data access
        public static List<RawSpectrum> GetAllSpectra(string filepath) {
            List<RawSpectrum> rawSpectra = null;
            using (var rawDataAccess = new RawDataAccess(filepath, 0, false)) {
                var measurment = GetRawDataMeasurement(rawDataAccess);
                rawSpectra = measurment.SpectrumList;
            }
            return rawSpectra;
        }

        public static RawMeasurement GetRawDataMeasurement(RawDataAccess rawDataAccess) {
            var mes = rawDataAccess.GetMeasurement();
            return mes;
        }

        public static List<RawSpectrum> GetAllSpectra(RawDataAccess rawDataAccess) {
            var mes = rawDataAccess.GetMeasurement();
            if (mes == null) return null;
            return mes.SpectrumList;
        }

        // smoother
        public static List<ChromatogramPeak> GetSmoothedPeaklist(List<ChromatogramPeak> peaklist, SmoothingMethod smoothingMethod, int smoothingLevel) {
            var smoothedPeaklist = new List<ChromatogramPeak>();

            switch (smoothingMethod) {
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
       
        // converter
        public static ChromatogramPeakFeature GetChromatogramPeakFeature(PeakDetectionResult result, ChromXType type, ChromXUnit unit, double mass) {
            if (result == null) return null;

            var peakFeature = new ChromatogramPeakFeature() {

                PeakID = result.PeakID,
               
                ChromScanIdLeft = result.ScanNumAtLeftPeakEdge,
                ChromScanIdTop = result.ScanNumAtPeakTop,
                ChromScanIdRight = result.ScanNumAtRightPeakEdge,

                ChromXsLeft = new ChromXs(result.ChromXAxisAtLeftPeakEdge, type, unit),
                ChromXsTop = new ChromXs(result.ChromXAxisAtPeakTop, type, unit),
                ChromXsRight = new ChromXs(result.ChromXAxisAtRightPeakEdge, type, unit),

                ChromXs = new ChromXs(result.ChromXAxisAtPeakTop, type, unit),

                PeakHeightLeft = result.IntensityAtLeftPeakEdge,
                PeakHeightTop = result.IntensityAtPeakTop,
                PeakHeightRight = result.IntensityAtRightPeakEdge,

                PeakAreaAboveZero = result.AreaAboveZero,
                PeakAreaAboveBaseline = result.AreaAboveBaseline,

                Mass = mass,

                PeakShape = new ChromatogramPeakShape() {
                    SignalToNoise = result.SignalToNoise,
                    EstimatedNoise = result.EstimatedNoise,
                    BasePeakValue = result.BasePeakValue,
                    GaussianSimilarityValue = result.GaussianSimilarityValue,
                    IdealSlopeValue = result.IdealSlopeValue,
                    PeakPureValue = result.PeakPureValue,
                    ShapenessValue = result.ShapnessValue,
                    SymmetryValue = result.SymmetryValue,
                    AmplitudeOrderValue = result.AmplitudeOrderValue,
                    AmplitudeScoreValue = result.AmplitudeScoreValue
                }
            };

            return peakFeature;
        }

        public static List<IsotopicPeak> GetIsotopicPeaks(List<RawSpectrum> rawSpectrumList, int scanID, float targetedMz, float massTolerance, int maxIsotopes = 5) {
            if (scanID < 0 || rawSpectrumList == null || scanID > rawSpectrumList.Count - 1) return null;
            var spectrum = rawSpectrumList[scanID].Spectrum;
            var startID = GetMs1StartIndex(targetedMz, massTolerance, spectrum);
            var massDiffBase = MassDiffDictionary.C13_C12;
            var maxIsotopeRange = (double)maxIsotopes;
            var isotopes = new List<IsotopicPeak>();
            for (int i = 0; i < maxIsotopes; i++) {
                isotopes.Add(new IsotopicPeak() {
                    Mass = targetedMz + (double)i * massDiffBase,
                    MassDifferenceFromMonoisotopicIon = (double)i * massDiffBase
                });
            }

            for (int i = startID; i < spectrum.Length; i++) {
                var peak = spectrum[i];
                if (peak.Mz < targetedMz - massTolerance) continue;
                if (peak.Mz > targetedMz + massDiffBase * maxIsotopeRange + massTolerance) break;

                foreach (var isotope in isotopes) {
                    if (Math.Abs(isotope.Mass - peak.Mz) < massTolerance)
                        isotope.RelativeAbundance += peak.Intensity;
                }
            }

            if (isotopes[0].RelativeAbundance <= 0) return null;
            var baseIntensity = isotopes[0].RelativeAbundance;

            foreach (var isotope in isotopes)
                isotope.RelativeAbundance = isotope.RelativeAbundance / baseIntensity * 100;

            return isotopes;
        }
     

        // index access
        public static int GetScanStartIndexByMz(float targetMass, List<ChromatogramPeakFeature> features) {
            int startIndex = 0, endIndex = features.Count - 1;

            int counter = 0;
            while (counter < 5) {
                if (features[startIndex].PrecursorMz <= targetMass &&
                    targetMass < features[(startIndex + endIndex) / 2].PrecursorMz) {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (features[(startIndex + endIndex) / 2].PrecursorMz <= targetMass &&
                    targetMass < features[endIndex].PrecursorMz) {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        public static int GetDatabaseStartIndex(double precursorMz, double ms1Tolerance, List<MoleculeMsReference> mspDB) {
            double targetMass = precursorMz - ms1Tolerance;
            int startIndex = 0, endIndex = mspDB.Count - 1;
            if (targetMass > mspDB[endIndex].PrecursorMz) return endIndex;

            int counter = 0;
            while (counter < 10) {
                if (mspDB[startIndex].PrecursorMz <= targetMass && targetMass < mspDB[(startIndex + endIndex) / 2].PrecursorMz) {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (mspDB[(startIndex + endIndex) / 2].PrecursorMz <= targetMass && targetMass < mspDB[endIndex].PrecursorMz) {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        public static int GetMs1StartIndex(float focusedMass, float ms1Tolerance, RawPeakElement[] massSpectra) {
            if (massSpectra == null || massSpectra.Length == 0) return 0;
            float targetMass = focusedMass - ms1Tolerance;
            int startIndex = 0, endIndex = massSpectra.Length - 1;
            int counter = 0;
            while (counter < 10) {
                if (massSpectra[startIndex].Mz <= targetMass && targetMass < massSpectra[(startIndex + endIndex) / 2].Mz) {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (massSpectra[(startIndex + endIndex) / 2].Mz <= targetMass && targetMass < massSpectra[endIndex].Mz) {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        public static int GetScanStartIndexByRt(float focusedRt, float rtTol, List<RawSpectrum> spectrumList) {

            var targetRt = focusedRt - rtTol;
            int startIndex = 0, endIndex = spectrumList.Count - 1;

            int counter = 0;
            int limit = spectrumList.Count > 50000 ? 20 : 10;
            while (counter < limit) {
                if (spectrumList[startIndex].ScanStartTime <= targetRt && targetRt < spectrumList[(startIndex + endIndex) / 2].ScanStartTime) {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (spectrumList[(startIndex + endIndex) / 2].ScanStartTime <= targetRt && targetRt < spectrumList[endIndex].ScanStartTime) {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        // get chromatograms
        public static List<ChromatogramPeak> GetMs1Peaklist(List<RawSpectrum> spectrumList, float targetMass, float ms1Tolerance, IonMode ionmode, 
            ChromXType type = ChromXType.RT, ChromXUnit unit = ChromXUnit.Min, float chromBegin = float.MinValue, float chromEnd = float.MaxValue) {
            if (spectrumList == null || spectrumList.Count == 0) return null;
            var peaklist = new List<ChromatogramPeak>();
            double sum = 0, maxIntensityMz = double.MinValue, maxMass = -1;
            var scanPolarity = ionmode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;

            foreach (var (spectrum, index) in spectrumList.WithIndex().Where(n => n.Item1.ScanPolarity == scanPolarity && n.Item1.MsLevel == 1)) {
                var chromX = type == ChromXType.Drift ? spectrum.DriftTime : spectrum.ScanStartTime;
                if (chromX < chromBegin) continue;
                if (chromX > chromEnd) break;
                var massSpectra = spectrum.Spectrum;
                var startIndex = GetMs1StartIndex(targetMass, ms1Tolerance, massSpectra);

                //bin intensities for focused MZ +- ms1Tolerance
                for (int j = startIndex; j < massSpectra.Length; j++) {
                    if (massSpectra[j].Mz < targetMass - ms1Tolerance) continue;
                    else if (targetMass - ms1Tolerance <= massSpectra[j].Mz && massSpectra[j].Mz <= targetMass + ms1Tolerance) {
                        sum += massSpectra[j].Intensity;
                        if (maxIntensityMz < massSpectra[j].Intensity) {
                            maxIntensityMz = massSpectra[j].Intensity;
                            maxMass = massSpectra[j].Mz;
                        }
                    }
                    else if (massSpectra[j].Mz > targetMass + ms1Tolerance) break;
                }

                peaklist.Add(new ChromatogramPeak() {  ID = index, ChromXs = new ChromXs(chromX, type, unit), Mass = maxMass, Intensity = sum });
            }

            return peaklist;
        }
      

        public static List<ChromatogramPeak> GetBaselineCorrectedPeaklistByMassAccuracy(List<RawSpectrum> spectrumList, float centralRt, float rtBegin, float rtEnd,
            float quantMass, ParameterBase param) {
            var peaklist = new List<ChromatogramPeak>();
            var scanPolarity = param.IonMode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;
            var sliceWidth = param.CentroidMs1Tolerance;

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
                var startIndex = GetMs1StartIndex(quantMass, sliceWidth, massSpectra);

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
                peaklist.Add(new ChromatogramPeak() { 
                    ID = spectrum.ScanNumber, 
                    ChromXs = new ChromXs(new RetentionTime(spectrum.ScanStartTime)), 
                    Mass = maxMass, 
                    Intensity = sum 
                });
            }

            var minLeftIntensity = double.MaxValue;
            var minRightIntensity = double.MaxValue;
            var minLeftID = 0;
            var minRightID = peaklist.Count - 1;

            //searching local left minimum
            for (int i = 0; i < peaklist.Count - 1; i++) {
                var peak = peaklist[i];
                if (peak.ChromXs.Value >= centralRt) break;
                if (peak.Intensity < minLeftIntensity) {
                    minLeftIntensity = peak.Intensity;
                    minLeftID = i;
                }
            }

            //searching local right minimum
            for (int i = peaklist.Count - 1; i >= 0; i--) {
                var peak = peaklist[i];
                if (peak.ChromXs.Value <= centralRt) break;
                if (peak.Intensity < minRightIntensity) {
                    minRightIntensity = peak.Intensity;
                    minRightID = i;
                }
            }

            var baselineCorrectedPeaklist = GetBaselineCorrectedPeaklist(peaklist, minLeftID, minRightID);

            return baselineCorrectedPeaklist;
        }

        public static List<ChromatogramPeak> GetBaselineCorrectedPeaklist(List<ChromatogramPeak> peaklist, int minLeftID, int minRightID) {
            var baselineCorrectedPeaklist = new List<ChromatogramPeak>();
            if (peaklist == null || peaklist.Count == 0) return baselineCorrectedPeaklist;

            double coeff = (peaklist[minRightID].Intensity - peaklist[minLeftID].Intensity) /
                (peaklist[minRightID].ChromXs.Value - peaklist[minLeftID].ChromXs.Value);
            double intercept = (peaklist[minRightID].ChromXs.Value * peaklist[minLeftID].Intensity -
                peaklist[minLeftID].ChromXs.Value * peaklist[minRightID].Intensity) /
                (peaklist[minRightID].ChromXs.Value - peaklist[minLeftID].ChromXs.Value);
            double correctedIntensity = 0;
            for (int i = 0; i < peaklist.Count; i++) {
                correctedIntensity = peaklist[i].Intensity - (int)(coeff * peaklist[i].ChromXs.Value + intercept);
                if (correctedIntensity >= 0)
                    baselineCorrectedPeaklist.Add(
                        new ChromatogramPeak() {
                            ID = peaklist[i].ID,
                            ChromXs = peaklist[i].ChromXs,
                            Mass = peaklist[i].Mass,
                            Intensity = correctedIntensity
                        });
                else
                    baselineCorrectedPeaklist.Add(
                        new ChromatogramPeak() {
                            ID = peaklist[i].ID,
                            ChromXs = peaklist[i].ChromXs,
                            Mass = peaklist[i].Mass,
                            Intensity = 0
                        });
            }

            return baselineCorrectedPeaklist;
        }

        // get chromatogram (ion mobility data)
        public static List<ChromatogramPeak> GetDriftChromatogramByScanRtMz(List<RawSpectrum> spectrumList,
          int scanID, float rt, float rtWidth, float mz, float mztol) {

            var driftBinToChromPeak = new Dictionary<int, ChromatogramPeak>();
            var driftBinToSpecPeak = new Dictionary<int, SpectrumPeak>();

            //accumulating peaks from peak top to peak left
            for (int i = scanID + 1; i >= 0; i--) {
                var spectrum = spectrumList[i];
                if (spectrum.MsLevel > 1) continue;
                var massSpectra = spectrum.Spectrum;
                var retention = spectrum.ScanStartTime;
                var driftTime = spectrum.DriftTime;
                var driftIndex = spectrum.OriginalIndex;
                var driftBin = (int)(driftTime * 1000);
                if (retention < rt - rtWidth * 0.5) break;

                var basepeakMz = 0.0;
                var basepeakIntensity = 0.0;
                var intensity = GetIonAbundanceOfMzInSpectrum(massSpectra, mz, mztol,
                    out basepeakMz, out basepeakIntensity);
                if (!driftBinToChromPeak.ContainsKey(driftBin)) {
                    driftBinToChromPeak[driftBin] = new ChromatogramPeak() { 
                        ID = driftIndex, ChromXs = new ChromXs(driftTime, ChromXType.Drift, ChromXUnit.Msec), Mass = basepeakMz, Intensity = intensity 
                    };
                    driftBinToSpecPeak[driftBin] = new SpectrumPeak() { Mass = basepeakMz, Intensity = basepeakIntensity };
                }
                else {
                    driftBinToChromPeak[driftBin].Intensity += intensity;
                    if (driftBinToSpecPeak[driftBin].Intensity < basepeakIntensity) {
                        driftBinToSpecPeak[driftBin].Mass = basepeakMz;
                        driftBinToSpecPeak[driftBin].Intensity = basepeakIntensity;
                        driftBinToChromPeak[driftBin].Mass = basepeakMz;
                    }
                }
            }

            //accumulating peaks from peak top to peak right
            for (int i = scanID + 2; i < spectrumList.Count; i++) {
                var spectrum = spectrumList[i];
                if (spectrum.MsLevel > 1) continue;
                var massSpectra = spectrum.Spectrum;
                var retention = spectrum.ScanStartTime;
                var driftTime = spectrum.DriftTime;
                var driftIndex = spectrum.OriginalIndex;
                var driftBin = (int)(driftTime * 1000);
                if (retention > rt + rtWidth * 0.5) break;

                var basepeakMz = 0.0;
                var basepeakIntensity = 0.0;
                var intensity = GetIonAbundanceOfMzInSpectrum(massSpectra, mz, mztol,
                   out basepeakMz, out basepeakIntensity);
                if (!driftBinToChromPeak.ContainsKey(driftBin)) {
                    driftBinToChromPeak[driftBin] = new ChromatogramPeak() {
                        ID = driftIndex, ChromXs = new ChromXs(driftTime, ChromXType.Drift, ChromXUnit.Msec), Mass = basepeakMz, Intensity = intensity
                    };
                    driftBinToSpecPeak[driftBin] = new SpectrumPeak() { Mass = basepeakMz, Intensity = basepeakIntensity };
                }
                else {
                    driftBinToChromPeak[driftBin].Intensity += intensity;
                    if (driftBinToSpecPeak[driftBin].Intensity < basepeakIntensity) {
                        driftBinToSpecPeak[driftBin].Mass = basepeakMz;
                        driftBinToSpecPeak[driftBin].Intensity = basepeakIntensity;
                        driftBinToChromPeak[driftBin].Mass = basepeakMz;
                    }
                }
            }

            var peaklist = new List<ChromatogramPeak>();
            foreach (var value in driftBinToChromPeak.Values) {
                peaklist.Add(value);
            }

            peaklist = peaklist.OrderBy(n => n.ChromXs.Value).ToList();
            return peaklist;
        }

        public static List<ChromatogramPeak> GetDriftChromatogramByRtMz(List<RawSpectrum> spectrumList,
           float rt, float rtWidth, float mz, float mztol, float minDt, float maxDt) {

            var startID = GetScanStartIndexByRt(rt, rtWidth * 0.5F, spectrumList);
            var driftBinToPeak = new Dictionary<int, ChromatogramPeak>();
            var driftBinToBasePeak = new Dictionary<int, SpectrumPeak>();

            for (int i = startID; i < spectrumList.Count; i++) {

                var spectrum = spectrumList[i];
                var massSpectra = spectrum.Spectrum;
                var retention = spectrum.ScanStartTime;
                var driftTime = spectrum.DriftTime;
                var driftScan = spectrum.DriftScanNumber;
                var driftBin = (int)(driftTime * 1000);

                //if (i > 1213450) {
                //    Debug.WriteLine("id {0} rt {1}", i, spectrum.ScanStartTime);
                //}

                if (retention < rt - rtWidth * 0.5) continue;
                if (driftTime < minDt || driftTime > maxDt) continue;
                if (retention > rt + rtWidth * 0.5) break;

                var basepeakMz = 0.0;
                var basepeakIntensity = 0.0;
                var intensity = GetIonAbundanceOfMzInSpectrum(massSpectra, mz, mztol,
                    out basepeakMz, out basepeakIntensity);
                if (!driftBinToPeak.ContainsKey(driftBin)) {
                    driftBinToPeak[driftBin] = new ChromatogramPeak() {
                        ID = driftScan, ChromXs = new ChromXs(driftTime, ChromXType.Drift, ChromXUnit.Msec), Mass = basepeakMz, Intensity = intensity
                    };
                    driftBinToBasePeak[driftBin] = new SpectrumPeak() { Mass = basepeakMz, Intensity = basepeakIntensity };
                }
                else {
                    driftBinToPeak[driftBin].Intensity += intensity;
                    if (driftBinToBasePeak[driftBin].Intensity < basepeakIntensity) {
                        driftBinToBasePeak[driftBin].Mass = basepeakMz;
                        driftBinToBasePeak[driftBin].Intensity = basepeakIntensity;
                        driftBinToPeak[driftBin].Mass = basepeakMz;
                    }
                }
            }
            var peaklist = new List<ChromatogramPeak>();
            foreach (var value in driftBinToPeak.Values) {
                peaklist.Add(value);
            }

            peaklist = peaklist.OrderBy(n => n.ChromXs.Value).ToList();
            return peaklist;
        }

        public static double GetIonAbundanceOfMzInSpectrum(RawPeakElement[] massSpectra,
            float mz, float mztol, out double basepeakMz, out double basepeakIntensity) {
            var startIndex = GetMs1StartIndex(mz, mztol, massSpectra);
            double sum = 0, maxIntensityMz = 0.0, maxMass = mz;

            //bin intensities for focused MZ +- ms1Tolerance
            for (int j = startIndex; j < massSpectra.Length; j++) {
                if (massSpectra[j].Mz < mz - mztol) continue;
                else if (mz - mztol <= massSpectra[j].Mz &&
                    massSpectra[j].Mz <= mz + mztol) {
                    sum += massSpectra[j].Intensity;
                    if (maxIntensityMz < massSpectra[j].Intensity) {
                        maxIntensityMz = massSpectra[j].Intensity;
                        maxMass = massSpectra[j].Mz;
                    }
                }
                else if (massSpectra[j].Mz > mz + mztol) break;
            }
            basepeakMz = maxMass;
            basepeakIntensity = maxIntensityMz;
            return sum;
        }


        // get spectrum
        public static List<SpectrumPeak> GetCentroidMasasSpectra(List<RawSpectrum> spectrumList, DataType dataType, 
            int msScanPoint, float amplitudeThresh, float mzBegin, float mzEnd) {
            if (msScanPoint < 0) return new List<SpectrumPeak>();

            var spectra = new List<SpectrumPeak>();
            var spectrum = spectrumList[msScanPoint];
            var massSpectra = spectrum.Spectrum;

            for (int i = 0; i < massSpectra.Length; i++) {
                if (massSpectra[i].Mz < mzBegin) continue;
                if (massSpectra[i].Mz > mzEnd) continue;
                spectra.Add(new SpectrumPeak() { Mass = massSpectra[i].Mz, Intensity = massSpectra[i].Intensity });
            }

            if (dataType == DataType.Centroid) return spectra.Where(n => n.Intensity > amplitudeThresh).ToList();

            if (spectra.Count == 0) return new List<SpectrumPeak>();

            var centroidedSpectra = SpectralCentroiding.Centroid(spectra, amplitudeThresh);

            if (centroidedSpectra != null && centroidedSpectra.Count != 0)
                return centroidedSpectra;
            else
                return spectra;
        }

        // get properties
        /// <summary>
        /// 
        /// </summary>
        /// <param name="spectrumList"></param>
        /// <param name="ionmode"></param>
        /// <returns>[0] min Mz [1] max Mz</returns>
        public static float[] GetMs1Range(List<RawSpectrum> spectrumList, IonMode ionmode) {
            float minMz = float.MaxValue, maxMz = float.MinValue;
            var scanPolarity = ionmode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;

            for (int i = 0; i < spectrumList.Count; i++) {
                if (spectrumList[i].MsLevel > 1) continue;
                if (spectrumList[i].ScanPolarity != scanPolarity) continue;
                //if (spectrumCollection[i].DriftScanNumber > 0) continue;

                if (spectrumList[i].LowestObservedMz < minMz) minMz = (float)spectrumList[i].LowestObservedMz;
                if (spectrumList[i].HighestObservedMz > maxMz) maxMz = (float)spectrumList[i].HighestObservedMz;
            }
            return new float[] { minMz, maxMz };
        }
    }
}
