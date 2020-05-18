using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Base
{
    public class DefaultControl : Control
    {
        public Drawing Chart
        {
            get => (Drawing)GetValue(ChartProperty);
            set => SetValue(ChartProperty, value);
        }
        public static readonly DependencyProperty ChartProperty = DependencyProperty.Register(
            nameof(Chart), typeof(Drawing), typeof(DefaultControl),
            new FrameworkPropertyMetadata(default,
                FrameworkPropertyMetadataOptions.AffectsRender)
            );
        public IDrawingChart DrawingChart
        {
            get => (IDrawingChart)GetValue(DrawingChartProperty);
            set => SetValue(DrawingChartProperty, value);
        }
        public static readonly DependencyProperty DrawingChartProperty = DependencyProperty.Register(
            nameof(DrawingChart), typeof(IDrawingChart), typeof(DefaultControl),
            new PropertyMetadata(default, OnDrawingChartPropertyChanged)
            );

        FrameworkElement chart;

        static DefaultControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(DefaultControl),
                new FrameworkPropertyMetadata(typeof(DefaultControl))
                );
        }

        public DefaultControl() { }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (chart != null)
                chart.SizeChanged -= OnChartSizeChanged;
            chart = GetTemplateChild("PART_Chart") as FrameworkElement;
            if (chart != null)
            {
                chart.SizeChanged += OnChartSizeChanged;
                if (DrawingChart != null)
                    DrawingChart.RenderSize = chart.RenderSize;
            }
        }

        void OnChartSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DrawingChart != null)
                DrawingChart.RenderSize = e.NewSize;
        }

        static void OnDrawingChartPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as DefaultControl;
            if (control == null) return;
            {
                if (e.OldValue is DrawingChartBase drawingChart)
                    drawingChart.PropertyChanged -= (_s, _e) => control.Chart = drawingChart.CreateChart();
            }
            {
                if (e.NewValue is DrawingChartBase drawingChart)
                {
                    if (control.chart != null)
                        drawingChart.RenderSize = control.chart.RenderSize;
                    drawingChart.PropertyChanged += (_s, _e) => control.Chart = drawingChart.CreateChart();
                    control.Chart = drawingChart.CreateChart();
                }
            }
        }
    }
}
