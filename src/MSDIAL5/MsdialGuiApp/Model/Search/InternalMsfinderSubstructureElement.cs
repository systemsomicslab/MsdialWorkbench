using System;
using System.Collections.Generic;
using CompMs.App.Msdial.Model.Information;
using CompMs.App.Msdial.ViewModel.Information;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Ion;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.CommonMVVM;
using Reactive.Bindings.Extensions;

namespace CompMs.App.Msdial.Model.Search {
    internal class InternalMsfinderSubstructureElement : DisposableModelBase {
        private readonly string id;

        private readonly string mass;
        private readonly string formula;
        private readonly string assignedType; // product ion or neutral loss

        private string? comment;
        private string? inchikey;
        private string? smiles;

        public InternalMsfinderSubstructureElement(int id, ProductIon productIon, int candidateNumber, List<FragmentOntology> uniqueFragmentDB) {
            this.id = id.ToString();
            this.mass = Math.Round(productIon.Mass, 5).ToString();
            this.formula = productIon.Formula.FormulaString + "\r\n" + "(" + Math.Round(productIon.Formula.Mass, 5) + ")";
            this.assignedType = "Product ion";
            MoleculeStructureModel = new MoleculeStructureModel().AddTo(Disposables);
            MoleculeStructureViewModel = new MoleculeStructureViewModel(MoleculeStructureModel).AddTo(Disposables);

            if (candidateNumber < 0)
                SetDefaultInformation();
            else {
                var candidateInChIKey = productIon.CandidateInChIKeys[candidateNumber];
                SetSubstructureInformation(candidateNumber, candidateInChIKey, uniqueFragmentDB);
            }
        }
        private void SetSubstructureInformation(int candidateNumber, string candidateInChIKey, List<FragmentOntology> uniqueFragmentDB) {
            var fragmentInfo = GetMatchedUniqueFragment(candidateInChIKey, uniqueFragmentDB);
            if (fragmentInfo == null)
                SetDefaultInformation();
            else {
                this.comment = "Candidate " + (candidateNumber + 1).ToString() + "\r\n" + fragmentInfo.Comment;
                this.smiles = fragmentInfo.Smiles;
                this.inchikey = fragmentInfo.ShortInChIKey;
                var molecule = new MoleculeProperty {
                    SMILES = fragmentInfo.Smiles
                };
                MoleculeStructureModel.UpdateMolecule(molecule);
            }
        }

        private FragmentOntology? GetMatchedUniqueFragment(string shortInChIKey, List<FragmentOntology> fragmentDB) {
            foreach (var frag in fragmentDB) {
                if (shortInChIKey == frag.ShortInChIKey)
                    return frag;
            }
            return null;
        }

        public InternalMsfinderSubstructureElement(int id, NeutralLoss neutralLoss, int candidateNumber, List<FragmentOntology> uniqueFragmentDB) {
            this.id = id.ToString();
            this.mass = Math.Round(neutralLoss.MassLoss, 5).ToString() + "\r\n" +
                "Precursor m/z: " + Math.Round(neutralLoss.PrecursorMz, 5).ToString() + "\r\n" +
                "Product ion m/z: " + Math.Round(neutralLoss.ProductMz, 5).ToString();

            this.formula = neutralLoss.Formula.FormulaString + "\r\n" + "(" + Math.Round(neutralLoss.Formula.Mass, 5) + ")";
            this.assignedType = "Neutral loss";
            MoleculeStructureModel = new MoleculeStructureModel().AddTo(Disposables);

            if (candidateNumber < 0)
                SetDefaultInformation();
            else {
                var candidateInChIKey = neutralLoss.CandidateInChIKeys[candidateNumber];
                SetSubstructureInformation(candidateNumber, candidateInChIKey, uniqueFragmentDB);
            }
        }

        public MoleculeStructureModel MoleculeStructureModel { get; }
        public MoleculeStructureViewModel? MoleculeStructureViewModel { get; }
        private void SetDefaultInformation() {
            this.comment = "NA";
            this.inchikey = "NA";
            this.smiles = "NA";
        }

        public string Id {
            get { return id; }
        }

        public string Mass {
            get { return mass; }
        }

        public string Formula {
            get { return formula; }
        }

        public string AssignedType {
            get { return assignedType; }
        }

        public string? Comment {
            get { return comment; }
        }

        public string? Inchikey {
            get { return inchikey; }
        }

        public string? Smiles {
            get { return smiles; }
        }
    }
}
