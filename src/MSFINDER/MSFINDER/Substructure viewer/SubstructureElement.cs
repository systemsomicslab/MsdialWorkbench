using Riken.Metabolomics.StructureFinder.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public class SubstructureElement
    {
        private string id;

        private string mass;
        private string formula;
        private string assignedType; // product ion or neutral loss

        private string comment;
        private string inchikey;
        private string smiles;
        private System.Windows.Media.Imaging.BitmapImage image;

        public SubstructureElement(int id, ProductIon productIon, int candidateNumber,
            List<FragmentOntology> uniqueFragmentDB, int panelWidth, int panelHeight)
        {
            this.id = id.ToString();
            this.mass = Math.Round(productIon.Mass, 5).ToString();
            this.formula = productIon.Formula.FormulaString + "\r\n" + "(" + Math.Round(productIon.Formula.Mass, 5) + ")";
            this.assignedType = "Product ion";

            if (candidateNumber < 0)
                setDefaultInformation();
            else {
                var candidateInChIKey = productIon.CandidateInChIKeys[candidateNumber];
                setSubstructureInformation(candidateNumber, candidateInChIKey, uniqueFragmentDB, panelWidth, panelHeight);
            }
        }

        private void setSubstructureInformation(int candidateNumber, string candidateInChIKey, List<FragmentOntology> uniqueFragmentDB,
            int panelWidth, int panelHeight)
        {
            var fragmentInfo = getMatchedUniqueFragment(candidateInChIKey, uniqueFragmentDB);
            if (fragmentInfo == null)
                setDefaultInformation();
            else {
                this.comment = "Candidate " + (candidateNumber + 1).ToString() + "\r\n" + fragmentInfo.Comment;
                this.smiles = fragmentInfo.Smiles;
                this.inchikey = fragmentInfo.ShortInChIKey;
                this.image = MoleculeImage.SmilesToMediaImageSource(this.smiles, panelWidth, panelHeight);
            }
        }

        private FragmentOntology getMatchedUniqueFragment(string shortInChIKey, List<FragmentOntology> fragmentDB)
        {
            foreach (var frag in fragmentDB) {
                if (shortInChIKey == frag.ShortInChIKey)
                    return frag;
            }
            return null;
        }

        public SubstructureElement(int id, NeutralLoss neutralLoss, int candidateNumber,
            List<FragmentOntology> uniqueFragmentDB, int panelWidth, int panelHeight)
        {
            this.id = id.ToString();
            this.mass = Math.Round(neutralLoss.MassLoss, 5).ToString() + "\r\n" +
                "Precursor m/z: " + Math.Round(neutralLoss.PrecursorMz, 5).ToString() + "\r\n" +
                "Product ion m/z: " + Math.Round(neutralLoss.ProductMz, 5).ToString();

            this.formula = neutralLoss.Formula.FormulaString + "\r\n" + "(" + Math.Round(neutralLoss.Formula.Mass, 5) + ")";
            this.assignedType = "Neutral loss";

            if (candidateNumber < 0)
                setDefaultInformation();
            else {
                var candidateInChIKey = neutralLoss.CandidateInChIKeys[candidateNumber];
                setSubstructureInformation(candidateNumber, candidateInChIKey, uniqueFragmentDB, panelWidth, panelHeight);
            }
        }

        private void setDefaultInformation()
        {
            this.comment = "NA";
            this.inchikey = "NA";
            this.smiles = "NA";

            var fileUri = new Uri("/Resources/NoStructureAssigned.png", UriKind.Relative);
            this.image = new System.Windows.Media.Imaging.BitmapImage(fileUri);
        }

        public string Id
        {
            get { return id; }
        }

        public string Mass
        {
            get { return mass; }
        }

        public string Formula
        {
            get { return formula; }
        }

        public string AssignedType
        {
            get { return assignedType; }
        }

        public string Comment
        {
            get { return comment; }
        }

        public string Inchikey
        {
            get { return inchikey; }
        }

        public string Smiles
        {
            get { return smiles; }
        }

        public System.Windows.Media.Imaging.BitmapImage Image
        {
            get { return image; }
        }
    }
}
