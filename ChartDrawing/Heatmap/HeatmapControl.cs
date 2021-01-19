using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Heatmap
{
    public class HeatmapControl : ChartBaseControl
    {
        #region DependencyProperty
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource), typeof(System.Collections.IEnumerable), typeof(HeatmapControl),
            new PropertyMetadata(default(System.Collections.IEnumerable), OnItemsSourceChanged)
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
            new PropertyMetadata(default(string), OnDegreePropertyNameChanged)
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

        public static readonly DependencyProperty FocusedItemProperty = DependencyProperty.Register(
            nameof(FocusedItem), typeof(object), typeof(HeatmapControl),
            new PropertyMetadata(null)
            );

        public static readonly DependencyProperty FocusedPointProperty = DependencyProperty.Register(
            nameof(FocusedPoint), typeof(Point), typeof(HeatmapControl),
            new PropertyMetadata(default(Point))
            );
        #endregion

        #region Property
        public System.Collections.IEnumerable ItemsSource
        {
            get => (System.Collections.IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
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

        public object FocusedItem
        {
            get => (object)GetValue(FocusedItemProperty);
            set => SetValue(FocusedItemProperty, value);
        }

        public Point FocusedPoint
        {
            get => (Point)GetValue(FocusedPointProperty);
            set => SetValue(FocusedPointProperty, value);
        }
        #endregion

        #region field
        private CollectionView cv;
        private Type dataType;
        private PropertyInfo hPropertyReflection;
        private PropertyInfo vPropertyReflection;
        private PropertyInfo zPropertyReflection;
        #endregion

        public HeatmapControl()
        {
            MouseLeftButtonDown += VisualSelectOnClick;
            MouseMove += VisualFocusOnMouseOver;
        }

        protected override void Update()
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
            double xwidth = (HorizontalAxis.TranslateToRenderPoint(1d, FlippedX) - HorizontalAxis.TranslateToRenderPoint(0d, FlippedX)) * ActualWidth;
            double ywidth = Math.Abs(VerticalAxis.TranslateToRenderPoint(1d, FlippedY) - VerticalAxis.TranslateToRenderPoint(0d, FlippedY)) * ActualHeight;
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

                double xx, yy, zz;
                xx = HorizontalAxis.TranslateToRenderPoint(x, FlippedX) * ActualWidth;
                yy = VerticalAxis.TranslateToRenderPoint(y, FlippedY) * ActualHeight;

                if (xx == double.NaN || yy == double.NaN) continue;

                var z = zPropertyReflection.GetValue(o);
                zz = Convert.ToDouble(z as IConvertible);
                var color = getGradientColor(GradientStops, zz, zmin, zmax);
                var brush = new SolidColorBrush(color);

                var dv = new AnnotatedDrawingVisual(o) { Center = new Point(xx, yy) };
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

        #region PropertyChanged event
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

        #region Visual hit Event
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
            var dv = (AnnotatedDrawingVisual)result.VisualHit;
            var focussed = dv.Annotation;
            if (focussed != FocusedItem)
            {
                FocusedItem = focussed;
                FocusedPoint = dv.Center;
            }
            return HitTestResultBehavior.Stop;
        }

        HitTestResultBehavior VisualSelectHitTest(HitTestResult result)
        {
            SelectedItem = ((AnnotatedDrawingVisual)result.VisualHit).Annotation;
            return HitTestResultBehavior.Stop;
        }
        #endregion

        #endregion
    }
}
