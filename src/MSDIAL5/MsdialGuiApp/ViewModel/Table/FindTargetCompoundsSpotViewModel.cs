using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Table;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Graphics.UI;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Table
{
    internal sealed class FindTargetCompoundsSpotViewModel : SettingDialogViewModel
    {
        private readonly FindTargetCompoundsSpotModel _model;
        private readonly IMessageBroker _broker;

        public FindTargetCompoundsSpotViewModel(FindTargetCompoundsSpotModel model, IMessageBroker broker) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _broker = broker ?? throw new ArgumentNullException(nameof(broker));
            SetLibraryCommand = new ReactiveCommand().WithSubscribe(OpenSetLibraryDialog).AddTo(Disposables);
            FindCommand = new ReactiveCommand().WithSubscribe(model.Find).AddTo(Disposables);
            ExportCommand = new AsyncReactiveCommand().WithSubscribe(ExportAsync).AddTo(Disposables);
            Candidates = model.ObserveProperty(m => m.Candidates).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            MzTolerance = model.ToReactivePropertySlimAsSynchronized(m => m.MzTolerance).AddTo(Disposables);
            MainChromXTolerance = model.ToReactivePropertySlimAsSynchronized(m => m.MainChromXTolerance).AddTo(Disposables);
            AmplitudeThreshold = model.ToReactivePropertySlimAsSynchronized(m => m.AmplitudeThreshold).AddTo(Disposables);
            RemoveCommand = new ReactiveCommand<MatchedSpotCandidate<AlignmentSpotPropertyModel>>().WithSubscribe(model.Remove).AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<ReadOnlyObservableCollection<MatchedSpotCandidate<AlignmentSpotPropertyModel>>> Candidates { get; }
        public ReactivePropertySlim<double> MzTolerance { get; }
        public ReactivePropertySlim<double> MainChromXTolerance { get; }
        public ReactivePropertySlim<double> AmplitudeThreshold { get; }

        public ReactivePropertySlim<MatchedSpotCandidate<AlignmentSpotPropertyModel>?> SelectedCandidate => _model.SelectedCandidate;
        public ReadOnlyReactivePropertySlim<ReadOnlyCollection<MatchedSpotCandidate<AlignmentChromPeakFeatureModel>?>> SelectedCandidatePeaks => _model.SelectedCandidatePeaks;

        public ReactiveCommand FindCommand { get; }
        public AsyncReactiveCommand ExportCommand { get; }
        public ReactiveCommand<MatchedSpotCandidate<AlignmentSpotPropertyModel>> RemoveCommand { get; }

        public ReactiveCommand SetLibraryCommand { get; }

        private void OpenSetLibraryDialog() {
            using var vm = new TargetCompoundLibrarySettingViewModel(_model.LibrarySettingModel, _broker);
            _broker.Publish(vm);
        }

        private async Task ExportAsync() {
            string exportFileName = string.Empty;
            var request = new SaveFileNameRequest(file => exportFileName = file)
            {
                AddExtension = true,
                Filter = "XML format|*.xml",
            };
            _broker.Publish(request);
            if (request.Result == true) {
                using var stream = File.Open(exportFileName, FileMode.Create);
                await _model.ExportAsync(stream).ConfigureAwait(false);
            }
        }

        public override ICommand ApplyCommand => CommonMVVM.NeverCommand.Instance;
        public override ICommand FinishCommand => CommonMVVM.IdentityCommand.Instance;
        public override ICommand CancelCommand => CommonMVVM.NeverCommand.Instance;
    }
}
