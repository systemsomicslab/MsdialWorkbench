using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Enum;
using CompMs.Common.Query;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialLcMsApi.Algorithm.Annotation;
using System;

namespace CompMs.App.Msdial.Model.Lcms
{
    sealed class LcmsLbmAnnotationSettingModel : LbmAnnotationSettingModel
    {
        public LcmsLbmAnnotationSettingModel(DataBaseAnnotationSettingModelBase other, ParameterBase parameter)
            : base(other) {

            LipidQueryContainer = parameter.LipidQueryContainer;
            IonMode = parameter.IonMode;
        }

        public LipidQueryBean LipidQueryContainer {
            get => lipidQueryContainer;
            set => SetProperty(ref lipidQueryContainer, value);
        }
        private LipidQueryBean lipidQueryContainer;

        public IonMode IonMode {
            get => ionMode;
            set => SetProperty(ref ionMode, value);
        }
        private IonMode ionMode;

        public override Annotator Build(ParameterBase parameter) {
            var molecules = LoadDataBase(parameter);
            return Build(parameter.ProjectParam, molecules);
        }

        public override Annotator Build(ProjectBaseParameter projectParameter, MoleculeDataBase molecules) {
            return new Annotator(
                new LcmsMspAnnotator(molecules.Database, Parameter, projectParameter.TargetOmics, AnnotatorID),
                Parameter);
        }

        public override MoleculeDataBase LoadDataBase(ParameterBase parameter) {
            switch (DBSource) {
                case DataBaseSource.Lbm:
                    return new MoleculeDataBase(LoadMspDataBase(DataBasePath, parameter), DataBaseID);
                default:
                    throw new NotSupportedException(DBSource.ToString());
            }
        }
    }
}
