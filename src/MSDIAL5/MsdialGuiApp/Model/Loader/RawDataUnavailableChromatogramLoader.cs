using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Service;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Loader
{
    /// <summary>
    /// The EIC of a sample whose raw data file is missing: always empty, with a description that says why,
    /// which the chart shows as its title (rather than "Max intensity: 0").
    /// </summary>
    internal sealed class RawDataUnavailableChromatogramLoader : IChromatogramLoader<ChromatogramPeakFeatureModel>
    {
        private readonly ChromXType _type;
        private readonly ChromXUnit _unit;

        public RawDataUnavailableChromatogramLoader(ChromXType type, ChromXUnit unit) {
            _type = type;
            _unit = unit;
        }

        public PeakChromatogram EmptyChromatogram => Create();

        public Task<PeakChromatogram> LoadChromatogramAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            return Task.FromResult(Create());
        }

        private PeakChromatogram Create() {
            return new PeakChromatogram(new Chromatogram(Array.Empty<ValuePeak>(), _type, _unit), null, string.Empty, Colors.Black, RawDataGuard.EicTitle);
        }
    }
}
