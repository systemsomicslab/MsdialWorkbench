using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using PlottingControls.Base;

namespace PlottingControls.GraphAxis
{
    public class CategoryHorizontalAxisControl : PlottingBase
    {
        #region Property
        public double LongTick { get; set; } = 5;
        public double Rotate { get; set; } = 0;

        public IReadOnlyList<string> XLabels
        {
            get => (IReadOnlyList<string>)GetValue(XLabelsProperty);
            set => SetValue(XLabelsProperty, value);
        }
        public static readonly DependencyProperty XLabelsProperty = DependencyProperty.Register(
            "XLabels", typeof(IReadOnlyList<string>), typeof(CategoryHorizontalAxisControl),
            new FrameworkPropertyMetadata(new string[] { }, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        #endregion

        public CategoryHorizontalAxisControl()
        {
            Yfreeze = true;
        }

        protected override void PlotChart(DrawingContext drawingContext)
        {
            if(XPositions != null)
                CategoryHorizontalAxisPainter.Draw(
                    drawingContext,
                    new Point(0,0), new Vector(ActualWidth, ActualHeight),
                    XDisplayMin, XDisplayMax, XPositions, XLabels,
                    LongTick, Rotate
                );
        }
    }
}
