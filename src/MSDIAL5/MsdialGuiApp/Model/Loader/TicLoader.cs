using CompMs.App.Msdial.Model.DataObj;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;

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

        DisplayChromatogram IWholeChromatogramLoader.LoadChromatogram() {
            var chromatogram = _rawSpectra.GetMS1TotalIonChromatogram(_chromatogramRange)
                .ChromatogramSmoothing(_peakPickParameter.SmoothingMethod, _peakPickParameter.SmoothingLevel);
            return new DisplayChromatogram(chromatogram);
        }
    }
}
