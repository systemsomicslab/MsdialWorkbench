using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.ComponentModel.DataAnnotations;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public class ProjectParameterSettingViewModel : ViewModelBase
    {
        public ProjectParameterSettingViewModel(ProjectParameterSettingModel model) {
            Model = model ?? throw new System.ArgumentNullException(nameof(model));

            ProjectTitle = Model
                .ToReactivePropertyAsSynchronized(m => m.ProjectTitle)
                .SetValidateAttribute(() => ProjectTitle)
                .AddTo(Disposables);

            ProjectFolderPath = Model
                .ToReactivePropertyAsSynchronized(m => m.ProjectFolderPath)
                .SetValidateAttribute(() => ProjectFolderPath)
                .AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                ProjectTitle.ObserveHasErrors,
                ProjectFolderPath.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim();
        }

        public ProjectParameterSettingModel Model { get; }

        [Required(ErrorMessage = "Project title is required.")]
        public ReactiveProperty<string> ProjectTitle { get; }

        [Required(ErrorMessage = "Project folder path is required.")]
        public ReactiveProperty<string> ProjectFolderPath { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
    }
}
