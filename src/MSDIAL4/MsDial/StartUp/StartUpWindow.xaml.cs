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
    /// StartUpWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class StartUpWindow : Window
    {
        private MainWindow mainWindow;

        public StartUpWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = new StartUpProjectVM(this.mainWindow, this);
        }

        private void Button_ProjectFilePathSelect_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.RootFolder = Environment.SpecialFolder.Desktop;
            fbd.Description = "Choose a project folder.";
            fbd.SelectedPath = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            DateTime dt = DateTime.Now;

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.TextBox_ProjectFile.Text = fbd.SelectedPath + "\\" + dt.Year + "_" + dt.Month + "_" + dt.Day + "_" + dt.Hour + "_" + dt.Minute + "_" + dt.Second + "." + SaveFileFormat.mtd;
            }
        }

        private void Button_AnalystExperimentFileSelect_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Text file(*.txt)|*.txt";
            ofd.Title = "Import an analyst experiment file";
            ofd.RestoreDirectory = true;
            ofd.Multiselect = true;

            if (ofd.ShowDialog() == true)
            {
                this.TextBox_AnalystExperimentFile.Text = ofd.FileName;
            }
        }

        private void RadioButton_ESI_Checked(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null) return;
            ((StartUpProjectVM)this.DataContext).Ionization = Ionization.ESI;
            this.GroupBox_MethodType.IsEnabled = true;
            this.GroupBox_DataTypeMS2.IsEnabled = true;

            this.RadioButton_NegativeMode.IsEnabled = true;
            this.RadioButton_PositiveMode.IsChecked = true;

            ((StartUpProjectVM)this.DataContext).IonMode = IonMode.Positive;

            this.RadioButton_Lipidomics.IsEnabled = true;
            this.RadioButton_Metabolomics.IsChecked = true;

            ((StartUpProjectVM)this.DataContext).TargetOmics = TargetOmics.Metablomics;
        }

        private void RadioButton_EI_Checked(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null) return;
            ((StartUpProjectVM)this.DataContext).Ionization = Ionization.EI;
            this.GroupBox_MethodType.IsEnabled = false;
            this.GroupBox_DataTypeMS2.IsEnabled = false;

            this.RadioButton_NegativeMode.IsEnabled = true;
            this.RadioButton_PositiveMode.IsChecked = true;
            this.RadioButton_CentroidMode.IsChecked = true;

            ((StartUpProjectVM)this.DataContext).IonMode = IonMode.Positive;

            this.RadioButton_Lipidomics.IsEnabled = false;
            this.RadioButton_Metabolomics.IsChecked = true;

            ((StartUpProjectVM)this.DataContext).TargetOmics = TargetOmics.Metablomics;
        }

        private void RadioButton_Separation_GCorLC_Checked(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null) return;
            ((StartUpProjectVM)this.DataContext).SeparationType = SeparationType.Chromatography;
            this.Button_AnalystExperimentFileSelect.IsEnabled = true;
            Button_AnalystExperimentFileSelect.Visibility = Visibility.Visible;
            RadioButton_DataIndependentMSMS_AIF.Visibility = Visibility.Visible;
            TextBox_AnalystExperimentFile.Visibility = Visibility.Visible;
            Label_ExperimentFile.Visibility = Visibility.Visible;
            RadioButton_DataIndependentMSMS.Content = "SWATH-MS or conventional All-ions method";
            RadioButton_DataIndependentMSMS.ToolTip = "Type 2: \r\n" +
                "When you analyze a data set from data independent MS / MS, select this option.\r\n" +
                "Moreover, you have to import the experiment information including\r\n" +
                "\"Experiment ID\", \"Scan or Swath \", and \"Mass range\".\r\n" +
                "Please see the downloadable format. \r\n" +
                "For SWATH users, utilize \"Sample Information\"of PeakView software. \r\n" +
                "It's quite easy to get that kind of information.";
        }

        private void RadioButton_Separation_LCIM_Checked(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null) return;
            ((StartUpProjectVM)this.DataContext).SeparationType = SeparationType.IonMobility;
            Button_AnalystExperimentFileSelect.IsEnabled = false;
            Button_AnalystExperimentFileSelect.Visibility = Visibility.Collapsed;
            RadioButton_DataIndependentMSMS_AIF.Visibility = Visibility.Collapsed;
            TextBox_AnalystExperimentFile.Visibility = Visibility.Collapsed;
            Label_ExperimentFile.Visibility = Visibility.Collapsed;
            RadioButton_DataIndependentMSMS.Content = "Data independent MS/MS";
            RadioButton_DataIndependentMSMS.ToolTip = "Type 2: When you analyze a data set from data independent MS/MS, select this option.";
        }


        private void RadioButton_DataDependentMSMS_Checked(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null) return;
            ((StartUpProjectVM)this.DataContext).MethodType = MethodType.ddMSMS;
            ((StartUpProjectVM)this.DataContext).CheckAIF = false;
            this.Label_ExperimentFile.IsEnabled = false;
            this.TextBox_AnalystExperimentFile.IsEnabled = false;
            this.Button_AnalystExperimentFileSelect.IsEnabled = false;
        }

        private void RadioButton_DataIndependentMSMS_Checked(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null) return;
            ((StartUpProjectVM)this.DataContext).MethodType = MethodType.diMSMS;
            ((StartUpProjectVM)this.DataContext).CheckAIF = false;
            this.Label_ExperimentFile.IsEnabled = true;
            this.TextBox_AnalystExperimentFile.IsEnabled = true;
            this.Button_AnalystExperimentFileSelect.IsEnabled = true;
        }

        private void RadioButton_DataIndependentMSMS_AIF_Checked(object sender, RoutedEventArgs e) {
            if (this.DataContext == null) return;
            ((StartUpProjectVM)this.DataContext).MethodType = MethodType.diMSMS;
            ((StartUpProjectVM)this.DataContext).CheckAIF = true;
            this.Label_ExperimentFile.IsEnabled = true;
            this.TextBox_AnalystExperimentFile.IsEnabled = true;
            this.Button_AnalystExperimentFileSelect.IsEnabled = true;
        }


        private void RadioButton_ProfileMode_Checked(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null) return;
            ((StartUpProjectVM)this.DataContext).DataType = DataType.Profile;
        }

        private void RadioButton_CentroidMode_Checked(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null) return;
            ((StartUpProjectVM)this.DataContext).DataType = DataType.Centroid;
        }

        private void RadioButton_ProfileModeMS2_Checked(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null) return;
            ((StartUpProjectVM)this.DataContext).DataTypeMS2 = DataType.Profile;
        }

        private void RadioButton_CentroidModeMS2_Checked(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null) return;
            ((StartUpProjectVM)this.DataContext).DataTypeMS2 = DataType.Centroid;
        }

        private void RadioButton_PositiveMode_Checked(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null) return;
            ((StartUpProjectVM)this.DataContext).IonMode = IonMode.Positive;
        }

        private void RadioButton_NegativeMode_Checked(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null) return;
            ((StartUpProjectVM)this.DataContext).IonMode = IonMode.Negative;
        }

        private void RadioButton_Metabolomics_Checked(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null) return;
            ((StartUpProjectVM)this.DataContext).TargetOmics = TargetOmics.Metablomics;
        }

        private void RadioButton_Lipidomics_Checked(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null) return;
            ((StartUpProjectVM)this.DataContext).TargetOmics = TargetOmics.Lipidomics;
        }

    }
}
