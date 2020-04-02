using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;

using Common.DataStructure;

namespace Msdial.Dendrogram
{
    public class DendrogramPlotControl : FrameworkElement
    {
        #region Property
        public Graph Dendrogram
        {
            get => (Graph)GetValue(DendrogramProperty);
            set => SetValue(DendrogramProperty, value);
        }
        public static readonly DependencyProperty DendrogramProperty = DependencyProperty.Register(
            "Dendrogram", typeof(Graph), typeof(DendrogramPlotControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public int Root
        {
            get => (int)GetValue(RootProperty);
            set => SetValue(RootProperty, value);
        }
        public static readonly DependencyProperty RootProperty = DependencyProperty.Register(
            "Root", typeof(int), typeof(DendrogramPlotControl),
            new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public IReadOnlyList<double> XPositions
        {
            get => (IReadOnlyList<double>)GetValue(XPositionsProperty);
            set => SetValue(XPositionsProperty, value);
        }
        public static readonly DependencyProperty XPositionsProperty = DependencyProperty.Register(
            "XPositions", typeof(IReadOnlyList<double>), typeof(DendrogramPlotControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public IReadOnlyList<double> YPositions
        {
            get => (IReadOnlyList<double>)GetValue(YPositionsProperty);
            set => SetValue(YPositionsProperty, value);
        }
        public static readonly DependencyProperty YPositionsProperty = DependencyProperty.Register(
            "YPositions", typeof(IReadOnlyList<double>), typeof(DendrogramPlotControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public double XDisplayMin
        {
            get => (double)GetValue(XDisplayMinProperty);
            set => SetValue(XDisplayMinProperty, value);
        }
        public static readonly DependencyProperty XDisplayMinProperty = DependencyProperty.Register(
            "XDisplayMin", typeof(double), typeof(DendrogramPlotControl),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public double XDisplayMax
        {
            get => (double)GetValue(XDisplayMaxProperty);
            set => SetValue(XDisplayMaxProperty, value);
        }
        public static readonly DependencyProperty XDisplayMaxProperty = DependencyProperty.Register(
            "XDisplayMax", typeof(double), typeof(DendrogramPlotControl),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public double YDisplayMin
        {
            get => (double)GetValue(YDisplayMinProperty);
            set => SetValue(YDisplayMinProperty, value);
        }
        public static readonly DependencyProperty YDisplayMinProperty = DependencyProperty.Register(
            "YDisplayMin", typeof(double), typeof(DendrogramPlotControl),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public double YDisplayMax
        {
            get => (double)GetValue(YDisplayMaxProperty);
            set => SetValue(YDisplayMaxProperty, value);
        }
        public static readonly DependencyProperty YDisplayMaxProperty = DependencyProperty.Register(
            "YDisplayMax", typeof(double), typeof(DendrogramPlotControl),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public IReadOnlyList<int> LeafIdxs
        {
            get => (IReadOnlyList<int>)GetValue(LeafIdxsProperty);
            set => SetValue(LeafIdxsProperty, value);
        }
        public static readonly DependencyProperty LeafIdxsProperty = DependencyProperty.Register(
            "LeafIdxs", typeof(IReadOnlyList<int>), typeof(DendrogramPlotControl),
            new FrameworkPropertyMetadata(new List<int>(), FrameworkPropertyMetadataOptions.AffectsRender)
        );
        #endregion

        public DendrogramPlotControl()
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

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            var point = new Point(0, 0);
            var vector = new Vector(ActualWidth, ActualHeight);
            DendrogramPlotPainter.DrawBackground(
                drawingContext, point, vector
            );
            if(Dendrogram != null)
                DendrogramPlotPainter.DrawTree(
                    drawingContext,
                    Dendrogram, Root,
                    XPositions, YPositions,
                    point, vector,
                    XDisplayMin, XDisplayMax,
                    YDisplayMin, YDisplayMax
                );
            // Console.WriteLine("OnRender called");
            if (isZooming)
            {
                DendrogramPlotPainter.DrawForegraound(
                    drawingContext, initialPosition, currentPosition
                );
            }
        }

        #region mouse event
        private void UpdateGraphRange(Point p, Point q)
        {
            var xFocusMin = Math.Min(p.X, q.X);
            var xFocusMax = Math.Max(p.X, q.X);
            var yFocusMin = ActualHeight - Math.Max(p.Y, q.Y);
            var yFocusMax = ActualHeight - Math.Min(p.Y, q.Y);

            if (xFocusMax - xFocusMin < 5 || yFocusMax - yFocusMin < 5)
                return;

            var xDisplayMin = xFocusMin / ActualWidth * (XDisplayMax - XDisplayMin) + XDisplayMin;
            var xDisplayMax = xFocusMax / ActualWidth * (XDisplayMax - XDisplayMin) + XDisplayMin;
            var yDisplayMin = yFocusMin / ActualHeight * (YDisplayMax - YDisplayMin) + YDisplayMin;
            var yDisplayMax = yFocusMax / ActualHeight * (YDisplayMax - YDisplayMin) + YDisplayMin;
            XDisplayMin = xDisplayMin;
            XDisplayMax = xDisplayMax;
            YDisplayMin = yDisplayMin;
            YDisplayMax = yDisplayMax;
        }

        bool isMoving = false;
        Point previousPosition;
        private void MoveLeftDragOnMouseDown(object sender, MouseButtonEventArgs e)
        {
            isMoving = true;
            previousPosition = e.GetPosition(this);
        }
        private void MoveLeftDragOnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isMoving)
            {
                var currentPosition = e.GetPosition(this);
                var v = previousPosition - currentPosition;
                UpdateGraphRange(new Point(0, 0) + v, new Point(ActualWidth, ActualHeight) + v);
                isMoving = false;
            }
        }
        private void MoveLeftDragOnMouseMove(object sender, MouseEventArgs e)
        {
            if (isMoving)
            {
                var currentPosition = e.GetPosition(this);
                var v = previousPosition - currentPosition;
                UpdateGraphRange(new Point(0, 0) + v, new Point(ActualWidth, ActualHeight) + v);
                previousPosition = currentPosition;
            }
        }
        private void MoveLeftDragOnMouseLeave(object sender, MouseEventArgs e)
        {
            isMoving = false;
        }

        bool isZooming = false;
        Point initialPosition;
        Point currentPosition;
        private void ZoomRightDragOnMouseDown(object sender, MouseButtonEventArgs e)
        {
            isZooming = true;
            initialPosition = e.GetPosition(this);
        }
        private void ZoomRightDragOnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isZooming)
            {
                UpdateGraphRange(initialPosition, e.GetPosition(this));
            }
            isZooming = false;
        }
        private void ZoomRightDragOnMouseMove(object sender, MouseEventArgs e)
        {
            if (isZooming)
            {
                currentPosition = e.GetPosition(this);
                InvalidateVisual();
            }
        }
        private void ZoomRightDragOnMouseLeave(object sender, MouseEventArgs e)
        {
            isZooming = false;
        }

        private void ZoomMouseWheel(object sender, MouseWheelEventArgs e)
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

        private void ResetDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                var xValueMin = XPositions.Min();
                var xValueMax = XPositions.Max();
                var yValueMin = YPositions.Min();
                var yValueMax = YPositions.Max();

                XDisplayMin = xValueMin - (xValueMax - xValueMin) * 0.05;
                XDisplayMax = xValueMax + (xValueMax - xValueMin) * 0.05;
                YDisplayMin = yValueMin;
                YDisplayMax = yValueMax + (yValueMax - yValueMin) * 0.05;
            }
        }
        #endregion
    }
}
