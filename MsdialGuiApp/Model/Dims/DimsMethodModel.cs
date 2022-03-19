using CompMs.App.Msdial.Model.Core;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Enum;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialDimsCore;
using CompMs.MsdialDimsCore.Algorithm.Alignment;
using CompMs.MsdialDimsCore.Parameter;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Dims
{
    class DimsMethodModel : MethodModelBase
    {
        static DimsMethodModel() {
            chromatogramSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.Mz);
        }

        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer;

        public DimsMethodModel(
            IMsdialDataStorage<MsdialDimsParameter> storage,
            List<AnalysisFileBean> analysisFiles,
            List<AlignmentFileBean> alignmentFiles)
            : base(analysisFiles, alignmentFiles) {
            Storage = storage;
            matchResultEvaluator = FacadeMatchResultEvaluator.FromDataBases(storage.DataBases);
        }

        private IAnnotationProcess annotationProcess;
        private FacadeMatchResultEvaluator matchResultEvaluator;

        public IMsdialDataStorage<MsdialDimsParameter> Storage { get; }

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

        public IDataProviderFactory<AnalysisFileBean> ProviderFactory { get; private set; }

        public void Load() {
            ProviderFactory = Storage.Parameter.ProviderFactoryParameter.Create(retry: 5, isGuiProcess: true);
        }

        public void AnalysisParamSetProcess(DimsAnalysisParameterSetModel parameterSetModel) {
            Storage.DataBases = parameterSetModel.IdentifySettingModel.Create();
            if (parameterSetModel.TogetherWithAlignment) {
                var alignmentResultFileName = parameterSetModel.AlignmentResultFileName;
                AlignmentFiles.Add(
                    new AlignmentFileBean
                    {
                        FileID = AlignmentFiles.Count,
                        FileName = alignmentResultFileName,
                        FilePath = Path.Combine(Storage.Parameter.ProjectFolderPath, alignmentResultFileName + "." + MsdialDataStorageFormat.arf),
                        EicFilePath = Path.Combine(Storage.Parameter.ProjectFolderPath, alignmentResultFileName + ".EIC.aef"),
                        SpectraFilePath = Path.Combine(Storage.Parameter.ProjectFolderPath, alignmentResultFileName + "." + MsdialDataStorageFormat.dcl),
                        ProteinAssembledResultFilePath = Path.Combine(Storage.Parameter.ProjectFolderPath, alignmentResultFileName + "." + MsdialDataStorageFormat.prf)
                    }
                );
                Storage.AlignmentFiles = AlignmentFiles.ToList();
            }

            Storage.DataBaseMapper = BuildDataBaseMapper(Storage.DataBases);
            matchResultEvaluator = FacadeMatchResultEvaluator.FromDataBases(Storage.DataBases);
            annotationProcess = BuildAnnotationProcess(Storage.DataBases);
            ProviderFactory = parameterSetModel.Parameter.ProviderFactoryParameter.Create(retry: 5, isGuiProcess: true);
        }

        private DataBaseMapper BuildDataBaseMapper(DataBaseStorage storage) {
            var mapper = new DataBaseMapper();
            foreach (var db in storage.MetabolomicsDataBases) {
                foreach (var pair in db.Pairs) {
                    mapper.Add(pair.SerializableAnnotator, db.DataBase);
                }
            }
            return mapper;
        }

        private IAnnotationProcess BuildAnnotationProcess(DataBaseStorage storage) {
            var containers = new List<IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>>();
            foreach (var annotators in storage.MetabolomicsDataBases) {
                containers.AddRange(annotators.Pairs.Select(annotator => annotator.ConvertToAnnotatorContainer()));
            }
            return new StandardAnnotationProcess<IAnnotationQuery>(
                containers.Select(container => (
                    new AnnotationQueryWithoutIsotopeFactory(container.Annotator) as IAnnotationQueryFactory<IAnnotationQuery>,
                    container.Annotator as IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>
                )).ToList());
        }

        public override void Run(ProcessOption option) {
            Storage.DataBaseMapper = BuildDataBaseMapper(Storage.DataBases);
            matchResultEvaluator = FacadeMatchResultEvaluator.FromDataBases(Storage.DataBases);
            annotationProcess = BuildAnnotationProcess(Storage.DataBases);
            ProviderFactory = Storage.Parameter.ProviderFactoryParameter.Create(retry: 5, isGuiProcess: true);

            var processOption = option;
            // Run Identification
            if (processOption.HasFlag(ProcessOption.Identification) || processOption.HasFlag(ProcessOption.PeakSpotting)) {
                foreach ((var analysisfile, var idx) in Storage.AnalysisFiles.WithIndex()) {
                    RunAnnotationProcessAsync(analysisfile, null).Wait(); // TODO: change to async method
                }
            }

            // Run Alignment
            if (processOption.HasFlag(ProcessOption.Alignment)) {
                RunAlignmentProcess();
            }

            LoadAnalysisFile(Storage.AnalysisFiles.FirstOrDefault());
        }

        public async Task RunAnnotationProcessAsync(AnalysisFileBean analysisfile, Action<int> action) {
            await Task.Run(() => ProcessFile.Run(analysisfile, ProviderFactory, Storage, annotationProcess, matchResultEvaluator, reportAction: action));
        }

        public void RunAlignmentProcess() {
            AlignmentProcessFactory aFactory = new DimsAlignmentProcessFactory(Storage, matchResultEvaluator);
            var alignmentFile = Storage.AlignmentFiles.Last();
            var aligner = aFactory.CreatePeakAligner();
            aligner.ProviderFactory = ProviderFactory;
            var result = aligner.Alignment(Storage.AnalysisFiles, alignmentFile, chromatogramSpotSerializer);
            MessagePackHandler.SaveToFile(result, alignmentFile.FilePath);
            MsdecResultsWriter.Write(alignmentFile.SpectraFilePath, LoadRepresentativeDeconvolutions(Storage, result.AlignmentSpotProperties).ToList());
        }

        private static IEnumerable<MSDecResult> LoadRepresentativeDeconvolutions(IMsdialDataStorage<MsdialDimsParameter> storage, IReadOnlyList<AlignmentSpotProperty> spots) {
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

        protected override void LoadAnalysisFileCore(AnalysisFileBean analysisFile) {
            if (AnalysisModel != null) {
                AnalysisModel.Dispose();
                Disposables.Remove(AnalysisModel);
            }
            AnalysisModel = new DimsAnalysisModel(
                analysisFile,
                ProviderFactory.Create(analysisFile),
                matchResultEvaluator,
                Storage.DataBaseMapper.MoleculeAnnotators,
                Storage.DataBaseMapper,
                Storage.Parameter).AddTo(Disposables);;
        }

        protected override AlignmentModelBase LoadAlignmentFileCore(AlignmentFileBean alignmentFile) {
            if (AlignmentModel != null) {
                AlignmentModel.Dispose();
                Disposables.Remove(AlignmentModel);
            }
            return AlignmentModel = new DimsAlignmentModel(
                alignmentFile,
                Storage.DataBaseMapper.MoleculeAnnotators,
                matchResultEvaluator,
                Storage.DataBaseMapper,
                Storage.Parameter).AddTo(Disposables);
        }
    }
}
