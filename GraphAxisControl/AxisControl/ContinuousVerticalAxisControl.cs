using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Msdial.AxisControl
{
    public class ContinuousVerticalAxisControl : FrameworkElement
    {
        #region Property
        public double LongTick { get; set; } = 10;
        public double ShortTick { get; set; } = 5;
        public AxisDirection Direction { get; set; } = AxisDirection.Right;
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
        // public double Rotate { get; set; }
        #endregion

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            ContinuousVerticalAxisPainter.Draw(
                drawingContext,
                new Point(0,0), new Vector(ActualWidth, ActualHeight),
                YMin, YMax, LongTick, ShortTick, Direction
            );
        }

        #region DependencyProperty
        public static readonly DependencyProperty YMinProperty = DependencyProperty.Register(
            "YMin", typeof(double), typeof(ContinuousVerticalAxisControl),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public static readonly DependencyProperty YMaxProperty = DependencyProperty.Register(
            "YMax", typeof(double), typeof(ContinuousVerticalAxisControl),
            new FrameworkPropertyMetadata(100d, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        #endregion
    }
}
