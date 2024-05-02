using CompMs.App.Msdial.Model.Core;
using CompMs.CommonMVVM;
using Reactive.Bindings.Notifiers;
using System;

namespace CompMs.App.Msdial.Model.Setting
{
    internal sealed class DatasetSettingModel : BindableBase
    {
        public DatasetSettingModel(bool isReadOnly, Action<IDatasetModel> handler, IMessageBroker broker) {
            IsReadOnlyDatasetParameter = isReadOnly;
            this.handler = handler;

            var dt = DateTime.Now;
            DatasetFileSettingModel = new DatasetFileSettingModel(dt);
            DatasetParameterSettingModel = new DatasetParameterSettingModel(dt, DatasetFileSettingModel, PrepareMethodSetting, broker);
        }

        public DatasetSettingModel(IDatasetModel model, IMessageBroker broker) {
            IsReadOnlyDatasetParameter = true;
            handler = null;

            Result = model;
            DatasetFileSettingModel = new DatasetFileSettingModel(model.AnalysisFiles);
            DatasetParameterSettingModel = new DatasetParameterSettingModel(model.Storage.Parameter, DatasetFileSettingModel, broker);
        }

        public MethodSettingModel? MethodSettingModel {
            get => methodSettingModel;
            private set => SetProperty(ref methodSettingModel, value);
        }
        private MethodSettingModel? methodSettingModel;
        private readonly Action<IDatasetModel>? handler;

        public DatasetFileSettingModel DatasetFileSettingModel { get; }

        public DatasetParameterSettingModel DatasetParameterSettingModel { get; }

        public bool IsReadOnlyDatasetParameter { get; }

        public IDatasetModel? Result {
            get => result;
            private set => SetProperty(ref result, value);
        }
        private IDatasetModel? result;

        private void PrepareMethodSetting(DatasetModel datasetModel) {
            Result = datasetModel;
            MethodSettingModel = datasetModel.AllProcessMethodSettingModel;
        }

        public void Run() {
            if (handler is null || Result is null) {
                return;
            }
            handler.Invoke(Result);
        }
    }
}
