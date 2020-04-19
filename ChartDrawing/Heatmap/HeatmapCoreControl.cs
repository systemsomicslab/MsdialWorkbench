using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Core.Heatmap
{
    public class HeatmapCoreControl : ChartControl
    {
        public double[,] DataMatrix
        {
            get => (double[,])GetValue(DataMatrixProperty);
            set => SetValue(DataMatrixProperty, value);
        }
        public static readonly DependencyProperty DataMatrixProperty = DependencyProperty.Register(
            nameof(DataMatrix), typeof(double[,]), typeof(HeatmapCoreControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnDataMatrixChanged)
        );

        static void OnDataMatrixChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as HeatmapCoreControl;
            if (control != null)
            {
                var chartmanager = new HeatmapManager(
                    (double[,])e.NewValue, control.XPositions, control.YPositions, control.Gsc );
                control.ChartDrawingArea = chartmanager.ChartArea;
                control.LimitDrawingArea = chartmanager.ChartArea;
                control.XPositions = chartmanager.XPositions;
                control.YPositions = chartmanager.YPositions;
                control.Gsc = chartmanager.Gsc;
                control.ChartManager = chartmanager;
            }
        }

        public GradientStopCollection Gsc
        {
            get => (GradientStopCollection)GetValue(GscProperty);
            set => SetValue(GscProperty, value);
        }
        public static readonly DependencyProperty GscProperty = DependencyProperty.Register(
            nameof(Gsc), typeof(LinearGradientBrush), typeof(HeatmapCoreControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public IReadOnlyList<double> XPositions
        {
            get => (IReadOnlyList<double>)GetValue(XPositionsProperty);
            set => SetValue(XPositionsProperty, value);
        }
        public static readonly DependencyProperty XPositionsProperty = DependencyProperty.Register(
            nameof(XPositions), typeof(IReadOnlyList<double>), typeof(HeatmapCoreControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender)
            );
        public IReadOnlyList<double> YPositions
        {
            get => (IReadOnlyList<double>)GetValue(YPositionsProperty);
            set => SetValue(YPositionsProperty, value);
        }
        public static readonly DependencyProperty YPositionsProperty = DependencyProperty.Register(
            nameof(YPositions), typeof(IReadOnlyList<double>), typeof(HeatmapCoreControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender)
            );
    }
}
