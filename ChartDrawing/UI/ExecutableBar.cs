using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
    ///     <MyNamespace:ExecutableBar/>
    ///
    /// </summary>
    public class ExecutableBar : ContentControl
    {
        static ExecutableBar() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ExecutableBar), new FrameworkPropertyMetadata(typeof(ExecutableBar)));
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(
                nameof(Label), typeof(object), typeof(ExecutableBar),
                new FrameworkPropertyMetadata(null));

        public object Label {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public static readonly DependencyProperty LabelWidthProperty =
            DependencyProperty.Register(
                nameof(LabelWidth), typeof(double), typeof(ExecutableBar),
                new FrameworkPropertyMetadata(80d));

        public double LabelWidth {
            get => (double)GetValue(LabelWidthProperty);
            set => SetValue(LabelWidthProperty, value);
        }

        public static readonly DependencyProperty ButtonContentProperty =
            DependencyProperty.Register(
                nameof(ButtonContent), typeof(object), typeof(ExecutableBar),
                new FrameworkPropertyMetadata(null));

        public object ButtonContent {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public static readonly DependencyProperty ButtonWidthProperty =
            DependencyProperty.Register(
                nameof(ButtonWidth), typeof(double), typeof(ExecutableBar),
                new FrameworkPropertyMetadata(64d));

        public double ButtonWidth {
            get => (double)GetValue(ButtonWidthProperty);
            set => SetValue(ButtonWidthProperty, value);
        }

        public static readonly DependencyProperty ButtonHeightProperty =
            DependencyProperty.Register(
                nameof(ButtonHeight), typeof(double), typeof(ExecutableBar),
                new FrameworkPropertyMetadata(30d));

        public double ButtonHeight {
            get => (double)GetValue(ButtonHeightProperty);
            set => SetValue(ButtonHeightProperty, value);
        }

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(
                nameof(Command), typeof(ICommand), typeof(ExecutableBar),
                new FrameworkPropertyMetadata(null));

        public ICommand Command {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.Register(
                nameof(CommandParameter), typeof(object), typeof(ExecutableBar),
                new FrameworkPropertyMetadata(null));
        
        public object CommandParameter {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        public static readonly DependencyProperty CommandIsEnabledProperty =
            DependencyProperty.Register(
                nameof(CommandIsEnabled), typeof(bool), typeof(ExecutableBar),
                new FrameworkPropertyMetadata(true));

        public bool CommandIsEnabled {
            get => (bool)GetValue(CommandIsEnabledProperty);
            set => SetValue(CommandIsEnabledProperty, value);
        }
    }
}
