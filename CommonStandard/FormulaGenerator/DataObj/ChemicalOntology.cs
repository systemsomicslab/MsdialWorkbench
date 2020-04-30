using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.FormulaGenerator.DataObj {
    public class ChemicalOntology {
        public ChemicalOntology() {
        }

        public string OntologyDescription { get; set; } = string.Empty;
        public string OntologyID { get; set; } = string.Empty;
        public string RepresentativeInChIKey { get; set; } = string.Empty;
        public string RepresentativeSMILES { get; set; } = string.Empty;
        public List<string> FragmentInChIKeys { get; set; } = new List<string>();
        public IonMode IonMode { get; set; }
        public List<string> FragmentOntologies { get; set; } = new List<string>();
        public Formula Formula { get; set; } = new Formula();
    }
}
