using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Export;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Graphics.UI.ProgressBar;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcImMsApi.Algorithm;
using CompMs.MsdialLcImMsApi.Algorithm.Alignment;
using CompMs.MsdialLcImMsApi.Algorithm.Annotation;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcImMsApi.Process;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Lcimms
{
    internal sealed class LcimmsMethodModel : MethodModelBase
    {
        static LcimmsMethodModel() {
            chromatogramSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.Drift);
        }

        public LcimmsMethodModel(AnalysisFileBeanModelCollection analysisFileBeanModelCollection, AlignmentFileBeanModelCollection alignmentFileBeanModelCollection, IMsdialDataStorage<MsdialLcImMsParameter> storage, ProjectBaseParameterModel projectBaseParameter, IMessageBroker broker)
            : base(analysisFileBeanModelCollection, alignmentFileBeanModelCollection, projectBaseParameter) {
            if (storage is null) {
                throw new ArgumentNullException(nameof(storage));
            }

            Storage = storage;
            _projectBaseParameter = projectBaseParameter ?? throw new ArgumentNullException(nameof(projectBaseParameter));
            _broker = broker;
            providerFactory = new StandardDataProviderFactory();
            accProviderFactory = new LcimmsAccumulateDataProviderFactory();
            matchResultEvaluator = FacadeMatchResultEvaluator.FromDataBases(storage.DataBases);
            PeakFilterModel = new PeakFilterModel(DisplayFilter.All);
            AccumulatedPeakFilterModel = new PeakFilterModel(DisplayFilter.All & ~DisplayFilter.CcsMatched);

            List<AnalysisFileBean> analysisFiles = analysisFileBeanModelCollection.AnalysisFiles.Select(f => f.File).ToList();
            var stats = new List<StatsValue> { StatsValue.Average, StatsValue.Stdev, };
            var metadataAccessorFactory = new LcimmsAlignmentMetadataAccessorFactory(storage.DataBaseMapper, storage.Parameter);
            var currentAlignmentResult = this.ObserveProperty(m => m.AlignmentModel).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            AlignmentPeakSpotSupplyer peakSpotSupplyer = new AlignmentPeakSpotSupplyer(PeakFilterModel, matchResultEvaluator.Contramap((IFilterable filterable) => filterable.MatchResults.Representative), currentAlignmentResult);
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
                    new ExportType("Mobility", new LegacyQuantValueAccessor("Mobility", storage.Parameter), "Mobility"),
                    new ExportType("CCS", new LegacyQuantValueAccessor("CCS", storage.Parameter), "CCS"),
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
            AlignmentResultExportModel = new AlignmentResultExportModel(new IAlignmentResultExportModel[] { peakGroup, spectraGroup, spectraAndReference, }, this.ObserveProperty(m => m.AlignmentFile), alignmentFileBeanModelCollection.Files, peakSpotSupplyer, storage.Parameter.DataExportParam).AddTo(Disposables);
        }

        private FacadeMatchResultEvaluator matchResultEvaluator;

        public IMsdialDataStorage<MsdialLcImMsParameter> Storage { get; }

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
        private readonly IDataProviderFactory<RawMeasurement> providerFactory;
        private readonly IDataProviderFactory<RawMeasurement> accProviderFactory;
        private readonly ProjectBaseParameterModel _projectBaseParameter;
        private readonly IMessageBroker _broker;

        public PeakFilterModel AccumulatedPeakFilterModel { get; }
        public PeakFilterModel PeakFilterModel { get; }
        public AlignmentResultExportModel AlignmentResultExportModel { get; }

        protected override IAnalysisModel LoadAnalysisFileCore(AnalysisFileBeanModel analysisFile) {
            if (AnalysisModel != null) {
                AnalysisModel.Dispose();
                Disposables.Remove(AnalysisModel);
            }
            var rawObj = analysisFile.File.LoadRawMeasurement(isImagingMsData: false, isGuiProcess: true, retry: 5, sleepMilliSeconds: 5000);
            return AnalysisModel = new LcimmsAnalysisModel(
                analysisFile,
                providerFactory.Create(rawObj),
                accProviderFactory.Create(rawObj),
                Storage.DataBases,
                matchResultEvaluator,
                Storage.DataBaseMapper,
                Storage.Parameter,
                PeakFilterModel,
                AccumulatedPeakFilterModel,
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
                _projectBaseParameter,
                Storage.Parameter,
                Storage.AnalysisFiles,
                PeakFilterModel,
                AccumulatedPeakFilterModel,
                _broker)
            .AddTo(Disposables);
        }

        public override async Task RunAsync(ProcessOption processOption, CancellationToken token) {
            // Set analysis param
            annotationProcess = BuildAnnotationProcess();

            // Run Identification
            if (processOption.HasFlag(ProcessOption.Identification)) {
                int usable = Math.Max(Storage.Parameter.ProcessBaseParam.UsableNumThreads / 2, 1);
                FileProcess processor = new FileProcess(providerFactory, accProviderFactory, annotationProcess, matchResultEvaluator, Storage, isGuiProcess: true);
                var runner = new ProcessRunner(processor);
                if (processOption.HasFlag(ProcessOption.PeakSpotting)) {
                    if (!RunFileProcess(Storage.AnalysisFiles, usable, runner)) {
                        return;
                    }
                }
                else {
                    if (!RunAnnotation(Storage.AnalysisFiles, usable, runner)) {
                        return;
                    }
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

        private IAnnotationProcess BuildAnnotationProcess() {
            return new LcimmsStandardAnnotationProcess(Storage.CreateAnnotationQueryFactoryStorage().MoleculeQueryFactories, matchResultEvaluator, Storage.DataBaseMapper);
        }

        private bool RunFileProcess(List<AnalysisFileBean> analysisFiles, int usable, ProcessRunner runner) {
            var request = new ProgressBarMultiContainerRequest(
                vm => runner.RunAllAsync(analysisFiles, vm.ProgressBarVMs.Select(vm_ => (Action<int>)((int v) => vm_.CurrentValue = v)), usable, vm.Increment, default),
                analysisFiles.Select(file => file.AnalysisFileName).ToArray());
            _broker.Publish(request);
            return request.Result ?? false;
        }

        private bool RunAnnotation(List<AnalysisFileBean> analysisFiles, int usable, ProcessRunner runner) {
            var request = new ProgressBarMultiContainerRequest(
                vm => runner.AnnotateAllAsync(analysisFiles, vm.ProgressBarVMs.Select(vm_ => (Action<int>)((int v) => vm_.CurrentValue = v)), usable, vm.Increment, default),
                analysisFiles.Select(file => file.AnalysisFileName).ToArray());
            _broker.Publish(request);
            return request.Result ?? false;
        }

        public bool RunAlignmentProcess() {
            var request = new ProgressBarRequest("Process alignment..", isIndeterminate: true,
                async _ =>
                {
                    RawMeasurement map(AnalysisFileBean file) {
                        return file.LoadRawMeasurement(false, true, 5, 1000);
                    }
                    AlignmentProcessFactory aFactory = new LcimmsAlignmentProcessFactory(Storage, matchResultEvaluator, providerFactory.ContraMap((Func<AnalysisFileBean, RawMeasurement>)map), accProviderFactory.ContraMap((Func<AnalysisFileBean, RawMeasurement>)map));
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

        public void SaveProject() {
            AlignmentModel?.SaveProject();
        }

        public ChromatogramsModel PrepareTIC() {
            var analysisModel = AnalysisModel;
            if (analysisModel is null) {
                return null;
            }

            var tic = analysisModel.EicLoader.LoadTic();
            var pen = new Pen(Brushes.Black, 1.0);
            pen.Freeze();
            return new ChromatogramsModel("Total ion chromatogram", new DisplayChromatogram(tic, pen, "TIC"), "Total ion chromatogram", "Retention time", "Absolute ion abundance");
        }

        public ChromatogramsModel PrepareBPC() {
            var analysisModel = AnalysisModel;
            if (analysisModel is null) {
                return null;
            }

            var bpc = analysisModel.EicLoader.LoadBpc();
            var pen = new Pen(Brushes.Red, 1.0);
            pen.Freeze();
            return new ChromatogramsModel("Base peak chromatogram", new DisplayChromatogram(bpc, pen, "BPC"), "Base peak chromatogram", "Retention time", "Absolute ion abundance");
        }

        public DisplayEicSettingModel PrepareEicSetting() {
            var analysisModel = AnalysisModel;
            if (analysisModel is null) {
                return null;
            }
            return new DisplayEicSettingModel(analysisModel.EicLoader, Storage.Parameter);
        }

        public ChromatogramsModel PrepareTicBpcRepEIC() {
            var analysisModel = AnalysisModel;
            if (analysisModel is null) {
                return null;
            }

            var tic = analysisModel.EicLoader.LoadTic();
            var bpc = analysisModel.EicLoader.LoadBpc();
            var eic = analysisModel.EicLoader.LoadHighestEicTrace(analysisModel.Ms1Peaks.ToList());

            var maxPeakMz = analysisModel.Ms1Peaks.Argmax(n => n.Intensity).Mass;

            Pen ticPen = new Pen(Brushes.Black, 1.0);
            ticPen.Freeze();
            Pen bpcPen = new Pen(Brushes.Red, 1.0);
            bpcPen.Freeze();
            Pen eicPen = new Pen(Brushes.Blue, 1.0);
            eicPen.Freeze();
            var displayChroms = new List<DisplayChromatogram>() {
                new DisplayChromatogram(tic, ticPen, "TIC"),
                new DisplayChromatogram(bpc, bpcPen, "BPC"),
                new DisplayChromatogram(eic, eicPen, "EIC of m/z " + Math.Round(maxPeakMz, 5).ToString())
            };
            return new ChromatogramsModel("TIC, BPC, and highest peak m/z's EIC", displayChroms, "TIC, BPC, and highest peak m/z's EIC", "Retention time [min]", "Absolute ion abundance");
        }
    }
}
