using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ChartDrawingUiTest.UI
{
    public class DPControl : ContentControl
    {
        public static readonly DependencyProperty HavingProperty =
            DependencyProperty.Register(
                "Having", typeof(string), typeof(DPControl),
                new FrameworkPropertyMetadata("Default (having)", FrameworkPropertyMetadataOptions.Inherits));

        public string Having {
            get => (string)GetValue(HavingProperty);
            set => SetValue(HavingProperty, value);
        }

        public static readonly DependencyProperty AttachedProperty =
            DependencyProperty.RegisterAttached(
                "Attached", typeof(string), typeof(DPControl),
                new FrameworkPropertyMetadata("Default (attached)", FrameworkPropertyMetadataOptions.Inherits));

        public static string GetAttached(DependencyObject d) {
            return (string)d.GetValue(AttachedProperty);
        }

        public static void SetAttached(DependencyObject d, object value) {
            d.SetValue(AttachedProperty, value);
        }
    }

    class DerivedDPControl : DPControl
    {

    }

    class AnotherControl : ContentControl
    {

    }
}
