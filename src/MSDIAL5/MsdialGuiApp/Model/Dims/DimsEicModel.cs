using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Dims
{
    internal sealed class DimsEicLoader : EicLoader, IChromatogramLoader
    {
        private static readonly double MZ_MARGIN = 10d;

        private readonly double _relativeRange;
        private readonly bool _isRelative;

        private DimsEicLoader(AnalysisFileBean analysisFile, IDataProvider provider, ParameterBase parameter, double rangeBegin, double rangeEnd) : base(analysisFile, provider, parameter, ChromXType.Mz, ChromXUnit.Mz, rangeBegin, rangeEnd) {
            _isRelative = false;
        }

        private DimsEicLoader(AnalysisFileBean analysisFile, IDataProvider provider, ParameterBase parameter, double rangeBegin, double rangeEnd, double relativeRange) : base(analysisFile, provider, parameter, ChromXType.Mz, ChromXUnit.Mz, rangeBegin, rangeEnd) {
            _isRelative = true;
            _relativeRange = relativeRange;
        }

        protected override Task<List<PeakItem>> LoadEicCoreAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            var width = _isRelative
                ? target.InnerModel.PeakWidth(ChromXType.Mz) / 2d * _relativeRange
                : MZ_MARGIN;
            var leftMz = (target.ChromXValue ?? 0d) - width;
            var rightMz = (target.ChromXValue ?? 0d) + width;
            return Task.Run(async () =>
            {
                var spectra = await provider.LoadMs1SpectrumsAsync(token).ConfigureAwait(false);
                return new CompMs.Common.Components.Chromatogram(DataAccess.ConvertRawPeakElementToChromatogramPeakList(spectra.Argmax(spectrum => spectrum.Spectrum.Length).Spectrum, leftMz, rightMz), ChromXType.Mz, ChromXUnit.Mz)
                    .Smoothing(parameter.SmoothingMethod, parameter.SmoothingLevel)
                    .Select(peak => new PeakItem(peak))
                    .ToList();
            });
        }

        public static DimsEicLoader BuildForEicView(AnalysisFileBean analysisFile, IDataProvider provider, ParameterBase parameter) {
            return new DimsEicLoader(analysisFile, provider, parameter, parameter.MassRangeBegin, parameter.MassRangeEnd);
        }

        public static DimsEicLoader BuildForPeakTable(AnalysisFileBean analysisFile, IDataProvider provider, ParameterBase parameter) {
            return new DimsEicLoader(analysisFile, provider, parameter, parameter.MassRangeBegin, parameter.MassRangeEnd, relativeRange: 3d);
        }
    }
}
