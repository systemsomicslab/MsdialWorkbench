using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.View.Setting;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.App.Msdial.ViewModel.Export;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.App.Msdial.ViewModel.Statistics;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.ViewModel.Lcms
{
    internal sealed class LcmsMethodViewModel : MethodViewModel {
        private readonly LcmsMethodModel _model;
        private readonly IMessageBroker _broker;
        private readonly MolecularNetworkingExportSettingViewModel _molecularNetworkingExportSettingViewModel;
        private readonly MolecularNetworkingSendingToCytoscapeJsSettingViewModel _molecularNetworkingSendingToCytoscapeJsSettingViewModel;

        private LcmsMethodViewModel(
            LcmsMethodModel model,
            IReadOnlyReactiveProperty<LcmsAnalysisViewModel?> analysisAsObservable,
            IReadOnlyReactiveProperty<LcmsAlignmentViewModel?> alignmentAsObservable,
            IMessageBroker broker,

            FocusControlManager focusControlManager)
            : base(
                  model, analysisAsObservable, alignmentAsObservable,
                  PrepareChromatogramViewModels(analysisAsObservable, alignmentAsObservable),
                  PrepareMassSpectrumViewModels(analysisAsObservable, alignmentAsObservable)) {

            _model = model;
            _broker = broker;
            Disposables.Add(focusControlManager);

            ShowExperimentSpectrumCommand = new ReactiveCommand().AddTo(Disposables);

            analysisAsObservable
                .Where(vm => vm is not null)
                .SelectSwitch(vm => ShowExperimentSpectrumCommand.WithLatestFrom(vm!.ExperimentSpectrumViewModel, (a, b) => b))
                .Subscribe(vm => broker.Publish(vm))
                .AddTo(Disposables);

            var selectedViewModel = this.ObserveProperty(vm => vm.SelectedViewModel)
                .SelectSwitch(vm => vm.StartWith(vm.Value))
                .ToReactiveProperty()
                .AddTo(Disposables);
            var proteinResultContainerAsObservable =
                new[]
                {
                    selectedViewModel.SkipNull().OfType<LcmsAnalysisViewModel>().Select(vm => vm.ProteinResultContainerAsObservable),
                    selectedViewModel.SkipNull().OfType<LcmsAlignmentViewModel>().Select(vm => vm.ProteinResultContainerAsObservable),
                }.Merge().Switch();

            var _proteinGroupTableViewModel = new ProteinGroupTableViewModel(proteinResultContainerAsObservable).AddTo(Disposables);
            ShowProteinGroupTableCommand = model.CanShowProteinGroupTable.ToReactiveCommand().AddTo(Disposables);
            ShowProteinGroupTableCommand.Subscribe(() => broker.Publish(_proteinGroupTableViewModel)).AddTo(Disposables);

            _molecularNetworkingExportSettingViewModel = new MolecularNetworkingExportSettingViewModel(_model.MolecularNetworkingSettingModel).AddTo(Disposables);
            _molecularNetworkingSendingToCytoscapeJsSettingViewModel = new MolecularNetworkingSendingToCytoscapeJsSettingViewModel(_model.MolecularNetworkingSettingModel).AddTo(Disposables);
            ExportParameterCommand = new AsyncReactiveCommand().WithSubscribe(model.ParameterExportModel.ExportAsync).AddTo(Disposables);

            var batchMsfinder = model.InternalMsfinderSettingModel;
            var msfinderBatchSettingVM = new InternalMsfinderBatchSettingViewModel(model.MsfinderSettingParameter, batchMsfinder, broker).AddTo(Disposables);
            ShowMsfinderSettingViewCommand = new ReactiveCommand().WithSubscribe(() => _broker.Publish(msfinderBatchSettingVM)).AddTo(Disposables);

            NotameViewModel = new NotameViewModel(model.Notame, broker).AddTo(Disposables);
        }

        protected override Task LoadAnalysisFileCoreAsync(AnalysisFileBeanViewModel analysisFile, CancellationToken token) {
            if (analysisFile?.File == null || analysisFile.File == _model.AnalysisFileModel) {
                return Task.CompletedTask;
            }
            return _model.LoadAnalysisFileAsync(analysisFile.File, token);
        }

        protected override Task LoadAlignmentFileCoreAsync(AlignmentFileBeanViewModel alignmentFile, CancellationToken token) {
            if (alignmentFile?.File == null || alignmentFile.File == _model.AlignmentFile) {
                return Task.CompletedTask;
            }
            return _model.LoadAlignmentFileAsync(alignmentFile.File, token);
        }

        public DelegateCommand ExportAnalysisResultCommand => _exportAnalysisResultCommand ??= new DelegateCommand(ExportAnalysis);
        private DelegateCommand? _exportAnalysisResultCommand;

        private void ExportAnalysis() {
            var m = _model.ExportAnalysis();
            using (var vm = new AnalysisResultExportViewModel(m)) {
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

        public DelegateCommand ShowTicCommand => _showTicCommand ??= new DelegateCommand(ShowChromatograms(tic: true));
        private DelegateCommand? _showTicCommand;

        public DelegateCommand ShowBpcCommand => _showBpcCommand ??= new DelegateCommand(ShowChromatograms(bpc: true));
        private DelegateCommand? _showBpcCommand;

        public DelegateCommand ShowEicCommand => _showEicCommand ??= new DelegateCommand(ShowChromatograms());
        private DelegateCommand? _showEicCommand;

        public DelegateCommand ShowTicBpcRepEICCommand => _showTicBpcRepEIC ??= new DelegateCommand(ShowChromatograms(tic: true, bpc: true, highestEic: true));
        private DelegateCommand? _showTicBpcRepEIC;

        private Action ShowChromatograms(bool tic = false, bool bpc = false, bool highestEic = false) {
            void InnerShowChromatorams() {
                var m = _model.ShowChromatograms(tic, bpc, highestEic);
                if (m is null) {
                    return;
                }
                var vm = new CheckChromatogramsViewModel(m, _broker);
                _broker.Publish(vm);
            }
            return InnerShowChromatorams;
        }

        public NotameViewModel NotameViewModel { get; set; }

        public ReactiveCommand ShowProteinGroupTableCommand { get; }

        public ReactiveCommand ShowExperimentSpectrumCommand { get; }

        public DelegateCommand<Window> ShowFragmentSearchSettingCommand => _fragmentSearchSettingCommand ??= new DelegateCommand<Window>(FragmentSearchSettingMethod);
        private DelegateCommand<Window>? _fragmentSearchSettingCommand;

        private void FragmentSearchSettingMethod(Window owner) {
            var m = _model.ShowShowFragmentSearchSettingView();

            var vm = new FragmentQuerySettingViewModel(m);
            vm.IsAlignSpotViewSelected.Value = SelectedViewModel.Value is IAlignmentResultViewModel;

            var dialog = new FragmentQuerySettingView() {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            if (dialog.ShowDialog() == true) {
                m.Search();
            }
        }

        public ReactiveCommand ShowMsfinderSettingViewCommand { get; }

        public DelegateCommand<Window> ShowMassqlSearchSettingCommand => _massqlSearchSettingCommand??= new DelegateCommand<Window>(MassqlSearchSettingMethod);
        private DelegateCommand<Window>? _massqlSearchSettingCommand;

        private void MassqlSearchSettingMethod(Window owner) {
            if (SelectedViewModel.Value is null) {
                return;
            }
            MassqlSettingModel? m = _model.ShowShowMassqlSearchSettingView(SelectedViewModel.Value.Model);
            if (m is null) {
                return;
            }

            var vm = new MassqlSettingViewModel(m);
            var dialog = new MassqlSettingView()
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            dialog.Show();
        }

        public DelegateCommand<Window> ShowMscleanrFilterSettingCommand => _mscleanrFilterSettingCommand ??= new DelegateCommand<Window>(MscleanrFilterSettingMethod);
        private DelegateCommand<Window>? _mscleanrFilterSettingCommand;

        private void MscleanrFilterSettingMethod(Window owner) {
            if (SelectedViewModel.Value is IAlignmentResultViewModel) {
                var m = _model.ShowShowMscleanrFilterSettingView();
                if (m is null) {
                    return;
                }
                var vm = new MscleanrSettingViewModel(m);
                var dialog = new MscleanrSettingView()
                {
                    DataContext = vm,
                    Owner = owner,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };
                dialog.Show();
            }
            else {
                MessageBox.Show("Please select an alignment result file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
                //Console.WriteLine("Please select an item in Alignment navigator!!");
            }
        }

        public DelegateCommand ShowMolecularNetworkingExportSettingCommand => _molecularNetworkingExportSettingCommand ??= new DelegateCommand(MolecularNetworkingExportSettingMethod);
        private DelegateCommand? _molecularNetworkingExportSettingCommand;

        private void MolecularNetworkingExportSettingMethod()
        {
            _broker.Publish(_molecularNetworkingExportSettingViewModel);
        }

        public DelegateCommand ShowMolecularNetworkingVisualizationSettingCommand => _molecularNetworkingVisualizationSettingCommand ??= new DelegateCommand(MolecularNetworkingVisualizationSettingMethod);
        private DelegateCommand? _molecularNetworkingVisualizationSettingCommand;

        private void MolecularNetworkingVisualizationSettingMethod() {
            _broker.Publish(_molecularNetworkingSendingToCytoscapeJsSettingViewModel);
        }

        public AsyncReactiveCommand ExportParameterCommand { get; }

        private static IReadOnlyReactiveProperty<LcmsAnalysisViewModel?> ConvertToAnalysisViewModelAsObservable(
            LcmsMethodModel method,
            IMessageBroker broker,
            FocusControlManager focusManager) {
            if (method is null) {
                throw new ArgumentNullException(nameof(method));
            }
            if (focusManager is null) {
                throw new ArgumentNullException(nameof(focusManager));
            }

            ReadOnlyReactivePropertySlim<LcmsAnalysisViewModel?>? result;
            using (var subject = new Subject<LcmsAnalysisModel?>()) {
                result = subject.Concat(method.ObserveProperty(m => m.AnalysisModel, isPushCurrentValueAtFirst: false)) // If 'isPushCurrentValueAtFirst' = true or using 'StartWith', first value can't release.
                    .Select(m => m is null ? null : new LcmsAnalysisViewModel(m, broker, focusManager))
                    .DisposePreviousValue()
                    .ToReadOnlyReactivePropertySlim();
                subject.OnNext(method.AnalysisModel);
                subject.OnCompleted();
            }
            return result;
        }

        private static IReadOnlyReactiveProperty<LcmsAlignmentViewModel?> ConvertToAlignmentViewModelAsObservable(
            LcmsMethodModel method,
            IMessageBroker broker,
            FocusControlManager focusControlManager) {
            if (method is null) {
                throw new ArgumentNullException(nameof(method));
            }
            if (focusControlManager is null) {
                throw new ArgumentNullException(nameof(focusControlManager));
            }

            ReadOnlyReactivePropertySlim<LcmsAlignmentViewModel?>? result;
            using (var subject = new Subject<LcmsAlignmentModel?>()) {
                result = subject.Concat(method.ObserveProperty(m => m.AlignmentModel, isPushCurrentValueAtFirst: false)) // If 'isPushCurrentValueAtFirst' = true or using 'StartWith', first value can't release.
                    .Select(m => m is null ? null : new LcmsAlignmentViewModel(m, broker, focusControlManager))
                    .DisposePreviousValue()
                    .ToReadOnlyReactivePropertySlim();
                subject.OnNext(method.AlignmentModel);
                subject.OnCompleted();
            }
            return result;
        }

        private static ViewModelSwitcher PrepareChromatogramViewModels(IObservable<LcmsAnalysisViewModel?> analysisAsObservable, IObservable<LcmsAlignmentViewModel?> alignmentAsObservable) {
            var eic = analysisAsObservable.Select(vm => vm?.EicViewModel);
            var bar = alignmentAsObservable.Select(vm => vm?.BarChartViewModel);
            var alignmentEic = alignmentAsObservable.Select(vm => vm?.AlignmentEicViewModel);
            return new ViewModelSwitcher(eic, bar, new IObservable<ViewModelBase?>[] { eic, bar, alignmentEic});
        }

        private static ViewModelSwitcher PrepareMassSpectrumViewModels(IObservable<LcmsAnalysisViewModel?> analysisAsObservable, IObservable<LcmsAlignmentViewModel?> alignmentAsObservable) {
            var rawdec = analysisAsObservable.Select(vm => vm?.RawDecSpectrumsViewModel);
            var rawpur = analysisAsObservable.Select(vm => vm?.RawPurifiedSpectrumsViewModel);
            var ms2chrom = analysisAsObservable.Select(vm => vm?.Ms2ChromatogramsViewModel);
            var repref = alignmentAsObservable.Select(vm => vm?.Ms2SpectrumViewModel);
            return new ViewModelSwitcher(rawdec, repref, new IObservable<ViewModelBase?>[] { rawdec, ms2chrom, rawpur, repref});
        }

        public static LcmsMethodViewModel Create(
                LcmsMethodModel model,
                IMessageBroker broker) {

            var focusControlManager = new FocusControlManager();
            var analysisAsObservable = ConvertToAnalysisViewModelAsObservable(model, broker, focusControlManager);
            var alignmentAsObservable = ConvertToAlignmentViewModelAsObservable(model, broker, focusControlManager);

            return new LcmsMethodViewModel(model, analysisAsObservable, alignmentAsObservable, broker, focusControlManager);
        }
    }
}