using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;

namespace CompMs.App.Msdial.Model.Core
{
    public class MainWindowModel : BindableBase
    {
        public IProjectModel CurrentProject { get; private set; }

        public ProjectSettingModel ProjectSetting { get; private set; }

        public void CreateNewProjectSetting() {
            if (ProjectSetting is null) {
                ProjectSetting = new ProjectSettingModel();
            }
        }

        public void StartNewProject() {
            var project = new ProjectModel();

            if (project.Start()) {
                CurrentProject = project;
            }
        }

        public void AddNewDataset() {
            var project = CurrentProject;
            if (project is null) {
                return;
            }

            project.Add();
        }

        public void ChangeDataset(DatasetModel dataset) {
            var project = CurrentProject;
            if (project is null) {
                return;
            }

            project.Change(dataset);
        }

        public void OpenProject() {
            CurrentProject = new ProjectModel(); // load project
        }

        public void ReprocessAll() {
            CurrentProject?.Reprocess(ProcessOption.All);
        }

        public void ReprocessFromAnnotation() {
            CurrentProject?.Reprocess(ProcessOption.IdentificationPlusAlignment);
        }

        public void ReprocessFromAlignment() {
            CurrentProject?.Reprocess(ProcessOption.Alignment);
        }
    }
}
