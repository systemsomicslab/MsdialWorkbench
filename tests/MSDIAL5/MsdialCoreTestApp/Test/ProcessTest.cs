using CompMs.Common.Lipidomics;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.App.MsdialConsole.Test {
    internal class ProcessTest {
        public ProcessTest() { }
        public void Run() {

            var lcmsfile = @"D:\msdial_test\Msdial\out\wine\0717_kinetex_wine_50_4min_pos_IDA_A1.abf";
            var dimsfile = @"D:\msdial_test\Msdial\out\MSMSALL_Positive\20200717_Posi_MSMSALL_Liver1.abf";
            var immsfile = @"D:\msdial_test\Msdial\out\infusion_neg_timsON_pasef\kidney1_3times_timsON_pasef_neg000001.d";
            var lcimmsfile = @"D:\BugReport\20201216_MS2missing\PS78_Plasma1_4_1_4029.d";
            var samplefile = @"D:\infusion_project\Bruker_20210521_original\Bruker_20210521\infusion\timsOFF_pos\kidney1_1-47_1_14919.d";


            //MspCurator.ExtractCCSValues(
            //    @"E:\0_SourceCode\lbm_versions\Msp20230725171853_LBM\Msp20230725171853_conventional_converted_dev.lbm2",
            //    @"E:\0_SourceCode\lbm_versions\Msp20230725171853_LBM\Msp20230725171853_conventional_converted_dev_extracted.txt");

            //MspCurator.MatchInChIKeyAndAdductToExtractCCS(
            //    @"E:\0_SourceCode\lbm_versions\Msp20230725171853_LBM\20220725_timsTOFpro_TextLibrary_Brain_Neg.txt",
            //    @"E:\0_SourceCode\lbm_versions\Msp20230725171853_LBM\Msp20230725171853_conventional_converted_dev_extracted.txt",
            //    @"E:\0_SourceCode\lbm_versions\Msp20230725171853_LBM\20220725_timsTOFpro_TextLibrary_Brain_Neg_with_CCS.txt");

            //MspCurator.MatchNameAndAdductToExtractCCS(
            //    @"E:\0_SourceCode\lbm_versions\Msp20230725171853_LBM\20220725_timsTOFpro_TextLibrary_Brain_Neg.txt",
            //    @"E:\0_SourceCode\lbm_versions\Msp20230725171853_LBM\Msp20230725171853_conventional_converted_dev_extracted.txt",
            //    @"E:\0_SourceCode\lbm_versions\Msp20230725171853_LBM\20220725_timsTOFpro_TextLibrary_Brain_Neg_with_CCS_v2.txt");


            //DumpSpectrum(samplefile, 1206, 800, 100);

            //new FileParser().FastaParserTest(@"E:\6_Projects\PROJECT_Proteomics\jPOST_files_JPST000200.0\human_proteins_ref_wrong.fasta");
            //new EnzymesXmlRefParser().Read();
            //new ModificationsXmlRefParser().Read();

            //new TestProteomicsProcess().PDFTest();
            //new TestProteomicsProcess().ProcessTest();
            //MaldiMsProcessTest.TimsOffTest();
            //MaldiMsProcessTest.TimsOnTest();

            //FormulaStringParcer.Convert2FormulaObjV2("C6H12O6");
            //FormulaStringParcer.Convert2FormulaObjV2("CH3COONa");
            //FormulaStringParcer.Convert2FormulaObjV2("C2[13C]2O3Cl3");

            // Console.WriteLine("Lcms");
            // DumpN(lcmsfile, 50);
            // Console.WriteLine("Dims");
            // DumpN(dimsfile, 50);
            // Console.WriteLine("Imms");
            // DumpN(immsfile, 1000);
            // Console.WriteLine("LcImms");
            // DumpN(lcimmsfile, 1000);
            // RawDataDump.Dump(immsfile);
            // Console.WriteLine("Scan number 1359");
            // foreach (var spec in allspectra[1359].Spectrum)
            //     Console.WriteLine($"Mass = {spec.Mz}, Intensity = {spec.Intensity}");

            // var lipidGenerator = new LipidGenerator();
            // var spectrumGenerator = new PCSpectrumGenerator();
            // var adduct = Common.Parser.AdductIonParser.GetAdductIonBean("[M+H]+");
            // Common.Parser.MspFileParser.WriteAsMsp(
            //     @"D:\PROJECT_EAD\output\PC_ALL.msp",
            //     GeneratePCLipids()
            //         .SelectMany(lipid => lipid.Generate(lipidGenerator))
            //         .SelectMany(lipid => lipid.Generate(lipidGenerator))
            //         .Select(lipid => lipid.GenerateSpectrum(spectrumGenerator, adduct))
            //         .Cast<Common.Components.MoleculeMsReference>());


            // below is the curation process for standard compound spectra
            // step 1: export msp files from msdial project files
            //MsdialPrivateConsoleApp.CreateMspFileFromEadProject.Run(
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\AGC\NEG\20230105_agc_neg.mdproject",
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\agc_compounds.txt",
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\NEG_MSP");

            //MsdialPrivateConsoleApp.CreateMspFileFromEadProject.Run(
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\AGC\POS\20230105_agc_pos.mdproject",
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\agc_compounds.txt",
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\POS_MSP");

            //MsdialPrivateConsoleApp.CreateMspFileFromEadProject.Run(
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\RIKEN\POS\2022_11_09_01_52_59.mdproject",
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\riken_compounds.txt",
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\POS_MSP");

            //MsdialPrivateConsoleApp.CreateMspFileFromEadProject.Run(
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\RIKEN\NEG\20230105_riken_neg_stds.mdproject",
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\riken_compounds.txt",
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\NEG_MSP");

            // step 2: separate files by the collision information, then, merge them into one msp file
            //MspCurator.Batch_ExtractMSPsByCEField(@"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\POS_MSP\Curated",
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\POS_MSP\Analysis");

            //MspCurator.Batch_ExtractMSPsByCEField(@"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\NEG_MSP\Curated",
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\NEG_MSP\Analysis");

            // step 2': separate files by the collision information, then, merge them into one msp file with the filter of alkaloids
            //MspCurator.Batch_ExtractMSPsByCEField(@"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\POS_MSP\Curated",
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\POS_MSP\Analysis_Alkaloids", "Superclass_Alkaloids");

            //MspCurator.Batch_ExtractMSPsByCEField(@"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\NEG_MSP\Curated",
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\NEG_MSP\Analysis_Alkaloids", "Superclass_Alkaloids");


            // step 3: calculate spectrum entropy statistics
            //EadSpectraAnalysis.EadSpectraAnalysis.GenerateSpectralEntropyList(
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\NEG_MSP\Analysis",
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\NEG_MSP\Analysis\Entropy");

            //EadSpectraAnalysis.EadSpectraAnalysis.GenerateSpectralEntropyList(
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\POS_MSP\Analysis",
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\POS_MSP\Analysis\Entropy");

            //EadSpectraAnalysis.EadSpectraAnalysis.GenerateSpectralEntropyListAsSeparateFormat(
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\POS_MSP\Analysis",
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\all_compounds_list.txt",
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\all_compounds_entropies.txt");

            //EadSpectraAnalysis.EadSpectraAnalysis.Check144ExistenceInMspFiles(
            //   @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\POS_MSP\Analysis",
            //   @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\all_compounds_list.txt",
            //   @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\all_compounds_144Exists.txt");

            // step 4: generate molecular networking file
            //EadSpectraAnalysis.EadSpectraAnalysis.GenerateMoleculerSpectrumNetforkFilesByModifiedDotProductFunction(
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\POS_MSP\Analysis",
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\POS_MSP\Analysis\Network");
            //EadSpectraAnalysis.EadSpectraAnalysis.GenerateMoleculerSpectrumNetforkFilesByModifiedDotProductFunction(
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\NEG_MSP\Analysis",
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\NEG_MSP\Analysis\Network");

            //// step 4': generate molecular networking file for alkalod network
            //EadSpectraAnalysis.EadSpectraAnalysis.GenerateMoleculerSpectrumNetforkFilesByModifiedDotProductFunction(
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\POS_MSP\Analysis_Alkaloids",
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\POS_MSP\Analysis_Alkaloids\Network");
            //EadSpectraAnalysis.EadSpectraAnalysis.GenerateMoleculerSpectrumNetforkFilesByModifiedDotProductFunction(
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\NEG_MSP\Analysis_Alkaloids",
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\NEG_MSP\Analysis_Alkaloids\Network");

            // step 5': export true false count
            //EadSpectraAnalysis.EadSpectraAnalysis.CountTrueFalse(
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\NEG_MSP\Analysis_Alkaloids\Network",
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\NEG_MSP\Analysis_Alkaloids\TrueFalseCount.csv");
            //EadSpectraAnalysis.EadSpectraAnalysis.CountTrueFalse(
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\POS_MSP\Analysis_Alkaloids\Network",
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\POS_MSP\Analysis_Alkaloids\TrueFalseCount.csv");

            //EadSpectraAnalysis.EadSpectraAnalysis.CountTrueFalse(
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\NEG_MSP\Analysis\Network",
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\NEG_MSP\Analysis\TrueFalseCount.csv");
            //EadSpectraAnalysis.EadSpectraAnalysis.CountTrueFalse(
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\POS_MSP\Analysis\Network",
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\all\POS_MSP\Analysis\TrueFalseCount.csv");


            //EadSpectraAnalysis.EadSpectraAnalysis.TestMolecularNetworkingFunctions(
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\test\two_isorhamnetin_test.msp",
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\MSDIAL_1000samples_submit\test\result");

            //MspCurator.MergeMSPs(
            //    @"E:\7_MassSpecCuration\Distributed MSPs\20220715_msp_renew\msp\POS",
            //    @"E:\7_MassSpecCuration\Distributed MSPs\20220715_msp_renew\msp\POS_Private_MSP_list.txt",
            //    @"E:\7_MassSpecCuration\Distributed MSPs\20220715_msp_renew\msp\MSMS_Private_Pos_VS17.msp");

            //MspCurator.MergeMSPs(
            //    @"E:\7_MassSpecCuration\Distributed MSPs\20220715_msp_renew\msp\POS",
            //    @"E:\7_MassSpecCuration\Distributed MSPs\20220715_msp_renew\msp\POS_Public_MSP_exp_list.txt",
            //    @"E:\7_MassSpecCuration\Distributed MSPs\20220715_msp_renew\msp\MSMS_Public_EXP_Pos_VS17.msp");

            //MspCurator.MergeMSPs(
            //    @"E:\7_MassSpecCuration\Distributed MSPs\20220715_msp_renew\msp\POS",
            //    @"E:\7_MassSpecCuration\Distributed MSPs\20220715_msp_renew\msp\POS_Public_MSP_exp_bio_insilico_list.txt",
            //    @"E:\7_MassSpecCuration\Distributed MSPs\20220715_msp_renew\msp\MSMS_Public_ExpBioInsilico_Pos_VS17.msp");

            //MspCurator.MergeMSPs(
            //   @"E:\7_MassSpecCuration\Distributed MSPs\20220715_msp_renew\msp\NEG",
            //   @"E:\7_MassSpecCuration\Distributed MSPs\20220715_msp_renew\msp\NEG_Private_MSP_list.txt",
            //   @"E:\7_MassSpecCuration\Distributed MSPs\20220715_msp_renew\msp\MSMS_Private_NEG_VS17.msp");

            //MspCurator.MergeMSPs(
            //    @"E:\7_MassSpecCuration\Distributed MSPs\20220715_msp_renew\msp\NEG",
            //    @"E:\7_MassSpecCuration\Distributed MSPs\20220715_msp_renew\msp\NEG_Public_MSP_exp_list.txt",
            //    @"E:\7_MassSpecCuration\Distributed MSPs\20220715_msp_renew\msp\MSMS_Public_EXP_NEG_VS17.msp");

            //MspCurator.MergeMSPs(
            //    @"E:\7_MassSpecCuration\Distributed MSPs\20220715_msp_renew\msp\NEG",
            //    @"E:\7_MassSpecCuration\Distributed MSPs\20220715_msp_renew\msp\NEG_Public_MSP_exp_bio_insilico_list.txt",
            //    @"E:\7_MassSpecCuration\Distributed MSPs\20220715_msp_renew\msp\MSMS_Public_ExpBioInsilico_NEG_VS17.msp");

#if DEBUG
            //var projectPath = @"C:\Users\lab\Desktop\dropmet\20140809_MSDIAL_DemoFiles_Swath\neg\hoge20220427.mtd3";
            //var output = new MemoryStream();
            // using var output = File.Open(@"C:\Users\lab\Desktop\dropmet\output.tsv", FileMode.Create);
            // var tester = new Export.ExporterTest();
            // var curator = new PostCurator();
            //var curator = (PostCurator)null; // new PostCurator();
            //tester.Export(projectPath, output, curator);
            //Console.WriteLine(Encoding.UTF8.GetString(output.ToArray()));


            //var massqlTester = new MassQLTest();
            //massqlTester.Run();



#endif
            // casmi 2022
            //ParseArpanaDatabase.Convert2InChIKeyRtList(@"E:\6_Projects\PROJECT_CASMI2022\PFP_DB\original", @"E:\6_Projects\PROJECT_CASMI2022\PFP_DB\InChIKeyRtList.txt");
            //ParseArpanaDatabase.InsertSmiles2InChIKeyRtList(
            //    @"E:\6_Projects\PROJECT_CASMI2022\PFP_DB\InChIKeyRtList.txt",
            //    @"E:\6_Projects\PROJECT_CASMI2022\PFP_DB\MSMS-Neg-Vaniya-Fiehn_Natural_Products_Library_20200109.msp",
            //    @"E:\6_Projects\PROJECT_CASMI2022\PFP_DB\MSMS-Pos-Vaniya-Fiehn_Natural_Products_Library_20200109.msp",
            //    @"E:\6_Projects\PROJECT_CASMI2022\PFP_DB\InChIKeySmilesRtList.txt");

            // ParseArpanaDatabase.CreatInChIKeySmilesList(
            //     @"E:\6_Projects\PROJECT_CASMI2022\PFP_DB\MsfinderStructureDB-VS15.esd",
            //     @"E:\6_Projects\PROJECT_CASMI2022\PFP_DB\MSMS-RIKEN-Neg-VS15.msp",
            //     @"E:\6_Projects\PROJECT_CASMI2022\PFP_DB\MSMS-RIKEN-Pos-VS15.msp",
            //     @"E:\6_Projects\PROJECT_CASMI2022\PFP_DB\RIKEN_All_Smiles.txt");


            //MspCurator.WriteRtMzInChIKey(@"E:\7_MassSpecCuration\Distributed MSPs\MSMS-RIKEN-Neg-VS15_PfppRT.msp");
            //MspCurator.AddRT2MspQueries(@"E:\7_MassSpecCuration\Distributed MSPs\MSMS-RIKEN-Neg-VS15.msp", @"E:\6_Projects\2_metabolome_protocol\PFPP_NEG.txt");
            //MspCurator.AddRT2MspQueries(@"E:\7_MassSpecCuration\Distributed MSPs\MSMS-RIKEN-Pos-VS15.msp", @"E:\6_Projects\2_metabolome_protocol\PFPP_POS.txt");

            //RnaSeqProcess.Convert2Csv4ViolinPlot(
            //    @"E:\6_Projects\PROJECT_500cells\20220808_10cells_rnaseq\ViolinPlot\log_met_subtract_median.csv",
            //    @"E:\6_Projects\PROJECT_500cells\20220808_10cells_rnaseq\ViolinPlot\log_met_subtract_median_violin.csv");

            //RnaSeqProcess.Convert2Csv4ViolinPlot(
            //    @"E:\6_Projects\PROJECT_500cells\20220808_10cells_rnaseq\ViolinPlot\transcriptome.csv",
            //    @"E:\6_Projects\PROJECT_500cells\20220808_10cells_rnaseq\ViolinPlot\transcriptome_violin.csv");

            //RnaSeqProcess.Convert2Csv4ViolinPlot(
            //    @"E:\6_Projects\PROJECT_ImagingMS\EYE Project\ImagingMS\POS_result\lipidome_pos.csv",
            //    @"E:\6_Projects\PROJECT_ImagingMS\EYE Project\ImagingMS\POS_result\lipidome_pos_violin.csv");

            //RnaSeqProcess.Convert2Csv4ViolinPlot(
            //    @"E:\6_Projects\PROJECT_ImagingMS\EYE Project\ImagingMS\Result\table.csv",
            //    @"E:\6_Projects\PROJECT_ImagingMS\EYE Project\ImagingMS\Result\table_violin.csv");

            //RnaSeqProcess.Convert2Csv4ViolinPlot(
            //    @"E:\6_Projects\PROJECT_ImagingMS\EYE Project\pic_transcriptome\pic_table.csv",
            //    @"E:\6_Projects\PROJECT_ImagingMS\EYE Project\pic_transcriptome\pic_table_violin.csv");

            //CreateStatisticsInEieioProject.WriteSummary(
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\20220818_EIEIO_checked_MT\results",
            //    @"E:\6_Projects\PROJECT_SCIEXEAD\20220818_EIEIO_checked_MT\result.txt");


            // swai-kun for 13C plant table curation
            //LabelDataHandler.LabelDataHandler.ExtractCorrectPeakList(
            //    @"E:\6_Projects\PROJECT_GlycyrrhizaMetabolomics\glabra leaf\pos\pos_table_vs1.txt",
            //    @"E:\6_Projects\PROJECT_GlycyrrhizaMetabolomics\glabra leaf\pos");

            //LabelDataHandler.LabelDataHandler.GenerateMoleculerSpectrumNetforkFilesByModifiedDotProductFunction(
            //   @"E:\6_Projects\PROJECT_GlycyrrhizaMetabolomics\glabra leaf\pos\pos_table_vs1_extracted_12C_ms2contained.msp",
            //   @"E:\7_MassSpecCuration\Distributed MSPs\20220715_msp_renew\msp_ht\POS\MSMS-Pos-RIKEN_PlaSMA_Bio.msp",
            //   @"E:\6_Projects\PROJECT_GlycyrrhizaMetabolomics\glabra leaf\pos\cytoscape");

            //MoleculerSpectrumNetworkingTest.Run(
            //    @"E:\0_SourceCode\BugReports\20230209_molecularnetwork\NEG\after_rt_correction\2023281539_spectra_0.msp",
            //    @"E:\0_SourceCode\BugReports\20230209_molecularnetwork\NEG\after_rt_correction\", "4");

            //MoleculerSpectrumNetworkingTest.Run(
            //    @"E:\0_SourceCode\BugReports\20230209_molecularnetwork\NEG\before_rt_correction\beforeRTcorrect.msp",
            //    @"E:\0_SourceCode\BugReports\20230209_molecularnetwork\NEG\before_rt_correction\", "4");
        }

        private static void DumpN(string file, int n) {
            var allspectra = DataAccess.GetAllSpectra(file);
            Console.WriteLine($"Number of spectrum: {allspectra.Count}");
            Console.WriteLine($"Number of Ms1 spectrum {allspectra.Count(spec => spec.MsLevel == 1)}");
            Console.WriteLine($"Number of scan {allspectra.Where(spec => spec.MsLevel == 1).Select(spec => spec.ScanNumber).Distinct().Count()}");
            for (int i = 0; i < n; i++) {
                var spec = allspectra[i];
                Console.WriteLine("Original index={0} ID={1}, Time={2}, Drift ID={3}, Drift time={4}, Polarity={5}, MS level={6}, Precursor mz={7}, CollisionEnergy={8}, SpecCount={9}", spec.OriginalIndex, spec.ScanNumber, spec.ScanStartTime, spec.DriftScanNumber, spec.DriftTime, spec.ScanPolarity, spec.MsLevel, spec.Precursor?.SelectedIonMz ?? -1, spec.CollisionEnergy, spec.Spectrum.Length);
            }
        }

        private static void DumpSpectrum(string file, int scanNumber, double mz, double mztol) {
            var allspectra = DataAccess.GetAllSpectra(file);
            var spectra = allspectra.FirstOrDefault(spec => spec.ScanNumber == scanNumber);
            Console.WriteLine(
                "Original index={0} ID={1}, Time={2}, Drift ID={3}, Drift time={4}, Polarity={5}, MS level={6}, Precursor mz={7}, CollisionEnergy={8}, SpecCount={9}",
                spectra.OriginalIndex, spectra.ScanNumber, spectra.ScanStartTime, spectra.DriftScanNumber, spectra.DriftTime, spectra.ScanPolarity, spectra.MsLevel, spectra.Precursor?.SelectedIonMz ?? -1, spectra.CollisionEnergy, spectra.Spectrum.Length);
            foreach (var spec in spectra.Spectrum.SkipWhile(spec => spec.Mz < mz - mztol).TakeWhile(spec => spec.Mz < mz + mztol)) {
                Console.WriteLine($"Mz: {spec.Mz}\tIntensity: {spec.Intensity}");
            }
        }

        private static IEnumerable<ILipid> GeneratePCLipids() {
            var parser = new PCLipidParser();
            for (int i = 0; i < AcylCandidates.Count; i++) {
                (var carbon1, var bond1) = AcylCandidates[i];
                for (int j = i; j < AcylCandidates.Count; j++) {
                    (var carbon2, var bond2) = AcylCandidates[j];
                    yield return parser.Parse($"PC {carbon1}:{bond1}_{carbon2}:{bond2}");
                }
            }
        }

        private static List<(int, int)> AcylCandidates = new List<(int, int)>{
            (16, 0), (16, 1),
            (18, 0), (18, 1), (18, 2),
            (20, 3), (20, 4), (20, 5),
            (22, 5), (22, 6),
        };
    }
}
