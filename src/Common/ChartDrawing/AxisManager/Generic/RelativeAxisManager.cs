using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Graphics.AxisManager.Generic
{
    public sealed class RelativeAxisManager : ViewModelBase, IAxisManager<double>
    {
        private static readonly AxisRange RELATIVE_RANGE = new AxisRange(0d, 1d);
        private readonly IAxisManager<double> _axisManagerImpl;

        public RelativeAxisManager(double low, double high, IAxisManager<double> shareAxis) {
            Low = low;
            High = high;
            Delta = high - low;
            _axisManagerImpl = shareAxis;
        }

        public RelativeAxisManager(double low, double high, RelativeAxisManager shareAxis) : this(low, high, shareAxis._axisManagerImpl) {

        }

        private RelativeAxisManager(double low, double high, ContinuousAxisManager<double> impl) : this(low, high, (IAxisManager<double>)impl) {
            Disposables.Add(impl);
        }

        public RelativeAxisManager(double low, double high) : this(low, high, new ContinuousAxisManager<double>(RELATIVE_RANGE)) {

        }

        public RelativeAxisManager(double low, double high, AxisRange bounds) : this(low, high, new ContinuousAxisManager<double>(RELATIVE_RANGE, bounds)) {

        }

        public RelativeAxisManager(double low, double high, IChartMargin margin) : this(low, high, new ContinuousAxisManager<double>(RELATIVE_RANGE, margin)) {

        }

        public RelativeAxisManager(double low, double high, IChartMargin margin, AxisRange bounds) : this(low, high, new ContinuousAxisManager<double>(RELATIVE_RANGE, margin, bounds)) {

        }

        public double Low { get; }
        public double High { get; }
        public double Delta { get; }

        public AxisRange Range => new AxisRange(_axisManagerImpl.Range.Minimum.Value * Delta + Low, _axisManagerImpl.Range.Maximum.Value * Delta + Low);

        public event EventHandler RangeChanged {
            add => _axisManagerImpl.RangeChanged += value;
            remove => _axisManagerImpl.RangeChanged -= value;
        }

        public event EventHandler InitialRangeChanged {
            add => _axisManagerImpl.InitialRangeChanged += value;
            remove => _axisManagerImpl.InitialRangeChanged -= value;
        }

        public event EventHandler AxisValueMappingChanged {
            add => _axisManagerImpl.AxisValueMappingChanged += value;
            remove => _axisManagerImpl.AxisValueMappingChanged -= value;
        }

        public bool Contains(AxisValue value) => _axisManagerImpl.Contains(value);

        public bool ContainsCurrent(AxisValue value) => _axisManagerImpl.ContainsCurrent(value);

        public void Focus(AxisRange range) => _axisManagerImpl.Focus(range);

        public List<LabelTickData> GetLabelTicks() => _axisManagerImpl.GetLabelTicks();

        public void Recalculate(double drawableLength) => _axisManagerImpl.Recalculate(drawableLength);

        public void Reset() => _axisManagerImpl.Reset();

        public AxisValue TranslateFromRenderPoint(double value, bool isFlipped, double drawableLength) => _axisManagerImpl.TranslateFromRenderPoint(value, isFlipped, drawableLength);

        public AxisValue TranslateToAxisValue(double value) {
            return _axisManagerImpl.TranslateToAxisValue((value - Low) / Delta);
        }

        public AxisValue TranslateToAxisValue(object value) => TranslateToAxisValue((double)value);

        public double TranslateToRenderPoint(AxisValue value, bool isFlipped, double drawableLength) => _axisManagerImpl.TranslateToRenderPoint(value, isFlipped, drawableLength);

        public List<double> TranslateToRenderPoints(IEnumerable<double> values, bool isFlipped, double drawableLength) {
            return _axisManagerImpl.TranslateToRenderPoints(values.Select(val => (val - Low) / Delta), isFlipped, drawableLength);
        }

        public List<double> TranslateToRenderPoints(IEnumerable<object> values, bool isFlipped, double drawableLength) {
            return TranslateToRenderPoints(values.OfType<double>(), isFlipped, drawableLength);
        }

        public List<double> TranslateToRenderPoints(IEnumerable<AxisValue> values, bool isFlipped, double drawableLength) => _axisManagerImpl.TranslateToRenderPoints(values, isFlipped, drawableLength);

        public static IAxisManager<double> CreateBaseAxis() {
            return new ContinuousAxisManager<double>(RELATIVE_RANGE);
        }

        public static IAxisManager<double> CreateBaseAxis(AxisRange bounds) {
            return new ContinuousAxisManager<double>(RELATIVE_RANGE, bounds);
        }

        public static IAxisManager<double> CreateBaseAxis(IChartMargin margin) {
            return new ContinuousAxisManager<double>(RELATIVE_RANGE, margin);
        }

        public static IAxisManager<double> CreateBaseAxis(IChartMargin margin, AxisRange bounds) {
            return new ContinuousAxisManager<double>(RELATIVE_RANGE, margin, bounds);
        }
    }
}
