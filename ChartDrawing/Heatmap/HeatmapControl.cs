using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Heatmap
{
    public class HeatmapControl : FrameworkElement
    {
        #region DependencyProperty
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource), typeof(System.Collections.IEnumerable), typeof(HeatmapControl),
            new PropertyMetadata(default(System.Collections.IEnumerable), OnItemsSourceChanged)
            );

        public static readonly DependencyProperty HorizontalAxisProperty = DependencyProperty.Register(
            nameof(HorizontalAxis), typeof(AxisManager), typeof(HeatmapControl),
            new PropertyMetadata(default(AxisManager), OnHorizontalAxisChanged)
            );

        public static readonly DependencyProperty VerticalAxisProperty = DependencyProperty.Register(
            nameof(VerticalAxis), typeof(AxisManager), typeof(HeatmapControl),
            new PropertyMetadata(default(AxisManager), OnVerticalAxisChanged)
            );

        public static readonly DependencyProperty HorizontalPropertyNameProperty = DependencyProperty.Register(
            nameof(HorizontalPropertyName), typeof(string), typeof(HeatmapControl),
            new PropertyMetadata(default(string), OnHorizontalPropertyNameChanged)
            );

        public static readonly DependencyProperty VerticalPropertyNameProperty = DependencyProperty.Register(
            nameof(VerticalPropertyName), typeof(string), typeof(HeatmapControl),
            new PropertyMetadata(default(string), OnVerticalPropertyNameChanged)
            );

        public static readonly DependencyProperty DegreePropertyNameProperty = DependencyProperty.Register(
            nameof(DegreePropertyName), typeof(string), typeof(HeatmapControl),
            new PropertyMetadata(default(string))
            );

        public static readonly DependencyProperty GradientStopsProperty = DependencyProperty.Register(
            nameof(GradientStops), typeof(GradientStopCollection), typeof(HeatmapControl),
            new PropertyMetadata(new GradientStopCollection()
            {
                new GradientStop(Colors.Blue, 0),
                new GradientStop(Colors.White, 0.5),
                new GradientStop(Colors.Red, 1),
            })
            );

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            nameof(SelectedItem), typeof(object), typeof(HeatmapControl),
            new PropertyMetadata(null, OnSelectedItemChanged));

        public static readonly DependencyProperty FocussedItemProperty = DependencyProperty.Register(
            nameof(FocussedItem), typeof(object), typeof(HeatmapControl),
            new PropertyMetadata(null)
            );
        #endregion

        #region Property
        public System.Collections.IEnumerable ItemsSource
        {
            get => (System.Collections.IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public AxisManager HorizontalAxis
        {
            get => (AxisManager)GetValue(HorizontalAxisProperty);
            set => SetValue(HorizontalAxisProperty, value);
        }

        public AxisManager VerticalAxis
        {
            get => (AxisManager)GetValue(VerticalAxisProperty);
            set => SetValue(VerticalAxisProperty, value);
        }

        public string HorizontalPropertyName
        {
            get => (string)GetValue(HorizontalPropertyNameProperty);
            set => SetValue(HorizontalPropertyNameProperty, value);
        }

        public string VerticalPropertyName
        {
            get => (string)GetValue(VerticalPropertyNameProperty);
            set => SetValue(VerticalPropertyNameProperty, value);
        }

        public string DegreePropertyName
        {
            get => (string)GetValue(DegreePropertyNameProperty);
            set => SetValue(DegreePropertyNameProperty, value);
        }

        public GradientStopCollection GradientStops
        {
            get => (GradientStopCollection)GetValue(GradientStopsProperty);
            set => SetValue(GradientStopsProperty, value);
        }

        public object SelectedItem
        {
            get { return (object)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public object FocussedItem
        {
            get => (object)GetValue(FocussedItemProperty);
            set => SetValue(FocussedItemProperty, value);
        }
        #endregion

        #region field
        private VisualCollection visualChildren;
        private CollectionView cv;
        private Type dataType;
        private PropertyInfo hPropertyReflection;
        private PropertyInfo vPropertyReflection;
        private PropertyInfo zPropertyReflection;
        #endregion

        public HeatmapControl()
        {
            visualChildren = new VisualCollection(this);

            MouseMove += VisualFocusOnMouseOver;
            MouseLeftButtonDown += VisualSelectOnClick;
        }

        private void Update()
        {
            if (  hPropertyReflection == null
               || vPropertyReflection == null
               || zPropertyReflection == null
               || HorizontalAxis == null
               || VerticalAxis == null
               || GradientStops == null
               || cv == null
               )
                return;

            visualChildren.Clear();
            double zmax = double.MinValue, zmin = double.MaxValue;
            double xwidth = (HorizontalAxis.ValueToRenderPosition(1d) - HorizontalAxis.ValueToRenderPosition(0d)) * ActualWidth;
            double ywidth = (VerticalAxis.ValueToRenderPosition(1d) - VerticalAxis.ValueToRenderPosition(0d)) * ActualHeight;
            foreach(var o in cv)
            {
                var z = zPropertyReflection.GetValue(o);
                zmax = Math.Max(zmax, Convert.ToDouble(z));
                zmin = Math.Min(zmin, Convert.ToDouble(z));
            }

            foreach (var o in cv)
            {
                var x = hPropertyReflection.GetValue(o);
                var y = vPropertyReflection.GetValue(o);
                var z = zPropertyReflection.GetValue(o);

                double xx, yy, zz;
                if (x is double)
                    xx = HorizontalAxis.ValueToRenderPosition((double)x) * ActualWidth;
                else if (x is string)
                    xx = HorizontalAxis.ValueToRenderPosition(x) * ActualWidth;
                else if (x is IConvertible)
                    xx = HorizontalAxis.ValueToRenderPosition(x as IConvertible) * ActualWidth;
                else
                    xx = HorizontalAxis.ValueToRenderPosition(x) * ActualWidth;

                if (y is double)
                    yy = VerticalAxis.ValueToRenderPosition((double)y) * ActualHeight;
                else if (y is string)
                    yy = VerticalAxis.ValueToRenderPosition(y) * ActualHeight;
                else if (y is IConvertible)
                    yy = VerticalAxis.ValueToRenderPosition(y as IConvertible) * ActualHeight;
                else
                    yy = VerticalAxis.ValueToRenderPosition(y) * ActualHeight;

                zz = Convert.ToDouble(z as IConvertible);
                var color = getGradientColor(GradientStops, zz, zmin, zmax);
                var brush = new SolidColorBrush(color);

                var dv = new AnnotatedDrawingVisual(o);
                dv.Clip = new RectangleGeometry(new Rect(RenderSize));
                var dc = dv.RenderOpen();
                dc.DrawRectangle(brush, null, new Rect(xx - xwidth / 2, yy - ywidth / 2, xwidth, ywidth));
                dc.Close();
                visualChildren.Add(dv);
            }
        }

        static Color blendColors(Color ca, Color cb, double factor)
        {
            var f = (float)factor;
            return cb * f + ca * (1 - f);
        }

        static Color getGradientColor(GradientStopCollection gsc, double offset)
        {
            var lowers = gsc.Where(gs => gs.Offset <= offset).ToArray();
            var highers = gsc.Where(gs => gs.Offset > offset).ToArray();
            if (offset < 0) return highers.Min(gs => (gs.Offset, gs.Color)).Color;
            if (offset >= 1) return lowers.Max(gs => (gs.Offset, gs.Color)).Color; 

            var lo = lowers.Max(gs => (gs.Offset, gs.Color)); 
            var hi = highers.Min(gs => (gs.Offset, gs.Color));
            var o = ((offset - lo.Offset) / (hi.Offset - lo.Offset));
            return blendColors(lo.Color, hi.Color, o);
        }

        static Color getGradientColor(GradientStopCollection gsc, double value, double min, double max)
            => getGradientColor(gsc, (value - min) / (max - min));

        #region Event handler
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) => Update();

        static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as HeatmapControl;
            if (chart == null) return;

            var enumerator = chart.ItemsSource.GetEnumerator();
            enumerator.MoveNext();
            chart.dataType = enumerator.Current.GetType();
            chart.cv = CollectionViewSource.GetDefaultView(chart.ItemsSource) as CollectionView;

            if (chart.HorizontalPropertyName != null)
                chart.hPropertyReflection = chart.dataType.GetProperty(chart.HorizontalPropertyName);
            if (chart.VerticalPropertyName != null)
                chart.vPropertyReflection = chart.dataType.GetProperty(chart.VerticalPropertyName);
            if (chart.DegreePropertyName != null)
                chart.zPropertyReflection = chart.dataType.GetProperty(chart.DegreePropertyName);
            if (chart.SelectedItem != null)
                chart.cv.MoveCurrentTo(chart.SelectedItem);

            chart.Update();
        }

        static void OnHorizontalAxisChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HeatmapControl chart) chart.Update();
        }

        static void OnVerticalAxisChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HeatmapControl chart) chart.Update();
        }

        static void OnHorizontalPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as HeatmapControl;
            if (chart == null) return;

            if (chart.dataType != null)
                chart.hPropertyReflection = chart.dataType.GetProperty((string)e.NewValue);

            chart.Update();
        }

        static void OnVerticalPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as HeatmapControl;
            if (chart == null) return;

            if (chart.dataType != null)
                chart.vPropertyReflection = chart.dataType.GetProperty((string)e.NewValue);

            chart.Update();
        }

        static void OnDegreePropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as HeatmapControl;
            if (chart == null) return;

            if (chart.dataType != null)
                chart.zPropertyReflection = chart.dataType.GetProperty((string)e.NewValue);

            chart.Update();
        }

        static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as HeatmapControl;
            if (chart == null) return;

            if (chart.cv != null)
                chart.cv.MoveCurrentTo(e.NewValue);
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

        void VisualSelectOnClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                var pt = e.GetPosition(this);

                VisualTreeHelper.HitTest(this,
                    new HitTestFilterCallback(VisualHitTestFilter),
                    new HitTestResultCallback(VisualSelectHitTest),
                    new PointHitTestParameters(pt)
                    );
            }
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

        HitTestResultBehavior VisualSelectHitTest(HitTestResult result)
        {
            SelectedItem = ((AnnotatedDrawingVisual)result.VisualHit).Annotation;
            return HitTestResultBehavior.Stop;
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
