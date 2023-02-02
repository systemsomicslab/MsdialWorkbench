using CompMs.Common.DataObj.Ion;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.Common.FormulaGenerator.Parser {
    public sealed class ChemOntologyDbParser {

        public static List<ChemicalOntology> Read(string input, out string error) {

            error = string.Empty;
            var chemOntologies = new List<ChemicalOntology>();
            using (var sr = new StreamReader(input)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    #region
                    var line = sr.ReadLine();
                    if (line == string.Empty)
                        continue;

                    var lineArray = line.Split('\t');
                    if (lineArray.Length != 7) {
                        error += "The ChemOntology content must be the array of [0] Ontology description [1] Ontology ID " +
                            "[2] Representative InChIKey [3] Representative SMILES [4] Formula [5] Ion mode" + 
                            "[6] Fragment InChIKeys (; for separater)";
                        break;
                    }

                    var chemOnt = new ChemicalOntology() {
                        OntologyDescription = lineArray[0], OntologyID = lineArray[1],
                        RepresentativeInChIKey = lineArray[2], RepresentativeSMILES = lineArray[3],
                        Formula = FormulaStringParcer.OrganicElementsReader(lineArray[4]),
                        IonMode = lineArray[5].Contains("P") ? IonMode.Positive : IonMode.Negative
                    };

                    if (lineArray[6] == string.Empty) {
                        error += "Fragment InChIKeys for a chemical ontology must be needed.";
                        break;
                    }
                    else if (!lineArray[6].Contains(';') && lineArray[6].Trim().Length == 14) {
                        chemOnt.FragmentInChIKeys.Add(lineArray[6]);
                    }
                    else if (lineArray[6].Contains(';')) {
                        var fragInChIKeys = lineArray[6].Split(';');
                        foreach (var key in fragInChIKeys) {
                            chemOnt.FragmentInChIKeys.Add(key);
                        }
                    }
                    #endregion
                    chemOntologies.Add(chemOnt);
                }
            }

            if (error != string.Empty) {
                //MessageBox.Show(error, "Error in Chemical Ontology DB", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            return chemOntologies;
        }

        public static void ConvertInChIKeyToChemicalOntology(List<ChemicalOntology> chemicalOntologies, List<FragmentOntology> fragmentOntologies) {
            foreach (var chemOnt in chemicalOntologies) {
                var inchikeys = chemOnt.FragmentInChIKeys;
                foreach (var key in inchikeys) {

                    foreach (var fragOnt in fragmentOntologies) {
                        if (key == fragOnt.ShortInChIKey) {
                            if (!chemOnt.FragmentOntologies.Contains(fragOnt.ChemOntID))
                                chemOnt.FragmentOntologies.Add(fragOnt.ChemOntID);
                            break;
                        }
                    }

                }
            }
        }

        public static void ConvertInChIKeyToChemicalOntology(List<ProductIon> ions, List<FragmentOntology> fragmentOntologies) {
            foreach (var ion in ions) {
                var inchikeys = ion.CandidateInChIKeys;
                foreach (var key in inchikeys) {

                    foreach (var fragOnt in fragmentOntologies) {
                        if (key == fragOnt.ShortInChIKey) {
                            if (!ion.CandidateOntologies.Contains(fragOnt.ChemOntID))
                                ion.CandidateOntologies.Add(fragOnt.ChemOntID);
                            break;
                        }
                    }

                }
            }
        }

        public static void ConvertInChIKeyToChemicalOntology(List<NeutralLoss> losses, List<FragmentOntology> fragmentOntologies) {
            foreach (var loss in losses) {
                var inchikeys = loss.CandidateInChIKeys;
                foreach (var key in inchikeys) {

                    foreach (var fragOnt in fragmentOntologies) {
                        if (key == fragOnt.ShortInChIKey) {
                            if (!loss.CandidateOntologies.Contains(fragOnt.ChemOntID))
                                loss.CandidateOntologies.Add(fragOnt.ChemOntID);
                            break;
                        }
                    }
                }
            }
        }
    }
}
