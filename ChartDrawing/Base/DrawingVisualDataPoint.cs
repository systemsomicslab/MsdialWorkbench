using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace CompMs.Graphics.Core.Base
{
    public class DrawingVisualDataPoint : DrawingVisual
    {
        public DataPoint DataPoint { get; private set; }

        public DrawingVisualDataPoint(DataPoint dp)
        {
            DataPoint = dp;
        }
    }
}
