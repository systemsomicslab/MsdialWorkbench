using System;
using System.Collections.Generic;

namespace CompMs.Graphics.Core.Base
{
    public enum TickType
    {
        LongTick, ShortTick
    }

    public class LabelTickData
    {
        public string Label { get; set; }
        public TickType TickType { get; set; }
        public double Center { get; set; }
        public double Width { get; set; }
        public object Source { get; set; }
    }

    public interface IAxisManager {
        AxisRange Range { get; }

        event EventHandler RangeChanged;
        event EventHandler InitialRangeChanged;
        event EventHandler AxisValueMappingChanged;

        AxisValue TranslateToAxisValue(object value);
        double TranslateToRenderPoint(AxisValue value, bool isFlipped, double drawableLength);
        List<double> TranslateToRenderPoints(IEnumerable<object> values, bool isFlipped, double drawableLength);
        List<double> TranslateToRenderPoints(IEnumerable<AxisValue> values, bool isFlipped, double drawableLength);
        AxisValue TranslateFromRenderPoint(double value, bool isFlipped, double drawableLength);

        bool Contains(AxisValue value);
        bool ContainsCurrent(AxisValue value);
        void Focus(AxisRange range);
        void Reset();
        void Recalculate(double drawableLength);
        List<LabelTickData> GetLabelTicks();
    }

    public interface IAxisManager<T> : IAxisManager
    {
        AxisValue TranslateToAxisValue(T value);
        List<double> TranslateToRenderPoints(IEnumerable<T> values, bool isFlipped, double drawableLength);
    }

    public static class AxisManager
    {
        public static double TranslateToRenderPoint(this IAxisManager axis, object value, bool isFlipped, double drawbleLength) {
            return axis.TranslateToRenderPoint(axis.TranslateToAxisValue(value), isFlipped, drawbleLength);
        }

        public static double TranslateToRenderPoint<T>(this IAxisManager<T> axis, T value, bool isFlipped, double drawableLength) {
            return axis.TranslateToRenderPoint(axis.TranslateToAxisValue(value), isFlipped, drawableLength);
        }

        public static bool Contains(this IAxisManager axis, AxisRange range) {
            return axis.Contains(range.Minimum) && axis.Contains(range.Maximum);
        }

        public static bool Contains(this IAxisManager axis, object value) {
            return axis.Contains(axis.TranslateToAxisValue(value));
        }

        public static bool Contains<T>(this IAxisManager<T> axis, T value) {
            return axis.Contains(axis.TranslateToAxisValue(value));
        }

        public static void Focus(this IAxisManager axis, object low, object high) {
            axis.Focus(new AxisRange(axis.TranslateToAxisValue(low), axis.TranslateToAxisValue(high)));
        }

        public static void Focus<T>(this IAxisManager<T> axis, T low, T high) {
            axis.Focus(new AxisRange(axis.TranslateToAxisValue(low), axis.TranslateToAxisValue(high)));
        }
    }
}
