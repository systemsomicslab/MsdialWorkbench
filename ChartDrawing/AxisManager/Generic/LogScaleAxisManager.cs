using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Graphics.AxisManager.Generic
{
    public class LogScaleAxisManager<T> : BaseAxisManager<T> where T : IConvertible
    {
        internal LogScaleAxisManager(Range range) : base(range) {
            _labelGenerator = new LogScaleLabelGenerator();
            Base = 10;
        }

        internal LogScaleAxisManager(Range range, Range bounds) : base(range, bounds) {
            _labelGenerator = new LogScaleLabelGenerator();
            Base = 10;
        }

        internal LogScaleAxisManager(Range range, IChartMargin margin) : base(range, margin) {
            _labelGenerator = new LogScaleLabelGenerator();
            Base = 10;
        }

        internal LogScaleAxisManager(Range range, IChartMargin margin, int base_) : base(range, margin) {
            _labelGenerator = new BaseSelectableLogScaleLabelGenerator(base_);
            Base = base_;
        }

        internal LogScaleAxisManager(Range range, IChartMargin margin, Range bounds) : base(range, margin, bounds) {
            _labelGenerator = new LogScaleLabelGenerator();
            Base = 10;
        }

        internal LogScaleAxisManager(Range range, IChartMargin margin, T lowBound, T highBound) : this(range, margin, new Range(ConvertToAxisValue(lowBound, 10), ConvertToAxisValue(highBound, 10))) {

        }

        internal LogScaleAxisManager(Range range, T lowBound, T highBound) : this(range, new Range(ConvertToAxisValue(lowBound, 10), ConvertToAxisValue(highBound, 10))) {

        }

        public LogScaleAxisManager(T low, T high)
            : this(new Range(ConvertToAxisValue(low, 10), ConvertToAxisValue(high, 10))) {

        }

        public LogScaleAxisManager(T low, T high, IChartMargin margin)
            : this(new Range(ConvertToAxisValue(low, 10), ConvertToAxisValue(high, 10)), margin) {

        }

        public LogScaleAxisManager(T low, T high, IChartMargin margin, int base_)
            : this(new Range(ConvertToAxisValue(low, base_), ConvertToAxisValue(high, base_)), margin, base_) {

        }

        public LogScaleAxisManager(T low, T high, T lowBound, T highBound)
            : this(new Range(ConvertToAxisValue(low, 10), ConvertToAxisValue(high, 10)),
                  new Range(ConvertToAxisValue(lowBound, 10), ConvertToAxisValue(highBound, 10))) {

        }

        public LogScaleAxisManager(T low, T high, IChartMargin margin, T lowBound, T highBound)
            : this(new Range(ConvertToAxisValue(low, 10), ConvertToAxisValue(high, 10)),
                  margin,
                  new Range(ConvertToAxisValue(lowBound, 10), ConvertToAxisValue(highBound, 10))) {

        }

        public LogScaleAxisManager(ICollection<T> source) : this(source.DefaultIfEmpty().Min(), source.DefaultIfEmpty().Max()) {

        }

        public LogScaleAxisManager(ICollection<T> source, IChartMargin margin) : this(source.DefaultIfEmpty().Min(), source.DefaultIfEmpty().Max(), margin) {

        }

        public LogScaleAxisManager(ICollection<T> source, IChartMargin margin, int base_) : this(source.DefaultIfEmpty().Min(), source.DefaultIfEmpty().Max(), margin, base_) {

        }

        public LogScaleAxisManager(ICollection<T> source, T low, T high) : this(source.DefaultIfEmpty().Min(), source.DefaultIfEmpty().Max(), low, high) {

        }

        public LogScaleAxisManager(ICollection<T> source, IChartMargin margin, T low, T high) : this(source.DefaultIfEmpty().Min(), source.DefaultIfEmpty().Max(), margin, low, high) {

        }

        public int Base { get; }

        public LabelType LabelType {
            get => _labelType;
            set => SetProperty(ref _labelType, value);
        }
        private LabelType _labelType = LabelType.Standard;

        public void UpdateInitialRange(T low, T high) {
            UpdateInitialRange(new Range(ConvertToAxisValue(low, Base), ConvertToAxisValue(high, Base)));
        }

        public void UpdateInitialRange(ICollection<T> source) {
            UpdateInitialRange(new Range(ConvertToAxisValue(source.DefaultIfEmpty().Min(), Base), ConvertToAxisValue(source.DefaultIfEmpty().Max(), Base)));
        }

        protected override void OnRangeChanged() {
            labelTicks = null;
            base.OnRangeChanged();
        }

        private ILabelGenerator LabelGenerator {
            get {
                switch (LabelType) {
                    case LabelType.Relative:
                        return _labelGenerator is RelativeLabelGenerator ? _labelGenerator : _labelGenerator = new RelativeLabelGenerator();
                    case LabelType.Percent:
                        return _labelGenerator is PercentLabelGenerator ? _labelGenerator : _labelGenerator = new PercentLabelGenerator();
                    case LabelType.Standard:
                    case LabelType.Order:
                    default:
                        return _labelGenerator is LogScaleLabelGenerator ? _labelGenerator : _labelGenerator = new LogScaleLabelGenerator();
                }
            }
        }
        private ILabelGenerator _labelGenerator = new LogScaleLabelGenerator();

        public override List<LabelTickData> GetLabelTicks() {
            var initialRangeCore = CoerceRange(InitialRangeCore, Bounds);
            List<LabelTickData> ticks;
            (ticks, UnitLabel) = LabelGenerator.Generate(Range.Minimum.Value, Range.Maximum.Value, initialRangeCore.Minimum.Value, initialRangeCore.Maximum.Value);
            return ticks;
        }

        public override AxisValue TranslateToAxisValue(T value) {
            return ConvertToAxisValue(value, Base);
        }

        private static AxisValue ConvertToAxisValue(T value, int base_) {
            return new AxisValue(Math.Log(Convert.ToDouble(value), base_));
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
