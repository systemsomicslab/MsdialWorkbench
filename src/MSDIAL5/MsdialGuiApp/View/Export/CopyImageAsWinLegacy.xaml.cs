using CompMs.Graphics.IO;
using System.Windows;

namespace CompMs.App.Msdial.View.Export
{
    /// <summary>
    /// Interaction logic for CopyImageAsWinLegacy.xaml
    /// </summary>
    public partial class CopyImageAsWinLegacy : Window {
        private object? target;
        private string? saveImageFilePath;
        private double horizontalDpi;
        private double verticalDpi;

        public CopyImageAsWinLegacy() {
            InitializeComponent();
            this.TextBox_HorizontalResolution.Text = "300";
            this.TextBox_VerticalResolution.Text = "300";
            this.ComboBox_Format.ItemsSource = new string[] { ".png", ".jpg", ".bmp", ".tiff", ".gif", ".emf" };
            this.ComboBox_Format.SelectedIndex = 0;
        }

        public CopyImageAsWinLegacy(object target) {
            InitializeComponent();
            this.TextBox_HorizontalResolution.Text = "300";
            this.TextBox_VerticalResolution.Text = "300";
            this.ComboBox_Format.ItemsSource = new string[] { ".png", ".jpg", ".bmp", ".tiff", ".gif", ".emf" };
            this.ComboBox_Format.SelectedIndex = 0;
            this.target = target;
        }

        private void Click_Cancel(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void Finish_Click(object sender, RoutedEventArgs e) {
            if (!double.TryParse(this.TextBox_HorizontalResolution.Text, out this.horizontalDpi)) {
                MessageBox.Show("Enter the dpi(Dots Per Inch) value for horizontal resolution.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!double.TryParse(this.TextBox_VerticalResolution.Text, out this.verticalDpi)) {
                MessageBox.Show("Enter the dpi(Dots Per Inch) value for vertical resolution.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.saveImageFilePath = this.ComboBox_Format.SelectedItem.ToString();
            if (this.saveImageFilePath.Equals(".emf")) {
                if (this.target is FrameworkElement element) {
                    var encoder = new EmfEncoder();
                    var obj = encoder.Get(element);
                    Clipboard.SetData(DataFormats.EnhancedMetafile, obj);
                }
            }
            else {
                if (this.target is FrameworkElement element) {
                    var encoder = new PngEncoder(this.horizontalDpi, this.verticalDpi);
                    var obj = encoder.Get(element);
                    Clipboard.SetData(DataFormats.Bitmap, obj);
                }
            }

            this.Close();
        }
    }
}
