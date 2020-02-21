using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Msdial.Heatmap
{
    public class HeatmapControl : FrameworkElement
    {
        #region Property
        public double[,] DataMatrix
        {
            get => (double[,])GetValue(DataMatrixProperty);
            set => SetValue(DataMatrixProperty, value);
        }
        public LinearGradientBrush Brush
        {
            get => (LinearGradientBrush)GetValue(BrushProperty);
            set => SetValue(BrushProperty, value);
        }
        public double ValueMin
        {
            get => (double)GetValue(ValueMinProperty);
            set => SetValue(ValueMinProperty, value);
        }
        public double ValueMax
        {
            get => (double)GetValue(ValueMaxProperty);
            set => SetValue(ValueMaxProperty, value);
        }
        public IReadOnlyList<double> XPositions
        {
            get => (IReadOnlyList<double>)GetValue(XPositionsProperty);
            set => SetValue(XPositionsProperty, value);
        }
        public IReadOnlyList<double> YPositions
        {
            get => (IReadOnlyList<double>)GetValue(YPositionsProperty);
            set => SetValue(YPositionsProperty, value);
        }
        public IReadOnlyList<double> XBorders
        {
            get => (IReadOnlyList<double>)GetValue(XBordersProperty);
            set => SetValue(XBordersProperty, value);
        }
        public IReadOnlyList<double> YBorders
        {
            get => (IReadOnlyList<double>)GetValue(YBordersProperty);
            set => SetValue(YBordersProperty, value);
        }
        public double XDisplayMin
        {
            get => (double)GetValue(XDisplayMinProperty);
            set => SetValue(XDisplayMinProperty, value);
        }
        public double XDisplayMax
        {
            get => (double)GetValue(XDisplayMaxProperty);
            set => SetValue(XDisplayMaxProperty, value);
        }
        public double YDisplayMin
        {
            get => (double)GetValue(YDisplayMinProperty);
            set => SetValue(YDisplayMinProperty, value);
        }
        public double YDisplayMax
        {
            get => (double)GetValue(YDisplayMaxProperty);
            set => SetValue(YDisplayMaxProperty, value);
        }
        #endregion

        public HeatmapControl() { }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if(DataMatrix != null)
                HeatmapPainter.Draw(
                    drawingContext,
                    DataMatrix,
                    ValueMin, ValueMax,
                    new Point(0,0), new Vector(ActualWidth, ActualHeight),
                    XDisplayMin, XDisplayMax,
                    YDisplayMin, YDisplayMax,
                    XBorders, YBorders,
                    Brush
                );
            // Console.WriteLine("OnRender called");
        }

        #region DependencyProperty
        public static readonly DependencyProperty DataMatrixProperty = DependencyProperty.Register(
            "DataMatrix", typeof(double[,]), typeof(HeatmapControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public static readonly DependencyProperty ValueMinProperty = DependencyProperty.Register(
            "ValueMin", typeof(double), typeof(HeatmapControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public static readonly DependencyProperty ValueMaxProperty = DependencyProperty.Register(
            "ValueMax", typeof(double), typeof(HeatmapControl),
            new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public static readonly DependencyProperty XPositionsProperty = DependencyProperty.Register(
            "XPositions", typeof(IReadOnlyList<double>), typeof(HeatmapControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public static readonly DependencyProperty YPositionsProperty = DependencyProperty.Register(
            "YPositions", typeof(IReadOnlyList<double>), typeof(HeatmapControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public static readonly DependencyProperty XBordersProperty = DependencyProperty.Register(
            "XBorders", typeof(IReadOnlyList<double>), typeof(HeatmapControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public static readonly DependencyProperty YBordersProperty = DependencyProperty.Register(
            "YBorders", typeof(IReadOnlyList<double>), typeof(HeatmapControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public static readonly DependencyProperty XDisplayMinProperty = DependencyProperty.Register(
            "XDisplayMin", typeof(double), typeof(HeatmapControl),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public static readonly DependencyProperty XDisplayMaxProperty = DependencyProperty.Register(
            "XDisplayMax", typeof(double), typeof(HeatmapControl),
            new FrameworkPropertyMetadata(100d, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public static readonly DependencyProperty YDisplayMinProperty = DependencyProperty.Register(
            "YDisplayMin", typeof(double), typeof(HeatmapControl),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public static readonly DependencyProperty YDisplayMaxProperty = DependencyProperty.Register(
            "YDisplayMax", typeof(double), typeof(HeatmapControl),
            new FrameworkPropertyMetadata(100d, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public static readonly DependencyProperty BrushProperty = DependencyProperty.Register(
            "Brush", typeof(LinearGradientBrush), typeof(HeatmapControl),
            new FrameworkPropertyMetadata(
                new LinearGradientBrush(Colors.Blue, Colors.Red, new Point(0, 0), new Point(0, 1)),
                FrameworkPropertyMetadataOptions.AffectsRender
            )
        );
        // private static void OnDataMatrixChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }
        #endregion
    }
}
