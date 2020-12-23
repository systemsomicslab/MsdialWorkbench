using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

using CompMs.Common.Extension;
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

        public static readonly DependencyProperty PointGeometryProperty = DependencyProperty.Register(
            nameof(PointGeometry), typeof(Geometry), typeof(ScatterControl),
            new PropertyMetadata(new EllipseGeometry(new Rect(0, 0, 1, 1)))
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

        public static readonly DependencyProperty SelectedPointProperty = DependencyProperty.Register(
            nameof(SelectedPoint), typeof(Point?), typeof(ScatterControl),
            new PropertyMetadata(default)
            );

        public static readonly DependencyProperty FocusedItemProperty = DependencyProperty.Register(
            nameof(FocusedItem), typeof(object), typeof(ScatterControl),
            new PropertyMetadata(null)
            );

        public static readonly DependencyProperty FocusedPointProperty = DependencyProperty.Register(
            nameof(FocusedPoint), typeof(Point?), typeof(ScatterControl),
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

        public Geometry PointGeometry
        {
            get => (Geometry)GetValue(PointGeometryProperty);
            set => SetValue(PointGeometryProperty, value);
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

        public Point? SelectedPoint
        {
            get => (Point?)GetValue(SelectedPointProperty);
            set => SetValue(SelectedPointProperty, value);
        }

        public object FocusedItem
        {
            get => (object)GetValue(FocusedItemProperty);
            set => SetValue(FocusedItemProperty, value);
        }

        public Point? FocusedPoint
        {
            get => (Point?)GetValue(FocusedPointProperty);
            set => SetValue(FocusedPointProperty, value);
        }
        #endregion

        #region field
        private CollectionView cv;
        private Type dataType;
        private PropertyInfo hPropertyReflection;
        private PropertyInfo vPropertyReflection;
        private AnnotatedDrawingVisual focus, select;
        #endregion

        public ScatterControl()
        {
            MouseLeftButtonDown += VisualSelectOnClick;
            MouseMove += VisualFocusOnMouseOver;
            ClipToBounds = true;
        }

        protected override void Update()
        {
            base.Update();
            if (  hPropertyReflection == null
               || vPropertyReflection == null
               || HorizontalAxis == null
               || VerticalAxis == null
               || PointBrush == null
               )
                return;

            var brush = new DrawingBrush(new GeometryDrawing(PointBrush, null, PointGeometry));
            brush.Freeze();
            double radius = Radius, actualWidth = ActualWidth, actualHeight = ActualHeight;

            foreach(var visual in visualChildren)
            {
                if (!(visual is AnnotatedDrawingVisual dv)) continue;
                var o = dv.Annotation;
                var x = hPropertyReflection.GetValue(o);
                var y = vPropertyReflection.GetValue(o);

                double xx = HorizontalAxis.TranslateToRenderPoint(x, FlippedX) * actualWidth;
                double yy = VerticalAxis.TranslateToRenderPoint(y, FlippedY) * actualHeight;
                dv.Center = new Point(xx, yy);

                using (var dc = dv.RenderOpen()) {
                    dc.DrawRectangle(brush, null, new Rect(xx - radius, yy - radius, radius * 2, radius * 2));
                }
            }
        }

        private void SetDrawingVisuals() {
            if (cv == null) return;

            visualChildren.Clear();
            foreach (var o in cv)
                visualChildren.Add(new AnnotatedDrawingVisual(o));
        }

        private void ItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            SetDrawingVisuals();
            Update();
        }

        #region Event
        public static RoutedEvent FocusChangedEvent =
            EventManager.RegisterRoutedEvent(nameof(FocusChanged), RoutingStrategy.Bubble, typeof(RoutedEvent), typeof(ScatterControl));
        private static RoutedEventArgs FocusChangedEventArgs = new RoutedEventArgs(FocusChangedEvent);

        public event RoutedEventHandler FocusChanged {
            add => AddHandler(FocusChangedEvent, value);
            remove => RemoveHandler(FocusChangedEvent, value);
        }

        public static RoutedEvent SelectChangedEvent =
            EventManager.RegisterRoutedEvent(nameof(SelectChanged), RoutingStrategy.Bubble, typeof(RoutedEvent), typeof(ScatterControl));
        private static RoutedEventArgs SelectChangedEventArgs = new RoutedEventArgs(SelectChangedEvent);

        public event RoutedEventHandler SelectChanged {
            add => AddHandler(SelectChangedEvent, value);
            remove => RemoveHandler(SelectChangedEvent, value);
        }
        #endregion

        protected override void OnRender(DrawingContext drawingContext) {
            base.OnRender(drawingContext);
            FocusedPoint = focus?.Center;
            SelectedPoint = select?.Center;
        }

        #region Event handler
        static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as ScatterControl;
            if (chart == null) return;

            chart.dataType = null;

            if (chart.cv != null) {
                chart.cv.CurrentChanged -= chart.OnCurrentChanged;
            }
            if (e.OldValue is INotifyCollectionChanged collectionOld) {
                collectionOld.CollectionChanged -= chart.ItemsSourceCollectionChanged;
            }

            chart.cv = null;
            if (e.NewValue == null) return;

            chart.cv = CollectionViewSource.GetDefaultView(e.NewValue) as CollectionView;
            chart.cv.CurrentChanged += chart.OnCurrentChanged;
            if (e.NewValue is INotifyCollectionChanged collectionNew) {
                collectionNew.CollectionChanged += chart.ItemsSourceCollectionChanged;
            }

            if (chart.cv.Count == 0) return;
            chart.dataType = chart.cv.GetItemAt(0).GetType();

            if (chart.HorizontalPropertyName != null)
                chart.hPropertyReflection = chart.dataType.GetProperty(chart.HorizontalPropertyName);
            if (chart.VerticalPropertyName != null)
                chart.vPropertyReflection = chart.dataType.GetProperty(chart.VerticalPropertyName);
            if (chart.SelectedItem != null)
                chart.cv.MoveCurrentTo(chart.SelectedItem);

            chart.SetDrawingVisuals();
            chart.Update();
        }

        void OnCurrentChanged(object obj, EventArgs e) {
            if (cv == null) return;
            var item = cv.CurrentItem;
            SelectedItem = item;

            if (item == null) {
                SelectedPoint = null;
                return;
            }
            var x = hPropertyReflection.GetValue(item);
            var y = vPropertyReflection.GetValue(item);
            double xx = HorizontalAxis.TranslateToRenderPoint(x, FlippedX) * ActualWidth;
            double yy = VerticalAxis.TranslateToRenderPoint(y, FlippedY) * ActualHeight;
            var pos = new Point(xx, yy);
            select = visualChildren.OfType<AnnotatedDrawingVisual>().Argmin(dv => (dv.Center - pos).Length);
            SelectedPoint = select.Center;
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
                // new GeometryHitTestParameters(new EllipseGeometry(pt, 50d, 50d))
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
            if (dv != focus) {
                focus = dv;
                RaiseEvent(FocusChangedEventArgs);
            }
            if (FocusedItem != focus.Annotation) {
                FocusedItem = focus.Annotation;
            }
            if (FocusedPoint != focus.Center) {
                FocusedPoint = focus.Center;
            }
            return HitTestResultBehavior.Stop;
        }

        HitTestResultBehavior VisualSelectHitTest(HitTestResult result)
        {
            var dv = (AnnotatedDrawingVisual)result.VisualHit;
            if (dv != select) {
                select = dv;
                RaiseEvent(SelectChangedEventArgs);
            }
            if (select.Annotation != SelectedItem) {
                SelectedItem = select.Annotation;
            }
            if (select.Center != SelectedPoint) {
                SelectedPoint = select.Center;
            }
            return HitTestResultBehavior.Stop;
        }
        #endregion
    }
}
