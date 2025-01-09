using CompMs.App.Msdial.Model.DataObj;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;

namespace CompMs.App.Msdial.Model.Loader
{
    internal sealed class BpcLoader : IWholeChromatogramLoader {
        private readonly RawSpectra _rawSpectra;
        private readonly ChromatogramRange _chromatogramRange;
        private readonly PeakPickBaseParameter _peakPickParameter;

        public BpcLoader(RawSpectra rawSpectra, ChromatogramRange chromatogramRange, PeakPickBaseParameter peakPickParameter) {
            _chromatogramRange = chromatogramRange;
            _rawSpectra = rawSpectra;
            _peakPickParameter = peakPickParameter;
        }

        [System.Obsolete("zzz")]
        DisplayChromatogram IWholeChromatogramLoader.LoadChromatogram() {
            using var chromatogram = _rawSpectra.GetMS1BasePeakChromatogramAsync(_chromatogramRange, default).Result;
            var smoothed = chromatogram.ChromatogramSmoothing(_peakPickParameter.SmoothingMethod, _peakPickParameter.SmoothingLevel);
            return new DisplayChromatogram(smoothed);
        }
    }
}
