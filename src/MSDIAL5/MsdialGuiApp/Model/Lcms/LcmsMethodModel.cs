using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Export;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Graphics.UI.ProgressBar;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.Algorithm.Alignment;
using CompMs.MsdialLcMsApi.Export;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Lcms
{
    internal sealed class LcmsMethodModel : MethodModelBase
    {
        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> CHROMATOGRAM_SPOT_SERIALIZER;

        static LcmsMethodModel() {
            CHROMATOGRAM_SPOT_SERIALIZER = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.RT);
        }

        private readonly IDataProviderFactory<AnalysisFileBean> _providerFactory;
        private readonly ProjectBaseParameterModel _projectBaseParameter;
        private readonly IMessageBroker _broker;
        private readonly IMsdialDataStorage<MsdialLcmsParameter> _storage;
        private readonly FacadeMatchResultEvaluator _matchResultEvaluator;

        public LcmsMethodModel(
            AnalysisFileBeanModelCollection analysisFileBeanModelCollection,
            AlignmentFileBeanModelCollection alignmentFileBeanModelCollection,
            IMsdialDataStorage<MsdialLcmsParameter> storage,
            IDataProviderFactory<AnalysisFileBean> providerFactory,
            ProjectBaseParameterModel projectBaseParameter, 
            IMessageBroker broker)
            : base(analysisFileBeanModelCollection, alignmentFileBeanModelCollection, projectBaseParameter) {

            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _matchResultEvaluator = FacadeMatchResultEvaluator.FromDataBases(storage.DataBases);
            _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
            _projectBaseParameter = projectBaseParameter ?? throw new ArgumentNullException(nameof(projectBaseParameter));
            _broker = broker;
            PeakFilterModel = new PeakFilterModel(DisplayFilter.All & ~DisplayFilter.CcsMatched);
            CanShowProteinGroupTable = Observable.Return(storage.Parameter.TargetOmics == TargetOmics.Proteomics);

            List<AnalysisFileBean> analysisFiles = analysisFileBeanModelCollection.AnalysisFiles.Select(f => f.File).ToList();
            var stats = new List<StatsValue> { StatsValue.Average, StatsValue.Stdev, };
            var metadataAccessorFactory = new LcmsAlignmentMetadataAccessorFactory(storage.DataBaseMapper, storage.Parameter);
            var currentAlignmentResult = this.ObserveProperty(m => m.AlignmentModel).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            AlignmentPeakSpotSupplyer peakSpotSupplyer = new AlignmentPeakSpotSupplyer(PeakFilterModel, _matchResultEvaluator.Contramap((IFilterable filterable) => filterable.MatchResults.Representative), currentAlignmentResult);
            var peakGroup = new AlignmentExportGroupModel(
                "Peaks",
                new ExportMethod(
                    analysisFiles,
                    metadataAccessorFactory,
                    ExportFormat.Tsv,
                    ExportFormat.Csv
                ),
                new[]
                {
                    new ExportType("Raw data (Height)", new LegacyQuantValueAccessor("Height", storage.Parameter), "Height", stats, true),
                    new ExportType("Raw data (Area)", new LegacyQuantValueAccessor("Area", storage.Parameter), "Area", stats),
                    new ExportType("Normalized data (Height)", new LegacyQuantValueAccessor("Normalized height", storage.Parameter), "NormalizedHeight", stats),
                    new ExportType("Normalized data (Area)", new LegacyQuantValueAccessor("Normalized area", storage.Parameter), "NormalizedArea", stats),
                    new ExportType("Peak ID", new LegacyQuantValueAccessor("ID", storage.Parameter), "PeakID"),
                    new ExportType("m/z", new LegacyQuantValueAccessor("MZ", storage.Parameter), "Mz"),
                    new ExportType("Retention time", new LegacyQuantValueAccessor("RT", storage.Parameter), "Rt"),
                    new ExportType("S/N", new LegacyQuantValueAccessor("SN", storage.Parameter), "SN"),
                    new ExportType("MS/MS included", new LegacyQuantValueAccessor("MSMS", storage.Parameter), "MsmsIncluded"),
                    new ExportType("Identification method", new AnnotationMethodAccessor(), "IdentificationMethod"),
                },
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
            var exportGroups = new List<IAlignmentResultExportModel> { peakGroup, spectraGroup, spectraAndReference, };
            if (storage.Parameter.TargetOmics == TargetOmics.Proteomics) {
                exportGroups.Add(new ProteinGroupExportModel(new ProteinGroupExporter(), analysisFiles));
            }

            AlignmentResultExportModel = new AlignmentResultExportModel(exportGroups, this.ObserveProperty(m => m.AlignmentFile), alignmentFileBeanModelCollection.Files, peakSpotSupplyer, storage.Parameter.DataExportParam).AddTo(Disposables);
        }

        public PeakFilterModel PeakFilterModel { get; }

        public IObservable<bool> CanShowProteinGroupTable { get; }

        public LcmsAnalysisModel AnalysisModel {
            get => _analysisModel;
            private set => SetProperty(ref _analysisModel, value);
        }
        private LcmsAnalysisModel _analysisModel;

        public LcmsAlignmentModel AlignmentModel {
            get => _alignmentModel;
            set => SetProperty(ref _alignmentModel, value);
        }
        private LcmsAlignmentModel _alignmentModel;

        public AlignmentResultExportModel AlignmentResultExportModel { get; }

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
                PeakFilterModel,
                _storage.DataBaseMapper,
                _storage.Parameter,
                _projectBaseParameter,
                _storage.AnalysisFiles,
                AnalysisFileModelCollection,
                _broker)
            .AddTo(Disposables);
        }

        public override async Task RunAsync(ProcessOption processOption, CancellationToken token) {
            // Set analysis param
            var parameter = _storage.Parameter;
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
            if (processOption.HasFlag(ProcessOption.Identification | ProcessOption.PeakSpotting)) {
                if (!ProcessPickAndAnnotaion(_storage, annotationProcess))
                    return;
            }
            else if (processOption.HasFlag(ProcessOption.Identification)) {
                if (!ProcessAnnotaion(_storage, annotationProcess))
                    return;
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

            await LoadAnalysisFileAsync(AnalysisFileModelCollection.AnalysisFiles.FirstOrDefault(), token).ConfigureAwait(false);

#if DEBUG
            Console.WriteLine(string.Join("\n", _storage.Parameter.ParametersAsText()));
#endif
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

        private bool ProcessPickAndAnnotaion(IMsdialDataStorage<MsdialLcmsParameter> storage, IAnnotationProcess annotationProcess) {
            var request = new ProgressBarMultiContainerRequest(
                vm_ =>
                {
                    var processor = new MsdialLcMsApi.Process.FileProcess(_providerFactory, storage, annotationProcess, _matchResultEvaluator);
                    var runner = new ProcessRunner(processor);
                    return runner.RunAllAsync(
                        storage.AnalysisFiles,
                        vm_.ProgressBarVMs.Select(pbvm => (Action<int>)((int v) => pbvm.CurrentValue = v)),
                        Math.Max(1, storage.Parameter.ProcessBaseParam.UsableNumThreads / 2),
                        vm_.Increment,
                        default);
                },
                storage.AnalysisFiles.Select(file => file.AnalysisFileName).ToArray());
            _broker.Publish(request);
            return request.Result ?? false;
        }

        private bool ProcessAnnotaion(IMsdialDataStorage<MsdialLcmsParameter> storage, IAnnotationProcess annotationProcess) {
            var request = new ProgressBarMultiContainerRequest(
                vm_ =>
                {
                    var processor = new MsdialLcMsApi.Process.FileProcess(_providerFactory, storage, annotationProcess, _matchResultEvaluator);
                    var runner = new ProcessRunner(processor);
                    return runner.AnnotateAllAsync(
                        storage.AnalysisFiles,
                        vm_.ProgressBarVMs.Select(pbvm => (Action<int>)((int v) => pbvm.CurrentValue = v)),
                        Math.Max(1, storage.Parameter.ProcessBaseParam.UsableNumThreads / 2),
                        vm_.Increment,
                        default);
                },
                storage.AnalysisFiles.Select(file => file.AnalysisFileName).ToArray());
            _broker.Publish(request);
            return request.Result ?? false;
        }

        public bool ProcessSeccondAnnotaion4ShotgunProteomics(IMsdialDataStorage<MsdialLcmsParameter> storage) {
            var request = new ProgressBarRequest("Process second annotation..", isIndeterminate: false,
                async vm =>
                {
                    var proteomicsAnnotator = new ProteomeDataAnnotator();
                    await Task.Run(() => proteomicsAnnotator.ExecuteSecondRoundAnnotationProcess(
                        storage.AnalysisFiles,
                        storage.DataBaseMapper,
                        _matchResultEvaluator,
                        storage.DataBases,
                        storage.Parameter,
                        v => vm.CurrentValue = v)).ConfigureAwait(false);
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
                        ReportAction = v => vm.CurrentValue = v
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
                new SpectraType(
                    ExportspectraType.deconvoluted,
                    new LcmsAnalysisMetadataAccessor(_storage.DataBaseMapper, _storage.Parameter, ExportspectraType.deconvoluted)),
                new SpectraType(
                    ExportspectraType.centroid,
                    new LcmsAnalysisMetadataAccessor(_storage.DataBaseMapper, _storage.Parameter, ExportspectraType.centroid)),
                new SpectraType(
                    ExportspectraType.profile,
                    new LcmsAnalysisMetadataAccessor(_storage.DataBaseMapper, _storage.Parameter, ExportspectraType.profile)),
            };
            var spectraFormats = new List<SpectraFormat>
            {
                new SpectraFormat(ExportSpectraFileFormat.txt, new AnalysisCSVExporter()),
            };

            var models = new IMsdialAnalysisExport[]
            {
                new MsdialAnalysisTableExportModel(spectraTypes, spectraFormats, _providerFactory.ContraMap((AnalysisFileBeanModel file) => file.File)),
                new SpectraTypeSelectableMsdialAnalysisExportModel(new Dictionary<ExportspectraType, IAnalysisExporter> {
                    [ExportspectraType.deconvoluted] = new AnalysisMspExporter(_storage.DataBaseMapper, _storage.Parameter),
                    [ExportspectraType.centroid] = new AnalysisMspExporter(_storage.DataBaseMapper, _storage.Parameter, file => new CentroidMsScanPropertyLoader(_providerFactory.Create(file), _storage.Parameter.MS2DataType)),
                })
                {
                    FilePrefix = "Msp",
                    FileSuffix = "msp",
                    Label = "Nist format (*.msp)"
                },
            };
            return new AnalysisResultExportModel(AnalysisFileModelCollection, _storage.Parameter.ProjectParam.ProjectFolderPath, models);
        }

        public ChromatogramsModel ShowTIC() {
            var analysisModel = AnalysisModel;
            if (analysisModel is null) {
                return null;
            }

            var tic = analysisModel.EicLoader.LoadTic();
            var chromatogram = new DisplayChromatogram(tic, new Pen(Brushes.Black, 1.0), "TIC");
            return new ChromatogramsModel("Total ion chromatogram", chromatogram, "Total ion chromatogram", "Retention time", "Absolute ion abundance");
        }

        public ChromatogramsModel ShowBPC() {
            var analysisModel = AnalysisModel;
            if (analysisModel is null) {
                return null;
            }
            var bpc = analysisModel.EicLoader.LoadBpc();
            var chromatogram = new DisplayChromatogram(bpc, new Pen(Brushes.Red, 1.0), "BPC");
            return new ChromatogramsModel("Base peak chromatogram", chromatogram, "Base peak chromatogram", "Retention time", "Absolute ion abundance");
        }

        public DisplayEicSettingModel ShowEIC() {
            if (AnalysisModel is null) {
                return null;
            }
            return new DisplayEicSettingModel(AnalysisModel.EicLoader, _storage.Parameter);
        }

        public ChromatogramsModel ShowTicBpcRepEIC() {
            var container = _storage;
            var analysisModel = AnalysisModel;
            if (analysisModel is null) {
                return null;
            }

            var tic = analysisModel.EicLoader.LoadTic();
            var bpc = analysisModel.EicLoader.LoadBpc();
            var eic = analysisModel.EicLoader.LoadHighestEicTrace(analysisModel.Ms1Peaks.ToList());

            var maxPeakMz = analysisModel.Ms1Peaks.Argmax(n => n.Intensity).Mass;

            var displayChroms = new List<DisplayChromatogram>() {
                new DisplayChromatogram(tic, new Pen(Brushes.Black, 1.0), "TIC"),
                new DisplayChromatogram(bpc, new Pen(Brushes.Red, 1.0), "BPC"),
                new DisplayChromatogram(eic, new Pen(Brushes.Blue, 1.0), "EIC of m/z " + Math.Round(maxPeakMz, 5).ToString())
            };

            return new ChromatogramsModel("TIC, BPC, and highest peak m/z's EIC", displayChroms, "TIC, BPC, and highest peak m/z's EIC", "Retention time [min]", "Absolute ion abundance");
        }

        public FragmentQuerySettingModel ShowShowFragmentSearchSettingView() {
            return new FragmentQuerySettingModel(_storage.Parameter.AdvancedProcessOptionBaseParam, AnalysisModel, AlignmentModel);
        }

        public MassqlSettingModel ShowShowMassqlSearchSettingView(IResultModel model) {
            if (model is null) {
                return null;
            }
            return new MassqlSettingModel(model, _storage.Parameter.AdvancedProcessOptionBaseParam);
        }

        public MscleanrSettingModel ShowShowMscleanrFilterSettingView() {
            if (AlignmentModel is null) {
                return null;
            }
            return new MscleanrSettingModel(_storage.Parameter, AlignmentModel.Ms1Spots);
        }
    }
}
