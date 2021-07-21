using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;

namespace CompMs.App.Msdial.Model.Setting
{
    abstract class DataBaseAnnotationSettingModelBase : BindableBase, IAnnotationSettingModel
    {
        public DataBaseAnnotationSettingModelBase() {

        }

        public DataBaseAnnotationSettingModelBase(DataBaseAnnotationSettingModelBase model) {
            DataBasePath = model.DataBasePath;
            DataBaseID = model.DataBaseID;
            Source = model.Source;
            AnnotatorID = model.AnnotatorID;
            Parameter = model.Parameter;
        }

        public string DataBasePath {
            get => dataBasePath;
            set => SetProperty(ref dataBasePath, value);
        }
        private string dataBasePath = string.Empty;

        public string DataBaseID {
            get => dataBaseID;
            set => SetProperty(ref dataBaseID, value);
        }
        private string dataBaseID = string.Empty;

        public SourceType Source {
            get => source;
            set => SetProperty(ref source, value);
        }
        private SourceType source = SourceType.None;

        public string AnnotatorID {
            get => annotatorID;
            set => SetProperty(ref annotatorID, value);
        }
        private string annotatorID;

        public MsRefSearchParameterBase Parameter {
            get => parameter;
            set => SetProperty(ref parameter, value);
        }
        private MsRefSearchParameterBase parameter = new MsRefSearchParameterBase();

        public abstract Annotator Build(ParameterBase parameter);
        public abstract Annotator Build(ProjectBaseParameter projectParameter, MoleculeDataBase molecules);
        public abstract MoleculeDataBase LoadDataBase(ParameterBase parameter);
    }
}