using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CompMs.Graphics.Behavior;
using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Chart
{
    public class VerticalAxisControl : ChartBaseControl
    {
        #region DependencyProperty
        public static readonly DependencyProperty TickPenProperty = DependencyProperty.Register(
            nameof(TickPen), typeof(Pen), typeof(VerticalAxisControl),
            new PropertyMetadata(new Pen(Brushes.Black, 1), ChartUpdate)
            );

        public static readonly DependencyProperty LabelBrushProperty = DependencyProperty.Register(
            nameof(LabelBrush), typeof(Brush), typeof(VerticalAxisControl),
            new PropertyMetadata(Brushes.Black, ChartUpdate)
            );

        public static readonly DependencyProperty LabelSizeProperty = DependencyProperty.Register(
            nameof(LabelSize), typeof(double), typeof(VerticalAxisControl),
            new PropertyMetadata(12d, ChartUpdate)
            );

        public static readonly DependencyProperty DisplayPropertyNameProperty = DependencyProperty.Register(
            nameof(DisplayPropertyName), typeof(string), typeof(VerticalAxisControl),
            new PropertyMetadata(null, OnDisplayPropertyNameChanged)
            );

        public static readonly DependencyProperty FocusedItemProperty = DependencyProperty.Register(
            nameof(FocusedItem), typeof(object), typeof(VerticalAxisControl),
            new PropertyMetadata(default(object))
            );

        public static readonly DependencyProperty FocusedPointProperty = DependencyProperty.Register(
            nameof(FocusedPoint), typeof(Point), typeof(VerticalAxisControl),
            new PropertyMetadata(default(Point))
            );
        #endregion

        #region Property
        public Pen TickPen {
            get => (Pen)GetValue(TickPenProperty);
            set => SetValue(TickPenProperty, value);
        }

        public Brush LabelBrush {
            get => (Brush)GetValue(LabelBrushProperty);
            set => SetValue(LabelBrushProperty, value);
        }

        public double LabelSize {
            get => (double)GetValue(LabelSizeProperty);
            set => SetValue(LabelSizeProperty, value);
        }

        public string DisplayPropertyName {
            get => (string)GetValue(DisplayPropertyNameProperty);
            set => SetValue(DisplayPropertyNameProperty, value);
        }

        public object FocusedItem {
            get => GetValue(FocusedItemProperty);
            set => SetValue(FocusedItemProperty, value);
        }

        public Point FocusedPoint {
            get => (Point)GetValue(FocusedPointProperty);
            set => SetValue(FocusedPointProperty, value);
        }

        public double ShortTickSize { get; set; } = 3;
        public double LongTickSize { get; set; } = 5;
        #endregion

        #region Field
        private PropertyInfo dPropertyReflection;
        #endregion

        public VerticalAxisControl() {
            MouseMove += VisualFocusOnMouseOver;
            ZoomByDragBehavior.SetIsEnabled(this, true);
            ZoomByDragBehavior.SetStrechHorizontal(this, true);
            ZoomByWheelBehavior.SetIsEnabled(this, true);
            MoveByDragBehavior.SetIsEnabled(this, true);
            ResetRangeByDoubleClickBehavior.SetIsEnabled(this, true);
        }

        protected override void Update() {
            if (VerticalAxis == null
                || TickPen == null
                || LabelBrush == null
                ) return;

            Func<LabelTickData, string> toLabel = null;

            visualChildren.Clear();

            double actualWidth = ActualWidth, actualHeight = ActualHeight;
            double basePoint = VerticalAxis.TranslateToRenderPoint(0d, FlippedY, actualHeight);

            var labelTicks = VerticalAxis
                .GetLabelTicks()
                .Where(data => RangeY.Minimum <= data.Center && data.Center <= RangeY.Maximum)
                .ToList();
            if (labelTicks.Count > 100)
                labelTicks = labelTicks.Where(data => data.TickType == TickType.LongTick).ToList();
            if (labelTicks.Count > 100) {
                var m = (labelTicks.Count + 100 - 1) / 100;
                labelTicks = labelTicks.Where((_, index) => index % m == 0).ToList();
            }

            foreach (var data in labelTicks) {
                if (dPropertyReflection == null && DisplayPropertyName != null)
                    dPropertyReflection = data.Source.GetType().GetProperty(DisplayPropertyName);

                if (toLabel == null) {
                    if (dPropertyReflection == null)
                        toLabel = o => o.Label;
                    else
                        toLabel = o => dPropertyReflection.GetValue(o.Source).ToString();
                }

                var center = VerticalAxis.TranslateToRenderPoint(data.Center, FlippedY, actualHeight);

                var dv = new AnnotatedDrawingVisual(data.Source) { Center = new Point(actualWidth / 2, center) };
                // dv.Clip = new RectangleGeometry(new Rect(RenderSize));
                var dc = dv.RenderOpen();

                switch (data.TickType) {
                    case TickType.LongTick:
                        dc.DrawLine(TickPen, new Point(actualWidth, center), new Point(actualWidth - LongTickSize, center));
                        var maxHeight = VerticalAxis.TranslateToRenderPoint(data.Width, FlippedY, actualHeight) - basePoint;
                        var formattedText = new FormattedText(
                            toLabel(data), CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight, new Typeface("Calibri"),
                            LabelSize, LabelBrush, 1)
                        {
                            MaxTextWidth = Math.Max(1, actualWidth - LongTickSize),
                            MaxTextHeight = Math.Max(1, Math.Min(actualHeight, Math.Abs(maxHeight))),
                        };
                        dc.DrawText(formattedText, new Point(actualWidth - LongTickSize - formattedText.Width, center - formattedText.Height / 2));
                        break;
                    case TickType.ShortTick:
                        dc.DrawLine(TickPen, new Point(actualWidth, center), new Point(actualWidth - ShortTickSize, center));
                        break;
                }
                dc.Close();
                visualChildren.Add(dv);
            }
        }

        #region Event handler
        static void OnDisplayPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is VerticalAxisControl chart) {
                chart.dPropertyReflection = null;
                chart.Update();
            }
        }
        #endregion

        #region Mouse event
        void VisualFocusOnMouseOver(object sender, MouseEventArgs e) {
            var pt = e.GetPosition(this);

            VisualTreeHelper.HitTest(this,
                new HitTestFilterCallback(VisualHitTestFilter),
                new HitTestResultCallback(VisualFocusHitTest),
                new PointHitTestParameters(pt)
                );
        }

        HitTestFilterBehavior VisualHitTestFilter(DependencyObject d) {
            if (d is AnnotatedDrawingVisual)
                return HitTestFilterBehavior.Continue;
            return HitTestFilterBehavior.ContinueSkipSelf;
        }

        HitTestResultBehavior VisualFocusHitTest(HitTestResult result) {
            var dv = (AnnotatedDrawingVisual)result.VisualHit;
            var focussed = dv.Annotation;
            if (focussed != FocusedItem) {
                FocusedItem = focussed;
                FocusedPoint = dv.Center;
            }
            return HitTestResultBehavior.Stop;
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters) {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }
        #endregion
    }
}
