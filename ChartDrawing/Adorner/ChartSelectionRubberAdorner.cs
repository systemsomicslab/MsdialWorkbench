using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace CompMs.Graphics.Adorner
{
    public class ChartSelectionRubberAdorner : System.Windows.Documents.Adorner, IObservable<ChartSelectionRubberAdorner>
    {
        static ChartSelectionRubberAdorner() {
            UnselectedForeground = new SolidColorBrush(Colors.DarkGray) { Opacity = 0.5 };
            UnselectedBorder = new Pen(Brushes.DarkGray, 2);
            UnselectedForeground.Freeze();
            UnselectedBorder.Freeze();
        }

        private static readonly Brush UnselectedForeground;
        private static readonly Pen UnselectedBorder;

        public ChartSelectionRubberAdorner(UIElement adornedElement, Point initialPoint, Color color) : base(adornedElement) {
            if (GetHorizontalAxis(adornedElement) is IAxisManager ha) {
                InitialX = ha?.TranslateFromRenderPoint(initialPoint.X, GetFlippedX(adornedElement), adornedElement.RenderSize.Width) ?? AxisValue.NaN;
                ha.RangeChanged += OnRangeChanged;
            }
            if (GetVerticalAxis(adornedElement) is IAxisManager va) {
                InitialY = va?.TranslateFromRenderPoint(initialPoint.Y, GetFlippedY(adornedElement), adornedElement.RenderSize.Height) ?? AxisValue.NaN;
                va.RangeChanged += OnRangeChanged;
            }

            this.color = color;

            var foreground = new SolidColorBrush(color) { Opacity = 0.5 };
            foreground.Freeze();
            Foreground = foreground;

            var border = new Pen(new SolidColorBrush(color), 2);
            border.Freeze();
            Border = border;
        }

        private Color color;

        public static readonly DependencyProperty HorizontalAxisProperty =
            ChartBaseControl.HorizontalAxisProperty.AddOwner(
                typeof(ChartSelectionRubberAdorner),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnHorizontalAxisPropertyChanged));

        private static void OnHorizontalAxisPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is ChartSelectionRubberAdorner adorner) {
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

        public static readonly DependencyProperty VerticalAxisProperty =
            ChartBaseControl.VerticalAxisProperty.AddOwner(
                typeof(ChartSelectionRubberAdorner),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnVerticalAxisPropertyChanged));

        private static void OnVerticalAxisPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is ChartSelectionRubberAdorner adorner) {
                adorner.OnVerticalAxisChanged((IAxisManager)e.OldValue, (IAxisManager)e.NewValue);
            }
        }

        private void OnVerticalAxisChanged(IAxisManager prev, IAxisManager next) {
            if (prev != null) {
                prev.RangeChanged -= OnRangeChanged;
            }
            if (next != null) {
                next.RangeChanged += OnRangeChanged;
            }
        }

        public IAxisManager GetVerticalAxis(DependencyObject d) {
            return (IAxisManager)d.GetValue(VerticalAxisProperty);
        }

        private void OnRangeChanged(object sender, EventArgs e) {
            InvalidateVisual();
        }

        public static readonly DependencyProperty FlippedXProperty =
            ChartBaseControl.FlippedXProperty.AddOwner(
                typeof(ChartSelectionRubberAdorner),
                new FrameworkPropertyMetadata(
                    BooleanBoxes.FalseBox,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public bool GetFlippedX(DependencyObject d) {
            return (bool)d.GetValue(FlippedXProperty);
        }

        public static readonly DependencyProperty FlippedYProperty =
            ChartBaseControl.FlippedYProperty.AddOwner(
                typeof(ChartSelectionRubberAdorner),
                new FrameworkPropertyMetadata(
                    BooleanBoxes.TrueBox,
                    FrameworkPropertyMetadataOptions.AffectsRender));
        public bool GetFlippedY(DependencyObject d) {
            return (bool)d.GetValue(FlippedYProperty);
        }

        public static readonly DependencyProperty InitialXProperty =
            DependencyProperty.Register(
                nameof(InitialX),
                typeof(AxisValue),
                typeof(ChartSelectionRubberAdorner),
                new FrameworkPropertyMetadata(AxisValue.NaN, FrameworkPropertyMetadataOptions.AffectsRender));

        public AxisValue InitialX {
            get => (AxisValue)GetValue(InitialXProperty);
            set => SetValue(InitialXProperty, value);
        }

        public static readonly DependencyProperty InitialYProperty =
            DependencyProperty.Register(
                nameof(InitialY),
                typeof(AxisValue),
                typeof(ChartSelectionRubberAdorner),
                new FrameworkPropertyMetadata(AxisValue.NaN, FrameworkPropertyMetadataOptions.AffectsRender));

        public AxisValue InitialY {
            get => (AxisValue)GetValue(InitialYProperty);
            set => SetValue(InitialYProperty, value);
        }

        public Point InitialPoint {
            get {
                return new Point(
                    GetHorizontalAxis(AdornedElement)?.TranslateToRenderPoint(InitialX, GetFlippedX(AdornedElement), AdornedElement.RenderSize.Width) ?? 0,
                    GetVerticalAxis(AdornedElement)?.TranslateToRenderPoint(InitialY, GetFlippedY(AdornedElement), AdornedElement.RenderSize.Height) ?? 0);
            }
        }

        public static readonly DependencyProperty CurrentXProperty =
            DependencyProperty.Register(
                nameof(CurrentX),
                typeof(AxisValue),
                typeof(ChartSelectionRubberAdorner),
                new FrameworkPropertyMetadata(AxisValue.NaN, FrameworkPropertyMetadataOptions.AffectsRender));

        public AxisValue CurrentX {
            get => (AxisValue)GetValue(CurrentXProperty);
            set => SetValue(CurrentXProperty, value);
        }

        public static readonly DependencyProperty CurrentYProperty =
            DependencyProperty.Register(
                nameof(CurrentY),
                typeof(AxisValue),
                typeof(ChartSelectionRubberAdorner),
                new FrameworkPropertyMetadata(AxisValue.NaN, FrameworkPropertyMetadataOptions.AffectsRender));

        public AxisValue CurrentY {
            get => (AxisValue)GetValue(CurrentYProperty);
            set => SetValue(CurrentYProperty, value);
        }

        public Point CurrentPoint {
            get {
                return new Point(
                    GetHorizontalAxis(AdornedElement)?.TranslateToRenderPoint(CurrentX, GetFlippedX(AdornedElement), AdornedElement.RenderSize.Width) ?? AdornedElement.RenderSize.Width,
                    GetVerticalAxis(AdornedElement)?.TranslateToRenderPoint(CurrentY, GetFlippedY(AdornedElement), AdornedElement.RenderSize.Height) ?? AdornedElement.RenderSize.Height);
            }
            set {
                CurrentX = GetHorizontalAxis(AdornedElement)?.TranslateFromRenderPoint(value.X, GetFlippedX(AdornedElement), AdornedElement.RenderSize.Width) ?? AxisValue.NaN;
                CurrentY = GetVerticalAxis(AdornedElement)?.TranslateFromRenderPoint(value.Y, GetFlippedY(AdornedElement), AdornedElement.RenderSize.Height) ?? AxisValue.NaN;
            }
        }

        private static readonly DependencyProperty ForegroundProperty =
            DependencyProperty.Register(
                nameof(Foreground),
                typeof(Brush),
                typeof(ChartSelectionRubberAdorner),
                new FrameworkPropertyMetadata(default, FrameworkPropertyMetadataOptions.AffectsRender));

        private Brush Foreground {
            get => (Brush)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        private static readonly DependencyProperty BorderProperty =
            DependencyProperty.Register(
                nameof(Border),
                typeof(Pen),
                typeof(ChartSelectionRubberAdorner),
                new FrameworkPropertyMetadata(default, FrameworkPropertyMetadataOptions.AffectsRender));
        private Pen Border {
            get => (Pen)GetValue(BorderProperty);
            set => SetValue(BorderProperty, value);
        }

        public void ChangeColor(Color color) {
            if (color == this.color) {
                return;
            }

            this.color = color;

            var foreground = new SolidColorBrush(color) { Opacity = 0.5 };
            foreground.Freeze();
            Foreground = foreground;

            var border = new Pen(new SolidColorBrush(color), 2);
            border.Freeze();
            Border = border;
        }

        public static readonly DependencyProperty IsSelectedProperty =
            DependencyProperty.Register(
                nameof(IsSelected),
                typeof(bool),
                typeof(ChartSelectionRubberAdorner),
                new FrameworkPropertyMetadata(
                    BooleanBoxes.FalseBox,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    OnIsSelectedChanged));

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is ChartSelectionRubberAdorner adorner) {
                foreach (var observer in adorner.observers) {
                    observer.OnNext(adorner);
                }
            }
        }

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
                observers.Clear();
            }
        }

        protected override void OnRender(DrawingContext drawingContext) {
            if (IsSelected) {
                drawingContext.DrawRectangle(
                    Foreground,
                    Border,
                    new Rect(InitialPoint, CurrentPoint));
            }
            else {
                drawingContext.DrawRectangle(
                    UnselectedForeground,
                    UnselectedBorder,
                    new Rect(InitialPoint, CurrentPoint));
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e) {
            base.OnMouseLeftButtonUp(e);
            IsSelected = !IsSelected;
        }

        List<IObserver<ChartSelectionRubberAdorner>> observers = new List<IObserver<ChartSelectionRubberAdorner>>();
        public IDisposable Subscribe(IObserver<ChartSelectionRubberAdorner> observer) {
            observers.Add(observer);
            return new Unsubscriber(this, observer);
        }

        class Unsubscriber : IDisposable
        {
            private ChartSelectionRubberAdorner adorner;
            private IObserver<ChartSelectionRubberAdorner> observer;

            public Unsubscriber(ChartSelectionRubberAdorner adorner, IObserver<ChartSelectionRubberAdorner> observer) {
                this.adorner = adorner;
                this.observer = observer;
            }

            public void Dispose() {
                if (adorner != null || observer != null) {
                    adorner.observers.Remove(observer);
                    observer = null;
                    adorner = null;
                }
            }
        }
    }
}
