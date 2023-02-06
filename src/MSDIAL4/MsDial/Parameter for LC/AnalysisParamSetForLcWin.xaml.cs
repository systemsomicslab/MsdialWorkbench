using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
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
using System.Collections.ObjectModel;
using System.Windows.Resources;
using Msdial.Lcms.Dataprocess.Utility;
using CompMs.RawDataHandler.Core;
using CompMs.Common.MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// ParameterSettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class AnalysisParamSetForLcWin : Window
    {
        private MainWindow mainWindow;
        private ProcessOption processOption;

        public AnalysisParamSetForLcWin(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
        }
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.mainWindow.MainWindowTitle.Contains("-dev")) {
                this.mainWindow.ProjectProperty.IsLabPrivateVersion = true;
            }
            else if (this.mainWindow.MainWindowTitle.Contains("-tada"))
            {
                this.mainWindow.ProjectProperty.IsLabPrivateVersionTada = true;
            }
            else {
                this.mainWindow.ProjectProperty.IsLabPrivateVersion = false;
            }

            if (this.mainWindow.AnalysisParamForLC.AdductIonInformationBeanList == null || this.mainWindow.AnalysisParamForLC.AdductIonInformationBeanList.Count == 0) {
                this.mainWindow.AnalysisParamForLC.AdductIonInformationBeanList = getAdductIonInformationList(this.mainWindow.ProjectProperty.IonMode);
                this.mainWindow.AnalysisParamForLC.LipidQueryBean = getLbmQueries();
                if (this.mainWindow.ProjectProperty.SeparationType == SeparationType.IonMobility) {
                    this.mainWindow.AnalysisParamForLC.IsIonMobility = true;
                }
            }


            if (this.mainWindow.AnalysisParamForLC.QcAtLeastFilter == true) {
                this.mainWindow.AnalysisParamForLC.QcAtLeastFilter = false;
            }

            this.ComboBox_SmoothingMethod.ItemsSource = new string[] { "Simple moving average", "Linear weighted moving average", "Savitzky–Golay filter", "Binomial filter" };
            this.ComboBox_FilteringMethod.ItemsSource = new string[] { "Sample max / blank average: ", "Sample average / blank average: " };
            //this.ComboBox_PeakConsideration.ItemsSource = new string[] { "One", "Both" };

            this.ComboBox_ReferenceFileID.DisplayMemberPath = "AnalysisFilePropertyBean.AnalysisFileName";
            this.ComboBox_NonLabeledReference.DisplayMemberPath = "AnalysisFilePropertyBean.AnalysisFileName";
            this.ComboBox_FullyLabeledReference.DisplayMemberPath = "AnalysisFilePropertyBean.AnalysisFileName";
            this.ComboBox_LabeledElement.DisplayMemberPath = "ElementName";
            
            this.ComboBox_ReferenceFileID.ItemsSource = this.mainWindow.AnalysisFiles;
            this.ComboBox_NonLabeledReference.ItemsSource = this.mainWindow.AnalysisFiles;
            this.ComboBox_FullyLabeledReference.ItemsSource = this.mainWindow.AnalysisFiles;
            this.ComboBox_LabeledElement.ItemsSource = this.mainWindow.AnalysisParamForLC.IsotopeTrackingDictionary.IsotopeElements;

            this.DataContext = new AnalysisParamSetForLcVM(this.mainWindow, this);

            this.ComboBox_SmoothingMethod.SelectedIndex = (int)this.mainWindow.AnalysisParamForLC.SmoothingMethod;
            //this.ComboBox_PeakConsideration.SelectedIndex = (int)this.mainWindow.AnalysisParamForLC.DeconvolutionType;
            this.ComboBox_ReferenceFileID.SelectedIndex = this.mainWindow.AnalysisParamForLC.AlignmentReferenceFileID;
            this.ComboBox_NonLabeledReference.SelectedIndex = this.mainWindow.AnalysisParamForLC.NonLabeledReferenceID;
            this.ComboBox_FullyLabeledReference.SelectedIndex = this.mainWindow.AnalysisParamForLC.FullyLabeledReferenceID;
            this.ComboBox_LabeledElement.SelectedIndex = this.mainWindow.AnalysisParamForLC.IsotopeTrackingDictionary.SelectedID;
            this.ComboBox_FilteringMethod.SelectedIndex = (int)this.mainWindow.AnalysisParamForLC.BlankFiltering;

            WindowIsEnabledSetting(this.mainWindow.AnalysisParamForLC.ProcessOption, this.mainWindow.ProjectProperty.SeparationType);
          
            if (this.mainWindow.ProjectProperty.SeparationType == SeparationType.IonMobility) {
                Mouse.OverrideCursor = Cursors.Wait;
                setCalibrateInformation(this.mainWindow.AnalysisParamForLC, this.mainWindow.AnalysisFiles);
                Mouse.OverrideCursor = null;
            }
        }

        private void setCalibrateInformation(AnalysisParametersBean param, ObservableCollection<AnalysisFileBean> files) {
            if (param.FileidToCcsCalibrantData != null && param.FileidToCcsCalibrantData.Count > 0) return;
            param.FileidToCcsCalibrantData = new Dictionary<int, CoefficientsForCcsCalculation>();

            var isAllCalibrantImported = true;
            foreach (var file in files) {
                var ibfpath = file.AnalysisFilePropertyBean.AnalysisFilePath;
                using (var access = new RawDataAccess(ibfpath, 0, false, false, true)) {
                    var calinfo = access.ReadIonmobilityCalibrationInfo();
                    var fileid = file.AnalysisFilePropertyBean.AnalysisFileId;
                    CoefficientsForCcsCalculation ccsCalinfo;
                    if (calinfo == null) {
                        ccsCalinfo = new CoefficientsForCcsCalculation() {
                            IsAgilentIM = false, AgilentBeta = -1, AgilentTFix = -1,
                            IsBrukerIM = false, IsWatersIM = false, WatersCoefficient = -1, WatersExponent = -1, WatersT0 = -1
                        };
                    }
                    else {
                        ccsCalinfo = new CoefficientsForCcsCalculation() {
                            IsAgilentIM = calinfo.IsAgilentIM, AgilentBeta = calinfo.AgilentBeta, AgilentTFix = calinfo.AgilentTFix,
                            IsBrukerIM = calinfo.IsBrukerIM, IsWatersIM = calinfo.IsWatersIM, WatersCoefficient = calinfo.WatersCoefficient, WatersExponent = calinfo.WatersExponent, WatersT0 = calinfo.WatersT0
                        };
                        if (calinfo.IsAgilentIM) {
                            param.IonMobilityType = IonMobilityType.Dtims;
                            ((AnalysisParamSetForLcVM)this.DataContext).IsDTIMS = true;
                        }
                        else if (calinfo.IsWatersIM) {
                            param.IonMobilityType = IonMobilityType.Twims;
                            ((AnalysisParamSetForLcVM)this.DataContext).IsTWIMS = true;
                        }
                        else {
                            param.IonMobilityType = IonMobilityType.Tims;
                            ((AnalysisParamSetForLcVM)this.DataContext).IsTIMS = true;
                        }
                    }
                    
                    param.FileidToCcsCalibrantData[fileid] = ccsCalinfo;
                    if (ccsCalinfo.AgilentBeta == -1 && ccsCalinfo.AgilentTFix == -1 && 
                        ccsCalinfo.WatersCoefficient == -1 && ccsCalinfo.WatersExponent == -1 && ccsCalinfo.WatersT0 == -1) {
                        isAllCalibrantImported = false;
                    }
                }
            }
            param.IsAllCalibrantDataImported = isAllCalibrantImported;
            if (isAllCalibrantImported) {
                this.Label_CcsCalibrantImport.Content = "Status: imported";
            }
            else {
                this.Label_CcsCalibrantImport.Content = "Status: not imported yet";
            }
        }

        private void WindowIsEnabledSetting(ProcessOption processOption, SeparationType separationType)
        {
            this.processOption = processOption;
            switch (processOption)
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
                    TabItem_Adduct.IsEnabled = false;
                    TabItem_Identification.IsEnabled = false;

                    this.mainWindow.AnalysisParamForLC.TogetherWithAlignment = true;
                    CheckBox_WithAlignment.IsEnabled = false;

                    TabItem_Alignment.IsSelected = true;
                    break;
            }

            if (separationType == SeparationType.IonMobility) {
                TabItem_Mobility.IsEnabled = true;
            }
            else {
                TabItem_Mobility.IsEnabled = false;
            }
        }

        private LipidQueryBean getLbmQueries()
        {
            var lipidQueryBean = new LipidQueryBean();
            var queries = LbmQueryParcer.GetLbmQueries(this.mainWindow.ProjectProperty.IsLabPrivateVersion);
            foreach (var query in queries)
            {
                if (this.mainWindow.ProjectProperty.IonMode != IonMode.Both && query.IonMode != this.mainWindow.ProjectProperty.IonMode)
                {
                    query.IsSelected = false;
                }
            }
            lipidQueryBean.IonMode = this.mainWindow.ProjectProperty.IonMode;
            lipidQueryBean.SolventType = SolventType.CH3COONH4;
            lipidQueryBean.CollisionType = CollisionType.CID;
            lipidQueryBean.LbmQueries = queries;

            return lipidQueryBean;
        }

        private List<AdductIonInformationBean> getAdductIonInformationList(IonMode ionMode)
        {
            return AdductResourceParser.GetAdductIonInformationList(ionMode);

            //Uri fileUri;
            //if (ionMode == IonMode.Positive)
            //    fileUri = new Uri("/Resources/AdductIonResource_Positive.txt", UriKind.Relative);
            //else
            //    fileUri = new Uri("/Resources/AdductIonResource_Negative.txt", UriKind.Relative);

            //var info = Application.GetResourceStream(fileUri);
            //var adductList = new List<AdductIonInformationBean>();
            //var adduct = new AdductIonInformationBean();

            //bool checker = true;

            //using (var sr = new StreamReader(info.Stream))
            //{
            //    string line;
            //    string[] lineArray;
            //    sr.ReadLine();
            //    while (sr.Peek() > -1)
            //    {
            //        line = sr.ReadLine();
            //        if (line == "") break;
            //        lineArray = line.Split('\t');

            //        var adductIon = AdductIonParcer.GetAdductIonBean(lineArray[0]);

            //        adduct = new AdductIonInformationBean();
            //        adduct.AdductName = adductIon.AdductIonName;
            //        adduct.Charge = adductIon.ChargeNumber;
            //        adduct.AccurateMass = adductIon.AdductIonAccurateMass;
            //        adduct.IonMode = adductIon.IonMode;
            //        adduct.Xmer = adductIon.AdductIonXmer;

            //        if (checker) { adduct.Included = true; checker = false; }
            //        else adduct.Included = false;

            //        adductList.Add(adduct);
            //    }
            //}

            //return adductList;
        }

        private void Click_DatabaseFilePathSelect(object sender, RoutedEventArgs e)
        {
            if (this.mainWindow.ProjectProperty.TargetOmics == TargetOmics.Metablomics)
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "MSP file(*.msp)|*.msp*";
                ofd.Title = "Import a library file";
                ofd.RestoreDirectory = true;
                ofd.Multiselect = false;

                if (ofd.ShowDialog() == true)
                {
                    this.TextBox_LibraryFilePath.Text = ofd.FileName;
                }
            }
            else if (this.mainWindow.ProjectProperty.TargetOmics == TargetOmics.Lipidomics)
            {
                string mainDirectory = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
                if (System.IO.Directory.GetFiles(mainDirectory, "*." + SaveFileFormat.lbm + "*", 
                    System.IO.SearchOption.TopDirectoryOnly).Length == 1)
                {
                    var window = new LipidDbSetWin(this.mainWindow);
                    window.Owner = this;
                    window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                    window.ShowDialog();
                }
                else
                {
                    MessageBox.Show("There is no LBM file or several LBM files are existed in this application folder. Please see the tutorial.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
        }

        private void Click_TextReferenceFilePathSelect(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Text file(*.txt)|*.txt;";
            ofd.Title = "Import a library file for post identification";
            ofd.RestoreDirectory = true;
            ofd.Multiselect = false;

            if (ofd.ShowDialog() == true)
            {
                this.TextBox_PostIdentificationLibraryFilePath.Text = ofd.FileName;
            }
        }

        private void Click_TargetFormulaLibraryFilePathSelect(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Text file(*.txt)|*.txt;";
            ofd.Title = "Import target formulas library";
            ofd.RestoreDirectory = true;
            ofd.Multiselect = false;

            if (ofd.ShowDialog() == true)
            {
                this.TextBox_TargetFormulaLibraryFilePath.Text = ofd.FileName;
            }
        }

        private void ComboBox_SmoothingMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.DataContext == null) return;
            ((AnalysisParamSetForLcVM)this.DataContext).SmoothingMethod = (SmoothingMethod)((ComboBox)sender).SelectedIndex;
        }

        private void ComboBox_PeakConsideration_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.DataContext == null) return;
            ((AnalysisParamSetForLcVM)this.DataContext).DeconvolutionType = (DeconvolutionType)((ComboBox)sender).SelectedIndex;
        }
        
        private void Button_AdductIonAdd_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null) return;

            var window = new UserDefinedAdductSetWin(this, this.mainWindow);
            window.Owner = this;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;

            if (window.ShowDialog() == true)
            {
                ((AnalysisParamSetForLcVM)this.DataContext).UpdateAdductListByUser(this.mainWindow.AnalysisParamForLC.AdductIonInformationBeanList);
                UpdateLayout();
            }
        }

        private void Click_Cancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ComboBox_ReferenceFileID_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.DataContext == null) return;
            ((AnalysisParamSetForLcVM)this.DataContext).AlignmentReferenceFileID = ((ComboBox)sender).SelectedIndex;
        }

        private void Click_Load(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null) return;

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "MED file(*.med*)|*.med*";
            ofd.Title = "Import a method file";
            ofd.RestoreDirectory = true;

            if (ofd.ShowDialog() == true)
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var param = MessagePackHandler.LoadFromFile<AnalysisParametersBean>(ofd.FileName);
                param.ProcessOption = this.processOption;

                //var param = (AnalysisParametersBean)DataStorageLcUtility.LoadFromXmlFile(ofd.FileName, typeof(AnalysisParametersBean));

                if (param.AdductIonInformationBeanList != null && param.AdductIonInformationBeanList.Count != 0 && param.AdductIonInformationBeanList[0].IonMode != this.mainWindow.ProjectProperty.IonMode)
                {
                    MessageBox.Show("Importing file is not for " + this.mainWindow.ProjectProperty.IonMode.ToString() + ".", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Mouse.OverrideCursor = null;
                    return;
                }
                if (param.IsotopeTrackingDictionary == null) param.IsotopeTrackingDictionary = new IsotopeTrackingDictionary();

                param.AlignmentReferenceFileID = 0;
                param.NonLabeledReferenceID = 0;

                param.LipidQueryBean = DataStorageLcUtility.LipidQueryRetrieve(param.LipidQueryBean, this.mainWindow.ProjectProperty);

                if (param.MaxChargeNumber == 0)
                    param.MaxChargeNumber = 2;
                if (param.NumThreads == 0)
                    param.NumThreads = 1;

                if (param.QcAtLeastFilter == true)
                    param.QcAtLeastFilter = false;

                param.RetentionTimeCorrectionCommon = new RetentionTimeCorrection.RetentionTimeCorrectionCommon();
                if (param.IsIonMobility) {
                    var files = this.mainWindow.AnalysisFiles;
                    param.FileidToCcsCalibrantData = null;
                    setCalibrateInformation(param, files);
                }

                if (param.Ms2MassRangeBegin == 0 && param.Ms2MassRangeEnd == 0) {
                    param.Ms2MassRangeEnd = 2000;
                }

                ((AnalysisParamSetForLcVM)this.DataContext).Param = param;
                ((AnalysisParamSetForLcVM)this.DataContext).UpdateAdductList(param);
                ((AnalysisParamSetForLcVM)this.DataContext).UpdateExcludedMassList(param);
                ((AnalysisParamSetForLcVM)this.DataContext).VmUpdate();
                
                Mouse.OverrideCursor = null;
            }
        }

        private void DataGrid_ExcludeMassSetting_CurrentCellChanged(object sender, EventArgs e)
        {
            var cell = DataGridHelper.GetCell((DataGridCellInfo)this.DataGrid_ExcludeMassSetting.CurrentCell);
            if (cell == null) return;
            cell.Focus();
            this.DataGrid_ExcludeMassSetting.BeginEdit();
        }

        private void DataGrid_ExcludeMassSetting_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V & Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                string[] clipText = Clipboard.GetText().Replace("\r\n", "\n").Split('\n');
                List<string[]> clipTextList = new List<string[]>();
                for (int i = 0; i < clipText.Length; i++) { clipTextList.Add(clipText[i].Split('\t')); }
                if (clipTextList.Count > 1) clipTextList.RemoveAt(clipTextList.Count - 1);

                if (clipTextList.Count > 1 && this.DataGrid_ExcludeMassSetting.SelectedCells[0].Column.DisplayIndex == 0)
                {
                    int startRow = this.DataGrid_ExcludeMassSetting.Items.IndexOf(this.DataGrid_ExcludeMassSetting.SelectedCells[0].Item);
                    double exactMass, massTolerance;
                    for (int i = 0; i < clipTextList.Count; i++)
                    {
                        if (startRow + i > this.DataGrid_ExcludeMassSetting.Items.Count - 1) break;
                        if (clipTextList[i].Length > 0)
                        {
                            if (double.TryParse(clipTextList[i][0], out exactMass))
                            {
                                ((ExcludeMassVM)this.DataGrid_ExcludeMassSetting.Items[startRow + i]).ExcludedMass = (float)exactMass;
                            }
                        }
                        if (clipTextList[i].Length > 1)
                        {
                            if (double.TryParse(clipTextList[i][1], out massTolerance))
                            {
                                ((ExcludeMassVM)this.DataGrid_ExcludeMassSetting.Items[startRow + i]).MassTolerance = (float)massTolerance;
                            }
                        }
                    }
                    this.DataGrid_ExcludeMassSetting.UpdateLayout();
                }
                else if (clipTextList.Count > 1 && this.DataGrid_ExcludeMassSetting.SelectedCells[0].Column.DisplayIndex == 1)
                {
                    int startRow = this.DataGrid_ExcludeMassSetting.Items.IndexOf(this.DataGrid_ExcludeMassSetting.SelectedCells[0].Item);
                    double exactMass, massTolerance;
                    for (int i = 0; i < clipTextList.Count; i++)
                    {
                        if (startRow + i > this.DataGrid_ExcludeMassSetting.Items.Count - 1) break;
                        if (clipTextList[i].Length > 0)
                        {
                            if (double.TryParse(clipTextList[i][0], out massTolerance))
                            {
                                ((ExcludeMassVM)this.DataGrid_ExcludeMassSetting.Items[startRow + i]).MassTolerance = (float)massTolerance;
                            }
                        }
                    }
                    this.DataGrid_ExcludeMassSetting.UpdateLayout();
                }
                else if (clipTextList.Count == 1 && this.DataGrid_ExcludeMassSetting.SelectedCells[0].Column.DisplayIndex == 0)
                {
                    int startRow = this.DataGrid_ExcludeMassSetting.Items.IndexOf(this.DataGrid_ExcludeMassSetting.SelectedCells[0].Item);
                    for (int i = 0; i < this.DataGrid_ExcludeMassSetting.SelectedCells.Count; i++)
                    {
                        if (this.DataGrid_ExcludeMassSetting.SelectedCells[i].Column.DisplayIndex != 0) continue;
                        if (startRow + i > this.DataGrid_ExcludeMassSetting.Items.Count - 1) break;
                        double d;
                        if (double.TryParse(clipTextList[0][0], out d))
                            ((ExcludeMassVM)this.DataGrid_ExcludeMassSetting.Items[startRow + i]).ExcludedMass = (float)d;
                    }
                }
                else if (clipTextList.Count == 1 && this.DataGrid_ExcludeMassSetting.SelectedCells[0].Column.DisplayIndex == 1)
                {
                    int startRow = this.DataGrid_ExcludeMassSetting.Items.IndexOf(this.DataGrid_ExcludeMassSetting.SelectedCells[0].Item);
                    for (int i = 0; i < this.DataGrid_ExcludeMassSetting.SelectedCells.Count; i++)
                    {
                        if (this.DataGrid_ExcludeMassSetting.SelectedCells[i].Column.DisplayIndex != 1) continue;
                        if (startRow + i > this.DataGrid_ExcludeMassSetting.Items.Count - 1) break;
                        double d;
                        if (double.TryParse(clipTextList[0][1], out d))
                            ((ExcludeMassVM)this.DataGrid_ExcludeMassSetting.Items[startRow + i]).MassTolerance = (float)d;
                    }
                }
            }
        }

        private void AdvancedLibrarySearchOption_Button_Click(object sender, RoutedEventArgs e)
        {
            AdvancedLibrarySearchOptionWin advanceWindow = new AdvancedLibrarySearchOptionWin(this.mainWindow);
            advanceWindow.Owner = this;
            advanceWindow.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;

            if (advanceWindow.ShowDialog() == true)
            {

            }
        }

        private void ComboBox_LabeledElement_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.DataContext == null) return;
            ((AnalysisParamSetForLcVM)this.DataContext).IsotopeTrackingDictionary.SelectedID = ((ComboBox)sender).SelectedIndex;
        }

        private void ComboBox_NonLabeledReference_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.DataContext == null) return;
            ((AnalysisParamSetForLcVM)this.DataContext).NonLabeledReferenceID = ((ComboBox)sender).SelectedIndex;
        }

        private void ComboBox_FullyLabeledReference_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (this.DataContext == null)
                return;
            ((AnalysisParamSetForLcVM)this.DataContext).FullyLabeledReferenceID = ((ComboBox)sender).SelectedIndex;
        }

        private void CheckBox_OnlyReportTopHitForPostAnnotation_Unchecked(object sender, RoutedEventArgs e) {
            if (this.DataContext == null)
                return;
            ((AnalysisParamSetForLcVM)this.DataContext).OnlyReportTopHitForPostAnnotation = false;
        }

        private void CheckBox_OnlyReportTopHitForPostAnnotation_Checked(object sender, RoutedEventArgs e) {
            if (this.DataContext == null)
                return;
            ((AnalysisParamSetForLcVM)this.DataContext).OnlyReportTopHitForPostAnnotation = true;
        }

        private void ComboBox_FilteringMethod_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (this.DataContext == null)
                return;
            var index = ((ComboBox)sender).SelectedIndex;
            if (index < 0 || index > 2)
                index = 0;

            ((AnalysisParamSetForLcVM)this.DataContext).BlankFiltering = (BlankFiltering)index;
        }

        private void Click_CcsData_Browse(object sender, RoutedEventArgs e) {
            if (this.DataContext == null)
                return;
            var vm = (AnalysisParamSetForLcVM)this.DataContext;
            if (vm.Param.IonMobilityType == IonMobilityType.Tims) {
                MessageBox.Show(
                    "Bruker TIMS does not require the calibrant information. However, please reflect the calibration data to your raw data in Bruker DataAnalysis software before MS-DIAL analysis.",
                    "Notice", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            } else if (vm.Param.IonMobilityType == IonMobilityType.Twims) {
                var win = new WatersCcsCalibrationSetWin();
                win.Owner = this;
                win.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                win.ShowDialog();
            }
            else if (vm.Param.IonMobilityType == IonMobilityType.Dtims) {
                var win = new AgilentCcsCalibrationSetWin();
                win.Owner = this;
                win.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                win.ShowDialog();
            }
        }
    }
}
