using CompMs.Common.DataObj;
using System;
using System.Buffers;
using System.Collections.Generic;

namespace CompMs.Raw;

public static class RawSpectrumHandler
{
    static ArrayPool<RawPeakElement>? pool;

    public static void SetSpectrumProperties(RawSpectrum spectrum, double[] masses, double[] intensities, double peakCutOff, bool sortByMz = true) {
        pool ??= ArrayPool<RawPeakElement>.Shared;

        var basepeakIntensity = 0.0;
        var basepeakMz = 0.0;
        var totalIonCurrnt = 0.0;
        var lowestMz = double.MaxValue;
        var highestMz = double.MinValue;
        var minIntensity = double.MaxValue;

        var buffer = pool.Rent(masses.Length);
        var idx = 0;
        for (int i = 0; i < masses.Length; i++) {
            var mass = masses[i];
            var intensity = intensities[i];
            if (intensity <= peakCutOff) {
                continue;
            }
            totalIonCurrnt += intensity;

            if (intensity > basepeakIntensity) {
                basepeakIntensity = intensity;
                basepeakMz = mass;
            }
            if (lowestMz > mass) {
                lowestMz = mass;
            }
            if (highestMz < mass) {
                highestMz = mass;
            }
            if (minIntensity > intensity) {
                minIntensity = intensity;
            }

            var spec = new RawPeakElement() {
                Mz = Math.Round(mass, 5),
                Intensity = Math.Round(intensity, 0)
            };
            buffer[idx++] = spec;
        }
        var spectra = new RawPeakElement[idx];
        Array.Copy(buffer, spectra, idx);
        pool.Return(buffer);

        if (sortByMz) {
            Array.Sort(spectra, MzComparer.Instance);
        }
        spectrum.Spectrum = spectra;

        spectrum.DefaultArrayLength = spectra.Length;
        spectrum.BasePeakIntensity = basepeakIntensity;
        spectrum.BasePeakMz = basepeakMz;
        spectrum.TotalIonCurrent = totalIonCurrnt;
        spectrum.LowestObservedMz = lowestMz;
        spectrum.HighestObservedMz = highestMz;
        spectrum.MinIntensity = minIntensity;
    }

    class MzComparer : IComparer<RawPeakElement>
    {
        public static readonly MzComparer Instance = new();
        public int Compare(RawPeakElement x, RawPeakElement y) {
            return x.Mz.CompareTo(y.Mz);
        }
    }
}
