using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Loader
{
    internal sealed class TicLoader {
        public TicLoader(AnalysisFileBean file, IDataProvider provider, ParameterBase parameter, ChromXType chromXType, ChromXUnit chromXUnit, double rangeBegin, double rangeEnd) {
            this.parameter = parameter;
            _rawSpectra = new RawSpectra(provider.LoadMs1Spectrums(), parameter.IonMode, file.AcquisitionType);
            _chromatogramRange = new ChromatogramRange(rangeBegin, rangeEnd, chromXType, chromXUnit);
        }

        private readonly ParameterBase parameter;
        private readonly RawSpectra _rawSpectra;
        private readonly ChromatogramRange _chromatogramRange;

        internal List<PeakItem> LoadTic() {
            var tic = LoadTicCore();
            if (tic.Count == 0) {
                return new List<PeakItem>(0);
            }
            return tic;
        }

        private List<PeakItem> LoadTicCore() {
            var chromatogram = _rawSpectra.GetMs1TotalIonChromatogram(_chromatogramRange);
            return chromatogram
                .Smoothing(parameter.SmoothingMethod, parameter.SmoothingLevel)
                .Where(peak => peak != null)
                .Select(peak => new PeakItem(peak))
                .ToList();
        }
    }
}
