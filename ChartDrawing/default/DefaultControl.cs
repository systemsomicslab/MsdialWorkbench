using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;

namespace CompMs.Graphics.Core.Base
{
    public class DefaultControl : FrameworkElement
    {
        public IChartData Data
        {
            get => (IChartData)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
            "Data", typeof(IChartData), typeof(DefaultControl),
            new FrameworkPropertyMetadata(new DefaultChartData(), FrameworkPropertyMetadataOptions.AffectsRender) 
        );
        protected IDrawingChart drawingChart;
        protected bool Xfreeze = false;
        protected bool Yfreeze = false;
        

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
            drawingChart.Draw(drawingContext, new Point(0, 0), RenderSize, Data);
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
            base.OnRender(drawingContext);
            var point = new Point(0, 0);
            DrawBackground(drawingContext);
            DrawChart(drawingContext);
            DrawForeground(drawingContext);
        }

        #region mouse event
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
                Data.UpdateGraphRange(new Point(0, 0) - v, (Point)RenderSize - v);
                isMoving = false;
            }
        }
        protected void MoveLeftDragOnMouseMove(object sender, MouseEventArgs e)
        {
            if (isMoving)
            {
                var currentPosition = e.GetPosition(this);
                var v = currentPosition - previousPosition;
                Data.UpdateGraphRange(new Point(0, 0) - v, (Point)RenderSize - v);
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
                Data.UpdateGraphRange(initialPosition, e.GetPosition(this));
            }
            isZooming = false;
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
            var p = e.GetPosition((IInputElement)sender);
            var delta = e.Delta;
            var scale = 1 - 0.1 * Math.Sign(delta);

            var xNextMin = p.X * (1 - scale);
            var xNextMax = p.X + (ActualWidth - p.X) * scale;
            var yNextMin = p.Y * (1 - scale);
            var yNextMax = p.Y + (ActualHeight - p.Y) * scale;

            Data.UpdateGraphRange(new Point(xNextMin, yNextMin), new Point(xNextMax, yNextMax));
        }

        protected virtual void ResetDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                Data.Reset();
            }
        }
        #endregion
    }

    class DefaultDrawingChart : IDrawingChart
    {
        protected static readonly Brush graphBackground = Brushes.WhiteSmoke;
        protected static readonly Pen graphBorder = new Pen(Brushes.Black, 1);
        protected static readonly Brush rubberForeground = new SolidColorBrush(Colors.DarkGray) { Opacity = 0.5 };
        protected static readonly Pen rubberBorder = new Pen(Brushes.DarkGray, 1);

        virtual public void Draw(DrawingContext drawingContext, Point point, Size size, IChartData chartData) { }
        virtual public void DrawBackGround(DrawingContext drawingContext, Point point, Size size)
        {
            var rect = new Rect(point, size);
            drawingContext.PushClip(new RectangleGeometry(rect));
            drawingContext.DrawRectangle(graphBackground, graphBorder, rect);
            drawingContext.Pop();
        }
        virtual public void DrawForeground(DrawingContext drawingContext, Point point, Size size)
        {
            var rect = new Rect(point, size);
            drawingContext.PushClip(new RectangleGeometry(rect));
            drawingContext.DrawRectangle(rubberForeground, rubberBorder, rect);
            drawingContext.Pop();
        }

        void IDrawingChart.Draw(DrawingContext drawingContext, Point point, Size size, IChartData chartData)
            => Draw(drawingContext, point, size, chartData);
        void IDrawingChart.DrawBackground(DrawingContext drawingContext, Point point, Size size)
            => DrawBackGround(drawingContext, point, size);
        void IDrawingChart.DrawForeground(DrawingContext drawingContext, Point point, Size size)
            => DrawForeground(drawingContext, point, size);
    }

    class DefaultChartData : IChartData
    {
        virtual public void Reset() { }
        virtual public void UpdateGraphRange(Point p1, Point p2) { }

        void IChartData.Reset() => Reset();
        void IChartData.UpdateGraphRange(Point p1, Point p2) => UpdateGraphRange(p1, p2);
    }
}
