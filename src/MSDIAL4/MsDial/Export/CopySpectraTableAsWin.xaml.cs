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
    /// CopySpectraTableAsWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class CopySpectraTableAsWin : Window
    {
        private object target;
        private PeakAreaBean peakAreaBean;
        private AlignmentPropertyBean alignmentPropertyBean;
        private MS1DecResult ms1DecResult;
        private ProjectPropertyBean projectProp;
        private List<MspFormatCompoundInformationBean> mspDB;

        public CopySpectraTableAsWin()
        {
            InitializeComponent();
            this.ComboBox_Format.ItemsSource = new string[] { ".msp", ".txt", ".mgf" };
            this.ComboBox_Format.SelectedIndex = 0;
        }

        public CopySpectraTableAsWin(object target, PeakAreaBean peakAreaBean, MainWindow mainWindow)
        {
            InitializeComponent();
            this.ComboBox_Format.ItemsSource = new string[] { ".msp", ".txt", ".mgf" };
            this.ComboBox_Format.SelectedIndex = 0;
            this.target = target;
            this.peakAreaBean = peakAreaBean;
            this.projectProp = mainWindow.ProjectProperty;
            this.mspDB = mainWindow.MspDB;
        }

        public CopySpectraTableAsWin(object target, PeakAreaBean peakAreaBean, ProjectPropertyBean projectProperty, List<MspFormatCompoundInformationBean> mspDB) {
            InitializeComponent();
            this.ComboBox_Format.ItemsSource = new string[] { ".msp", ".txt", ".mgf" };
            this.ComboBox_Format.SelectedIndex = 0;
            this.target = target;
            this.peakAreaBean = peakAreaBean;
            this.projectProp = projectProperty;
            this.mspDB = mspDB;
        }


        public CopySpectraTableAsWin(object target, AlignmentPropertyBean alignmentPropertyBean, MainWindow mainWindow)
        {
            InitializeComponent();
            this.ComboBox_Format.ItemsSource = new string[] { ".msp", ".txt", ".mgf" };
            this.ComboBox_Format.SelectedIndex = 0;
            this.target = target;
            this.alignmentPropertyBean = alignmentPropertyBean;
            this.projectProp = mainWindow.ProjectProperty;
            this.mspDB = mainWindow.MspDB;
        }

        public CopySpectraTableAsWin(object target, MS1DecResult ms1DecResult, MainWindow mainWindow)
        {
            InitializeComponent();
            this.ComboBox_Format.ItemsSource = new string[] { ".msp", ".txt", ".mgf" };
            this.ComboBox_Format.SelectedIndex = 0;
            this.target = target;
            this.ms1DecResult = ms1DecResult;
            this.projectProp = mainWindow.ProjectProperty;
            this.mspDB = mainWindow.MspDB;
        }

        private void Click_Cancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            string spectraFormat = this.ComboBox_Format.SelectedItem.ToString();

            if (this.peakAreaBean != null)
                TableExportUtility.CopySpectraTable(spectraFormat, this.target, this.peakAreaBean, this.projectProp, this.mspDB);
            else if (this.alignmentPropertyBean != null)
                TableExportUtility.CopySpectraTable(spectraFormat, this.target, this.alignmentPropertyBean, this.projectProp, this.mspDB);
            else if (this.ms1DecResult != null)
                TableExportUtility.CopySpectraTable(spectraFormat, this.target, this.ms1DecResult, this.projectProp, this.mspDB);

            this.Close();
        }
    }
}
