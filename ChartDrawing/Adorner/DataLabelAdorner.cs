using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Core.Adorner
{
    internal class DataLabelAdorner : System.Windows.Documents.Adorner
    {
        public DataLabelAdorner(
            UIElement adornedElement,
            string label
            ) : base(adornedElement)
        {
            layer = AdornerLayer.GetAdornerLayer(adornedElement);
            IsHitTestVisible = false;
            Text = new FormattedText(
                label, CultureInfo.GetCultureInfo("en-us"),
                FlowDirection.LeftToRight, new Typeface("Calibli"),
                14, Brushes.Black, 1
                );
        }

        public Point Position { get; set; }
        public FormattedText Text { get; set; }

        public void Attach()
        {
            if (layer != null)
                layer.Add(this);
        }

        public void Detach()
        {
            if (layer != null)
                layer.Remove(this);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var position = Position + new Vector(- Text.Width / 2, -20);
            drawingContext.DrawRectangle(
                Brushes.WhiteSmoke, null,
                new Rect(position.X, position.Y,
                         Text.Width, Text.Height));
            drawingContext.DrawText(Text, position);             
        }

        AdornerLayer layer;
    }
}
