using BCDev.XamlToys; // This comes from http://xamltoys.codeplex.com/. Unfortunately the repo no longer exists. We gratefully use+modify it here.
using Microsoft.Win32;
using Riken.Metabolomics.MsfinderCommon.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace Rfx.Riken.OsakaUniv
{
	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : Window
    {
        #region members
        private static MainWindow mainWindow;
        public string MainWindowTitle;
        private MainWindowVM mainWindowVM;

        public MainWindowVM MainWindowVM
        {
            get { return mainWindowVM; }
            set { mainWindowVM = value; }
        }
        #endregion

        #region constructors
        public MainWindow()
        {
            InitializeComponent();
            MainWindowInitialize();
        }

        public void MainWindowInitialize()
        {
            mainWindow = this;
            this.Title = Properties.Resources.VERSION;
            MainWindowTitle = this.Title;
            this.DataContext = new MainWindowVM(mainWindow);
            this.mainWindowVM = (MainWindowVM)this.DataContext;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var workingSpacePrep = new WorkingSpacePreparation();
            workingSpacePrep.Process(this, this.mainWindowVM);
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            VersionUpdateNotificationService.CheckForUpdates();
        }

        #endregion

        #region menu items
        private void menuItem_Import_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.RootFolder = Environment.SpecialFolder.Desktop;
            fbd.Description = "Choose a project folder.";
            fbd.SelectedPath = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                this.mainWindowVM.Refresh_ImportFolder(fbd.SelectedPath);
        }

        private void menuItem_Create_Click(object sender, RoutedEventArgs e)
        {
            var window = new CreateSpectrumFileWindow(this.mainWindowVM);
            window.Owner = this;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.ShowDialog();
        }

        private void menuItem_ParameterSetting_Click(object sender, RoutedEventArgs e)
        {
            var window = new AnalysisParameterSettingWindow(this.mainWindowVM);
            window.Owner = this;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.ShowDialog();
        }


        private void menuItem_BatchJob_Click(object sender, RoutedEventArgs e)
        {
            if (!checkFileImport()) return;

            var window = new BatchJobSettingWindow(this.mainWindowVM);
            window.Owner = this;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            
            if (window.ShowDialog() == true)
            {
                BatchJobProcess.Process(this, this.mainWindowVM);
            }
        }

        private void menuItem_HelpAbout_Click(object sender, RoutedEventArgs e)
        {
            var window = new HelpAboutWindow();
            window.DataContext = new HelpAboutWindowVM();
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void menuItem_UserDefinedAdduct_Click(object sender, RoutedEventArgs e)
        {
            var window = new UserDefinedAdductSetWindow(this, this.mainWindowVM);
            window.Owner = this;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.ShowDialog();
        }

        private void menuItem_PeakAssignmentSingle_Click(object sender, RoutedEventArgs e)
        {
            if (!checkFileImport()) return;

            var window = new PeakAssignerSettingWindow(this, this.mainWindowVM);
            window.Owner = this;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void menuItem_PeakAssignmentBatchJob_Click(object sender, RoutedEventArgs e)
        {
            if (!checkFileImport()) return;

            new PeakAssignerBatchProcess().Process(this, this.mainWindowVM);
        }

        private bool checkFileImport()
        {
            if (this.mainWindowVM == null) return false;

            if (this.mainWindowVM.AnalysisFiles == null || this.mainWindowVM.AnalysisFiles.Count == 0)
            {
                MessageBox.Show("Please import your data files.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        private void menuItem_CopyScreenshotToClipboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var target = this;

                System.Windows.Media.Drawing drawing = Utility.GetDrawingFromXaml((MainWindow)this);

                var wmfStream = new MemoryStream();
                using (var graphics = Utility.CreateEmf(wmfStream, drawing.Bounds))
                    Utility.RenderDrawingToGraphics(drawing, graphics);
                wmfStream.Position = 0;
                System.Drawing.Imaging.Metafile metafile = new System.Drawing.Imaging.Metafile(wmfStream);
                IntPtr hEMF, hEMF2;
                hEMF = metafile.GetHenhmetafile(); // invalidates mf

                if (!hEMF.Equals(new IntPtr(0)))
                {
                    hEMF2 = ExtensionMethods.CopyEnhMetaFile(hEMF, new IntPtr(0));
                    if (!hEMF2.Equals(new IntPtr(0)))
                    {
                        if (ExtensionMethods.OpenClipboard(((IWin32Window)this.OwnerAsWin32()).Handle))
                        {
                            if (ExtensionMethods.EmptyClipboard())
                            {
                                ExtensionMethods.SetClipboardData(14 /*CF_ENHMETAFILE*/, hEMF2);
                                ExtensionMethods.CloseClipboard();
                            }
                        }
                    }
                    ExtensionMethods.DeleteEnhMetaFile(hEMF);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        #endregion

        #region context menu items
        private void contextMenu_ShowFormulaResultDetail_Click(object sender, RoutedEventArgs e)
        {
            if (this.mainWindowVM == null) return;
            if (this.mainWindowVM.SelectedFormulaVM == null) return;

            var window = new FormulaResultWindow(this.mainWindowVM);
            window.Owner = this;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void contextMenu_SearchStructures_Click(object sender, RoutedEventArgs e)
        {
            if (this.mainWindowVM == null) return;
            if (this.mainWindowVM.FormulaResultVMs == null) return;

            RefreshUtility.UpdataFiles(this.mainWindowVM, this.mainWindowVM.SelectedRawFileId);
            
            var process = new StructureFinderCurrentSearch();
            process.Process(mainWindow, mainWindowVM);
        }

        private void contextMenu_SaveImageAs_Click(object sender, RoutedEventArgs e)
        {
            var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;

            SaveImageAsWin window = new SaveImageAsWin(target);
            window.Owner = this;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void contextMenu_SaveImageForPublication_Click(object sender, RoutedEventArgs e) {
            if (this.mainWindowVM == null) return;
            if (this.mainWindowVM.SelectedRawFileId < 0) return;
            if (this.mainWindowVM.RawDataVM == null) return;

            var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;
            ImageExportUtility.particular_settings_tada(target, this.mainWindowVM.RawDataVM.RawData);
        }

        private void contextMenu_CopyImageAs_Click(object sender, RoutedEventArgs e)
        {
            var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;

            CopyImageAsWin window = new CopyImageAsWin(target);
            window.Owner = this;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.Show();
        }
        #endregion

        #region events
        private void DataGrid_FormulaResult_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.mainWindowVM == null) return;
            RefreshUtility.RefreshMS1Spectrum(this, this.mainWindowVM);
            RefreshUtility.RefrechMS2Spectrum(this, this.mainWindowVM);
            RefreshUtility.StructureDataFileRefresh(this, this.mainWindowVM, this.mainWindowVM.SelectedRawFileId);
        }

        private void DataGrid_FragmenterResult_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.mainWindowVM == null) return;
            if (this.mainWindowVM.SelectedFragmenterVM == null) return;

            RefreshUtility.ActualMsMsVsInSilicoMsMsUiRefresh(this, this.mainWindowVM);

            if (this.TabItem_StructureImage.IsSelected == true) {
                this.Image_Structure.Source = UiAccessUtility.GetSmilesAsImage(this.mainWindowVM.SelectedFragmenterVM, this.TabControl_Structure.ActualWidth, this.TabControl_Structure.ActualHeight);
            }
        }
        #endregion

        private void ToggleButton_ShowMs1RawSpectrum_Checked(object sender, RoutedEventArgs e)
        {
            if (this.mainWindowVM == null) return;
            
            this.ToggleButton_ShowIsotopeSpectrum.IsChecked = false;
            
            RefreshUtility.RefreshMS1Spectrum(this, this.mainWindowVM);
        }

        private void ToggleButton_ShowIsotopeSpectrum_Checked(object sender, RoutedEventArgs e)
        {
            if (this.mainWindowVM == null) return;
            
            this.ToggleButton_ShowMs1RawSpectrum.IsChecked = false;
            
            RefreshUtility.RefreshMS1Spectrum(this, this.mainWindowVM);
        }

        private void ToggleButton_ShowMs2RawSpectrum_Checked(object sender, RoutedEventArgs e)
        {
            if (this.mainWindowVM == null) return;
            
            this.ToggleButton_ShowProductIonSpectrum.IsChecked = false;
            this.ToggleButton_ShowNeutralLossSpectrum.IsChecked = false;
            
            RefreshUtility.RefrechMS2Spectrum(this, this.mainWindowVM);
        }

        private void ToggleButton_ShowProductIonSpectrum_Checked(object sender, RoutedEventArgs e)
        {
            if (this.mainWindowVM == null) return;
            
            this.ToggleButton_ShowMs2RawSpectrum.IsChecked = false;
            this.ToggleButton_ShowNeutralLossSpectrum.IsChecked = false;
            
            RefreshUtility.RefrechMS2Spectrum(this, this.mainWindowVM);
        }

        private void ToggleButton_ShowNeutralLossSpectrum_Checked(object sender, RoutedEventArgs e)
        {
            if (this.mainWindowVM == null) return;

            this.ToggleButton_ShowProductIonSpectrum.IsChecked = false;
            this.ToggleButton_ShowMs2RawSpectrum.IsChecked = false;
            
            RefreshUtility.RefrechMS2Spectrum(this, this.mainWindowVM);
        }

        private void Button_ShowSubstructureViewer_Click(object sender, RoutedEventArgs e)
        {
            if (this.mainWindowVM == null) return;
            if (this.mainWindowVM.SelectedFormulaVM == null) return;

            Mouse.OverrideCursor = Cursors.Wait;

            var shortMessageWin = new ShortMessageWindow() {
                Owner = mainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            shortMessageWin.Label_MessageTitle.Content = "Preparing the viewer..";
            shortMessageWin.Show();

            var window = new SubstructureViewer(this.mainWindowVM.RawDataVM.RawData, 
                this.mainWindowVM.SelectedFormulaVM.FormulaResult, 
                this.mainWindowVM.FragmentOntologyDB);

            shortMessageWin.Close();
            Mouse.OverrideCursor = null;

            window.Owner = this;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void Button_ShowFseaResult_Click(object sender, RoutedEventArgs e) {
            if (this.mainWindowVM == null) return;
            if (this.mainWindowVM.SelectedFormulaVM == null) return;

            Mouse.OverrideCursor = Cursors.Wait;

            var window = new FseaResultViewer();
            window.DataContext = new FseaResultViewerVM(this.mainWindowVM.SelectedFormulaVM.FormulaResult,
                this.mainWindowVM.ChemicalOntologies, this.mainWindowVM.FragmentOntologyDB, this.mainWindowVM.RawDataVM.IonMode);
            window.Owner = this;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.Show();

            Mouse.OverrideCursor = null;

           
        }

        private void menuItem_BatchResultExport_Click(object sender, RoutedEventArgs e)
        {
            if (!checkFileImport()) return;

            var window = new BatchExportWin(this, this.mainWindowVM);
            window.Owner = this;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.Show();

        }


        private void menuItem_PeakAnnotationResultExportAsMsp_Click(object sender, RoutedEventArgs e)
        {
            if (!checkFileImport()) return;

            var sfd = new SaveFileDialog();
            sfd.Filter = "MSP file(.msp)|*.msp|All Files (*.*)|*.*";

            if (sfd.ShowDialog() == true)
            {
                var filePath = sfd.FileName;
                Mouse.OverrideCursor = Cursors.Wait;

                ExportUtility.PeakAnnotationResultExportAsMsp(this, this.mainWindowVM, filePath);

                Mouse.OverrideCursor = null;
            }
        }

        private void menuItem_PeakAnnotationResultExportAsMassBankRecord_Click(object sender, RoutedEventArgs e)
        {
            if (!checkFileImport()) return;

            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog
            {
                RootFolder = Environment.SpecialFolder.Desktop,
                Description = "Select a folder to export the MassBank records to",
                SelectedPath = string.IsNullOrEmpty(this.mainWindowVM.DataStorageBean.ImportFolderPath)
                    ? Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                    : this.mainWindowVM.DataStorageBean.ImportFolderPath
            };

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                Mouse.OverrideCursor = Cursors.Wait;

                ExportUtility.PeakAnnotationResultExportAsMassBankRecord(this.mainWindowVM, fbd.SelectedPath);

                Mouse.OverrideCursor = null;
            }
        }

        private void menuItem_Test_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog();
            sfd.Filter = "TEXT file(.txt)|*.txt|All Files (*.*)|*.*";

            if (sfd.ShowDialog() == true) {
                var filePath = sfd.FileName;
                //MsFinderValidation.MassBankTestResult(this, this.mainWindowVM, filePath);
                MsFinderValidation.FormulaAccuracyTest(this, this.mainWindowVM, filePath);
            }

            //var exportFolder = @"D:\PROJECT_Plant Specialized Metabolites Annotations\MS-FINDER validations\MSMS Validation kits\Kits";
            //var filepathDictionary = new Dictionary<string, string>() {
            //    #region
            //    { @"D:\PROJECT_Plant Specialized Metabolites Annotations\MS-FINDER validations\MSMS Validation kits\Kits\Negative\MSMS-Public-ValidationKit-Neg-C-fixed", "High quality MSMS kit-public only-Neg.msp" },
            //   // { @"D:\PROJECT_Plant Specialized Metabolites Annotations\MS-FINDER validations\MSMS Validation kits\Kits\Negative\MSMS-Public-ValidationKit-Neg", "MSMS-Public-ValidationKit-Neg-MfResult.txt" },
            //   // { @"D:\PROJECT_Plant Specialized Metabolites Annotations\MS-FINDER validations\MSMS Validation kits\Kits\Negative\MSMS-Public-ValidationKit-Neg-C-fixed", "MSMS-Public-ValidationKit-Neg-C-fixed-MfResult.txt" },
            //   // { @"D:\PROJECT_Plant Specialized Metabolites Annotations\MS-FINDER validations\MSMS Validation kits\Kits\Negative\MSMS-Public-ValidationKit-Neg-C-fixed-combined", "MSMS-Public-ValidationKit-Neg-C-fixed-combined-MfResult.txt" },
            //   // { @"D:\PROJECT_Plant Specialized Metabolites Annotations\MS-FINDER validations\MSMS Validation kits\Kits\Negative\MSMS-Public-ValidationKit-Neg-combined", "MSMS-Public-ValidationKit-Neg-combined-MfResult.txt" },

            //  //  { @"D:\PROJECT_Plant Specialized Metabolites Annotations\MS-FINDER validations\MSMS Validation kits\Kits\Positive\MSMS-Public-ValidationKit-Pos", "MSMS-Public-ValidationKit-Pos-MfResult.txt" },
            //    { @"D:\PROJECT_Plant Specialized Metabolites Annotations\MS-FINDER validations\MSMS Validation kits\Kits\Positive\MSMS-Public-ValidationKit-Pos-C-fixed", "High quality MSMS kit-public only-Pos.msp" },
            //  //  { @"D:\PROJECT_Plant Specialized Metabolites Annotations\MS-FINDER validations\MSMS Validation kits\Kits\Positive\MSMS-Public-ValidationKit-Pos-C-fixed-combined", "MSMS-Public-ValidationKit-Pos-C-fixed-combined-MfResult.txt" },
            //  //  { @"D:\PROJECT_Plant Specialized Metabolites Annotations\MS-FINDER validations\MSMS Validation kits\Kits\Positive\MSMS-Public-ValidationKit-Pos-combined", "MSMS-Public-ValidationKit-Pos-combined-MfResult.txt" },
            //    #endregion
            //};

            //foreach (var pair in filepathDictionary) {
            //    var filepath = pair.Key;
            //    var output = exportFolder + "\\" + pair.Value;
            //    this.mainWindowVM.Refresh_ImportFolder(filepath);

            //    MsFinderValidation.MassBankTestResult(this, this.mainWindowVM, output);
            //}

            //MassBankFragmentAssigner.Fragmentasigner();
            //InSilicoFragmenter.FragmentGenerator(@"C:\Users\tensa_000\Documents\Metabolomics project\Small Molecular SDFs\20150106_Downloaded_Sdfs\IntegratedMetaData\ExistStructureDB.sdf", @"C:\Users\tensa_000\Documents\Metabolomics project\Small Molecular SDFs\20150106_Downloaded_Sdfs\IntegratedMetaData\ExistStructureFragments.sfd");
        }

        private void menuItem_FseaResult_Click(object sender, RoutedEventArgs e) {
            var sfd = new SaveFileDialog();
            sfd.Filter = "TEXT file(.txt)|*.txt|All Files (*.*)|*.*";

            //if (sfd.ShowDialog() == true) {
            //    var filePath = sfd.FileName;
            //    var curatedOntologyListFile = @"C:\Users\hiroshi.tsugawa\Dropbox\Manuscript mapping plant metabolomes by mass spectrometry informatics\Supplementary files\Ontology curations.txt";

            //    MsFinderValidation.FseaResultExport(this, this.mainWindowVM, filePath, curatedOntologyListFile, "Positive");
            //}


            var exportFolder = @"D:\PROJECT_Plant Specialized Metabolites Annotations\MS-FINDER validations\MSMS Validation kits\Kits";
            var filepathDictionary = new Dictionary<string, string>() {
                #region
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\MS-FINDER validations\MSMS Validation kits\Kits\Negative\MSMS-Public-ValidationKit-Neg", "MSMS-Public-ValidationKit-Neg-FseaResult.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\MS-FINDER validations\MSMS Validation kits\Kits\Negative\MSMS-Public-ValidationKit-Neg-C-fixed", "MSMS-Public-ValidationKit-Neg-C-fixed-FseaResult.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\MS-FINDER validations\MSMS Validation kits\Kits\Negative\MSMS-Public-ValidationKit-Neg-C-fixed-combined", "MSMS-Public-ValidationKit-Neg-C-fixed-combined-FseaResult.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\MS-FINDER validations\MSMS Validation kits\Kits\Negative\MSMS-Public-ValidationKit-Neg-combined", "MSMS-Public-ValidationKit-Neg-combined-FseaResult.txt" },

                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\MS-FINDER validations\MSMS Validation kits\Kits\Positive\MSMS-Public-ValidationKit-Pos", "MSMS-Public-ValidationKit-Pos-FseaResult.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\MS-FINDER validations\MSMS Validation kits\Kits\Positive\MSMS-Public-ValidationKit-Pos-C-fixed", "MSMS-Public-ValidationKit-Pos-C-fixed-FseaResult.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\MS-FINDER validations\MSMS Validation kits\Kits\Positive\MSMS-Public-ValidationKit-Pos-C-fixed-combined", "MSMS-Public-ValidationKit-Pos-C-fixed-combined-FseaResult.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\MS-FINDER validations\MSMS Validation kits\Kits\Positive\MSMS-Public-ValidationKit-Pos-combined", "MSMS-Public-ValidationKit-Pos-combined-FseaResult.txt" },
                #endregion
            };

            var curatedOntologyListFile = @"C:\Users\hiroshi.tsugawa\Dropbox\Manuscript mapping plant metabolomes by mass spectrometry informatics\Supplementary files\Ontology curations.txt";

            foreach (var pair in filepathDictionary) {
                var filepath = pair.Key;
                var output = Path.Combine(exportFolder, pair.Value);
                this.mainWindowVM.Refresh_ImportFolder(filepath);

                var filename = System.IO.Path.GetFileNameWithoutExtension(filepath);
                var ionMode = filename.Contains("Pos") ? "Positive" : "Negative";

                MsFinderValidation.FseaResultExport(this, this.mainWindowVM, output, curatedOntologyListFile, ionMode);
            }

        }

        private void menuItem_ScoreExport_Click(object sender, RoutedEventArgs e) {
            var sfd = new SaveFileDialog();
            sfd.Filter = "TEXT file(.txt)|*.txt|All Files (*.*)|*.*";

            if (sfd.ShowDialog() == true) {
                var filePath = sfd.FileName;
                MsFinderValidation.ScoreExport(this, this.mainWindowVM, filePath);
            }
        }

        private void menuItem_CasmiResultExport2017_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.RootFolder = Environment.SpecialFolder.Desktop;
            fbd.Description = "Choose a project folder.";
            fbd.SelectedPath = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                MsFinderValidation.CasmiResultExport(this, this.mainWindowVM, fbd.SelectedPath);
        }

        private void menuItem_SpecializedMetaboliteAnnotation_Click(object sender, RoutedEventArgs e) {
            var exportFolder = @"D:\PROJECT_Plant Specialized Metabolites Annotations\Msfinder annotation results Plant C12-C13 manual curation−VS2";
            var filepathDictionary = new Dictionary<string, string>() {
                #region
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\1 Arabidopsis thaliana\Leaf and stem\Negative\Result\Mat files", "AT_LeafStem_Neg_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\1 Arabidopsis thaliana\Leaf and stem\Positive\Result\Mat files", "AT_LeafStem_Pos_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\1 Arabidopsis thaliana\Root\Negative\Result\Mat files", "AT_Root_Neg_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\1 Arabidopsis thaliana\Root\Positive\Result\Mat files", "AT_Root_Pos_msfinder_vs1.txt" },

                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\2 Nicotiana tabacum\Leaf and stem\Negative\Result\Mat files", "NT_LeafStem_Neg_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\2 Nicotiana tabacum\Leaf and stem\Positive\Result\Mat files", "NT_LeafStem_Pos_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\2 Nicotiana tabacum\Root\Negative\Result\Mat files", "NT_Root_Neg_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\2 Nicotiana tabacum\Root\Positive\Result\Mat files", "NT_Root_Pos_msfinder_vs1.txt" },

                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\3 Allium cepa\Bulb\Negative\Result\Mat files", "AC_Bulb_Neg_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\3 Allium cepa\Bulb\Positive\Result\Mat files", "AC_Bulb_Pos_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\3 Allium cepa\Leaf\Negative\Result\Mat files", "AC_Leaf_Neg_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\3 Allium cepa\Leaf\Positive\Result\Mat files", "AC_Leaf_Pos_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\3 Allium cepa\Root\Negative\Result\Mat files", "AC_Root_Neg_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\3 Allium cepa\Root\Positive\Result\Mat files", "AC_Root_Pos_msfinder_vs1.txt" },

                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\4 Glycine max\Leaf and stem\Negative\Result\Mat files", "GM_LeafStem_Neg_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\4 Glycine max\Leaf and stem\Positive\Result\Mat files", "GM_LeafStem_Pos_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\4 Glycine max\Root\Negative\Result\Mat files", "GM_Root_Neg_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\4 Glycine max\Root\Positive\Result\Mat files", "GM_Root_Pos_msfinder_vs1.txt" },

                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\5 Glycyrrhiza glabra\Leaf and stem\Negative\Result\Mat files", "GG_LeafStem_Neg_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\5 Glycyrrhiza glabra\Leaf and stem\Positive\Result\Mat files", "GG_LeafStem_Pos_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\5 Glycyrrhiza glabra\Root\Negative\Result\Mat files", "GG_Root_Neg_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\5 Glycyrrhiza glabra\Root\Positive\Result\Mat files", "GG_Root_Pos_msfinder_vs1.txt" },

                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\6 Glycyrrhiza urahensis\Leaf and stem\Negative\Result\Mat files", "GU_LeafStem_Neg_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\6 Glycyrrhiza urahensis\Leaf and stem\Positive\Result\Mat files", "GU_LeafStem_Pos_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\6 Glycyrrhiza urahensis\Root\Negative\Result\Mat files", "GU_Root_Neg_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\6 Glycyrrhiza urahensis\Root\Positive\Result\Mat files", "GU_Root_Pos_msfinder_vs1.txt" },

                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\7 Lycopersicon esculentum\Fruit green\Negative\Result\Mat files", "LE_FruitGreen_Neg_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\7 Lycopersicon esculentum\Fruit green\Positive\Result\Mat files", "LE_FruitGreen_Pos_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\7 Lycopersicon esculentum\Leaf and stem\Negative\Result\Mat files", "LE_LeafStem_Neg_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\7 Lycopersicon esculentum\Leaf and stem\Positive\Result\Mat files", "LE_LeafStem_Pos_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\7 Lycopersicon esculentum\Ripe\Negative\Result\Mat files", "LE_Ripe_Neg_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\7 Lycopersicon esculentum\Ripe\Positive\Result\Mat files", "LE_Ripe_Pos_msfinder_vs1.txt" },

                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\8 Medicago truncatula\Flower\Negative\Result\Mat files", "MT_Flower_Neg_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\8 Medicago truncatula\Flower\Positive\Result\Mat files", "MT_Flower_Pos_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\8 Medicago truncatula\Leaf and stem\Negative\Result\Mat files", "MT_LeafStem_Neg_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\8 Medicago truncatula\Leaf and stem\Positive\Result\Mat files", "MT_LeafStem_Pos_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\8 Medicago truncatula\Ripe pod\Negative\Result\Mat files", "MT_RipePod_Neg_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\8 Medicago truncatula\Ripe pod\Positive\Result\Mat files", "MT_RipePod_Pos_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\8 Medicago truncatula\Root\Negative\Result\Mat files", "MT_Root_Neg_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\8 Medicago truncatula\Root\Positive\Result\Mat files", "MT_Root_Pos_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\8 Medicago truncatula\Seed\Negative\Result\Mat files", "MT_Seed_Neg_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\8 Medicago truncatula\Seed\Positive\Result\Mat files", "MT_Seed_Pos_msfinder_vs1.txt" },

                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\9 Oryza sativa\Leaf and stem\Negative\Result\Mat files", "OS_LeafStem_Neg_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\9 Oryza sativa\Leaf and stem\Positive\Result\Mat files", "OS_LeafStem_Pos_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\9 Oryza sativa\Root\Negative\Result\Mat files", "OS_Root_Neg_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\9 Oryza sativa\Root\Positive\Result\Mat files", "OS_Root_Pos_msfinder_vs1.txt" },

                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\10 Solanum tuberosum\Leaf and stem\Negative\Result\Mat files", "ST_LeafStem_Neg_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\10 Solanum tuberosum\Leaf and stem\Positive\Result\Mat files", "ST_LeafStem_Pos_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\10 Solanum tuberosum\Root\Negative\Result\Mat files", "ST_Root_Neg_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\10 Solanum tuberosum\Root\Positive\Result\Mat files", "ST_Root_Pos_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\10 Solanum tuberosum\Tuber\Negative\Result\Mat files", "ST_Tuber_Neg_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\10 Solanum tuberosum\Tuber\Positive\Result\Mat files", "ST_Tuber_Pos_msfinder_vs1.txt" },

                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\11 Zea mays\Leaf\Negative\Result\Mat files", "ZM_Leaf_Neg_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\11 Zea mays\Leaf\Positive\Result\Mat files", "ZM_Leaf_Pos_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\11 Zea mays\Root\Negative\Result\Mat files", "ZM_Root_Neg_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\11 Zea mays\Root\Positive\Result\Mat files", "ZM_Root_Pos_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\11 Zea mays\Seed\Negative\Result\Mat files", "ZM_Seed_Neg_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\11 Zea mays\Seed\Positive\Result\Mat files", "ZM_Seed_Pos_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\11 Zea mays\Stem\Negative\Result\Mat files", "ZM_Stem_Neg_msfinder_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\11 Zea mays\Stem\Positive\Result\Mat files", "ZM_Stem_Pos_msfinder_vs1.txt" }
                #endregion
            };
            foreach (var pair in filepathDictionary) {
                var filepath = pair.Key;
                var output = exportFolder + "\\" + pair.Value;
                this.mainWindowVM.Refresh_ImportFolder(filepath);

                MsFinderValidation.ExportPlantSpecializedMetaboliteAnnotationProjectResult(this, this.mainWindowVM, output);
            }
        }

        private void menuItem_ExportMsFinderSpecificField_Click(object sender, RoutedEventArgs e) {
            var sfd = new SaveFileDialog();
            sfd.Filter = "TEXT file(.txt)|*.txt|All Files (*.*)|*.*";

            if (sfd.ShowDialog() == true) {
                var filePath = sfd.FileName;
                MsFinderValidation.ExportPlantSpecializedMetaboliteAnnotationProjectResult(this, this.mainWindowVM, filePath);
            }
        }

        private void menuItem_VariableTest_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog();
            sfd.Filter = "TEXT file(.txt)|*.txt|All Files (*.*)|*.*";

            if (sfd.ShowDialog() == true)
            {
                var filePath = sfd.FileName;
                MsFinderValidation.FormulaVariablesTestResult(this, this.mainWindowVM, filePath);
            }
        }

        private void menuItem_PeakAssignmentTest_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog();
            sfd.Filter = "TEXT file(.txt)|*.txt|All Files (*.*)|*.*";

            if (sfd.ShowDialog() == true)
            {
                var filePath = sfd.FileName;
                MsFinderValidation.PeakAssignmentResultExport(this, this.mainWindowVM, filePath);
            }
        }

        private void menuItem_TopFiveFormulaCheck_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog();
            sfd.Filter = "TEXT file(.txt)|*.txt|All Files (*.*)|*.*";

            if (sfd.ShowDialog() == true)
            {
                var filePath = sfd.FileName;
                MsFinderValidation.TopFiveFormulaResult(this, this.mainWindowVM, filePath);
            }
        }

        private void menuItem_AllStructureCandidateGenerateCheck_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog();
            sfd.Filter = "TEXT file(.txt)|*.txt|All Files (*.*)|*.*";

            if (sfd.ShowDialog() == true) {
                var filePath = sfd.FileName;
                MsFinderValidation.AllStructureCandidateReports(this, this.mainWindowVM, filePath);
            }
        }

        private void TabControl_Structure_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.mainWindowVM == null) return;
            if (this.mainWindowVM.SelectedFragmenterVM == null) return;
            
            if (this.TabItem_StructureImage.IsSelected == true)
            {
                this.Image_Structure.Source = UiAccessUtility.GetSmilesAsImage(this.mainWindowVM.SelectedFragmenterVM, this.TabControl_Structure.ActualWidth, this.TabControl_Structure.ActualHeight);
            }
        }

        private void menuItem_MspToSeparatedMSPs_Click(object sender, RoutedEventArgs e) {
            var window = new ConversionWin("msp");
            window.Owner = this;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void menuItem_MgfToSeaparatedMSPs_Click(object sender, RoutedEventArgs e) {
            var window = new ConversionWin("mgf");
            window.Owner = this;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void menuItem_MolecularNetworking_Click(object sender, RoutedEventArgs e) {

            if (!checkFileImport()) return;
            if (this.mainWindowVM.DataStorageBean.AnalysisParameter.IsTmsMeoxDerivative) {
                MessageBox.Show("This function is now available for MS/MS spectra", "Message", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var window = new MolecularNetworkingSettingWin();
            window.Owner = this;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void menuItem_FseaParameterSet_Click(object sender, RoutedEventArgs e) {
            var window = new FseaParamerSetWin();
            window.Owner = this;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void menuItem_MolecularNetworkExport_Click(object sender, RoutedEventArgs e) {
            if (!checkFileImport()) return;
            if (this.mainWindowVM.DataStorageBean.AnalysisParameter.IsTmsMeoxDerivative) {
                MessageBox.Show("This function is now available for MS/MS spectra", "Message", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var window = new MolecularNetworkingExportWin();
            window.Owner = this;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void menuItem_ReflectMsfinderResultToMspFile_Click(object sender, RoutedEventArgs e) {
            if (!checkFileImport()) return;
            Mouse.OverrideCursor = Cursors.Wait;
            ExportUtility.ReflectMsfinderResultToMspFile(this, this.mainWindowVM);
            Mouse.OverrideCursor = null;
        }

        //private void MsfinderEvent_KeyUp(object sender, KeyEventArgs e) {
        //    if (!checkFileImport()) return;
        //    if (e.Key == Key.D && Keyboard.Modifiers == ModifierKeys.Control) {
        //        var fileId = this.mainWindowVM.SelectedRawFileId;
        //        var rawdataVM = this.mainWindowVM.RawDataVM;
        //        rawdataVM.IsMarked = true;
        //        this.mainWindowVM.DataStorageBean.QueryFiles[fileId].BgColor = Brushes.Gray;
        //    }
        //    else if (e.Key == Key.Q && Keyboard.Modifiers == ModifierKeys.Control) {
        //        var fileId = this.mainWindowVM.SelectedRawFileId;
        //        var rawdataVM = this.mainWindowVM.RawDataVM;
        //        rawdataVM.IsMarked = false;
        //        this.mainWindowVM.DataStorageBean.QueryFiles[fileId].BgColor = Brushes.White;
        //    }
        //}
    }
}
