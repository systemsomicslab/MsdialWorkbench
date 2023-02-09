using CompMs.RawDataHandler.Core;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace RawDataHandlerGuiApp {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
            this.DataContext = new MainWindowVM(this);
        }

        private void Click_Cancel(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void Click_Clear(object sender, RoutedEventArgs e) {
            var vm = (MainWindowVM)this.DataContext;
            vm.FilePathes = new ObservableCollection<string>();
        }

        private void Click_Convert(object sender, RoutedEventArgs e) {
            var vm = (MainWindowVM)this.DataContext;
            vm.ConvertFiles();
        }

        private void textFiles_DragOver(object sender, DragEventArgs e) {
            e.Effects = System.Windows.DragDropEffects.Copy;
            e.Handled = true;
        }

        private void textFiles_Drop(object sender, DragEventArgs e) {
            string[] files = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
            string lastfile = "";
            var includedFiles = new List<string>();
            var excludedFiles = new List<string>();

            // Set BaseFileList Clone
            for (int i = 0; i < files.Length; i++) {
                if (IsAccepted(files[i])) {
                    includedFiles.Add(files[i]);
                }
                else {
                    excludedFiles.Add(System.IO.Path.GetFileName(files[i]));
                }
            }

            if (0 < excludedFiles.Count) {
                System.Windows.MessageBox.Show("The following file(s) cannot be converted because they are not acceptable raw files\n" +
                    String.Join("\n", excludedFiles.ToArray()),
                    "Unacceptable Files",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            if (includedFiles.Count > 0) {
                var vm = (MainWindowVM)this.DataContext;
                vm.FilePathes = new ObservableCollection<string>(includedFiles);
            }
        }

        private bool IsAccepted(string file) {
            var extension = System.IO.Path.GetExtension(file).ToLower();
            if (extension != ".mzml" && extension != ".raw" && extension != ".d")
                return false;
            else
                return true;
        }
    }

    public class MainWindowVM : ViewModelBase {
        public MainWindowVM(MainWindow mainWindow) {
            this.MainWindow = mainWindow;
            this.FilePathes = new ObservableCollection<string>() { MessageToUsers };
            this.Status = "Status:";
            this.AbundanceCutoff = 5.0;
        }

        public MainWindow MainWindow { get; set; }
        public string MessageToUsers { get; private set; } = "Drag your raw data files here.";

        private string status;
        public string Status {
            get { return status; }
            set {
                if (status == value) return;
                status = value;
                OnPropertyChanged("Status");
            }
        }

        private ObservableCollection<string> filePathes;
        public ObservableCollection<string> FilePathes {
            get { return filePathes; }
            set {
                if (filePathes == value) return;
                filePathes = value;
                executeMessageToUsers();
                OnPropertyChanged("FilePathes");
            }
        }

        private double abundanceCutoff;
        public double AbundanceCutoff {
            get {
                return abundanceCutoff;
            }

            set {
                abundanceCutoff = value;
                OnPropertyChanged("AbundanceCutoff");
            }
        }

        private void executeMessageToUsers() {
            if (this.FilePathes == null || this.FilePathes.Count() == 0) {
                this.FilePathes = new ObservableCollection<string>() { MessageToUsers };
                this.MainWindow.ListBox_Files.FontStyle = FontStyles.Italic;
                this.MainWindow.ListBox_Files.Foreground = Brushes.Gray;
            } else if (this.FilePathes.Count() == 1 && this.FilePathes[0] == this.MessageToUsers) {
                this.MainWindow.ListBox_Files.FontStyle = FontStyles.Italic;
                this.MainWindow.ListBox_Files.Foreground = Brushes.Gray;
            }
            else {
                this.MainWindow.ListBox_Files.FontStyle = FontStyles.Normal;
                this.MainWindow.ListBox_Files.Foreground = Brushes.Black;
            }
        }

        public void ConvertFiles() {
            var conv = new IabfFileConversion();
            var cutoff = this.AbundanceCutoff;
            if (cutoff < 1) cutoff = 1.0;
            conv.ConvertToIABF(this.filePathes, this.MainWindow, this.AbundanceCutoff);
        }
    }

    public class IabfFileConversion {
        private BackgroundWorker bgWorker;
        private string progressFileMax;
        private int processedFileCount;
        private MainWindow mainWindow;

        public void ConvertToIABF(ObservableCollection<string> files, MainWindow mainWindow, double peakCutOff) {
            this.mainWindow = mainWindow;
            bgWorkerInitialize(files, mainWindow);
            this.bgWorker.RunWorkerAsync(new Object[] { files, peakCutOff });
        }

        private void bgWorkerInitialize(ObservableCollection<string> files, MainWindow mainWindow) {
            mainWindow.IsEnabled = false;
            this.progressFileMax = files.Count.ToString();
            this.processedFileCount = 0;

            //background worker
            this.bgWorker = new BackgroundWorker();
            this.bgWorker.WorkerReportsProgress = true;
            this.bgWorker.WorkerSupportsCancellation = true;
            this.bgWorker.ProgressChanged += new ProgressChangedEventHandler(bgWorker_ProgressChanged);
            this.bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);
            this.bgWorker.DoWork += new DoWorkEventHandler(bgWorker_DoWork);
        }

        private void bgWorker_DoWork(object sender, DoWorkEventArgs e) {
            object[] arg = (object[])e.Argument;

            var files = arg[0] as ObservableCollection<string>;
            var aCutOff = (double)arg[1];

            foreach (var file in files) {
                var access = new RawDataAccess(file, 0, false, false, true, null, this.bgWorker);
                access.PeakCutOff = aCutOff;
                access.ConvertToIABF();
                this.bgWorker.ReportProgress(101);
            }

            e.Result = new object[] { mainWindow };
        }

        private void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            var progress = e.ProgressPercentage;
            if (progress == 101) {
                this.processedFileCount++;
                progress = 0;
            }

            var vm = (MainWindowVM)this.mainWindow.DataContext;
            vm.Status = this.processedFileCount + "/" + this.progressFileMax + " finished. Current progress: " + progress + "%";
        }

        private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            this.mainWindow.IsEnabled = true;
            if (e.Error != null) {
                MessageBox.Show("Error is occured in file conversion.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else {
                MessageBox.Show("File conversion finished.", "Messegae", MessageBoxButton.OK, MessageBoxImage.Information);
                var vm = (MainWindowVM)this.mainWindow.DataContext;
                vm.FilePathes = new ObservableCollection<string>();
            }
        }
    }
}
