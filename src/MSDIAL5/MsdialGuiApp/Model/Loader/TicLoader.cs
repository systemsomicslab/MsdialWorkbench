using CompMs.App.Msdial.Model.DataObj;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Loader
{
    internal sealed class TicLoader : IWholeChromatogramLoader {
        private readonly RawSpectra _rawSpectra;
        private readonly ChromatogramRange _chromatogramRange;
        private readonly PeakPickBaseParameter _peakPickParameter;

        public TicLoader(RawSpectra rawSpectra, ChromatogramRange chromatogramRange, PeakPickBaseParameter peakPickParameter) {
            _peakPickParameter = peakPickParameter;
            _rawSpectra = rawSpectra;
            _chromatogramRange = chromatogramRange;
        }

        private List<PeakItem> LoadTicCore() {
            var chromatogram = _rawSpectra.GetMs1TotalIonChromatogram(_chromatogramRange);
            return chromatogram
                .ChromatogramSmoothing(_peakPickParameter.SmoothingMethod, _peakPickParameter.SmoothingLevel).AsPeakArray()
                .Where(peak => peak != null)
                .Select(peak => new PeakItem(peak))
                .ToList();
        }

        List<PeakItem> IWholeChromatogramLoader.LoadChromatogram() => LoadTicCore();
    }
}
