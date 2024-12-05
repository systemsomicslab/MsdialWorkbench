using CompMs.App.Msdial.Dto;
using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using CompMs.Common.Enum;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.Model.Core
{
    internal sealed class MainWindowModel : BindableBase
    {
        private readonly IMessageBroker _broker;
        private readonly Properties.Settings _settings;

        public MainWindowModel(IMessageBroker broker) {
            projectSetting = new ProjectSettingModel(SetNewProject, broker);
            nowSaving = new BusyNotifier();
            _broker = broker;
            nowLoading = new BusyNotifier();

            _settings = Properties.Settings.Default;
            if (_settings.ShouldUpgrade) {
                _settings.Upgrade();
                _settings.ShouldUpgrade = false;
                _settings.Save();
            }
            if (_settings.PreviousProjects is null) {
                _settings.PreviousProjects = new List<ProjectCrumb>();
                _settings.Save();
            }
            _previousProjects = _settings.PreviousProjects;
            PreviousProjects = _previousProjects.AsReadOnly();

            //InternalMsfinderSettingModel = new InternalMsfinderSettingModel(IonMode.Negative);
        }

        public InternalMsfinderSettingModel InternalMsfinderSettingModel { get; }

        public IObservable<bool> NowSaving => nowSaving;
        private readonly BusyNotifier nowSaving;

        public IObservable<bool> NowLoading => nowLoading;
        private readonly BusyNotifier nowLoading;

        public IProjectModel? CurrentProject {
            get => currentProject;
            private set => SetProperty(ref currentProject, value);
        }
        private IProjectModel? currentProject;

        public ProjectSettingModel ProjectSetting {
            get => projectSetting;
            private set => SetProperty(ref projectSetting, value);
        }
        private ProjectSettingModel projectSetting;

        public ReadOnlyCollection<ProjectCrumb> PreviousProjects { get; }
        private readonly List<ProjectCrumb> _previousProjects;

        private Task SetNewProject(IProjectModel project) {
            CurrentProject = project;
            ProjectSetting = new ProjectSettingModel(SetNewProject, _broker);
            var currentCrumb = new ProjectCrumb(project.Storage.ProjectParameter);
            if (_previousProjects.Any(currentCrumb.MaybeSame)) {
                _previousProjects.RemoveAll(currentCrumb.MaybeSame);
            }
            _previousProjects.Insert(0, currentCrumb);
            if (_previousProjects.Count > 50) {
                _previousProjects.RemoveRange(50, _previousProjects.Count - 50);
            }
            return Task.CompletedTask;
        }

        public async Task SaveAsync() {
            if (CurrentProject is null) {
                return;
            }
            using (nowSaving.ProcessStart()) {
                await CurrentProject.SaveAsync().ConfigureAwait(false);
                _settings.Save();
            }
        }

        public async Task SaveAsAsync() {
            if (CurrentProject is null) {
                return;
            }
            using (nowSaving.ProcessStart()) {
                await CurrentProject.SaveAsAsync().ConfigureAwait(false);
                _settings.Save();
            }
        }

        public async Task LoadAsync() {
            using (nowLoading.ProcessStart()) {
                try {
                    string projectPath = string.Empty;
                    var request = new OpenFileRequest(path => projectPath = path)
                    {
                        Filter = string.Join("|", new[]{
                            "MS project file(.mdproject)|*.mdproject",
                            "MS dataset file(.mddata)|*.mddata",
                        }),
                        Title = "Import a project file",
                        RestoreDirectory = true,
                    };
                    _broker.Publish(request);
                    if (!File.Exists(projectPath)) {
                        return;
                    }
                    var loadedProject = await ProjectModel.LoadAsync(projectPath, _broker).ConfigureAwait(false);
                    if (loadedProject is null) {
                        _broker.Publish(new ShortMessageRequest("Project loading has failed."));
                        return;
                    }
                    CurrentProject = loadedProject;
                    var currentCrumb = new ProjectCrumb(loadedProject.Storage.ProjectParameter);
                    if (_previousProjects.Any(currentCrumb.MaybeSame)) {
                        _previousProjects.RemoveAll(currentCrumb.MaybeSame);
                    }
                    _previousProjects.Insert(0, currentCrumb);
                    if (_previousProjects.Count > 50) {
                        _previousProjects.RemoveRange(50, _previousProjects.Count - 50);
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

        public async Task LoadProjectAsync(ProjectCrumb projectCrumb) {
            using (nowLoading.ProcessStart()) {
                try {
                    if (projectCrumb.FilePath is null || !File.Exists(projectCrumb.FilePath)) {
                        return;
                    }
                    var loadedProject = await ProjectModel.LoadAsync(projectCrumb.FilePath, _broker).ConfigureAwait(true);
                    if (loadedProject is null) {
                        _broker.Publish(new ShortMessageRequest("Project loading has failed."));
                        return;
                    }
                    CurrentProject = loadedProject;
                    var currentCrumb = new ProjectCrumb(loadedProject.Storage.ProjectParameter);
                    if (_previousProjects.Any(currentCrumb.MaybeSame)) {
                        _previousProjects.RemoveAll(currentCrumb.MaybeSame);
                    }
                    _previousProjects.Insert(0, currentCrumb);
                    if (_previousProjects.Count > 50) {
                        _previousProjects.RemoveRange(50, _previousProjects.Count - 50);
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
