using CompMs.App.Msdial.Model.Lcimms;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.View.Export;
using CompMs.App.Msdial.View.Lcimms;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using CompMs.Graphics.UI.ProgressBar;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcImMsApi.DataObj;
using CompMs.MsdialLcImMsApi.Parameter;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace CompMs.App.Msdial.ViewModel.Lcimms
{
    sealed class LcimmsMethodVM : MethodViewModel
    {
        public LcimmsMethodVM(
            MsdialLcImMsDataStorage storage,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService)
            : this(
                  new LcimmsMethodModel(storage),
                  compoundSearchService,
                  peakSpotTableService) {
            
        }

        public LcimmsMethodVM(
            LcimmsMethodModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService)
            : base(model) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }

            if (compoundSearchService is null) {
                throw new ArgumentNullException(nameof(compoundSearchService));
            }

            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }

            this.model = model;
            this.compoundSearchService = compoundSearchService;
            this.peakSpotTableService = peakSpotTableService;

            PropertyChanged += OnDisplayFiltersChanged;
        }

        private readonly LcimmsMethodModel model;
        private readonly IWindowService<CompoundSearchVM> compoundSearchService;
        private readonly IWindowService<PeakSpotTableViewModelBase> peakSpotTableService;

        public AnalysisLcimmsVM AnalysisVM {
            get => analysisVM;
            set => SetProperty(ref analysisVM, value);
        }
        private AnalysisLcimmsVM analysisVM;

        public AlignmentLcimmsVM AlignmentVM {
            get => alignmentVM;
            set => SetProperty(ref alignmentVM, value);
        }
        private AlignmentLcimmsVM alignmentVM;

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
                if (AnalysisVM != null)
                    AnalysisVM.DisplayFilters = displayFilters;
                if (AlignmentVM != null)
                    AlignmentVM.DisplayFilters = displayFilters;
            }
        }

        public override int InitializeNewProject(Window window) {
            // Set analysis param
            if (!ProcessSetAnalysisParameter(window))
                return -1;

            var processOption = model.Storage.Parameter.ProcessOption;
            // Run Identification
            if (processOption.HasFlag(ProcessOption.Identification) || processOption.HasFlag(CompMs.Common.Enum.ProcessOption.PeakSpotting)) {
                if (!ProcessAnnotaion(window, model.Storage))
                    return -1;
            }

            // Run Alignment
            if (processOption.HasFlag(ProcessOption.Alignment)) {
                if (!ProcessAlignment(window))
                    return -1;
            }

            LoadAnalysisFileCommand.Execute();
            return 0;
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

        public override void LoadProject() {
            AnalysisFilesView.MoveCurrentToFirst();
            SelectedAnalysisFile.Value = AnalysisFilesView.CurrentItem as AnalysisFileBeanViewModel;
            LoadAnalysisFileCommand.Execute();
        }

        protected override void LoadAnalysisFileCore(AnalysisFileBeanViewModel analysisFile) {
            if (analysisFile?.File is null || analysisFile.File == model.AnalysisFile) {
                return;
            }
            model.LoadAnalysisFile(analysisFile.File);

            if (!(AnalysisVM is null)) {
                AnalysisVM.Dispose();
                Disposables.Remove(AnalysisVM);
            }
            AnalysisVM = new AnalysisLcimmsVM(model.AnalysisModel, compoundSearchService, peakSpotTableService)
            {
                DisplayFilters = displayFilters
            }.AddTo(Disposables);
        }

        protected override void LoadAlignmentFileCore(AlignmentFileBeanViewModel alignmentFile) {
            if (alignmentFile?.File is null || alignmentFile.File == model.AlignmentFile) {
                return;
            }
            model.LoadAlignmentFile(alignmentFile.File);

            if (!(alignmentVM is null)) {
                AlignmentVM.Dispose();
                Disposables.Remove(AlignmentVM);
            }
            AlignmentVM = new AlignmentLcimmsVM(model.AlignmentModel, compoundSearchService, peakSpotTableService)
            {
                DisplayFilters = displayFilters,
            }.AddTo(Disposables);
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
    }
}
