using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Gcms;

internal sealed class GcgcAlignmentPeakPlotModel : AlignmentPeakPlotModel
{
    private readonly DefectAxisManager _axis;

    public GcgcAlignmentPeakPlotModel(
        AlignmentSpotSource spots,
        AxisPropertySelectors<double> horizontalSelector,
        AxisPropertySelectors<double> verticalSelector,
        IReactiveProperty<AlignmentSpotPropertyModel?> targetSource,
        IObservable<string?> labelSource,
        BrushMapData<AlignmentSpotPropertyModel> selectedBrush,
        IList<BrushMapData<AlignmentSpotPropertyModel>> brushes,
        PeakLinkModel peakLinkModel)
        : base(spots, horizontalSelector, verticalSelector, targetSource, labelSource, selectedBrush, brushes, peakLinkModel) {
        var axis = new DefectAxisManager(_timeStep, _timeStep, new RelativeMargin(.05)).AddTo(Disposables);
        _axis = axis;

        var axisItem = new AxisItemModelWithValue<double>("2nd column", axis, "2nd column retention time (min)")
        {
            ValueLabel = "TimeStep",
            Value = 1d,
        };
        axisItem.PropertyChanged += (s, e) => {
            if (e.PropertyName == nameof(AxisItemModelWithValue<double>.Value)) {
                axis.Factor = axis.Divisor = ((AxisItemModelWithValue<double>)s).Value;
            }
        };

        verticalSelector.AxisItemSelector.AxisItems.Add(axisItem);
        verticalSelector.AxisItemSelector.SelectedAxisItem = axisItem;
    }

    public double TimeStep {
        get => _timeStep;
        set {
            if (SetProperty(ref _timeStep, value)) {
                if (_timeStep > 0d) {
                    _axis.Divisor = _axis.Factor = _timeStep;
                }
            }
        }
    }
    private double _timeStep = 1d;
}
