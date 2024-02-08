using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.Model.Export;
using CompMs.CommonMVVM;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using RDotNet;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CompMs.App.Msdial.Model.Statistics {
    internal sealed class Notame : BindableBase {
        public Notame(ExportMethod exportMethod, ExportType[] exportTypes, AlignmentFilesForExport alignmentFilesForExport, AlignmentPeakSpotSupplyer peakSpotSupplyer, DataExportBaseParameter dataExportParameter, ParameterBase parameterBase) {
            AlignmentFilesForExport = alignmentFilesForExport;
            PeakSpotSupplyer = peakSpotSupplyer ?? throw new ArgumentNullException(nameof(peakSpotSupplyer));
            ExportMethod = exportMethod;
            ExportTypes = exportTypes;
            ExportDirectory = dataExportParameter.ExportFolderPath;
            IonMode = parameterBase.IonMode;
        }

        public string ExportDirectory {
            get => _exportDirectory;
            set => SetProperty(ref _exportDirectory, value);
        }
        private string _exportDirectory;

        public string GetExportFolder() {
            var folder = ExportDirectory.Replace("\\", "/");
            return folder;
        }

        public AlignmentFilesForExport AlignmentFilesForExport { get; }
        public AlignmentPeakSpotSupplyer PeakSpotSupplyer { get; }
        public ExportMethod ExportMethod { get; }
        public ExportType[] ExportTypes { get; }

        public Task ExportAlignmentResultAsync(IMessageBroker broker) {
            return Task.Run(() => {
                var publisher = new TaskProgressPublisher(broker, $"Exporting {AlignmentFilesForExport.SelectedFile.FileName}");
                using (publisher.Start()) {
                    var alignmentFile = AlignmentFilesForExport.SelectedFile;
                    var type = ExportTypes.FirstOrDefault(type => type.IsSelected);
                    if (type is null) {
                        throw new Exception("Export type (Height, Area, ...) is not selected.");
                    }
                    var fileName = $"{type.TargetLabel}_{((IFileBean)alignmentFile).FileID}_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}";
                    var msdecResults = alignmentFile.LoadMSDecResults();
                    var lazyPeakSpot = new Lazy<IReadOnlyList<AlignmentSpotProperty>>(() => PeakSpotSupplyer.Supply(alignmentFile, default));
                    ExportMethod.Export(fileName, ExportDirectory, lazyPeakSpot, msdecResults, null, new[] { type });
                    FileName = ExportMethod.Format.WithExtension(fileName);
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
            return null;
        }

        private string NotameIonMode;
        private string NotameExport;
        private string FileName;

        public void Run() {
            NotameIonMode = GetIonMode();
            NotameExport = GetExportFolder();
            MessageBox.Show("Please wait a moment.");
            RunNotame();
        }

        private void RunNotame() {
            var xmlReader = new NotameXmlReader();
            xmlReader.Read();
            var NotameR = xmlReader.rScript;
            REngine.SetEnvironmentVariables();
            REngine.SetEnvironmentVariables("c:/program files/r/r-4.3.2/bin/x64", "c:/program files/r/r-4.3.2");
            var engine = REngine.GetInstance();
            engine.Evaluate("Sys.setenv(PATH = paste(\"C:/Program Files/R/R-4.3.2/bin/x64\", Sys.getenv(\"PATH\"), sep=\";\"))");
            engine.Evaluate("library(notame)");
            engine.Evaluate("library(doParallel)");
            engine.Evaluate("library(dplyr)");
            engine.Evaluate("library(openxlsx)");
            engine.SetSymbol("path", engine.CreateCharacter(NotameExport));
            engine.SetSymbol("file_name", engine.CreateCharacter(FileName));
            engine.SetSymbol("ion_mod", engine.CreateCharacter(NotameIonMode));

            engine.Evaluate(NotameR);
            MessageBox.Show("Output files are successfully created.");
            engine.Dispose();
        }
    }
}
