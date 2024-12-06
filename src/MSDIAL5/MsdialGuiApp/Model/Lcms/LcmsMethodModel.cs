using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Export;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Model.Statistics;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Graphics.UI.ProgressBar;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.Algorithm.Alignment;
using CompMs.MsdialLcMsApi.Export;
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

namespace CompMs.App.Msdial.Model.Lcms
{

    internal sealed class LcmsMethodModel : MethodModelBase
    {
        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> CHROMATOGRAM_SPOT_SERIALIZER;

        static LcmsMethodModel() {
            CHROMATOGRAM_SPOT_SERIALIZER = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.RT)!;
        }

        private readonly IDataProviderFactory<AnalysisFileBean> _providerFactory;
        private readonly FilePropertiesModel _fileProperties;
        private readonly StudyContextModel _studyContext;
        private readonly IMessageBroker _broker;
        private readonly IMsdialDataStorage<MsdialLcmsParameter> _storage;
        private readonly FacadeMatchResultEvaluator _matchResultEvaluator;
        private readonly PeakSpotFiltering<AlignmentSpotPropertyModel> _spotFiltering;
        private readonly PeakSpotFiltering<ChromatogramPeakFeatureModel> _peakFiltering;
        private readonly MsfinderSearcherFactory _msfinderSearcherFactory;

        public LcmsMethodModel(
            AnalysisFileBeanModelCollection analysisFileBeanModelCollection,
            AlignmentFileBeanModelCollection alignmentFileBeanModelCollection,
            IMsdialDataStorage<MsdialLcmsParameter> storage,
            IDataProviderFactory<AnalysisFileBean> providerFactory,
            FilePropertiesModel fileProperties,
            StudyContextModel studyContext,
            IMessageBroker broker)
            : base(analysisFileBeanModelCollection, alignmentFileBeanModelCollection, fileProperties) {

            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _matchResultEvaluator = FacadeMatchResultEvaluator.FromDataBases(storage.DataBases);
            _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
            _fileProperties = fileProperties ?? throw new ArgumentNullException(nameof(fileProperties));
            _studyContext = studyContext;
            _broker = broker;
            PeakFilterModel = new PeakFilterModel(DisplayFilter.All & ~DisplayFilter.CcsMatched);
            CanShowProteinGroupTable = Observable.Return(storage.Parameter.TargetOmics == TargetOmics.Proteomics);

            List<AnalysisFileBean> analysisFiles = analysisFileBeanModelCollection.AnalysisFiles.Select(f => f.File).ToList();
            var filterEnabled = FilterEnableStatus.All & ~FilterEnableStatus.Dt & ~FilterEnableStatus.Protein;
            if (storage.Parameter.TargetOmics == TargetOmics.Proteomics) {
                filterEnabled |= FilterEnableStatus.Protein;
            }
            _peakFiltering = new PeakSpotFiltering<ChromatogramPeakFeatureModel>(filterEnabled).AddTo(Disposables);
            _spotFiltering = new PeakSpotFiltering<AlignmentSpotPropertyModel>(filterEnabled).AddTo(Disposables);
            var filter = _spotFiltering.CreateFilter(PeakFilterModel, _matchResultEvaluator.Contramap((AlignmentSpotPropertyModel spot) => spot.ScanMatchResult), filterEnabled);
            var currentAlignmentResult = this.ObserveProperty(m => m.AlignmentModel).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            AlignmentFilesForExport alignmentFilesForExport = new AlignmentFilesForExport(alignmentFileBeanModelCollection.Files, this.ObserveProperty(m => m.AlignmentFile)).AddTo(Disposables);

            var isNormalized = alignmentFilesForExport.CanExportNormalizedData(currentAlignmentResult.Select(r => r?.NormalizationSetModel.IsNormalized ?? Observable.Return(false)).Switch()).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            AlignmentPeakSpotSupplyer peakSpotSupplyer = new AlignmentPeakSpotSupplyer(currentAlignmentResult, filter);
            var stats = new List<StatsValue> { StatsValue.Average, StatsValue.Stdev, };
            var peakGroup = new AlignmentExportGroupModel(
                "Peaks",
                new ExportMethod(
                    analysisFiles,
                    ExportFormat.Tsv,
                    ExportFormat.Csv
                ),
                new[]
                {
                    new ExportType("Raw data (Height)", new LegacyQuantValueAccessor("Height", storage.Parameter), "Height", stats, true),
                    new ExportType("Raw data (Area)", new LegacyQuantValueAccessor("Area", storage.Parameter), "Area", stats),
                    new ExportType("Normalized data (Height)", new LegacyQuantValueAccessor("Normalized height", storage.Parameter), "NormalizedHeight", stats, isNormalized),
                    new ExportType("Normalized data (Area)", new LegacyQuantValueAccessor("Normalized area", storage.Parameter), "NormalizedArea", stats, isNormalized),
                    new ExportType("Peak ID", new LegacyQuantValueAccessor("ID", storage.Parameter), "PeakID"),
                    new ExportType("m/z", new LegacyQuantValueAccessor("MZ", storage.Parameter), "Mz"),
                    new ExportType("Retention time", new LegacyQuantValueAccessor("RT", storage.Parameter), "Rt"),
                    new ExportType("S/N", new LegacyQuantValueAccessor("SN", storage.Parameter), "SN"),
                    new ExportType("MS/MS included", new LegacyQuantValueAccessor("MSMS", storage.Parameter), "MsmsIncluded"),
                    new ExportType("Identification method", new AnnotationMethodAccessor(), "IdentificationMethod"),
                },
                new AccessPeakMetaModel(new LcmsAlignmentMetadataAccessorFactory(storage.DataBaseMapper, storage.Parameter)),
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
            var massBank = new AlignmentResultMassBankRecordExportModel(peakSpotSupplyer, storage.Parameter.ProjectParam, studyContext);
            var spectraAndReference = new AlignmentMatchedSpectraExportModel(peakSpotSupplyer, storage.DataBaseMapper, analysisFileBeanModelCollection.IncludedAnalysisFiles, CompoundSearcherCollection.BuildSearchers(storage.DataBases, storage.DataBaseMapper));
            var exportGroups = new List<IAlignmentResultExportModel> { peakGroup, spectraGroup, massBank, spectraAndReference, };
            if (storage.Parameter.TargetOmics == TargetOmics.Proteomics) {
                exportGroups.Add(new ProteinGroupExportModel(new ProteinGroupExporter(), analysisFiles));
            }


            AlignmentResultExportModel = new AlignmentResultExportModel(exportGroups, alignmentFilesForExport, peakSpotSupplyer, storage.Parameter.DataExportParam, broker);
            var currentFileResult = this.ObserveProperty(m => m.AnalysisModel).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            MolecularNetworkingSettingModel = new MolecularNetworkingSettingModel(storage.Parameter.MolecularSpectrumNetworkingBaseParam, currentFileResult, currentAlignmentResult).AddTo(Disposables);

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

            var currentAlignmentFile = this.ObserveProperty(m => (IAlignmentModel)m.AlignmentModel).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            var notameExportModel = new AlignmentExportGroupModel(
                "Peaks",
                new ExportMethod(analysisFiles, ExportFormat.Tsv) { IsLongFormat = false, },
                new[] {
                    new ExportType("Raw data (Height)", new LegacyQuantValueAccessor("Height", storage.Parameter), "Height", new List<StatsValue>(0), true),
                    new ExportType("Raw data (Area)", new LegacyQuantValueAccessor("Area", storage.Parameter), "Area", new List<StatsValue>(0)),
                    new ExportType("Normalized data (Height)", new LegacyQuantValueAccessor("Normalized height", storage.Parameter), "NormalizedHeight", new List<StatsValue>(0), isNormalized),
                    new ExportType("Normalized data (Area)", new LegacyQuantValueAccessor("Normalized area", storage.Parameter), "NormalizedArea", new List<StatsValue>(0), isNormalized),
                },
                new AccessPeakMetaModel(new IdentityAlignmentMetadataAccessorFactory(
                    new LcmsMetadataAccessor(storage.DataBaseMapper, storage.Parameter, trimSpectrumToExcelLimit: true)
                        .Insert("Ion mode", 34, (p, _) => {
                            switch (p.IonMode) {
                                case IonMode.Positive:
                                    return "RP_pos";
                                case IonMode.Negative:
                                    return "RP_neg";
                                default:
                                    return "null";
                            }
                        }))),
                new AccessFileMetaModel(fileProperties) { EnableMultiClass = true, NumberOfClasses = 2, }.AddTo(Disposables),
                new[] { ExportspectraType.deconvoluted, },
                peakSpotSupplyer);
            Notame = new Notame(alignmentFilesForExport, peakSpotSupplyer, notameExportModel, storage.Parameter.DataExportParam, storage.Parameter);

            _msfinderSearcherFactory = new MsfinderSearcherFactory(storage.DataBases, storage.DataBaseMapper, storage.Parameter, "MS-FINDER").AddTo(Disposables);

            MsfinderSettingParameter = MsfinderParameterSetting.CreateSetting(storage.Parameter.ProjectParam);
            InternalMsfinderSettingModel = new InternalMsfinderSettingModel(MsfinderSettingParameter, exportMatForMsfinder, currentAlignmentFile);
        }

        public InternalMsfinderSettingModel InternalMsfinderSettingModel { get; }
        public MsfinderParameterSetting MsfinderSettingParameter { get; }

        public PeakFilterModel PeakFilterModel { get; }

        public IObservable<bool> CanShowProteinGroupTable { get; }

        public LcmsAnalysisModel? AnalysisModel {
            get => _analysisModel;
            private set => SetProperty(ref _analysisModel, value);
        }
        private LcmsAnalysisModel? _analysisModel;

        public LcmsAlignmentModel? AlignmentModel {
            get => _alignmentModel;
            set => SetProperty(ref _alignmentModel, value);
        }
        private LcmsAlignmentModel? _alignmentModel;

        public AlignmentResultExportModel AlignmentResultExportModel { get; }
        public MolecularNetworkingSettingModel MolecularNetworkingSettingModel { get; }
        public ParameterExportModel ParameterExportModel { get; }

        protected override IAnalysisModel LoadAnalysisFileCore(AnalysisFileBeanModel analysisFile) {
            if (AnalysisModel != null) {
                AnalysisModel.Dispose();
                Disposables.Remove(AnalysisModel);
            }
            var provider = _providerFactory.Create(analysisFile.File);
            return AnalysisModel = new LcmsAnalysisModel(
                analysisFile,
                provider,
                _storage.DataBases,
                _storage.DataBaseMapper,
                _matchResultEvaluator,
                _storage.Parameter,
                PeakFilterModel,
                _peakFiltering,
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

            return AlignmentModel = new LcmsAlignmentModel(
                alignmentFileModel,
                _matchResultEvaluator,
                _storage.DataBases,
                _spotFiltering,
                PeakFilterModel,
                _storage.DataBaseMapper,
                _storage.Parameter,
                _fileProperties,
                _storage.AnalysisFiles,
                AnalysisFileModelCollection,
                _msfinderSearcherFactory,
                _broker)
            .AddTo(Disposables);
        }

        public override async Task RunAsync(ProcessOption processOption, CancellationToken token) {
            // Set analysis param
            var parameter = _storage.Parameter;
            var starttimestamp = DateTime.Now.ToString("yyyyMMddHHmm");
            var stopwatch = Stopwatch.StartNew();
            IAnnotationProcess annotationProcess;
            if (parameter.TargetOmics == TargetOmics.Proteomics) {
                annotationProcess = BuildProteoMetabolomicsAnnotationProcess();
            }
            else if(parameter.TargetOmics == TargetOmics.Lipidomics && 
                (parameter.CollistionType == CollisionType.EIEIO || parameter.CollistionType == CollisionType.OAD || parameter.CollistionType == CollisionType.EID)) {
                annotationProcess = BuildEadLipidomicsAnnotationProcess();
            }
            else {
                annotationProcess = BuildAnnotationProcess();
            }

            // Run Identification
            if (processOption.HasFlag(ProcessOption.Identification)) {
                var processor = new MsdialLcMsApi.Process.FileProcess(_providerFactory, _storage, annotationProcess, _matchResultEvaluator);
                var runner = new ProcessRunner(processor, Math.Max(1, _storage.Parameter.ProcessBaseParam.UsableNumThreads / 2));
                if (!ProcessFiles(_storage.AnalysisFiles, runner, processOption)) {
                    return;
                }
            }

            // Run second process
            if (parameter.TargetOmics == TargetOmics.Proteomics) {
                if (!ProcessSeccondAnnotaion4ShotgunProteomics(_storage))
                    return;
            } 
            
            // Run Alignment
            if (processOption.HasFlag(ProcessOption.Alignment)) {
                if (!ProcessAlignment(_storage))
                    return;
            }
            stopwatch.Stop();
            var ts = stopwatch.Elapsed;
            AutoParametersSave(starttimestamp, ts, parameter);

            await LoadAnalysisFileAsync(AnalysisFileModelCollection.AnalysisFiles.FirstOrDefault(), token).ConfigureAwait(false);
        }

        private IAnnotationProcess BuildAnnotationProcess() {
            return new StandardAnnotationProcess(_storage.CreateAnnotationQueryFactoryStorage().MoleculeQueryFactories, _matchResultEvaluator, _storage.DataBaseMapper);
        }

        private IAnnotationProcess BuildProteoMetabolomicsAnnotationProcess() {
            var queryFactories = _storage.CreateAnnotationQueryFactoryStorage();
            return new AnnotationProcessOfProteoMetabolomics(queryFactories.MoleculeQueryFactories, queryFactories.PeptideQueryFactories, _matchResultEvaluator, _storage.DataBaseMapper, _storage.DataBaseMapper);
        }

        private IAnnotationProcess BuildEadLipidomicsAnnotationProcess() {
            var queryFactories = _storage.CreateAnnotationQueryFactoryStorage();
            return new EadLipidomicsAnnotationProcess(queryFactories.MoleculeQueryFactories, queryFactories.SecondQueryFactories, _storage.DataBaseMapper, _matchResultEvaluator);
        }

        private bool ProcessFiles(List<AnalysisFileBean> analysisFiles, ProcessRunner runner, ProcessOption processOption) {
            var request = new ProgressBarMultiContainerRequest(
                vm_ => runner.RunAllAsync(
                    analysisFiles, processOption,
                    vm_.ProgressBarVMs.Select(pbvm => new Progress<int>(v => pbvm.CurrentValue = v)),
                    vm_.Increment,
                    default),
                analysisFiles.Select(file => file.AnalysisFileName).ToArray());
            _broker.Publish(request);
            return request.Result ?? false;
        }

        public bool ProcessSeccondAnnotaion4ShotgunProteomics(IMsdialDataStorage<MsdialLcmsParameter> storage) {
            var request = new ProgressBarRequest("Process second annotation..", isIndeterminate: false,
                async vm =>
                {
                    var proteomicsAnnotator = new ProteomeDataAnnotator();
                    var progress = new Progress<int>(v => vm.CurrentValue = v);
                    await Task.Run(() => proteomicsAnnotator.ExecuteSecondRoundAnnotationProcess(
                        storage.AnalysisFiles,
                        storage.DataBaseMapper,
                        _matchResultEvaluator,
                        storage.DataBases,
                        storage.Parameter,
                        progress)).ConfigureAwait(false);
                });
            _broker.Publish(request);
            return request.Result ?? false;
        }

        public bool ProcessAlignment(IMsdialDataStorage<MsdialLcmsParameter> storage) {
            var request = new ProgressBarRequest("Process alignment..", isIndeterminate: false,
                async vm =>
                {
                    var factory = new LcmsAlignmentProcessFactory(storage, _matchResultEvaluator)
                    {
                        Progress = new Progress<int>(v => vm.CurrentValue = v)
                    };

                    var aligner = factory.CreatePeakAligner();
                    aligner.ProviderFactory = _providerFactory; // TODO: I'll remove this later.

                    var alignmentFileModel = AlignmentFiles.Files.Last();
                    var result = await Task.Run(() => alignmentFileModel.RunAlignment(aligner, CHROMATOGRAM_SPOT_SERIALIZER)).ConfigureAwait(false);

                    if (storage.DataBases.ProteomicsDataBases.Any()) {
                        new ProteomeDataAnnotator().MappingToProteinDatabase(
                            alignmentFileModel.ProteinAssembledResultFilePath,
                            result,
                            storage.DataBases.ProteomicsDataBases,
                            storage.DataBaseMapper,
                            _matchResultEvaluator,
                            storage.Parameter);
                    }

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

        public AnalysisResultExportModel ExportAnalysis() {
            var spectraTypes = new List<SpectraType>
            {
                //new SpectraType(
                //    ExportspectraType.deconvoluted,
                //    new LcmsAnalysisMetadataAccessor(_storage.DataBaseMapper, _storage.Parameter, ExportspectraType.deconvoluted)),
                new SpectraType(
                    ExportspectraType.deconvoluted,
                    new ChromatogramShapeMetadataAccessorDecorator(new LcmsAnalysisMetadataAccessor(_storage.DataBaseMapper, _storage.Parameter, ExportspectraType.deconvoluted)),
                    _providerFactory),
                //new SpectraType(
                //    ExportspectraType.centroid,
                //    new LcmsAnalysisMetadataAccessor(_storage.DataBaseMapper, _storage.Parameter, ExportspectraType.centroid)),
                //new SpectraType(
                //    ExportspectraType.profile,
                //    new LcmsAnalysisMetadataAccessor(_storage.DataBaseMapper, _storage.Parameter, ExportspectraType.profile)),
            };
            var spectraFormats = new[]
            {
                new SpectraFormat(ExportSpectraFileFormat.txt, new AnalysisCSVExporterFactory(separator: "\t")),
            };

            var models = new IMsdialAnalysisExport[]
            {
                new MsdialAnalysisTableExportModel(spectraTypes, spectraFormats, _broker),
                new SpectraTypeSelectableMsdialAnalysisExportModel(new Dictionary<ExportspectraType, IAnalysisExporter<ChromatogramPeakFeatureCollection>> {
                    [ExportspectraType.deconvoluted] = new AnalysisMspExporter(_storage.DataBaseMapper, _storage.Parameter),
                    [ExportspectraType.centroid] = new AnalysisMspExporter(_storage.DataBaseMapper, _storage.Parameter, file => new CentroidMsScanPropertyLoader(_providerFactory.Create(file), _storage.Parameter.MS2DataType)),
                })
                {
                    FilePrefix = "Msp",
                    FileSuffix = "msp",
                    Label = "Nist format (*.msp)"
                },
                new SpectraTypeSelectableMsdialAnalysisExportModel(new Dictionary<ExportspectraType, IAnalysisExporter<ChromatogramPeakFeatureCollection>> {
                    [ExportspectraType.deconvoluted] = new AnalysisMgfExporter(file => new MSDecLoader(file.DeconvolutionFilePath, file.DeconvolutionFilePathList)),
                    [ExportspectraType.centroid] = new AnalysisMgfExporter(file => new CentroidMsScanPropertyLoader(_providerFactory.Create(file), _storage.Parameter.MS2DataType)),
                })
                {
                    FilePrefix = "Mgf",
                    FileSuffix = "mgf",
                    Label = "MASCOT format (*.mgf)"
                },
                new MsdialAnalysisMassBankRecordExportModel(_storage.Parameter.ProjectParam, _studyContext),
            };
            return new AnalysisResultExportModel(AnalysisFileModelCollection, _storage.Parameter.ProjectParam.ProjectFolderPath, _broker, models);
        }

        public CheckChromatogramsModel? ShowChromatograms(bool tic = false, bool bpc = false, bool highestEic = false) {
            var analysisModel = AnalysisModel;
            if (analysisModel is null) {
                return null;
            }

            var loadChromatogramsUsecase = analysisModel.LoadChromatogramsUsecase();
            loadChromatogramsUsecase.InsertTic = tic;
            loadChromatogramsUsecase.InsertBpc = bpc;
            loadChromatogramsUsecase.InsertHighestEic = highestEic;
            var model = new CheckChromatogramsModel(loadChromatogramsUsecase, analysisModel.AccumulateSpectraUsecase, analysisModel.CompoundSearcher, _storage.Parameter.AdvancedProcessOptionBaseParam, analysisModel.AnalysisFileModel, _broker);
            model.Update();
            return model;
        }


        public Notame Notame {  get; }

        public FragmentQuerySettingModel ShowShowFragmentSearchSettingView() {
            return new FragmentQuerySettingModel(_storage.Parameter.AdvancedProcessOptionBaseParam, AnalysisModel, AlignmentModel);
        }

        public MassqlSettingModel? ShowShowMassqlSearchSettingView(IResultModel model) {
            if (model is null) {
                return null;
            }
            return new MassqlSettingModel(model, _storage.Parameter.AdvancedProcessOptionBaseParam);
        }

        public MscleanrSettingModel? ShowShowMscleanrFilterSettingView() {
            if (AlignmentModel is null) {
                return null;
            }
            return new MscleanrSettingModel(_storage.Parameter, AlignmentModel.Ms1Spots);
        }
    }
}
