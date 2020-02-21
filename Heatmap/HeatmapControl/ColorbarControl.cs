using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Msdial.Heatmap
{
    public class ColorbarControl : FrameworkElement
    {
        #region Property
        public LinearGradientBrush Brush
        {
            get => (LinearGradientBrush)GetValue(BrushProperty);
            set => SetValue(BrushProperty, value);
        }
        #endregion

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            ColorbarPainter.Draw(
                drawingContext,
                new Point(0,0), new Vector(ActualWidth, ActualHeight),
                Brush
            );
        }

        #region DependencyProperty
        public static readonly DependencyProperty BrushProperty = DependencyProperty.Register(
            "Brush", typeof(LinearGradientBrush), typeof(ColorbarControl),
            new FrameworkPropertyMetadata(default(LinearGradientBrush), FrameworkPropertyMetadataOptions.AffectsRender)
        );
        #endregion
    }
}
