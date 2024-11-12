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
using System.Windows.Resources;

namespace Rfx.Riken.OsakaUniv
{
    public class StartUpProjectVM : ViewModelBase
    {
        private MainWindow mainWindow;
        private Window window;
        private ProjectPropertyBean projectProperty;
        private IupacReferenceBean iupac;

        public StartUpProjectVM(MainWindow mainWindow, Window window) 
        {
            this.mainWindow = mainWindow;
            this.window = window; 
            this.projectProperty = mainWindow.ProjectProperty;
            this.iupac = mainWindow.IupacReference;
        }

        #region// properties
        public string ProjectFolderPath
        {
            get { return this.projectProperty.ProjectFolderPath; }
            set { this.projectProperty.ProjectFolderPath = value; }
        }

        [Required(ErrorMessage = "Choose a project folder path.")]
        public string ProjectFilePath
        {
            get { return this.projectProperty.ProjectFilePath; }
            set { if (this.projectProperty.ProjectFilePath == value) return; this.projectProperty.ProjectFilePath = value; OnPropertyChanged("ProjectFilePath"); Next.RaiseCanExecuteChanged(); }
        }

        public string ExperimentFilePath
        {
            get { return this.projectProperty.ExperimentFilePath; }
            set { if (this.projectProperty.ExperimentFilePath == value) return; this.projectProperty.ExperimentFilePath = value; OnPropertyChanged("ExperimentFilePath"); }
        }

        public Ionization Ionization
        {
            get { return this.projectProperty.Ionization; }
            set { if (this.projectProperty.Ionization == value) return; this.projectProperty.Ionization = value; OnPropertyChanged("Ionization"); }
        }

        public SeparationType SeparationType {
            get { return this.projectProperty.SeparationType; }
            set { if (this.projectProperty.SeparationType == value) return; this.projectProperty.SeparationType = value; OnPropertyChanged("SeparationType"); }
        }

        public MethodType MethodType
        {
            get { return this.projectProperty.MethodType; }
            set { if (this.projectProperty.MethodType == value) return; this.projectProperty.MethodType = value; OnPropertyChanged("MethodType"); OnPropertyChanged("ExperimentFilePath"); }
        }

        public DataType DataType
        {
            get { return this.projectProperty.DataType; }
            set { this.projectProperty.DataType = value; }
        }

        public DataType DataTypeMS2
        {
            get { return this.projectProperty.DataTypeMS2; }
            set { this.projectProperty.DataTypeMS2 = value; }
        }

        public IonMode IonMode
        {
            get { return this.projectProperty.IonMode; }
            set { this.projectProperty.IonMode = value; }
        }

        public TargetOmics TargetOmics
        {
            get { return this.projectProperty.TargetOmics; }
            set { this.projectProperty.TargetOmics = value; }
        }

        public bool CheckAIF {
            get { return this.projectProperty.CheckAIF; }
            set { if (this.projectProperty.CheckAIF == value) return; this.projectProperty.CheckAIF = value; OnPropertyChanged("CheckAIF"); }
        }

        public string InstrumentType
        {
            get { return this.projectProperty.InstrumentType; }
            set { this.projectProperty.InstrumentType = value; }
        }

        public string Instrument
        {
            get { return this.projectProperty.Instrument; }
            set { this.projectProperty.Instrument = value; }
        }

        public string Authors
        {
            get { return this.projectProperty.Authors; }
            set { this.projectProperty.Authors = value; }
        }

        public string License
        {
            get { return this.projectProperty.License; }
            set { this.projectProperty.License = value; }
        }

        public string CollisionEnergy
        {
            get { return this.projectProperty.CollisionEnergy; }
            set { this.projectProperty.CollisionEnergy = value; }
        }

        public string Comment
        {
            get { return this.projectProperty.Comment; }
            set { this.projectProperty.Comment = value; }
        }

        #endregion

        private bool ClosingMethod()
        {
            if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(this.projectProperty.ProjectFilePath))) {
                MessageBox.Show("Your project folder is not existed. Browse a folder containing ABF, mzML, or netCDF files.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            var projectFilePath = this.projectProperty.ProjectFilePath;
            this.projectProperty.ProjectFolderPath = System.IO.Path.GetDirectoryName(projectFilePath);

            if (this.projectProperty.Ionization == OsakaUniv.Ionization.ESI)
            {
                if (this.projectProperty.MethodType == MethodType.diMSMS && this.projectProperty.SeparationType == SeparationType.Chromatography)
                    if (analystExperimentFileCheck(this.projectProperty.ExperimentFilePath) == false) { return false; }

                if (this.projectProperty.TargetOmics == TargetOmics.Lipidomics)
                {
                    string mainDirectory = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
                    if (System.IO.Directory.GetFiles(mainDirectory, "*." + SaveFileFormat.lbm + "*", 
                        System.IO.SearchOption.TopDirectoryOnly).Length != 1)
                    {
                        MessageBox.Show("There is no LBM file or several LBM files are existed in this application folder. Please see the tutorial.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
            }

            //Get IUPAC reference
            this.mainWindow.IupacReference = IupacResourceParser.GetIupacReferenceBean();
            //IupacResourceParser.SetIupacReferenceBean(this.iupac);

            if (this.projectProperty.Comment != null && this.projectProperty.Comment != string.Empty)
                this.projectProperty.Comment = this.projectProperty.Comment.Replace("\r", "").Replace("\n", " ");

            return true;
        }

        //private void setIupacReferenceBean(IupacReferenceBean iupacReferenceBean)
        //{
        //    Uri fileUri = new Uri("/Resources/IUPAC.txt", UriKind.Relative);
        //    StreamResourceInfo info = Application.GetResourceStream(fileUri);

        //    int iupacID = 0;
        //    string elementName = "";
        //    string line;
        //    string[] lineArray;

        //    List<IupacElementPropertyBean> iupacElementPropertyBeanList = new List<IupacElementPropertyBean>();
        //    IupacElementPropertyBean iupacElementPropertyBean = new IupacElementPropertyBean();

        //    using (StreamReader sr = new StreamReader(info.Stream))
        //    {
        //        sr.ReadLine();
        //        while (sr.Peek() > -1)
        //        {
        //            line = sr.ReadLine();
        //            if (line == string.Empty) break;

        //            lineArray = line.Split('\t');

        //            if (iupacID != int.Parse(lineArray[0]))
        //            {
        //                if (iupacID != 0) { iupacReferenceBean.IupacID_IupacElementPropertyBeanList[iupacID] = iupacElementPropertyBeanList; iupacReferenceBean.ElementName_IupacElementPropertyBeanList[elementName] = iupacElementPropertyBeanList; }

        //                iupacElementPropertyBeanList = new List<IupacElementPropertyBean>();
        //                iupacID = int.Parse(lineArray[0]);
        //                elementName = lineArray[1];

        //                iupacElementPropertyBean = new IupacElementPropertyBean();
        //                iupacElementPropertyBean.AccurateMass = double.Parse(lineArray[4]);
        //                iupacElementPropertyBean.ElementName = elementName;
        //                iupacElementPropertyBean.IupacID = iupacID;
        //                iupacElementPropertyBean.NaturalRelativeAbundance = double.Parse(lineArray[3]);
        //                iupacElementPropertyBean.NominalMass = int.Parse(lineArray[2]);

        //                iupacElementPropertyBeanList.Add(iupacElementPropertyBean);
        //            }
        //            else
        //            {
        //                iupacElementPropertyBean = new IupacElementPropertyBean();
        //                iupacElementPropertyBean.AccurateMass = double.Parse(lineArray[4]);
        //                iupacElementPropertyBean.ElementName = elementName;
        //                iupacElementPropertyBean.IupacID = iupacID;
        //                iupacElementPropertyBean.NaturalRelativeAbundance = double.Parse(lineArray[3]);
        //                iupacElementPropertyBean.NominalMass = int.Parse(lineArray[2]);

        //                iupacElementPropertyBeanList.Add(iupacElementPropertyBean);
        //            }
        //        }
        //        //reminder
        //        iupacReferenceBean.IupacID_IupacElementPropertyBeanList[iupacID] = iupacElementPropertyBeanList;
        //        iupacReferenceBean.ElementName_IupacElementPropertyBeanList[elementName] = iupacElementPropertyBeanList;
        //    }
        //}

        private bool analystExperimentFileCheck(string filename)
        {
            if (filename == null || filename == string.Empty || !System.IO.File.Exists(filename)) {
                MessageBox.Show("Please import an experimental file to be used in DIA-MS project. The format is described in the tutorial.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            Dictionary<int, AnalystExperimentInformationBean> experimentID_AnalystExperimentInformationBean = new Dictionary<int, AnalystExperimentInformationBean>();

            string[] lines;
            int counter = 0;

            bool checker = true, checkerAif = true;
            int experimentID, check;
            float startMz, endMz, ce;

            StreamReader sr = new StreamReader(filename);
            var header = sr.ReadLine();

            while (sr.Peek() > -1)
            {
                lines = sr.ReadLine().Split('\t');
                if (lines.Length == 0) break;

                if (lines.Length > 0 && lines.Length < 3) { checker = false; break; }

                if (lines.Length <= 7 && this.projectProperty.CheckAIF == false) {
                    if (lines[1] == "SCAN") {
                        if (int.TryParse(lines[0], out experimentID) && float.TryParse(lines[2], out startMz) && float.TryParse(lines[3], out endMz))
                            experimentID_AnalystExperimentInformationBean[counter] = new AnalystExperimentInformationBean() { ExperimentNumber = experimentID, MsType = MsType.SCAN, StartMz = startMz, EndMz = endMz };
                        else {
                            checker = false;
                            break;
                        }
                    }
                    else if (lines[1] != "SCAN") {
                        if (int.TryParse(lines[0], out experimentID) && float.TryParse(lines[2], out startMz) && float.TryParse(lines[3], out endMz))
                            experimentID_AnalystExperimentInformationBean[counter] = new AnalystExperimentInformationBean() { ExperimentNumber = experimentID, MsType = MsType.SWATH, StartMz = startMz, EndMz = endMz };
                        else {
                            checker = false;
                            break;
                        }
                    }
                    else {
                        checker = false;
                        break;
                    }
                    counter++;
                } else if(this.projectProperty.CheckAIF && lines.Length == 6) {    // w/o CE column            
                    if (lines[1] == "SCAN") {
                        if (int.TryParse(lines[0], out experimentID) && float.TryParse(lines[2], out startMz) && float.TryParse(lines[3], out endMz) && int.TryParse(lines[5], out check) && lines[4].Length > 0)
                            experimentID_AnalystExperimentInformationBean[counter] = new AnalystExperimentInformationBean() { ExperimentNumber = experimentID, MsType = MsType.SCAN, StartMz = startMz, EndMz = endMz, Name = lines[4].ToString(), CheckDecTarget = check };
                        else {
                            checkerAif = false;
                            break;
                        }
                    }
                    else if (lines[1] != "SCAN") {
                        if (int.TryParse(lines[0], out experimentID) && float.TryParse(lines[2], out startMz) && float.TryParse(lines[3], out endMz) && int.TryParse(lines[5], out check) && lines[4].Length > 0)
                            experimentID_AnalystExperimentInformationBean[counter] = new AnalystExperimentInformationBean() { ExperimentNumber = experimentID, MsType = MsType.AIF, StartMz = startMz, EndMz = endMz, Name = lines[4].ToString(), CheckDecTarget = check };
                        else {
                            checkerAif = false;
                            break;
                        }
                    }
                    else {
                        checkerAif = false;
                        break;
                    }
                    counter++;
                
                } else if (this.projectProperty.CheckAIF && lines.Length == 7) { // w/ CE column
                    if (lines[1] == "SCAN") {
                        if (int.TryParse(lines[0], out experimentID) && float.TryParse(lines[2], out startMz) && float.TryParse(lines[3], out endMz) && float.TryParse(lines[5], out ce) && int.TryParse(lines[6], out check) && lines[4].Length > 0)
                            experimentID_AnalystExperimentInformationBean[counter] = new AnalystExperimentInformationBean() { ExperimentNumber = experimentID, MsType = MsType.SCAN, StartMz = startMz, EndMz = endMz, Name = lines[4].ToString(),  CheckDecTarget = check, CollisionEnergy = ce };
                        else {
                            checkerAif = false;
                            break;
                        }
                    }
                    else if (lines[1] != "SCAN") {
                        if (int.TryParse(lines[0], out experimentID) && float.TryParse(lines[2], out startMz) && float.TryParse(lines[3], out endMz) && float.TryParse(lines[5], out ce) && int.TryParse(lines[6], out check) && lines[4].Length > 0)
                            experimentID_AnalystExperimentInformationBean[counter] = new AnalystExperimentInformationBean() { ExperimentNumber = experimentID, MsType = MsType.AIF, StartMz = startMz, EndMz = endMz, Name = lines[4].ToString(), CheckDecTarget = check, CollisionEnergy = ce };
                        else {
                            checkerAif = false;
                            break;
                        }
                    }
                    else {
                        checkerAif = false;
                        break;
                    }
                    counter++;
                }
            }
            sr.Close();

            if (CheckAIF) {
                if (experimentID_AnalystExperimentInformationBean.Values.Count(x => x.CheckDecTarget == 1) == 0 || checkerAif == false) {
                    {
                        string text = "Invalid analyst experiment information. Please confirm your file and prepare the following information.\r\n";
                        text += "Experiment\tMS Type\tMin m/z\tMax m/z\tDisplay Name\tCollisionEnergy\tDeconvolution Target (1:Yes, 0:No)\r\n";
                        text += "0\tAIF\t50\t1500\t10eV\t10\t1\r\n";
                        text += "1\tAIF\t50\t1500\t30eV\t30\t1\r\n";
                        text += "2\tSCAN\t50\t1500\t0eV\t0\t0\r\n";
                        var w = new ShortMessageWindow(text);
                        w.Width = 700; w.Height = 200;
                        w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                        w.Label_MessageTitle.TextAlignment = TextAlignment.Left;
                        w.Show();
                        return false;
                    }
                }
            }

            if (checker == false)
            {
                string text = "Invalid analyst experiment information. Please confirm your file and prepare the following information.\r\n";
                text += "Experiment\tMS Type\tMin m/z\tMax m/z\r\n";
                text += "0\tSCAN\t100\t500\r\n";
                text += "1\tSWATH\t100\t125\r\n";
                text += "2\tSWATH\t125\t150\r\n";
                text += "3\tSWATH\t150\t175\r\n";
                text += "4\tSWATH\t175\t200\r\n";
                text += "5\tSWATH\t200\t225\r\n";
                text += "6\tSWATH\t225\t250\r\n";
                text += "7\tSWATH\t250\t275\r\n";
                text += "8\tSWATH\t275\t300\r\n";
                text += "9\tSWATH\t300\t325\r\n";
                text += "10\tSWATH\t325\t350\r\n";
                text += "11\tSWATH\t350\t375\r\n";
                text += "12\tSWATH\t375\t400\r\n";
                text += "13\tSWATH\t400\t425\r\n";
                text += "14\tSWATH\t425\t450\r\n";
                text += "14\tSWATH\t450\t475\r\n";
                text += "14\tSWATH\t475\t500\r\n";
                text += "This information should be found from Show->Sample information in PeakViewer (AB Sciex case).";

                MessageBox.Show(text, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            else
            {
                this.projectProperty.ExperimentID_AnalystExperimentInformationBean = experimentID_AnalystExperimentInformationBean;
                if (this.projectProperty.CheckAIF) {
                    foreach(var value in experimentID_AnalystExperimentInformationBean) { if (value.Value.CheckDecTarget == 1) { this.projectProperty.Ms2LevelIdList.Add(value.Key); } }
                }
                this.projectProperty.CollisionEnergyList = experimentID_AnalystExperimentInformationBean.Select(x => x.Value.CollisionEnergy).ToList();
                return true;
            }
        }

        private DelegateCommand next;
        public DelegateCommand Next
        {
            get
            {
                return next ?? (next = new DelegateCommand(winobj => {
                    var view = (StartUpWindow)winobj;
                    if (ClosingMethod() == true)
                    {
                        view.DialogResult = true;
                        view.Close();
                    }
                }, CanExecuteOkCommand));
            }
        }

        private bool CanExecuteOkCommand(object arg)
        {
            if (this.projectProperty.ProjectFilePath == null || this.projectProperty.ProjectFilePath == string.Empty) {
                return false;
            }
            else if (!IsSafePath(this.projectProperty.ProjectFilePath, false)) {
                return false;
            }
            else if (!System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(this.projectProperty.ProjectFilePath))) {
                return false;
            }
            else {
                return true;
            }
        }

        public static bool IsSafePath(string path, bool isFileName) {
            if (string.IsNullOrEmpty(path)) {
                return false;
            }

            char[] invalidChars;
            if (isFileName) {
                invalidChars = System.IO.Path.GetInvalidFileNameChars();
            }
            else {
                invalidChars = System.IO.Path.GetInvalidPathChars();
            }
            if (path.IndexOfAny(invalidChars) >= 0) {
                return false;
            }

            if (System.Text.RegularExpressions.Regex.IsMatch(path
                                           , @"(^|\\|/)(CON|PRN|AUX|NUL|CLOCK\$|COM[0-9]|LPT[0-9])(\.|\\|/|$)"
                                           , System.Text.RegularExpressions.RegexOptions.IgnoreCase)) {
                return false;
            }
            return true;
        }
    }
}
