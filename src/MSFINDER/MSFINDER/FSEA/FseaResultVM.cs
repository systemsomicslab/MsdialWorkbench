using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv {
    public class FseaResultVM : ViewModelBase {

        private string chemicalOntology;
        private string pvalue;
        private string foundPerSpectrum;
        private string foundPerOntologies;
        private string fragmentOntologies;

        public FseaResultVM(FormulaResult formulaResult, int id, List<ChemicalOntology> chemOntDB, Dictionary<string, string> fragOntDB, IonMode ionMode) {
            var resultChemOntDescript = formulaResult.ChemicalOntologyDescriptions[id];
            var resultChemOntID = formulaResult.ChemicalOntologyIDs[id];
            var resultChemOntScore = formulaResult.ChemicalOntologyScores[id];
            var resultRepInChIKey = formulaResult.ChemicalOntologyRepresentativeInChIKeys[id];
            var chemOnts = chemOntDB.Where(n => n.RepresentativeInChIKey == resultRepInChIKey && n.IonMode == ionMode).ToList();
            if (chemOnts.Count == 0) return;
            var chemOnt = chemOnts[0];

            //this.chemicalOntology = resultChemOntDescript + "\r\n" + resultChemOntID;
            this.chemicalOntology = resultChemOntDescript;
            this.pvalue = resultChemOntScore == 0 ? "less than 1E-11" : resultChemOntScore.ToString();

            var a11Ontologies = new List<string>();
            var a21Ontologies = new List<string>();

            ChemicalOntologyAnnotation.FitSignificantOntologiesToFragmentSet(formulaResult, chemOnt, out a11Ontologies, out a21Ontologies);

            this.foundPerSpectrum = a11Ontologies.Count.ToString() + "/" 
                + (formulaResult.NeutralLossHits + formulaResult.ProductIonHits).ToString();
            this.foundPerOntologies = a11Ontologies.Count.ToString() + "/" +
                chemOnt.FragmentOntologies.Count.ToString();

            this.fragmentOntologies = string.Empty;
            foreach (var ontology in a11Ontologies) {
                var comment = string.Empty;
                if (fragOntDB.ContainsKey(ontology))
                    comment = fragOntDB[ontology];
                if (comment != string.Empty)
                    this.fragmentOntologies += comment + " ";
            }
        }


        #region properties
        public string ChemicalOntology {
            get {
                return chemicalOntology;
            }

            set {
                chemicalOntology = value;
                OnPropertyChanged("ChemicalOntology");
            }
        }

        public string Pvalue {
            get {
                return pvalue;
            }

            set {
                pvalue = value;
                OnPropertyChanged("Pvalue");
            }
        }

        public string FoundPerSpectrum {
            get {
                return foundPerSpectrum;
            }

            set {
                foundPerSpectrum = value;
                OnPropertyChanged("FoundPerSpectrum");
            }
        }

        public string FoundPerOntologies {
            get {
                return foundPerOntologies;
            }

            set {
                foundPerOntologies = value;
                OnPropertyChanged("FoundPerOntologies");
            }
        }

        public string FragmentOntologies {
            get {
                return fragmentOntologies;
            }

            set {
                fragmentOntologies = value;
                OnPropertyChanged("FragmentOntologies");
            }
        }
        #endregion
    }
}
