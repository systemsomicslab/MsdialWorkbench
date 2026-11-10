using CompMs.App.Msdial.Model.ImagingDims;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Dims;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.Imaging;
using CompMs.App.Msdial.ViewModel.Search;
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

internal sealed class WholeImageResultViewModel : ViewModelBase, IResultViewModel
{
    private readonly WholeImageResultModel _model;

    public WholeImageResultViewModel(WholeImageResultModel model, FocusControlManager focusManager, IWindowService<PeakSpotTableViewModelBase> peakSpotTableService, IMessageBroker broker) {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        var analysisViewModel = new DimsAnalysisViewModel(model.AnalysisModel, peakSpotTableService, broker, focusManager).AddTo(Disposables);
        AnalysisViewModel = analysisViewModel;

        ImagingRoiViewModel = new ImagingRoiViewModel(model.ImagingRoiModel).AddTo(Disposables);
        IntensityImagePlaceholder = model.IntensityImagePlaceholder.ObserveProperty(m => m.CurrentImage)
            .Select(m => m is null ? null : new BitmapImageViewModel(m))
            .DisposePreviousValue()
            .ToReadOnlyReactivePropertySlim().AddTo(Disposables);
    }

    public DimsAnalysisViewModel AnalysisViewModel { get; }
    public ImagingRoiViewModel ImagingRoiViewModel { get; }
    public AnalysisPeakPlotViewModel PeakPlotViewModel => AnalysisViewModel.PlotViewModel;
    public ReadOnlyReactivePropertySlim<BitmapImageViewModel?> IntensityImagePlaceholder { get; }

    public ICommand ShowIonTableCommand => AnalysisViewModel.ShowIonTableCommand;

    public ICommand SearchCompoundCommand => AnalysisViewModel.SearchCompoundCommand;

    // IResultViewModel
    public IResultModel Model => ((IResultViewModel)AnalysisViewModel).Model;
    public PeakSpotNavigatorViewModel PeakSpotNavigatorViewModel => AnalysisViewModel.PeakSpotNavigatorViewModel;
    public FocusNavigatorViewModel FocusNavigatorViewModel => AnalysisViewModel.FocusNavigatorViewModel;
    public ICommand SetUnknownCommand => AnalysisViewModel.SetUnknownCommand;
    public UndoManagerViewModel UndoManagerViewModel => AnalysisViewModel.UndoManagerViewModel;
    public ViewModelBase[] PeakDetailViewModels => AnalysisViewModel.PeakDetailViewModels;
}
