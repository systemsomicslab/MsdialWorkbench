using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv
{
    public class ProductIonVM : ViewModelBase
    {
        private ProductIon productIon;

        public ProductIonVM(ProductIon productIon)
        {
            this.productIon = productIon;
        }

        public ProductIon ProductIon
        {
            get { return productIon; }
            set { productIon = value; }
        }

        public string Formula
        {
            get { return this.productIon.Formula.FormulaString; }
        }

        public double ExactMass
        {
            get { return this.productIon.Formula.Mass; }
        }

        public double AccurateMass
        {
            get { return this.productIon.Mass; }
        }

        public double MassError
        {
            get { return this.productIon.MassDiff; }
        }

        public double Intensity
        {
            get { return this.productIon.Intensity; }
        }
        public List<string> CandidateInChIKeys
        {
            get { return this.productIon.CandidateInChIKeys; }
        }
    }
}
