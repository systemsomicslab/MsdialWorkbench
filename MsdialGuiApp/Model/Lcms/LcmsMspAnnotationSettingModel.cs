using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Setting;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialLcMsApi.Algorithm.Annotation;
using System;

namespace CompMs.App.Msdial.Model.Lcms
{
    sealed class LcmsMspAnnotationSettingModel : MspAnnotationSettingModel
    {
        public LcmsMspAnnotationSettingModel(DataBaseAnnotationSettingModelBase other)
            : base(other) {
            
        }

        public override IAnnotatorContainer Build(ParameterBase parameter) {
            var molecules = LoadDataBase(parameter);
            return Build(parameter.ProjectParam, molecules);
        }

        public override IAnnotatorContainer Build(ProjectBaseParameter projectParameter, MoleculeDataBase molecules) {
            return new DatabaseAnnotatorContainer(
                new LcmsMspAnnotator(molecules.Database, Parameter, projectParameter.TargetOmics, AnnotatorID),
                molecules,
                Parameter);
        }

        public override MoleculeDataBase LoadDataBase(ParameterBase parameter) {
            switch (DBSource) {
                case DataBaseSource.Msp:
                    return new MoleculeDataBase(LoadMspDataBase(DataBasePath), DataBaseID);
                default:
                    throw new NotSupportedException(DBSource.ToString());
            }
        }
    }
}
