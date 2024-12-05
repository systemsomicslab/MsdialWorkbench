using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.CommonMVVM.Validator;
using CompMs.Graphics.UI;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    internal class InternalMsfinderBatchSettingViewModel : SettingDialogViewModel {
        public InternalMsfinderBatchSettingViewModel(MsfinderParameterSetting model, InternalMsfinderSettingModel batchMsfinder, IMessageBroker broker) {
            InternalMsfinderSettingViewModel = new InternalMsfinderSettingViewModel(model, broker);

            TryTopNmolecularFormulaSearch = model.ToReactivePropertyAsSynchronized(
                m => m.TryTopNmolecularFormulaSearch,
                m => m.ToString(),
                vm => int.Parse(vm)
            ).SetValidateAttribute(() => TryTopNmolecularFormulaSearch).AddTo(Disposables);

            IsFormulaFinder = model.ToReactivePropertySlimAsSynchronized(m => m.IsFormulaFinder).AddTo(Disposables);

            IsStructureFinder = model.ToReactivePropertySlimAsSynchronized(m => m.IsStructureFinder).AddTo(Disposables);

            IsCreateNewProject = model.ToReactivePropertySlimAsSynchronized(m => m.IsCreateNewProject).AddTo(Disposables);
            IsUseAutoDefinedFolderName = model.ToReactivePropertySlimAsSynchronized(m => m.IsUseAutoDefinedFolderName).AddTo(Disposables);

            char[] invalidChars = Path.GetInvalidPathChars();
            UserDefinedProjectFolderName = model.ToReactivePropertyAsSynchronized(m => m.UserDefinedProjectFolderName, ignoreValidationErrorValue: true)
                .SetValidateAttribute(() => UserDefinedProjectFolderName)
                .SetValidateNotifyError(path => path.IndexOfAny(invalidChars) >= 0 ? "Invalid character contains." : null).AddTo(Disposables);
            ExistProjectPath = model.ToReactivePropertyAsSynchronized(m => m.ExistProjectPath, ignoreValidationErrorValue: true).SetValidateAttribute(() => ExistProjectPath).AddTo(Disposables);

            var folderDoesNotExists = ExistProjectPath.ObserveHasErrors;
            var loadProjectSelected = IsCreateNewProject.Inverse();
            var loadProjectAndFolderDoesNotExists = new[]
            {
                loadProjectSelected,
                folderDoesNotExists,
            }.CombineLatestValuesAreAllTrue();
            var createNewFolder = IsCreateNewProject;
            var invalidUserDefinedProjectFolderName = UserDefinedProjectFolderName.ObserveHasErrors;
            var createNewFolderAndInvalidFolderName = new[]
            {
                createNewFolder,
                IsUseAutoDefinedFolderName.Inverse(),
                invalidUserDefinedProjectFolderName,
            }.CombineLatestValuesAreAllTrue();

            Run = new ReactiveCommand().WithSubscribe(() => {
                Mouse.OverrideCursor = Cursors.Wait;
                model.Commit();
                var msfinder = batchMsfinder.Process();
                if (msfinder is not null) {
                    broker.Publish(new InternalMsFinderViewModel(msfinder, broker));
                } else {
                    Debug.Assert(true, "Msfinder null!!");
                }
                Mouse.OverrideCursor = null;
            }).AddTo(Disposables);

            Apply = new ReactiveCommand().WithSubscribe(() => {
                model.Commit();
            }).AddTo(Disposables);

            Cancel = new ReactiveCommand().WithSubscribe(() => {
                model.Cancel();
            }).AddTo(Disposables);
        }

        public InternalMsfinderSettingViewModel InternalMsfinderSettingViewModel { get; set; }

        public ReactiveProperty<string> TryTopNmolecularFormulaSearch { get; }

        public ReactivePropertySlim<bool> IsFormulaFinder { get; set; }

        public ReactivePropertySlim<bool> IsStructureFinder { get; set; }

        private ReactiveCommand Run { get; }
        private ReactiveCommand Apply { get; }
        private ReactiveCommand Cancel { get; }

        public ReactivePropertySlim<bool> IsCreateNewProject { get; }
        public ReactivePropertySlim<bool> IsUseAutoDefinedFolderName { get; }
        [Required(ErrorMessage = "Folder name is required.")]
        public ReactiveProperty<string> UserDefinedProjectFolderName { get; }
        [PathExists(ErrorMessage = "Please use unique project folder name.", IsDirectory = true)]
        public ReactiveProperty<string> ExistProjectPath { get; }

        public override ICommand? FinishCommand => Run;
        public override ICommand? ApplyCommand => Apply;
        public override ICommand? CancelCommand => Cancel;
    }
}
