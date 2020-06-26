using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

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
        #endregion

        public LineChartControl() : base() {
            var pen = new Pen(Brushes.Black, 1) { LineJoin = PenLineJoin.Bevel };
            pen.Freeze();
            LinePen = pen;
        }

        protected override void Update()
        {
            visualChildren.Clear();

            if (  hPropertyReflection == null
               || vPropertyReflection == null
               || HorizontalAxis == null
               || VerticalAxis == null
               || LinePen == null
               || cv == null
               )
                return;

            var dv = new DrawingVisual
            {
                Clip = new RectangleGeometry(new Rect(RenderSize))
            };
            var dc = dv.RenderOpen();
            var lineGeometry = new PathGeometry();
            var path = new PathFigure();
            if (cv.Count != 0)
            {
                path.StartPoint = ValueToRenderPosition(
                    hPropertyReflection.GetValue(cv.GetItemAt(0)),
                    vPropertyReflection.GetValue(cv.GetItemAt(0))
                    );
                for (int i = 1; i < cv.Count; ++i)
                {
                    var o = cv.GetItemAt(i);

                    path.Segments.Add(new LineSegment()
                    {
                        Point = ValueToRenderPosition(
                            hPropertyReflection.GetValue(o),
                            vPropertyReflection.GetValue(o)
                            )
                    });
                }
            }
            path.Freeze();
            lineGeometry.Figures = new PathFigureCollection { path };
            dc.DrawGeometry(null, LinePen, lineGeometry);
            dc.Close();
            visualChildren.Add(dv);
        }

        Point ValueToRenderPosition(object x, object y)
        {
            var xx = HorizontalAxis.ValueToRenderPosition(x) * ActualWidth;
            var yy = VerticalAxis.ValueToRenderPosition(y) * ActualHeight;
            return new Point(xx, yy);
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

            if (chart.HorizontalPropertyName != null)
                chart.hPropertyReflection = chart.dataType.GetProperty(chart.HorizontalPropertyName);
            if (chart.VerticalPropertyName != null)
                chart.vPropertyReflection = chart.dataType.GetProperty(chart.VerticalPropertyName);

            chart.Update();
        }

        static void OnHorizontalPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as LineChartControl;
            if (chart == null) return;

            if (chart.dataType != null)
                chart.hPropertyReflection = chart.dataType.GetProperty((string)e.NewValue);

            chart.Update();
        }

        static void OnVerticalPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = d as LineChartControl;
            if (chart == null) return;

            if (chart.dataType != null)
                chart.vPropertyReflection = chart.dataType.GetProperty((string)e.NewValue);

            chart.Update();
        }
        #endregion
    }
}
