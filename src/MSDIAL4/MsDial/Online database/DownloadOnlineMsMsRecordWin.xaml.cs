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
    /// Interaction logic for DownloadOnlineMsMsRecordWin.xaml
    /// </summary>
    public partial class DownloadOnlineMsMsRecordWin : Window
    {
        private MainWindow mainWindow;

        public DownloadOnlineMsMsRecordWin(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
        }

        private void Download_Click(object sender, RoutedEventArgs e)
        {
            var folderPath = this.TextBox_DownloadFolderPath.Text;
            if (folderPath == null || folderPath == string.Empty)
            {
                MessageBox.Show("Select a download folder path.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (this.CheckBox_MassBank.IsChecked == false && this.CheckBox_MoNA.IsChecked == false)
            {
                MessageBox.Show("Check at least one on-line DB.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var ionMode = IonMode.Both;
            if (this.RadioButton_Positive.IsChecked == true) ionMode = IonMode.Positive;
            else if (this.RadioButton_Negative.IsChecked == true) ionMode = IonMode.Negative;

            var dt = DateTime.Now;
            var extention = dt.Year.ToString() + dt.Month.ToString() + dt.Day.ToString() + ".msp";
            var filePathPosiMoNA = folderPath + "\\MoNA-PosiMSMS-" + extention;
            var filePathNegaMoNA = folderPath + "\\MoNA-NegaMSMS-" + extention;

            Mouse.OverrideCursor = Cursors.Wait;

            if (ionMode == IonMode.Both || ionMode == IonMode.Positive)
                new MonaRecordDownloadProcess().Process(mainWindow, filePathPosiMoNA, IonMode.Positive);
            
            if (ionMode == IonMode.Both || ionMode == IonMode.Negative)
                new MonaRecordDownloadProcess().Process(mainWindow, filePathNegaMoNA, IonMode.Negative);

            Mouse.OverrideCursor = null;

            this.Close();
        }

        private void Click_Cancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_DownloadFolder_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.RootFolder = Environment.SpecialFolder.Desktop;
            fbd.Description = "Choose an import folder.";
            fbd.SelectedPath = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.TextBox_DownloadFolderPath.Text = fbd.SelectedPath;
            }
        }
    }
}
