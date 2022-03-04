using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Graphics.AxisManager.Generic
{
    public class ContinuousAxisManager<T> : BaseAxisManager<T> where T : IConvertible
    {
        public ContinuousAxisManager(Range range) : base(range) {

        }

        public ContinuousAxisManager(Range range, Range bounds) : base(range, bounds) {

        }

        public ContinuousAxisManager(Range range, IChartMargin margin) : base(range, margin) {

        }

        public ContinuousAxisManager(Range range, IChartMargin margin, Range bounds) : base(range, margin, bounds) {

        }

        public ContinuousAxisManager(T low, T high)
            : base(new Range(Convert.ToDouble(low), Convert.ToDouble(high))) {

        }

        public ContinuousAxisManager(Range range, T lowBound, T highBound)
            : base(range, new Range(Convert.ToDouble(lowBound), Convert.ToDouble(highBound))) {

        }

        public ContinuousAxisManager(T low, T high, T lowBound, T highBound)
            : base(new Range(Convert.ToDouble(low), Convert.ToDouble(high)),
                  new Range(Convert.ToDouble(lowBound), Convert.ToDouble(highBound))) {

        }

        public ContinuousAxisManager(T low, T high, Range bounds)
            : base(new Range(Convert.ToDouble(low), Convert.ToDouble(high)), bounds) {

        }

        public ContinuousAxisManager(ICollection<T> source) : this(source.DefaultIfEmpty().Min(), source.DefaultIfEmpty().Max()) {

        }

        public ContinuousAxisManager(ICollection<T> source, T low, T high) : this(source.DefaultIfEmpty().Min(), source.DefaultIfEmpty().Max(), low, high) {

        }

        public ContinuousAxisManager(ICollection<T> source, Range bounds) : this(source.DefaultIfEmpty().Min(), source.DefaultIfEmpty().Max(), bounds) {

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
            UpdateInitialRange(new Range(Convert.ToDouble(low), Convert.ToDouble(high)));
        }

        public void UpdateInitialRange(ICollection<T> source) {
            UpdateInitialRange(new Range(Convert.ToDouble(source.DefaultIfEmpty().Min()), Convert.ToDouble(source.DefaultIfEmpty().Max())));
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

        public static ContinuousAxisManager<T> Build<U>(IEnumerable<U> source, Func<U, T> map, Range bound) {
            return new ContinuousAxisManager<T>(source.Select(map).ToList(), bound);
        }
    }
}
