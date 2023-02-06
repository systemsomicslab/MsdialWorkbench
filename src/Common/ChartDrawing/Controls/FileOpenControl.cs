using CompMs.Graphics.Window;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
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

namespace CompMs.Graphics.Controls
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:CompMs.Graphics.Controls"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:CompMs.Graphics.Controls;assembly=CompMs.Graphics.Controls"
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
    ///     <MyNamespace:FileSelectControl/>
    ///
    /// </summary>
    [TemplatePart(Name = "BrowseButtonElement", Type = typeof(Button))]
    public class FileOpenControl : Control
    {
        static FileOpenControl() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FileOpenControl), new FrameworkPropertyMetadata(typeof(FileOpenControl)));
        }

        public FileOpenControl() {
            DefaultStyleKey = typeof(FileOpenControl);
        }

        public static readonly DependencyProperty PathProperty = DependencyProperty.Register(
            nameof(Path), typeof(string), typeof(FileOpenControl), new PropertyMetadata(string.Empty)
            );
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title), typeof(string), typeof(FileOpenControl), new PropertyMetadata("Select file")
            );
        public static readonly DependencyProperty FilterProperty = DependencyProperty.Register(
            nameof(Filter), typeof(string), typeof(FileOpenControl), new PropertyMetadata("All file|*.*")
            );
        public static readonly DependencyProperty MultiSelectProperty = DependencyProperty.Register(
            nameof(MultiSelect), typeof(bool), typeof(FileOpenControl), new PropertyMetadata(false)
            );

        public string Path {
            get => (string)GetValue(PathProperty);
            set => SetValue(PathProperty, value);
        }
        public string Title {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
        public string Filter {
            get => (string)GetValue(FilterProperty);
            set => SetValue(FilterProperty, value);
        }
        public bool MultiSelect {
            get => (bool)GetValue(MultiSelectProperty);
            set => SetValue(MultiSelectProperty, value);
        }

        private Button browseButtonElement;

        private Button BrowseButtonElement {
            get {
                return browseButtonElement;
            }
            set {
                if (browseButtonElement != null)
                    browseButtonElement.Click -= browseButtonElement_Click;
                browseButtonElement = value;
                if (browseButtonElement != null)
                    browseButtonElement.Click += browseButtonElement_Click;
            }
        }

        public override void OnApplyTemplate() {
            BrowseButtonElement = GetTemplateChild("BrowseButton") as Button;
        }

        void browseButtonElement_Click(object sender, EventArgs e) {
            /*
            var ofd = new OpenFileDialog
            {
                Title = Title,
                Filter = Filter,
                RestoreDirectory = true,
                Multiselect = MultiSelect
            };

            if (ofd.ShowDialog() == true) {
                Path = ofd.FileName;
            }
            */
            var fbd = new SelectFolderDialog()
            {
                Title = Title,
                SelectedPath = Path,
            };
            var result = DialogResult.None;

            if (sender is Button btn) {
                var window = System.Windows.Window.GetWindow(btn);
                if (window != null) result = fbd.ShowDialog(window);
            }
            else {
                result = fbd.ShowDialog();
            }

            if (result == DialogResult.OK) {
                Path = fbd.SelectedPath;
            }
        }
    }
}
