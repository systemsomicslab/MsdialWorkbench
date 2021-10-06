using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Chart
{
    public class BarControl : ChartBaseControl
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource), typeof(System.Collections.IEnumerable), typeof(BarControl),
            new PropertyMetadata(default(System.Collections.IEnumerable), OnItemsSourceChanged)
            );

        public static readonly DependencyProperty HorizontalPropertyNameProperty = DependencyProperty.Register(
            nameof(HorizontalPropertyName), typeof(string), typeof(BarControl),
            new PropertyMetadata(default(string), OnHorizontalPropertyNameChanged)
            );

        public static readonly DependencyProperty VerticalPropertyNameProperty = DependencyProperty.Register(
            nameof(VerticalPropertyName), typeof(string), typeof(BarControl),
            new PropertyMetadata(default(string), OnVerticalPropertyNameChanged)
            );

        public static readonly DependencyProperty BarWidthProperty = DependencyProperty.Register(
            nameof(BarWidth), typeof(double), typeof(BarControl),
            new PropertyMetadata(0.95, ChartUpdate)
            );

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

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            nameof(SelectedItem), typeof(object), typeof(BarControl),
            new PropertyMetadata(null, OnSelectedItemChanged));

        public static readonly DependencyProperty FocusedItemProperty = DependencyProperty.Register(
            nameof(FocusedItem), typeof(object), typeof(BarControl),
            new PropertyMetadata(null)
            );

        public static readonly DependencyProperty FocusedPointProperty = DependencyProperty.Register(
            nameof(FocusedPoint), typeof(Point), typeof(BarControl),
            new PropertyMetadata(default)
            );

        public static readonly DependencyProperty BrushMapperProperty =
            DependencyProperty.Register(
                nameof(BrushMapper), typeof(IBrushMapper), typeof(BarControl),
                new PropertyMetadata(null, OnBrushMapperChanged));

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

        public System.Collections.IEnumerable ItemsSource {
            get => (System.Collections.IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public string HorizontalPropertyName {
            get => (string)GetValue(HorizontalPropertyNameProperty);
            set => SetValue(HorizontalPropertyNameProperty, value);
        }

        public string VerticalPropertyName {
            get => (string)GetValue(VerticalPropertyNameProperty);
            set => SetValue(VerticalPropertyNameProperty, value);
        }

        public double BarWidth {
            get => (double)GetValue(BarWidthProperty);
            set => SetValue(BarWidthProperty, value);
        }

        public object SelectedItem {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public object FocusedItem {
            get => GetValue(FocusedItemProperty);
            set => SetValue(FocusedItemProperty, value);
        }

        public Point FocusedPoint {
            get => (Point)GetValue(FocusedPointProperty);
            set => SetValue(FocusedPointProperty, value);
        }

        public static readonly DependencyProperty BarPenProperty =
            DependencyProperty.Register(nameof(BarPen), typeof(Pen), typeof(BarControl));

        public Pen BarPen {
            get => (Pen)GetValue(BarPenProperty);
            set => SetValue(BarPenProperty, value);
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
        private PropertyInfo hPropertyReflection;
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
            if (hPropertyReflection == null
               || vPropertyReflection == null
               || HorizontalAxis == null
               || VerticalAxis == null
               || cv == null
               )
                return;

            var pen = BarPen;
            double actualWidth = ActualWidth, actualHeight = ActualHeight;
            var xs = visualChildren.OfType<AnnotatedDrawingVisual>().Select(v => HorizontalAxis.TranslateToAxisValue(hPropertyReflection.GetValue(v.Annotation)).Value).OrderBy(x => x).ToArray();
            var barwidth = Enumerable.Range(1, xs.Length - 1).Min(i => xs[i] - xs[i - 1]) * BarWidth;

            double yorigin = VerticalAxis.TranslateToRenderPoint(0d, FlippedY, actualHeight);
            foreach (var visual in visualChildren) {
                var dv = visual as AnnotatedDrawingVisual;
                var o = dv.Annotation;
                var x = hPropertyReflection.GetValue(o);
                var y = vPropertyReflection.GetValue(o);

                var haxv = HorizontalAxis.TranslateToAxisValue(x);
                double xxl = HorizontalAxis.TranslateToRenderPoint(haxv - barwidth / 2, FlippedX, actualWidth);
                double xxr = HorizontalAxis.TranslateToRenderPoint(haxv + barwidth / 2, FlippedX, actualWidth);
                double yy = VerticalAxis.TranslateToRenderPoint(y, FlippedY, actualHeight);
                dv.Center = new Point((xxl + xxr) / 2, yy);

                using (var dc = dv.RenderOpen()) {
                    dc.DrawRectangle(Selector.GetBrush(o), pen, new Rect(new Point(xxl, yy), new Point(xxr, yorigin)));
                }
            }
        }

        private void SetDrawingVisuals() {
            if (cv == null) return;

            visualChildren.Clear();
            foreach (var o in cv)
                visualChildren.Add(new AnnotatedDrawingVisual(o));
        }

        #region Event handler
        static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = d as BarControl;
            if (chart == null) return;

            chart.dataType = null;
            chart.cv = null;

            if (chart.ItemsSource == null) return;

            var enumerator = chart.ItemsSource.GetEnumerator();
            if (!enumerator.MoveNext()) return;

            chart.dataType = enumerator.Current.GetType();
            chart.cv = CollectionViewSource.GetDefaultView(chart.ItemsSource) as CollectionView;

            chart.SetDrawingVisuals();

            if (chart.HorizontalPropertyName != null)
                chart.hPropertyReflection = chart.dataType.GetProperty(chart.HorizontalPropertyName);
            if (chart.VerticalPropertyName != null)
                chart.vPropertyReflection = chart.dataType.GetProperty(chart.VerticalPropertyName);
            if (chart.SelectedItem != null)
                chart.cv.MoveCurrentTo(chart.SelectedItem);

            chart.Update();
        }

        static void OnHorizontalPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = d as BarControl;
            if (chart == null) return;

            if (chart.dataType != null)
                chart.hPropertyReflection = chart.dataType.GetProperty((string)e.NewValue);

            chart.Update();
        }

        static void OnVerticalPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = d as BarControl;
            if (chart == null) return;

            if (chart.dataType != null)
                chart.vPropertyReflection = chart.dataType.GetProperty((string)e.NewValue);

            chart.Update();
        }

        static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = d as BarControl;
            if (chart == null) return;

            if (chart.cv != null)
                chart.cv.MoveCurrentTo(e.NewValue);
        }
        #endregion

        #region Mouse event
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
        #endregion

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
