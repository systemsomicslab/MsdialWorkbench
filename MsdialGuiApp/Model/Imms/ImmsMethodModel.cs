using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.View.Chart;
using CompMs.App.Msdial.View.Export;
using CompMs.App.Msdial.View.Setting;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Export;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.MessagePack;
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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Reactive.Bindings.Notifiers;
using System.Threading;
using CompMs.Common.DataObj;
using CompMs.App.Msdial.Model.Search;

namespace CompMs.App.Msdial.Model.Imms
{
    internal sealed class ImmsMethodModel : MethodModelBase
    {
        static ImmsMethodModel() {
            chromatogramSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.Drift);
        }
        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer;
        private readonly IMessageBroker _broker;

        public ImmsMethodModel(IMsdialDataStorage<MsdialImmsParameter> storage, ProjectBaseParameterModel projectBaseParameter, IMessageBroker broker)
            : base(storage.AnalysisFiles, storage.AlignmentFiles, projectBaseParameter) {
            Storage = storage;
            _broker = broker;
            matchResultEvaluator = FacadeMatchResultEvaluator.FromDataBases(storage.DataBases);

            var parameter = Storage.Parameter;
            if (parameter.ProviderFactoryParameter is null) {
                parameter.ProviderFactoryParameter = new ImmsAverageDataProviderFactoryParameter(0.01, 0.002, 0, 100);
            }
            ProviderFactory = parameter?.ProviderFactoryParameter.Create(5, true);

            PeakFilterModel = new PeakFilterModel(DisplayFilter.All);
        }

        private FacadeMatchResultEvaluator matchResultEvaluator;

        public ImmsAnalysisModel AnalysisModel {
            get => analysisModel;
            set {
                var old = analysisModel;
                if (SetProperty(ref analysisModel, value)) {
                    old?.Dispose();
                }
            }
        }
        private ImmsAnalysisModel analysisModel;

        public ImmsAlignmentModel AlignmentModel {
            get => alignmentModel;
            set {
                var old = alignmentModel;
                if (SetProperty(ref alignmentModel, value)) {
                    old?.Dispose();
                }
            }
        }
        private ImmsAlignmentModel alignmentModel;

        public PeakFilterModel PeakFilterModel { get; }

        public IMsdialDataStorage<MsdialImmsParameter> Storage { get; }

        public IDataProviderFactory<AnalysisFileBean> ProviderFactory { get; private set; }

        public void Load() {
            var parameter = Storage.Parameter;
            if (parameter.ProviderFactoryParameter is null) {
                parameter.ProviderFactoryParameter = new ImmsAverageDataProviderFactoryParameter(0.01, 0.002, 0, 100);
            }
            ProviderFactory = parameter?.ProviderFactoryParameter.Create(5, true);
        }

        public override Task RunAsync(ProcessOption option, CancellationToken token) {
            Storage.DataBaseMapper = Storage.DataBases.CreateDataBaseMapper();
            matchResultEvaluator = FacadeMatchResultEvaluator.FromDataBases(Storage.DataBases);
            ProviderFactory = Storage.Parameter.ProviderFactoryParameter.Create(5, true);

            var processOption = option;
            // Run Identification
            if (processOption.HasFlag(ProcessOption.Identification) || processOption.HasFlag(ProcessOption.PeakSpotting)) {
                if (!ProcessAnnotaion(null, Storage))
                    return Task.CompletedTask;
            }

            // Run Alignment
            if (processOption.HasFlag(ProcessOption.Alignment)) {
                if (!ProcessAlignment(null, Storage))
                    return Task.CompletedTask;
            }

            return LoadAnalysisFileAsync(Storage.AnalysisFiles.FirstOrDefault(), token);
        }

        private bool ProcessAnnotaion(Window owner, IMsdialDataStorage<MsdialImmsParameter> storage) {
            var vm = new ProgressBarMultiContainerVM
            {
                MaxValue = storage.AnalysisFiles.Count,
                CurrentValue = 0,
                ProgressBarVMs = new ObservableCollection<ProgressBarVM>(
                        storage.AnalysisFiles.Select(file => new ProgressBarVM { Label = file.AnalysisFileName })
                    ),
            };
            var pbmcw = new ProgressBarMultiContainerWindow
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            pbmcw.Loaded += async (s, e) => {
                foreach ((var analysisfile, var pbvm) in storage.AnalysisFiles.Zip(vm.ProgressBarVMs)) {
                    await Task.Run(() => FileProcess.Run(analysisfile, storage, null, null, (IDataProviderFactory<RawMeasurement>)ProviderFactory, matchResultEvaluator, isGuiProcess: true, reportAction: v => pbvm.CurrentValue = v));
                    vm.CurrentValue++;
                }
                pbmcw.Close();
            };

            pbmcw.ShowDialog();

            return true;
        }

        private bool ProcessAlignment(Window owner, IMsdialDataStorage<MsdialImmsParameter> storage) {
            var vm = new ProgressBarVM
            {
                IsIndeterminate = true,
                Label = "Process alignment..",
            };
            var pbw = new ProgressBarWindow
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            pbw.Show();

            var factory = new ImmsAlignmentProcessFactory(storage, matchResultEvaluator);
            var aligner = factory.CreatePeakAligner();
            aligner.ProviderFactory = ProviderFactory; // TODO: I'll remove this later.
            var alignmentFile = storage.AlignmentFiles.Last();
            var result = aligner.Alignment(storage.AnalysisFiles, alignmentFile, chromatogramSpotSerializer);
            result.Save(alignmentFile);
            MsdecResultsWriter.Write(alignmentFile.SpectraFilePath, LoadRepresentativeDeconvolutions(storage, result.AlignmentSpotProperties).ToList());

            pbw.Close();

            return true;
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

            var provider = ProviderFactory.Create(analysisFile);
            AnalysisModel = new ImmsAnalysisModel(
                analysisFile,
                provider,
                matchResultEvaluator,
                Storage.DataBaseMapper.MoleculeAnnotators,
                Storage.DataBaseMapper,
                Storage.Parameter,
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
                Storage.DataBaseMapper.MoleculeAnnotators,
                matchResultEvaluator,
                Storage.DataBaseMapper,
                PeakFilterModel,
                Storage.Parameter,
                Storage.AnalysisFiles)
            .AddTo(Disposables);
        }

        public void ExportAlignment(Window owner) {
            var container = Storage;
            var metadataAccessor = new ImmsMetadataAccessor(container.DataBaseMapper, container.Parameter);
            var vm = new AlignmentResultExport2VM(AlignmentFile, container.AlignmentFiles, container, _broker);
            vm.ExportTypes.AddRange(
                new List<ExportType2>
                {
                    new ExportType2("Raw data (Height)", metadataAccessor, new LegacyQuantValueAccessor("Height", container.Parameter), "Height", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }, true),
                    new ExportType2("Raw data (Area)", metadataAccessor, new LegacyQuantValueAccessor("Area", container.Parameter), "Area", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }),
                    new ExportType2("Normalized data (Height)", metadataAccessor, new LegacyQuantValueAccessor("Normalized height", container.Parameter), "NormalizedHeight", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }),
                    new ExportType2("Normalized data (Area)", metadataAccessor, new LegacyQuantValueAccessor("Normalized area", container.Parameter), "NormalizedArea", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }),
                    new ExportType2("Peak ID", metadataAccessor, new LegacyQuantValueAccessor("ID", container.Parameter), "PeakID"),
                    new ExportType2("m/z", metadataAccessor, new LegacyQuantValueAccessor("MZ", container.Parameter), "Mz"),
                    new ExportType2("S/N", metadataAccessor, new LegacyQuantValueAccessor("SN", container.Parameter), "SN"),
                    new ExportType2("MS/MS included", metadataAccessor, new LegacyQuantValueAccessor("MSMS", container.Parameter), "MsmsIncluded"),
                });
            var dialog = new AlignmentResultExportWin
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            dialog.ShowDialog();
        }

        public void ExportAnalysis(Window owner) {
            var container = Storage;
            var spectraTypes = new List<Export.SpectraType>
            {
                new Export.SpectraType(
                    ExportspectraType.deconvoluted,
                    new ImmsAnalysisMetadataAccessor(container.DataBaseMapper, container.Parameter, ExportspectraType.deconvoluted)),
                new Export.SpectraType(
                    ExportspectraType.centroid,
                    new ImmsAnalysisMetadataAccessor(container.DataBaseMapper, container.Parameter, ExportspectraType.centroid)),
                new Export.SpectraType(
                    ExportspectraType.profile,
                    new ImmsAnalysisMetadataAccessor(container.DataBaseMapper, container.Parameter, ExportspectraType.profile)),
            };
            var spectraFormats = new List<Export.SpectraFormat>
            {
                new Export.SpectraFormat(ExportSpectraFileFormat.txt, new AnalysisCSVExporter()),
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
            var container = Storage;
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
            var container = Storage;
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
            var container = Storage;
            var analysisModel = AnalysisModel;
            if (analysisModel is null) return;

            var param = container.Parameter;
            var model = new Setting.DisplayEicSettingModel(param);
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
            var container = Storage;
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
