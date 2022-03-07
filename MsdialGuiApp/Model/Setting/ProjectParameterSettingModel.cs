using CompMs.App.Msdial.Model.Core;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using System;

namespace CompMs.App.Msdial.Model.Setting
{
    public class ProjectParameterSettingModel : BindableBase
    {
        public ProjectParameterSettingModel(Action<ProjectModel> next) {
            var dt = DateTime.Now;
            ProjectTitle = $"{dt:yyyy_MM_dd_hh_mm_ss}.msproject";
            this.next = next;
        }

        private readonly Action<ProjectModel> next;

        public string ProjectTitle {
            get => projectTitle;
            set => SetProperty(ref projectTitle, value);
        }
        private string projectTitle;

        public string ProjectFolderPath {
            get => projectFolderPath;
            set => SetProperty(ref projectFolderPath, value);
        }
        private string projectFolderPath;

        public void Build() {
            var title = ProjectTitle;
            if (!title.EndsWith(".msproject")) {
                title += ".msproject";
            }
            var parameter = new ProjectParameter(DateTime.Now, ProjectFolderPath, title);
            next?.Invoke(new ProjectModel(parameter));
        }
    }
}