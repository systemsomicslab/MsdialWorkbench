using CompMs.Common.DataObj.Ion;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.Common.FormulaGenerator.Parser {
    public sealed class FragmentDbParser
    {
        private FragmentDbParser() { }

        public static List<ProductIon> GetProductIonDB(string dbFilePath, out string errorString)
        {
            var productIonDB = new List<ProductIon>();
            double mass;
            errorString = string.Empty;

            if (ErrorHandler.IsFileLocked(dbFilePath, out errorString)) {
                //MessageBox.Show(errorString, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            using (StreamReader sr = new StreamReader(dbFilePath, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) break;
                    var lineArray = line.Split('\t');

                    if (lineArray.Length != 5) {
                        errorString = "There is some empty values or redundant cells in your product ion DB. Please check your sheet.";
                        //MessageBox.Show("There is some empty values or redundant cells in your product ion DB. Please check your sheet.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return null;
                    }

                    if (double.TryParse(lineArray[0], out mass)) {
                        var formulaString = lineArray[1];
                        var formula = FormulaStringParcer.OrganicElementsReader(formulaString);
                        var ionMode = lineArray[2].Contains("P") == true ? IonMode.Positive : IonMode.Negative;
                        var inchikeys = lineArray[4].Split(';').ToList();
                        var frequency = 0.0;
                        double.TryParse(lineArray[3], out frequency);

                        productIonDB.Add(new ProductIon() {
                            Mass = double.Parse(lineArray[0]),
                            IonMode = ionMode,
                            Formula = formula,
                            CandidateInChIKeys = inchikeys,
                            Frequency = frequency
                        });
                    }
                    else {
                        errorString = "There are non-figure values in the Accurata mass or elemental composition cells in your product ion DB. Please check your sheet and reboot this application.";
                        //MessageBox.Show("There are non-figure values in the Accurata mass or elemental composition cells in your product ion DB. Please check your sheet and reboot this application.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return null;
                    }
                }
            }
            return productIonDB;
        }

		public static List<NeutralLoss> GetNeutralLossDB(string dbFilePath, out string errorString)
		{
            var neutralLossDB = new List<NeutralLoss>();
            double mass;
            errorString = string.Empty;

            if (ErrorHandler.IsFileLocked(dbFilePath, out errorString)) {
                //MessageBox.Show(errorString, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            using (StreamReader sr = new StreamReader(dbFilePath, Encoding.ASCII))
            {
                sr.ReadLine();
                while (sr.Peek() > -1)
                {
                    var line = sr.ReadLine();
                    if (line == string.Empty) break;
                    var lineArray = line.Split('\t');

                    if (lineArray.Length != 5) {
                        errorString = "There is some empty values or redundant cells in your neutral loss DB. Please check your sheet.";
                        //MessageBox.Show("There is some empty values or redundant cells in your neutral loss DB. Please check your sheet.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); 
                        return null; 
                    }
                    
                    if (double.TryParse(lineArray[0], out mass))
                    {
                        var formulaString = lineArray[1];
                        var formula = FormulaStringParcer.OrganicElementsReader(formulaString);
                        var ionMode = lineArray[2].Contains("P") == true ? IonMode.Positive : IonMode.Negative;
                        var inchikeys = lineArray[4].Split(';').ToList();
                        var frequency = 0.0;
                        double.TryParse(lineArray[3], out frequency);

                        neutralLossDB.Add(new NeutralLoss() {
                            MassLoss = double.Parse(lineArray[0]),
                            Formula = formula,
                            CandidateInChIKeys = inchikeys,
                            Iontype = ionMode,
                            Frequency = frequency
                        });
                    }
                    else
                    {
                        errorString = "There are non-figure values in the Accurata mass or elemental composition cells in your neutral loss DB. Please check your sheet and reboot this application.";
                        //MessageBox.Show("There are non-figure values in the Accurata mass or elemental composition cells in your neutral loss DB. Please check your sheet and reboot this application.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); 
                        return null;
                    }
                }
            }
            return neutralLossDB;
        }

        public static List<FragmentOntology> GetFragmentOntologyDB(string dbFilePath, out string errorString)
        {
            var uniqueFragmentDB = new List<FragmentOntology>();
            errorString = string.Empty;

            if (ErrorHandler.IsFileLocked(dbFilePath, out errorString)) {
                //MessageBox.Show(errorString, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            using (StreamReader sr = new StreamReader(dbFilePath, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) break;
                    var lineArray = line.Split('\t');

                    if (lineArray.Length != 6) {
                        errorString = "There is some empty values or redundant cells in your fragment ontology DB. Please check your sheet.";
                        //MessageBox.Show("There is some empty values or redundant cells in your fragment ontology DB. Please check your sheet.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return null;
                    }

                    var freq = 0.0;
                    double.TryParse(lineArray[3], out freq);

                    var uniqueFrag = new FragmentOntology() {
                        ShortInChIKey = lineArray[0], Smiles = lineArray[1],
                        Formula = lineArray[2], Frequency = freq,
                        Comment = lineArray[4], ChemOntID = lineArray[5]
                    };
                    uniqueFragmentDB.Add(uniqueFrag);
                }
            }
            return uniqueFragmentDB;
        }
    }
}
