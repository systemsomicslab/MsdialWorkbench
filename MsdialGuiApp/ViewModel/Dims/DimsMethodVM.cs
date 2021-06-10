using CompMs.App.Msdial.Dims;
using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.View.Export;
using CompMs.App.Msdial.View.Normalize;
using CompMs.App.Msdial.ViewModel.Export;
using CompMs.App.Msdial.ViewModel.Normalize;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.Graphics.UI.ProgressBar;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialDimsCore.Export;
using CompMs.MsdialDimsCore.Parameter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Dims
{
    class DimsMethodVM : MethodVM {
        static DimsMethodVM() {
            serializer = new MsdialDimsCore.Parser.MsdialDimsSerializer();
        }

        public DimsMethodVM(MsdialDataStorage storage, List<AnalysisFileBean> analysisFiles, List<AlignmentFileBean> alignmentFiles)
            : base(serializer) {

            Model = new DimsMethodModel(storage, analysisFiles, alignmentFiles, new StandardDataProviderFactory(retry: 5, isGuiProcess: true));
            Disposables.Add(Model);

            AnalysisFilesView = CollectionViewSource.GetDefaultView(Model.AnalysisFiles);
            AnalysisFilesView.MoveCurrentToFirst();

            AlignmentFilesView = CollectionViewSource.GetDefaultView(Model.AlignmentFiles);
            AlignmentFilesView.MoveCurrentToFirst();

            PropertyChanged += OnDisplayFiltersChanged;
        }

        internal DimsMethodModel Model { get; }

        public AnalysisDimsVM AnalysisVM {
            get => analysisVM;
            set => SetProperty(ref analysisVM, value);
        }
        private AnalysisDimsVM analysisVM;

        public AlignmentDimsVM AlignmentVM {
            get => alignmentVM;
            set => SetProperty(ref alignmentVM, value);
        }
        private AlignmentDimsVM alignmentVM;

        public ICollectionView AnalysisFilesView { get; }
        public ICollectionView AlignmentFilesView { get; }

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
                if (AnalysisVM != null)
                    AnalysisVM.DisplayFilters = displayFilters;
                if (AlignmentVM != null)
                    AlignmentVM.DisplayFilters = displayFilters;
            }
        }

        private static readonly MsdialSerializer serializer;

        public override int InitializeNewProject(Window window) {
            // Set analysis param
            if (!ProcessSetAnalysisParameter(window))
                return -1;

            var processOption = Model.Storage.ParameterBase.ProcessOption;
            // Run Identification
            if (processOption.HasFlag(ProcessOption.Identification) || processOption.HasFlag(ProcessOption.PeakSpotting)) {
                if (!ProcessAnnotaion(window, Model.Storage))
                    return -1;
            }

            // Run Alignment
            if (processOption.HasFlag(ProcessOption.Alignment)) {
                if (!ProcessAlignment(window, Model.Storage))
                    return -1;
            }

            LoadAnalysisFile(Model.Storage.AnalysisFiles.FirstOrDefault());
            return 0;
        }

        private bool ProcessSetAnalysisParameter(Window owner) {
            var analysisParamSetVM = new AnalysisParamSetVM<MsdialDimsParameter>((MsdialDimsParameter)Model.Storage.ParameterBase, Model.AnalysisFiles);
            var apsw = new AnalysisParamSetForDimsWindow
            {
                DataContext = analysisParamSetVM,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            if (apsw.ShowDialog() != true)
                return false;

            Model.SetStorageContent(analysisParamSetVM.AlignmentResultFileName, analysisParamSetVM.MspDB, analysisParamSetVM.TextDB);
            return true;
        }

        private bool ProcessAnnotaion(Window owner, MsdialDataStorage storage) {
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

        private bool ProcessAlignment(Window owner, MsdialDataStorage storage) {
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

        public override void LoadProject() {
            LoadSelectedAnalysisFile();
        }

        public DelegateCommand LoadAnalysisFileCommand {
            get => loadAnalysisFileCommand ?? (loadAnalysisFileCommand = new DelegateCommand(LoadSelectedAnalysisFile));
        }
        private DelegateCommand loadAnalysisFileCommand;

        private void LoadSelectedAnalysisFile() {
            if (AnalysisFilesView.CurrentItem is AnalysisFileBean analysis) {
                LoadAnalysisFile(analysis);
            }
        }

        public DelegateCommand LoadAlignmentFileCommand {
            get => loadAlignmentFileCommand ?? (loadAlignmentFileCommand = new DelegateCommand(LoadSelectedAlignmentFile));
        }
        private DelegateCommand loadAlignmentFileCommand;
        private void LoadSelectedAlignmentFile() {
            if (AlignmentFilesView.CurrentItem is AlignmentFileBean alignment) {
                LoadAlignmentFile(alignment);
            }
        }

        private AnalysisFileBean cacheAnalysisFile;
        private void LoadAnalysisFile(AnalysisFileBean analysis) {
            if (cacheAnalysisFile == analysis || analysis == null) {
                return;
            }

            cacheAnalysisFile = analysis;
            Model.AnalysisFile = analysis;
            AnalysisVM =  new AnalysisDimsVM(Model.AnalysisModel) { DisplayFilters = displayFilters };
        }

        private AlignmentFileBean cacheAlignmentFile;
        private void LoadAlignmentFile(AlignmentFileBean alignment) {
            if (cacheAlignmentFile == alignment || alignment == null) {
                return;
            }

            cacheAlignmentFile = alignment;
            Model.AlignmentFile = alignment;
            AlignmentVM = new AlignmentDimsVM(Model.AlignmentModel) { DisplayFilters = displayFilters };
        }

        public override void SaveProject() {
            Model.SaveProject();
        }

        public DelegateCommand<Window> ExportAnalysisResultCommand => exportAnalysisResultCommand ?? (exportAnalysisResultCommand = new DelegateCommand<Window>(ExportAnalysis));
        private DelegateCommand<Window> exportAnalysisResultCommand;

        private void ExportAnalysis(Window owner) {
            var container = Model.Storage;
            var spectraTypes = new List<Model.Export.SpectraType>
            {
                new Model.Export.SpectraType(
                    ExportspectraType.deconvoluted,
                    new DimsAnalysisMetadataAccessor(container.DataBaseMapper, container.ParameterBase, ExportspectraType.deconvoluted)),
                new Model.Export.SpectraType(
                    ExportspectraType.centroid,
                    new DimsAnalysisMetadataAccessor(container.DataBaseMapper, container.ParameterBase, ExportspectraType.centroid)),
                new Model.Export.SpectraType(
                    ExportspectraType.profile,
                    new DimsAnalysisMetadataAccessor(container.DataBaseMapper, container.ParameterBase, ExportspectraType.profile)),
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

        private void ExportAlignment(Window owner) {
            var container = Model.Storage;
            var metadataAccessor = new DimsMetadataAccessor(container.DataBaseMapper, container.ParameterBase);
            var vm = new AlignmentResultExport2VM(Model.AlignmentFile, Model.AlignmentFiles, container);
            vm.ExportTypes.AddRange(
                new List<ExportType2>
                {
                    new ExportType2("Raw data (Height)", metadataAccessor, new LegacyQuantValueAccessor("Height", container.ParameterBase), "Height", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }, true),
                    new ExportType2("Raw data (Area)", metadataAccessor, new LegacyQuantValueAccessor("Area", container.ParameterBase), "Area", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }),
                    new ExportType2("Normalized data (Height)", metadataAccessor, new LegacyQuantValueAccessor("Normalized height", container.ParameterBase), "NormalizedHeight", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }),
                    new ExportType2("Normalized data (Area)", metadataAccessor, new LegacyQuantValueAccessor("Normalized area", container.ParameterBase), "NormalizedArea", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }),
                    new ExportType2("Alignment ID", metadataAccessor, new LegacyQuantValueAccessor("ID", container.ParameterBase), "PeakID"),
                    new ExportType2("m/z", metadataAccessor, new LegacyQuantValueAccessor("MZ", container.ParameterBase), "Mz"),
                    new ExportType2("S/N", metadataAccessor, new LegacyQuantValueAccessor("SN", container.ParameterBase), "SN"),
                    new ExportType2("MS/MS included", metadataAccessor, new LegacyQuantValueAccessor("MSMS", container.ParameterBase), "MsmsIncluded"),
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
    }
}
