using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parameter;
using System;

namespace CompMs.App.Msdial.Model.Setting
{
    public interface IAnnotationSettingModel : IAnnotationSettingModel<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> {

    }

    public interface IAnnotationSettingModel<T, U, V>
    {
        string AnnotatorID { get; }

        ISerializableAnnotatorContainer<T, U, V> Build(ParameterBase parameter);
    }
}
