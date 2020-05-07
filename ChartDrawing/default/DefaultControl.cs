using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
        public double ChartHeight
        {
            get => (double)GetValue(ChartHeightProperty);
            set => SetValue(ChartHeightProperty, value);
        }
        public static readonly DependencyProperty ChartHeightProperty = DependencyProperty.Register(
            nameof(ChartHeight), typeof(double), typeof(DefaultControl),
            new FrameworkPropertyMetadata(default(double),
                FrameworkPropertyMetadataOptions.AffectsRender)
            );
        public double ChartWidth
        {
            get => (double)GetValue(ChartWidthProperty);
            set => SetValue(ChartWidthProperty, value);
        }
        public static readonly DependencyProperty ChartWidthProperty = DependencyProperty.Register(
            nameof(ChartWidth), typeof(double), typeof(DefaultControl),
            new FrameworkPropertyMetadata(default(double),
                FrameworkPropertyMetadataOptions.AffectsRender)
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
                ChartHeight = chart.ActualHeight;
                ChartWidth = chart.ActualWidth;
            }
        }

        void OnChartSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ChartHeight = e.NewSize.Height;
            ChartWidth = e.NewSize.Width;
        }
    }
}
