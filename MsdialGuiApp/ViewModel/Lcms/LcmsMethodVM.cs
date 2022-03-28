using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Reactive.Linq;
using System.Windows;

namespace CompMs.App.Msdial.ViewModel.Lcms
{
    class LcmsMethodVM : MethodViewModel {
        public LcmsMethodVM(
            LcmsMethodModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IWindowService<PeakSpotTableViewModelBase> proteomicsTableService)
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

            if (proteomicsTableService is null) {
                throw new ArgumentNullException(nameof(proteomicsTableService));
            }

            this.model = model;

            Storage = model.Storage;

            AnalysisViewModel = model.ObserveProperty(m => m.AnalysisModel)
                .Where(m => m != null)
                .Select(m => new AnalysisLcmsVM(m, compoundSearchService, peakSpotTableService, proteomicsTableService) { DisplayFilters = displayFilters, })
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            AlignmentViewModel = model.ObserveProperty(m => m.AlignmentModel)
                .Where(m => m != null)
                .Select(m => new AlignmentLcmsVM(m, compoundSearchService, peakSpotTableService, proteomicsTableService) { DisplayFilters = displayFilters, })
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            ShowExperimentSpectrumCommand = new ReactiveCommand().AddTo(Disposables);

            AnalysisViewModel //this.ObserveProperty(m => m.AnalysisVM)
                .Where(vm => vm != null)
                .Select(vm => ShowExperimentSpectrumCommand.WithLatestFrom(vm.ExperimentSpectrumViewModel, (a, b) => b))
                .Switch()
                .Subscribe(vm => MessageBroker.Default.Publish(vm))
                .AddTo(Disposables);
        }

        private readonly LcmsMethodModel model;

        public AnalysisLcmsVM AnalysisVM => AnalysisViewModel.Value;
        // {
        //     get => analysisVM;
        //     set => SetProperty(ref analysisVM, value);
        // }
        // private AnalysisLcmsVM analysisVM;

        public ReadOnlyReactivePropertySlim<AnalysisLcmsVM> AnalysisViewModel { get; }

        public AlignmentLcmsVM AlignmentVM => AlignmentViewModel.Value;
        // {
        //     get => alignmentVM;
        //     set => SetProperty(ref alignmentVM, value);
        // }
        // private AlignmentLcmsVM alignmentVM;

        public ReadOnlyReactivePropertySlim<AlignmentLcmsVM> AlignmentViewModel { get; }

        public IMsdialDataStorage<MsdialLcmsParameter> Storage {
            get => storage;
            set => SetProperty(ref storage, value);
        }
        private IMsdialDataStorage<MsdialLcmsParameter> storage;

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

            var processOption = Storage.Parameter.ProcessOption;
            // Run Identification
            if (processOption.HasFlag(ProcessOption.Identification) || processOption.HasFlag(ProcessOption.PeakSpotting)) {
                if (!model.ProcessAnnotaion(window, Storage))
                    return -1;
            }

            // Run second process
            var param = Storage.Parameter;
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

        protected override void LoadAnalysisFileCore(AnalysisFileBeanViewModel analysisFile) {
            if (analysisFile?.File == null || analysisFile.File == model.AnalysisFile) {
                return;
            }
            model.LoadAnalysisFile(analysisFile.File);

            // if (AnalysisVM != null) {
            //     AnalysisVM.Dispose();
            //     Disposables.Remove(AnalysisVM);
            // }
            // AnalysisVM = new AnalysisLcmsVM(
            //     model.AnalysisModel,
            //     compoundSearchService,
            //     peakSpotTableService, 
            //     proteomicsTableService) {
            //     DisplayFilters = displayFilters
            // }.AddTo(Disposables);
        }

        protected override void LoadAlignmentFileCore(AlignmentFileBeanViewModel alignmentFile) {
            if (alignmentFile?.File == null || alignmentFile.File == model.AlignmentFile) {
                return;
            }
            model.LoadAlignmentFile(alignmentFile.File);

            // if (AlignmentVM != null) {
            //     AlignmentVM?.Dispose();
            //     Disposables.Remove(AlignmentVM);
            // }
            // AlignmentVM = new AlignmentLcmsVM(
            //     model.AlignmentModel,
            //     compoundSearchService,
            //     peakSpotTableService,
            //     proteomicsTableService, parameter) {
            //     DisplayFilters = displayFilters
            // }.AddTo(Disposables);
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

        public ReactiveCommand ShowExperimentSpectrumCommand { get; }

        public DelegateCommand<Window> ShowFragmentSearchSettingCommand => fragmentSearchSettingCommand ??
            (fragmentSearchSettingCommand = new DelegateCommand<Window>(FragmentSearchSettingMethod));

        private void FragmentSearchSettingMethod(Window obj) {
            if (SelectedViewModel.Value is AlignmentFileViewModel) {
                model.ShowShowFragmentSearchSettingView(obj, true);
            }
            else {
                model.ShowShowFragmentSearchSettingView(obj, false);
            }
        }

        private DelegateCommand<Window> fragmentSearchSettingCommand;

        public DelegateCommand GoToMsfinderCommand => goToMsfinderCommand ??  (goToMsfinderCommand = new DelegateCommand(GoToMsfinderMethod));
        private DelegateCommand goToMsfinderCommand;

        private void GoToMsfinderMethod() {
            if (SelectedViewModel.Value is AlignmentFileViewModel) {
                model.GoToMsfinderMethod(true);
            }
            else {
                model.GoToMsfinderMethod(false);
            }
        }
    }
}
