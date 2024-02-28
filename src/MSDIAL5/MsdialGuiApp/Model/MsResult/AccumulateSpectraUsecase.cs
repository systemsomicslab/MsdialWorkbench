using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
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

        public async Task<MSScanProperty> AccumulateMs2Async(double mz, double mzTolerance, double rtStart, double rtEnd, CancellationToken token = default) {
            var spectrum = await _provider.LoadMsNSpectrumsAsync(level: 2, token).ConfigureAwait(false);
            var filtered = spectrum.Where(s => Math.Abs(s.Precursor.SelectedIonMz - mz) < mzTolerance).ToArray();
            var peaks = DataAccess.GetAverageSpectrum(filtered, rtStart, rtEnd, _parameter.CentroidMs2Tolerance);
            return new MSScanProperty
            {
                PrecursorMz = mz,
                IonMode = _ionMode,
                ChromXs = new ChromXs(new RetentionTime((rtStart + rtEnd) / 2, ChromXUnit.Min)),
                Spectrum = peaks,
            };
        }
    }
}
