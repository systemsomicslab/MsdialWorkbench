using CompMs.StructureFinder.NcdkDescriptor;
using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.StructureFinder.Fragmenter;
using Riken.Metabolomics.StructureFinder.SpectralAssigner;
using Riken.Metabolomics.StructureFinder.Statistics;
using Riken.Metabolomics.StructureFinder.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace StructureFinderConsoleApp
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {

            //Code4NPR2020.CalculatePrecursorMzVS2("", "");
            //Derivatization.Test();
            /// code for Natural Product Reports 2020
            //Code4NPR2020.GenerateStructureTableFromMSPs();
            //Code4NPR2020.ExtractClassyFireOntologies();
            //Code4NPR2020.CheckInChIKeyExistence();
            //Code4NPR2020.CheckCoverageOfMspMsfinder();
            //Code4NPR2020.CalculateTop50MostCommonFunctionalGroups2020();
            //Code4NPR2020.CheckCoverageOfTop50FG();
            //Code4NPR2020.CalculateInformationRichnessOfMsmsInEachSuperClass();
            //Code4NPR2020.GenerateFragmentStatisticsForEachOntology();
            //Code4NPR2020.GetHydrogenCorrectedFormulaStrings(@"D:\Paper of Natural Product Reports\Statistics\temp\list.txt", @"D:\Paper of Natural Product Reports\Statistics\temp\formula.txt", "-");
            //Code4NPR2020.GetStatisticsOfOntologyForEachPolarity();
            //Code4NPR2020.GenerateEdgesByTanimotoIndex(@"D:\Paper of Natural Product Reports\Statistics\alkaloids_descriptors.txt", @"D:\Paper of Natural Product Reports\Statistics\edge-2015-structure.txt");
            //Code4NPR2020.ExtractCCSValues(@"D:\Paper of Natural Product Reports\Statistics\node-2105.txt",
            //    @"D:\3_Project for ion mobility\20201214_zhiwei_ccs_library\MSDIAL_pred_ccs_201013.txt", @"D:\Paper of Natural Product Reports\Statistics\node-2105-ccs.txt");
            Code4NPR2020.Check144Existence(@"D:\Paper of Natural Product Reports\Statistics\node-2105.txt", @"D:\Paper of Natural Product Reports\Statistics\node-2105-144.txt");

            //Code4NPR2020.ExtractSubstructureContainingStructureQueries();
            //Code4NPR2020.CalculatePrecursorMz(@"E:\6_Projects\PROJECT_ImagingMS\Lipid reference library\temp.txt", @"E:\6_Projects\PROJECT_ImagingMS\Lipid reference library\temp_premz.txt");
            //Code4NPR2020.CheckPrecursorMzExistence();

            //BondPathStatistics.StatisticsOfMatchedFragmentIons(@"D:\9_Spectral library curations\Fragment curation\20200910\Pos\msp\", "-Pos");
            //BondPathStatistics.StatisticsOfMatchedFragmentIons(@"D:\9_Spectral library curations\Fragment curation\20200910\Neg\msp\", "-Neg");

            //For Negative data for Code4NPR2020
            //var files = System.IO.Directory.GetFiles(@"D:\9_Spectral library curations\Fragment curation\20200910\Neg\", "*.txt", SearchOption.TopDirectoryOnly);
            //foreach (var file in files) {
            //    var filename = System.IO.Path.GetFileNameWithoutExtension(file);
            //    var output = System.IO.Path.GetDirectoryName(file) + "\\" + filename + "-DR.txt";

            //    BondPathStatistics.RemoveDuplicatePIandNL(file, output);
            //    Console.WriteLine("Finishi: {0}", file);
            //}

            //BondPathStatistics.MergeFragmentInfo(System.IO.Directory.GetFiles(@"D:\9_Spectral library curations\Fragment curation\20200910\Neg\duplicate_removed\pi"
            //    , "*.txt", SearchOption.TopDirectoryOnly)
            //    , @"D:\9_Spectral library curations\Fragment curation\20200910\Neg\duplicate_removed\pi\Merged fragment info-PI-Neg.txt");

            //BondPathStatistics.MergeFragmentInfo(System.IO.Directory.GetFiles(@"D:\9_Spectral library curations\Fragment curation\20200910\Neg\duplicate_removed\nl"
            //   , "*.txt", SearchOption.TopDirectoryOnly)
            //   , @"D:\9_Spectral library curations\Fragment curation\20200910\Neg\duplicate_removed\nl\Merged fragment info-NL-Neg.txt");

            //BondPathStatistics.MergeInChIKeyToMergedFragmentInfoTable(
            //    @"D:\9_Spectral library curations\Fragment curation\20200910\Pos\duplicate_removed\pi\Merged fragment info-PI-Pos.txt",
            //    @"D:\9_Spectral library curations\Fragment curation\20200910\Pos\duplicate_removed\pi\Merged fragment info-PI-Pos-inchikey.txt",
            //    @"D:\9_Spectral library curations\Fragment curation\20200910\Pos\duplicate_removed\pi\Merged fragment info-PI-Pos-inchikey-merged.txt");

            //BondPathStatistics.MergeInChIKeyToMergedFragmentInfoTable(
            //   @"D:\9_Spectral library curations\Fragment curation\20200910\Neg\duplicate_removed\pi\Merged fragment info-PI-Neg.txt",
            //   @"D:\9_Spectral library curations\Fragment curation\20200910\Neg\duplicate_removed\pi\Merged fragment info-PI-Neg-inchikey.txt",
            //   @"D:\9_Spectral library curations\Fragment curation\20200910\Neg\duplicate_removed\pi\Merged fragment info-PI-Neg-inchikey-merged.txt");


            //BondPathStatistics.ExportFragmentStatistics(@"D:\9_Spectral library curations\Fragment curation\20200910\Pos\duplicate_removed\nl\Merged fragment info-NL-Pos.txt"
            //    , @"D:\9_Spectral library curations\Fragment curation\20200910\Pos\duplicate_removed\nl\Fragment statistics result-NL-Pos.txt");

            //BondPathStatistics.ExportFragmentStatistics(@"D:\9_Spectral library curations\Fragment curation\20200910\Pos\duplicate_removed\pi\Merged fragment info-PI-Pos-inchikey-merged.txt"
            //   , @"D:\9_Spectral library curations\Fragment curation\20200910\Pos\duplicate_removed\pi\Fragment statistics result-PI-Pos.txt");

            //BondPathStatistics.ExportFragmentStatistics(@"D:\9_Spectral library curations\Fragment curation\20200910\Neg\duplicate_removed\pi\Merged fragment info-PI-Neg-inchikey-merged.txt"
            //               , @"D:\9_Spectral library curations\Fragment curation\20200910\Neg\duplicate_removed\pi\Fragment statistics result-PI-Neg.txt");

            /// NCDK descriptor test
            //var smi = "O=C(O)CCCCC";
            //NcdkDescriptor.GenerateAllNCDKDescriptors(smi);

            ////Calculate bond statistics
            #region
            //BondPathStatistics.StatisticsOfMatchedFragmentIons(@"D:\MoNA and MassBank curations\Fragment database\Negative\MSPs\");
            //BondPathStatistics.StatisticsOfMatchedFragmentIons(@"D:\MoNA and MassBank curations\Fragment database\Positive\MSPs\");
            //BondPathStatistics.StatisticsOfMatchedFragmentIons(@"D:\MoNA and MassBank curations\MS fragmenter statistics\MSMS-AllPublic-Curated-Pos-MfKit-SeparatedMSPs\");
            //BondPathStatistics.StatisticsOfMatchedFragmentIons(@"D:\MoNA and MassBank curations\MS fragmenter statistics\MSMS-GNPS-Curated-Neg-MfKit-SeparatedMSPs\");
            //BondPathStatistics.StatisticsOfMatchedFragmentIons(@"D:\MoNA and MassBank curations\MS fragmenter statistics\MSMS-GNPS-Curated-Pos-MfKit-SeparatedMSPs\");
            //BondPathStatistics.StatisticsOfMatchedFragmentIons(@"D:\MoNA and MassBank curations\MS fragmenter statistics\MSMS-MassBank-Curated-Neg-MfKit-SeparatedMSPs\");
            //BondPathStatistics.StatisticsOfMatchedFragmentIons(@"D:\MoNA and MassBank curations\MS fragmenter statistics\MSMS-MassBank-Curated-Pos-MfKit-SeparatedMSPs\");
            //BondPathStatistics.StatisticsOfMatchedFragmentIons(@"D:\MoNA and MassBank curations\MS fragmenter statistics\MSMS-Metlin-PCDL-Neg-Curated-MfKit-SeparatedMSPs\");
            //BondPathStatistics.StatisticsOfMatchedFragmentIons(@"D:\MoNA and MassBank curations\MS fragmenter statistics\MSMS-Metlin-PCDL-Pos-Curated-MfKit-SeparatedMSPs\");
            //BondPathStatistics.StatisticsOfMatchedFragmentIons(@"D:\MoNA and MassBank curations\MS fragmenter statistics\MSMS-NIST-Curated-Neg-MfKit-SeparatedMSPs\");
            //BondPathStatistics.StatisticsOfMatchedFragmentIons(@"D:\MoNA and MassBank curations\MS fragmenter statistics\MSMS-NIST-Curated-Pos-MfKit-SeparatedMSPs\");
            //BondPathStatistics.StatisticsOfMatchedFragmentIons(@"D:\MoNA and MassBank curations\MS fragmenter statistics\MSMS-Respect-Curated-Neg-MfKit-SeparatedMSPs\");
            //BondPathStatistics.StatisticsOfMatchedFragmentIons(@"D:\MoNA and MassBank curations\MS fragmenter statistics\MSMS-Respect-Curated-Pos-MfKit-SeparatedMSPs\");
            //BondPathStatistics.StatisticsOfMatchedFragmentIons(@"D:\MoNA and MassBank curations\MS fragmenter statistics\MSMS-RIKEN-PlantMSMS-Pos-SeparatedMSPs\");
            //BondPathStatistics.StatisticsOfMatchedFragmentIons(@"D:\MoNA and MassBank curations\MS fragmenter statistics\MSMS-RIKEN-PlantMSMS-Neg-SeparatedMSPs\");
            //BondPathStatistics.StatisticsOfMatchedFragmentIons(@"C:\Users\tensa_000\Desktop\LipidBlast fragment validation\Mat files-Pos\");
            //BondPathStatistics.StatisticsOfMatchedFragmentIons(@"C:\Users\tensa_000\Desktop\LipidBlast fragment validation\Mat files-Neg\");
            #endregion

            ////Dupulicate remove methods
            #region
            //var files = System.IO.Directory.GetFiles(@"D:\MoNA and MassBank curations\MS fragmenter statistics\20170415-Result of descriptors-Step 1", "*.txt", SearchOption.AllDirectories);
            //foreach (var file in files) {
            //    var filename = System.IO.Path.GetFileNameWithoutExtension(file);
            //    var output = System.IO.Path.GetDirectoryName(file) + "\\" + filename + "-DR.txt";

            //    BondPathStatistics.RemoveDuplicateDL(file, output);
            //    Console.WriteLine("Finishi: {0}", file);
            //}

            //var files = System.IO.Directory.GetFiles(@"D:\MoNA and MassBank curations\Fragment database\Positive\", "*.txt", SearchOption.AllDirectories);
            //foreach (var file in files) {
            //    var filename = System.IO.Path.GetFileNameWithoutExtension(file);
            //    var output = System.IO.Path.GetDirectoryName(file) + "\\" + filename + "-DR.txt";

            //    BondPathStatistics.RemoveDuplicatePIandNL(file, output);
            //    Console.WriteLine("Finishi: {0}", file);
            //}

            //files = System.IO.Directory.GetFiles(@"D:\MoNA and MassBank curations\Fragment database\Negative\", "*.txt", SearchOption.AllDirectories);
            //foreach (var file in files) {
            //    var filename = System.IO.Path.GetFileNameWithoutExtension(file);
            //    var output = System.IO.Path.GetDirectoryName(file) + "\\" + filename + "-DR.txt";

            //    BondPathStatistics.RemoveDuplicatePIandNL(file, output);
            //    Console.WriteLine("Finishi: {0}", file);
            //}
            #endregion

            ////Product- and neutral loss statistics
            #region
            //BondPathStatistics.MergeFragmentInfo(System.IO.Directory.GetFiles(@"D:\MoNA and MassBank curations\Fragment database\Negative\PI statistics"
            //    , "*.txt", SearchOption.TopDirectoryOnly)
            //    , @"D:\MoNA and MassBank curations\Fragment database\Negative\PI statistics\Merged fragment info-PI-Neg.txt");

            //BondPathStatistics.MergeFragmentInfo(System.IO.Directory.GetFiles(@"D:\MoNA and MassBank curations\Fragment database\Negative\NL statistics"
            //   , "*.txt", SearchOption.TopDirectoryOnly)
            //   , @"D:\MoNA and MassBank curations\Fragment database\Negative\NL statistics\Merged fragment info-NL-Neg.txt");

            //BondPathStatistics.MergeFragmentInfo(System.IO.Directory.GetFiles(@"D:\MoNA and MassBank curations\Fragment database\Positive\PI statistics"
            //   , "*.txt", SearchOption.TopDirectoryOnly)
            //   , @"D:\MoNA and MassBank curations\Fragment database\Positive\PI statistics\Merged fragment info-PI-Pos.txt");


            //BondPathStatistics.MergeFragmentInfo(System.IO.Directory.GetFiles(@"D:\MoNA and MassBank curations\Fragment database\Positive\NL statistics"
            //   , "*.txt", SearchOption.TopDirectoryOnly)
            //   , @"D:\MoNA and MassBank curations\Fragment database\Positive\NL statistics\Merged fragment info-NL-Pos.txt");


            //for lipidblast
            //BondPathStatistics.MergeFragmentInfo(System.IO.Directory.GetFiles(@"C:\Users\tensa_000\Desktop\LipidBlast fragment validation\Product ion\Positive"
            //    , "*.txt", SearchOption.TopDirectoryOnly)
            //    , @"C:\Users\tensa_000\Desktop\LipidBlast fragment validation\Product ion\Positive\Merged fragment info-PI-Pos.txt");

            //BondPathStatistics.MergeFragmentInfo(System.IO.Directory.GetFiles(@"C:\Users\tensa_000\Desktop\LipidBlast fragment validation\Product ion\Negative"
            //   , "*.txt", SearchOption.TopDirectoryOnly)
            //   , @"C:\Users\tensa_000\Desktop\LipidBlast fragment validation\Product ion\Negative\Merged fragment info-PI-Neg.txt");

            //BondPathStatistics.MergeFragmentInfo(System.IO.Directory.GetFiles(@"C:\Users\tensa_000\Desktop\LipidBlast fragment validation\Neutral loss\Positive"
            //  , "*.txt", SearchOption.TopDirectoryOnly)
            //  , @"C:\Users\tensa_000\Desktop\LipidBlast fragment validation\Neutral loss\Positive\Merged fragment info-NL-Pos.txt");

            //BondPathStatistics.MergeFragmentInfo(System.IO.Directory.GetFiles(@"C:\Users\tensa_000\Desktop\LipidBlast fragment validation\Neutral loss\Negative"
            //  , "*.txt", SearchOption.TopDirectoryOnly)
            //  , @"C:\Users\tensa_000\Desktop\LipidBlast fragment validation\Neutral loss\Negative\Merged fragment info-NL-Neg.txt");




            ////after InChiKeys of fragments are generated...
            //BondPathStatistics.ExportFragmentStatistics(@"D:\MoNA and MassBank curations\Fragment database\Negative\PI statistics\Merged fragment info-PI-Neg.txt"
            //    , @"D:\MoNA and MassBank curations\Fragment database\Negative\PI statistics\Fragment statistics result-PI-Neg.txt");

            ////BondPathStatistics.ExportHrStatistics(@"D:\MoNA and MassBank curations\MS fragmenter statistics\20170802-Result of product ion-Step 1\Duplicate removed-Step 2\Negative\Merged fragment info-PI-Neg.txt"
            ////    , @"D:\MoNA and MassBank curations\MS fragmenter statistics\20170802-Result of product ion-Step 1\Duplicate removed-Step 2\Negative\HR statistics result-PI-Neg.txt");

            //BondPathStatistics.ExportFragmentStatistics(@"D:\MoNA and MassBank curations\Fragment database\Negative\NL statistics\Merged fragment info-NL-Neg.txt"
            //    , @"D:\MoNA and MassBank curations\Fragment database\Negative\NL statistics\Fragment statistics result-NL-Neg.txt");

            ////BondPathStatistics.ExportHrStatistics(@"D:\MoNA and MassBank curations\MS fragmenter statistics\20170802-Result of product ion-Step 1\Duplicate removed-Step 2\Positive\Merged fragment info-PI-Pos.txt"
            ////    , @"D:\MoNA and MassBank curations\MS fragmenter statistics\20170802-Result of product ion-Step 1\Duplicate removed-Step 2\Positive\HR statistics result-PI-Pos.txt");

            //BondPathStatistics.ExportFragmentStatistics(@"D:\MoNA and MassBank curations\Fragment database\Positive\PI statistics\Merged fragment info-PI-Pos.txt"
            //   , @"D:\MoNA and MassBank curations\Fragment database\Positive\PI statistics\Fragment statistics result-PI-Pos.txt");

            //BondPathStatistics.ExportFragmentStatistics(@"D:\MoNA and MassBank curations\Fragment database\Positive\NL statistics\Merged fragment info-NL-Pos.txt"
            //    , @"D:\MoNA and MassBank curations\Fragment database\Positive\NL statistics\Fragment statistics result-NL-Pos.txt");




            //for lipidblast
            //BondPathStatistics.ExportFragmentStatistics(@"C:\Users\tensa_000\Desktop\LipidBlast fragment validation\Product ion\Negative\Merged fragment info-PI-Neg.txt"
            //    , @"C:\Users\tensa_000\Desktop\LipidBlast fragment validation\Product ion\Negative\Fragment statistics result-PI-Neg.txt");

            //BondPathStatistics.ExportHrStatistics(@"C:\Users\tensa_000\Desktop\LipidBlast fragment validation\Product ion\Negative\Merged fragment info-PI-Neg.txt"
            //    , @"C:\Users\tensa_000\Desktop\LipidBlast fragment validation\Product ion\Negative\HR statistics result-PI-Neg.txt");

            //BondPathStatistics.ExportFragmentStatistics(@"C:\Users\tensa_000\Desktop\LipidBlast fragment validation\Product ion\Positive\Merged fragment info-PI-Pos.txt"
            //    , @"C:\Users\tensa_000\Desktop\LipidBlast fragment validation\Product ion\Positive\Fragment statistics result-PI-Pos.txt");

            //BondPathStatistics.ExportHrStatistics(@"C:\Users\tensa_000\Desktop\LipidBlast fragment validation\Product ion\Positive\Merged fragment info-PI-Pos.txt"
            //    , @"C:\Users\tensa_000\Desktop\LipidBlast fragment validation\Product ion\Positive\HR statistics result-PI-Pos.txt");

            //BondPathStatistics.ExportFragmentStatistics(@"C:\Users\tensa_000\Desktop\LipidBlast fragment validation\Neutral loss\Negative\Merged fragment info-NL-Neg.txt"
            //    , @"C:\Users\tensa_000\Desktop\LipidBlast fragment validation\Neutral loss\Negative\Fragment statistics result-NL-Neg.txt");

            //BondPathStatistics.ExportFragmentStatistics(@"C:\Users\tensa_000\Desktop\LipidBlast fragment validation\Neutral loss\Positive\Merged fragment info-NL-Pos.txt"
            //    , @"C:\Users\tensa_000\Desktop\LipidBlast fragment validation\Neutral loss\Positive\Fragment statistics result-NL-Pos.txt");


            #endregion

            ///generate descriptor statistics
            #region
            //var files = System.IO.Directory.GetFiles(@"D:\MoNA and MassBank curations\MS fragmenter statistics\20170217-Result of descriptors-Step 1\Duplicate removed-Step 2\Positive\", "*.txt", SearchOption.AllDirectories);
            //foreach (var file in files) {
            //    BondPathStatistics.DescriptorStatistics(file);
            //}

            //files = System.IO.Directory.GetFiles(@"D:\MoNA and MassBank curations\MS fragmenter statistics\20170217-Result of descriptors-Step 1\Duplicate removed-Step 2\Negative\", "*.txt", SearchOption.AllDirectories);
            //foreach (var file in files) {
            //    BondPathStatistics.DescriptorStatistics(file);
            //}

            //BondPathStatistics.MergeDescriptorStatistics(
            //    System.IO.Directory.GetFiles(@"D:\MoNA and MassBank curations\MS fragmenter statistics\20170217-Result of descriptors-Step 1\Duplicate removed-Step 2\Positive\Full layer statistics-Step 3"
            //    , "*.txt", SearchOption.AllDirectories)
            //    , @"D:\MoNA and MassBank curations\MS fragmenter statistics\20170217-Result of descriptors-Step 1\Duplicate removed-Step 2\Positive\Full layer statistics-Step 3.txt");

            //BondPathStatistics.MergeDescriptorStatistics(
            //    System.IO.Directory.GetFiles(@"D:\MoNA and MassBank curations\MS fragmenter statistics\20170217-Result of descriptors-Step 1\Duplicate removed-Step 2\Negative\Full layer statistics-Step 3"
            //    , "*.txt", SearchOption.AllDirectories)
            //    , @"D:\MoNA and MassBank curations\MS fragmenter statistics\20170217-Result of descriptors-Step 1\Duplicate removed-Step 2\Negative\Full layer statistics-Step 3.txt");

            //BondPathStatistics.MergeDescriptorStatistics(
            //    System.IO.Directory.GetFiles(@"D:\MoNA and MassBank curations\MS fragmenter statistics\20170217-Result of descriptors-Step 1\Duplicate removed-Step 2\Positive\Second layer statistics-Step 3"
            //    , "*.txt", SearchOption.AllDirectories)
            //    , @"D:\MoNA and MassBank curations\MS fragmenter statistics\20170217-Result of descriptors-Step 1\Duplicate removed-Step 2\Positive\Second layer statistics-Step 3.txt");

            //BondPathStatistics.MergeDescriptorStatistics(
            //    System.IO.Directory.GetFiles(@"D:\MoNA and MassBank curations\MS fragmenter statistics\20170217-Result of descriptors-Step 1\Duplicate removed-Step 2\Positive\First layer statistics-Step 3"
            //    , "*.txt", SearchOption.AllDirectories)
            //    , @"D:\MoNA and MassBank curations\MS fragmenter statistics\20170217-Result of descriptors-Step 1\Duplicate removed-Step 2\Positive\First layer statistics-Step 3.txt");

            //BondPathStatistics.MergeDescriptorStatistics(
            //    System.IO.Directory.GetFiles(@"D:\MoNA and MassBank curations\MS fragmenter statistics\20170217-Result of descriptors-Step 1\Duplicate removed-Step 2\Positive\Third layer statistics-Step 3"
            //    , "*.txt", SearchOption.AllDirectories)
            //    , @"D:\MoNA and MassBank curations\MS fragmenter statistics\20170217-Result of descriptors-Step 1\Duplicate removed-Step 2\Positive\Third layer statistics-Step 3.txt");

            //BondPathStatistics.MergeDescriptorStatistics(
            //    System.IO.Directory.GetFiles(@"D:\MoNA and MassBank curations\MS fragmenter statistics\20170217-Result of descriptors-Step 1\Duplicate removed-Step 2\Negative\Second layer statistics-Step 3"
            //    , "*.txt", SearchOption.AllDirectories)
            //    , @"D:\MoNA and MassBank curations\MS fragmenter statistics\20170217-Result of descriptors-Step 1\Duplicate removed-Step 2\Negative\Second layer statistics-Step 3.txt");

            //BondPathStatistics.MergeDescriptorStatistics(
            //    System.IO.Directory.GetFiles(@"D:\MoNA and MassBank curations\MS fragmenter statistics\20170217-Result of descriptors-Step 1\Duplicate removed-Step 2\Negative\First layer statistics-Step 3"
            //    , "*.txt", SearchOption.AllDirectories)
            //    , @"D:\MoNA and MassBank curations\MS fragmenter statistics\20170217-Result of descriptors-Step 1\Duplicate removed-Step 2\Negative\First layer statistics-Step 3.txt");

            //BondPathStatistics.MergeDescriptorStatistics(
            //    System.IO.Directory.GetFiles(@"D:\MoNA and MassBank curations\MS fragmenter statistics\20170217-Result of descriptors-Step 1\Duplicate removed-Step 2\Negative\Third layer statistics-Step 3"
            //    , "*.txt", SearchOption.AllDirectories)
            //    , @"D:\MoNA and MassBank curations\MS fragmenter statistics\20170217-Result of descriptors-Step 1\Duplicate removed-Step 2\Negative\Third layer statistics-Step 3.txt");

            //BondPathStatistics.MergeDescriptorPropStatistics(System.IO.Directory.GetFiles(@"D:\MoNA and MassBank curations\MS fragmenter statistics\20170217-Result of descriptors-Step 1\Duplicate removed-Step 2\Negative\Property statistics-Step 3"
            //    , "*.txt", SearchOption.AllDirectories)
            //    , @"D:\MoNA and MassBank curations\MS fragmenter statistics\20170217-Result of descriptors-Step 1\Duplicate removed-Step 2\Negative\Property statistics-Step 3.txt");

            //BondPathStatistics.MergeDescriptorPropStatistics(System.IO.Directory.GetFiles(@"D:\MoNA and MassBank curations\MS fragmenter statistics\20170217-Result of descriptors-Step 1\Duplicate removed-Step 2\Positive\Property statistics-Step 3"
            //   , "*.txt", SearchOption.AllDirectories)
            //   , @"D:\MoNA and MassBank curations\MS fragmenter statistics\20170217-Result of descriptors-Step 1\Duplicate removed-Step 2\Positive\Property statistics-Step 3.txt");
            #endregion

            //MergeFragmentInfoManager.Convert(
            //    @"C:\Users\tensa_000\Desktop\20170802-Result of product ion-Step 1\Duplicate removed-Step 2\Positive\Merged fragment info-PI-Pos.txt",
            //    @"C:\Users\tensa_000\Desktop\20170802-Result of neutral loss-Step 1\Duplicate removed-Step 2\Positive\Merged fragment info-NL-Pos.txt",
            //    @"C:\Users\tensa_000\Desktop\Fragment statistics 20170802\Frequent fragments-All.txt",
            //    @"C:\Users\tensa_000\Desktop\Fragment statistics 20170802\ParentFragmentsPairList_Pos.txt");

            //MergeFragmentInfoManager.Convert(
            //    @"C:\Users\tensa_000\Desktop\20170802-Result of product ion-Step 1\Duplicate removed-Step 2\Negative\Merged fragment info-PI-Neg.txt",
            //    @"C:\Users\tensa_000\Desktop\20170802-Result of neutral loss-Step 1\Duplicate removed-Step 2\Negative\Merged fragment info-NL-Neg.txt",
            //    @"C:\Users\tensa_000\Desktop\Fragment statistics 20170802\Frequent fragments-All.txt",
            //    @"C:\Users\tensa_000\Desktop\Fragment statistics 20170802\ParentFragmentsPairList_Neg.txt");

            //MergeFragmentInfoManager.Convert(
            //   @"C:\Users\tensa_000\Desktop\LipidBlast fragment validation\Product ion\Positive\Merged fragment info-PI-Pos.txt",
            //   @"C:\Users\tensa_000\Desktop\LipidBlast fragment validation\Neutral loss\Positive\Merged fragment info-NL-Pos.txt",
            //   @"C:\Users\tensa_000\Desktop\LipidBlast fragment validation\Fragment curations\Frequent fragments-All.txt",
            //   @"C:\Users\tensa_000\Desktop\LipidBlast fragment validation\Fragment curations\ParentFragmentsPairList_Pos.txt");


            //MergeFragmentInfoManager.Convert(
            //    @"C:\Users\tensa_000\Desktop\LipidBlast fragment validation\Product ion\Negative\Merged fragment info-PI-Neg.txt",
            //    @"C:\Users\tensa_000\Desktop\LipidBlast fragment validation\Neutral loss\Negative\Merged fragment info-NL-Neg.txt",
            //    @"C:\Users\tensa_000\Desktop\LipidBlast fragment validation\Fragment curations\Frequent fragments-All.txt",
            //    @"C:\Users\tensa_000\Desktop\LipidBlast fragment validation\Fragment curations\ParentFragmentsPairList_Neg.txt");


            //BondPathStatistics.StatisticsOfMatchedFragmentIons(@"D:\PROJECT for MSFINDER\Test molecules\");

            //var param = new AnalysisParamOfMsfinder() { Mass2Tolerance = 0.01F, MassTolType = MassToleranceType.Da, TreeDepth = 2 };

            //glutahione-pos
            //var molecule = RawDataParcer.RawDataFileReader(@"C:\Users\hiroshi.tsugawa\Dropbox\PRIMe homepage\MS-FINDER\MS-FINDER demo files\S-1-propenylmercaptoglutathione-Pos.mat", param);
            //reserpine-pos
            //var molecule = RawDataParcer.RawDataFileReader(@"C:\Users\hiroshi.tsugawa\Dropbox\PRIMe homepage\MS-FINDER\MS-FINDER demo files\Reserpine Trial 1022 Pos.msp", param);
            //deoxycitidinephosphate-neg
            //var molecule = RawDataParcer.RawDataFileReader(@"C:\Users\hiroshi.tsugawa\Dropbox\PRIMe homepage\MS-FINDER\MS-FINDER demo files\2-Deoxycytidine 5-diphosphate Trial 193 Neg.msp", param);
            //var molecule = RawDataParcer.RawDataFileReader(@"D:\MoNA and MassBank curations\MS fragmenter statistics\MSMS-AllPublic-Curated-Neg-MfKit-SeparatedMSPs\CKIJIGYDFNXSET_[M+K-2H]-_NA_LC-ESI-QTOF_UPLC Q-Tof Premier, Waters.msp", param);
            //var molecule = RawDataParcer.RawDataFileReader(@"D:\PROJECT for MSFINDER\Test molecules\Apigenin-Na adduct.msp", param);
            //var molecule = RawDataParcer.RawDataFileReader(@"D:\MoNA and MassBank curations\MS fragmenter statistics\MSMS-GNPS-Curated-Pos-MfKit-SeparatedMSPs\APKFDSVGJQXUKY_[M+H]+_NA_LC-ESI-qTof_LC-ESI-qTof.msp", param);
            var errorString = string.Empty;
            //var structure = MoleculeConverter.SmilesToStructure(molecule.Smiles, out errorString);
            //var fragments = FragmentGenerator.GetFragmentCandidates(structure, 2, (float)molecule.Ms2Spectrum.PeakList.Min(n => n.Mz));
            //var peakFragPairs = FragmentPeakMatcher.GetSpectralAssignmentResult(fragments, molecule.Ms2Spectrum.PeakList,
            //    AdductIonParcer.GetAdductIonBean(molecule.PrecursorType), param.TreeDepth,
            //    (float)param.Mass2Tolerance, param.MassTolType, molecule.IonMode);

            //var matchedNeutralLosses = new List<MatchedIon>();
            //var matchedProductIons = new List<MatchedIon>();
            //var unCleavedBondDescriptors = new List<string>();
            //BondStatistics.BondEnvironmentalFingerprintGenerator(structure, fragments, peakFragPairs, out matchedProductIons, out matchedNeutralLosses, out unCleavedBondDescriptors);

            //TG(22:6/22:6/22:6)
            //var smiles = @"OCC%20%30.CCCCCCCCC\C=C\C(O)%20.CCCCCCCCCC(O)CC(=O)N%30";
            //var structure = MoleculeConverter.SmilesToStructure(smiles, out errorString);
            //var inchikey = MoleculeConverter.AtomContainerToInChIKey(structure.IContainer);
            //var inchikey = MoleculeConverter.SmilesToInChIKey(structure.Smiles);

            //Console.WriteLine(structure.Inchikey);
            //Console.ReadLine();

            //var xlogP = XlogpCalculator.XlogP(structure);
            //Console.WriteLine(xlogP);

            ////Zeaxanthin: carotenoid
            //smiles = "CC1=C(C(CC(C1)O)(C)C)C=CC(=CC=CC(=CC=CC=C(C)C=CC=C(C)C=CC2=C(CC(CC2(C)C)O)C)C)C";
            //structure = MoleculeConverter.SmilesToStructure(smiles, out errorString);

            ////Alanine
            //smiles = "CC(C(=O)O)N";
            //structure = MoleculeConverter.SmilesToStructure(smiles, out errorString);


            //var smiles = @"O=C3C(OC2OC(COC1OC(C)C(O)C(O)C1(O))C(O)C(O)C2(O))=COC4=CC(O)=CC(O)=C34";
            //var structure = MoleculeConverter.SmilesToStructure(smiles, out errorString);

            //var inputfile = string.Empty;
            //var zeroPathFile = string.Empty;
            //var firstPathFile = string.Empty;
            //var secondPathFile = string.Empty;
            //var thirdPathFile = string.Empty;
            //var errorPath = string.Empty;

            //inputfile = @"D:\MoNA and MassBank curations\MS fragmenter statistics\Bond path statistics\RIKEN-MSMS-All-VS6-standadized.smiles";
            //zeroPathFile = @"D:\MoNA and MassBank curations\MS fragmenter statistics\Bond path statistics\RIKEN-MSMS-All-VS6-standadized-zero.txt";
            //firstPathFile = @"D:\MoNA and MassBank curations\MS fragmenter statistics\Bond path statistics\RIKEN-MSMS-All-VS6-standadized-first.txt";
            //secondPathFile = @"D:\MoNA and MassBank curations\MS fragmenter statistics\Bond path statistics\RIKEN-MSMS-All-VS6-standadized-second.txt";
            //thirdPathFile = @"D:\MoNA and MassBank curations\MS fragmenter statistics\Bond path statistics\RIKEN-MSMS-All-VS6-standadized-third.txt";
            //errorPath = @"D:\MoNA and MassBank curations\MS fragmenter statistics\Bond path statistics\RIKEN-MSMS-All-VS6-standadized-error.txt";

            //BondPathStatistics.Run(inputfile, zeroPathFile, firstPathFile, secondPathFile, thirdPathFile, errorPath);

            //inputfile = @"D:\MoNA and MassBank curations\MS fragmenter statistics\Bond path statistics\FindMetDB.smiles";
            //zeroPathFile = @"D:\MoNA and MassBank curations\MS fragmenter statistics\Bond path statistics\FindMetDB-zero.txt";
            //firstPathFile = @"D:\MoNA and MassBank curations\MS fragmenter statistics\Bond path statistics\FindMetDB-first.txt";
            //secondPathFile = @"D:\MoNA and MassBank curations\MS fragmenter statistics\Bond path statistics\FindMetDB-second.txt";
            //thirdPathFile = @"D:\MoNA and MassBank curations\MS fragmenter statistics\Bond path statistics\FindMetDB-third.txt";
            //errorPath = @"D:\MoNA and MassBank curations\MS fragmenter statistics\Bond path statistics\FindMetDB-error.txt";
            //BondPathStatistics.Run(inputfile, zeroPathFile, firstPathFile, secondPathFile, thirdPathFile, errorPath);

            //StructureRecognitionTest.ParcerTest(@"C:\Users\tensa_000\Desktop\Test.txt", @"C:\Users\tensa_000\Desktop\Test-result.txt");
        }
    }
}
