using CompMs.Common.DataObj;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialImmsCore.Algorithm
{
    public class ImmsRepresentativeDataProvider : BaseDataProvider
    {
        public ImmsRepresentativeDataProvider(IEnumerable<RawSpectrum> spectrums, double timeBegin, double timeEnd)
            : base(SelectRepresentative(FilterByScanTime(spectrums, timeBegin, timeEnd))) {
            
        }

        public ImmsRepresentativeDataProvider(IEnumerable<RawSpectrum> spectrums)
            : base(SelectRepresentative(spectrums)) {

        }

        public ImmsRepresentativeDataProvider(RawMeasurement rawObj, double timeBegin, double timeEnd)
            : this(rawObj.SpectrumList, timeBegin, timeEnd) {

        }

        public ImmsRepresentativeDataProvider(RawMeasurement rawObj)
            : this(rawObj.SpectrumList) {

        }

        public ImmsRepresentativeDataProvider(AnalysisFileBean file, double timeBegin, double timeEnd, bool isGuiProcess = false, int retry = 5)
            :this(LoadMeasurement(file, false, false, isGuiProcess, retry).SpectrumList, timeBegin, timeEnd) {

        }

        public ImmsRepresentativeDataProvider(AnalysisFileBean file, bool isGuiProcess = false, int retry = 5)
            :this(LoadMeasurement(file, false, false, isGuiProcess, retry).SpectrumList) {

        }

        private static List<RawSpectrum> SelectRepresentative(IEnumerable<RawSpectrum> rawSpectrums) {
            var ms1Spectrums = rawSpectrums
                .Where(spectrum => spectrum.MsLevel == 1)
                .GroupBy(spectrum => spectrum.ScanNumber)
                .Argmax(spectrums => spectrums.Sum(spectrum => spectrum.TotalIonCurrent));
            var result = ms1Spectrums.Concat(rawSpectrums.Where(spec => spec.MsLevel != 1))
                .Select(spec => spec.ShallowCopy())
                .OrderBy(spectrum => spectrum.DriftTime).ToList();
            for (int i = 0; i < result.Count; i++) {
                result[i].Index = i;
            }
            return result;
        }
    }

    public class ImmsAverageDataProvider : BaseDataProvider
    {
        public ImmsAverageDataProvider(IEnumerable<RawSpectrum> spectrums, double massTolerance, double driftTolerance, double timeBegin, double timeEnd)
            : base(AccumulateSpectrum(FilterByScanTime(spectrums, timeBegin, timeEnd).ToList(), massTolerance, driftTolerance)) {

        }
        
        public ImmsAverageDataProvider(IEnumerable<RawSpectrum> spectrums, double massTolerance, double driftTolerance)
            : base(AccumulateSpectrum(spectrums.ToList(), massTolerance, driftTolerance)) {

        }

        public ImmsAverageDataProvider(RawMeasurement rawObj, double massTolerance, double driftTolerance, double timeBegin, double timeEnd)
            : this(rawObj.SpectrumList, massTolerance, driftTolerance, timeBegin, timeEnd) {

        }

        public ImmsAverageDataProvider(RawMeasurement rawObj, double massTolerance, double driftTolerance)
            : this(rawObj.SpectrumList, massTolerance, driftTolerance) {

        }

        public ImmsAverageDataProvider(RawMeasurement rawObj)
            :this(rawObj, 0.001, 0.002) { }

        public ImmsAverageDataProvider(AnalysisFileBean file, double massTolerance, double driftTolerance, double timeBegin, double timeEnd, bool isGuiProcess = false, int retry = 5)
            :this(LoadMeasurement(file, false, false, isGuiProcess, retry), massTolerance, driftTolerance, timeBegin, timeEnd) {

        }

        public ImmsAverageDataProvider(AnalysisFileBean file, bool isGuiProcess = false, int retry = 5)
            :this(LoadMeasurement(file, false, false, isGuiProcess, retry)) {

        }

        public ImmsAverageDataProvider(AnalysisFileBean file, double massTolerance, double driftTolerance, bool isGuiProcess = false, int retry = 5)
            :this(LoadMeasurement(file, false, false, isGuiProcess, retry), massTolerance, driftTolerance) {

        }

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
                //var accIntensity = peaks.Sum(peak => peak.Intensity);
                var basepeak = peaks.Argmax(peak => peak.Intensity);
                massBins[group.Key] = new double[] { basepeak.Mz, accIntensity, basepeak.Intensity };
            }
            var result = ms1Spectrums.First().ShallowCopy();
            SpectrumParser.setSpectrumProperties(result, massBins);
            return new[] { result }.Concat(
                spectrums.Where(spectrum => spectrum.MsLevel != 1)
                .Select(spec => spec.ShallowCopy())
                .OrderBy(spectrum => spectrum.Index));
        }
    }

    public sealed class ImmsRepresentativeDataProviderFactory : IDataProviderFactory<AnalysisFileBean>, IDataProviderFactory<RawMeasurement>
    {
        private readonly bool _isGuiProcess;
        private readonly int _retry;
        private readonly double _timeBegin;
        private readonly double _timeEnd;

        public ImmsRepresentativeDataProviderFactory(double timeBegin, double timeEnd, int retry = 5, bool isGuiProcess = false) {

            _timeBegin = timeBegin;
            _timeEnd = timeEnd;
            _retry = retry;
            _isGuiProcess = isGuiProcess;
        }

        public IDataProvider Create(AnalysisFileBean file) {
            var measurement = file.LoadRawMeasurement(isImagingMsData: false, isGuiProcess: _isGuiProcess, retry: _retry, sleepMilliSeconds: 1000);
            return new ImmsRepresentativeDataProvider(measurement, _timeBegin, _timeEnd);
        }

        public IDataProvider Create(RawMeasurement rawMeasurement) {
            return new ImmsRepresentativeDataProvider(rawMeasurement, _timeBegin, _timeEnd);
        }
    }

    public sealed class ImmsAverageDataProviderFactory : IDataProviderFactory<AnalysisFileBean>, IDataProviderFactory<RawMeasurement>
    {
        private readonly bool _isGuiProcess;
        private readonly int _retry;
        private readonly double _massTolerance, _driftTolerance;
        private readonly double _timeBegin;
        private readonly double _timeEnd;

        public ImmsAverageDataProviderFactory(double massTolerance, double driftTolerance, double timeBegin, double timeEnd, int retry = 5, bool isGuiProcess = false) {
            _retry = retry;
            _isGuiProcess = isGuiProcess;
            _massTolerance = massTolerance;
            _driftTolerance = driftTolerance;
            _timeBegin = timeBegin;
            _timeEnd = timeEnd;
        }

        public ImmsAverageDataProviderFactory(double massTolerance, double driftTolerance, int retry = 5, bool isGuiProcess = false)
            :this(massTolerance, driftTolerance, double.MinValue, double.MaxValue, retry, isGuiProcess) {

        }

        public IDataProvider Create(AnalysisFileBean file) {
            var measurement = file.LoadRawMeasurement(isImagingMsData: false, isGuiProcess: _isGuiProcess, retry: _retry, sleepMilliSeconds: 1000);
            return new ImmsAverageDataProvider(measurement, _massTolerance, _driftTolerance, _timeBegin, _timeEnd);
        }

        public IDataProvider Create(RawMeasurement rawMeasurement) {
            return new ImmsAverageDataProvider(rawMeasurement, _massTolerance, _driftTolerance, _timeBegin, _timeEnd);
        }
    }
}
