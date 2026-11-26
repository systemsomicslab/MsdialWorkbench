using CompMs.Common.DataObj;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialLcImMsApi.Algorithm
{
    public class LcimmsAccumulateDataProvider : BaseDataProvider
    {
        public LcimmsAccumulateDataProvider(AnalysisFileBean file, bool isGuiProcess = false, int retry = 5) :
            base(LcimmsMs1Accumulator.Accumulate(LoadMeasurement(file, false, false, isGuiProcess, retry).SpectrumList)) {

        }

        //public LcimmsAccumulateDataProvider(RawMeasurement rawObj) : base(rawObj.AccumulatedSpectrumList) {

        //}

        public LcimmsAccumulateDataProvider(RawMeasurement rawObj) : base(LcimmsMs1Accumulator.Accumulate(rawObj.SpectrumList)) {

        }
    }

    /// <summary>
    /// Sums the drift-time spectra of each LC frame into one MS1 spectrum.
    /// It does what SpectrumParser.GetAccumulatedMs1Spectrum does, but finds the frames by retention time
    /// instead of ScanNumber, because readers do not agree on what ScanNumber means
    /// (the mzML reader numbers every spectrum, so no two spectra share a frame).
    /// </summary>
    internal static class LcimmsMs1Accumulator
    {
        // All drift-time spectra of one frame carry the frame's retention time.
        // The tolerance only absorbs floating-point noise; frames are far wider apart.
        private const double SameFrameRtTolerance = 1e-6;

        // spectra should be sorted so that the drift-time spectra of a frame are contiguous.
        public static List<RawSpectrum> Accumulate(List<RawSpectrum> spectra) {
            var ms1Spectra = spectra.Where(s => s.MsLevel == 1).ToList();
            var result = new List<RawSpectrum>();
            var pool = new MassBinPool();
            var massBins = new Dictionary<int, MassBin>();

            foreach (var (start, end) in GetFrameRanges(ms1Spectra)) {
                massBins.Clear();
                var first = ms1Spectra[start];
                var lowest = first.LowestObservedMz;
                var highest = first.HighestObservedMz;
                for (int i = start; i <= end; i++) {
                    foreach (var peak in ms1Spectra[i].Spectrum) {
                        SpectrumParser.AddToMassBinDictionary(massBins, pool, peak.Mz, peak.Intensity);
                    }
                    lowest = Math.Min(ms1Spectra[i].LowestObservedMz, lowest);
                    highest = Math.Max(ms1Spectra[i].HighestObservedMz, highest);
                }
                var accumulated = new RawSpectrum
                {
                    OriginalIndex = first.OriginalIndex,
                    Index = result.Count,
                    ScanNumber = first.ScanNumber,
                    ScanStartTime = first.ScanStartTime,
                    ScanStartTimeUnit = first.ScanStartTimeUnit,
                    MsLevel = first.MsLevel,
                    ScanPolarity = first.ScanPolarity,
                    Precursor = first.Precursor,
                    DriftScanNumber = first.DriftScanNumber,
                    DriftTime = first.DriftTime,
                    DriftTimeUnit = first.DriftTimeUnit,
                    MaldiFrameInfo = first.MaldiFrameInfo,
                    LowestObservedMz = lowest,
                    HighestObservedMz = highest,
                };
                SpectrumParser.setSpectrumProperties(accumulated, massBins);
                result.Add(accumulated);
                pool.BatchReturn(massBins.Values);
            }
            return result;
        }

        public static List<(int Start, int End)> GetFrameRanges(IReadOnlyList<RawSpectrum> spectra) {
            var ranges = new List<(int, int)>();
            if (spectra.Count == 0) {
                return ranges;
            }
            var start = 0;
            for (int i = 1; i < spectra.Count; i++) {
                if (Math.Abs(spectra[i].ScanStartTime - spectra[start].ScanStartTime) > SameFrameRtTolerance) {
                    ranges.Add((start, i - 1));
                    start = i;
                }
            }
            ranges.Add((start, spectra.Count - 1));
            return ranges;
        }
    }

    public class LcimmsAccumulateDataProviderFactory : IDataProviderFactory<AnalysisFileBean>, IDataProviderFactory<RawMeasurement>
    {
        public LcimmsAccumulateDataProviderFactory(int retry = 5, bool isGuiProcess = false) {
            this.retry = retry;
            this.isGuiProcess = isGuiProcess;
        }

        private readonly int retry;
        private readonly bool isGuiProcess;

        public LcimmsAccumulateDataProvider Create(AnalysisFileBean file) {
            return new LcimmsAccumulateDataProvider(file, isGuiProcess, retry);
        }

        public LcimmsAccumulateDataProvider Create(RawMeasurement rawMeasurement) {
            return new LcimmsAccumulateDataProvider(rawMeasurement);
        }

        IDataProvider IDataProviderFactory<AnalysisFileBean>.Create(AnalysisFileBean source) => Create(source);
        IDataProvider IDataProviderFactory<RawMeasurement>.Create(RawMeasurement source) => Create(source);
    }

}
