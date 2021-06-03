using CompMs.App.Msdial.Model.Imms;
using CompMs.App.Msdial.View.Export;
using CompMs.App.Msdial.View.Imms;
using CompMs.App.Msdial.ViewModel.Export;
using CompMs.Common.Extension;
using CompMs.Common.MessagePack;
using CompMs.CommonMVVM;
using CompMs.Graphics.UI.ProgressBar;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Enum;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialImmsCore.Algorithm;
using CompMs.MsdialImmsCore.Algorithm.Alignment;
using CompMs.MsdialImmsCore.Algorithm.Annotation;
using CompMs.MsdialImmsCore.Export;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialImmsCore.Process;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Imms
{
    class ImmsMethodVM : MethodVM
    {
        static ImmsMethodVM() {
            serializer = new MsdialImmsCore.Parser.MsdialImmsSerializer();
            chromatogramSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", CompMs.Common.Components.ChromXType.Drift);
        }
        private static readonly MsdialSerializer serializer;
        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer;

        public ImmsMethodVM(MsdialDataStorage storage, List<AnalysisFileBean> analysisFiles, List<AlignmentFileBean> alignmentFiles) : base(serializer) {
            Storage = storage;

            mspChromatogramAnnotator = new ImmsMspAnnotator<ChromatogramPeakFeature>(Storage.MspDB, Storage.ParameterBase.MspSearchParam, Storage.ParameterBase.TargetOmics, "MspDB");
            textDBChromatogramAnnotator = new ImmsTextDBAnnotator<ChromatogramPeakFeature>(Storage.TextDB, Storage.ParameterBase.TextDbSearchParam, "TextDB");

            mspAlignmentAnnotator = new ImmsMspAnnotator<AlignmentSpotProperty>(Storage.MspDB, Storage.ParameterBase.MspSearchParam, Storage.ParameterBase.TargetOmics, "MspDB");
            textDBAlignmentAnnotator = new ImmsTextDBAnnotator<AlignmentSpotProperty>(Storage.TextDB, Storage.ParameterBase.TextDbSearchParam, "TextDB");

            AnalysisFiles = new ObservableCollection<AnalysisFileBean>(analysisFiles);
            analysisFilesView = CollectionViewSource.GetDefaultView(AnalysisFiles);
            AlignmentFiles = new ObservableCollection<AlignmentFileBean>(alignmentFiles);
            alignmentFilesView = CollectionViewSource.GetDefaultView(AlignmentFiles);

            PropertyChanged += OnDisplayFiltersChanged;
        }

        private IAnnotator<ChromatogramPeakFeature, MSDecResult> mspChromatogramAnnotator, textDBChromatogramAnnotator;
        private IAnnotator<AlignmentSpotProperty, MSDecResult> mspAlignmentAnnotator, textDBAlignmentAnnotator;

        public MsdialDataStorage Storage {
            get => storage;
            set => SetProperty(ref storage, value);
        }
        private MsdialDataStorage storage;

        public ObservableCollection<AnalysisFileBean> AnalysisFiles {
            get => analysisFiles;
            set => SetProperty(ref analysisFiles, value);
        }
        private ObservableCollection<AnalysisFileBean> analysisFiles;
        private ICollectionView analysisFilesView;

        public ObservableCollection<AlignmentFileBean> AlignmentFiles {
            get => alignmentFiles;
            set => SetProperty(ref alignmentFiles, value);
        }
        private ObservableCollection<AlignmentFileBean> alignmentFiles;
        private ICollectionView alignmentFilesView;

        public AnalysisImmsVM AnalysisVM {
            get => analysisVM;
            set => SetProperty(ref analysisVM, value);
        }
        private AnalysisImmsVM analysisVM;

        public AlignmentImmsVM AlignmentVM {
            get => alignmentVM;
            set => SetProperty(ref alignmentVM, value);
        }
        private AlignmentImmsVM alignmentVM;

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

            var processOption = Storage.ParameterBase.ProcessOption;
            // Run Identification
            if (processOption.HasFlag(CompMs.Common.Enum.ProcessOption.Identification) || processOption.HasFlag(CompMs.Common.Enum.ProcessOption.PeakSpotting)) {
                if (!ProcessAnnotaion(window, Storage))
                    return -1;
            }

            // Run Alignment
            if (processOption.HasFlag(CompMs.Common.Enum.ProcessOption.Alignment)) {
                if (!ProcessAlignment(window, Storage))
                    return -1;
            }

            LoadAnalysisFile(Storage.AnalysisFiles.FirstOrDefault());

            return 0;
        }

        private bool ProcessSetAnalysisParameter(Window owner) {
            var analysisParamSetVM = new ImmsAnalysisParamSetVM((MsdialImmsParameter)Storage.ParameterBase, Storage.AnalysisFiles);
            var apsw = new AnalysisParamSetForImmsWindow
            {
                DataContext = analysisParamSetVM,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            var apsw_result = apsw.ShowDialog();
            if (apsw_result != true) return false;

            if (AlignmentFiles == null)
                AlignmentFiles = new ObservableCollection<AlignmentFileBean>();
            var filename = analysisParamSetVM.AlignmentResultFileName;
            AlignmentFiles.Add(
                new AlignmentFileBean
                {
                    FileID = AlignmentFiles.Count,
                    FileName = filename,
                    FilePath = System.IO.Path.Combine(Storage.ParameterBase.ProjectFolderPath, filename + "." + MsdialDataStorageFormat.arf),
                    EicFilePath = System.IO.Path.Combine(Storage.ParameterBase.ProjectFolderPath, filename + ".EIC.aef"),
                    SpectraFilePath = System.IO.Path.Combine(Storage.ParameterBase.ProjectFolderPath, filename + "." + MsdialDataStorageFormat.dcl)
                }
            );
            Storage.AlignmentFiles = AlignmentFiles.ToList();
            Storage.MspDB = analysisParamSetVM.MspDB;
            mspChromatogramAnnotator = new ImmsMspAnnotator<ChromatogramPeakFeature>(Storage.MspDB, Storage.ParameterBase.MspSearchParam, Storage.ParameterBase.TargetOmics, "MspDB");
            mspAlignmentAnnotator = new ImmsMspAnnotator<AlignmentSpotProperty>(Storage.MspDB, Storage.ParameterBase.MspSearchParam, Storage.ParameterBase.TargetOmics, "MspDB");
            Storage.TextDB = analysisParamSetVM.TextDB;
            textDBChromatogramAnnotator = new ImmsTextDBAnnotator<ChromatogramPeakFeature>(Storage.TextDB, Storage.ParameterBase.TextDbSearchParam, "TextDB");
            textDBAlignmentAnnotator = new ImmsTextDBAnnotator<AlignmentSpotProperty>(Storage.TextDB, Storage.ParameterBase.TextDbSearchParam, "TextDB");
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
                foreach ((var analysisfile, var pbvm) in storage.AnalysisFiles.Zip(vm.ProgressBarVMs)) {
                    await Task.Run(() => FileProcess.Run(analysisfile, storage, mspChromatogramAnnotator, textDBChromatogramAnnotator, isGuiProcess: true, reportAction: v => pbvm.CurrentValue = v));
                    vm.CurrentValue++;
                }
                pbmcw.Close();
            };

            pbmcw.ShowDialog();

            return true;
        }

        private static bool ProcessAlignment(Window owner, MsdialDataStorage storage) {
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

            var factory = new ImmsProcessFactory(storage.ParameterBase as MsdialImmsParameter, storage.IupacDatabase);
            AlignmentProcessFactory aFactory = factory.CreateAlignmentFactory();
            var alignmentFile = storage.AlignmentFiles.Last();
            var aligner = aFactory.CreatePeakAligner();
            aligner.ProcessFactory = factory; // TODO: I'll remove this later.
            var result = aligner.Alignment(storage.AnalysisFiles, alignmentFile, chromatogramSpotSerializer);
            MessagePackHandler.SaveToFile(result, alignmentFile.FilePath);
            MsdecResultsWriter.Write(alignmentFile.SpectraFilePath, LoadRepresentativeDeconvolutions(storage, result.AlignmentSpotProperties).ToList());

            pbw.Close();

            return true;
        }

        private static IEnumerable<MSDecResult> LoadRepresentativeDeconvolutions(MsdialDataStorage storage, IReadOnlyList<AlignmentSpotProperty> spots) {
            var files = storage.AnalysisFiles;

            var pointerss = new List<(int version, List<long> pointers, bool isAnnotationInfo)>();
            foreach (var file in files) {
                MsdecResultsReader.GetSeekPointers(file.DeconvolutionFilePath, out var version, out var pointers, out var isAnnotationInfo);
                pointerss.Add((version, pointers, isAnnotationInfo));
            }

            var streams = new List<System.IO.FileStream>();
            try {
                streams = files.Select(file => System.IO.File.OpenRead(file.DeconvolutionFilePath)).ToList();
                foreach (var spot in spots) {
                    var repID = spot.RepresentativeFileID;
                    var peakID = spot.AlignedPeakProperties[repID].MasterPeakID;
                    var decResult = MsdecResultsReader.ReadMSDecResult(
                        streams[repID], pointerss[repID].pointers[peakID],
                        pointerss[repID].version, pointerss[repID].isAnnotationInfo);
                    yield return decResult;
                }
            }
            finally {
                streams.ForEach(stream => stream.Close());
            }
        }

        public override void LoadProject() {
            LoadSelectedAnalysisFile();
        }

        public DelegateCommand LoadAnalysisFileCommand {
            get => loadAnalysisFileCommand ?? (loadAnalysisFileCommand = new DelegateCommand(LoadSelectedAnalysisFile));
        }
        private DelegateCommand loadAnalysisFileCommand;

        private void LoadSelectedAnalysisFile() {
            if (analysisFilesView.CurrentItem is AnalysisFileBean analysis) {
                LoadAnalysisFile(analysis);
            }
        }

        public DelegateCommand LoadAlignmentFileCommand {
            get => loadAlignmentFileCommand ?? (loadAlignmentFileCommand = new DelegateCommand(LoadSelectedAlignmentFile));
        }
        private DelegateCommand loadAlignmentFileCommand;
        private void LoadSelectedAlignmentFile() {
            if (alignmentFilesView.CurrentItem is AlignmentFileBean alignment) {
                LoadAlignmentFile(alignment);
            }
        }

        private void LoadAnalysisFile(AnalysisFileBean analysis) {
            if (cacheAnalysisFile == analysis) return;

            cacheAnalysisFile = analysis;
            var provider = new ImmsAverageDataProvider(analysis);
            AnalysisVM = new AnalysisImmsVM(
                new ImmsAnalysisModel( // should dispose.
                    analysis,
                    provider,
                    storage.DataBaseMapper,
                    Storage.ParameterBase,
                    mspChromatogramAnnotator,
                    textDBChromatogramAnnotator),
                analysis,
                provider,
                Storage.ParameterBase,
                mspChromatogramAnnotator,
                textDBChromatogramAnnotator)
            {
                DisplayFilters = displayFilters
            };
        }

        private AnalysisFileBean cacheAnalysisFile;

        private void LoadAlignmentFile(AlignmentFileBean alignment) {
            if (cacheAlignmentFile == alignment) return;

            cacheAlignmentFile = alignment;
            AlignmentVM = new AlignmentImmsVM(
                new ImmsAlignmentModel( // should dispose
                    alignment,
                    Storage.ParameterBase,
                    Storage.DataBaseMapper,
                    mspAlignmentAnnotator,
                    textDBAlignmentAnnotator),
                mspAlignmentAnnotator,
                textDBAlignmentAnnotator)
            {
                DisplayFilters = displayFilters
            };
        }

        private AlignmentFileBean cacheAlignmentFile;

        public override void SaveProject() {
            AlignmentVM?.SaveProject();
        }

        public DelegateCommand<Window> ExportAlignmentResultCommand => exportAlignmentResultCommand ?? (exportAlignmentResultCommand = new DelegateCommand<Window>(ExportAlignment));
        private DelegateCommand<Window> exportAlignmentResultCommand;

        private void ExportAlignment(Window owner) {
            var container = Storage;
            var metadataAccessor = new ImmsMetadataAccessor(container.DataBaseMapper, container.ParameterBase);
            var vm = new AlignmentResultExport2VM(cacheAlignmentFile, container.AlignmentFiles, container);
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
            return displayFilters.Read(flag);
        }

        private void WriteDisplayFilter(DisplayFilter flag, bool set) {
            displayFilters.Write(flag, set);
            OnPropertyChanged(nameof(displayFilters));
        }
    }
}
