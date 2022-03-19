using CompMs.App.Msdial.Model.Core;
using CompMs.CommonMVVM;
using System;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Setting
{
    public class ProjectSettingModel : BindableBase
    {
        public ProjectSettingModel(Func<IProjectModel, Task> run) {
            ProjectParameterSettingModel = new ProjectParameterSettingModel(PrepareDatasetSetting);
            IsReadOnlyProjectParameter = false;
            this.run = run;
        }

        public ProjectSettingModel(IProjectModel model) {
            Result = model;
            DatasetSettingModel = model.DatasetSettingModel;
            ProjectParameterSettingModel = new ProjectParameterSettingModel(model.Storage.ProjectParameter);
            IsReadOnlyProjectParameter = true;
            run = null;
        }

        private readonly Func<IProjectModel, Task> run;

        public IProjectModel Result {
            get => result;
            private set => SetProperty(ref result, value);
        }
        private IProjectModel result;

        public ProjectParameterSettingModel ProjectParameterSettingModel { get; }

        public DatasetSettingModel DatasetSettingModel {
            get => datasetSettingModel;
            private set => SetProperty(ref datasetSettingModel, value);
        }
        private DatasetSettingModel datasetSettingModel;

        public bool IsReadOnlyProjectParameter { get; }

        private void PrepareDatasetSetting(ProjectModel projectModel) {
            DatasetSettingModel = projectModel.DatasetSettingModel;
            Result = projectModel;
        }

        public Task RunAsync() {
            return run?.Invoke(Result) ?? Task.CompletedTask;
        }
    }
}
