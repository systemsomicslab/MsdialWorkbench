using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.Model.Export;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.App.Msdial.ViewModel.Export;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialDimsCore.Export;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.ViewModel.Dims
{
    internal sealed class DimsMethodViewModel : MethodViewModel {
        private readonly DimsMethodModel _model;
        private readonly IMessageBroker _broker;
        private readonly FocusControlManager _focusControlManager;

        private DimsMethodViewModel(
            DimsMethodModel model,
            IMessageBroker broker,
            IReadOnlyReactiveProperty<DimsAnalysisViewModel?> analysisVM,
            IReadOnlyReactiveProperty<DimsAlignmentViewModel?> alignmentVM,
            ViewModelSwitcher chromatogramViewModels,
            ViewModelSwitcher massSpectrumViewModels,
            FocusControlManager focusControlManager)
            : base(model, analysisVM, alignmentVM, chromatogramViewModels, massSpectrumViewModels) {

            _model = model;
            _broker = broker;
            _focusControlManager = focusControlManager.AddTo(Disposables);

            PeakFilterViewModel = new PeakFilterViewModel(model.PeakFilterModel).AddTo(Disposables);
            ExportParameterCommand = new AsyncReactiveCommand().WithSubscribe(model.ParameterExportModel.ExportAsync).AddTo(Disposables);

            var batchMsfinder = model.InternalMsfinderSettingModel;
            var msfinderBatchSettingVM = new InternalMsfinderBatchSettingViewModel(model.MsfinderParameterSetting, batchMsfinder, broker).AddTo(Disposables);
            ShowMsfinderSettingViewCommand = new ReactiveCommand().WithSubscribe(() => _broker.Publish(msfinderBatchSettingVM)).AddTo(Disposables);
        }

        public PeakFilterViewModel PeakFilterViewModel { get; }
        public AsyncReactiveCommand ExportParameterCommand { get; }

        protected override Task LoadAnalysisFileCoreAsync(AnalysisFileBeanViewModel analysisFile, CancellationToken token) {
            if (analysisFile?.File == null || _model.AnalysisFileModel == analysisFile.File) {
                return Task.CompletedTask;
            }
            return _model.LoadAnalysisFileAsync(analysisFile.File, token);
        }

        protected override Task LoadAlignmentFileCoreAsync(AlignmentFileBeanViewModel alignmentFile, CancellationToken token) {
            if (alignmentFile?.File == null || _model.AlignmentFile == alignmentFile.File) {
                return Task.CompletedTask;
            }
            return _model.LoadAlignmentFileAsync(alignmentFile.File, token);
        }

        public DelegateCommand ExportAnalysisResultCommand => _exportAnalysisResultCommand ??= new DelegateCommand(ExportAnalysis);
        private DelegateCommand? _exportAnalysisResultCommand;

        private void ExportAnalysis() {
            var container = _model.Storage;
            var spectraTypes = new List<SpectraType>
            {
                new SpectraType(
                    ExportspectraType.deconvoluted,
                    new DimsAnalysisMetadataAccessor(container.DataBaseMapper, container.Parameter, ExportspectraType.deconvoluted),
                    _model.ProviderFactory),
                //new SpectraType(
                //    ExportspectraType.centroid,
                //    new DimsAnalysisMetadataAccessor(container.DataBaseMapper, container.Parameter, ExportspectraType.centroid)),
                //new SpectraType(
                //    ExportspectraType.profile,
                //    new DimsAnalysisMetadataAccessor(container.DataBaseMapper, container.Parameter, ExportspectraType.profile)),
            };
            var spectraFormats = new[]
            {
                new SpectraFormat(ExportSpectraFileFormat.txt, new AnalysisCSVExporterFactory(separator: "\t")),
            };

            var models = new IMsdialAnalysisExport[]
            {
                new MsdialAnalysisTableExportModel(spectraTypes, spectraFormats, _broker),
                new SpectraTypeSelectableMsdialAnalysisExportModel(new Dictionary<ExportspectraType, IAnalysisExporter<ChromatogramPeakFeatureCollection>> {
                    [ExportspectraType.deconvoluted] = new AnalysisMspExporter(container.DataBaseMapper, container.Parameter),
                    [ExportspectraType.centroid] = new AnalysisMspExporter(container.DataBaseMapper, container.Parameter, file => new CentroidMsScanPropertyLoader(_model.ProviderFactory.Create(file), container.Parameter.MS2DataType))
                })
                {
                    FilePrefix = "Msp",
                    FileSuffix = "msp",
                    Label = "Nist format (*.msp)"
                },
                new SpectraTypeSelectableMsdialAnalysisExportModel(new Dictionary<ExportspectraType, IAnalysisExporter<ChromatogramPeakFeatureCollection>> {
                    [ExportspectraType.deconvoluted] = new AnalysisMgfExporter(file => new MSDecLoader(file.DeconvolutionFilePath, file.DeconvolutionFilePathList)),
                    [ExportspectraType.centroid] = new AnalysisMgfExporter(file => new CentroidMsScanPropertyLoader(_model.ProviderFactory.Create(file), container.Parameter.MS2DataType))
                })
                {
                    FilePrefix = "Mgf",
                    FileSuffix = "mgf",
                    Label = "MASCOT format (*.mgf)"
                },
                new MsdialAnalysisMassBankRecordExportModel(container.Parameter.ProjectParam, _model.StudyContext),
            };
            var model = new AnalysisResultExportModel(_model.AnalysisFileModelCollection, _model.Storage.Parameter.ProjectParam.ProjectFolderPath, _broker, models);
            using (var vm = new AnalysisResultExportViewModel(model)) {
                _broker.Publish(vm);
            }
        }

        public DelegateCommand ExportAlignmentResultCommand => _exportAlignmentResultCommand ??= new DelegateCommand(ExportAlignment);
        private DelegateCommand? _exportAlignmentResultCommand;

        private void ExportAlignment() {
            using (var vm = new AlignmentResultExportViewModel(_model.AlignmentResultExportModel, _broker)) {
                _broker.Publish(vm);
            }
        }

        private static IReadOnlyReactiveProperty<DimsAnalysisViewModel?> ConvertToAnalysisViewModel(
            DimsMethodModel method,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IMessageBroker broker,
            FocusControlManager focusControlManager) {
            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }
            ReadOnlyReactivePropertySlim<DimsAnalysisViewModel?> result;
            using (var subject = new Subject<DimsAnalysisModel?>()) {
                result = subject.Concat(method.ObserveProperty(m => m.AnalysisModel, isPushCurrentValueAtFirst: false)) // If 'isPushCurrentValueAtFirst' = true or using 'StartWith', first value can't release.
                    .Select(m => m is null ? null : new DimsAnalysisViewModel(m, peakSpotTableService, broker, focusControlManager))
                    .DisposePreviousValue()
                    .ToReadOnlyReactivePropertySlim();
                subject.OnNext(method.AnalysisModel);
                subject.OnCompleted();
            }
            return result;
        }

        private static IReadOnlyReactiveProperty<DimsAlignmentViewModel?> ConvertToAlignmentViewModel(
            DimsMethodModel method,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IMessageBroker broker,
            FocusControlManager focusControlManager) {
            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }

            if (focusControlManager is null) {
                throw new ArgumentNullException(nameof(focusControlManager));
            }

            return method.ObserveProperty(m => m.AlignmentModel)
                .SkipNull()
                .Select(m => new DimsAlignmentViewModel(m, peakSpotTableService, broker, focusControlManager))
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim();
        }

        public static DimsMethodViewModel Create(
            DimsMethodModel model,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IMessageBroker broker) {
            var focusControlManager = new FocusControlManager();
            var analysisVM = ConvertToAnalysisViewModel(model, peakSpotTableService, broker, focusControlManager);
            var alignmentVM = ConvertToAlignmentViewModel(model, peakSpotTableService, broker, focusControlManager);
            var chromvms = PrepareChromatogramViewModels(analysisVM, alignmentVM);
            var msvms = PrepareMassSpectrumViewModels(analysisVM, alignmentVM);
            return new DimsMethodViewModel(model, broker, analysisVM, alignmentVM, chromvms, msvms, focusControlManager);
        }

        private static ViewModelSwitcher PrepareChromatogramViewModels(IObservable<DimsAnalysisViewModel?> analysisAsObservable, IObservable<DimsAlignmentViewModel?> alignmentAsObservable) {
            var eic = analysisAsObservable.Select(vm => vm?.EicViewModel);
            var bar = alignmentAsObservable.Select(vm => vm?.BarChartViewModel);
            var alignmentEic = alignmentAsObservable.Select(vm => vm?.AlignmentEicViewModel);
            return new ViewModelSwitcher(eic, bar, new IObservable<ViewModelBase?>[] { eic, bar, alignmentEic});
        }

        private static ViewModelSwitcher PrepareMassSpectrumViewModels(IObservable<DimsAnalysisViewModel?> analysisAsObservable, IObservable<DimsAlignmentViewModel?> alignmentAsObservable) {
            var rawdec = analysisAsObservable.Select(vm => vm?.RawDecSpectrumsViewModel);
            var rawpur = Observable.Return<ViewModelBase?>(null); // analysisAsObservable.Select(vm => vm?.RawPurifiedSpectrumsViewModel);
            var ms2chrom = analysisAsObservable.Select(vm => vm?.Ms2ChromatogramsViewModel);
            var repref = alignmentAsObservable.Select(vm => vm?.Ms2SpectrumViewModel);
            return new ViewModelSwitcher(rawdec, repref, new IObservable<ViewModelBase?>[] { rawdec, ms2chrom, rawpur, repref});
        }
        public ReactiveCommand ShowMsfinderSettingViewCommand { get; }
    }
}
