using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;

namespace CompMs.Graphics.Core.Base
{
    public class DefaultControl : FrameworkElement
    {
        public IChartData ChartData
        {
            get => (IChartData)GetValue(ChartDataProperty);
            set => SetValue(ChartDataProperty, value);
        }
        public static readonly DependencyProperty ChartDataProperty = DependencyProperty.Register(
            "ChartData", typeof(IChartData), typeof(DefaultControl),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.AffectsRender |
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                new PropertyChangedCallback(OnDependencyPropertyChanged))
        );
        protected IDrawingChart drawingChart;
        protected Matrix ToRelativePosition => new Matrix(1 / ActualWidth, 0, 0, 1 / ActualHeight, 0, 0);
        protected bool Xfreeze = false;
        protected bool Yfreeze = false;
        
        private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // (d as FrameworkElement)?.InvalidateVisual();
            Console.WriteLine("Dependency property changed.");
        }

        public DefaultControl()
        {
            drawingChart = new DefaultDrawingChart();

            MouseLeftButtonDown += MoveLeftDragOnMouseDown;
            MouseLeftButtonUp += MoveLeftDragOnMouseUp;
            MouseMove += MoveLeftDragOnMouseMove;
            MouseLeave += MoveLeftDragOnMouseLeave;
            MouseRightButtonDown += ZoomRightDragOnMouseDown;
            MouseRightButtonUp += ZoomRightDragOnMouseUp;
            MouseMove += ZoomRightDragOnMouseMove;
            MouseLeave += ZoomRightDragOnMouseLeave;
            MouseWheel += ZoomMouseWheel;
            MouseLeftButtonDown += ResetDoubleClick;
        }

        protected virtual void DrawChart(DrawingContext drawingContext)
        {
            drawingChart.Draw(drawingContext, new Point(0, 0), RenderSize, ChartData);
        }
        protected virtual void DrawBackground(DrawingContext drawingContext)
        {
            drawingChart.DrawBackground(drawingContext, new Point(0, 0), RenderSize);
        }
        protected virtual void DrawForeground(DrawingContext drawingContext)
        {
            if (isZooming && (!Xfreeze || !Yfreeze))
            {
                var inip = initialPosition;
                var curp = currentPosition;
                if (Xfreeze)
                {
                    inip.X = 0;
                    curp.X = ActualWidth;
                }
                if (Yfreeze)
                {
                    inip.Y = 0;
                    curp.Y = ActualHeight;
                }
                drawingChart.DrawForeground(
                    drawingContext,
                    new Point(Math.Min(inip.X, curp.X), Math.Min(inip.Y, curp.Y)),
                    new Size(Math.Abs(inip.X - curp.X), Math.Abs(inip.Y - curp.Y))
                );
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            Console.WriteLine("Rendered.");
            base.OnRender(drawingContext);
            var point = new Point(0, 0);
            DrawBackground(drawingContext);
            DrawChart(drawingContext);
            DrawForeground(drawingContext);
        }

        #region mouse event
        void UpdateGraphRange(Point p1, Point p2)
        {
            var mat = ToRelativePosition;
            ChartData?.UpdateGraphRange(p1 * mat, p2 * mat);
        }
        protected bool isMoving = false;
        private Point previousPosition;
        protected void MoveLeftDragOnMouseDown(object sender, MouseButtonEventArgs e)
        {
            isMoving = true;
            previousPosition = e.GetPosition(this);
        }
        protected void MoveLeftDragOnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isMoving)
            {
                var currentPosition = e.GetPosition(this);
                var v = currentPosition - previousPosition;
                UpdateGraphRange(new Point(0, 0) - v, (Point)RenderSize - v);
                isMoving = false;
            }
        }
        protected void MoveLeftDragOnMouseMove(object sender, MouseEventArgs e)
        {
            if (isMoving)
            {
                var currentPosition = e.GetPosition(this);
                var v = currentPosition - previousPosition;
                UpdateGraphRange(new Point(0, 0) - v, (Point)RenderSize - v);
                previousPosition = currentPosition;
            }
        }
        protected void MoveLeftDragOnMouseLeave(object sender, MouseEventArgs e)
        {
            isMoving = false;
        }

        protected bool isZooming = false;
        private Point initialPosition;
        private Point currentPosition;
        protected void ZoomRightDragOnMouseDown(object sender, MouseButtonEventArgs e)
        {
            isZooming = true;
            initialPosition = e.GetPosition(this);
        }
        protected void ZoomRightDragOnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isZooming)
            {
                isZooming = false;
                UpdateGraphRange(initialPosition, e.GetPosition(this));
            }
        }
        protected void ZoomRightDragOnMouseMove(object sender, MouseEventArgs e)
        {
            if (isZooming)
            {
                currentPosition = e.GetPosition(this);
                InvalidateVisual();
            }
        }
        protected void ZoomRightDragOnMouseLeave(object sender, MouseEventArgs e)
        {
            if (isZooming)
            {
                isZooming = false;
                InvalidateVisual();
            }
        }

        protected void ZoomMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var p = e.GetPosition(this);
            var delta = e.Delta;
            var scale = 1 - 0.1 * Math.Sign(delta);

            var xNextMin = p.X * (1 - scale);
            var xNextMax = p.X + (ActualWidth - p.X) * scale;
            var yNextMin = p.Y * (1 - scale);
            var yNextMax = p.Y + (ActualHeight - p.Y) * scale;

            UpdateGraphRange(new Point(xNextMin, yNextMin), new Point(xNextMax, yNextMax));
        }

        protected virtual void ResetDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ChartData?.Reset();
            }
        }
        #endregion
    }
}
