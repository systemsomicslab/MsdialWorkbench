using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CompMs.Common.Algorithm.IsotopeCalc;
using CompMs.Common.Components;

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

                if (n < 2)
                {
                    messages.Add(string.Format(error_message_templates[0], counter));
                    continue;
                }

                var reference = new MoleculeMsReference() { ScanID = counter - 1 };
                reference.Name = lineArray[0];
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
                    reference.AdductType = AdductIonParser.GetAdductIonBean(lineArray[3]);
                if (n > 4)
                    reference.InChIKey = lineArray[4];
                if (n > 5)
                {
                    reference.Formula = FormulaGenerator.Parser.FormulaStringParcer.OrganicElementsReader(lineArray[5]);
                    reference.Formula.M1IsotopicAbundance = FormulaGenerator.Function.SevenGoldenRulesCheck.GetM1IsotopicAbundance(reference.Formula);
                    reference.Formula.M2IsotopicAbundance = FormulaGenerator.Function.SevenGoldenRulesCheck.GetM2IsotopicAbundance(reference.Formula);
                    reference.IsotopicPeaks = IsotopeCalculator.GetAccurateIsotopeProperty(reference.Formula.FormulaString, 2, iupacDb).IsotopeProfile;
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

        public static List<MoleculeMsReference> TextLibraryReader(string filePath, out string error)
        {
            List<MoleculeMsReference> result = null;
            using (StreamReader sr = new StreamReader(filePath, Encoding.ASCII))
            {
                result = TextLibraryReader(sr, out error);
            }
            return result;
        }

        public static List<MoleculeMsReference> TargetFormulaLibraryReader(string filePath, out string error)
        {
            throw new NotImplementedException();
        }

        public static List<MoleculeMsReference> StandardTextLibraryReader(string filePath, out string error)
        {
            throw new NotImplementedException();
        }

        public static List<MoleculeMsReference> CompoundListInTargetModeReader(string filePath, out string error)
        {
            throw new NotImplementedException();
        }

    }
}
