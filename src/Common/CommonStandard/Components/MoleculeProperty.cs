using System;
using System.Collections.Generic;
using System.Text;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Interfaces;

namespace CompMs.Common.Components
{
    public class MoleculeProperty: IMoleculeProperty
    {
        #region Properties
        public string Name { get; set; } = string.Empty;
        public Formula Formula { get; set; } = new Formula();
        public string Ontology { get; set; } = string.Empty;
        public string SMILES { get; set; } = string.Empty;
        public string InChIKey { get; set; } = string.Empty;
        #endregion

        public MoleculeProperty() { }
        public MoleculeProperty(string name, Formula formula, string ontology, string smiles, string inchikey) {
            Name = name;
            Formula = formula;
            Ontology = ontology;
            SMILES = smiles;
            InChIKey = inchikey;
        }
    }
}
