using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Dims
{
    class DimsEicLoader : EicLoader
    {
        public DimsEicLoader(
            IDataProvider provider,
            ParameterBase parameter,
            ChromXType chromXType,
            ChromXUnit chromXUnit,
            double rangeBegin,
            double rangeEnd)
            : base(provider, parameter, chromXType, chromXUnit, rangeBegin, rangeEnd) {

        }

        protected override List<ChromatogramPeakWrapper> LoadEicCore(ChromatogramPeakFeatureModel target) {
            var leftMz = target.ChromXValue - 10 ?? 0;
            var rightMz = target.ChromXValue + 10 ?? 0;
            return DataAccess.GetSmoothedPeaklist(
                DataAccess.ConvertRawPeakElementToChromatogramPeakList(
                    provider.LoadMs1Spectrums().Argmax(spectrum => spectrum.Spectrum.Length).Spectrum, leftMz, rightMz),
                parameter.SmoothingMethod, parameter.SmoothingLevel)
                .Select(peak => new ChromatogramPeakWrapper(peak))
                .ToList();
        }
    }
}
