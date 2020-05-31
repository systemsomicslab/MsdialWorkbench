using System;
using System.Windows.Media;

namespace CompMs.Graphics.Core.Base
{
    public class AnnotatedDrawingVisual : DrawingVisual
    {
        public object Annotation { get; private set; }

        public AnnotatedDrawingVisual(object obj)
        {
            Annotation = obj;
        }
    }
}
