using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv
{
    public class NeutralLossVM : ViewModelBase
    {
        private NeutralLoss neutralLoss;

        public NeutralLossVM(NeutralLoss neutralLoss)
        {
            this.neutralLoss = neutralLoss;
        }

        public NeutralLoss NeutralLoss
        {
            get { return neutralLoss; }
            set { neutralLoss = value; }
        }

        public string Formula
        {
            get { return this.neutralLoss.Formula.FormulaString; }
        }

        public double ExactMass
        {
            get { return this.neutralLoss.Formula.Mass; }
        }

        public double LossMass
        {
            get { return this.neutralLoss.MassLoss; }
        }

        public double PrecursorMass
        {
            get { return this.neutralLoss.PrecursorMz; }
        }

        public double ProductMass
        {
            get { return this.neutralLoss.ProductMz; }
        }

        public double NeutralLossError
        {
            get { return this.neutralLoss.MassError; }
        }

        public string Comment
        {
            get { return this.neutralLoss.Comment; }
        }

        public List<string> CandidateInChIKeys
        {
            get { return this.neutralLoss.CandidateInChIKeys; }
        }
    }
}
