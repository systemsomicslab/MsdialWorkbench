using CompMs.App.Msdial.Dims;
using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.View.Export;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.App.Msdial.ViewModel.Export;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using CompMs.Graphics.UI.ProgressBar;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialDimsCore.Export;
using CompMs.MsdialDimsCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.ViewModel.Dims
{
    class DimsMethodVM : MethodViewModel {
        public DimsMethodVM(
            DimsMethodModel model,
            IMessageBroker broker,
            IReadOnlyReactiveProperty<AnalysisDimsVM> analysisVM,
            IReadOnlyReactiveProperty<AlignmentDimsVM> alignmentVM,
            ViewModelSwitcher chromatogramViewModels,
            ViewModelSwitcher massSpectrumViewModels)
            : base(model, analysisVM, alignmentVM, chromatogramViewModels, massSpectrumViewModels) {

            Model = model;
            _broker = broker;
            PropertyChanged += OnDisplayFiltersChanged;
        }

        internal DimsMethodModel Model { get; }

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
        private DisplayFilter displayFilters = 0;

        void OnDisplayFiltersChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(displayFilters)) {
                if (AnalysisViewModel.Value != null)
                    AnalysisViewModel.Value.DisplayFilters = displayFilters;
            }
        }

        private bool ProcessSetAnalysisParameter(Window owner) {
            var parameter = Model.Storage.Parameter;
            var analysisModel = new DimsAnalysisParameterSetModel(parameter, Model.AnalysisFiles);
            using (var analysisParamSetVM = new DimsAnalysisParameterSetViewModel(analysisModel)) {

                // var analysisParamSetVM = new AnalysisParamSetVM<MsdialDimsParameter>((MsdialDimsParameter)Model.Storage.ParameterBase, Model.AnalysisFiles);
                var apsw = new AnalysisParamSetForDimsWindow
                {
                    DataContext = analysisParamSetVM,
                    Owner = owner,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                if (apsw.ShowDialog() != true)
                    return false;
            }
            parameter.ProviderFactoryParameter = analysisModel.DataCollectionSettingModel.CreateDataProviderFactoryParameter();
            Model.AnalysisParamSetProcess(analysisModel);
            
            return true;
        }

        private bool ProcessAnnotaion(Window owner, IMsdialDataStorage<MsdialDimsParameter> storage) {
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
                foreach (((var analysisfile, var pbvm), var idx) in storage.AnalysisFiles.Zip(vm.ProgressBarVMs).WithIndex()) {
                    await Model.RunAnnotationProcessAsync(analysisfile, v => pbvm.CurrentValue = v);
                    vm.CurrentValue++;
                }
                pbmcw.Close();
            };

            pbmcw.ShowDialog();

            return true;
        }

        private bool ProcessAlignment(Window owner, IMsdialDataStorage<MsdialDimsParameter> storage) {
            var vm = new ProgressBarVM
            {
                IsIndeterminate = true,
                Label = "Alignment process..",
            };
            var pbw = new ProgressBarWindow
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            pbw.Loaded += async (s, e) => {
                await Task.Run(() => Model.RunAlignmentProcess());
                pbw.Close();
            };
            pbw.ShowDialog();
            return true;
        }

        protected override void LoadAnalysisFileCore(AnalysisFileBeanViewModel analysisFile) {
            if (analysisFile?.File == null || Model.AnalysisFile == analysisFile.File) {
                return;
            }
            Model.LoadAnalysisFile(analysisFile.File);
        }

        protected override void LoadAlignmentFileCore(AlignmentFileBeanViewModel alignmentFile) {
            if (alignmentFile?.File == null || Model.AlignmentFile == alignmentFile.File) {
                return;
            }
            Model.LoadAlignmentFile(alignmentFile.File);
        }

        public DelegateCommand<Window> ExportAnalysisResultCommand => exportAnalysisResultCommand ?? (exportAnalysisResultCommand = new DelegateCommand<Window>(ExportAnalysis));
        private DelegateCommand<Window> exportAnalysisResultCommand;

        private void ExportAnalysis(Window owner) {
            var container = Model.Storage;
            var spectraTypes = new List<Model.Export.SpectraType>
            {
                new Model.Export.SpectraType(
                    ExportspectraType.deconvoluted,
                    new DimsAnalysisMetadataAccessor(container.DataBaseMapper, container.Parameter, ExportspectraType.deconvoluted)),
                new Model.Export.SpectraType(
                    ExportspectraType.centroid,
                    new DimsAnalysisMetadataAccessor(container.DataBaseMapper, container.Parameter, ExportspectraType.centroid)),
                new Model.Export.SpectraType(
                    ExportspectraType.profile,
                    new DimsAnalysisMetadataAccessor(container.DataBaseMapper, container.Parameter, ExportspectraType.profile)),
            };
            var spectraFormats = new List<Model.Export.SpectraFormat>
            {
                new Model.Export.SpectraFormat(ExportSpectraFileFormat.txt, new AnalysisCSVExporter()),
            };

            using (var vm = new AnalysisResultExportViewModel(container.AnalysisFiles, spectraTypes, spectraFormats, Model.ProviderFactory)) {

                var dialog = new AnalysisResultExportWin
                {
                    DataContext = vm,
                    Owner = owner,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };

                dialog.ShowDialog();
            }
        }

        public DelegateCommand<Window> ExportAlignmentResultCommand => exportAlignmentResultCommand ?? (exportAlignmentResultCommand = new DelegateCommand<Window>(ExportAlignment));
        private DelegateCommand<Window> exportAlignmentResultCommand;
        private readonly IMessageBroker _broker;

        private void ExportAlignment(Window owner) {
            var container = Model.Storage;
            var metadataAccessor = new DimsMetadataAccessor(container.DataBaseMapper, container.Parameter);
            var vm = new AlignmentResultExport2VM(Model.AlignmentFile, Model.AlignmentFiles, container, _broker);
            vm.ExportTypes.AddRange(
                new List<ExportType2>
                {
                    new ExportType2("Raw data (Height)", metadataAccessor, new LegacyQuantValueAccessor("Height", container.Parameter), "Height", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }, true),
                    new ExportType2("Raw data (Area)", metadataAccessor, new LegacyQuantValueAccessor("Area", container.Parameter), "Area", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }),
                    new ExportType2("Normalized data (Height)", metadataAccessor, new LegacyQuantValueAccessor("Normalized height", container.Parameter), "NormalizedHeight", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }),
                    new ExportType2("Normalized data (Area)", metadataAccessor, new LegacyQuantValueAccessor("Normalized area", container.Parameter), "NormalizedArea", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }),
                    new ExportType2("Alignment ID", metadataAccessor, new LegacyQuantValueAccessor("ID", container.Parameter), "PeakID"),
                    new ExportType2("m/z", metadataAccessor, new LegacyQuantValueAccessor("MZ", container.Parameter), "Mz"),
                    new ExportType2("S/N", metadataAccessor, new LegacyQuantValueAccessor("SN", container.Parameter), "SN"),
                    new ExportType2("MS/MS included", metadataAccessor, new LegacyQuantValueAccessor("MSMS", container.Parameter), "MsmsIncluded"),
                });
            var dialog = new AlignmentResultExportWin
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            dialog.ShowDialog();
        }

        private bool ReadDisplayFilter(DisplayFilter flag) {
            return (displayFilters & flag) != 0;
        }

        private void WriteDisplayFilter(DisplayFilter flag, bool set) {
            if (set) {
                displayFilters |= flag;
            }
            else {
                displayFilters &= (~flag);
            }
            OnPropertyChanged(nameof(displayFilters));
        }

        private static IReadOnlyReactiveProperty<AnalysisDimsVM> ConvertToAnalysisViewModel(
            DimsMethodModel method,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService) {
            if (compoundSearchService is null) {
                throw new ArgumentNullException(nameof(compoundSearchService));
            }
            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }
            ReadOnlyReactivePropertySlim<AnalysisDimsVM> result;
            using (var subject = new Subject<DimsAnalysisModel>()) {
                result = subject.Concat(method.ObserveProperty(m => m.AnalysisModel, isPushCurrentValueAtFirst: false)) // If 'isPushCurrentValueAtFirst' = true or using 'StartWith', first value can't release.
                    .Select(m => m is null ? null : new AnalysisDimsVM(m, compoundSearchService, peakSpotTableService))
                    .DisposePreviousValue()
                    .ToReadOnlyReactivePropertySlim();
                subject.OnNext(method.AnalysisModel);
                subject.OnCompleted();
            }
            return result;
        }

        private static IReadOnlyReactiveProperty<AlignmentDimsVM> ConvertToAlignmentViewModel(
            DimsMethodModel method,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IMessageBroker broker) {
            if (compoundSearchService is null) {
                throw new ArgumentNullException(nameof(compoundSearchService));
            }
            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }
            return method.ObserveProperty(m => m.AlignmentModel)
                .Where(m => m != null)
                .Select(m => new AlignmentDimsVM(m, compoundSearchService, peakSpotTableService, broker))
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim();
        }

        public static DimsMethodVM Create(
            DimsMethodModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IMessageBroker broker) {
            var analysisVM = ConvertToAnalysisViewModel(model, compoundSearchService, peakSpotTableService);
            var alignmentVM = ConvertToAlignmentViewModel(model, compoundSearchService, peakSpotTableService, broker);
            var chromvms = PrepareChromatogramViewModels(analysisVM, alignmentVM);
            var msvms = PrepareMassSpectrumViewModels(analysisVM, alignmentVM);
            return new DimsMethodVM(model, broker, analysisVM, alignmentVM, chromvms, msvms);
        }

        private static ViewModelSwitcher PrepareChromatogramViewModels(IObservable<AnalysisDimsVM> analysisAsObservable, IObservable<AlignmentDimsVM> alignmentAsObservable) {
            var eic = analysisAsObservable.Select(vm => vm?.EicViewModel);
            var bar = alignmentAsObservable.Select(vm => vm?.BarChartViewModel);
            var alignmentEic = alignmentAsObservable.Select(vm => vm?.AlignmentEicViewModel);
            return new ViewModelSwitcher(eic, bar, new IObservable<ViewModelBase>[] { eic, bar, alignmentEic});
        }

        private static ViewModelSwitcher PrepareMassSpectrumViewModels(IObservable<AnalysisDimsVM> analysisAsObservable, IObservable<AlignmentDimsVM> alignmentAsObservable) {
            var rawdec = analysisAsObservable.Select(vm => vm?.RawDecSpectrumsViewModel);
            var rawpur = Observable.Return<ViewModelBase>(null); // analysisAsObservable.Select(vm => vm?.RawPurifiedSpectrumsViewModel);
            var ms2chrom = Observable.Return<ViewModelBase>(null); // ms2 chrom
            var repref = alignmentAsObservable.Select(vm => vm?.Ms2SpectrumViewModel);
            return new ViewModelSwitcher(rawdec, repref, new IObservable<ViewModelBase>[] { rawdec, ms2chrom, rawpur, repref});
        }
    }
}
