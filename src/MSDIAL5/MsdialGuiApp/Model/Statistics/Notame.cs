using CompMs.App.Msdial.Model.Export;
using CompMs.App.Msdial.Properties;
using CompMs.App.Msdial.Utility;
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

        public void Run() {
            try {
                NotameIonMode = GetIonMode();
                NotameExport = GetExportFolder(ExportDirectory);
                RPath = GetExportFolder(RDirectory);
                REngine.SetEnvironmentVariables();
                REngine.SetEnvironmentVariables($"{RPath}/bin/x64", RPath);
                var engine = REngine.GetInstance();
                engine.Evaluate($@"Sys.setenv(PATH = paste('{RPath}/bin/x64', Sys.getenv('PATH'), sep=';'))");
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
                MessageBox.Show($"An error occurred: {ex.Message}");
            } finally {
                if (Settings.Default.RHome != RDirectory) {
                    Settings.Default.RHome = RDirectory;
                    Settings.Default.Save();
                }
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
}
