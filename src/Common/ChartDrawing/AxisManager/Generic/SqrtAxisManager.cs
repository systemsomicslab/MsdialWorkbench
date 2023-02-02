using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;

namespace CompMs.Graphics.AxisManager.Generic
{
    public sealed class SqrtAxisManager : BaseAxisManager<double>
    {
        public SqrtAxisManager(Range range) : base(range) {

        }

        public SqrtAxisManager(Range range, Range bounds) : base(range, bounds) {

        }

        public SqrtAxisManager(Range range, IChartMargin margin) : base(range, margin) {

        }

        public SqrtAxisManager(Range range, IChartMargin margin, Range bounds) : base(range, margin, bounds) {

        }

        public SqrtAxisManager(Range range, IChartMargin margin, double lowBound, double highBound) : base(range, margin, ConvertToRange(lowBound, highBound)) {

        }

        public SqrtAxisManager(double low, double high) : this(ConvertToRange(low, high)) {

        }

        public SqrtAxisManager(double low, double high, Range bounds) : this(ConvertToRange(low, high), bounds) {

        }

        public SqrtAxisManager(double low, double high, IChartMargin margin) : this(ConvertToRange(low, high), margin) {

        }

        public SqrtAxisManager(double low, double high, IChartMargin margin, Range bounds) : this(ConvertToRange(low, high), margin, bounds) {

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
            return new AxisValue(Math.Sqrt(value));
        }

        private static AxisValue ConvertToAxisValue(double value) {
            return new AxisValue(Math.Sqrt(value));
        }

        private static Range ConvertToRange(double low, double high) {
            return new Range(ConvertToAxisValue(low), ConvertToAxisValue(high));
        }
    }
}
