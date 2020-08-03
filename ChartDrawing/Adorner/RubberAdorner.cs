using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace CompMs.Graphics.Core.Adorner
{
    internal class RubberAdorner : System.Windows.Documents.Adorner
    {
        public RubberAdorner(UIElement adornedElement, Point point) : base(adornedElement)
        {
            layer = AdornerLayer.GetAdornerLayer(adornedElement);
            InitialPoint = point;
            IsHitTestVisible = false;

            Attach();
        }

        public Point InitialPoint
        {
            get => (Point)GetValue(InitialPointProperty);
            set => SetValue(InitialPointProperty, value);
        }
        public static readonly DependencyProperty InitialPointProperty = DependencyProperty.Register(
            nameof(InitialPoint), typeof(Point), typeof(RubberAdorner),
            new FrameworkPropertyMetadata(default(Point), FrameworkPropertyMetadataOptions.AffectsRender)
            );
        
        public Vector Offset
        {
            get => (Vector)GetValue(OffsetProperty);
            set => SetValue(OffsetProperty, value);
        }
        public static readonly DependencyProperty OffsetProperty = DependencyProperty.Register(
            nameof(Offset), typeof(Vector), typeof(RubberAdorner),
            new FrameworkPropertyMetadata(default(Vector), FrameworkPropertyMetadataOptions.AffectsRender)
            );

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
            drawingContext.DrawRectangle(
                new SolidColorBrush(Colors.DarkGray) { Opacity=0.5 },
                new Pen(Brushes.DarkGray, 1),
                new Rect(InitialPoint, Offset)
                );
        }

        AdornerLayer layer;
    }
}
