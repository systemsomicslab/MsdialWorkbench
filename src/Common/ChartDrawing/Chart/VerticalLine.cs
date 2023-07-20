using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using CompMs.Graphics.Helper;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace CompMs.Graphics.Chart
{
    public sealed class VerticalLine : ChartBaseControl
    {
        public VerticalLine() {
            ClipToBounds = true;
        }

        public object Item {
            get => GetValue(ItemProperty);
            set => SetValue(ItemProperty, value);
        }

        public static readonly DependencyProperty ItemProperty =
            DependencyProperty.Register(
                nameof(Item),
                typeof(object),
                typeof(VerticalLine),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnItemChanged));
        
        private static void OnItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var vline = (VerticalLine)d;
            vline.OnItemChanged(e.OldValue, e.NewValue);
        }

        private void OnItemChanged(object oldValue, object newValue) {

        }

        public Type DataType {
            get => (Type)GetValue(DataTypeProperty);
            set => SetValue(DataTypeProperty, value);
        }

        public static readonly DependencyProperty DataTypeProperty =
            DependencyProperty.Register(
                nameof(DataType),
                typeof(Type),
                typeof(VerticalLine),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnDataTypeChanged));

        private static void OnDataTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var vline = (VerticalLine)d;
            vline.OnDataTypeChanged((Type)e.OldValue, (Type)e.NewValue);
        }

        private void OnDataTypeChanged(Type oldValue, Type newValue) {
            CoerceXProperty(newValue, HorizontalProperty);
            CoerceYProperty(newValue, VerticalProperty);
        }

        public string HorizontalProperty {
            get => (string)GetValue(HorizontalPropertyProperty);
            set => SetValue(HorizontalPropertyProperty, value);
        }

        public static readonly DependencyProperty HorizontalPropertyProperty =
            DependencyProperty.Register(
                nameof(HorizontalProperty),
                typeof(string),
                typeof(VerticalLine),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnHorizontalPropertyChanged));

        private static void OnHorizontalPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var vline = (VerticalLine)d;
            vline.OnHorizontalPropertyChanged((string)e.OldValue, (string)e.NewValue);
        }

        private void OnHorizontalPropertyChanged(string oldValue, string newValue) {
            CoerceXProperty(DataType, newValue);
        }

        public string VerticalProperty {
            get => (string)GetValue(VerticalPropertyProperty);
            set => SetValue(VerticalPropertyProperty, value);
        }

        public static readonly DependencyProperty VerticalPropertyProperty =
            DependencyProperty.Register(
                nameof(VerticalProperty),
                typeof(string),
                typeof(VerticalLine),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnVerticalPropertyChanged));

        private static void OnVerticalPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var vline = (VerticalLine)d;
            vline.OnVerticalPropertyChanged((string)e.OldValue, (string)e.NewValue);
        }

        private void OnVerticalPropertyChanged(string oldValue, string newValue) {
            CoerceYProperty(DataType, newValue);
        }

        protected override void OnHorizontalAxisChanged(IAxisManager oldValue, IAxisManager newValue) {
            base.OnHorizontalAxisChanged(oldValue, newValue);
        }

        private AxisValue _yBase;
        protected override void OnVerticalAxisChanged(IAxisManager oldValue, IAxisManager newValue) {
            base.OnVerticalAxisChanged(oldValue, newValue);
            _yBase = newValue?.TranslateToAxisValue(0d) ?? AxisValue.NaN;
            if (double.IsNegativeInfinity(_yBase.Value)) {
                _yBase = new AxisValue(1e-20);
            }
        }

        private Lazy<Func<object, IAxisManager, AxisValue>> xLambda, yLambda;

        private void CoerceXProperty(Type type, string hprop) {
            if (type is null
                || string.IsNullOrEmpty(hprop)
                || !ExpressionHelper.ValidatePropertyString(type, hprop)) {
                xLambda = null;
                return;
            }
            xLambda = new Lazy<Func<object, IAxisManager, AxisValue>>(() => ExpressionHelper.GetConvertToAxisValueExpression(type, hprop).Compile());
        }

        private void CoerceYProperty(Type type, string vprop) {
            if (type is null
                || string.IsNullOrEmpty(vprop)
                || !ExpressionHelper.ValidatePropertyString(type, vprop)) {
                yLambda = null;
                return;
            }
            yLambda = new Lazy<Func<object, IAxisManager, AxisValue>>(() => ExpressionHelper.GetConvertToAxisValueExpression(type, vprop).Compile());
        }

        public IBrushMapper LineBrush {
            get => (IBrushMapper)GetValue(LineBrushProperty);
            set => SetValue(LineBrushProperty, value);
        }

        public static readonly DependencyProperty LineBrushProperty =
            DependencyProperty.Register(
                nameof(LineBrush),
                typeof(IBrushMapper),
                typeof(VerticalLine),
                new FrameworkPropertyMetadata(
                    new ConstantBrushMapper<object>(Brushes.Black),
                    OnLineBrushPropertyChanged));

        private static void OnLineBrushPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var vline = (VerticalLine)d;
            vline.OnLineBrushPropertyChanged((IBrushMapper)e.OldValue, (IBrushMapper)e.NewValue);
        }

        private void OnLineBrushPropertyChanged(IBrushMapper oldValue, IBrushMapper newValue) {
            Selector.Update(newValue, LineThickness);
        }

        public static readonly DependencyProperty HuePropertyProperty =
            DependencyProperty.Register(
                nameof(HueProperty), typeof(string), typeof(VerticalLine),
                new PropertyMetadata(null, OnHuePropertyChanged));

        public string HueProperty {
            get => (string)GetValue(HuePropertyProperty);
            set => SetValue(HuePropertyProperty, value);
        }

        private static void OnHuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var vline = (VerticalLine)d;
            vline.OnHuePropertyChanged((string)e.OldValue, (string)e.NewValue);
        }

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
                nameof(LineThickness), typeof(double), typeof(VerticalLine),
                new FrameworkPropertyMetadata(
                    2d,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnLineThicknessChanged));

        public double LineThickness {
            get => (double)GetValue(LineThicknessProperty);
            set => SetValue(LineThicknessProperty, value);
        }

        private static void OnLineThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var vline = (VerticalLine)d;
            vline.OnLineThicknessChanged((double)e.OldValue, (double)e.NewValue);
        }

        private void OnLineThicknessChanged(double oldValue, double newValue) {
            Selector.Update(LineBrush, newValue);
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
            if (HorizontalAxis is IAxisManager haxis && VerticalAxis is IAxisManager vaxis && xLambda != null && yLambda != null && Item is object item) {
                bool flippedX = FlippedX, flippedY = FlippedY;
                double actualWidth = ActualWidth, actualHeight = ActualHeight;

                var x = haxis.TranslateToRenderPoint(xLambda.Value.Invoke(item, haxis), flippedX, actualWidth);
                var y = vaxis.TranslateToRenderPoint(yLambda.Value.Invoke(item, vaxis), flippedY, actualHeight);
                var y0 = vaxis.TranslateToRenderPoint(_yBase, flippedY, actualHeight);
                drawingContext.DrawLine(Selector.GetPen(hueLambda?.Value?.Invoke(item) ?? item), new Point(x, y), new Point(x, y0));
            }
        }
    }
}
