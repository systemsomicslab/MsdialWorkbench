using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Table;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.ViewModel.Table
{
    internal sealed class FindTargetCompoundsSpotViewModel : ViewModelBase
    {
        private readonly FindTargetCompoundsSpotModel _model;
        private readonly IMessageBroker _broker;

        public FindTargetCompoundsSpotViewModel(FindTargetCompoundsSpotModel model, IMessageBroker broker) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));
            SetLibraryCommand = new ReactiveCommand().WithSubscribe(OpenSetLibraryDialog).AddTo(Disposables);
            FindCommand = new ReactiveCommand().WithSubscribe(model.Find).AddTo(Disposables);
            Candidates = model.ObserveProperty(m => m.Candidates).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<ReadOnlyCollection<MatchedSpotCandidate<AlignmentSpotPropertyModel>>> Candidates { get; }

        public ReactiveCommand FindCommand { get; }

        public ReactiveCommand SetLibraryCommand { get; }

        private void OpenSetLibraryDialog() {
            using (var vm = new TargetCompoundLibrarySettingViewModel(_model.LibrarySettingModel, _broker)) {
                _broker.Publish(vm);
            }
        }
    }
}
