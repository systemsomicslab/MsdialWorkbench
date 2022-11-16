using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Export;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Parameter;
using CompMs.Graphics.UI.ProgressBar;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialDimsCore;
using CompMs.MsdialDimsCore.Algorithm.Alignment;
using CompMs.MsdialDimsCore.Export;
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
    internal sealed class DimsMethodModel : MethodModelBase
    {
        static DimsMethodModel() {
            CHROMATOGRAM_SPOT_SERIALIZER = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.Mz);
        }

        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> CHROMATOGRAM_SPOT_SERIALIZER;

        private readonly IMessageBroker _broker;
        private IAnnotationProcess _annotationProcess;
        private FacadeMatchResultEvaluator _matchResultEvaluator;

        public DimsMethodModel(
            IMsdialDataStorage<MsdialDimsParameter> storage,
            AnalysisFileBeanModelCollection analysisFileBeanModelCollection,
            List<AlignmentFileBean> alignmentFiles,
            ProjectBaseParameterModel projectBaseParameter,
            IMessageBroker broker)
            : base(analysisFileBeanModelCollection, alignmentFiles, projectBaseParameter) {
            Storage = storage;
            _broker = broker;
            _matchResultEvaluator = FacadeMatchResultEvaluator.FromDataBases(storage.DataBases);
            PeakFilterModel = new PeakFilterModel(DisplayFilter.All & ~DisplayFilter.CcsMatched);
            ProviderFactory = storage.Parameter.ProviderFactoryParameter.Create(retry: 5, isGuiProcess: true);

            var metadataAccessor = new DimsMetadataAccessor(storage.DataBaseMapper, storage.Parameter);
            var stats = new List<StatsValue> { StatsValue.Average, StatsValue.Stdev, };
            AlignmentResultExportModel = new AlignmentResultExportModel(AlignmentFile, AlignmentFiles, storage);
            AlignmentResultExportModel.AddExportTypes(
                    new ExportType("Raw data (Height)", metadataAccessor, new LegacyQuantValueAccessor("Height", storage.Parameter), "Height", stats, isSelected: true),
                    new ExportType("Raw data (Area)", metadataAccessor, new LegacyQuantValueAccessor("Area", storage.Parameter), "Area", stats),
                    new ExportType("Normalized data (Height)", metadataAccessor, new LegacyQuantValueAccessor("Normalized height", storage.Parameter), "NormalizedHeight", stats),
                    new ExportType("Normalized data (Area)", metadataAccessor, new LegacyQuantValueAccessor("Normalized area", storage.Parameter), "NormalizedArea", stats),
                    new ExportType("Alignment ID", metadataAccessor, new LegacyQuantValueAccessor("ID", storage.Parameter), "PeakID"),
                    new ExportType("m/z", metadataAccessor, new LegacyQuantValueAccessor("MZ", storage.Parameter), "Mz"),
                    new ExportType("S/N", metadataAccessor, new LegacyQuantValueAccessor("SN", storage.Parameter), "SN"),
                    new ExportType("MS/MS included", metadataAccessor, new LegacyQuantValueAccessor("MSMS", storage.Parameter), "MsmsIncluded"));
            this.ObserveProperty(m => m.AlignmentFile)
                .Subscribe(file => AlignmentResultExportModel.AlignmentFile = file)
                .AddTo(Disposables);
        }

        public PeakFilterModel PeakFilterModel { get; }
        public IMsdialDataStorage<MsdialDimsParameter> Storage { get; }

        public DimsAnalysisModel AnalysisModel {
            get => _analysisModel;
            private set {
                var old = _analysisModel;
                if (SetProperty(ref _analysisModel, value)) {
                    old?.Dispose();
                }
            }
        }
        private DimsAnalysisModel _analysisModel;

        public DimsAlignmentModel AlignmentModel {
            get => _alignmentModel;
            private set {
                var old = _alignmentModel;
                if (SetProperty(ref _alignmentModel, value)) {
                    old?.Dispose();
                }
            }
        }
        private DimsAlignmentModel _alignmentModel;

        public IDataProviderFactory<AnalysisFileBean> ProviderFactory { get; }
        private IDataProviderFactory<AnalysisFileBeanModel> ProviderFactory2 => ProviderFactory.ContraMap((AnalysisFileBeanModel file) => file.File);

        public AlignmentResultExportModel AlignmentResultExportModel { get; }

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

        private IAnnotationProcess BuildEadLipidomicsAnnotationProcess(DataBaseStorage storage, DataBaseMapper mapper, ParameterBase parameter) {
            var containerPairs = new List<(IAnnotationQueryFactory<IAnnotationQuery>, IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>)>();
            foreach (var annotators in storage.MetabolomicsDataBases) {
                containerPairs.AddRange(annotators.Pairs.Select(annotator => (new AnnotationQueryFactory(annotator.SerializableAnnotator, parameter.PeakPickBaseParam) as IAnnotationQueryFactory<IAnnotationQuery>, annotator.ConvertToAnnotatorContainer())));
            }
            var eadAnnotationQueryFactoryTriple = new List<(IAnnotationQueryFactory<ICallableAnnotationQuery<MsScanMatchResult>>, IMatchResultEvaluator<MsScanMatchResult>, MsRefSearchParameterBase)>();
            foreach (var annotators in storage.EadLipidomicsDatabases) {
                eadAnnotationQueryFactoryTriple.AddRange(annotators.Pairs.Select(annotator => (new AnnotationQueryWithReferenceFactory(mapper, annotator.SerializableAnnotator, parameter.PeakPickBaseParam) as IAnnotationQueryFactory<ICallableAnnotationQuery<MsScanMatchResult>>, annotator.SerializableAnnotator as IMatchResultEvaluator<MsScanMatchResult>, annotator.SearchParameter)));
            }
            return new EadLipidomicsAnnotationProcess<IAnnotationQuery>(containerPairs, eadAnnotationQueryFactoryTriple, mapper);
        }

        public override async Task RunAsync(ProcessOption option, CancellationToken token) {
            // Storage.DataBaseMapper = BuildDataBaseMapper(Storage.DataBases);
            _matchResultEvaluator = FacadeMatchResultEvaluator.FromDataBases(Storage.DataBases);
            if (Storage.Parameter.TargetOmics == TargetOmics.Lipidomics && (Storage.Parameter.CollistionType == CollisionType.EIEIO || Storage.Parameter.CollistionType == CollisionType.OAD)) {
                _annotationProcess = BuildEadLipidomicsAnnotationProcess(Storage.DataBases, Storage.DataBaseMapper, Storage.Parameter);
            }
            else {
                _annotationProcess = BuildAnnotationProcess(Storage.DataBases);
            }

            var processOption = option;
            // Run Identification
            if (processOption.HasFlag(ProcessOption.Identification) || processOption.HasFlag(ProcessOption.PeakSpotting)) {
                if (!RunAnnotationAll(Storage.AnalysisFiles, Storage.Parameter.ProcessBaseParam)) {
                    return;
                }
            }

            // Run Alignment
            if (processOption.HasFlag(ProcessOption.Alignment)) {
                if (!RunAlignmentProcess()) {
                    return;
                }
            }

            await LoadAnalysisFileAsync(AnalysisFileModelCollection.AnalysisFiles.FirstOrDefault(), token).ConfigureAwait(false);
        }

        private bool RunAnnotationAll(List<AnalysisFileBean> analysisFiles, ProcessBaseParameter parameter) {
            var request = new ProgressBarMultiContainerRequest(
                async vm =>
                {
                    var tasks = new List<Task>();
                    var usable = Math.Max(parameter.UsableNumThreads / 2, 1);
                    using (var sem = new SemaphoreSlim(usable, usable)) {
                        foreach ((var analysisfile, var pb) in analysisFiles.Zip(vm.ProgressBarVMs)) {
                            var task = Task.Run(async () =>
                            {
                                await sem.WaitAsync();
                                try {
                                    ProcessFile.Run(analysisfile, ProviderFactory.Create(analysisfile), Storage, _annotationProcess, _matchResultEvaluator, reportAction: (int v) => pb.CurrentValue = v);
                                    vm.Increment();
                                }
                                finally {
                                    sem.Release();
                                }
                            });
                            tasks.Add(task);
                        }
                        await Task.WhenAll(tasks).ConfigureAwait(false);
                    }
                },
                analysisFiles.Select(file => file.AnalysisFileName).ToArray());
            _broker.Publish(request);
            return request.Result ?? false;
        }

        public bool RunAlignmentProcess() {
            var request = new ProgressBarRequest("Process alignment..", isIndeterminate: true,
                async _ =>
                {
                    AlignmentProcessFactory aFactory = new DimsAlignmentProcessFactory(Storage, _matchResultEvaluator);
                    var alignmentFile = Storage.AlignmentFiles.Last();
                    var aligner = aFactory.CreatePeakAligner();
                    aligner.ProviderFactory = ProviderFactory;
                    var result = await Task.Run(() => aligner.Alignment(Storage.AnalysisFiles, alignmentFile, CHROMATOGRAM_SPOT_SERIALIZER)).ConfigureAwait(false);
                    result.Save(alignmentFile);
                    MsdecResultsWriter.Write(alignmentFile.SpectraFilePath, LoadRepresentativeDeconvolutions(Storage, result.AlignmentSpotProperties).ToList());
                });
            _broker.Publish(request);
            return request.Result ?? false;
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
                    var peakID = spot.AlignedPeakProperties[repID].GetMSDecResultID();
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

        protected override IAnalysisModel LoadAnalysisFileCore(AnalysisFileBeanModel analysisFile) {
            if (AnalysisModel != null) {
                AnalysisModel.Dispose();
                Disposables.Remove(AnalysisModel);
            }
            return AnalysisModel = new DimsAnalysisModel(
                analysisFile,
                ProviderFactory2.Create(analysisFile),
                _matchResultEvaluator,
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
                _matchResultEvaluator,
                Storage.DataBaseMapper,
                Storage.Parameter,
                Storage.AnalysisFiles,
                AnalysisFileModelCollection,
                PeakFilterModel,
                _broker).AddTo(Disposables);
        }
    }
}
