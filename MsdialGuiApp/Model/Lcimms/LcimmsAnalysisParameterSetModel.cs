using CompMs.App.Msdial.Model.Core;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcImMsApi.Parameter;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Lcimms
{
    class LcimmsAnalysisParameterSetModel : AnalysisParameterSetModelBase
    {
        public LcimmsAnalysisParameterSetModel(MsdialLcImMsParameter parameter, IEnumerable<AnalysisFileBean> files)
            : base(parameter, files) {
            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }

            LcimmsParameter = parameter;
        }

        public MsdialLcImMsParameter LcimmsParameter { get; }
    }
}
