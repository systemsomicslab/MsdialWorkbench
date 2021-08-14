using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.DataObj.Result;
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

        public override ISerializableAnnotatorContainer Build(ParameterBase parameter) {
            var molecules = LoadDataBase();
            return Build(parameter.ProjectParam, molecules);
        }

        private ISerializableAnnotatorContainer Build(ProjectBaseParameter projectParameter, MoleculeDataBase molecules) {
            return new DatabaseAnnotatorContainer(
                new LcmsMspAnnotator(molecules.Database, Parameter, projectParameter.TargetOmics, AnnotatorID),
                molecules,
                Parameter);
        }

        private MoleculeDataBase LoadDataBase() {
            switch (DBSource) {
                case DataBaseSource.Msp:
                    return new MoleculeDataBase(LoadMspDataBase(DataBasePath), DataBaseID, DataBaseSource.Msp, SourceType.MspDB);
                default:
                    throw new NotSupportedException(DBSource.ToString());
            }
        }
    }
}
