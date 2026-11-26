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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Statistics {
    internal sealed class Notame : DisposableModelBase {
        /// <summary>R packages the scripts in Resources (Notame.R, Report.R) use.</summary>
        private static readonly string[] _requiredPackages = [
            "notame", "SummarizedExperiment", "BiocParallel", "pcaMethods", "missForest", "MUVR",
            "doParallel", "foreach", "dplyr", "ggplot2", "openxlsx", "cowplot", "ggsignif", "futile.logger",
        ];

        public static string PackageInstallScript =>
            GetPackageInstallScript([.. _requiredPackages, "Cairo", "tinytex"], installTinyTex: true);

        private static string GetPackageInstallScript(IEnumerable<string> packages, bool installTinyTex) {
            using var stream = typeof(Notame).Assembly.GetManifestResourceStream("NotamePackageInstall.R")
                ?? throw new InvalidOperationException("The Notame package installation script is missing.");
            using var reader = new StreamReader(stream);
            var packageNames = string.Join(", ", packages.Select(NotameRSession.ToRString));
            var script = reader.ReadToEnd().Replace("\r\n", "\n").Replace("\n", "\r\n");
            return script + $"\r\ninstall_notame_packages(c({packageNames}), {(installTinyTex ? "TRUE" : "FALSE")})\r\n";
        }

        public static async Task InstallPackagesAsync(NotamePackageException requirements) {
            // Use a fresh process because the embedded R keeps loaded namespaces until restart.
            var rscript = Path.Combine(requirements.RHome, "bin", "x64", "Rscript.exe");
            var script = GetPackageInstallScript(requirements.Packages, requirements.InstallTinyTex);
            using var process = Process.Start(new ProcessStartInfo(rscript, "-") {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }) ?? throw new InvalidOperationException("Could not start Rscript.");

            var output = process.StandardOutput.ReadToEndAsync();
            var errors = process.StandardError.ReadToEndAsync();
            using (var input = process.StandardInput) {
                await input.WriteAsync(script).ConfigureAwait(false);
            }
            await Task.WhenAll(output, errors).ConfigureAwait(false);
            process.WaitForExit();
            if (process.ExitCode != 0) {
                throw new InvalidOperationException($"Rscript exited with code {process.ExitCode}.\r\n\r\n{errors.Result}\r\n{output.Result}");
            }
        }

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
            var rHome = Path.GetFullPath(RDirectory);
            var exportReport = this.exportReport;
            var missing = await NotameRSession.RunAsync(rHome, engine => CheckPackages(engine, rHome, exportReport)).ConfigureAwait(false);
            if (Settings.Default.RHome != rHome) {
                Settings.Default.RHome = rHome;
                Settings.Default.Save();
            }
            if (missing is not null) {
                throw missing;
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

        private static NotamePackageException? CheckPackages(REngine engine, string rHome, bool exportReport) {
            IEnumerable<string> packages = exportReport ? [.. _requiredPackages, "tinytex"] : _requiredPackages;
            var oldNotameVersion = engine.Evaluate(
                "tryCatch(if (utils::packageVersion('notame') < '0.99.0') as.character(utils::packageVersion('notame')) else '', error = function(e) '')")
                .AsCharacter().First();
            var missing = NotameRSession.MissingPackages(engine, packages.Where(package => package != "notame" || oldNotameVersion.Length == 0)).ToList();
            if (oldNotameVersion.Length > 0) {
                missing.Insert(0, "notame");
            }
            var installTinyTex = exportReport
                && !engine.Evaluate("nzchar(Sys.which('pdflatex')) || (requireNamespace('tinytex', quietly = TRUE) && tinytex::is_tinytex())")
                    .AsLogical().First();
            if (missing.Count == 0 && !installTinyTex) {
                return null;
            }
            return new NotamePackageException(rHome, missing, oldNotameVersion, installTinyTex);
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
        public NotamePackageException(string rHome, IReadOnlyList<string> packages, string oldNotameVersion, bool installTinyTex)
            : base("The Notame preprocessing needs additional packages.") {
            RHome = rHome;
            Packages = packages;
            OldNotameVersion = oldNotameVersion;
            InstallTinyTex = installTinyTex;
        }

        public string RHome { get; }
        public IReadOnlyList<string> Packages { get; }
        public string OldNotameVersion { get; }
        public bool InstallTinyTex { get; }
    }
}
