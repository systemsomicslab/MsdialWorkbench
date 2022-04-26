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
            // args = new string[]
            // {
            //     "lcms"
            //     , "-i"
            //     , @"D:\test_data\wine\"
            //     , "-o"
            //     , @"D:\test_data\wine\"
            //     , "-m"
            //     , @"D:\test_data\wine\lcms_param.txt"
            //     , "-p"
            // };

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

            //MainProcess.Run(args);

            var lcmsfile = @"D:\msdial_test\Msdial\out\wine\0717_kinetex_wine_50_4min_pos_IDA_A1.abf";
            var dimsfile = @"D:\msdial_test\Msdial\out\MSMSALL_Positive\20200717_Posi_MSMSALL_Liver1.abf";
            var immsfile = @"D:\msdial_test\Msdial\out\infusion_neg_timsON_pasef\kidney1_3times_timsON_pasef_neg000001.d";
            var lcimmsfile = @"D:\BugReport\20201216_MS2missing\PS78_Plasma1_4_1_4029.d";
            var samplefile = @"D:\infusion_project\Bruker_20210521_original\Bruker_20210521\infusion\timsOFF_pos\kidney1_1-47_1_14919.d";
            //DumpSpectrum(samplefile, 1206, 800, 100);

            //new FileParser().FastaParserTest(@"E:\6_Projects\PROJECT_Proteomics\jPOST_files_JPST000200.0\human_proteins_ref_wrong.fasta");
            //new EnzymesXmlRefParser().Read();
            //new ModificationsXmlRefParser().Read();

            //new TestProteomicsProcess().PDFTest();
            //new TestProteomicsProcess().ProcessTest();

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

#if DEBUG
            //var projectPath = @"D:\msdial_test\Msdial\out\wine2\2022_01_05_13_17_26.mtd3";
            //var output = new MemoryStream();
            //// using var output = File.Open(@"D:\msdial_test\Msdial\out\wine2\output.tsv", FileMode.Create);
            //var tester = new Export.ExporterTest();
            var curator = (PostCurator)null; // new PostCurator();
            //tester.Export(projectPath, output, curator);
            //Console.WriteLine(Encoding.UTF8.GetString(output.ToArray()));
#endif

            //MspCurator.WriteRtMzInChIKey(@"E:\7_MassSpecCuration\Distributed MSPs\MSMS-RIKEN-Neg-VS15_PfppRT.msp");
            //MspCurator.AddRT2MspQueries(@"E:\7_MassSpecCuration\Distributed MSPs\MSMS-RIKEN-Neg-VS15.msp", @"E:\6_Projects\2_metabolome_protocol\PFPP_NEG.txt");
            //MspCurator.AddRT2MspQueries(@"E:\7_MassSpecCuration\Distributed MSPs\MSMS-RIKEN-Pos-VS15.msp", @"E:\6_Projects\2_metabolome_protocol\PFPP_POS.txt");
        }

        private static void DumpN(string file, int n) {
            var allspectra = DataAccess.GetAllSpectra(file);
            Console.WriteLine($"Number of spectrum: {allspectra.Count}");
            Console.WriteLine($"Number of Ms1 spectrum {allspectra.Count(spec => spec.MsLevel == 1)}");
            Console.WriteLine($"Number of scan {allspectra.Where(spec => spec.MsLevel == 1).Select(spec => spec.ScanNumber).Distinct().Count()}");
            for(int i = 0; i < n; i++) {
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
