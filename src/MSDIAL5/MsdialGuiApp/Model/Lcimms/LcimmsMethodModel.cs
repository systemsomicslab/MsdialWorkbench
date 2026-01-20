using CompMs.App.Msdial.Model.Chart;
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
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcImMsApi.Algorithm;
using CompMs.MsdialLcImMsApi.Algorithm.Alignment;
using CompMs.MsdialLcImMsApi.Algorithm.Annotation;
using CompMs.MsdialLcImMsApi.Export;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcImMsApi.Process;
using CompMs.Raw.Abstractions;
using CompMs.RawDataHandler.DataProvider;
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

namespace CompMs.App.Msdial.Model.Lcimms
{
    internal sealed class LcimmsMethodModel : MethodModelBase
    {
        static LcimmsMethodModel() {
            chromatogramSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.Drift)!;
        }

        private readonly MsfinderSearcherFactory _msfinderSearcherFactory;
        public LcimmsMethodModel(AnalysisFileBeanModelCollection analysisFileBeanModelCollection, AlignmentFileBeanModelCollection alignmentFileBeanModelCollection, IMsdialDataStorage<MsdialLcImMsParameter> storage, FilePropertiesModel fileProperties, StudyContextModel studyContext, IMessageBroker broker)
            : base(analysisFileBeanModelCollection, alignmentFileBeanModelCollection, fileProperties) {
            if (storage is null) {
                throw new ArgumentNullException(nameof(storage));
            }

            Storage = storage;
            _fileProperties = fileProperties ?? throw new ArgumentNullException(nameof(fileProperties));
            _studyContext = studyContext;
            _broker = broker;
            providerFactory = new StandardDataProviderFactory().ContraMap((AnalysisFileBean file) => (file.AnalysisFilePath, file.RetentionTimeCorrectionBean.PredictedRt));
            accProviderFactory = new LcimmsAccumulateDataProviderFactory<AnalysisFileBean>(providerFactory);
            matchResultEvaluator = FacadeMatchResultEvaluator.FromDataBases(storage.DataBases);
            PeakFilterModel = new PeakFilterModel(DisplayFilter.All);
            AccumulatedPeakFilterModel = new PeakFilterModel(DisplayFilter.All & ~DisplayFilter.CcsMatched);

            List<AnalysisFileBean> analysisFiles = analysisFileBeanModelCollection.AnalysisFiles.Select(f => f.File).ToList();
            var filterEnabled = FilterEnableStatus.All & ~FilterEnableStatus.Protein;
            if (storage.Parameter.TargetOmics == TargetOmics.Proteomics) {
                filterEnabled |= FilterEnableStatus.Protein;
            }
            _peakSpotFiltering = new PeakSpotFiltering<AlignmentSpotPropertyModel>(filterEnabled).AddTo(Disposables);
            var filter = _peakSpotFiltering.CreateFilter(PeakFilterModel, matchResultEvaluator.Contramap((AlignmentSpotPropertyModel spot) => spot.ScanMatchResult), filterEnabled);
            var stats = new List<StatsValue> { StatsValue.Average, StatsValue.Stdev, };
            var metadataAccessorFactory = new LcimmsAlignmentMetadataAccessorFactory(storage.DataBaseMapper, storage.Parameter);
            var currentAlignmentResult = this.ObserveProperty(m => m.AlignmentModel).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            AlignmentFilesForExport alignmentFilesForExport = new AlignmentFilesForExport(alignmentFileBeanModelCollection.Files, this.ObserveProperty(m => m.AlignmentFile)).AddTo(Disposables);
            var isNormalized = alignmentFilesForExport.CanExportNormalizedData(currentAlignmentResult.Select(r => r?.NormalizationSetModel.IsNormalized ?? Observable.Return(false)).Switch()).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            AlignmentPeakSpotSupplyer peakSpotSupplyer = new AlignmentPeakSpotSupplyer(currentAlignmentResult, filter);
            ExportType[] quantTypes =
                [
                    new ExportType("Raw data (Height)", new LegacyQuantValueAccessor("Height", storage.Parameter), "Height", stats, true),
                    new ExportType("Raw data (Area)", new LegacyQuantValueAccessor("Area", storage.Parameter), "Area", stats),
                    new ExportType("Normalized data (Height)", new LegacyQuantValueAccessor("Normalized height", storage.Parameter), "NormalizedHeight", stats, isNormalized),
                    new ExportType("Normalized data (Area)", new LegacyQuantValueAccessor("Normalized area", storage.Parameter), "NormalizedArea", stats, isNormalized),
                    new ExportType("Peak ID", new LegacyQuantValueAccessor("ID", storage.Parameter), "PeakID"),
                    new ExportType("m/z", new LegacyQuantValueAccessor("MZ", storage.Parameter), "Mz"),
                    new ExportType("Retention time", new LegacyQuantValueAccessor("RT", storage.Parameter), "Rt"),
                    new ExportType("Mobility", new LegacyQuantValueAccessor("Mobility", storage.Parameter), "Mobility"),
                    new ExportType("CCS", new LegacyQuantValueAccessor("CCS", storage.Parameter), "CCS"),
                    new ExportType("S/N", new LegacyQuantValueAccessor("SN", storage.Parameter), "SN"),
                    new ExportType("MS/MS included", new LegacyQuantValueAccessor("MSMS", storage.Parameter), "MsmsIncluded"),
                    new ExportType("Identification method", new AnnotationMethodAccessor(), "IdentificationMethod"),
                ];
            AccessPeakMetaModel peakMeta = new(metadataAccessorFactory);
            AccessFileMetaModel fileMeta = new AccessFileMetaModel(fileProperties).AddTo(Disposables);
            var peakGroup = new AlignmentExportGroupModel(
                "Peaks",
                new ExportMethod(analysisFiles, ExportFormat.Tsv, ExportFormat.Csv ),
                quantTypes,
                peakMeta,
                fileMeta,
                [ ExportspectraType.deconvoluted, ],
                peakSpotSupplyer);
            var spectraGroup = new AlignmentSpectraExportGroupModel(
                [ ExportspectraType.deconvoluted, ],
                peakSpotSupplyer,
                new AlignmentSpectraExportFormat("Msp", "msp", new AlignmentMspExporter(storage.DataBaseMapper, storage.Parameter)),
                new AlignmentSpectraExportFormat("Mgf", "mgf", new AlignmentMgfExporter()),
                new AlignmentSpectraExportFormat("Mat", "mat", new AlignmentMatExporter(storage.DataBaseMapper, storage.Parameter)));
            var gnps = new AlignmentGnpsExportModel("GNPS", quantTypes, new GnpsMetadataAccessor(storage.DataBaseMapper, storage.Parameter), peakMeta.GetAccessor(), fileMeta.GetAccessor(), analysisFileBeanModelCollection);
            var spectraAndReference = new AlignmentMatchedSpectraExportModel(peakSpotSupplyer, storage.DataBaseMapper, analysisFileBeanModelCollection.IncludedAnalysisFiles, CompoundSearcherCollection.BuildSearchers(storage.DataBases, storage.DataBaseMapper));

            AlignmentResultExportModel = new AlignmentResultExportModel([peakGroup, spectraGroup, gnps, spectraAndReference,], alignmentFilesForExport, peakSpotSupplyer, storage.Parameter.DataExportParam, broker);

            ParameterExportModel = new ParameterExportModel(storage.DataBases, storage.Parameter, broker);

            AlignmentPeakSpotSupplyer peakSpotSupplyerForMsfinder = new AlignmentPeakSpotSupplyer(currentAlignmentResult, filter)
            {
                UseFilter = true,
            };
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

            var currentAlignmentFile = this.ObserveProperty(m => (IAlignmentModel?)m.AlignmentModel).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            _msfinderSearcherFactory = new MsfinderSearcherFactory(storage.DataBases, storage.DataBaseMapper, storage.Parameter, "MS-FINDER").AddTo(Disposables);

            MsfinderSettingParameter = MsfinderParameterSetting.CreateSetting(storage.Parameter.ProjectParam);
            InternalMsfinderSettingModel = new InternalMsfinderSettingModel(MsfinderSettingParameter, exportMatForMsfinder, currentAlignmentFile);
            var currentFileResult = this.ObserveProperty(m => m.AnalysisModel).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            MolecularNetworkingSettingModel = new MolecularNetworkingSettingModel(storage.Parameter.MolecularSpectrumNetworkingBaseParam, currentFileResult, currentAlignmentResult).AddTo(Disposables);
        }

        private FacadeMatchResultEvaluator matchResultEvaluator;

        public IMsdialDataStorage<MsdialLcImMsParameter> Storage { get; }

        public LcimmsAnalysisModel? AnalysisModel {
            get => analysisModel;
            private set => SetProperty(ref analysisModel, value);
        }
        private LcimmsAnalysisModel? analysisModel;

        public LcimmsAlignmentModel? AlignmentModel {
            get => alignmentModel;
            private set => SetProperty(ref alignmentModel, value);
        }
        private LcimmsAlignmentModel? alignmentModel;

        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer;
        private readonly IDataProviderFactory<AnalysisFileBean> providerFactory;
        private readonly IDataProviderFactory<AnalysisFileBean> accProviderFactory;
        private readonly FilePropertiesModel _fileProperties;
        private readonly StudyContextModel _studyContext;
        private readonly IMessageBroker _broker;
        private readonly PeakSpotFiltering<AlignmentSpotPropertyModel> _peakSpotFiltering;

        public PeakFilterModel AccumulatedPeakFilterModel { get; }
        public PeakFilterModel PeakFilterModel { get; }
        public AlignmentResultExportModel AlignmentResultExportModel { get; }
        public ParameterExportModel ParameterExportModel { get; }
        public InternalMsfinderSettingModel InternalMsfinderSettingModel { get; }
        public MsfinderParameterSetting MsfinderSettingParameter { get; }

        public MolecularNetworkingSettingModel MolecularNetworkingSettingModel { get; }
        protected override IAnalysisModel LoadAnalysisFileCore(AnalysisFileBeanModel analysisFile) {
            if (AnalysisModel != null) {
                AnalysisModel.Dispose();
                Disposables.Remove(AnalysisModel);
            }
            return AnalysisModel = new LcimmsAnalysisModel(
                analysisFile,
                providerFactory.Create(analysisFile.File),
                accProviderFactory.Create(analysisFile.File),
                Storage.DataBases,
                matchResultEvaluator,
                Storage.DataBaseMapper,
                Storage.Parameter,
                PeakFilterModel,
                AccumulatedPeakFilterModel,
                _fileProperties,
                _msfinderSearcherFactory,
                _broker)
            .AddTo(Disposables);
        }

        protected override IAlignmentModel LoadAlignmentFileCore(AlignmentFileBeanModel alignmentFileModel) {
            if (AlignmentModel != null) {
                AlignmentModel.Dispose();
                Disposables.Remove(AlignmentModel);
            }
            return AlignmentModel = new LcimmsAlignmentModel(
                alignmentFileModel,
                AnalysisFileModelCollection,
                matchResultEvaluator,
                Storage.DataBases,
                Storage.DataBaseMapper,
                _fileProperties,
                Storage.Parameter,
                Storage.AnalysisFiles,
                _peakSpotFiltering,
                PeakFilterModel,
                AccumulatedPeakFilterModel,
                _msfinderSearcherFactory,
                _broker)
            .AddTo(Disposables);
        }

        public override async Task RunAsync(ProcessOption processOption, CancellationToken token) {

            var parameter = Storage.Parameter;
            var starttimestamp = DateTime.Now.ToString("yyyyMMddHHmm");
            var stopwatch = Stopwatch.StartNew();

            // Set analysis param
            var annotationProcess = BuildAnnotationProcess();

            // Run Identification
            if (processOption.HasFlag(ProcessOption.Identification)) {
                int usable = Math.Max(Storage.Parameter.ProcessBaseParam.UsableNumThreads / 2, 1);
                FileProcess processor = new FileProcess(providerFactory, accProviderFactory, annotationProcess, matchResultEvaluator, Storage);
                var runner = new ProcessRunner(processor, usable);
                if (!RunFileProcess(Storage.AnalysisFiles, runner, processOption)) {
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

        private IAnnotationProcess BuildAnnotationProcess() {
            return new LcimmsStandardAnnotationProcess(Storage.CreateAnnotationQueryFactoryStorage().MoleculeQueryFactories, matchResultEvaluator, Storage.DataBaseMapper);
        }

        private bool RunFileProcess(List<AnalysisFileBean> analysisFiles, ProcessRunner runner, ProcessOption processOption) {
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
                    AlignmentProcessFactory aFactory = new LcimmsAlignmentProcessFactory(Storage, matchResultEvaluator, providerFactory, accProviderFactory);
                    var alignmentFile = Storage.AlignmentFiles.Last();
                    var alignmentFileModel = AlignmentFiles.Files.Last();
                    var aligner = aFactory.CreatePeakAligner();
                    var result = await Task.Run(() => alignmentFileModel.RunAlignment(aligner, chromatogramSpotSerializer)).ConfigureAwait(false);
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

        public AnalysisResultExportModel ExportAnalysis()
        {
            var spectraTypes = new List<SpectraType>
            {
                new SpectraType(
                    ExportspectraType.deconvoluted,
                    new ChromatogramShapeMetadataAccessorDecorator(new LcimmsAnalysisMetadataAccessor(Storage.DataBaseMapper, Storage.Parameter, ExportspectraType.deconvoluted)),
                    providerFactory),
            };
            var spectraFormats = new[]
            {
                new SpectraFormat(ExportSpectraFileFormat.txt, new AnalysisCSVExporterFactory(separator: "\t")),
            };

            var models = new IMsdialAnalysisExport[]
            {
                new MsdialAnalysisTableExportModel(spectraTypes, spectraFormats, _broker),
                new SpectraTypeSelectableMsdialAnalysisExportModel(new Dictionary<ExportspectraType, IAnalysisExporter<ChromatogramPeakFeatureCollection>> {
                    [ExportspectraType.deconvoluted] = new AnalysisMspExporter(Storage.DataBaseMapper, Storage.Parameter),
                    [ExportspectraType.centroid] = new AnalysisMspExporter(Storage.DataBaseMapper, Storage.Parameter, file => new CentroidMsScanPropertyLoader(providerFactory.Create(file), Storage.Parameter.MS2DataType)),
                })
                {
                    FilePrefix = "Msp",
                    FileSuffix = "msp",
                    Label = "Nist format (*.msp)"
                },
                new SpectraTypeSelectableMsdialAnalysisExportModel(new Dictionary<ExportspectraType, IAnalysisExporter<ChromatogramPeakFeatureCollection>> {
                    [ExportspectraType.deconvoluted] = new AnalysisMgfExporter(file => new MSDecLoader(file.DeconvolutionFilePath, file.DeconvolutionFilePathList)),
                    [ExportspectraType.centroid] = new AnalysisMgfExporter(file => new CentroidMsScanPropertyLoader(providerFactory.Create(file), Storage.Parameter.MS2DataType)),
                })
                {
                    FilePrefix = "Mgf",
                    FileSuffix = "mgf",
                    Label = "MASCOT format (*.mgf)"
                },
                new MsdialAnalysisMassBankRecordExportModel(Storage.Parameter.ProjectParam, _studyContext),
            };
            return new AnalysisResultExportModel(AnalysisFileModelCollection, Storage.Parameter.ProjectParam.ProjectFolderPath, _broker, models);
        }

        public void SaveProject() {
            AlignmentModel?.SaveProject();
        }

        public async Task<CheckChromatogramsModel?> PrepareChromatogramsAsync(bool tic, bool bpc, bool highestEic, CancellationToken token) {
            var analysisModel = AnalysisModel;
            if (analysisModel is null) {
                return null;
            }

            var loadChromatogramsUsecase = analysisModel.LoadChromatogramsUsecase();
            loadChromatogramsUsecase.InsertTic = tic;
            loadChromatogramsUsecase.InsertBpc = bpc;
            loadChromatogramsUsecase.InsertHighestEic = highestEic;
            var model = new CheckChromatogramsModel(loadChromatogramsUsecase, analysisModel.AccumulateSpectraUsecase, analysisModel.CompoundSearcher, Storage.Parameter.AdvancedProcessOptionBaseParam, analysisModel.AnalysisFileModel, _broker);
            await model.UpdateAsync(token).ConfigureAwait(false);
            return model;
        }
    }
}
