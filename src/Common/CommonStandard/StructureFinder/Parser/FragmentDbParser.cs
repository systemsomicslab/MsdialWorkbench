using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.Parser;
using CompMs.Common.StructureFinder.DataObj;
using CompMs.Common.StructureFinder.Utility;
using CompMs.Common.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.Common.StructureFinder.Parser
{
    public sealed class FragmentDbParser
    {
        private FragmentDbParser() { }

        public static List<FragmentLibrary> ReadEiFragmentDB(string file)
        {
            var fragments = new List<FragmentLibrary>();

            var errorString = string.Empty;
            if (ErrorHandler.IsFileLocked(file, out errorString))
            {
                Console.WriteLine(errorString);
                //MessageBox.Show(errorString, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            using (var sr = new StreamReader(file, Encoding.UTF8))
            {

                var lineCounter = 2;

                sr.ReadLine();
                while (sr.Peek() > -1)
                {
                    var line = sr.ReadLine();
                    if (line == string.Empty) break;

                    var lineArray = line.Split('\t');
                    if (lineArray.Length < 4)
                    {
                        errorString += "Line " + lineCounter + ": the format is not correct. Correct order: Fragment mass, Formula, SMILES, Ion mode\r\n";
                        continue;
                    }

                    float fragMass;
                    if (!float.TryParse(lineArray[0], out fragMass))
                    {
                        errorString += "Line " + lineCounter + ": the exact mass cannot be converted.\r\n";
                        continue;
                    }

                    var formula = FormulaStringParcer.OrganicElementsReader(lineArray[1]);
                    var smiles = lineArray[2];
                    var ionMode = IonMode.Positive; if (lineArray[3].Contains('N')) ionMode = IonMode.Negative;
                    var structure = MoleculeConverter.SmilesToStructure(smiles, out errorString);

                    var fragment = new FragmentLibrary()
                    {
                        FragmentFormula = formula,
                        FragmentMass = fragMass,
                        FragmentSmiles = smiles,
                        IonMode = ionMode,
                        Id = fragments.Count,
                        FragmentStructure = structure
                    };
                    fragments.Add(fragment);
                    lineCounter++;
                }
            }
            return fragments;
        }
    }
}
