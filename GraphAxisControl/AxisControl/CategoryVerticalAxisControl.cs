using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Msdial.AxisControl
{
    public class CategoryVerticalAxisControl : FrameworkElement
    {
        #region Property
        public double LongTick { get; set; } = 5;
        public IReadOnlyList<string> YLabels
        {
            get => (IReadOnlyList<string>)GetValue(YLabelsProperty);
            set => SetValue(YLabelsProperty, value);
        }
        public IReadOnlyList<double> YPositions
        {
            get => (IReadOnlyList<double>)GetValue(YPositionsProperty);
            set => SetValue(YPositionsProperty, value);
        }
        public double YMin
        {
            get => (double)GetValue(YMinProperty);
            set => SetValue(YMinProperty, value);
        }
        public double YMax
        {
            get => (double)GetValue(YMaxProperty);
            set => SetValue(YMaxProperty, value);
        }
        public double Rotate { get; set; }
        #endregion

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if(YPositions != null)
                CategoryVerticalAxisPainter.Draw(
                    drawingContext,
                    new Point(0,0), new Vector(ActualWidth, ActualHeight),
                    YMin, YMax, YPositions, YLabels,
                    LongTick, Rotate
                );
        }

        #region DependencyProperty
        public static readonly DependencyProperty YPositionsProperty = DependencyProperty.Register(
            "YPositions", typeof(IReadOnlyList<double>), typeof(CategoryVerticalAxisControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public static readonly DependencyProperty YLabelsProperty = DependencyProperty.Register(
            "YLabels", typeof(IReadOnlyList<string>), typeof(CategoryVerticalAxisControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public static readonly DependencyProperty YMinProperty = DependencyProperty.Register(
            "YMin", typeof(double), typeof(CategoryVerticalAxisControl),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public static readonly DependencyProperty YMaxProperty = DependencyProperty.Register(
            "YMax", typeof(double), typeof(CategoryVerticalAxisControl),
            new FrameworkPropertyMetadata(100d, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        #endregion
    }
}
