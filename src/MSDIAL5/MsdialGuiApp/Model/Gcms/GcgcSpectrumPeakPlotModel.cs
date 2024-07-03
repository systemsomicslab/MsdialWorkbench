using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;

namespace CompMs.App.Msdial.Model.Gcms;

internal sealed class GcgcSpectrumPeakPlotModel : AnalysisPeakPlotModel<Ms1BasedSpectrumFeature, ReadOnlyObservableCollection<Ms1BasedSpectrumFeature>>
{
    private readonly SerialDisposable _serialDisposable;

    public GcgcSpectrumPeakPlotModel(ReadOnlyObservableCollection<Ms1BasedSpectrumFeature> spots, Func<Ms1BasedSpectrumFeature, double> horizontalSelector, Func<Ms1BasedSpectrumFeature, double> verticalSelector, IReactiveProperty<Ms1BasedSpectrumFeature?> targetSource, IObservable<string?> labelSource, BrushMapData<Ms1BasedSpectrumFeature> selectedBrush, IList<BrushMapData<Ms1BasedSpectrumFeature>> brushes, PeakLinkModel peakLinkModel, IAxisManager<double>? horizontalAxis = null, IAxisManager<double>? verticalAxis = null)
        : base(spots, horizontalSelector, verticalSelector, targetSource, labelSource, selectedBrush, brushes, peakLinkModel, horizontalAxis, verticalAxis) {

        var disposable = new SerialDisposable();
        _serialDisposable = disposable;
        Disposables.Add(disposable);
        var axis = new DefectAxisManager(_timeStep, _timeStep, new RelativeMargin(.05));
        _secondColumnAxis = axis;
        _serialDisposable.Disposable = axis;
    }

    public double TimeStep {
        get => _timeStep;
        set {
            if (SetProperty(ref _timeStep, value)) {
                if (_timeStep > 0d) {
                    SecondColumnAxis = new DefectAxisManager(_timeStep, _timeStep, new RelativeMargin(.05));
                }
            }
        }
    }
    private double _timeStep = 1d;

    public IAxisManager<double> SecondColumnAxis {
        get => _secondColumnAxis;
        private set {
            if (SetProperty(ref _secondColumnAxis, value) && _secondColumnAxis is IDisposable disposable) {
                _serialDisposable.Disposable = disposable;
            }
        }
    }
    private IAxisManager<double> _secondColumnAxis;
}
