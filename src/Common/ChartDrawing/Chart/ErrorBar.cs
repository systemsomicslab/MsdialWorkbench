using CompMs.Common.Extension;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace CompMs.Graphics.Chart
{
    public class ErrorBar : ChartBaseControl {

        public ErrorBar() {
            var pen = new Pen(Brushes.Black, 2d);
            pen.Freeze();
            SetValue(LinePenProperty, pen);
        }

        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.Register(
                nameof(Items), typeof(List<ErrorData>), typeof(ErrorBar),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnItemsChanged,
                    CoerceItems));

        List<ErrorData> Items {
            get => (List<ErrorData>)GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        static void OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {

        }

        bool ShouldCoerceItems = false;
        static object CoerceItems(DependencyObject d, object value) {
            var eb = (ErrorBar)d;
            if (eb.ShouldCoerceItems) {
                eb.ShouldCoerceItems = false;

                if (eb.ItemsSource == null || string.IsNullOrEmpty(eb.HorizontalProperty) || string.IsNullOrEmpty(eb.VerticalProperty)) {
                    return new List<ErrorData>();
                }

                var itemssource = eb.ItemsSource?.Cast<object>().ToList();
                if (itemssource == null) {
                    return new List<ErrorData>();
                }

                var current = itemssource.FirstOrDefault();
                if (current == null) {
                    return new List<ErrorData>();
                }

                var type = current.GetType();
                var hProp = type.GetProperty(eb.HorizontalProperty);
                var vProp = type.GetProperty(eb.VerticalProperty);
                if (hProp == null || vProp == null) {
                    return new List<ErrorData>();
                }

                var herrors = eb.HorizontalErrors?.Errors;
                var verrors = eb.VerticalErrors?.Errors;
                if ((herrors != null && itemssource.Count != herrors.Count)
                    || (verrors != null && itemssource.Count != verrors.Count)) {
                    return new List<ErrorData>();
                }

                var haxis = eb.HorizontalAxis;
                var vaxis = eb.VerticalAxis;
                if (haxis == null || vaxis == null) {
                    return new List<ErrorData>();
                }

                var items = new List<ErrorData>(itemssource.Count);
                foreach (var item in itemssource) {
                    var hc = haxis.TranslateToAxisValue(hProp.GetValue(item));
                    var vc = vaxis.TranslateToAxisValue(vProp.GetValue(item));
                    items.Add(new ErrorData {
                        HorizontalCenter = hc, HorizontalUpper = AxisValue.NaN, HorizontalLower = AxisValue.NaN,
                        VerticalCenter = vc, VerticalUpper = AxisValue.NaN, VerticalLower = AxisValue.NaN,
                    });
                }

                if (herrors != null) {
                    var b = haxis.TranslateToAxisValue(0d);
                    foreach ((var herr, var item) in herrors.ZipInternal(items)) {
                        item.HorizontalLower = item.HorizontalCenter + b - haxis.TranslateToAxisValue(herr.Item1);
                        item.HorizontalUpper = item.HorizontalCenter - b + haxis.TranslateToAxisValue(herr.Item2);
                    }
                }

                if (verrors != null) {
                    var b = vaxis.TranslateToAxisValue(0d);
                    foreach ((var verr, var item) in verrors.ZipInternal(items)) {
                        item.VerticalLower = item.VerticalCenter + b - vaxis.TranslateToAxisValue(verr.Item1);
                        item.VerticalUpper = item.VerticalCenter - b + vaxis.TranslateToAxisValue(verr.Item2);
                    }
                }

                return items;
            }
            return value;
        }

        public static readonly DependencyProperty HorizontalErrorsProperty =
            DependencyProperty.Register(
                nameof(HorizontalErrors), typeof(ErrorSource), typeof(ErrorBar),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnHorizontalErrorsChanged));

        public ErrorSource HorizontalErrors {
            get => (ErrorSource)GetValue(HorizontalErrorsProperty);
            set => SetValue(HorizontalErrorsProperty, value);
        }

        static void OnHorizontalErrorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var eb = (ErrorBar)d;
            eb.ShouldCoerceItems = true;
            eb.CoerceValue(ItemsProperty);
        }

        public static readonly DependencyProperty VerticalErrorsProperty =
            DependencyProperty.Register(
                nameof(VerticalErrors), typeof(ErrorSource), typeof(ErrorBar),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnVerticalErrorsChanged));

        public ErrorSource VerticalErrors {
            get => (ErrorSource)GetValue(VerticalErrorsProperty);
            set => SetValue(VerticalErrorsProperty, value);
        }

        static void OnVerticalErrorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var eb = (ErrorBar)d;
            eb.ShouldCoerceItems = true;
            eb.CoerceValue(ItemsProperty);
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource), typeof(System.Collections.IEnumerable), typeof(ErrorBar),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnItemsSourceChanged));

        public System.Collections.IEnumerable ItemsSource {
            get => (System.Collections.IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var eb = (ErrorBar)d;
            eb.OnItemsSourceChanged((System.Collections.IEnumerable)e.OldValue, (System.Collections.IEnumerable)e.NewValue);
            eb.ShouldCoerceItems = true;
            eb.CoerceValue(ItemsProperty);
        }

        void OnItemsSourceChanged(System.Collections.IEnumerable oldValue, System.Collections.IEnumerable newValue) {
            if (oldValue is INotifyCollectionChanged oldCollection) {
                oldCollection.CollectionChanged -= OnCollectionChanged;
            }
            if (newValue is INotifyCollectionChanged newCollection) {
                newCollection.CollectionChanged += OnCollectionChanged;
            }
        }

        void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            ShouldCoerceItems = true;
            CoerceValue(ItemsProperty);
        }

        public static readonly DependencyProperty HorizontalPropertyProperty =
            DependencyProperty.Register(
                nameof(HorizontalProperty), typeof(string), typeof(ErrorBar),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnHorizontalPropertyChanged));

        public string HorizontalProperty {
            get => (string)GetValue(HorizontalPropertyProperty);
            set => SetValue(HorizontalPropertyProperty, value);
        }

        static void OnHorizontalPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var eb = (ErrorBar)d;
            eb.ShouldCoerceItems = true;
            eb.CoerceValue(ItemsProperty);
        }

        protected override void OnHorizontalAxisChanged(IAxisManager oldValue, IAxisManager newValue) {
            base.OnHorizontalAxisChanged(oldValue, newValue);
            ShouldCoerceItems = true;
            CoerceValue(ItemsProperty);
        }

        protected override void OnHorizontalRangeChanged(object sender, EventArgs e) {
            base.OnHorizontalRangeChanged(sender, e);
            ShouldCoerceItems = true;
            CoerceValue(ItemsProperty);
        }

        protected override void OnHorizontalMappingChanged(object sender, EventArgs e) {
            base.OnHorizontalMappingChanged(sender, e);
            ShouldCoerceItems = true;
            CoerceValue(ItemsProperty);
            InvalidateVisual();
        }

        public static readonly DependencyProperty VerticalPropertyProperty =
            DependencyProperty.Register(
                nameof(VerticalProperty), typeof(string), typeof(ErrorBar),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnVerticalPropertyChanged));

        public string VerticalProperty {
            get => (string)GetValue(VerticalPropertyProperty);
            set => SetValue(VerticalPropertyProperty, value);
        }

        static void OnVerticalPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var eb = (ErrorBar)d;
            eb.ShouldCoerceItems = true;
            eb.CoerceValue(ItemsProperty);
        }

        protected override void OnVerticalAxisChanged(IAxisManager oldValue, IAxisManager newValue) {
            base.OnVerticalAxisChanged(oldValue, newValue);
            ShouldCoerceItems = true;
            CoerceValue(ItemsProperty);
        }

        protected override void OnVerticalRangeChanged(object sender, EventArgs e) {
            base.OnVerticalRangeChanged(sender, e);
            ShouldCoerceItems = true;
            CoerceValue(ItemsProperty);
        }

        protected override void OnVerticalMappingChanged(object sender, EventArgs e) {
            base.OnVerticalMappingChanged(sender, e);
            ShouldCoerceItems = true;
            CoerceValue(ItemsProperty);
            InvalidateVisual();
        }

        public static readonly DependencyProperty LinePenProperty =
            DependencyProperty.Register(
                nameof(LinePen), typeof(Pen), typeof(ErrorBar),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender |
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public Pen LinePen {
            get => (Pen)GetValue(LinePenProperty);
            set => SetValue(LinePenProperty, value);
        }

        public static readonly DependencyProperty ErrorCapProperty =
            DependencyProperty.Register(
                nameof(ErrorCap), typeof(double), typeof(ErrorBar),
                new FrameworkPropertyMetadata(
                    5d,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public double ErrorCap {
            get => (double)GetValue(ErrorCapProperty);
            set => SetValue(ErrorCapProperty, value);
        }

        protected override void OnRender(DrawingContext drawingContext) {
            var items = Items;
            var haxis = HorizontalAxis;
            var vaxis = VerticalAxis;
            if (items == null || haxis == null || vaxis == null)
                return;

            var flipx = FlippedX;
            var flipy = FlippedY;
            var actualWidth = ActualWidth;
            var actualHeight = ActualHeight;
            var pen = LinePen;
            var halfcap = ErrorCap / 2d;

            foreach (var item in items) {
                if (!haxis.Range.Contains(item.HorizontalCenter) || !vaxis.Range.Contains(item.VerticalCenter)) {
                    continue;
                }
                var hcenter = haxis.TranslateToRenderPoint(item.HorizontalCenter, flipx, actualWidth);
                var vcenter = vaxis.TranslateToRenderPoint(item.VerticalCenter, flipy, actualHeight);

                if (!item.HorizontalLower.IsNaN()) {
                    var hlower = haxis.TranslateToRenderPoint(item.HorizontalLower, flipx, actualWidth);
                    drawingContext.DrawLine(pen, new Point(hlower, vcenter), new Point(hcenter, vcenter));
                    drawingContext.DrawLine(pen, new Point(hlower, vcenter - halfcap), new Point(hlower, vcenter + halfcap));
                }
                if (!item.HorizontalUpper.IsNaN()) {
                    var hupper = haxis.TranslateToRenderPoint(item.HorizontalUpper, flipx, actualWidth);
                    drawingContext.DrawLine(pen, new Point(hcenter, vcenter), new Point(hupper, vcenter));
                    drawingContext.DrawLine(pen, new Point(hupper, vcenter - halfcap), new Point(hupper, vcenter + halfcap));
                }
                if (!item.VerticalLower.IsNaN()) {
                    var vlower = vaxis.TranslateToRenderPoint(item.VerticalLower, flipy, actualHeight);
                    drawingContext.DrawLine(pen, new Point(hcenter, vlower), new Point(hcenter, vcenter));
                    drawingContext.DrawLine(pen, new Point(hcenter - halfcap, vlower), new Point(hcenter + halfcap, vlower));
                }
                if (!item.VerticalUpper.IsNaN()) {
                    var vupper = vaxis.TranslateToRenderPoint(item.VerticalUpper, flipy, actualHeight);
                    drawingContext.DrawLine(pen, new Point(hcenter, vcenter), new Point(hcenter, vupper));
                    drawingContext.DrawLine(pen, new Point(hcenter - halfcap, vupper), new Point(hcenter + halfcap, vupper));
                }
            }
        }
    }

    class ErrorData
    {
        internal AxisValue HorizontalCenter, HorizontalLower, HorizontalUpper;
        internal AxisValue VerticalCenter, VerticalLower, VerticalUpper;
    }

    [TypeConverter(typeof(ErrorSourceTypeConverter))]
    public class ErrorSource
    {
        internal List<Tuple<double, double>> Errors { get; set; }
    }

    public class ErrorSourceTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            return sourceType.GetInterfaces()
                .Any(t => t == typeof(IEnumerable<double>)
                || t == typeof(IEnumerable<Tuple<double, double>>)
                || t == typeof(IEnumerable<ValueTuple<double, double>>));
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            if (value is IEnumerable<double> ds) {
                return new ErrorSource { Errors = ds.Select(d => Tuple.Create(d, d)).ToList() };
            }
            else if (value is IEnumerable<IConvertible> cs) {
                return new ErrorSource { Errors = cs.Select(c => Convert.ToDouble(c)).Select(d => Tuple.Create(d, d)).ToList() };
            }
            else if (value is IEnumerable<Tuple<double, double>> dds) {
                return new ErrorSource { Errors = dds.ToList() };
            }
            else if (value is IEnumerable<ValueTuple<double, double>> vdds) {
                return new ErrorSource { Errors = vdds.Select(vdd => Tuple.Create(vdd.Item1, vdd.Item2)).ToList() };
            }
            return null;
        }
    }
}
