using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parser;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialLcMsApi.Algorithm.Annotation;
using System;

namespace CompMs.App.Msdial.Model.Lcms
{
    sealed class LcmsTextDBAnnotationSettingModel : DataBaseAnnotationSettingModelBase
    {
        public LcmsTextDBAnnotationSettingModel(DataBaseAnnotationSettingModelBase other)
            : base(other) {
            
        }

        public override ISerializableAnnotatorContainer Build(ParameterBase parameter) {
            var molecules = LoadDataBase();
            return Build(molecules);
        }

        private ISerializableAnnotatorContainer Build(MoleculeDataBase molecules) {
            return new DatabaseAnnotatorContainer(
                new LcmsTextDBAnnotator(molecules, Parameter, AnnotatorID),
                molecules,
                Parameter);
        }

        private MoleculeDataBase LoadDataBase() {
            switch (DBSource) {
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
