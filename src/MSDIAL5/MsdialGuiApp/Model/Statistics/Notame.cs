using CompMs.App.Msdial.Model.Export;
using CompMs.App.Msdial.Properties;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using RDotNet;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CompMs.App.Msdial.Model.Statistics {
    internal sealed class Notame : DisposableModelBase {
        public Notame(AlignmentFilesForExport alignmentFilesForExport, AlignmentPeakSpotSupplyer peakSpotSupplyer, AlignmentExportGroupModel exportModel, DataExportBaseParameter dataExportParameter, ParameterBase parameterBase) {
            AlignmentFilesForExport = alignmentFilesForExport;
            PeakSpotSupplyer = peakSpotSupplyer ?? throw new ArgumentNullException(nameof(peakSpotSupplyer));
            ExportModel = exportModel;
            ExportDirectory = dataExportParameter.ExportFolderPath;
            IonMode = parameterBase.IonMode;
            RDirectory = Settings.Default.RHome;
        }

        public string ExportDirectory {
            get => _exportDirectory;
            set => SetProperty(ref _exportDirectory, value);
        }
        private string _exportDirectory = string.Empty;

        public string RDirectory {
            get => _rDirectory;
            set => SetProperty(ref _rDirectory, value);
        }
        private string _rDirectory = string.Empty;

        public string GetExportFolder(string directory) {
            var folder = directory.Replace("\\", "/");
            return folder;
        }

        public AlignmentExportGroupModel ExportModel { get; }

        public AlignmentFilesForExport AlignmentFilesForExport { get; }
        public AlignmentPeakSpotSupplyer PeakSpotSupplyer { get; }
        public ExportMethod ExportMethod => ExportModel.ExportMethod;
        public ReadOnlyObservableCollection<ExportType> ExportTypes => ExportModel.Types;
        public bool exportReport;

        public Task ExportAlignmentResultAsync(IMessageBroker broker) {
            return Task.Run(() => {
                if (AlignmentFilesForExport.SelectedFile is null) {
                    return;
                }
                var publisher = new TaskProgressPublisher(broker, $"Exporting {AlignmentFilesForExport.SelectedFile.FileName}");
                using (publisher.Start()) {
                    var alignmentFile = AlignmentFilesForExport.SelectedFile;
                    if (ExportTypes.FirstOrDefault(type => type.IsSelected) is not { } type) {
                        throw new Exception("Export type (Height, Area, ...) is not selected.");
                    }
                    var fileName = $"{type.TargetLabel}_{((IFileBean)alignmentFile).FileID}_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}";
                    FileName = ExportMethod.Format.WithExtension(fileName);
                    ExportModel.Export(alignmentFile, ExportDirectory, fileName, null);
                }
            });
        }

        private readonly IonMode IonMode;
        private REngine? engine;
        private string originalDir;

        public string GetIonMode() {
            if (IonMode == IonMode.Positive) {
                return "pos";
            }
            else if (IonMode == IonMode.Negative) {
                return "neg";
            }
            return string.Empty;
        }

        private string NotameIonMode = string.Empty;
        private string NotameExport = string.Empty;
        private string FileName = string.Empty;
        private string RPath = string.Empty;

        [STAThread]
        public void Run() {
            try {
                NotameIonMode = GetIonMode();
                NotameExport = GetExportFolder(ExportDirectory);
                RPath = GetExportFolder(RDirectory);
                REngine.SetEnvironmentVariables();
                REngine.SetEnvironmentVariables($"{RPath}/bin/x64", RPath);
                engine = REngine.GetInstance();
                engine.Evaluate($@"Sys.setenv(PATH = paste('{RPath}/bin/x64', Sys.getenv('PATH'), sep=';'))");
                
                string[] libraries = ["BiocManager", "pcaMethods", "Biobase", "devtools", "notame", "remotes", "tinytex", "cowplot", "missForest", "ggpubr", "Cairo", "doParallel", "dplyr", "tidyr", "openxlsx", "MUVR"];
                var check = libraries.Select(lib => new {
                    Package = lib,
                    IsLoaded = engine.Evaluate($"require(\"{lib}\")").AsLogical().FirstOrDefault()
                });
                if (check.Any(x => !x.IsLoaded)) {
                    foreach (var item in check.Where(x => !x.IsLoaded)) {
                        MessageBox.Show($"Some packages failed to load: {item.Package}");
                    }
                    Packages();
                }
                originalDir = engine.Evaluate("getwd()").AsCharacter().First();
                RunNotame(engine);
                if (exportReport) {
                    ExportReport(engine);
                }
                MessageBox.Show("Output files are successfully created.");
                if (Settings.Default.RHome != RDirectory) {
                    Settings.Default.RHome = RDirectory;
                    Settings.Default.Save();
                }
            } catch (Exception ex) {
                MessageBox.Show($"An error occurred on R: {ex.Message}");
            } finally {
                if (Settings.Default.RHome != RDirectory) {
                    Settings.Default.RHome = RDirectory;
                    Settings.Default.Save();
                }
                engine?.Evaluate($"setwd('{originalDir.Replace(@"\", @"\\")}')");
            }
        }

        private bool Packages() {
            string msgtext = "There is a package which has not installed yet. Please run this scripts on your local R Studio to install packages used in Notame preprocessing.\r\n\r\n" +
                            "###########################################\r\n\r\n" +
                            "if (!requireNamespace('BiocManager', quietly=TRUE)){\r\n  " +
                            "install.packages('BiocManager')}\r\n" +
                            "BiocManager::install('pcaMethods')\r\n" +
                            "BiocManager::install('Biobase')\r\n\r\n" +
                            "if (!requireNamespace('devtools', quietly = TRUE)) {\r\n  " +
                            "install.packages('devtools')}\r\n" +
                            "devtools::install_github('antonvsdata/notame')\r\n\r\n" +
                            "if (!requireNamespace('remotes', quietly=TRUE)){\r\n  " +
                            "install.packages('remotes')}\r\n" +
                            "library(remotes)\r\n" +
                            "install_gitlab('CarlBrunius/MUVR')\r\n\r\n" +
                            "if (!requireNamespace('tinytex', quietly=TRUE)){\r\n  " +
                            "install.packages('tinytex')}\r\n" +
                            "tinytex::install_tinytex()\r\n" +
                            "tinytex::tlmgr_install('grfext')\r\n\r\n" +
                            "required_packages <- c('doParallel', 'dplyr', 'openxlsx', 'cowplot', 'missForest', 'ggpubr', 'Cairo', 'tidyr')\r\n" +
                            "packages_to_install <- required_packages[!(required_packages %in% installed.packages()[,'Package'])]\r\n\r\n" +
                            "if(length(packages_to_install)) {\r\n  " +
                            "install.packages(packages_to_install)}\r\n" +
                            "lapply(required_packages, library, character.only = TRUE)\r\n\r\n" +
                            "##########################################";
            var result = CustomPackageDialog.ShowDialog(msgtext);
            if (MessageBox.Show("Click OK to run Notame (packages needed to be installed)", "Notame",
                MessageBoxButtons.OKCancel) == DialogResult.Cancel) {
                return false;
            } else {
                return true;
            }
        }

        private void RunNotame(REngine engine) {
            string notameScriptPath = "Resources/Notame.R";
            string notameScript = System.IO.File.ReadAllText(notameScriptPath);

            engine.SetSymbol("path", engine.CreateCharacter(NotameExport));
            engine.SetSymbol("file_name", engine.CreateCharacter(FileName));
            engine.SetSymbol("ion_mod", engine.CreateCharacter(NotameIonMode));
            engine.Evaluate(notameScript);
        }

        private void ExportReport(REngine engine) {
            string reportScriptPath = "Resources/Report.R";
            string reportScript = System.IO.File.ReadAllText(reportScriptPath);

            engine.SetSymbol("path", engine.CreateCharacter(NotameExport));
            engine.Evaluate(reportScript);
        }
    }
    public class CustomPackageDialog : Form {
        private TextBox txtMessage;
        private string message;

        public CustomPackageDialog(string message) {
            this.message = message;
            InitializeComponents();
        }

        private void InitializeComponents() {
            this.Text = "Package Installation";
            this.Size = new System.Drawing.Size(600, 400);

            txtMessage = new TextBox();
            txtMessage.Multiline = true;
            txtMessage.ReadOnly = true;
            txtMessage.Text = message;
            txtMessage.Dock = DockStyle.Top;
            txtMessage.Height = 350;
            this.Controls.Add(txtMessage);
        }

        public static DialogResult ShowDialog(string message) {
            using (var dialog = new CustomPackageDialog(message)) {
                return dialog.ShowDialog();
            }
        }
    }
}
