using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// This class is the storage of product ion assignment used in MS-FINDER program.
    /// </summary>
    public class ProductIon
    {
        private double mass;
        private double intensity;

        private Formula formula;
        private IonMode ionMode;

        private string smiles;
        private double massDiff;
        private double isotopeDiff;
        private string comment;
        private double frequency;
        private List<string> candidateInChIKeys;
        private List<string> candidateOntologies;

        public ProductIon() {
            formula = new Formula();
            candidateInChIKeys = new List<string>();
            candidateOntologies = new List<string>();
        }

        public double Mass
        {
            get { return mass; }
            set { mass = value; }
        }

        public double Intensity
        {
            get { return intensity; }
            set { intensity = value; }
        }

        public Formula Formula
        {
            get { return formula; }
            set { formula = value; }
        }

        public IonMode IonMode
        {
            get { return ionMode; }
            set { ionMode = value; }
        }

        public string Smiles
        {
            get { return smiles; }
            set { smiles = value; }
        }

        public double MassDiff
        {
            get { return massDiff; }
            set { massDiff = value; }
        }

        public double IsotopeDiff
        {
            get { return isotopeDiff; }
            set { isotopeDiff = value; }
        }

        public string Comment
        {
            get { return comment; }
            set { comment = value; }
        }

        public List<string> CandidateInChIKeys
        {
            get { return candidateInChIKeys; }
            set { candidateInChIKeys = value; }
        }

        public double Frequency {
            get { return frequency; }
            set { frequency = value; }
        }

        public List<string> CandidateOntologies {
            get { return candidateOntologies; }
            set { candidateOntologies = value; }
        }

    }
}
