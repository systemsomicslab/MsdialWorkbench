using CompMs.App.MsdialConsole.Process;
using CompMs.App.MsdialConsole.Properties;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.App.MsdialConsole;
class Program {
    public static Task<int> Main(string[] args) {
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

        var root = new RootCommand($"MSDIAL Console Application {Resources.VERSION}");
        if (root.Options.OfType<VersionOption>().SingleOrDefault() is { } versionOption) {
            versionOption.Action = new VersionAction();
        }

        MainProcess.SetGcmsCommand(root);
        MainProcess.SetLcmsCommand(root);
        MainProcess.SetLcimmsCommand(root);
        MainProcess.SetDimsCommand(root);
        MainProcess.SetImmsCommand(root);
        MainProcess.SetMsnCommand(root);
        MainProcess.SetImageGenCommand(root);

        var parseResult = root.Parse(args);
        return parseResult.InvokeAsync();
    }

    internal class VersionAction : SynchronousCommandLineAction
    {
        public override int Invoke(ParseResult parseResult) {
            System.Console.WriteLine(Resources.VERSION);
            return 0;
        }
    }
}
