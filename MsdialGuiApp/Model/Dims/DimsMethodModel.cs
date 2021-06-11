using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.MessagePack;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Enum;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialDimsCore;
using CompMs.MsdialDimsCore.Algorithm.Alignment;
using CompMs.MsdialDimsCore.Algorithm.Annotation;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.MsdialDimsCore.Parser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Dims
{
    class DimsMethodModel : BindableBase, IDisposable
    {
        static DimsMethodModel() {
            chromatogramSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.Mz);
        }

        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer;

        public DimsMethodModel(MsdialDataStorage storage, List<AnalysisFileBean> analysisFiles, List<AlignmentFileBean> alignmentFiles, IDataProviderFactory<AnalysisFileBean> providerFactory) {
            Storage = storage;
            AnalysisFiles = new ObservableCollection<AnalysisFileBean>(analysisFiles);
            AlignmentFiles = new ObservableCollection<AlignmentFileBean>(alignmentFiles);
            ProviderFactory = providerFactory;
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

        public AnalysisFileBean AnalysisFile {
            get => analysisFile;
            set {
                if (SetProperty(ref analysisFile, value)) {
                    AnalysisModel = CreateAnalysisModel(value);
                }
            }
        }
        private AnalysisFileBean analysisFile;

        public ObservableCollection<AlignmentFileBean> AlignmentFiles {
            get => alignmentFiles;
            set => SetProperty(ref alignmentFiles, value);
        }
        private ObservableCollection<AlignmentFileBean> alignmentFiles;

        public AlignmentFileBean AlignmentFile {
            get => alignmentFile;
            set {
                if (SetProperty(ref alignmentFile, value)) {
                    AlignmentModel = CreateAlignmentModel(value);
                }
            }
        }
        private AlignmentFileBean alignmentFile;

        public DimsAnalysisModel AnalysisModel {
            get => analysisModel;
            private set {
                var old = analysisModel;
                if (SetProperty(ref analysisModel, value)) {
                    old?.Dispose();
                }
            }
        }
        private DimsAnalysisModel analysisModel;

        public DimsAlignmentModel AlignmentModel {
            get => alignmentModel;
            private set {
                var old = alignmentModel;
                if (SetProperty(ref alignmentModel, value)) {
                    old?.Dispose();
                }
            }
        }
        private DimsAlignmentModel alignmentModel;

        public MsdialDimsSerializer Serializer {
            get => serializer ?? (serializer = new MsdialDimsSerializer());
        }
        private MsdialDimsSerializer serializer;
        private bool disposedValue;

        public IDataProviderFactory<AnalysisFileBean> ProviderFactory { get; }

        public void SetStorageContent(string alignmentResultFileName, List<MoleculeMsReference> MspDB, List<MoleculeMsReference> TextDB) {
            if (AlignmentFiles == null)
                AlignmentFiles = new ObservableCollection<AlignmentFileBean>();
            AlignmentFiles.Add(
                new AlignmentFileBean
                {
                    FileID = AlignmentFiles.Count,
                    FileName = alignmentResultFileName,
                    FilePath = System.IO.Path.Combine(Storage.ParameterBase.ProjectFolderPath, alignmentResultFileName + "." + MsdialDataStorageFormat.arf),
                    EicFilePath = System.IO.Path.Combine(Storage.ParameterBase.ProjectFolderPath, alignmentResultFileName + ".EIC.aef"),
                    SpectraFilePath = System.IO.Path.Combine(Storage.ParameterBase.ProjectFolderPath, alignmentResultFileName + "." + MsdialDataStorageFormat.dcl)
                }
            );
            Storage.AlignmentFiles = AlignmentFiles.ToList();

            Storage.DataBaseMapper = new DataBaseMapper();

            var mspAnnotator = new DimsMspAnnotator(MspDB, Storage.ParameterBase.MspSearchParam, Storage.ParameterBase.TargetOmics, "MspDB");
            mspChromatogramAnnotator = mspAnnotator;
            mspAlignmentAnnotator = mspAnnotator;
            Storage.DataBaseMapper.Add(mspAnnotator);

            var textAnnotator = new MassAnnotator(TextDB, Storage.ParameterBase.TextDbSearchParam, Storage.ParameterBase.TargetOmics, SourceType.TextDB, "TextDB");
            textDBChromatogramAnnotator = textAnnotator;
            textDBAlignmentAnnotator = textAnnotator;
            Storage.DataBaseMapper.Add(textAnnotator);
        }

        public async Task RunAnnotationProcessAsync(AnalysisFileBean analysisfile, Action<int> action) {
            await Task.Run(() => ProcessFile.Run(analysisfile, storage, mspChromatogramAnnotator, textDBChromatogramAnnotator, isGuiProcess: true, reportAction: action));
        }

        public void RunAlignmentProcess() {
            AlignmentProcessFactory aFactory = new DimsAlignmentProcessFactory(Storage.ParameterBase as MsdialDimsParameter, Storage.IupacDatabase);
            var alignmentFile = Storage.AlignmentFiles.Last();
            var aligner = aFactory.CreatePeakAligner();
            var result = aligner.Alignment(storage.AnalysisFiles, alignmentFile, chromatogramSpotSerializer);
            MessagePackHandler.SaveToFile(result, alignmentFile.FilePath);
            MsdecResultsWriter.Write(alignmentFile.SpectraFilePath, LoadRepresentativeDeconvolutions(storage, result.AlignmentSpotProperties).ToList());
        }

        private static IEnumerable<MSDecResult> LoadRepresentativeDeconvolutions(MsdialDataStorage storage, IReadOnlyList<AlignmentSpotProperty> spots) {
            var files = storage.AnalysisFiles;

            var pointerss = new List<(int version, List<long> pointers, bool isAnnotationInfo)>();
            foreach (var file in files) {
                MsdecResultsReader.GetSeekPointers(file.DeconvolutionFilePath, out var version, out var pointers, out var isAnnotationInfo);
                pointerss.Add((version, pointers, isAnnotationInfo));
            }

            var streams = new List<FileStream>();
            try {
                streams = files.Select(file => File.Open(file.DeconvolutionFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)).ToList();
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

        public void SaveProject() {
            AlignmentModel?.SaveProject();
        }

        public DimsAnalysisModel CreateAnalysisModel(AnalysisFileBean analysisFile) {
            if (analysisFile == null) {
                return null;
            }
            return new DimsAnalysisModel(
                    analysisFile,
                    ProviderFactory.Create(analysisFile),
                    Storage.DataBaseMapper,
                    Storage.ParameterBase,
                    mspChromatogramAnnotator,
                    textDBChromatogramAnnotator);
        }

        private DimsAlignmentModel CreateAlignmentModel(AlignmentFileBean alignmentFile) {
            if (alignmentFile == null) {
                return null;
            }
            return new DimsAlignmentModel(
                alignmentFile,
                Storage.DataBaseMapper,
                Storage.ParameterBase,
                mspAlignmentAnnotator,
                textDBAlignmentAnnotator);
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    AnalysisModel?.Dispose();
                    AlignmentModel?.Dispose();
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
