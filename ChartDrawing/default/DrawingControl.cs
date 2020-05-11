using System;
using System.Windows;
using System.Windows.Media;

namespace CompMs.Graphics.Base
{
    public class DrawingControl : FrameworkElement
    {
        public Drawing Source
        {
            get => (Drawing)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source), typeof(Drawing), typeof(DrawingControl),
            new FrameworkPropertyMetadata(default(Drawing),
                FrameworkPropertyMetadataOptions.AffectsRender)
            );

        public DrawingControl() { }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            drawingContext.PushClip(new RectangleGeometry(new Rect(RenderSize)));
            drawingContext.DrawDrawing(Source);
            drawingContext.Pop();
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }
    }
}
