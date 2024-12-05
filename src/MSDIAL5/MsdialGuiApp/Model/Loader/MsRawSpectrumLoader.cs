using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Loader
{
    internal sealed class MsRawSpectrumLoader : IMsSpectrumLoader<QuantifiedChromatogramPeak?>
    {
        private readonly IDataProvider _provider;
        private readonly MSDataType _dataType;

        public MsRawSpectrumLoader(IDataProvider provider, MSDataType dataType)
        {
            _provider = provider;
            _dataType = dataType;
        }

        private async Task<IMSScanProperty?> LoadMsPropertymCoreAsync(QuantifiedChromatogramPeak? target, CancellationToken token) {
            if (target is null || target.MS1RawSpectrumIdTop < 0) {
                return null;
            }
            var msSpectrum = await _provider.LoadMsSpectrumFromIndexAsync(target.MS1RawSpectrumIdTop, token).ConfigureAwait(false);
            var spectra = DataAccess.GetCentroidMassSpectra(msSpectrum, _dataType, 0f, float.MinValue, float.MaxValue);
            return new MSScanProperty(target.MS1RawSpectrumIdTop, 0d, target.PeakFeature.ChromXsTop.GetRepresentativeXAxis(), IonMode.Positive) { Spectrum = spectra };
        }

        IObservable<IMSScanProperty?> IMsSpectrumLoader<QuantifiedChromatogramPeak?>.LoadScanAsObservable(QuantifiedChromatogramPeak? target) {
            return Observable.FromAsync(token => LoadMsPropertymCoreAsync(target, token));
        }
    }
}
