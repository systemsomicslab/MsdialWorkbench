using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.Model.Gcms;
using CompMs.App.Msdial.Model.ImagingDims;
using CompMs.App.Msdial.Model.ImagingImms;
using CompMs.App.Msdial.Model.Imms;
using CompMs.App.Msdial.Model.Lcimms;
using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.ViewModel.Dims;
using CompMs.App.Msdial.ViewModel.Gcms;
using CompMs.App.Msdial.ViewModel.ImagingDims;
using CompMs.App.Msdial.ViewModel.ImagingImms;
using CompMs.App.Msdial.ViewModel.Imms;
using CompMs.App.Msdial.ViewModel.Lcimms;
using CompMs.App.Msdial.ViewModel.Lcms;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Core
{
    internal sealed class DatasetViewModel : ViewModelBase
    {
        private readonly IWindowService<PeakSpotTableViewModelBase> peakSpotTableService;
        private readonly IMessageBroker _messageBroker;

        public DatasetViewModel(
            IDatasetModel model,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IMessageBroker messageBroker) {
            Model = model;
            this.peakSpotTableService = peakSpotTableService;
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
            var projectSettingViewModel = new ProjectPropertySettingViewModel(((DatasetModel)model).StudyContext).AddTo(Disposables);
            ProjectSettingCommand = new ReactiveCommand()
                .WithSubscribe(() => messageBroker.Publish(projectSettingViewModel))
                .AddTo(Disposables);
            SaveParameterAsCommand = new ReactiveCommand()
                .WithSubscribe(() => model.SaveParameterAsAsync())
                .AddTo(Disposables);
        }

        public IDatasetModel Model { get; }

        public ParameterBase? Parameter => Model?.Storage?.Parameter; // To show the project folder path in MainWindow title.

        public ReadOnlyReactivePropertySlim<MethodViewModel?> MethodViewModel { get; }

        public ReactiveCommand FilePropertyResetCommand { get; }

        private void FilePropertyResetting() {
            var model = Model.AnalysisFilePropertyResetModel;
            using var analysisFilePropertyResetWindowVM = new AnalysisFilePropertyResetViewModel(model);
            _messageBroker.Publish(analysisFilePropertyResetWindowVM);
            var afpsw_result = analysisFilePropertyResetWindowVM.Result;// analysisFilePropertyResetService.ShowDialog(analysisFilePropertyResetWindowVM);
            if (afpsw_result == true) {
                model.Update();
            }
        }

        public ReactiveCommand FileClassSettingCommand { get; }
        public ReactiveCommand ProjectSettingCommand { get; }
        public ReactiveCommand SaveParameterAsCommand { get; }

        private MethodViewModel? ConvertToViewModel(IMethodModel? model) {
            return model switch
            {
                LcmsMethodModel lc => LcmsMethodViewModel.Create(lc, _messageBroker),
                ImmsMethodModel im => ImmsMethodViewModel.Create(im, peakSpotTableService, _messageBroker),
                DimsMethodModel di => DimsMethodViewModel.Create(di, peakSpotTableService, _messageBroker),
                LcimmsMethodModel lcim => LcimmsMethodViewModel.Create(lcim, peakSpotTableService, _messageBroker),
                GcmsMethodModel gc => GcmsMethodViewModel.Create(gc, peakSpotTableService, _messageBroker),
                ImagingImmsMethodModel iim => new ImagingImmsMainViewModel(iim, _messageBroker, peakSpotTableService),
                ImagingDimsMethodModel idi => new ImagingDimsMainViewModel(idi, _messageBroker, peakSpotTableService),
                _ => null,
            };
        }
    }
}
