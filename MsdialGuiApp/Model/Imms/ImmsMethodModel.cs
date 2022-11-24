using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.View.Chart;
using CompMs.App.Msdial.View.Export;
using CompMs.App.Msdial.View.Setting;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Export;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Graphics.UI.ProgressBar;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialImmsCore.Algorithm.Alignment;
using CompMs.MsdialImmsCore.Export;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialImmsCore.Process;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CompMs.App.Msdial.Model.Export;

namespace CompMs.App.Msdial.Model.Imms
{
    internal sealed class ImmsMethodModel : MethodModelBase
    {
        static ImmsMethodModel() {
            CHROMATOGRAM_SPOT_SERIALIZER = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.Drift);
        }
        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> CHROMATOGRAM_SPOT_SERIALIZER;

        private readonly IMessageBroker _broker;
        private readonly IMsdialDataStorage<MsdialImmsParameter> _storage;
        private readonly ProjectBaseParameterModel _projectBaseParameter;
        private readonly FacadeMatchResultEvaluator _matchResultEvaluator;

        public ImmsMethodModel(AnalysisFileBeanModelCollection analysisFileBeanModelCollection, IMsdialDataStorage<MsdialImmsParameter> storage, ProjectBaseParameterModel projectBaseParameter, IMessageBroker broker)
            : base(analysisFileBeanModelCollection, storage.AlignmentFiles, projectBaseParameter) {
            _storage = storage;
            _projectBaseParameter = projectBaseParameter ?? throw new ArgumentNullException(nameof(projectBaseParameter));
            _broker = broker;
            _matchResultEvaluator = FacadeMatchResultEvaluator.FromDataBases(storage.DataBases);
            _storage.DataBaseMapper = _storage.DataBases.CreateDataBaseMapper();

            var parameter = _storage.Parameter;
            if (parameter.ProviderFactoryParameter is null) {
                parameter.ProviderFactoryParameter = new ImmsAverageDataProviderFactoryParameter(0.01, 0.002, 0, 100);
            }
            ProviderFactory = parameter?.ProviderFactoryParameter.Create(5, true);

            PeakFilterModel = new PeakFilterModel(DisplayFilter.All);

            var metadataAccessor = new ImmsMetadataAccessor(storage.DataBaseMapper, storage.Parameter);
            var peakGroup = new AlignmentExportGroupModel(
                "Peaks",
                new[]
                {
                    new ExportFormat("txt", "txt", new AlignmentCSVExporter()),
                    new ExportFormat("csv", "csv", new AlignmentCSVExporter(separator: ",")),
                },
                new[]
                {
                    new ExportType("Raw data (Height)", metadataAccessor, new LegacyQuantValueAccessor("Height", storage.Parameter), "Height", new List<StatsValue> { StatsValue.Average, StatsValue.Stdev }, true),
                    new ExportType("Raw data (Area)", metadataAccessor, new LegacyQuantValueAccessor("Area", storage.Parameter), "Area", new List<StatsValue> { StatsValue.Average, StatsValue.Stdev }),
                    new ExportType("Normalized data (Height)", metadataAccessor, new LegacyQuantValueAccessor("Normalized height", storage.Parameter), "NormalizedHeight", new List<StatsValue> { StatsValue.Average, StatsValue.Stdev }),
                    new ExportType("Normalized data (Area)", metadataAccessor, new LegacyQuantValueAccessor("Normalized area", storage.Parameter), "NormalizedArea", new List<StatsValue> { StatsValue.Average, StatsValue.Stdev }),
                    new ExportType("Peak ID", metadataAccessor, new LegacyQuantValueAccessor("ID", storage.Parameter), "PeakID"),
                    new ExportType("m/z", metadataAccessor, new LegacyQuantValueAccessor("MZ", storage.Parameter), "Mz"),
                    new ExportType("Mobility", metadataAccessor, new LegacyQuantValueAccessor("Mobility", storage.Parameter), "Mobility"),
                    new ExportType("CCS", metadataAccessor, new LegacyQuantValueAccessor("CCS", storage.Parameter), "CCS"),
                    new ExportType("S/N", metadataAccessor, new LegacyQuantValueAccessor("SN", storage.Parameter), "SN"),
                    new ExportType("MS/MS included", metadataAccessor, new LegacyQuantValueAccessor("MSMS", storage.Parameter), "MsmsIncluded")
                },
                new[]
                {
                    ExportspectraType.deconvoluted,
                });
            var spectraGroup = new AlignmentExportGroupModel(
                "Spectra",
                new[]
                {
                    new ExportFormat("msp", "msp", new AlignmentMspExporter(storage.DataBaseMapper, storage.Parameter)),
                },
                new[]
                {
                    new ExportType("MS/MS spectra", null, null, "Spectra"),
                },
                new[]
                {
                    ExportspectraType.deconvoluted,
                });
            AlignmentResultExportModel = new AlignmentResultExportModel(AlignmentFile, storage.AlignmentFiles, storage, new[] { peakGroup, spectraGroup, });
            this.ObserveProperty(m => m.AlignmentFile)
                .Subscribe(file => AlignmentResultExportModel.AlignmentFile = file)
                .AddTo(Disposables);
        }

        public ImmsAnalysisModel AnalysisModel {
            get => _analysisModel;
            set {
                var old = _analysisModel;
                if (SetProperty(ref _analysisModel, value)) {
                    old?.Dispose();
                }
            }
        }
        private ImmsAnalysisModel _analysisModel;

        public ImmsAlignmentModel AlignmentModel {
            get => _alignmentModel;
            set {
                var old = _alignmentModel;
                if (SetProperty(ref _alignmentModel, value)) {
                    old?.Dispose();
                }
            }
        }
        private ImmsAlignmentModel _alignmentModel;

        public PeakFilterModel PeakFilterModel { get; }

        public IDataProviderFactory<AnalysisFileBean> ProviderFactory { get; }
        public AlignmentResultExportModel AlignmentResultExportModel { get; }

        public override Task RunAsync(ProcessOption option, CancellationToken token) {
            // Run PeakPick and Identification
            if (option.HasFlag(ProcessOption.Identification | ProcessOption.PeakSpotting)) {
                if (!ProcessPeakPickAndAnnotation(_storage)) {
                    return Task.CompletedTask;
                }
            }
            else if (option.HasFlag(ProcessOption.Identification)) {
                if (!ProcessAnnotation(_storage))
                    return Task.CompletedTask;
            }

            // Run Alignment
            if (option.HasFlag(ProcessOption.Alignment)) {
                if (!ProcessAlignment(_storage))
                    return Task.CompletedTask;
            }

            return LoadAnalysisFileAsync(AnalysisFileModelCollection.AnalysisFiles.FirstOrDefault(), token);
        }

        private bool ProcessPeakPickAndAnnotation(IMsdialDataStorage<MsdialImmsParameter> storage) {
            var request = new ProgressBarMultiContainerRequest(
                async vm =>
                {
                    var usable = Math.Max(_storage.Parameter.ProcessBaseParam.UsableNumThreads / 2, 1);
                    var processor = new FileProcess(storage, null, null, _matchResultEvaluator);
                    await processor.RunAllAsync(
                        storage.AnalysisFiles,
                        storage.AnalysisFiles.Select(ProviderFactory.Create),
                        vm.ProgressBarVMs.Select(pbvm => (Action<int>)((int v) => pbvm.CurrentValue = v)),
                        usable,
                        vm.Increment)
                    .ConfigureAwait(false);
                },
                storage.AnalysisFiles.Select(file => file.AnalysisFileName).ToArray());
            _broker.Publish(request);

            return request.Result ?? false;
        }

        private bool ProcessAnnotation(IMsdialDataStorage<MsdialImmsParameter> storage) {
            var request = new ProgressBarMultiContainerRequest(
                async vm =>
                {
                    var usable = Math.Max(_storage.Parameter.ProcessBaseParam.UsableNumThreads / 2, 1);
                    var processor = new FileProcess(storage, null, null, _matchResultEvaluator);
                    await processor.AnnotateAllAsync(
                        storage.AnalysisFiles,
                        storage.AnalysisFiles.Select(ProviderFactory.Create),
                        vm.ProgressBarVMs.Select(pbvm => (Action<int>)((int v) => pbvm.CurrentValue = v)),
                        usable,
                        vm.Increment)
                    .ConfigureAwait(false);
                },
                storage.AnalysisFiles.Select(file => file.AnalysisFileName).ToArray());
            _broker.Publish(request);

            return request.Result ?? false;
        }

        private bool ProcessAlignment(IMsdialDataStorage<MsdialImmsParameter> storage) {
            var request = new ProgressBarRequest("Process alignment..", isIndeterminate: true,
                async _ =>
                {
                    var factory = new ImmsAlignmentProcessFactory(storage, _matchResultEvaluator);
                    var aligner = factory.CreatePeakAligner();
                    aligner.ProviderFactory = ProviderFactory; // TODO: I'll remove this later.
                    var alignmentFile = storage.AlignmentFiles.Last();
                    var result = await Task.Run(() => aligner.Alignment(storage.AnalysisFiles, alignmentFile, CHROMATOGRAM_SPOT_SERIALIZER)).ConfigureAwait(false);
                    result.Save(alignmentFile);
                    MsdecResultsWriter.Write(alignmentFile.SpectraFilePath, LoadRepresentativeDeconvolutions(storage, result.AlignmentSpotProperties).ToList());
                });
            _broker.Publish(request);
            return request.Result ?? false;
        }

        private static IEnumerable<MSDecResult> LoadRepresentativeDeconvolutions(IMsdialDataStorage<MsdialImmsParameter> storage, IReadOnlyList<AlignmentSpotProperty> spots) {
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

            var provider = ProviderFactory.Create(analysisFile.File);
            AnalysisModel = new ImmsAnalysisModel(
                analysisFile,
                provider, _matchResultEvaluator,
                _storage.DataBases,
                _storage.DataBaseMapper,
                _storage.Parameter,
                PeakFilterModel)
            .AddTo(Disposables);
            return AnalysisModel;
        }

        protected override IAlignmentModel LoadAlignmentFileCore(AlignmentFileBean alignmentFile) {
            if (AlignmentModel != null) {
                AlignmentModel.Dispose();
                Disposables.Remove(AlignmentModel);
            }

            return AlignmentModel = new ImmsAlignmentModel(
                alignmentFile,
                AnalysisFileModelCollection,
                _matchResultEvaluator,
                _storage.DataBases,
                _storage.DataBaseMapper,
                PeakFilterModel,
               _projectBaseParameter,
                _storage.Parameter,
                _storage.AnalysisFiles)
            .AddTo(Disposables);
        }

        public void ExportAnalysis(Window owner) {
            var container = _storage;
            var spectraTypes = new List<SpectraType>
            {
                new SpectraType(
                    ExportspectraType.deconvoluted,
                    new ImmsAnalysisMetadataAccessor(container.DataBaseMapper, container.Parameter, ExportspectraType.deconvoluted)),
                new SpectraType(
                    ExportspectraType.centroid,
                    new ImmsAnalysisMetadataAccessor(container.DataBaseMapper, container.Parameter, ExportspectraType.centroid)),
                new SpectraType(
                    ExportspectraType.profile,
                    new ImmsAnalysisMetadataAccessor(container.DataBaseMapper, container.Parameter, ExportspectraType.profile)),
            };
            var spectraFormats = new List<SpectraFormat>
            {
                new SpectraFormat(ExportSpectraFileFormat.txt, new AnalysisCSVExporter()),
            };

            using (var vm = new AnalysisResultExportViewModel(container.AnalysisFiles, spectraTypes, spectraFormats, ProviderFactory)) {
                var dialog = new AnalysisResultExportWin
                {
                    DataContext = vm,
                    Owner = owner,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };

                dialog.ShowDialog();
            }
        }

        public void ShowTIC(Window owner) {
            var analysisModel = AnalysisModel;
            if (analysisModel is null) return;

            var tic = analysisModel.EicLoader.LoadTic();
            var vm = new ChromatogramsViewModel(
                new ChromatogramsModel("Total ion chromatogram", 
                new DisplayChromatogram(tic, new Pen(Brushes.Black, 1.0), "TIC"), "Total ion chromatogram", "Mobility", "Absolute ion abundance"));
            var view = new DisplayChromatogramsView() {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            view.Show();
        }

        public void ShowBPC(Window owner) {
            var analysisModel = AnalysisModel;
            if (analysisModel is null) return;

            var bpc = analysisModel.EicLoader.LoadBpc();
            var vm = new ChromatogramsViewModel(new ChromatogramsModel("Base peak chromatogram", new DisplayChromatogram(bpc, new Pen(Brushes.Red, 1.0), "BPC"),
                "Base peak chromatogram", "Mobility", "Absolute ion abundance"));
            var view = new DisplayChromatogramsView() {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            view.Show();
        }

        public void ShowEIC(Window owner) {
            var analysisModel = AnalysisModel;
            if (analysisModel is null) return;

            var param = _storage.Parameter;
            var model = new Setting.DisplayEicSettingModel(analysisModel.EicLoader, param);
            var dialog = new EICDisplaySettingView() {
                DataContext = new DisplayEicSettingViewModel(model),
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            if (dialog.ShowDialog() == true) {
                param.AdvancedProcessOptionBaseParam.DiplayEicSettingValues = model.DiplayEicSettingValues.Where(n => n.Mass > 0 && n.MassTolerance > 0).ToList();
                var displayEICs = param.AdvancedProcessOptionBaseParam.DiplayEicSettingValues;
                if (!displayEICs.IsEmptyOrNull()) {
                    var displayChroms = new List<DisplayChromatogram>();
                    var counter = 0;
                    foreach (var set in displayEICs.Where(n => n.Mass > 0 && n.MassTolerance > 0)) {
                        var eic = analysisModel.EicLoader.LoadEicTrace(set.Mass, set.MassTolerance);
                        var subtitle = "[" + Math.Round(set.Mass - set.MassTolerance, 4).ToString() + "-" + Math.Round(set.Mass + set.MassTolerance, 4).ToString() + "]";
                        var chrom = new DisplayChromatogram(eic, new Pen(ChartBrushes.GetChartBrush(counter), 1.0), set.Title + "; " + subtitle);
                        counter++;
                        displayChroms.Add(chrom);
                    }
                    var vm = new ChromatogramsViewModel(new ChromatogramsModel("EIC", displayChroms, "EIC", "Mobility", "Absolute ion abundance"));
                    var view = new DisplayChromatogramsView() {
                        DataContext = vm,
                        Owner = owner,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    view.Show();
                }
            }
        }

        public void ShowTicBpcRepEIC(Window owner) {
            var analysisModel = AnalysisModel;
            if (analysisModel is null) return;

            var tic = analysisModel.EicLoader.LoadTic();
            var bpc = analysisModel.EicLoader.LoadBpc();
            var eic = analysisModel.EicLoader.LoadHighestEicTrace(analysisModel.Ms1Peaks.ToList());

            var maxPeakMz = analysisModel.Ms1Peaks.Argmax(n => n.Intensity).Mass;


            var displayChroms = new List<DisplayChromatogram>() {
                new DisplayChromatogram(tic, new Pen(Brushes.Black, 1.0), "TIC"),
                new DisplayChromatogram(bpc, new Pen(Brushes.Red, 1.0), "BPC"),
                new DisplayChromatogram(eic, new Pen(Brushes.Blue, 1.0), "EIC of m/z " + Math.Round(maxPeakMz, 5).ToString())
            };

            var vm = new ChromatogramsViewModel(new ChromatogramsModel("TIC, BPC, and highest peak m/z's EIC", displayChroms, "TIC, BPC, and highest peak m/z's EIC", "Mobility", "Absolute ion abundance"));
            var view = new DisplayChromatogramsView() {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            view.Show();
        }
    }
}
