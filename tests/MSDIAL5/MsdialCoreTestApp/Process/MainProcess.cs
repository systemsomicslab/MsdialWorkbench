using CompMs.App.MsdialConsole.Process.MoleculerNetworking;
using CompMs.App.MsdialConsole.Properties;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;

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
            Description = "Input folder containing the files to be processed or a single file",
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
            Description = "Option",
        };
        var ionmodeOpt = new Option<string>("--ionmode", "-ionmode")
        {
            Description = "Ion mode for MS/MS data processing. Valid options are 'Positive' or 'Negative'",
            DefaultValueFactory = _ => "Positive",
        };
        var overwriteOpt = new Option<bool>("--overwrite", "-overwrite")
        {
            Description = "Option to overwrite existing output files. Default is false.",
            DefaultValueFactory = _ => false,
        };
        var allEdgeExportOpt = new Option<bool>("--all-edge-export", "-a")
        {
            Description = "Option to export all edges in the molecular network. Default is false.",
            DefaultValueFactory = _ => false,
        };
        inputOpt.Validators.Add(result => {
            var input = result.GetValueOrDefault<FileSystemInfo>();
            if (input is null || !input.Exists) {
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
        cmd.Options.Add(ionmodeOpt);
        cmd.Options.Add(overwriteOpt);
        cmd.Options.Add(allEdgeExportOpt);
        cmd.Validators.Add(result => {
            string input = result.GetRequiredValue(inputOpt);
            string output = result.GetRequiredValue(outputOpt);

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
                var ionmode = parseResult.GetRequiredValue(ionmodeOpt);
                var overwrite = parseResult.GetValue(overwriteOpt);
                var allEdgeExport = parseResult.GetValue(allEdgeExportOpt);

                if (Directory.Exists(input)) {
                    if (allEdgeExport){
                        return new MoleculerNetworkProcess().Run4AllEdgeGeneration(input, output, methodFile.FullName, ionmode, overwrite);
                    }
                    else
                        return new MoleculerNetworkProcess().Run(input, output, methodFile.FullName, ionmode, overwrite);
                }
                else {
                    if (targetFile != null && targetFile.Exists) {
                        return new MoleculerNetworkProcess().Map2TargetFile(targetFile.FullName, input, methodFile.FullName, output, ionmode);
                    }
                    else {
                        return new MoleculerNetworkProcess().Run4Onefile(input, output, methodFile.FullName, ionmode);
                    }
                }
            }
            catch (Exception ex) {
                var msg = String.Format("{0} -- {1} -- {2}", ex.InnerException, ex.Message, ex.StackTrace);
                Console.WriteLine(msg);
                return 1;
            }
        });

        root.Add(cmd);
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

}
