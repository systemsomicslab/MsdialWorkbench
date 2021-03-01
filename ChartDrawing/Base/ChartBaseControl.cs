using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Adorner;


namespace CompMs.Graphics.Core.Base
{
    public abstract class ChartBaseControl : FrameworkElement
    {
        #region DependencyProperty
        public static readonly DependencyProperty HorizontalAxisProperty = DependencyProperty.Register(
            nameof(HorizontalAxis), typeof(AxisMapper), typeof(ChartBaseControl),
            new PropertyMetadata(default(AxisMapper), ChartUpdate));

        public static readonly DependencyProperty VerticalAxisProperty = DependencyProperty.Register(
            nameof(VerticalAxis), typeof(AxisMapper), typeof(ChartBaseControl),
            new PropertyMetadata(default(AxisMapper), ChartUpdate)
            );

        public static readonly DependencyProperty RangeXProperty = DependencyProperty.Register(
            nameof(RangeX), typeof(Range), typeof(ChartBaseControl),
            new FrameworkPropertyMetadata(new Range(minimum: 0d, maximum: 1d), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
            );

        public static readonly DependencyProperty RangeYProperty = DependencyProperty.Register(
            nameof(RangeY), typeof(Range), typeof(ChartBaseControl),
            new FrameworkPropertyMetadata(new Range(minimum: 0d, maximum: 1d), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
            );

        public static readonly DependencyProperty FlippedXProperty = DependencyProperty.Register(
            nameof(FlippedX), typeof(bool), typeof(ChartBaseControl),
            new FrameworkPropertyMetadata(false, ChartUpdate)
            );

        public static readonly DependencyProperty FlippedYProperty = DependencyProperty.Register(
            nameof(FlippedY), typeof(bool), typeof(ChartBaseControl),
            new FrameworkPropertyMetadata(true, ChartUpdate)
            );
        #endregion

        #region Property
        public AxisMapper HorizontalAxis
        {
            get => (AxisMapper)GetValue(HorizontalAxisProperty);
            set => SetValue(HorizontalAxisProperty, value);
        }

        public AxisMapper VerticalAxis
        {
            get => (AxisMapper)GetValue(VerticalAxisProperty);
            set => SetValue(VerticalAxisProperty, value);
        }

        public Range RangeX {
            get => (Range)GetValue(RangeXProperty);
            set => SetValue(RangeXProperty, value);
        }

        public Range RangeY {
            get => (Range)GetValue(RangeYProperty);
            set => SetValue(RangeYProperty, value);
        }

        public bool FlippedX {
            get => (bool)GetValue(FlippedXProperty);
            set => SetValue(FlippedXProperty, value);
        }

        public bool FlippedY {
            get => (bool)GetValue(FlippedYProperty);
            set => SetValue(FlippedYProperty, value);
        }

        public Rect InitialArea
        {
            get
            {
                double minx = 0, miny = 0, maxx = 0, maxy = 0;
                if (HorizontalAxis != null)
                {
                    minx = HorizontalAxis.InitialMin;
                    maxx = HorizontalAxis.InitialMax;
                }
                if (VerticalAxis != null)
                {
                    miny = VerticalAxis.InitialMin;
                    maxy = VerticalAxis.InitialMax;
                }
                return new Rect(new Point(minx, miny), new Point(maxx, maxy));
            }
        }
        #endregion

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

        protected virtual void Update() {
            InvalidateVisual();
        }

        #region Event handler
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) => Update();

        #region PropertyChanged event
        protected static void ChartUpdate(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChartBaseControl chart) chart.Update();
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
            //Console.WriteLine(delta + "\t" + scale);
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
                    RangeX = new Range(minimum: HorizontalAxis.InitialMin, maximum: HorizontalAxis.InitialMax);
                }
                if (VerticalAxis != null)
                {
                    RangeY = new Range(minimum: VerticalAxis.InitialMin, maximum: VerticalAxis.InitialMax);
                }
            }
        }
        #endregion
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
