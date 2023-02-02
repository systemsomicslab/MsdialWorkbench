using System;
using System.Windows;
using System.Windows.Media;

namespace CompMs.Graphics.Core.Base
{
    public class AnnotatedDrawingVisual : DrawingVisual
    {
        public object Annotation { get; private set; }
        public Point Center { get; set; }

        public AnnotatedDrawingVisual(object obj)
        {
            Annotation = obj;
        }
    }
}
