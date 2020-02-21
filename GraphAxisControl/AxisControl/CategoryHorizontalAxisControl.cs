using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace Msdial.AxisControl
{
    public class CategoryHorizontalAxisControl : FrameworkElement
    {
        #region Property
        public double LongTick { get; set; } = 5;
        public IReadOnlyList<string> XLabels
        {
            get => (IReadOnlyList<string>)GetValue(XLabelsProperty);
            set => SetValue(XLabelsProperty, value);
        }
        public IReadOnlyList<double> XPositions
        {
            get => (IReadOnlyList<double>)GetValue(XPositionsProperty);
            set => SetValue(XPositionsProperty, value);
        }
        public double XMin
        {
            get => (double)GetValue(XMinProperty);
            set => SetValue(XMinProperty, value);
        }
        public double XMax
        {
            get => (double)GetValue(XMaxProperty);
            set => SetValue(XMaxProperty, value);
        }
        public double Rotate { get; set; }
        #endregion

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if(XPositions != null)
                CategoryHorizontalAxisPainter.Draw(
                    drawingContext,
                    new Point(0,0), new Vector(ActualWidth, ActualHeight),
                    XMin, XMax, XPositions, XLabels,
                    LongTick, Rotate
                );
        }

        #region DependencyProperty
        public static readonly DependencyProperty XPositionsProperty = DependencyProperty.Register(
            "XPositions", typeof(IReadOnlyList<double>), typeof(CategoryHorizontalAxisControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public static readonly DependencyProperty XLabelsProperty = DependencyProperty.Register(
            "XLabels", typeof(IReadOnlyList<string>), typeof(CategoryHorizontalAxisControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public static readonly DependencyProperty XMinProperty = DependencyProperty.Register(
            "XMin", typeof(double), typeof(CategoryHorizontalAxisControl),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        public static readonly DependencyProperty XMaxProperty = DependencyProperty.Register(
            "XMax", typeof(double), typeof(CategoryHorizontalAxisControl),
            new FrameworkPropertyMetadata(100d, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        #endregion
    }
}
