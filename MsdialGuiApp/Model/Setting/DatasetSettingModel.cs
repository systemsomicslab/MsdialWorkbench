using CompMs.App.Msdial.Model.Core;
using CompMs.CommonMVVM;
using System;

namespace CompMs.App.Msdial.Model.Setting
{
    public class DatasetSettingModel : BindableBase
    {
        public DatasetSettingModel(bool isReadOnly, Action<DatasetModel> handler) {
            IsReadOnlyDatasetParameter = isReadOnly;
            this.handler = handler;

            var dt = DateTime.Now;
            DatasetFileSettingModel = new DatasetFileSettingModel(dt);
            DatasetParameterSettingModel = new DatasetParameterSettingModel(dt, DatasetFileSettingModel, PrepareMethodSetting);
        }

        public MethodSettingModel MethodSettingModel {
            get => methodSettingModel;
            private set => SetProperty(ref methodSettingModel, value);
        }
        private MethodSettingModel methodSettingModel;
        private readonly Action<DatasetModel> handler;

        public DatasetFileSettingModel DatasetFileSettingModel { get; }

        public DatasetParameterSettingModel DatasetParameterSettingModel { get; }

        public bool IsReadOnlyDatasetParameter { get; }

        private void PrepareMethodSetting(DatasetModel datasetModel) {
            MethodSettingModel = datasetModel.AllProcessMethodSettingModel;
            handler?.Invoke(datasetModel);
        }
    }
}
