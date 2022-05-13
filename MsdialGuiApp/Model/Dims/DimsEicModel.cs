using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Dims
{
    class DimsEicLoader : EicLoader
    {
        public DimsEicLoader(
            IDataProvider provider,
            ParameterBase parameter,
            double rangeBegin,
            double rangeEnd)
            : base(provider, parameter, ChromXType.Mz, ChromXUnit.Mz, rangeBegin, rangeEnd) {

        }

        protected override Task<List<ChromatogramPeakWrapper>> LoadEicCoreAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            var leftMz = target.ChromXValue - 10 ?? 0;
            var rightMz = target.ChromXValue + 10 ?? 0;
            return Task.Run(async () =>
            {
                var spectra = await provider.LoadMs1SpectrumsAsync(token).ConfigureAwait(false);
                return DataAccess.GetSmoothedPeaklist(
                    DataAccess.ConvertRawPeakElementToChromatogramPeakList(
                        spectra.Argmax(spectrum => spectrum.Spectrum.Length).Spectrum, leftMz, rightMz),
                    parameter.SmoothingMethod, parameter.SmoothingLevel)
                    .Select(peak => new ChromatogramPeakWrapper(peak))
                    .ToList();
            });
        }
    }
}
