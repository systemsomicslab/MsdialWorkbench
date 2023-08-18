using CompMs.App.MsdialConsole.Export;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.FormulaGenerator.Parser;
using CompMs.Common.Parser;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MsdialPrivateConsoleApp {

    public class Compound {
        public string ProductName { get; set; }
        public string POS { get; set; }
        public string NEG { get; set; }

        public Formula Formula { get; set; }
        public string SMILES { get; set; }  
        public string InChIKey { get; set; }
        public AdductIon AdductPos { get; set; }
        public AdductIon AdductNeg { get; set; }    
        public string Superclass { get; set; }
        public string CClass { get; set; }
        public string Subclass { get; set; }
        public string ParentLevel1 { get; set; }
    }
    public sealed class CreateMspFileFromEadProject {
        private CreateMspFileFromEadProject() { }

        public static void Run(string mdprojectfile, string referencefile, string outputDir) {

            var compounds = getReferenceCompounds(referencefile);
            var exporter = new ExporterTest();
            var storage = exporter.LoadProjectFromPathAsync(mdprojectfile).Result;
            var param = storage.Parameter;
            var polarity = param.IonMode;

            var logs = new List<string>();
            var expConditions = new Dictionary<int, string>() {
                { 1, "CID 10V" },
                { 2, "CID 20V" },
                { 3, "CID 40V" },
                { 4, "CID 10V CES 15V" },
                { 5, "CID 20V CES 15V" },
                { 6, "CID 40V CES 15V" },
                { 7, "EAD 10eV CID 10V" },
                { 8, "EAD 15eV CID 10V" },
                { 9, "EAD 20eV CID 10V" },
            };

            var mainSubFolder = outputDir + "\\" + "Acc";
            var subtractFolder = outputDir + "\\" + "Subtract";
            var curatedFolder = outputDir + "\\" + "Curated";

            if (!Directory.Exists(mainSubFolder)) {
                Directory.CreateDirectory(mainSubFolder);
            }

            if (!Directory.Exists(subtractFolder)) {
                Directory.CreateDirectory(subtractFolder);
            }

            if (!Directory.Exists(curatedFolder)) {
                Directory.CreateDirectory(curatedFolder);
            }

            foreach (var file in storage.AnalysisFiles) {

                Console.WriteLine("Start {0}", file.AnalysisFileName);
                var peaks = MsdialPeakSerializer.LoadChromatogramPeakFeatures(file.PeakAreaBeanInformationFilePath);
                var decResults = MsdecResultsReader.ReadMSDecResults(file.DeconvolutionFilePath, out _, out _);

                var filename = file.AnalysisFileName;
                Compound compound = null;
                if (param.IonMode == CompMs.Common.Enum.IonMode.Positive) {
                    compound = compounds.Where(n => n.POS == filename).FirstOrDefault();
                }
                else {
                    compound = compounds.Where(n => n.NEG == filename).FirstOrDefault();
                }
                var adduct = polarity == CompMs.Common.Enum.IonMode.Positive ? compound.AdductPos : compound.AdductNeg;
                var exactmass = compound.Formula.Mass;
                var theoreticalMz = MolecularFormulaUtility.ConvertExactMassToPrecursorMz(adduct, exactmass);

                var bestID = -1;
                var maxIntensity = double.MinValue;
                for (int i = 0; i < peaks.Count; i++) {
                    var peak = peaks[i];
                    if (Math.Abs(peak.Mass - theoreticalMz) < 0.01) {
                        if (maxIntensity < peak.PeakHeightTop) {
                            maxIntensity = peak.PeakHeightTop;
                            bestID = i;
                        }
                    }
                }

                if (bestID == -1) {
                    var error = "peak is not found for " + file.AnalysisFileName;
                    Console.WriteLine(error);
                    logs.Add(error);
                    continue;
                }

                var bestPeak = peaks[bestID];
                var rt = bestPeak.ChromXsTop.RT.Value;
                if (Math.Abs(rt - 0.5) > 0.5) {
                    var error = "retention time of peak is strange for " + file.AnalysisFileName;
                    Console.WriteLine(error);
                    logs.Add(error);
                    continue;
                }

                //if (bestPeak.MasterPeakID == -1) {
                //    var error = "ms2 is not observed for " + file.AnalysisFileName;
                //    Console.WriteLine(error);
                //    logs.Add(error);
                //    continue;
                //}
                
                var ms2Spec = decResults[bestPeak.MasterPeakID];
                if (ms2Spec.Spectrum.IsEmptyOrNull()) {
                    var error = "ms2 is not observed for " + file.AnalysisFileName;
                    Console.WriteLine(error);
                    logs.Add(error);
                    continue;
                }

                var beginRt = rt - 0.1;
                var endRt = rt + 0.1;

                var beginRtForSub = 2.0 - 0.1;
                var endRtForSub = 2.0 + 0.1;

                var provider = new StandardDataProvider(file, false, false, 5);

                var spectrum = provider.LoadMsNSpectrums(level: 2);
                var experiments = spectrum.Select(spec => spec.ExperimentID).Distinct().OrderBy(v => v).ToArray();

                var accFile = Path.Combine(mainSubFolder, file.AnalysisFileName + "_Acc.msp");
                var subFile = Path.Combine(subtractFolder, file.AnalysisFileName + "_Sub.msp");
                var curateFile = Path.Combine(curatedFolder, file.AnalysisFileName + "_Curated.msp");

                var swAcc = new StreamWriter(accFile, false, Encoding.ASCII);
                var swSub = new StreamWriter(subFile, false, Encoding.ASCII);
                var swCurate = new StreamWriter(curateFile, false, Encoding.ASCII);

                foreach (var exp in experiments) {

                    var mainAveSpec = DataAccess.GetAverageSpectrum(spectrum, beginRt, endRt, 0.05, exp);
                    var subtractAveSpec = DataAccess.GetAverageSpectrum(spectrum, beginRtForSub, endRtForSub, 0.05, exp);
                    var subtractSpec = DataAccess.GetSubtractSpectrum(mainAveSpec, subtractAveSpec, 0.05);

                    writeMspFormat(swAcc, mainAveSpec, compound, rt, theoreticalMz, adduct, polarity, expConditions[exp]);
                    writeMspFormat(swSub, subtractAveSpec, compound, rt, theoreticalMz, adduct, polarity, expConditions[exp]);
                    writeMspFormat(swCurate, subtractSpec, compound, rt, theoreticalMz, adduct, polarity, expConditions[exp]);
                }

                swAcc.Close();
                swSub.Close();
                swCurate.Close();
            }
            var errorfile = Path.Combine(outputDir, "log.txt");
            using (var sw = new StreamWriter(errorfile, false, Encoding.ASCII)) {
                foreach (var messeage in logs) {
                    sw.WriteLine(messeage);
                }
            }
        }

        private static void writeMspFormat(StreamWriter sw, List<SpectrumPeak> peaks, Compound compound, 
            double rt, double theoreticalMz, AdductIon adduct, CompMs.Common.Enum.IonMode polarity, string ce) {
            sw.WriteLine("NAME: {0}", compound.ProductName);
            sw.WriteLine("PRECURSORMZ: {0}", theoreticalMz);
            sw.WriteLine("PRECURSORTYPE: {0}", adduct.AdductIonName);
            sw.WriteLine("IONMODE: {0}", polarity.ToString());
            sw.WriteLine("FORMULA: {0}", compound.Formula.FormulaString);
            sw.WriteLine("Superclass: {0}", compound.Superclass);
            sw.WriteLine("Class: {0}", compound.CClass);
            sw.WriteLine("Subclass: {0}", compound.Subclass);
            sw.WriteLine("ParentLevel1: {0}", compound.ParentLevel1);
            sw.WriteLine("SMILES: {0}", compound.SMILES);
            sw.WriteLine("INCHIKEY: {0}", compound.InChIKey);
            sw.WriteLine("INSTRUMENTTYPE: LC-ESI-QTOF");
            sw.WriteLine("COLLISIONENERGY: {0}", ce);
            sw.WriteLine("RETENTIONTIME: {0}", rt);
            sw.WriteLine("COMMENT: {0}", string.Empty);
            sw.WriteLine("Num Peaks: {0}", peaks.Count);
            foreach (var peak in peaks.OrderBy(n => n.Mass)) sw.WriteLine(peak.Mass + "\t" + peak.Intensity);
            sw.WriteLine();
        }

        private static List<Compound> getReferenceCompounds(string referencefile) {
            var compounds = new List<Compound>();

            using (var sr = new StreamReader(referencefile)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    var lineArray = line.Split('\t');
                    var compound = new Compound() {
                        ProductName = lineArray[0],
                        POS = lineArray[1],
                        NEG = lineArray[2],
                        Formula = FormulaStringParcer.Convert2FormulaObjV2(lineArray[3]),
                        SMILES = lineArray[4],
                        InChIKey = lineArray[5],
                        AdductPos = AdductIon.GetAdductIon(lineArray[6]),
                        AdductNeg = AdductIon.GetAdductIon(lineArray[7]),
                        Superclass = lineArray.Length > 9 ? lineArray[9] : string.Empty,
                        CClass = lineArray.Length > 10 ? lineArray[10] : string.Empty,
                        Subclass = lineArray.Length > 11 ? lineArray[11] : string.Empty,
                        ParentLevel1 = lineArray.Length > 12 ? lineArray[12] : string.Empty,
                    };
                    compounds.Add(compound);
                }
            }

            return compounds;
        }
    }
}
