using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.View.Chart;
using CompMs.App.Msdial.View.Setting;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialLcImMsApi.Algorithm;
using CompMs.MsdialLcImMsApi.Algorithm.Alignment;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcImMsApi.Process;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Lcimms
{
    class LcimmsMethodModel : MethodModelBase
    {
        static LcimmsMethodModel() {
            chromatogramSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.Drift);
        }

        public LcimmsMethodModel(IMsdialDataStorage<MsdialLcImMsParameter> storage, ProjectBaseParameterModel projectBaseParameter)
            : base(storage.AnalysisFiles, storage.AlignmentFiles, projectBaseParameter) {
            if (storage is null) {
                throw new ArgumentNullException(nameof(storage));
            }

            Storage = storage;
            providerFactory = new StandardDataProviderFactory();
            accProviderFactory = new LcimmsAccumulateDataProviderFactory();
            matchResultEvaluator = FacadeMatchResultEvaluator.FromDataBases(storage.DataBases);
            PeakFilterModel = new PeakFilterModel(DisplayFilter.All);
            AccumulatedPeakFilterModel = new PeakFilterModel(DisplayFilter.All & ~DisplayFilter.CcsMatched);
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

        public PeakFilterModel AccumulatedPeakFilterModel { get; }
        public PeakFilterModel PeakFilterModel { get; }

        protected override IAnalysisModel LoadAnalysisFileCore(AnalysisFileBean analysisFile) {
            if (AnalysisModel != null) {
                AnalysisModel.Dispose();
                Disposables.Remove(AnalysisModel);
            }
            var rawObj = DataAccess.LoadMeasurement(analysisFile, isGuiProcess: true, retry: 5, sleepMilliSeconds: 5000);
            return AnalysisModel = new LcimmsAnalysisModel(
                analysisFile,
                providerFactory.Create(rawObj),
                accProviderFactory.Create(rawObj),
                matchResultEvaluator,
                Storage.DataBaseMapper,
                Storage.Parameter,
                PeakFilterModel,
                AccumulatedPeakFilterModel)
            .AddTo(Disposables);
        }

        protected override IAlignmentModel LoadAlignmentFileCore(AlignmentFileBean alignmentFile) {
            if (AlignmentModel != null) {
                AlignmentModel.Dispose();
                Disposables.Remove(AlignmentModel);
            }
            return AlignmentModel = new LcimmsAlignmentModel(
                alignmentFile,
                matchResultEvaluator,
                Storage.DataBaseMapper,
                Storage.Parameter,
                Storage.AnalysisFiles,
                PeakFilterModel)
            .AddTo(Disposables);
        }

        public override async Task RunAsync(ProcessOption option, CancellationToken token) {
            // Set analysis param
            var parameter = Storage.Parameter;
            annotationProcess = BuildAnnotationProcess(Storage.DataBases, parameter.PeakPickBaseParam);

            var processOption = option;
            // Run Identification
            if (processOption.HasFlag(ProcessOption.Identification) || processOption.HasFlag(ProcessOption.PeakSpotting)) {
                var tasks = Storage.AnalysisFiles.Select(file => RunAnnotationProcess(file, null));
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            // Run Alignment
            if (processOption.HasFlag(ProcessOption.Alignment)) {
                RunAlignmentProcess();
            }

            await LoadAnalysisFileAsync(Storage.AnalysisFiles.FirstOrDefault(), token).ConfigureAwait(false);
        }

        private IAnnotationProcess BuildAnnotationProcess(DataBaseStorage storage, PeakPickBaseParameter parameter) {
            var containers = new List<IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>>();
            foreach (var annotators in storage.MetabolomicsDataBases) {
                containers.AddRange(annotators.Pairs.Select(annotator => annotator.ConvertToAnnotatorContainer()));
            }
            return new StandardAnnotationProcess<IAnnotationQuery>(
                containers.Select(container => (
                    new AnnotationQueryFactory(container.Annotator, parameter) as IAnnotationQueryFactory<IAnnotationQuery>,
                    container
                )).ToList());
        }

        public async Task RunAnnotationProcess(AnalysisFileBean analysisfile, Action<int> action) {
            await Task.Run(() => FileProcess.Run(analysisfile, providerFactory, accProviderFactory, annotationProcess, matchResultEvaluator, Storage, isGuiProcess: true, reportAction: action)).ConfigureAwait(false);
        }

        public void RunAlignmentProcess() {
            AlignmentProcessFactory aFactory = new LcimmsAlignmentProcessFactory(Storage, matchResultEvaluator);
            var alignmentFile = Storage.AlignmentFiles.Last();
            var aligner = aFactory.CreatePeakAligner();
            var result = aligner.Alignment(Storage.AnalysisFiles, alignmentFile, chromatogramSpotSerializer);
            result.Save(alignmentFile);
            MsdecResultsWriter.Write(alignmentFile.SpectraFilePath, LoadRepresentativeDeconvolutions(Storage, result.AlignmentSpotProperties).ToList());
        }

        private static IEnumerable<MSDecResult> LoadRepresentativeDeconvolutions(IMsdialDataStorage<MsdialLcImMsParameter> storage, IReadOnlyList<AlignmentSpotProperty> spots) {
            var files = storage.AnalysisFiles;

            var pointerss = new List<(int version, List<long> pointers, bool isAnnotationInfo)>();
            foreach (var file in files) {
                MsdecResultsReader.GetSeekPointers(file.DeconvolutionFilePath, out var version, out var pointers, out var isAnnotationInfo);
                pointerss.Add((version, pointers, isAnnotationInfo));
            }

            var streams = new List<FileStream>();
            try {
                streams = files.Select(file => File.OpenRead(file.DeconvolutionFilePath)).ToList();
                foreach (var spot in spots) {
                    var repID = spot.RepresentativeFileID;
                    var peakID = spot.AlignedPeakProperties[repID].MasterPeakID;
                    var decResult = MsdecResultsReader.ReadMSDecResult(
                        streams[repID], pointerss[repID].pointers[peakID],
                        pointerss[repID].version, pointerss[repID].isAnnotationInfo);
                    yield return decResult;
                    foreach (var dSpot in spot.AlignmentDriftSpotFeatures) {
                        var dRepID = dSpot.RepresentativeFileID;
                        var dPeakID = dSpot.AlignedPeakProperties[dRepID].MasterPeakID;
                        var dDecResult = MsdecResultsReader.ReadMSDecResult(
                            streams[dRepID], pointerss[dRepID].pointers[dPeakID],
                            pointerss[dRepID].version, pointerss[dRepID].isAnnotationInfo);
                        yield return dDecResult;
                    }
                }
            }
            finally {
                streams.ForEach(stream => stream.Close());
            }
        }

        public void SaveProject() {
            AlignmentModel?.SaveProject();
        }

        public void ShowTIC(Window owner) {
            var container = Storage;
            var analysisModel = AnalysisModel;
            if (analysisModel is null) return;

            var tic = analysisModel.EicLoader.LoadTic();
            var vm = new ChromatogramsViewModel(new ChromatogramsModel("Total ion chromatogram", new DisplayChromatogram(tic, new Pen(Brushes.Black, 1.0), "TIC"),
                "Total ion chromatogram", "Retention time", "Absolute ion abundance"));
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
            var vm = new ChromatogramsViewModel(new ChromatogramsModel("Base peak chromatogram", 
                new DisplayChromatogram(bpc, new Pen(Brushes.Red, 1.0), "BPC"),
                "Base peak chromatogram", "Retention time", "Absolute ion abundance"));
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
                    var vm = new ChromatogramsViewModel(new ChromatogramsModel("EIC", displayChroms, "EIC", "Retention time [min]", "Absolute ion abundance"));
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

            var vm = new ChromatogramsViewModel(new ChromatogramsModel("TIC, BPC, and highest peak m/z's EIC", displayChroms, "TIC, BPC, and highest peak m/z's EIC", "Retention time [min]", "Absolute ion abundance"));
            var view = new DisplayChromatogramsView() {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            view.Show();
        }
    }
}
