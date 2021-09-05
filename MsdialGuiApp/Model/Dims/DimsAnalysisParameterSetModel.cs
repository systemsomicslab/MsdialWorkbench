using CompMs.App.Msdial.Model.Core;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialDimsCore.Parameter;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Dims
{
    public sealed class DimsAnalysisParameterSetModel : AnalysisParameterSetModelBase
    {
        public DimsAnalysisParameterSetModel(MsdialDimsParameter parameter, IEnumerable<AnalysisFileBean> files)
            : base(parameter, files) {
            Parameter = parameter;

            DataCollectionSettingModel = new DimsDataCollectionSettingModel(parameter.ProcessBaseParam, parameter.PeakPickBaseParam);
        }

        public MsdialDimsParameter Parameter { get; }

        public DimsDataCollectionSettingModel DataCollectionSettingModel { get; }

        public IAnnotationProcess BuildAnnotationProcess() {
            var annotators = new List<IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>>();
            foreach (var annotation in AnnotationProcessSettingModel.Annotations) {
                annotators.Add(annotation.Build(ParameterBase));
            }
            return new StandardAnnotationProcess<IAnnotationQuery>(
                new AnnotationQueryWithoutIsotopeFactory(),
                annotators);
        }
    }
}
