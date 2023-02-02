using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parameter;
using System;

namespace CompMs.App.Msdial.Model.Setting
{
    public interface IAnnotationSettingModel
    {
        string AnnotatorID { get; }

        ISerializableAnnotatorContainer<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult> Build(ParameterBase parameter);
    }
}
