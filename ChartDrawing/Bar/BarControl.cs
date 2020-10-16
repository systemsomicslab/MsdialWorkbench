using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Bar
{
    public class BarControl : ChartBaseControl
    {
        #region DependencyProperty
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource), typeof(System.Collections.IEnumerable), typeof(BarControl),
            new PropertyMetadata(default(System.Collections.IEnumerable), OnItemsSourceChanged)
            );

        public static readonly DependencyProperty HorizontalPropertyNameProperty = DependencyProperty.Register(
            nameof(HorizontalPropertyName), typeof(string), typeof(BarControl),
            new PropertyMetadata(default(string), OnHorizontalPropertyNameChanged)
            );

        public static readonly DependencyProperty VerticalPropertyNameProperty = DependencyProperty.Register(
            nameof(VerticalPropertyName), typeof(string), typeof(BarControl),
            new PropertyMetadata(default(string), OnVerticalPropertyNameChanged)
            );

        public static readonly DependencyProperty BarWidthProperty = DependencyProperty.Register(
            nameof(BarWidth), typeof(double), typeof(BarControl),
            new PropertyMetadata(0.95, ChartUpdate)
            );

        public static readonly DependencyProperty BarBrushProperty = DependencyProperty.Register(
            nameof(BarBrush), typeof(Brush), typeof(BarControl),
            new PropertyMetadata(Brushes.Blue)
            );

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            nameof(SelectedItem), typeof(object), typeof(BarControl),
            new PropertyMetadata(null, OnSelectedItemChanged));

        public static readonly DependencyProperty FocusedItemProperty = DependencyProperty.Register(
            nameof(FocusedItem), typeof(object), typeof(BarControl),
            new PropertyMetadata(null)
            );

        public static readonly DependencyProperty FocusedPointProperty = DependencyProperty.Register(
            nameof(FocusedPoint), typeof(Point), typeof(BarControl),
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

        public double BarWidth {
            get => (double)GetValue(BarWidthProperty);
            set => SetValue(BarWidthProperty, value);
        }

        public Brush BarBrush
        {
            get => (Brush)GetValue(BarBrushProperty);
            set => SetValue(BarBrushProperty, value);
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

        public BarControl()
        {
            BarBrush.Freeze();
            MouseLeftButtonDown += VisualSelectOnClick;
            MouseMove += VisualFocusOnMouseOver;
            ClipToBounds = true;
        }

        protected override void Update()
        {
            if (  hPropertyReflection == null
               || vPropertyReflection == null
               || HorizontalAxis == null
               || VerticalAxis == null
               || BarBrush == null
               || cv == null
               )
                return;

            var brush = BarBrush;
            double actualWidth = ActualWidth, actualHeight = ActualHeight;
            double barwidth = BarWidth * actualWidth / visualChildren.Count;

            double yorigin = VerticalAxis.TranslateToRenderPoint(0d) * actualHeight;
            foreach(var visual in visualChildren)
            {
                var dv = visual as AnnotatedDrawingVisual;
                var o = dv.Annotation;
                var x = hPropertyReflection.GetValue(o);
                var y = vPropertyReflection.GetValue(o);

                double xx = HorizontalAxis.TranslateToRenderPoint(x) * actualWidth;
                double yy = VerticalAxis.TranslateToRenderPoint(y) * actualHeight;
                dv.Center = new Point(xx, yy);

                using (var dc = dv.RenderOpen()) {
                    dc.DrawRectangle(brush, null, new Rect(new Point(xx - barwidth / 2, yy), new Point(xx + barwidth / 2, yorigin)));
                }
            }
        }

        private void SetDrawingVisuals() {
            if (cv == null) return;

            visualChildren.Clear();
            foreach (var o in cv)
                visualChildren.Add(new AnnotatedDrawingVisual(o));
        }

        #region Event handler
        static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as BarControl;
            if (chart == null) return;

            chart.dataType = null;
            chart.cv = null;

            if (chart.ItemsSource == null) return;

            var enumerator = chart.ItemsSource.GetEnumerator();
            if (!enumerator.MoveNext()) return;

            chart.dataType = enumerator.Current.GetType();
            chart.cv = CollectionViewSource.GetDefaultView(chart.ItemsSource) as CollectionView;

            chart.SetDrawingVisuals();

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
            var chart = d as BarControl;
            if (chart == null) return;

            if (chart.dataType != null)
                chart.hPropertyReflection = chart.dataType.GetProperty((string)e.NewValue);

            chart.Update();
        }

        static void OnVerticalPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as BarControl;
            if (chart == null) return;

            if (chart.dataType != null)
                chart.vPropertyReflection = chart.dataType.GetProperty((string)e.NewValue);

            chart.Update();
        }

        static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as BarControl;
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
