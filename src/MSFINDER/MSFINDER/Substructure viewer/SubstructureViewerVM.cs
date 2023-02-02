using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public class SubstructureViewerVM : ViewModelBase
    {
        private const int panelWidth = 300;
        private const int panelHeight = 300;
        private ObservableCollection<SubstructureElement> substructureElements;

        public ObservableCollection<SubstructureElement> SubstructureElements
        {
            get { return substructureElements; }
            set { if (substructureElements == value) return; substructureElements = value; OnPropertyChanged("SubstructureElements"); }
        }

        public SubstructureViewerVM(Rfx.Riken.OsakaUniv.RawData rawData, FormulaResult formulaResult, List<FragmentOntology> uniqueFragmentDB)
        {
            if (formulaResult == null) return;
            
            var productIons = formulaResult.ProductIonResult;
            var neutralLosses = formulaResult.NeutralLossResult;
            if (productIons == null && neutralLosses == null) return;
            if (productIons.Count == 0 && neutralLosses.Count == 0) return;

            var syncObj = new object();
            var substructureElements = new ObservableCollection<SubstructureElement>();

            var counter = 1;
            foreach (var ion in productIons.Where(n => n.CandidateInChIKeys != null && 
                n.CandidateInChIKeys.Count > 0 && n.CandidateInChIKeys[0] != "NA"))
            {
                for (int i = 0; i < ion.CandidateInChIKeys.Count; i++) {
                    var element = new SubstructureElement(counter, ion, i, uniqueFragmentDB, panelWidth, panelHeight);
                    substructureElements.Add(element);
                }
                counter++;
            }

            foreach (var loss in neutralLosses.Where(n => n.CandidateInChIKeys != null &&
                n.CandidateInChIKeys.Count > 0 && n.CandidateInChIKeys[0] != "NA")) {
                for (int i = 0; i < loss.CandidateInChIKeys.Count; i++) {
                    var element = new SubstructureElement(counter, loss, i, uniqueFragmentDB, panelWidth, panelHeight);
                    substructureElements.Add(element);
                }
                counter++;
            }

            foreach (var ion in productIons.Where(n => n.CandidateInChIKeys != null &&
                n.CandidateInChIKeys.Count == 1 && n.CandidateInChIKeys[0] == "NA")) {
                var element = new SubstructureElement(counter, ion, -1, uniqueFragmentDB, panelWidth, panelHeight);
                substructureElements.Add(element);
                counter++;
            }

            this.SubstructureElements = substructureElements;
        }
    }
}
