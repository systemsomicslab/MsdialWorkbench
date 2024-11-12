using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using GongSolutions.Wpf.DragDrop;
#if !DEBUG_VENDOR_UNSUPPORTED && !RELEASE_VENDOR_UNSUPPORTED
using CompMs.RawDataHandler.Abf;
#endif

namespace Rfx.Riken.OsakaUniv
{
    public class AnalysisFilePropertySettingVM : ViewModelBase
    {
        private MainWindow mainWindow;
        private Window window;
        private ObservableCollection<AnalysisFilePropertyVM> analysisFilePropertyCollection;

        public AnalysisFilePropertySettingVM(MainWindow mainWindow, Window window) 
        { 
            AnalysisFilePropertyCollection = new ObservableCollection<AnalysisFilePropertyVM>();
            this.mainWindow = mainWindow;
            this.window = window;
        }

        #region // Properties
        [CustomValidation(typeof(AnalysisFilePropertySettingVM), "ValidateAnalysisFile")]
        public ObservableCollection<AnalysisFilePropertyVM> AnalysisFilePropertyCollection
        {
            get { return analysisFilePropertyCollection; }
            set { if (analysisFilePropertyCollection == value) return; analysisFilePropertyCollection = value; OnPropertyChanged("AnalysisFilePropertyCollection"); Next.RaiseCanExecuteChanged(); }
        }

        public static ValidationResult ValidateAnalysisFile(ObservableCollection<AnalysisFilePropertyVM> test, ValidationContext context)
        {
            if (test == null || test.Count == 0) return new ValidationResult("Choose the analysis files (abf, mzML, or netCDF format).");
            else return ValidationResult.Success;
        }
        #endregion


        private DelegateCommand next;

        public DelegateCommand Next
        {
            get
            {
                return next ?? (next = new DelegateCommand(winobj => {
                    var view = (AnalysisFilePropertySetWin)winobj;
                    if (ClosingMethod() == true) {
                        view.DialogResult = true;
                        view.Close();
                    }
                }, CanExecuteOkCommand));
            }
        }

        private bool CanExecuteOkCommand(object arg)
        {
            if (this.analysisFilePropertyCollection == null || this.analysisFilePropertyCollection.Count == 0)
                return false;
            else
                return true;
        }

        private bool ClosingMethod()
        {
            this.mainWindow.AnalysisFiles = new ObservableCollection<AnalysisFileBean>();
            var counter = 0;
            var errorString = string.Empty;
            
            for (int i = 0; i < analysisFilePropertyCollection.Count; i++)
            {
                if (analysisFilePropertyCollection[i].AnalysisFileIncluded)
                {
                    var dt = DateTime.Now;
                    var filename = analysisFilePropertyCollection[i].AnalysisFilePropertyBean.AnalysisFileName;
                    var invalidChars = System.IO.Path.GetInvalidFileNameChars();
                    if (filename.IndexOfAny(invalidChars) >= 0) {
                        errorString = "File name field cannot support \\/:*?\"<>| characters";
                        break;
                    }

                    var rdamID = this.mainWindow.RdamProperty.RdamFilePath_RdamFileID[analysisFilePropertyCollection[i].AnalysisFilePath];
                    var rdamFileContentBean = this.mainWindow.RdamProperty.RdamFileContentBeanCollection[rdamID];

                    rdamFileContentBean.FileID_MeasurementID[counter] = rdamFileContentBean.FileID_MeasurementID[i];
                    rdamFileContentBean.MeasurementID_FileID[rdamFileContentBean.FileID_MeasurementID[counter]] = counter;

                    if (mainWindow.ProjectProperty.CheckAIF) {
                        var decPathList = new List<string>();
                        var ms2LevelIdList = new List<int>();
                        foreach (var value in mainWindow.ProjectProperty.ExperimentID_AnalystExperimentInformationBean) { if (value.Value.CheckDecTarget == 1) { ms2LevelIdList.Add(value.Key); } }
                        for (var j = 0; j < ms2LevelIdList.Count; j++) {
                            var tmpPath = System.IO.Path.GetDirectoryName(analysisFilePropertyCollection[i].AnalysisFilePropertyBean.AnalysisFilePath) + "\\" + analysisFilePropertyCollection[i].AnalysisFilePropertyBean.AnalysisFileName + "_" + dt.Year.ToString() + dt.Month.ToString() + dt.Day.ToString() + dt.Hour.ToString() + dt.Minute.ToString() + "." + j + "." + SaveFileFormat.dcl;
                            decPathList.Add(tmpPath);
                        }
                        this.mainWindow.AnalysisFiles.Add(new AnalysisFileBean() {
                            AnalysisFilePropertyBean = new AnalysisFilePropertyBean() {
                                AnalysisFileAnalyticalOrder = analysisFilePropertyCollection[i].AnalysisFilePropertyBean.AnalysisFileAnalyticalOrder,
                                AnalysisFileClass = analysisFilePropertyCollection[i].AnalysisFilePropertyBean.AnalysisFileClass,
                                AnalysisFileId = counter, AnalysisFileIncluded = true,
                                AnalysisFileName = analysisFilePropertyCollection[i].AnalysisFilePropertyBean.AnalysisFileName,
                                AnalysisFilePath = analysisFilePropertyCollection[i].AnalysisFilePropertyBean.AnalysisFilePath,
                                AnalysisFileType = analysisFilePropertyCollection[i].AnalysisFilePropertyBean.AnalysisFileType,
                                AnalysisBatch = analysisFilePropertyCollection[i].AnalysisFilePropertyBean.AnalysisBatch,
                                InjectionVolume = analysisFilePropertyCollection[i].AnalysisFilePropertyBean.InjectionVolume,
                                DeconvolutionFilePath = decPathList[0],
                                DeconvolutionFilePathList = decPathList,
                                PeakAreaBeanInformationFilePath = System.IO.Path.GetDirectoryName(analysisFilePropertyCollection[i].AnalysisFilePropertyBean.AnalysisFilePath) + "\\" + analysisFilePropertyCollection[i].AnalysisFilePropertyBean.AnalysisFileName + "_" + dt.Year.ToString() + dt.Month.ToString() + dt.Day.ToString() + dt.Hour.ToString() + dt.Minute.ToString() + "." + SaveFileFormat.pai
                            }
                        });
                    }
                    else {
                        this.mainWindow.AnalysisFiles.Add(new AnalysisFileBean() {
                            AnalysisFilePropertyBean = new AnalysisFilePropertyBean() {
                                AnalysisFileAnalyticalOrder = analysisFilePropertyCollection[i].AnalysisFilePropertyBean.AnalysisFileAnalyticalOrder,
                                AnalysisFileClass = analysisFilePropertyCollection[i].AnalysisFilePropertyBean.AnalysisFileClass,
                                AnalysisFileId = counter, AnalysisFileIncluded = true,
                                AnalysisFileName = analysisFilePropertyCollection[i].AnalysisFilePropertyBean.AnalysisFileName,
                                AnalysisFilePath = analysisFilePropertyCollection[i].AnalysisFilePropertyBean.AnalysisFilePath,
                                AnalysisFileType = analysisFilePropertyCollection[i].AnalysisFilePropertyBean.AnalysisFileType,
                                AnalysisBatch = analysisFilePropertyCollection[i].AnalysisFilePropertyBean.AnalysisBatch,
                                InjectionVolume = analysisFilePropertyCollection[i].AnalysisFilePropertyBean.InjectionVolume,
                                DeconvolutionFilePath = System.IO.Path.GetDirectoryName(analysisFilePropertyCollection[i].AnalysisFilePropertyBean.AnalysisFilePath) + "\\" + analysisFilePropertyCollection[i].AnalysisFilePropertyBean.AnalysisFileName + "_" + dt.Year.ToString() + dt.Month.ToString() + dt.Day.ToString() + dt.Hour.ToString() + dt.Minute.ToString() + "." + SaveFileFormat.dcl,
                                PeakAreaBeanInformationFilePath = System.IO.Path.GetDirectoryName(analysisFilePropertyCollection[i].AnalysisFilePropertyBean.AnalysisFilePath) + "\\" + analysisFilePropertyCollection[i].AnalysisFilePropertyBean.AnalysisFileName + "_" + dt.Year.ToString() + dt.Month.ToString() + dt.Day.ToString() + dt.Hour.ToString() + dt.Minute.ToString() + "." + SaveFileFormat.pai
                            }
                        }); 
                     }
                    this.mainWindow.ProjectProperty.FileID_RdamID[counter] = rdamID;
                    this.mainWindow.ProjectProperty.FileID_ClassName[counter] = analysisFilePropertyCollection[i].AnalysisFilePropertyBean.AnalysisFileClass;
                    this.mainWindow.ProjectProperty.FileID_AnalysisFileType[counter] = analysisFilePropertyCollection[i].AnalysisFilePropertyBean.AnalysisFileType;

                    counter++;
                }
            }

            if (this.mainWindow.AnalysisFiles.Count == 0) {
                MessageBox.Show("Select at least one analysis file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (errorString != string.Empty) {
                MessageBox.Show(errorString, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            // file class count except for qc, standard, and blank
            var classToCount = new Dictionary<string, int>();
            foreach (var file in this.mainWindow.AnalysisFiles) {
                var prop = file.AnalysisFilePropertyBean;
                var type = prop.AnalysisFileType;
                if (type == AnalysisFileType.Sample) {
                    var classname = prop.AnalysisFileClass;
                    var fileid = prop.AnalysisFileId;
                    if (!classToCount.ContainsKey(classname))
                        classToCount[classname] = 1;
                    else
                        classToCount[classname]++;
                }
            }
            if (classToCount.Count > 0) {
                var classNumAve = classToCount.Values.Average();
                if (classNumAve > 4) this.mainWindow.ProjectProperty.IsBoxPlotForAlignmentResult = true;
            }

            MsDialStatistics.ClassColorDictionaryInitialization(this.mainWindow.AnalysisFiles, this.mainWindow.ProjectProperty, this.mainWindow.SolidColorBrushList);
            return true;
        }

        public void ReadImportedFiles(string[] filepathes) {
            var rdamProperty = new RdamPropertyBean();
            var analysisFiles = new List<AnalysisFilePropertyBean>();
            var errorMessage = string.Empty;
            if (filepathes == null || filepathes.Length == 0) return;
            filepathes = filepathes.OrderBy(n => n).ToArray();
            for (int i = 0; i < filepathes.Length; i++) {

                var filepath = filepathes[i];
                var filename = System.IO.Path.GetFileNameWithoutExtension(filepath);
                var fileExtension = System.IO.Path.GetExtension(filepath).ToLower();

#if DEBUG_VENDOR_UNSUPPORTED || RELEASE_VENDOR_UNSUPPORTED
                if (fileExtension != ".mzml") {
                    errorMessage += "This program can just accept .mzml files.";
                    break;
                }
#else
                if (fileExtension != ".abf" && fileExtension != ".hmd" && fileExtension != ".mzb" &&
                    fileExtension != ".cdf" && fileExtension != ".mzml" && fileExtension != ".raw" &&
                    fileExtension != ".d" && fileExtension != ".iabf" && fileExtension != ".ibf"
                     && fileExtension != ".wiff" && fileExtension != ".wiff2" && fileExtension != ".qgd" && fileExtension != ".lcd"
                     && fileExtension != ".lrp") {
                    errorMessage += "This program can just accept .abf, .hmd, .mzb, .mzml, .cdf, .d, .ibf, .wiff, .wiff2, .lcd, .qgd, .lrp or .raw files.";
                    break;
                }
#endif

                rdamProperty.RdamFilePath_RdamFileID[filepath] = i;
                rdamProperty.RdamFileID_RdamFilePath[i] = filepath;
                setProperties(filepath, filename, fileExtension, i, analysisFiles.Count, rdamProperty, analysisFiles);
            }

            if (errorMessage != string.Empty) {
                MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var analysisFileVMs = new ObservableCollection<AnalysisFilePropertyVM>();
            foreach (var file in analysisFiles) {
                var fileVM = new AnalysisFilePropertyVM() {
                    AnalysisFilePath = file.AnalysisFilePath,
                    AnalysisFileName = file.AnalysisFileName,
                    AnalysisFileType = file.AnalysisFileType,
                    AnalysisFileClass = file.AnalysisFileClass,
                    AnalysisFileAnalyticalOrder = file.AnalysisFileAnalyticalOrder,
                    AnalysisFileId = file.AnalysisFileId,
                    AnalysisFileIncluded = file.AnalysisFileIncluded,
                    InjectionVolume = file.InjectionVolume
                };
                analysisFileVMs.Add(fileVM);
            }
            AnalysisFilePropertyCollection = analysisFileVMs;
            this.mainWindow.RdamProperty = rdamProperty;
            //return analysisFileVMs;
        }

        private void setProperties(string filepath, string filename, string fileExtension, int i, int counter,
            RdamPropertyBean rdamProperty, List<AnalysisFilePropertyBean> analysisFiles) {
            var error = string.Empty;
            if (fileExtension == ".abf") {
                try {
#if !DEBUG_VENDOR_UNSUPPORTED && !RELEASE_VENDOR_UNSUPPORTED
                    RdamPropertySetting.SetAbfProperties(filepath, filename, fileExtension, i, analysisFiles.Count, rdamProperty, analysisFiles);
#endif
                }
                catch (System.NotSupportedException ex) {
                    error += filename + "\r\n";
                }
            }
            else {

                var rdamFileContentBean = new RdamFileContentBean();

                analysisFiles.Add(
                           new AnalysisFilePropertyBean() {
                               AnalysisFilePath = filepath,
                               AnalysisFileName = filename,
                               AnalysisFileType = AnalysisFileType.Sample,
                               AnalysisFileClass = "1",
                               AnalysisFileAnalyticalOrder = counter + 1,
                               AnalysisFileId = counter,
                               AnalysisFileIncluded = true
                           }
                       );

                rdamFileContentBean.MeasurementID_FileID[0] = counter;
                rdamFileContentBean.FileID_MeasurementID[counter] = 0;
                rdamFileContentBean.MeasurementNumber = 1;
                rdamFileContentBean.RdamFileID = i;
                rdamFileContentBean.RdamFileName = filename;
                rdamFileContentBean.RdamFilePath = filepath;

                rdamProperty.RdamFileContentBeanCollection.Add(rdamFileContentBean);
            }

            if (error != string.Empty) {
                error += "The above ABF files are not supported anymore, so please use new file converter to be used in MS-DIAL";
                MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        #region // Required Methods for IDropTarget
        //public void DragOver(IDropInfo dropInfo) {
        //    var dragFileList = ((DataObject)dropInfo.Data).GetFileDropList().Cast<string>();
        //    dropInfo.Effects = dragFileList.Any(_ => {
        //        return IsAccepted(_);
        //    }) ? DragDropEffects.Copy : DragDropEffects.None;
        //}

        //public void Drop(IDropInfo dropInfo) {
        //    var dragFileList = ((DataObject)dropInfo.Data).GetFileDropList().Cast<string>();
        //    dropInfo.Effects = dragFileList.Any(_ => {
        //        return IsAccepted(_);
        //    }) ? DragDropEffects.Copy : DragDropEffects.None;

        //    var filepathes = new List<string>();
        //    foreach (var file in dragFileList) {
        //        if (IsAccepted(file)) {
        //            filepathes.Add(file);
        //        }
        //    }

        //    if (filepathes.Count > 0)
        //        ReadImportedFiles(filepathes.ToArray());
        //}

        //private bool IsAccepted(string data) {
        //    var extension = Path.GetExtension(data).ToLower();
        //    if (extension != ".abf" && extension != ".mzml" && extension != ".cdf" && extension != ".raw")
        //        return false;
        //    else
        //        return true;
        //}
        #endregion
    }
}
