using CompMs.App.Msdial.Model.Core;
using CompMs.CommonMVVM;
using System;

namespace CompMs.App.Msdial.Model.Setting
{
    public class ProjectSettingModel : BindableBase
    {
        private readonly Action<IProjectModel> continuous;

        public ProjectSettingModel(Action<IProjectModel> continuous) {
            this.continuous = continuous;
        }

        public bool IsComplete {
            get => isComplete;
            private set => SetProperty(ref isComplete, value);
        }
        private bool isComplete;

        public void Execute() {
            var project = new ProjectModel();

            continuous?.Invoke(project);
        }
    }
}