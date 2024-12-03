using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Export;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Chart;

internal class AlignmentPeakPlotModel : DisposableModelBase
{
    private readonly AlignmentSpotSource? _spotsSource;

    public AlignmentPeakPlotModel(
        AlignmentSpotSource spotsSource,
        Func<AlignmentSpotPropertyModel, double> horizontalSelector,
        Func<AlignmentSpotPropertyModel, double> verticalSelector,
        IReactiveProperty<AlignmentSpotPropertyModel?> targetSource,
        IObservable<string?> labelSource,
        BrushMapData<AlignmentSpotPropertyModel> selectedBrush,
        IList<BrushMapData<AlignmentSpotPropertyModel>> brushes,
        PeakLinkModel peakLinkModel,
        IAxisManager<double>? horizontalAxis = null)
        : this(spotsSource.Spots?.Items, horizontalSelector, verticalSelector, targetSource, labelSource, selectedBrush, brushes, peakLinkModel, horizontalAxis) {

        _spotsSource = spotsSource;
    }

    public AlignmentPeakPlotModel(
        ReadOnlyObservableCollection<AlignmentSpotPropertyModel>? spots,
        Func<AlignmentSpotPropertyModel, double> horizontalSelector,
        Func<AlignmentSpotPropertyModel, double> verticalSelector,
        IReactiveProperty<AlignmentSpotPropertyModel?> targetSource,
        IObservable<string?> labelSource,
        BrushMapData<AlignmentSpotPropertyModel> selectedBrush,
        IList<BrushMapData<AlignmentSpotPropertyModel>> brushes,
        PeakLinkModel peakLinkModel,
        IAxisManager<double>? horizontalAxis = null) {
        if (horizontalSelector is null) {
            throw new ArgumentNullException(nameof(horizontalSelector));
        }

        if (verticalSelector is null) {
            throw new ArgumentNullException(nameof(verticalSelector));
        }

        if (brushes is null) {
            throw new ArgumentNullException(nameof(brushes));
        }

        Spots = spots ?? throw new ArgumentNullException(nameof(spots));
        TargetSource = targetSource ?? throw new ArgumentNullException(nameof(targetSource));
        LabelSource = labelSource ?? throw new ArgumentNullException(nameof(labelSource));
        _selectedBrush = selectedBrush ?? throw new ArgumentNullException(nameof(selectedBrush));
        PeakLinkModel = peakLinkModel;
        Brushes = new ReadOnlyCollection<BrushMapData<AlignmentSpotPropertyModel>>(brushes);

        GraphTitle = string.Empty;
        HorizontalTitle = string.Empty;
        VerticalTitle = string.Empty;
        HorizontalProperty = string.Empty;
        VerticalProperty = string.Empty;

        var unitRange = new AxisRange(0d, 1d);
        var collectionChanged = spots.CollectionChangedAsObservable().ToUnit().StartWith(Unit.Default).Publish();
        HorizontalAxis = horizontalAxis ?? collectionChanged
            .Select(_ => spots.Any() ? new AxisRange(spots.Min(horizontalSelector), spots.Max(horizontalSelector)) : unitRange)
            .ToReactiveContinuousAxisManager<double>(new RelativeMargin(0.05))
            .AddTo(Disposables);
        _verticalAxis = collectionChanged
            .Select(_ => spots.Any() ? new AxisRange(spots.Min(verticalSelector), spots.Max(verticalSelector)) : unitRange)
            .ToReactiveContinuousAxisManager<double>(new RelativeMargin(0.05))
            .AddTo(Disposables);
        Disposables.Add(collectionChanged.Connect());
    }

    public ReadOnlyObservableCollection<AlignmentSpotPropertyModel> Spots { get; }

    public IReactiveProperty<AlignmentSpotPropertyModel?> TargetSource { get; }

    public IAxisManager<double> HorizontalAxis { get; }
    public IAxisManager<double> VerticalAxis {
        get => _verticalAxis;
        protected set => SetProperty(ref _verticalAxis, value);
    }
    private IAxisManager<double> _verticalAxis;

    public string? GraphTitle {
        get => graphTitle;
        set => SetProperty(ref graphTitle, value);
    }
    private string? graphTitle;

    public string? HorizontalTitle {
        get => horizontalTitle;
        set => SetProperty(ref horizontalTitle, value);
    }
    private string? horizontalTitle;

    public string? VerticalTitle {
        get => verticalTitle;
        set => SetProperty(ref verticalTitle, value);
    }
    private string? verticalTitle;

    public string? HorizontalProperty {
        get => horizontalProperty;
        set => SetProperty(ref horizontalProperty, value);
    }
    private string? horizontalProperty;

    public string? VerticalProperty {
        get => verticalProperty;
        set => SetProperty(ref verticalProperty, value);
    }
    private string? verticalProperty;

    public IObservable<string?> LabelSource { get; }
    public PeakLinkModel PeakLinkModel { get; }
    public BrushMapData<AlignmentSpotPropertyModel> SelectedBrush {
        get => _selectedBrush;
        set => SetProperty(ref _selectedBrush, value);
    }
    private BrushMapData<AlignmentSpotPropertyModel> _selectedBrush;

    public ReadOnlyCollection<BrushMapData<AlignmentSpotPropertyModel>> Brushes { get; }

    public IObservable<bool> CanDuplicates => new[]{
        Observable.Return(_spotsSource is null),
        TargetSource.Select(t => t is null),
    }.CombineLatestValuesAreAllFalse();

    public Task DuplicatesAsync() {
        var spot = TargetSource.Value;
        if (spot is null || _spotsSource is null) {
            return Task.CompletedTask;
        }
        return _spotsSource.DuplicateSpotAsync(spot);
    }

    public IExportMrmprobsUsecase? ExportMrmprobs { get; set; }

    public ExportMrmprobsModel? ExportMrmprobsModel() {
        if (ExportMrmprobs is null) {
            return null;
        }
        return new ExportMrmprobsModel(ExportMrmprobs);
    }
}
