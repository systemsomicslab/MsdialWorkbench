using CompMs.App.Msdial.Model.Export;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace CompMs.App.Msdial.ViewModel.Export
{
    internal sealed class AlignmentSpectraExportGroupViewModel : ViewModelBase, IAlignmentResultExportViewModel
    {
        private readonly AlignmentSpectraExportGroupModel _model;
        private readonly DelegateCommand _exportCommand;

        public AlignmentSpectraExportGroupViewModel(AlignmentSpectraExportGroupModel model, DelegateCommand exportCommand) {
            IsSelected = model.ToReactivePropertySlimAsSynchronized(m => m.IsSelected).AddTo(Disposables);
            _model = model;
            _exportCommand = exportCommand;
            SpectraTypes = new ReadOnlyObservableCollection<ExportspectraType>(model.SpectraTypes);
        }

        public bool IsExpanded {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }
        private bool _isExpanded = false;

        public ReactivePropertySlim<bool> IsSelected { get; }

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

        public ReadOnlyObservableCollection<ExportspectraType> SpectraTypes { get; }
    }
}
