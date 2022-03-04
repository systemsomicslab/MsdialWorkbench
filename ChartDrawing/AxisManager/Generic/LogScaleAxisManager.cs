using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Graphics.AxisManager.Generic
{
    public class LogScaleAxisManager<T> : BaseAxisManager<T> where T : IConvertible
    {
        internal LogScaleAxisManager(Range range) : base(range) {

        }

        internal LogScaleAxisManager(Range range, T lowBound, T highBound) : base(range, new Range(ConvertToAxisValue(lowBound), ConvertToAxisValue(highBound))) {

        }

        internal LogScaleAxisManager(Range range, IChartMargin margin, T lowBound, T highBound) : base(range, margin, new Range(ConvertToAxisValue(lowBound), ConvertToAxisValue(highBound))) {

        }

        public LogScaleAxisManager(T low, T high)
            : base(new Range(ConvertToAxisValue(low), ConvertToAxisValue(high))) {

        }

        public LogScaleAxisManager(T low, T high, IChartMargin margin)
            : base(new Range(ConvertToAxisValue(low), ConvertToAxisValue(high)), margin) {

        }

        public LogScaleAxisManager(T low, T high, T lowBound, T highBound)
            : base(new Range(ConvertToAxisValue(low), ConvertToAxisValue(high)),
                  new Range(ConvertToAxisValue(lowBound), ConvertToAxisValue(highBound))) {

        }

        public LogScaleAxisManager(T low, T high, IChartMargin margin, T lowBound, T highBound)
            : base(new Range(ConvertToAxisValue(low), ConvertToAxisValue(high)),
                  margin,
                  new Range(ConvertToAxisValue(lowBound), ConvertToAxisValue(highBound))) {

        }

        public LogScaleAxisManager(ICollection<T> source) : this(source.DefaultIfEmpty().Min(), source.DefaultIfEmpty().Max()) {

        }

        public LogScaleAxisManager(ICollection<T> source, IChartMargin margin) : this(source.DefaultIfEmpty().Min(), source.DefaultIfEmpty().Max(), margin) {

        }

        public LogScaleAxisManager(ICollection<T> source, T low, T high) : this(source.DefaultIfEmpty().Min(), source.DefaultIfEmpty().Max(), low, high) {

        }

        public LogScaleAxisManager(ICollection<T> source, IChartMargin margin, T low, T high) : this(source.DefaultIfEmpty().Min(), source.DefaultIfEmpty().Max(), margin, low, high) {

        }

        public void UpdateInitialRange(T low, T high) {
            UpdateInitialRange(new Range(ConvertToAxisValue(low), ConvertToAxisValue(high)));
        }

        public void UpdateInitialRange(ICollection<T> source) {
            UpdateInitialRange(new Range(ConvertToAxisValue(source.DefaultIfEmpty().Min()), ConvertToAxisValue(source.DefaultIfEmpty().Max())));
        }

        protected override void OnRangeChanged() {
            labelTicks = null;
            base.OnRangeChanged();
        }

        private readonly ILabelGenerator labelGenerator = new LogScaleLabelGenerator();
        public override List<LabelTickData> GetLabelTicks() {
            var initialRangeCore = CoerceRange(InitialRangeCore, Bounds);
            List<LabelTickData> ticks;
            (ticks, UnitLabel) = labelGenerator.Generate(Range.Minimum.Value, Range.Maximum.Value, initialRangeCore.Minimum.Value, initialRangeCore.Maximum.Value);
            return ticks;
        }

        public override AxisValue TranslateToAxisValue(T value) {
            return ConvertToAxisValue(value);
        }

        private static AxisValue ConvertToAxisValue(T value) {
            return new AxisValue(Math.Log10(Convert.ToDouble(value)));
        }

        public static LogScaleAxisManager<T> Build<U>(IEnumerable<U> source, Func<U, T> map) {
            return new LogScaleAxisManager<T>(source.Select(map).ToList());
        }

        public static LogScaleAxisManager<T> Build<U>(IEnumerable<U> source, Func<U, T> map, T lowBound, T highBound) {
            return new LogScaleAxisManager<T>(source.Select(map).ToList(), lowBound, highBound);
        }

        public static LogScaleAxisManager<T> BuildDefault<U>(IEnumerable<U> source, Func<U, T> map) {
            return new LogScaleAxisManager<T>(source.Select(map).ToList());
        }

        public static LogScaleAxisManager<T> BuildDefault<U>(IEnumerable<U> source, Func<U, T> map, T lowBound, T highBound) {
            return new LogScaleAxisManager<T>(source.Select(map).ToList(), lowBound, highBound);
        }
    }
}
