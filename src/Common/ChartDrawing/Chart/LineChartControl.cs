using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Helper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace CompMs.Graphics.Chart
{
    public sealed class LineChartControl : ChartBaseControl
    {
        static LineChartControl() {
            DEFAULT_PEN.Freeze();
        }

        public LineChartControl() : base() {
            ClipToBounds = true;
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource), typeof(IEnumerable), typeof(LineChartControl),
                new FrameworkPropertyMetadata(
                    default(IEnumerable),
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnItemsSourceChanged));

        public IEnumerable ItemsSource {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = (LineChartControl)d;
            chart.OnItemsSourceChanged((IEnumerable)e.OldValue, (IEnumerable)e.NewValue);
        }

        private ICollectionView _cv;
        private Type _dataType;

        private void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue) {
            if (_cv is INotifyCollectionChanged oldCV) {
                oldCV.CollectionChanged -= OnItemsSourceCollectionChanged;
            }
            _cv = CollectionViewSource.GetDefaultView(newValue);
            if (_cv is INotifyCollectionChanged newCV) {
                newCV.CollectionChanged += OnItemsSourceCollectionChanged;
            }

            if (newValue?.GetEnumerator() is IEnumerator enumerator && enumerator.MoveNext()) {
                _dataType = enumerator.Current.GetType();
            }
            else {
                _dataType = null;
            }
            SetHorizontalLambdaExpression(_dataType, HorizontalPropertyName);
            SetVerticalLambdaExpression(_dataType, VerticalPropertyName);
            SetDatas();
        }

        private void OnItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            SetDatas();
        }

        private Lazy<Data[]> _lazyDatas;
        private void SetDatas() {
            if (_xLambda is null || _yLambda is null || _cv is null || HorizontalAxis is null || VerticalAxis is null) {
                _lazyDatas = null;
                return;
            }
            var xFunc = _xLambda.Compile();
            var yFunc = _yLambda.Compile();
            var hAxis = HorizontalAxis;
            var vAxis = VerticalAxis;
            _lazyDatas = new Lazy<Data[]>(() => _cv.OfType<object>().Select(o => new Data { x = xFunc(o, hAxis), y = yFunc(o, vAxis), }).ToArray());
        }

        public static readonly DependencyProperty HorizontalPropertyNameProperty =
            DependencyProperty.Register(
                nameof(HorizontalPropertyName), typeof(string), typeof(LineChartControl),
                new FrameworkPropertyMetadata(
                    default(string),
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnHorizontalPropertyNameChanged));

        public string HorizontalPropertyName {
            get => (string)GetValue(HorizontalPropertyNameProperty);
            set => SetValue(HorizontalPropertyNameProperty, value);
        }

        private static void OnHorizontalPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = (LineChartControl)d;
            chart.OnHorizontalPropertyNameChanged((string)e.OldValue, (string)e.NewValue);
        }

        private void OnHorizontalPropertyNameChanged(string oldValue, string newValue) {
            SetHorizontalLambdaExpression(_dataType, newValue);
            SetDatas();
        }

        private System.Linq.Expressions.Expression<Func<object, IAxisManager, AxisValue>> _xLambda;
        private void SetHorizontalLambdaExpression(Type dataType, string horizontalProperty) {
            if (dataType is null || !ExpressionHelper.ValidatePropertyString(dataType, horizontalProperty)) {
                _xLambda = null;
                return;
            }
            _xLambda = ExpressionHelper.GetConvertToAxisValueExpression(dataType, horizontalProperty);
        }

        protected override void OnHorizontalAxisChanged(IAxisManager oldValue, IAxisManager newValue) {
            base.OnHorizontalAxisChanged(oldValue, newValue);
            SetDatas();
        }

        public static readonly DependencyProperty VerticalPropertyNameProperty =
            DependencyProperty.Register(
                nameof(VerticalPropertyName), typeof(string), typeof(LineChartControl),
                new FrameworkPropertyMetadata(
                    default(string),
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnVerticalPropertyNameChanged));

        public string VerticalPropertyName {
            get => (string)GetValue(VerticalPropertyNameProperty);
            set => SetValue(VerticalPropertyNameProperty, value);
        }

        private static void OnVerticalPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = (LineChartControl)d;
            chart.OnVerticalPropertyNameChanged((string)e.OldValue, (string)e.NewValue);
        }

        private void OnVerticalPropertyNameChanged(string oldValue, string newValue) {
            SetVerticalLambdaExpression(_dataType, newValue);
            SetDatas();
        }

        private System.Linq.Expressions.Expression<Func<object, IAxisManager, AxisValue>> _yLambda;
        private void SetVerticalLambdaExpression(Type dataType, string verticalProperty) {
            if (dataType is null || !ExpressionHelper.ValidatePropertyString(dataType, verticalProperty)) {
                _yLambda = null;
                return;
            }
            _yLambda = ExpressionHelper.GetConvertToAxisValueExpression(dataType, verticalProperty);
        }

        protected override void OnVerticalAxisChanged(IAxisManager oldValue, IAxisManager newValue) {
            base.OnVerticalAxisChanged(oldValue, newValue);
            SetDatas();
        }

        private static readonly Pen DEFAULT_PEN = new Pen(Brushes.Black, 1d) { LineJoin = PenLineJoin.Bevel, };

        public static readonly DependencyProperty LinePenProperty =
            DependencyProperty.Register(
                nameof(LinePen), typeof(Pen), typeof(LineChartControl),
                new PropertyMetadata(DEFAULT_PEN));

        public Pen LinePen {
            get => (Pen)GetValue(LinePenProperty);
            set => SetValue(LinePenProperty, value);
        }

        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);
            if (LinePen is null || _lazyDatas is null) {
                return;
            }

            var points = ValuesToRenderPositions(_lazyDatas.Value, ActualWidth, ActualHeight, HorizontalAxis, VerticalAxis, FlippedX, FlippedY);

            if (points.Length != 0) {
                var areaGeometry = new PathGeometry();
                var area = new Rect(RenderSize);
                var i = 0;
                while (i < points.Length) {
                    var path = new PathFigure() { IsClosed = false };
                    while (i < points.Length && !area.Contains(points[i])) {
                        i++;
                    }

                    if (0 <= i - 1 && i - 1 < points.Length) {
                        path.Segments.Add(new LineSegment(points[i - 1], isStroked: true));
                    }

                    while (i < points.Length && area.Contains(points[i])) {
                        path.Segments.Add(new LineSegment(points[i++], isStroked: true));
                    }

                    if (i < points.Length) {
                        path.Segments.Add(new LineSegment(points[i], isStroked: true));
                    }
                    if (path.Segments.Count == 0) {
                        break;
                    }
                    var p = (path.Segments.First() as LineSegment).Point;
                    path.StartPoint = p;
                    path.Freeze();
                    areaGeometry.Figures.Add(path);
                }
                areaGeometry.Freeze();
                drawingContext.DrawGeometry(null, LinePen, areaGeometry);
            }
        }

        private static Point[] ValuesToRenderPositions(IReadOnlyList<Data> ds, double actualWidth, double actualHeight, IAxisManager horizontalAxis, IAxisManager verticalAxis, bool flippedX, bool flippedY) {
            if (horizontalAxis is null || verticalAxis is null) {
                return new Point[0];
            }
            var xs = ds.Select(d => horizontalAxis.TranslateToRenderPoint(d.x, flippedX, actualWidth));
            var ys = ds.Select(d => verticalAxis.TranslateToRenderPoint(d.y, flippedY, actualHeight));
            return Enumerable.Zip(xs, ys, (x, y) => new Point(x, y)).ToArray();
        }

        private sealed class Data
        {
            public AxisValue x, y;
        }
    }
}
