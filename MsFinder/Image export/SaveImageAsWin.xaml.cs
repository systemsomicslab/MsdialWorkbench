using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// SaveImageWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SaveImageAsWin : Window
    {
        private object target;
        private string saveImageFormat;
        private string saveImageFilePath;
        private double horizontalDpi;
        private double verticalDpi;


        public SaveImageAsWin()
        {
            InitializeComponent();
            this.TextBox_HorizontalResolution.Text = "300";
            this.TextBox_VerticalResolution.Text = "300";
        }

        public SaveImageAsWin(object target)
        {
            InitializeComponent();
            this.TextBox_HorizontalResolution.Text = "300";
            this.TextBox_VerticalResolution.Text = "300";
            this.target = target;
        }

        private void Click_ExportFilePathSelect(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = "*.png";
            sfd.Filter = "PNG format(*.png)|*.png;|JPEG format(*.jpg)|*.jpg;|BMP format(*.bmp)|*.bmp;|TIFF format(*.tiff)|*.tiff;|GIF format(*.gif)|*.gif;|EMF format(*.emf)|*.emf;";
            sfd.Title = "Save file dialog";
            sfd.RestoreDirectory = true;

            if (sfd.ShowDialog(this) == true)
            {
                this.TextBox_ExportFilePath.Text = sfd.FileName;
            }
        }

        private void Click_Cancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            if (this.TextBox_ExportFilePath.Text == string.Empty)
            {
                MessageBox.Show("Select an export file path.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!double.TryParse(this.TextBox_HorizontalResolution.Text, out this.horizontalDpi))
            {
                MessageBox.Show("Enter the dpi(Dots Per Inch) value for horizontal resolution.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!double.TryParse(this.TextBox_VerticalResolution.Text, out this.verticalDpi))
            {
                MessageBox.Show("Enter the dpi(Dots Per Inch) value for vertical resolution.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.saveImageFormat = System.IO.Path.GetExtension(this.TextBox_ExportFilePath.Text);
            this.saveImageFilePath = this.TextBox_ExportFilePath.Text;

            if (this.saveImageFormat.Equals(".emf"))
                ImageExportUtility.SaveImageAsEmf(this.saveImageFilePath, this.target);
            else
                ImageExportUtility.SaveImageAsBitmap(this.saveImageFormat, this.saveImageFilePath, this.target, this.horizontalDpi, this.verticalDpi);

            this.Close();
        }
    }
}
