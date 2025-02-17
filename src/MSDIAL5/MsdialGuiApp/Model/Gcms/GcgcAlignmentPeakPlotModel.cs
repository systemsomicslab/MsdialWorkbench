using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace CompMs.App.Msdial.Model.Gcms;

internal sealed class GcgcAlignmentPeakPlotModel : AlignmentPeakPlotModel
{
    private readonly SerialDisposable _serialDisposable;

    public GcgcAlignmentPeakPlotModel(
        AlignmentSpotSource spots,
        Func<AlignmentSpotPropertyModel, double> horizontalSelector,
        Func<AlignmentSpotPropertyModel, double> verticalSelector,
        IReactiveProperty<AlignmentSpotPropertyModel?> targetSource,
        IObservable<string?> labelSource,
        BrushMapData<AlignmentSpotPropertyModel> selectedBrush,
        IList<BrushMapData<AlignmentSpotPropertyModel>> brushes,
        PeakLinkModel peakLinkModel,
        IAxisManager<double>? horizontalAxis = null)
        : base(spots, horizontalSelector, verticalSelector, targetSource, labelSource, selectedBrush, brushes, peakLinkModel, horizontalAxis) {
        var disposable = new SerialDisposable();
        _serialDisposable = disposable;
        Disposables.Add(disposable);
        var axis = new DefectAxisManager(_timeStep, _timeStep, new RelativeMargin(.05));
        VerticalAxis = axis;
        _serialDisposable.Disposable = axis;
    }

    public double TimeStep {
        get => _timeStep;
        set {
            if (SetProperty(ref _timeStep, value)) {
                if (_timeStep > 0d) {
                    VerticalAxis = new DefectAxisManager(_timeStep, _timeStep, new RelativeMargin(.05));
                }
            }
        }
    }
    private double _timeStep = 1d;
}
