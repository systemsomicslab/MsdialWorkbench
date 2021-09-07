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
        protected readonly List<RawSpectrum> rawSpectrums;
        private readonly Dictionary<int, ReadOnlyCollection<RawSpectrum>> msSpectrums;

        public ImmsBaseDataProvider(IEnumerable<RawSpectrum> spectrums)
            : base(spectrums) {

            rawSpectrums = this.spectrums.Select(spec => spec.ShallowCopy()).ToList();
            msSpectrums = rawSpectrums
                .GroupBy(spectrum => spectrum.MsLevel)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderBy(spectrum => spectrum.DriftTime).ToList().AsReadOnly());
        }

        public ImmsBaseDataProvider(RawMeasurement rawObj) : this(rawObj.SpectrumList) {

        }

        public ImmsBaseDataProvider(AnalysisFileBean file) : this(LoadMeasurement(file, false, 5)) {

        }

        public ImmsBaseDataProvider(AnalysisFileBean file, bool isGuiProcess, int retry)
            : this(LoadMeasurement(file, isGuiProcess, retry)) {

        }

        public override ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) {
            if (msSpectrums.ContainsKey(level))
                return msSpectrums[level];
            else
                return new List<RawSpectrum>(0).AsReadOnly();
        }

        public override ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() {
            return rawSpectrums.AsReadOnly();
        }
    }

    public class ImmsRepresentativeDataProvider : ImmsBaseDataProvider
    {
        private readonly List<RawSpectrum> representativeSpectrum;

        public ImmsRepresentativeDataProvider(IEnumerable<RawSpectrum> spectrums) : base(spectrums) {
            this.representativeSpectrum = SelectRepresentative(rawSpectrums).OrderBy(spectrum => spectrum.DriftTime).ToList();

        }

        public ImmsRepresentativeDataProvider(RawMeasurement rawObj) : base(rawObj) {
            this.representativeSpectrum = SelectRepresentative(rawSpectrums).OrderBy(spectrum => spectrum.DriftTime).ToList();
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

        public override ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() {
            return representativeSpectrum.Concat(rawSpectrums.Where(spec => spec.MsLevel != 1)).ToList().AsReadOnly();
        }
    }

    public class ImmsAverageDataProvider : ImmsBaseDataProvider
    {
        private readonly List<RawSpectrum> accumulatedSpectrum;

        public ImmsAverageDataProvider(IEnumerable<RawSpectrum> spectrums, double massTolerance, double driftTolerance)
            : base(spectrums) {

            this.accumulatedSpectrum = AccumulateSpectrum(rawSpectrums, massTolerance, driftTolerance).ToList();
            this.cache = new Dictionary<int, ReadOnlyCollection<RawSpectrum>>();
        }

        public ImmsAverageDataProvider(RawMeasurement rawObj, double massTolerance, double driftTolerance)
            : this(rawObj.SpectrumList, massTolerance, driftTolerance) {

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

            var groups = rawSpectrums.GroupBy(spectrum => Math.Ceiling(spectrum.DriftTime / driftTolerance));
            var result = groups
                .OrderBy(kvp => kvp.Key)
                .SelectMany(group => AccumulateRawSpectrums(group.ToList(), massTolerance, numOfMeasurement))
                .ToList();
            for (var i = 0; i < result.Count; i++) {
                result[i].Index = i;
            }

            return result;
        }

        private static IEnumerable<RawSpectrum> AccumulateRawSpectrums(IReadOnlyCollection<RawSpectrum> spectrums, double massTolerance, int numOfMeasurement) {
            var ms1Spectrums = spectrums.Where(spectrum => spectrum.MsLevel == 1).ToList();
            var groups = ms1Spectrums.SelectMany(spectrum => spectrum.Spectrum)
                .GroupBy(peak => (int)(peak.Mz / massTolerance));
            var massBins = new Dictionary<int, double[]>();
            foreach (var group in groups) {
                var peaks = group.ToList();
                var accIntensity = peaks.Sum(peak => peak.Intensity) / numOfMeasurement;
                var basepeak = peaks.Argmax(peak => peak.Intensity);
                massBins[group.Key] = new double[] { basepeak.Mz, accIntensity, basepeak.Intensity };
            }
            var result = CloneRawSpectrum(ms1Spectrums.First());
            SpectrumParser.setSpectrumProperties(result, massBins);
            return new[] { result }.Concat(
                spectrums.Where(spectrum => spectrum.MsLevel != 1)
                .OrderBy(spectrum => spectrum.Index));
        }

        private static RawSpectrum CloneRawSpectrum(RawSpectrum spec) {
            return new RawSpectrum() {
                OriginalIndex = spec.OriginalIndex,
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
            return LoadMsNSpectrums(1);
        }

        private readonly Dictionary<int, ReadOnlyCollection<RawSpectrum>> cache;
        public override ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) {
            if (cache.ContainsKey(level))
                return cache[level];
            else
                return cache[level] = accumulatedSpectrum.Where(spectrum => spectrum.MsLevel == level).ToList().AsReadOnly();
        }

        public override ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() {
            return accumulatedSpectrum.AsReadOnly();
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
