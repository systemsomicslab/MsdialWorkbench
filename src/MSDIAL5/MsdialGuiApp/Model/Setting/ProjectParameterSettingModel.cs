using CompMs.App.Msdial.Model.Core;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings.Notifiers;
using System;

namespace CompMs.App.Msdial.Model.Setting
{
    internal sealed class ProjectParameterSettingModel : BindableBase
    {
        public ProjectParameterSettingModel(Action<ProjectModel> next, IMessageBroker broker) {
            var dt = DateTime.Now;
            ProjectTitle = $"{dt:yyyy_MM_dd_HH_mm_ss}.mdproject";
            this.next = next;
            _broker = broker;
            IsReadOnly = false;
        }

        public ProjectParameterSettingModel(ProjectParameter parameter, IMessageBroker broker) {
            ProjectTitle = parameter.Title;
            ProjectFolderPath = parameter.FolderPath;
            this.parameter = parameter;
            _broker = broker;
            IsReadOnly = true;
        }

        private readonly Action<ProjectModel>? next;
        private readonly IMessageBroker _broker;
        private readonly ProjectParameter? parameter;

        public bool IsReadOnly { get; }

        public string ProjectTitle {
            get => projectTitle;
            set => SetProperty(ref projectTitle, value);
        }
        private string projectTitle = string.Empty;

        public string ProjectFolderPath {
            get => projectFolderPath;
            set => SetProperty(ref projectFolderPath, value);
        }
        private string projectFolderPath = string.Empty;

        public void Build() {
            var title = ProjectTitle;
            if (!title.EndsWith(".mdproject")) {
                title += ".mdproject";
            }
            var parameter = this.parameter ?? new ProjectParameter(DateTime.Now, ProjectFolderPath, title);
            var storage = new ProjectDataStorage(parameter);
            next?.Invoke(new ProjectModel(storage, _broker));
        }
    }
}