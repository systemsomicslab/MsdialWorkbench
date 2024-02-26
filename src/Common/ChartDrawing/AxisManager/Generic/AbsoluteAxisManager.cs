using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Graphics.AxisManager.Generic
{
    public sealed class AbsoluteAxisManager : BaseAxisManager<double>
    {
        public AbsoluteAxisManager(AxisRange range) : base(range) {
        }

        public AbsoluteAxisManager(AxisRange range, AxisRange bounds) : base(range, bounds) {

        }

        public AbsoluteAxisManager(AxisRange range, IChartMargin margin) : base(range, margin) {

        }

        public AbsoluteAxisManager(AxisRange range, IChartMargin margin, AxisRange bounds) : base(range, margin, bounds) {

        }

        public AbsoluteAxisManager(IEnumerable<double> source) : this(ToRange(source)) {

        }

        public AbsoluteAxisManager(IEnumerable<double> source, AxisRange bounds) : this(ToRange(source), bounds) {

        }

        public AbsoluteAxisManager(IEnumerable<double> source, IChartMargin margin) : this(ToRange(source), margin) {

        }

        public AbsoluteAxisManager(IEnumerable<double> source, IChartMargin margin, AxisRange bounds) : this(ToRange(source), margin, bounds) {

        }

        public LabelType LabelType {
            get => _labelType;
            set => SetProperty(ref _labelType, value);
        }
        private LabelType _labelType = LabelType.Standard;

        private ILabelGenerator LabelGenerator {
            get {
                switch (LabelType) {
                    case LabelType.Order:
                        return _labelGenerator is OrderLabelGenerator
                            ? _labelGenerator
                            : _labelGenerator = new OrderLabelGenerator();
                    case LabelType.Relative:
                        return _labelGenerator is RelativeLabelGenerator
                            ? _labelGenerator
                            : _labelGenerator = new RelativeLabelGenerator();
                    case LabelType.Percent:
                        return _labelGenerator is PercentLabelGenerator
                            ? _labelGenerator
                            : _labelGenerator = new PercentLabelGenerator();
                    case LabelType.Standard:
                    default:
                        return _labelGenerator is StandardLabelGenerator
                            ? _labelGenerator
                            : _labelGenerator = new StandardLabelGenerator();
                }
            }
        }
        private ILabelGenerator _labelGenerator;

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

        public override AxisValue TranslateToAxisValue(double value) {
            return new AxisValue(Math.Abs(value));
        }

        public static AbsoluteAxisManager Build<T>(IEnumerable<T> source, Func<T, double> map) {
            return new AbsoluteAxisManager(ToRange(source.Select(map)));
        }

        public static AbsoluteAxisManager Build<T>(IEnumerable<T> source, Func<T, double> map, double lowBound, double highBound) {
            return new AbsoluteAxisManager(ToRange(source.Select(map)), new AxisRange(lowBound, highBound));
        }

        private static AxisRange ToRange(IEnumerable<double> source) {
            var arr = source.ToArray();
            return new AxisRange(arr.DefaultIfEmpty().Min(), arr.DefaultIfEmpty().Max());
        }
    }
}
