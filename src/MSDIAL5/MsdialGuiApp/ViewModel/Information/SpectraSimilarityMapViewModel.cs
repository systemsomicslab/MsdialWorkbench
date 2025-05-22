using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Information;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace CompMs.App.Msdial.ViewModel.Information;

internal sealed class SpectraSimilarityMapViewModel : ViewModelBase
{
    private readonly SpectraSimilarityMapModel _model;

    public SpectraSimilarityMapViewModel(SpectraSimilarityMapModel model) {
        _model = model;

        MzBin = model.ToReactivePropertyAsSynchronized(m => m.MzBin).AddTo(Disposables);
        MzBegin = model.ToReactivePropertyAsSynchronized(m => m.MzBegin).AddTo(Disposables);
        MzEnd = model.ToReactivePropertyAsSynchronized(m => m.MzEnd).AddTo(Disposables);
        Result = model.ObserveProperty(m => m.Result).ToReadOnlyReactivePropertySlim([]).AddTo(Disposables);

        UpdateCommand = new AsyncReactiveCommand().WithSubscribe(() => model.UpdateSimilaritiesAsync()).AddTo(Disposables);

        HeatmapVerticalAxis = new CategoryAxisManager<AnalysisFileBeanModel>(model.Files.AnalysisFiles, toLabel: f => f.AnalysisFileName) { ChartMargin = new ConstantMargin(0d), }.AddTo(Disposables);
        HeatmapHorizontalAxis = new CategoryAxisManager<AnalysisFileBeanModel>(model.Files.AnalysisFiles, toLabel: f => f.AnalysisFileName) { ChartMargin = new ConstantMargin(0d) }.AddTo(Disposables);
        HeatmapClusteredVerticalAxis = model.ObserveProperty(m => m.OrderedFiles).ToReactiveCategoryAxisManager(toLabel: f => f.AnalysisFileName).AddTo(Disposables);
        HeatmapClusteredVerticalAxis.ChartMargin = new ConstantMargin(0d);
        HeatmapClusteredHorizontalAxis = model.ObserveProperty(m => m.OrderedFiles).ToReactiveCategoryAxisManager(toLabel: f => f.AnalysisFileName).AddTo(Disposables);
        HeatmapClusteredHorizontalAxis.ChartMargin = new ConstantMargin(0d);
        ValueAxis = new ContinuousAxisManager<double>(0d, 1d).AddTo(Disposables);


        SelectedMatrixItem = model.ToReactivePropertySlimAsSynchronized(m => m.SelectedMatrixItem).AddTo(Disposables);
    }

    public ReactiveProperty<double> MzBin { get; }
    public ReactiveProperty<double> MzBegin { get; }
    public ReactiveProperty<double> MzEnd { get; }

    public ReactivePropertySlim<SimilarityMatrixItem?> SelectedMatrixItem { get; }

    public ReadOnlyReactivePropertySlim<SimilarityMatrixItem[]> Result { get; }

    public CategoryAxisManager<AnalysisFileBeanModel> HeatmapVerticalAxis { get; }
    public CategoryAxisManager<AnalysisFileBeanModel> HeatmapHorizontalAxis { get; }
    public CategoryAxisManager<AnalysisFileBeanModel> HeatmapClusteredVerticalAxis { get; }
    public CategoryAxisManager<AnalysisFileBeanModel> HeatmapClusteredHorizontalAxis { get; }
    public ContinuousAxisManager<double> ValueAxis { get; }

    public IAxisManager<double> SpectrumHorizontalAxis => _model.HorizontalAxis;
    public IAxisManager<double> SpectrumUpperVerticalAxis => _model.UpperVerticalAxis;
    public IAxisManager<double> SpectrumLowerVerticalAxis => _model.LowerVerticalAxis;

    public AsyncReactiveCommand UpdateCommand { get; }
}
