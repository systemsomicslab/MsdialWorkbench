using CompMs.App.Msdial.Model.Export;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Reactive;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Export
{
    internal sealed class MsdialAnalysisTableExportViewModel : ViewModelBase, IMsdialAnalysisExportViewModel
    {
        private readonly MsdialAnalysisTableExportModel _model;
        private readonly IObservable<bool> _canExport;

        public MsdialAnalysisTableExportViewModel(MsdialAnalysisTableExportModel model) {
            _model = model;

            ExportSpectraTypes = new ReadOnlyObservableCollection<ISpectraType>(model.ExportSpectraTypes);
            ExportSpectraFileFormats = new ReadOnlyObservableCollection<SpectraFormat>(model.ExportSpectraFileFormats);
            model.ObserveProperty(m => m.SelectedSpectraType).Subscribe(t => SelectedSpectraType = t).AddTo(Disposables);
            model.ObserveProperty(m => m.SelectedFileFormat).Subscribe(f => SelectedFileFormat = f).AddTo(Disposables);
            model.ObserveProperty(m => m.IsotopeExportMax).Subscribe(m => IsotopeExportMax = m).AddTo(Disposables);
            ShouldExport = model.ToReactivePropertySlimAsSynchronized(m => m.ShouldExport).AddTo(Disposables);
            TrimToExcelLimit = model.ToReactivePropertySlimAsSynchronized(m => m.TrimToExcelLimit).AddTo(Disposables);

            _canExport = new[]{
                ShouldExport,
                this.ErrorsChangedAsObservable().ToUnit().StartWith(Unit.Default).Select(_ => HasValidationErrors),
            }.CombineLatestValuesAreAllTrue().Inverse();
        }

        public ReactivePropertySlim<bool> ShouldExport { get; }
        public ReactivePropertySlim<bool> TrimToExcelLimit { get; }

        public ReadOnlyObservableCollection<ISpectraType> ExportSpectraTypes { get; }

        [Required(ErrorMessage = "Choose a spectra type.")]
        public ISpectraType? SelectedSpectraType {
            get {
                return _selectedSpectraType;
            }

            set {
                if (SetProperty(ref _selectedSpectraType, value)) {
                    if (!ContainsError(nameof(SelectedSpectraType))) {
                        _model.SelectedSpectraType = _selectedSpectraType;
                    }
                }
            }
        }
        private ISpectraType? _selectedSpectraType;

        public ReadOnlyObservableCollection<SpectraFormat> ExportSpectraFileFormats { get; }

        [Required(ErrorMessage = "Choose a spectra format.")]
        public SpectraFormat? SelectedFileFormat {
            get {
                return _selectedFileFormat;
            }

            set {
                if (SetProperty(ref _selectedFileFormat, value)) {
                    if (!ContainsError(nameof(SelectedFileFormat))) {
                        _model.SelectedFileFormat = _selectedFileFormat;
                    }
                }
            }
        }
        private SpectraFormat? _selectedFileFormat;

        public int IsotopeExportMax {
            get {
                return _isotopeExportMax;
            }
            set {
                if (SetProperty(ref _isotopeExportMax, value)) {
                    if (!ContainsError(nameof(IsotopeExportMax))) {
                        _model.IsotopeExportMax = _isotopeExportMax;
                    }
                }
            }
        }
        private int _isotopeExportMax = 2;

        IObservable<bool> IMsdialAnalysisExportViewModel.CanExport => _canExport;
        IObservable<bool> IMsdialAnalysisExportViewModel.ShouldExport => ShouldExport;
    }
}
