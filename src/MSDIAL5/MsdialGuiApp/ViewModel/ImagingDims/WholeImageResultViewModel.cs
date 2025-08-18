using CompMs.App.Msdial.Model.ImagingDims;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Dims;
using CompMs.App.Msdial.ViewModel.Imaging;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.ImagingDims;

internal sealed class WholeImageResultViewModel : ViewModelBase
{
    private readonly WholeImageResultModel _model;

    public WholeImageResultViewModel(WholeImageResultModel model, FocusControlManager focusManager, IWindowService<PeakSpotTableViewModelBase> peakSpotTableService, IMessageBroker broker) {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        var analysisViewModel = new DimsAnalysisViewModel(model.AnalysisModel, peakSpotTableService, broker, focusManager).AddTo(Disposables);
        AnalysisViewModel = analysisViewModel;

        Intensities = model.Intensities.ToReadOnlyReactiveCollection(intensity => new IntensityImageViewModel(intensity)).AddTo(Disposables);
        SelectedPeakIntensities = model.ToReactivePropertyAsSynchronized(
            m => m.SelectedPeakIntensities,
            mox => mox.Select(m => Intensities.FirstOrDefault(vm => vm.Model == m)),
            vmox => vmox.Select(vm => vm?.Model))
            .AddTo(Disposables);
        ImagingRoiViewModel = new ImagingRoiViewModel(model.ImagingRoiModel).AddTo(Disposables);
    }

    public DimsAnalysisViewModel AnalysisViewModel { get; }
    public ImagingRoiViewModel ImagingRoiViewModel { get; }
    public ReadOnlyReactiveCollection<IntensityImageViewModel> Intensities { get; }
    public AnalysisPeakPlotViewModel PeakPlotViewModel => AnalysisViewModel.PlotViewModel;
    public ReactiveProperty<IntensityImageViewModel> SelectedPeakIntensities { get; }

    public ICommand ShowIonTableCommand => AnalysisViewModel.ShowIonTableCommand;

    public ICommand SearchCompoundCommand => AnalysisViewModel.SearchCompoundCommand;
}
