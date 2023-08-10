using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Helper;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace CompMs.Graphics.Chart
{
    public sealed class HeatmapControl : ChartBaseControl
    {
        private ICollectionView _cv;
        private Func<object, IAxisManager, AxisValue> _hGetter, _vGetter;
        private Func<object, object> _zGetter;

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource), typeof(System.Collections.IEnumerable), typeof(HeatmapControl),
                new FrameworkPropertyMetadata(
                    default(System.Collections.IEnumerable),
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnItemsSourceChanged));

        public System.Collections.IEnumerable ItemsSource {
            get => (System.Collections.IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is HeatmapControl chart) {
                chart._cv = CollectionViewSource.GetDefaultView(chart.ItemsSource);
                var type = chart.GetDataType();
                if (type is null) {
                    return;
                }

                if (ExpressionHelper.ValidatePropertyString(type, chart.HorizontalPropertyName)) {
                    chart._hGetter = ExpressionHelper.GetConvertToAxisValueExpression(type, chart.HorizontalPropertyName).Compile();
                }
                if (ExpressionHelper.ValidatePropertyString(type, chart.VerticalPropertyName)) {
                    chart._vGetter = ExpressionHelper.GetConvertToAxisValueExpression(type, chart.VerticalPropertyName).Compile();
                }
                if (ExpressionHelper.ValidatePropertyString(type, chart.DegreePropertyName)) {
                    chart._zGetter = ExpressionHelper.GetPropertyGetterExpression(type, chart.DegreePropertyName).Compile();
                }
                if (chart.SelectedItem != null) {
                    chart._cv.MoveCurrentTo(chart.SelectedItem);
                }
            }
        }

        public static readonly DependencyProperty DataTypeProperty =
            DependencyProperty.Register(
                nameof(DataType),
                typeof(Type),
                typeof(HeatmapControl),
                new FrameworkPropertyMetadata(
                    default,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public Type DataType {
            get => (Type)GetValue(DataTypeProperty);
            set => SetValue(DataTypeProperty, value);
        }

        private Type GetDataType() {
            if (DataType is null) {
                return _cv?.OfType<object>().FirstOrDefault()?.GetType();
            }
            return DataType;
        }

        public static readonly DependencyProperty HorizontalPropertyNameProperty =
            DependencyProperty.Register(
                nameof(HorizontalPropertyName), typeof(string), typeof(HeatmapControl),
                new FrameworkPropertyMetadata(
                    default(string),
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnHorizontalPropertyNameChanged));

        public string HorizontalPropertyName {
            get => (string)GetValue(HorizontalPropertyNameProperty);
            set => SetValue(HorizontalPropertyNameProperty, value);
        }

        static void OnHorizontalPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is HeatmapControl chart) {
                if (chart.GetDataType() is Type type) {
                    if (ExpressionHelper.ValidatePropertyString(type, (string)e.NewValue)) {
                        chart._hGetter = ExpressionHelper.GetConvertToAxisValueExpression(type, (string)e.NewValue).Compile();
                    }
                }
            }
        }

        public static readonly DependencyProperty VerticalPropertyNameProperty =
            DependencyProperty.Register(
                nameof(VerticalPropertyName), typeof(string), typeof(HeatmapControl),
                new FrameworkPropertyMetadata(
                    default(string),
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnVerticalPropertyNameChanged));

        public string VerticalPropertyName {
            get => (string)GetValue(VerticalPropertyNameProperty);
            set => SetValue(VerticalPropertyNameProperty, value);
        }

        static void OnVerticalPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is HeatmapControl chart) {
                if (chart.GetDataType() is Type type) {
                    if (ExpressionHelper.ValidatePropertyString(type, (string)e.NewValue)) {
                        chart._vGetter = ExpressionHelper.GetConvertToAxisValueExpression(type, (string)e.NewValue).Compile();
                    }
                }
            }
        }

        public static readonly DependencyProperty DegreePropertyNameProperty =
            DependencyProperty.Register(
                nameof(DegreePropertyName), typeof(string), typeof(HeatmapControl),
                new FrameworkPropertyMetadata(
                    default(string),
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnDegreePropertyNameChanged));

        public string DegreePropertyName {
            get => (string)GetValue(DegreePropertyNameProperty);
            set => SetValue(DegreePropertyNameProperty, value);
        }

        static void OnDegreePropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is HeatmapControl chart) {
                if (chart.GetDataType() is Type type) {
                    if (ExpressionHelper.ValidatePropertyString(type, (string)e.NewValue)) {
                        chart._zGetter = ExpressionHelper.GetPropertyGetterExpression(type, (string)e.NewValue).Compile();
                    }
                }
            }
        }

        public static readonly DependencyProperty GradientBrushProperty =
            DependencyProperty.Register(
                nameof(GradientBrush), typeof(IBrushMapper), typeof(HeatmapControl),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public IBrushMapper GradientBrush {
            get => (IBrushMapper)GetValue(GradientBrushProperty);
            set => SetValue(GradientStopsProperty, value);
        }

        public static readonly DependencyProperty GradientStopsProperty =
            DependencyProperty.Register(
                nameof(GradientStops), typeof(GradientStopCollection), typeof(HeatmapControl),
                new PropertyMetadata(new GradientStopCollection()
                {
                    new GradientStop(Colors.Blue, 0),
                    new GradientStop(Colors.White, 0.5),
                    new GradientStop(Colors.Red, 1),
                }));

        public GradientStopCollection GradientStops {
            get => (GradientStopCollection)GetValue(GradientStopsProperty);
            set => SetValue(GradientStopsProperty, value);
        }

        public static readonly DependencyProperty PatchWidthProperty =
            DependencyProperty.Register(
                nameof(PatchWidth), typeof(double), typeof(HeatmapControl),
                new FrameworkPropertyMetadata(
                    1d, FrameworkPropertyMetadataOptions.AffectsRender));

        public double PatchWidth {
            get => (double)GetValue(PatchWidthProperty);
            set => SetValue(PatchWidthProperty, value);
        }

        public static readonly DependencyProperty PatchHeightProperty =
            DependencyProperty.Register(
                nameof(PatchHeight), typeof(double), typeof(HeatmapControl),
                new FrameworkPropertyMetadata(
                    1d, FrameworkPropertyMetadataOptions.AffectsRender));

        public double PatchHeight {
            get => (double)GetValue(PatchHeightProperty);
            set => SetValue(PatchHeightProperty, value);
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                nameof(SelectedItem), typeof(object), typeof(HeatmapControl),
                new PropertyMetadata(null, OnSelectedItemChanged));

        public object SelectedItem {
            get { return GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is HeatmapControl chart) {
                if (chart._cv != null)
                    chart._cv.MoveCurrentTo(e.NewValue);
            }
        }

        public static readonly DependencyProperty FocusedItemProperty =
            DependencyProperty.Register(
                nameof(FocusedItem), typeof(object), typeof(HeatmapControl),
                new PropertyMetadata(null));

        public object FocusedItem {
            get => GetValue(FocusedItemProperty);
            set => SetValue(FocusedItemProperty, value);
        }

        public static readonly DependencyProperty FocusedPointProperty =
            DependencyProperty.Register(
                nameof(FocusedPoint), typeof(Point), typeof(HeatmapControl),
                new PropertyMetadata(default(Point)));

        public Point FocusedPoint {
            get => (Point)GetValue(FocusedPointProperty);
            set => SetValue(FocusedPointProperty, value);
        }

        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);

            if (_hGetter is null
               || _vGetter is null
               || _zGetter is null
               || HorizontalAxis is null
               || VerticalAxis is null
               || GradientStops is null
               || _cv is null
               )
                return;

            visualChildren.Clear();
            double zmax = double.MinValue, zmin = double.MaxValue;
            double xwidth = HorizontalAxis.TranslateToRenderPoint(PatchWidth, FlippedX, ActualWidth) - HorizontalAxis.TranslateToRenderPoint(0d, FlippedX, ActualWidth);
            double ywidth = Math.Abs(VerticalAxis.TranslateToRenderPoint(PatchHeight, FlippedY, ActualHeight) - VerticalAxis.TranslateToRenderPoint(0d, FlippedY, ActualHeight));
            foreach (var o in _cv) {
                var z = _zGetter?.Invoke(o);
                zmax = Math.Max(zmax, Convert.ToDouble(z));
                zmin = Math.Min(zmin, Convert.ToDouble(z));
            }

            foreach (var o in _cv) {
                var x = _hGetter.Invoke(o, HorizontalAxis);
                var y = _vGetter.Invoke(o, VerticalAxis);

                double xx, yy, zz;
                xx = HorizontalAxis.TranslateToRenderPoint(x, FlippedX, ActualWidth);
                yy = VerticalAxis.TranslateToRenderPoint(y, FlippedY, ActualHeight);

                if (xx == double.NaN || yy == double.NaN) continue;

                var z = _zGetter.Invoke(o);
                Brush brush;
                if (GradientBrush is null)
                {
                    zz = Convert.ToDouble(z as IConvertible);
                    var color = GetGradientColor(GradientStops, zz, zmin, zmax);
                    brush = new SolidColorBrush(color);
                    brush.Freeze();
                }
                else {
                    brush = GradientBrush.Map(z);
                }

                var dv = new AnnotatedDrawingVisual(o)
                {
                    Center = new Point(xx, yy),
                    Clip = new RectangleGeometry(new Rect(RenderSize))
                };
                var dc = dv.RenderOpen();
                dc.DrawRectangle(brush, null, new Rect(xx - xwidth / 2, yy - ywidth / 2, xwidth, ywidth));
                dc.Close();
                visualChildren.Add(dv);
            }
        }

        private static Color BlendColors(Color ca, Color cb, double factor) {
            var f = (float)factor;
            return cb * f + ca * (1 - f);
        }

        private static Color GetGradientColor(GradientStopCollection gsc, double offset) {
            var lowers = gsc.Where(gs => gs.Offset <= offset).ToArray();
            var highers = gsc.Where(gs => gs.Offset > offset).ToArray();
            if (lowers.Length == 0) return highers.Min(gs => (gs.Offset, gs.Color)).Color;
            if (highers.Length == 0) return lowers.Max(gs => (gs.Offset, gs.Color)).Color;

            var lo = lowers.Max(gs => (gs.Offset, gs.Color));
            var hi = highers.Min(gs => (gs.Offset, gs.Color));
            var o = (offset - lo.Offset) / (hi.Offset - lo.Offset);
            return BlendColors(lo.Color, hi.Color, o);
        }

        private static Color GetGradientColor(GradientStopCollection gsc, double value, double min, double max)
            => GetGradientColor(gsc, (value - min) / (max - min));

        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            VisualFocusOnMouseOver(this, e);
        }

        private void VisualFocusOnMouseOver(object sender, MouseEventArgs e) {
            var pt = e.GetPosition(this);

            VisualTreeHelper.HitTest(this,
                new HitTestFilterCallback(VisualHitTestFilter),
                new HitTestResultCallback(VisualFocusHitTest),
                new PointHitTestParameters(pt)
                );
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) {
            base.OnMouseLeftButtonDown(e);
            VisualSelectOnClick(this, e);
        }

        private void VisualSelectOnClick(object sender, MouseButtonEventArgs e) {
            if (e.ClickCount == 1) {
                var pt = e.GetPosition(this);

                VisualTreeHelper.HitTest(this,
                    new HitTestFilterCallback(VisualHitTestFilter),
                    new HitTestResultCallback(VisualSelectHitTest),
                    new PointHitTestParameters(pt)
                    );
            }
        }

        private HitTestFilterBehavior VisualHitTestFilter(DependencyObject d) {
            if (d is AnnotatedDrawingVisual)
                return HitTestFilterBehavior.Continue;
            return HitTestFilterBehavior.ContinueSkipSelf;
        }

        private HitTestResultBehavior VisualFocusHitTest(HitTestResult result) {
            var dv = (AnnotatedDrawingVisual)result.VisualHit;
            var focussed = dv.Annotation;
            if (focussed != FocusedItem) {
                FocusedItem = focussed;
                FocusedPoint = dv.Center;
            }
            return HitTestResultBehavior.Stop;
        }

        private HitTestResultBehavior VisualSelectHitTest(HitTestResult result) {
            SelectedItem = ((AnnotatedDrawingVisual)result.VisualHit).Annotation;
            return HitTestResultBehavior.Stop;
        }
    }
}
