using System.Windows;
using System.Windows.Controls;

namespace CompMs.Graphics.UI
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:CompMs.Graphics.UI"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:CompMs.Graphics.UI;assembly=CompMs.Graphics.UI"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:LabeledBar/>
    ///
    /// </summary>
    public class LabeledBar : ContentControl
    {
        static LabeledBar() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LabeledBar), new FrameworkPropertyMetadata(typeof(LabeledBar)));
        }

        public static readonly DependencyProperty LabelWidthProperty =
            DependencyProperty.Register(
                nameof(LabelWidth), typeof(double), typeof(LabeledBar),
                new FrameworkPropertyMetadata(80d));

        public double LabelWidth {
            get => (double)GetValue(LabelWidthProperty);
            set => SetValue(LabelWidthProperty, value);
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(
                nameof(Label), typeof(object), typeof(LabeledBar),
                new FrameworkPropertyMetadata(null));

        public object Label {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }
    }
}
