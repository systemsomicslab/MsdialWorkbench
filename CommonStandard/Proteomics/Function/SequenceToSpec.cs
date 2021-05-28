using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Proteomics.DataObj;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Proteomics.Function {
    public sealed class SequenceToSpec {
        private SequenceToSpec() { }
        public static MoleculeMsReference Convert2SpecObj(Peptide peptide, AdductIon adduct, Dictionary<char, double> mod2mass = null) {
            return null;
        }
    }
}
