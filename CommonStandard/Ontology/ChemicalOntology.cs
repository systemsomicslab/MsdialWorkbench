using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv {
    public class ChemicalOntology {
        private string ontologyDescription;
        private string ontologyID;
        private string representativeInChIKey;
        private string representativeSMILES;
        private Formula formula;
        private IonMode ionMode;
        private List<string> fragmentInChIKeys;
        private List<string> fragmentOntologies;

        public ChemicalOntology() {
            fragmentInChIKeys = new List<string>();
            FragmentOntologies = new List<string>();
        }

        public string OntologyDescription {
            get { return ontologyDescription; }
            set { ontologyDescription = value; }
        }

        public string OntologyID {
            get { return ontologyID; }
            set { ontologyID = value; }
        }

        public string RepresentativeInChIKey {
            get { return representativeInChIKey; }
            set { representativeInChIKey = value; }
        }

        public string RepresentativeSMILES {
            get { return representativeSMILES; }
            set { representativeSMILES = value; }
        }

        public List<string> FragmentInChIKeys {
            get { return fragmentInChIKeys; }
            set { fragmentInChIKeys = value; }
        }

        public IonMode IonMode {
            get { return ionMode; }
            set { ionMode = value; }
        }

        public List<string> FragmentOntologies {
            get { return fragmentOntologies; }
            set { fragmentOntologies = value; }
        }

        public Formula Formula {
            get { return formula; }
            set { formula = value; }
        }
    }
}
