using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.GraphAxis
{
    public class HorizontalAxisControl : FrameworkElement
    {
        #region DependencyProperty
        public static readonly DependencyProperty HorizontalAxisProperty = DependencyProperty.Register(
            nameof(HorizontalAxis), typeof(AxisManager), typeof(HorizontalAxisControl),
            new PropertyMetadata(default(AxisManager), OnHorizontalAxisChanged)
            );

        public static readonly DependencyProperty TickPenProperty = DependencyProperty.Register(
            nameof(TickPen), typeof(Pen), typeof(HorizontalAxisControl),
            new PropertyMetadata(new Pen(Brushes.Black, 1))
            );

        public static readonly DependencyProperty LabelBrushProperty = DependencyProperty.Register(
            nameof(LabelBrush), typeof(Brush), typeof(HorizontalAxisControl),
            new PropertyMetadata(Brushes.Black)
            );

        public static readonly DependencyProperty LabelSizeProperty = DependencyProperty.Register(
            nameof(LabelSize), typeof(double), typeof(HorizontalAxisControl),
            new PropertyMetadata(12d)
            );

        public static readonly DependencyProperty FocussedItemProperty = DependencyProperty.Register(
            nameof(FocussedItem), typeof(object), typeof(HorizontalAxisControl),
            new PropertyMetadata(default(object))
            );
        #endregion

        #region Property
        public AxisManager HorizontalAxis
        {
            get => (AxisManager)GetValue(HorizontalAxisProperty);
            set => SetValue(HorizontalAxisProperty, value);
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

        public HorizontalAxisControl()
        {
            visualChildren = new VisualCollection(this);

            MouseMove += VisualFocusOnMouseOver;
        }

        private void Update()
        {
            if (HorizontalAxis == null
                || TickPen == null
                || LabelBrush == null
                ) return;

            visualChildren.Clear();
            foreach (var data in HorizontalAxis.GetLabelTicks())
            {
                var center = HorizontalAxis.ValueToRenderPosition(data.Center) * ActualWidth;

                var dv = new AnnotatedDrawingVisual(data);
                var dc = dv.RenderOpen();

                switch (data.TickType)
                {
                    case TickType.LongTick:
                        dc.DrawLine(TickPen, new Point(center, 0), new Point(center, LongTickSize));
                        var formattedText = new FormattedText(
                            data.Label, CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight, new Typeface("Calibri"),
                            LabelSize, LabelBrush, 1)
                        {
                            MaxTextWidth = Math.Abs(HorizontalAxis.ValueToRenderPosition(data.Width) - HorizontalAxis.ValueToRenderPosition(0)) * ActualWidth,
                            MaxTextHeight = Math.Max(1, ActualHeight - LongTickSize),
                        };
                        dc.DrawText(formattedText, new Point(center - formattedText.Width / 2, LongTickSize));
                        break;
                    case TickType.ShortTick:
                        dc.DrawLine(TickPen, new Point(center, 0), new Point(center, ShortTickSize));
                        break;
                }
                dc.Close();
                visualChildren.Add(dv);
            }
        }

        #region Event handler
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) => Update();

        static void OnHorizontalAxisChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HorizontalAxisControl chart) chart.Update();
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
