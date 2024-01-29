using CompMs.Common.Components;
using CompMs.Common.Parser;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Utility;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.Model.Table
{
    internal sealed class TargetCompoundLibrarySettingModel : BindableBase
    {
        public ReadOnlyCollection<MoleculeMsReference>? ReferenceMolecules {
            get => _referenceMolecules;
            private set {
                if (SetProperty(ref _referenceMolecules, value)) {
                    IsLoaded = value?.Any() ?? false;
                }
            }
        }
        private ReadOnlyCollection<MoleculeMsReference>? _referenceMolecules;

        public bool IsLoaded {
            get => _isLoaded;
            private set => SetProperty(ref _isLoaded, value);
        }
        private bool _isLoaded;

        public string TargetLibrary {
            get => _targetLibrary;
            set => SetProperty(ref _targetLibrary, value);
        }
        private string _targetLibrary = string.Empty;

        public string LoadingError {
            get => _loadingError;
            private set => SetProperty(ref _loadingError, value);
        }
        private string _loadingError = string.Empty;


        public void Load() {
            if (!File.Exists(TargetLibrary)) {
                LoadingError = "Library path is not entered.";
                return;
            }

            LoadingError = string.Empty;
            switch (Path.GetExtension(TargetLibrary)) {
                case ".txt":
                    var textdb = TextLibraryParser.TextLibraryReader(TargetLibrary, out string error);
                    if (string.IsNullOrEmpty(error)) {
                        ReferenceMolecules = textdb.AsReadOnly();
                    }
                    else {
                        LoadingError = error;
                    }
                    return;
                case ".msp":
                    var mspdb = LibraryHandler.ReadMspLibrary(TargetLibrary);
                    ReferenceMolecules = mspdb.AsReadOnly();
                    return;
                default:
                    LoadingError = "Unsupported library type.";
                    return;
            }
        }
    }
}
