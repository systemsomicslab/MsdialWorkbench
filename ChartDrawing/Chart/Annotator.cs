using CompMs.Graphics.Core.Base;
using CompMs.Common.Extension;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Data;

namespace CompMs.Graphics.Chart
{
    public class Annotator : ChartBaseControl
    {
        static Annotator() {
            culture = CultureInfo.GetCultureInfo("en-us");
            typeFace = new Typeface("Calibri");
            IsHitTestVisibleProperty.OverrideMetadata(typeof(Annotator), new FrameworkPropertyMetadata(false));
        }

        public static readonly DependencyProperty DatasProperty =
            DependencyProperty.Register(
                nameof(Datas), typeof(LabelData[]), typeof(Annotator),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnDatasChanged,
                    CoerceDatas));

        LabelData[] Datas {
            get => (LabelData[])GetValue(DatasProperty);
            set => SetValue(DatasProperty, value);
        }

        bool ShouldCoerceDatas = false;
        static void OnDatasChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {

        }

        static object CoerceDatas(DependencyObject d, object value) {
            var chart = (Annotator)d;
            var datas = chart.Datas;

            if (chart.ShouldCoerceDatas) {
                if (!chart.ReadCleanFlag(PropertyClean.ItemsSource)) {
                    datas = chart.UpdateItemsSource(datas);
                }
                if (!chart.ReadCleanFlag(PropertyClean.Item)) {
                    datas = chart.UpdateItem(datas);
                }
                if (!chart.ReadCleanFlag(PropertyClean.Horizontal)) {
                    datas = chart.UpdateHorizontalItems(datas);
                }
                if (!chart.ReadCleanFlag(PropertyClean.Vertical)) {
                    datas = chart.UpdateVerticalItems(datas);
                }
                if (!chart.ReadCleanFlag(PropertyClean.Order)) {
                    datas = chart.UpdateOrderItems(datas);
                }
                if (!chart.ReadCleanFlag(PropertyClean.Label)) {
                    datas = chart.UpdateLabel(datas);
                }
                chart.ShouldCoerceDatas = new[]
                {
                    !chart.ReadCleanFlag(PropertyClean.ItemsSource),
                    !chart.ReadCleanFlag(PropertyClean.Item),
                    !chart.ReadCleanFlag(PropertyClean.Horizontal),
                    !chart.ReadCleanFlag(PropertyClean.Vertical),
                    !chart.ReadCleanFlag(PropertyClean.Order),
                    !chart.ReadCleanFlag(PropertyClean.Label),
                }.Any(b => b);
            }

            value = datas;
            return value;
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource), typeof(System.Collections.IEnumerable), typeof(Annotator),
                new FrameworkPropertyMetadata(
                    default(System.Collections.IEnumerable),
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
            chart.CoerceValue(DatasProperty);
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
            CoerceValue(DatasProperty);
        }

        private LabelData[] UpdateItemsSource(LabelData[] value) {
            source = collectionView?.Cast<object>().ToList();
            if (source == null || !source.Any()) {
                WriteCleanFlag(PropertyClean.ItemsSource, true);
                return new LabelData[0];
            }

            dataType = source.First().GetType();
            var datas = source.Select(_ => new LabelData()).ToArray();

            WriteCleanFlag(PropertyClean.ItemsSource, true);
            WriteCleanFlag(PropertyClean.Horizontal | PropertyClean.Vertical | PropertyClean.Label | PropertyClean.Order, false);

            return datas;
        }

        private LabelData[] UpdateItem(LabelData[] value) {
            var result = UpdateItemsSource(value);
            WriteCleanFlag(PropertyClean.Item, true);
            return result;
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
            chart.CoerceValue(DatasProperty);
        }

        LabelData[] UpdateHorizontalItems(LabelData[] datas) {
            if (string.IsNullOrEmpty(HorizontalPropertyName)) {
                WriteCleanFlag(PropertyClean.Horizontal, true);
                return datas;
            }

            var hPropertyInfo = dataType?.GetProperty(HorizontalPropertyName);
            var hAxis = HorizontalAxis;
            if (hPropertyInfo == null || hAxis == null || datas == null || !datas.Any()) {
                return datas;
            }

            foreach ((var obj, var idx) in source.WithIndex()) {
                datas[idx].x = hPropertyInfo == null ? 0 : hAxis.TranslateToAxisValue(hPropertyInfo.GetValue(obj));
            }
            WriteCleanFlag(PropertyClean.Horizontal, true);
            return datas;
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
            chart.CoerceValue(DatasProperty);
        }

        LabelData[] UpdateVerticalItems(LabelData[] datas) {
            if (string.IsNullOrEmpty(VerticalPropertyName)) {
                WriteCleanFlag(PropertyClean.Vertical, true);
                return datas;
            }

            var vPropertyInfo = dataType?.GetProperty(VerticalPropertyName);
            var vAxis = VerticalAxis;
            if (vPropertyInfo == null || vAxis == null || datas == null || !datas.Any()) {
                return datas;
            }

            foreach ((var obj, var idx) in source.WithIndex()) {
                datas[idx].y = vPropertyInfo == null ? 0 : vAxis.TranslateToAxisValue(vPropertyInfo.GetValue(obj));
            }
            WriteCleanFlag(PropertyClean.Vertical, true);
            return datas;
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
            chart.CoerceValue(DatasProperty);
        }

        LabelData[] UpdateOrderItems(LabelData[] datas) {
            if (datas == null || !datas.Any()) {
                return datas;
            }

            PropertyInfo prop = null;
            if (!string.IsNullOrEmpty(OrderingPropertyName)) {
                prop = dataType?.GetProperty(OrderingPropertyName);
            }
            foreach ((var obj, var idx) in source.WithIndex()) {
                var value = prop?.GetValue(obj);
                if (value is double dvalue) {
                    datas[idx].order = dvalue;
                }
                else if (value is IConvertible conv) {
                    datas[idx].order = Convert.ToDouble(conv);
                }
                else {
                    datas[idx].order = 0;
                }
            }
            WriteCleanFlag(PropertyClean.Order, true);
            return datas;
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
            chart.CoerceValue(DatasProperty);
        }

        LabelData[] UpdateLabel(LabelData[] datas) {
            if (dataType == null || datas == null || !datas.Any()) {
                return datas;
            }

            var format = Format;
            var lPropertyInfo = LabelPropertyName != null ? dataType.GetProperty(LabelPropertyName) : null;

            foreach ((var obj, var idx) in source.Select(o => lPropertyInfo?.GetValue(o)).WithIndex()) {
                if (obj == null)
                    datas[idx].label = string.Empty;
                else if (obj is double val)
                    datas[idx].label = val.ToString(format);
                else
                    datas[idx].label = obj.ToString();
            }
            WriteCleanFlag(PropertyClean.Label, true);
            return datas;
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
            var datas = Datas;
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

            var texts = new List<Tuple<FormattedText, Point>>();
            var brush = Brush;
            var fontSize = FontSize;
            bool flippedX = FlippedX, flippedY = FlippedY;
            var overlap = Overlap;
            foreach(var data in datas.OrderByDescending(d => d.order)) {
                if (TopN.HasValue && texts.Count >= TopN)
                    break;
                if (!string.IsNullOrEmpty(data.label) && hAxis.Range.Contains(data.x) && vAxis.Range.Contains(data.y)) {

                    double xx = hAxis.TranslateToRenderPoint(data.x, flippedX) * actualWidth;
                    double yy = vAxis.TranslateToRenderPoint(data.y, flippedY) * actualHeight;

                    var text = new FormattedText(data.label, culture, FlowDirection.LeftToRight, typeFace, fontSize, brush, 1)
                    {
                        TextAlignment = TextAlignment.Center
                    };
                    var p = new Point(xx + dx, yy + dy);

                    if (!texts.Any(other => overlap.IsOverlap(text, p, other.Item1, other.Item2))) {
                        texts.Add(Tuple.Create(text, p));
                    }
                }
            }

            foreach ((var text, var point) in texts) {
                drawingContext.DrawText(text, point);
            }
        }

        #region field
        private ICollectionView collectionView;
        private List<object> source;
        private Type dataType;
        private static readonly CultureInfo culture;
        private static readonly Typeface typeFace;
        private PropertyClean cleanFlags = PropertyClean.All;
        #endregion

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
    }

    [TypeConverter(typeof(OverlapMethodTypeConverter))]
    public interface IOverlapMethod
    {
        bool IsOverlap(FormattedText text1, Point p1, FormattedText text2, Point p2);
    }

    class DirectOverlap : IOverlapMethod
    {
        public bool IsOverlap(FormattedText text1, Point p1, FormattedText text2, Point p2) {
            return (text1.Width + text2.Width) / 2 > Math.Abs(p1.X - p2.X)
                && (text1.Height + text2.Height) / 2 > Math.Abs(p1.Y - p2.Y);
        }

        public static DirectOverlap Method { get; } = new DirectOverlap();

        public override string ToString() {
            return "Direct";
        }
    }

    class HorizontalOverlap : IOverlapMethod
    {
        public bool IsOverlap(FormattedText text1, Point p1, FormattedText text2, Point p2) {
            return text2.Width / 2 > Math.Abs(p1.X - p2.X);
        }

        public static HorizontalOverlap Method { get; } = new HorizontalOverlap();

        public override string ToString() {
            return "Horizontal";
        }
    }

    class VerticalOverlap : IOverlapMethod
    {
        public bool IsOverlap(FormattedText text1, Point p1, FormattedText text2, Point p2) {
            return text2.Height / 2 > Math.Abs(p1.Y - p2.Y);
        }

        public static VerticalOverlap Method { get; } = new VerticalOverlap();

        public override string ToString() {
            return "Vertical";
        }
    }

    class IgnoreOverlap : IOverlapMethod
    {
        public bool IsOverlap(FormattedText text1, Point p1, FormattedText text2, Point p2) {
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

        public bool IsOverlap(FormattedText text1, Point p1, FormattedText text2, Point p2) {
            if (methods == null) {
                return false;
            }
            return methods.Any(method => method.IsOverlap(text1, p1, text2, p2));
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
                case "":
                case "Ignore":
                    return IgnoreOverlap.Method;
                case "Direct":
                    return DirectOverlap.Method;
                case "Horizontal":
                    return HorizontalOverlap.Method;
                case "Vertical":
                    return VerticalOverlap.Method;
                default:
                    throw new NotSupportedException(method);
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
