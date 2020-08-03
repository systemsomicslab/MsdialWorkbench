using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace CompMs.Graphics.Core.Base
{
    public class DrawingVisualDataSeries : DrawingVisual
    {
        public DataSeries DataSeries { get; private set; }

        public DrawingVisualDataSeries(DataSeries ds)
        {
            DataSeries = ds;
        }
    }
}
