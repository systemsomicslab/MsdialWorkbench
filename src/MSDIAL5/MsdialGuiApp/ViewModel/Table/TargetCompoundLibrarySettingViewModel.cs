using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.Model.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Validator;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Table
{
    internal sealed class TargetCompoundLibrarySettingViewModel : ViewModelBase
    {
        private readonly IMessageBroker _broker;

        public TargetCompoundLibrarySettingViewModel(TargetCompoundLibrarySettingModel model, IMessageBroker broker) {
            TargetLibrary = model.ToReactivePropertySlimAsSynchronized(m => m.TargetLibrary).AddTo(Disposables);
            TargetLibrary.Throttle(TimeSpan.FromSeconds(.1d)).Subscribe(library => ValidateProperty(nameof(TargetLibrary), library)).AddTo(Disposables);
            LoadingError = model.ObserveProperty(m => m.LoadingError).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            LoadCommand = this.ErrorsChangedAsObservable().ToUnit().StartWith(Unit.Default)
                .Select(_ => !HasValidationErrors)
                .ToReactiveCommand()
                .WithSubscribe(model.Load).AddTo(Disposables);
            OpenCommand = new ReactiveCommand().WithSubscribe(OpenFile).AddTo(Disposables);
            _broker = broker;
        }

        [PathExists(ErrorMessage = "Library file is not found.", IsFile = true)]
        public ReactivePropertySlim<string> TargetLibrary { get; }
        public ReadOnlyReactivePropertySlim<string> LoadingError { get; }
        public ReactiveCommand LoadCommand { get; }
        public ReactiveCommand OpenCommand { get; }

        private void OpenFile() {
            var request = new OpenFileRequest(file => TargetLibrary.Value = file);
            _broker.Publish(request);
        }
    }
}
