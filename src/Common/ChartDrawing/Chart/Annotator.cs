using CompMs.Graphics.Core.Base;
using System;
using CompMs.Graphics.Helper;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace CompMs.Graphics.Chart
{
    public sealed class Annotator : ChartBaseControl
    {
        static Annotator() {
            culture = CultureInfo.GetCultureInfo("en-us");
            typeFace = new Typeface("Calibri");
            IsHitTestVisibleProperty.OverrideMetadata(typeof(Annotator), new FrameworkPropertyMetadata(false));
        }

        private readonly LazyDatas _lazyDatas = new LazyDatas();

        private bool ShouldCoerceDatas = false;

        private void CoerceLazyDatas() {
            if (ShouldCoerceDatas) {
                var lazyDatas = _lazyDatas;
                if (!ReadCleanFlag(PropertyClean.ItemsSource)) {
                    UpdateItemsSource(lazyDatas);
                }
                if (!ReadCleanFlag(PropertyClean.Item)) {
                    UpdateItem(lazyDatas);
                }
                if (!ReadCleanFlag(PropertyClean.Horizontal)) {
                    UpdateHorizontalItems(lazyDatas);
                }
                if (!ReadCleanFlag(PropertyClean.Vertical)) {
                    UpdateVerticalItems(lazyDatas);
                }
                if (!ReadCleanFlag(PropertyClean.Order)) {
                    UpdateOrderItems(lazyDatas);
                }
                if (!ReadCleanFlag(PropertyClean.Label)) {
                    UpdateLabel(lazyDatas);
                }
                ShouldCoerceDatas = new[]
                {
                    !ReadCleanFlag(PropertyClean.ItemsSource),
                    !ReadCleanFlag(PropertyClean.Item),
                    !ReadCleanFlag(PropertyClean.Horizontal),
                    !ReadCleanFlag(PropertyClean.Vertical),
                    !ReadCleanFlag(PropertyClean.Order),
                    !ReadCleanFlag(PropertyClean.Label),
                }.Any(b => b);
            }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource), typeof(System.Collections.IEnumerable), typeof(Annotator),
                new FrameworkPropertyMetadata(
                    default(System.Collections.IEnumerable),
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnItemsSourceChanged));

        public System.Collections.IEnumerable ItemsSource
        {
            get => (System.Collections.IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = (Annotator)d;
            chart.OnItemsSourceChanged((System.Collections.IEnumerable)e.NewValue, (System.Collections.IEnumerable)e.OldValue);

            chart.ShouldCoerceDatas = true;
            chart.WriteCleanFlag(PropertyClean.ItemsSource, false);
            chart.CoerceLazyDatas();
        }

        void OnItemsSourceChanged(System.Collections.IEnumerable newValue, System.Collections.IEnumerable oldValue) {
            var oldView = CollectionViewSource.GetDefaultView(oldValue);
            if (oldView is INotifyCollectionChanged collectionOld) {
                collectionOld.CollectionChanged -= ItemsSourceCollectionChanged;
            }
            var newView = CollectionViewSource.GetDefaultView(newValue);
            if (newView is INotifyCollectionChanged collectionNew) {
                collectionNew.CollectionChanged += ItemsSourceCollectionChanged;
            }
            collectionView = newView;
        }

        private void ItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            ShouldCoerceDatas = true;
            WriteCleanFlag(PropertyClean.Item, false);
            CoerceLazyDatas();
            InvalidateVisual();
        }

        private void UpdateItemsSource(LazyDatas lazyDatas) {
            var firstItem = collectionView?.Cast<object>().FirstOrDefault();
            if (firstItem is null) {
                WriteCleanFlag(PropertyClean.ItemsSource, true);
                lazyDatas.SetSource(collectionView);
                return;
            }

            dataType = firstItem.GetType();
            lazyDatas.SetSource(collectionView);

            WriteCleanFlag(PropertyClean.ItemsSource, true);
            WriteCleanFlag(PropertyClean.Horizontal | PropertyClean.Vertical | PropertyClean.Label | PropertyClean.Order, false);
        }

        private void UpdateItem(LazyDatas lazyDatas) {
            UpdateItemsSource(lazyDatas);
            WriteCleanFlag(PropertyClean.Item, true);
        }

        protected override void OnHorizontalAxisChanged(IAxisManager oldValue, IAxisManager newValue) {
            base.OnHorizontalAxisChanged(oldValue, newValue);
            WriteCleanFlag(PropertyClean.Horizontal, false);
            ShouldCoerceDatas = true;
            CoerceLazyDatas();
        }

        protected override void OnHorizontalMappingChanged(object sender, EventArgs e) {
            base.OnHorizontalMappingChanged(sender, e);
            WriteCleanFlag(PropertyClean.Horizontal, false);
            ShouldCoerceDatas = true;
            CoerceLazyDatas();
            InvalidateVisual();
        }

        protected override void OnVerticalAxisChanged(IAxisManager oldValue, IAxisManager newValue) {
            base.OnVerticalAxisChanged(oldValue, newValue);
            WriteCleanFlag(PropertyClean.Vertical, false);
            ShouldCoerceDatas = true;
            CoerceLazyDatas();
        }

        protected override void OnVerticalMappingChanged(object sender, EventArgs e) {
            base.OnVerticalMappingChanged(sender, e);
            WriteCleanFlag(PropertyClean.Vertical, false);
            ShouldCoerceDatas = true;
            CoerceLazyDatas();
            InvalidateVisual();
        }

        public static readonly DependencyProperty HorizontalPropertyNameProperty =
            DependencyProperty.Register(
                nameof(HorizontalPropertyName), typeof(string), typeof(Annotator),
                new FrameworkPropertyMetadata(
                    default(string),
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnHorizontalChanged));
        public string HorizontalPropertyName
        {
            get => (string)GetValue(HorizontalPropertyNameProperty);
            set => SetValue(HorizontalPropertyNameProperty, value);
        }

        static void OnHorizontalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = (Annotator)d;
            chart.WriteCleanFlag(PropertyClean.Horizontal, false);
            chart.ShouldCoerceDatas = true;
            chart.CoerceLazyDatas();
        }

        private void UpdateHorizontalItems(LazyDatas lazyDatas) {
            if (string.IsNullOrEmpty(HorizontalPropertyName) || dataType is null || !ExpressionHelper.ValidatePropertyString(dataType, HorizontalPropertyName)) {
                WriteCleanFlag(PropertyClean.Horizontal, true);
                return;
            }

            var expression = ExpressionHelper.GetConvertToAxisValueExpression(dataType, HorizontalPropertyName);
            lazyDatas.UpdateHorizontalValue(expression);
            lazyDatas.UpdateHorizontalAxis(HorizontalAxis);
            WriteCleanFlag(PropertyClean.Horizontal, true);
        }

        public static readonly DependencyProperty VerticalPropertyNameProperty =
            DependencyProperty.Register(
                nameof(VerticalPropertyName), typeof(string), typeof(Annotator),
                new FrameworkPropertyMetadata(
                    default(string),
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnVerticalChanged));
        public string VerticalPropertyName
        {
            get => (string)GetValue(VerticalPropertyNameProperty);
            set => SetValue(VerticalPropertyNameProperty, value);
        }

        static void OnVerticalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = (Annotator)d;
            chart.WriteCleanFlag(PropertyClean.Vertical, false);
            chart.ShouldCoerceDatas = true;
            chart.CoerceLazyDatas();
        }

        private void UpdateVerticalItems(LazyDatas lazyDatas) {
            if (string.IsNullOrEmpty(VerticalPropertyName) || dataType is null || !ExpressionHelper.ValidatePropertyString(dataType, VerticalPropertyName)) {
                WriteCleanFlag(PropertyClean.Vertical, true);
                return;
            }
            var expression = ExpressionHelper.GetConvertToAxisValueExpression(dataType, VerticalPropertyName);
            lazyDatas.UpdateVerticalValue(expression);
            lazyDatas.UpdateVerticalAxis(VerticalAxis);
            WriteCleanFlag(PropertyClean.Vertical, true);
        }

        public static readonly DependencyProperty OrderingPropertyNameProperty =
            DependencyProperty.Register(
                nameof(OrderingPropertyName), typeof(string), typeof(Annotator),
                new PropertyMetadata(
                    string.Empty,
                    OnOrderChanged));
        public string OrderingPropertyName {
            get => (string)GetValue(OrderingPropertyNameProperty);
            set => SetValue(OrderingPropertyNameProperty, value);
        }

        private static void OnOrderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = (Annotator)d;
            chart.WriteCleanFlag(PropertyClean.Order, false);
            chart.ShouldCoerceDatas = true;
            chart.CoerceLazyDatas();
        }

        private void UpdateOrderItems(LazyDatas lazyDatas) {
            if (dataType is null) {
                return;
            }

            if (!string.IsNullOrEmpty(OrderingPropertyName) && dataType != null && ExpressionHelper.ValidatePropertyString(dataType, OrderingPropertyName)) {
                var expression = ExpressionHelper.GetPropertyGetterExpression(dataType, OrderingPropertyName);
                lazyDatas.UpdateOrderValue(expression);
            }
            else {
                lazyDatas.ClearOrderValueUpdator();
            }
            WriteCleanFlag(PropertyClean.Order, true);
        }

        public static readonly DependencyProperty LabelPropertyNameProperty = DependencyProperty.Register(
            nameof(LabelPropertyName), typeof(string), typeof(Annotator),
            new FrameworkPropertyMetadata(
                default(string),
                FrameworkPropertyMetadataOptions.AffectsRender,
                OnLabelChanged));

        public string LabelPropertyName
        {
            get => (string)GetValue(LabelPropertyNameProperty);
            set => SetValue(LabelPropertyNameProperty, value);
        }

        private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = (Annotator)d;
            chart.WriteCleanFlag(PropertyClean.Label, false);
            chart.ShouldCoerceDatas = true;
            chart.CoerceLazyDatas();
        }

        private void UpdateLabel(LazyDatas lazyDatas) {
            if (dataType is null) {
                return;
            }

            if (dataType != null && !string.IsNullOrEmpty(LabelPropertyName) && ExpressionHelper.ValidatePropertyString(dataType, LabelPropertyName)) {
                var expression = ExpressionHelper.GetPropertyGetterExpression(dataType, LabelPropertyName);
                lazyDatas.UpdateLabelValue(expression, Format);
            }
            else {
                lazyDatas.ClearLabelValueUpdator();
            }
            WriteCleanFlag(PropertyClean.Label, true);
        }

        public static readonly DependencyProperty TopNProperty =
            DependencyProperty.Register(
                nameof(TopN), typeof(int?), typeof(Annotator),
                new PropertyMetadata(null));
        public int? TopN {
            get => (int?)GetValue(TopNProperty);
            set => SetValue(TopNProperty, value);
        }

        public static readonly DependencyProperty FormatProperty = DependencyProperty.Register(
            nameof(Format), typeof(string), typeof(Annotator),
            new PropertyMetadata("N5")
            );
        public string Format {
            get => (string)GetValue(FormatProperty);
            set => SetValue(FormatProperty, value);
        }

        public static readonly DependencyProperty ContentAlignmentProperty =
            DependencyProperty.Register(
                nameof(ContentAlignment), typeof(System.Drawing.ContentAlignment), typeof(Annotator),
                new PropertyMetadata(System.Drawing.ContentAlignment.TopCenter));
        public System.Drawing.ContentAlignment ContentAlignment {
            get => (System.Drawing.ContentAlignment)GetValue(ContentAlignmentProperty);
            set => SetValue(ContentAlignmentProperty, value);
        }

        public static readonly DependencyProperty BrushProperty =
            DependencyProperty.Register(
                nameof(Brush), typeof(Brush), typeof(Annotator),
                new PropertyMetadata(Brushes.Gray));
        public Brush Brush {
            get => (Brush)GetValue(BrushProperty);
            set => SetValue(BrushProperty, value);
        }

        public static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register(
                nameof(FontSize), typeof(double), typeof(Annotator),
                new PropertyMetadata(13d));

        public double FontSize {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public static readonly DependencyProperty OverlapProperty =
            DependencyProperty.Register(
                nameof(Overlap), typeof(IOverlapMethod), typeof(Annotator),
                new PropertyMetadata(IgnoreOverlap.Method));

        public IOverlapMethod Overlap {
            get => (IOverlapMethod)GetValue(OverlapProperty);
            set => SetValue(OverlapProperty, value);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            double actualWidth = ActualWidth, actualHeight = ActualHeight;
            var hAxis = HorizontalAxis;
            var vAxis = VerticalAxis;
            var datas = _lazyDatas.Datas;
            if (hAxis == null || vAxis == null || datas == null)
                return;

            double dx = 0, dy = 0;
            switch (ContentAlignment) {
                case System.Drawing.ContentAlignment.BottomLeft:
                case System.Drawing.ContentAlignment.MiddleLeft:
                case System.Drawing.ContentAlignment.TopLeft:
                    dx = 20;
                    break;
                case System.Drawing.ContentAlignment.TopRight:
                case System.Drawing.ContentAlignment.MiddleRight:
                case System.Drawing.ContentAlignment.BottomRight:
                    dx = -20;
                    break;
            }
            switch (ContentAlignment) {
                case System.Drawing.ContentAlignment.TopCenter:
                case System.Drawing.ContentAlignment.TopLeft:
                case System.Drawing.ContentAlignment.TopRight:
                    dy = -20;
                    break;
                case System.Drawing.ContentAlignment.BottomCenter:
                case System.Drawing.ContentAlignment.BottomLeft:
                case System.Drawing.ContentAlignment.BottomRight:
                    dy = 5;
                    break;
            }

            var texts = new List<Tuple<FormattedText, Point, Rect>>();
            var brush = Brush;
            var fontSize = FontSize;
            bool flippedX = FlippedX, flippedY = FlippedY;
            var overlap = Overlap;
            var ppd = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var repText = new FormattedText("1000.00000", culture, FlowDirection.LeftToRight, typeFace, fontSize, brush, ppd) { TextAlignment = TextAlignment.Center };
            var repWidth = repText.Width;
            var repHeight = repText.Height;
            var repVec = new Vector(repWidth / 2, repHeight / 2);
            foreach(var data in datas.OrderByDescending(d => d.order)) {
                if (TopN.HasValue && texts.Count >= TopN)
                    break;
                if (!string.IsNullOrEmpty(data.label) && hAxis.Range.Contains(data.x) && vAxis.Range.Contains(data.y)) {

                    double xx = hAxis.TranslateToRenderPoint(data.x, flippedX, actualWidth);
                    double yy = vAxis.TranslateToRenderPoint(data.y, flippedY, actualHeight);
                    var p = new Point(xx + dx, yy + dy);
                    var rect = new Rect(p - repVec, p + repVec);
                    if (texts.Any(other => overlap.IsOverlap(rect, p, other.Item3, other.Item2))) {
                        continue;
                    }

                    var text = new FormattedText(data.label, culture, FlowDirection.LeftToRight, typeFace, fontSize, brush, ppd)
                    {
                        TextAlignment = TextAlignment.Center
                    };
                    texts.Add(Tuple.Create(text, p, rect));
                }
            }

            foreach ((var text, var point, _) in texts) {
                drawingContext.DrawText(text, point);
            }
        }

        private ICollectionView collectionView;
        private Type dataType;
        private static readonly CultureInfo culture;
        private static readonly Typeface typeFace;
        private PropertyClean cleanFlags = PropertyClean.All;

        private bool ReadCleanFlag(PropertyClean flag) {
            return (cleanFlags & flag) != 0;
        }

        private void WriteCleanFlag(PropertyClean flag, bool set) {
            if (set)
                cleanFlags |= flag;
            else
                cleanFlags &= (~flag);
        }
        
        [Flags]
        enum PropertyClean : uint
        {
            None = 0x0,
            ItemsSource = 0x1,
            Item = 0x2,
            Horizontal = 0x4,
            Vertical = 0x8,
            Label = 0x10,
            Order = 0x20,
            Format = 0x40,
            All = ItemsSource | Item | Horizontal | Vertical | Label | Order | Format,
        }

        class LabelData {
            internal AxisValue x, y;
            internal string label;
            internal double order;
            internal Brush brush;
        }

        sealed class LazyDatas {
            private LabelData[] _datas;
            private IAxisManager _horizontalAxis;
            private IAxisManager _verticalAxis;
            private Expression<Func<object, IAxisManager, AxisValue>> _horizontalUpdator;
            private Expression<Func<object, IAxisManager, AxisValue>> _verticalUpdator;
            private ICollectionView _collectionView;
            private Expression<Func<object, object>> _orderValueUpdator;
            private Expression<Func<object, object>> _labelValueUpdator;
            private string _format;

            public LabelData[] Datas {
                get {
                    if (_datas is null) {
                        Reconstruct();
                    }
                    return _datas;
                }
            }

            private void Reconstruct() {
                var source = _collectionView?.Cast<object>().ToArray();
                if (source is null || _horizontalAxis is null || _verticalAxis is null || _horizontalUpdator is null || _verticalUpdator is null) {
                    _datas = new LabelData[0];
                    return;
                }
                var data = new LabelData[source.Length];
                var horizontalUpdator = _horizontalUpdator.Compile();
                var verticalUpdator = _verticalUpdator.Compile();
                var orderValueUpdator = _orderValueUpdator?.Compile();
                var labelValueUpdator = _labelValueUpdator?.Compile();
                for (int i = 0; i < data.Length; i++) {
                    data[i] = new LabelData();

                    var item = source[i];
                    data[i].x = horizontalUpdator.Invoke(item, _horizontalAxis);
                    data[i].y = verticalUpdator.Invoke(item, _verticalAxis);

                    var order = orderValueUpdator?.Invoke(item);
                    if (order is double dvalue) {
                        data[i].order = dvalue;
                    }
                    else if (order is IConvertible conv) {
                        data[i].order = Convert.ToDouble(conv);
                    }
                    else {
                        data[i].order = 0;
                    }

                    var label = labelValueUpdator?.Invoke(item);
                    if (label == null) {
                        data[i].label = string.Empty;
                    }
                    else if (label is double val && _format != null) {
                        data[i].label = val.ToString(_format);
                    }
                    else {
                        data[i].label = label.ToString();
                    }
                }
                _datas = data;
            }

            public void SetSource(ICollectionView collectionView) {
                _collectionView = collectionView;
                _datas = null;
            }

            public bool IsEmpty => !_collectionView.Cast<object>().Any();

            public void UpdateHorizontalAxis(IAxisManager axis) {
                _horizontalAxis = axis;
                _datas = null;
            }

            public void UpdateVerticalAxis(IAxisManager axis) {
                _verticalAxis = axis;
                _datas = null;
            }

            public void UpdateHorizontalValue(Expression<Func<object, IAxisManager, AxisValue>> updator) {
                _horizontalUpdator = updator;
                _datas = null;
            }

            public void UpdateVerticalValue(Expression<Func<object, IAxisManager, AxisValue>> updator) {
                _verticalUpdator = updator;
                _datas = null;
            }

            public void ClearOrderValueUpdator() {
                _orderValueUpdator = null;
                _datas = null;
            }

            public void UpdateOrderValue(Expression<Func<object, object>> updator) {
                _orderValueUpdator = updator;
                _datas = null;
            }

            public void ClearLabelValueUpdator() {
                _labelValueUpdator = null;
                _datas = null;
            }

            public void UpdateLabelValue(Expression<Func<object, object>> updator, string format) {
                _labelValueUpdator = updator;
                _format = format;
                _datas = null;
            }
        }
    }

    [TypeConverter(typeof(OverlapMethodTypeConverter))]
    public interface IOverlapMethod
    {
        bool IsOverlap(Rect rect1, Point p1, Rect rect2, Point p2);
    }

    class DirectOverlap : IOverlapMethod
    {
        public bool IsOverlap(Rect rect1, Point p1, Rect rect2, Point p2) {
            return (rect1.Width + rect2.Width) / 2 > Math.Abs(p1.X - p2.X)
                && (rect1.Height + rect2.Height) / 2 > Math.Abs(p1.Y - p2.Y);
        }

        public static DirectOverlap Method { get; } = new DirectOverlap();

        public override string ToString() {
            return "Direct";
        }
    }

    class HorizontalOverlap : IOverlapMethod
    {
        public bool IsOverlap(Rect rect1, Point p1, Rect rect2, Point p2) {
            return (rect1.Width + rect2.Width) / 2 > Math.Abs(p1.X - p2.X);
        }

        public static HorizontalOverlap Method { get; } = new HorizontalOverlap();

        public override string ToString() {
            return "Horizontal";
        }
    }

    class VerticalOverlap : IOverlapMethod
    {
        public bool IsOverlap(Rect rect1, Point p1, Rect rect2, Point p2) {
            return (rect1.Height + rect2.Height) / 2 > Math.Abs(p1.Y - p2.Y);
        }

        public static VerticalOverlap Method { get; } = new VerticalOverlap();

        public override string ToString() {
            return "Vertical";
        }
    }

    class IgnoreOverlap : IOverlapMethod
    {
        public bool IsOverlap(Rect rect1, Point p1, Rect rect2, Point p2) {
            return false;
        }

        public static IgnoreOverlap Method { get; } = new IgnoreOverlap();

        public override string ToString() {
            return "Ignore";
        }
    }

    class CompositeOverlap : IOverlapMethod
    {
        public CompositeOverlap(IEnumerable<IOverlapMethod> methods) {
            this.methods = methods?.ToList();
        }

        public CompositeOverlap(params IOverlapMethod[] methods) {
            this.methods = methods.ToList();
        }

        private readonly List<IOverlapMethod> methods;

        public bool IsOverlap(Rect rect1, Point p1, Rect rect2, Point p2) {
            if (methods == null) {
                return false;
            }
            return methods.Any(method => method.IsOverlap(rect1, p1, rect2, p2));
        }

        public override string ToString() {
            return string.Join(",", methods.Select(method => method.ToString()));
        }
    }

    public class OverlapMethodTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            if (!(value is string text)) return null;

            var values = text.Split(',');

            if (values.Length == 1) {
                return MapToMethod(values[0]);
            }
            if (values.All(IsValidMethod)) {
                return new CompositeOverlap(values.Select(MapToMethod));
            }
            return null;
        }

        private static IOverlapMethod MapToMethod(string method) {
            switch (method.Trim()) {
                case "Direct":
                    return DirectOverlap.Method;
                case "Horizontal":
                    return HorizontalOverlap.Method;
                case "Vertical":
                    return VerticalOverlap.Method;
                case "":
                case "Ignore":
                default:
                    return IgnoreOverlap.Method;
            }
        }

        private static readonly string[] valids = new[] { "", "Direct", "Horizontal", "Vertical", "Ignore" };
        private static bool IsValidMethod(string method) {
            method = method.Trim();
            return valids.Contains(method);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
            return destinationType == typeof(string);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            if (value is IOverlapMethod) {
                return value.ToString();
            }
            return null;
        }
    }
}
