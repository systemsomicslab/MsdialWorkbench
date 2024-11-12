using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Graphics.AxisManager.Generic
{
    public sealed class ConstantAxisManager<T> : IAxisManager<T>
    {
        public static ConstantAxisManager<T> Instance { get; } = new ConstantAxisManager<T>();

        public AxisRange Range { get; } = new AxisRange(new AxisValue(-1d), new AxisValue(1d));

        public event EventHandler RangeChanged {
            add { }
            remove { }
        }
        public event EventHandler InitialRangeChanged {
            add { }
            remove { }
        }
        public event EventHandler AxisValueMappingChanged {
            add { }
            remove { }
        }

        public bool Contains(AxisValue value) {
            return true;
        }

        public bool ContainsCurrent(AxisValue value) {
            return true;
        }

        public void Focus(AxisRange range) {
            
        }

        public List<LabelTickData> GetLabelTicks() {
            return new List<LabelTickData>(0);
        }

        public void Recalculate(double drawableLength) {

        }

        public void Reset() {

        }

        public AxisValue TranslateFromRenderPoint(double value, bool isFlipped, double drawableLength) {
            return new AxisValue(0d);
        }

        public AxisValue TranslateToAxisValue(T value) {
            return new AxisValue(0d);
        }

        public AxisValue TranslateToAxisValue(object value) {
            return new AxisValue(0d);
        }

        public double TranslateToRenderPoint(AxisValue value, bool isFlipped, double drawableLength) {
            return .5d * drawableLength;
        }

        public List<double> TranslateToRenderPoints(IEnumerable<T> values, bool isFlipped, double drawableLength) {
            return Enumerable.Repeat(.5d * drawableLength, values.Count()).ToList();
        }

        public List<double> TranslateToRenderPoints(IEnumerable<object> values, bool isFlipped, double drawableLength) {
            return Enumerable.Repeat(.5d * drawableLength, values.Count()).ToList();
        }

        public List<double> TranslateToRenderPoints(IEnumerable<AxisValue> values, bool isFlipped, double drawableLength) {
            return Enumerable.Repeat(.5d * drawableLength, values.Count()).ToList();
        }
    }

    public static class ConstantAxisManager {
        public static IAxisManager Instance { get; } = new ConstantAxisManager<object>();
    }
}
