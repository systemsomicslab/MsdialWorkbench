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

        //public string ModifiedLetters { get; set; }
        //public string ModifiedNtermLetters { get; set; }
        //public string ModifiedCtermLetters { get; set; }
        //public bool IsResidueModified { get; set; }
        //public bool IsNtermModified { get; set; }
        //public bool IsCtermModified { get; set; }
        //public Formula ModifiedFormula { get; set; } = null; // modified molecular formula info

        public AminoAcid(char oneletter) {
            var char2formula = AminoAcidObjUtility.OneChar2FormulaString;
            var char2string = AminoAcidObjUtility.OneChar2ThreeLetter;
            this.OneLetter = oneletter;
            this.ThreeLetters = char2string[oneletter]; 
            this.Formula = FormulaStringParcer.Convert2FormulaObjV2(char2formula[oneletter]);
        }

        public AminoAcid(char oneletter, string code, Formula formula) {
            OneLetter = oneletter; ThreeLetters = code; Formula = formula;
        }
    }
}
