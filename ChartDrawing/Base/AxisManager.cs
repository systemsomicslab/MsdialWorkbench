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
        AxisValue Min { get; }
        AxisValue Max { get; }
        Range Range { get; set; }
        Range InitialRange { get; }
        Range InitialValueRange { get; } // TODO: rename
        Range Bounds { get; }

        event EventHandler RangeChanged;

        AxisValue TranslateToAxisValue(object value);
        double TranslateToRenderPoint(AxisValue value, bool isFlipped, double drawableLength);
        double TranslateToRenderPoint(object value, bool isFlipped, double drawableLength);
        List<double> TranslateToRenderPoints(IEnumerable<object> values, bool isFlipped, double drawableLength);
        AxisValue TranslateFromRenderPoint(double value, bool isFlipped, double drawableLength);

        bool Contains(AxisValue value);
        bool Contains(object obj);
        void Focus(object low, object high);
        void Focus(Range range);
        List<LabelTickData> GetLabelTicks();
    }

    public interface IAxisManager<T> : IAxisManager
    {
        AxisValue TranslateToAxisValue(T value);
        double TranslateToRenderPoint(T value, bool isFlipped, double drawableLength);
        List<double> TranslateToRenderPoints(IEnumerable<T> values, bool isFlipped, double drawableLength);
        bool Contains(T obj);
        void Focus(T low, T high);
    }
}
