using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

using CompMs.Graphics.Core.Adorner;
using CompMs.Graphics.Core.Base;


namespace CompMs.Graphics.GraphAxis
{
    public class ContinuousHorizontalAxis : FrameworkElement
    {
        #region DependencyProperty
        public static readonly DependencyProperty MinXProperty = DependencyProperty.Register(
            nameof(MinX), typeof(double), typeof(ContinuousHorizontalAxis),
            new PropertyMetadata(default(double), OnMinXChanged)
            );

        public static readonly DependencyProperty MaxXProperty = DependencyProperty.Register(
            nameof(MaxX), typeof(double), typeof(ContinuousHorizontalAxis),
            new PropertyMetadata(default(double), OnMaxXChanged)
            );

        public static readonly DependencyProperty ChartAreaProperty = DependencyProperty.Register(
            nameof(ChartArea), typeof(Rect), typeof(ContinuousHorizontalAxis),
            new PropertyMetadata(default(Rect), UpdateVisual)
            );

        public static readonly DependencyProperty InitialAreaProperty = DependencyProperty.Register(
            nameof(InitialArea), typeof(Rect), typeof(ContinuousHorizontalAxis),
            new PropertyMetadata(default(Rect))
            );

        public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register(
            nameof(FontSize), typeof(double), typeof(ContinuousHorizontalAxis),
            new PropertyMetadata(14d)
            );

        public static readonly DependencyProperty TicknessProperty = DependencyProperty.Register(
            nameof(Tickness), typeof(double), typeof(ContinuousHorizontalAxis),
            new PropertyMetadata(1d)
            );

        public static readonly DependencyProperty MajorTickSizeProperty = DependencyProperty.Register(
            nameof(MajorTickSize), typeof(double), typeof(ContinuousHorizontalAxis),
            new PropertyMetadata(5d)
            );

        public static readonly DependencyProperty MinorTickSizeProperty = DependencyProperty.Register(
            nameof(MinorTickSize), typeof(double), typeof(ContinuousHorizontalAxis),
            new PropertyMetadata(3d)
            );
        #endregion

        #region Property
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

        public Rect ChartArea
        {
            get => (Rect)GetValue(ChartAreaProperty);
            set => SetValue(ChartAreaProperty, value);
        }

        public Rect InitialArea
        {
            get => (Rect)GetValue(InitialAreaProperty);
            set => SetValue(InitialAreaProperty, value);
        }

        public double FontSize
        {
            get => (double)GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public double Tickness
        {
            get => (double)GetValue(TicknessProperty);
            set => SetValue(TicknessProperty, value);
        }

        public double MajorTickSize
        {
            get => (double)GetValue(MajorTickSizeProperty);
            set => SetValue(MajorTickSizeProperty, value);
        }

        public double MinorTickSize
        {
            get => (double)GetValue(MinorTickSizeProperty);
            set => SetValue(MinorTickSizeProperty, value);
        }

        public decimal TickInterval => (decimal)Math.Pow(10, Math.Floor(Math.Log10(ChartArea.Width)));
        public string LabelFormat
        {
            get
            {
                var exp = Math.Floor(Math.Log10(ChartArea.Right));
                return exp > 3 ? "0.00e0" : exp < 0 ? "0.0e0" : TickInterval >= 1 ? "f0" : "f3";
            }
        }
        #endregion

        #region field
        private VisualCollection visualChildren;
        private ToolTip tooltip;
        private RubberAdorner adorner;
        private Point zoomInitial;
        private Point moveCurrent;
        private bool moving;
        private Brush textBrush = Brushes.Black;
        private Pen tickPen;
        #endregion

        public ContinuousHorizontalAxis()
        {
            visualChildren = new VisualCollection(this);
            tooltip = new ToolTip();
            ToolTip = tooltip;
            ToolTipService.SetInitialShowDelay(this, 0);
            tickPen = new Pen(Brushes.Black, 2);
            tickPen.Freeze();

            SizeChanged += OnSizeChanged;
            MouseWheel += ZoomOnMouseWheel;
            MouseRightButtonDown += ZoomOnMouseRightButtonDown;
            MouseRightButtonUp += ZoomOnMouseRightButtonUp;
            MouseMove += ZoomOnMouseMove;
            MouseLeftButtonDown += MoveOnMouseLeftButtonDown;
            MouseLeftButtonUp += MoveOnMouseLeftButtonUp;
            MouseMove += MoveOnMouseMove;
            MouseLeftButtonDown += ResetOnDoubleClick;
            MouseMove += OnFocusDataPoint;
            MouseLeftButtonDown += SelectDataPointOnClick;
        }

        private void DrawMajorDrawingVisual(DrawingVisualDataPoint dv)
        {
            var x = dv.DataPoint.X;
            var dc = dv.RenderOpen();
            var xx = ConvertValueToRenderPosition(new Point(x, 0)).X;
            dc.DrawLine(tickPen, new Point(xx, 0), new Point(xx, MajorTickSize));
            var maxWidth = (double)TickInterval / ChartArea.Width * RenderSize.Width;
            var formattedText = new FormattedText(
                x.ToString(LabelFormat), CultureInfo.GetCultureInfo("en-us"),
                FlowDirection.LeftToRight, new Typeface("Calibii"), FontSize, textBrush, 1
                )
            {
                MaxTextWidth = maxWidth,
                MaxTextHeight = Math.Max(1, RenderSize.Height * 0.8),
            };
            var width = formattedText.Width;
            dc.DrawText(formattedText,  new Point(xx - width / 2, RenderSize.Height * 0.2));
            dc.Close();
        }

        private void DrawMinorDrawingVisual(DrawingVisualDataPoint dv)
        {
            var x = dv.DataPoint.X;
            dv.Clip = new RectangleGeometry(new Rect(RenderSize));
            var dc = dv.RenderOpen();
            var xx = ConvertValueToRenderPosition(new Point(x, 0)).X;
            dc.DrawLine(tickPen, new Point(xx, 0), new Point(xx, MinorTickSize));
            dc.Close();
        }

        public void UpdateVisualChildren()
        {
            if (TickInterval == 0) return;

            decimal shortTickInterval;
            var fold = (decimal)ChartArea.Width / TickInterval;
            if (fold >= 5) shortTickInterval = TickInterval * (decimal)0.5;
            else if (fold >= 2) shortTickInterval = TickInterval * (decimal)0.25;
            else shortTickInterval = TickInterval * (decimal)0.1;

            for(var i = Math.Ceiling((decimal)ChartArea.Left / TickInterval); i * TickInterval <= (decimal)ChartArea.Right; ++i)
            {
                var dv = new DrawingVisualDataPoint(new DataPoint() { X = (double)(i * TickInterval) });
                DrawMajorDrawingVisual(dv);
                visualChildren.Add(dv);
            }
            if (shortTickInterval == 0) return;
            for(var i = Math.Ceiling((decimal)ChartArea.Left / shortTickInterval); i * shortTickInterval <= (decimal)ChartArea.Right; ++i)
            {
                var dv = new DrawingVisualDataPoint(new DataPoint() { X = (double)(i * shortTickInterval) });
                DrawMinorDrawingVisual(dv);
                visualChildren.Add(dv);
            }
        }

        static void UpdateVisual(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as ContinuousHorizontalAxis;
            if (chart == null) return;

            chart.visualChildren.Clear();
            chart.UpdateVisualChildren();
        }

        static void OnMinXChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as ContinuousHorizontalAxis;
            if (chart == null) return;

            chart.InitialArea = new Rect(new Point((double)e.NewValue, 0), chart.InitialArea.BottomRight);
            chart.ChartArea = chart.InitialArea;
            chart.visualChildren.Clear();
            chart.UpdateVisualChildren();
        }

        static void OnMaxXChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as ContinuousHorizontalAxis;
            if (chart == null) return;

            chart.InitialArea = new Rect(chart.InitialArea.TopLeft, new Point((double)e.NewValue, 0));
            chart.ChartArea = chart.InitialArea;
            chart.visualChildren.Clear();
            chart.UpdateVisualChildren();
        }

        void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            visualChildren.Clear();
            UpdateVisualChildren();
        }

        void ZoomOnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var p = e.GetPosition(this);
            var delta = e.Delta;
            var scale = 1 - 0.1 * Math.Sign(delta);

            var xmin = p.X * (1 - scale);
            var xmax = p.X + (ActualWidth - p.X) * scale;

                ChartArea = Rect.Intersect(
                    new Rect(
                        ConvertRenderPositionToValue(new Point(xmin, 0)),
                        ConvertRenderPositionToValue(new Point(xmax, 0))
                        ),
                    InitialArea
                    );
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
                ChartArea = Rect.Intersect(
                    new Rect(
                        ConvertRenderPositionToValue(zoomInitial),
                        ConvertRenderPositionToValue(e.GetPosition(this))
                        ),
                    InitialArea
                    );
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
                var cand = new Rect(
                        ConvertRenderPositionToValue(area.TopLeft),
                        ConvertRenderPositionToValue(area.BottomRight)
                        );
                if (InitialArea.Contains(cand))
                    ChartArea = cand;
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

        void ResetOnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ChartArea = InitialArea;
            }
        }

        Point ConvertRenderPositionToValue(Point p)
        {
            return new Point(p.X / ActualWidth * ChartArea.Width + ChartArea.Left, 0);
        }

        Point ConvertValueToRenderPosition(Point p)
        {
            return new Point((p.X - ChartArea.Left) / ChartArea.Width * ActualWidth, 0);
        }

        void OnFocusDataPoint(object sender, MouseEventArgs e)
        {
            var pt = e.GetPosition(this);

            tooltip.IsOpen = false;
            tooltip.Content = "";
            VisualTreeHelper.HitTest(this,
                new HitTestFilterCallback(DataPointHitTestFilter),
                new HitTestResultCallback(DataPointTipHitTest),
                new PointHitTestParameters(pt)
                );
        }

        void SelectDataPointOnClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                var pt = e.GetPosition(this);

                VisualTreeHelper.HitTest(this,
                    new HitTestFilterCallback(DataPointHitTestFilter),
                    new HitTestResultCallback(DataPointSelectHitTest),
                    new PointHitTestParameters(pt)
                    );

                }
        }

        HitTestFilterBehavior DataPointHitTestFilter(DependencyObject d)
        {
            if (d is DrawingVisualDataPoint)
                return HitTestFilterBehavior.Continue;
            return HitTestFilterBehavior.ContinueSkipSelf;
        }

        HitTestResultBehavior DataPointTipHitTest(HitTestResult result)
        {
            var focussed = (DrawingVisualDataPoint)result.VisualHit;
            tooltip.Content = focussed.DataPoint.X.ToString();
            tooltip.IsOpen = true;
            return HitTestResultBehavior.Stop;
        }

        HitTestResultBehavior DataPointSelectHitTest(HitTestResult result)
        {
            var focussed = (DrawingVisualDataPoint)result.VisualHit;
            return HitTestResultBehavior.Stop;
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }

        protected override int VisualChildrenCount => visualChildren.Count;
        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || visualChildren.Count <= index)
                throw new ArgumentOutOfRangeException();
            return visualChildren[index];
        }
    }
}
