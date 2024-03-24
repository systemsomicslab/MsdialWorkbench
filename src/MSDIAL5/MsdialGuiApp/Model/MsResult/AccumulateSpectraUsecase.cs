using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.MsResult
{
    internal sealed class AccumulateSpectraUsecase
    {
        private readonly IDataProvider _provider;
        private readonly PeakPickBaseParameter _parameter;
        private readonly IonMode _ionMode;

        public AccumulateSpectraUsecase(IDataProvider provider, PeakPickBaseParameter parameter, IonMode ionMode)
        {
            _provider = provider;
            _parameter = parameter;
            _ionMode = ionMode;
        }

        public async Task<MSScanProperty> AccumulateMs1Async((double rtStart, double rtEnd) rtRange, IEnumerable<(double rtStart, double rtEnd)> subtracts, CancellationToken token = default) {
            var provider = _provider;
            var (rtStart, rtEnd) = rtRange;
            var spectra = await provider.LoadMs1SpectraWithRtRangeAsync(rtStart, rtEnd, token).ConfigureAwait(false);
            var peaks = DataAccess.GetAverageSpectrum(spectra, _parameter.CentroidMs2Tolerance);

            var subsTask = subtracts.Select(r => provider.LoadMs1SpectraWithRtRangeAsync(r.rtStart, r.rtEnd, token)).ToArray();
            var subs = await Task.WhenAll(subsTask);
            var subPeaks = DataAccess.GetAverageSpectrum(subs.SelectMany(s => s).ToArray(), _parameter.CentroidMs1Tolerance);
            var subtracted = DataAccess.GetSubtractSpectrum(peaks, subPeaks, _parameter.CentroidMs1Tolerance);

            var rt = new RetentionTime((rtStart + rtEnd) / 2, ChromXUnit.Min);
            var scan = new MSScanProperty
            {
                IonMode = _ionMode,
                ChromXs = new ChromXs(rt),
                Spectrum = subtracted,
            };
            return scan;
        }

        public async Task<MSScanProperty> AccumulateMs2Async((double rtStart, double rtEnd) rtRange, IEnumerable<(double rtStart, double rtEnd)> subtracts, CancellationToken token = default) {
            var (rtStart, rtEnd) = rtRange;
            var spectra = await _provider.LoadMs2SpectraWithRtRangeAsync(rtStart, rtEnd, token).ConfigureAwait(false);
            var peaks = DataAccess.GetAverageSpectrum(spectra, _parameter.CentroidMs2Tolerance);

            var subsTask = subtracts.Select(r => _provider.LoadMs2SpectraWithRtRangeAsync(r.rtStart, r.rtEnd, token)).ToArray();
            var subs = await Task.WhenAll(subsTask);
            var subPeaks = DataAccess.GetAverageSpectrum(subs.SelectMany(s => s).ToArray(), _parameter.CentroidMs2Tolerance);
            var subtracted = DataAccess.GetSubtractSpectrum(peaks, subPeaks, _parameter.CentroidMs2Tolerance);

            var rt = new RetentionTime((rtStart + rtEnd) / 2, ChromXUnit.Min);
            var scan = new MSScanProperty
            {
                IonMode = _ionMode,
                ChromXs = new ChromXs(rt),
                Spectrum = subtracted,
            };
            return scan;
        }

        public async Task<MSScanProperty> AccumulateMs2Async(double mz, double mzTolerance, (double rtStart, double rtEnd) rtRange, IEnumerable<(double rtStart, double rtEnd)> subtracts, CancellationToken token = default) {
            var provider = _provider.FilterByMz(mz, mzTolerance).Cache();
            var (rtStart, rtEnd) = rtRange;
            var spectra = await provider.LoadMs2SpectraWithRtRangeAsync(rtStart, rtEnd, token).ConfigureAwait(false);
            var peaks = DataAccess.GetAverageSpectrum(spectra, _parameter.CentroidMs2Tolerance);

            var subsTask = subtracts.Select(r => provider.LoadMs2SpectraWithRtRangeAsync(r.rtStart, r.rtEnd, token)).ToArray();
            var subs = await Task.WhenAll(subsTask);
            var subPeaks = DataAccess.GetAverageSpectrum(subs.SelectMany(s => s).ToArray(), _parameter.CentroidMs2Tolerance);
            var subtracted = DataAccess.GetSubtractSpectrum(peaks, subPeaks, _parameter.CentroidMs2Tolerance);

            var rt = new RetentionTime((rtStart + rtEnd) / 2, ChromXUnit.Min);
            var scan = new MSScanProperty
            {
                PrecursorMz = mz,
                IonMode = _ionMode,
                ChromXs = new ChromXs(rt),
                Spectrum = subtracted,
            };
            return scan;
        }

        public async Task<MSScanProperty> AccumulateMs2Async(int experimentID, (double rtStart, double rtEnd) rtRange, IEnumerable<(double rtStart, double rtEnd)> subtracts, CancellationToken token = default) {
            var provider = _provider.SelectExperimentID(experimentID).Cache();
            var (rtStart, rtEnd) = rtRange;
            var spectra = await provider.LoadMs2SpectraWithRtRangeAsync(rtStart, rtEnd, token).ConfigureAwait(false);
            var peaks = DataAccess.GetAverageSpectrum(spectra, _parameter.CentroidMs2Tolerance);

            var subsTask = subtracts.Select(r => provider.LoadMs2SpectraWithRtRangeAsync(r.rtStart, r.rtEnd, token)).ToArray();
            var subs = await Task.WhenAll(subsTask);
            var subPeaks = DataAccess.GetAverageSpectrum(subs.SelectMany(s => s).ToArray(), _parameter.CentroidMs2Tolerance);
            var subtracted = DataAccess.GetSubtractSpectrum(peaks, subPeaks, _parameter.CentroidMs2Tolerance);

            var (hi, lo) = provider.GetMassRange();
            var rt = new RetentionTime((rtStart + rtEnd) / 2, ChromXUnit.Min);
            var scan = new MSScanProperty
            {
                PrecursorMz = (hi + lo) / 2,
                IonMode = _ionMode,
                ChromXs = new ChromXs(rt),
                Spectrum = subtracted,
            };
            return scan;
        }
    }
}
