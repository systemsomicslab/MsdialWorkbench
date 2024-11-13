using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.ViewModel.Information;
using CompMs.Common.DataObj;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.CommonMVVM;
using Reactive.Bindings.Extensions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace CompMs.App.Msdial.View.Search {
    public partial class SubstructureView : Window {
        public SubstructureView() {
            InitializeComponent();
        }
    }
    internal sealed class InternalMsfinderSubstructure : ViewModelBase {

        private ObservableCollection<InternalMsfinderSubstructureElement> substructureElements;
        public ObservableCollection<InternalMsfinderSubstructureElement> SubstructureElements {
            get { return substructureElements; }
            set { SetProperty(ref substructureElements, value); }
        }
        public InternalMsfinderSubstructure(List<FormulaResult> formulaList, List<FragmentOntology> uniqueFragmentDB) {
            substructureElements ??= [];
            if (formulaList == null) return;
            var counter = 1;
            foreach (var formulaResult in formulaList) {
                var productIons = formulaResult.ProductIonResult;
                var neutralLosses = formulaResult.NeutralLossResult;
                if (productIons == null && neutralLosses == null) return;
                if (productIons?.Count == 0 && neutralLosses.Count == 0) return;

                foreach (var ion in productIons.Where(n => n.CandidateInChIKeys != null && n.CandidateInChIKeys.Count > 0 && n.CandidateInChIKeys[0] != "NA")) {
                    for (int i = 0; i < ion.CandidateInChIKeys.Count; i++) {
                        var element = new InternalMsfinderSubstructureElement(counter, ion, i, uniqueFragmentDB);
                        substructureElements.Add(element);
                        MoleculeStructureViewModel = new MoleculeStructureViewModel(element.MoleculeStructureModel).AddTo(Disposables);
                    }
                    counter++;
                }

                foreach (var loss in neutralLosses.Where(n => n.CandidateInChIKeys != null && n.CandidateInChIKeys.Count > 0 && n.CandidateInChIKeys[0] != "NA")) {
                    for (int i = 0; i < loss.CandidateInChIKeys.Count; i++) {
                        var element = new InternalMsfinderSubstructureElement(counter, loss, i, uniqueFragmentDB);
                        substructureElements.Add(element);
                        MoleculeStructureViewModel = new MoleculeStructureViewModel(element.MoleculeStructureModel).AddTo(Disposables);
                    }
                    counter++;
                }

                foreach (var ion in productIons.Where(n => n.CandidateInChIKeys != null && n.CandidateInChIKeys.Count == 1 && n.CandidateInChIKeys[0] == "NA")) {
                    var element = new InternalMsfinderSubstructureElement(counter, ion, -1, uniqueFragmentDB);
                    substructureElements.Add(element);
                    MoleculeStructureViewModel = new MoleculeStructureViewModel(element.MoleculeStructureModel).AddTo(Disposables);
                    counter++;
                }
            }
        }
        public MoleculeStructureViewModel? MoleculeStructureViewModel { get; }
    }
}
