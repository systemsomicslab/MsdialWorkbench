using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Base
{
    public class DefaultControl : Control
    {
        public DrawingImage Chart
        {
            get => (DrawingImage)GetValue(ChartProperty);
            set => SetValue(ChartProperty, value);
        }
        public static readonly DependencyProperty ChartProperty = DependencyProperty.Register(
            nameof(Chart), typeof(DrawingImage), typeof(DefaultControl),
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
                DrawingChart.RenderSize = new Size(chart.ActualWidth, chart.ActualHeight);
            }
        }

        void OnChartSizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawingChart.RenderSize = e.NewSize;
        }

        static void OnDrawingChartPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as DefaultControl;
            if (control == null) return;
            {
                if (e.OldValue is DrawingChartBase drawingChart)
                    drawingChart.PropertyChanged -= (_s, _e) => control.Chart = new DrawingImage(drawingChart.CreateChart());
            }
            {
                if (e.NewValue is DrawingChartBase drawingChart)
                {
                    drawingChart.PropertyChanged += (_s, _e) => control.Chart = new DrawingImage(drawingChart.CreateChart());
                    control.Chart = new DrawingImage(drawingChart.CreateChart());
                }
            }
        }
    }
}
