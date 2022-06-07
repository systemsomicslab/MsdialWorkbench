using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using Reactive.Bindings.Notifiers;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.Model.Core
{
    internal sealed class MainWindowModel : BindableBase
    {
        public MainWindowModel(IMessageBroker broker) {
            ProjectSetting = new ProjectSettingModel(SetNewProject, broker);
            nowSaving = new BusyNotifier();
            _broker = broker;
            nowLoading = new BusyNotifier();
        }

        public IObservable<bool> NowSaving => nowSaving;
        private readonly BusyNotifier nowSaving;
        private readonly IMessageBroker _broker;

        public IObservable<bool> NowLoading => nowLoading;
        private readonly BusyNotifier nowLoading;

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
            ProjectSetting = new ProjectSettingModel(SetNewProject, _broker);
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
            using (nowLoading.ProcessStart()) {
                try {
                    var loadedProject = await ProjectModel.LoadAsync(_broker).ConfigureAwait(true);
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
}
