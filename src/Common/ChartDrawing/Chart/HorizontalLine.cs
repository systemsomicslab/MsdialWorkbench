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
    public sealed class HorizontalLine : ChartBaseControl
    {
        public HorizontalLine() {
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
                typeof(HorizontalLine),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnItemChanged));
        
        private static void OnItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var hline = (HorizontalLine)d;
            hline.OnItemChanged(e.OldValue, e.NewValue);
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
                typeof(HorizontalLine),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnDataTypeChanged));

        private static void OnDataTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var hline = (HorizontalLine)d;
            hline.OnDataTypeChanged((Type)e.OldValue, (Type)e.NewValue);
        }

        private void OnDataTypeChanged(Type oldValue, Type newValue) {
            CoerceYProperty(newValue, VerticalProperty);
        }

        public string VerticalProperty {
            get => (string)GetValue(VerticalPropertyProperty);
            set => SetValue(VerticalPropertyProperty, value);
        }

        public static readonly DependencyProperty VerticalPropertyProperty =
            DependencyProperty.Register(
                nameof(VerticalProperty),
                typeof(string),
                typeof(HorizontalLine),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnVerticalPropertyChanged));

        private static void OnVerticalPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var hline = (HorizontalLine)d;
            hline.OnVerticalPropertyChanged((string)e.OldValue, (string)e.NewValue);
        }

        private void OnVerticalPropertyChanged(string oldValue, string newValue) {
            CoerceYProperty(DataType, newValue);
        }

        protected override void OnHorizontalAxisChanged(IAxisManager oldValue, IAxisManager newValue) {
            base.OnHorizontalAxisChanged(oldValue, newValue);
        }

        protected override void OnVerticalAxisChanged(IAxisManager oldValue, IAxisManager newValue) {
            base.OnVerticalAxisChanged(oldValue, newValue);
        }

        private Lazy<Func<object, IAxisManager, AxisValue>> yLambda;

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
                typeof(HorizontalLine),
                new FrameworkPropertyMetadata(
                    new ConstantBrushMapper<object>(Brushes.Black),
                    OnLineBrushPropertyChanged));

        private static void OnLineBrushPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var hline = (HorizontalLine)d;
            hline.OnLineBrushPropertyChanged((IBrushMapper)e.OldValue, (IBrushMapper)e.NewValue);
        }

        private void OnLineBrushPropertyChanged(IBrushMapper oldValue, IBrushMapper newValue) {
            Selector.Update(newValue, LineThickness);
        }

        public static readonly DependencyProperty HuePropertyProperty =
            DependencyProperty.Register(
                nameof(HueProperty), typeof(string), typeof(HorizontalLine),
                new PropertyMetadata(null, OnHuePropertyChanged));

        public string HueProperty {
            get => (string)GetValue(HuePropertyProperty);
            set => SetValue(HuePropertyProperty, value);
        }

        private static void OnHuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var hline = (HorizontalLine)d;
            hline.OnHuePropertyChanged((string)e.OldValue, (string)e.NewValue);
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
                nameof(LineThickness), typeof(double), typeof(HorizontalLine),
                new FrameworkPropertyMetadata(
                    2d,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnLineThicknessChanged));

        public double LineThickness {
            get => (double)GetValue(LineThicknessProperty);
            set => SetValue(LineThicknessProperty, value);
        }

        private static void OnLineThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var hline = (HorizontalLine)d;
            hline.OnLineThicknessChanged((double)e.OldValue, (double)e.NewValue);
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
            if (VerticalAxis is IAxisManager vaxis && yLambda != null && Item is object item) {
                bool flippedY = FlippedY;
                double actualWidth = ActualWidth, actualHeight = ActualHeight;

                var y = vaxis.TranslateToRenderPoint(yLambda.Value.Invoke(item, vaxis), flippedY, actualHeight);
                drawingContext.DrawLine(Selector.GetPen(hueLambda?.Value?.Invoke(item) ?? item), new Point(0, y), new Point(actualWidth, y));
            }
        }
    }
}
