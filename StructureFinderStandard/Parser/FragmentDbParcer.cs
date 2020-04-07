using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.StructureFinder.Property;
using Riken.Metabolomics.StructureFinder.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Riken.Metabolomics.StructureFinder.Parser
{
    public class FragmentLibrary
    {
        private IonMode ionMode;
        private double fragmentMass;

        private Formula fragmentFormula;
        private string fragmentSmiles;
        private int id;
        private Structure fragmentStructure;

        public FragmentLibrary()
        {
        }

        public IonMode IonMode
        {
            get { return ionMode; }
            set { ionMode = value; }
        }

        public double FragmentMass
        {
            get { return fragmentMass; }
            set { fragmentMass = value; }
        }

        public Formula FragmentFormula
        {
            get { return fragmentFormula; }
            set { fragmentFormula = value; }
        }

        public string FragmentSmiles
        {
            get { return fragmentSmiles; }
            set { fragmentSmiles = value; }
        }

        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        public Structure FragmentStructure
        {
            get {
                return fragmentStructure;
            }

            set {
                fragmentStructure = value;
            }
        }
    }

    public sealed class FragmentDbParcer
    {
        private FragmentDbParcer() { }

        public static List<FragmentLibrary> ReadEiFragmentDB(string file)
        {
            var fragments = new List<FragmentLibrary>();

            var errorString = string.Empty;
            if (ErrorHandler.IsFileLocked(file, out errorString)) {
                Console.WriteLine(errorString);
                //MessageBox.Show(errorString, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            using (var sr = new StreamReader(file, Encoding.UTF8)) {

                var lineCounter = 2;

                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) break;

                    var lineArray = line.Split('\t');
                    if (lineArray.Length < 4) {
                        errorString += "Line " + lineCounter + ": the format is not correct. Correct order: Fragment mass, Formula, SMILES, Ion mode\r\n";
                        continue;
                    }

                    float fragMass;
                    if (!float.TryParse(lineArray[0], out fragMass)) {
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
