using CompMs.App.Msdial.Model.Export;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace CompMs.App.Msdial.ViewModel.Export
{
    internal sealed class AlignmentExportGroupViewModel : ViewModelBase, IAlignmentResultExportViewModel {
        private readonly AlignmentExportGroupModel _model;
        private readonly DelegateCommand _exportCommand;

        public AlignmentExportGroupViewModel(AlignmentExportGroupModel model, DelegateCommand exportCommand) {
            _model = model ?? throw new System.ArgumentNullException(nameof(model));
            _exportCommand = exportCommand;
            Format = model.ExportMethod.Format;
            SpectraType = model.SpectraType;
            IsLongFormat = model.ExportMethod.IsLongFormat;
            TrimContentToExcelLimit = model.ExportMethod.TrimToExcelLimit;
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
                        _model.ExportMethod.Format = _format;
                    }
                    _exportCommand?.RaiseCanExecuteChanged();
                }
            }
        }
        private ExportFormat _format;
        public ExportFormat[] Formats => _model.ExportMethod.Formats;

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

        public bool IsLongFormat {
            get => _isLongFormat;
            set {
                if (SetProperty(ref _isLongFormat, value)) {
                    _model.ExportMethod.IsLongFormat = _isLongFormat;
                }
            }
        }
        private bool _isLongFormat;

        public bool TrimContentToExcelLimit {
            get => _trimContentToExcelLimit;
            set {
                if (SetProperty(ref _trimContentToExcelLimit, value)) {
                    _model.ExportMethod.TrimToExcelLimit = _trimContentToExcelLimit;
                }
            }
        }
        private bool _trimContentToExcelLimit;
    }
}
