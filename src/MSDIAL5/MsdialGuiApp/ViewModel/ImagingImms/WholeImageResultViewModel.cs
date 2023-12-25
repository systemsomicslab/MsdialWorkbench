using CompMs.App.Msdial.Model.ImagingImms;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Imaging;
using CompMs.App.Msdial.ViewModel.Imms;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.ImagingImms
{
    internal sealed class WholeImageResultViewModel : ViewModelBase
    {
        private readonly WholeImageResultModel _model;

        public WholeImageResultViewModel(WholeImageResultModel model, FocusControlManager focusManager, IWindowService<PeakSpotTableViewModelBase> peakSpotTableService, IMessageBroker broker) {
            _model = model ?? throw new System.ArgumentNullException(nameof(model));
            var analysisViewModel = new ImmsAnalysisViewModel(model.AnalysisModel, peakSpotTableService, broker, focusManager).AddTo(Disposables);
            AnalysisViewModel = analysisViewModel;

            Intensities = model.Intensities.ToReadOnlyReactiveCollection(intensity => new IntensityImageViewModel(intensity)).AddTo(Disposables);
            SelectedPeakIntensities = model.ToReactivePropertyAsSynchronized(
                m => m.SelectedPeakIntensities,
                mox => mox.Select(m => Intensities.FirstOrDefault(vm => vm.Model == m)),
                vmox => vmox.Select(vm => vm?.Model))
                .AddTo(Disposables);
            ImagingRoiViewModel = new ImagingRoiViewModel(model.ImagingRoiModel).AddTo(Disposables);
        }

        public ImmsAnalysisViewModel AnalysisViewModel { get; }
        public AnalysisPeakPlotViewModel PeakPlotViewModel => AnalysisViewModel.PlotViewModel;
        public ReadOnlyReactiveCollection<IntensityImageViewModel> Intensities { get; }
        public ReactiveProperty<IntensityImageViewModel> SelectedPeakIntensities { get; }
        public ImagingRoiViewModel ImagingRoiViewModel { get; }

        public ICommand ShowIonTableCommand => AnalysisViewModel.ShowIonTableCommand;

        public ICommand SearchCompoundCommand => AnalysisViewModel.SearchCompoundCommand;
    }
}
