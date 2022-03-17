using CompMs.App.Msdial.Model.Core;
using CompMs.CommonMVVM;
using System;

namespace CompMs.App.Msdial.Model.Setting
{
    public class DatasetSettingModel : BindableBase
    {
        public DatasetSettingModel(bool isReadOnly, Action<IDatasetModel> handler) {
            IsReadOnlyDatasetParameter = isReadOnly;
            this.handler = handler;

            var dt = DateTime.Now;
            DatasetFileSettingModel = new DatasetFileSettingModel(dt);
            DatasetParameterSettingModel = new DatasetParameterSettingModel(dt, DatasetFileSettingModel, PrepareMethodSetting);
        }

        public DatasetSettingModel(IDatasetModel model) {
            IsReadOnlyDatasetParameter = true;
            handler = null;

            DatasetFileSettingModel = new DatasetFileSettingModel(model.Storage.AnalysisFiles);
            DatasetParameterSettingModel = new DatasetParameterSettingModel(model.Storage.Parameter, DatasetFileSettingModel);
        }

        public MethodSettingModel MethodSettingModel {
            get => methodSettingModel;
            private set => SetProperty(ref methodSettingModel, value);
        }
        private MethodSettingModel methodSettingModel;
        private readonly Action<IDatasetModel> handler;

        public DatasetFileSettingModel DatasetFileSettingModel { get; }

        public DatasetParameterSettingModel DatasetParameterSettingModel { get; }

        public bool IsReadOnlyDatasetParameter { get; }

        public IDatasetModel Result {
            get => result;
            private set => SetProperty(ref result, value);
        }
        private IDatasetModel result;

        private void PrepareMethodSetting(DatasetModel datasetModel) {
            Result = datasetModel;
            MethodSettingModel = datasetModel.AllProcessMethodSettingModel;
        }

        public void Run() {
            handler?.Invoke(Result);
        }
    }
}
