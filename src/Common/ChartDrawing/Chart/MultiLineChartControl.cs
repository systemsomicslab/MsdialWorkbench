using CompMs.Common.DataStructure;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using CompMs.Graphics.Helper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace CompMs.Graphics.Chart
{
    public sealed class MultiLineChartControl : ChartBaseControl
    {
        public MultiLineChartControl() : base() {
            ClipToBounds = true;
        }

        public static readonly DependencyProperty ItemsSourcesProperty =
            DependencyProperty.Register(
                nameof(ItemsSources), typeof(IEnumerable), typeof(MultiLineChartControl),
                new FrameworkPropertyMetadata(
                    default(IEnumerable),
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnItemsSourcesChanged));

        public IEnumerable ItemsSources {
            get => (IEnumerable)GetValue(ItemsSourcesProperty);
            set => SetValue(ItemsSourcesProperty, value);
        }

        private static void OnItemsSourcesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = (MultiLineChartControl)d;
            chart.OnItemsSourcesChanged((IEnumerable)e.OldValue, (IEnumerable)e.NewValue);
        }

        private ICollectionView _seriesesCV;
        private IDisposable _unsubscriber;
        private void OnItemsSourcesChanged(IEnumerable oldValue, IEnumerable newValue) {
            if (!(_seriesesCV is null)) {
                _seriesesCV.CollectionChanged -= OnItemsChanged;
                _unsubscriber?.Dispose();
                _unsubscriber = null;
            }
            _seriesesCV = CollectionViewSource.GetDefaultView(newValue);
            if (!(_seriesesCV is null || _itemsGetter is null)) {
                _seriesesCV.CollectionChanged += OnItemsChanged;
                _unsubscriber = NotifyCollectionChangedHelper.Manage(_seriesesCV, _itemsGetter, OnItemsChanged);
            }
            SetDatas();
        }

        private void OnItemsChanged(object sender, NotifyCollectionChangedEventArgs e) {
            SetDatas();
            InvalidateVisual();
        }

        public static readonly DependencyProperty CollectionDataTypeProperty =
            DependencyProperty.Register(
                nameof(CollectionDataType), typeof(Type), typeof(MultiLineChartControl),
                new FrameworkPropertyMetadata(null, OnCollectionDataTypeChanged));

        public Type CollectionDataType {
            get => (Type)GetValue(CollectionDataTypeProperty);
            set => SetValue(CollectionDataTypeProperty, value);
        }

        private static void OnCollectionDataTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = (MultiLineChartControl)d;
            chart.OnCollectionDataTypeChanged((Type)e.OldValue, (Type)e.NewValue);
        }

        private void OnCollectionDataTypeChanged(Type oldValue, Type newValue) {
            if (oldValue != newValue) {
                _unsubscriber?.Dispose();
                _unsubscriber = null;
                SetItemsGetter(newValue, ItemsProperty);
                if (!(_seriesesCV is null || _itemsGetter is null)) {
                    _unsubscriber = NotifyCollectionChangedHelper.Manage(_seriesesCV, _itemsGetter, OnItemsChanged);
                }
                SetDatas();
            }
        }

        public static readonly DependencyProperty ItemsPropertyProperty =
            DependencyProperty.Register(
                nameof(ItemsProperty), typeof(string), typeof(MultiLineChartControl),
                new FrameworkPropertyMetadata(null, OnItemsPropertyChanged));

        public string ItemsProperty {
            get => (string)GetValue(ItemsPropertyProperty);
            set => SetValue(ItemsPropertyProperty, value);
        }

        private static void OnItemsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = (MultiLineChartControl)d;
            chart.OnItemsPropertyChanged((string)e.OldValue, (string)e.NewValue);
        }

        private void OnItemsPropertyChanged(string oldValue, string newValue) {
            if (oldValue != newValue) {
                _unsubscriber?.Dispose();
                _unsubscriber = null;
                SetItemsGetter(CollectionDataType, newValue);
                if (!(_seriesesCV is null || _itemsGetter is null)) {
                    _unsubscriber = NotifyCollectionChangedHelper.Manage(_seriesesCV, _itemsGetter, OnItemsChanged);
                }
                SetDatas();
            }
        }

        private Func<object, object> _itemsGetter;
        private void SetItemsGetter(Type type, string property) {
            if (type is null || !ExpressionHelper.ValidatePropertyString(type, property)) {
                _itemsGetter = null;
                return;
            }
            var itemsLambda = ExpressionHelper.GetPropertyGetterExpression(type, property);
            _itemsGetter = itemsLambda.Compile();
        }

        public static readonly DependencyProperty ItemsDataTypeProperty =
            DependencyProperty.Register(
                nameof(ItemsDataType), typeof(Type), typeof(MultiLineChartControl),
                new FrameworkPropertyMetadata(null, OnItemsDataTypeChanged));

        public Type ItemsDataType {
            get => (Type)GetValue(ItemsDataTypeProperty);
            set => SetValue(ItemsPropertyProperty, value);
        }

        private static void OnItemsDataTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = (MultiLineChartControl)d;
            chart.OnItemsDataTypeChagned((Type)e.OldValue, (Type)e.NewValue);
        }

        private void OnItemsDataTypeChagned(Type oldValue, Type newValue) {
            if (oldValue != newValue) {
                SetHorizontalGetter(newValue, HorizontalProperty);
                SetVerticalGetter(newValue, VerticalProperty);
                SetDatas();
            }
        }

        private Lazy<List<Series>> _lazySerieses;
        private Lazy<KdTree<(Data, Series)>> _lazyKdTree;
        private void SetDatas() {
            _lazySerieses = new Lazy<List<Series>>(() =>
            {
                var xGetter = _xGetter;
                var yGetter = _yGetter;
                var hAxis = HorizontalAxis;
                var vAxis = VerticalAxis;
                if (xGetter is null || yGetter is null || _itemsGetter is null || _seriesesCV is null || hAxis is null || vAxis is null) {
                    return new List<Series>(0);
                }

                var results = new List<Series>();
                foreach (var series in _seriesesCV.OfType<object>()) {
                    var rawItems = _itemsGetter(series);
                    if (rawItems is IEnumerable<object> items) {
                        results.Add(new Series() { raw = series, data = items.Select(item => new Data { x = xGetter(item, hAxis), y = yGetter(item, vAxis), raw = item, }).ToArray() });
                    }
                }
                return results;
            });

            _lazyKdTree = new Lazy<KdTree<(Data, Series)>>(
                () => KdTree.Build(
                    _lazySerieses.Value.SelectMany(series => series.data, (series, data) => (data, series)),
                    new ChartDistanceCalculator(this),
                    pair => pair.data.x.Value,
                    pair => pair.data.y.Value));
        }

        public static readonly DependencyProperty HorizontalPropertyProperty =
            DependencyProperty.Register(
                nameof(HorizontalProperty), typeof(string), typeof(MultiLineChartControl),
                new FrameworkPropertyMetadata(
                    default(string),
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnHorizontalPropertyChanged));

        public string HorizontalProperty {
            get => (string)GetValue(HorizontalPropertyProperty);
            set => SetValue(HorizontalPropertyProperty, value);
        }

        private static void OnHorizontalPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = (MultiLineChartControl)d;
            chart.OnHorizontalPropertyChanged((string)e.OldValue, (string)e.NewValue);
        }

        private void OnHorizontalPropertyChanged(string oldValue, string newValue) {
            if (oldValue != newValue) {
                SetHorizontalGetter(ItemsDataType, newValue);
                SetDatas();
            }
        }

        private Func<object, IAxisManager, AxisValue> _xGetter;
        private void SetHorizontalGetter(Type dataType, string horizontalProperty) {
            if (dataType is null || !ExpressionHelper.ValidatePropertyString(dataType, horizontalProperty)) {
                _xGetter = null;
                return;
            }
            var xLambda = ExpressionHelper.GetConvertToAxisValueExpression(dataType, horizontalProperty);
            _xGetter = xLambda.Compile();
        }

        protected override void OnHorizontalAxisChanged(IAxisManager oldValue, IAxisManager newValue) {
            base.OnHorizontalAxisChanged(oldValue, newValue);
            SetDatas();
        }

        public static readonly DependencyProperty VerticalPropertyProperty =
            DependencyProperty.Register(
                nameof(VerticalProperty), typeof(string), typeof(MultiLineChartControl),
                new FrameworkPropertyMetadata(
                    default(string),
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnVerticalPropertyChanged));

        public string VerticalProperty {
            get => (string)GetValue(VerticalPropertyProperty);
            set => SetValue(VerticalPropertyProperty, value);
        }

        private static void OnVerticalPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = (MultiLineChartControl)d;
            chart.OnVerticalPropertyChanged((string)e.OldValue, (string)e.NewValue);
        }

        private void OnVerticalPropertyChanged(string oldValue, string newValue) {
            if (oldValue != newValue) {
                SetVerticalGetter(ItemsDataType, newValue);
                SetDatas();
            }
        }

        private Func<object, IAxisManager, AxisValue> _yGetter;
        private void SetVerticalGetter(Type dataType, string verticalProperty) {
            if (dataType is null || !ExpressionHelper.ValidatePropertyString(dataType, verticalProperty)) {
                _yGetter = null;
                return;
            }
            var yLambda = ExpressionHelper.GetConvertToAxisValueExpression(dataType, verticalProperty);
            _yGetter = yLambda.Compile();
        }

        protected override void OnVerticalAxisChanged(IAxisManager oldValue, IAxisManager newValue) {
            base.OnVerticalAxisChanged(oldValue, newValue);
            SetDatas();
        }

        public static readonly DependencyProperty LineBrushProperty =
            DependencyProperty.Register(
                nameof(LineBrush), typeof(IBrushMapper), typeof(MultiLineChartControl),
                new FrameworkPropertyMetadata(null, OnLineBrushChanged));

        public IBrushMapper LineBrush {
            get => (IBrushMapper)GetValue(LineBrushProperty);
            set => SetValue(LineBrushProperty, value);
        }

        private static void OnLineBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = (MultiLineChartControl)d;
            chart.OnLineBrushChanged((IBrushMapper)e.OldValue, (IBrushMapper)e.NewValue);
        }

        private void OnLineBrushChanged(IBrushMapper oldValue, IBrushMapper newValue) {
            if (newValue is null) {
                Selector.Reset();
            }
            else {
                Selector.Update(newValue, LineThickness);
            }
        }

        public static readonly DependencyProperty LineThicknessProperty =
            DependencyProperty.Register(
                nameof(LineThickness), typeof(double), typeof(MultiLineChartControl),
                new FrameworkPropertyMetadata(1d, OnLineThicknessChanged));

        public double LineThickness {
            get => (double)GetValue(LineThicknessProperty);
            set=> SetValue(LineThicknessProperty, value);
        }

        private static void OnLineThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = (MultiLineChartControl)d;
            chart.OnLineThicknessChanged((double)e.OldValue, (double)e.NewValue);
        }

        private void OnLineThicknessChanged(double oldValue, double newValue) {
            if (!(LineBrush is null)) {
                Selector.Update(LineBrush, newValue);
            }
        }

        private PenSelector Selector {
            get {
                if (_selector is null) {
                    _selector = new PenSelector();
                    if (!(LineBrush is null)) {
                        _selector.Update(LineBrush, LineThickness);
                    }
                }
                return _selector;
            }
        }
        private PenSelector _selector;

        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);
            if (_lazySerieses is null || HorizontalAxis is null || VerticalAxis is null) {
                return;
            }

            var area = new Rect(RenderSize);
            var segmentPoints = new List<Point>();
            foreach (var series in _lazySerieses.Value) {
                var points = ValuesToRenderPositions(series, ActualWidth, ActualHeight, HorizontalAxis, VerticalAxis, FlippedX, FlippedY);
                var areaGeometry = new PathGeometry();
                if (points.Count != 0) {
                    var i = 0;
                    while (i < points.Count) {
                        segmentPoints.Clear();
                        while (i < points.Count && !area.Contains(points[i])) {
                            i++;
                        }

                        if (0 <= i - 1 && i - 1 < points.Count) {
                            segmentPoints.Add(points[i - 1]);
                        }

                        while (i < points.Count && area.Contains(points[i])) {
                            segmentPoints.Add(points[i++]);
                        }

                        if (i < points.Count) {
                            segmentPoints.Add(points[i]);
                        }
                        if (segmentPoints.Count == 0) {
                            break;
                        }
                        var p = segmentPoints.FirstOrDefault();
                        var path = new PathFigure
                        {
                            IsClosed = false,
                            StartPoint = p
                        };
                        path.Segments.Add(new PolyLineSegment(segmentPoints.Skip(1), isStroked: true));
                        path.Freeze();
                        areaGeometry.Figures.Add(path);
                    }
                }
                areaGeometry.Freeze();
                drawingContext.DrawGeometry(null, Selector.GetPen(series.raw), areaGeometry);
            }
        }

        public static readonly DependencyProperty SelectedSeriesProperty =
            DependencyProperty.Register(
                nameof(SelectedSeries), typeof(object), typeof(MultiLineChartControl),
                new PropertyMetadata(null, OnSelectedSeriesChanged));

        public object SelectedSeries {
            get => GetValue(SelectedSeriesProperty);
            set => SetValue(SelectedSeriesProperty, value);
        }

        private static void OnSelectedSeriesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = (MultiLineChartControl)d;
            chart.OnSelectedSeriesChanged(e.OldValue, e.NewValue);
        } 

        private void OnSelectedSeriesChanged(object oldValue, object newValue) {

        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                nameof(SelectedItem), typeof(object), typeof(MultiLineChartControl),
                new PropertyMetadata(null, OnSelectedItemChanged));

        public object SelectedItem {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = (MultiLineChartControl)d;
            chart.OnSelectedItemChanged(e.OldValue, e.NewValue);
        }

        private void OnSelectedItemChanged(object oldValue, object newValue) {

        }

        public static readonly DependencyProperty SelectedPointProperty =
            DependencyProperty.Register(
                nameof(SelectedPoint), typeof(Point?), typeof(MultiLineChartControl),
                new PropertyMetadata(null));

        public Point? SelectedPoint {
            get => (Point?)GetValue(SelectedPointProperty);
            set => SetValue(SelectedPointProperty, value);
        }

        public static readonly DependencyProperty FocusedSeriesProperty =
            DependencyProperty.Register(
                nameof(FocusedSeries), typeof(object), typeof(MultiLineChartControl),
                new PropertyMetadata(null, OnFocusedSeriesChanged));

        public object FocusedSeries {
            get => GetValue(FocusedSeriesProperty);
            set => SetValue(FocusedSeriesProperty, value);
        }

        private static void OnFocusedSeriesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = (MultiLineChartControl)d;
            chart.OnFocusedSeriesChanged(e.OldValue, e.NewValue);
        }

        private void OnFocusedSeriesChanged(object oldValue, object newValue) {

        }

        public static readonly DependencyProperty FocusedItemProperty =
            DependencyProperty.Register(
                nameof(FocusedItem), typeof(object), typeof(MultiLineChartControl),
                new PropertyMetadata(null, OnFocusedItemChanged));

        public object FocusedItem {
            get => GetValue(FocusedItemProperty);
            set => SetValue(FocusedItemProperty, value);
        }

        private static void OnFocusedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = (MultiLineChartControl)d;
            chart.OnFocusedItemChanged(e.OldValue, e.NewValue);
        }

        private  void OnFocusedItemChanged(object oldValue, object newValue) {

        }

        public static readonly DependencyProperty FocusedPointProperty =
            DependencyProperty.Register(
                nameof(FocusedPoint), typeof(Point?), typeof(MultiLineChartControl),
                new PropertyMetadata(null));

        public Point? FocusedPoint {
            get => (Point?)GetValue(FocusedPointProperty);
            set => SetValue(FocusedPointProperty, value);
        }

        private static readonly double ITEM_SELECT_MAXIMUM_DISTANCE = 3d;
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) {
            base.OnMouseLeftButtonDown(e);
            if (e.ClickCount == 1) {
                if (_lazyKdTree != null && HorizontalAxis is IAxisManager haxis && VerticalAxis is IAxisManager vaxis) {
                    var pt = e.GetPosition(this);
                    var flippedX = FlippedX;
                    var flippedY = FlippedY;
                    var width = ActualWidth;
                    var height = ActualHeight;

                    var spot = _lazyKdTree.Value.NearestNeighbor(new[]
                        {
                            haxis.TranslateFromRenderPoint(pt.X, flippedX, width).Value,
                            vaxis.TranslateFromRenderPoint(pt.Y, flippedY, height).Value,
                        });

                    if (Math.Pow(haxis.TranslateToRenderPoint(spot.Item1.x, flippedX, width) - pt.X, 2)
                        + Math.Pow(vaxis.TranslateToRenderPoint(spot.Item1.y, flippedY, height) - pt.Y, 2)
                        <= Math.Pow(ITEM_SELECT_MAXIMUM_DISTANCE, 2)) {
                        if (SelectedItem != spot.Item1.raw) {
                            SelectedSeries = spot.Item2.raw;
                            SelectedItem = spot.Item1.raw;
                            SelectedPoint = pt;
                        }
                    }
                }
            }
        }

        private void MouseFocusItem(MouseEventArgs e) {
            if (_lazyKdTree != null && HorizontalAxis is IAxisManager haxis && VerticalAxis is IAxisManager vaxis) {
                var pt = e.GetPosition(this);
                var flippedX = FlippedX;
                var flippedY = FlippedY;
                var width = ActualWidth;
                var height = ActualHeight;

                var spot = _lazyKdTree.Value.NearestNeighbor(new[]
                    {
                        haxis.TranslateFromRenderPoint(pt.X, flippedX, width).Value,
                        vaxis.TranslateFromRenderPoint(pt.Y, flippedY, height).Value,
                    });

                if (FocusedItem != spot.Item1.raw) {
                    FocusedSeries = spot.Item2.raw;
                    FocusedItem = spot.Item1.raw;
                    FocusedPoint = pt;
                }
            }
        }

        protected override void OnMouseEnter(MouseEventArgs e) {
            base.OnMouseEnter(e);
            MouseFocusItem(e);
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            MouseFocusItem(e);
        }

        protected override void OnMouseLeave(MouseEventArgs e) {
            base.OnMouseLeave(e);
            FocusedSeries = null;
            FocusedItem = null;
            FocusedPoint = null;
        }

        private static List<Point> ValuesToRenderPositions(Series s, double actualWidth, double actualHeight, IAxisManager horizontalAxis, IAxisManager verticalAxis, bool flippedX, bool flippedY) {
            if (horizontalAxis is null || verticalAxis is null) {
                return new List<Point>(0);
            }
            var ds = s.data;
            var xs = horizontalAxis.TranslateToRenderPoints(ds.Select(d => d.x), flippedX, actualWidth);
            var ys = verticalAxis.TranslateToRenderPoints(ds.Select(d => d.y), flippedY, actualHeight);
            return Enumerable.Zip(xs, ys, (x, y) => new Point(x, y)).ToList();
        }

        private sealed class Series {
            public Data[] data;
            public object raw;
        }

        private sealed class Data
        {
            public AxisValue x, y;
            public object raw;
        }
    }
}
