using CompMs.Common.DataObj.Property;
using CompMs.Common.FormulaGenerator.Parser;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Proteomics.DataObj {
    public class AminoAcid {
        public char OneLetter { get; set; }
        public string ThreeLetters { get; set; }
        public Formula Formula { get; set; } // original formula information

        public string ModifiedLetters { get; set; }
        public string ModifiedNtermLetters { get; set; }
        public string ModifiedCtermLetters { get; set; }
        public bool IsResidueModified { get; set; }
        public bool IsNtermModified { get; set; }
        public bool IsCtermModified { get; set; }
        public Formula ModifiedFormula { get; set; } = null; // modified molecular formula info

        public AminoAcid(char oneletter) {
            var char2formula = AminoAcidDictionary.OneChar2FormulaString;
            var char2string = AminoAcidDictionary.OneChar2ThreeLetter;
            this.OneLetter = oneletter;
            this.ThreeLetters = char2string[oneletter]; 
            this.Formula = FormulaStringParcer.Convert2FormulaObj(char2formula[oneletter]);
        }
    }
}
