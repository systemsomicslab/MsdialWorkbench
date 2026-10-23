using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Data;
using CompMs.Graphics.Helper;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
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

        public Annotator()
        {
            _lazyDatas.Attach((s, e) => InvalidateVisual());
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

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
            var accessor = PropertiesAccessor.Build(dataType, HorizontalPropertyName);
            lazyDatas.UpdateHorizontalValue(accessor);
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
            var accessor = PropertiesAccessor.Build(dataType, VerticalPropertyName);
            lazyDatas.UpdateVerticalValue(accessor);
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
            var accessor = PropertiesAccessor.Build(dataType, OrderingPropertyName);
            lazyDatas.UpdateOrderValue(accessor);
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
            var accessor = PropertiesAccessor.Build(dataType, LabelPropertyName);
            lazyDatas.UpdateLabelValue(accessor, Format);
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
            new FrameworkPropertyMetadata("N5", FrameworkPropertyMetadataOptions.AffectsRender, OnLabelChanged)
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

        public static readonly DependencyProperty MinimumRelativeOrderProperty = DependencyProperty.Register(
            nameof(MinimumRelativeOrder), typeof(double), typeof(Annotator),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender),
            value => (double)value >= 0d && (double)value <= 1d);

        /// <summary>
        /// Minimum fraction of the maximum ordering value in the full source required for a label.
        /// Spectrum views order by raw intensity, so this is independent of zoom and axis transforms.
        /// </summary>
        public double MinimumRelativeOrder {
            get => (double)GetValue(MinimumRelativeOrderProperty);
            set => SetValue(MinimumRelativeOrderProperty, value);
        }

        public static readonly DependencyProperty MinimumOrderProperty = DependencyProperty.Register(
            nameof(MinimumOrder), typeof(double), typeof(Annotator),
            new FrameworkPropertyMetadata(double.NegativeInfinity, FrameworkPropertyMetadataOptions.AffectsRender),
            value => !double.IsNaN((double)value));

        /// <summary>
        /// Minimum ordering value required for a label.
        /// </summary>
        public double MinimumOrder {
            get => (double)GetValue(MinimumOrderProperty);
            set => SetValue(MinimumOrderProperty, value);
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

            var texts = new List<(FormattedText Text, Point Point, Rect Bounds, Rect Region, double Mass)>();
            var brush = Brush;
            var fontSize = FontSize;
            bool flippedX = FlippedX, flippedY = FlippedY;
            var overlap = Overlap;
            var ppd = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var isSpectrum = overlap is SpectrumOverlap;
            FormattedText CreateText(string label) => new FormattedText(label, culture, FlowDirection.LeftToRight, typeFace, fontSize, brush, ppd) {
                TextAlignment = TextAlignment.Center,
            };
            var repText = new FormattedText("1000.00000", culture, FlowDirection.LeftToRight, typeFace, fontSize, brush, ppd) { TextAlignment = TextAlignment.Center };
            var repWidth = repText.Width;
            var repHeight = repText.Height;
            var repVec = new Vector(repWidth / 2, repHeight / 2);
            var minimumRelativeOrder = MinimumRelativeOrder;
            var minimumOrder = minimumRelativeOrder > 0d
                ? datas.Select(data => data.order).DefaultIfEmpty(0d).Max() * minimumRelativeOrder
                : double.NegativeInfinity;
            minimumOrder = Math.Max(minimumOrder, MinimumOrder);
            foreach(var data in datas.OrderByDescending(d => d.order)) {
                if (TopN.HasValue && texts.Count >= TopN)
                    break;
                if (data.order < minimumOrder || (minimumRelativeOrder > 0d && data.order <= 0d)) {
                    break;
                }
                if (!string.IsNullOrEmpty(data.label) && hAxis.Range.Contains(data.X) && vAxis.Range.Contains(data.Y)) {
                    var mass = isSpectrum ? data.HorizontalValue : double.NaN;
                    // Accepted stronger peaks suppress neighbours using unrounded m/z, independently of zoom.
                    if (isSpectrum && texts.Any(other => Math.Abs(mass - other.Mass) <= SpectrumOverlap.NeighbourMassTolerance)) {
                        continue;
                    }

                    double xx = hAxis.TranslateToRenderPoint(data.X, flippedX, actualWidth);
                    double yy = vAxis.TranslateToRenderPoint(data.Y, flippedY, actualHeight);
                    var p = new Point(xx + dx, yy + dy);
                    var text = isSpectrum ? CreateText(data.label) : null;
                    // Exclude line leading from collision bounds, keeping the full text width.
                    var rect = text is null
                        ? new Rect(p - repVec, p + repVec)
                        : new Rect(p.X - text.Width / 2d, p.Y + text.Height + text.OverhangAfter - text.Extent, text.Width, text.Extent);
                    var region = text is null ? rect : new Rect(p.X - text.Width / 2d, p.Y, text.Width, text.Height);
                    if (isSpectrum) {
                        var relativeHeight = Math.Max(0d, Math.Min(1d, flippedY ? 1d - yy / actualHeight : yy / actualHeight));
                        var anchorX = p.X;
                        foreach (var other in texts.OrderBy(other => other.Bounds.Left)) {
                            if (!overlap.IsOverlap(rect, p, other.Bounds, other.Point)) {
                                continue;
                            }
                            var shift = other.Bounds.Right - rect.Left + 2d;
                            // Limit movement so the label remains associated with its peak.
                            if (p.X + shift - anchorX > text.Width / 2d || rect.Right + shift > actualWidth) {
                                rect = Rect.Empty;
                                break;
                            }
                            p.X += shift;
                            rect.Offset(shift, 0d);
                        }
                        if (rect.IsEmpty) {
                            continue;
                        }
                        region.Offset(p.X - anchorX, 0d);
                        if (IsSpectrumRegionCrowded(region, texts.Select(other => other.Region), relativeHeight)) {
                            continue;
                        }
                    }
                    else if (texts.Any(other => overlap.IsOverlap(rect, p, other.Bounds, other.Point))) {
                        continue;
                    }

                    texts.Add((text ?? CreateText(data.label), p, rect, region, mass));
                }
            }

            foreach (var label in texts) {
                drawingContext.DrawText(label.Text, label.Point);
            }
        }

        private static bool IsSpectrumRegionCrowded(Rect candidate, IEnumerable<Rect> regions, double relativeHeight) {
            // Weak peaks need horizontal space; prominent peaks can also use vertical clearance.
            // Vertical clearance reaches full weight at half the plot height.
            var verticalWeight = Math.Min(1d, relativeHeight / (1d - relativeHeight));
            foreach (var region in regions) {
                var dx = Math.Abs(candidate.Left + candidate.Width / 2d - region.Left - region.Width / 2d);
                var dy = Math.Abs(candidate.Top - region.Top);
                var horizontalSpacing = (candidate.Width + region.Width) / 2d;
                var verticalSpacing = (candidate.Height + region.Height) / 2d;
                var verticalClearance = dy / verticalSpacing * verticalWeight;
                if (dx / horizontalSpacing + verticalClearance < 1d) {
                    return true;
                }
            }
            return false;
        }

        private ICollectionView collectionView;
        private Type dataType;
        private static readonly CultureInfo culture;
        private static readonly Typeface typeFace;
        private PropertyClean cleanFlags = PropertyClean.All;

        private void OnLoaded(object sender, RoutedEventArgs e) {
            _lazyDatas?.Attach((s, e) => InvalidateVisual());
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) {
            _lazyDatas?.Detach();
        }

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

        class LabelData : NotifiableDataPoint {

            internal string label;
            internal double order;
            internal Brush brush;
            private readonly IPropertiesAccessor _horizontalAccessor;

            internal double HorizontalValue => _horizontalAccessor.Apply(Item!) is double value ? value : double.NaN;

            public LabelData(object data, IAxisManager xAxisManager, IAxisManager yAxisManager, IPropertiesAccessor xPropertiesAccessor, IPropertiesAccessor yPropertiesAccessor)
                : base(data, xAxisManager, yAxisManager, xPropertiesAccessor, yPropertiesAccessor) {
                _horizontalAccessor = xPropertiesAccessor;
            }
        }

        sealed class LazyDatas {
            private LabelData[]? _datas;
            private IAxisManager? _horizontalAxis;
            private IAxisManager? _verticalAxis;
            private IPropertiesAccessor? _horizontalAccessor, _verticalAccessor, _orderValueAccessor, _labelValueAccessor;
            private ICollectionView? _collectionView;
            private string? _format;

            public LabelData[]? Datas {
                get {
                    if (_datas is null) {
                        Reconstruct();
                    }
                    return _datas;
                }
            }

            private void Reconstruct() {
                var source = _collectionView?.Cast<object>().ToArray();
                if (source is null || _horizontalAxis is null || _verticalAxis is null || _horizontalAccessor is null || _verticalAccessor is null) {
                    ResetDatas();
                    return;
                }
                var data = new LabelData[source.Length];
                for (int i = 0; i < data.Length; i++) {
                    var item = source[i];
                    data[i] = new LabelData(item, _horizontalAxis, _verticalAxis, _horizontalAccessor, _verticalAccessor);
                    data[i].PropertyChanged += _attaching;

                    var order = _orderValueAccessor?.Apply(item);
                    if (order is double dvalue) {
                        data[i].order = dvalue;
                    }
                    else if (order is IConvertible conv) {
                        data[i].order = Convert.ToDouble(conv);
                    }
                    else {
                        data[i].order = 0;
                    }

                    var label = _labelValueAccessor?.Apply(item);
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
                _attached = _attaching;
                _attaching = null;
                _datas = data;
            }

            private void ResetDatas() {
                if (_datas is null) {
                    return;
                }
                foreach (var data in _datas) {
                    data.PropertyChanged -= _attached;
                }
                _attaching = _attached;
                _attached = null;
                _datas = null;
            }

            public void SetSource(ICollectionView collectionView) {
                _collectionView = collectionView;
                ResetDatas();
            }

            public bool IsEmpty => !_collectionView.Cast<object>().Any();

            public void UpdateHorizontalAxis(IAxisManager axis) {
                _horizontalAxis = axis;
                ResetDatas();
            }

            public void UpdateVerticalAxis(IAxisManager axis) {
                _verticalAxis = axis;
                ResetDatas();
            }

            public void UpdateHorizontalValue(IPropertiesAccessor accessor) {
                _horizontalAccessor = accessor;
                ResetDatas();
            }

            public void UpdateVerticalValue(IPropertiesAccessor accessor) {
                _verticalAccessor = accessor;
                ResetDatas();
            }

            public void UpdateOrderValue(IPropertiesAccessor? accessor) {
                _orderValueAccessor = accessor;
                ResetDatas();
            }

            public void ClearLabelValueAccessor() {
                _labelValueAccessor = null;
                ResetDatas();
            }

            public void UpdateLabelValue(IPropertiesAccessor accessor, string format) {
                _labelValueAccessor = accessor;
                _format = format;
                ResetDatas();
            }

            private PropertyChangedEventHandler? _attaching, _attached;

            public void Attach(PropertyChangedEventHandler handle) {
                if (_datas is not null) {
                    foreach (var data in _datas) {
                        data.PropertyChanged += handle;
                    }
                    _attached += handle;
                }
                else {
                    _attaching += handle;
                }
            }

            public void Detach() {
                if (_datas is not null) {
                    foreach (var data in _datas) {
                        data.PropertyChanged -= _attached;
                    }
                    _attached = null;
                }
                else {
                    _attaching = null;
                }
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

    class SpectrumOverlap : IOverlapMethod
    {
        internal const double NeighbourMassTolerance = 5d;

        public bool IsOverlap(Rect rect1, Point p1, Rect rect2, Point p2) {
            return rect1.IntersectsWith(rect2);
        }

        public static SpectrumOverlap Method { get; } = new SpectrumOverlap();

        public override string ToString() => "Spectrum";
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
            if (value is not string text) return null;

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
                case "Spectrum":
                    return SpectrumOverlap.Method;
                case "":
                case "Ignore":
                default:
                    return IgnoreOverlap.Method;
            }
        }

        private static readonly string[] valids = ["", "Direct", "Horizontal", "Vertical", "Spectrum", "Ignore"];
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
