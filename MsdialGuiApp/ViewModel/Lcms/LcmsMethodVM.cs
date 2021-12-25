using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcMsApi.DataObj;
using Reactive.Bindings.Extensions;
using System;
using System.Windows;

namespace CompMs.App.Msdial.ViewModel.Lcms
{
    class LcmsMethodVM : MethodViewModel {
        public LcmsMethodVM(
            MsdialLcmsDataStorage storage,
            IWindowService<ViewModel.CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService)
            : this(
                  new LcmsMethodModel(storage, new StandardDataProviderFactory(retry: 5, isGuiProcess: true)),
                  compoundSearchService,
                  peakSpotTableService) {

        }

        public LcmsMethodVM(
            LcmsMethodModel model,
            IWindowService<ViewModel.CompoundSearchVM> compoundSearchService,
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

            Storage = model.Storage;
        }

        private readonly LcmsMethodModel model;
        private readonly IWindowService<ViewModel.CompoundSearchVM> compoundSearchService;
        private readonly IWindowService<PeakSpotTableViewModelBase> peakSpotTableService;

        public AnalysisLcmsVM AnalysisVM {
            get => analysisVM;
            set => SetProperty(ref analysisVM, value);
        }
        private AnalysisLcmsVM analysisVM;

        public AlignmentLcmsVM AlignmentVM {
            get => alignmentVM;
            set => SetProperty(ref alignmentVM, value);
        }
        private AlignmentLcmsVM alignmentVM;

        public MsdialLcmsDataStorage Storage {
            get => storage;
            set => SetProperty(ref storage, value);
        }
        private MsdialLcmsDataStorage storage;

        public bool RefMatchedChecked {
            get => ReadDisplayFilters(DisplayFilter.RefMatched);
            set => SetDisplayFilters(DisplayFilter.RefMatched, value);
        }
        public bool SuggestedChecked {
            get => ReadDisplayFilters(DisplayFilter.Suggested);
            set => SetDisplayFilters(DisplayFilter.Suggested, value);
        }
        public bool UnknownChecked {
            get => ReadDisplayFilters(DisplayFilter.Unknown);
            set => SetDisplayFilters(DisplayFilter.Unknown, value);
        }

        public bool Ms2AcquiredChecked {
            get => ReadDisplayFilters(DisplayFilter.Ms2Acquired);
            set => SetDisplayFilters(DisplayFilter.Ms2Acquired, value);
        }
        public bool MolecularIonChecked {
            get => ReadDisplayFilters(DisplayFilter.MolecularIon);
            set => SetDisplayFilters(DisplayFilter.MolecularIon, value);
        }
        public bool BlankFilterChecked {
            get => ReadDisplayFilters(DisplayFilter.Blank);
            set => SetDisplayFilters(DisplayFilter.Blank, value);
        }
        public bool UniqueIonsChecked {
            get => ReadDisplayFilters(DisplayFilter.UniqueIons);
            set => SetDisplayFilters(DisplayFilter.UniqueIons, value);
        }
        public bool ManuallyModifiedChecked {
            get => ReadDisplayFilters(DisplayFilter.ManuallyModified);
            set => SetDisplayFilters(DisplayFilter.ManuallyModified, value);
        }

        public DisplayFilter DisplayFilters {
            get => displayFilters;
            set => SetProperty(ref displayFilters, value);
        }
        private DisplayFilter displayFilters = DisplayFilter.Unset;

        private bool ReadDisplayFilters(DisplayFilter flags) {
            return displayFilters.Read(flags);
        }

        private void WriteDisplayFilters(DisplayFilter flags, bool value) {
            displayFilters.Write(flags, value);
        }

        private bool SetDisplayFilters(DisplayFilter flags, bool value) {
            if (ReadDisplayFilters(flags) != value) {
                WriteDisplayFilters(flags, value);
                OnPropertyChanged(nameof(DisplayFilters));
                return true;
            }
            return false;
        }

        public override int InitializeNewProject(Window window) {
            if (InitializeNewProjectCore(window) != 0) {
                return -1;
            }

            AnalysisFilesView.MoveCurrentToFirst();
            SelectedAnalysisFile.Value = AnalysisFilesView.CurrentItem as AnalysisFileBeanViewModel;
            LoadAnalysisFileCommand.Execute();

            return 0;
        }

        private int InitializeNewProjectCore(Window window) {
            // Set analysis param
            if (!model.ProcessSetAnalysisParameter(window))
                return -1;

            var processOption = Storage.MsdialLcmsParameter.ProcessOption;
            // Run Identification
            if (processOption.HasFlag(ProcessOption.Identification) || processOption.HasFlag(ProcessOption.PeakSpotting)) {
                if (!model.ProcessAnnotaion(window, Storage))
                    return -1;
            }

            // Run second process
            var param = Storage.MsdialLcmsParameter;
            if (param.TargetOmics == TargetOmics.Proteomics) {
                if (!model.ProcessSeccondAnnotaion4ShotgunProteomics(window, Storage))
                    return -1;
            } 

            // Run Alignment
            if (processOption.HasFlag(ProcessOption.Alignment)) {
                if (!model.ProcessAlignment(window, Storage))
                    return -1;
            }

            return 0;
        }

        public override void LoadProject() {
            AnalysisFilesView.MoveCurrentToFirst();
            SelectedAnalysisFile.Value = AnalysisFilesView.CurrentItem as AnalysisFileBeanViewModel;
            LoadAnalysisFileCommand.Execute();
        }

        public override void SaveProject() {
            AlignmentVM?.SaveProject();
        }

        protected override void LoadAnalysisFileCore(AnalysisFileBeanViewModel analysisFile) {
            if (analysisFile?.File == null || analysisFile.File == model.AnalysisFile) {
                return;
            }
            model.LoadAnalysisFile(analysisFile.File);

            if (AnalysisVM != null) {
                AnalysisVM.Dispose();
                Disposables.Remove(AnalysisVM);
            }
            AnalysisVM = new AnalysisLcmsVM(
                model.AnalysisModel,
                compoundSearchService,
                peakSpotTableService)
            {
                DisplayFilters = displayFilters
            }.AddTo(Disposables);
        }

        protected override void LoadAlignmentFileCore(AlignmentFileBeanViewModel alignmentFile) {
            if (alignmentFile?.File == null || alignmentFile.File == model.AlignmentFile) {
                return;
            }
            model.LoadAlignmentFile(alignmentFile.File);

            if (AlignmentVM != null) {
                AlignmentVM?.Dispose();
                Disposables.Remove(AlignmentVM);
            }
            AlignmentVM = new AlignmentLcmsVM(
                model.AlignmentModel,
                compoundSearchService,
                peakSpotTableService)
            {
                DisplayFilters = displayFilters
            }.AddTo(Disposables);
        }

        public DelegateCommand<Window> ExportAnalysisResultCommand => exportAnalysisResultCommand ?? (exportAnalysisResultCommand = new DelegateCommand<Window>(model.ExportAnalysis));
        private DelegateCommand<Window> exportAnalysisResultCommand;

        public DelegateCommand<Window> ExportAlignmentResultCommand => exportAlignmentResultCommand ?? (exportAlignmentResultCommand = new DelegateCommand<Window>(model.ExportAlignment));
        private DelegateCommand<Window> exportAlignmentResultCommand;

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
