using CompMs.Common.DataObj.Ion;
using CompMs.Common.DataObj.Property;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.FormulaGenerator.DataObj {
    public class FormulaResult
    {
        public FormulaResult()
        {
           
        }

        #region properties
        public Formula Formula { get; set; } = new Formula();

        public double MatchedMass { get; set; }
        public double MassDiff { get; set; }
        public double MassDiffPpm { 
            get => MassDiff / Formula.Mass*1000000;
        }
        public double M1IsotopicDiff { get; set; }
        public double M2IsotopicDiff { get; set; }
        public double M1IsotopicIntensity { get; set; }
        public double M2IsotopicIntensity { get; set; }
        public double MassDiffScore { get; set; }
        public double IsotopicScore { get; set; }
        public double ProductIonScore { get; set; }
        public int NeutralLossHits { get; set; }
        public double NeutralLossScore { get; set; }
        public int ProductIonHits { get; set; }
        public int ProductIonNum { get; set; }
        public int NeutralLossNum { get; set; }
        public double ResourceScore { get; set; }
        public string ResourceNames { get; set; } = string.Empty;
        public int ResourceRecords { get; set; }
        public double TotalScore { get; set; }
        public bool IsSelected { get; set; }
        public List<ProductIon> ProductIonResult { get; set; } = new List<ProductIon>();
        public List<NeutralLoss> NeutralLossResult { get; set; } = new List<NeutralLoss>();
        public List<AnnotatedIon> AnnotatedIonResult { get; set; } = new List<AnnotatedIon>();
        public List<int> PubchemResources { get; set; } = new List<int>();
        public List<string> ChemicalOntologyDescriptions { get; set; } = new List<string>();
        public List<string> ChemicalOntologyIDs { get; set; } = new List<string>();
        public List<double> ChemicalOntologyScores { get; set; } = new List<double>();
        public List<string> ChemicalOntologyRepresentativeInChIKeys { get; set; } = new List<string>();

        #endregion
    }
}
