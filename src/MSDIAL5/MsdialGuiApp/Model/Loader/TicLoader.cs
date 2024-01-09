using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Loader
{
    internal sealed class TicLoader : IWholeChromatogramLoader {
        private readonly ParameterBase _parameter;
        private readonly RawSpectra _rawSpectra;
        private readonly ChromatogramRange _chromatogramRange;

        public TicLoader(AnalysisFileBean file, IDataProvider provider, ParameterBase parameter, ChromXType chromXType, ChromXUnit chromXUnit, double rangeBegin, double rangeEnd) {
            _parameter = parameter;
            _rawSpectra = new RawSpectra(provider.LoadMs1Spectrums(), parameter.IonMode, file.AcquisitionType);
            _chromatogramRange = new ChromatogramRange(rangeBegin, rangeEnd, chromXType, chromXUnit);
        }

        private List<PeakItem> LoadTicCore() {
            var chromatogram = _rawSpectra.GetMs1TotalIonChromatogram(_chromatogramRange);
            return chromatogram
                .Smoothing(_parameter.SmoothingMethod, _parameter.SmoothingLevel)
                .Where(peak => peak != null)
                .Select(peak => new PeakItem(peak))
                .ToList();
        }

        List<PeakItem> IWholeChromatogramLoader.LoadChromatogram() => LoadTicCore();
    }
}
