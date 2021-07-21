using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Parser;
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

        public override Annotator Build(ParameterBase parameter) {
            var molecules = LoadDataBase(parameter);
            return Build(parameter.ProjectParam, molecules);
        }

        public override Annotator Build(ProjectBaseParameter projectParameter, MoleculeDataBase molecules) {
            return new Annotator(
                new LcmsTextDBAnnotator(molecules.Database, Parameter, AnnotatorID),
                Parameter);
        }

        public override MoleculeDataBase LoadDataBase(ParameterBase parameter) {
            switch (DBSource) {
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
