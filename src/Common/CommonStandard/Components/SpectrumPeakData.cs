using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CompMs.Common.Components
{
    /// <summary>Numeric access without expanding a compact reference into mutable peak objects.</summary>
    public readonly struct SpectrumPeakData
    {
        private readonly List<SpectrumPeak> _peaks;
        private readonly SpectrumPeakValue[] _values;
        private readonly uint[] _packedValues;

        public bool SharesStorageWith(SpectrumPeakData other)
            => ReferenceEquals(_values, other._values) && ReferenceEquals(_packedValues, other._packedValues) && ReferenceEquals(_peaks, other._peaks);

        internal SpectrumPeakData(List<SpectrumPeak> peaks) { _peaks = peaks; _values = null; _packedValues = null; }
        internal SpectrumPeakData(SpectrumPeakValue[] values) { _values = values; _peaks = null; _packedValues = null; }
        internal SpectrumPeakData(uint[] packedValues) { _packedValues = packedValues; _values = null; _peaks = null; }

        internal static SpectrumPeakData FromCompact(Array values) => values is uint[] packed
            ? new SpectrumPeakData(packed) : new SpectrumPeakData((SpectrumPeakValue[])values);

        public static SpectrumPeakData From(IMSScanProperty scan) => scan is MoleculeMsReference reference
            ? reference.GetSpectrumPeakData() : new SpectrumPeakData(scan?.Spectrum);

        public int Count {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _values != null ? _values.Length : _packedValues != null ? _packedValues.Length : _peaks != null ? _peaks.Count : 0;
        }

        public SpectrumPeakValue this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                if (_values != null) { return _values[index]; }
                if (_packedValues != null) {
                    var packed = _packedValues[index];
                    return new SpectrumPeakValue((int)(packed >> 16), (int)(packed & 0xffffu));
                }
                var peak = _peaks[index];
                return new SpectrumPeakValue(peak.Mass, peak.Intensity);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double MassAt(int index) => _values != null ? _values[index].Mass : _packedValues != null ? (int)(_packedValues[index] >> 16) : _peaks[index].Mass;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double IntensityAt(int index) => _values != null ? _values[index].Intensity : _packedValues != null ? (int)(_packedValues[index] & 0xffffu) : _peaks[index].Intensity;

        internal double MaxIntensity() {
            if (Count == 0) { throw new InvalidOperationException("Spectrum contains no peaks."); }
            var maximum = IntensityAt(0);
            for (var i = 1; i < Count; i++) {
                var intensity = IntensityAt(i);
                if (intensity > maximum || double.IsNaN(maximum)) { maximum = intensity; }
            }
            return maximum;
        }

        internal List<SpectrumPeak> ToMutableList() {
            var result = new List<SpectrumPeak>(Count);
            for (var i = 0; i < Count; i++) {
                var peak = this[i];
                result.Add(new SpectrumPeak(peak.Mass, peak.Intensity));
            }
            return result;
        }
    }

    public readonly struct SpectrumPeakValue
    {
        public SpectrumPeakValue(double mass, double intensity) { Mass = mass; Intensity = intensity; }
        public double Mass { get; }
        public double Intensity { get; }
    }
}
