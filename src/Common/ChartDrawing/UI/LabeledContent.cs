using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
    ///     <MyNamespace:LabeldContent/>
    ///
    /// </summary>
    public class LabeledContent : ContentControl
    {
        static LabeledContent() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LabeledContent), new FrameworkPropertyMetadata(typeof(LabeledContent)));
        }

        public static readonly DependencyProperty PrependLabelProperty =
            DependencyProperty.Register(
                nameof(PrependLabel), typeof(string), typeof(LabeledContent),
                new FrameworkPropertyMetadata(string.Empty));

        public string PrependLabel {
            get => (string)GetValue(PrependLabelProperty);
            set => SetValue(PrependLabelProperty, value);
        }

        public static readonly DependencyProperty AppendLabelProperty =
            DependencyProperty.Register(
                nameof(AppendLabel), typeof(string), typeof(LabeledContent),
                new FrameworkPropertyMetadata(string.Empty));

        public string AppendLabel {
            get => (string)GetValue(AppendLabelProperty);
            set => SetValue(AppendLabelProperty, value);
        }
    }
}
