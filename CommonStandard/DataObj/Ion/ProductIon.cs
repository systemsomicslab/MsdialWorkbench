using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.DataObj.Ion {
    /// <summary>
    /// This class is the storage of product ion assignment used in MS-FINDER program.
    /// </summary>
    [MessagePackObject]
    public class ProductIon
    {
        public ProductIon() {
        }

        [Key(0)]
        public double Mass { get; set; }
        [Key(1)]
        public double Intensity { get; set; }
        [Key(2)]
        public Formula Formula { get; set; } = new Formula();
        [Key(3)]
        public IonMode IonMode { get; set; }
        [Key(4)]
        public string Smiles { get; set; }
        [Key(5)]
        public double MassDiff { get; set; }
        [Key(6)]
        public double IsotopeDiff { get; set; }
        [Key(7)]
        public string Comment { get; set; }
        [Key(8)]
        public string Name { get; set; }
        [Key(9)]
        public string ShortName { get; set; }
        [Key(10)]
        public List<string> CandidateInChIKeys { get; set; } = new List<string>();
        [Key(11)]
        public double Frequency { get; set; }
        [Key(12)]
        public List<string> CandidateOntologies { get; set; } = new List<string>();
    }
}
