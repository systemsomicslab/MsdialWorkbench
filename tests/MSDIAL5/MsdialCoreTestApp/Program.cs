using CompMs.App.MsdialConsole.Process;
using System;
using System.Collections.Generic;
using System.CommandLine;

namespace CompMs.App.MsdialConsole;
class Program {
    public static int Main(string[] args) {
        // gcms
        // args = new string[] {
        //     "gcms"
        //     , "-i"
        //     , @"D:\msdial_test\Msdial\out\GCMS"
        //     , "-o"
        //     , @"D:\msdial_test\Msdial\out\GCMS"
        //     , "-m"
        //     , @"D:\msdial_test\Msdial\out\GCMS\Msdial-GCMS-Param.txt"
        //     , "-p"
        // };

        // lcms
        //args = new string[]
        //{
        //    "lcms"
        //    , "-i"
        //    , @"E:\0_SourceCode\MsdialWorkbenchDemo\console_fastlc_demo"
        //    , "-o"
        //    , @"E:\0_SourceCode\MsdialWorkbenchDemo\console_fastlc_demo"
        //    , "-m"
        //    , @"E:\0_SourceCode\MsdialWorkbenchDemo\console_fastlc_demo\lib\msdial_console_param4lipidomics.txt"
        //    , "-p"
        //};

        // lcms csv file import
        //args = new string[]
        //{
        //    "lcms"
        //    , "-i"
        //    , @"D:\0_SourceCode\MsdialWorkbenchDemo\mzml_gnps\consoleapp_demo_csvimportfiles.csv"
        //    , "-o"
        //    , @"D:\0_SourceCode\MsdialWorkbenchDemo\mzml_gnps"
        //    , "-m"
        //    , @"D:\0_SourceCode\MsdialWorkbenchDemo\mzml_gnps\msdial_console_param4metabolomics.txt"
        //    , "-p"
        //};

        // dims
        // args = new string[]
        // {
        //     "dims"
        //     , "-i"
        //     , @"\\mtbdt\Mtb_info\data\msdial_test\MSMSALL_Positive"
        //     , "-o"
        //     , @"\\mtbdt\Mtb_info\data\msdial_test\MSMSALL_Positive"
        //     , "-m"
        //     , @"\\mtbdt\Mtb_info\data\msdial_test\MSMSALL_Positive\dims_param.txt"
        //     , "-p"
        // };

        // imms
        // args = new string[]
        // {
        //     "imms"
        //     , "-i"
        //     , @"D:\msdial_test\Msdial\out\infusion_neg_timsON_pasef_ibf"
        //     , "-o"
        //     , @"D:\msdial_test\Msdial\out\infusion_neg_timsON_pasef_ibf"
        //     , "-m"
        //     , @"D:\msdial_test\Msdial\out\infusion_neg_timsON_pasef_ibf\Msdial-imms-Param.txt"
        //     , "-p"
        // };

        // lcimms
        //args = new string[] {
        //    "lcimms"
        //    , "-i"
        //    , @"D:\msdial_test\Msdial\out\IonMobilityDemoFiles\IonMobilityDemoFiles\IBF"
        //    , "-o"
        //    , @"D:\msdial_test\Msdial\out\IonMobilityDemoFiles\IonMobilityDemoFiles\IBF"
        //    , "-m"
        //    , @"D:\msdial_test\Msdial\out\IonMobilityDemoFiles\IonMobilityDemoFiles\IBF\lcimms_param.txt"
        //    , "-p"
        //};

        // moleculer networking
        //args = new string[] {
        //    "msn"
        //    , "-i"
        //    , @"E:\6_Projects\PROJECT_MsMachineLearning\data\ogawa_20240123\input_msn_neg"
        //    , "-o"
        //    , @"E:\6_Projects\PROJECT_MsMachineLearning\data\ogawa_20240123\output_msn_neg"
        //    , "-m"
        //    , @"E:\6_Projects\PROJECT_MsMachineLearning\data\ogawa_20240123\msn_param_20240127.txt"
        //    , "-ionmode"
        //    , "Negative"
        //    , "-overwrite"
        //    , "false"
        //};


        //args = new string[] {
        //    "msn"
        //    , "-i"
        //    , @"\\165.93.102.222\Public\MetaboBankPeakPick\ogawa_20240123\msn_msp_neg\MSMS-Public_experimentspectra-neg-VS19.msp"
        //    , "-o"
        //    , @"\\165.93.102.222\Public\MetaboBankPeakPick\ogawa_20240123\msn_msp_neg\MSMS-Public_experimentspectra-neg-VS19_v2.edge"
        //    , "-m"
        //    , @"\\165.93.102.222\Public\MetaboBankPeakPick\ogawa_20240123\msn_param_20240401.txt"
        //    , "-ionmode"
        //    , "Negative"
        //};

        //args = new string[] {
        //    "msn"
        //    , "-i"
        //    , @"E:\6_Projects\PROJECT_MsMachineLearning\msn\aging_lipidome\data\brain_test_neg.msp"
        //    , "-t"
        //    , @"E:\6_Projects\PROJECT_MsMachineLearning\msn\aging_lipidome\data\aging_lipidome_neg_for_model.msp"
        //    , "-o"
        //    , @"E:\6_Projects\PROJECT_MsMachineLearning\msn\aging_lipidome\data\brain2model.edge"
        //    , "-m"
        //    , @"E:\6_Projects\PROJECT_MsMachineLearning\msn\aging_lipidome\msn_param_for_mapping.txt"
        //    , "-ionmode"
        //    , "Negative"
        //};

        //args = new string[] {
        //    "msn"
        //    , "-i"
        //    , @"E:\6_Projects\PROJECT_MsMachineLearning\msn\msp\neg\casmi2022_neg.msp"
        //    , "-t"
        //    , @"E:\6_Projects\PROJECT_MsMachineLearning\msn\msp\neg\MSMS-Public_experimentspectra-neg-VS19-curated.msp"
        //    , "-o"
        //    , @"E:\6_Projects\PROJECT_MsMachineLearning\msn\msp\neg\casmi2model.edge"
        //    , "-m"
        //    , @"E:\6_Projects\PROJECT_MsMachineLearning\msn\msp\neg\msn_param_for_mapping.txt"
        //    , "-ionmode"
        //    , "Negative"
        //};

        //args = new string[] {
        //    "msn"
        //    , "-i"
        //    , @"E:\6_Projects\PROJECT_MsMachineLearning\msn\aging_lipidome\data\aging_lipidome_neg.msp"
        //    , "-o"
        //    , @"E:\6_Projects\PROJECT_MsMachineLearning\msn\aging_lipidome\data\aging_lipidome_neg.edge"
        //    , "-m"
        //    , @"E:\6_Projects\PROJECT_MsMachineLearning\msn\aging_lipidome\msn_param_20240403.txt"
        //    , "-ionmode"
        //    , "Negative"
        //};

        //MoleculerSpectrumNetworkingTest.MergeNodeFiles(@"E:\6_Projects\PROJECT_MsMachineLearning\data\MTBKS157\peakpick\neg", @"E:\6_Projects\PROJECT_MsMachineLearning\msn\cytoscape_test\node.txt");
        //MoleculerSpectrumNetworkingTest.MergeEdgeFiles(@"E:\6_Projects\PROJECT_MsMachineLearning\msn\result-2309271138", @"E:\6_Projects\PROJECT_MsMachineLearning\msn\cytoscape_test\edge.txt");
        //EadAnnotationTest.Run(
        //    @"E:\6_Projects\PAPERWORK_MSDIAL5\04_MSDIAL5_validation_eieio\LightSplash\result\annotation\pairfile.txt",
        //    @"E:\6_Projects\PAPERWORK_MSDIAL5\04_MSDIAL5_validation_eieio\LightSplash\result\annotation\annofile.txt",
        //    @"E:\6_Projects\PAPERWORK_MSDIAL5\04_MSDIAL5_validation_eieio\LightSplash\result\annotation\peaknamefile.txt",
        //    @"E:\6_Projects\PAPERWORK_MSDIAL5\04_MSDIAL5_validation_eieio\LightSplash\result\annotation\resultexport.txt");

        //EadAnnotationTest.Run(
        //    @"E:\6_Projects\PAPERWORK_MSDIAL5\04_MSDIAL5_validation_eieio\StandardMix\KE14_output\pairfile.txt",
        //    @"E:\6_Projects\PAPERWORK_MSDIAL5\04_MSDIAL5_validation_eieio\StandardMix\KE14_output\annofile.txt",
        //    @"E:\6_Projects\PAPERWORK_MSDIAL5\04_MSDIAL5_validation_eieio\StandardMix\KE14_output\peaknamefile.txt",
        //    @"E:\6_Projects\PAPERWORK_MSDIAL5\04_MSDIAL5_validation_eieio\StandardMix\KE14_output\resultexport.txt");

        //MainProcess.CreateMsp4Model(
        //    @"E:\6_Projects\PROJECT_MsMachineLearning\msn\aging_lipidome\data\aging_lipidome_neg.msp",
        //    @"E:\6_Projects\PROJECT_MsMachineLearning\msn\aging_lipidome\data\aging_lipidome_neg_filtered.edge",
        //     @"E:\6_Projects\PROJECT_MsMachineLearning\msn\aging_lipidome\data\aging_lipidome_neg_for_model.msp");

        var analysisArg = new Argument<string>("analysisType") {
            Description = "gcms | lcms | lcimms | dims | imms | msn | imagegen"
        };

        var inputOpt = new Option<string>("--input", "-i") {
            Description = "input folder or file",
            Required = true,
        };
        var outputOpt = new Option<string>("--output", "-o") {
            Description = "output folder",
            Required = true,
        };
        var methodOpt = new Option<string>("--method", "-m") {
            Description = "method file",
            Required = true,
        };
        var projectOpt = new Option<bool>("--project", "-p") {
            Description = "generate .mdproject file",
        };
        var targetFolderOpt = new Option<string?>("--targetFolder", "-t") {
            Description = "target folder or file",
        };
        var targetMzOpt = new Option<string?>("--target") {
            Description = "target m/z",
        };
        var ionmodeOpt = new Option<string>("--ionmode") {
            Description = "ion mode",
            Arity = ArgumentArity.ZeroOrOne,
            DefaultValueFactory = _ => "Positive",
        };
        var overwriteOpt = new Option<bool>("--overwrite") {
            Description = "overwrite",
            Arity = ArgumentArity.ExactlyOne,
            DefaultValueFactory = _ => false,
        };
        var allEdgeOpt = new Option<bool>("--allEdgeExport", "-a") {
            Description = "export all edges",
        };

        var root = new RootCommand("MSDIAL Console Application");
        root.Arguments.Add(analysisArg);
        root.Options.Add(inputOpt);
        root.Options.Add(outputOpt);
        root.Options.Add(methodOpt);
        root.Options.Add(projectOpt);
        root.Options.Add(targetFolderOpt);
        root.Options.Add(targetMzOpt);
        root.Options.Add(ionmodeOpt);
        root.Options.Add(overwriteOpt);
        root.Options.Add(allEdgeOpt);

        root.SetAction(parseResult => {
            var analysisType = parseResult.GetRequiredValue(analysisArg);
            var input = parseResult.GetResult(inputOpt);
            var output = parseResult.GetResult(outputOpt);
            var method = parseResult.GetResult(methodOpt);
            var project = parseResult.GetResult(projectOpt);
            var targetFolder = parseResult.GetResult(targetFolderOpt);
            var targetMz = parseResult.GetResult(targetMzOpt);
            var ionmode = parseResult.GetResult(ionmodeOpt);
            var overwrite = parseResult.GetResult(overwriteOpt);
            var allEdge = parseResult.GetResult(allEdgeOpt);

            var argList = new List<string> { analysisType };
            if (input?.GetValueOrDefault<string>() is { Length: > 0 } inputStr) { argList.Add("-i"); argList.Add(inputStr); }
            if (method?.GetValueOrDefault<string>() is { Length: > 0 } methodStr) { argList.Add("-m"); argList.Add(methodStr); }
            if (targetFolder?.GetValueOrDefault<string>() is { Length: > 0 } targetFolderStr) { argList.Add("-t"); argList.Add(targetFolderStr); }
            if (output?.GetValueOrDefault<string>() is { Length: > 0 } outputStr) { argList.Add("-o"); argList.Add(outputStr); }
            if (project?.GetValueOrDefault<bool>() ?? false) argList.Add("-p");
            if (targetMz?.GetValueOrDefault<string>() is { Length: > 0 } targetMzStr) { argList.Add("-target"); argList.Add(targetMzStr); }
            if (ionmode?.GetValueOrDefault<string>() is { Length: > 0 } ionmodeStr) { argList.Add("-ionmode"); argList.Add(ionmodeStr); }
            if (overwrite?.GetValueOrDefault<bool>() ?? false) argList.Add("-overwrite");
            if (allEdge?.GetValueOrDefault<bool>() ?? false) argList.Add("-a");

            var rc = MainProcess.Run(argList.ToArray());
            Environment.Exit(rc);
        });

        return root.Parse(args).Invoke();
    }
}
