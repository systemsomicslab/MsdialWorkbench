using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv {
    public class FseaResultViewerVM : ViewModelBase {

        private List<FseaResultVM> fseaResultVMs;

        public FseaResultViewerVM(FormulaResult formulaResult, List<ChemicalOntology> chemOntDB, List<FragmentOntology> fragmentOntDB, IonMode ionMode) {
            if (formulaResult.ChemicalOntologyDescriptions == null || formulaResult.ChemicalOntologyDescriptions.Count == 0)
                return;

            var descriptCount = formulaResult.ChemicalOntologyDescriptions.Count;
            var idCount = formulaResult.ChemicalOntologyIDs.Count;
            var repInchiCount = formulaResult.ChemicalOntologyRepresentativeInChIKeys.Count;
            var scoreCount = formulaResult.ChemicalOntologyScores.Count;
            var fragmentOntDictionary = new Dictionary<string, string>();
            foreach (var ont in fragmentOntDB) {
                if (!fragmentOntDictionary.ContainsKey(ont.ChemOntID))
                    fragmentOntDictionary[ont.ChemOntID] = ont.Comment;
            }

            if (descriptCount != idCount || idCount != repInchiCount || repInchiCount != scoreCount || scoreCount != descriptCount)
                return;

            this.fseaResultVMs = new List<FseaResultVM>();
            for(int i = 0; i < descriptCount;i++) {
                var fseaResultVM = new FseaResultVM(formulaResult, i, chemOntDB, fragmentOntDictionary, ionMode);
                if (fseaResultVM.FragmentOntologies == null || fseaResultVM.FragmentOntologies == string.Empty) continue;

                this.fseaResultVMs.Add(fseaResultVM);
            }

            OnPropertyChanged("FseaResultVMs");
        }



        public List<FseaResultVM> FseaResultVMs {
            get {
                return fseaResultVMs;
            }

            set {
                fseaResultVMs = value;
                OnPropertyChanged("FseaResultVMs");
            }
        }
    }
}
