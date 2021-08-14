using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parameter;
using System;

namespace CompMs.App.Msdial.Model.Setting
{
    public interface IAnnotationSettingModel
    {
        string AnnotatorID { get; }

        ISerializableAnnotatorContainer Build(ParameterBase parameter);
    }
}
