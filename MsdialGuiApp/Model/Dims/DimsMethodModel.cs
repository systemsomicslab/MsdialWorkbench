using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialDimsCore;
using CompMs.MsdialDimsCore.Algorithm.Alignment;
using CompMs.MsdialDimsCore.Parameter;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Dims
{
    class DimsMethodModel : MethodModelBase
    {
        static DimsMethodModel() {
            chromatogramSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.Mz);
        }

        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer;
        private readonly IMessageBroker _broker;

        public DimsMethodModel(
            IMsdialDataStorage<MsdialDimsParameter> storage,
            List<AnalysisFileBean> analysisFiles,
            List<AlignmentFileBean> alignmentFiles,
            ProjectBaseParameterModel projectBaseParameter,
            IMessageBroker broker)
            : base(analysisFiles, alignmentFiles, projectBaseParameter) {
            Storage = storage;
            _broker = broker;
            matchResultEvaluator = FacadeMatchResultEvaluator.FromDataBases(storage.DataBases);
            PeakFilterModel = new PeakFilterModel(DisplayFilter.All & ~DisplayFilter.CcsMatched);
        }

        private IAnnotationProcess annotationProcess;
        private FacadeMatchResultEvaluator matchResultEvaluator;

        public PeakFilterModel PeakFilterModel { get; }
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
                    container
                )).ToList());
        }

        public override async Task RunAsync(ProcessOption option, CancellationToken token) {
            Storage.DataBaseMapper = BuildDataBaseMapper(Storage.DataBases);
            matchResultEvaluator = FacadeMatchResultEvaluator.FromDataBases(Storage.DataBases);
            annotationProcess = BuildAnnotationProcess(Storage.DataBases);
            ProviderFactory = Storage.Parameter.ProviderFactoryParameter.Create(retry: 5, isGuiProcess: true);

            var processOption = option;
            // Run Identification
            if (processOption.HasFlag(ProcessOption.Identification) || processOption.HasFlag(ProcessOption.PeakSpotting)) {
                var tasks = new List<Task>();
                foreach (var analysisfile in Storage.AnalysisFiles) {
                    tasks.Add(RunAnnotationProcessAsync(analysisfile, null));
                }
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            // Run Alignment
            if (processOption.HasFlag(ProcessOption.Alignment)) {
                RunAlignmentProcess();
            }

            await LoadAnalysisFileAsync(Storage.AnalysisFiles.FirstOrDefault(), token).ConfigureAwait(false);
        }

        public Task RunAnnotationProcessAsync(AnalysisFileBean analysisfile, Action<int> action) {
            return Task.Run(() => ProcessFile.Run(analysisfile, ProviderFactory, Storage, annotationProcess, matchResultEvaluator, reportAction: action));
        }

        public void RunAlignmentProcess() {
            AlignmentProcessFactory aFactory = new DimsAlignmentProcessFactory(Storage, matchResultEvaluator);
            var alignmentFile = Storage.AlignmentFiles.Last();
            var aligner = aFactory.CreatePeakAligner();
            aligner.ProviderFactory = ProviderFactory;
            var result = aligner.Alignment(Storage.AnalysisFiles, alignmentFile, chromatogramSpotSerializer);
            result.Save(alignmentFile);
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

        protected override IAnalysisModel LoadAnalysisFileCore(AnalysisFileBean analysisFile) {
            if (AnalysisModel != null) {
                AnalysisModel.Dispose();
                Disposables.Remove(AnalysisModel);
            }
            return AnalysisModel = new DimsAnalysisModel(
                analysisFile,
                ProviderFactory.Create(analysisFile),
                matchResultEvaluator,
                Storage.DataBases,
                Storage.DataBaseMapper,
                Storage.Parameter,
                PeakFilterModel).AddTo(Disposables);
        }

        protected override IAlignmentModel LoadAlignmentFileCore(AlignmentFileBean alignmentFile) {
            if (AlignmentModel != null) {
                AlignmentModel.Dispose();
                Disposables.Remove(AlignmentModel);
            }
            return AlignmentModel = new DimsAlignmentModel(
                alignmentFile,
                Storage.DataBases,
                matchResultEvaluator,
                Storage.DataBaseMapper,
                Storage.Parameter,
                Storage.AnalysisFiles,
                PeakFilterModel,
                _broker).AddTo(Disposables);
        }
    }
}
