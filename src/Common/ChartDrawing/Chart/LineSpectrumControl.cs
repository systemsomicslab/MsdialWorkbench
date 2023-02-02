using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;

namespace CompMs.Graphics.Chart
{
    public sealed class LineSpectrumControl : ChartBaseControl
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource), typeof(IEnumerable), typeof(LineSpectrumControl),
                new FrameworkPropertyMetadata(
                    default(IEnumerable),
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnItemsSourceChanged));

        public IEnumerable ItemsSource {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = (LineSpectrumControl)d;
            chart.dataType = null;
            chart.cv = null;

            if (e.NewValue is null) {
                return;
            }

            var enumerator = ((IEnumerable)e.NewValue).GetEnumerator();
            if (!enumerator.MoveNext()) {
                return;
            }

            chart.dataType = enumerator.Current.GetType();
            chart.UpdateHorizontalPropertyGetter();
            chart.UpdateVerticalPropertyGetter();
            chart.UpdateHueGetter();

            chart.cv = CollectionViewSource.GetDefaultView(e.NewValue) as CollectionView;
            if (chart.GetValue(SelectedItemProperty) is object obj) {
                chart.cv?.MoveCurrentTo(obj);
            }
        }

        private Type dataType;
        private CollectionView cv;

        public static readonly DependencyProperty HorizontalPropertyNameProperty = DependencyProperty.Register(
            nameof(HorizontalPropertyName), typeof(string), typeof(LineSpectrumControl),
                new FrameworkPropertyMetadata(
                    default,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnHorizontalPropertyNameChanged));

        public string HorizontalPropertyName {
            get => (string)GetValue(HorizontalPropertyNameProperty);
            set => SetValue(HorizontalPropertyNameProperty, value);
        }

        private static void OnHorizontalPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = (LineSpectrumControl)d;
            chart.UpdateHorizontalPropertyGetter();
        }

        private Func<object, object> HorizontalPropertyGetter;

        private void UpdateHorizontalPropertyGetter() {
            if (dataType is null || GetValue(HorizontalPropertyNameProperty) is null) {
                return;
            }

            var arg = System.Linq.Expressions.Expression.Parameter(typeof(object));
            var casted = System.Linq.Expressions.Expression.Convert(arg, dataType);
            var property = System.Linq.Expressions.Expression.Property(casted, HorizontalPropertyName);
            var result = System.Linq.Expressions.Expression.Convert(property, typeof(object));
            var lambda = System.Linq.Expressions.Expression.Lambda<Func<object, object>>(result, arg);
            HorizontalPropertyGetter = lambda.Compile();
        }

        public static readonly DependencyProperty VerticalPropertyNameProperty =
            DependencyProperty.Register(
                nameof(VerticalPropertyName), typeof(string), typeof(LineSpectrumControl),
                new FrameworkPropertyMetadata(
                    default,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnVerticalPropertyNameChanged));

        public string VerticalPropertyName {
            get => (string)GetValue(VerticalPropertyNameProperty);
            set => SetValue(VerticalPropertyNameProperty, value);
        }

        private static void OnVerticalPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = (LineSpectrumControl)d;
            chart.UpdateVerticalPropertyGetter();
        }

        private Func<object, object> VerticalPropertyGetter;

        private void UpdateVerticalPropertyGetter() {
            if (dataType is null || GetValue(VerticalPropertyNameProperty) is null) {
                return;
            }

            var arg = System.Linq.Expressions.Expression.Parameter(typeof(object));
            var casted = System.Linq.Expressions.Expression.Convert(arg, dataType);
            var property = System.Linq.Expressions.Expression.Property(casted, VerticalPropertyName);
            var result = System.Linq.Expressions.Expression.Convert(property, typeof(object));
            var lambda = System.Linq.Expressions.Expression.Lambda<Func<object, object>>(result, arg);
            VerticalPropertyGetter = lambda.Compile();
        }

        public static readonly DependencyProperty LinePenProperty =
            DependencyProperty.Register(
                nameof(LinePen), typeof(Pen), typeof(LineSpectrumControl),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnLinePenChanged));

        public Pen LinePen {
            get => (Pen)GetValue(LinePenProperty);
            set => SetValue(LinePenProperty, value);
        }

        private static void OnLinePenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var c = (LineSpectrumControl)d;
            c.OnLinePenChanged((Pen)e.OldValue, (Pen)e.NewValue);
        }

        private void OnLinePenChanged(Pen oldValue, Pen newValue) {
            Selector.Update(newValue);
        }

        public static readonly DependencyProperty BrushMapperProperty =
            DependencyProperty.Register(
                nameof(BrushMapper), typeof(IBrushMapper), typeof(LineSpectrumControl),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnBrushMapperChanged));

        public IBrushMapper BrushMapper {
            get => (IBrushMapper)GetValue(BrushMapperProperty);
            set => SetValue(BrushMapperProperty, value);
        }

        private static void OnBrushMapperChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var c = (LineSpectrumControl)d;
            c.OnBrushMapperChanged((IBrushMapper)e.OldValue, (IBrushMapper)e.NewValue);
        }

        private void OnBrushMapperChanged(IBrushMapper oldValue, IBrushMapper newValue) {
            Selector.Update(newValue, LineThickness);
        }

        public static readonly DependencyProperty HuePropertyProperty =
            DependencyProperty.Register(
                nameof(HueProperty), typeof(string), typeof(LineSpectrumControl),
                new PropertyMetadata(null, OnHuePropertyChanged));

        public string HueProperty {
            get => (string)GetValue(HuePropertyProperty);
            set => SetValue(HuePropertyProperty, value);
        }

        private static void OnHuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var c = (LineSpectrumControl)d;
            c.UpdateHueGetter();
        }

        private Func<object, object> HueGetter;

        private void UpdateHueGetter() {
            if (dataType is null || GetValue(HuePropertyProperty) is null) {
                return;
            }
            var arg = System.Linq.Expressions.Expression.Parameter(typeof(object));
            var casted = System.Linq.Expressions.Expression.Convert(arg, dataType);
            var property = System.Linq.Expressions.Expression.Property(casted, HueProperty);
            var result = System.Linq.Expressions.Expression.Convert(property, typeof(object));
            var lambda = System.Linq.Expressions.Expression.Lambda<Func<object, object>>(result, arg);
            HueGetter = lambda.Compile();
        }

        public static readonly DependencyProperty LineThicknessProperty =
            DependencyProperty.Register(
                nameof(LineThickness), typeof(double), typeof(LineSpectrumControl),
                new FrameworkPropertyMetadata(
                    2d,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnLineThicknessChanged));

        public double LineThickness {
            get => (double)GetValue(LineThicknessProperty);
            set => SetValue(LineThicknessProperty, value);
        }

        private static void OnLineThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var c = (LineSpectrumControl)d;
            c.OnLineThicknessChanged((double)e.OldValue, (double)e.NewValue);
        }

        private void OnLineThicknessChanged(double oldValue, double newValue) {
            Selector.Update(BrushMapper, newValue);
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                nameof(SelectedItem), typeof(object), typeof(LineSpectrumControl),
                new PropertyMetadata(null, OnSelectedItemChanged));

        static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is LineSpectrumControl chart) {
                chart.cv?.MoveCurrentTo(e.NewValue);
            }
        }

        public object SelectedItem {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public static readonly DependencyProperty FocusedItemProperty =
            DependencyProperty.Register(
                nameof(FocusedItem), typeof(object), typeof(LineSpectrumControl),
                new PropertyMetadata(null));

        public object FocusedItem {
            get => GetValue(FocusedItemProperty);
            set => SetValue(FocusedItemProperty, value);
        }

        public static readonly DependencyProperty FocusedPointProperty =
            DependencyProperty.Register(
                nameof(FocusedPoint), typeof(Point), typeof(LineSpectrumControl),
                new PropertyMetadata(default));

        public Point FocusedPoint {
            get => (Point)GetValue(FocusedPointProperty);
            set => SetValue(FocusedPointProperty, value);
        }

        private PenSelector Selector {
            get {
                if (selector is null) {
                    return selector = new PenSelector();
                }
                return selector;
            }
        }
        private PenSelector selector;

        public LineSpectrumControl() {
            MouseLeftButtonDown += VisualSelectOnClick;
            MouseMove += VisualFocusOnMouseOver;
            ClipToBounds = true;
        }

        protected override void Update() {
            visualChildren.Clear();

            if (HorizontalPropertyGetter == null
               || VerticalPropertyGetter == null
               || HorizontalAxis == null
               || VerticalAxis == null
               || cv == null)
                return;

            var datas = new List<(double, double, DrawingVisual, Pen)>();
            try {
                foreach (var o in cv) {
                    var x = HorizontalPropertyGetter.Invoke(o);
                    var xx = HorizontalAxis.TranslateToRenderPoint(x, FlippedX, ActualWidth);
                    if (xx < 0 || xx > ActualWidth) {
                        continue;
                    }

                    var y = VerticalPropertyGetter.Invoke(o);
                    var yy = VerticalAxis.TranslateToRenderPoint(y, FlippedY, ActualHeight);

                    var dv = new AnnotatedDrawingVisual(o) { Center = new Point(xx, yy) };
                    var pen = Selector.GetPen(HueGetter?.Invoke(o) ?? o);

                    datas.Add((xx, yy, dv, pen));
                }
            }
            catch (NotSupportedException) {
                return;
            }

            var y0 = VerticalAxis.TranslateToRenderPoint(0, FlippedY, ActualHeight);
            foreach ((var x, var y, var dv, var pen) in datas) {
                using (var dc = dv.RenderOpen()) {
                    dc.DrawLine(pen, new Point(x, y), new Point(x, y0));
                }
                visualChildren.Add(dv);
            }
        }

        void VisualFocusOnMouseOver(object sender, MouseEventArgs e) {
            var pt = e.GetPosition(this);

            VisualTreeHelper.HitTest(this,
                new HitTestFilterCallback(VisualHitTestFilter),
                new HitTestResultCallback(VisualFocusHitTest),
                // new GeometryHitTestParameters(new EllipseGeometry(pt, 50d, 50d))
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
    }
}