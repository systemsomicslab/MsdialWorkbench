using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

using CompMs.Graphics.Core.Base;


namespace CompMs.Graphics.Core.Behavior
{
    public class ResetZoomBehavior : Behavior<FrameworkElement>
    {
        public IDrawingChart DrawingChart
        {
            get => (IDrawingChart)GetValue(DrawingChartProperty);
            set => SetValue(DrawingChartProperty, value);
        }
        public static readonly DependencyProperty DrawingChartProperty = DependencyProperty.Register(
            nameof(DrawingChart), typeof(IDrawingChart), typeof(ResetZoomBehavior),
            new PropertyMetadata(default(IDrawingChart))
            );

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseLeftButtonDown += ResetZoomOnMouseDoubleClick;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.MouseLeftButtonDown -= ResetZoomOnMouseDoubleClick;
        }

        void ResetZoomOnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DrawingChart == null) return;
            if (e.ClickCount == 2)
            {
                DrawingChart.ChartArea = DrawingChart.InitialArea;
            }
        }
    }
}
