using CompMs.App.Msdial.Model.DataObj;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;

namespace CompMs.App.Msdial.Model.Setting
{
    interface IAnnotationSettingModel
    {
        string AnnotatorID { get; }

        Annotator Build(ParameterBase parameter);
        Annotator Build(ProjectBaseParameter projectParameter, MoleculeDataBase molecules);
        MoleculeDataBase LoadDataBase(ParameterBase parameter);
    }
}
