using CompMs.Common.DataObj;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.MsdialImmsCore.Algorithm
{
    public abstract class ImmsBaseDataProvider : BaseDataProvider
    {
        private Dictionary<int, ReadOnlyCollection<RawSpectrum>> msSpectrums;

        public ImmsBaseDataProvider(RawMeasurement rawObj) : base(rawObj) {
            msSpectrums = rawObj.SpectrumList
                .GroupBy(spectrum => spectrum.MsLevel)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderBy(spectrum => spectrum.DriftTime).ToList().AsReadOnly());
        }

        public ImmsBaseDataProvider(AnalysisFileBean file) : this(LoadMeasurement(file, false, 5)) {

        }

        public ImmsBaseDataProvider(AnalysisFileBean file, bool isGuiProcess, int retry) : this(LoadMeasurement(file, isGuiProcess, retry)) {

        }

        public override ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) {
            if (msSpectrums.ContainsKey(level))
                return msSpectrums[level];
            else
                return new List<RawSpectrum>(0).AsReadOnly();
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
            this.accumulatedSpectrum = AccumulateSpectrum(rawObj.SpectrumList, massTolerance, driftTolerance).ToList();
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
            var result = groups
                .Select(group => AccumulateRawSpectrums(group.ToList(), massTolerance, numOfMeasurement))
                .OrderBy(spectrum => spectrum.DriftTime)
                .ToList();
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

    public class ImmsRepresentativeDataProviderFactory
        : IDataProviderFactory<AnalysisFileBean>, IDataProviderFactory<RawMeasurement>
    {
        public ImmsRepresentativeDataProviderFactory(
            int retry = 5, bool isGuiProcess = false) {

            this.retry = retry;
            this.isGuiProcess = isGuiProcess;
        }

        private readonly bool isGuiProcess;
        private readonly int retry;

        public IDataProvider Create(AnalysisFileBean file) {
            return new ImmsRepresentativeDataProvider(file, isGuiProcess, retry);
        }

        public IDataProvider Create(RawMeasurement rawMeasurement) {
            return new ImmsRepresentativeDataProvider(rawMeasurement);
        }
    }

    public class ImmsAverageDataProviderFactory
        : IDataProviderFactory<AnalysisFileBean>, IDataProviderFactory<RawMeasurement>
    {
        public ImmsAverageDataProviderFactory(
            double massTolerance, double driftTolerance,
            int retry = 5, bool isGuiProcess = false) {

            this.retry = retry;
            this.isGuiProcess = isGuiProcess;
            this.massTolerance = massTolerance;
            this.driftTolerance = driftTolerance;
        }

        private readonly bool isGuiProcess;
        private readonly int retry;
        private readonly double massTolerance, driftTolerance;

        public IDataProvider Create(AnalysisFileBean file) {
            return new ImmsAverageDataProvider(file, massTolerance, driftTolerance, isGuiProcess, retry);
        }

        public IDataProvider Create(RawMeasurement rawMeasurement) {
            return new ImmsAverageDataProvider(rawMeasurement, massTolerance, driftTolerance);
        }
    }
}
