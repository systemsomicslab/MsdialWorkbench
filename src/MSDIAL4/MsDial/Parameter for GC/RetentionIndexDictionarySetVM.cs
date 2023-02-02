using Microsoft.Win32;
using Msdial.Gcms.Dataprocess.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv
{
    public class RetentionIndexDictionarySetVM : ViewModelBase
    {
        private ObservableCollection<AnalysisFilePropertyVM> analysisFileVMs;
        private AnalysisParamOfMsdialGcms param;
        //private DataGrid datagrid;

        public ObservableCollection<AnalysisFilePropertyVM> AnalysisFileVMs
        {
            get { return analysisFileVMs; }
            set { analysisFileVMs = value; OnPropertyChanged("AnalysisFileVMs"); }
        }

        public AnalysisParamOfMsdialGcms Param
        {
            get { return param; }
            set { param = value; }
        }

        /// <summary>
        /// Sets up the view model for the Ri dictionary setting window in InvokeCommandAction
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
            var view = (RetentionIndexDictionarySetWin)obj;
            var paramSetWin = (AnalysisParamSetForGcWin)view.Owner;
            var mainWindow = (MainWindow)paramSetWin.Owner;

            var files = mainWindow.AnalysisFiles;
            AnalysisFileVMs = new ObservableCollection<AnalysisFilePropertyVM>();
            foreach (var file in files) { AnalysisFileVMs.Add(new AnalysisFilePropertyVM(file.AnalysisFilePropertyBean)); }
            
            Param = mainWindow.AnalysisParamForGC;
        }

        /// <summary>
        /// Closes the window (on Cancel)
        /// </summary>
        private DelegateCommand closeWindow;
        public DelegateCommand CloseWindow
        {
            get
            {
                return closeWindow ?? (closeWindow = new DelegateCommand(obj => {
                    Window view = (Window)obj;
                    view.Close();
                }, obj => { return true; }));
            }
        }

        /// <summary>
        /// Set Ri dictionary
        /// </summary>
        private DelegateCommand dictionarySet;
        public DelegateCommand DictionarySet
        {
            get 
            { 
                return dictionarySet ?? (dictionarySet = new DelegateCommand(executeDictionarySet, canExcecuteDictionarySet)); 
            }
        }

        private void executeDictionarySet(object obj)
        {
            var view = (RetentionIndexDictionarySetWin)obj;
            var paramSetWin = (AnalysisParamSetForGcWin)view.Owner;
            var error = string.Empty;

            foreach (var file in this.analysisFileVMs) {
                var riFilePath = file.RiDictionaryFilePath;
                var fileName = file.AnalysisFileName;
                if (riFilePath == null || riFilePath == string.Empty) {
                    error += fileName + "\r\n";
                }
                else if (!System.IO.File.Exists(riFilePath)) {
                    error += fileName + "\r\n";
                }
            }

            if (error != string.Empty) {
                error += "\r\n" + "The RI dictionary file of the above files is not set correctly. Set your RI dictionary file for all imported files";
                MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var flgIncorrectFormat = false;
            var flgIncorrectFiehnFormat = false;
            var flgIncorrectOrdering = false;
            this.param.FileIdRiInfoDictionary = new Dictionary<int, RiDictionaryInfo>();
            foreach (var file in this.analysisFileVMs) {
                this.param.FileIdRiInfoDictionary[file.AnalysisFileId] = new RiDictionaryInfo() {
                     DictionaryFilePath = file.RiDictionaryFilePath,
                     RiDictionary = DatabaseGcUtility.GetRiDictionary(file.RiDictionaryFilePath)
                };
                
                var dictionary = this.param.FileIdRiInfoDictionary[file.AnalysisFileId].RiDictionary;
                if (dictionary == null || dictionary.Count == 0) {
                    flgIncorrectFormat = true;
                    break;
                }

                if (this.param.RiCompoundType == RiCompoundType.Fames) {
                    if (!isFamesContanesMatch(dictionary)) {
                        flgIncorrectFiehnFormat = true;
                        break;
                    }
                }

                if (!isSequentialCarbonRtOrdering(dictionary)) {
                    flgIncorrectOrdering = true;
                    break;
                }
            }

            if (flgIncorrectFormat == true) {
                
                var text = "Invalid RI information. Please confirm your file and prepare the following information.\r\n";
                text += "Carbon number\tRT(min)\r\n";
                text += "10\t4.72\r\n";
                text += "11\t5.63\r\n";
                text += "12\t6.81\r\n";
                text += "13\t8.08\r\n";
                text += "14\t9.12\r\n";
                text += "15\t10.33\r\n";
                text += "16\t11.91\r\n";
                text += "18\t14.01\r\n";
                text += "20\t16.15\r\n";
                text += "22\t18.28\r\n";
                text += "24\t20.33\r\n";
                text += "26\t22.17\r\n";
                text += "\r\n";
                text += "This information should be required for RI calculation.";

                MessageBox.Show(text, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (flgIncorrectOrdering == true) {

                var text = "Invalid carbon-rt sequence: incorrect ordering of retention times.\r\n";
                text += "Carbon number\tRT(min)\r\n";
                text += "10\t4.72\r\n";
                text += "11\t5.63\r\n";
                text += "12\t6.81\r\n";
                text += "13\t8.08\r\n";
                text += "14\t9.12\r\n";
                text += "15\t10.33\r\n";
                text += "16\t11.91\r\n";
                text += "18\t14.01\r\n";
                text += "20\t16.15\r\n";
                text += "22\t18.28\r\n";
                text += "24\t20.33\r\n";
                text += "26\t22.17\r\n";
                text += "\r\n";
                text += "This information should be required for RI calculation.";

                MessageBox.Show(text, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (flgIncorrectFiehnFormat == true) {
                var text = "If you use the FAMEs RI, you have to decide the retention times as minute for \r\n"
                            + "C8, C9, C10, C12, C14, C16, C18, C20, C22, C24, C26, C28, C30.";

                MessageBox.Show(text, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            ((AnalysisParamSetForGcVM)paramSetWin.DataContext).Param.FileIdRiInfoDictionary = this.param.FileIdRiInfoDictionary;
            paramSetWin.Label_RiDictionaryFilePath.Content = "Status: imported";
            view.Close();
        }

        private bool isSequentialCarbonRtOrdering(Dictionary<int, float> dictionary) {
            var carbonRts = new List<double[]>();
            foreach (var pair in dictionary) {
                carbonRts.Add(new double[] { pair.Key, pair.Value });
            }
            carbonRts = carbonRts.OrderBy(n => n[0]).ToList();
            for (int i = 0; i < carbonRts.Count -1; i++) {
                if (carbonRts[i][1] >= carbonRts[i + 1][1]) {
                    return false;
                }
            }
            return true;
        }

        private bool isFamesContanesMatch(Dictionary<int, float> riDictionary)
        {
            var fiehnFamesDictionary = MspFileParcer.GetFiehnFamesDictionary();

            if (fiehnFamesDictionary.Count != riDictionary.Count) return false;
            foreach (var fFame in fiehnFamesDictionary) {
                var fiehnCnumber = fFame.Key;
                var flg = false;
                foreach (var dict in riDictionary) {
                    if (fiehnCnumber == dict.Key) {
                        flg = true;
                        break;
                    }
                }
                if (flg == false) return false;
            }
            return true;
        }

        private bool canExcecuteDictionarySet(object arg)
        {
            return true;
            
        }

        /// <summary>
        /// Open file dialog
        /// </summary>
        private DelegateCommand openRiFileDialog;
        public DelegateCommand OpenRiFileDialog
        {
            get
            {
                return openRiFileDialog ?? (openRiFileDialog = new DelegateCommand(TextFile_Browse, obj => { return true; }));
            }
        }

        private void TextFile_Browse(object obj)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Text file(*.txt)|*.txt;";
            ofd.Title = "Import a library file for RI calculation";
            ofd.RestoreDirectory = true;
            ofd.Multiselect = false;

            if (ofd.ShowDialog() == true) {
                var prop = (AnalysisFilePropertyBean)obj;
                foreach (var fileVM in analysisFileVMs.Where(n => n.AnalysisFileId == prop.AnalysisFileId))
                    fileVM.RiDictionaryFilePath = ofd.FileName;
            }
        }
    }
}
