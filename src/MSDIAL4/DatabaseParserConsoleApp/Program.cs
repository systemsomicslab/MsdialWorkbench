using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.Lipidomics;
using Riken.Metabolomics.Lipidomics.Generator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseParserConsoleApp {
    class Program {
        static void Main(string[] args) {
            #region code for generating libraries
            //runLipidMassGenerator();
            #endregion

            runLipidChalacterization(); // characterize lipid structure by understanding MS/MS spectra
            //var results = MspFileParcer.MspFileReader(@"C:\Users\hiros\Downloads\MSMS-Pos-MRI_CES-3515.txt");
            //convertAsciiToBinary();

            // 

            //testLipidChalacterizationMethods();

            Console.WriteLine("Finish");
            Console.ReadLine();

        }

        private static void testLipidChalacterizationMethods() {

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            //var testFolder = @"C:\Users\igi\Desktop\Tsugawa-san_work\20190318\MSP-Neg";
            //var testFolder = @"D:\MSDial\LipidAnnotation_lib";
            var testFolder = @"D:\MSDial\test";
            var reffile = @"D:\MSDial\lipid_database\Library.txt";

            var testFiles = System.IO.Directory.GetFiles(testFolder, "*.msp", System.IO.SearchOption.TopDirectoryOnly);
            var logFile = testFolder + "\\log.txt";
            var ms1Tol = 0.01;
            var ms2Tol = 0.025;
            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + ms1Tol)); //calculate ppm @ m/z500

            var truepositive = 0;
            var truenegative = 0;
            var falsepositive = 0;
            var falsenegative = 0;
            var refMolecules = LipidLibraryParser.ReadLibrary(reffile);
            var total = testFiles.Length;
            var counter = 0;
            using (var sr = new StreamWriter(logFile, false, Encoding.ASCII)) {
                sr.WriteLine("ID\tQuery\tSuggest\tTrue positive\tTrue negative\tFalse positive\tFalse negative");
                foreach (var querypath in testFiles) {

                    var filename = System.IO.Path.GetFileNameWithoutExtension(querypath);
                    var query = LipidAnnotation.ReadTestSpectrum(querypath);
                    var spectrum = LipidAnnotation.ConvertToRequiredSpectrumFormat(query.Ms2Spectrum.PeakList);
                    var massaccuracy = ms1Tol;
                    if (query.PrecursorMz > 500) {
                        massaccuracy = (float)MolecularFormulaUtility.ConvertPpmToMassAccuracy(query.PrecursorMz, ppm);
                    }
                    var cMolecule = LipidAnnotation.Characterize(query.PrecursorMz, query.RetentionTime, spectrum,
                        refMolecules, query.IonMode, massaccuracy, ms2Tol);

                    var characterizedString = "Unknown";
                    if (cMolecule == null)
                    {

                    }
                    else if (cMolecule.AnnotationLevel == 1) {
                        characterizedString = cMolecule.SublevelLipidName + "; " + cMolecule.Adduct.AdductIonName;
                    }
                    else if (cMolecule.AnnotationLevel == 2) {
                        if (cMolecule.SublevelLipidName == cMolecule.LipidName)
                            characterizedString = cMolecule.LipidName + "; " + cMolecule.Adduct.AdductIonName;
                        else
                            characterizedString = cMolecule.SublevelLipidName + "; " + cMolecule.LipidName + "; " + cMolecule.Adduct.AdductIonName;
                    }

                    if (query.Name == "Unknown") {
                        if (characterizedString != "Unknown") {
                            falsepositive++;
                            sr.WriteLine(filename + "\t" + query.Name + "\t" + characterizedString + "\t" + "0\t0\t1\t0");
                        }
                        else {
                            truenegative++;
                            sr.WriteLine(filename + "\t" + query.Name + "\t" + characterizedString + "\t" + "0\t1\t0\t0");
                        }
                    }
                    else {
                        if (characterizedString != query.Name) {
                            falsenegative++;
                            sr.WriteLine(filename + "\t" + query.Name + "\t" + characterizedString + "\t" + "0\t0\t0\t1");
                        }
                        else {
                            truepositive++;
                            sr.WriteLine(filename + "\t" + query.Name + "\t" + characterizedString + "\t" + "1\t0\t0\t0");

                        }
                    }

                    counter++;
                    if (!Console.IsOutputRedirected) {
                        Console.Write("{0} / {1}", counter, total);
                        Console.SetCursorPosition(0, Console.CursorTop);
                    }
                    else {
                        Console.WriteLine("{0} / {1}", counter, total);
                    }
                }
            }

            var precision = Math.Round((double)truepositive / (double)(truepositive + falsepositive) * 100.0, 3);
            var recall = Math.Round((double)truepositive / (double)(truepositive + falsenegative) * 100.0, 3);
            var fscore = Math.Round((double)truepositive / (double)(truepositive + (falsenegative + falsepositive) * 0.5) * 100.0, 3);

            Console.WriteLine("True positive\t{0}", truepositive);
            Console.WriteLine("True negative\t{0}", truenegative);
            Console.WriteLine("False positive\t{0}", falsepositive);
            Console.WriteLine("False negative\t{0}", falsenegative);
            Console.WriteLine("Precision\t{0}", precision);
            Console.WriteLine("Recall\t{0}", recall);
            Console.WriteLine("F score\t{0}", fscore);

            var endtime = stopwatch.ElapsedMilliseconds / 1000 / 60.0;
            Console.WriteLine(Math.Round(endtime, 5) + " min");
        }

        private static void convertAsciiToBinary() {
            //test
            //LipidomicsConverter.AsciiToSerializedObject(
            //    @"C:\Users\ADMIN\Desktop\MSDIAL-LipidDB-VS47-AritaM-replaced.lbm",
            //    @"C:\Users\ADMIN\Desktop\LipidMsmsBinaryDB-VS47-AritaM.lbm2");
            LipidomicsConverter.AsciiToSerializedObject(
                @"C:\Users\hiroshi.tsugawa\Desktop\MSDIAL_LipidMsmsCreater\Msp20200812154316.jointedmsp",
                @"C:\Users\hiroshi.tsugawa\Desktop\MSDIAL_LipidMsmsCreater\Msp20200812154316.lbm2");
            //LipidomicsConverter.AsciiToSerializedObject(
            //    @"C:\Users\ADMIN\Desktop\MSDIAL-LipidDB-VS47-SaitoK.lbm",
            //    @"C:\Users\ADMIN\Desktop\LipidMsmsBinaryDB-VS47-SaitoK.lbm");
        }

        private static void runLipidChalacterization() {

            var testFolder = @"D:\mikikot\Desktop\Tsugawa-san_work\20230328_UltimateSPLASH\check_ceramide\";
            var testFilename = @"\GM1_181_180.msp";
            var querypath = testFolder + testFilename;
            var reffile = testFolder + @"\library.txt";

            var query = LipidAnnotation.ReadTestSpectrum(querypath);
            var refMolecules = LipidLibraryParser.ReadLibrary(reffile);
            var spectrum = LipidAnnotation.ConvertToRequiredSpectrumFormat(query.Ms2Spectrum.PeakList);
            var characterizedMolecule = LipidAnnotation.Characterize(query.PrecursorMz, query.RetentionTime, spectrum,
                refMolecules, query.IonMode, 0.05, 0.05);
            Console.WriteLine("Done");
            Console.ReadLine();
        }

        private static void runLipidMassGenerator() {
            var path = @"D:\takahashi\desktop\Tsugawa-san_work\20211026_sterolPHex\";
            //LipidMassLibraryGenerator.Run(path, LbmClass.PC, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.PC, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.PE, AdductIonParcer.GetAdductIonBean("[M-H]-"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.PS, AdductIonParcer.GetAdductIonBean("[M-H]-"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.PG, AdductIonParcer.GetAdductIonBean("[M-H]-"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.PG, AdductIonParcer.GetAdductIonBean("[M+Na]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.PG, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.PI, AdductIonParcer.GetAdductIonBean("[M-H]-"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.SM, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 6, 90, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.SM, AdductIonParcer.GetAdductIonBean("[M+Na]+"), 6, 90, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.SM, AdductIonParcer.GetAdductIonBean("[M+H]+"), 6, 90, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.TG, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 6, 132, 0, 36, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.TG, AdductIonParcer.GetAdductIonBean("[M+Na]+"), 6, 132, 0, 36, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.LNAPE, AdductIonParcer.GetAdductIonBean("[M-H]-"), 4, 88, 0, 24, 0);
            //// add MT
            //LipidMassLibraryGenerator.Run(path, LbmClass.PC, AdductIonParcer.GetAdductIonBean("[M+H]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.PC, AdductIonParcer.GetAdductIonBean("[M+Na]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.PE, AdductIonParcer.GetAdductIonBean("[M+H]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.PE, AdductIonParcer.GetAdductIonBean("[M+Na]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.PS, AdductIonParcer.GetAdductIonBean("[M+H]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.PS, AdductIonParcer.GetAdductIonBean("[M+Na]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.PI, AdductIonParcer.GetAdductIonBean("[M+Na]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.PI, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.DG, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.DG, AdductIonParcer.GetAdductIonBean("[M+Na]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.MG, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.FA, AdductIonParcer.GetAdductIonBean("[M-H]-"), 2, 44, 0, 22, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.FAHFA, AdductIonParcer.GetAdductIonBean("[M-H]-"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.LPC, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.LPC, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.LPC, AdductIonParcer.GetAdductIonBean("[M+H]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.LPC, AdductIonParcer.GetAdductIonBean("[M+Na]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.LPE, AdductIonParcer.GetAdductIonBean("[M-H]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.LPE, AdductIonParcer.GetAdductIonBean("[M+H]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.LPE, AdductIonParcer.GetAdductIonBean("[M+Na]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.LPG, AdductIonParcer.GetAdductIonBean("[M-H]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.LPI, AdductIonParcer.GetAdductIonBean("[M-H]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.LPS, AdductIonParcer.GetAdductIonBean("[M-H]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.LPA, AdductIonParcer.GetAdductIonBean("[M-H]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.PA, AdductIonParcer.GetAdductIonBean("[M-H]-"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.MGDG, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.MGDG, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.MGDG, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.MGDG, AdductIonParcer.GetAdductIonBean("[M+Na]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.SQDG, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.SQDG, AdductIonParcer.GetAdductIonBean("[M-H]-"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.DGDG, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.DGDG, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.DGDG, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.DGDG, AdductIonParcer.GetAdductIonBean("[M+Na]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.DGTS, AdductIonParcer.GetAdductIonBean("[M+H]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.LDGTS, AdductIonParcer.GetAdductIonBean("[M+H]+"), 2, 44, 0, 22, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.HBMP, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 6, 132, 0, 36, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.HBMP, AdductIonParcer.GetAdductIonBean("[M-H]-"), 6, 132, 0, 36, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.BMP, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.CAR, AdductIonParcer.GetAdductIonBean("[M]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.DGGA, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.DGGA, AdductIonParcer.GetAdductIonBean("[M-H]-"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.ADGGA, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 6, 132, 0, 36, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.ADGGA, AdductIonParcer.GetAdductIonBean("[M-H]-"), 6, 132, 0, 36, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.CL, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 8, 176, 0, 48, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.CL, AdductIonParcer.GetAdductIonBean("[M-H]-"), 8, 176, 0, 48, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.CL, AdductIonParcer.GetAdductIonBean("[M-2H]2-"), 8, 176, 0, 48, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.CE, AdductIonParcer.GetAdductIonBean("[M+Na]+"), 2, 44, 0, 22, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.CE, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 2, 44, 0, 22, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherPE, AdductIonParcer.GetAdductIonBean("[M+H]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherPE, AdductIonParcer.GetAdductIonBean("[M-H]-"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherPC, AdductIonParcer.GetAdductIonBean("[M+H]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherPC, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherPC, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 4, 88, 0, 24, 0);
            //// ceramide
            //LipidMassLibraryGenerator.Run(path, LbmClass.SM, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 6, 90, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_AP, AdductIonParcer.GetAdductIonBean("[M+H]+"), 6, 90, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_AP, AdductIonParcer.GetAdductIonBean("[M-H]-"), 6, 90, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_AP, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 6, 90, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_AP, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 6, 90, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_ADS, AdductIonParcer.GetAdductIonBean("[M-H]-"), 6, 90, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_ADS, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 6, 90, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_ADS, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 6, 90, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_AS, AdductIonParcer.GetAdductIonBean("[M-H]-"), 6, 90, 1, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_AS, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 6, 90, 1, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_AS, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 6, 90, 1, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_NP, AdductIonParcer.GetAdductIonBean("[M-H]-"), 6, 90, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_NP, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 6, 90, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_NP, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 6, 90, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_NDS, AdductIonParcer.GetAdductIonBean("[M+H]+"), 6, 90, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_NDS, AdductIonParcer.GetAdductIonBean("[M-H]-"), 6, 90, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_NDS, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 6, 90, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_NDS, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 6, 90, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_NS, AdductIonParcer.GetAdductIonBean("[M+H]+"), 6, 90, 1, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_NS, AdductIonParcer.GetAdductIonBean("[M-H]-"), 6, 90, 1, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_NS, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 6, 90, 1, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_NS, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 6, 90, 1, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_BDS, AdductIonParcer.GetAdductIonBean("[M-H]-"), 6, 90, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_BDS, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 6, 90, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_BDS, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 6, 90, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_BS, AdductIonParcer.GetAdductIonBean("[M-H]-"), 6, 90, 1, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_BS, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 6, 90, 1, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_BS, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 6, 90, 1, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.HexCer_AP, AdductIonParcer.GetAdductIonBean("[M+H]+"), 6, 90, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.HexCer_AP, AdductIonParcer.GetAdductIonBean("[M-H]-"), 6, 90, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.HexCer_AP, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 6, 90, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.HexCer_AP, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 6, 90, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.HexCer_NDS, AdductIonParcer.GetAdductIonBean("[M+H]+"), 6, 90, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.HexCer_NDS, AdductIonParcer.GetAdductIonBean("[M-H]-"), 6, 90, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.HexCer_NDS, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 6, 90, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.HexCer_NDS, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 6, 90, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.HexCer_NS, AdductIonParcer.GetAdductIonBean("[M+H]+"), 6, 90, 1, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.HexCer_NS, AdductIonParcer.GetAdductIonBean("[M-H]-"), 6, 90, 1, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.HexCer_NS, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 6, 90, 1, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.HexCer_NS, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 6, 90, 1, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_EODS, AdductIonParcer.GetAdductIonBean("[M-H]-"), 8, 134, 0, 36, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_EODS, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 8, 134, 0, 36, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_EODS, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 8, 134, 0, 36, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_EOS, AdductIonParcer.GetAdductIonBean("[M-H]-"), 8, 134, 1, 36, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_EOS, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 8, 134, 1, 36, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_EOS, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 8, 134, 1, 36, 0);
            ////OxPLs
            //LipidMassLibraryGenerator.Run(path, LbmClass.OxFA, AdductIonParcer.GetAdductIonBean("[M-H]-"), 2, 44, 0, 12, 6);
            //LipidMassLibraryGenerator.Run(path, LbmClass.OxPC, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 4, 88, 0, 24, 12);
            //LipidMassLibraryGenerator.Run(path, LbmClass.OxPC, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 4, 88, 0, 24, 12);
            //LipidMassLibraryGenerator.Run(path, LbmClass.OxPE, AdductIonParcer.GetAdductIonBean("[M-H]-"), 4, 88, 0, 24, 12);
            //LipidMassLibraryGenerator.Run(path, LbmClass.OxPG, AdductIonParcer.GetAdductIonBean("[M-H]-"), 4, 88, 0, 24, 12);
            //LipidMassLibraryGenerator.Run(path, LbmClass.OxPI, AdductIonParcer.GetAdductIonBean("[M-H]-"), 4, 88, 0, 24, 12);
            //LipidMassLibraryGenerator.Run(path, LbmClass.OxPS, AdductIonParcer.GetAdductIonBean("[M-H]-"), 4, 88, 0, 24, 12);
            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherOxPC, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 4, 88, 0, 24, 12);
            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherOxPC, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 4, 88, 0, 24, 12);
            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherOxPE, AdductIonParcer.GetAdductIonBean("[M-H]-"), 4, 88, 0, 24, 12);
            ////others
            //LipidMassLibraryGenerator.Run(path, LbmClass.PEtOH, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.PEtOH, AdductIonParcer.GetAdductIonBean("[M-H]-"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.PMeOH, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.PMeOH, AdductIonParcer.GetAdductIonBean("[M-H]-"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.PBtOH, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.PBtOH, AdductIonParcer.GetAdductIonBean("[M-H]-"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.GM3, AdductIonParcer.GetAdductIonBean("[M+H]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.GM3, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.GM3, AdductIonParcer.GetAdductIonBean("[M-H]-"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.LNAPS, AdductIonParcer.GetAdductIonBean("[M-H]-"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.LNAPS, AdductIonParcer.GetAdductIonBean("[M+H]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.LNAPS, AdductIonParcer.GetAdductIonBean("[M+Na]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.LNAPE, AdductIonParcer.GetAdductIonBean("[M+H]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.LNAPE, AdductIonParcer.GetAdductIonBean("[M+Na]+"), 4, 88, 0, 24, 0);
            ////
            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherLPE, AdductIonParcer.GetAdductIonBean("[M+H]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherLPE, AdductIonParcer.GetAdductIonBean("[M-H]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherLPC, AdductIonParcer.GetAdductIonBean("[M+H]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherLPC, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherLPC, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Hex2Cer, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 6, 90, 1, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Hex2Cer, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 6, 90, 1, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Hex2Cer, AdductIonParcer.GetAdductIonBean("[M+H]+"), 6, 90, 1, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Hex3Cer, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 6, 90, 1, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Hex3Cer, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 6, 90, 1, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Hex3Cer, AdductIonParcer.GetAdductIonBean("[M+H]+"), 6, 90, 1, 24, 0);
            ////LipidMassLibraryGenerator.Run(path, LbmClass.AcylCer_BDS, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 8, 134, 0, 36, 0);
            ////LipidMassLibraryGenerator.Run(path, LbmClass.AcylCer_BDS, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 8, 134, 0, 36, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.AHexCer, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 8, 134, 1, 36, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.AHexCer, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 8, 134, 1, 36, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.ASM, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 8, 134, 1, 36, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.ASM, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 8, 134, 1, 36, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_OS, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 6, 90, 1, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Cer_OS, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 6, 90, 1, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.MLCL, AdductIonParcer.GetAdductIonBean("[M-H]-"), 6, 132, 0, 36, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.DLCL, AdductIonParcer.GetAdductIonBean("[M-H]-"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.PhytoSph, AdductIonParcer.GetAdductIonBean("[M+H]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.Sph, AdductIonParcer.GetAdductIonBean("[M+H]+"), 2, 44, 1, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.DHSph, AdductIonParcer.GetAdductIonBean("[M+H]+"), 2, 44, 0, 0, 0);
            //// add 31/1/19
            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherMGDG, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherMGDG, AdductIonParcer.GetAdductIonBean("[M+Na]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherMGDG, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherMGDG, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherDGDG, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherDGDG, AdductIonParcer.GetAdductIonBean("[M+Na]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherDGDG, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherDGDG, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherTG, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 6, 132, 0, 36, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherTG, AdductIonParcer.GetAdductIonBean("[M+Na]+"), 6, 132, 0, 36, 0);
            //// add 14/2/19
            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherPC, AdductIonParcer.GetAdductIonBean("[M+Na]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherPE, AdductIonParcer.GetAdductIonBean("[M+Na]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherLPC, AdductIonParcer.GetAdductIonBean("[M+Na]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherLPE, AdductIonParcer.GetAdductIonBean("[M+Na]+"), 4, 88, 0, 24, 0);
            //// add 1/3/19
            //LipidMassLibraryGenerator.Run(path, LbmClass.HexCer_EOS, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 8, 134, 1, 36, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.HexCer_EOS, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 8, 134, 1, 36, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.HexCer_EOS, AdductIonParcer.GetAdductIonBean("[M+H]+"), 8, 134, 1, 36, 0);
            //// add 10/04/19
            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherPI, AdductIonParcer.GetAdductIonBean("[M-H]-"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherPS, AdductIonParcer.GetAdductIonBean("[M-H]-"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.PE_Cer, AdductIonParcer.GetAdductIonBean("[M-H]-"), 4, 88, 0, 24, 0);
            //// add 13/05/19
            //LipidMassLibraryGenerator.Run(path, LbmClass.DCAE, AdductIonParcer.GetAdductIonBean("[M-H]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.GDCAE, AdductIonParcer.GetAdductIonBean("[M-H]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.GLCAE, AdductIonParcer.GetAdductIonBean("[M-H]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.TDCAE, AdductIonParcer.GetAdductIonBean("[M-H]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.TLCAE, AdductIonParcer.GetAdductIonBean("[M-H]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.NAE, AdductIonParcer.GetAdductIonBean("[M+H]+"), 2, 44, 0, 12, 0);

            //LipidMassLibraryGenerator.Run(path, LbmClass.NAGly, AdductIonParcer.GetAdductIonBean("[M+H]+"), 4, 44, 0, 6, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.NAGly, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 4, 44, 0, 6, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.NAGly, AdductIonParcer.GetAdductIonBean("[M-H]-"), 4, 44, 0, 6, 0);

            //LipidMassLibraryGenerator.Run(path, LbmClass.NAGlySer, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 4, 44, 0, 6, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.NAGlySer, AdductIonParcer.GetAdductIonBean("[M-H]-"), 4, 44, 0, 6, 0);

            //LipidMassLibraryGenerator.Run(path, LbmClass.NAOrn, AdductIonParcer.GetAdductIonBean("[M+H]+"), 4, 44, 0, 6, 0);

            //LipidMassLibraryGenerator.Run(path, LbmClass.SL, AdductIonParcer.GetAdductIonBean("[M+H]+"), 4, 88, 0, 24, 1);
            //LipidMassLibraryGenerator.Run(path, LbmClass.SL, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 4, 88, 0, 24, 1);
            //LipidMassLibraryGenerator.Run(path, LbmClass.SL, AdductIonParcer.GetAdductIonBean("[M-H]-"), 4, 88, 0, 24, 1);

            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherPG, AdductIonParcer.GetAdductIonBean("[M-H]-"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.EtherLPG, AdductIonParcer.GetAdductIonBean("[M-H]-"), 2, 44, 0, 12, 0);

            //LipidMassLibraryGenerator.Run(path, LbmClass.PI_Cer, AdductIonParcer.GetAdductIonBean("[M-H]-"), 4, 88, 0, 24, 1);
            //LipidMassLibraryGenerator.Run(path, LbmClass.PI_Cer, AdductIonParcer.GetAdductIonBean("[M+H]+"), 4, 88, 0, 24, 1);

            //LipidMassLibraryGenerator.Run(path, LbmClass.SHexCer, AdductIonParcer.GetAdductIonBean("[M+H]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.SHexCer, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 4, 88, 0, 24, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.SHexCer, AdductIonParcer.GetAdductIonBean("[M-H]-"), 4, 88, 0, 24, 0);

            //LipidMassLibraryGenerator.Run(path, LbmClass.CoQ, AdductIonParcer.GetAdductIonBean("[M+H]+"), 1, 13, 0, 0, 0);
            ////LipidMassLibraryGenerator.Run(path, LbmClass.Vitamin, AdductIonParcer.GetAdductIonBean("[M+H]+"), 2, 22, 0, 6, 0);
            ////LipidMassLibraryGenerator.Run(path, LbmClass.Vitamin, AdductIonParcer.GetAdductIonBean("[M-H]-"), 2, 22, 0, 6, 0);
            ////LipidMassLibraryGenerator.Run(path, LbmClass.Vitamin, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 2, 22, 0, 6, 0);
            ////LipidMassLibraryGenerator.Run(path, LbmClass.Vitamin, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 2, 22, 0, 6, 0);

            //LipidMassLibraryGenerator.Run(path, LbmClass.VAE, AdductIonParcer.GetAdductIonBean("[M+H]+"), 2, 22, 0, 6, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.VAE, AdductIonParcer.GetAdductIonBean("[M+Na]+"), 2, 22, 0, 6, 0);

            //LipidMassLibraryGenerator.Run(path, LbmClass.DCAE, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.GDCAE, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.GLCAE, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.TDCAE, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.TLCAE, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 2, 44, 0, 12, 0);

            //LipidMassLibraryGenerator.Run(path, LbmClass.AHexBRS, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.AHexCAS, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.AHexCS, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.AHexSIS, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.AHexSTS, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.AHexBRS, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.AHexCAS, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.AHexCS, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.AHexSIS, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.AHexSTS, AdductIonParcer.GetAdductIonBean("[M+HCOO]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.AHexBRS, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.AHexCAS, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.AHexCS, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.AHexSIS, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.AHexSTS, AdductIonParcer.GetAdductIonBean("[M+CH3COO]-"), 2, 44, 0, 12, 0);

            //LipidMassLibraryGenerator.Run(path, LbmClass.BRSE, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.CASE, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.SISE, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.STSE, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 2, 44, 0, 12, 0);

            //LipidMassLibraryGenerator.Run(path, LbmClass.CSLPHex, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.CSLPHex, AdductIonParcer.GetAdductIonBean("[M-H]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.BRSLPHex, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.BRSLPHex, AdductIonParcer.GetAdductIonBean("[M-H]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.CASLPHex, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.CASLPHex, AdductIonParcer.GetAdductIonBean("[M-H]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.SISLPHex, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.SISLPHex, AdductIonParcer.GetAdductIonBean("[M-H]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.STSLPHex, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.STSLPHex, AdductIonParcer.GetAdductIonBean("[M-H]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.CSPHex, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.CSPHex, AdductIonParcer.GetAdductIonBean("[M-H]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.BRSPHex, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.BRSPHex, AdductIonParcer.GetAdductIonBean("[M-H]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.CASPHex, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.CASPHex, AdductIonParcer.GetAdductIonBean("[M-H]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.SISPHex, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.SISPHex, AdductIonParcer.GetAdductIonBean("[M-H]-"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.STSPHex, AdductIonParcer.GetAdductIonBean("[M+NH4]+"), 2, 44, 0, 12, 0);
            //LipidMassLibraryGenerator.Run(path, LbmClass.STSPHex, AdductIonParcer.GetAdductIonBean("[M-H]-"), 2, 44, 0, 12, 0);


            var output = path + "\\Library.txt";
            if (System.IO.File.Exists(output)) System.IO.File.Delete(output);
            LipidMassLibraryGenerator.Integrate(path, path + "\\Library.txt");
            Console.WriteLine("Done");

            Console.ReadLine();
        }
    }
}
