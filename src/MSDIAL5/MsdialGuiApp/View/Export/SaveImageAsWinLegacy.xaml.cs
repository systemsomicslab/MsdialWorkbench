using CompMs.Graphics.IO;
using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace CompMs.App.Msdial.View.Export
{
    /// <summary>
    /// Interaction logic for SaveImageAsWinLegacy.xaml
    /// </summary>
    public partial class SaveImageAsWinLegacy : Window {
        private object? target;
        private string? saveImageFormat;
        private string? saveImageFilePath;
        private double horizontalDpi;
        private double verticalDpi;
        private string filefilter = "PNG format(*.png)|*.png;|JPEG format(*.jpg)|*.jpg;|BMP format(*.bmp)|*.bmp;|TIFF format(*.tiff)|*.tiff;|GIF format(*.gif)|*.gif;|EMF format(*.emf)|*.emf;";

        public SaveImageAsWinLegacy() {
            InitializeComponent();
            this.TextBox_HorizontalResolution.Text = "300";
            this.TextBox_VerticalResolution.Text = "300";
        }

        public SaveImageAsWinLegacy(object target) {
            InitializeComponent();
            this.TextBox_HorizontalResolution.Text = "300";
            this.TextBox_VerticalResolution.Text = "300";
            this.target = target;
        }

        public SaveImageAsWinLegacy(object target, string hpx, string wpx) {
            InitializeComponent();
            this.TextBox_HorizontalResolution.Text = hpx;
            this.TextBox_VerticalResolution.Text = wpx;
            this.target = target;
            filefilter = "PNG format(*.png)|*.png;|JPEG format(*.jpg)|*.jpg;|BMP format(*.bmp)|*.bmp;|TIFF format(*.tiff)|*.tiff;|GIF format(*.gif)|*.gif;";
        }

        private void Click_ExportFilePathSelect(object sender, RoutedEventArgs e) {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = "*.png";
            sfd.Filter = filefilter;
            sfd.Title = "Save file dialog";
            sfd.RestoreDirectory = true;

            if (sfd.ShowDialog(this) == true) {
                this.TextBox_ExportFilePath.Text = sfd.FileName;
            }
        }

        private void Click_Cancel(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void Finish_Click(object sender, RoutedEventArgs e) {
            if (this.TextBox_ExportFilePath.Text == string.Empty) {
                MessageBox.Show("Select an export file path.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!double.TryParse(this.TextBox_HorizontalResolution.Text, out this.horizontalDpi)) {
                MessageBox.Show("Enter the dpi(Dots Per Inch) value for horizontal resolution.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!double.TryParse(this.TextBox_VerticalResolution.Text, out this.verticalDpi)) {
                MessageBox.Show("Enter the dpi(Dots Per Inch) value for vertical resolution.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.saveImageFormat = System.IO.Path.GetExtension(this.TextBox_ExportFilePath.Text);
            this.saveImageFilePath = this.TextBox_ExportFilePath.Text;

            if (this.saveImageFormat.Equals(".emf")) {
                if (this.target is FrameworkElement element) {
                    var encoder = new EmfEncoder();
                    using (var fs = File.Open(this.saveImageFilePath, FileMode.Create)) {
                        encoder.Save(element, fs);
                    }
                }
            }
            else {
                if (this.target is FrameworkElement element) {
                    var encoder = new PngEncoder(this.horizontalDpi, this.verticalDpi);
                    using (var fs = File.Open(this.saveImageFilePath, FileMode.Create)) {
                        encoder.Save(element, fs);
                    }
                }
            }
            this.Close();
        }
    }
}
