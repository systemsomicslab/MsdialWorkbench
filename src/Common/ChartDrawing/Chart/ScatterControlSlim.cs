using CompMs.Common.DataStructure;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Data;
using CompMs.Graphics.Design;
using CompMs.Graphics.Helper;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace CompMs.Graphics.Chart;

public class ScatterControlSlim : ChartBaseControl
{
    static ScatterControlSlim()
    {
        var clipToBoundsMetadata = ClipToBoundsProperty.GetMetadata(typeof(ChartBaseControl));
        var newClipToBoundsMetadata = new FrameworkPropertyMetadata(
            BooleanBoxes.TrueBox,
            clipToBoundsMetadata.PropertyChangedCallback,
            clipToBoundsMetadata.CoerceValueCallback);
        ClipToBoundsProperty.OverrideMetadata(typeof(ScatterControlSlim), newClipToBoundsMetadata);
    }

    public ScatterControlSlim()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public IEnumerable ItemsSource {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(ScatterControlSlim),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsRender,
                OnItemsSourceChanged));

    private ICollectionView _collectionView;

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        var scatter = (ScatterControlSlim)d;
        scatter.OnItemsSourceChanged((IEnumerable)e.OldValue, (IEnumerable)e.NewValue);
    }

    protected virtual void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue) {
        if (_collectionView != null) {
            _collectionView.CollectionChanged -= OnItemsSourceCollectionChanged;
            _collectionView.CurrentChanged -= OnItemsSourceCurrentChanged;
        }
        _collectionView = newValue as ICollectionView ?? CollectionViewSource.GetDefaultView(newValue);
        if (_collectionView != null) {
            _collectionView.CollectionChanged += OnItemsSourceCollectionChanged;
            _collectionView.CurrentChanged += OnItemsSourceCurrentChanged;
        }

        CoerceTree();
    }

    private void OnItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
        CoerceTree();
        InvalidateVisual();
    }

    private void OnItemsSourceCurrentChanged(object sender, EventArgs e) {
        SelectedItem = _collectionView?.CurrentItem;
    }

    public Type DataType {
        get => (Type)GetValue(DataTypeProperty);
        set => SetValue(DataTypeProperty, value);
    }

    public static readonly DependencyProperty DataTypeProperty =
        DependencyProperty.Register(
            nameof(DataType),
            typeof(Type),
            typeof(ScatterControlSlim),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsRender,
                OnDataTypeChanged));

    private static void OnDataTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        var scatter = (ScatterControlSlim)d;
        scatter.OnDataTypeChanged((Type)e.OldValue, (Type)e.NewValue);
    }

    protected virtual void OnDataTypeChanged(Type oldValue, Type newValue) {
        CoerceXProperty(newValue, HorizontalProperty);
        CoerceYProperty(newValue, VerticalProperty);
        CoerceTree();
        CoerceHueProperty(newValue, HueProperty);
    }

    public string HorizontalProperty {
        get => (string)GetValue(HorizontalPropertyProperty);
        set => SetValue(HorizontalPropertyProperty, value);
    }

    public static readonly DependencyProperty HorizontalPropertyProperty =
        DependencyProperty.Register(
            nameof(HorizontalProperty),
            typeof(string),
            typeof(ScatterControlSlim),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsRender,
                OnHorizontalPropertyChanged));

    private static void OnHorizontalPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        var scatter = (ScatterControlSlim)d;
        scatter.OnHorizontalPropertyChanged((string)e.OldValue, (string)e.NewValue);
    }

    protected virtual void OnHorizontalPropertyChanged(string oldValue, string newValue) {
        CoerceXProperty(DataType, newValue);
        CoerceTree();
    }

    public string VerticalProperty {
        get => (string)GetValue(VerticalPropertyProperty);
        set => SetValue(VerticalPropertyProperty, value);
    }

    public static readonly DependencyProperty VerticalPropertyProperty =
        DependencyProperty.Register(
            nameof(VerticalProperty),
            typeof(string),
            typeof(ScatterControlSlim),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.AffectsRender,
                OnVerticalPropertyChanged));

    private static void OnVerticalPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        var scatter = (ScatterControlSlim)d;
        scatter.OnVerticalPropertyChanged((string)e.OldValue, (string)e.NewValue);
    }

    protected virtual void OnVerticalPropertyChanged(string oldValue, string newValue) {
        CoerceYProperty(DataType, newValue);
        CoerceTree();
    }

    protected override void OnHorizontalAxisChanged(IAxisManager oldValue, IAxisManager newValue) {
        base.OnHorizontalAxisChanged(oldValue, newValue);
        CoerceTree();
    }

    protected override void OnVerticalAxisChanged(IAxisManager oldValue, IAxisManager newValue) {
        base.OnVerticalAxisChanged(oldValue, newValue);
        CoerceTree();
    }

    private Lazy<NotifiableDataPoint[]> _items;
    private Lazy<KdTree<NotifiableDataPoint>> _tree;

    private void CoerceTree() {
        if (_items != null && _items.IsValueCreated) {
            foreach (var item in _items.Value) {
                item.Dispose();
            }
            _items = null;
        }

        if (_collectionView is not IEnumerable source
            || DataType is null
            || _xAccessor is null
            || _yAccessor is null
            || HorizontalAxis is not IAxisManager haxis
            || VerticalAxis is not IAxisManager vaxis) {
            _tree = null;
            return;
        }
        
        var collection = source.Cast<object>().Select(item => new NotifiableDataPoint(item, haxis, vaxis, _xAccessor, _yAccessor)); // TODO: Need to remove boxing here.
        _items = new Lazy<NotifiableDataPoint[]>(() => {
            var items = collection.ToArray();
            if (IsLoaded) {
                foreach (var item in items) {
                    item.PropertyChanged += UpdateVisual;
                }
            }
            return items;
        });
        ResetTree();
    }

    private void ResetTree() {
        _tree = new Lazy<KdTree<NotifiableDataPoint>>(() => KdTree.Build(_items.Value, new ScatterDistanceCalculator(this), v => v.X.Value, v => v.Y.Value));
    }

    private TypedPropertiesAccessor? _xAccessor, _yAccessor;

    private void CoerceXProperty(Type type, string hprop) {
        if (type is null
            || string.IsNullOrEmpty(hprop)
            || !ExpressionHelper.ValidatePropertyString(type, hprop)) {
            _xAccessor = null;
            return;
        }
        _xAccessor = new TypedPropertiesAccessor(hprop, type);
    }

    private void CoerceYProperty(Type type, string vprop) {
        if (type is null
            || string.IsNullOrEmpty(vprop)
            || !ExpressionHelper.ValidatePropertyString(type, vprop)) {
            _yAccessor = null;
            return;
        }
        _yAccessor = new TypedPropertiesAccessor(vprop, type);
    }

    public static readonly DependencyProperty HuePropertyProperty =
        DependencyProperty.Register(
            nameof(HueProperty), typeof(string), typeof(ScatterControlSlim),
            new PropertyMetadata(null, OnHuePropertyChanged));

    public string HueProperty {
        get => (string)GetValue(HuePropertyProperty);
        set => SetValue(HuePropertyProperty, value);
    }

    private static void OnHuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        var c = (ScatterControlSlim)d;
        c.OnHuePropertyChanged((string)e.OldValue, (string)e.NewValue);
    }

    protected virtual void OnHuePropertyChanged(string oldValue, string newValue) {
        CoerceHueProperty(DataType, newValue);
    }

    private TypedPropertiesAccessor? _hueAccessor;

    private void CoerceHueProperty(Type type, string hueProperty) {
        if (type is null
            || string.IsNullOrEmpty(hueProperty)
            || !ExpressionHelper.ValidatePropertyString(type, hueProperty)) {
            _hueAccessor = null;
            return;
        }
        _hueAccessor = new TypedPropertiesAccessor(hueProperty, type);
    }

    public double Radius {
        get => (double)GetValue(RadiusProperty);
        set => SetValue(RadiusProperty, value);
    }

    public static readonly DependencyProperty RadiusProperty =
        DependencyProperty.Register(
            nameof(Radius),
            typeof(double),
            typeof(ScatterControlSlim),
            new FrameworkPropertyMetadata(
                3d,
                FrameworkPropertyMetadataOptions.AffectsRender));

    public IBrushMapper PointBrush {
        get => (IBrushMapper)GetValue(PointBrushProperty);
        set => SetValue(PointBrushProperty, value);
    }

    public static readonly DependencyProperty PointBrushProperty =
        DependencyProperty.Register(
            nameof(PointBrush),
            typeof(IBrushMapper),
            typeof(ScatterControlSlim),
            new FrameworkPropertyMetadata(
                new ConstantBrushMapper<object>(Brushes.Black)));

    public object SelectedItem {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(
            nameof(SelectedItem),
            typeof(object),
            typeof(ScatterControlSlim),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedItemChanged));

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        var scatter = (ScatterControlSlim)d;
        scatter.OnSelectedItemChanged(e.OldValue, e.NewValue);
    }

    protected virtual void OnSelectedItemChanged(object oldValue, object newValue) {
        _collectionView?.MoveCurrentTo(newValue);
        UpdateSelectedPoint();
    }

    public Point? SelectedPoint {
        get => (Point?)GetValue(SelectedPointProperty);
        set => SetValue(SelectedPointProperty, value);
    }

    public static readonly DependencyProperty SelectedPointProperty =
        DependencyProperty.Register(
            nameof(SelectedPoint),
            typeof(Point?),
            typeof(ScatterControlSlim),
            new PropertyMetadata(null));

    private void UpdateSelectedPoint() {
        if (HorizontalAxis is IAxisManager haxis
            && VerticalAxis is IAxisManager vaxis
            && _xAccessor?.GetAxisValue is Func<object, IAxisManager, AxisValue> xlambda
            && _yAccessor?.GetAxisValue is Func<object, IAxisManager, AxisValue> ylambda
            && SelectedItem != null) {

            var item = SelectedItem;
            var pt = new Point(
                haxis.TranslateToRenderPoint(xlambda(item, haxis), FlippedX, ActualWidth),
                vaxis.TranslateToRenderPoint(ylambda(item, vaxis), FlippedY, ActualHeight));
            if (pt != SelectedPoint) {
                SelectedPoint = pt;
            }
        }
    }

    public object FocusedItem {
        get => GetValue(FocusedItemProperty);
        set => SetValue(FocusedItemProperty, value);
    }

    public static readonly DependencyProperty FocusedItemProperty =
        DependencyProperty.Register(
            nameof(FocusedItem),
            typeof(object),
            typeof(ScatterControlSlim),
            new FrameworkPropertyMetadata(
                null,
                OnFocusedItemChanged));

    private static void OnFocusedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        var scatter = (ScatterControlSlim)d;
        scatter.OnFocusedItemChanged(e.OldValue, e.NewValue);
    }

    protected virtual void OnFocusedItemChanged(object oldValue, object newValue) {
        UpdateFocusedPoint();
    }

    public Point? FocusedPoint {
        get => (Point?)GetValue(FocusedPointProperty);
        set => SetValue(FocusedPointProperty, value);
    }

    public static readonly DependencyProperty FocusedPointProperty =
        DependencyProperty.Register(
            nameof(FocusedPoint),
            typeof(Point?),
            typeof(ScatterControlSlim),
            new PropertyMetadata(null));

    private void UpdateFocusedPoint() {
        if (HorizontalAxis is IAxisManager haxis
            && VerticalAxis is IAxisManager vaxis
            && _xAccessor?.GetAxisValue is Func<object, IAxisManager, AxisValue> xlambda
            && _yAccessor?.GetAxisValue is Func<object, IAxisManager, AxisValue> ylambda
            && FocusedItem is object item) {

            var pt = new Point(
                haxis.TranslateToRenderPoint(xlambda(item, haxis), FlippedX, ActualWidth),
                vaxis.TranslateToRenderPoint(ylambda(item, vaxis), FlippedY, ActualHeight));
            if (pt != FocusedPoint) {
                FocusedPoint = pt;
            }
        }
    }

    protected override void OnRender(DrawingContext drawingContext) {
        base.OnRender(drawingContext);

        if (HorizontalAxis is IAxisManager haxis && VerticalAxis is IAxisManager vaxis && PointBrush is IBrushMapper brush && _tree != null) {
            var hr = haxis.Range;
            var vr = vaxis.Range;
            bool flippedX = FlippedX, flippedY = FlippedY;
            double radius = Radius, actualWidth = ActualWidth, actualHeight = ActualHeight;

            var items = _tree.Value.RangeSearch([hr.Minimum.Value, vr.Minimum.Value,], [hr.Maximum.Value, vr.Maximum.Value]);
            foreach (var item in items) {
                var x = haxis.TranslateToRenderPoint(item.X, flippedX, actualWidth);
                var y = vaxis.TranslateToRenderPoint(item.Y, flippedY, actualHeight);
                // drawingContext.DrawRectangle(brush.Map(item.Item), null, new Rect(x - radius, y - radius, radius * 2, radius * 2));
                drawingContext.DrawEllipse(brush.Map(_hueAccessor?.Delegates.LastOrDefault()?.DynamicInvoke(item.Item) ?? item.Item), null, new Point(x, y), radius, radius);
            }
        }
        UpdateSelectedPoint();
        UpdateFocusedPoint();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) {
        base.OnMouseLeftButtonDown(e);
        if (e.ClickCount == 1) {
            if (_tree != null && HorizontalAxis is IAxisManager haxis && VerticalAxis is IAxisManager vaxis) {
                var pt = e.GetPosition(this);
                var flippedX = FlippedX;
                var flippedY = FlippedY;
                var width = ActualWidth;
                var height = ActualHeight;

                var spot = _tree.Value.NearestNeighbor(
                    [
                        haxis.TranslateFromRenderPoint(pt.X, flippedX, width).Value,
                        vaxis.TranslateFromRenderPoint(pt.Y, flippedY, height).Value,
                    ]);

                if (Math.Pow(haxis.TranslateToRenderPoint(spot.X, flippedX, width) - pt.X, 2)
                    + Math.Pow(vaxis.TranslateToRenderPoint(spot.Y, flippedY, height) - pt.Y, 2)
                    <= Math.Pow(Radius, 2)) {
                    if (SelectedItem != spot.Item) {
                        SelectedItem = spot.Item;
                    }
                }
            }
        }
    }

    protected override void OnMouseMove(MouseEventArgs e) {
        base.OnMouseMove(e);
        if (_tree != null && HorizontalAxis is IAxisManager haxis && VerticalAxis is IAxisManager vaxis) {
            var pt = e.GetPosition(this);
            var flippedX = FlippedX;
            var flippedY = FlippedY;
            var width = ActualWidth;
            var height = ActualHeight;

            var spot = _tree.Value.NearestNeighbor(
                [
                    haxis.TranslateFromRenderPoint(pt.X, flippedX, width).Value,
                    vaxis.TranslateFromRenderPoint(pt.Y, flippedY, height).Value,
                ]);

            if (Math.Pow(haxis.TranslateToRenderPoint(spot.X, flippedX, width) - pt.X, 2)
                + Math.Pow(vaxis.TranslateToRenderPoint(spot.Y, flippedY, height) - pt.Y, 2)
                <= Math.Pow(Radius, 2)) {
                if (FocusedItem != spot.Item) {
                    FocusedItem = spot.Item;
                }
            }
        }
    }

    protected override void OnMouseLeave(MouseEventArgs e) {
        base.OnMouseLeave(e);
        FocusedItem = null;
        FocusedPoint = null;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) {
        if (_items.IsValueCreated) {
            foreach (var item in _items.Value) {
                item.PropertyChanged += UpdateVisual;
            }
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) {
        if (_items.IsValueCreated) {
            foreach (var item in _items.Value) {
                item.PropertyChanged -= UpdateVisual;
            }
        }
    }

    private void UpdateVisual(object sender, PropertyChangedEventArgs e) {
        ResetTree();
        InvalidateVisual();
    }

    class ScatterDistanceCalculator(ScatterControlSlim scatter) : IDistanceCalculator
    {
        public double Distance(double[] xs, double[] ys) {
            return Math.Sqrt(
                Math.Pow((xs[0] - ys[0]) / scatter.HorizontalAxis.Range.Delta * scatter.ActualWidth, 2) +
                Math.Pow((xs[1] - ys[1]) / scatter.VerticalAxis.Range.Delta * scatter.ActualHeight, 2));
        }

        public double RoughDistance(double[] xs, double[] ys, int i) {
            if (i == 0) {
                return Math.Abs((xs[0] - ys[0]) / scatter.HorizontalAxis.Range.Delta * scatter.ActualWidth);
            }
            else {
                return Math.Abs((xs[1] - ys[1]) / scatter.VerticalAxis.Range.Delta * scatter.ActualHeight);
            }
        }
    }
}
