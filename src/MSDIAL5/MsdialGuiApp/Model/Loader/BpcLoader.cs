using CompMs.App.Msdial.Model.DataObj;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System.Collections.Generic;
using System.Linq;

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

        private List<PeakItem> LoadBpcCore() {
            return _rawSpectra
                .GetMs1BasePeakChromatogram(_chromatogramRange)
                .Smoothing(_peakPickParameter.SmoothingMethod, _peakPickParameter.SmoothingLevel)
                .Where(peak => peak != null)
                .Select(peak => new PeakItem(peak))
                .ToList();
        }

        List<PeakItem> IWholeChromatogramLoader.LoadChromatogram() => LoadBpcCore();
    }
}
