using CompMs.Common.Algorithm.IsotopeCalc;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.Common.Parser
{
    public sealed class TextLibraryParser
    {
        private TextLibraryParser() { }

        private static DataObj.Database.IupacDatabase iupacDb = IupacResourceParser.GetIUPACDatabase();

        private static readonly string[] error_message_templates = new string[]
        {
            "Error type 1: line {0} is not suitable.",
            "Error type 2: line {0} includes non-numerical value for {1} information.",
            "Error type 3: line {0} includes negative value for {1} information.",
            "Error type 4: This library doesn't include suitable information.",
        };

        private static readonly string help_message = string.Join("\r\n", new string[]
        {
                "",
                "You should write the following information as the reference library for post identification method.",
                "First- and second columns are required, and the others are option.",
                "[0]Compound Name\t[1]m/z\t[2]Retention time[min]\t[3]adduct\t[4]inchikey\t[5]formula\t[6]smiles\t[7]ontology\t[8]CCS",
                "Metabolite A\t100.000\t5.0\t[M+H]+\tAAAAAAAAAAAAA-BBBBBBBB\tC6H12O6\tCOCOCOCOC\tSugar\t210.2",
                "Metabolite B\t200.000\t6.0\t[M+H]+\tAAAAAAAAAAAAA-BBBBBBBB\tC6H12O6\tCOCOCOCOC\tSugar\t210.2",
                "Metabolite C\t300.000\t7.0\t[M+H]+\tAAAAAAAAAAAAA-BBBBBBBB\tC6H12O6\tCOCOCOCOC\tSugar\t210.2",
                "Metabolite D\t400.000\t8.0\t[M+H]+\tAAAAAAAAAAAAA-BBBBBBBB\tC6H12O6\tCOCOCOCOC\tSugar\t210.2",
                "Metabolite E\t500.000\t9.0\t[M+H]+\tAAAAAAAAAAAAA-BBBBBBBB\tC6H12O6\tCOCOCOCOC\tSugar\t210.2",
                "",
        });

        private static readonly string help_message_targetmodelib = string.Join("\r\n", new string[]
        {
                "",
                "You should write the following information as the reference library for targeted peak detection.",
                "First- and second columns are required, and the others are option.",
                "[0]Name\t[1]RetentionTime\t[2]RT tolerance\t[3]m/z\t[4]m/z tolerance\t[5]Min Height\t[6]Include",
                "Name A\t2.0\t0.1\t100.000\t0.01\t1000\ttrue",
                "Name B\t4.0\t0.1\t200.000\t0.01\t1000\ttrue",
                "Name C\t6.0\t0.1\t300.000\t0.01\t1000\ttrue",
                "Name D\t7.0\t0.1\t400.000\t0.01\t1000\ttrue",
                "Name E\t10.0\t0.1\t500.000\t0.01\t1000\ttrue",
                "",
        });


        public static List<MoleculeMsReference> TextLibraryReader(TextReader reader, out string error)
        {
            var results = new List<MoleculeMsReference>();

            string line;
            string[] lineArray;
            int counter = 0;
            var messages = new List<string>();

            reader.ReadLine(); // skip header

            while (reader.Peek() > -1)
            {
                line = reader.ReadLine();
                ++counter;

                lineArray = line.Split('\t');
                var n = lineArray.Length;

                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                if (n < 2)
                {
                    messages.Add(string.Format(error_message_templates[0], counter));
                    continue;
                }

                var reference = new MoleculeMsReference() { ScanID = counter - 1 };
                reference.Name = lineArray[0].Trim('"');
                if (double.TryParse(lineArray[1], out double mz))
                {
                    if (mz < 0)
                    {
                        messages.Add(string.Format(error_message_templates[2], counter, "accurate mass"));
                        continue;
                    }
                    reference.PrecursorMz = mz;
                }
                else
                {
                    messages.Add(string.Format(error_message_templates[1], counter, "accurate mass"));
                    continue;
                }

                if (n > 2)
                    if (double.TryParse(lineArray[2], out double rt))
                    {
                        reference.ChromXs.RT = new RetentionTime(rt);
                    }
                    else
                    {
                        messages.Add(string.Format(error_message_templates[1], counter, "retention time"));
                        continue;
                    }
                if (n > 3)
                    reference.AdductType = AdductIon.GetAdductIon(lineArray[3]);
                if (n > 4)
                    reference.InChIKey = lineArray[4];
                if (n > 5)
                {
                    reference.Formula = FormulaGenerator.Parser.FormulaStringParcer.OrganicElementsReader(lineArray[5]);
                    if (reference.Formula is { } formula && formula.IsCorrectlyImported) {
                        reference.Formula.M1IsotopicAbundance = FormulaGenerator.Function.SevenGoldenRulesCheck.GetM1IsotopicAbundance(formula);
                        reference.Formula.M2IsotopicAbundance = FormulaGenerator.Function.SevenGoldenRulesCheck.GetM2IsotopicAbundance(formula);
                        reference.IsotopicPeaks = IsotopeCalculator.GetAccurateIsotopeProperty(formula.FormulaString, 2, iupacDb)?.IsotopeProfile;
                    }
                }
                if (n > 6)
                    reference.SMILES = lineArray[6];
                if (n > 7)
                    reference.Ontology = lineArray[7];
                if (n > 8)
                    if (double.TryParse(lineArray[8], out double ccs))
                    {
                        reference.CollisionCrossSection = ccs;
                    }
                    else
                    {
                        messages.Add(string.Format(error_message_templates[1], counter, "CCS"));
                        continue;
                    }

                results.Add(reference);
            }

            if (results.Count == 0)
            {
                messages.Add(error_message_templates[3]);
            }

            if (messages.Count > 0)
            {
                messages.Add(help_message);
                results = null;
            }

            error = string.Join("\r\n", messages);
            return results;
        }

        public static List<SpectrumPeak> TextToSpectrumList(string specText, char mzIntSep, char peakSep, double threshold = 0.0) {
            var peaks = new List<SpectrumPeak>();

            if (specText == null || specText == string.Empty) return peaks;
            var specArray = specText.Split(peakSep).ToList();
            if (specArray.Count == 0) return peaks;

            foreach (var spec in specArray) {
                var mzInt = spec.Split(mzIntSep).ToArray();
                if (mzInt.Length >= 2) {
                    var mz = mzInt[0];
                    var intensity = mzInt[1];

                    var peak = new SpectrumPeak() { Mass = double.Parse(mz), Intensity = double.Parse(intensity) };
                    if (peak.Intensity > threshold) {
                        peaks.Add(peak);
                    }
                }
            }

            return peaks;
        }

        public static string PeaksToText(IReadOnlyList<ISpectrumPeak> peaks, char mzIntSep, char peakSep) {
            if (peaks.IsEmptyOrNull()) return string.Empty;
            var peakstrings = new List<string>();
            foreach (var peak in peaks) {
                peakstrings.Add(peak.Mass.ToString() + mzIntSep.ToString() + peak.Intensity.ToString());
            }
            return String.Join(peakSep.ToString(), peakstrings);
        }

        public static List<MoleculeMsReference> TextLibraryReader(string filePath, out string error)
        {
            List<MoleculeMsReference> result = null;
            using (StreamReader sr = new StreamReader(File.Open(filePath, FileMode.Open, FileAccess.Read), Encoding.ASCII))
            {
                result = TextLibraryReader(sr, out error);
            }
            return result;
        }

       
        public static List<MoleculeMsReference> CompoundListInTargetModeReader(string filePath, out string error)
        {
            error = string.Empty;

            var textQueries = new List<MoleculeMsReference>();
            var messages = new List<string>();

            string line, errorMessage = string.Empty, adductString = string.Empty, inchikey = string.Empty;
            string[] lineArray;
            float retentionTime, accurateMass, rtTol, massTol, minInt;
            int counter = 1;

            using (StreamReader sr = new StreamReader(filePath, Encoding.ASCII)) {
                sr.ReadLine();

                while (sr.Peek() > -1) {
                    #region
                    line = sr.ReadLine();
                    counter++;

                    if (line == string.Empty) break;

                    lineArray = line.Split('\t');

                    if (lineArray.Length < 2) { messages.Add(string.Format(error_message_templates[0], counter)); continue; }

                    retentionTime = -1; accurateMass = -1; rtTol = -1; massTol = -1;

                    var textQuery = new MoleculeMsReference();
                    textQuery.Name = lineArray[0];
                    textQuery.ScanID = textQueries.Count;

                    if (lineArray.Length >= 2) {
                        if (float.TryParse(lineArray[1], out accurateMass)) {
                            if (accurateMass < 0) {
                                messages.Add(string.Format(error_message_templates[2], counter, "accurate mass"));
                                continue;
                            }
                            else {
                                textQuery.PrecursorMz = accurateMass;
                            }
                        }
                        else {
                            messages.Add(string.Format(error_message_templates[1], counter, "accurate mass"));
                            continue;
                        }
                    }

                    if (lineArray.Length >= 3) {
                        if (float.TryParse(lineArray[2], out massTol)) {
                            textQuery.MassTolerance = massTol;
                        }
                        else {
                            messages.Add(string.Format(error_message_templates[1], counter, "accurate mass tolerance"));
                            continue;
                        }
                    }


                    if (lineArray.Length >= 4) {
                        if (float.TryParse(lineArray[3], out retentionTime)) {
                            if (retentionTime < 0) {
                                messages.Add(string.Format(error_message_templates[2], counter, "retention time"));
                                continue;
                            }
                            else {
                                textQuery.ChromXs = new ChromXs(retentionTime);
                            }
                        }
                        else {
                            messages.Add(string.Format(error_message_templates[1], counter, "retention time"));
                            continue;
                        }
                    }

                    
                    if (lineArray.Length >= 5) {
                        if (float.TryParse(lineArray[4], out rtTol)) {
                            textQuery.RetentionTimeTolerance = rtTol;
                        }
                        else {
                            messages.Add(string.Format(error_message_templates[1], counter, "retention time tolerance"));
                            continue;
                        }
                    }

                    if (lineArray.Length >= 6) {
                        if (float.TryParse(lineArray[5], out minInt)) {
                            textQuery.MinimumPeakHeight = minInt;
                        }
                        else {
                            messages.Add(string.Format(error_message_templates[1], counter, "minimum peak height"));
                            continue;
                        }
                    }

                    if (lineArray.Length >= 7) {
                        if (bool.TryParse(lineArray[6], out var res)) {
                            textQuery.IsTargetMolecule = res;
                        }
                    }

                    #endregion

                    textQueries.Add(textQuery);
                }

                if (textQueries.Count == 0) {
                    messages.Add(error_message_templates[3]);
                }
            }
            if (messages.Count > 0) {
                messages.Add(help_message);
                textQueries = null;
            }

            error = string.Join("\r\n", messages);
            return textQueries;
        }

        public static List<MoleculeMsReference> StandardTextLibraryReader(string filePath, out string error) {
            error = string.Empty;

            var textQueries = new List<MoleculeMsReference>();
            var textQuery = new MoleculeMsReference();

            string line, errorMessage = string.Empty, adductString = string.Empty, inchikey = string.Empty;
            string[] lineArray;
            float retentionTime, accurateMass, rtTol, massTol, minInt;
            int counter = 1;

            using (StreamReader sr = new StreamReader(filePath, Encoding.ASCII)) {
                sr.ReadLine();

                while (sr.Peek() > -1) {
                    #region
                    line = sr.ReadLine();
                    counter++;

                    if (line == string.Empty) break;

                    lineArray = line.Split('\t');

                    if (lineArray.Length < 2) { errorMessage += "Error type 1: line " + counter + " is not suitable.\r\n"; continue; }

                    retentionTime = -1; accurateMass = -1; rtTol = -1; massTol = -1;

                    textQuery = new MoleculeMsReference();
                    textQuery.Name = lineArray[0];
                    textQuery.ScanID = textQueries.Count;

                    if (lineArray.Length >= 2) {
                        if (float.TryParse(lineArray[1], out retentionTime)) {
                            textQuery.ChromXs.RT = new RetentionTime(retentionTime, textQuery.ChromXs.RT.Unit);
                        }
                        else {
                            errorMessage += "Error type 2: line " + counter + "includes non-numerical value for retention time information.\r\n";
                            continue;
                        }
                    }

                    if (lineArray.Length >= 4) {
                        if (float.TryParse(lineArray[3], out accurateMass)) {
                            if (accurateMass < 0) {
                                errorMessage += "Error type 3: line " + counter + "includes negative value for accurate mass information.\r\n";
                                continue;
                            }
                            else {
                                textQuery.PrecursorMz = accurateMass;
                            }
                        }
                        else {
                            errorMessage += "Error type 2: line " + counter + "includes non-numerical value for accurate mass information.\r\n";
                            continue;
                        }
                    }

                    if (lineArray.Length >= 3) {
                        if (float.TryParse(lineArray[2], out rtTol)) {
                            textQuery.RetentionTimeTolerance = rtTol;
                        }
                        else {
                            errorMessage += "Error type 2: line " + counter + "includes non-numerical value for retention time tolerance.\r\n";
                            continue;
                        }
                    }

                    if (lineArray.Length >= 5) {
                        if (float.TryParse(lineArray[4], out massTol)) {
                            textQuery.MassTolerance = massTol;
                        }
                        else {
                            errorMessage += "Error type 2: line " + counter + "includes non-numerical value for accurate mass tolerance.\r\n";
                            continue;
                        }
                    }

                    if (lineArray.Length >= 6) {
                        if (float.TryParse(lineArray[5], out minInt)) {
                            textQuery.MinimumPeakHeight = minInt;
                        }
                        else {
                            errorMessage += "Error type 2: line " + counter + "includes non-numerical value for minimum peak height.\r\n";
                            continue;
                        }
                    }

                    if (lineArray.Length >= 7) {
                        if (bool.TryParse(lineArray[6], out var res)) {
                            textQuery.IsTargetMolecule = res;
                        }
                    }

                    #endregion

                    textQueries.Add(textQuery);
                }

                if (textQueries.Count == 0) {
                    errorMessage += "Error type 4: This library doesn't include suitable information.\r\n";
                }
            }

            if (errorMessage != string.Empty) {
                errorMessage += "\r\n";
                errorMessage += "You should write the following information as the target standard library.\r\n";

                errorMessage += "[0]Name\t[1]RetentionTime\t[2]RT tolerance\t[3]m/z\t[4]m/z tolerance\t[5]Min Height\t[6]Include\r\n";
                errorMessage += "Name A\t2.0\t0.1\t100.000\t0.01\t1000\ttrue\r\n";
                errorMessage += "Name B\t4.0\t0.1\t200.000\t0.01\t1000\ttrue\r\n";
                errorMessage += "Name C\t6.0\t0.1\t300.000\t0.01\t1000\ttrue\r\n";
                errorMessage += "Name D\t7.0\t0.1\t400.000\t0.01\t1000\ttrue\r\n";
                errorMessage += "Name E\t10.0\t0.1\t500.000\t0.01\t1000\ttrue\r\n";

                error = errorMessage;
                //MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                return null;
            }

            return textQueries;
        }
    }
}
