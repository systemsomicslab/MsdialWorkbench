using CompMs.App.Msdial.Model.Search;
using CompMs.Common.DataObj;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.CommonMVVM;
using Reactive.Bindings.Notifiers;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.ViewModel.Search {
    internal class InternalMsfinderSubstructureViewModel : ViewModelBase {

        private ObservableCollection<InternalMsfinderSubstructureElement> substructureElements;

        public ObservableCollection<InternalMsfinderSubstructureElement> SubstructureElements {
            get { return substructureElements; }
            set { if (substructureElements == value) return; substructureElements = value; OnPropertyChanged("SubstructureElements"); }
        }

        public InternalMsfinderSubstructureViewModel(RawData rawData, FormulaResult formulaResult, List<FragmentOntology> uniqueFragmentDB) {
            if (formulaResult == null) return;

            var productIons = formulaResult.ProductIonResult;
            var neutralLosses = formulaResult.NeutralLossResult;
            if (productIons == null && neutralLosses == null) return;
            if (productIons.Count == 0 && neutralLosses.Count == 0) return;

            var syncObj = new object();
            var substructureElements = new ObservableCollection<InternalMsfinderSubstructureElement>();

            var counter = 1;
            foreach (var ion in productIons.Where(n => n.CandidateInChIKeys != null && n.CandidateInChIKeys.Count > 0 && n.CandidateInChIKeys[0] != "NA")) {
                for (int i = 0; i < ion.CandidateInChIKeys.Count; i++) {
                    var element = new InternalMsfinderSubstructureElement(counter, ion, i, uniqueFragmentDB);
                    substructureElements.Add(element);
                }
                counter++;
            }

            foreach (var loss in neutralLosses.Where(n => n.CandidateInChIKeys != null && n.CandidateInChIKeys.Count > 0 && n.CandidateInChIKeys[0] != "NA")) {
                for (int i = 0; i < loss.CandidateInChIKeys.Count; i++) {
                    var element = new InternalMsfinderSubstructureElement(counter, loss, i, uniqueFragmentDB);
                    substructureElements.Add(element);
                }
                counter++;
            }

            foreach (var ion in productIons.Where(n => n.CandidateInChIKeys != null && n.CandidateInChIKeys.Count == 1 && n.CandidateInChIKeys[0] == "NA")) {
                var element = new InternalMsfinderSubstructureElement(counter, ion, -1, uniqueFragmentDB);
                substructureElements.Add(element);
                counter++;
            }

            this.substructureElements = substructureElements;
        }
    }
}
