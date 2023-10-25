using CompMs.App.MsdialConsole.Casmi;
using CompMs.App.MsdialConsole.DataObjTest;
using CompMs.App.MsdialConsole.EadSpectraAnalysis;
using CompMs.App.MsdialConsole.Export;
using CompMs.App.MsdialConsole.MolecularNetwork;
using CompMs.App.MsdialConsole.MspCuration;
using CompMs.App.MsdialConsole.Parser;
using CompMs.App.MsdialConsole.Process;
using CompMs.App.MsdialConsole.ProteomicsTest;
using CompMs.Common.FormulaGenerator.Parser;
using CompMs.Common.Lipidomics;
using CompMs.Common.Proteomics.Parser;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialLcMsApi.Algorithm.PostCuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.App.MsdialConsole {
    class Program {
        static void Main(string[] args) {
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
            //    , @"E:\3_RIKEN\MedicalMS\data\neg"
            //    , "-o"
            //    , @"E:\3_RIKEN\MedicalMS\data\neg_output"
            //    , "-m"
            //    , @"E:\3_RIKEN\MedicalMS\data\neg\lipidomics_neg_library.txt"
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
            //    , @"E:\6_Projects\PROJECT_MsMachineLearning\data"
            //    , "-o"
            //    , @"E:\6_Projects\PROJECT_MsMachineLearning\msn"
            //    , "-m"
            //    , @"E:\6_Projects\PROJECT_MsMachineLearning\msn_param.txt"
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

            EadAnnotationTest.Run(
                @"E:\6_Projects\PAPERWORK_MSDIAL5\04_MSDIAL5_validation_eieio\StandardMix\KE14_output\pairfile.txt",
                @"E:\6_Projects\PAPERWORK_MSDIAL5\04_MSDIAL5_validation_eieio\StandardMix\KE14_output\annofile.txt",
                @"E:\6_Projects\PAPERWORK_MSDIAL5\04_MSDIAL5_validation_eieio\StandardMix\KE14_output\peaknamefile.txt",
                @"E:\6_Projects\PAPERWORK_MSDIAL5\04_MSDIAL5_validation_eieio\StandardMix\KE14_output\resultexport.txt");
            //MainProcess.Run(args);
        }
    }
}
