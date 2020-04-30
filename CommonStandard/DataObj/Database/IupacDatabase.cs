using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.DataObj.Database {
    /// <summary>
    /// This is the storage of IUPAC queries described in 'IUPAC.txt' of Resources folder of MSDIAL assembry.
    /// Each chemical element such as C, N, O, S has the generic list of IupacChemicalElement.cs.
    /// This Iupac.cs has the queries of each chemical element detail as the dictionary.
    /// </summary>
    public class IupacDatabase {

        public Dictionary<int, List<AtomElementProperty>> Id2AtomElementProperties { get; set; } = new Dictionary<int, List<AtomElementProperty>>();
        public Dictionary<string, List<AtomElementProperty>> ElementName2AtomElementProperties { get; set; } = new Dictionary<string, List<AtomElementProperty>>();

        public IupacDatabase() { }
    }

    /// <summary>
    /// This is the storage of natural abundance or mass properties of each chemical element such as 12C, 13C etc.
    /// </summary>
    public class AtomElementProperty {

        public int ID { get; set; }

        public string ElementName { get; set; } = string.Empty;

        public int NominalMass { get; set; }

        public double NaturalRelativeAbundance { get; set; }

        public double ExactMass { get; set; }
    }
}
