using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Scatter
{
    public class ScatterControl : ChartBaseControl
    {
        #region DependencyProperty
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource), typeof(System.Collections.IEnumerable), typeof(ScatterControl),
            new PropertyMetadata(default(System.Collections.IEnumerable), OnItemsSourceChanged)
            );

        public static readonly DependencyProperty HorizontalPropertyNameProperty = DependencyProperty.Register(
            nameof(HorizontalPropertyName), typeof(string), typeof(ScatterControl),
            new PropertyMetadata(default(string), OnHorizontalPropertyNameChanged)
            );

        public static readonly DependencyProperty VerticalPropertyNameProperty = DependencyProperty.Register(
            nameof(VerticalPropertyName), typeof(string), typeof(ScatterControl),
            new PropertyMetadata(default(string), OnVerticalPropertyNameChanged)
            );

        public static readonly DependencyProperty PointBrushProperty = DependencyProperty.Register(
            nameof(PointBrush), typeof(Brush), typeof(ScatterControl),
            new PropertyMetadata(Brushes.Black)
            );

        public static readonly DependencyProperty RadiusProperty = DependencyProperty.Register(
            nameof(Radius), typeof(double), typeof(ScatterControl),
            new PropertyMetadata(3d)
            );

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            nameof(SelectedItem), typeof(object), typeof(ScatterControl),
            new PropertyMetadata(null, OnSelectedItemChanged));

        public static readonly DependencyProperty FocusedItemProperty = DependencyProperty.Register(
            nameof(FocusedItem), typeof(object), typeof(ScatterControl),
            new PropertyMetadata(null)
            );

        public static readonly DependencyProperty FocusedPointProperty = DependencyProperty.Register(
            nameof(FocusedPoint), typeof(Point), typeof(ScatterControl),
            new PropertyMetadata(default)
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

        public Brush PointBrush
        {
            get => (Brush)GetValue(PointBrushProperty);
            set => SetValue(PointBrushProperty, value);
        }

        public double Radius
        {
            get => (double)GetValue(RadiusProperty);
            set => SetValue(RadiusProperty, value);
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
        #endregion

        public ScatterControl()
        {
            MouseLeftButtonDown += VisualSelectOnClick;
            MouseMove += VisualFocusOnMouseOver;
        }

        protected override void Update()
        {
            if (  hPropertyReflection == null
               || vPropertyReflection == null
               || HorizontalAxis == null
               || VerticalAxis == null
               || PointBrush == null
               || cv == null
               )
                return;

            visualChildren.Clear();
            foreach (var o in cv)
            {
                var x = hPropertyReflection.GetValue(o);
                var y = vPropertyReflection.GetValue(o);

                double xx, yy;
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

                var dv = new AnnotatedDrawingVisual(o) { Center = new Point(xx, yy) };
                dv.Clip = new RectangleGeometry(new Rect(RenderSize));
                var dc = dv.RenderOpen();
                dc.DrawEllipse(PointBrush, null, new Point(xx, yy), Radius, Radius);
                dc.Close();
                visualChildren.Add(dv);
            }
        }

        #region Event handler
        static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as ScatterControl;
            if (chart == null) return;

            chart.dataType = null;
            chart.cv = null;

            if (chart.ItemsSource == null) return;

            var enumerator = chart.ItemsSource.GetEnumerator();
            if (!enumerator.MoveNext()) return;

            chart.dataType = enumerator.Current.GetType();
            chart.cv = CollectionViewSource.GetDefaultView(chart.ItemsSource) as CollectionView;

            if (chart.HorizontalPropertyName != null)
                chart.hPropertyReflection = chart.dataType.GetProperty(chart.HorizontalPropertyName);
            if (chart.VerticalPropertyName != null)
                chart.vPropertyReflection = chart.dataType.GetProperty(chart.VerticalPropertyName);
            if (chart.SelectedItem != null)
                chart.cv.MoveCurrentTo(chart.SelectedItem);

            chart.Update();
        }

        static void OnHorizontalPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as ScatterControl;
            if (chart == null) return;

            if (chart.dataType != null)
                chart.hPropertyReflection = chart.dataType.GetProperty((string)e.NewValue);

            chart.Update();
        }

        static void OnVerticalPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as ScatterControl;
            if (chart == null) return;

            if (chart.dataType != null)
                chart.vPropertyReflection = chart.dataType.GetProperty((string)e.NewValue);

            chart.Update();
        }

        static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as ScatterControl;
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
                new GeometryHitTestParameters(new EllipseGeometry(pt, 50d, 50d))
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
    }
}
