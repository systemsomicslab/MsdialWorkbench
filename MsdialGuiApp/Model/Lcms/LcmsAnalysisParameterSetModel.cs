using CompMs.App.Msdial.Model.Core;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
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

        public IAnnotationProcess BuildAnnotationProcess() {
            var annotators = new List<IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>>();
            foreach (var annotation in AnnotationProcessSettingModel.Annotations) {
                annotators.Add(annotation.Build(ParameterBase));
            }
            return new StandardAnnotationProcess<IAnnotationQuery>(new AnnotationQueryFactory(Parameter.PeakPickBaseParam), annotators);
        }
    }
}
