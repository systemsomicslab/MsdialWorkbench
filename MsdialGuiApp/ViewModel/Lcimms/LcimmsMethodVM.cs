using CompMs.App.Msdial.Model.Lcimms;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.View.Export;
using CompMs.App.Msdial.View.Lcimms;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using CompMs.Graphics.UI.ProgressBar;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcImMsApi.Parameter;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;

namespace CompMs.App.Msdial.ViewModel.Lcimms
{
    sealed class LcimmsMethodVM : MethodViewModel
    {
        public LcimmsMethodVM(
            LcimmsMethodModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService)
            : base(model, ConvertToAnalysisViewModel(model, compoundSearchService, peakSpotTableService), ConvertToAlignmentViewModel(model, compoundSearchService, peakSpotTableService)) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }

            this.model = model;
            PropertyChanged += OnDisplayFiltersChanged;
        }

        private readonly LcimmsMethodModel model;

        public bool RefMatchedChecked {
            get => ReadDisplayFilter(DisplayFilter.RefMatched);
            set => WriteDisplayFilter(DisplayFilter.RefMatched, value);
        }
        public bool SuggestedChecked {
            get => ReadDisplayFilter(DisplayFilter.Suggested);
            set => WriteDisplayFilter(DisplayFilter.Suggested, value);
        }
        public bool UnknownChecked {
            get => ReadDisplayFilter(DisplayFilter.Unknown);
            set => WriteDisplayFilter(DisplayFilter.Unknown, value);
        }
        public bool CcsChecked {
            get => ReadDisplayFilter(DisplayFilter.CcsMatched);
            set => WriteDisplayFilter(DisplayFilter.CcsMatched, value);
        }
        public bool Ms2AcquiredChecked {
            get => ReadDisplayFilter(DisplayFilter.Ms2Acquired);
            set => WriteDisplayFilter(DisplayFilter.Ms2Acquired, value);
        }
        public bool MolecularIonChecked {
            get => ReadDisplayFilter(DisplayFilter.MolecularIon);
            set => WriteDisplayFilter(DisplayFilter.MolecularIon, value);
        }
        public bool BlankFilterChecked {
            get => ReadDisplayFilter(DisplayFilter.Blank);
            set => WriteDisplayFilter(DisplayFilter.Blank, value);
        }
        public bool UniqueIonsChecked {
            get => ReadDisplayFilter(DisplayFilter.UniqueIons);
            set => WriteDisplayFilter(DisplayFilter.UniqueIons, value);
        }
        public bool ManuallyModifiedChecked {
            get => ReadDisplayFilter(DisplayFilter.ManuallyModified);
            set => WriteDisplayFilter(DisplayFilter.ManuallyModified, value);
        }
        private DisplayFilter displayFilters = DisplayFilter.Unset;

        private bool ReadDisplayFilter(DisplayFilter flag) {
            return displayFilters.Read(flag);
        }

        private void WriteDisplayFilter(DisplayFilter flag, bool set) {
            displayFilters.Write(flag, set);
            OnPropertyChanged(nameof(displayFilters));
        }

        void OnDisplayFiltersChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(displayFilters)) {
                if (AnalysisViewModel.Value != null)
                    AnalysisViewModel.Value.DisplayFilters = displayFilters;
                // if (AlignmentViewModel.Value != null)
                //     AlignmentViewModel.Value.DisplayFilters = displayFilters;
            }
        }

        private bool ProcessSetAnalysisParameter(Window owner) {
            var parameter = model.Storage.Parameter;
            var analysisParamSetModel = new LcimmsAnalysisParameterSetModel(parameter, model.Storage.AnalysisFiles, model.Storage.DataBases);
            using (var analysisParamSetVM = new LcimmsAnalysisParameterSetViewModel(analysisParamSetModel)) {
                var apsw = new AnalysisParamSetForLcimmsWindow
                {
                    DataContext = analysisParamSetVM,
                    Owner = owner,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };

                if (apsw.ShowDialog() != true)
                    return false;
            }

            model.SetAnalysisParameter(analysisParamSetModel);
            return true;
        }

        private bool ProcessAnnotaion(Window owner, IMsdialDataStorage<MsdialLcImMsParameter> storage) {
            var vm = new ProgressBarMultiContainerVM
            {
                MaxValue = storage.AnalysisFiles.Count,
                CurrentValue = 0,
                ProgressBarVMs = new ObservableCollection<ProgressBarVM>(
                        storage.AnalysisFiles.Select(file => new ProgressBarVM { Label = file.AnalysisFileName })
                    ),
            };
            var pbmcw = new ProgressBarMultiContainerWindow
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            pbmcw.Loaded += async (s, e) => {
                foreach ((var analysisfile, var pbvm) in storage.AnalysisFiles.Zip(vm.ProgressBarVMs)) {
                    // use annotationProcess
                    await model.RunAnnotationProcess(analysisfile, v => pbvm.CurrentValue = v);
                    vm.CurrentValue++;
                }
                pbmcw.Close();
            };
            pbmcw.ShowDialog();

            return true;
        }

        private bool ProcessAlignment(Window owner) {
            var vm = new ProgressBarVM
            {
                IsIndeterminate = true,
                Label = "Process alignment..",
            };
            var pbw = new ProgressBarWindow
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            pbw.Show();

            model.RunAlignmentProcess();
            pbw.Close();

            return true;
        }

        protected override void LoadAnalysisFileCore(AnalysisFileBeanViewModel analysisFile) {
            if (analysisFile?.File is null || analysisFile.File == model.AnalysisFile) {
                return;
            }
            model.LoadAnalysisFile(analysisFile.File);
        }

        protected override void LoadAlignmentFileCore(AlignmentFileBeanViewModel alignmentFile) {
            if (alignmentFile?.File is null || alignmentFile.File == model.AlignmentFile) {
                return;
            }
            model.LoadAlignmentFile(alignmentFile.File);
        }

        public DelegateCommand<Window> ExportAlignmentResultCommand => exportAlignmentResultCommand ?? (exportAlignmentResultCommand = new DelegateCommand<Window>(ExportAlignment));
        private DelegateCommand<Window> exportAlignmentResultCommand;

        private void ExportAlignment(Window owner) {
            var dialog = new AlignmentResultExportWin
            {
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            dialog.ShowDialog();
        }

        public DelegateCommand<Window> ShowTicCommand => showTicCommand ?? (showTicCommand = new DelegateCommand<Window>(model.ShowTIC));
        private DelegateCommand<Window> showTicCommand;

        public DelegateCommand<Window> ShowBpcCommand => showBpcCommand ?? (showBpcCommand = new DelegateCommand<Window>(model.ShowBPC));
        private DelegateCommand<Window> showBpcCommand;

        public DelegateCommand<Window> ShowTicBpcRepEICCommand => showTicBpcRepEIC ?? (showTicBpcRepEIC = new DelegateCommand<Window>(model.ShowTicBpcRepEIC));
        private DelegateCommand<Window> showTicBpcRepEIC;

        public DelegateCommand<Window> ShowEicCommand => showEicCommand ?? (showEicCommand = new DelegateCommand<Window>(model.ShowEIC));
        private DelegateCommand<Window> showEicCommand;

        private static IObservable<AnalysisLcimmsVM> ConvertToAnalysisViewModel(
            LcimmsMethodModel method,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService) {
            if (compoundSearchService is null) {
                throw new ArgumentNullException(nameof(compoundSearchService));
            }
            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }
            return method.ObserveProperty(m => m.AnalysisModel)
                .Where(m => m != null)
                .Select(m => new AnalysisLcimmsVM(m, compoundSearchService, peakSpotTableService))
                .DisposePreviousValue();

        }

        private static IObservable<AlignmentLcimmsVM> ConvertToAlignmentViewModel(
            LcimmsMethodModel method,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService) {
            if (compoundSearchService is null) {
                throw new ArgumentNullException(nameof(compoundSearchService));
            }
            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }
            return method.ObserveProperty(m => m.AlignmentModel)
                .Where(m => m != null)
                .Select(m => new AlignmentLcimmsVM(m, compoundSearchService, peakSpotTableService))
                .DisposePreviousValue();
        }
    }
}
