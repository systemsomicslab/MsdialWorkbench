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
        public Rect ChartDrawingArea
        {
            get => new Rect(MinX, MaxX, MinY, MaxY);
            set
            {
                MinX = value.Left;
                MaxX = value.Right;
                MinY = value.Top;
                MaxY = value.Bottom;
            }
        }
        public IChartFactory ChartFactory
        {
            get => (IChartFactory)GetValue(ChartFactoryProperty);
            set => SetValue(ChartFactoryProperty, value);
        }
        public static readonly DependencyProperty ChartFactoryProperty = DependencyProperty.Register(
            "ChartFactory", typeof(IChartFactory), typeof(ChartControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender)
            );
        public double MinX
        {
            get => (double)GetValue(MinXProperty);
            set => SetValue(MinXProperty, value);
        }
        public static readonly DependencyProperty MinXProperty = DependencyProperty.Register(
            "MinX", typeof(double), typeof(ChartControl),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender, OnMinXChanged)
            );
        public double MaxX
        {
            get => (double)GetValue(MaxXProperty);
            set => SetValue(MaxXProperty, value);
        }
        public static readonly DependencyProperty MaxXProperty = DependencyProperty.Register(
            "MaxX", typeof(double), typeof(ChartControl),
            new FrameworkPropertyMetadata(100d, FrameworkPropertyMetadataOptions.AffectsRender, OnMaxXChanged)
            );
        public double MinY
        {
            get => (double)GetValue(MinYProperty);
            set => SetValue(MinYProperty, value);
        }
        public static readonly DependencyProperty MinYProperty = DependencyProperty.Register(
            "MinY", typeof(double), typeof(ChartControl),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender, OnMinYChanged)
            );
        public double MaxY
        {
            get => (double)GetValue(MaxYProperty);
            set => SetValue(MaxYProperty, value);
        }
        public static readonly DependencyProperty MaxYProperty = DependencyProperty.Register(
            "MaxY", typeof(double), typeof(ChartControl),
            new FrameworkPropertyMetadata(100d, FrameworkPropertyMetadataOptions.AffectsRender, OnMaxYChanged)
            );

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
                control.ChartFactory.ElementArea = new Rect(new Point(rect.Right, (double)e.NewValue), rect.BottomLeft);
            }
        }

        public ChartControl()
        {
            ChartFactory = new DefaultChartFactory(RenderSize);

            MouseLeftButtonDown += MoveLeftDragOnMouseDown;
            MouseLeftButtonUp += MoveLeftDragOnMouseUp;
            MouseMove += MoveLeftDragOnMouseMove;
            MouseLeave += MoveLeftDragOnMouseLeave;
            MouseRightButtonDown += ZoomRightDragOnMouseDown;
            MouseRightButtonUp += ZoomRightDragOnMouseUp;
            // MouseMove += ZoomRightDragOnMouseMove;
            MouseLeave += ZoomRightDragOnMouseLeave;
            MouseWheel += ZoomMouseWheel;
            MouseLeftButtonDown += ResetDoubleClick;
            SizeChanged += (s, e) => ChartFactory.SizeChanged(e.NewSize);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            drawingContext.DrawDrawing(ChartFactory.CreateChart(RenderSize));
        }

        /*
        protected void Update()
        {
            Source = new DrawingImage(ChartFactory.CreateChart(RenderSize));
        }
        */

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
                ChartDrawingArea = Rect.Offset(ChartDrawingArea, ChartFactory.Move(v));
                isMoving = false;
            }
        }
        protected void MoveLeftDragOnMouseMove(object sender, MouseEventArgs e)
        {
            if (isMoving)
            {
                var currentPosition = e.GetPosition(this);
                var v = currentPosition - previousPosition;
                ChartDrawingArea = Rect.Offset(ChartDrawingArea, ChartFactory.Move(v));
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
            if (isZooming)
            {
                isZooming = false;
                ChartFactory.UpdateRange(new Rect(initialPosition, e.GetPosition(this)));
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
        protected void ZoomRightDragOnMouseLeave(object sender, MouseEventArgs e)
        {
            if (isZooming)
            {
                isZooming = false;
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

            ChartFactory.UpdateRange(new Rect(new Point(xNextMin, yNextMin), new Point(xNextMax, yNextMax)));
        }

        protected virtual void ResetDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ChartFactory.Reset();
            }
        }
        #endregion
    }

    class DefaultChartFactory : IChartFactory
    {
        public Rect ElementArea { get; set; }
        public Transform TransformElement { get; set; } = Transform.Identity;
        IChartFactory chartFactory;

        public DefaultChartFactory(Size size)
        {
            chartFactory = new BackgroundChartFactory(size);
            //chartFactories.Add(new ForegroundChartFactory());
            ElementArea = chartFactory.ElementArea;
        }

        public Drawing CreateChart(Size size)
        {
            var drawingGroup = new DrawingGroup();
            drawingGroup.Children.Add(chartFactory?.CreateChart(size));
            return drawingGroup;
        }
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
    }
}
