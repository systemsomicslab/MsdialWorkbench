using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using PlottingControls.Base;

namespace PlottingControls.GraphAxis
{
    public class ContinuousVerticalAxisControl : PlottingBase
    {
        #region Property
        public double LongTick { get; set; } = 10;
        public double ShortTick { get; set; } = 5;
        // public double Rotate { get; set; }
        public AxisDirection Direction { get; set; } = AxisDirection.Right;
        #endregion

        public ContinuousVerticalAxisControl()
        {
            Xfreeze = true;
        }

        protected override void PlotChart(DrawingContext drawingContext)
        {
            ContinuousVerticalAxisPainter.Draw(
                drawingContext,
                new Point(0,0), new Vector(ActualWidth, ActualHeight),
                YDisplayMin, YDisplayMax, LongTick, ShortTick, Direction
            );
        }
    }
}
