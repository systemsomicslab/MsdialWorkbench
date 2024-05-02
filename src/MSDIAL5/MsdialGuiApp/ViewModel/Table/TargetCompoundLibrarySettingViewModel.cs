using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.Model.Table;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Validator;
using CompMs.Graphics.UI;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Table
{
    internal sealed class TargetCompoundLibrarySettingViewModel : SettingDialogViewModel
    {
        private readonly IMessageBroker _broker;

        public TargetCompoundLibrarySettingViewModel(TargetCompoundLibrarySettingModel model, IMessageBroker broker) {
            TargetLibrary = model.ToReactivePropertyAsSynchronized(m => m.TargetLibrary).SetValidateAttribute(() => TargetLibrary).AddTo(Disposables);
            //TargetLibrary.Throttle(TimeSpan.FromSeconds(.1d)).Subscribe(library => ValidateProperty(nameof(TargetLibrary), library)).AddTo(Disposables);
            LoadingError = model.ObserveProperty(m => m.LoadingError).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            References = model.ObserveProperty(m => m.ReferenceMolecules).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            LoadCommand = this.ErrorsChangedAsObservable().ToUnit().StartWith(Unit.Default)
                .Select(_ => !HasValidationErrors)
                .ToReactiveCommand()
                .WithSubscribe(model.Load).AddTo(Disposables);
            OpenCommand = new ReactiveCommand().WithSubscribe(OpenFile).AddTo(Disposables);
            _broker = broker;
        }

        [PathExists(ErrorMessage = "Library file is not found.", IsFile = true)]
        public ReactiveProperty<string> TargetLibrary { get; }
        public ReadOnlyReactivePropertySlim<string?> LoadingError { get; }

        public ReadOnlyReactivePropertySlim<ReadOnlyCollection<MoleculeMsReference>> References { get; }

        public ReactiveCommand LoadCommand { get; }
        public ReactiveCommand OpenCommand { get; }

        private void OpenFile() {
            var request = new OpenFileRequest(file => TargetLibrary.Value = file);
            _broker.Publish(request);
        }

        public override ICommand ApplyCommand => NeverCommand.Instance;
        public override ICommand FinishCommand => IdentityCommand.Instance;
        public override ICommand CancelCommand => NeverCommand.Instance;
    }
}
