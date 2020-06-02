using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.GraphAxis
{
    public class VerticalColorAxisControl : FrameworkElement
    {
        #region DependencyProperty
        public static readonly DependencyProperty VerticalAxisProperty = DependencyProperty.Register(
            nameof(VerticalAxis), typeof(AxisManager), typeof(VerticalColorAxisControl),
            new PropertyMetadata(default(AxisManager), OnVerticalAxisChanged)
            );

        public static readonly DependencyProperty LabelBrushesProperty = DependencyProperty.Register(
            nameof(LabelBrushes), typeof(IList<Brush>), typeof(VerticalColorAxisControl),
            new PropertyMetadata(null)
            );

        public static readonly DependencyProperty IdentityPropertyNameProperty = DependencyProperty.Register(
            nameof(IdentityPropertyName), typeof(string), typeof(VerticalColorAxisControl),
            new PropertyMetadata(null, OnIdentityPropertyNamePropertyChanged)
            );

        public static readonly DependencyProperty FocussedItemProperty = DependencyProperty.Register(
            nameof(FocussedItem), typeof(object), typeof(VerticalColorAxisControl),
            new PropertyMetadata(default(object))
            );
        #endregion

        #region Property
        public AxisManager VerticalAxis
        {
            get => (AxisManager)GetValue(VerticalAxisProperty);
            set => SetValue(VerticalAxisProperty, value);
        }

        public IList<Brush> LabelBrushes
        {
            get => (IList<Brush>)GetValue(LabelBrushesProperty);
            set => SetValue(LabelBrushesProperty, value);
        }

        public string IdentityPropertyName
        {
            get => (string)GetValue(IdentityPropertyNameProperty);
            set => SetValue(IdentityPropertyNameProperty, value);
        }

        public object FocussedItem
        {
            get => (object)GetValue(FocussedItemProperty);
            set => SetValue(FocussedItemProperty, value);
        }
        #endregion

        #region field
        private VisualCollection visualChildren;
        private PropertyInfo iPropertyReflection;
        #endregion

        public VerticalColorAxisControl()
        {
            visualChildren = new VisualCollection(this);

            MouseMove += VisualFocusOnMouseOver;
        }

        private void Update()
        {
            if (VerticalAxis == null) return;

            var memo = new Dictionary<object, int>();
            var id = 0;
            Func<object, object> toKey = null;
            Func<object, Brush> toBrush = null;

            visualChildren.Clear();
            foreach (var data in VerticalAxis.GetLabelTicks())
            {
                if (data.TickType != TickType.LongTick) continue;

                if (IdentityPropertyName != null && iPropertyReflection == null)
                    iPropertyReflection = data.Source.GetType().GetProperty(IdentityPropertyName);

                if (toKey == null)
                {
                    if (iPropertyReflection == null)
                        toKey = o => o;
                    else
                        toKey = o => iPropertyReflection.GetValue(o);

                    if (!(toKey(data.Source) is Brush) && LabelBrushes == null)
                        return;
                }

                if (toBrush == null)
                    toBrush = o =>
                    {
                        var x = toKey(o);
                        if (x is Brush b) return b;
                        if (!memo.ContainsKey(x)) memo[x] = id++;
                        return LabelBrushes[memo[x] % LabelBrushes.Count];
                    };

                var yorigin = VerticalAxis.ValueToRenderPosition(data.Center - data.Width / 2) * ActualHeight;
                var yheight = (VerticalAxis.ValueToRenderPosition(data.Width) - VerticalAxis.ValueToRenderPosition(0)) * ActualHeight;

                var dv = new AnnotatedDrawingVisual(data.Source);
                var dc = dv.RenderOpen();
                dc.DrawRectangle(toBrush(data.Source), null, new Rect(0, yorigin, ActualWidth, yheight));
                dc.Close();
                visualChildren.Add(dv);
            }
        }

        #region Event handler
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) => Update();

        static void OnVerticalAxisChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VerticalColorAxisControl chart) chart.Update();
        }

        static void OnIdentityPropertyNamePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VerticalColorAxisControl chart)
                chart.iPropertyReflection = null;
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
