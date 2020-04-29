using CompMs.Common.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.DataObj {
    /// <summary>
    /// This class is the storage of product ion assignment used in MS-FINDER program.
    /// </summary>
    public class ProductIon
    {
        public ProductIon() {
        }

        public double Mass { get; set; }
        public double Intensity { get; set; }
        public Formula Formula { get; set; } = new Formula();
        public IonMode IonMode { get; set; }
        public string Smiles { get; set; }
        public double MassDiff { get; set; }
        public double IsotopeDiff { get; set; }
        public string Comment { get; set; }
        public List<string> CandidateInChIKeys { get; set; } = new List<string>();
        public double Frequency { get; set; }
        public List<string> CandidateOntologies { get; set; } = new List<string>();
    }
}
