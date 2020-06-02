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

namespace CompMs.Graphics.Scatter
{
    public class ScatterControl : FrameworkElement
    {
        #region DependencyProperty
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource), typeof(System.Collections.IEnumerable), typeof(ScatterControl),
            new PropertyMetadata(default(System.Collections.IEnumerable), OnItemsSourceChanged)
            );

        public static readonly DependencyProperty HorizontalAxisProperty = DependencyProperty.Register(
            nameof(HorizontalAxis), typeof(AxisManager), typeof(ScatterControl),
            new PropertyMetadata(default(AxisManager), OnHorizontalAxisChanged)
            );

        public static readonly DependencyProperty VerticalAxisProperty = DependencyProperty.Register(
            nameof(VerticalAxis), typeof(AxisManager), typeof(ScatterControl),
            new PropertyMetadata(default(AxisManager), OnVerticalAxisChanged)
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
        #endregion

        #region field
        private VisualCollection visualChildren;
        private CollectionView cv;
        private Type dataType;
        private PropertyInfo hPropertyReflection;
        private PropertyInfo vPropertyReflection;
        #endregion

        public ScatterControl()
        {
            visualChildren = new VisualCollection(this);

            MouseMove += VisualFocusOnMouseOver;
            MouseLeftButtonDown += VisualSelectOnClick;
        }

        private void Update()
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

                var dv = new AnnotatedDrawingVisual(o);
                dv.Clip = new RectangleGeometry(new Rect(RenderSize));
                var dc = dv.RenderOpen();
                dc.DrawEllipse(PointBrush, null, new Point(xx, yy), Radius, Radius);
                dc.Close();
                visualChildren.Add(dv);
            }
        }

        #region Event handler
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) => Update();

        static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as ScatterControl;
            if (chart == null) return;

            var enumerator = chart.ItemsSource.GetEnumerator();
            enumerator.MoveNext();
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

        static void OnHorizontalAxisChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScatterControl chart) chart.Update();
        }

        static void OnVerticalAxisChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScatterControl chart) chart.Update();
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
            FocusedItem = ((AnnotatedDrawingVisual)result.VisualHit).Annotation;
            return HitTestResultBehavior.Stop;
        }

        HitTestResultBehavior VisualSelectHitTest(HitTestResult result)
        {
            SelectedItem = ((AnnotatedDrawingVisual)result.VisualHit).Annotation;
            return HitTestResultBehavior.Stop;
        }

        /*
        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }
        */
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
