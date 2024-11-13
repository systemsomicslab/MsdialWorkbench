using CompMs.Common.Extension;
using CompMs.Common.Utility;
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
    public class LineSpectrumControlSlim : ChartBaseControl
    {
        public LineSpectrumControlSlim() {
            ClipToBounds = true;
        }
        public IEnumerable ItemsSource {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(IEnumerable),
                typeof(LineSpectrumControlSlim),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnItemsSourceChanged));

        private ICollectionView collectionView;

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var lineSpectrum = (LineSpectrumControlSlim)d;
            lineSpectrum.OnItemsSourceChanged((IEnumerable)e.OldValue, (IEnumerable)e.NewValue);
        }

        private void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue) {
            if (oldValue is INotifyCollectionChanged oldCollection) {
                oldCollection.CollectionChanged -= OnItemsSourceCollectionChanged;
            }
            if (newValue is INotifyCollectionChanged newCollection) {
                newCollection.CollectionChanged += OnItemsSourceCollectionChanged;
            }

            if (collectionView != null) {
                collectionView.CurrentChanged -= OnItemsSourceCurrentChanged;
            }
            collectionView = newValue as ICollectionView ?? CollectionViewSource.GetDefaultView(newValue);
            if (collectionView != null) {
                collectionView.CurrentChanged += OnItemsSourceCurrentChanged;
            }

            CoerceTree();
        }

        private void OnItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            CoerceTree();
            InvalidateVisual();
        }

        private void OnItemsSourceCurrentChanged(object sender, EventArgs e) {
            SelectedItem = collectionView?.CurrentItem;
        }

        public Type DataType {
            get => (Type)GetValue(DataTypeProperty);
            set => SetValue(DataTypeProperty, value);
        }

        public static readonly DependencyProperty DataTypeProperty =
            DependencyProperty.Register(
                nameof(DataType),
                typeof(Type),
                typeof(LineSpectrumControlSlim),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnDataTypeChanged));

        private static void OnDataTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var lineSpectrum = (LineSpectrumControlSlim)d;
            lineSpectrum.OnDataTypeChanged((Type)e.OldValue, (Type)e.NewValue);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        private void OnDataTypeChanged(Type oldValue, Type newValue) {
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
                typeof(LineSpectrumControlSlim),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnHorizontalPropertyChanged));

        private static void OnHorizontalPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var lineSpectrum = (LineSpectrumControlSlim)d;
            lineSpectrum.OnHorizontalPropertyChanged((string)e.OldValue, (string)e.NewValue);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        private void OnHorizontalPropertyChanged(string oldValue, string newValue) {
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
                typeof(LineSpectrumControlSlim),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnVerticalPropertyChanged));

        private static void OnVerticalPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var lineSpectrum = (LineSpectrumControlSlim)d;
            lineSpectrum.OnVerticalPropertyChanged((string)e.OldValue, (string)e.NewValue);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        private void OnVerticalPropertyChanged(string oldValue, string newValue) {
            CoerceYProperty(DataType, newValue);
            CoerceTree();
        }

        protected override void OnHorizontalAxisChanged(IAxisManager oldValue, IAxisManager newValue) {
            base.OnHorizontalAxisChanged(oldValue, newValue);
            CoerceTree();
        }

        protected override void OnHorizontalMappingChanged(object sender, EventArgs e) {
            base.OnHorizontalMappingChanged(sender, e);
            CoerceTree();
            InvalidateVisual();
        }

        private AxisValue yBase;
        protected override void OnVerticalAxisChanged(IAxisManager oldValue, IAxisManager newValue) {
            base.OnVerticalAxisChanged(oldValue, newValue);
            yBase = newValue?.TranslateToAxisValue(0d) ?? AxisValue.NaN;
            if (double.IsNegativeInfinity(yBase.Value)) {
                yBase = new AxisValue(1e-20);
            }
            CoerceTree();
        }

        protected override void OnVerticalMappingChanged(object sender, EventArgs e) {
            base.OnVerticalMappingChanged(sender, e);
            yBase = VerticalAxis?.TranslateToAxisValue(0d) ?? AxisValue.NaN;
            if (double.IsNegativeInfinity(yBase.Value)) {
                yBase = new AxisValue(1e-20);
            }
            CoerceTree();
            InvalidateVisual();
        }

        private Lazy<List<LineSpectrumControlSlimItem>> tree;

        private void CoerceTree() {
            if (!(ItemsSource is IEnumerable source)
                || xLambda == null
                || yLambda == null
                || !(HorizontalAxis is IAxisManager haxis)
                || !(VerticalAxis is IAxisManager vaxis)) {
                tree = null;
                return;
            }

            var xlambda = xLambda.Value;
            var ylambda = yLambda.Value;

            tree = new Lazy<List<LineSpectrumControlSlimItem>>(() =>
                source.Cast<object>().Select(item => // TODO: I need to remove boxing here.
                    new LineSpectrumControlSlimItem(xlambda(item, haxis), ylambda(item, vaxis), item))
                .OrderBy(item => item.X.Value)
                .ToList());
        }

        private Lazy<Func<object, IAxisManager, AxisValue>> xLambda, yLambda;

        private void CoerceXProperty(Type type, string hprop) {
            if (type == null
                || string.IsNullOrEmpty(hprop)
                || !ExpressionHelper.ValidatePropertyString(type, hprop)) {
                xLambda = null;
                return;
            }
            var xProperty = ExpressionHelper.GetConvertToAxisValueExpression(type, hprop);
            xLambda = new Lazy<Func<object, IAxisManager, AxisValue>>(xProperty.Compile);
        }

        private void CoerceYProperty(Type type, string vprop) {
            if (type == null
                || string.IsNullOrEmpty(vprop)
                || !ExpressionHelper.ValidatePropertyString(type, vprop)) {
                yLambda = null;
                return;
            }
            var yProperty = ExpressionHelper.GetConvertToAxisValueExpression(type, vprop);
            yLambda = new Lazy<Func<object, IAxisManager, AxisValue>>(yProperty.Compile);
        }

        public IBrushMapper LineBrush {
            get => (IBrushMapper)GetValue(LineBrushProperty);
            set => SetValue(LineBrushProperty, value);
        }

        public static readonly DependencyProperty LineBrushProperty =
            DependencyProperty.Register(
                nameof(LineBrush),
                typeof(IBrushMapper),
                typeof(LineSpectrumControlSlim),
                new FrameworkPropertyMetadata(
                    new ConstantBrushMapper<object>(Brushes.Black),
                    OnLineBrushPropertyChanged));

        private static void OnLineBrushPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var c = (LineSpectrumControlSlim)d;
            c.OnLineBrushPropertyChanged((IBrushMapper)e.OldValue, (IBrushMapper)e.NewValue);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        private void OnLineBrushPropertyChanged(IBrushMapper oldValue, IBrushMapper newValue) {
            Selector.Update(newValue, LineThickness);
        }

        public static readonly DependencyProperty HuePropertyProperty =
            DependencyProperty.Register(
                nameof(HueProperty), typeof(string), typeof(LineSpectrumControlSlim),
                new PropertyMetadata(null, OnHuePropertyChanged));

        public string HueProperty {
            get => (string)GetValue(HuePropertyProperty);
            set => SetValue(HuePropertyProperty, value);
        }

        private static void OnHuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var c = (LineSpectrumControlSlim)d;
            c.OnHuePropertyChanged((string)e.OldValue, (string)e.NewValue);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        private void OnHuePropertyChanged(string oldValue, string newValue) {
            CoerceHueProperty(DataType, newValue);
        }

        private Lazy<Func<object, object>> hueLambda;

        private void CoerceHueProperty(Type type, string hueProperty) {
            if (type == null
                || string.IsNullOrEmpty(hueProperty)
                || !type.GetProperties().Any(m => m.Name == hueProperty)) {
                hueLambda = null;
                return;
            }
            var arg = System.Linq.Expressions.Expression.Parameter(typeof(object));
            var casted = System.Linq.Expressions.Expression.Convert(arg, type);
            var property = System.Linq.Expressions.Expression.Property(casted, HueProperty);
            var result = System.Linq.Expressions.Expression.Convert(property, typeof(object));
            var lambda = System.Linq.Expressions.Expression.Lambda<Func<object, object>>(result, arg);
            hueLambda = new Lazy<Func<object, object>>(lambda.Compile);
        }

        public static readonly DependencyProperty LineThicknessProperty =
            DependencyProperty.Register(
                nameof(LineThickness), typeof(double), typeof(LineSpectrumControlSlim),
                new FrameworkPropertyMetadata(
                    2d,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnLineThicknessChanged));

        public double LineThickness {
            get => (double)GetValue(LineThicknessProperty);
            set => SetValue(LineThicknessProperty, value);
        }

        private static void OnLineThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var c = (LineSpectrumControlSlim)d;
            c.OnLineThicknessChanged((double)e.OldValue, (double)e.NewValue);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        private void OnLineThicknessChanged(double oldValue, double newValue) {
            Selector.Update(LineBrush, newValue);
        }

        private PenSelector Selector {
            get {
                if (selector is null) {
                    selector = new PenSelector();
                    if (!(LineBrush is null)) {
                        selector.Update(LineBrush, LineThickness);
                    }
                }
                return selector;
            }
        }
        private PenSelector selector;

        public object SelectedItem {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                nameof(SelectedItem),
                typeof(object),
                typeof(LineSpectrumControlSlim),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedItemChanged));

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var lineSpectrum = (LineSpectrumControlSlim)d;
            lineSpectrum.OnSelectedItemChanged(e.OldValue, e.NewValue);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        private void OnSelectedItemChanged(object oldValue, object newValue) {
            collectionView?.MoveCurrentTo(newValue);
            UpdateSelectedPoint();
        }

        public Point? SelectedPoint {
            get => (Point?)GetValue(SelectedPointProperty);
            set => SetValue(SelectedPointProperty, value);
        }

        private static readonly DependencyProperty SelectedPointProperty =
            DependencyProperty.Register(
                nameof(SelectedPoint),
                typeof(Point?),
                typeof(LineSpectrumControlSlim),
                new PropertyMetadata(null));

        private void UpdateSelectedPoint() {
            if (HorizontalAxis is IAxisManager haxis
                && VerticalAxis is IAxisManager vaxis
                && xLambda?.Value is Func<object, IAxisManager, AxisValue> xlambda
                && yLambda?.Value is Func<object, IAxisManager, AxisValue> ylambda
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
                typeof(LineSpectrumControlSlim),
                new FrameworkPropertyMetadata(
                    null,
                    OnFocusedItemChanged));

        private static void OnFocusedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var lineSpectrum = (LineSpectrumControlSlim)d;
            lineSpectrum.OnFocusedItemChanged(e.OldValue, e.NewValue);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        private void OnFocusedItemChanged(object oldValue, object newValue) {

        }

        public Point? FocusedPoint {
            get => (Point?)GetValue(FocusedPointProperty);
            set => SetValue(FocusedPointProperty, value);
        }

        private static readonly DependencyProperty FocusedPointProperty =
            DependencyProperty.Register(
                nameof(FocusedPoint),
                typeof(Point?),
                typeof(LineSpectrumControlSlim),
                new PropertyMetadata(null));

        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);
            if (HorizontalAxis is IAxisManager haxis && VerticalAxis is IAxisManager vaxis && tree != null) {
                var hr = haxis.Range;
                var vr = vaxis.Range;
                bool flippedX = FlippedX, flippedY = FlippedY;
                double actualWidth = ActualWidth, actualHeight = ActualHeight;
                var y0 = vaxis.TranslateToRenderPoint(yBase, flippedY, actualHeight);

                var lo = SearchCollection.LowerBound(tree.Value, new LineSpectrumControlSlimItem(hr.Minimum, 0d, null), (a, b) => a.X.Value.CompareTo(b.X.Value));
                var hi = SearchCollection.UpperBound(tree.Value, new LineSpectrumControlSlimItem(hr.Maximum, 0d, null), (a, b) => a.X.Value.CompareTo(b.X.Value));
                for (int i = lo; i < hi; i++) {
                    var item = tree.Value[i];
                    var x = haxis.TranslateToRenderPoint(item.X, flippedX, actualWidth);
                    var y = vaxis.TranslateToRenderPoint(item.Y, flippedY, actualHeight);
                    drawingContext.DrawLine(Selector.GetPen(hueLambda?.Value?.Invoke(item.Item) ?? item.Item), new Point(x, y), new Point(x, y0));
                }
            }

            UpdateSelectedPoint();
        }

        private static readonly double radius = 3d;

        private bool CursorOnLine(AxisValue x, AxisValue y, LineSpectrumControlSlimItem item, double cutoff) {
            return (item.Y >= y && y > yBase || yBase > y && y >= item.Y)
                && item.X.Value - cutoff < x.Value && x.Value < item.X.Value + cutoff;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) {
            base.OnMouseLeftButtonDown(e);
            if (e.ClickCount == 1) {
                if (tree != null && HorizontalAxis is IAxisManager haxis && VerticalAxis is IAxisManager vaxis) {
                    var pt = e.GetPosition(this);
                    var flippedX = FlippedX;
                    var flippedY = FlippedY;
                    var width = ActualWidth;
                    var height = ActualHeight;

                    var x = haxis.TranslateFromRenderPoint(pt.X, flippedX, width);
                    var dx = Math.Abs(x - haxis.TranslateFromRenderPoint(pt.X - radius, flippedX, width));
                    var y = vaxis.TranslateFromRenderPoint(pt.Y, flippedY, height);
                    var lo = SearchCollection.LowerBound(tree.Value, new LineSpectrumControlSlimItem(x - dx, 0d, null), (a, b) => a.X.Value.CompareTo(b.X.Value));
                    var hi = SearchCollection.UpperBound(tree.Value, new LineSpectrumControlSlimItem(x + dx, 0d, null), (a, b) => a.X.Value.CompareTo(b.X.Value));

                    var spot = tree.Value.GetRange(lo, hi - lo)
                        .Where(item => CursorOnLine(x, y, item, dx))
                        .DefaultIfEmpty()
                        .Argmin(item => Math.Abs(x - item?.X ?? 0d));
                    if (spot != null && SelectedItem != spot.Item) {
                        SelectedItem = spot.Item;
                        SelectedPoint = pt;
                    }
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);
            if (tree != null && HorizontalAxis is IAxisManager haxis && VerticalAxis is IAxisManager vaxis) {
                var pt = e.GetPosition(this);
                var flippedX = FlippedX;
                var flippedY = FlippedY;
                var width = ActualWidth;
                var height = ActualHeight;

                var x = haxis.TranslateFromRenderPoint(pt.X, flippedX, width);
                var dx = Math.Abs(x - haxis.TranslateFromRenderPoint(pt.X - radius, flippedX, width));
                var y = vaxis.TranslateFromRenderPoint(pt.Y, flippedY, height);
                var lo = SearchCollection.LowerBound(tree.Value, new LineSpectrumControlSlimItem(x - dx, 0d, null), (a, b) => a.X.Value.CompareTo(b.X.Value));
                var hi = SearchCollection.UpperBound(tree.Value, new LineSpectrumControlSlimItem(x + dx, 0d, null), (a, b) => a.X.Value.CompareTo(b.X.Value));

                var spot = tree.Value.GetRange(lo, hi - lo)
                    .Where(item => CursorOnLine(x, y, item, dx))
                    .DefaultIfEmpty()
                    .Argmin(item => Math.Abs(x - item?.X ?? 0d));
                if (spot != null && FocusedItem != spot.Item) {
                    FocusedItem = spot.Item;
                    FocusedPoint = pt;
                }
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e) {
            base.OnMouseLeave(e);
            FocusedItem = null;
            FocusedPoint = null;
        }
    }

    class LineSpectrumControlSlimItem
    {
        public LineSpectrumControlSlimItem(AxisValue x, AxisValue y, object item) {
            X = x;
            Y = y;
            Item = item;
        }

        public AxisValue X { get; }
        public AxisValue Y { get; }
        public object Item { get; }
    }
}
