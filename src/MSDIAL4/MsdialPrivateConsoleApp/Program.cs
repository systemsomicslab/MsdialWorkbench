using Msdial.Lcms.Dataprocess.Algorithm.Clustering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsdialPrivateConsoleApp {
    class Program {
        static void Main(string[] args) {


            //adhoc.Run_CalcLipidomeRnaCorrelation_20231016(
            //    @"E:\6_Projects\PROJECT_500cells\20230901_250cells\correlation_rna_lipidome\metafile.txt",
            //    @"E:\6_Projects\PROJECT_500cells\20230901_250cells\correlation_rna_lipidome\lipidfile.txt",
            //    @"E:\6_Projects\PROJECT_500cells\20230901_250cells\correlation_rna_lipidome\rnafile.txt",
            //    @"E:\6_Projects\PROJECT_500cells\20230901_250cells\correlation_rna_lipidome\resultfile.txt"
            //    );


            //LipidomicsResultCuration.Name2Smiles(@"E:\6_Projects\Collaboration\NCC\20210406\temp.txt", @"E:\6_Projects\Collaboration\NCC\20210406\temp_result.txt");
            //LipidomicsResultCuration.Text2Msp(@"E:\6_Projects\PROJECT_Aging\paper\analysis_v2\Fig2\MN\node.txt", @"E:\6_Projects\PROJECT_Aging\paper\analysis_v2\Fig2\MN\feces_negspec.msp");

            //LipidomicsResultCuration.EadValidationResultExport(
            //    @"E:\6_Projects\PAPERWORK_MSDIAL5\04_MSDIAL5_validation_eieio\StandardMix\KE14_output\AlignmentResult_2024_01_07_09_08_59.xml",
            //    @"E:\6_Projects\PAPERWORK_MSDIAL5\04_MSDIAL5_validation_eieio\StandardMix\KE14_output\pairfile.txt");

            //LipidomicsResultCuration.EadValidationResultExport(
            //   @"E:\6_Projects\PAPERWORK_MSDIAL5\04_MSDIAL5_validation_eieio\LightSplash\result\annotation\AlignmentResult_2024_01_07_19_12_06.xml",
            //   @"E:\6_Projects\PAPERWORK_MSDIAL5\04_MSDIAL5_validation_eieio\LightSplash\result\annotation\pairfile.txt");

            var mspfile = @"E:\6_Projects\PROJECT_MsMachineLearning\msn\msp\neg\MSMS-Public_experimentspectra-neg-VS19.msp";
            var inchikeytable = @"E:\6_Projects\PROJECT_MsMachineLearning\msn\msp\neg\inchikey2ontology.txt";
            var new_mspfile = @"E:\6_Projects\PROJECT_MsMachineLearning\msn\msp\neg\MSMS-Public_experimentspectra-neg-VS19-curated.msp";
            var new_resultfile = @"E:\6_Projects\PROJECT_MsMachineLearning\msn\msp\neg\MSMS-Public_experimentspectra-neg-VS19-curated_woms2.txt";

            //MspManager.ExtractPositiveSpectra(
            //    @"E:\6_Projects\PROJECT_MsMachineLearning\msn\msp\new_ontlogycasmi2022.msp", 
            //    @"E:\6_Projects\PROJECT_MsMachineLearning\msn\msp\casmi2022_pos.msp");

            //MspManager.ExtractNegativeSpectra(
            //    @"E:\6_Projects\PROJECT_MsMachineLearning\msn\msp\new_ontlogycasmi2022.msp",
            //    @"E:\6_Projects\PROJECT_MsMachineLearning\msn\msp\casmi2022_neg.msp");

            //MspManager.CurateOntologyField(mspfile, inchikeytable, new_mspfile);
            //MspManager.AddOntologies(mspfile, inchikeytable, new_mspfile);
            //MspManager.Msp2TextAsMsdialAlignmentResultFormat(new_mspfile, new_resultfile, "msp_neg");

            //MspManager.MergeEdgePairs(@"\\165.93.102.222\Public\MetaboBankPeakPick\ogawa_20240123\output_msn_neg\result-2403280444",
            //    @"E:\6_Projects\PROJECT_MsMachineLearning\msn\mtbks\neg\mtbks_neg.edge");
            //MspManager.MergeMspFiles(@"\\165.93.102.222\Public\MetaboBankPeakPick\ogawa_20240123\input_msn_neg",
            //    @"E:\6_Projects\PROJECT_MsMachineLearning\msn\mtbks\neg\mtbks_neg.msp");
            MspManager.Msp2TextAsMsdialAlignmentResultFormat(@"E:\6_Projects\PROJECT_MsMachineLearning\msn\mtbks\neg\mtbks_neg.msp",
                @"E:\6_Projects\PROJECT_MsMachineLearning\msn\mtbks\neg\mtbks_neg.txt");
            //MspManager.Msp2TextAsMsdialAlignmentResultFormat(
            //    @"E:\6_Projects\PROJECT_MsMachineLearning\msn\msp\neg\casmi2022_neg.msp",
            //    @"E:\6_Projects\PROJECT_MsMachineLearning\msn\msp\neg\casmi2022_neg.txt",
            //    "casmi_neg");

            //mspfile = @"D:\9_Spectral library curations\Distributed MSPs\MSMS-RIKEN-Neg-VS15.msp";
            //new_mspfile = @"D:\9_Spectral library curations\Distributed MSPs\MSMS-RIKEN-Neg-VS15-For-Statistics.msp";

            //MspManager.CurateOntologyField(mspfile, inchikeytable, new_mspfile);

            //var directory = @"D:\1_PaperForLipoquality\ReviewerOnlyMaterials\SpectralKit to evalue FDR from Supplementary Data 2"; // please change this directory for your PC

            //var rttols = new List<float>();
            //for (int i = 0; i < 50; i++) {
            //    rttols.Add((float)(0.25 + i * 0.25));
            //}
            //for (int i = 0; i < 30; i++) {
            //    rttols.Add((float)(15.0 + i * 5.0));
            //}

            //LipidomicsResultCuration.FalseDiscoveryRateCalculationRtDependencyVS2(directory + @"\DDA_Negative_True positive and negative spectral kit.txt",
            // directory + @"\LipidMsmsBinaryDB-VS66-AritaM.lbm2",
            //  directory + @"\lbmqueries.txt",
            //  Rfx.Riken.OsakaUniv.IonMode.Negative, rttols, 0.01F, 0.05F,
            //  directory + @"\expected results\DDA_Neg_Test.csv");

            //LipidomicsResultCuration.FalseDiscoveryRateCalculationRtDependencyVS2(directory + @"\DDA_Positive_True positive and negative spectral kit.txt",
            //  directory + @"\LipidMsmsBinaryDB-VS66-AritaM.lbm2",
            //  directory + @"\lbmqueries.txt",
            //  Rfx.Riken.OsakaUniv.IonMode.Positive, rttols, 0.01F, 0.05F,
            //  directory + @"\expected results\DDA_Pos_Test.csv");

            //var ccstols = new List<float>();
            //for (int i = 0; i < 50; i++) {
            //    ccstols.Add((float)(1.0 + i * 1.0));
            //}
            //for (int i = 0; i < 2; i++) {
            //    ccstols.Add((float)(60.0 + i * 10));
            //}

            //LipidomicsResultCuration.FalseDiscoveryRateCalculationCcsDependencyVS2(directory + @"\PASEF_Negative_True positive and negative spectral kit.txt",
            //  directory + @"\LipidMsmsBinaryDB-VS66-AritaM.lbm2",
            //  directory + @"\lbmqueries.txt",
            //  Rfx.Riken.OsakaUniv.IonMode.Negative, ccstols, 100F, 0.01F, 0.05F,
            //  directory + @"\expected results\Pasef_Neg_Test_100RTtol.csv");

            //LipidomicsResultCuration.FalseDiscoveryRateCalculationCcsDependencyVS2(directory + @"\PASEF_Positive_True positive and negative spectral kit.txt",
            //  directory + @"\LipidMsmsBinaryDB-VS66-AritaM.lbm2",
            //  directory + @"\lbmqueries.txt",
            //  Rfx.Riken.OsakaUniv.IonMode.Positive, ccstols, 100F, 0.01F, 0.05F,
            //  directory + @"\expected results\Pasef_Pos_Test_100RTtol.csv");

            //LipidomicsResultCuration.FalseDiscoveryRateCalculationCcsDependencyVS2(directory + @"\PASEF_Negative_True positive and negative spectral kit.txt",
            //  directory + @"\LipidMsmsBinaryDB-VS66-AritaM.lbm2",
            //  directory + @"\lbmqueries.txt",
            //  Rfx.Riken.OsakaUniv.IonMode.Negative, ccstols, 1.5F, 0.01F, 0.05F,
            //  directory + @"\expected results\Pasef_Neg_Test_1RTtol.csv");

            //LipidomicsResultCuration.FalseDiscoveryRateCalculationCcsDependencyVS2(directory + @"\PASEF_Positive_True positive and negative spectral kit.txt",
            //  directory + @"\LipidMsmsBinaryDB-VS66-AritaM.lbm2",
            //  directory + @"\lbmqueries.txt",
            //  Rfx.Riken.OsakaUniv.IonMode.Positive, ccstols, 1.5F, 0.01F, 0.05F,
            //  directory + @"\expected results\Pasef_Pos_Test_1RTtol.csv");



            //below is Hiroshi Tsugawa's private source code
            #region lipoquality database curation utilities
            //var outputDir = @"C:\Users\hiroshi.tsugawa\Desktop\Supplementary Data 1\TSV\LQDB_format";
            //var oneGroupfiles = System.IO.Directory.GetFiles(@"C:\Users\hiroshi.tsugawa\Desktop\Supplementary Data 1\TSV\OneGroup");
            //var pairedfiles = System.IO.Directory.GetFiles(@"C:\Users\hiroshi.tsugawa\Desktop\Supplementary Data 1\TSV\Paired");
            //var fourGroupfiles = System.IO.Directory.GetFiles(@"C:\Users\hiroshi.tsugawa\Desktop\Supplementary Data 1\TSV\FourGroups");
            //foreach (var file in oneGroupfiles) {
            //    LipidomicsResultCuration.Msdial4TsvToLipoqualityDataFormatOneBioSample(file, outputDir);
            //}

            //foreach (var file in pairedfiles) {
            //    LipidomicsResultCuration.Msdial4TsvToLipoqualityDataFormatPaired(file, outputDir);
            //}

            //foreach (var file in fourGroupfiles) {
            //    LipidomicsResultCuration.Msdial4TsvToLipoqualityDataFormatFourPaired(file, outputDir);
            //}

            // LipidomicsResultCuration.ExtractInChIKeySmilesPair(@"C:\Users\hiroshi.tsugawa\Desktop\MSDIAL_LipidMsmsCreater\BACKUP\VS66\LipidMsmsBinaryDB-VS66-AritaM.lbm2", @"C:\Users\hiroshi.tsugawa\Desktop\MSDIAL_LipidMsmsCreater\BACKUP\VS66\LipidMsmsBinaryDB-VS66-InChIkeySMILES.csv");

            #endregion

            #region lipidomics data curation utilities
            //LipidomicsResultCuration.AdductCurator(@"C:\Users\ADMIN\Desktop\Curation\Original", @"C:\Users\ADMIN\Desktop\Curation\adductcurationresult from adductcurated.txt");
            //LipidomicsResultCuration.AdductCorrection(@"C:\Users\ADMIN\Desktop\Curation\Original", @"C:\Users\ADMIN\Desktop\Curation\adductcorrectionlist.txt", @"C:\Users\ADMIN\Desktop\Curation\AdductCurated");
            //LipidomicsResultCuration.AdductCurator(@"C:\Users\ADMIN\Desktop\Curation\AdductCurated", @"C:\Users\ADMIN\Desktop\Curation\adductcurationresult.txt");
            ////LipidomicsResultCuration.OntologyCurator(@"C:\Users\ADMIN\Desktop\Curation\AdductCurated", @"C:\Users\ADMIN\Desktop\Curation\ontology_curation_result.txt");
            ////LipidomicsResultCuration.ExtractOntologyAdductPair(@"C:\Users\ADMIN\Desktop\Curation\AdductCurated", @"C:\Users\ADMIN\Desktop\Curation\ontology_adduct_list.txt");
            //LipidomicsResultCuration.ExportQuantFiles(@"C:\Users\ADMIN\Desktop\Curation\AdductCurated", @"C:\Users\ADMIN\Desktop\Curation\QuantCuratedVS2");
            //LipidomicsResultCuration.QuantDiff(@"C:\Users\ADMIN\Desktop\Curation\QuantCurated", @"C:\Users\ADMIN\Desktop\Curation\QuantCuratedVS2", @"C:\Users\ADMIN\Desktop\Curation\QuantDiffVS2");
            //LipidomicsResultCuration.CheckOntologyAdductPair(@"C:\Users\ADMIN\Desktop\Curation\QuantOriginal", @"C:\Users\ADMIN\Desktop\Curation\sm_protonloss_check.txt");
            //LipidomicsResultCuration.ExportDdaStatistics(@"C:\Users\ADMIN\Desktop\Curation\AdductCurated", @"C:\Users\ADMIN\Desktop\Curation\Statistics\DdaStatistics-VS2-test.txt");
            //LipidomicsResultCuration.ExportDdaStatistics(@"C:\Users\ADMIN\Desktop\Curation\RtPredictionSet", @"C:\Users\ADMIN\Desktop\Curation\Statistics\DdaStatisticsTableForRtPred-VS2.txt");
            //LipidomicsResultCuration.ExportPasefStatistics(@"C:\Users\ADMIN\Desktop\Curation\AdductCurated", @"C:\Users\ADMIN\Desktop\Curation\Statistics\PasefStatistics-VS2.txt");

            //LipidomicsResultCuration.DdaMSPsToMeta(@"C:\Users\ADMIN\Desktop\Curation\AnnotationPipelineEvaluation\OriginalMSPs",
            //    @"C:\Users\ADMIN\Desktop\Curation\AnnotationPipelineEvaluation\TextFormatsFromMSPs\DDA_Pos_Spec.txt", "Positive");
            //LipidomicsResultCuration.DdaMSPsToMeta(@"C:\Users\ADMIN\Desktop\Curation\AnnotationPipelineEvaluation\OriginalMSPs",
            //    @"C:\Users\ADMIN\Desktop\Curation\AnnotationPipelineEvaluation\TextFormatsFromMSPs\DDA_Neg_Spec.txt", "Negative");
            //LipidomicsResultCuration.PasefMSPsToMeta(@"C:\Users\ADMIN\Desktop\Curation\AnnotationPipelineEvaluation\OriginalMSPs",
            //   @"C:\Users\ADMIN\Desktop\Curation\AnnotationPipelineEvaluation\TextFormatsFromMSPs\Pasef_Pos_Spec.txt", "Positive");
            //LipidomicsResultCuration.PasefMSPsToMeta(@"C:\Users\ADMIN\Desktop\Curation\AnnotationPipelineEvaluation\OriginalMSPs",
            //    @"C:\Users\ADMIN\Desktop\Curation\AnnotationPipelineEvaluation\TextFormatsFromMSPs\Pasef_Neg_Spec.txt", "Negative");
            //LipidomicsResultCuration.AdductCuratorForMspText(@"C:\Users\ADMIN\Desktop\Curation\AnnotationPipelineEvaluation\TextFormatsFromMSPs", @"C:\Users\ADMIN\Desktop\Curation\AnnotationPipelineEvaluation\adduct incorrect list.txt");

            //LipidomicsResultCuration.FalseDiscoveryRateCalculation(@"C:\Users\ADMIN\Desktop\Curation\AnnotationPipelineEvaluation\TextFormatsFromMSPs\DDA_Neg_Spec.txt",
            //    @"C:\Users\ADMIN\Desktop\LipidBlast MSP backup\VS61\LipidMsmsBinaryDB-VS61-AritaM.lbm2",
            //    @"C:\Users\ADMIN\Desktop\Curation\AnnotationPipelineEvaluation\lbmqueries.txt",
            //    Rfx.Riken.OsakaUniv.IonMode.Negative, 1.25F, 0.015F, 0.05F, -1.0F,
            //    @"C:\Users\ADMIN\Desktop\Curation\AnnotationPipelineEvaluation\Result\DDA_Neg_Spec-testresult.txt");

            //LipidomicsResultCuration.FalseDiscoveryRateCalculation(@"C:\Users\ADMIN\Desktop\Curation\AnnotationPipelineEvaluation\TextFormatsFromMSPs\DDA_Pos_Spec.txt",
            //    @"C:\Users\ADMIN\Desktop\LipidBlast MSP backup\VS61\LipidMsmsBinaryDB-VS61-AritaM.lbm2",
            //    @"C:\Users\ADMIN\Desktop\Curation\AnnotationPipelineEvaluation\lbmqueries.txt",
            //    Rfx.Riken.OsakaUniv.IonMode.Positive, 1.25F, 0.01F, 0.05F, -1.0F,
            //    @"C:\Users\ADMIN\Desktop\Curation\AnnotationPipelineEvaluation\Result\DDA_Pos_Spec-testresult.txt");


            //// for aging
            //LipidomicsResultCuration.AdductCurator(@"C:\Users\ADMIN\Desktop\Curation\Aging\Original", @"C:\Users\ADMIN\Desktop\Curation\adductcurationresult.txt");
            //LipidomicsResultCuration.AdductCorrection(@"C:\Users\ADMIN\Desktop\Curation\Aging\Original", @"C:\Users\ADMIN\Desktop\Curation\adductcorrectionlist-no.txt", @"C:\Users\ADMIN\Desktop\Curation\Aging\AdductCurated");
            //LipidomicsResultCuration.ExtractOntologyAdductPair(@"C:\Users\ADMIN\Desktop\Curation\Aging\AdductCurated", @"C:\Users\ADMIN\Desktop\Curation\Aging\ontology_adduct_list.txt");

            //LipidomicsResultCuration.ExportQuantFiles(@"C:\Users\ADMIN\Desktop\Curation\Aging\AdductCurated", @"C:\Users\ADMIN\Desktop\Curation\Aging\QuantCurated");

            // for additional feces samples
            //LipidomicsResultCuration.AdductCurator(@"C:\Users\ADMIN\Desktop\Curation\ImsMouseFecesOriginal", @"C:\Users\ADMIN\Desktop\Curation\adductcurationresult-feces.txt");
            //LipidomicsResultCuration.AdductCorrection(@"C:\Users\ADMIN\Desktop\Curation\ImsMouseFecesOriginal", @"C:\Users\ADMIN\Desktop\Curation\adductcorrectionlist-no.txt", @"C:\Users\ADMIN\Desktop\Curation\ImsMouseFecesAdductCurated");
            //LipidomicsResultCuration.ExportQuantFiles(@"C:\Users\ADMIN\Desktop\Curation\ImsMouseFecesOriginal", @"C:\Users\ADMIN\Desktop\Curation\ImsMouseFecesQuant");


            // for named file statistics
            //LipidomicsResultCuration.ExtractSuspectOrigins(@"D:\1_PaperForLipoquality\Raw data\Result\Checklist.txt", @"D:\1_PaperForLipoquality\Raw data\Result\2_0_NamedData", @"D:\1_PaperForLipoquality\Raw data\Result\2_1_NamdedDataStatistics\ExcludedInfo_191007.txt");

            //LipidomicsResultCuration.ExportDdaStatistics(@"D:\1_PaperForLipoquality\Raw data\Result\2_0_NamedData", @"D:\1_PaperForLipoquality\Raw data\Result\2_1_NamdedDataStatistics\DdaStatistics_191128.txt");
            //LipidomicsResultCuration.ExportPasefStatistics(@"D:\1_PaperForLipoquality\Raw data\Result\2_0_NamedData", @"D:\1_PaperForLipoquality\Raw data\Result\2_1_NamdedDataStatistics\PasefStatistics_191128.txt");

            //// for pasef vs dda statistics
            //LipidomicsResultCuration.ExportDdaStatistics(@"D:\1_PaperForLipoquality\Raw data\Result\3_0_PasefVsDda\DDAdataset",
            //    @"D:\1_PaperForLipoquality\Raw data\Result\3_0_PasefVsDda\DdaBrukerStatistics_191012.txt");

            //// for rt prediction statistics
            //LipidomicsResultCuration.ExportDdaStatistics(@"D:\1_PaperForLipoquality\Raw data\Result\4_0_RtPredictionData\TrainingSet",
            //    @"D:\1_PaperForLipoquality\Raw data\Result\4_0_RtPredictionData\TrainingSet_191128.txt");
            //LipidomicsResultCuration.ExportDdaStatistics(@"D:\1_PaperForLipoquality\Raw data\Result\4_0_RtPredictionData\ExternalValidationSet",
            //   @"D:\1_PaperForLipoquality\Raw data\Result\4_0_RtPredictionData\ExValidationDdaSet_191012.txt");
            //LipidomicsResultCuration.ExportPasefStatistics(@"D:\1_PaperForLipoquality\Raw data\Result\4_0_RtPredictionData\ExternalValidationSet",
            //   @"D:\1_PaperForLipoquality\Raw data\Result\4_0_RtPredictionData\ExValidationPasefSet_191012.txt");

            ////for spectrum curation kit
            //LipidomicsResultCuration.DdaMSPsToMeta(@"D:\1_PaperForLipoquality\Raw data\Result\6_0_SpectralAnnotationData",
            //    @"D:\1_PaperForLipoquality\Raw data\Result\6_1_SpectralAnnotationKit\DDA_Pos_Spec.txt", "Positive");
            //LipidomicsResultCuration.DdaMSPsToMeta(@"D:\1_PaperForLipoquality\Raw data\Result\6_0_SpectralAnnotationData",
            //    @"D:\1_PaperForLipoquality\Raw data\Result\6_1_SpectralAnnotationKit\DDA_Neg_Spec.txt", "Negative");
            //LipidomicsResultCuration.PasefMSPsToMeta(@"D:\1_PaperForLipoquality\Raw data\Result\6_0_2_SpectralAnnotationDataPasef",
            //    @"D:\1_PaperForLipoquality\Raw data\Result\6_1_SpectralAnnotationKit\Pasef_Pos_Spec_vs2.txt", "Positive");
            //LipidomicsResultCuration.PasefMSPsToMeta(@"D:\1_PaperForLipoquality\Raw data\Result\6_0_2_SpectralAnnotationDataPasef",
            //    @"D:\1_PaperForLipoquality\Raw data\Result\6_1_SpectralAnnotationKit\Pasef_Neg_Spec_vs2.txt", "Negative");

            //LipidomicsResultCuration.ConvertTxtToBoxplotFormat(@"D:\1_PaperForLipoquality\Raw data\Result\1_3_QuantDataHumanPlasma\QuantFiles\TAG_Quant.txt", @"D:\1_PaperForLipoquality\Raw data\Result\1_3_QuantDataHumanPlasma\QuantFiles\TAG_Quant_boxplot.csv");

            //LipidomicsResultCuration.AnnotationForTestQueries(@"D:\1_PaperForLipoquality\Raw data\Result\6_3_SpectralAnnotationTest\DDA_Negative_True positive and negative spectral kit.txt",
            //   @"D:\1_PaperForLipoquality\Raw data\Result\6_3_SpectralAnnotationTest\LipidMsmsBinaryDB-VS68-AritaM.lbm2",
            //   @"D:\1_PaperForLipoquality\Raw data\Result\6_3_SpectralAnnotationTest\lbmqueries.txt",
            //   Rfx.Riken.OsakaUniv.IonMode.Negative, 1.5F, 0.01F, 0.05F, -1.0F,
            //   @"D:\1_PaperForLipoquality\Raw data\Result\6_3_SpectralAnnotationTest\DDA_Negative_True positive and negative spectral kit msp");

            //LipidomicsResultCuration.AnnotationForTestQueries(@"D:\1_PaperForLipoquality\Raw data\Result\6_3_SpectralAnnotationTest\DDA_Positive_True positive and negative spectral kit.txt",
            //   @"D:\1_PaperForLipoquality\Raw data\Result\6_3_SpectralAnnotationTest\LipidMsmsBinaryDB-VS68-AritaM.lbm2",
            //   @"D:\1_PaperForLipoquality\Raw data\Result\6_3_SpectralAnnotationTest\lbmqueries.txt",
            //   Rfx.Riken.OsakaUniv.IonMode.Positive, 1.5F, 0.01F, 0.05F, -1.0F,
            //   @"D:\1_PaperForLipoquality\Raw data\Result\6_3_SpectralAnnotationTest\DDA_Positive_True positive and negative spectral kit msp");

            //LipidomicsResultCuration.AnnotationForTestQueries(@"D:\1_PaperForLipoquality\Raw data\Result\6_3_SpectralAnnotationTest\PASEF_Negative_True positive and negative spectral kit.txt",
            //  @"D:\1_PaperForLipoquality\Raw data\Result\6_3_SpectralAnnotationTest\LipidMsmsBinaryDB-VS68-AritaM.lbm2",
            //  @"D:\1_PaperForLipoquality\Raw data\Result\6_3_SpectralAnnotationTest\lbmqueries.txt",
            //  Rfx.Riken.OsakaUniv.IonMode.Negative, 1.5F, 0.01F, 0.05F, 10F,
            //  @"D:\1_PaperForLipoquality\Raw data\Result\6_3_SpectralAnnotationTest\PASEF_Negative_True positive and negative spectral kit msp");

            //LipidomicsResultCuration.AnnotationForTestQueries(@"D:\1_PaperForLipoquality\Raw data\Result\6_3_SpectralAnnotationTest\PASEF_Positive_True positive and negative spectral kit.txt",
            //   @"D:\1_PaperForLipoquality\Raw data\Result\6_3_SpectralAnnotationTest\LipidMsmsBinaryDB-VS68-AritaM.lbm2",
            //   @"D:\1_PaperForLipoquality\Raw data\Result\6_3_SpectralAnnotationTest\lbmqueries.txt",
            //   Rfx.Riken.OsakaUniv.IonMode.Positive, 1.5F, 0.01F, 0.05F, 10F,
            //   @"D:\1_PaperForLipoquality\Raw data\Result\6_3_SpectralAnnotationTest\PASEF_Positive_True positive and negative spectral kit msp");

            //LipidomicsResultCuration.ConvertUnrealizedLipidsToUnknownsInSpectralKit(
            //    @"D:\1_PaperForLipoquality\Raw data\Result\6_1_SpectralAnnotationKit\DDA_Neg_Spec.txt",
            //    @"D:\1_PaperForLipoquality\Raw data\Result\6_1_SpectralAnnotationKit\DDA_Neg_Spec_vs3.txt",
            //    false, Rfx.Riken.OsakaUniv.IonMode.Negative);

            //LipidomicsResultCuration.ConvertUnrealizedLipidsToUnknownsInSpectralKit(
            //     @"D:\1_PaperForLipoquality\Raw data\Result\6_1_SpectralAnnotationKit\DDA_Pos_Spec.txt",
            //     @"D:\1_PaperForLipoquality\Raw data\Result\6_1_SpectralAnnotationKit\DDA_Pos_Spec_vs3.txt",
            //     false, Rfx.Riken.OsakaUniv.IonMode.Positive);

            //LipidomicsResultCuration.ConvertUnrealizedLipidsToUnknownsInSpectralKit(
            //    @"C:\Users\hiroshi.tsugawa\Desktop\NewMsdialTest\Pasef_Pos_Spec.txt",
            //    @"C:\Users\hiroshi.tsugawa\Desktop\NewMsdialTest\Pasef_Pos_Spec_vs2.txt",
            //    true, Rfx.Riken.OsakaUniv.IonMode.Positive);

            //LipidomicsResultCuration.ConvertUnrealizedLipidsToUnknownsInSpectralKit(
            //   @"C:\Users\hiroshi.tsugawa\Desktop\NewMsdialTest\Pasef_Neg_Spec.txt",
            //   @"C:\Users\hiroshi.tsugawa\Desktop\NewMsdialTest\Pasef_Neg_Spec_vs2.txt",
            //   true, Rfx.Riken.OsakaUniv.IonMode.Negative);


            //// for spectrum kit test
            //LipidomicsResultCuration.FalseDiscoveryRateCalculationVS2(@"C:\Users\hiroshi.tsugawa\Desktop\NewMsdialTest\DDA_Neg_Spec.txt",
            //   @"C:\Users\hiroshi.tsugawa\Desktop\NewMsdialTest\LipidMsmsBinaryDB-VS66-AritaM.lbm2",
            //   @"C:\Users\hiroshi.tsugawa\Desktop\NewMsdialTest\lbmqueries.txt",
            //   Rfx.Riken.OsakaUniv.IonMode.Negative, 1.25F, 0.01F, 0.05F, -1.0F,
            //   @"C:\Users\hiroshi.tsugawa\Desktop\NewMsdialTest\result\DDA_Neg_Spec_result.txt");

            //LipidomicsResultCuration.FalseDiscoveryRateCalculationVS2(@"C:\Users\hiroshi.tsugawa\Desktop\NewMsdialTest\DDA_Pos_Spec.txt",
            //  @"C:\Users\hiroshi.tsugawa\Desktop\NewMsdialTest\LipidMsmsBinaryDB-VS66-AritaM.lbm2",
            //  @"C:\Users\hiroshi.tsugawa\Desktop\NewMsdialTest\lbmqueries.txt",
            //  Rfx.Riken.OsakaUniv.IonMode.Positive, 1.25F, 0.01F, 0.05F, -1.0F,
            //  @"C:\Users\hiroshi.tsugawa\Desktop\NewMsdialTest\result\DDA_Pos_Spec_result.txt");

            //LipidomicsResultCuration.FalseDiscoveryRateCalculationVS2(@"C:\Users\hiroshi.tsugawa\Desktop\NewMsdialTest\Pasef_Neg_Spec_vs2.txt",
            //  @"C:\Users\hiroshi.tsugawa\Desktop\NewMsdialTest\LipidMsmsBinaryDB-VS66-AritaM.lbm2",
            //  @"C:\Users\hiroshi.tsugawa\Desktop\NewMsdialTest\lbmqueries.txt",
            //  Rfx.Riken.OsakaUniv.IonMode.Negative, 1.5F, 0.01F, 0.05F, 10.0F,
            //  @"C:\Users\hiroshi.tsugawa\Desktop\NewMsdialTest\result\Pasef_Neg_Spec_result.txt");

            //LipidomicsResultCuration.FalseDiscoveryRateCalculationVS2(@"C:\Users\hiroshi.tsugawa\Desktop\NewMsdialTest\Pasef_Pos_Spec_vs2.txt",
            //  @"C:\Users\hiroshi.tsugawa\Desktop\NewMsdialTest\LipidMsmsBinaryDB-VS66-AritaM.lbm2",
            //  @"C:\Users\hiroshi.tsugawa\Desktop\NewMsdialTest\lbmqueries.txt",
            //  Rfx.Riken.OsakaUniv.IonMode.Positive, 1.5F, 0.01F, 0.05F, 10.0F,
            //  @"C:\Users\hiroshi.tsugawa\Desktop\NewMsdialTest\result\Pasef_Pos_Spec_result.txt");


            // quant statistics
            //LipidomicsResultCuration.ExportQuantTsvOntologies(@"D:\1_PaperForLipoquality\Raw data\Result\1_1_QuantDataTsv", 
            //    @"D:\1_PaperForLipoquality\Raw data\Result\1_1_QuantDataTsv\ontologies_191127.txt");
            //LipidomicsResultCuration.ExportQuantStatistics(@"D:\1_PaperForLipoquality\Raw data\Result\1_1_QuantDataTsv",
            //    @"D:\1_PaperForLipoquality\Raw data\Result\1_1_QuantDataTsv\category_list_animal.txt",
            //    @"D:\1_PaperForLipoquality\Raw data\Result\1_1_QuantDataTsv\tissue_caterory_animal.txt",
            //    @"D:\1_PaperForLipoquality\Raw data\Result\1_1_QuantDataTsv\lipid_caterory.txt",
            //    @"D:\1_PaperForLipoquality\Raw data\Result\1_2_QuantDataStatistics\Result-animal_191220.csv");
            //LipidomicsResultCuration.ExportHumanPlasmaQuantSummary(@"D:\1_PaperForLipoquality\Raw data\Result\1_3_QuantDataHumanPlasma\Human plasma data tsv",
            //    @"D:\1_PaperForLipoquality\Raw data\Result\1_3_QuantDataHumanPlasma\Human plasma result test\Summary.csv");
            //LipidomicsResultCuration.CalculateCod(@"D:\1_PaperForLipoquality\Raw data\Result\1_3_QuantDataHumanPlasma\Human plasma result\Summary-class-ave-matrix-final.txt",
            //    @"D:\1_PaperForLipoquality\Raw data\Result\1_3_QuantDataHumanPlasma\Human plasma result\Summary-class-ave-matrix-final-cod.txt");
            //var files = System.IO.Directory.GetFiles(@"D:\1_PaperForLipoquality\Raw data\Result\1_3_QuantDataHumanPlasma\QuantFiles", "*.txt");
            //foreach (var file in files) {
            //    var filename = System.IO.Path.GetFileNameWithoutExtension(file);
            //    var output = System.IO.Path.GetDirectoryName(file) + "\\" + filename + "_boxplot.csv";
            //    LipidomicsResultCuration.ConvertTxtToBoxplotFormat(file, output);
            //}


            // further curation mag and fa
            //LipidomicsResultCuration.AdductCurator(@"C:\Users\ADMIN\Desktop\Curation\OriginalVs3", @"C:\Users\ADMIN\Desktop\Curation\adductcurationresult from adductcurated_vs4.txt");
            //LipidomicsResultCuration.AdductCorrection(@"C:\Users\ADMIN\Desktop\Curation\OriginalVs3", @"C:\Users\ADMIN\Desktop\Curation\adductcorrectionlist-vs3.txt", @"C:\Users\ADMIN\Desktop\Curation\AdductCurated");
            //LipidomicsResultCuration.ExportQuantFiles(@"C:\Users\ADMIN\Desktop\Curation\AdductCurated", @"C:\Users\ADMIN\Desktop\Curation\QuantCuratedVS4");
            //LipidomicsResultCuration.QuantDiff(@"C:\Users\ADMIN\Desktop\Curation\QuantCuratedVS3", @"C:\Users\ADMIN\Desktop\Curation\QuantCuratedVS4", @"C:\Users\ADMIN\Desktop\Curation\QuantDiffVS4");
            //LipidomicsResultCuration.CheckOntologyAdductPair(@"C:\Users\ADMIN\Desktop\Curation\QuantOriginal", @"C:\Users\ADMIN\Desktop\Curation\sm_protonloss_check.txt");
            //LipidomicsResultCuration.ExportDdaStatistics(@"C:\Users\ADMIN\Desktop\Curation\AdductCurated", @"C:\Users\ADMIN\Desktop\Curation\Statistics\DdaStatistics-VS2-test.txt");
            //LipidomicsResultCuration.ExportDdaStatistics(@"C:\Users\ADMIN\Desktop\Curation\RtPredictionSet", @"C:\Users\ADMIN\Desktop\Curation\Statistics\DdaStatisticsTableForRtPred-VS2.txt");
            //LipidomicsResultCuration.ExportPasefStatistics(@"C:\Users\ADMIN\Desktop\Curation\AdductCurated", @"C:\Users\ADMIN\Desktop\Curation\Statistics\PasefStatistics-VS2.txt");

            // further curation Bruker Plasma
            //LipidomicsResultCuration.AdductCurator(@"D:\1_PaperForLipoquality\Raw data\Curation\OriginalVs4", @"D:\1_PaperForLipoquality\Raw data\Curation\adductcurationresult for bruker.txt");
            //LipidomicsResultCuration.AdductCorrection(@"D:\1_PaperForLipoquality\Raw data\Curation\OriginalVs4", @"D:\1_PaperForLipoquality\Raw data\Curation\adductcorrectionlist for bruker.txt", @"D:\1_PaperForLipoquality\Raw data\Curation\AdductCuratedVs4");
            //LipidomicsResultCuration.AdductCurator(@"D:\1_PaperForLipoquality\Raw data\Curation\AdductCuratedVs4", @"D:\1_PaperForLipoquality\Raw data\Curation\adductcurationresult for bruker-check.txt");
            //LipidomicsResultCuration.ExportQuantFiles(@"D:\1_PaperForLipoquality\Raw data\Curation\AdductCuratedVs4", @"D:\1_PaperForLipoquality\Raw data\Curation\QuantCuratedVS4");
            //LipidomicsResultCuration.QuantDiff(@"C:\Users\ADMIN\Desktop\Curation\QuantCuratedVS3", @"C:\Users\ADMIN\Desktop\Curation\QuantCuratedVS4", @"C:\Users\ADMIN\Desktop\Curation\QuantDiffVS4");
            //LipidomicsResultCuration.CheckOntologyAdductPair(@"C:\Users\ADMIN\Desktop\Curation\QuantOriginal", @"C:\Users\ADMIN\Desktop\Curation\sm_protonloss_check.txt");
            //LipidomicsResultCuration.ExportDdaStatistics(@"C:\Users\ADMIN\Desktop\Curation\AdductCurated", @"C:\Users\ADMIN\Desktop\Curation\Statistics\DdaStatistics-VS2-test.txt");
            //LipidomicsResultCuration.ExportDdaStatistics(@"C:\Users\ADMIN\Desktop\Curation\RtPredictionSet", @"C:\Users\ADMIN\Desktop\Curation\Statistics\DdaStatisticsTableForRtPred-VS2.txt");
            //LipidomicsResultCuration.ExportPasefStatistics(@"C:\Users\ADMIN\Desktop\Curation\AdductCurated", @"C:\Users\ADMIN\Desktop\Curation\Statistics\PasefStatistics-VS2.txt");




            //Console.WriteLine();
            //CcsCalculator.Run(@"D:\1_PaperForLipoquality\Database\RtCcsPrediction\DriftFormulaAdduct.txt",
            //    @"D:\1_PaperForLipoquality\Database\RtCcsPrediction\CcsFromTheoreticalMz.txt");

            //CircosUtility.GenerateCircosFilesVs2(
            //    @"D:\Project for lipidomics in Makoto Arita lab\Mochida Naoe Project\Macrophage.txt",
            //    12,
            //    @"D:\Project for lipidomics in Makoto Arita lab\Mochida Naoe Project\Macrophage");
            //CircosUtility.GenerateCircosFilesVs2(
            //    @"D:\Project for lipidomics in Makoto Arita lab\Mochida Naoe Project\Brain.txt",
            //    20,
            //    @"D:\Project for lipidomics in Makoto Arita lab\Mochida Naoe Project\Brain");
            //CircosUtility.GenerateCircosFilesVs2(
            //    @"D:\Project for lipidomics in Makoto Arita lab\Mochida Naoe Project\Adipose.txt",
            //    20,
            //    @"D:\Project for lipidomics in Makoto Arita lab\Mochida Naoe Project\Adipose");
            //CircosUtility.GenerateCircosFilesVs2(
            //    @"D:\Project for lipidomics in Makoto Arita lab\Mochida Naoe Project\Heart.txt",
            //    20,
            //    @"D:\Project for lipidomics in Makoto Arita lab\Mochida Naoe Project\Heart");
            //CircosUtility.GenerateCircosFilesVs2(
            //    @"D:\Project for lipidomics in Makoto Arita lab\Mochida Naoe Project\Kidney.txt",
            //    20,
            //    @"D:\Project for lipidomics in Makoto Arita lab\Mochida Naoe Project\Kidney");
            //CircosUtility.GenerateCircosFilesVs2(
            //    @"D:\Project for lipidomics in Makoto Arita lab\Mochida Naoe Project\Liver.txt",
            //    16,
            //    @"D:\Project for lipidomics in Makoto Arita lab\Mochida Naoe Project\Liver");
            //CircosUtility.GenerateCircosFilesVs2(
            //    @"D:\Project for lipidomics in Makoto Arita lab\Mochida Naoe Project\Lung.txt",
            //    20,
            //    @"D:\Project for lipidomics in Makoto Arita lab\Mochida Naoe Project\Lung");
            //CircosUtility.GenerateCircosFilesVs2(
            //    @"D:\Project for lipidomics in Makoto Arita lab\Mochida Naoe Project\Muscle.txt",
            //    20,
            //    @"D:\Project for lipidomics in Makoto Arita lab\Mochida Naoe Project\Muscle");
            //CircosUtility.GenerateCircosFilesVs2(
            //    @"D:\Project for lipidomics in Makoto Arita lab\Mochida Naoe Project\Plasma.txt",
            //    20,
            //    @"D:\Project for lipidomics in Makoto Arita lab\Mochida Naoe Project\Plasma");
            //CircosUtility.GenerateCircosFilesVs2(
            //    @"D:\Project for lipidomics in Makoto Arita lab\Mochida Naoe Project\SmallIntestine.txt",
            //    20,
            //    @"D:\Project for lipidomics in Makoto Arita lab\Mochida Naoe Project\SmallIntestine");
            //CircosUtility.GenerateCircosFilesVs2(
            //    @"D:\Project for lipidomics in Makoto Arita lab\Mochida Naoe Project\Spleen.txt",
            //    20,
            //    @"D:\Project for lipidomics in Makoto Arita lab\Mochida Naoe Project\Spleen");


            //Console.ReadLine();
            //CircosUtility.GenerateCircosFiles(@"D:\20190222-RyoN-CircosPlot\Matrix.txt");
            //EgdeGenerator.GenerateEdgesForCircusPlotFromText(@"C:\Users\ADMIN\Desktop\Circos plots for Amit paper\mapping data.txt",
            //    @"C:\Users\ADMIN\Desktop\Circos plots for Amit paper\plant-link.txt");

            //CtraceCorrectCheck.CheckingInsourceFragment(@"D:\PROJECT_Plant Specialized Metabolites Annotations\All plant tissues\MSDIAL-Test\Correct list\Insource correct list-Pos.txt",
            //     @"D:\PROJECT_Plant Specialized Metabolites Annotations\All plant tissues\MSDIAL-Test\Positive-Results-VS2",
            //     @"D:\PROJECT_Plant Specialized Metabolites Annotations\All plant tissues\MSDIAL-Test\Correct list\Insource correct list-Pos-result-vs2.txt");

            //CtraceCorrectCheck.CheckingInsourceFragment(@"D:\PROJECT_Plant Specialized Metabolites Annotations\All plant tissues\MSDIAL-Test\Correct list\Insource correct list-Neg.txt",
            //     @"D:\PROJECT_Plant Specialized Metabolites Annotations\All plant tissues\MSDIAL-Test\Negative-Results-VS2",
            //     @"D:\PROJECT_Plant Specialized Metabolites Annotations\All plant tissues\MSDIAL-Test\Correct list\Insource correct list-Neg-result-vs2.txt");

            //CtraceCorrectCheck.CheckingAdductContent(@"D:\PROJECT_Plant Specialized Metabolites Annotations\All plant tissues\MSDIAL-Test\Correct list\Carbon Adduct correct list-Pos.txt",
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\All plant tissues\MSDIAL-Test\Positive-Results-VS2",
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\All plant tissues\MSDIAL-Test\Correct list\Adduct correct list-Pos-result-vs3.txt");

            //CtraceCorrectCheck.CheckingCtraceCount(@"D:\PROJECT_Plant Specialized Metabolites Annotations\All plant tissues\MSDIAL-Test\Correct list\Carbon Adduct correct list-Pos.txt",
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\All plant tissues\MSDIAL-Test\C grouping result",
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\All plant tissues\MSDIAL-Test\Correct list\Carbon correct list-Pos-result-vs3.txt");

            //CtraceCorrectCheck.CheckingAdductContent(@"D:\PROJECT_Plant Specialized Metabolites Annotations\All plant tissues\MSDIAL-Test\Correct list\Carbon Adduct correct list-Neg.txt",
            //   @"D:\PROJECT_Plant Specialized Metabolites Annotations\All plant tissues\MSDIAL-Test\Negative-Results-VS2",
            //   @"D:\PROJECT_Plant Specialized Metabolites Annotations\All plant tissues\MSDIAL-Test\Correct list\Adduct correct list-Neg-result-vs3.txt");

            //CtraceCorrectCheck.CheckingCtraceCount(@"D:\PROJECT_Plant Specialized Metabolites Annotations\All plant tissues\MSDIAL-Test\Correct list\Carbon Adduct correct list-Neg.txt",
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\All plant tissues\MSDIAL-Test\C grouping result",
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\All plant tissues\MSDIAL-Test\Correct list\Carbon correct list-Neg-result-vs3.txt");

            //CtraceCorrectCheck.CheckingAdductContentOfXcmsCamera(@"D:\PROJECT_Plant Specialized Metabolites Annotations\All plant tissues\MSDIAL-Test\Correct list\Carbon Adduct correct list-Neg.txt",
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\xcms-camera\neg-results",
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\xcms-camera\Adduct correct list-Neg-result.txt");

            //CtraceCorrectCheck.CheckingAdductContentOfXcmsCamera(@"D:\PROJECT_Plant Specialized Metabolites Annotations\All plant tissues\MSDIAL-Test\Correct list\Carbon Adduct correct list-Pos.txt",
            //   @"D:\PROJECT_Plant Specialized Metabolites Annotations\xcms-camera\pos-results",
            //   @"D:\PROJECT_Plant Specialized Metabolites Annotations\xcms-camera\Adduct correct list-Pos-result.txt");


            //Msdial.Lcms.Dataprocess.Algorithm.Clustering.EgdeGenerator.GenerateEdgesFromMsp(
            //  @"D:\PROJECT_Plant Specialized Metabolites Annotations\Molecular networking\Plant metabolome-Nodes-Neg-VS5.msp",
            //  @"D:\PROJECT_Plant Specialized Metabolites Annotations\Biotransformations.txt",
            //  0.025F,
            //  @"D:\PROJECT_Plant Specialized Metabolites Annotations\Molecular networking\Plant metabolome-Edges-Neg-VS5.txt");

            //Msdial.Lcms.Dataprocess.Algorithm.Clustering.EgdeGenerator.GenerateEdgesFromMsp(
            // @"D:\PROJECT_Plant Specialized Metabolites Annotations\Molecular networking\Plant metabolome-Nodes-Pos-VS5.msp",
            // @"D:\PROJECT_Plant Specialized Metabolites Annotations\Biotransformations.txt",
            // 0.025F,
            // @"D:\PROJECT_Plant Specialized Metabolites Annotations\Molecular networking\Plant metabolome-Edges-Pos-VS5.txt");

            //Msdial.Lcms.Dataprocess.Algorithm.Clustering.CorrelationClustering.TextFormatFileToEdgeList(
            //     @"C:\Users\hiroshi.tsugawa\Dropbox\Manuscript mapping plant metabolomes by mass spectrometry informatics\Supplementary files\correlations-root.txt",
            //     @"C:\Users\hiroshi.tsugawa\Dropbox\Manuscript mapping plant metabolomes by mass spectrometry informatics\Supplementary files\correlations-root-links.txt",
            //     80);
            //Msdial.Lcms.Dataprocess.Algorithm.Clustering.CorrelationClustering.TextFormatFileToEdgeList(
            //     @"C:\Users\hiroshi.tsugawa\Dropbox\Manuscript mapping plant metabolomes by mass spectrometry informatics\Supplementary files\correlations-shoot.txt",
            //     @"C:\Users\hiroshi.tsugawa\Dropbox\Manuscript mapping plant metabolomes by mass spectrometry informatics\Supplementary files\correlations-shoot-links.txt",
            //     80);
            //Msdial.Lcms.Dataprocess.Algorithm.Clustering.EgdeGenerator.GenerateEdgesFromMsp(
            //  @"D:\PROJECT_Plant Specialized Metabolites Annotations\Molecular networking\Plant metabolome-Nodes-Pos-VS2.msp",
            //  @"D:\PROJECT_Plant Specialized Metabolites Annotations\Biotransformations.txt",
            //  0.015F,
            //  @"D:\PROJECT_Plant Specialized Metabolites Annotations\Molecular networking\Plant metabolome-Edges-Pos.txt");
            #endregion

            #region msms cluster test
            //ValidateMsmsClustering.Run(@"D:\20170901-Okahashi-Faces-Msms\Neg\MspRecords-Neg-All500.msp"
            //    , @"D:\20170901-Okahashi-Faces-Msms\Neg\MspRecords-Neg-All-Edge500.txt");

            //ValidateMsmsClustering.Run(@"D:\20170901-Okahashi-Faces-Msms\Neg\MspRecords-Neg-Identified.msp"
            //    , @"D:\20170901-Okahashi-Faces-Msms\Neg\MspRecords-Neg-Identified-Edge.txt");

            //MsmsClustering.Run(@"D:\20170901-Okahashi-Faces-Msms\Pos\MspRecords-Pos-All500.msp"
            //   , @"D:\20170901-Okahashi-Faces-Msms\Pos\MspRecords-Pos-All-Edge500.txt");

            //ValidateMsmsClustering.Run(@"D:\20170901-Okahashi-Faces-Msms\Pos\MspRecords-Pos-Identified.msp"
            //    , @"D:\20170901-Okahashi-Faces-Msms\Pos\MspRecords-Pos-Identified-Edge.txt");

            //Msdial.Gcms.Dataprocess.Algorithm.EimsClustering.EimsSpectrumNetwork(@"D:\20170920-OsakaUniv-Cohort\Statistics\Spectrum.msp",
            //    @"D:\20170920-OsakaUniv-Cohort\Statistics\Spectrum.edge");

            //return -1;
            #endregion

            #region plant specialized metabolome annotation project
            //new TextAlignment().Aligner(System.IO.Directory.GetFiles(
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\Msdial-Msfinder-ResultMerged\Positive", "*.txt", System.IO.SearchOption.TopDirectoryOnly),
            //    0.2, 0.01,
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\Biotransformations.txt",
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\Msdial-Msfinder-ResultMerged\pos-node-0.2min-tol.txt",
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\Msdial-Msfinder-ResultMerged\pos-edge-0.2min-tol.txt");

            //new TextAlignment().Aligner(System.IO.Directory.GetFiles(
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\Msdial-Msfinder-ResultMerged\Negative", "*.txt", System.IO.SearchOption.TopDirectoryOnly),
            //    0.2, 0.01,
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\Biotransformations.txt",
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\Msdial-Msfinder-ResultMerged\neg-node-0.2min-tol.txt",
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\Msdial-Msfinder-ResultMerged\neg-edge-0.2min-tol.txt");

            //new TextAlignment().Aligner(System.IO.Directory.GetFiles(
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\Msdial-Msfinder-ResultMerged\Positive", "*.txt", System.IO.SearchOption.TopDirectoryOnly),
            //    0.1, 0.01,
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\Biotransformations.txt",
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\Msdial-Msfinder-ResultMerged\pos-node.txt",
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\Msdial-Msfinder-ResultMerged\pos-edge.txt");

            //Msdial.Lcms.Dataprocess.Algorithm.Clustering.EgdeGenerator.GenerateEdgesFromMsp(
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\Msdial-Msfinder-ResultMerged\Cytoscape-Pos-0.1RT tol\Plant chemical diversity in 11 plants-Pos-vs2.msp",
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\Biotransformations.txt",
            //    0.05F,
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\Msdial-Msfinder-ResultMerged\Cytoscape-Pos-0.1RT tol\Plant chemical diversity in 11 plants-Pos-vs2-edges.txt");

            //Msdial.Lcms.Dataprocess.Algorithm.Clustering.EgdeGenerator.GenerateEdgesFromMsp(
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\Msdial-Msfinder-ResultMerged\Version 3 results\Plant chemical diversity in 12 plants-Neg-VS3.msp",
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\Biotransformations.txt",
            //    0.025F,
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\Msdial-Msfinder-ResultMerged\Version 3 results\Plant chemical diversity in 12 plants-Neg-VS3-edge.txt");

            //Msdial.Lcms.Dataprocess.Algorithm.Clustering.EgdeGenerator.GenerateEdgesFromMsp(
            //  @"D:\PROJECT_Plant Specialized Metabolites Annotations\Msdial-Msfinder-ResultMerged\Version 3 results\Plant chemical diversity in 12 plants-Pos-VS3.msp",
            //  @"D:\PROJECT_Plant Specialized Metabolites Annotations\Biotransformations.txt",
            //  0.025F,
            //  @"D:\PROJECT_Plant Specialized Metabolites Annotations\Msdial-Msfinder-ResultMerged\Version 3 results\Plant chemical diversity in 12 plants-Pos-VS3-edge.txt");

            //Msdial.Lcms.Dataprocess.Algorithm.Clustering.EgdeGenerator.GenerateEdgesFromMsp(
            //  @"D:\20180412-RyoN-Alkaloid MSMS network\Alkaloid-network-MSMS.msp",
            //  @"D:\PROJECT_Plant Specialized Metabolites Annotations\Biotransformations.txt",
            //  0.015F,
            //  @"D:\20180412-RyoN-Alkaloid MSMS network\Alkaloid-network-MSMS-edge.txt");

            //Msdial.Lcms.Dataprocess.Algorithm.Clustering.EgdeGenerator.GenerateEdgesFromMsp(
            //    @"D:\20180228_Gowda_QTOF_MGFs\Neg_CE40\MSMS-Neg.msp",
            //    0.02F,
            //    @"D:\20180228_Gowda_QTOF_MGFs\Neg_CE40\MSMS-Neg-edge.txt");

            //Msdial.Lcms.Dataprocess.Algorithm.Clustering.EgdeGenerator.GenerateEdgesFromMsp(
            //    @"D:\20180220-Ariyasu-QTOF-MSMS\pos_mgf\MSMS-Pos.msp",
            //    0.02F,
            //    @"D:\20180220-Ariyasu-QTOF-MSMS\pos_mgf\MSMS-Pos-edge.txt");

            //Msdial.Lcms.Dataprocess.Algorithm.Clustering.EgdeGenerator.GenerateEdgesFromMsp(
            //    @"C:\Users\Hiroshi Tsugawa\Desktop\Hirata-san\20183161949_spectra_0.msp",
            //    0.02F,
            //    @"C:\Users\Hiroshi Tsugawa\Desktop\Hirata-san\MSMS-Pos-edge.txt");

            //Msdial.Lcms.Dataprocess.Algorithm.Clustering.EgdeGenerator.GenerateOntologyOrientedEdgesFromText(
            //    @"D:\PROJECT for MSFINDER\Classyfire results\Combined-ESD and MSMS-alkaloids.txt",
            //    @"D:\PROJECT for MSFINDER\Classyfire results\Combined-ESD and MSMS-nodes-alkaloids.txt",
            //    @"D:\PROJECT for MSFINDER\Classyfire results\Combined-ESD and MSMS-edges-alkaloids.txt");

            //new TextAlignment().Aligner(System.IO.Directory.GetFiles(
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\Msdial-Msfinder-ResultMerged\Positive", "*.txt", System.IO.SearchOption.TopDirectoryOnly),
            //    0.1, 0.01,
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\Biotransformations.txt",
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\Msdial-Msfinder-ResultMerged\pos-node.txt",
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\Msdial-Msfinder-ResultMerged\pos-edge.txt");

            //new TextAlignment().Aligner(System.IO.Directory.GetFiles(
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\Msdial-Msfinder-ResultMerged\Negative", "*.txt", System.IO.SearchOption.TopDirectoryOnly),
            //    0.1, 0.01,
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\Biotransformations.txt",
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\Msdial-Msfinder-ResultMerged\neg-node.txt",
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\Msdial-Msfinder-ResultMerged\neg-edge.txt");

            //var sim = OntologyClustering.ontologyStringSimilarity("flavonol O-glycosides", "flavone O-glycosides");
            ////var score = MsmsClustering.calculationTest(
            ////    @"D:\PROJECT_Plant Specialized Metabolites Annotations\Msdial-Msfinder-ResultMerged\Version 3 results\Plant chemical diversity in 12 plants-Neg-VS3\Query-1082_4-Methylthiobutyl glucosinolate.mat",
            ////    @"D:\PROJECT_Plant Specialized Metabolites Annotations\Msdial-Msfinder-ResultMerged\Version 3 results\Plant chemical diversity in 12 plants-Neg-VS3\Query-611_GM_Root_Neg-448.mat");

            //Console.WriteLine(sim);
            //Console.ReadLine();
            //return 1;
            #endregion
        }
    }
}
