using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Graphics.AxisManager.Generic
{
    public class ContinuousAxisManager<T> : BaseAxisManager<T> where T : IConvertible
    {
        public ContinuousAxisManager(AxisRange range) : base(range) {

        }

        public ContinuousAxisManager(AxisRange range, AxisRange bounds) : base(range, bounds) {

        }

        public ContinuousAxisManager(AxisRange range, IChartMargin margin) : base(range, margin) {

        }

        public ContinuousAxisManager(AxisRange range, IChartMargin margin, AxisRange bounds) : base(range, margin, bounds) {

        }

        public ContinuousAxisManager(AxisRange range, IChartMargin margin, T lowBound, T highBound) : base(range, margin, new AxisRange(Convert.ToDouble(lowBound), Convert.ToDouble(highBound))) {

        }

        public ContinuousAxisManager(T low, T high)
            : base(new AxisRange(Convert.ToDouble(low), Convert.ToDouble(high))) {

        }

        public ContinuousAxisManager(T low, T high, IChartMargin margin)
            : base(new AxisRange(Convert.ToDouble(low), Convert.ToDouble(high)), margin) {

        }

        public ContinuousAxisManager(AxisRange range, T lowBound, T highBound)
            : base(range, new AxisRange(Convert.ToDouble(lowBound), Convert.ToDouble(highBound))) {

        }

        public ContinuousAxisManager(T low, T high, T lowBound, T highBound)
            : base(new AxisRange(Convert.ToDouble(low), Convert.ToDouble(high)),
                  new AxisRange(Convert.ToDouble(lowBound), Convert.ToDouble(highBound))) {

        }

        public ContinuousAxisManager(T low, T high, AxisRange bounds)
            : base(new AxisRange(Convert.ToDouble(low), Convert.ToDouble(high)), bounds) {

        }

        public ContinuousAxisManager(T low, T high, IChartMargin margin, AxisRange bounds)
            : base(new AxisRange(Convert.ToDouble(low), Convert.ToDouble(high)), margin, bounds) {

        }

        public ContinuousAxisManager(T low, T high, IChartMargin margin, T lowBound, T highBound)
            : base(new AxisRange(Convert.ToDouble(low), Convert.ToDouble(high)), margin, new AxisRange(Convert.ToDouble(lowBound), Convert.ToDouble(highBound))) {

        }

        public ContinuousAxisManager(ICollection<T> source) : this(source.DefaultIfEmpty().Min(), source.DefaultIfEmpty().Max()) {

        }

        public ContinuousAxisManager(ICollection<T> source, IChartMargin margin) : this(source.DefaultIfEmpty().Min(), source.DefaultIfEmpty().Max(), margin) {

        }

        public ContinuousAxisManager(ICollection<T> source, T low, T high) : this(source.DefaultIfEmpty().Min(), source.DefaultIfEmpty().Max(), low, high) {

        }

        public ContinuousAxisManager(ICollection<T> source, AxisRange bounds) : this(source.DefaultIfEmpty().Min(), source.DefaultIfEmpty().Max(), bounds) {

        }

        public ContinuousAxisManager(ICollection<T> source, IChartMargin margin, AxisRange bounds) : this(source.DefaultIfEmpty().Min(), source.DefaultIfEmpty().Max(), margin, bounds) {

        }

        public LabelType LabelType {
            get => labelType;
            set => SetProperty(ref labelType, value);
        }
        private LabelType labelType = LabelType.Standard;

        private ILabelGenerator LabelGenerator {
            get {
                switch (LabelType) {
                    case LabelType.Order:
                        return labelGenerator is OrderLabelGenerator
                            ? labelGenerator
                            : labelGenerator = new OrderLabelGenerator();
                    case LabelType.Relative:
                        return labelGenerator is RelativeLabelGenerator
                            ? labelGenerator
                            : labelGenerator = new RelativeLabelGenerator();
                    case LabelType.Percent:
                        return labelGenerator is PercentLabelGenerator
                            ? labelGenerator
                            : labelGenerator = new PercentLabelGenerator();
                    case LabelType.Standard:
                    default:
                        return labelGenerator is StandardLabelGenerator
                            ? labelGenerator
                            : labelGenerator = new StandardLabelGenerator();
                }
            }
        }
        private ILabelGenerator labelGenerator;

        public void UpdateInitialRange(T low, T high) {
            UpdateInitialRange(new AxisRange(Convert.ToDouble(low), Convert.ToDouble(high)));
        }

        public void UpdateInitialRange(ICollection<T> source) {
            UpdateInitialRange(new AxisRange(Convert.ToDouble(source.DefaultIfEmpty().Min()), Convert.ToDouble(source.DefaultIfEmpty().Max())));
        }

        protected override void OnRangeChanged() {
            labelTicks = null;
            base.OnRangeChanged();
        }

        public override List<LabelTickData> GetLabelTicks() {
            var generator = LabelGenerator;
            var initialRangeCore = CoerceRange(InitialRangeCore, Bounds);
            List<LabelTickData> ticks;
            (ticks, UnitLabel) = generator.Generate(Range.Minimum.Value, Range.Maximum.Value, initialRangeCore.Minimum.Value, initialRangeCore.Maximum.Value);
            return ticks;
        }

        public override AxisValue TranslateToAxisValue(T value) {
            return new AxisValue(Convert.ToDouble(value));
        }

        public static ContinuousAxisManager<T> Build<U>(IEnumerable<U> source, Func<U, T> map) {
            return new ContinuousAxisManager<T>(source.Select(map).ToList());
        }

        public static ContinuousAxisManager<T> Build<U>(IEnumerable<U> source, Func<U, T> map, T lowBound, T highBound) {
            return new ContinuousAxisManager<T>(source.Select(map).ToList(), lowBound, highBound);
        }

        public static ContinuousAxisManager<T> Build<U>(IEnumerable<U> source, Func<U, T> map, AxisRange bound) {
            return new ContinuousAxisManager<T>(source.Select(map).ToList(), bound);
        }
    }
}
