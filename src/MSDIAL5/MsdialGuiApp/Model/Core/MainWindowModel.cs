using CompMs.App.Msdial.Dto;
using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
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
            _previousProjects = new ObservableCollection<ProjectCrumb>(LoadPreviousProjects());
            PreviousProjects = new ReadOnlyObservableCollection<ProjectCrumb>(_previousProjects);
        }

        /// <summary>
        /// Reads the recent-project list out of user.config.
        ///
        /// This is the first thing the application touches on the user's disk, and it used to be
        /// unguarded. A user.config truncated by a crash or a power loss while Save was writing it
        /// therefore threw here, inside the MainWindow constructor, where WPF reports it only as
        /// "The invocation of the constructor on type ... threw an exception". The application then
        /// could not be started again at all, and reinstalling did not help, because the file is
        /// keyed on where the executable sits rather than on which version it is.
        ///
        /// A file we cannot read is moved aside and the application starts with an empty history
        /// instead. Losing the recent-project list is a much smaller loss than not starting, and
        /// keeping the moved file means it can still be examined when the user reports the problem.
        /// </summary>
        private List<ProjectCrumb> LoadPreviousProjects() {
            try {
                return ReadPreviousProjects();
            }
            catch (ConfigurationErrorsException e) {
                var movedTo = TryMoveBrokenUserConfigAside(e);
                List<ProjectCrumb> projects;
                try {
                    _settings.Reload();
                    projects = ReadPreviousProjects();
                }
                catch (ConfigurationErrorsException) {
                    projects = new List<ProjectCrumb>();
                }
                ReportUserConfigWasReset(e, movedTo);
                return projects;
            }
        }

        private List<ProjectCrumb> ReadPreviousProjects() {
            if (_settings.ShouldUpgrade) {
                _settings.Upgrade();
                _settings.ShouldUpgrade = false;
                _settings.Save();
            }
            if (_settings.PreviousProjects is null) {
                _settings.PreviousProjects = new List<ProjectCrumb>();
                _settings.Save();
            }
            return _settings.PreviousProjects;
        }

        private static string? TryMoveBrokenUserConfigAside(ConfigurationErrorsException error) {
            var path = string.IsNullOrEmpty(error.Filename) ? TryGetUserConfigPath() : error.Filename;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) {
                return null;
            }
            try {
                var movedTo = $"{path}.broken-{DateTime.Now:yyyyMMddHHmmss}";
                File.Move(path, movedTo);
                return movedTo;
            }
            catch (Exception moveFailure) {
                System.Diagnostics.Debug.Write($"Could not move {path} aside\n\n{moveFailure.Message}");
                return null;
            }
        }

        private static string? TryGetUserConfigPath() {
            try {
                return ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
            }
            catch (ConfigurationErrorsException e) {
                return e.Filename;
            }
        }

        private static void ReportUserConfigWasReset(ConfigurationErrorsException error, string? movedTo) {
            // The message broker is not wired to a window yet at this point in startup, so this is
            // said directly. It happens once, and saying nothing would silently drop the history.
            var message = new StringBuilder()
                .AppendLine("The MS-DIAL settings file could not be read, so the recent-project list was reset.")
                .AppendLine("Your projects and measurement data are not affected.")
                .AppendLine()
                .AppendLine($"Reason: {error.Message}")
                .ToString();
            if (movedTo is not null) {
                message += $"\nThe unreadable file was kept as:\n{movedTo}\n";
            }
            MessageBox.Show(message, "MS-DIAL settings were reset", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>
        /// A settings file we cannot write is not a reason to fail the project save that asked for it.
        /// </summary>
        private void TrySaveSettings() {
            try {
                _settings.Save();
            }
            catch (ConfigurationErrorsException e) {
                System.Diagnostics.Debug.Write($"Could not save user settings\n\n{e.Message}");
            }
        }

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

        public ReadOnlyObservableCollection<ProjectCrumb> PreviousProjects { get; }
        private readonly ObservableCollection<ProjectCrumb> _previousProjects;

        private async Task SetNewProject(IProjectModel project) {
            CurrentProject = project;
            ProjectSetting = new ProjectSettingModel(SetNewProject, _broker);
            var currentCrumb = new ProjectCrumb(project.Storage.ProjectParameter);
            await Application.Current.Dispatcher.InvokeAsync(() => {
                DeleteSimilarProjectHistories(currentCrumb);
                InsertTopProjectHistory(currentCrumb);
            });
        }

        public async Task SaveAsync() {
            if (CurrentProject is null) {
                return;
            }
            using (nowSaving.ProcessStart()) {
                await CurrentProject.SaveAsync().ConfigureAwait(false);
                var currentCrumb = new ProjectCrumb(CurrentProject.Storage.ProjectParameter);
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    DeleteSimilarProjectHistories(currentCrumb);
                    InsertTopProjectHistory(currentCrumb);
                });
                _settings.PreviousProjects = PreviousProjects.ToList();
                TrySaveSettings();
            }
        }

        public async Task SaveAsAsync() {
            if (CurrentProject is null) {
                return;
            }
            using (nowSaving.ProcessStart()) {
                await CurrentProject.SaveAsAsync().ConfigureAwait(false);
                TrySaveSettings();
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
                    await Application.Current.Dispatcher.InvokeAsync(() => {
                        DeleteSimilarProjectHistories(currentCrumb);
                        InsertTopProjectHistory(currentCrumb);
                    });
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
                    var loadedProject = await ProjectModel.LoadAsync(projectCrumb.FilePath, _broker).ConfigureAwait(false);
                    if (loadedProject is null) {
                        _broker.Publish(new ShortMessageRequest("Project loading has failed."));
                        return;
                    }
                    CurrentProject = loadedProject;
                    var currentCrumb = new ProjectCrumb(loadedProject.Storage.ProjectParameter);
                    await Application.Current.Dispatcher.InvokeAsync(() => {
                        DeleteSimilarProjectHistories(currentCrumb);
                        InsertTopProjectHistory(currentCrumb);
                    });
                }
                catch {
                    await Application.Current.Dispatcher.InvokeAsync(() => {
                        MessageBox.Show("Failed to load project.\nPlease check your project.");
                        return Task.CompletedTask;
                    });
                }
            }
        }

        public async Task DeleteProjectAsync(ProjectCrumb projectCrumb){
            using (nowLoading.ProcessStart()){
                try
                {
                    await Application.Current.Dispatcher.InvokeAsync(() => {
                        DeleteSimilarProjectHistories(projectCrumb);
                    });
                    _settings.PreviousProjects = PreviousProjects.ToList();
                    TrySaveSettings();
                }
                catch
                {
                    await Application.Current.Dispatcher.InvokeAsync(() => {
                        MessageBox.Show("Failed to delete project.\nPlease check your project.");
                        return Task.CompletedTask;
                    });
                }
            }
        }

        private void DeleteSimilarProjectHistories(ProjectCrumb currentProject) {
            var resembleProjects = _previousProjects.Where(currentProject.MaybeSame).ToList();
            foreach (var resembleProject in resembleProjects)
            {
                _previousProjects.Remove(resembleProject);
            }
        }

        private void InsertTopProjectHistory(ProjectCrumb currentProject) {
            _previousProjects.Insert(0, currentProject);
            if (_previousProjects.Count > 50) {
                while (_previousProjects.Count > 50) {
                    _previousProjects.RemoveAt(_previousProjects.Count - 1);
                }
            }
        }
    }
}
