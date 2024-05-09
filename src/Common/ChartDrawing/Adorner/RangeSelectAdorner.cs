using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace CompMs.Graphics.Adorner
{
    public class RangeSelectAdorner : System.Windows.Documents.Adorner
    {
        public RangeSelectAdorner(UIElement adornedElement, AxisRange horizontalRange, Color color, bool isSelected) : base(adornedElement) {
            if (GetHorizontalAxis(adornedElement) is IAxisManager ha) {
                InitialX = horizontalRange.Minimum;
                CurrentX = horizontalRange.Maximum;
                ha.RangeChanged += OnRangeChanged;
            }

            Color = color;
        }

        public RangeSelectAdorner(UIElement adornedElement, RangeSelection rangeSelection) : this(adornedElement, rangeSelection.Range, rangeSelection.Color, rangeSelection.IsSelected) {
            RangeSelection = rangeSelection;

            var isSelected = new Binding(nameof(rangeSelection.IsSelected));
            isSelected.Source = rangeSelection;
            isSelected.Mode = BindingMode.TwoWay;
            SetBinding(IsSelectedProperty, isSelected);

            var color = new Binding(nameof(rangeSelection.Color));
            color.Source = rangeSelection;
            color.Mode = BindingMode.TwoWay;
            SetBinding(ColorProperty, color);
        }

        public RangeSelection RangeSelection { get; }

        static RangeSelectAdorner() {
            UnselectedForeground = new SolidColorBrush(Colors.DarkGray) { Opacity = 0.5 };
            UnselectedBorder = new Pen(Brushes.DarkGray, 2);
            UnselectedForeground.Freeze();
            UnselectedBorder.Freeze();
        }

        private static readonly Brush UnselectedForeground;
        private static readonly Pen UnselectedBorder;


        public static readonly DependencyProperty HorizontalAxisProperty =
            ChartBaseControl.HorizontalAxisProperty.AddOwner(
                typeof(RangeSelectAdorner),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnHorizontalAxisPropertyChanged));

        private static void OnHorizontalAxisPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is RangeSelectAdorner adorner) {
                adorner.OnHorizontalAxisChanged((IAxisManager)e.OldValue, (IAxisManager)e.NewValue);
            }
        }

        private void OnHorizontalAxisChanged(IAxisManager prev, IAxisManager next) {
            if (prev != null) {
                prev.RangeChanged -= OnRangeChanged;
            }
            if (next != null) {
                next.RangeChanged += OnRangeChanged;
            }
        }

        public IAxisManager GetHorizontalAxis(DependencyObject d) {
            return (IAxisManager)d.GetValue(HorizontalAxisProperty);
        }

        private void OnRangeChanged(object sender, EventArgs e) {
            InvalidateVisual();
        }

        public static readonly DependencyProperty FlippedXProperty =
            ChartBaseControl.FlippedXProperty.AddOwner(
                typeof(RangeSelectAdorner),
                new FrameworkPropertyMetadata(
                    BooleanBoxes.FalseBox,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public bool GetFlippedX(DependencyObject d) {
            return (bool)d.GetValue(FlippedXProperty);
        }

        public static readonly DependencyProperty InitialXProperty =
            DependencyProperty.Register(
                nameof(InitialX),
                typeof(AxisValue),
                typeof(RangeSelectAdorner),
                new FrameworkPropertyMetadata(AxisValue.NaN, FrameworkPropertyMetadataOptions.AffectsRender));

        public AxisValue InitialX {
            get => (AxisValue)GetValue(InitialXProperty);
            set => SetValue(InitialXProperty, value);
        }

        public static readonly DependencyProperty CurrentXProperty =
            DependencyProperty.Register(
                nameof(CurrentX),
                typeof(AxisValue),
                typeof(RangeSelectAdorner),
                new FrameworkPropertyMetadata(AxisValue.NaN, FrameworkPropertyMetadataOptions.AffectsRender));

        public AxisValue CurrentX {
            get => (AxisValue)GetValue(CurrentXProperty);
            set => SetValue(CurrentXProperty, value);
        }

        public AxisRange HorizontalRange {
            get => new AxisRange(Math.Min(InitialX, CurrentX), Math.Max(InitialX, CurrentX));
            set {
                InitialX = value.Minimum;
                CurrentX = value.Maximum;
            }
        }

        public Rect RenderRectangle {
            get {
                var adornedElement = AdornedElement;
                var size = adornedElement.RenderSize;
                var iniX = GetHorizontalAxis(adornedElement)?.TranslateToRenderPoint(InitialX, GetFlippedX(adornedElement), size.Width) ?? 0d;
                var curX = GetHorizontalAxis(adornedElement)?.TranslateToRenderPoint(CurrentX, GetFlippedX(adornedElement), size.Width) ?? size.Width;
                return new Rect(new Point(iniX, 0), new Point(curX, size.Height));
            }
        }

        private static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(
                nameof(Color),
                typeof(Color),
                typeof(RangeSelectAdorner),
                new FrameworkPropertyMetadata(
                    Colors.Gray,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnColorChanged));

        public Color Color {
            get => (Color)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is RangeSelectAdorner adorner) {
                adorner.OnColorChanged((Color)e.OldValue, (Color)e.NewValue);
            }
        }

        private void OnColorChanged(Color oldValue, Color newValue) {
            if (oldValue == newValue) {
                return;
            }

            var foreground = new SolidColorBrush(newValue) { Opacity = 0.5d };
            foreground.Freeze();
            Foreground = foreground;

            var border = new Pen(new SolidColorBrush(newValue), 2);
            border.Freeze();
            Border = border;
        }

        private Brush Foreground { get; set; }

        private Pen Border { get; set; }

        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register(
                nameof(IsSelected),
                typeof(bool),
                typeof(RangeSelectAdorner),
                new FrameworkPropertyMetadata(
                    BooleanBoxes.FalseBox,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public bool IsSelected {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, BooleanBoxes.Box(value));
        }

        private AdornerLayer layer;

        public void Attach() {
            layer = AdornerLayer.GetAdornerLayer(AdornedElement);

            if (layer != null) {
                layer.Add(this);
            }
        }

        public void Detach() {
            if (layer != null) {
                layer.Remove(this);
                layer = null;
            }
        }

        protected override void OnRender(DrawingContext drawingContext) {
            if (IsSelected) {
                drawingContext.DrawRectangle(
                    Foreground,
                    Border,
                    RenderRectangle);
            }
            else {
                drawingContext.DrawRectangle(
                    UnselectedForeground,
                    UnselectedBorder,
                    RenderRectangle);
            }
        }

        private Point initial;
        private bool click = false;
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) {
            base.OnMouseLeftButtonDown(e);
            initial = e.GetPosition(this);
            click = true;
            CaptureMouse();
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e) {
            base.OnMouseLeftButtonUp(e);
            if (click) {
                ReleaseMouseCapture();
                click = false;

                var current = e.GetPosition(this);
                if (Math.Abs(initial.X - current.X) <= SystemParameters.MinimumHorizontalDragDistance &&
                    Math.Abs(initial.Y - current.Y) <= SystemParameters.MinimumVerticalDragDistance) {
                    IsSelected = !IsSelected;
                }
            }
        }
    }
}
