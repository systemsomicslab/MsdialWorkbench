using CompMs.App.Msdial.Model.Export;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace CompMs.App.Msdial.ViewModel.Export
{
    internal sealed class AlignmentExportGroupViewModel : ViewModelBase {
        private readonly AlignmentExportGroupModel _model;
        private readonly DelegateCommand _exportCommand;

        public AlignmentExportGroupViewModel(AlignmentExportGroupModel model, DelegateCommand exportCommand) {
            _model = model ?? throw new System.ArgumentNullException(nameof(model));
            _exportCommand = exportCommand;
            _format = model.Format;
            _spectraType = model.SpectraType;
        }

        public string Label => _model.Label;

        public bool IsExpanded {
            get => _isExapnded;
            set => SetProperty(ref _isExapnded, value);
        }
        private bool _isExapnded = false;

        [Required(ErrorMessage = "Please select format.")]
        public ExportFormat Format {
            get => _format;
            set {
                if (SetProperty(ref _format, value)) {
                    if (!ContainsError(nameof(Format))) {
                        _model.Format = _format;
                    }
                    _exportCommand?.RaiseCanExecuteChanged();
                }
            }
        }
        private ExportFormat _format;
        public ReadOnlyObservableCollection<ExportFormat> Formats => _model.Formats;

        [Required(ErrorMessage = "Please select spectra type.")]
        public ExportspectraType SpectraType {
            get => _spectraType;
            set {
                if (SetProperty(ref _spectraType, value)) {
                    if (!ContainsError(nameof(SpectraType))) {
                        _model.SpectraType = _spectraType;
                    }
                    _exportCommand?.RaiseCanExecuteChanged();
                }
            }
        }
        private ExportspectraType _spectraType = ExportspectraType.deconvoluted;
        public ReadOnlyObservableCollection<ExportspectraType> SpectraTypes => _model.SpectraTypes;

        public ReadOnlyObservableCollection<ExportType> Types => _model.Types;

        public void AddExportTypes(params ExportType[] exportTypes) {
            _model.AddExportTypes(exportTypes);
        }
    }
}
