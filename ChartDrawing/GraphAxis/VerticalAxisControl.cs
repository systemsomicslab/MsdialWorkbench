using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.GraphAxis
{
    public class VerticalAxisControl : FrameworkElement
    {
        #region DependencyProperty
        public static readonly DependencyProperty VerticalAxisProperty = DependencyProperty.Register(
            nameof(VerticalAxis), typeof(AxisManager), typeof(VerticalAxisControl),
            new PropertyMetadata(default(AxisManager), OnVerticalAxisChanged)
            );

        public static readonly DependencyProperty TickPenProperty = DependencyProperty.Register(
            nameof(TickPen), typeof(Pen), typeof(VerticalAxisControl),
            new PropertyMetadata(new Pen(Brushes.Black, 1))
            );

        public static readonly DependencyProperty LabelBrushProperty = DependencyProperty.Register(
            nameof(LabelBrush), typeof(Brush), typeof(VerticalAxisControl),
            new PropertyMetadata(Brushes.Black)
            );

        public static readonly DependencyProperty LabelSizeProperty = DependencyProperty.Register(
            nameof(LabelSize), typeof(double), typeof(VerticalAxisControl),
            new PropertyMetadata(12d)
            );

        public static readonly DependencyProperty FocussedItemProperty = DependencyProperty.Register(
            nameof(FocussedItem), typeof(object), typeof(VerticalAxisControl),
            new PropertyMetadata(default(object))
            );
        #endregion

        #region Property
        public AxisManager VerticalAxis
        {
            get => (AxisManager)GetValue(VerticalAxisProperty);
            set => SetValue(VerticalAxisProperty, value);
        }

        public Pen TickPen
        {
            get => (Pen)GetValue(TickPenProperty);
            set => SetValue(TickPenProperty, value);
        }

        public Brush LabelBrush
        {
            get => (Brush)GetValue(LabelBrushProperty);
            set => SetValue(LabelBrushProperty, value);
        }

        public double LabelSize
        {
            get => (double)GetValue(LabelSizeProperty);
            set => SetValue(LabelSizeProperty, value);
        }

        public object FocussedItem
        {
            get => (object)GetValue(FocussedItemProperty);
            set => SetValue(FocussedItemProperty, value);
        }

        public double ShortTickSize { get; set; } = 3;
        public double LongTickSize { get; set; } = 5;
        #endregion

        #region field
        private VisualCollection visualChildren;
        #endregion

        public VerticalAxisControl()
        {
            visualChildren = new VisualCollection(this);

            MouseMove += VisualFocusOnMouseOver;
        }

        private void Update()
        {
            if (VerticalAxis == null
                || TickPen == null
                || LabelBrush == null
                ) return;

            visualChildren.Clear();
            foreach (var data in VerticalAxis.GetLabelTicks())
            {
                var center = VerticalAxis.ValueToRenderPosition(data.Center) * ActualHeight;

                var dv = new AnnotatedDrawingVisual(data);
                var dc = dv.RenderOpen();

                switch (data.TickType)
                {
                    case TickType.LongTick:
                        dc.DrawLine(TickPen, new Point(ActualWidth, center), new Point(ActualWidth - LongTickSize, center));
                        var formattedText = new FormattedText(
                            data.Label, CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight, new Typeface("Calibri"),
                            LabelSize, LabelBrush, 1)
                        {
                            MaxTextWidth = Math.Max(1, ActualWidth - LongTickSize),
                            MaxTextHeight = Math.Max(1, Math.Abs(VerticalAxis.ValueToRenderPosition(data.Width) - VerticalAxis.ValueToRenderPosition(0)) * ActualHeight),
                        };
                        dc.DrawText(formattedText, new Point(ActualWidth - LongTickSize - formattedText.Width, center - formattedText.Height / 2));
                        break;
                    case TickType.ShortTick:
                        dc.DrawLine(TickPen, new Point(ActualWidth, center), new Point(ActualWidth - ShortTickSize, center));
                        break;
                }
                dc.Close();
                visualChildren.Add(dv);
            }
        }

        #region Event handler
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) => Update();

        static void OnVerticalAxisChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VerticalAxisControl chart) chart.Update();
        }
        #endregion

        #region Mouse event
        void VisualFocusOnMouseOver(object sender, MouseEventArgs e)
        {
            var pt = e.GetPosition(this);

            VisualTreeHelper.HitTest(this,
                new HitTestFilterCallback(VisualHitTestFilter),
                new HitTestResultCallback(VisualFocusHitTest),
                new PointHitTestParameters(pt)
                );
        }

        HitTestFilterBehavior VisualHitTestFilter(DependencyObject d)
        {
            if (d is AnnotatedDrawingVisual)
                return HitTestFilterBehavior.Continue;
            return HitTestFilterBehavior.ContinueSkipSelf;
        }

        HitTestResultBehavior VisualFocusHitTest(HitTestResult result)
        {
            FocussedItem = ((AnnotatedDrawingVisual)result.VisualHit).Annotation;
            return HitTestResultBehavior.Stop;
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }
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
