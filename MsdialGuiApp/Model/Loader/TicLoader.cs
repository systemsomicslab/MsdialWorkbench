using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Loader {
    class TicLoader {
        public TicLoader(
            IDataProvider provider,
            ParameterBase parameter,
            ChromXType chromXType,
            ChromXUnit chromXUnit,
            double rangeBegin,
            double rangeEnd) {
            
            this.provider = provider;
            this.parameter = parameter;
            this.chromXType = chromXType;
            this.chromXUnit = chromXUnit;
            this.rangeBegin = rangeBegin;
            this.rangeEnd = rangeEnd;
        }

        protected readonly IDataProvider provider;
        protected readonly ParameterBase parameter;
        protected readonly ChromXType chromXType;
        protected readonly ChromXUnit chromXUnit;
        protected readonly double rangeBegin, rangeEnd;


        internal List<ChromatogramPeakWrapper>
            LoadTic() {

            var tic = LoadTicCore();
            if (tic.Count == 0) {
                return new List<ChromatogramPeakWrapper>();
            }

            return tic;
        }

        protected virtual List<ChromatogramPeakWrapper> LoadTicCore() {
            var rawSpectra = new RawSpectra(provider.LoadMs1Spectrums(), parameter.IonMode, parameter.AcquisitionType);
            var chromatogramRange = new ChromatogramRange(rangeBegin, rangeEnd, chromXType, chromXUnit);
            var chromatogram = rawSpectra.GetMs1TotalIonChromatogram(chromatogramRange);
            return chromatogram
                .Smoothing(parameter.SmoothingMethod, parameter.SmoothingLevel)
                .Where(peak => peak != null)
                .Select(peak => new ChromatogramPeakWrapper(peak))
                .ToList();
        }
    }
}
