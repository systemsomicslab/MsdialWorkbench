using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        //protected virtual List<ChromatogramPeakWrapper> LoadTicCore() {
        //    return DataAccess.GetSmoothedPeaklist(
        //        DataAccess.GetMs1Peaklist(
        //                provider.LoadMs1Spectrums(),
        //                target.Mass, parameter.CentroidMs1Tolerance,
        //                parameter.IonMode,
        //                chromXType, chromXUnit,
        //                rangeBegin, rangeEnd),
        //            parameter.SmoothingMethod, parameter.SmoothingLevel)
        //    .Where(peak => peak != null)
        //    .Select(peak => new ChromatogramPeakWrapper(peak))
        //    .ToList();
        //}
    }
}
