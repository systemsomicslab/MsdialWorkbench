using CompMs.Common.MessagePack;
using Microsoft.Win32;
using Msdial.Gcms.Dataprocess.Utility;
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
    /// Interaction logic for AnalysisParamSetForGcWin.xaml
    /// </summary>
    public partial class AnalysisParamSetForGcWin : Window
    {
        private MainWindow mainWindow;
        private ProcessOption processOption;

        public AnalysisParamSetForGcWin(MainWindow mainWindow)
        {
            InitializeComponent();
            
            this.mainWindow = mainWindow;
        }
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.ComboBox_SmoothingMethod.ItemsSource = new string[] { "Simple moving average", "Linear weighted moving average", "Savitzky–Golay filter", "Binomial filter" };
            this.ComboBox_ReferenceFileID.DisplayMemberPath = "AnalysisFilePropertyBean.AnalysisFileName";
            this.ComboBox_FilteringMethod.ItemsSource = new string[] { "Sample max / blank average: ", "Sample average / blank average: " };

            this.ComboBox_ReferenceFileID.ItemsSource = this.mainWindow.AnalysisFiles;

            this.DataContext = new AnalysisParamSetForGcVM(this.mainWindow, this);

            this.ComboBox_SmoothingMethod.SelectedIndex = (int)this.mainWindow.AnalysisParamForGC.SmoothingMethod;
            this.ComboBox_ReferenceFileID.SelectedIndex = this.mainWindow.AnalysisParamForGC.AlignmentReferenceFileID;
            this.ComboBox_FilteringMethod.SelectedIndex = (int)this.mainWindow.AnalysisParamForGC.BlankFiltering;

            if (this.mainWindow.AnalysisParamForGC.FileIdRiInfoDictionary == null || this.mainWindow.AnalysisParamForGC.FileIdRiInfoDictionary.Keys.Count != this.mainWindow.AnalysisFiles.Count)
                this.Label_RiDictionaryFilePath.Content = "Status: empty";
            else
                this.Label_RiDictionaryFilePath.Content = "Status: imported";

            if (this.mainWindow.AnalysisParamForGC.QcAtLeastFilter == true) {
                this.mainWindow.AnalysisParamForGC.QcAtLeastFilter = false;
            }

            WindowIsEnabledSetting(this.mainWindow.AnalysisParamForGC);
        }

        private void WindowIsEnabledSetting(AnalysisParamOfMsdialGcms param)
        {

            if (param.AccuracyType == AccuracyType.IsAccurate)
                this.CheckBox_IsAccurate.IsChecked = true;
            else
                this.CheckBox_IsAccurate.IsChecked = false;

            if (param.RetentionType == RetentionType.RI) {
                this.RadioButton_UseRetentionIndex.IsChecked = true;
                this.Button_RiDictionaryFilePath.IsEnabled = true;
            }
            else {
                this.RadioButton_UseRetentionTime.IsChecked = true;
                this.Button_RiDictionaryFilePath.IsEnabled = false;
            }

            if (param.RiCompoundType == RiCompoundType.Alkanes)
                this.RadioButton_Alkanes.IsChecked = true;
            else
                this.RadioButton_Fame.IsChecked = true;

            if (param.AlignmentIndexType == AlignmentIndexType.RI) {
                this.RadioButton_UseRetentionIndexForAlignment.IsChecked = true;
                this.TextBox_RetentionTimeAlignmentTolerance.IsEnabled = false;
                this.TextBox_RetentionIndexAlignmentTolerance.IsEnabled = true;
            }
            else {
                this.RadioButton_UseRetentionTimeForAlignment.IsChecked = true;
                this.TextBox_RetentionTimeAlignmentTolerance.IsEnabled = true;
                this.TextBox_RetentionIndexAlignmentTolerance.IsEnabled = false;
            }

            this.processOption = param.ProcessOption;
            switch (param.ProcessOption)
            {
                case ProcessOption.All:
                    
                    TabItem_DataCollection.IsSelected = true;
                    
                    break;
            
                case ProcessOption.IdentificationPlusAlignment:
                    
                    TabItem_DataCollection.IsEnabled = false;
                    TabItem_PeakDetection.IsEnabled = false;
                    TabItem_Deconvolution.IsEnabled = false;

                    TabItem_Identification.IsSelected = true;
                    break;
               
                case ProcessOption.Alignment:
                  
                    TabItem_DataCollection.IsEnabled = false;
                    TabItem_PeakDetection.IsEnabled = false;
                    TabItem_Deconvolution.IsEnabled = false;
                    TabItem_Identification.IsEnabled = false;

                    this.mainWindow.AnalysisParamForGC.TogetherWithAlignment = true;
                    CheckBox_WithAlignment.IsEnabled = false;

                    TabItem_Alignment.IsSelected = true;
                    break;
            }
        }

        private void Click_MspFileBrowse(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "MSP file(*.msp)|*.msp;";
            ofd.Title = "Import a library file";
            ofd.RestoreDirectory = true;
            ofd.Multiselect = false;

            if (ofd.ShowDialog() == true)
            {
                this.TextBox_MspFilePath.Text = ofd.FileName;
            }
        }

        private void Click_RiFile_Browse(object sender, RoutedEventArgs e)
        {
            var window = new RetentionIndexDictionarySetWin();
            window.Owner = this;
            window.ShowDialog();
        }

        private void ComboBox_SmoothingMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.DataContext == null) return;
            ((AnalysisParamSetForGcVM)this.DataContext).SmoothingMethod = (SmoothingMethod)((ComboBox)sender).SelectedIndex;
        }

        private void Click_Cancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ComboBox_ReferenceFileID_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.DataContext == null) return;
            ((AnalysisParamSetForGcVM)this.DataContext).AlignmentReferenceFileID = ((ComboBox)sender).SelectedIndex;
        }

        private void Click_Load(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null) return;

            var ofd = new OpenFileDialog();
            ofd.Filter = "MED file(*.med*)|*.med*";
            ofd.Title = "Import a method file";
            ofd.RestoreDirectory = true;

            if (ofd.ShowDialog() == true)
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var param = MessagePackHandler.LoadFromFile<AnalysisParamOfMsdialGcms>(ofd.FileName);
                //var param = (AnalysisParamOfMsdialGcms)DataStorageGcUtility.LoadFromXmlFile(ofd.FileName, typeof(AnalysisParamOfMsdialGcms));

                param.ProcessOption = this.processOption;
                param.AlignmentReferenceFileID = 0;
                
                if (param.RetentionType == RetentionType.RT)
                {
                    this.Button_RiDictionaryFilePath.IsEnabled = false;
                }
                else
                {
                    this.Button_RiDictionaryFilePath.IsEnabled = true;
                }

                if (param.FileIdRiInfoDictionary == null || param.FileIdRiInfoDictionary.Keys.Count != this.mainWindow.AnalysisFiles.Count) {
                    this.Label_RiDictionaryFilePath.Content = "Status: empty";
                    param.FileIdRiInfoDictionary = new Dictionary<int, RiDictionaryInfo>();
                }
                else
                    this.Label_RiDictionaryFilePath.Content = "Status: imported";

                if (param.NumThreads == 0)
                    param.NumThreads = 1;

                if (param.QcAtLeastFilter == true)
                    param.QcAtLeastFilter = false;

                ((AnalysisParamSetForGcVM)this.DataContext).Param = param;
                ((AnalysisParamSetForGcVM)this.DataContext).VmUpdate();

                Mouse.OverrideCursor = null;
            }
        }

        private void RadioButton_UseRetentionIndex_Checked(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null) return;
           
            ((AnalysisParamSetForGcVM)this.DataContext).Param.RetentionType = RetentionType.RI;
            this.Button_RiDictionaryFilePath.IsEnabled = true;
        }

        private void RadioButton_UseRetentionTime_Checked(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null) return;

            ((AnalysisParamSetForGcVM)this.DataContext).Param.RetentionType = RetentionType.RT;
            this.Button_RiDictionaryFilePath.IsEnabled = false;
        }

        private void RadioButton_Alkanes_Checked(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null) return;

            ((AnalysisParamSetForGcVM)this.DataContext).Param.RiCompoundType = RiCompoundType.Alkanes;
        }

        private void RadioButton_Fame_Checked(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null) return;

            ((AnalysisParamSetForGcVM)this.DataContext).Param.RiCompoundType = RiCompoundType.Fames;
        }

        private void RadioButton_UseRetentionIndexForAlignment_Checked(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null) return;

            ((AnalysisParamSetForGcVM)this.DataContext).Param.AlignmentIndexType = AlignmentIndexType.RI;
            this.TextBox_RetentionTimeAlignmentTolerance.IsEnabled = false;
            this.TextBox_RetentionIndexAlignmentTolerance.IsEnabled = true;
        }

        private void RadioButton_UseRetentionTimeForAlignment_Checked(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null) return;

            ((AnalysisParamSetForGcVM)this.DataContext).Param.AlignmentIndexType = AlignmentIndexType.RT;
            this.TextBox_RetentionTimeAlignmentTolerance.IsEnabled = true;
            this.TextBox_RetentionIndexAlignmentTolerance.IsEnabled = false;
        }

        private void ComboBox_FilteringMethod_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (this.DataContext == null)
                return;
            var index = ((ComboBox)sender).SelectedIndex;
            if (index < 0 || index > 2)
                index = 0;

            ((AnalysisParamSetForGcVM)this.DataContext).BlankFiltering = (BlankFiltering)index;
        }

        private void DataGrid_ExcludeMassSetting_CurrentCellChanged(object sender, EventArgs e) {
            var cell = DataGridHelper.GetCell((DataGridCellInfo)this.DataGrid_ExcludeMassSetting.CurrentCell);
            if (cell == null) return;
            cell.Focus();
            this.DataGrid_ExcludeMassSetting.BeginEdit();
        }

        private void DataGrid_ExcludeMassSetting_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.V & Keyboard.Modifiers == ModifierKeys.Control) {
                e.Handled = true;
                string[] clipText = Clipboard.GetText().Replace("\r\n", "\n").Split('\n');
                List<string[]> clipTextList = new List<string[]>();
                for (int i = 0; i < clipText.Length; i++) { clipTextList.Add(clipText[i].Split('\t')); }
                if (clipTextList.Count > 1) clipTextList.RemoveAt(clipTextList.Count - 1);

                if (clipTextList.Count > 1 && this.DataGrid_ExcludeMassSetting.SelectedCells[0].Column.DisplayIndex == 0) {
                    int startRow = this.DataGrid_ExcludeMassSetting.Items.IndexOf(this.DataGrid_ExcludeMassSetting.SelectedCells[0].Item);
                    double exactMass, massTolerance;
                    for (int i = 0; i < clipTextList.Count; i++) {
                        if (startRow + i > this.DataGrid_ExcludeMassSetting.Items.Count - 1) break;
                        if (clipTextList[i].Length > 0) {
                            if (double.TryParse(clipTextList[i][0], out exactMass)) {
                                ((ExcludeMassVM)this.DataGrid_ExcludeMassSetting.Items[startRow + i]).ExcludedMass = (float)exactMass;
                            }
                        }
                        if (clipTextList[i].Length > 1) {
                            if (double.TryParse(clipTextList[i][1], out massTolerance)) {
                                ((ExcludeMassVM)this.DataGrid_ExcludeMassSetting.Items[startRow + i]).MassTolerance = (float)massTolerance;
                            }
                        }
                    }
                    this.DataGrid_ExcludeMassSetting.UpdateLayout();
                }
                else if (clipTextList.Count > 1 && this.DataGrid_ExcludeMassSetting.SelectedCells[0].Column.DisplayIndex == 1) {
                    int startRow = this.DataGrid_ExcludeMassSetting.Items.IndexOf(this.DataGrid_ExcludeMassSetting.SelectedCells[0].Item);
                    double exactMass, massTolerance;
                    for (int i = 0; i < clipTextList.Count; i++) {
                        if (startRow + i > this.DataGrid_ExcludeMassSetting.Items.Count - 1) break;
                        if (clipTextList[i].Length > 0) {
                            if (double.TryParse(clipTextList[i][0], out massTolerance)) {
                                ((ExcludeMassVM)this.DataGrid_ExcludeMassSetting.Items[startRow + i]).MassTolerance = (float)massTolerance;
                            }
                        }
                    }
                    this.DataGrid_ExcludeMassSetting.UpdateLayout();
                }
                else if (clipTextList.Count == 1 && this.DataGrid_ExcludeMassSetting.SelectedCells[0].Column.DisplayIndex == 0) {
                    int startRow = this.DataGrid_ExcludeMassSetting.Items.IndexOf(this.DataGrid_ExcludeMassSetting.SelectedCells[0].Item);
                    for (int i = 0; i < this.DataGrid_ExcludeMassSetting.SelectedCells.Count; i++) {
                        if (this.DataGrid_ExcludeMassSetting.SelectedCells[i].Column.DisplayIndex != 0) continue;
                        if (startRow + i > this.DataGrid_ExcludeMassSetting.Items.Count - 1) break;
                        double d;
                        if (double.TryParse(clipTextList[0][0], out d))
                            ((ExcludeMassVM)this.DataGrid_ExcludeMassSetting.Items[startRow + i]).ExcludedMass = (float)d;
                    }
                }
                else if (clipTextList.Count == 1 && this.DataGrid_ExcludeMassSetting.SelectedCells[0].Column.DisplayIndex == 1) {
                    int startRow = this.DataGrid_ExcludeMassSetting.Items.IndexOf(this.DataGrid_ExcludeMassSetting.SelectedCells[0].Item);
                    for (int i = 0; i < this.DataGrid_ExcludeMassSetting.SelectedCells.Count; i++) {
                        if (this.DataGrid_ExcludeMassSetting.SelectedCells[i].Column.DisplayIndex != 1) continue;
                        if (startRow + i > this.DataGrid_ExcludeMassSetting.Items.Count - 1) break;
                        double d;
                        if (double.TryParse(clipTextList[0][1], out d))
                            ((ExcludeMassVM)this.DataGrid_ExcludeMassSetting.Items[startRow + i]).MassTolerance = (float)d;
                    }
                }
            }
        }


    }
}
