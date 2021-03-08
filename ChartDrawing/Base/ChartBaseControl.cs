using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CompMs.Graphics.Core.Adorner;


namespace CompMs.Graphics.Core.Base
{
    public abstract class ChartBaseControl : FrameworkElement
    {
        public static readonly DependencyProperty HorizontalAxisProperty =
            DependencyProperty.RegisterAttached(
                nameof(HorizontalAxis), typeof(IAxisManager), typeof(ChartBaseControl),
                new FrameworkPropertyMetadata(
                    default(IAxisManager),
                    FrameworkPropertyMetadataOptions.AffectsRender
                    | FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender
                    | FrameworkPropertyMetadataOptions.Inherits,
                    OnHorizontalAxisChanged));

        public static IAxisManager GetHorizontalAxis(DependencyObject d)
            => (IAxisManager)d.GetValue(HorizontalAxisProperty);

        public static void SetHorizontalAxis(DependencyObject d, IAxisManager value)
            => d.SetValue(HorizontalAxisProperty, value);

        public IAxisManager HorizontalAxis
        {
            get => (IAxisManager)GetValue(HorizontalAxisProperty);
            set => SetValue(HorizontalAxisProperty, value);
        }

        static void OnHorizontalAxisChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is ChartBaseControl bc) {
                bc.OnHorizontalAxisChanged((IAxisManager)e.OldValue, (IAxisManager)e.NewValue);
            }
        }

        void OnHorizontalAxisChanged(IAxisManager oldValue, IAxisManager newValue) {
            if (oldValue != null) {
                oldValue.RangeChanged -= OnHorizontalRangeChanged;
            }

            if (newValue != null) {
                newValue.RangeChanged += OnHorizontalRangeChanged;
            }
        }

        void OnHorizontalRangeChanged(object sender, EventArgs e) {
            InvalidateVisual();
        }

        public static readonly DependencyProperty VerticalAxisProperty =
            DependencyProperty.RegisterAttached(
                nameof(VerticalAxis), typeof(IAxisManager), typeof(ChartBaseControl),
                new FrameworkPropertyMetadata(
                    default(IAxisManager),
                    FrameworkPropertyMetadataOptions.AffectsRender
                    | FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender
                    | FrameworkPropertyMetadataOptions.Inherits,
                    OnVerticalAxisChanged));

        public static IAxisManager GetVerticalAxis(DependencyObject d)
            => (IAxisManager)d.GetValue(VerticalAxisProperty);

        public static void SetVerticalAxis(DependencyObject d, IAxisManager value)
            => d.SetValue(VerticalAxisProperty, value);

        public IAxisManager VerticalAxis
        {
            get => (IAxisManager)GetValue(VerticalAxisProperty);
            set => SetValue(VerticalAxisProperty, value);
        }

        static void OnVerticalAxisChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is ChartBaseControl bc) {
                bc.OnVerticalAxisChanged((IAxisManager)e.OldValue, (IAxisManager)e.NewValue);
            }
        }

        void OnVerticalAxisChanged(IAxisManager oldValue, IAxisManager newValue) {
            if (oldValue != null) {
                oldValue.RangeChanged -= OnVerticalRangeChanged;
            }

            if (newValue != null) {
                newValue.RangeChanged += OnVerticalRangeChanged;
            }
        }

        void OnVerticalRangeChanged(object sender, EventArgs e) {
            InvalidateVisual();
        }

        [Obsolete("Range infomation move to Axis.")]
        public static readonly DependencyProperty RangeXProperty = DependencyProperty.Register(
            nameof(RangeX), typeof(Range), typeof(ChartBaseControl),
            new FrameworkPropertyMetadata(new Range(minimum: 0d, maximum: 1d), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
            );

        public Range RangeX {
            get => HorizontalAxis?.Range;
            set {
                if (HorizontalAxis != null)
                    HorizontalAxis.Range = value;
            }
        }

        [Obsolete("Range infomation move to Axis.")]
        public static readonly DependencyProperty RangeYProperty = DependencyProperty.Register(
            nameof(RangeY), typeof(Range), typeof(ChartBaseControl),
            new FrameworkPropertyMetadata(new Range(minimum: 0d, maximum: 1d), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
            );

        public Range RangeY {
            get => VerticalAxis?.Range;
            set {
                if (VerticalAxis != null)
                    VerticalAxis.Range = value;
            }
        }

        public static readonly DependencyProperty FlippedXProperty =
            DependencyProperty.RegisterAttached(
                nameof(FlippedX), typeof(bool), typeof(ChartBaseControl),
                new FrameworkPropertyMetadata(
                    false,
                    FrameworkPropertyMetadataOptions.Inherits,
                    ChartUpdate));

        public static IAxisManager GetFlippedX(DependencyObject d)
            => (IAxisManager)d.GetValue(FlippedXProperty);

        public static void SetFlippedX(DependencyObject d, IAxisManager value)
            => d.SetValue(FlippedXProperty, value);

        public bool FlippedX {
            get => (bool)GetValue(FlippedXProperty);
            set => SetValue(FlippedXProperty, value);
        }

        public static readonly DependencyProperty FlippedYProperty =
            DependencyProperty.RegisterAttached(
                nameof(FlippedY), typeof(bool), typeof(ChartBaseControl),
                new FrameworkPropertyMetadata(
                    true,
                    FrameworkPropertyMetadataOptions.Inherits,
                    ChartUpdate));

        public static IAxisManager GetFlippedY(DependencyObject d)
            => (IAxisManager)d.GetValue(FlippedYProperty);

        public static void SetFlippedY(DependencyObject d, IAxisManager value)
            => d.SetValue(FlippedYProperty, value);

        public bool FlippedY {
            get => (bool)GetValue(FlippedYProperty);
            set => SetValue(FlippedYProperty, value);
        }

        public Rect InitialArea
        {
            get
            {
                var rangeX = HorizontalAxis?.InitialRange ?? new Range(0, 1);
                var rangeY = VerticalAxis?.InitialRange ?? new Range(0, 1);
                return new Rect(new Point(rangeX.Minimum, rangeY.Minimum), new Point(rangeX.Maximum, rangeY.Maximum));
            }
        }

        #region field
        protected VisualCollection visualChildren;
        #endregion

        public ChartBaseControl()
        {
            visualChildren = new VisualCollection(this);

            MouseLeftButtonDown += ResetChartAreaOnDoubleClick;
            MouseWheel += ZoomOnMouseWheel;
            MouseLeftButtonDown += MoveOnMouseLeftButtonDown;
            MouseLeftButtonUp += MoveOnMouseLeftButtonUp;
            MouseMove += MoveOnMouseMove;
            MouseRightButtonDown += ZoomOnMouseRightButtonDown;
            MouseRightButtonUp += ZoomOnMouseRightButtonUp;
            MouseMove += ZoomOnMouseMove;
        }

        protected virtual void Update() { }

        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);
            Update();
        }

        #region PropertyChanged event
        protected static void ChartUpdate(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement fe) {
                fe.InvalidateVisual();
            }
        }
        #endregion

        Point RenderPositionToValue(Point p)
        {
            double x = 0, y = 0;
            if (HorizontalAxis != null)
                x = HorizontalAxis.TranslateFromRenderPoint(p.X / ActualWidth, FlippedX).Value;
            if (VerticalAxis != null)
                y = VerticalAxis.TranslateFromRenderPoint(p.Y / ActualHeight, FlippedY).Value;
            return new Point(x, y);
        }

        #region Chart update Event 
        private Point zoomInitial, moveCurrent;
        private RubberAdorner adorner;
        private bool moving;

        void ZoomOnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var p = e.GetPosition(this);
            var delta = e.Delta;
            var scale = 1 - 0.1 * Math.Sign(delta);
            var area = Rect.Intersect(
                new Rect(
                    RenderPositionToValue(new Point(p.X * (1 - scale), p.Y * (1 - scale))),
                    RenderPositionToValue(new Point(p.X + (ActualWidth - p.X) * scale, p.Y + (ActualHeight - p.Y) * scale))
                    ),
                InitialArea
                );
            RangeX = new Range(minimum: area.Left, maximum: area.Right);
            RangeY = new Range(minimum: area.Top, maximum: area.Bottom);
        }

        void ZoomOnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            zoomInitial = e.GetPosition(this);
            adorner = new RubberAdorner(this, zoomInitial);
            CaptureMouse();
        }

        void ZoomOnMouseMove(object sender, MouseEventArgs e)
        {
            if (adorner != null)
            {
                var initial = zoomInitial;
                var current = e.GetPosition(this);
                adorner.Offset = current - initial;
            }
        }

        void ZoomOnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (adorner != null)
            {
                ReleaseMouseCapture();
                adorner.Detach();
                adorner = null;

                var current = e.GetPosition(this);
                var area = Rect.Intersect(
                    new Rect(
                        RenderPositionToValue(zoomInitial),
                        RenderPositionToValue(current)
                        ),
                    InitialArea
                    );
                if (area.Width > 0)
                {
                    RangeX = new Range(minimum: area.Left, maximum: area.Right);
                }
                if (area.Height > 0)
                {
                    RangeY = new Range(minimum: area.Top, maximum: area.Bottom);
                }
            }
        }

        void MoveOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            moveCurrent = e.GetPosition(this);
            moving = true;
            CaptureMouse();
        }

        void MoveOnMouseMove(object sender, MouseEventArgs e)
        {
            if (moving)
            {
                var previous = moveCurrent;
                moveCurrent = e.GetPosition(this);
                var area = Rect.Offset(new Rect(RenderSize), previous - moveCurrent);
                var cand = new Rect(RenderPositionToValue(area.TopLeft), RenderPositionToValue(area.BottomRight));
                if (InitialArea.Contains(cand))
                {
                    RangeX = new Range(minimum: cand.Left, maximum: cand.Right);
                    RangeY = new Range(minimum: cand.Top, maximum: cand.Bottom);
                }
            }
        }

        void MoveOnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (moving)
            {
                moving = false;
                ReleaseMouseCapture();
            }
        }

        void ResetChartAreaOnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (HorizontalAxis != null)
                {
                    RangeX = HorizontalAxis.InitialRange;
                }
                if (VerticalAxis != null)
                {
                    RangeY = VerticalAxis.InitialRange;
                }
            }
        }
        #endregion

        #region VisualCollection
        protected override int VisualChildrenCount => visualChildren.Count;
        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || visualChildren.Count <= index)
                throw new ArgumentOutOfRangeException();
            return visualChildren[index];
        }
        #endregion
    }
}
