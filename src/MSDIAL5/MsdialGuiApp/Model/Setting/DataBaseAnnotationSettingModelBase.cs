using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;

namespace CompMs.App.Msdial.Model.Setting
{

    public abstract class DataBaseAnnotationSettingModelBase : BindableBase
    {
        public DataBaseAnnotationSettingModelBase(DataBaseAnnotationSettingModelBase model) {
            DataBasePath = model.DataBasePath;
            DataBaseID = model.DataBaseID;
            DBSource = model.DBSource;
            AnnotationSource = model.AnnotationSource;
            annotatorID = model.AnnotatorID;
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

        public DataBaseSource DBSource {
            get => source;
            set => SetProperty(ref source, value);
        }
        private DataBaseSource source = DataBaseSource.None;

        public SourceType AnnotationSource {
            get => annotationSource;
            set => SetProperty(ref annotationSource, value);
        }
        private SourceType annotationSource;

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

        //public abstract ISerializableAnnotatorContainer Build(ParameterBase parameter);

        
    }
}