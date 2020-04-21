using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using PlottingControls.Base;

namespace PlottingControls.GraphAxis
{
    public class CategoryVerticalAxisControl : PlottingBase
    {
        #region Property
        public double LongTick { get; set; } = 5;
        public IReadOnlyList<string> YLabels
        {
            get => (IReadOnlyList<string>)GetValue(YLabelsProperty);
            set => SetValue(YLabelsProperty, value);
        }
        public double Rotate { get; set; }
        #endregion

        public CategoryVerticalAxisControl()
        {
            Xfreeze = true;
        }

        protected override void DrawChart(DrawingContext drawingContext)
        {
            if(YPositions != null)
                CategoryVerticalAxisPainter.Draw(
                    drawingContext,
                    new Point(0,0), new Vector(ActualWidth, ActualHeight),
                    YDisplayMin, YDisplayMax, YPositions, YLabels,
                    LongTick, Rotate
                );
        }

        #region DependencyProperty
        public static readonly DependencyProperty YLabelsProperty = DependencyProperty.Register(
            "YLabels", typeof(IReadOnlyList<string>), typeof(CategoryVerticalAxisControl),
            new FrameworkPropertyMetadata(new string[] { }, FrameworkPropertyMetadataOptions.AffectsRender)
        );
        #endregion
    }
}
