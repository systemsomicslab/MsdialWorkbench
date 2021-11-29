using CompMs.App.Msdial.Model.Core;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Enum;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcImMsApi.Algorithm;
using CompMs.MsdialLcImMsApi.Algorithm.Alignment;
using CompMs.MsdialLcImMsApi.DataObj;
using CompMs.MsdialLcImMsApi.Process;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Lcimms
{
    class LcimmsMethodModel : MethodModelBase
    {
        static LcimmsMethodModel() {
            chromatogramSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.Drift);
        }

        public LcimmsMethodModel(MsdialLcImMsDataStorage storage, IDataProviderFactory<AnalysisFileBean> providerFactory)
            : base(storage.AnalysisFiles, storage.AlignmentFiles) {
            if (storage is null) {
                throw new ArgumentNullException(nameof(storage));
            }

            if (providerFactory is null) {
                throw new ArgumentNullException(nameof(providerFactory));
            }

            Storage = storage;
            this.providerFactory = providerFactory;
        }

        public MsdialLcImMsDataStorage Storage {
            get => storage;
            set => SetProperty(ref storage, value);
        }
        private MsdialLcImMsDataStorage storage;

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

        private IAnnotationProcess annotationProcess;
        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer;
        private readonly IDataProviderFactory<AnalysisFileBean> providerFactory;

        protected override void LoadAnalysisFileCore(AnalysisFileBean analysisFile) {
            if (AnalysisModel != null) {
                AnalysisModel.Dispose();
                Disposables.Remove(AnalysisModel);
            }
            var provider = providerFactory.Create(analysisFile);
            AnalysisModel = new LcimmsAnalysisModel(
                analysisFile,
                provider,
                null, null, Storage.MsdialLcImMsParameter,
                null, null)
            .AddTo(Disposables);
        }

        protected override void LoadAlignmentFileCore(AlignmentFileBean alignmentFile) {
            if (AlignmentModel != null) {
                AlignmentModel.Dispose();
                Disposables.Remove(AlignmentModel);
            }
            AlignmentModel = new LcimmsAlignmentModel(
                alignmentFile,
                Storage.MsdialLcImMsParameter, Storage.DataBaseMapper)
            .AddTo(Disposables);
        }

        public void SetAnalysisParameter(LcimmsAnalysisParameterSetModel analysisParamSetModel) {
            if (Storage.MsdialLcImMsParameter.ProcessOption.HasFlag(ProcessOption.Alignment)) {
                var filename = analysisParamSetModel.AlignmentResultFileName;
                AlignmentFiles.Add(
                    new AlignmentFileBean
                    {
                        FileID = AlignmentFiles.Count,
                        FileName = filename,
                        FilePath = Path.Combine(Storage.MsdialLcImMsParameter.ProjectFolderPath, $"{filename}.{MsdialDataStorageFormat.arf}"),
                        EicFilePath = Path.Combine(Storage.MsdialLcImMsParameter.ProjectFolderPath, $"{filename}.EIC.aef"),
                        SpectraFilePath = Path.Combine(Storage.MsdialLcImMsParameter.ProjectFolderPath, $"{filename}.{MsdialDataStorageFormat.dcl}"),
                    });
                Storage.AlignmentFiles = AlignmentFiles.ToList();
            }

            annotationProcess = BuildAnnotationProcess(Storage.DataBases, Storage.MsdialLcImMsParameter.PeakPickBaseParam);
            Storage.DataBaseMapper = CreateDataBaseMapper(Storage.DataBases);
        }

        private IAnnotationProcess BuildAnnotationProcess(DataBaseStorage storage, PeakPickBaseParameter parameter) {
            var containers = new List<IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>>();
            foreach (var annotators in storage.MetabolomicsDataBases) {
                containers.AddRange(annotators.Pairs.Select(annotator => annotator.ConvertToAnnotatorContainer()));
            }
            return new StandardAnnotationProcess<IAnnotationQuery>(new AnnotationQueryFactory(parameter), containers);
        }

        private DataBaseMapper CreateDataBaseMapper(DataBaseStorage storage) {
            var mapper = new DataBaseMapper();
            foreach (var db in storage.MetabolomicsDataBases) {
                foreach (var pair in db.Pairs) {
                    mapper.Add(pair.SerializableAnnotator, db.DataBase);
                }
            }
            foreach (var db in storage.ProteomicsDataBases) {
                foreach (var pair in db.Pairs) {
                    mapper.Add(pair.SerializableAnnotator, db.DataBase);
                }
            }
            return mapper;
        }

        public async Task RunAnnotationProcess(AnalysisFileBean analysisfile, Action<int> action) {
            await Task.Run(() => FileProcess.Run(analysisfile, new StandardDataProviderFactory(), new LcimmsAccumulateDataProviderFactory(), annotationProcess, storage, isGuiProcess: true, reportAction: action));
        }

        public void RunAlignmentProcess() {
            AlignmentProcessFactory aFactory = new LcimmsAlignmentProcessFactory(Storage.MsdialLcImMsParameter, Storage.IupacDatabase, Storage.DataBaseMapper);
            var alignmentFile = Storage.AlignmentFiles.Last();
            var aligner = aFactory.CreatePeakAligner();
            var result = aligner.Alignment(storage.AnalysisFiles, alignmentFile, chromatogramSpotSerializer);
            MessagePackHandler.SaveToFile(result, alignmentFile.FilePath);
            MsdecResultsWriter.Write(alignmentFile.SpectraFilePath, LoadRepresentativeDeconvolutions(storage, result.AlignmentSpotProperties).ToList());
        }

        private static IEnumerable<MSDecResult> LoadRepresentativeDeconvolutions(MsdialLcImMsDataStorage storage, IReadOnlyList<AlignmentSpotProperty> spots) {
            var files = storage.AnalysisFiles;

            var pointerss = new List<(int version, List<long> pointers, bool isAnnotationInfo)>();
            foreach (var file in files) {
                MsdecResultsReader.GetSeekPointers(file.DeconvolutionFilePath, out var version, out var pointers, out var isAnnotationInfo);
                pointerss.Add((version, pointers, isAnnotationInfo));
            }

            var streams = new List<FileStream>();
            try {
                streams = files.Select(file => File.OpenRead(file.DeconvolutionFilePath)).ToList();
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
    }
}
