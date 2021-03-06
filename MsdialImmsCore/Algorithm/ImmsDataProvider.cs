using CompMs.Common.DataObj;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;

namespace CompMs.MsdialImmsCore.Algorithm
{
    public abstract class ImmsBaseDataProvider : IDataProvider
    {
        private readonly RawMeasurement rawObj;

        public ImmsBaseDataProvider(RawMeasurement rawObj) {
            this.rawObj = rawObj;
        }

        public ImmsBaseDataProvider(AnalysisFileBean file)
            :this(LoadMeasurement(file, false, 5)) { }

        public ImmsBaseDataProvider(AnalysisFileBean file, bool isGuiProcess, int retry)
            :this(LoadMeasurement(file, isGuiProcess, retry)) { }

        protected static RawMeasurement LoadMeasurement(AnalysisFileBean file, bool isGuiProcess, int retry) {
            using (var access = new RawDataAccess(file.AnalysisFilePath, 0, isGuiProcess)) {
                for (var i = 0; i < retry; i++) {
                    var rawObj = DataAccess.GetRawDataMeasurement(access);
                    if (rawObj != null) {
                        return rawObj;
                    }
                    Thread.Sleep(5000);
                }
            }
            throw new FileLoadException($"Loading {file.AnalysisFilePath} failed.");
        }

        public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() {
            return new ReadOnlyCollection<RawSpectrum>(rawObj.SpectrumList);
        }

        public abstract ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums();

        public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) {
            return new ReadOnlyCollection<RawSpectrum>(rawObj.SpectrumList
                .Where(spectrum => spectrum.MsLevel == level)
                .OrderBy(spectrum => spectrum.DriftTime)
                .ToList());
        }
    }

    public class ImmsRepresentativeDataProvider : ImmsBaseDataProvider
    {
        private readonly List<RawSpectrum> representativeSpectrum;

        public ImmsRepresentativeDataProvider(RawMeasurement rawObj) : base(rawObj) {
            this.representativeSpectrum = SelectRepresentative(rawObj.SpectrumList).OrderBy(spectrum => spectrum.DriftTime).ToList();
        }

        public ImmsRepresentativeDataProvider(AnalysisFileBean file, bool isGuiProcess = false, int retry = 5)
            :this(LoadMeasurement(file, isGuiProcess, retry)) { }

        private static List<RawSpectrum> SelectRepresentative(List<RawSpectrum> rawSpectrums) {
            var scannumber = rawSpectrums
                .Where(spectrum => spectrum.MsLevel == 1)
                .GroupBy(spectrum => spectrum.ScanNumber)
                .Argmax(spectrums => spectrums.Sum(spectrum => spectrum.TotalIonCurrent)).Key;
            return rawSpectrums.Where(spectrum => spectrum.ScanNumber == scannumber).ToList();
        }

        public override ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() {
            return new ReadOnlyCollection<RawSpectrum>(representativeSpectrum);
        }
    }

    public class ImmsAverageDataProvider : ImmsBaseDataProvider
    {
        private readonly List<RawSpectrum> accumulatedSpectrum;

        public ImmsAverageDataProvider(RawMeasurement rawObj, double massTolerance, double driftTolerance) : base(rawObj) {
            this.accumulatedSpectrum = AccumulateSpectrum(rawObj.SpectrumList, massTolerance, driftTolerance).OrderBy(spectrum => spectrum.DriftTime).ToList();
        }

        public ImmsAverageDataProvider(RawMeasurement rawObj)
            :this(rawObj, 0.001, 0.002) { }

        public ImmsAverageDataProvider(AnalysisFileBean file, bool isGuiProcess = false, int retry = 5)
            :this(LoadMeasurement(file, isGuiProcess, retry)) { }

        public ImmsAverageDataProvider(AnalysisFileBean file, double massTolerance, double driftTolerance, bool isGuiProcess = false, int retry = 5)
            :this(LoadMeasurement(file, isGuiProcess, retry), massTolerance, driftTolerance) { }

        private static List<RawSpectrum> AccumulateSpectrum(List<RawSpectrum> rawSpectrums, double massTolerance, double driftTolerance) {
            var ms1Spectrum = rawSpectrums.Where(spectrum => spectrum.MsLevel == 1).ToList();
            var numOfMeasurement = ms1Spectrum.Select(spectrum => spectrum.ScanStartTime).Distinct().Count();

            var groups = ms1Spectrum.GroupBy(spectrum => Math.Ceiling(spectrum.DriftTime / driftTolerance));
            var result = groups.Select(group => AccumulateRawSpectrums(group.ToList(), massTolerance, numOfMeasurement)).ToList();
            for (var i = 0; i < result.Count; i++) {
                result[i].Index = i;
            }

            return result;
        }

        private static RawSpectrum AccumulateRawSpectrums(IReadOnlyCollection<RawSpectrum> spectrums, double massTolerance, int numOfMeasurement) {
            var groups = spectrums.SelectMany(spectrum => spectrum.Spectrum)
                .GroupBy(peak => (int)(peak.Mz / massTolerance));
            var massBins = new Dictionary<int, double[]>();
            foreach (var group in groups) {
                var peaks = group.ToList();
                var accIntensity = peaks.Sum(peak => peak.Intensity) / numOfMeasurement;
                var basepeak = peaks.Argmax(peak => peak.Intensity);
                massBins[group.Key] = new double[] { basepeak.Mz, accIntensity, basepeak.Intensity };
            }
            var result = CloneRawSpectrum(spectrums.First());
            SpectrumParser.setSpectrumProperties(result, massBins);
            return result;
        }

        private static RawSpectrum CloneRawSpectrum(RawSpectrum spec) {
            return new RawSpectrum() {
                ScanNumber = spec.ScanNumber,
                ScanStartTime = spec.ScanStartTime,
                ScanStartTimeUnit = spec.ScanStartTimeUnit,
                MsLevel = 1,
                ScanPolarity = spec.ScanPolarity,
                Precursor = null,
                DriftScanNumber = spec.DriftScanNumber,
                DriftTime = spec.DriftTime,
                DriftTimeUnit = spec.DriftTimeUnit,
            };
        }

        public override ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() {
            return new ReadOnlyCollection<RawSpectrum>(accumulatedSpectrum);
        }
    }
}
