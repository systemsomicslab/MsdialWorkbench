using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace CompMs.Graphics.Core.Base
{
    public interface IDrawingChart
    {
        Size RenderSize { get; set; }
        Rect ChartArea { get; set; }
        Rect InitialArea { get; set; }

        Drawing CreateChart();

        Point RealToImagine(Point point);
        Point ImagineToReal(Point point);
    }
}
