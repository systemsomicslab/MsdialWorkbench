using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parser;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;

namespace CompMs.App.Msdial.Model.Setting
{
    public abstract class TextDBAnnotationSettingModel : DataBaseAnnotationSettingModelBase
    {
        public TextDBAnnotationSettingModel(DataBaseAnnotationSettingModelBase other)
            : base(other) {

        }


        public override ISerializableAnnotatorContainer Build(ParameterBase parameter) {
            return BuildCore(LoadDataBase(DataBaseID, DataBasePath, DBSource));
        }

        protected abstract ISerializableAnnotatorContainer BuildCore(MoleculeDataBase molecules);

        protected static MoleculeDataBase LoadDataBase(string id, string path, DataBaseSource dbsource) {
            switch (dbsource) {
                case DataBaseSource.Text:
                    var textdb = TextLibraryParser.TextLibraryReader(path, out string error);
                    if (!string.IsNullOrEmpty(error)) {
                        throw new Exception(error);
                    }
                    return new MoleculeDataBase(textdb, id, DataBaseSource.Text, SourceType.TextDB);
                default:
                    throw new NotSupportedException(dbsource.ToString());
            }
        }
    }
}