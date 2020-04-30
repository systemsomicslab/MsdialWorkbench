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
            new FrameworkPropertyMetadata(null, ResetManager)
        );

        /*
        static void OnDataMatrixChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as HeatmapCoreControl;
            if (control != null)
            {
                var chartmanager = new HeatmapManager((double[,])e.NewValue, control.XPositions, control.YPositions, control.Gsc );
                control.ChartDrawingArea = chartmanager.ChartArea;
                control.LimitDrawingArea = chartmanager.ChartArea;
                control.XPositions = chartmanager.XPositions;
                control.YPositions = chartmanager.YPositions;
                control.Gsc = chartmanager.Gsc;
                control.ChartManager = chartmanager;
            }
        }
        */

        public GradientStopCollection Gsc
        {
            get => (GradientStopCollection)GetValue(GscProperty);
            set => SetValue(GscProperty, value);
        }
        public static readonly DependencyProperty GscProperty = DependencyProperty.Register(
            nameof(Gsc), typeof(LinearGradientBrush), typeof(HeatmapCoreControl),
            new FrameworkPropertyMetadata(null, ResetManager)
        );

        public IReadOnlyList<double> XPositions
        {
            get => (IReadOnlyList<double>)GetValue(XPositionsProperty);
            set => SetValue(XPositionsProperty, value);
        }
        public static readonly DependencyProperty XPositionsProperty = DependencyProperty.Register(
            nameof(XPositions), typeof(IReadOnlyList<double>), typeof(HeatmapCoreControl),
            new FrameworkPropertyMetadata(null, ResetManager)
            );
        public IReadOnlyList<double> YPositions
        {
            get => (IReadOnlyList<double>)GetValue(YPositionsProperty);
            set => SetValue(YPositionsProperty, value);
        }
        public static readonly DependencyProperty YPositionsProperty = DependencyProperty.Register(
            nameof(YPositions), typeof(IReadOnlyList<double>), typeof(HeatmapCoreControl),
            new FrameworkPropertyMetadata(null, ResetManager)
            );

        static void ResetManager(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as ChartControl;
            if (control != null) control.ChartManager = null;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (ChartManager == null) UpdateManager();
            base.OnRender(drawingContext);
        }

        private void UpdateManager()
        {
            var chartmanager = new HeatmapManager(DataMatrix, XPositions, YPositions, Gsc);
            ChartDrawingArea = chartmanager.ChartArea;
            LimitDrawingArea = chartmanager.ChartArea;
            // XPositions = chartmanager.XPositions;
            // YPositions = chartmanager.YPositions;
            Gsc = chartmanager.Gsc;
            ChartManager = chartmanager;
        }
    }
}
