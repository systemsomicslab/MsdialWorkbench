using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv
{
    public class MrmprobsExportVM : ViewModelBase
    {
        private AnalysisParametersBean paramLC;
        private AnalysisParamOfMsdialGcms paramGC;
        private Ionization ionization;
        private string exportFolderPath;
        private float mpMs1Tolerance;
        private float mpMs2Tolerance;
        private float mpRtTolerance;
        private float identificationScoreCutOff;
        private int mpTopN;
        private bool mpIsIncludeMsLevel1;
        private bool mpIsUseMs1LevelForQuant;
        private bool mpIsFocusedSpotOutput;
        private bool mpIsReferenceBaseOutput;
        private bool isAlignmentView;
        private bool isExportOtherCandidates;

        public MrmprobsExportVM(bool isAlignmentView)
        {
            this.isAlignmentView = isAlignmentView;
        }

        #region properties
        /// <summary>
        /// Folder where the export function will export Mrmprobs library
        /// </summary>
        [Required(ErrorMessage = "Choose a folder for the exported file.")]
        public string ExportFolderPath
        {
            get { return exportFolderPath; }
            set { if (exportFolderPath == value) return; exportFolderPath = value; OnPropertyChanged("ExportFolderPath"); }
        }

        public float MpMs1Tolerance
        {
            get { return mpMs1Tolerance; }
            set { if (mpMs1Tolerance == value) return; mpMs1Tolerance = value; OnPropertyChanged("MpMs1Tolerance"); }
        }

        public float MpMs2Tolerance
        {
            get { return mpMs2Tolerance; }
            set { if (mpMs2Tolerance == value) return; mpMs2Tolerance = value; OnPropertyChanged("MpMs2Tolerance"); }
        }

        public float MpRtTolerance
        {
            get { return mpRtTolerance; }
            set { if (mpRtTolerance == value) return; mpRtTolerance = value; OnPropertyChanged("MpRtTolerance"); }
        }

        public int MpTopNoutput
        {
            get { return mpTopN; }
            set { if (mpTopN == value) return; mpTopN = value; OnPropertyChanged("MpTopNoutput"); }
        }

        public bool IsIncludeMsLevel1
        {
            get { return mpIsIncludeMsLevel1; }
            set { if (mpIsIncludeMsLevel1 == value) return; 
                mpIsIncludeMsLevel1 = value; 
                OnPropertyChanged("IsIncludeMsLevel1");

                if (mpIsIncludeMsLevel1 == false) {
                    IsUseMs1LevelForQuant = false;
                }
            }
        }

        public bool IsUseMs1LevelForQuant
        {
            get { return mpIsUseMs1LevelForQuant; }
            set { if (mpIsUseMs1LevelForQuant == value) return; mpIsUseMs1LevelForQuant = value; OnPropertyChanged("IsUseMs1LevelForQuant"); }
        }

        public bool IsFocusedSpotOutput
        {
            get { return mpIsFocusedSpotOutput; }
            set { if (mpIsFocusedSpotOutput == value) return; mpIsFocusedSpotOutput = value; OnPropertyChanged("IsFocusedSpotOutput"); }
        }

        public bool IsReferenceBaseOutput
        {
            get { return mpIsReferenceBaseOutput; }
            set { if (mpIsReferenceBaseOutput == value) return; mpIsReferenceBaseOutput = value; OnPropertyChanged("IsReferenceBaseOutput"); }
        }

        public bool IsExportOtherCandidates {
            get {
                return isExportOtherCandidates;
            }

            set {
                isExportOtherCandidates = value;
                OnPropertyChanged("IsExportOtherCandidates");
            }
        }

        public float IdentificationScoreCutOff {
            get {
                return identificationScoreCutOff;
            }

            set {
                identificationScoreCutOff = value;
                OnPropertyChanged("IdentificationScoreCutOff");
            }
        }
        #endregion

        #region commands

        /// <summary>
        /// Sets up the view model for the MrmprobsExport window in InvokeCommandAction
        /// </summary>
        private DelegateCommand windowLoaded;
        public DelegateCommand WindowLoaded
        {
            get
            {
                return windowLoaded ?? (windowLoaded = new DelegateCommand(Window_Loaded, obj => { return true; }));
            }
        }

        /// <summary>
        /// Action for the WindowLoaded command
        /// </summary>
        /// <param name="obj"></param>
        private void Window_Loaded(object obj)
        {
            var view = (MrmprobsExportWin)obj;
            var mainWindow = (MainWindow)view.Owner;

            this.paramLC = mainWindow.AnalysisParamForLC;
            this.paramGC = mainWindow.AnalysisParamForGC;
            this.ionization = mainWindow.ProjectProperty.Ionization;
            this.exportFolderPath = string.Empty;

            if (this.ionization == Ionization.EI) {
                view.TextBox_MpMs2Tolerance.IsEnabled = false;
                view.CheckBox_IsIncludeMsLevel1.IsEnabled = false;
                view.CheckBox_IsUseMs1LevelForQuant.IsEnabled = false;
                view.CheckBox_IsExportOtherCandidates.IsEnabled = false;
            }

            copyParamToVM();
        }

        private void copyParamToVM()
        {
            if (this.ionization == Ionization.ESI) {
                if (paramLC.MpMs1Tolerance == 0) {
                    paramLC.MpMs1Tolerance = 0.005F;
                    paramLC.MpMs2Tolerance = 0.01F;
                    paramLC.MpRtTolerance = 0.5F;
                    paramLC.MpTopN = 5;
                    paramLC.MpIsIncludeMsLevel1 = true;
                    paramLC.MpIsUseMs1LevelForQuant = false;
                    paramLC.MpIsFocusedSpotOutput = true;
                    paramLC.MpIsReferenceBaseOutput = true;
                    paramLC.MpIsExportOtherCandidates = false;
                    paramLC.MpIdentificationScoreCutOff = 80;
                }

                MpMs1Tolerance = paramLC.MpMs1Tolerance;
                MpMs2Tolerance = paramLC.MpMs2Tolerance;
                MpRtTolerance = paramLC.MpRtTolerance;
                MpTopNoutput = paramLC.MpTopN;
                IsIncludeMsLevel1 = paramLC.MpIsIncludeMsLevel1;
                IsUseMs1LevelForQuant = paramLC.MpIsUseMs1LevelForQuant;
                IsFocusedSpotOutput = paramLC.MpIsFocusedSpotOutput;
                IsReferenceBaseOutput = paramLC.MpIsReferenceBaseOutput;
                IsExportOtherCandidates = paramLC.MpIsExportOtherCandidates;
                if (paramLC.MpIdentificationScoreCutOff <= 0) {
                    paramLC.MpIdentificationScoreCutOff = 80;
                }
                IdentificationScoreCutOff = paramLC.MpIdentificationScoreCutOff;
            }
            else {
                if (paramGC.MpMs1Tolerance == 0) {
                    paramGC.MpMs1Tolerance = 0.2F;
                    paramGC.MpMs2Tolerance = 0.2F;
                    paramGC.MpRtTolerance = 0.5F;
                    paramGC.MpTopN = 5;
                    paramGC.MpIsIncludeMsLevel1 = true;
                    paramGC.MpIsUseMs1LevelForQuant = true;
                    paramGC.MpIsFocusedSpotOutput = false;
                    paramGC.MpIsReferenceBaseOutput = true;
                    paramGC.MpIsExportOtherCandidates = false;
                    paramGC.MpIdentificationScoreCutOff = 80;
                }

                MpMs1Tolerance = paramGC.MpMs1Tolerance;
                MpMs2Tolerance = paramGC.MpMs2Tolerance;
                MpRtTolerance = paramGC.MpRtTolerance;
                MpTopNoutput = paramGC.MpTopN;
                IsIncludeMsLevel1 = paramGC.MpIsIncludeMsLevel1;
                IsUseMs1LevelForQuant = paramGC.MpIsUseMs1LevelForQuant;
                IsFocusedSpotOutput = paramGC.MpIsFocusedSpotOutput;
                IsReferenceBaseOutput = paramGC.MpIsReferenceBaseOutput;
                IsExportOtherCandidates = paramGC.MpIsExportOtherCandidates;
                if (paramGC.MpIdentificationScoreCutOff <= 0) {
                    paramGC.MpIdentificationScoreCutOff = 80;
                }
                IdentificationScoreCutOff = paramGC.MpIdentificationScoreCutOff;
            }
        }

        private void copyVMToParam()
        {
            if (this.ionization == Ionization.ESI) {
                paramLC.MpMs1Tolerance = this.mpMs1Tolerance;
                paramLC.MpMs2Tolerance = this.mpMs2Tolerance;
                paramLC.MpRtTolerance = this.mpRtTolerance;
                paramLC.MpTopN = this.mpTopN;
                paramLC.MpIsIncludeMsLevel1 = this.mpIsIncludeMsLevel1;
                paramLC.MpIsUseMs1LevelForQuant = this.mpIsUseMs1LevelForQuant;
                paramLC.MpIsFocusedSpotOutput = this.mpIsFocusedSpotOutput;
                paramLC.MpIsReferenceBaseOutput = this.mpIsReferenceBaseOutput;
                paramLC.MpIsExportOtherCandidates = this.isExportOtherCandidates;
                paramLC.MpIdentificationScoreCutOff = this.identificationScoreCutOff;
            }
            else {
                paramGC.MpMs1Tolerance = this.mpMs1Tolerance;
                paramGC.MpMs2Tolerance = this.mpMs2Tolerance;
                paramGC.MpRtTolerance = this.mpRtTolerance;
                paramGC.MpTopN = this.mpTopN;
                paramGC.MpIsIncludeMsLevel1 = this.mpIsIncludeMsLevel1;
                paramGC.MpIsUseMs1LevelForQuant = this.mpIsUseMs1LevelForQuant;
                paramGC.MpIsFocusedSpotOutput = this.mpIsFocusedSpotOutput;
                paramGC.MpIsReferenceBaseOutput = this.mpIsReferenceBaseOutput;
                paramGC.MpIsExportOtherCandidates = this.isExportOtherCandidates;
                paramGC.MpIdentificationScoreCutOff = this.identificationScoreCutOff;
            }
        }

        /// <summary>
        /// Opens the folder selection dialog and sets the value to the ExportFolderPath field
        /// </summary>
        private DelegateCommand selectDestinationFolder;
        public DelegateCommand SelectDestinationFolder
        {
            get
            {
                return selectDestinationFolder ?? (selectDestinationFolder = new DelegateCommand(ShowFolderSelectionDialog, arguments => { return true; }));
            }
        }

        /// <summary>
        /// actual action for the SelectExportFolder command
        /// </summary>
        /// <param name="obj"></param>
        private void ShowFolderSelectionDialog(object obj)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.RootFolder = Environment.SpecialFolder.Desktop;
            fbd.Description = "Choose a folder where to save the exported files.";
            fbd.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                ExportFolderPath = fbd.SelectedPath;
            }
            ExportMrmprobsReference.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Closes the window (on Cancel)
        /// </summary>
        private DelegateCommand closeExportWindow;
        public DelegateCommand CloseExportWindow
        {
            get
            {
                return closeExportWindow ?? (closeExportWindow = new DelegateCommand(obj => {
                    Window view = (Window)obj;
                    view.Close();
                }, obj => { return true; }));
            }
        }

        /// <summary>
        /// Saves the Peak list and closes the window
        /// </summary>
        private DelegateCommand exportMrmprobsReference;
        public DelegateCommand ExportMrmprobsReference
        {
            get
            {
                return exportMrmprobsReference ?? (exportMrmprobsReference = new DelegateCommand(winobj => {

                    copyVMToParam();

                    var view = (MrmprobsExportWin)winobj;
                    var mainWindow = (MainWindow)view.Owner;

                    var dt = DateTime.Now;
                    var timeString = dt.Year.ToString() + dt.Month.ToString() + dt.Day.ToString() + dt.Hour.ToString() + dt.Minute.ToString() + dt.Second.ToString();
                    var exportFilePath = this.exportFolderPath + "\\" + "Mrmprobs-Ref-" + timeString + ".txt";

                    Mouse.OverrideCursor = Cursors.Wait;
                    if (this.isAlignmentView) {
                        #region
                        var focusedAlignmentFileID = mainWindow.FocusedAlignmentFileID;
                        var focusedAlignmentMs1DecID = mainWindow.FocusedAlignmentMs1DecID;
                        var focusedAlignmentSpotID = mainWindow.FocusedAlignmentPeakID;
                        var alignmentSpots = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection;
                        var alignmentViewDecFS = mainWindow.AlignViewDecFS;
                        var alignmentSeekPoints = mainWindow.AlignViewDecSeekPoints;
                        var ms1DecResults = mainWindow.FocusedAlignmentMS1DecResults;
                        var ms2DecResult = mainWindow.AlignViewMS2DecResult;
                        var mspDB = mainWindow.MspDB;
                        var projectProp = mainWindow.ProjectProperty;

                        var rtTolerance = Math.Round(MpRtTolerance, 4);
                        var ms1Tolerance = Math.Round(MpMs1Tolerance, 6);
                        var ms2Tolerance = Math.Round(MpMs2Tolerance, 6);

                        if (this.ionization == Ionization.ESI) {
                            if (this.paramLC.MpIsReferenceBaseOutput) {
                                if (this.paramLC.MpIsFocusedSpotOutput) {
                                    DataExportLcUtility.ExportReferenceMsmsAsMrmprobsFormat(exportFilePath, alignmentViewDecFS, alignmentSeekPoints, alignmentSpots[focusedAlignmentSpotID], mspDB, paramLC, projectProp);
                                }
                                else {
                                    DataExportLcUtility.ExportReferenceMsmsAsMrmprobsFormat(exportFilePath, alignmentViewDecFS, alignmentSeekPoints, alignmentSpots, mspDB, paramLC, projectProp);
                                }
                            }
                            else {
                                if (this.paramLC.MpIsFocusedSpotOutput) {
                                    DataExportLcUtility.ExportExperimentalMsmsAsMrmprobsFormat(exportFilePath, ms2DecResult, alignmentSpots[focusedAlignmentSpotID], rtTolerance
                                        , ms1Tolerance, ms2Tolerance, MpTopNoutput, IsIncludeMsLevel1, IsUseMs1LevelForQuant);
                                }
                                else {
                                    DataExportLcUtility.ExportExperimentalMsmsAsMrmprobsFormat(exportFilePath, alignmentSpots, alignmentViewDecFS, alignmentSeekPoints, rtTolerance
                                        , ms1Tolerance, ms2Tolerance, MpTopNoutput, IsIncludeMsLevel1, IsUseMs1LevelForQuant);
                                }
                            }
                        }
                        else {
                            if (this.paramGC.MpIsReferenceBaseOutput) {
                                if (this.paramGC.MpIsFocusedSpotOutput) {
                                    DataExportGcUtility.ExportSpectraAsMrmprobsFormat(exportFilePath, ms1DecResults, focusedAlignmentMs1DecID, rtTolerance, ms1Tolerance
                                        , mspDB, MpTopNoutput, true);
                                }
                                else {
                                    DataExportGcUtility.ExportSpectraAsMrmprobsFormat(exportFilePath, ms1DecResults, -1, rtTolerance, ms1Tolerance
                                        , mspDB, MpTopNoutput, true);
                                }
                            }
                            else {
                                if (this.paramGC.MpIsFocusedSpotOutput) {
                                    DataExportGcUtility.ExportSpectraAsMrmprobsFormat(exportFilePath, ms1DecResults, focusedAlignmentMs1DecID, rtTolerance, ms1Tolerance
                                        , mspDB, MpTopNoutput, false);
                                }
                                else {
                                    DataExportGcUtility.ExportSpectraAsMrmprobsFormat(exportFilePath, ms1DecResults, -1, rtTolerance, ms1Tolerance
                                        , mspDB, MpTopNoutput, false);
                                }
                            }
                        }
                        #endregion
                    }
                    else {
                        #region
                        var focusedFileID = mainWindow.FocusedFileID;
                        var focusedMs1DecID = mainWindow.FocusedMS1DecID;
                        var focusedSpotID = mainWindow.FocusedPeakID;
                        var peakSpots = mainWindow.AnalysisFiles[focusedFileID].PeakAreaBeanCollection;
                        var peakViewDecFS = mainWindow.PeakViewDecFS;
                        var peakViewSeekPoints = mainWindow.PeakViewDecSeekPoints;
                        var ms1DecResults = mainWindow.Ms1DecResults;
                        var ms2DecResult = mainWindow.PeakViewMS2DecResult;
                        var mspDB = mainWindow.MspDB;
                        var projectProp = mainWindow.ProjectProperty;

                        var rtTolerance = Math.Round(MpRtTolerance, 4);
                        var ms1Tolerance = Math.Round(MpMs1Tolerance, 6);
                        var ms2Tolerance = Math.Round(MpMs2Tolerance, 6);

                        if (this.ionization == Ionization.ESI) {
                            if (this.paramLC.MpIsReferenceBaseOutput) {
                                if (this.paramLC.MpIsFocusedSpotOutput) {
                                    DataExportLcUtility.ExportReferenceMsmsAsMrmprobsFormat(exportFilePath, peakViewDecFS, peakViewSeekPoints, peakSpots[focusedSpotID], mspDB, paramLC, projectProp);
                                }
                                else {
                                    DataExportLcUtility.ExportReferenceMsmsAsMrmprobsFormat(exportFilePath, peakViewDecFS, peakViewSeekPoints, peakSpots, mspDB, paramLC, projectProp);
                                }
                            }
                            else {
                                if (this.paramLC.MpIsFocusedSpotOutput) {
                                    DataExportLcUtility.ExportExperimentalMsmsAsMrmprobsFormat(exportFilePath, ms2DecResult, peakSpots[focusedSpotID], rtTolerance
                                        , ms1Tolerance, ms2Tolerance, MpTopNoutput, IsIncludeMsLevel1, IsUseMs1LevelForQuant);
                                }
                                else {
                                    DataExportLcUtility.ExportExperimentalMsmsAsMrmprobsFormat(exportFilePath, peakSpots, peakViewDecFS, peakViewSeekPoints, rtTolerance
                                        , ms1Tolerance, ms2Tolerance, MpTopNoutput, IsIncludeMsLevel1, IsUseMs1LevelForQuant);
                                }
                            }
                        }
                        else {
                            if (this.paramGC.MpIsReferenceBaseOutput) {
                                if (this.paramGC.MpIsFocusedSpotOutput) {
                                    DataExportGcUtility.ExportSpectraAsMrmprobsFormat(exportFilePath, ms1DecResults, focusedMs1DecID, rtTolerance, ms1Tolerance
                                        , mspDB, MpTopNoutput, true);
                                }
                                else {
                                    DataExportGcUtility.ExportSpectraAsMrmprobsFormat(exportFilePath, ms1DecResults, -1, rtTolerance, ms1Tolerance
                                        , mspDB, MpTopNoutput, true);
                                }
                            }
                            else {
                                if (this.paramGC.MpIsFocusedSpotOutput) {
                                    DataExportGcUtility.ExportSpectraAsMrmprobsFormat(exportFilePath, ms1DecResults, focusedMs1DecID, rtTolerance, ms1Tolerance
                                        , mspDB, MpTopNoutput, false);
                                }
                                else {
                                    DataExportGcUtility.ExportSpectraAsMrmprobsFormat(exportFilePath, ms1DecResults, -1, rtTolerance, ms1Tolerance
                                        , mspDB, MpTopNoutput, false);
                                }
                            }
                        }
                        #endregion
                    }

                    Mouse.OverrideCursor = null;
                    view.Close();
                }, CanExportMrmprobsRef));
            }
        }

    

        /// <summary>
        /// Checks whether the exportMrmprobsRef command can be executed or not
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private bool CanExportMrmprobsRef(object arg)
        {
            if (this.HasViewError) return false;
            else return true;
        }
        #endregion
    }
}
