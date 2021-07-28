using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;

namespace CompMs.App.Msdial.Model.Setting
{
    interface IAnnotationSettingModel
    {
        string AnnotatorID { get; }

        IAnnotatorContainer Build(ParameterBase parameter);
        IAnnotatorContainer Build(ProjectBaseParameter projectParameter, MoleculeDataBase molecules);
        MoleculeDataBase LoadDataBase(ParameterBase parameter);
    }
}
