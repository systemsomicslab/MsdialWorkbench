using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Loader
{
    internal sealed class MsRawSpectrumLoader : IMsSpectrumLoader<QuantifiedChromatogramPeak>
    {
        private readonly IDataProvider _provider;
        private readonly MSDataType _dataType;
        private readonly ChromDecBaseParameter _chromDecParameter;

        public MsRawSpectrumLoader(IDataProvider provider, MSDataType dataType, ChromDecBaseParameter chromDecParameter) {
            _provider = provider;
            _dataType = dataType;
            _chromDecParameter = chromDecParameter;
        }

        public Task<List<SpectrumPeak>> LoadSpectrumAsync(QuantifiedChromatogramPeak target, CancellationToken token) {
            return target is null
                ? Task.FromResult(new List<SpectrumPeak>())
                : LoadSpectrumCoreAsync(target, token);
        }

        private async Task<List<SpectrumPeak>> LoadSpectrumCoreAsync(QuantifiedChromatogramPeak target, CancellationToken token) {
            if (target.MS1RawSpectrumIdTop < 0) {
                return new List<SpectrumPeak>(0);
            }
            var msSpectrum = await _provider.LoadMsSpectrumFromIndexAsync(target.MS1RawSpectrumIdTop, token).ConfigureAwait(false);
            var spectra = DataAccess.GetCentroidMassSpectra(msSpectrum, _dataType, 0f, float.MinValue, float.MaxValue);
            if (_chromDecParameter.RemoveAfterPrecursor) {
                spectra = spectra.Where(peak => peak.Mass <= target.PeakFeature.Mass + _chromDecParameter.KeptIsotopeRange).ToList();
            }
            return spectra;
        }

        public IObservable<List<SpectrumPeak>> LoadSpectrumAsObservable(QuantifiedChromatogramPeak target) {
            return Observable.FromAsync(token => LoadSpectrumAsync(target, token));
        }
    }
}
