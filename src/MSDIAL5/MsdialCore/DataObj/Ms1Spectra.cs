using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Raw.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.DataObj
{
    public sealed class Ms1Spectra
    {
        private readonly ConcurrentDictionary<(ChromXType, ChromXUnit), IChromatogramTypedSpectra> _spectraImpls;
        private readonly IonMode _ionMode;
        private readonly AcquisitionType _acquisitionType;
        private readonly IDataProvider _spectraProvider;

        public Ms1Spectra(IonMode ionMode, AcquisitionType acquisitionType, IDataProvider spectraProvider) {
            _ionMode = ionMode;
            _acquisitionType = acquisitionType;
            _spectraProvider = spectraProvider;
            _spectraImpls = new ConcurrentDictionary<(ChromXType, ChromXUnit), IChromatogramTypedSpectra>();
        }

        public async Task<Chromatogram> GetMS1ExtractedChromatogramAsync(double mz, double tolerance, ChromatogramRange chromatogramRange, CancellationToken token) {
            var impl = BuildIfNotExists(chromatogramRange.Type, chromatogramRange.Unit);
            return await impl.GetMS1ExtractedChromatogramAsync(mz, tolerance, chromatogramRange.Begin, chromatogramRange.End, token).ConfigureAwait(false);
        }

        public Task<ExtractedIonChromatogram> GetMS1ExtractedChromatogramAsync(MzRange mzRange, ChromatogramRange chromatogramRange, CancellationToken token) {
            var impl = BuildIfNotExists(chromatogramRange.Type, chromatogramRange.Unit);
            return impl.GetMS1ExtractedChromatogramAsync(mzRange.Mz, mzRange.Tolerance, chromatogramRange.Begin, chromatogramRange.End, token);
        }

        /// <summary>
        /// Retrieves a representative spectrum from the collection of spectra.
        /// </summary>
        /// <returns>The representative <see cref="RawSpectrum"/> object.</returns>
        /// <remarks>
        /// This method selects a spectrum with MS level 1 and the largest number of peaks, 
        /// excluding empty or null spectra. It is designed with the assumption that, in the 
        /// typical use case, the collection contains only one spectrum. The selected spectrum 
        /// does not hold significant meaning and is chosen arbitrarily.
        /// </remarks>
        public RawSpectrum GetRepresentativeSpectrum() {
            return _spectraProvider.LoadMs1Spectrums()
                .Where(spec => spec.MsLevel == 1 && !spec.Spectrum.IsEmptyOrNull())
                .Argmax(spec => spec.Spectrum.Length);
        }

        private IChromatogramTypedSpectra BuildIfNotExists(ChromXType type, ChromXUnit unit) {
            return _spectraImpls.GetOrAdd((type, unit), pair => BuildTypedSpectra(pair.Item1, pair.Item2, _ionMode, _acquisitionType, _spectraProvider));
        }

        private static IChromatogramTypedSpectra BuildTypedSpectra(ChromXType type, ChromXUnit unit, IonMode ionMode, AcquisitionType acquisitionType, IDataProvider spectraProvider) {
            switch (type) {
                case ChromXType.RT:
                    return new RetentionTimeTypedSpectra(spectraProvider, unit, ionMode, acquisitionType);
                case ChromXType.Drift:
                    return new DriftTimeTypedSpectra(spectraProvider, unit, ionMode, acquisitionType);
                default:
                    throw new ArgumentException($"ChromXType {type} is not supported.");
            }
        }
    }
}
