using System;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Helper;

namespace CompMs.Graphics.Chart
{
    public sealed class BarControl : ChartBaseControl
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource), typeof(System.Collections.IEnumerable), typeof(BarControl),
                new FrameworkPropertyMetadata(
                    default(System.Collections.IEnumerable),
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnItemsSourceChanged));

        public System.Collections.IEnumerable ItemsSource {
            get => (System.Collections.IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = d as BarControl;
            if (chart == null) return;

            chart.dataType = null;
            chart.cv = null;

            if (chart.ItemsSource == null) return;
            chart.cv = CollectionViewSource.GetDefaultView(chart.ItemsSource) as CollectionView;

            chart.SetDrawingVisuals();
            if (e.OldValue is INotifyCollectionChanged oldCollection) {
                oldCollection.CollectionChanged -= chart.OnItemsSourceCollectionChanged;
            }
            if (e.NewValue is INotifyCollectionChanged newCollection) {
                newCollection.CollectionChanged += chart.OnItemsSourceCollectionChanged;
            }

            var enumerator = chart.ItemsSource.GetEnumerator();
            if (!enumerator.MoveNext()) return;
            chart.dataType = enumerator.Current.GetType();

            if (chart.HorizontalPropertyName != null && ExpressionHelper.ValidatePropertyString(chart.dataType, chart.HorizontalPropertyName)) {
                chart.hGetter = ExpressionHelper.GetConvertToAxisValueExpression(chart.dataType, chart.HorizontalPropertyName).Compile();
            }
            if (chart.VerticalPropertyName != null)
                chart.vPropertyReflection = chart.dataType.GetProperty(chart.VerticalPropertyName);
            if (chart.SelectedItem != null)
                chart.cv.MoveCurrentTo(chart.SelectedItem);
        }

        private void OnItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (dataType == null) {
                dataType = cv.OfType<object>().FirstOrDefault()?.GetType();
                if (dataType != null) {
                    if (HorizontalPropertyName != null && ExpressionHelper.ValidatePropertyString(dataType, HorizontalPropertyName)) {
                        hGetter = ExpressionHelper.GetConvertToAxisValueExpression(dataType, HorizontalPropertyName).Compile();
                    }
                    if (VerticalPropertyName != null)
                        vPropertyReflection = dataType.GetProperty(VerticalPropertyName);
                    if (SelectedItem != null)
                        cv.MoveCurrentTo(SelectedItem);
                }
            }
            switch (e.Action) {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems) {
                        var visual = new AnnotatedDrawingVisual(item);
                        if (_itemVisual.TryAdd(item, visual)) {
                            visualChildren.Add(visual);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems) {
                        if (_itemVisual.TryRemove(item, out var visual)) {
                            visualChildren.Remove(visual);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    foreach (var item in e.NewItems) {
                        var visual = new AnnotatedDrawingVisual(item);
                        if (_itemVisual.TryAdd(item, visual)) {
                            visualChildren.Add(visual);
                        }
                    }
                    foreach (var item in e.NewItems) {
                        if (_itemVisual.TryRemove(item, out var visual)) {
                            visualChildren.Remove(visual);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    visualChildren.Clear();
                    _itemVisual.Clear();
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                default:
                    SetDrawingVisuals();
                    break;
            }
            if (SelectedItem != null && cv.Contains(SelectedItem))
                cv.MoveCurrentTo(SelectedItem);
            InvalidateVisual();
        }

        private readonly ConcurrentDictionary<object, AnnotatedDrawingVisual> _itemVisual = new ConcurrentDictionary<object, AnnotatedDrawingVisual>();
        private void SetDrawingVisuals() {
            if (cv == null) return;

            visualChildren.Clear();
            _itemVisual.Clear();
            foreach (var o in cv) {
                var visual = new AnnotatedDrawingVisual(o);
                if (_itemVisual.TryAdd(o, visual)) {
                    visualChildren.Add(visual);
                }
            }
        }

        public static readonly DependencyProperty HorizontalPropertyNameProperty =
            DependencyProperty.Register(
                nameof(HorizontalPropertyName), typeof(string), typeof(BarControl),
                new FrameworkPropertyMetadata(
                    default(string),
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnHorizontalPropertyNameChanged));

        public string HorizontalPropertyName {
            get => (string)GetValue(HorizontalPropertyNameProperty);
            set => SetValue(HorizontalPropertyNameProperty, value);
        }

        static void OnHorizontalPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = d as BarControl;
            if (chart == null) return;

            if (chart.dataType != null && ExpressionHelper.ValidatePropertyString(chart.dataType, (string)e.NewValue)) {
                chart.hGetter = ExpressionHelper.GetConvertToAxisValueExpression(chart.dataType, (string)e.NewValue).Compile();
            }
        }

        public static readonly DependencyProperty VerticalPropertyNameProperty =
            DependencyProperty.Register(
                nameof(VerticalPropertyName), typeof(string), typeof(BarControl),
                new FrameworkPropertyMetadata(
                    default(string),
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnVerticalPropertyNameChanged));

        public string VerticalPropertyName {
            get => (string)GetValue(VerticalPropertyNameProperty);
            set => SetValue(VerticalPropertyNameProperty, value);
        }

        static void OnVerticalPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = d as BarControl;
            if (chart == null) return;

            if (chart.dataType != null)
                chart.vPropertyReflection = chart.dataType.GetProperty((string)e.NewValue);
        }

        public static readonly DependencyProperty BarWidthProperty =
            DependencyProperty.Register(
                nameof(BarWidth), typeof(double), typeof(BarControl),
                new PropertyMetadata(0.95, ChartUpdate));

        public double BarWidth {
            get => (double)GetValue(BarWidthProperty);
            set => SetValue(BarWidthProperty, value);
        }

        public static readonly DependencyProperty BarBrushProperty =
            DependencyProperty.Register(
                nameof(BarBrush), typeof(Brush), typeof(BarControl),
                new PropertyMetadata(null, OnBarBrushChanged));

        public Brush BarBrush {
            get => (Brush)GetValue(BarBrushProperty);
            set => SetValue(BarBrushProperty, value);
        }

        static void OnBarBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((BarControl)d).BarBrushChanged((Brush)e.NewValue, (Brush)e.OldValue);
        }

        void BarBrushChanged(Brush newValue, Brush oldValue) {
            Selector.BarBrush = newValue;
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                nameof(SelectedItem), typeof(object), typeof(BarControl),
                new PropertyMetadata(null, OnSelectedItemChanged));

        public object SelectedItem {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = d as BarControl;
            if (chart == null) return;

            if (chart.cv != null)
                chart.cv.MoveCurrentTo(e.NewValue);
        }

        public static readonly DependencyProperty FocusedItemProperty =
            DependencyProperty.Register(
                nameof(FocusedItem), typeof(object), typeof(BarControl),
                new PropertyMetadata(null));

        public object FocusedItem {
            get => GetValue(FocusedItemProperty);
            set => SetValue(FocusedItemProperty, value);
        }

        public static readonly DependencyProperty FocusedPointProperty =
            DependencyProperty.Register(
                nameof(FocusedPoint), typeof(Point), typeof(BarControl),
                new PropertyMetadata(default));

        public Point FocusedPoint {
            get => (Point)GetValue(FocusedPointProperty);
            set => SetValue(FocusedPointProperty, value);
        }

        public static readonly DependencyProperty BrushMapperProperty =
            DependencyProperty.Register(
                nameof(BrushMapper), typeof(IBrushMapper), typeof(BarControl),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnBrushMapperChanged));

        public IBrushMapper BrushMapper {
            get => (IBrushMapper)GetValue(BrushMapperProperty);
            set => SetValue(BrushMapperProperty, value);
        }

        static void OnBrushMapperChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((BarControl)d).BrushMapperChanged((IBrushMapper)e.NewValue, (IBrushMapper)e.OldValue);
        }

        void BrushMapperChanged(IBrushMapper newValue, IBrushMapper oldValue) {
            Selector.Mapper = newValue;
        }

        public static readonly DependencyProperty BarPenProperty =
            DependencyProperty.Register(nameof(BarPen), typeof(Pen), typeof(BarControl));

        public Pen BarPen {
            get => (Pen)GetValue(BarPenProperty);
            set => SetValue(BarPenProperty, value);
        }

        private static readonly DependencyPropertyKey ActualBarWidthPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ActualBarWidth),
                typeof(double),
                typeof(BarControl),
                new PropertyMetadata(0d));

        public static readonly DependencyProperty ActualBarWidthProperty = ActualBarWidthPropertyKey.DependencyProperty;

        public double ActualBarWidth {
            get => (double)GetValue(ActualBarWidthProperty);
            private set => SetValue(ActualBarWidthPropertyKey, value);
        }

        private BrushSelector Selector {
            get {
                if (selector is null) {
                    return selector = new BrushSelector();
                }
                return selector;
            }
        }
        private BrushSelector selector;

        #region field
        private CollectionView cv;
        private Type dataType;
        private Func<object, IAxisManager, AxisValue> hGetter;
        private PropertyInfo vPropertyReflection;
        #endregion

        public BarControl() {
            BarPen = new Pen(Brushes.Black, 1);
            BarPen.Freeze();
            MouseLeftButtonDown += VisualSelectOnClick;
            MouseMove += VisualFocusOnMouseOver;
            ClipToBounds = true;
        }

        protected override void Update() {
            if (hGetter is null
               || vPropertyReflection is null
               || HorizontalAxis is null
               || VerticalAxis is null
               || cv is null
               )
                return;

            var pen = BarPen;
            var hAxis = HorizontalAxis;
            var vAxis = VerticalAxis;
            var flippedX = FlippedX;
            var flippedY = FlippedY;
            double actualWidth = ActualWidth, actualHeight = ActualHeight;
            var xs = visualChildren.OfType<AnnotatedDrawingVisual>().Select(v => hGetter.Invoke(v.Annotation, hAxis).Value).OrderBy(x => x).ToArray();
            var barwidth = 0d;
            if (xs.Length == 0) {
                barwidth = actualWidth;   
            }
            else if (xs.Length == 1) {
                barwidth = (RangeX?.Delta ?? 1d) * BarWidth;
            }
            else {
                barwidth = Enumerable.Range(1, xs.Length - 1).Min(i => xs[i] - xs[i - 1]) * BarWidth;
            }

            var actualBarWidth = ActualBarWidth = hAxis.TranslateToRenderPoint(barwidth, flippedX, actualWidth) - hAxis.TranslateToRenderPoint(0d, flippedX, actualWidth);

            double yorigin = vAxis.TranslateToRenderPoint(0d, flippedY, actualHeight);
            foreach (var visual in visualChildren) {
                var dv = visual as AnnotatedDrawingVisual;
                var o = dv.Annotation;
                var y = vPropertyReflection.GetValue(o);

                using (var dc = dv.RenderOpen()) {
                    var xAx = hGetter.Invoke(o, hAxis);
                    if (!hAxis.ContainsCurrent(xAx)) {
                        continue;
                    }
                    var yAx = vAxis.TranslateToAxisValue(y);
                    var xx = hAxis.TranslateToRenderPoint(xAx, flippedX, actualWidth);
                    var yy = vAxis.TranslateToRenderPoint(yAx, flippedY, actualHeight);
                    dv.Center = new Point(xx, yy);

                    dc.DrawRectangle(Selector.GetBrush(o), pen, new Rect(new Point(xx - actualBarWidth / 2, yy), new Point(xx + actualBarWidth / 2, yorigin)));
                }
            }
        }

        void VisualFocusOnMouseOver(object sender, MouseEventArgs e) {
            var pt = e.GetPosition(this);

            VisualTreeHelper.HitTest(this,
                new HitTestFilterCallback(VisualHitTestFilter),
                new HitTestResultCallback(VisualFocusHitTest),
                new PointHitTestParameters(pt)
                );
        }

        void VisualSelectOnClick(object sender, MouseButtonEventArgs e) {
            if (e.ClickCount == 1) {
                var pt = e.GetPosition(this);

                VisualTreeHelper.HitTest(this,
                    new HitTestFilterCallback(VisualHitTestFilter),
                    new HitTestResultCallback(VisualSelectHitTest),
                    new PointHitTestParameters(pt)
                    );
            }
        }

        HitTestFilterBehavior VisualHitTestFilter(DependencyObject d) {
            if (d is AnnotatedDrawingVisual)
                return HitTestFilterBehavior.Continue;
            return HitTestFilterBehavior.ContinueSkipSelf;
        }

        HitTestResultBehavior VisualFocusHitTest(HitTestResult result) {
            var dv = (AnnotatedDrawingVisual)result.VisualHit;
            var focussed = dv.Annotation;
            if (focussed != FocusedItem) {
                FocusedItem = focussed;
                FocusedPoint = dv.Center;
            }
            return HitTestResultBehavior.Stop;
        }

        HitTestResultBehavior VisualSelectHitTest(HitTestResult result) {
            SelectedItem = ((AnnotatedDrawingVisual)result.VisualHit).Annotation;
            return HitTestResultBehavior.Stop;
        }

        class BrushSelector
        {
            // 1. BrushMapper
            // 2. BarBrush
            // 3. Default (blue)
            public Brush BarBrush { get; set; }
            public IBrushMapper Mapper { get; set; }

            public Brush GetBrush(object o) {
                return Mapper?.Map(o)
                    ?? BarBrush
                    ?? Brushes.Blue;
            }
        }
    }
}
