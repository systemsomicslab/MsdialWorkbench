using CompMs.App.MsdialConsole.Parser;
using CompMs.App.MsdialConsole.Process.MoleculerNetworking;
using CompMs.App.MsdialConsole.Properties;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Help;
using System.IO;
using System.Linq;

namespace CompMs.App.MsdialConsole.Process;

public static class MainProcess
{
    public static int CreateMsp4Model(string inputMspFile, string inputEdgeFile, string outputMspFile) {
        new MoleculerNetworkProcess().GetMsp4Model(inputMspFile, inputEdgeFile, outputMspFile);
        return 1;
    }

    public static void SetGcmsCommand(Command root) {
        var cmd = new Command("gcms", "Run GC-MS data processing");
        var inputOpt = new Option<FileSystemInfo>("--input", "-i") {
            Description = "Input folder containing the files to be processed",
            Required = true,
        };
        var outputOpt = new Option<DirectoryInfo>("--output", "-o")
        {
            Description = "Output folder to save results",
            Required = true,
        };
        var methodOpt = new Option<FileInfo>("--method", "-m")
        {
            Description = "Method file holding processing properties",
            Required = true,
        };
        var projectOpt = new Option<bool>("--project", "-p") {
            Description = "Option to generate .mdproject file to be loaded in MSDIAL5 GUI application"
        };
        cmd.Options.Add(inputOpt);
        cmd.Options.Add(outputOpt);
        cmd.Options.Add(methodOpt);
        cmd.Options.Add(projectOpt);
        inputOpt.Validators.Add(result => {
            var input = result.GetValueOrDefault<FileSystemInfo>();
            if (input is null || !input.Exists) {
                result.AddError("Input path does not exist.");
            }
        });
        outputOpt.Validators.Add(result => {
            var output = result.GetValueOrDefault<DirectoryInfo>();
            if (output is null || File.Exists(output.FullName)) {
                result.AddError("Output path cannot be a file.");
            }
        });
        methodOpt.Validators.Add(result => {
            var methodFile = result.GetValueOrDefault<FileInfo>();
            if (methodFile is null || !methodFile.Exists) {
                result.AddError("Method file does not exist.");
            }
        });
        cmd.SetAction(parseResult => {
            try {
                var input = parseResult.GetRequiredValue(inputOpt);
                var outputFolder = parseResult.GetRequiredValue(outputOpt);
                var methodFile = parseResult.GetRequiredValue(methodOpt);
                var isProjectStore = parseResult.GetValue(projectOpt);
                return new GcmsProcess().Run(input.FullName, outputFolder.FullName, methodFile.FullName, isProjectStore);
            }
            catch (Exception ex) {
                var msg = String.Format("{0} -- {1} -- {2}", ex.InnerException, ex.Message, ex.StackTrace);
                Console.WriteLine(msg);
                return 1;
            }
        });
        root.Add(cmd);
    }

    public static void SetLcmsCommand(Command root) {
        var cmd = new Command("lcms", "Run LC-MS data processing");
        var inputOpt = new Option<FileSystemInfo>("--input", "-i") {
            Description = "Input folder containing the files to be processed",
            Required = true,
        };
        var outputOpt = new Option<DirectoryInfo>("--output", "-o")
        {
            Description = "Output folder to save results",
            Required = true,
        };
        var methodOpt = new Option<FileInfo>("--method", "-m")
        {
            Description = "Method file holding processing properties",
            Required = true,
        };
        var projectOpt = new Option<bool>("--project", "-p") {
            Description = "Option to generate .mdproject file to be loaded in MSDIAL5 GUI application"
        };
        var targetOpt = new Option<float>("--target", "-target", "-t")
        {
            Description = "Option to run as target mode. please set m/z",
        };
        inputOpt.Validators.Add(result => {
            var input = result.GetValueOrDefault<FileSystemInfo>();
            if (input is null || !input.Exists) {
                result.AddError("Input path does not exist.");
            }
        });
        outputOpt.Validators.Add(result => {
            var output = result.GetValueOrDefault<DirectoryInfo>();
            if (output is null || File.Exists(output.FullName)) {
                result.AddError("Output path cannot be a file.");
            }
        });
        methodOpt.Validators.Add(result => {
            var methodFile = result.GetValueOrDefault<FileInfo>();
            if (methodFile is null || !methodFile.Exists) {
                result.AddError("Method file does not exist.");
            }
        });
        cmd.Options.Add(inputOpt);
        cmd.Options.Add(outputOpt);
        cmd.Options.Add(methodOpt);
        cmd.Options.Add(projectOpt);
        cmd.Options.Add(targetOpt);
        cmd.SetAction(parseResult => {
            try {
                var inputFolder = parseResult.GetRequiredValue(inputOpt);
                var outputFolder = parseResult.GetRequiredValue(outputOpt);
                var methodFile = parseResult.GetRequiredValue(methodOpt);
                var isProjectStore = parseResult.GetValue(projectOpt);
                var targetMz = parseResult.GetResult(targetOpt)?.GetValueOrDefault<float>() ?? -1f;
                return new LcmsProcess().Run(inputFolder.FullName, outputFolder.FullName, methodFile.FullName, isProjectStore, targetMz);
            }
            catch (Exception ex) {
                var msg = String.Format("{0} -- {1} -- {2}", ex.InnerException, ex.Message, ex.StackTrace);
                Console.WriteLine(msg);
                return 1;
            }
        });
        root.Add(cmd);
    }


    public static void SetLcimmsCommand(Command root) {
        var cmd = new Command("lcimms", "Run LC-IM-MS data processing");
        var inputOpt = new Option<FileSystemInfo>("--input", "-i") {
            Description = "Input folder containing the files to be processed",
            Required = true,
        };
        var outputOpt = new Option<DirectoryInfo>("--output", "-o")
        {
            Description = "Output folder to save results",
            Required = true,
        };
        var methodOpt = new Option<FileInfo>("--method", "-m")
        {
            Description = "Method file holding processing properties",
            Required = true,
        };
        var projectOpt = new Option<bool>("--project", "-p") {
            Description = "Option to generate .mdproject file to be loaded in MSDIAL5 GUI application"
        };
        var targetOpt = new Option<float>("--target", "-target", "-t")
        {
            Description = "Option to run as target mode. please set m/z",
        };
        inputOpt.Validators.Add(result => {
            var input = result.GetValueOrDefault<FileSystemInfo>();
            if (input is null || !input.Exists) {
                result.AddError("Input path does not exist.");
            }
        });
        outputOpt.Validators.Add(result => {
            var output = result.GetValueOrDefault<DirectoryInfo>();
            if (output is null || File.Exists(output.FullName)) {
                result.AddError("Output path cannot be a file.");
            }
        });
        methodOpt.Validators.Add(result => {
            var methodFile = result.GetValueOrDefault<FileInfo>();
            if (methodFile is null || !methodFile.Exists) {
                result.AddError("Method file does not exist.");
            }
        });
        cmd.Options.Add(inputOpt);
        cmd.Options.Add(outputOpt);
        cmd.Options.Add(methodOpt);
        cmd.Options.Add(projectOpt);
        cmd.Options.Add(targetOpt);
        cmd.SetAction(parseResult => {
            try {
                var inputFolder = parseResult.GetRequiredValue(inputOpt);
                var outputFolder = parseResult.GetRequiredValue(outputOpt);
                var methodFile = parseResult.GetRequiredValue(methodOpt);
                var isProjectStore = parseResult.GetValue(projectOpt);
                var targetMz = parseResult.GetResult(targetOpt)?.GetValueOrDefault<float>() ?? -1f;
                return new LcimmsProcess().Run(inputFolder.FullName, outputFolder.FullName, methodFile.FullName, isProjectStore, targetMz);
            }
            catch (Exception ex) {
                var msg = String.Format("{0} -- {1} -- {2}", ex.InnerException, ex.Message, ex.StackTrace);
                Console.WriteLine(msg);
                return 1;
            }
        });
        root.Add(cmd);
    }

    public static void SetDimsCommand(Command root) {
        var cmd = new Command("dims", "Run DI-MS data processing");
        var inputOpt = new Option<FileSystemInfo>("--input", "-i") {
            Description = "Input folder containing the files to be processed",
            Required = true,
        };
        var outputOpt = new Option<DirectoryInfo>("--output", "-o")
        {
            Description = "Output folder to save results",
            Required = true,
        };
        var methodOpt = new Option<FileInfo>("--method", "-m")
        {
            Description = "Method file holding processing properties",
            Required = true,
        };
        var projectOpt = new Option<bool>("--project", "-p") {
            Description = "Option to generate .mdproject file to be loaded in MSDIAL5 GUI application"
        };
        var targetOpt = new Option<float>("--target", "-target", "-t")
        {
            Description = "Option to run as target mode. please set m/z",
        };
        inputOpt.Validators.Add(result => {
            var input = result.GetValueOrDefault<FileSystemInfo>();
            if (input is null || !input.Exists) {
                result.AddError("Input path does not exist.");
            }
        });
        outputOpt.Validators.Add(result => {
            var output = result.GetValueOrDefault<DirectoryInfo>();
            if (output is null || File.Exists(output.FullName)) {
                result.AddError("Output path cannot be a file.");
            }
        });
        methodOpt.Validators.Add(result => {
            var methodFile = result.GetValueOrDefault<FileInfo>();
            if (methodFile is null || !methodFile.Exists) {
                result.AddError("Method file does not exist.");
            }
        });
        cmd.Options.Add(inputOpt);
        cmd.Options.Add(outputOpt);
        cmd.Options.Add(methodOpt);
        cmd.Options.Add(projectOpt);
        cmd.Options.Add(targetOpt);
        cmd.SetAction(parseResult => {
            try {
                var inputFolder = parseResult.GetRequiredValue(inputOpt);
                var outputFolder = parseResult.GetRequiredValue(outputOpt);
                var methodFile = parseResult.GetRequiredValue(methodOpt);
                var isProjectStore = parseResult.GetValue(projectOpt);
                var targetMz = parseResult.GetResult(targetOpt)?.GetValueOrDefault<float>() ?? -1f;
                return new DimsProcess().Run(inputFolder.FullName, outputFolder.FullName, methodFile.FullName, isProjectStore, targetMz);
            }
            catch (Exception ex) {
                var msg = String.Format("{0} -- {1} -- {2}", ex.InnerException, ex.Message, ex.StackTrace);
                Console.WriteLine(msg);
                return 1;
            }
        });
        root.Add(cmd);
    }

    public static void SetImmsCommand(Command root) {
        var cmd = new Command("imms", "Run IC-MS data processing");
        var inputOpt = new Option<FileSystemInfo>("--input", "-i") {
            Description = "Input folder containing the files to be processed",
            Required = true,
        };
        var outputOpt = new Option<DirectoryInfo>("--output", "-o")
        {
            Description = "Output folder to save results",
            Required = true,
        };
        var methodOpt = new Option<FileInfo>("--method", "-m")
        {
            Description = "Method file holding processing properties",
            Required = true,
        };
        var projectOpt = new Option<bool>("--project", "-p") {
            Description = "Option to generate .mdproject file to be loaded in MSDIAL5 GUI application"
        };
        var targetOpt = new Option<float>("--target", "-target", "-t")
        {
            Description = "Option to run as target mode. please set m/z",
        };
        inputOpt.Validators.Add(result => {
            var input = result.GetValueOrDefault<FileSystemInfo>();
            if (input is null || !input.Exists) {
                result.AddError("Input path does not exist.");
            }
        });
        outputOpt.Validators.Add(result => {
            var output = result.GetValueOrDefault<DirectoryInfo>();
            if (output is null || File.Exists(output.FullName)) {
                result.AddError("Output path cannot be a file.");
            }
        });
        methodOpt.Validators.Add(result => {
            var methodFile = result.GetValueOrDefault<FileInfo>();
            if (methodFile is null || !methodFile.Exists) {
                result.AddError("Method file does not exist.");
            }
        });
        cmd.Options.Add(inputOpt);
        cmd.Options.Add(outputOpt);
        cmd.Options.Add(methodOpt);
        cmd.Options.Add(projectOpt);
        cmd.Options.Add(targetOpt);
        cmd.SetAction(parseResult => {
            try {
                var inputFolder = parseResult.GetRequiredValue(inputOpt);
                var outputFolder = parseResult.GetRequiredValue(outputOpt);
                var methodFile = parseResult.GetRequiredValue(methodOpt);
                var isProjectStore = parseResult.GetValue(projectOpt);
                var targetMz = parseResult.GetResult(targetOpt)?.GetValueOrDefault<float>() ?? -1f;
                return new ImmsProcess().Run(inputFolder.FullName, outputFolder.FullName, methodFile.FullName, isProjectStore, targetMz);
            }
            catch (Exception ex) {
                var msg = String.Format("{0} -- {1} -- {2}", ex.InnerException, ex.Message, ex.StackTrace);
                Console.WriteLine(msg);
                return 1;
            }
        });
        root.Add(cmd);
    }

    public static void SetMsnCommand(Command root) {
        var cmd = new Command("msn", "Run molecular networking data processing");
        var inputOpt = new Option<string>("--input", "-i")
        {
            Description = "Input folder or file. Supported files: .msp, .mddata, .pai, .pai2, .arf, and .arf2.",
            Required = true,
        };
        var outputOpt = new Option<string>("--output", "-o")
        {
            Description = "Output folder to save results or a single result file",
            Required = true,
        };
        var methodOpt = new Option<FileInfo>("--method", "-m")
        {
            Description = "Method file holding processing properties",
            Required = true,
        };
        var targetFileOpt = new Option<FileInfo>("--targetFile", "-t")
        {
            Description = "Optional target MSP library file for mapping input compounds.",
        };
        var spectrumFileOpt = new Option<FileInfo>("--spectrum", "-s")
        {
            Description = "Associated MS/MS spectrum file for binary peak-list input. Supported files: .dcl.",
        };
        var analysisFileOpt = new Option<string>("--analysis-file")
        {
            Description = "Analysis file name for .mddata input. Takes precedence over --alignment when both are specified.",
        };
        var alignmentOpt = new Option<bool>("--alignment")
        {
            Description = "Use the project's alignment result for .mddata input. Ignored when --analysis-file is specified.",
            DefaultValueFactory = _ => true,
        };
        var ionmodeOpt = new Option<string>("--ionmode", "-ionmode")
        {
            Description = "Ion mode for MS/MS data processing. Valid options are 'Positive' or 'Negative'.",
            DefaultValueFactory = _ => "Positive",
        };
        var overwriteOpt = new Option<bool>("--overwrite", "-overwrite")
        {
            Description = "Option to overwrite existing output files. Default is false.",
            DefaultValueFactory = _ => false,
        };
        var allEdgeExportOpt = new Option<bool>("--all-edge-export", "-a")
        {
            Description = "Export all edges in the molecular network. [default: false]",
            DefaultValueFactory = _ => false,
        };
        var formatOpt = new Option<string>("--format")
        {
            Description = "Output format: v1 (default; ID, ID, scores) or v2 (metadata lines, record ID columns, one score and matched column per kernel identity).",
            DefaultValueFactory = _ => "v1",
        };
        var allPairsOpt = new Option<bool>("--all-pairs")
        {
            Description = "v2 only: write every scored pair, ignoring MinimumPeakMatch and the similarity cutoff. MaxPrecursorDifference still applies.",
        };
        var kernelsOpt = new Option<string>("--kernels")
        {
            Description = "v2 only: comma-separated kernels (ModifiedCosine, ModDotLegacy, Bonanza, All). The first is primary. Default: the method file's MsmsSimilarityCalc.",
        };
        var redactOpt = new Option<string>("--redact-identity")
        {
            Description = "v2 only: blank the name, InChIKey and database ID of the query, the target, both, or none. In self mode any value blanks both sides.",
            DefaultValueFactory = _ => "none",
        };
        var threadsOpt = new Option<int>("--threads")
        {
            Description = "v2 only: threads that score query blocks in parallel. The output is identical for any value.",
            DefaultValueFactory = _ => 1,
        };
        var helpOpt = root.Options.OfType<HelpOption>().Single();
        inputOpt.Validators.Add(result => {
            var input = result.GetValueOrDefault<string>();
            if (string.IsNullOrEmpty(input) || (!File.Exists(input) && !Directory.Exists(input))) {
                result.AddError("Input path does not exist.");
            }
        });
        methodOpt.Validators.Add(result => {
            var methodFile = result.GetValueOrDefault<FileInfo>();
            if (methodFile is null || !methodFile.Exists) {
                result.AddError("Method file does not exist.");
            }
        });
        cmd.Options.Add(inputOpt);
        cmd.Options.Add(outputOpt);
        cmd.Options.Add(methodOpt);
        cmd.Options.Add(targetFileOpt);
        cmd.Options.Add(spectrumFileOpt);
        cmd.Options.Add(analysisFileOpt);
        cmd.Options.Add(alignmentOpt);
        cmd.Options.Add(ionmodeOpt);
        cmd.Options.Add(overwriteOpt);
        cmd.Options.Add(allEdgeExportOpt);
        cmd.Options.Add(formatOpt);
        cmd.Options.Add(allPairsOpt);
        cmd.Options.Add(kernelsOpt);
        cmd.Options.Add(redactOpt);
        cmd.Options.Add(threadsOpt);
        cmd.Validators.Add(result => {
            if (result.GetResult(helpOpt) is not null) {
                return;
            }

            var input = result.GetRequiredValue(inputOpt);
            var output = result.GetRequiredValue(outputOpt);

            if (!Directory.Exists(input) && !File.Exists(input)) {
                result.AddError("Input path does not exist.");
            }
            if (Directory.Exists(input) && File.Exists(output)) {
                result.AddError("Output path cannot be a file when input is a directory.");
            }
            if (File.Exists(input) && Directory.Exists(output)) {
                result.AddError("Output path cannot be a directory when input is a file.");
            }
        });
        ionmodeOpt.Validators.Add(result => {
            var value = result.GetValueOrDefault<string>();
            if (value is not ("Positive" or "Negative")) {
                result.AddError("Ion mode must be Positive or Negative.");
            }
        });
        cmd.SetAction(parseResult => {
            try {
                var input = Path.GetFullPath(parseResult.GetRequiredValue(inputOpt));
                var output = Path.GetFullPath(parseResult.GetRequiredValue(outputOpt));
                var methodFile = parseResult.GetRequiredValue(methodOpt);
                var targetFile = parseResult.GetValue(targetFileOpt);
                var spectrumFile = parseResult.GetValue(spectrumFileOpt);
                var analysisFileName = parseResult.GetValue(analysisFileOpt);
                var alignment = parseResult.GetValue(alignmentOpt);
                var ionmode = parseResult.GetRequiredValue(ionmodeOpt);
                var overwrite = parseResult.GetValue(overwriteOpt);
                var allEdgeExport = parseResult.GetValue(allEdgeExportOpt);
                var v2 = ReadMsnV2Options(parseResult.GetValue(formatOpt), parseResult.GetValue(allPairsOpt),
                    parseResult.GetValue(kernelsOpt), parseResult.GetValue(redactOpt), parseResult.GetValue(threadsOpt));
                if (v2 is not null && Directory.Exists(input)) {
                    throw new MsnUsageException("--format v2 takes a single input file, not an input folder.");
                }

                // A target the run cannot use must stop it: networking the input against itself
                // instead would write a plausible file that answers a different question.
                if (targetFile is not null) {
                    if (!targetFile.Exists) {
                        Console.Error.WriteLine($"The --targetFile {targetFile.FullName} does not exist.");
                        return MsnExitCode.UnusableInput;
                    }
                    if (Directory.Exists(input)) {
                        Console.Error.WriteLine("--targetFile is used only with a single MSP input file, not with an input folder.");
                        return MsnExitCode.UnusableInput;
                    }
                }

                if (Directory.Exists(input)) {
                    if (allEdgeExport){
                        return new MoleculerNetworkProcess().Run4AllEdgeGeneration(input, output, methodFile.FullName, ionmode, overwrite);
                    }
                    else
                        return new MoleculerNetworkProcess().Run(input, output, methodFile.FullName, ionmode, overwrite);
                }
                else {
                    var isProject = Path.GetExtension(input).Equals(".mddata", StringComparison.OrdinalIgnoreCase);
                    var binaryInput = Path.GetExtension(input).Equals(".pai", StringComparison.OrdinalIgnoreCase)
                        || Path.GetExtension(input).Equals(".pai2", StringComparison.OrdinalIgnoreCase)
                        || Path.GetExtension(input).Equals(".arf", StringComparison.OrdinalIgnoreCase)
                        || Path.GetExtension(input).Equals(".arf2", StringComparison.OrdinalIgnoreCase);
                    if ((isProject || binaryInput) && targetFile is not null) {
                        Console.Error.WriteLine("--targetFile is used only with an MSP input file.");
                        return MsnExitCode.UnusableInput;
                    }
                    if (isProject) {
                        return new MoleculerNetworkProcess().Run4Project(input, output, methodFile.FullName, ionmode, alignment, analysisFileName, v2);
                    }
                    if (binaryInput) {
                        if (spectrumFile is null || !spectrumFile.Exists) {
                            throw new FileNotFoundException("A --spectrum file is required for binary peak-list input.", spectrumFile?.FullName);
                        }
                        return new MoleculerNetworkProcess().Run4Binary(input, spectrumFile.FullName, output, methodFile.FullName, ionmode, v2);
                    }
                    if (spectrumFile != null) {
                        throw new ArgumentException("--spectrum is only used with .pai, .pai2, .arf, or .arf2 input.");
                    }
                    if (v2 is not null) {
                        return new MoleculerNetworkProcess().RunV2(input, targetFile?.FullName, output, methodFile.FullName, ionmode, v2);
                    }
                    if (targetFile != null) {
                        return new MoleculerNetworkProcess().Map2TargetFile(targetFile.FullName, input, methodFile.FullName, output, ionmode);
                    }
                    else {
                        return new MoleculerNetworkProcess().Run4Onefile(input, output, methodFile.FullName, ionmode);
                    }
                }
            }
            catch (MethodFileException ex) {
                Console.Error.WriteLine(ex.Message);
                return MsnExitCode.UnusableInput;
            }
            catch (MsnUsageException ex) {
                Console.Error.WriteLine(ex.Message);
                return MsnExitCode.UnusableInput;
            }
            catch (Exception ex) {
                var msg = String.Format("{0} -- {1} -- {2}", ex.InnerException, ex.Message, ex.StackTrace);
                Console.Error.WriteLine(msg);
                return MsnExitCode.Failure;
            }
        });

        root.Add(cmd);
    }

    /// <summary>
    /// The v2 options, or null for v1. Options that only v2 has are refused with v1, rather than
    /// ignored.
    /// </summary>
    internal static MsnV2Options? ReadMsnV2Options(string? format, bool allPairs, string? kernels, string? redact, int threads) {
        var isV2 = string.Equals(format, "v2", StringComparison.OrdinalIgnoreCase);
        if (!isV2 && !string.Equals(format ?? "v1", "v1", StringComparison.OrdinalIgnoreCase)) {
            throw new MsnUsageException($"Unknown --format '{format}'. Use v1 or v2.");
        }
        if (!Enum.TryParse<MsnRedaction>(redact ?? "none", ignoreCase: true, out var redaction)
            || !Enum.IsDefined(typeof(MsnRedaction), redaction)
            || (redact ?? "none").Any(char.IsDigit)) {
            throw new MsnUsageException($"Unknown --redact-identity '{redact}'. Use none, query, target or both.");
        }
        if (!isV2) {
            if (allPairs || !string.IsNullOrEmpty(kernels) || redaction != MsnRedaction.None || threads != 1) {
                throw new MsnUsageException("--all-pairs, --kernels, --redact-identity and --threads are used only with --format v2.");
            }
            return null;
        }
        if (threads < 1) {
            throw new MsnUsageException("--threads must be at least 1.");
        }
        List<MsmsSimilarityCalc>? calcs = null;
        if (!string.IsNullOrWhiteSpace(kernels)) {
            calcs = new List<MsmsSimilarityCalc>();
            foreach (var name in kernels!.Split(',')) {
                var outcome = ConfigParser.SimilarityCalc(name, calcs.Add);
                if (!outcome.IsApplied) {
                    throw new MsnUsageException($"Unknown kernel '{name.Trim()}' in --kernels. Use {string.Join(", ", Enum.GetNames(typeof(MsmsSimilarityCalc)))}.");
                }
            }
        }
        return new MsnV2Options { AllPairs = allPairs, Kernels = calcs, Redaction = redaction, Threads = threads, };
    }

    /// <summary>
    /// Write a method file for one processing mode, holding the built-in defaults.
    /// </summary>
    /// <remarks>
    /// Written to standard output unless --output is given, so it can be piped or redirected. An
    /// existing file is not replaced without --force: the file most likely to be named is the method
    /// file somebody has already edited.
    /// </remarks>
    public static void SetTemplateCommand(Command root) {
        var cmd = new Command("template", "Write a method file template holding the built-in defaults for a processing mode");
        var modes = Enum.GetValues(typeof(MethodFileMode)).Cast<MethodFileMode>()
            .ToDictionary(MethodFileTemplate.CommandName, StringComparer.OrdinalIgnoreCase);
        var mode = new Argument<string>("mode") {
            Description = "The command the method file is for: " + string.Join(", ", modes.Keys),
        };
        // Not AcceptOnlyFromAmong: it compares case-sensitively, and "LCMS" names the same command.
        mode.Validators.Add(result => {
            var value = result.GetValueOrDefault<string>();
            if (value is null || !modes.ContainsKey(value)) {
                result.AddError($"Unknown mode '{value}'. Use one of: {string.Join(", ", modes.Keys)}.");
            }
        });
        var output = new Option<FileInfo>("--output", "-o") {
            Description = "File to write the template to. Standard output when omitted",
        };
        var force = new Option<bool>("--force", "-f") {
            Description = "Replace the output file if it already exists",
        };
        var ionMode = new Option<IonMode>("--ionmode") {
            Description = "Positive or Negative. The searched adduct follows it ([M+H]+ or [M-H]-)",
            DefaultValueFactory = _ => IonMode.Positive,
        };
        // IonMode also has Both, and the enum parser takes any number. The method-file reader
        // understands neither, so a template holding one would run as Positive without saying so.
        ionMode.Validators.Add(result => {
            var value = result.GetValueOrDefault<IonMode>();
            if (value != IonMode.Positive && value != IonMode.Negative) {
                result.AddError("--ionmode must be Positive or Negative.");
            }
        });
        cmd.Arguments.Add(mode);
        cmd.Options.Add(output);
        cmd.Options.Add(force);
        cmd.Options.Add(ionMode);
        cmd.SetAction(parseResult => {
            var text = MethodFileTemplate.Create(modes[parseResult.GetRequiredValue(mode)], parseResult.GetValue(ionMode));
            var file = parseResult.GetValue(output);
            if (file is null) {
                Console.Out.Write(text);
                return 0;
            }
            if (file.Exists && !parseResult.GetValue(force)) {
                Console.Error.WriteLine($"{file.FullName} already exists. Use --force to replace it.");
                return 1;
            }
            try {
                file.Directory?.Create();
                File.WriteAllText(file.FullName, text, new System.Text.UTF8Encoding(false));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) {
                Console.Error.WriteLine($"The template could not be written to {file.FullName}: {ex.Message}");
                return 1;
            }
            Console.WriteLine($"Method file template written to {file.FullName}");
            return 0;
        });
        root.Subcommands.Add(cmd);
    }

    public static void SetNormalizeCommand(Command root) {
        var cmd = new Command(
            "normalize",
            "Normalize an aligned result against internal standards and export the matrix");
        var input = new Option<FileInfo>("--input", "-i") { Required = true };
        input.Description = "MS-DIAL project file holding the alignment to normalize";
        var standards = new Option<FileInfo>("--standards", "-s") { Required = true };
        standards.Description = "Table of internal standards: StandardName, TargetClass, Concentration, optional PeakID, AssignName (true exports the peak at PeakID under StandardName, as an internal standard assignment, with none of the annotation it had; a peak not already carrying that name is renamed only with --allow-mismatched-peak-ids), DilutionRate, MolecularWeight";
        var output = new Option<DirectoryInfo>("--output", "-o") { Required = true };
        output.Description = "Directory to write the raw and normalized alignment matrices into";
        var unit = new Option<IonAbundanceUnit>("--unit", "-u") {
            DefaultValueFactory = _ => IonAbundanceUnit.NormalizedByInternalStandardPeakHeight,
        };
        unit.Description = "Unit of the normalized abundance, e.g. pmol_per_microL_plasma";
        var alignment = new Option<int>("--alignment") { DefaultValueFactory = _ => 0 };
        alignment.Description = "Index of the alignment result within the project";
        var dilution = new Option<bool>("--apply-dilution-factor", "-d");
        dilution.Description = "Multiply each file's normalized values by its dilution factor after normalizing";
        var allowUnresolved = new Option<bool>("--allow-unresolved-standards");
        allowUnresolved.Description = "Continue when a standard is not found, leaving the classes no other standard quantifies without a concentration";
        var allowMismatched = new Option<bool>("--allow-mismatched-peak-ids");
        allowMismatched.Description = "Continue when a standard's alignment ID holds a different compound or none; needed for AssignName to rename a peak";
        var peakIdsOnly = new Option<bool>("--peak-ids-only");
        peakIdsOnly.Description = "Take each standard only from its PeakID: one without a PeakID, or with one not in the alignment, is unresolved and never looked up by name";
        cmd.Options.Add(input);
        cmd.Options.Add(standards);
        cmd.Options.Add(output);
        cmd.Options.Add(unit);
        cmd.Options.Add(alignment);
        cmd.Options.Add(dilution);
        cmd.Options.Add(allowUnresolved);
        cmd.Options.Add(allowMismatched);
        cmd.Options.Add(peakIdsOnly);
        cmd.SetAction(parseResult => new NormalizationProcess().Run(
            parseResult.GetRequiredValue(input),
            parseResult.GetRequiredValue(standards),
            parseResult.GetRequiredValue(output),
            parseResult.GetValue(unit),
            parseResult.GetValue(alignment),
            parseResult.GetValue(dilution),
            parseResult.GetValue(allowUnresolved),
            parseResult.GetValue(allowMismatched),
            parseResult.GetValue(peakIdsOnly)));
        root.Subcommands.Add(cmd);
    }

    public static void SetEicCommand(Command root) {
        var eic = new Command("eic", "Export extracted ion chromatograms");
        var raw = new Command("raw", "Export EICs from a raw data file");
        var rawInput = new Option<FileInfo>("--input", "-i") { Required = true };
        var rawOutput = new Option<FileInfo>("--output", "-o") { Required = true };
        var targets = new Option<double[]>("--target", "-target") { Required = true };
        targets.Arity = ArgumentArity.OneOrMore;
        var acquisitionType = new Option<AcquisitionType>("--acquisitiontype") { DefaultValueFactory = _ => AcquisitionType.DDA };
        raw.Options.Add(rawInput);
        raw.Options.Add(rawOutput);
        raw.Options.Add(targets);
        raw.Options.Add(acquisitionType);
        raw.SetAction(parseResult => {
            return new EicProcess().RunRaw(
                parseResult.GetRequiredValue(rawInput),
                parseResult.GetRequiredValue(rawOutput),
                parseResult.GetValue(targets),
                parseResult.GetValue(acquisitionType));
        });

        var project = new Command("project", "Export EICs from an MS-DIAL project");
        var projectInput = new Option<FileInfo>("--input", "-i") { Required = true };
        var projectOutput = new Option<FileInfo>("--output", "-o") { Required = true };
        var format = new Option<string>("--format") { DefaultValueFactory = _ => "json" };
        project.Options.Add(projectInput);
        project.Options.Add(projectOutput);
        project.Options.Add(format);
        project.SetAction(parseResult => new EicProcess().RunProject(
            parseResult.GetRequiredValue(projectInput),
            parseResult.GetRequiredValue(projectOutput),
            parseResult.GetValue(format)));

        eic.Subcommands.Add(raw);
        eic.Subcommands.Add(project);
        root.Subcommands.Add(eic);
    }

    public static void SetRtCorrectionCommand(Command root) {
        root.Subcommands.Add(CreateRtCorrectionCommand(hidden: false));

        // Keep the pre-release command path working without advertising it in help.
        var eic = root.Subcommands.FirstOrDefault(command => command.Name == "eic");
        eic?.Subcommands.Add(CreateRtCorrectionCommand(hidden: true));
    }

    private static Command CreateRtCorrectionCommand(bool hidden) {
        var rtCorrection = new Command(
            "rtcorrection",
            "Prepare retention-time correction anchor EICs and correction profiles") {
            Hidden = hidden,
        };
        var rtInputs = new Option<FileSystemInfo[]>("--input", "-i") { Required = true };
        rtInputs.Arity = ArgumentArity.OneOrMore;
        rtInputs.Description = "Raw data files, vendor data directories, or a directory containing raw data";
        var rtLibrary = new Option<FileInfo>("--library") {
            Description = "MS-DIAL text library containing the retention-time correction anchors",
            Required = true,
        };
        var rtOutput = new Option<FileInfo>("--output", "-o") {
            Description = "Output CSV containing anchor EICs and corrected retention times",
            Required = true,
        };
        var rtMethod = new Option<FileInfo?>("--method", "-m") {
            Description = "Optional MS-DIAL LC-MS parameter file",
        };
        var rtSelection = new Option<FileInfo?>("--selection") {
            Description = "Optional reviewed anchor-peak selection TSV",
        };
        var rtIonMode = new Option<IonMode>("--ionmode") {
            Description = "Ion mode used to extract the anchor EICs",
            DefaultValueFactory = _ => IonMode.Positive,
        };
        var rtAcquisitionType = new Option<AcquisitionType>("--acquisitiontype") {
            Description = "Acquisition type of the input data",
            DefaultValueFactory = _ => AcquisitionType.DDA,
        };
        rtCorrection.Options.Add(rtInputs);
        rtCorrection.Options.Add(rtLibrary);
        rtCorrection.Options.Add(rtOutput);
        rtCorrection.Options.Add(rtMethod);
        rtCorrection.Options.Add(rtSelection);
        rtCorrection.Options.Add(rtIonMode);
        rtCorrection.Options.Add(rtAcquisitionType);
        rtCorrection.SetAction(parseResult => new RtCorrectionProcess().Run(
            parseResult.GetRequiredValue(rtInputs),
            parseResult.GetRequiredValue(rtLibrary),
            parseResult.GetRequiredValue(rtOutput),
            parseResult.GetValue(rtMethod),
            parseResult.GetValue(rtSelection),
            parseResult.GetValue(rtIonMode),
            parseResult.GetValue(rtAcquisitionType)));

        return rtCorrection;
    }

    public static void SetImageGenerationCommand(Command root) {
        var imagegen = new Command("imagegen", "Generate imaging output from an MS-DIAL project");
        var input = new Option<FileInfo>("--input", "-i") {
            Description = "MS-DIAL project file containing imaging analysis results",
            Required = true,
        };
        imagegen.Options.Add(input);
        imagegen.SetAction(async (parseResult, token) => {
            try {
                await new ImageGenerationProcess().RunAsync(parseResult.GetRequiredValue(input).FullName, token).ConfigureAwait(false);
                return 0;
            }
            catch (OperationCanceledException) {
                Console.WriteLine("Cancelled.");
                return 1;
            }
            catch (Exception ex) {
                var msg = String.Format("{0} -- {1} -- {2}", ex.InnerException, ex.Message, ex.StackTrace);
                Console.WriteLine(msg);
                return 1;
            }
        });
        root.Subcommands.Add(imagegen);
    }

}
