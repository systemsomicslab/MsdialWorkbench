using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
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

        MoleculeDataBase molecules;
        public override ISerializableAnnotatorContainer Build(ParameterBase parameter) {
            if (molecules is null) {
                molecules = LoadDataBase(parameter);
            }
            return Build(parameter.ProjectParam, molecules);
        }

        private ISerializableAnnotatorContainer Build(ProjectBaseParameter projectParameter, MoleculeDataBase molecules) {
            return new DatabaseAnnotatorContainer(
                new MassAnnotator(molecules, Parameter, projectParameter.TargetOmics, AnnotationSource, DataBaseID),
                molecules,
                Parameter);
        }

        private MoleculeDataBase LoadDataBase(ParameterBase parameter) {
            switch (DBSource) {
                case DataBaseSource.Msp:
                case DataBaseSource.Lbm:
                    return new MoleculeDataBase(LoadMspDataBase(DataBasePath, DBSource, parameter), DataBaseID, DataBaseSource.Lbm, SourceType.MspDB);
                case DataBaseSource.Text:
                    var textdb = TextLibraryParser.TextLibraryReader(DataBasePath, out string error);
                    if (!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }
                    return new MoleculeDataBase(textdb, DataBaseID, DataBaseSource.Text, SourceType.TextDB);
                default:
                    throw new NotSupportedException(DBSource.ToString());
            }
        }
    }
}
