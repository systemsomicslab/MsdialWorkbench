using CompMs.App.Msdial.Model.Core;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcmsApi.Parameter;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Lcms
{
    class LcmsAnalysisParameterSetModel : AnalysisParameterSetModelBase
    {
        public LcmsAnalysisParameterSetModel(MsdialLcmsParameter parameter, IEnumerable<AnalysisFileBean> files)
            : base(parameter, files) {

            LcmsParameter = parameter;
        }

        public MsdialLcmsParameter LcmsParameter { get; }
    }
}
