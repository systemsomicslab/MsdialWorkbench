using Microsoft.Win32;
using Msdial.Gcms.Dataprocess.Algorithm;
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
    /// SaveSpectrumAsWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SaveSpectraTableAsWin : Window
    {
        private object target;
        private PeakAreaBean peakAreaBean;
        private AlignmentPropertyBean alignmentPropertyBean;
        private MS1DecResult ms1DecResult;
        private ProjectPropertyBean projectProp;
        private List<MspFormatCompoundInformationBean> mspDB;

        public SaveSpectraTableAsWin()
        {
            InitializeComponent();
        }

        public SaveSpectraTableAsWin(object target, PeakAreaBean peakAreaBean, MainWindow mainWindow)
        {
            InitializeComponent();
            this.target = target;
            this.peakAreaBean = peakAreaBean;
            this.projectProp = mainWindow.ProjectProperty;
            this.mspDB = mainWindow.MspDB;
        }

        public SaveSpectraTableAsWin(object target, PeakAreaBean peakAreaBean, ProjectPropertyBean projectProperty, List<MspFormatCompoundInformationBean> mspDB) {
            InitializeComponent();
            this.target = target;
            this.peakAreaBean = peakAreaBean;
            this.projectProp = projectProperty;
            this.mspDB = mspDB;
        }

        public SaveSpectraTableAsWin(object target, AlignmentPropertyBean alignmentPropertyBean, MainWindow mainWindow)
        {
            InitializeComponent();
            this.target = target;
            this.alignmentPropertyBean = alignmentPropertyBean;
            this.projectProp = mainWindow.ProjectProperty;
            this.mspDB = mainWindow.MspDB;
        }

        public SaveSpectraTableAsWin(object target, MS1DecResult ms1DecResult, MainWindow mainWindow)
        {
            InitializeComponent();
            this.target = target;
            this.ms1DecResult = ms1DecResult;
            this.projectProp = mainWindow.ProjectProperty;
            this.mspDB = mainWindow.MspDB;
        }

        private void Click_ExportFilePathSelect(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = "*.txt";
            sfd.Filter = "MassBank format(*.txt)|*.txt|NIST format(*.msp)|*.msp|MASCOT format(*.mgf)|*.mgf";
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

            string saveDataTableFormat = System.IO.Path.GetExtension(this.TextBox_ExportFilePath.Text);
            string saveDataTableFilePath = this.TextBox_ExportFilePath.Text;

            if (this.peakAreaBean != null)
                TableExportUtility.SaveSpectraTabel(saveDataTableFormat, saveDataTableFilePath, this.target, this.peakAreaBean, this.projectProp, this.mspDB);
            else if (this.alignmentPropertyBean != null)
                TableExportUtility.SaveSpectraTabel(saveDataTableFormat, saveDataTableFilePath, this.target, this.alignmentPropertyBean, this.projectProp, this.mspDB);
            else if (this.ms1DecResult != null)
                TableExportUtility.SaveSpectraTabel(saveDataTableFormat, saveDataTableFilePath, this.target, this.ms1DecResult, this.projectProp, this.mspDB);

            this.Close();
        }
    }
}
