using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.Parser;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace CompMs.App.Msdial.Model.Table
{
    internal sealed class TargetCompoundLibrarySettingModel : BindableBase
    {
        public ReadOnlyCollection<MoleculeMsReference> ReferenceMolecules {
            get => _referenceMolecules;
            private set => SetProperty(ref _referenceMolecules, value);
        }
        private ReadOnlyCollection<MoleculeMsReference> _referenceMolecules;

        public string TargetLibrary {
            get => _targetLibrary;
            set => SetProperty(ref _targetLibrary, value);
        }
        private string _targetLibrary;

        public string LoadingError {
            get => _loadingError;
            private set => SetProperty(ref _loadingError, value);
        }
        private string _loadingError;


        public void Load() {
            if (!File.Exists(TargetLibrary)) {
                LoadingError = "Library path is not entered.";
                return;
            }

            LoadingError = string.Empty;
            switch (Path.GetExtension(TargetLibrary)) {
                case "txt":
                    var textdb = TextLibraryParser.TextLibraryReader(TargetLibrary, out string error);
                    if (string.IsNullOrEmpty(error)) {
                        ReferenceMolecules = textdb.AsReadOnly();
                    }
                    else {
                        LoadingError = error;
                    }
                    return;
                case "msp":
                    var mspdb = LibraryHandler.ReadMspLibrary(TargetLibrary);
                    ReferenceMolecules = mspdb.AsReadOnly();
                    return;
                default:
                    LoadingError = "Unsupported library type.";
                    return;
            }
        }

        public List<MatchedSpotCandidate<AlignmentSpotPropertyModel>> Find(IReadOnlyList<AlignmentSpotPropertyModel> spots) {
            if (ReferenceMolecules is null) {
                return new List<MatchedSpotCandidate<AlignmentSpotPropertyModel>>(0);
            }
            var candidates = new List<MatchedSpotCandidate<AlignmentSpotPropertyModel>>();
            foreach (var reference in ReferenceMolecules) {
                foreach (var spot in spots) {
                    var candidate = spot.IsMatchedWith(reference);
                    if (candidate != null) {
                        candidates.Add(candidate);
                    }
                }
            }
            return candidates;
        }
    }
}
