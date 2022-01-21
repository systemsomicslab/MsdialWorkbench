using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;

namespace CompMs.App.Msdial.Model.Core
{
    public class MainWindowModel : BindableBase
    {
        public IProjectModel CurrentProject { get; private set; }

        public ProjectSettingModel ProjectSetting {
            get {
                if (projectSetting is null || projectSetting.IsComplete) {
                    projectSetting = new ProjectSettingModel(SetNewProject);
                }
                return projectSetting;
            }
        }
        private ProjectSettingModel projectSetting;

        private void SetNewProject(IProjectModel project) {
            CurrentProject = project;
        }
    }
}
