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
using CompMs.MsdialLcImMsApi.Algorithm.Alignment;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcImMsApi.Parser;
using CompMs.MsdialLcImMsApi.Process;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Lcimms
{
    class LcimmsMethodModel : ViewModelBase
    {
        static LcimmsMethodModel() {
            chromatogramSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.Drift);
        }
        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer;

        public LcimmsMethodModel(MsdialDataStorage storage, List<AnalysisFileBean> analysisFiles, List<AlignmentFileBean> alignmentFiles) {
            Storage = storage;

            mspChromatogramAnnotator = new MassAnnotator(Storage.MspDB, Storage.ParameterBase.MspSearchParam, Storage.ParameterBase.TargetOmics, SourceType.MspDB, "MspDB");
            textDBChromatogramAnnotator = new MassAnnotator(Storage.TextDB, Storage.ParameterBase.TextDbSearchParam, Storage.ParameterBase.TargetOmics, SourceType.TextDB, "TextDB");

            mspAlignmentAnnotator = new MassAnnotator(Storage.MspDB, Storage.ParameterBase.MspSearchParam, Storage.ParameterBase.TargetOmics, SourceType.MspDB, "MspDB");
            textDBAlignmentAnnotator = new MassAnnotator(Storage.TextDB, Storage.ParameterBase.TextDbSearchParam, Storage.ParameterBase.TargetOmics, SourceType.TextDB, "TextDB");

            AnalysisFiles = new ObservableCollection<AnalysisFileBean>(analysisFiles);
            AnalysisFile = AnalysisFiles.FirstOrDefault();
            AlignmentFiles = new ObservableCollection<AlignmentFileBean>(alignmentFiles);
            AlignmentFile = AlignmentFiles.FirstOrDefault();

            var dataMapper = Storage.DataBaseMapper;
            dataMapper.Add(mspChromatogramAnnotator);
            dataMapper.Add(textDBChromatogramAnnotator);
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

        public LcimmsAnalysisModel AnalysisModel {
            get => analysisModel;
            private set => SetProperty(ref analysisModel, value);
        }
        private LcimmsAnalysisModel analysisModel;

        public LcimmsAlignmentModel AlignmentModel {
            get => alignmentModel;
            private set => SetProperty(ref alignmentModel, value);
        }
        private LcimmsAlignmentModel alignmentModel;

        public MsdialLcImMsSerializer Serializer {
            get => serializer ?? (serializer = new MsdialLcImMsSerializer());
        }
        private MsdialLcImMsSerializer serializer;

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

            Storage.MspDB = MspDB;
            mspChromatogramAnnotator = new MassAnnotator(Storage.MspDB, Storage.ParameterBase.MspSearchParam, Storage.ParameterBase.TargetOmics, SourceType.MspDB, "MspDB");
            mspAlignmentAnnotator = new MassAnnotator(Storage.MspDB, Storage.ParameterBase.MspSearchParam, Storage.ParameterBase.TargetOmics, SourceType.MspDB, "MspDB");

            Storage.TextDB = TextDB;
            textDBChromatogramAnnotator = new MassAnnotator(Storage.TextDB, Storage.ParameterBase.TextDbSearchParam, Storage.ParameterBase.TargetOmics, SourceType.TextDB, "TextDB");
            textDBAlignmentAnnotator = new MassAnnotator(Storage.TextDB, Storage.ParameterBase.TextDbSearchParam, Storage.ParameterBase.TargetOmics, SourceType.TextDB, "TextDB");
        }

        public async Task RunAnnotationProcess(AnalysisFileBean analysisfile, Action<int> action) {
            await Task.Run(() => FileProcess.Run(analysisfile, storage, isGuiProcess: true, reportAction: action));
        }

        public void RunAlignmentProcess() {
            AlignmentProcessFactory aFactory = new LcimmsAlignmentProcessFactory(Storage.ParameterBase as MsdialLcImMsParameter, Storage.IupacDatabase);
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

        public void SaveProject() {
            AlignmentModel?.SaveProject();
        }

        private LcimmsAnalysisModel CreateAnalysisModel(AnalysisFileBean analysisFile) {
            if (analysisFile == null) {
                return null;
            }
            return new LcimmsAnalysisModel(
                    analysisFile,
                    new StandardDataProvider(analysisFile, isGuiProcess: true, retry: 5),
                    Storage.ParameterBase,
                    mspChromatogramAnnotator,
                    textDBChromatogramAnnotator);
        }

        private LcimmsAlignmentModel CreateAlignmentModel(AlignmentFileBean alignmentFile) {
            if (alignmentFile == null) {
                return null;
            }
            return new LcimmsAlignmentModel(
                alignmentFile,
                Storage.ParameterBase,
                mspAlignmentAnnotator,
                textDBAlignmentAnnotator);
        }
    }
}
