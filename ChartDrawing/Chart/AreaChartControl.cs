using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Chart
{
    public class AreaChartControl : ChartBaseControl
    {
        #region DependencyProperty
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource), typeof(System.Collections.IEnumerable), typeof(AreaChartControl),
            new PropertyMetadata(default(System.Collections.IEnumerable), OnItemsSourceChanged)
            );

        public static readonly DependencyProperty HorizontalPropertyNameProperty = DependencyProperty.Register(
            nameof(HorizontalPropertyName), typeof(string), typeof(AreaChartControl),
            new PropertyMetadata(default(string), OnHorizontalPropertyNameChanged)
            );

        public static readonly DependencyProperty VerticalPropertyNameProperty = DependencyProperty.Register(
            nameof(VerticalPropertyName), typeof(string), typeof(AreaChartControl),
            new PropertyMetadata(default(string), OnVerticalPropertyNameChanged)
            );

        public static readonly DependencyProperty AreaBrushProperty = DependencyProperty.Register(
            nameof(AreaBrush), typeof(Brush), typeof(AreaChartControl),
            new PropertyMetadata(Brushes.Aqua)
            );
        #endregion

        #region Property
        public System.Collections.IEnumerable ItemsSource {
            get => (System.Collections.IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public string HorizontalPropertyName {
            get => (string)GetValue(HorizontalPropertyNameProperty);
            set => SetValue(HorizontalPropertyNameProperty, value);
        }

        public string VerticalPropertyName {
            get => (string)GetValue(VerticalPropertyNameProperty);
            set => SetValue(VerticalPropertyNameProperty, value);
        }

        public Brush AreaBrush {
            get => (Brush)GetValue(AreaBrushProperty);
            set => SetValue(AreaBrushProperty, value);
        }
        #endregion

        #region field
        private CollectionView cv;
        private Type dataType;
        private PropertyInfo hPropertyReflection;
        private PropertyInfo vPropertyReflection;
        #endregion

        protected override void Update() {
            visualChildren.Clear();

            if (hPropertyReflection == null
               || vPropertyReflection == null
               || HorizontalAxis == null
               || VerticalAxis == null
               || AreaBrush == null
               || cv == null
               )
                return;

            var dv = new DrawingVisual
            {
                Clip = new RectangleGeometry(new Rect(RenderSize))
            };
            var dc = dv.RenderOpen();
            var areaGeometry = new PathGeometry();
            var path = new PathFigure() { IsClosed = true };
            if (cv.Count != 0) {
                foreach (var o in cv) {
                    path.Segments.Add(new LineSegment()
                    {
                        Point = new Point(
                            HorizontalAxis.TranslateToRenderPoint(hPropertyReflection.GetValue(o), FlippedX, ActualWidth),
                            VerticalAxis.TranslateToRenderPoint(vPropertyReflection.GetValue(o), FlippedY, ActualHeight)
                            ),
                    });
                }
                var p = (path.Segments.First() as LineSegment).Point;
                p.Y = VerticalAxis.TranslateToRenderPoint(0d, FlippedY, ActualHeight);
                path.StartPoint = p;
                var q = (path.Segments.Last() as LineSegment).Point;
                q.Y = p.Y;
                path.Segments.Add(new LineSegment() { Point = q });
            }
            path.Freeze();
            areaGeometry.Figures = new PathFigureCollection { path };
            dc.DrawGeometry(AreaBrush, null, areaGeometry);
            dc.Close();
            visualChildren.Add(dv);
        }

        #region Event handler
        static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = d as AreaChartControl;
            if (chart == null) return;

            chart.dataType = null;
            chart.cv = null;

            if (chart.ItemsSource == null) {
                chart.Update();
                return;
            }
            var enumerator = chart.ItemsSource.GetEnumerator();
            if (!enumerator.MoveNext()) {
                chart.Update();
                return;
            }
            chart.dataType = enumerator.Current.GetType();
            chart.cv = CollectionViewSource.GetDefaultView(chart.ItemsSource) as CollectionView;

            if (chart.HorizontalPropertyName != null)
                chart.hPropertyReflection = chart.dataType.GetProperty(chart.HorizontalPropertyName);
            if (chart.VerticalPropertyName != null)
                chart.vPropertyReflection = chart.dataType.GetProperty(chart.VerticalPropertyName);

            chart.Update();
        }

        static void OnHorizontalPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = d as AreaChartControl;
            if (chart == null) return;

            if (chart.dataType != null)
                chart.hPropertyReflection = chart.dataType.GetProperty((string)e.NewValue);

            chart.Update();
        }

        static void OnVerticalPropertyNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var chart = d as AreaChartControl;
            if (chart == null) return;

            if (chart.dataType != null)
                chart.vPropertyReflection = chart.dataType.GetProperty((string)e.NewValue);

            chart.Update();
        }
        #endregion
    }
}
