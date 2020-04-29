using System;
using System.Collections.Generic;
using System.Text;
using CompMs.Common.Components;
using CompMs.Common.DataObj;

namespace CompMs.Common.Interfaces
{
    public interface IMoleculeProperty
    {
        string Name { get; set; }
        Formula Formula { get; set; }
        string Ontology { get; set; }
        string SMILES { get; set; }
        string InChIKey { get; set; }
    }
}
