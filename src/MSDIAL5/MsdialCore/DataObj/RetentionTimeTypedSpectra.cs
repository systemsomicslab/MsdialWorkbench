using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Utility;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.DataObj;

internal sealed class RetentionTimeTypedSpectra : IChromatogramTypedSpectra
{
    private readonly ChromXUnit _unit;
    private readonly AcquisitionType _acquisitionType;
    private readonly ScanPolarity _polarity;
    private readonly List<RawSpectrum> _spectra;
    private readonly RetentionTime[] _idToRetentionTime;
    private readonly ConcurrentDictionary<int, Spectrum> _lazySpectra;
    private readonly int[] _ms1Counts;

    public RetentionTimeTypedSpectra(IReadOnlyList<RawSpectrum> spectra, ChromXUnit unit, IonMode ionMode, AcquisitionType acquisitionType) {
        _unit = unit;
        _acquisitionType = acquisitionType;
        _polarity = ionMode switch
        {
            IonMode.Positive => ScanPolarity.Positive,
            IonMode.Negative => ScanPolarity.Negative,
            _ => throw new ArgumentException($"IonMode {ionMode} is not supported."),
        };
        _spectra = spectra?.OrderBy(spectrum => spectrum.ScanStartTime).ToList() ?? throw new ArgumentNullException(nameof(spectra));
        _idToRetentionTime = new RetentionTime[_spectra.Count];
        _ms1Counts = new int[_spectra.Count + 1];
        _lazySpectra = new ConcurrentDictionary<int, Spectrum>();
        for (int i = 0; i < _spectra.Count; i++) {
            _idToRetentionTime[i] = new RetentionTime(_spectra[i].ScanStartTime, unit);
            if (_spectra[i].MsLevel == 1 && _spectra[i].ScanPolarity == _polarity) {
                _ms1Counts[i + 1]++;
            }
        }
        for (int i = 1; i < _ms1Counts.Length; i++) {
            _ms1Counts[i] += _ms1Counts[i - 1];
        }
    }

    public Chromatogram GetMs1BasePeakChromatogram(double start, double end) {
        var startIndex = _spectra.LowerBound(start, (spectrum, target) => spectrum.ScanStartTime.CompareTo(target));
        var endIndex = _spectra.UpperBound(end, startIndex, _spectra.Count, (spectrum, target) => spectrum.ScanStartTime.CompareTo(target));
        var results = new List<ChromatogramPeak>();
        for (int i = startIndex; i < endIndex; i++) {
            if (_spectra[i].MsLevel != 1 ||
                _spectra[i].ScanPolarity != _polarity) {
                continue;
            }
            var (basePeakMz, basePeakIntensity, _) = new Spectrum(_spectra[i].Spectrum).RetrieveTotalIntensity();
            results.Add(ChromatogramPeak.Create(_spectra[i].Index, basePeakMz, basePeakIntensity, _idToRetentionTime[i]));
        }
        return new Chromatogram(results, ChromXType.RT, _unit);
    }

    public Chromatogram GetMs1ExtractedChromatogram(double mz, double tolerance, double start, double end) {
        var startIndex = _spectra.LowerBound(start, (spectrum, target) => spectrum.ScanStartTime.CompareTo(target));
        var endIndex = _spectra.UpperBound(end, startIndex, _spectra.Count, (spectrum, target) => spectrum.ScanStartTime.CompareTo(target));
        var results = new List<ChromatogramPeak>();
        for (int i = startIndex; i < endIndex; i++) {
            if (_spectra[i].MsLevel != 1 ||
                _spectra[i].ScanPolarity != _polarity) {
                continue;
            }
            var (basePeakMz, _, summedIntensity) = new Spectrum(_spectra[i].Spectrum).RetrieveBin(mz, tolerance);
            results.Add(ChromatogramPeak.Create(_spectra[i].Index, basePeakMz, summedIntensity, _idToRetentionTime[i]));
        }
        return new Chromatogram(results, ChromXType.RT, _unit);
    }

    public ExtractedIonChromatogram GetMs1ExtractedChromatogram_temp2(double mz, double tolerance, double start, double end) {
        var startIndex = _spectra.LowerBound(start, (spectrum, target) => spectrum.ScanStartTime.CompareTo(target));
        var endIndex = _spectra.UpperBound(end, startIndex, _spectra.Count, (spectrum, target) => spectrum.ScanStartTime.CompareTo(target));
        var arrayPool = ArrayPool<ValuePeak>.Shared;
        var results = arrayPool.Rent(_ms1Counts[endIndex] - _ms1Counts[startIndex]);
        var idc = 0;
        for (int i = startIndex; i < endIndex; i++) {
            if (_spectra[i].MsLevel != 1 ||
                _spectra[i].ScanPolarity != _polarity) {
                continue;
            }
            var spectrum = _lazySpectra.GetOrAdd(i, index => new Spectrum(_spectra[index].Spectrum));
            var (basePeakMz, _, summedIntensity) = spectrum.RetrieveBin(mz, tolerance);
            results[idc++] = new ValuePeak(_spectra[i].Index, _idToRetentionTime[i].Value, basePeakMz, summedIntensity);
        }
        return new ExtractedIonChromatogram(results, idc, ChromXType.RT, _unit, mz, arrayPool);
    }

    public IEnumerable<ExtractedIonChromatogram> GetMs1ExtractedChromatograms_temp2(IEnumerable<double> mzs, double tolerance, double start, double end) {
        var startIndex = _spectra.LowerBound(start, (spectrum, target) => spectrum.ScanStartTime.CompareTo(target));
        var endIndex = _spectra.UpperBound(end, startIndex, _spectra.Count, (spectrum, target) => spectrum.ScanStartTime.CompareTo(target));
        var enumerators = new IEnumerator<Spectrum.SummarizedSpectrum>[_ms1Counts[endIndex] - _ms1Counts[startIndex]];
        var indexs = new int[_ms1Counts[endIndex] - _ms1Counts[startIndex]];
        var times = new double[_ms1Counts[endIndex] - _ms1Counts[startIndex]];
        var idc = 0;
        var mzs_ = mzs.ToList();
        for (int i = startIndex; i < endIndex; i++) {
            if (_spectra[i].MsLevel != 1 ||
                _spectra[i].ScanPolarity != _polarity) {
                continue;
            }
            var spectrum = _lazySpectra.GetOrAdd(i, index => new Spectrum(_spectra[index].Spectrum));
            enumerators[idc] = spectrum.RetrieveBins(mzs_, tolerance).GetEnumerator();
            indexs[idc] = _spectra[i].Index;
            times[idc] = _idToRetentionTime[i].Value;
            ++idc;
        }
        var counter = 0;
        while (true) {
            var peaks = ArrayPool<ValuePeak>.Shared.Rent(indexs.Length);
            for (int i = 0; i < indexs.Length; i++) {
                if (!enumerators[i].MoveNext()) {
                    ArrayPool<ValuePeak>.Shared.Return(peaks);
                    yield break;
                }
                var peak = enumerators[i].Current;
                peaks[i] = new ValuePeak(indexs[i], times[i], peak.BasePeakMz, peak.SummedIntensity);
            }
            yield return new ExtractedIonChromatogram(peaks, indexs.Length, ChromXType.RT, _unit, mzs_[counter++], ArrayPool<ValuePeak>.Shared);
        }
    }

    public Chromatogram GetMs1TotalIonChromatogram(double start, double end) {
        var startIndex = _spectra.LowerBound(start, (spectrum, target) => spectrum.ScanStartTime.CompareTo(target));
        var endIndex = _spectra.UpperBound(end, startIndex, _spectra.Count, (spectrum, target) => spectrum.ScanStartTime.CompareTo(target));
        var results = new List<ChromatogramPeak>();
        for (int i = startIndex; i < endIndex; i++) {
            if (_spectra[i].MsLevel != 1 ||
                _spectra[i].ScanPolarity != _polarity) {
                continue;
            }
            var (basePeakMz, _, summedIntensity) = new Spectrum(_spectra[i].Spectrum).RetrieveTotalIntensity();
            results.Add(ChromatogramPeak.Create(_spectra[i].Index, basePeakMz, summedIntensity, _idToRetentionTime[i]));
        }
        return new Chromatogram(results, ChromXType.RT, _unit);
    }

    /// <summary>
    /// Generates a chromatogram for product ions within specified precursor and product m/z ranges and a chromatogram time range.
    /// </summary>
    /// <param name="precursor">The m/z range of the precursor ions.</param>
    /// <param name="product">The m/z range of the product ions.</param>
    /// <param name="chromatogramRange">The range of retention times to include in the chromatogram.</param>
    /// <returns>A <see cref="ExtractedIonChromatogram"/> object representing the intensity of product ions across the specified retention time range.</returns>
    /// <remarks>
    /// This method filters spectra based on their MS level, the specified precursor and product m/z ranges, and the scan polarity.
    /// It then calculates the summed intensity of product ions that fall within the specified product m/z range for each spectrum,
    /// creating a chromatogram peak for each qualifying spectrum. The resulting chromatogram provides a visualization of
    /// how the intensity of specified product ions changes over the selected range of retention times.
    /// </remarks>
    public ExtractedIonChromatogram GetProductIonChromatogram(MzRange precursor, MzRange product, ChromatogramRange chromatogramRange) {
        var startIndex = _spectra.LowerBound(chromatogramRange.Begin, (spectrum, target) => spectrum.ScanStartTime.CompareTo(target));
        var endIndex = _spectra.UpperBound(chromatogramRange.End, startIndex, _spectra.Count, (spectrum, target) => spectrum.ScanStartTime.CompareTo(target));
        var results = new List<ValuePeak>();
        for (int i = startIndex; i < endIndex; i++) {
            if (_spectra[i].MsLevel != 2 ||
                _spectra[i].Precursor is null ||
                !_spectra[i].Precursor.ContainsMz(precursor.Mz, precursor.Tolerance, _acquisitionType) ||
                _spectra[i].ScanPolarity != _polarity) {
                continue;
            }
            var (basePeakMz, _, summedIntensity) = new Spectrum(_spectra[i].Spectrum).RetrieveBin(product.Mz, product.Tolerance);
            results.Add(new ValuePeak(_spectra[i].Index, _spectra[i].ScanStartTime, basePeakMz, summedIntensity));
        }
        return new ExtractedIonChromatogram(results, ChromXType.RT, _unit, product.Mz);
    }

    /// <summary>
    /// Generates a total ion chromatogram for MS2 spectra within a specified chromatogram range.
    /// </summary>
    /// <param name="chromatogramRange">The range of the chromatogram, specified by start and end retention times. The range should be of type ChromXType.RT.</param>
    /// <returns>A <see cref="Chromatogram"/> object representing the total ion chromatogram of MS2 spectra within the specified range. The chromatogram includes data points for each spectrum that falls within the range, specifying the index, scan start time, base peak m/z value, and summed intensity of the spectrum.</returns>
    /// <remarks>
    /// This method processes only MS2 level spectra, filtering out spectra based on the predefined scan polarity and the specified retention time range. It aggregates the total ion intensity from each selected MS2 spectrum to construct the chromatogram. The method is optimized for performance by utilizing an array pool for efficient memory management during the grouping of spectra.
    /// </remarks>
    public Chromatogram GetMs2TotalIonChromatogram(ChromatogramRange chromatogramRange) {
        var results = new List<ValuePeak>();
        var arrayPool = ArrayPool<RawSpectrum>.Shared;
        foreach (var spectra in GroupedSpectrum(chromatogramRange, arrayPool)) {
            var (basePeakMz, basePeakIntensity, summedIntensity) = spectra.Select(s => new Spectrum(s.Spectrum).RetrieveTotalIntensity()).Aggregate(AccumulateSpectra);
            results.Add(new ValuePeak(spectra.Array[0].Index, spectra.Array[0].ScanStartTime, basePeakMz, summedIntensity));
            arrayPool.Return(spectra.Array);
        }
        return new Chromatogram(results, ChromXType.RT, _unit);
    }

    /// <summary>
    /// Accumulates spectra information, selecting the base peak with the highest intensity and summing total intensities.
    /// </summary>
    /// <param name="x">The first spectrum to compare.</param>
    /// <param name="y">The second spectrum to compare.</param>
    /// <returns>A tuple containing the m/z value of the base peak with the highest intensity, that peak's intensity, and the summed intensity of both spectra.</returns>
    private (double, double, double) AccumulateSpectra((double BasePeakMz, double BasePeakIntensity, double SummedIntensity) x, (double BasePeakMz, double BasePeakIntensity, double SummedIntensity) y) {
        if (x.BasePeakIntensity > y.BasePeakIntensity) {
            return (x.BasePeakMz, x.BasePeakIntensity, x.SummedIntensity + y.SummedIntensity);
        }
        else {
            return (y.BasePeakMz, y.BasePeakIntensity, x.SummedIntensity + y.SummedIntensity);
        }
    }

    /// <summary>
    /// Groups spectra by MS level and scan polarity, utilizing a buffer and array pool for efficient memory management.
    /// </summary>
    /// <param name="chromatogramRange">The chromatogram range based on retention time.</param>
    /// <param name="arrayPool">An array pool for reusing arrays and reducing GC pressure.</param>
    /// <returns>An enumerable of grouped spectra segments, ready for processing.</returns>
    /// <remarks>
    /// This method organizes spectra into groups suitable for generating a total ion chromatogram. It filters spectra by scan polarity and groups adjacent MS2 level spectra together, skipping over MS1 level spectra and ensuring all returned groups are relevant for TIC generation.
    /// </remarks>
    private IEnumerable<ArraySegment<RawSpectrum>> GroupedSpectrum(ChromatogramRange chromatogramRange, ArrayPool<RawSpectrum> arrayPool) {
        System.Diagnostics.Debug.Assert(chromatogramRange.Type == ChromXType.RT);
        var startIndex = _spectra.LowerBound(chromatogramRange.Begin, (spectrum, target) => spectrum.ScanStartTime.CompareTo(target));
        if (startIndex - 1 < 0 || _spectra[startIndex - 1].MsLevel != 1) {
            while (startIndex < _spectra.Count && _spectra[startIndex].MsLevel != 1) {
                ++startIndex;
            }
            if (startIndex < _spectra.Count) {
                ++startIndex;
            }
        }
        var endIndex = _spectra.UpperBound(chromatogramRange.End, startIndex, _spectra.Count, (spectrum, target) => spectrum.ScanStartTime.CompareTo(target));
        while (endIndex < _spectra.Count && _spectra[endIndex].MsLevel != 1) {
            ++endIndex;
        }
        var buffer = new List<RawSpectrum>();
        for (int i = startIndex; i < endIndex; i++) {
            if (_spectra[i].ScanPolarity != _polarity) {
                continue;
            }
            else if (_spectra[i].MsLevel == 1) {
                if (buffer.Count > 0) {
                    var result = arrayPool.Rent(buffer.Count);
                    buffer.CopyTo(result);
                    yield return new ArraySegment<RawSpectrum>(result, 0, buffer.Count);
                    buffer.Clear();
                }
            }
            else if (_spectra[i].MsLevel == 2) {
                buffer.Add(_spectra[i]);
            }
        }
        if (buffer.Count > 0) {
            var result = arrayPool.Rent(buffer.Count);
            buffer.CopyTo(result);
            yield return new ArraySegment<RawSpectrum>(result, 0, buffer.Count);
            buffer.Clear();
        }
    }

    /// <summary>
    /// Generates a total ion chromatogram for MS2 spectra from a specific experiment within a specified chromatogram range.
    /// </summary>
    /// <param name="chromatogramRange">The range of the chromatogram, specified by start and end retention times. The range should be of type ChromXType.RT.</param>
    /// <param name="experimentID">The ID of the experiment from which to retrieve MS2 spectra. Only spectra matching this experiment ID are included in the chromatogram.</param>
    /// <returns>A <see cref="SpecificExperimentChromatogram"/> object representing the total ion chromatogram of MS2 spectra from the specified experiment within the given range. The chromatogram includes data points for each spectrum that matches the experiment ID and falls within the range, specifying the index, scan start time, base peak m/z value, and summed intensity of the spectrum.</returns>
    /// <remarks>
    /// In addition to filtering by MS level (MS2) and scan polarity, this method also filters spectra by the specified experiment ID, allowing for more targeted analysis within complex datasets. It calculates the total ion chromatogram by summing the intensities of all ions in each selected spectrum.
    /// </remarks>
    public SpecificExperimentChromatogram GetMS2TotalIonChromatogram(ChromatogramRange chromatogramRange, int experimentID) {
        System.Diagnostics.Debug.Assert(chromatogramRange.Type == ChromXType.RT);
        var startIndex = _spectra.LowerBound(chromatogramRange.Begin, (spectrum, target) => spectrum.ScanStartTime.CompareTo(target));
        var endIndex = _spectra.UpperBound(chromatogramRange.End, startIndex, _spectra.Count, (spectrum, target) => spectrum.ScanStartTime.CompareTo(target));
        var results = new List<ValuePeak>();
        for (int i = startIndex; i < endIndex; i++) {
            if (_spectra[i].MsLevel != 2 ||
                _spectra[i].ExperimentID != experimentID ||
                _spectra[i].ScanPolarity != _polarity) {
                continue;
            }
            var (basePeakMz, _, summedIntensity) = new Spectrum(_spectra[i].Spectrum).RetrieveTotalIntensity();
            results.Add(new ValuePeak(_spectra[i].Index, _spectra[i].ScanStartTime, basePeakMz, summedIntensity));
        }
        return new SpecificExperimentChromatogram(results, ChromXType.RT, _unit, experimentID);
    }

    /// <summary>
    /// Generates an extracted ion chromatogram from MS2 spectra for a specified m/z range and experiment ID within a given chromatogram range.
    /// </summary>
    /// <param name="product">The target m/z range for the extracted ion. Includes the center m/z value and tolerance for the extraction.</param>
    /// <param name="chromatogramRange">The range of the chromatogram, specified by start and end retention times. The range should be of type ChromXType.RT.</param>
    /// <param name="experimentID">The ID of the experiment from which to retrieve MS2 spectra. Only spectra matching this experiment ID and falling within the specified m/z range are included in the chromatogram.</param>
    /// <returns>A <see cref="ExtractedIonChromatogram"/> object representing the extracted ion chromatogram of MS2 spectra for the specified m/z range and experiment ID within the given range. The chromatogram includes data points for each spectrum that matches the criteria, specifying the index, scan start time, base peak m/z value, and summed intensity of the extracted ion.</returns>
    /// <remarks>
    /// This method filters spectra based on MS level (MS2), scan polarity, specified experiment ID, and the targeted m/z range. It calculates the extracted ion chromatogram by summing the intensities of ions within the specified m/z range for each selected spectrum. This approach enables targeted analysis of specific ions within complex datasets, providing insights into the presence and behavior of particular molecules across the experiment.
    /// </remarks>
    public ExtractedIonChromatogram GetMS2ExtractedIonChromatogram(MzRange product, ChromatogramRange chromatogramRange, int experimentID) {
        System.Diagnostics.Debug.Assert(chromatogramRange.Type == ChromXType.RT);
        var startIndex = _spectra.LowerBound(chromatogramRange.Begin, (spectrum, target) => spectrum.ScanStartTime.CompareTo(target));
        var endIndex = _spectra.UpperBound(chromatogramRange.End, startIndex, _spectra.Count, (spectrum, target) => spectrum.ScanStartTime.CompareTo(target));
        var results = new List<ValuePeak>();
        for (int i = startIndex; i < endIndex; i++) {
            if (_spectra[i].MsLevel != 2 ||
                _spectra[i].ExperimentID != experimentID ||
                _spectra[i].ScanPolarity != _polarity) {
                continue;
            }
            var (basePeakMz, _, summedIntensity) = new Spectrum(_spectra[i].Spectrum).RetrieveBin(product.Mz, product.Tolerance);
            results.Add(new ValuePeak(_spectra[i].Index, _spectra[i].ScanStartTime, basePeakMz, summedIntensity));
        }
        return new ExtractedIonChromatogram(results, ChromXType.RT, _unit, product.Mz);
    }
}
