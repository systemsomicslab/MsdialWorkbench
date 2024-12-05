using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace CompMs.Graphics.AxisManager.Generic
{
    public abstract class BaseAxisManager<T> : DisposableModelBase, IAxisManager<T>
    {
        public BaseAxisManager(AxisRange range, AxisRange bounds) {
            InitialRangeCore = range;
            InitialRange = CoerceRange(InitialRangeCore, bounds);
            Bounds = bounds;
            Range = InitialRange;
        }

        public BaseAxisManager(AxisRange range, IChartMargin margin) {
            InitialRangeCore = range;
            InitialRange = CoerceRange(InitialRangeCore, null);
            ChartMargin = margin;
            Range = InitialRange;
        }

        public BaseAxisManager(AxisRange range, IChartMargin margin, AxisRange bounds) {
            InitialRangeCore = range;
            InitialRange = CoerceRange(InitialRangeCore, bounds);
            Bounds = bounds;
            ChartMargin = margin;
            Range = InitialRange;
        }

        public BaseAxisManager(AxisRange range) {
            InitialRangeCore = range;
            InitialRange = CoerceRange(InitialRangeCore, null);
            Range = InitialRange;
        }

        public BaseAxisManager(BaseAxisManager<T> source) {
            InitialRangeCore = source.InitialRangeCore;
            InitialRange = CoerceRange(InitialRangeCore, null);
            Bounds = source.Bounds;
            Range = InitialRange;
        }

        public AxisValue Min => Range?.Minimum ?? new AxisValue(0d);

        public AxisValue Max => Range?.Maximum ?? new AxisValue(0d);

        public AxisRange Range {
            get {
                return range;
            }
            private set {
                var r = CoerceRange(value, Bounds);
                if (InitialRange.Contains(r)) {
                    range = r;
                    OnRangeChanged();
                }
            }
        }
        private AxisRange range;

        protected AxisRange InitialRangeCore { get; private set; }

        public AxisRange InitialRange { get; private set; }

        protected static AxisRange CoerceRange(AxisRange r, AxisRange bound) {
            var range = r.Union(bound);
            if (range.Delta <= 1e-10) {
                range = new AxisRange(range.Minimum - 1, range.Maximum + 1);
            }
            return range;
        }

        public AxisRange Bounds { get; protected set; }

        public IChartMargin ChartMargin { get; set; } = new RelativeMargin(0, 0);

        public double ConstantMargin { get; set; } = 0;

        public List<LabelTickData> LabelTicks {
            get => labelTicks ?? GetLabelTicks();
        }

        protected List<LabelTickData> labelTicks = null;

        public string UnitLabel {
            get => unitLabel;
            set => SetProperty(ref unitLabel, value);
        }
        private string unitLabel = string.Empty;

        private static readonly EventArgs args = new EventArgs();

        public event EventHandler RangeChanged;

        protected virtual void OnRangeChanged() {
            RangeChanged?.Invoke(this, args);
        }

        public event EventHandler InitialRangeChanged;

        protected virtual void OnInitialRangeChanged() {
            InitialRangeChanged?.Invoke(this, args);
        }

        public event EventHandler AxisValueMappingChanged;

        protected virtual void OnAxisValueMappingChanged() {
            AxisValueMappingChanged?.Invoke(this, args);
        }

        public void UpdateInitialRange(AxisRange range) {
            InitialRangeCore = range;
            InitialRange = CoerceRange(range, Bounds);
            Range = InitialRange;
            OnInitialRangeChanged();
            // Focus(InitialRangeCore);
        }

        public bool Contains(AxisValue value) {
            return InitialRange.Contains(value);
        }

        public bool ContainsCurrent(AxisValue value) {
            return Range.Contains(value);
        }

        public void Focus(AxisRange range) {
            Range = range.Union(Bounds).Intersect(InitialRange);
        }

        public void Recalculate(double drawableLength) {
            if (drawableLength == 0) {
                return;
            }
            // var lo = -(1 + ChartMargin.Left + ChartMargin.Right) / (drawableLength - 2 * ConstantMargin) * ConstantMargin - ChartMargin.Left;
            // var hi = 1 - lo - ChartMargin.Left + ChartMargin.Right;
            (var lo, var hi) = ChartMargin.Add(0, drawableLength);
            InitialRange = TranslateFromRelativeRange(lo / drawableLength, hi / drawableLength, CoerceRange(InitialRangeCore, Bounds));
        }

        public void Reset() {
            Focus(InitialRange);
        }

        public abstract List<LabelTickData> GetLabelTicks();

        public abstract AxisValue TranslateToAxisValue(T value);

        public virtual AxisValue TranslateToAxisValue(object value) {
            if (TryConvert(value, out var value_)) {
                return TranslateToAxisValue(value_);
            }
            return AxisValue.NaN;
        }

        private static Dictionary<Type, bool> _cacheConvertible = new Dictionary<Type, bool>();
        private static bool _isConvertible = typeof(T).GetInterfaces().Any(t => t.Name == nameof(IConvertible));
        private static bool TryConvert(object value, out T result) {
            result = default;
            var baseType = value.GetType();
            if (_cacheConvertible.TryGetValue(baseType, out var convertible) && !convertible) {
                return convertible;
            }
            if (value is T value_) {
                result = value_;
                return _cacheConvertible[baseType] = true;
            }

            var targetType = typeof(T);
            if (_isConvertible && value is IConvertible cvalue) {
                result = (T)((IConvertible)Convert.ToDouble(cvalue)).ToType(targetType, CultureInfo.CurrentCulture);
                return _cacheConvertible[baseType] = true;
            }

            var canImplicitConvert = baseType
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(m =>
                    m.Name == "op_implicit" &&
                    m.ReturnType == targetType &&
                    m.GetParameters().FirstOrDefault() is ParameterInfo pi &&
                    pi.ParameterType == baseType
                );
            if (canImplicitConvert) {
                result = (T)Convert.ChangeType(value, targetType);
            }
            return _cacheConvertible[baseType] = canImplicitConvert;
        }

        private double TranslateRelativePointCore(AxisValue value, AxisValue min, AxisValue max) {
            return (value.Value - min.Value) / (max.Value - min.Value);
        }

        private List<double> TranslateToRelativePoints(IEnumerable<T> values) {
            AxisValue max = Max, min = Min;
            var result = new List<double>();
            foreach (var value in values) {
                result.Add(TranslateRelativePointCore(TranslateToAxisValue(value), min, max));
            }
            return result;
        }

        private List<double> TranslateToRelativePoints(IEnumerable<AxisValue> values) {
            AxisValue max = Max, min = Min;
            return values.Select(axVal => TranslateRelativePointCore(axVal, min, max)).ToList();
        }

        private double FlipRelative(double relative, bool isFlipped) {
            return isFlipped ? 1 - relative : relative;
        }

        public double TranslateToRenderPoint(AxisValue value, bool isFlipped, double drawableLength) {
            return FlipRelative(TranslateRelativePointCore(value, Min, Max), isFlipped) * drawableLength;
        }

        private AxisValue TranslateFromRelativePoint(double value) {
            return TranslateFromRelativePoint(value, Range);
        }

        private AxisValue TranslateFromRelativePoint(double value, AxisRange range) {
            return new AxisValue(value * (range.Maximum.Value - range.Minimum.Value) + range.Minimum.Value);
        }

        private AxisRange TranslateFromRelativeRange(double low, double hi, AxisRange range) {
            return new AxisRange(TranslateFromRelativePoint(low, range), TranslateFromRelativePoint(hi, range));
        }

        public AxisValue TranslateFromRenderPoint(double value, bool isFlipped, double drawableLength) {
            return TranslateFromRelativePoint(FlipRelative(value / drawableLength, isFlipped));
        }

        public List<double> TranslateToRenderPoints(IEnumerable<T> values, bool isFlipped, double drawableLength) {
            var results = TranslateToRelativePoints(values);
            for (var i = 0; i < results.Count; i++) {
                results[i] = FlipRelative(results[i], isFlipped) * drawableLength;
            }
            return results;
        }

        public List<double> TranslateToRenderPoints(IEnumerable<AxisValue> values, bool isFlipped, double drawableLength) {
            var results = TranslateToRelativePoints(values);
            for (var i = 0; i < results.Count; i++) {
                results[i] = FlipRelative(results[i], isFlipped) * drawableLength;
            }
            return results;
        }

        public List<double> TranslateToRenderPoints(IEnumerable<object> values, bool isFlipped, double drawableLength) {
            return TranslateToRenderPoints(values.OfType<T>(), isFlipped, drawableLength);
        }
    }
}
