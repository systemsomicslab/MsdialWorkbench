using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

namespace CompMs.Graphics.Core.Behavior
{
    public class WheelZoomBehavior : Behavior<FrameworkElement>
    {
        public Rect ChartArea
        {
            get => (Rect)GetValue(ChartAreaProperty);
            set => SetValue(ChartAreaProperty, value);
        }
        public static readonly DependencyProperty ChartAreaProperty = DependencyProperty.Register(
            nameof(ChartArea), typeof(Rect), typeof(WheelZoomBehavior),
            new FrameworkPropertyMetadata(default(Rect),
                FrameworkPropertyMetadataOptions.AffectsRender)
            );

        public Rect InitialArea
        {
            get => (Rect)GetValue(InitialAreaProperty);
            set => SetValue(InitialAreaProperty, value);
        }
        public static readonly DependencyProperty InitialAreaProperty = DependencyProperty.Register(
            nameof(InitialArea), typeof(Rect), typeof(WheelZoomBehavior),
            new FrameworkPropertyMetadata(default(Rect),
                FrameworkPropertyMetadataOptions.AffectsRender)
            );

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseWheel += ZoomOnMouseWheel;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.MouseWheel -= ZoomOnMouseWheel;
        }

        void ZoomOnMouseWheel(object sender, MouseEventArgs e)
        {
            
        }
    }
}
