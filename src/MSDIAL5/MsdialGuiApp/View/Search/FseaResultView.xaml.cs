using CompMs.CommonMVVM;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace CompMs.App.Msdial.View.Search {
    public partial class FseaResultView : Window {
        public FseaResultView() {
            InitializeComponent();
        }
    }

    internal sealed class FseaResultViewModel : ViewModelBase {
        
        public FseaResultViewModel(List<FormulaResult> formulaResults, List<ChemicalOntology> chemOntDB, List<FragmentOntology> fragmentOntDB, IonMode ionMode) {
            fseaResultVMs ??= [];
            foreach (var formula in formulaResults) {
                var descriptCount = formula.ChemicalOntologyDescriptions.Count;
                var idCount = formula.ChemicalOntologyIDs.Count;
                var repInchiCount = formula.ChemicalOntologyRepresentativeInChIKeys.Count;
                var scoreCount = formula.ChemicalOntologyScores.Count;
                var fragmentOntDictionary = new Dictionary<string, string>();
                foreach (var ont in fragmentOntDB) {
                    if (!fragmentOntDictionary.ContainsKey(ont.ChemOntID))
                        fragmentOntDictionary[ont.ChemOntID] = ont.Comment;
                }

                if (descriptCount != idCount || idCount != repInchiCount || repInchiCount != scoreCount || scoreCount != descriptCount)
                    return;

                for (int i = 0; i < descriptCount; i++) {
                    var fseaResultVM = new FseaResult(formula, i, chemOntDB, fragmentOntDictionary, ionMode);
                    if (fseaResultVM.FragmentOntologies == null || fseaResultVM.FragmentOntologies == string.Empty) continue;

                    this.fseaResultVMs.Add(fseaResultVM);
                }
                OnPropertyChanged("FseaResultVMs");
            }
        }

        private List<FseaResult> fseaResultVMs;
        public List<FseaResult> FseaResultVMs {
            get { return fseaResultVMs; }
            set { SetProperty(ref fseaResultVMs, value); }
        }
    }
    internal sealed class FseaResult : ViewModelBase {

        private string? chemicalOntology;
        private string? pvalue;
        private string? foundPerSpectrum;
        private string? foundPerOntologies;
        private string? fragmentOntologies;

        public FseaResult(FormulaResult formulaResult, int id, List<ChemicalOntology> chemOntDB, Dictionary<string, string> fragOntDB, IonMode ionMode) {
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

            FitSignificantOntologiesToFragmentSet(formulaResult, chemOnt, out a11Ontologies, out a21Ontologies);

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

        public static void FitSignificantOntologiesToFragmentSet(FormulaResult result, ChemicalOntology chemOntology,
            out List<string> a11Ontologies, out List<string> a21Ontologies) {

            a11Ontologies = [];
            a21Ontologies = [];
            if (result.ProductIonResult != null && result.ProductIonResult.Count > 0) {
                var maxIntensity = result.ProductIonResult.Max(n => n.Intensity);
                foreach (var ion in result.ProductIonResult) {
                    if (ion.Intensity >= maxIntensity * 0.05) {
                        if (ion.CandidateOntologies != null && ion.CandidateOntologies.Count > 0) {
                            var flg = false;
                            foreach (var ontology in ion.CandidateOntologies) {
                                if (chemOntology.FragmentOntologies.Contains(ontology)) {
                                    if (!a11Ontologies.Contains(ontology)) a11Ontologies.Add(ontology);
                                    flg = true;
                                    break;
                                }
                            }
                            if (flg == false) { //if false, we have to register the significant ion which does not be included in the fragment set.
                                var candidateString = string.Empty;
                                var isAleardyDetected = false;
                                foreach (var ontology in ion.CandidateOntologies) {
                                    foreach (var a21Ont in a21Ontologies) {
                                        if (a21Ont.Contains(ontology)) {
                                            isAleardyDetected = true;
                                            break;
                                        }
                                    }
                                    if (isAleardyDetected) break;
                                    candidateString += ontology + ";";
                                }
                                if (isAleardyDetected == false)
                                    a21Ontologies.Add(candidateString);
                            }
                        }
                    }
                }
            }

            if (result.NeutralLossResult != null && result.NeutralLossResult.Count > 0) {
                var maxIntensity = result.NeutralLossResult.Max(n => n.ProductIntensity);
                foreach (var ion in result.NeutralLossResult) {
                    if (ion.ProductIntensity >= maxIntensity * 0.05) {
                        if (ion.CandidateOntologies != null && ion.CandidateOntologies.Count > 0) {
                            var flg = false;
                            foreach (var ontology in ion.CandidateOntologies) {
                                if (chemOntology.FragmentOntologies.Contains(ontology)) {
                                    if (!a11Ontologies.Contains(ontology)) a11Ontologies.Add(ontology);
                                    flg = true;
                                    break;
                                }
                            }
                            if (flg == false) { //if false, we have to register the significant ion which does not be included in the fragment set.
                                var candidateString = string.Empty;
                                var isAleardyDetected = false;
                                foreach (var ontology in ion.CandidateOntologies) {
                                    foreach (var a21Ont in a21Ontologies) {
                                        if (a21Ont.Contains(ontology)) {
                                            isAleardyDetected = true;
                                            break;
                                        }
                                    }
                                    if (isAleardyDetected) break;
                                    candidateString += ontology + ";";
                                }
                                if (isAleardyDetected == false)
                                    a21Ontologies.Add(candidateString);
                            }
                        }
                    }
                }
            }
        }

        #region properties
        public string? ChemicalOntology {
            get { return chemicalOntology; }
            set { SetProperty(ref chemicalOntology, value); }
        }

        public string? Pvalue {
            get { return pvalue; }
            set { SetProperty(ref pvalue, value); }
        }

        public string? FoundPerSpectrum {
            get { return foundPerSpectrum; }
            set { SetProperty(ref foundPerSpectrum, value); }
        }

        public string? FoundPerOntologies {
            get { return foundPerOntologies; }
            set { SetProperty(ref foundPerOntologies, value); }
        }

        public string? FragmentOntologies {
            get { return fragmentOntologies; }
            set { SetProperty(ref fragmentOntologies, value); }
        }
        #endregion
    }
}
