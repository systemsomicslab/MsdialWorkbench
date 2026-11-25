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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CompMs.App.Msdial.Model.Statistics {
    internal sealed class Notame : DisposableModelBase {
        /// <summary>R packages the scripts in Resources (Notame.R, Report.R) use.</summary>
        private static readonly string[] _requiredPackages = [
            "notame", "SummarizedExperiment", "BiocParallel", "pcaMethods", "missForest", "MUVR",
            "doParallel", "foreach", "dplyr", "openxlsx", "cowplot", "ggsignif", "futile.logger",
        ];

        /// <summary>Installs everything the Notame preprocessing needs (notame 1.0 or later from Bioconductor).</summary>
        public const string PackageInstallScript =
            "# Run this in R (4.4 or later) or RStudio to install the packages used by the Notame preprocessing.\r\n" +
            "if (!requireNamespace('BiocManager', quietly = TRUE)) {\r\n" +
            "  install.packages('BiocManager')\r\n" +
            "}\r\n" +
            "BiocManager::install(c('notame', 'SummarizedExperiment', 'BiocParallel', 'pcaMethods'), update = FALSE, ask = FALSE)\r\n" +
            "install.packages(c('remotes', 'doParallel', 'foreach', 'dplyr', 'openxlsx', 'cowplot',\r\n" +
            "                   'ggsignif', 'missForest', 'futile.logger', 'Cairo', 'tinytex'))\r\n" +
            "remotes::install_gitlab('CarlBrunius/MUVR')\r\n" +
            "\r\n" +
            "# For the PDF report:\r\n" +
            "tinytex::install_tinytex()\r\n";

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

        public AlignmentExportGroupModel ExportModel { get; }

        public AlignmentFilesForExport AlignmentFilesForExport { get; }
        public AlignmentPeakSpotSupplyer PeakSpotSupplyer { get; }
        public ExportMethod ExportMethod => ExportModel.ExportMethod;
        public ReadOnlyObservableCollection<ExportType> ExportTypes => ExportModel.Types;
        public bool exportReport;

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

        /// <summary>
        /// Checks that R has the packages, exports the selected alignment result and runs the Notame
        /// workflow (and the PDF report when <see cref="exportReport"/> is set) on it.
        /// </summary>
        /// <exception cref="NotamePackageException">R lacks packages the workflow needs.</exception>
        public async Task RunAsync(IMessageBroker broker) {
            var rHome = RDirectory;
            var exportReport = this.exportReport;
            var missing = await NotameRSession.RunAsync(rHome, engine => MissingPackages(engine, exportReport)).ConfigureAwait(false);
            if (Settings.Default.RHome != rHome) {
                Settings.Default.RHome = rHome;
                Settings.Default.Save();
            }
            if (missing.Length > 0) {
                throw new NotamePackageException(missing);
            }

            var exportDirectory = ExportDirectory;
            var fileName = await ExportAlignmentResultAsync(broker, exportDirectory).ConfigureAwait(false);
            var ionMode = GetIonMode();
            var resources = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");

            var publisher = new TaskProgressPublisher(broker, $"Running notame on {fileName}");
            using (publisher.Start()) {
                await NotameRSession.RunAsync(rHome, engine => {
                    // Report.R changes R's working directory; it is kept in R, not passed through .NET strings.
                    engine.Evaluate(".msdial_wd <- getwd()");
                    try {
                        NotameRSession.SetString(engine, "path", exportDirectory);
                        NotameRSession.SetString(engine, "file_name", fileName);
                        NotameRSession.SetString(engine, "ion_mod", ionMode);
                        NotameRSession.Source(engine, Path.Combine(resources, "Notame.R"));
                        if (exportReport) {
                            CreateSweave(exportDirectory);
                            NotameRSession.Source(engine, Path.Combine(resources, "Report.R"));
                        }
                    }
                    finally {
                        engine.Evaluate("setwd(.msdial_wd)");
                    }
                }).ConfigureAwait(false);
            }
        }

        private static string[] MissingPackages(REngine engine, bool exportReport) {
            IEnumerable<string> packages = exportReport ? [.. _requiredPackages, "tinytex"] : _requiredPackages;
            var missing = NotameRSession.MissingPackages(engine, packages).ToList();
            if (!missing.Contains("notame")
                && engine.Evaluate("utils::packageVersion('notame') < '0.99.0'").AsLogical().First()) {
                var version = engine.Evaluate("as.character(utils::packageVersion('notame'))").AsCharacter().First();
                missing.Insert(0, $"notame 1.0 or later from Bioconductor (installed: {version})");
            }
            if (exportReport && !missing.Contains("tinytex")
                && !engine.Evaluate("tinytex::is_tinytex() || nzchar(Sys.which('pdflatex'))").AsLogical().First()) {
                missing.Add("TinyTeX (run tinytex::install_tinytex())");
            }
            return missing.ToArray();
        }

        private Task<string> ExportAlignmentResultAsync(IMessageBroker broker, string exportDirectory) {
            return Task.Run(() => {
                var alignmentFile = AlignmentFilesForExport.SelectedFile ?? throw new InvalidOperationException("Select an alignment result to process.");
                if (ExportTypes.FirstOrDefault(type => type.ShouldExport) is not { } type) {
                    throw new InvalidOperationException("Export type (Height, Area, ...) is not selected.");
                }
                var publisher = new TaskProgressPublisher(broker, $"Exporting {alignmentFile.FileName}");
                using (publisher.Start()) {
                    var fileName = $"{type.TargetLabel}_{((IFileBean)alignmentFile).FileID}_{DateTime.Now:yyyy_MM_dd_HH_mm_ss}";
                    ExportModel.Export(alignmentFile, exportDirectory, fileName, null);
                    return ExportMethod.Format.WithExtension(fileName);
                }
            });
        }

        private static void CreateSweave(string exportDirectory) {
            string path = Path.Combine(exportDirectory, "Sweave.sty");

            string content = @"%% This is file `Sweave.sty`,
%% generated with the docstrip utility.
%%
%% Copyright (C) 2000-2014 R Core Team
%%
\NeedsTeXFormat{LaTeX2e}
\ProvidesPackage{Sweave}[2014/09/10 Sweave package]
\RequirePackage{ifthen}

\newboolean{Sweave@gincl}
\setboolean{Sweave@gincl}{true}

\newboolean{Sweave@echo}
\setboolean{Sweave@echo}{true}

\newboolean{Sweave@eval}
\setboolean{Sweave@eval}{true}

\newboolean{Sweave@keep.source}
\setboolean{Sweave@keep.source}{false}

\newboolean{Sweave@term}
\setboolean{Sweave@term}{false}

\newboolean{Sweave@input}
\setboolean{Sweave@input}{false}

\newboolean{Sweave@strip.white}
\setboolean{Sweave@strip.white}{false}

\newcommand{\SweaveOpts}[1]{%
  \setkeys{Gin}{#1}%
}

\newcommand{\SweaveInput}[1]{%
  \IfFileExists{#1}{\input{#1}}{\typeout{No file #1}}%
}

\newenvironment{Schunk}{}{}

\newenvironment{Sinput}{\par\smallskip\noindent\ttfamily}{\par\smallskip}
\newenvironment{Soutput}{\par\smallskip\noindent\ttfamily}{\par\smallskip}
\newenvironment{Scode}{\par\smallskip\noindent\ttfamily}{\par\smallskip}

\newenvironment{Sinputverbatim}{\verbatim}{\endverbatim}
\newenvironment{Soutputverbatim}{\verbatim}{\endverbatim}
\newenvironment{Scodeverbatim}{\verbatim}{\endverbatim}

\endinput
%%
%% End of file `Sweave.sty`.
";

            File.WriteAllText(path, content, new UTF8Encoding(false));
        }
    }

    /// <summary>R lacks packages the Notame workflow needs.</summary>
    internal sealed class NotamePackageException : Exception {
        public NotamePackageException(IReadOnlyList<string> missingPackages)
            : base("R is missing packages the Notame preprocessing needs: " + string.Join(", ", missingPackages)) {
            MissingPackages = missingPackages;
        }

        public IReadOnlyList<string> MissingPackages { get; }
    }

    public class CustomPackageDialog : Form {
        private string message;

        public CustomPackageDialog(string message) {
            this.message = message;
            InitializeComponents();
        }

        private void InitializeComponents() {
            this.Text = "Package Installation";
            this.Size = new System.Drawing.Size(700, 420);

            var txtMessage = new TextBox();
            txtMessage.Multiline = true;
            txtMessage.ReadOnly = true;
            txtMessage.ScrollBars = ScrollBars.Both;
            txtMessage.WordWrap = false;
            txtMessage.Font = new System.Drawing.Font(System.Drawing.FontFamily.GenericMonospace, 9f);
            txtMessage.Text = message;
            txtMessage.Dock = DockStyle.Fill;
            this.Controls.Add(txtMessage);
        }

        public static DialogResult ShowDialog(string message) {
            using (var dialog = new CustomPackageDialog(message)) {
                return dialog.ShowDialog();
            }
        }
    }
}
