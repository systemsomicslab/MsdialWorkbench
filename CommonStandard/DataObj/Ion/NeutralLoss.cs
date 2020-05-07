using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.DataObj.Ion {
    /// <summary>
    /// This class is the storage of each neutral loss information used in MS-FINDER program.
    /// </summary>
    public class NeutralLoss
    {
        public NeutralLoss() {
            
        }

        public double MassLoss { get; set; }
        public double PrecursorMz { get; set; }
        public double ProductMz { get; set; }
        public double PrecursorIntensity { get; set; }
        public double ProductIntensity { get; set; }
        public double MassError { get; set; }
        public Formula Formula { get; set; } = new Formula();
        public IonMode Iontype { get; set; }
        public string Comment { get; set; }
        public string Smiles { get; set; }
        public double Frequency { get; set; }
        public List<string> CandidateInChIKeys { get; set; } = new List<string>();
        public List<string> CandidateOntologies { get; set; } = new List<string>();
    }
}
