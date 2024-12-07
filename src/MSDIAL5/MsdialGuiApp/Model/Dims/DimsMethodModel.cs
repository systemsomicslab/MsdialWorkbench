using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Export;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Graphics.UI.ProgressBar;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialDimsCore;
using CompMs.MsdialDimsCore.Algorithm.Alignment;
using CompMs.MsdialDimsCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Dims
{
    internal sealed class DimsMethodModel : MethodModelBase
    {
        static DimsMethodModel() {
            CHROMATOGRAM_SPOT_SERIALIZER = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.Mz)!;
        }

        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> CHROMATOGRAM_SPOT_SERIALIZER;
        private readonly FilePropertiesModel _fileProperties;
        private readonly IMessageBroker _broker;
        private readonly PeakSpotFiltering<AlignmentSpotPropertyModel> _spotFiltering;
        private readonly PeakSpotFiltering<ChromatogramPeakFeatureModel> _peakFiltering;
        private FacadeMatchResultEvaluator _matchResultEvaluator;
        private readonly MsfinderSearcherFactory _msfinderSearcherFactory;

        public DimsMethodModel(
            IMsdialDataStorage<MsdialDimsParameter> storage,
            AnalysisFileBeanModelCollection analysisFileBeanModelCollection,
            AlignmentFileBeanModelCollection alignmentFiles,
            FilePropertiesModel fileProperties,
            StudyContextModel studyContext,
            IMessageBroker broker)
            : base(analysisFileBeanModelCollection, alignmentFiles, fileProperties) {
            Storage = storage;
            _fileProperties = fileProperties ?? throw new ArgumentNullException(nameof(fileProperties));
            StudyContext = studyContext;
            _broker = broker;
            _matchResultEvaluator = FacadeMatchResultEvaluator.FromDataBases(storage.DataBases);
            PeakFilterModel = new PeakFilterModel(DisplayFilter.All & ~DisplayFilter.CcsMatched);
            ProviderFactory = storage.Parameter.ProviderFactoryParameter.Create(retry: 5, isGuiProcess: true);
            List<AnalysisFileBean> analysisFiles = analysisFileBeanModelCollection.AnalysisFiles.Select(f => f.File).ToList();
            var filterEnabled = FilterEnableStatus.All & ~FilterEnableStatus.Rt & ~FilterEnableStatus.Dt & ~FilterEnableStatus.Protein;
            if (storage.Parameter.TargetOmics == TargetOmics.Proteomics) {
                filterEnabled |= FilterEnableStatus.Protein;
            }
            _spotFiltering = new PeakSpotFiltering<AlignmentSpotPropertyModel>(filterEnabled).AddTo(Disposables);
            _peakFiltering = new PeakSpotFiltering<ChromatogramPeakFeatureModel>(filterEnabled).AddTo(Disposables);
            var filter = _spotFiltering.CreateFilter(PeakFilterModel, _matchResultEvaluator.Contramap((AlignmentSpotPropertyModel spot) => spot.ScanMatchResult), filterEnabled);

            DimsAlignmentMetadataAccessorFactory metadataAccessorFactory = new DimsAlignmentMetadataAccessorFactory(storage.DataBaseMapper, storage.Parameter);
            var stats = new List<StatsValue> { StatsValue.Average, StatsValue.Stdev, };
            var currentAlignmentResult = this.ObserveProperty(m => m.AlignmentModel).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            AlignmentFilesForExport alignmentFilesForExport = new AlignmentFilesForExport(alignmentFiles.Files, this.ObserveProperty(m => m.AlignmentFile)).AddTo(Disposables);
            var isNormalized = alignmentFilesForExport.CanExportNormalizedData(currentAlignmentResult.Select(r => r?.NormalizationSetModel.IsNormalized ?? Observable.Return(false)).Switch()).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            AlignmentPeakSpotSupplyer peakSpotSupplyer = new AlignmentPeakSpotSupplyer(currentAlignmentResult, filter);
            var peakGroup = new AlignmentExportGroupModel(
                "Peaks",
                new ExportMethod(
                    analysisFiles,
                    ExportFormat.Tsv,
                    ExportFormat.Csv),
                new[]
                {
                    new ExportType("Raw data (Height)", new LegacyQuantValueAccessor("Height", storage.Parameter), "Height", stats, isSelected: true),
                    new ExportType("Raw data (Area)", new LegacyQuantValueAccessor("Area", storage.Parameter), "Area", stats),
                    new ExportType("Normalized data (Height)", new LegacyQuantValueAccessor("Normalized height", storage.Parameter), "NormalizedHeight", stats, isNormalized),
                    new ExportType("Normalized data (Area)", new LegacyQuantValueAccessor("Normalized area", storage.Parameter), "NormalizedArea", stats, isNormalized),
                    new ExportType("Alignment ID", new LegacyQuantValueAccessor("ID", storage.Parameter), "PeakID"),
                    new ExportType("m/z", new LegacyQuantValueAccessor("MZ", storage.Parameter), "Mz"),
                    new ExportType("S/N", new LegacyQuantValueAccessor("SN", storage.Parameter), "SN"),
                    new ExportType("MS/MS included", new LegacyQuantValueAccessor("MSMS", storage.Parameter), "MsmsIncluded"),
                    new ExportType("Identification method", new AnnotationMethodAccessor(), "IdentificationMethod"),
                },
                new AccessPeakMetaModel(metadataAccessorFactory),
                new AccessFileMetaModel(fileProperties).AddTo(Disposables),
                new[]
                {
                    ExportspectraType.deconvoluted,
                },
                peakSpotSupplyer);
            var spectraGroup = new AlignmentSpectraExportGroupModel(
                new[]
                {
                    ExportspectraType.deconvoluted,
                },
                peakSpotSupplyer,
                new AlignmentSpectraExportFormat("Msp", "msp", new AlignmentMspExporter(storage.DataBaseMapper, storage.Parameter)),
                new AlignmentSpectraExportFormat("Mgf", "mgf", new AlignmentMgfExporter()),
                new AlignmentSpectraExportFormat("Mat", "mat", new AlignmentMatExporter(storage.DataBaseMapper, storage.Parameter)));
            var spectraAndReference = new AlignmentMatchedSpectraExportModel(peakSpotSupplyer, storage.DataBaseMapper, analysisFileBeanModelCollection.IncludedAnalysisFiles, CompoundSearcherCollection.BuildSearchers(storage.DataBases, storage.DataBaseMapper));

            AlignmentResultExportModel = new AlignmentResultExportModel(new IAlignmentResultExportModel[] { peakGroup, spectraGroup, spectraAndReference, }, alignmentFilesForExport, peakSpotSupplyer, storage.Parameter.DataExportParam, broker);

            ParameterExportModel = new ParameterExportModel(storage.DataBases, storage.Parameter, broker);

            _msfinderSearcherFactory = new MsfinderSearcherFactory(storage.DataBases, storage.DataBaseMapper, storage.Parameter, "MS-FINDER").AddTo(Disposables);
            
            AlignmentPeakSpotSupplyer peakSpotSupplyerForMsfinder = new AlignmentPeakSpotSupplyer(currentAlignmentResult, filter)
            {
                UseFilter = true,
            };
            var currentAlignmentFile = this.ObserveProperty(m => (IAlignmentModel)m.AlignmentModel).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            var exportMatForMsfinder = new AlignmentSpectraExportGroupModel(
                new[]
                {
                    ExportspectraType.deconvoluted,
                },
                peakSpotSupplyerForMsfinder,
                new AlignmentSpectraExportFormat("Mat", "mat", new AlignmentMatExporter(storage.DataBaseMapper, storage.Parameter))
                {
                    IsSelected = true,
                })
            {
                ExportIndividually = true,
            };
            MsfinderParameterSetting = MsfinderParameterSetting.CreateSetting(storage.Parameter.ProjectParam);
            InternalMsfinderSettingModel = new InternalMsfinderSettingModel(MsfinderParameterSetting, exportMatForMsfinder, currentAlignmentFile);
        }

        public PeakFilterModel PeakFilterModel { get; }
        public IMsdialDataStorage<MsdialDimsParameter> Storage { get; }
        public StudyContextModel StudyContext { get; }
        public MsfinderParameterSetting MsfinderParameterSetting { get; }
        public InternalMsfinderSettingModel InternalMsfinderSettingModel { get; }

        public DimsAnalysisModel? AnalysisModel {
            get => _analysisModel;
            private set {
                var old = _analysisModel;
                if (SetProperty(ref _analysisModel, value)) {
                    old?.Dispose();
                }
            }
        }
        private DimsAnalysisModel? _analysisModel;

        public DimsAlignmentModel? AlignmentModel {
            get => _alignmentModel;
            private set {
                var old = _alignmentModel;
                if (SetProperty(ref _alignmentModel, value)) {
                    old?.Dispose();
                }
            }
        }
        private DimsAlignmentModel? _alignmentModel;

        public IDataProviderFactory<AnalysisFileBean> ProviderFactory { get; }
        private IDataProviderFactory<AnalysisFileBeanModel> ProviderFactory2 => ProviderFactory.ContraMap((AnalysisFileBeanModel file) => file.File);

        public AlignmentResultExportModel AlignmentResultExportModel { get; }
        public ParameterExportModel ParameterExportModel { get; }

        private IAnnotationProcess BuildAnnotationProcess() {
            var queryFatoires = Storage.CreateAnnotationQueryFactoryStorage();
            return new StandardAnnotationProcess(queryFatoires.MoleculeQueryFactories, _matchResultEvaluator, Storage.DataBaseMapper);
        }

        private IAnnotationProcess BuildEadLipidomicsAnnotationProcess() {
            var queryFactories = Storage.CreateAnnotationQueryFactoryStorage();
            return new EadLipidomicsAnnotationProcess(queryFactories.MoleculeQueryFactories, queryFactories.SecondQueryFactories, Storage.DataBaseMapper, _matchResultEvaluator);
        }

        public override async Task RunAsync(ProcessOption processOption, CancellationToken token) {

            var parameter = Storage.Parameter;
            var starttimestamp = DateTime.Now.ToString("yyyyMMddHHmm");
            var stopwatch = Stopwatch.StartNew();


            _matchResultEvaluator = FacadeMatchResultEvaluator.FromDataBases(Storage.DataBases);
            IAnnotationProcess annotationProcess;
            if (Storage.Parameter.TargetOmics == TargetOmics.Lipidomics && 
                (Storage.Parameter.CollistionType == CollisionType.EIEIO || Storage.Parameter.CollistionType == CollisionType.OAD || Storage.Parameter.CollistionType == CollisionType.EID)) {
                annotationProcess = BuildEadLipidomicsAnnotationProcess();
            }
            else {
                annotationProcess = BuildAnnotationProcess();
            }

            // Run Identification
            if (processOption.HasFlag(ProcessOption.Identification)) {
                var usable = Math.Max(Storage.Parameter.ProcessBaseParam.UsableNumThreads / 2, 1);
                var processor = new ProcessFile(ProviderFactory, Storage, annotationProcess, _matchResultEvaluator);
                var runner = new ProcessRunner(processor, usable);
                if (!RunProcessAll(Storage.AnalysisFiles, runner, processOption)) {
                    return;
                }
            }

            // Run Alignment
            if (processOption.HasFlag(ProcessOption.Alignment)) {
                if (!RunAlignmentProcess()) {
                    return;
                }
            }

            stopwatch.Stop();
            var ts = stopwatch.Elapsed;
            AutoParametersSave(starttimestamp, ts, parameter);

            await LoadAnalysisFileAsync(AnalysisFileModelCollection.AnalysisFiles.FirstOrDefault(), token).ConfigureAwait(false);
        }

        private bool RunProcessAll(List<AnalysisFileBean> analysisFiles, ProcessRunner runner, ProcessOption processOption) {
            var request = new ProgressBarMultiContainerRequest(
                vm => runner.RunAllAsync(analysisFiles, processOption, vm.ProgressBarVMs.Select(vm_ => new Progress<int>(v => vm_.CurrentValue = v)), vm.Increment, default),
                analysisFiles.Select(file => file.AnalysisFileName).ToArray());
            _broker.Publish(request);
            return request.Result ?? false;
        }

        public bool RunAlignmentProcess() {
            var request = new ProgressBarRequest("Process alignment..", isIndeterminate: true,
                async _ =>
                {
                    AlignmentProcessFactory aFactory = new DimsAlignmentProcessFactory(Storage, _matchResultEvaluator);
                    var alignmentFileModel = AlignmentFiles.Files.Last();
                    var aligner = aFactory.CreatePeakAligner();
                    aligner.ProviderFactory = ProviderFactory;
                    var result = await Task.Run(() => alignmentFileModel.RunAlignment(aligner, CHROMATOGRAM_SPOT_SERIALIZER)).ConfigureAwait(false);

                    var tasks = new[]
                    {
                        alignmentFileModel.SaveAlignmentResultAsync(result),
                        alignmentFileModel.SaveMSDecResultsAsync(alignmentFileModel.LoadMSDecResultsFromEachFiles(result.AlignmentSpotProperties)),
                    };
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                });
            _broker.Publish(request);
            return request.Result ?? false;
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
                PeakFilterModel,
                _peakFiltering,
                _fileProperties,
                _msfinderSearcherFactory,
                _broker).AddTo(Disposables);
        }

        protected override IAlignmentModel LoadAlignmentFileCore(AlignmentFileBeanModel alignmentFileModel) {
            if (AlignmentModel != null) {
                AlignmentModel.Dispose();
                Disposables.Remove(AlignmentModel);
            }
            return AlignmentModel = new DimsAlignmentModel(
                alignmentFileModel,
                Storage.DataBases,
                _matchResultEvaluator,
                Storage.DataBaseMapper,
                _fileProperties,
                Storage.Parameter,
                Storage.AnalysisFiles,
                AnalysisFileModelCollection,
                PeakFilterModel,
                _spotFiltering,
                _msfinderSearcherFactory,
                _broker).AddTo(Disposables);
        }
    }
}
