using CompMs.App.Msdial.Model.Core;
using CompMs.CommonMVVM;
using System;

namespace CompMs.App.Msdial.Model.Setting
{
    public class ProjectSettingModel : BindableBase
    {
        public ProjectSettingModel(Action<IProjectModel> run) {
            ProjectParameterSettingModel = new ProjectParameterSettingModel(PrepareDatasetSetting);
            IsReadOnlyProjectParameter = false;
            this.run = run;
        }

        private readonly Action<IProjectModel> run;

        public ProjectModel Result {
            get => result;
            private set => SetProperty(ref result, value);
        }
        private ProjectModel result;

        public ProjectParameterSettingModel ProjectParameterSettingModel { get; }

        public DatasetSettingModel DatasetSettingModel {
            get => datasetSettingModel;
            private set => SetProperty(ref datasetSettingModel, value);
        }
        private DatasetSettingModel datasetSettingModel;

        public bool IsReadOnlyProjectParameter { get; }

        private void PrepareDatasetSetting(ProjectModel projectModel) {
            DatasetSettingModel = projectModel.DatasetSettingModel;
        }

        public void Run() {
            run?.Invoke(Result);
        }
    }
}
