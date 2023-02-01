using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MspGenerator
{
    public class Peak
    {
        private double mz;
        private double intensity;
        private string comment;

        public Peak() { }

        public double Mz
        {
            get { return Math.Round(mz, 6); }
            set { mz = Math.Round(value, 6); }
        }

        public double Intensity
        {
            get { return Math.Round(intensity, 6); }
            set { intensity = Math.Round(value, 6); }
        }

        public string Comment
        {
            get { return comment; }
            set { comment = value; }
        }
    }

    public class MetaProperty
    {
        //smiles, formula, InChIKey, exactMass
        private string smiles;
        private string formula;
        private string InChIKey;
        private double exactMass;
        private double logP;

        public MetaProperty()
        {
            smiles = string.Empty;
            formula = string.Empty;
            InChIKey = string.Empty;
            exactMass = 0.0;
            logP = 0.0;
        }

        public string Smiles
        {
            get { return smiles; }
            set { smiles = value; }
        }

        public string Formula
        {
            get { return formula; }
            set { formula = value; }
        }

        public string inChIKey
        {
            get { return InChIKey; }
            set { InChIKey = value; }
        }

        public double ExactMass
        {
            get { return exactMass; }
            set { exactMass = value; }
        }

        public double LogP 
        {
            get { return logP; }
            set { logP = value; }
        }
    }
}
