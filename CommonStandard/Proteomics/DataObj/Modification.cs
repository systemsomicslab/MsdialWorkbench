using CompMs.Common.DataObj.Ion;
using CompMs.Common.DataObj.Property;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.Proteomics.DataObj {

    public class ModificationContainer {
        public List<Modification> ProteinNtermMods { get; set; }
        public List<Modification> ProteinCtermMods { get; set; }
        public List<Modification> AnyNtermMods { get; set; }
        public List<Modification> AnyCtermMods { get; set; }
        public List<Modification> AnywhereMods { get; set; }
        public List<Modification> NotCtermMods { get; set; }

        public Dictionary<char, ModificationSeq> AnywehereSite2Mod { get; set; }


        public ModificationContainer(List<Modification> modifications) {
            ProteinNtermMods = modifications.Where(n => n.Position == "proteinNterm").ToList(); 
            ProteinCtermMods = modifications.Where(n => n.Position == "proteinCterm").ToList(); 
            AnyCtermMods = modifications.Where(n => n.Position == "anyNterm").ToList(); 
            AnyCtermMods = modifications.Where(n => n.Position == "anyCterm").ToList(); 
            AnywhereMods = modifications.Where(n => n.Position == "anywhere").ToList();
            NotCtermMods = modifications.Where(n => n.Position == "notCterm").ToList();
        }
    }

    public class ModificationSeq {
        public AminoAcid OriginalAA { get; set; }


        public string ModifiedAACode { get; set; } //Tp, K(Acethyl)
        public AminoAcid ModifiedAA { get; set; }
        public List<Modification> ModSequence { get; set; } // A -> K -> K(Acethyl)
    }

    //proteinNterm modification is allowed only once.
    //proteinCterm modification is allowed only once.
    //anyCterm modification is allowed only once.
    //anyNterm modification is allowed only once.
    public class Modification {
        public string Title { get; set; }
        public string Description { get; set; }
        public Formula Composition { get; set; } // only derivative moiety 
        public Formula ModifiedAminoResidue { get; set; }
        public List<string> ModificationSites { get; set; } = new List<string>();
        public string Position { get; set; } // anyCterm, anyNterm, anywhere, notCterm, proteinCterm, proteinNterm
        public string Type { get; set; } // Standard, Label, IsobaricLabel, Glycan, AaSubstitution, CleavedCrosslink, NeuCodeLabel
        public List<ProductIon> DiagnosticIons { get; set; } = new List<ProductIon>();
        public List<NeutralLoss> DiagnosticNLs { get; set; } = new List<NeutralLoss>();

    }
}
