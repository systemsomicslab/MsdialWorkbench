using CompMs.App.Msdial.Model.Export;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Export
{
    internal sealed class AlignmentSpectraExportGroupViewModel : ViewModelBase, IAlignmentResultExportViewModel
    {
        private readonly AlignmentSpectraExportGroupModel _model;

        public AlignmentSpectraExportGroupViewModel(AlignmentSpectraExportGroupModel model) {
            _model = model;
            SpectraTypes = new ReadOnlyObservableCollection<ExportspectraType>(model.SpectraTypes);
            Formats = new ReadOnlyObservableCollection<AlignmentSpectraExportFormat>(model.Formats);
            ExportIndividually = model.ToReactivePropertySlimAsSynchronized(m => m.ExportIndividually).AddTo(Disposables);
            CanExport = this.ErrorsChangedAsObservable().Select(_ => !HasValidationErrors).ToReadOnlyReactivePropertySlim(!HasValidationErrors).AddTo(Disposables);
        }

        public bool IsExpanded {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }
        private bool _isExpanded = false;

        [Required(ErrorMessage = "Please select spectra type.")]
        public ExportspectraType SpectraType {
            get => _spectraType;
            set {
                if (SetProperty(ref _spectraType, value)) {
                    if (!ContainsError(nameof(SpectraType))) {
                        _model.SpectraType = _spectraType;
                    }
                }
            }
        }
        private ExportspectraType _spectraType = ExportspectraType.deconvoluted;

        public ReadOnlyObservableCollection<ExportspectraType> SpectraTypes { get; }

        public ReadOnlyObservableCollection<AlignmentSpectraExportFormat> Formats { get; }

        public ReactivePropertySlim<bool> ExportIndividually { get; }

        public IReadOnlyReactiveProperty<bool> CanExport { get; }
    }
}
