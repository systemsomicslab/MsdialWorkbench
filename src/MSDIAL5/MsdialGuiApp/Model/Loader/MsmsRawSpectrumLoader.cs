using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.MsdialCore.Algorithm;
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
    internal sealed class MsmsRawSpectrumLoader : IMsSpectrumLoader<ChromatogramPeakFeatureModel>
    {
        public MsmsRawSpectrumLoader(IDataProvider provider, ParameterBase parameter) {
            this.provider = provider;
            this.parameter = parameter;
        }

        private readonly IDataProvider provider;
        private readonly ParameterBase parameter;

        public Task<List<SpectrumPeak>> LoadSpectrumAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            if (target is null) {
                throw new ArgumentNullException(nameof(target));
            }
            return LoadSpectrumCoreAsync(target, token);
        }

        private async Task<List<SpectrumPeak>> LoadSpectrumCoreAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            if (target.MS2RawSpectrumId < 0) {
                return new List<SpectrumPeak>(0);
            }
            var msSpectra = await provider.LoadMsSpectrumsAsync(token).ConfigureAwait(false);
            var spectra = DataAccess.GetCentroidMassSpectra(
                msSpectra[target.MS2RawSpectrumId],
                parameter.MS2DataType,
                0f, float.MinValue, float.MaxValue);
            if (parameter.RemoveAfterPrecursor) {
                spectra = spectra.Where(peak => peak.Mass <= target.Mass + parameter.KeptIsotopeRange).ToList();
            }
            return spectra;
        }

        public IObservable<List<SpectrumPeak>> LoadSpectrumAsObservable(ChromatogramPeakFeatureModel target) {
            return Observable.FromAsync(token => LoadSpectrumAsync(target, token));
        }
    }
}
