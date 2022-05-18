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
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Loader {
    class BpcLoader {
        public BpcLoader(
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
            LoadBpc() {

            var bpc = LoadBpcCore();
            if (bpc.Count == 0) {
                return new List<ChromatogramPeakWrapper>();
            }

            return bpc;
        }

        protected virtual List<ChromatogramPeakWrapper> LoadBpcCore() {
            return new RawSpectra(provider.LoadMs1Spectrums(), chromXType, chromXUnit, parameter.IonMode)
                .GetMs1BasePeakChromatogram(rangeBegin, rangeEnd)
                .Smoothing(parameter.SmoothingMethod, parameter.SmoothingLevel)
                .Where(peak => peak != null)
                .Select(peak => new ChromatogramPeakWrapper(peak))
                .ToList();
        }
    }
}
