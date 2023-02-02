using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Loader
{
    internal sealed class BpcLoader {
        public BpcLoader(
            IDataProvider provider,
            ParameterBase parameter,
            ChromXType chromXType,
            ChromXUnit chromXUnit,
            double rangeBegin,
            double rangeEnd) {

            _provider = provider;
            _parameter = parameter;
            _chromXType = chromXType;
            _chromXUnit = chromXUnit;
            _chromatogramRange = new ChromatogramRange(rangeBegin, rangeEnd, chromXType, chromXUnit);
        }

        private readonly IDataProvider _provider;
        private readonly ParameterBase _parameter;
        private readonly ChromXType _chromXType;
        private readonly ChromXUnit _chromXUnit;
        private readonly ChromatogramRange _chromatogramRange;

        internal List<PeakItem>
            LoadBpc() {

            var bpc = LoadBpcCore();
            if (bpc.Count == 0) {
                return new List<PeakItem>();
            }

            return bpc;
        }

        private List<PeakItem> LoadBpcCore() {
            return new RawSpectra(_provider.LoadMs1Spectrums(), _parameter.IonMode, _parameter.AcquisitionType)
                .GetMs1BasePeakChromatogram(_chromatogramRange)
                .Smoothing(_parameter.SmoothingMethod, _parameter.SmoothingLevel)
                .Where(peak => peak != null)
                .Select(peak => new PeakItem(peak))
                .ToList();
        }
    }
}
