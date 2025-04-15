using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Information;
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

        VerticalAxis = new CategoryAxisManager<AnalysisFileBeanModel>(model.Files.AnalysisFiles, toLabel: f => f.AnalysisFileName) { ChartMargin = new ConstantMargin(0d), }.AddTo(Disposables);
        HorizontalAxis = new CategoryAxisManager<AnalysisFileBeanModel>(model.Files.AnalysisFiles, toLabel: f => f.AnalysisFileName) { ChartMargin = new ConstantMargin(0d) }.AddTo(Disposables);
        ValueAxis = new ContinuousAxisManager<double>(0d, 1d).AddTo(Disposables);
    }

    public ReactiveProperty<double> MzBin { get; }
    public ReactiveProperty<double> MzBegin { get; }
    public ReactiveProperty<double> MzEnd { get; }

    public ReadOnlyReactivePropertySlim<SimilarityMatrixItem[]> Result { get; }

    public CategoryAxisManager<AnalysisFileBeanModel> VerticalAxis { get; }
    public CategoryAxisManager<AnalysisFileBeanModel> HorizontalAxis { get; }
    public ContinuousAxisManager<double> ValueAxis { get; }

    public AsyncReactiveCommand UpdateCommand { get; }
}
