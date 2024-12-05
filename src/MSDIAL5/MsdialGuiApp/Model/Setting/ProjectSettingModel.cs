using CompMs.App.Msdial.Model.Core;
using CompMs.CommonMVVM;
using Reactive.Bindings.Notifiers;
using System;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Setting
{
    internal sealed class ProjectSettingModel : BindableBase
    {
        public ProjectSettingModel(Func<IProjectModel, Task> run, IMessageBroker broker) {
            ProjectParameterSettingModel = new ProjectParameterSettingModel(PrepareDatasetSetting, broker);
            IsReadOnlyProjectParameter = false;
            this.run = run;
        }

        public ProjectSettingModel(IProjectModel model, IMessageBroker broker) {
            Result = model;
            DatasetSettingModel = model.DatasetSettingModel;
            ProjectParameterSettingModel = new ProjectParameterSettingModel(model.Storage.ProjectParameter, broker);
            IsReadOnlyProjectParameter = true;
            run = null;
        }

        private readonly Func<IProjectModel, Task>? run;

        public IProjectModel? Result {
            get => result;
            private set => SetProperty(ref result, value);
        }
        private IProjectModel? result;

        public ProjectParameterSettingModel ProjectParameterSettingModel { get; }

        public DatasetSettingModel? DatasetSettingModel {
            get => datasetSettingModel;
            private set => SetProperty(ref datasetSettingModel, value);
        }
        private DatasetSettingModel? datasetSettingModel;

        public bool IsReadOnlyProjectParameter { get; }

        private void PrepareDatasetSetting(ProjectModel projectModel) {
            DatasetSettingModel = projectModel.DatasetSettingModel;
            Result = projectModel;
        }

        public Task RunAsync() {
            if (Result is null || run is null) {
                return Task.CompletedTask;
            }
            return run.Invoke(Result);
        }
    }
}
