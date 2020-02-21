using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// This class is the storage of each neutral loss information used in MS-FINDER program.
    /// </summary>
    public class NeutralLoss
    {
        private double massLoss;
        private double precursorMz;
        private double productMz;
        private double precursorIntensity;
        private double productIntensity;

        private double massError;

        private Formula formula;
        private IonMode iontype;
        private string comment;
        private string smiles;
        private double frequency;
        private List<string> candidateInChIKeys;
        private List<string> candidateOntologies;

        public NeutralLoss() {
            formula = new Formula();
            candidateInChIKeys = new List<string>();
            candidateOntologies = new List<string>();
        }

        public double MassLoss
        {
            get { return massLoss; }
            set { massLoss = value; }
        }

        public double PrecursorMz
        {
            get { return precursorMz; }
            set { precursorMz = value; }
        }

        public double ProductMz
        {
            get { return productMz; }
            set { productMz = value; }
        }

        public double PrecursorIntensity
        {
            get { return precursorIntensity; }
            set { precursorIntensity = value; }
        }

        public double ProductIntensity
        {
            get { return productIntensity; }
            set { productIntensity = value; }
        }

        public double MassError
        {
            get { return massError; }
            set { massError = value; }
        }

        public Formula Formula
        {
            get { return formula; }
            set { formula = value; }
        }

        public IonMode Iontype
        {
            get { return iontype; }
            set { iontype = value; }
        }

        public string Comment
        {
            get { return comment; }
            set { comment = value; }
        }

        public string Smiles
        {
            get { return smiles; }
            set { smiles = value; }
        }

        public double Frequency {
            get { return frequency; }
            set { frequency = value; }
        }

        public List<string> CandidateInChIKeys
        {
            get { return candidateInChIKeys; }
            set { candidateInChIKeys = value; }
        }

        public List<string> CandidateOntologies {
            get { return candidateOntologies; }
            set { candidateOntologies = value; }
        }
    }
}
