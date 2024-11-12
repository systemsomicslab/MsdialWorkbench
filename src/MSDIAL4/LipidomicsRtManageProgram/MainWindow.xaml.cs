using Microsoft.Win32;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using RDotNet;
using System.IO;
using CompMs.Common.MessagePack;
using CompMs.Common.Parser;
using CompMs.Common.Components;

namespace Lipidomics.Retentiontime.Manager {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
            this.DataContext = new MainWindowVM();
            Console.WriteLine("Optimization program for retention times used in MSDIAL lipidomics lbm2 file.");
        }
    }

    public class MainWindowVM : ViewModelBase {
        private string lbmFilePath;
        private string modelFilePath;
        private string rLocationPath;
        private string outputFolderPath;
        private string rWorkingDirectry;

        #region 
        public string LbmFilePath {
            get {
                return lbmFilePath;
            }

            set {
                lbmFilePath = value; OnPropertyChanged("LbmFilePath");
            }
        }

        public string ModelFilePath {
            get {
                return modelFilePath;
            }

            set {
                modelFilePath = value; OnPropertyChanged("ModelFilePath");
            }
        }

        public string RLocationPath {
            get {
                return rLocationPath;
            }

            set {
                rLocationPath = value; OnPropertyChanged("RLocationPath");
            }
        }

        public string OutputFolderPath {
            get {
                return outputFolderPath;
            }

            set {
                outputFolderPath = value; OnPropertyChanged("OutputFolderPath");
            }
        }

        public string RWorkingDirectry {
            get {
                return rWorkingDirectry;
            }

            set {
                rWorkingDirectry = value; OnPropertyChanged("RWorkingDirectry");
            }
        }
        #endregion

        /// <summary>
        /// Opens the folder selection dialog
        /// </summary>
        private DelegateCommand selectRLocationFolder;
        public DelegateCommand SelectRLocationFolder {
            get {
                return selectRLocationFolder ?? (selectRLocationFolder = new DelegateCommand(ShowRLocationSelectDialog, arguments => { return true; }));
            }
        }

        /// <summary>
        /// actual action for the SelectExportFolder command
        /// </summary>
        /// <param name="obj"></param>
        private void ShowRLocationSelectDialog(object obj) {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.RootFolder = Environment.SpecialFolder.Desktop;
            fbd.Description = "Choose a folder where to save the exported files.";
            fbd.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                this.RLocationPath = fbd.SelectedPath;
            }
        }

        /// <summary>
        /// Opens the folder selection dialog
        /// </summary>
        private DelegateCommand selectOutputFolder;
        public DelegateCommand SelectOutputFolder {
            get {
                return selectOutputFolder ?? (selectOutputFolder = new DelegateCommand(ShowOutputFolderSelectDialog, arguments => { return true; }));
            }
        }

        /// <summary>
        /// actual action for the SelectExportFolder command
        /// </summary>
        /// <param name="obj"></param>
        private void ShowOutputFolderSelectDialog(object obj) {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.RootFolder = Environment.SpecialFolder.Desktop;
            fbd.Description = "Choose a folder where to save the exported files.";
            fbd.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                this.OutputFolderPath = fbd.SelectedPath;
            }
        }

        /// <summary>
        /// Opens the folder selection dialog
        /// </summary>
        private DelegateCommand selectLbmFilePath;
        public DelegateCommand SelectLbmFilePath {
            get {
                return selectLbmFilePath ?? (selectLbmFilePath = new DelegateCommand(ShowLbmFilePathSelectDialog, arguments => { return true; }));
            }
        }

        /// <summary>
        /// actual action for the SelectExportFolder command
        /// </summary>
        /// <param name="obj"></param>
        private void ShowLbmFilePathSelectDialog(object obj) {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "LBM2 file(*.lbm2)|*.lbm2";
            ofd.Title = "Import an LBM2 file";
            ofd.RestoreDirectory = true;
            ofd.Multiselect = true;

            if (ofd.ShowDialog() == true) {
                this.LbmFilePath = ofd.FileName;
            }
        }


        /// <summary>
        /// Opens the folder selection dialog
        /// </summary>
        private DelegateCommand selectModelFilePath;
        public DelegateCommand SelectModelFilePath {
            get {
                return selectModelFilePath ?? (selectModelFilePath = new DelegateCommand(ShowModelFilePathSelectDialog, arguments => { return true; }));
            }
        }

        /// <summary>
        /// actual action for the SelectExportFolder command
        /// </summary>
        /// <param name="obj"></param>
        private void ShowModelFilePathSelectDialog(object obj) {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Excel file(*.xlsx)|*.xlsx";
            ofd.Title = "Import a model file";
            ofd.RestoreDirectory = true;
            ofd.Multiselect = true;

            if (ofd.ShowDialog() == true) {
                this.ModelFilePath = ofd.FileName;
            }
        }

        /// <summary>
        /// Opens the folder selection dialog
        /// </summary>
        private DelegateCommand run;
        public DelegateCommand Run {
            get {
                return run ?? (run = new DelegateCommand(ProcessRun, arguments => { return true; }));
            }
        }

        
        private void ProcessRun(object obj) {
            
            //Set the directory containing R.dll
            REngine.SetEnvironmentVariables(@"D:\Program Files\R\R-3.6.3\bin\x64");

            this.modelFilePath = @"D:\takahashi\desktop\Tsugawa-san_work\20200601RTprediction\Retiprt_training_20200119.xlsx";
            this.rLocationPath = @"D:\Program Files\R\R-3.6.3\bin\x64";
            this.lbmFilePath = @"D:\takahashi\desktop\Tsugawa-san_work\20200601RTprediction\AAHFA_H_Neg.msp";
            this.outputFolderPath = @"D:\takahashi\desktop\Tsugawa-san_work\20200601RTprediction\result\";
            this.rWorkingDirectry = @"D:\takahashi\desktop\Tsugawa-san_work\20200601RTprediction\Retip_C\";

            var outputFilePath = this.outputFolderPath + "\\" + System.IO.Path.GetFileNameWithoutExtension(this.lbmFilePath) + "_converted.lbm2";
            var rWorkingDirectryConverted = this.rWorkingDirectry.Replace("\\", "/");

            Console.WriteLine("Loading the lbm2 file.");

            var mspDB = MspFileParser.MspFileReader(this.lbmFilePath);
            var inchikeyToSmiles = new Dictionary<string, string>();
            foreach (var query in mspDB) {
                if (!inchikeyToSmiles.ContainsKey(query.InChIKey)) {
                    inchikeyToSmiles[query.InChIKey] = query.SMILES;
                }
            }

            var tempCsvFilePath = this.rWorkingDirectry + "\\" + "temp.csv";
            var counter = 0;
            using (var sw = new StreamWriter(tempCsvFilePath, false, Encoding.ASCII)) {
                sw.WriteLine("Name,InChIKey,SMILES");
                foreach (var pair in inchikeyToSmiles) {
                    sw.WriteLine("ID_"+ counter + "," + pair.Key + "," + pair.Value);
                    counter++;
                }
            }

            var tempPredictedRtFile = this.rWorkingDirectry + "\\" + "demo_rt.txt";

            //r execute instance．

            using (REngine r = REngine.GetInstance()) {
                r.Initialize();

                r.Evaluate("library(Retip)");
                r.Evaluate("library(readr)");

                //Set the working directory
                r.Evaluate("setwd(\"" + rWorkingDirectryConverted + "\")");

                //Starts parallel computing
                r.Evaluate("prep.wizard()");

                //Import excel file for training data
                r.Evaluate("master <- readxl::read_excel(\"demo_master.xlsx\", sheet = \"master\", col_types = c(\"text\", \"text\", \"text\", \"numeric\"))");

                //Calculate Chemical Descriptors from CDK
                r.Evaluate("descs <- getCD(master)");

                //Clean dataset from NA and low variance value
                r.Evaluate("db_rt <- proc.data(descs)");

                //Train Model
                r.Evaluate("xgb <- fit.xgboost(db_rt)");

                //Import dataset
                r.Evaluate("DB <- read_csv(\"temp.csv\")");

                //Compute Chemical descriptors
                r.Evaluate("DB_desc <- getC D(DB)");

                //Perform the RT spell
                r.Evaluate("DB_pred <- RT.spell(db_rt,DB_desc,model=xgb)");

                //Export
                r.Evaluate("write.table(DB_pred, \"demo_rt.txt\", quote=F,  col.names=F, append=F)");
                
            }

            var inchikeyToPredictedRt = new Dictionary<string, float>();
            using (var sr = new StreamReader(tempPredictedRtFile, true)) {
                while (sr.Peek()> -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) continue;
                    var linearray = line.Split(' ');
                    var inchikey = linearray[2];
                    var predictedRtString = linearray[4];
                    var predictedRt = -1.0F;
                    if (float.TryParse(predictedRtString,out predictedRt) && !inchikeyToPredictedRt.ContainsKey(inchikey)) {
                        inchikeyToPredictedRt[inchikey] = predictedRt;
                    }
                }
            }

            foreach (var query in mspDB) {
                if (inchikeyToPredictedRt.ContainsKey(query.InChIKey)) {
                    query.ChromXs = new ChromXs(inchikeyToPredictedRt[query.InChIKey], ChromXType.RT, ChromXUnit.Min);
                }
                else {
                    Console.WriteLine("Error at {0}", query.InChIKey);
                }
            }

            MoleculeMsRefMethods.SaveMspToFile(mspDB, outputFilePath);

        }
    }
}
