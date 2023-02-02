using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.FormulaGenerator.DataObj {
    public class FragmentOntology
    {
        private string shortInChIKey;
        private string smiles;
        private string formula;
        private string comment;
        private double frequency;
        private string chemOntID;

        public string ShortInChIKey
        {
            get { return shortInChIKey; }
            set { shortInChIKey = value; }
        }

        public string Smiles
        {
            get { return smiles; }
            set { smiles = value; }
        }

        public string Comment
        {
            get { return comment; }
            set { comment = value; }
        }

        public string Formula {
            get { return formula; }
            set { formula = value; }
        }

        public double Frequency {
            get { return frequency; }
            set { frequency = value; }
        }

        public string ChemOntID {
            get { return chemOntID; }
            set { chemOntID = value; }
        }
    }
}
