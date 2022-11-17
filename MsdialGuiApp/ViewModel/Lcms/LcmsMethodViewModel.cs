using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.View.Export;
using CompMs.App.Msdial.View.Setting;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.App.Msdial.ViewModel.Export;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
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

        private LcmsMethodViewModel(
            LcmsMethodModel model,
            IReadOnlyReactiveProperty<LcmsAnalysisViewModel> analysisAsObservable,
            IReadOnlyReactiveProperty<LcmsAlignmentViewModel> alignmentAsObservable,
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
                .Where(vm => vm != null)
                .Select(vm => ShowExperimentSpectrumCommand.WithLatestFrom(vm.ExperimentSpectrumViewModel, (a, b) => b))
                .Switch()
                .Subscribe(vm => broker.Publish(vm))
                .AddTo(Disposables);

            var selectedViewModel = this.ObserveProperty(vm => vm.SelectedViewModel)
                .Select(vm => vm.StartWith(vm.Value))
                .Switch()
                .ToReactiveProperty()
                .AddTo(Disposables);
            var proteinResultContainerAsObservable =
                new[]
                {
                    selectedViewModel.OfType<LcmsAnalysisViewModel>().Select(vm => vm.ProteinResultContainerAsObservable),
                    selectedViewModel.OfType<LcmsAlignmentViewModel>().Select(vm => vm.ProteinResultContainerAsObservable),
                }.Merge().Switch();

            var _proteinGroupTableViewModel = new ProteinGroupTableViewModel(proteinResultContainerAsObservable).AddTo(Disposables);
            ShowProteinGroupTableCommand = model.CanShowProteinGroupTable.ToReactiveCommand().AddTo(Disposables);
            ShowProteinGroupTableCommand.Subscribe(() => broker.Publish(_proteinGroupTableViewModel)).AddTo(Disposables);
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

        public DelegateCommand<Window> ExportAnalysisResultCommand => _exportAnalysisResultCommand ?? (_exportAnalysisResultCommand = new DelegateCommand<Window>(ExportAnalysis));
        private DelegateCommand<Window> _exportAnalysisResultCommand;

        private void ExportAnalysis(Window owner) {
            var m = _model.ExportAnalysis();
            using (var vm = new AnalysisResultExportViewModel(m)) {
                var dialog = new AnalysisResultExportWin
                {
                    DataContext = vm,
                    Owner = owner,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };

                dialog.ShowDialog();
            }
        }

        public DelegateCommand ExportAlignmentResultCommand => _exportAlignmentResultCommand ?? (_exportAlignmentResultCommand = new DelegateCommand(ExportAlignment));
        private DelegateCommand _exportAlignmentResultCommand;

        private void ExportAlignment() {
            using (var vm = new AlignmentResultExport2VM(_model.AlignmentResultExportModel, _broker)) {
                _broker.Publish(vm);
            }
        }

        public DelegateCommand ShowTicCommand => _showTicCommand ?? (_showTicCommand = new DelegateCommand(ShowTIC));
        private DelegateCommand _showTicCommand;

        private void ShowTIC() {
            var m = _model.ShowTIC();
            if (m is null) {
                return;
            }
            var vm = new ChromatogramsViewModel(m);
            _broker.Publish(vm);
        }

        public DelegateCommand ShowBpcCommand => _showBpcCommand ?? (_showBpcCommand = new DelegateCommand(ShowBPC));
        private DelegateCommand _showBpcCommand;

        private void ShowBPC() {
            var m = _model.ShowBPC();
            if (m is null) {
                return;
            }
            var vm = new ChromatogramsViewModel(m);
            _broker.Publish(vm);
        }

        public DelegateCommand ShowEicCommand => _showEicCommand ?? (_showEicCommand = new DelegateCommand(ShowEIC));
        private DelegateCommand _showEicCommand;

        private void ShowEIC() {
            var m = _model.ShowEIC();
            using (var settingvm = new DisplayEicSettingViewModel(m)) {
                _broker.Publish(settingvm);
                if (!settingvm.DialogResult) {
                    return;
                }
            }
            var chromatograms = m.PrepareChromatograms();
            if (chromatograms is null) {
                return;
            }
            var vm = new ChromatogramsViewModel(chromatograms);
            _broker.Publish(vm);
        }

        public DelegateCommand ShowTicBpcRepEICCommand => _showTicBpcRepEIC ?? (_showTicBpcRepEIC = new DelegateCommand(ShowTicBpcRepEIC));
        private DelegateCommand _showTicBpcRepEIC;

        private void ShowTicBpcRepEIC() {
            var m = _model.ShowTicBpcRepEIC();
            if (m is null) {
                return;
            }
            var vm = new ChromatogramsViewModel(m);
            _broker.Publish(vm);
        }

        public ReactiveCommand ShowProteinGroupTableCommand { get; }

        public ReactiveCommand ShowExperimentSpectrumCommand { get; }

        public DelegateCommand<Window> ShowFragmentSearchSettingCommand => _fragmentSearchSettingCommand ??
            (_fragmentSearchSettingCommand = new DelegateCommand<Window>(FragmentSearchSettingMethod));
        private DelegateCommand<Window> _fragmentSearchSettingCommand;

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

        public DelegateCommand<Window> ShowMassqlSearchSettingCommand => _massqlSearchSettingCommand ??
            (_massqlSearchSettingCommand= new DelegateCommand<Window>(MassqlSearchSettingMethod));
        private DelegateCommand<Window> _massqlSearchSettingCommand;

        private void MassqlSearchSettingMethod(Window owner) {
            MassqlSettingModel m = _model.ShowShowMassqlSearchSettingView(SelectedViewModel.Value.Model);
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

        public DelegateCommand<Window> ShowMscleanrFilterSettingCommand => _mscleanrFilterSettingCommand ??
            (_mscleanrFilterSettingCommand = new DelegateCommand<Window>(MscleanrFilterSettingMethod));
        private DelegateCommand<Window> _mscleanrFilterSettingCommand;

        private void MscleanrFilterSettingMethod(Window owner) {
            if (SelectedViewModel.Value is IAlignmentResultViewModel) {
                var m = _model.ShowShowMscleanrFilterSettingView();
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

        private static IReadOnlyReactiveProperty<LcmsAnalysisViewModel> ConvertToAnalysisViewModelAsObservable(
            LcmsMethodModel method,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IWindowService<PeakSpotTableViewModelBase> proteomicsTableService,
            FocusControlManager focusManager) {
            if (method is null) {
                throw new ArgumentNullException(nameof(method));
            }

            if (compoundSearchService is null) {
                throw new ArgumentNullException(nameof(compoundSearchService));
            }

            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }

            if (proteomicsTableService is null) {
                throw new ArgumentNullException(nameof(proteomicsTableService));
            }

            if (focusManager is null) {
                throw new ArgumentNullException(nameof(focusManager));
            }

            ReadOnlyReactivePropertySlim<LcmsAnalysisViewModel> result;
            using (var subject = new Subject<LcmsAnalysisModel>()) {
                result = subject.Concat(method.ObserveProperty(m => m.AnalysisModel, isPushCurrentValueAtFirst: false)) // If 'isPushCurrentValueAtFirst' = true or using 'StartWith', first value can't release.
                    .Select(m => m is null ? null : new LcmsAnalysisViewModel(m, compoundSearchService, peakSpotTableService, proteomicsTableService, focusManager))
                    .DisposePreviousValue()
                    .ToReadOnlyReactivePropertySlim();
                subject.OnNext(method.AnalysisModel);
                subject.OnCompleted();
            }
            return result;
        }

        private static IReadOnlyReactiveProperty<LcmsAlignmentViewModel> ConvertToAlignmentViewModelAsObservable(
            LcmsMethodModel method,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IWindowService<PeakSpotTableViewModelBase> proteomicsTableService,
            IMessageBroker broker,
            FocusControlManager focusControlManager) {
            if (method is null) {
                throw new ArgumentNullException(nameof(method));
            }

            if (compoundSearchService is null) {
                throw new ArgumentNullException(nameof(compoundSearchService));
            }

            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }

            if (proteomicsTableService is null) {
                throw new ArgumentNullException(nameof(proteomicsTableService));
            }

            if (focusControlManager is null) {
                throw new ArgumentNullException(nameof(focusControlManager));
            }

            ReadOnlyReactivePropertySlim<LcmsAlignmentViewModel> result;
            using (var subject = new Subject<LcmsAlignmentModel>()) {
                result = subject.Concat(method.ObserveProperty(m => m.AlignmentModel, isPushCurrentValueAtFirst: false)) // If 'isPushCurrentValueAtFirst' = true or using 'StartWith', first value can't release.
                    .Select(m => m is null ? null : new LcmsAlignmentViewModel(m, compoundSearchService, peakSpotTableService, proteomicsTableService, broker, focusControlManager))
                    .DisposePreviousValue()
                    .ToReadOnlyReactivePropertySlim();
                subject.OnNext(method.AlignmentModel);
                subject.OnCompleted();
            }
            return result;
        }

        private static ViewModelSwitcher PrepareChromatogramViewModels(IObservable<LcmsAnalysisViewModel> analysisAsObservable, IObservable<LcmsAlignmentViewModel> alignmentAsObservable) {
            var eic = analysisAsObservable.Select(vm => vm?.EicViewModel);
            var bar = alignmentAsObservable.Select(vm => vm?.BarChartViewModel);
            var alignmentEic = alignmentAsObservable.Select(vm => vm?.AlignmentEicViewModel);
            return new ViewModelSwitcher(eic, bar, new IObservable<ViewModelBase>[] { eic, bar, alignmentEic});
        }

        private static ViewModelSwitcher PrepareMassSpectrumViewModels(IObservable<LcmsAnalysisViewModel> analysisAsObservable, IObservable<LcmsAlignmentViewModel> alignmentAsObservable) {
            var rawdec = analysisAsObservable.Select(vm => vm?.RawDecSpectrumsViewModel);
            var rawpur = analysisAsObservable.Select(vm => vm?.RawPurifiedSpectrumsViewModel);
            var ms2chrom = analysisAsObservable.Select(vm => vm?.Ms2ChromatogramsViewModel);
            var repref = alignmentAsObservable.Select(vm => vm?.Ms2SpectrumViewModel);
            return new ViewModelSwitcher(rawdec, repref, new IObservable<ViewModelBase>[] { rawdec, ms2chrom, rawpur, repref});
        }

        public static LcmsMethodViewModel Create(
                LcmsMethodModel model,
                IWindowService<CompoundSearchVM> compoundSearchService,
                IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
                IWindowService<PeakSpotTableViewModelBase> proteomicsTableService,
                IMessageBroker broker) {

            var focusControlManager = new FocusControlManager();
            var analysisAsObservable = ConvertToAnalysisViewModelAsObservable(model, compoundSearchService, peakSpotTableService, proteomicsTableService, focusControlManager);
            var alignmentAsObservable = ConvertToAlignmentViewModelAsObservable(model, compoundSearchService, peakSpotTableService, proteomicsTableService, broker, focusControlManager);

            return new LcmsMethodViewModel(model, analysisAsObservable, alignmentAsObservable, broker, focusControlManager);
        }
    }
}
