using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.Model.Gcms;
using CompMs.App.Msdial.Model.ImagingImms;
using CompMs.App.Msdial.Model.Imms;
using CompMs.App.Msdial.Model.Lcimms;
using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.ViewModel.Dims;
using CompMs.App.Msdial.ViewModel.Gcms;
using CompMs.App.Msdial.ViewModel.ImagingImms;
using CompMs.App.Msdial.ViewModel.Imms;
using CompMs.App.Msdial.ViewModel.Lcimms;
using CompMs.App.Msdial.ViewModel.Lcms;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Core
{
    internal sealed class DatasetViewModel : ViewModelBase
    {
        private readonly IWindowService<CompoundSearchVM> compoundSearchService;
        private readonly IWindowService<PeakSpotTableViewModelBase> peakSpotTableService;
        private readonly IWindowService<PeakSpotTableViewModelBase> proteomicsTableService;
        private readonly IWindowService<AnalysisFilePropertyResetViewModel> analysisFilePropertyResetService;
        private readonly IMessageBroker _messageBroker;

        public DatasetViewModel(
            IDatasetModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IWindowService<PeakSpotTableViewModelBase> proteomicsTableService,
            IWindowService<AnalysisFilePropertyResetViewModel> analysisFilePropertyResetService,
            IMessageBroker messageBroker) {
            Model = model;
            this.compoundSearchService = compoundSearchService;
            this.peakSpotTableService = peakSpotTableService;
            this.proteomicsTableService = proteomicsTableService;
            this.analysisFilePropertyResetService = analysisFilePropertyResetService;
            _messageBroker = messageBroker;
            MethodViewModel = model.ObserveProperty(m => m.Method)
                .Select(ConvertToViewModel)
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            FilePropertyResetCommand = new ReactiveCommand()
                .WithSubscribe(FilePropertyResetting)
                .AddTo(Disposables);
            var fileClassSetViewModel = new FileClassSetViewModel(model.FileClassSetModel).AddTo(Disposables);
            FileClassSettingCommand = new ReactiveCommand()
                .WithSubscribe(() => messageBroker.Publish(fileClassSetViewModel))
                .AddTo(Disposables);
            SaveParameterAsCommand = new ReactiveCommand()
                .WithSubscribe(() => model.SaveParameterAsAsync())
                .AddTo(Disposables);
        }

        public IDatasetModel Model { get; }

        public ParameterBase Parameter => Model?.Storage?.Parameter; // To show the project folder path in MainWindow title.

        public ReadOnlyReactivePropertySlim<MethodViewModel> MethodViewModel { get; }

        public ReactiveCommand FilePropertyResetCommand { get; }

        private void FilePropertyResetting() {
            var model = Model.AnalysisFilePropertyResetModel;
            using (var analysisFilePropertyResetWindowVM = new AnalysisFilePropertyResetViewModel(model)) {
                var afpsw_result = analysisFilePropertyResetService.ShowDialog(analysisFilePropertyResetWindowVM);
                if (afpsw_result == true) {
                    model.Update();
                }
            }
        }

        public ReactiveCommand FileClassSettingCommand { get; }

        public ReactiveCommand SaveParameterAsCommand { get; }

        private MethodViewModel ConvertToViewModel(IMethodModel model) {
            switch (model) {
                case LcmsMethodModel lc:
                    return LcmsMethodViewModel.Create(lc, compoundSearchService, peakSpotTableService, proteomicsTableService, _messageBroker);
                case ImmsMethodModel im:
                    return ImmsMethodViewModel.Create(im, compoundSearchService, peakSpotTableService, _messageBroker);
                case DimsMethodModel di:
                    return DimsMethodViewModel.Create(di, compoundSearchService, peakSpotTableService, _messageBroker);
                case LcimmsMethodModel lcim:
                    return LcimmsMethodViewModel.Create(lcim, compoundSearchService, peakSpotTableService, _messageBroker);
                case GcmsMethodModel gc:
                    return GcmsMethodViewModel.Create(gc, peakSpotTableService, _messageBroker);
                case ImagingImmsMethodModel iim:
                    return new ImagingImmsMainViewModel(iim, _messageBroker, compoundSearchService, peakSpotTableService);
                default:
                    return null;
            }
        }
    }
}
