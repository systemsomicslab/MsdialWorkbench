using CompMs.App.Msdial.Model.Core;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using System;
using System.ComponentModel;

namespace CompMs.App.Msdial.Model.Setting
{
    public class ProjectParameterSettingModel : BindableBase
    {
        public ProjectParameterSettingModel(Action<IProjectModel> continuous) {
            this.continuous = continuous;
        }

        private readonly Action<IProjectModel> continuous;

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

        public bool IsComplete {
            get => isComplete;
            private set => SetProperty(ref isComplete, value);
        }
        private bool isComplete;

        public ProjectModel Build() {
            var parameter = new ProjectParameter(DateTime.UtcNow, ProjectFolderPath, ProjectTitle);
            var project = new ProjectModel(parameter);
            continuous?.Invoke(project);
            return project;
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs args) {
            base.OnPropertyChanged(args);
            if (args.PropertyName == nameof(ProjectFolderPath)) {
                IsComplete = false;
            }
        }
    }
}