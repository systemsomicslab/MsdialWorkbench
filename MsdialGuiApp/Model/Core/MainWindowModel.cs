using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;

namespace CompMs.App.Msdial.Model.Core
{
    public class MainWindowModel : BindableBase
    {
        public MainWindowModel() {
            ProjectSetting = new ProjectSettingModel(SetNewProject);
        }

        public IProjectModel CurrentProject {
            get => currentProject;
            private set => SetProperty(ref currentProject, value);
        }
        private IProjectModel currentProject;

        public ProjectSettingModel ProjectSetting {
            get => projectSetting;
            private set => SetProperty(ref projectSetting, value);
        }
        private ProjectSettingModel projectSetting;

        private void SetNewProject(IProjectModel project) {
            CurrentProject = project;
            ProjectSetting = new ProjectSettingModel(SetNewProject);
        }
    }
}
