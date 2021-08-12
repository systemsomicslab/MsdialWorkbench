using CompMs.App.Msdial.Model.Core;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcmsApi.Parameter;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Lcms
{
    public class LcmsAnalysisParameterSetModel : AnalysisParameterSetModelBase
    {
        public LcmsAnalysisParameterSetModel(MsdialLcmsParameter parameter, IEnumerable<AnalysisFileBean> files)
            : base(parameter, files) {

            Parameter = parameter;
        }

        public MsdialLcmsParameter Parameter { get; }
    }
}
