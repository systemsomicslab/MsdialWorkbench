using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace CompMs.Graphics.Scatter
{
    public class ScatterChart : FrameworkElement
    {
        #region DependencyProperty
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource), typeof(ObservableCollection<DataPoint>), typeof(ScatterChart),
            new PropertyMetadata(default(ObservableCollection<DataPoint>), OnItemsSourceChanged)
            );

        public static readonly DependencyProperty BrushesProperty = DependencyProperty.Register(
            nameof(Brushes), typeof(IList<Brush>), typeof(ScatterChart),
            new PropertyMetadata(default(IList<Brush>))
            );

        public static readonly DependencyProperty ChartAreaProperty = DependencyProperty.Register(
            nameof(ChartArea), typeof(Rect), typeof(ScatterChart),
            new PropertyMetadata(default(Rect), OnChartAreaChanged)
            );

        public static readonly DependencyProperty InitialAreaProperty = DependencyProperty.Register(
            nameof(InitialArea), typeof(Rect), typeof(ScatterChart),
            new PropertyMetadata(default(Rect))
            );

        public static readonly DependencyProperty RadiusProperty = DependencyProperty.Register(
            nameof(Radius), typeof(double), typeof(ScatterChart),
            new FrameworkPropertyMetadata(2d)
            );
        #endregion

        #region Property
        public ObservableCollection<DataPoint> ItemsSource
        {
            get => (ObservableCollection<DataPoint>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public IList<Brush> Brushes  
        {
            get => (IList<Brush>)GetValue(BrushesProperty);
            set => SetValue(BrushesProperty, value);
        }

        public Rect ChartArea
        {
            get => (Rect)GetValue(ChartAreaProperty);
            set => SetValue(ChartAreaProperty, value);
        }

        public Rect InitialArea
        {
            get => (Rect)GetValue(InitialAreaProperty);
            set => SetValue(InitialAreaProperty, value);
        }

        public double Radius
        {
            get => (double)GetValue(RadiusProperty);
            set => SetValue(RadiusProperty, value);
        }
        #endregion

        #region field
        private VisualCollection visualChildren;
        private List<PropertyChangedEventHandler> handlers;
        #endregion

        public ScatterChart()
        {
            visualChildren = new VisualCollection(this);
            handlers = new List<PropertyChangedEventHandler>();
            SizeChanged += OnSizeChanged;
        }

        private void AddDrawingVisual(DataPoint dp)
        {
            var dv = new DrawingVisualDataPoint(dp);
            DrawDrawingVisual(dv);

            var index = VisualChildrenLowerBound(dp);
            visualChildren.Insert(index, dv);

            PropertyChangedEventHandler handler = (s, e) => DrawDrawingVisual(dv);
            handlers.Insert(index, handler);
            dp.PropertyChanged += handler;
        }

        private void RemoveDrawingVisual(DataPoint dp)
        {
            var index = VisualChildrenLowerBound(dp);

            if (((DrawingVisualDataPoint)visualChildren[index]).DataPoint.ID != dp.ID) return;

            visualChildren.RemoveAt(index);

            dp.PropertyChanged -= handlers[index];
            handlers.RemoveAt(index);
        }

        private void DrawDrawingVisual(DrawingVisualDataPoint dv)
        {
            var dp = dv.DataPoint;
            dv.Clip = new RectangleGeometry(new Rect(RenderSize));
            var dc = dv.RenderOpen();
            dc.DrawEllipse(
                Brushes[dp.Type], null,
                new Point(
                    (dp.X - ChartArea.Left) / ChartArea.Width * ActualWidth,
                    (ChartArea.Bottom - dp.Y) / ChartArea.Height * ActualHeight
                    ),
                Radius, Radius);
            dc.Close();
        }

        static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as ScatterChart;
            if (chart == null) return;

            if (e.OldValue != null)
            {
                if (e.OldValue is ObservableCollection<DataPoint> dps)
                {
                    chart.visualChildren.Clear();
                    foreach (var dp in dps.OrderByDescending(dp => dp.ID))
                        chart.RemoveDrawingVisual(dp);
                    chart.ItemsSource.CollectionChanged -= chart.OnItemsSourceElementChanged;
                }
            }
            if (e.NewValue != null)
            {
                if (e.NewValue is ObservableCollection<DataPoint> dps)
                {
                    var area = new Rect(
                        new Point(dps.Min(dp => dp.X), dps.Min(dp => dp.Y)),
                        new Point(dps.Max(dp => dp.X), dps.Max(dp => dp.Y))
                        );
                    area.Inflate(area.Width * 0.05, area.Height * 0.05);
                    chart.InitialArea = area;
                    chart.ChartArea = area;
                    foreach (var dp in dps.OrderBy(dp => dp.ID))
                        chart.AddDrawingVisual(dp);
                    chart.ItemsSource.CollectionChanged += chart.OnItemsSourceElementChanged;
                }
            }
        }

        static void OnChartAreaChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as ScatterChart;
            if (chart == null) return;

            foreach (var dv in chart.visualChildren)
                chart.DrawDrawingVisual((DrawingVisualDataPoint)dv);
        }

        void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            foreach (var dv in visualChildren)
                DrawDrawingVisual((DrawingVisualDataPoint)dv);
        }

        void OnItemsSourceElementChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems == null || e.NewItems.Count == 0) break;
                    foreach (var item in e.NewItems)
                        AddDrawingVisual((DataPoint)item);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems == null || e.OldItems.Count == 0) break;
                    foreach (var item in e.OldItems)
                        RemoveDrawingVisual((DataPoint)item);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    if (e.OldItems != null && e.OldItems.Count != 0)
                        foreach (var item in e.OldItems)
                            RemoveDrawingVisual((DataPoint)item);
                    if (e.NewItems != null && e.NewItems.Count != 0)
                        foreach (var item in e.NewItems)
                            AddDrawingVisual((DataPoint)item);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    foreach (var dv in visualChildren)
                        RemoveDrawingVisual(((DrawingVisualDataPoint)dv).DataPoint);
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
            }
        }

        int VisualChildrenLowerBound(DataPoint dp)
        {
            int lo = 0, hi = visualChildren.Count;
            while (lo < hi)
            {
                var mid = (lo + hi) / 2;
                if (((DrawingVisualDataPoint)visualChildren[mid]).DataPoint.ID <= dp.ID)
                    lo = mid + 1;
                else
                    hi = mid;
            }
            return lo;
        }

        protected override int VisualChildrenCount => visualChildren.Count;
        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || visualChildren.Count <= index)
                throw new ArgumentOutOfRangeException();
            return visualChildren[index];
        }
    }
}
