using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

using CompMs.Common.Extension;
using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.LineChart
{
    public class LineChartControl : ChartBaseControl
    {
        #region DependencyProperty
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource), typeof(System.Collections.IEnumerable), typeof(LineChartControl),
            new PropertyMetadata(default(System.Collections.IEnumerable), OnItemsSourceChanged)
            );

        public static readonly DependencyProperty HorizontalPropertyNameProperty = DependencyProperty.Register(
            nameof(HorizontalPropertyName), typeof(string), typeof(LineChartControl),
            new PropertyMetadata(default(string), OnHorizontalPropertyNameChanged)
            );

        public static readonly DependencyProperty VerticalPropertyNameProperty = DependencyProperty.Register(
            nameof(VerticalPropertyName), typeof(string), typeof(LineChartControl),
            new PropertyMetadata(default(string), OnVerticalPropertyNameChanged)
            );

        public static readonly DependencyProperty LinePenProperty = DependencyProperty.Register(
            nameof(LinePen), typeof(Pen), typeof(LineChartControl),
            new PropertyMetadata(null)
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

        public Pen LinePen
        {
            get => (Pen)GetValue(LinePenProperty);
            set => SetValue(LinePenProperty, value);
        }
        #endregion

        #region field
        private CollectionView cv;
        private Type dataType;
        private PropertyInfo hPropertyReflection;
        private PropertyInfo vPropertyReflection;
        private DrawingVisual dv;
        private Data[] datas;
        #endregion

        static LineChartControl() {
            var pen = new Pen(Brushes.Black, 1) { LineJoin = PenLineJoin.Bevel };
            pen.Freeze();
            LinePenProperty.OverrideMetadata(typeof(Pen), new PropertyMetadata(pen));
        }

        public LineChartControl() : base() {
            dv = new DrawingVisual { Clip = new RectangleGeometry(new Rect(RenderSize)) };
            visualChildren.Add(dv);
        }

        protected override void Update()
        {
            if (  HorizontalAxis == null
               || VerticalAxis == null
               || LinePen == null
               || datas == null
               )
                return;

            var dc = dv.RenderOpen();

            var points = ValuesToRenderPositions(datas, ActualWidth, ActualHeight);

            if (points.Count != 0)
                for (int i = 1; i < points.Count; ++i)
                    dc.DrawLine(LinePen, points[i - 1], points[i]);
            
            dc.Close();
        }

        List<Point> ValuesToRenderPositions(IReadOnlyList<Data> ds, double actualWidth, double actualHeight) {
            var points = new List<Point>(ds.Count);

            var xs = HorizontalAxis.TranslateToRenderPoints(ds.Select(d => d.x), FlippedX);
            var ys = VerticalAxis.TranslateToRenderPoints(ds.Select(d => d.y), FlippedY);

            return xs.Zip(ys, (x, y) => new Point(x * actualWidth, y * actualHeight)).ToList();
        }

        void SetHorizontalDatas() {
            if (dataType == null || HorizontalPropertyName == null) return;

            hPropertyReflection = dataType.GetProperty(HorizontalPropertyName);
            foreach ((var obj, var idx) in cv.Cast<object>().WithIndex())
                datas[idx].x = hPropertyReflection.GetValue(obj);
        }

        void SetVerticalDatas() {
            if (dataType == null || VerticalPropertyName == null) return;

            vPropertyReflection = dataType.GetProperty(VerticalPropertyName);
            foreach ((var obj, var idx) in cv.Cast<object>().WithIndex())
                datas[idx].y = vPropertyReflection.GetValue(obj);
        }

        #region Event handler
        static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as LineChartControl;
            if (chart == null || chart.ItemsSource == null) return;

            var enumerator = chart.ItemsSource.GetEnumerator();
            if (!enumerator.MoveNext()) return;
            chart.dataType = enumerator.Current.GetType();
            chart.cv = CollectionViewSource.GetDefaultView(chart.ItemsSource) as CollectionView;
            chart.datas = Enumerable.Range(0, chart.cv.Count).Select(_ => new Data()).ToArray();

            chart.SetHorizontalDatas();
            chart.SetVerticalDatas();

            chart.Update();
        }

        static void OnHorizontalPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as LineChartControl;
            if (chart == null) return;

            chart.SetHorizontalDatas();

            chart.Update();
        }

        static void OnVerticalPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as LineChartControl;
            if (chart == null) return;

            chart.SetVerticalDatas();

            chart.Update();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) {
            dv.Clip = new RectangleGeometry(new Rect(RenderSize));
            base.OnRenderSizeChanged(sizeInfo);
        }
        #endregion

        class Data
        {
            public object x, y;
        }
    }

}
