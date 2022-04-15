using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using Reactive.Bindings.Notifiers;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.Model.Core
{
    public class MainWindowModel : BindableBase
    {
        public MainWindowModel() {
            ProjectSetting = new ProjectSettingModel(SetNewProject);
            nowSaving = new BusyNotifier();
        }

        public IObservable<bool> NowSaving => nowSaving;
        private readonly BusyNotifier nowSaving;

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

        private Task SetNewProject(IProjectModel project) {
            CurrentProject = project;
            ProjectSetting = new ProjectSettingModel(SetNewProject);
            return Task.CompletedTask;
        }

        public async Task SaveAsync() {
            if (CurrentProject is null) {
                return;
            }
            using (nowSaving.ProcessStart()) {
                await CurrentProject.SaveAsync().ConfigureAwait(false);
            }
        }

        public async Task SaveAsAsync() {
            if (CurrentProject is null) {
                return;
            }
            using (nowSaving.ProcessStart()) {
                await CurrentProject.SaveAsAsync().ConfigureAwait(false);
            }
        }

        public async Task LoadAsync() {
            try {
                var loadedProject = await ProjectModel.LoadAsync().ConfigureAwait(true);
                if (!(loadedProject is null)) {
                    CurrentProject = loadedProject;
                }
            }
            catch {
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    MessageBox.Show("Failed to load project.\nPlease check your project.");
                    return Task.CompletedTask;
                });
            }
        }
    }
}
