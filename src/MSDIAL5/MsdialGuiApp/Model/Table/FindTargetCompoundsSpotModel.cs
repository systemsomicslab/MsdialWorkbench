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
    internal sealed class FindTargetCompoundsSpotModel : DisposableModelBase
    {
        private readonly IReadOnlyList<AlignmentSpotPropertyModel> _spots;
        private readonly IMessageBroker _broker;
        private readonly AlignmentMatchedSpotCandidateExporter _exporter;

        public FindTargetCompoundsSpotModel(IReadOnlyList<AlignmentSpotPropertyModel> spots, IReactiveProperty<AlignmentSpotPropertyModel> selectedSpot, IMessageBroker broker) {
            _spots = spots ?? throw new ArgumentNullException(nameof(spots));
            _broker = broker;
            LibrarySettingModel = new TargetCompoundLibrarySettingModel();
            _exporter = new AlignmentMatchedSpotCandidateExporter();
            SelectedCandidate = new ReactivePropertySlim<MatchedSpotCandidate<AlignmentSpotPropertyModel>>().AddTo(Disposables);
            SelectedCandidate.Where(candidate => candidate != null).Subscribe(candidate => selectedSpot.Value = candidate.Spot).AddTo(Disposables);
        }

        public ReadOnlyCollection<MatchedSpotCandidate<AlignmentSpotPropertyModel>> Candidates {
            get => _candidates;
            private set => SetProperty(ref _candidates, value);
        }
        private ReadOnlyCollection<MatchedSpotCandidate<AlignmentSpotPropertyModel>> _candidates;

        public ReactivePropertySlim<MatchedSpotCandidate<AlignmentSpotPropertyModel>> SelectedCandidate { get; }

        public string FindMessage {
            get => _findMessage;
            private set => SetProperty(ref _findMessage, value);
        }
        private string _findMessage;

        public TargetCompoundLibrarySettingModel LibrarySettingModel { get; }

        public void Find() {
            if (!LibrarySettingModel.IsLoaded) {
                FindMessage = "Target compounds are not setted.";
                return;
            }
            FindMessage = string.Empty;
            var candidates = new List<MatchedSpotCandidate<AlignmentSpotPropertyModel>>();
            foreach (var reference in LibrarySettingModel.ReferenceMolecules) {
                foreach (var spot in _spots) {
                    var candidate = spot.IsMatchedWith(reference, .01d, 1d);
                    if (candidate != null) {
                        candidates.Add(candidate);
                    }
                }
            }
            if (candidates.Any()) {
                Candidates = candidates.AsReadOnly();
            }
            else {
                FindMessage = "No target compounds found.";
            }
        }

        public async Task ExportAsync(Stream stream) {
            var task = TaskNotification.Start("Exporting library matched spots");
            _broker.Publish(task);
            await _exporter.ExportAsync(stream, Candidates).ConfigureAwait(false);
            _broker.Publish(task.End());
        }
    }
}
