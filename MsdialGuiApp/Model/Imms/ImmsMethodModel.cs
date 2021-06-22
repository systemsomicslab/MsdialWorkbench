using CompMs.App.Msdial.View.Export;
using CompMs.App.Msdial.View.Imms;
using CompMs.App.Msdial.ViewModel.Export;
using CompMs.App.Msdial.ViewModel.Imms;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.Common.MessagePack;
using CompMs.CommonMVVM;
using CompMs.Graphics.UI.ProgressBar;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Enum;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialImmsCore.Algorithm.Alignment;
using CompMs.MsdialImmsCore.Algorithm.Annotation;
using CompMs.MsdialImmsCore.Export;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialImmsCore.Process;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.Model.Imms
{
    class ImmsMethodModel : BindableBase, IDisposable
    {
        static ImmsMethodModel() {
            chromatogramSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", CompMs.Common.Components.ChromXType.Drift);
        }
        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer;

        public ImmsMethodModel(MsdialDataStorage storage, IDataProviderFactory<AnalysisFileBean> providerFactory) {
            Storage = storage;
            AnalysisFiles = new ObservableCollection<AnalysisFileBean>(storage.AnalysisFiles);
            AlignmentFiles = new ObservableCollection<AlignmentFileBean>(storage.AlignmentFiles);
            this.providerFactory = providerFactory;
        }

        private readonly IDataProviderFactory<AnalysisFileBean> providerFactory;

        public IAnnotator<ChromatogramPeakFeature, MSDecResult> MspChromatogramAnnotator { get; private set; }
        public IAnnotator<ChromatogramPeakFeature, MSDecResult> TextDBChromatogramAnnotator { get; private set; }
        public IAnnotator<AlignmentSpotProperty, MSDecResult> MspAlignmentAnnotator { get; private set; }
        public IAnnotator<AlignmentSpotProperty, MSDecResult> TextDBAlignmentAnnotator { get; private set; }

        public ImmsAnalysisModel AnalysisModel {
            get => analysisModel;
            set {
                var old = analysisModel;
                if (SetProperty(ref analysisModel, value)) {
                    old?.Dispose();
                }
            }
        }
        private ImmsAnalysisModel analysisModel;

        public ImmsAlignmentModel AlignmentModel {
            get => alignmentModel;
            set {
                var old = alignmentModel;
                if (SetProperty(ref alignmentModel, value)) {
                    old?.Dispose();
                }
            }
        }
        private ImmsAlignmentModel alignmentModel;

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

        public ObservableCollection<AlignmentFileBean> AlignmentFiles {
            get => alignmentFiles;
            set => SetProperty(ref alignmentFiles, value);
        }
        private ObservableCollection<AlignmentFileBean> alignmentFiles;

        public int InitializeNewProject(Window window) {
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

            return 0;
        }

        public void LoadAnnotator() {
            var mspAnnotator = Storage.DataBaseMapper.KeyToRefer["MspDB"] as IAnnotator<IMSIonProperty, MSDecResult>;
            MspChromatogramAnnotator = mspAnnotator;
            MspAlignmentAnnotator = mspAnnotator;
            var textAnnotator = Storage.DataBaseMapper.KeyToRefer["TextDB"] as IAnnotator<IMSIonProperty, MSDecResult>;
            TextDBChromatogramAnnotator = textAnnotator;
            TextDBAlignmentAnnotator = textAnnotator;
        }

        private bool ProcessSetAnalysisParameter(Window owner) {
            var analysisParamSetVM = new ImmsAnalysisParamSetVM((MsdialImmsParameter)Storage.ParameterBase, AnalysisFiles);
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
            var mspAnnotator = new ImmsMspAnnotator(analysisParamSetVM.MspDB, Storage.ParameterBase.MspSearchParam, Storage.ParameterBase.TargetOmics, "MspDB");
            MspChromatogramAnnotator = mspAnnotator;
            MspAlignmentAnnotator = mspAnnotator;
            Storage.DataBaseMapper.Databases.Add(new MoleculeDataBase(analysisParamSetVM.MspDB, "MspDB"));
            Storage.DataBaseMapper.Add(mspAnnotator);
            var textAnnotator = new ImmsTextDBAnnotator(analysisParamSetVM.TextDB, Storage.ParameterBase.TextDbSearchParam, "TextDB");
            TextDBChromatogramAnnotator = textAnnotator;
            TextDBAlignmentAnnotator = textAnnotator;
            Storage.DataBaseMapper.Databases.Add(new MoleculeDataBase(analysisParamSetVM.TextDB, "TextDB"));
            Storage.DataBaseMapper.Add(textAnnotator);
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
                    await Task.Run(() => FileProcess.Run(analysisfile, storage, MspChromatogramAnnotator, TextDBChromatogramAnnotator, providerFactory, isGuiProcess: true, reportAction: v => pbvm.CurrentValue = v));
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
                Label = "Process alignment..",
            };
            var pbw = new ProgressBarWindow
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            pbw.Show();

            var factory = new ImmsAlignmentProcessFactory(storage.ParameterBase as MsdialImmsParameter, storage.IupacDatabase);
            var aligner = factory.CreatePeakAligner();
            aligner.ProviderFactory = providerFactory; // TODO: I'll remove this later.
            var alignmentFile = storage.AlignmentFiles.Last();
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

        public void LoadAnalysisFile(AnalysisFileBean analysis) {
            if (cacheAnalysisFile == analysis) return;

            cacheAnalysisFile = analysis;
            var provider = providerFactory.Create(analysis);
            AnalysisModel = new ImmsAnalysisModel( // should dispose.
                analysis,
                provider,
                storage.DataBaseMapper,
                Storage.ParameterBase,
                MspChromatogramAnnotator,
                TextDBChromatogramAnnotator);
        }

        private AnalysisFileBean cacheAnalysisFile;

        public void LoadAlignmentFile(AlignmentFileBean alignment) {
            if (cacheAlignmentFile == alignment) return;

            cacheAlignmentFile = alignment;
            AlignmentModel = new ImmsAlignmentModel( // should dispose
                alignment,
                Storage.ParameterBase,
                Storage.DataBaseMapper,
                MspAlignmentAnnotator,
                TextDBAlignmentAnnotator);
        }

        private AlignmentFileBean cacheAlignmentFile;

        public void ExportAlignment(Window owner) {
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

        public void ExportAnalysis(Window owner) {
            var container = Storage;
            var spectraTypes = new List<Export.SpectraType>
            {
                new Export.SpectraType(
                    ExportspectraType.deconvoluted,
                    new ImmsAnalysisMetadataAccessor(container.DataBaseMapper, container.ParameterBase, ExportspectraType.deconvoluted)),
                new Export.SpectraType(
                    ExportspectraType.centroid,
                    new ImmsAnalysisMetadataAccessor(container.DataBaseMapper, container.ParameterBase, ExportspectraType.centroid)),
                new Export.SpectraType(
                    ExportspectraType.profile,
                    new ImmsAnalysisMetadataAccessor(container.DataBaseMapper, container.ParameterBase, ExportspectraType.profile)),
            };
            var spectraFormats = new List<Export.SpectraFormat>
            {
                new Export.SpectraFormat(ExportSpectraFileFormat.txt, new AnalysisCSVExporter()),
            };

            using (var vm = new AnalysisResultExportViewModel(container.AnalysisFiles, spectraTypes, spectraFormats, providerFactory)) {
                var dialog = new AnalysisResultExportWin
                {
                    DataContext = vm,
                    Owner = owner,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };

                dialog.ShowDialog();
            }
        }

        private bool disposedValue;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    analysisModel?.Dispose();
                    alignmentModel?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
