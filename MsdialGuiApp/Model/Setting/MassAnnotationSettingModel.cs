using CompMs.Common.Parser;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;

namespace CompMs.App.Msdial.Model.Setting
{
    sealed class MassAnnotationSettingModel : DataBaseAnnotationSettingModelBase
    {
        public MassAnnotationSettingModel() {

        }

        public MassAnnotationSettingModel(DataBaseAnnotationSettingModelBase other)
            : base(other) {
            
        }

        public override IAnnotatorContainer Build(ParameterBase parameter) {
            var molecules = LoadDataBase(parameter);
            return Build(parameter.ProjectParam, molecules);
        }

        public override IAnnotatorContainer Build(ProjectBaseParameter projectParameter, MoleculeDataBase molecules) {
            return new DatabaseAnnotatorContainer(
                new MassAnnotator(molecules.Database, Parameter, projectParameter.TargetOmics, AnnotationSource, DataBaseID),
                molecules,
                Parameter);
        }

        public override MoleculeDataBase LoadDataBase(ParameterBase parameter) {
            switch (DBSource) {
                case DataBaseSource.Msp:
                case DataBaseSource.Lbm:
                    return new MoleculeDataBase(LoadMspDataBase(DataBasePath, DBSource, parameter), DataBaseID);
                case DataBaseSource.Text:
                    var textdb = TextLibraryParser.TextLibraryReader(DataBasePath, out string error);
                    if (!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }
                    return new MoleculeDataBase(textdb, DataBaseID);
                default:
                    throw new NotSupportedException(DBSource.ToString());
            }
        }
    }
}
