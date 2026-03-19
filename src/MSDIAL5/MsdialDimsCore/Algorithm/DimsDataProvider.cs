using CompMs.Common.DataObj;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialDimsCore.Algorithm
{
    public class DimsBpiDataProvider : BaseDataProvider
    {
        public DimsBpiDataProvider(IEnumerable<RawSpectrum> spectrums, double timeBegin, double timeEnd)
            : base(BasePeakIntensitySelect(
                spectrums
                    .Where(spec => timeBegin <= spec.ScanStartTime && spec.ScanStartTime <= timeEnd)
                    .Select(spec => spec.ShallowCopy())
                    .ToArray()
                )) {
        }

        public DimsBpiDataProvider(RawMeasurement rawObj, double timeBegin, double timeEnd)
            : this(rawObj.SpectrumList, timeBegin, timeEnd) {

        }

        public DimsBpiDataProvider(AnalysisFileBean file, double timeBegin, double timeEnd, bool isGuiProcess = false, int retry = 5)
            : this(LoadMeasurement(file, true, false, isGuiProcess, retry).SpectrumList, timeBegin, timeEnd) {

        }

        private static List<RawSpectrum> BasePeakIntensitySelect(RawSpectrum[] spectrums) {
            var ms1Spectrum = spectrums.Where(spectrum => spectrum.MsLevel == 1).Argmax(spectrum => spectrum.BasePeakIntensity);
            var msSpectrums = spectrums.Where(spectrum => spectrum.MsLevel != 1);
            var result = new[] { ms1Spectrum }.Concat(msSpectrums).ToList();
            for (int i = 0; i < result.Count; i++) {
                result[i].Index = i;
            }
            return result;
        }
    }

    public class DimsTicDataProvider : BaseDataProvider
    {
        public DimsTicDataProvider(IEnumerable<RawSpectrum> spectrums, double timeBegin, double timeEnd)
            : base(TotalIonCurrentSelect(
                spectrums
                    .Where(spec => timeBegin <= spec.ScanStartTime && spec.ScanStartTime <= timeEnd)
                    .Select(spec => spec.ShallowCopy())
                    .ToArray()
                )) {
        }

        public DimsTicDataProvider(RawMeasurement rawObj, double timeBegin, double timeEnd)
            : this(rawObj.SpectrumList, timeBegin, timeEnd) {

        }

        public DimsTicDataProvider(AnalysisFileBean file, double timeBegin, double timeEnd, bool isGuiProcess = false, int retry = 5)
            : this(LoadMeasurement(file, true, false, isGuiProcess, retry).SpectrumList, timeBegin, timeEnd) {

        }

        private static List<RawSpectrum> TotalIonCurrentSelect(RawSpectrum[] spectrums) {
            if (spectrums.IsEmptyOrNull()) return null;
            var ms1Spectrum = spectrums.Length > 1 
                ? spectrums.Where(spectrum => spectrum.MsLevel == 1).Argmax(spectrum => spectrum.TotalIonCurrent)
                : spectrums[0];
            var msSpectrums = spectrums.Where(spectrum => spectrum.MsLevel != 1);
            var result = new[] { ms1Spectrum }.Concat(msSpectrums).ToList();
            for (int i = 0; i < result.Count; i++) {
                result[i].Index = i;
            }
            return result;
        }
    }

    public class DimsAverageDataProvider : BaseDataProvider
    {
        public DimsAverageDataProvider(IEnumerable<RawSpectrum> spectrums, double massTolerance, double timeBegin, double timeEnd)
            : base(AccumulateRawSpectrums(
                spectrums
                    .Where(spec => timeBegin <= spec.ScanStartTime && spec.ScanStartTime <= timeEnd)
                    .Select(spec => spec.ShallowCopy())
                    .ToArray(),
                massTolerance)) {

        }

        public DimsAverageDataProvider(RawMeasurement rawObj, double massTolerance, double timeBegin, double timeEnd)
            : this(rawObj.SpectrumList, massTolerance, timeBegin, timeEnd) {

        }

        public DimsAverageDataProvider(AnalysisFileBean file, double massTolerance, double timeBegin, double timeEnd, bool isGuiProcess = false, int retry = 5)
            : this(LoadMeasurement(file, true, false, isGuiProcess, retry).SpectrumList, massTolerance, timeBegin, timeEnd) {

        }


        private static List<RawSpectrum> AccumulateRawSpectrums(RawSpectrum[] spectrums, double massTolerance) {
            var targetmz4ppmcalc = 500.0;
            var binning = new Binning(targetmz4ppmcalc, massTolerance);
            var ms1Spectrums = spectrums.Where(spectrum => spectrum.MsLevel == 1).ToList();
            var groups = ms1Spectrums.SelectMany(spectrum => spectrum.Spectrum)
                .GroupBy(peak => binning.BinningMz(peak.Mz));
            var massBins = new Dictionary<int, double[]>();
            foreach (var group in groups) {
                var peaks = group.ToList();
                var accIntensity = peaks.Sum(peak => peak.Intensity) / ms1Spectrums.Count;
                var basepeak = peaks.Argmax(peak => peak.Intensity);
                massBins[group.Key] = new double[] { basepeak.Mz, accIntensity, basepeak.Intensity };
            }
            var result = ms1Spectrums.First().ShallowCopy();
            SpectrumParser.setSpectrumProperties(result, massBins, isSortMz: true);
            var results = new[] { result }.Concat(spectrums.Where(spectrum => spectrum.MsLevel != 1)).ToList();
            for (int i = 0; i < results.Count; i++) {
                results[i].Index = i;
            }
            return results;
        }

        class Binning
        {
            public double Pivot { get; }
            public double Tolerance { get; }
            public double Ppm { get; }
            public double LogTolerance { get; }

            public Binning(double pivot, double tolerance) {
                Pivot = pivot;
                Tolerance = tolerance;
                Ppm = tolerance / Pivot * 1_000_000d;
                LogTolerance = Math.Log(1 + Ppm / 1_000_000d);
            }

            /// <summary>
            /// Calculates the bin index for a given m/z value.
            /// </summary>
            /// <param name="mz">The mass-to-charge (m/z) value.</param>
            /// <returns>The bin index as an integer.</returns>
            public int BinningMz(double mz) {
                if (mz <= Pivot) {
                    return (int)(mz / Tolerance);
                }
                else {
                    int pivotbin = (int)(Pivot / Tolerance) + 1;
                    return pivotbin + (int)(Math.Log(mz / Pivot) / LogTolerance);
                }
            }
        }
    }

    public class DimsBpiDataProviderFactory
        : IDataProviderFactory<AnalysisFileBean>, IDataProviderFactory<RawMeasurement>
    {
        private readonly double timeBegin;
        private readonly double timeEnd;
        private readonly int retry;
        private readonly bool isGuiProcess;

        public DimsBpiDataProviderFactory(
            double timeBegin = double.MinValue,
            double timeEnd = double.MaxValue,
            int retry = 5,
            bool isGuiProcess = false) {
            this.timeBegin = timeBegin;
            this.timeEnd = timeEnd;
            this.retry = retry;
            this.isGuiProcess = isGuiProcess;
        }

        public IDataProvider Create(AnalysisFileBean source) {
            return new DimsBpiDataProvider(source, timeBegin, timeEnd, isGuiProcess, retry);
        }

        public IDataProvider Create(RawMeasurement source) {
            return new DimsBpiDataProvider(source, timeBegin, timeEnd);
        }
    }

    public class DimsTicDataProviderFactory
        : IDataProviderFactory<AnalysisFileBean>, IDataProviderFactory<RawMeasurement>
    {
        private readonly double timeBegin;
        private readonly double timeEnd;
        private readonly int retry;
        private readonly bool isGuiProcess;

        public DimsTicDataProviderFactory(
            double timeBegin = double.MinValue,
            double timeEnd = double.MaxValue,
            int retry = 5,
            bool isGuiProcess = false) {
            this.timeBegin = timeBegin;
            this.timeEnd = timeEnd;
            this.retry = retry;
            this.isGuiProcess = isGuiProcess;
        }

        public IDataProvider Create(AnalysisFileBean source) {
            return new DimsTicDataProvider(source, timeBegin, timeEnd, isGuiProcess, retry);
        }

        public IDataProvider Create(RawMeasurement source) {
            return new DimsTicDataProvider(source, timeBegin, timeEnd);
        }
    }

    public class DimsAverageDataProviderFactory
        : IDataProviderFactory<AnalysisFileBean>, IDataProviderFactory<RawMeasurement>
    {
        private readonly double massTolerance;
        private readonly double timeBegin;
        private readonly double timeEnd;
        private readonly int retry;
        private readonly bool isGuiProcess;

        public DimsAverageDataProviderFactory(
            double massTolerance,
            double timeBegin = double.MinValue,
            double timeEnd = double.MaxValue,
            int retry = 5,
            bool isGuiProcess = false) {
            this.massTolerance = massTolerance;
            this.timeBegin = timeBegin;
            this.timeEnd = timeEnd;
            this.retry = retry;
            this.isGuiProcess = isGuiProcess;
        }

        public IDataProvider Create(AnalysisFileBean source) {
            return new DimsAverageDataProvider(source, massTolerance, timeBegin, timeEnd, isGuiProcess, retry);
        }

        public IDataProvider Create(RawMeasurement source) {
            return new DimsAverageDataProvider(source, massTolerance, timeBegin, timeEnd);
        }
    }
}
