using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.Graphics.Base
{
    public class AxisMapper : IAxisManager
    {
        public Range InitialRange => manager.InitialRange;
        public AxisValue InitialMin => manager.InitialRange.Minimum;
        public AxisValue InitialMax => manager.InitialRange.Maximum;

        public AxisValue Min => manager.Min;
        public AxisValue Max => manager.Max;
        public Range Range {
            get => manager.Range;
            set => manager.Range = value;
        }
        public Range Bounds => manager.Bounds;

        private readonly IAxisManager manager;

        public event EventHandler RangeChanged;

        public AxisMapper(IAxisManager manager_) {
            manager = manager_;
            manager.RangeChanged += (s, e) => RangeChanged?.Invoke(this, e);
        }

        public AxisValue TranslateToAxisValue(object value) {
            return manager.TranslateToAxisValue(value);
        }

        public double TranslateToRenderPoint(AxisValue value, bool isFlipped) {
            return manager.TranslateToRenderPoint(value, isFlipped);
        }

        public double TranslateToRenderPoint(object value, bool isFlipped) {
            return manager.TranslateToRenderPoint(value, isFlipped);
        }

        public AxisValue TranslateFromRenderPoint(double value, bool isFlipped) {
            return manager.TranslateFromRenderPoint(value, isFlipped);
        }

        public List<double> TranslateToRenderPoints(IEnumerable<object> values, bool isFlipped) {
            return manager.TranslateToRenderPoints(values, isFlipped);
        }

        public bool Contains(AxisValue val) {
            return manager.Contains(val);
        }

        public bool Contains(object obj) {
            return manager.Contains(obj);
        }

        public void Focus(object low, object high) {
            manager.Focus(low, high);
        }

        public void Focus(Range range) {
            manager.Focus(range);
        }

        public List<LabelTickData> GetLabelTicks() {
            return manager.GetLabelTicks();
        }
    }
}
