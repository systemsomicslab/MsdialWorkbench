using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using CompMs.Graphics.Core.Adorner;


namespace CompMs.Graphics.Core.Base
{
    public abstract class ChartBaseControl : FrameworkElement
    {
        #region DependencyProperty
        public static readonly DependencyProperty HorizontalAxisProperty = DependencyProperty.Register(
            nameof(HorizontalAxis), typeof(AxisManager), typeof(ChartBaseControl),
            new PropertyMetadata(default(AxisManager), ChartUpdate)
            );

        public static readonly DependencyProperty VerticalAxisProperty = DependencyProperty.Register(
            nameof(VerticalAxis), typeof(AxisManager), typeof(ChartBaseControl),
            new PropertyMetadata(default(AxisManager), ChartUpdate)
            );

        public static readonly DependencyProperty MinXProperty = DependencyProperty.Register(
            nameof(MinX), typeof(double), typeof(ChartBaseControl),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ChartUpdate)
            );

        public static readonly DependencyProperty MaxXProperty = DependencyProperty.Register(
            nameof(MaxX), typeof(double), typeof(ChartBaseControl),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ChartUpdate)
            );

        public static readonly DependencyProperty MinYProperty = DependencyProperty.Register(
            nameof(MinY), typeof(double), typeof(ChartBaseControl),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ChartUpdate)
            );

        public static readonly DependencyProperty MaxYProperty = DependencyProperty.Register(
            nameof(MaxY), typeof(double), typeof(ChartBaseControl),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ChartUpdate)
            );
        #endregion

        #region Property
        public AxisManager HorizontalAxis
        {
            get => (AxisManager)GetValue(HorizontalAxisProperty);
            set => SetValue(HorizontalAxisProperty, value);
        }

        public AxisManager VerticalAxis
        {
            get => (AxisManager)GetValue(VerticalAxisProperty);
            set => SetValue(VerticalAxisProperty, value);
        }

        public double MinX
        {
            get => (double)GetValue(MinXProperty);
            set => SetValue(MinXProperty, value);
        }

        public double MaxX
        {
            get => (double)GetValue(MaxXProperty);
            set => SetValue(MaxXProperty, value);
        }

        public double MinY
        {
            get => (double)GetValue(MinYProperty);
            set => SetValue(MinYProperty, value);
        }

        public double MaxY
        {
            get => (double)GetValue(MaxYProperty);
            set => SetValue(MaxYProperty, value);
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

        protected virtual void Update() { }

        #region Event handler
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) => Update();

        #region PropertyChanged event
        static void ChartUpdate(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChartBaseControl chart) chart.Update();
        }
        #endregion

        Point RenderPositionToValue(Point p)
        {
            double x = 0, y = 0;
            if (HorizontalAxis != null)
                x = HorizontalAxis.RenderPositionToValue(p.X / ActualWidth);
            if (VerticalAxis != null)
                y = VerticalAxis.RenderPositionToValue(p.Y / ActualHeight);
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
            MinX = area.Left;
            MaxX = area.Right;
            MinY = area.Top;
            MaxY = area.Bottom;
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
                MinX = area.Left;
                MaxX = area.Right;
                MinY = area.Top;
                MaxY = area.Bottom;
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
                    MinX = cand.Left;
                    MaxX = cand.Right;
                    MinY = cand.Top;
                    MaxY = cand.Bottom;
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
                    MinX = HorizontalAxis.InitialMin;
                    MaxX = HorizontalAxis.InitialMax;
                }
                if (VerticalAxis != null)
                {
                    MinY = VerticalAxis.InitialMin;
                    MaxY = VerticalAxis.InitialMax;
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
