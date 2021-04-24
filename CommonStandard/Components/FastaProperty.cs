using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Components {

    public enum PeptideModification { Carbamidomethyl }
    public enum Digestion { Trypsin, TrypsinP, Chymotrypsin, GluC, AspN, ArgN }

    public class FastaProperty : IFastaProperty {
        public string Header { get; set; }
        public string Sequence { get; set; }

        public string DB { get; set; }
        public string UniqueIdentifier { get; set; }
        public string EntryName { get; set; }
        public string ProteinName { get; set; }
        public string OrganismName { get; set; }
        public string OrganismIdentifier { get; set; }
        public string GeneName { get; set; }
        public string ProteinExistence { get; set; }
        public string SequenceVersion { get; set; }

        public bool IsValidated { get; set; } // no X(any), no *(stop codon), no -(gap)

    }
}
