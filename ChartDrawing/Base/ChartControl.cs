using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace CompMs.Graphics.Core.Base
{
    public class ChartControl : FrameworkElement
    {
        /*
        public Object ChartData
        {
            get => (Object)GetValue(ChartDataProperty);
            set => SetValue(ChartDataProperty, value);
        }
        public static readonly DependencyProperty ChartDataProperty = DependencyProperty.Register(
            "ChartData", typeof(Object), typeof(ChartControl),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.AffectsRender |
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
        );
        */
        public IChartManager ChartManager
        {
            get => (IChartManager)GetValue(ChartManagerProperty);
            set => SetValue(ChartManagerProperty, value);
        }
        public static readonly DependencyProperty ChartManagerProperty = DependencyProperty.Register(
            nameof(ChartManager), typeof(IChartManager), typeof(ChartControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender)
            );
        public Rect ChartDrawingArea
        {
            get => new Rect(MinX, MinY, Math.Max(0, MaxX - MinX), Math.Max(0, MaxY - MinY));
            set
            {
                MinX = value.Left;
                MaxX = value.Right;
                MinY = value.Top;
                MaxY = value.Bottom;
            }
        }
        public double MinX
        {
            get => (double)GetValue(MinXProperty);
            set => SetValue(MinXProperty, value);
        }
        public static readonly DependencyProperty MinXProperty = DependencyProperty.Register(
            nameof(MinX), typeof(double), typeof(ChartControl),
            new FrameworkPropertyMetadata(0d,
                FrameworkPropertyMetadataOptions.AffectsRender |
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault//, OnMinXChanged
                )
            );
        public double MaxX
        {
            get => (double)GetValue(MaxXProperty);
            set => SetValue(MaxXProperty, value);
        }
        public static readonly DependencyProperty MaxXProperty = DependencyProperty.Register(
            nameof(MaxX), typeof(double), typeof(ChartControl),
            new FrameworkPropertyMetadata(100d,
                FrameworkPropertyMetadataOptions.AffectsRender |
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault//, OnMaxXChanged
                )
            );
        public double MinY
        {
            get => (double)GetValue(MinYProperty);
            set => SetValue(MinYProperty, value);
        }
        public static readonly DependencyProperty MinYProperty = DependencyProperty.Register(
            nameof(MinY), typeof(double), typeof(ChartControl),
            new FrameworkPropertyMetadata(0d,
                FrameworkPropertyMetadataOptions.AffectsRender |
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault//, OnMinYChanged
                )
            );
        public double MaxY
        {
            get => (double)GetValue(MaxYProperty);
            set => SetValue(MaxYProperty, value);
        }
        public static readonly DependencyProperty MaxYProperty = DependencyProperty.Register(
            nameof(MaxY), typeof(double), typeof(ChartControl),
            new FrameworkPropertyMetadata(100d,
                FrameworkPropertyMetadataOptions.AffectsRender |
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault//, OnMaxYChanged
                )
            );

        public Rect LimitDrawingArea
        {
            get => new Rect(LimitMinX, LimitMinY, Math.Max(0, LimitMaxX - LimitMinX), Math.Max(0, LimitMaxY - LimitMinY));
            set
            {
                LimitMinX = value.Left;
                LimitMaxX = value.Right;
                LimitMinY = value.Top;
                LimitMaxY = value.Bottom;
            }
        }
        public double LimitMinX
        {
            get => (double)GetValue(LimitMinXProperty);
            set => SetValue(LimitMinXProperty, value);
        }
        public static readonly DependencyProperty LimitMinXProperty = DependencyProperty.Register(
            nameof(LimitMinX), typeof(double), typeof(ChartControl),
            new FrameworkPropertyMetadata(0d,
                FrameworkPropertyMetadataOptions.AffectsRender
                )
            );
        public double LimitMaxX
        {
            get => (double)GetValue(LimitMaxXProperty);
            set => SetValue(LimitMaxXProperty, value);
        }
        public static readonly DependencyProperty LimitMaxXProperty = DependencyProperty.Register(
            nameof(LimitMaxX), typeof(double), typeof(ChartControl),
            new FrameworkPropertyMetadata(100d,
                FrameworkPropertyMetadataOptions.AffectsRender
                )
            );
        public double LimitMinY
        {
            get => (double)GetValue(LimitMinYProperty);
            set => SetValue(LimitMinYProperty, value);
        }
        public static readonly DependencyProperty LimitMinYProperty = DependencyProperty.Register(
            nameof(LimitMinY), typeof(double), typeof(ChartControl),
            new FrameworkPropertyMetadata(0d,
                FrameworkPropertyMetadataOptions.AffectsRender
                )
            );
        public double LimitMaxY
        {
            get => (double)GetValue(LimitMaxYProperty);
            set => SetValue(LimitMaxYProperty, value);
        }
        public static readonly DependencyProperty LimitMaxYProperty = DependencyProperty.Register(
            nameof(LimitMaxY), typeof(double), typeof(ChartControl),
            new FrameworkPropertyMetadata(100d,
                FrameworkPropertyMetadataOptions.AffectsRender
                )
            );
        /*
        protected static void OnChartDrawingAreaChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var control = sender as ChartControl;
            control?.ChartFactory?.UpdateRange((Rect)e.NewValue);
        }

        protected static void OnMinXChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var control = sender as ChartControl;
            if (control != null && control.ChartFactory != null)
            {
                var rect = control.ChartFactory.ElementArea;
                control.ChartFactory.ElementArea = new Rect(new Point((double)e.NewValue, rect.Top), rect.BottomRight);
            }
        }

        protected static void OnMaxXChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var control = sender as ChartControl;
            if (control != null && control.ChartFactory != null)
            {
                var rect = control.ChartFactory.ElementArea;
                control.ChartFactory.ElementArea = new Rect(new Point((double)e.NewValue, rect.Top), rect.BottomLeft);
            }
        }

        protected static void OnMinYChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var control = sender as ChartControl;
            if (control != null && control.ChartFactory != null)
            {
                var rect = control.ChartFactory.ElementArea;
                control.ChartFactory.ElementArea = new Rect(new Point(rect.Left, (double)e.NewValue), rect.BottomRight);
            }
        }

        protected static void OnMaxYChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var control = sender as ChartControl;
            if (control != null && control.ChartFactory != null)
            {
                var rect = control.ChartFactory.ElementArea;
                control.ChartFactory.ElementArea = new Rect(new Point(rect.Right, (double)e.NewValue), rect.TopLeft);
            }
        }
        */

        protected BackgroundManager background = new BackgroundManager();

        public ChartControl()
        {

            MouseLeftButtonDown += MoveLeftDragOnMouseDown;
            MouseLeftButtonUp += MoveLeftDragOnMouseUp;
            MouseMove += MoveLeftDragOnMouseMove;
            MouseLeave += MoveLeftDragOnMouseLeave;
            MouseRightButtonDown += ZoomRightDragOnMouseDown;
            MouseRightButtonUp += ZoomRightDragOnMouseUp;
            // MouseMove += ZoomRightDragOnMouseMove;
            // MouseLeave += ZoomRightDragOnMouseLeave;
            MouseWheel += ZoomMouseWheel;
            MouseLeftButtonDown += ResetDoubleClick;
            // SizeChanged += OnSizeChanged;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            var drawing = new DrawingGroup();
            Drawing back, chart;
            if (background != null)
            {
                back = background.CreateChart(ChartDrawingArea, RenderSize);
                drawing.Children.Add(back);
            }
            if (ChartManager != null)
            {
                // chart = ChartManager.CreateChart(ChartDrawingArea, RenderSize);
                chart = ChartManager.CreateChart(ChartDrawingArea, RenderSize);
                drawing.Children.Add(chart);
            }
            drawing.ClipGeometry = new RectangleGeometry(new Rect(RenderSize));
            drawingContext.DrawDrawing(drawing);
        }

        /*
        protected void Update()
        {
            Source = new DrawingImage(ChartFactory.CreateChart(RenderSize));
        }
        */

        #region mouse event
        protected bool isMoving = false;
        // private Point previousPosition;
        public Point previousPosition
        {
            get => (Point)GetValue(PreviousPositionProperty);
            set => SetValue(PreviousPositionProperty, value);
        }
        static readonly DependencyProperty PreviousPositionProperty = DependencyProperty.Register(
            nameof(previousPosition), typeof(Point), typeof(ChartControl));
        protected void MoveLeftDragOnMouseDown(object sender, MouseButtonEventArgs e)
        {
            isMoving = true;
            previousPosition = e.GetPosition(this);
        }
        protected void MoveLeftDragOnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isMoving && ChartManager != null)
            {
                var currentPosition = e.GetPosition(this);
                var v = currentPosition - previousPosition;
                var suggested = Rect.Offset(ChartDrawingArea, ChartManager.Translate(-v, ChartDrawingArea, RenderSize));
                if (LimitDrawingArea.Contains(suggested))
                    ChartDrawingArea = suggested;
                isMoving = false;
            }
        }
        protected void MoveLeftDragOnMouseMove(object sender, MouseEventArgs e)
        {
            if (isMoving && ChartManager != null)
            {
                var currentPosition = e.GetPosition(this);
                var v = currentPosition - previousPosition;
                var suggested = Rect.Offset(ChartDrawingArea, ChartManager.Translate(-v, ChartDrawingArea, RenderSize));
                if (LimitDrawingArea.Contains(suggested))
                    ChartDrawingArea = suggested;
                previousPosition = currentPosition;
            }
        }
        protected void MoveLeftDragOnMouseLeave(object sender, MouseEventArgs e)
        {
            isMoving = false;
        }

        protected bool isZooming = false;
        private Point initialPosition;
        protected void ZoomRightDragOnMouseDown(object sender, MouseButtonEventArgs e)
        {
            isZooming = true;
            initialPosition = e.GetPosition(this);
        }
        protected void ZoomRightDragOnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isZooming && ChartManager != null)
            {
                isZooming = false;
                ChartDrawingArea = Rect.Intersect(
                    new Rect(
                        ChartManager.Translate(initialPosition, ChartDrawingArea, RenderSize),
                        ChartManager.Translate(e.GetPosition(this), ChartDrawingArea, RenderSize)
                        ),
                    LimitDrawingArea
                    );
            }
        }
        /*
        protected void ZoomRightDragOnMouseMove(object sender, MouseEventArgs e)
        {
            if (isZooming)
            {
                currentPosition = e.GetPosition(this);
            }
        }
        */
        /*
        protected void ZoomRightDragOnMouseLeave(object sender, MouseEventArgs e)
        {
            if (isZooming)
            {
                isZooming = false;
            }
        }
        */

        protected void ZoomMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (ChartManager != null)
            {
                var p = e.GetPosition(this);
                var delta = e.Delta;
                var scale = 1 - 0.1 * Math.Sign(delta);

                var xNextMin = p.X * (1 - scale);
                var xNextMax = p.X + (ActualWidth - p.X) * scale;
                var yNextMin = p.Y * (1 - scale);
                var yNextMax = p.Y + (ActualHeight - p.Y) * scale;

                ChartDrawingArea = Rect.Intersect(
                    ChartManager.Translate(
                        new Rect(xNextMin, yNextMin, xNextMax - xNextMin, yNextMax - yNextMin),
                        ChartDrawingArea, RenderSize
                        ),
                    LimitDrawingArea
                    );

            }
        }

        protected virtual void ResetDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && ChartManager != null)
            {
                ChartDrawingArea = LimitDrawingArea;
            }
        }

        /*
        protected virtual void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ChartManager?.SizeChanged(e.NewSize);
        }
        */
        #endregion
    }

    class DefaultChartFactory : IChartManager
    {
        // public Rect ElementArea { get; set; }
        // public Transform TransformElement { get; set; } = Transform.Identity;
        IChartManager chartManager;

        public Rect ChartArea { get; }

        public DefaultChartFactory()
        {
            chartManager = new BackgroundManager();
            ChartArea = chartManager.ChartArea;
            //chartFactories.Add(new ForegroundChartFactory());
            // ElementArea = chartFactory.ElementArea;
        }

        public Drawing CreateChart(Rect rect, Size size)
        {
            var drawingGroup = new DrawingGroup();
            drawingGroup.Children.Add(chartManager.CreateChart(rect, size));
            return drawingGroup;
        }

        public Point Translate(Point point, Rect area, Size size)
        {
            return chartManager.Translate(point, area, size);
        }
        public Vector Translate(Vector vector, Rect area, Size size)
        {
            return chartManager.Translate(vector, area, size);
        }
        public Rect Translate(Rect rect, Rect area, Size size)
        {
            return chartManager.Translate(rect, area, size);
        }

        /*
        public Vector Move(Vector vector)
        {
             var vec = chartFactory.Move(vector);
            ElementArea.Offset(vec);
            return vec;
        }
        public Rect UpdateRange(Rect rect)
        {
            ElementArea = chartFactory.UpdateRange(rect);
            return ElementArea;
        }
        public Rect Reset()
        {
            ElementArea = chartFactory.Reset();
            return ElementArea;
        }
        public void SizeChanged(Size size) => chartFactory.SizeChanged(size);
        */
    }
}
