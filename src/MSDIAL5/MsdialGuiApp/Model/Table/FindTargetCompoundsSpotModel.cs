using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Export;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Table
{
    internal sealed class FindTargetCompoundsSpotModel : DisposableModelBase {
        private readonly IReadOnlyList<AlignmentSpotPropertyModel> _spots;
        private readonly IMessageBroker _broker;
        private readonly AlignmentMatchedSpotCandidateExporter _exporter;
        private ObservableCollection<MatchedSpotCandidate<AlignmentSpotPropertyModel>>? _editableCandidates;
        private MatchedSpotCandidateCalculator? _currentCalculator = null;

        public FindTargetCompoundsSpotModel(IReadOnlyList<AlignmentSpotPropertyModel> spots, IReactiveProperty<AlignmentSpotPropertyModel?> selectedSpot, IMessageBroker broker) {
            _spots = spots ?? throw new ArgumentNullException(nameof(spots));
            _broker = broker;
            LibrarySettingModel = new TargetCompoundLibrarySettingModel();
            _exporter = new AlignmentMatchedSpotCandidateExporter();
            SelectedCandidate = new ReactivePropertySlim<MatchedSpotCandidate<AlignmentSpotPropertyModel>?>().AddTo(Disposables);
            SelectedCandidate.Where(candidate => candidate != null).Subscribe(candidate => selectedSpot.Value = candidate!.Spot).AddTo(Disposables);
            var empty = new ReadOnlyCollection<MatchedSpotCandidate<AlignmentChromPeakFeatureModel>?>(Array.Empty<MatchedSpotCandidate<AlignmentChromPeakFeatureModel>>());
            SelectedCandidatePeaks = SelectedCandidate.Select(
                candidate => {
                    if (candidate is null) {
                        return Observable.Return(empty);
                    }
                    return candidate.Spot.AlignedPeakPropertiesModelProperty.Select(peaks => peaks.Select(peak => _currentCalculator?.Match(peak, candidate.Reference)).ToList().AsReadOnly());
                }).Switch()
                .ToReadOnlyReactivePropertySlim(empty).AddTo(Disposables);
        }

        public ReadOnlyObservableCollection<MatchedSpotCandidate<AlignmentSpotPropertyModel>>? Candidates {
            get => _candidates;
            private set => SetProperty(ref _candidates, value);
        }
        private ReadOnlyObservableCollection<MatchedSpotCandidate<AlignmentSpotPropertyModel>>? _candidates;

        public ReactivePropertySlim<MatchedSpotCandidate<AlignmentSpotPropertyModel>?> SelectedCandidate { get; }

        public ReadOnlyReactivePropertySlim<ReadOnlyCollection<MatchedSpotCandidate<AlignmentChromPeakFeatureModel>?>> SelectedCandidatePeaks { get; }

        public string FindMessage {
            get => _findMessage;
            private set => SetProperty(ref _findMessage, value);
        }
        private string _findMessage = string.Empty;

        public TargetCompoundLibrarySettingModel LibrarySettingModel { get; }

        public double MzTolerance {
            get => _mzTolerance;
            set => SetProperty(ref _mzTolerance, value);
        }
        private double _mzTolerance = .01d;

        public double MainChromXTolerance {
            get => _mainChromXTolerance;
            set => SetProperty(ref _mainChromXTolerance, value);
        }
        private double _mainChromXTolerance = 1d;

        public double AmplitudeThreshold {
            get => _amplitudeThreashold;
            set => SetProperty(ref _amplitudeThreashold, value);
        }
        private double _amplitudeThreashold = 0d;

        public void Find() {
            if (!LibrarySettingModel.IsLoaded) {
                FindMessage = "Target compounds are not setted.";
                return;
            }
            FindMessage = string.Empty;
            var candidates = new ObservableCollection<MatchedSpotCandidate<AlignmentSpotPropertyModel>>();
            var calculator = new MatchedSpotCandidateCalculator(MzTolerance, MainChromXTolerance, AmplitudeThreshold);
            foreach (var reference in LibrarySettingModel.ReferenceMolecules!) { // LibrarySettingModel.IsLoaded is True
                foreach (var spot in _spots) {
                    var candidate = calculator.Match(spot, reference);
                    if (candidate != null) {
                        candidates.Add(candidate);
                    }
                }
            }
            if (candidates.Any()) {
                _editableCandidates = candidates;
                _currentCalculator = calculator;
                Candidates = new ReadOnlyObservableCollection<MatchedSpotCandidate<AlignmentSpotPropertyModel>>(candidates);
            }
            else {
                FindMessage = "No target compounds found.";
            }
        }

        public void Remove(MatchedSpotCandidate<AlignmentSpotPropertyModel> spot) {
            if (_editableCandidates is not null && _editableCandidates.Contains(spot)) {
                _editableCandidates.Remove(spot);
            }
        }

        public async Task ExportAsync(Stream stream) {
            if (_currentCalculator is null || Candidates is null) {
                return;
            }
            var task = TaskNotification.Start("Exporting library matched spots");
            _broker.Publish(task);
            await _exporter.ExportAsync(stream, Candidates, _currentCalculator).ConfigureAwait(false);
            _broker.Publish(task.End());
        }
    }
}
