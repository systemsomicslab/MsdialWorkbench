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

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// CopyImageAsWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class CopyImageAsWin : Window
    {
        private object target;
        private string saveImageFilePath;
        private double horizontalDpi;
        private double verticalDpi;

        public CopyImageAsWin()
        {
            InitializeComponent();
            this.TextBox_HorizontalResolution.Text = "300";
            this.TextBox_VerticalResolution.Text = "300";
            this.ComboBox_Format.ItemsSource = new string[] { ".png", ".jpg", ".bmp", ".tiff", ".gif", ".emf" };
            this.ComboBox_Format.SelectedIndex = 0;
        }

        public CopyImageAsWin(object target)
        {
            InitializeComponent();
            this.TextBox_HorizontalResolution.Text = "300";
            this.TextBox_VerticalResolution.Text = "300";
            this.ComboBox_Format.ItemsSource = new string[] { ".png", ".jpg", ".bmp", ".tiff", ".gif", ".emf" };
            this.ComboBox_Format.SelectedIndex = 0;
            this.target = target;
        }

        private void Click_Cancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {
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

            this.saveImageFilePath = this.ComboBox_Format.SelectedItem.ToString();

            if (this.saveImageFilePath.Equals(".emf"))
                ImageExportUtility.CopyImageAsEmf(this.saveImageFilePath, this.target, this.Owner);
            else
                ImageExportUtility.CopyImageAsBitmap(this.saveImageFilePath, this.target, this.horizontalDpi, this.verticalDpi);


            this.Close();
        }
    }
}
