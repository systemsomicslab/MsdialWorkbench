using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Loader
{
    internal sealed class BpcLoader {
        public BpcLoader(RawSpectra rawSpectra, ParameterBase parameter, ChromXType chromXType, ChromXUnit chromXUnit, double rangeBegin, double rangeEnd) {
            _parameter = parameter;
            _chromatogramRange = new ChromatogramRange(rangeBegin, rangeEnd, chromXType, chromXUnit);
            _rawSpectra = rawSpectra;
        }

        private readonly RawSpectra _rawSpectra;
        private readonly ParameterBase _parameter;
        private readonly ChromatogramRange _chromatogramRange;

        internal List<PeakItem> LoadBpc() {
            var bpc = LoadBpcCore();
            if (bpc.Count == 0) {
                return new List<PeakItem>(0);
            }
            return bpc;
        }

        private List<PeakItem> LoadBpcCore() {
            return _rawSpectra
                .GetMs1BasePeakChromatogram(_chromatogramRange)
                .Smoothing(_parameter.SmoothingMethod, _parameter.SmoothingLevel)
                .Where(peak => peak != null)
                .Select(peak => new PeakItem(peak))
                .ToList();
        }
    }
}
