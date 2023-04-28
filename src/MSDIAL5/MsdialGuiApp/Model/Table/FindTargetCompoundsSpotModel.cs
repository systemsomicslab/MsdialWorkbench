using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Table
{
    internal sealed class FindTargetCompoundsSpotModel : BindableBase
    {
        private readonly IReadOnlyList<AlignmentSpotPropertyModel> _spots;

        public FindTargetCompoundsSpotModel(IReadOnlyList<AlignmentSpotPropertyModel> spots) {
            _spots = spots ?? throw new ArgumentNullException(nameof(spots));
            LibrarySettingModel = new TargetCompoundLibrarySettingModel();
        }

        public ReadOnlyCollection<MatchedSpotCandidate<AlignmentSpotPropertyModel>> Candidates {
            get => _candidates;
            private set => SetProperty(ref _candidates, value);
        }
        private ReadOnlyCollection<MatchedSpotCandidate<AlignmentSpotPropertyModel>> _candidates;

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
                    var candidate = spot.IsMatchedWith(reference);
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
    }
}
