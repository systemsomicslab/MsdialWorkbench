using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

using CompMs.Graphics.Core.Adorner;
using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.LineChart
{
    public class LineChart : FrameworkElement
    {
        #region DependencyProperty
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource), typeof(ObservableCollection<DataSeries>), typeof(LineChart),
            new PropertyMetadata(default(ObservableCollection<DataSeries>), OnItemsSourceChanged)
            );

        public static readonly DependencyProperty PensProperty = DependencyProperty.Register(
            nameof(Brushes), typeof(IList<Pen>), typeof(LineChart),
            new PropertyMetadata(default(IList<Pen>))
            );

        public static readonly DependencyProperty ChartAreaProperty = DependencyProperty.Register(
            nameof(ChartArea), typeof(Rect), typeof(LineChart),
            new PropertyMetadata(default(Rect), OnChartAreaChanged)
            );

        public static readonly DependencyProperty InitialAreaProperty = DependencyProperty.Register(
            nameof(InitialArea), typeof(Rect), typeof(LineChart),
            new PropertyMetadata(default(Rect))
            );
        #endregion

        #region Property
        public ObservableCollection<DataSeries> ItemsSource
        {
            get => (ObservableCollection<DataSeries>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public IList<Pen> Pens
        {
            get => (IList<Pen>)GetValue(PensProperty);
            set => SetValue(PensProperty, value);
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
        #endregion

        #region field
        private VisualCollection visualChildren;
        private List<PropertyChangedEventHandler> handlers;
        private ToolTip tooltip;
        private ICollectionView cv;
        private RubberAdorner adorner;
        private Point zoomInitial;
        private Point moveCurrent;
        private bool moving;
        #endregion

        public LineChart()
        {
            visualChildren = new VisualCollection(this);
            handlers = new List<PropertyChangedEventHandler>();
            tooltip = new ToolTip();
            ToolTip = tooltip;
            ToolTipService.SetInitialShowDelay(this, 0);

            SizeChanged += OnSizeChanged;
            MouseWheel += ZoomOnMouseWheel;
            MouseRightButtonDown += ZoomOnMouseRightButtonDown;
            MouseRightButtonUp += ZoomOnMouseRightButtonUp;
            MouseMove += ZoomOnMouseMove;
            MouseLeftButtonDown += MoveOnMouseLeftButtonDown;
            MouseLeftButtonUp += MoveOnMouseLeftButtonUp;
            MouseMove += MoveOnMouseMove;
            MouseLeftButtonDown += ResetOnDoubleClick;
            MouseMove += OnFocusDataSeries;
            MouseLeftButtonDown += SelectDataSeriesOnClick;
        }

        private void AddDrawingVisual(DataSeries ds)
        {
            var dv = new DrawingVisualDataSeries(ds);
            DrawDrawingVisual(dv);

            var index = VisualChildrenLowerBound(ds);
            visualChildren.Insert(index, dv);

            PropertyChangedEventHandler handler = (s, e) => DrawDrawingVisual(dv);
            handlers.Insert(index, handler);
            ds.PropertyChanged += handler;
        }

        private void RemoveDrawingVisual(DataSeries dp)
        {
            var index = VisualChildrenLowerBound(dp);

            if (((DrawingVisualDataSeries)visualChildren[index]).DataSeries.ID != dp.ID) return;

            visualChildren.RemoveAt(index);

            dp.PropertyChanged -= handlers[index];
            handlers.RemoveAt(index);
        }

        private void DrawDrawingVisual(DrawingVisualDataSeries dv)
        {
            var ds = dv.DataSeries;
            dv.Clip = new RectangleGeometry(new Rect(RenderSize));
            var dc = dv.RenderOpen();
            var lineGeometry = new PathGeometry();
            var path = new PathFigure();
            if (ds.Datas.Count != 0)
            {
                path.StartPoint = ConvertValueToRenderPosition(new Point(ds.Datas[0].X, ds.Datas[0].Y));
                foreach(var dp in ds.Datas.Skip(1))
                    path.Segments.Add(new LineSegment() { Point = ConvertValueToRenderPosition(new Point(dp.X, dp.Y)) });
            }
            path.Freeze();
            lineGeometry.Figures = new PathFigureCollection { path };
            dc.DrawGeometry(null, Pens[ds.Type % Pens.Count], lineGeometry);
            dc.Close();
        }

        static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as LineChart;
            chart.cv = CollectionViewSource.GetDefaultView(chart.ItemsSource);
            if (chart == null) return;

            if (e.OldValue != null)
            {
                if (e.OldValue is ObservableCollection<DataSeries> dss)
                {
                    chart.visualChildren.Clear();
                    foreach (var ds in dss.OrderByDescending(ds => ds.ID))
                        chart.RemoveDrawingVisual(ds);
                    chart.ItemsSource.CollectionChanged -= chart.OnItemsSourceElementChanged;
                }
            }
            if (e.NewValue != null)
            {
                if (e.NewValue is ObservableCollection<DataSeries> dss)
                {
                    var area = new Rect(
                        new Point(dss.Min(ds => ds.Datas.Min(dp => dp.X)), dss.Min(ds => ds.Datas.Min(dp => dp.Y))),
                        new Point(dss.Max(ds => ds.Datas.Max(dp => dp.X)), dss.Max(ds => ds.Datas.Max(dp => dp.Y)))
                        );
                    area.Inflate(area.Width * 0.05, area.Height * 0.05);
                    chart.InitialArea = area;
                    chart.ChartArea = area;
                    foreach (var ds in dss.OrderBy(ds => ds.ID))
                        chart.AddDrawingVisual(ds);
                    chart.ItemsSource.CollectionChanged += chart.OnItemsSourceElementChanged;
                }
            }
        }

        static void OnChartAreaChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as LineChart;
            if (chart == null) return;

            foreach (var dv in chart.visualChildren)
                chart.DrawDrawingVisual((DrawingVisualDataSeries)dv);
        }

        void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            foreach (var dv in visualChildren)
                DrawDrawingVisual((DrawingVisualDataSeries)dv);
        }

        void OnItemsSourceElementChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems == null || e.NewItems.Count == 0) break;
                    foreach (var item in e.NewItems)
                        AddDrawingVisual((DataSeries)item);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems == null || e.OldItems.Count == 0) break;
                    foreach (var item in e.OldItems)
                        RemoveDrawingVisual((DataSeries)item);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    if (e.OldItems != null && e.OldItems.Count != 0)
                        foreach (var item in e.OldItems)
                            RemoveDrawingVisual((DataSeries)item);
                    if (e.NewItems != null && e.NewItems.Count != 0)
                        foreach (var item in e.NewItems)
                            AddDrawingVisual((DataSeries)item);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    foreach (var dv in visualChildren)
                        RemoveDrawingVisual(((DrawingVisualDataSeries)dv).DataSeries);
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
            }
        }

        void ZoomOnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var p = e.GetPosition(this);
            var delta = e.Delta;
            var scale = 1 - 0.1 * Math.Sign(delta);

            var xmin = p.X * (1 - scale);
            var xmax = p.X + (ActualWidth - p.X) * scale;
            var ymin = p.Y * (1 - scale);
            var ymax = p.Y + (ActualHeight - p.Y) * scale;

                ChartArea = Rect.Intersect(
                    new Rect(
                        ConvertRenderPositionToValue(new Point(xmin, ymin)),
                        ConvertRenderPositionToValue(new Point(xmax, ymax))
                        ),
                    InitialArea
                    );
        }

        void ZoomOnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            zoomInitial = e.GetPosition(this);
            adorner = new RubberAdorner(this, zoomInitial);
            CaptureMouse();
        }

        void ZoomOnMouseMove(object sender, MouseEventArgs e)
        {
            if (adorner != null)
            {
                var initial = zoomInitial;
                var current = e.GetPosition(this);
                adorner.Offset = current - initial;
            }
        }

        void ZoomOnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (adorner != null)
            {
                ReleaseMouseCapture();
                adorner.Detach();
                adorner = null;
                ChartArea = Rect.Intersect(
                    new Rect(
                        ConvertRenderPositionToValue(zoomInitial),
                        ConvertRenderPositionToValue(e.GetPosition(this))
                        ),
                    InitialArea
                    );
            }
        }

        void MoveOnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            moveCurrent = e.GetPosition(this);
            moving = true;
            CaptureMouse();
        }

        void MoveOnMouseMove(object sender, MouseEventArgs e)
        {
            if (moving)
            {
                var previous = moveCurrent;
                moveCurrent = e.GetPosition(this);
                var area = Rect.Offset(new Rect(RenderSize), previous - moveCurrent);
                var cand = new Rect(
                        ConvertRenderPositionToValue(area.TopLeft),
                        ConvertRenderPositionToValue(area.BottomRight)
                        );
                if (InitialArea.Contains(cand))
                    ChartArea = cand;
            }
        }

        void MoveOnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (moving)
            {
                moving = false;
                ReleaseMouseCapture();
            }
        }

        void ResetOnDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ChartArea = InitialArea;
            }
        }

        Point ConvertRenderPositionToValue(Point p)
        {
            return new Point(
                p.X / ActualWidth * ChartArea.Width + ChartArea.Left,
                ChartArea.Bottom - p.Y / ActualHeight * ChartArea.Height
                );
        }

        Point ConvertValueToRenderPosition(Point p)
        {
            return new Point(
                (p.X - ChartArea.Left) / ChartArea.Width * ActualWidth,
                (ChartArea.Bottom - p.Y) / ChartArea.Height * ActualHeight
                );
        }

        void OnFocusDataSeries(object sender, MouseEventArgs e)
        {
            var pt = e.GetPosition(this);

            tooltip.IsOpen = false;
            tooltip.Content = "";
            VisualTreeHelper.HitTest(this,
                new HitTestFilterCallback(DataSeriesHitTestFilter),
                new HitTestResultCallback(DataSeriesTipHitTest),
                new PointHitTestParameters(pt)
                );
        }

        void SelectDataSeriesOnClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                var pt = e.GetPosition(this);

                VisualTreeHelper.HitTest(this,
                    new HitTestFilterCallback(DataSeriesHitTestFilter),
                    new HitTestResultCallback(DataSeriesSelectHitTest),
                    new PointHitTestParameters(pt)
                    );

                }
        }

        HitTestFilterBehavior DataSeriesHitTestFilter(DependencyObject d)
        {
            if (d is DrawingVisualDataSeries)
                return HitTestFilterBehavior.Continue;
            return HitTestFilterBehavior.ContinueSkipSelf;
        }

        HitTestResultBehavior DataSeriesTipHitTest(HitTestResult result)
        {
            var focussed = (DrawingVisualDataSeries)result.VisualHit;
            tooltip.Content = focussed.DataSeries;
            tooltip.IsOpen = true;
            return HitTestResultBehavior.Stop;
        }

        HitTestResultBehavior DataSeriesSelectHitTest(HitTestResult result)
        {
            var focussed = (DrawingVisualDataSeries)result.VisualHit;
            cv.MoveCurrentTo(focussed.DataSeries);
            return HitTestResultBehavior.Stop;
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }

        int VisualChildrenLowerBound(DataSeries ds)
        {
            int lo = 0, hi = visualChildren.Count;
            while (lo < hi)
            {
                var mid = (lo + hi) / 2;
                if (((DrawingVisualDataSeries)visualChildren[mid]).DataSeries.ID <= ds.ID)
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
