using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CompMs.Graphics.Behavior;
using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Chart
{
    public class HorizontalColorAxisControl : ChartBaseControl
    {
        #region DependencyProperty
        public static readonly DependencyProperty LabelBrushesProperty = DependencyProperty.Register(
            nameof(LabelBrushes), typeof(IList<Brush>), typeof(HorizontalColorAxisControl),
            new PropertyMetadata(null)
            );

        public static readonly DependencyProperty IdentityPropertyNameProperty = DependencyProperty.Register(
            nameof(IdentityPropertyName), typeof(string), typeof(HorizontalColorAxisControl),
            new PropertyMetadata(null, OnIdentityPropertyNamePropertyChanged)
            );

        public static readonly DependencyProperty FocusedItemProperty = DependencyProperty.Register(
            nameof(FocusedItem), typeof(object), typeof(HorizontalColorAxisControl),
            new PropertyMetadata(default(object))
            );

        public static readonly DependencyProperty FocusedPointProperty = DependencyProperty.Register(
            nameof(FocusedPoint), typeof(Point), typeof(HorizontalColorAxisControl),
            new PropertyMetadata(default(Point))
            );
        #endregion

        #region Property
        public IList<Brush> LabelBrushes {
            get => (IList<Brush>)GetValue(LabelBrushesProperty);
            set => SetValue(LabelBrushesProperty, value);
        }

        public string IdentityPropertyName {
            get => (string)GetValue(IdentityPropertyNameProperty);
            set => SetValue(IdentityPropertyNameProperty, value);
        }

        public object FocusedItem {
            get => GetValue(FocusedItemProperty);
            set => SetValue(FocusedItemProperty, value);
        }

        public Point FocusedPoint {
            get => (Point)GetValue(FocusedPointProperty);
            set => SetValue(FocusedPointProperty, value);
        }
        #endregion

        #region field
        private PropertyInfo iPropertyReflection;
        #endregion

        public HorizontalColorAxisControl() {
            MouseMove += VisualFocusOnMouseOver;
            ZoomByDragBehavior.SetIsEnabled(this, true);
            ZoomByDragBehavior.SetStrechVertical(this, true);
            ZoomByWheelBehavior.SetIsEnabled(this, true);
            MoveByDragBehavior.SetIsEnabled(this, true);
            ResetRangeByDoubleClickBehavior.SetIsEnabled(this, true);
        }

        protected override void Update() {
            if (HorizontalAxis == null) return;

            var memo = new Dictionary<object, int>();
            var id = 0;
            Func<object, object> toKey = null;
            Func<object, Brush> toBrush = null;
            var sign = FlippedX ? 1 : -1;

            visualChildren.Clear();
            foreach (var data in HorizontalAxis.GetLabelTicks()) {
                if (data.TickType != TickType.LongTick) continue;

                if (IdentityPropertyName != null && iPropertyReflection == null)
                    iPropertyReflection = data.Source.GetType().GetProperty(IdentityPropertyName);

                if (toKey == null) {
                    if (iPropertyReflection == null)
                        toKey = o => o;
                    else
                        toKey = o => iPropertyReflection.GetValue(o);

                    if (!(toKey(data.Source) is Brush) && LabelBrushes == null)
                        return;
                }

                if (toBrush == null)
                    toBrush = o => {
                        var x = toKey(o);
                        if (x is Brush b) return b;
                        if (!memo.ContainsKey(x)) memo[x] = id++;
                        return LabelBrushes[memo[x] % LabelBrushes.Count];
                    };

                var xorigin = HorizontalAxis.TranslateToRenderPoint(data.Center + sign * data.Width / 2, FlippedX, ActualWidth);
                var xwidth = Math.Abs(HorizontalAxis.TranslateToRenderPoint(data.Width, FlippedX, ActualWidth) - HorizontalAxis.TranslateToRenderPoint(0d, FlippedX, ActualWidth));

                var dv = new AnnotatedDrawingVisual(data.Source) { Center = new Point(xorigin + xwidth / 2, ActualHeight / 2) };
                dv.Clip = new RectangleGeometry(new Rect(RenderSize));
                var dc = dv.RenderOpen();
                dc.DrawRectangle(toBrush(data.Source), null, new Rect(xorigin, 0, xwidth, ActualHeight));
                dc.Close();
                visualChildren.Add(dv);
            }
        }

        #region Event handler
        static void OnIdentityPropertyNamePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is HorizontalColorAxisControl chart)
                chart.iPropertyReflection = null;
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
