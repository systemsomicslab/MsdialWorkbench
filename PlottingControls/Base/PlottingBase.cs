using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;

namespace PlottingControls.Base
{
    public class PlottingBase : FrameworkElement
    {
        #region Property
        public IReadOnlyList<double> XPositions
        {
            get => (IReadOnlyList<double>)GetValue(XPositionsProperty);
            set => SetValue(XPositionsProperty, value);
        }
        public static readonly DependencyProperty XPositionsProperty = DependencyProperty.Register(
            "XPositions", typeof(IReadOnlyList<double>), typeof(PlottingBase),
            new FrameworkPropertyMetadata(new double[] { }, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public IReadOnlyList<double> YPositions
        {
            get => (IReadOnlyList<double>)GetValue(YPositionsProperty);
            set => SetValue(YPositionsProperty, value);
        }
        public static readonly DependencyProperty YPositionsProperty = DependencyProperty.Register(
            "YPositions", typeof(IReadOnlyList<double>), typeof(PlottingBase),
            new FrameworkPropertyMetadata(new double[] { }, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public IReadOnlyList<double> ZPositions
        {
            get => (IReadOnlyList<double>)GetValue(ZPositionsProperty);
            set => SetValue(ZPositionsProperty, value);
        }
        public static readonly DependencyProperty ZPositionsProperty = DependencyProperty.Register(
            "ZPositions", typeof(IReadOnlyList<double>), typeof(PlottingBase),
            new FrameworkPropertyMetadata(new double[] { }, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public double XDisplayMin
        {
            get => (double)GetValue(XDisplayMinProperty);
            set => SetValue(XDisplayMinProperty, value);
        }
        public static readonly DependencyProperty XDisplayMinProperty = DependencyProperty.Register(
            "XDisplayMin", typeof(double), typeof(PlottingBase),
            new FrameworkPropertyMetadata(0d,
                FrameworkPropertyMetadataOptions.AffectsRender |
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
        );
        public double XDisplayMax
        {
            get => (double)GetValue(XDisplayMaxProperty);
            set => SetValue(XDisplayMaxProperty, value);
        }
        public static readonly DependencyProperty XDisplayMaxProperty = DependencyProperty.Register(
            "XDisplayMax", typeof(double), typeof(PlottingBase),
            new FrameworkPropertyMetadata(10d,
                FrameworkPropertyMetadataOptions.AffectsRender |
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
        );
        public double YDisplayMin
        {
            get => (double)GetValue(YDisplayMinProperty);
            set => SetValue(YDisplayMinProperty, value);
        }
        public static readonly DependencyProperty YDisplayMinProperty = DependencyProperty.Register(
            "YDisplayMin", typeof(double), typeof(PlottingBase),
            new FrameworkPropertyMetadata(0d,
                FrameworkPropertyMetadataOptions.AffectsRender |
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
        );
        public double YDisplayMax
        {
            get => (double)GetValue(YDisplayMaxProperty);
            set => SetValue(YDisplayMaxProperty, value);
        }
        public static readonly DependencyProperty YDisplayMaxProperty = DependencyProperty.Register(
            "YDisplayMax", typeof(double), typeof(PlottingBase),
            new FrameworkPropertyMetadata(10d,
                FrameworkPropertyMetadataOptions.AffectsRender |
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
        );
        public double ZDisplayMin
        {
            get => (double)GetValue(ZDisplayMinProperty);
            set => SetValue(ZDisplayMinProperty, value);
        }
        public static readonly DependencyProperty ZDisplayMinProperty = DependencyProperty.Register(
            "ZDisplayMin", typeof(double), typeof(PlottingBase),
            new FrameworkPropertyMetadata(0d,
                FrameworkPropertyMetadataOptions.AffectsRender |
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
        );
        public double ZDisplayMax
        {
            get => (double)GetValue(ZDisplayMaxProperty);
            set => SetValue(ZDisplayMaxProperty, value);
        }
        public static readonly DependencyProperty ZDisplayMaxProperty = DependencyProperty.Register(
            "ZDisplayMax", typeof(double), typeof(PlottingBase),
            new FrameworkPropertyMetadata(10d,
                FrameworkPropertyMetadataOptions.AffectsRender |
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
        );
        #endregion

        public PlottingBase()
        {
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

        protected virtual void DrawChart(DrawingContext drawingContext) { }
        protected virtual void DrawBackground(DrawingContext drawingContext, Point p, Vector v)
        {
            PlottingBasePainter.DrawBackground(drawingContext, p, v);
        }
        protected virtual void DrawForeground(DrawingContext drawingContext, Point p, Vector v)
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
                PlottingBasePainter.DrawForegraound(
                    drawingContext, inip, curp
                );
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            var point = new Point(0, 0);
            var vector = new Vector(ActualWidth, ActualHeight);
            DrawBackground(drawingContext, point, vector);
            DrawChart(drawingContext);
            DrawForeground(drawingContext, point, vector);
        }

        #region mouse event
        protected bool Xfreeze = false;
        protected bool Yfreeze = false;
        private void UpdateGraphRange(Point p, Point q)
        {
            if (!Xfreeze) UpdateGraphRangeX(p.X, q.X);
            if (!Yfreeze) UpdateGraphRangeY(p.Y, q.Y);
        }

        private void UpdateGraphRangeX(double p, double q)
        {
            var xFocusMin = Math.Min(p, q);
            var xFocusMax = Math.Max(p, q);

            if (xFocusMax - xFocusMin < 5)
                return;

            var xDisplayMin = xFocusMin / ActualWidth * (XDisplayMax - XDisplayMin) + XDisplayMin;
            var xDisplayMax = xFocusMax / ActualWidth * (XDisplayMax - XDisplayMin) + XDisplayMin;
            XDisplayMin = xDisplayMin;
            XDisplayMax = xDisplayMax;
            if (XDisplayMin == XDisplayMax)
            {
                XDisplayMin -= 1;
                XDisplayMax += 1;
            }
        }

        protected void UpdateGraphRangeY(double p, double q)
        {
            var yFocusMin = ActualHeight - Math.Max(p, q);
            var yFocusMax = ActualHeight - Math.Min(p, q);

            if (yFocusMax - yFocusMin < 5)
                return;

            var yDisplayMin = yFocusMin / ActualHeight * (YDisplayMax - YDisplayMin) + YDisplayMin;
            var yDisplayMax = yFocusMax / ActualHeight * (YDisplayMax - YDisplayMin) + YDisplayMin;
            YDisplayMin = yDisplayMin;
            YDisplayMax = yDisplayMax;
            if (YDisplayMin == YDisplayMax)
            {
                YDisplayMin -= 1;
                YDisplayMax += 1;
            }
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
                var v = previousPosition - currentPosition;
                UpdateGraphRange(new Point(0, 0) + v, new Point(ActualWidth, ActualHeight) + v);
                isMoving = false;
            }
        }
        protected void MoveLeftDragOnMouseMove(object sender, MouseEventArgs e)
        {
            if (isMoving)
            {
                var currentPosition = e.GetPosition(this);
                var v = previousPosition - currentPosition;
                UpdateGraphRange(new Point(0, 0) + v, new Point(ActualWidth, ActualHeight) + v);
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
                UpdateGraphRange(initialPosition, e.GetPosition(this));
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

            var xNextMin = p.X *(1 - scale);
            var xNextMax = p.X + (ActualWidth - p.X) * scale;
            var yNextMin = p.Y * (1 - scale);
            var yNextMax = p.Y + (ActualHeight - p.Y) * scale;

            UpdateGraphRange(new Point(xNextMin, yNextMin), new Point(xNextMax, yNextMax));
        }

        protected virtual void ResetDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (!Xfreeze)
                {
                    XDisplayMin = XPositions.Min();
                    XDisplayMax = XPositions.Max();
                }

                if (!Yfreeze)
                {
                    YDisplayMin = YPositions.Min();
                    YDisplayMax = YPositions.Max();
                }
            }
        }
        #endregion
    }
}
