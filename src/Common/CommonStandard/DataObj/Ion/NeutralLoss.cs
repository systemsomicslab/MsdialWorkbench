using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.DataObj.Ion {
    /// <summary>
    /// This class is the storage of each neutral loss information used in MS-FINDER program.
    /// </summary>
    [MessagePackObject]
    public class NeutralLoss
    {
        public NeutralLoss() {
            
        }

        [Key(0)]
        public double MassLoss { get; set; }
        [Key(1)]
        public double PrecursorMz { get; set; }
        [Key(2)]
        public double ProductMz { get; set; }
        [Key(3)]
        public double PrecursorIntensity { get; set; }
        [Key(4)]
        public double ProductIntensity { get; set; }
        [Key(5)]
        public double MassError { get; set; }
        [Key(6)]
        public Formula Formula { get; set; } = new Formula();
        [Key(7)]
        public IonMode Iontype { get; set; }
        [Key(8)]
        public string Comment { get; set; }
        [Key(9)]
        public string Smiles { get; set; }
        [Key(10)]
        public double Frequency { get; set; }
        [Key(11)]
        public string Name { get; set; }
        [Key(12)]
        public string ShortName { get; set; }
        [Key(13)]
        public List<string> CandidateInChIKeys { get; set; } = new List<string>();
        [Key(14)]
        public List<string> CandidateOntologies { get; set; } = new List<string>();
    }
}
