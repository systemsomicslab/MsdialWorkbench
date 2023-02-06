using CompMs.Common.DataObj.Property;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.FormulaGenerator.Parser;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Proteomics.DataObj {
    [MessagePackObject]
    public class AminoAcid {
        [Key(0)]
        public char OneLetter { get; set; }
        [Key(1)]
        public string ThreeLetters { get; set; }
        [Key(2)]
        public Formula Formula { get; set; } // original formula information

        [Key(3)]
        public string ModifiedCode { get; set; }
        [Key(4)]
        public Formula ModifiedFormula { get; set; }
        [Key(5)]
        public Formula ModifiedComposition { get; set; }
        [Key(6)]
        public List<Modification> Modifications { get; set; }

        public bool IsModified() {
            return !ModifiedCode.IsEmptyOrNull();
        }

        public double ExactMass() {
            if (IsModified()) return ModifiedFormula.Mass;
            return Formula.Mass;
        }

        public string Code() {
            if (IsModified()) return ModifiedCode;
            return OneLetter.ToString();
        }

        public Formula GetFormula() {
            if (IsModified()) return ModifiedFormula;
            return Formula;
        }

        public AminoAcid() {

        }

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

        public AminoAcid(AminoAcid aa, string modifiedCode, Formula modifiedComposition) {
            this.OneLetter = aa.OneLetter;
            this.ThreeLetters = aa.ThreeLetters;
            this.Formula = aa.Formula;

            if (modifiedCode.IsEmptyOrNull()) return;

            this.ModifiedCode = modifiedCode;
            this.ModifiedComposition = modifiedComposition;
            this.ModifiedFormula = MolecularFormulaUtility.SumFormulas(modifiedComposition, aa.Formula);
        }

        public AminoAcid(AminoAcid aa, string modifiedCode, Formula modifiedComposition, List<Modification> modifications) {
            this.OneLetter = aa.OneLetter;
            this.ThreeLetters = aa.ThreeLetters;
            this.Formula = aa.Formula;

            if (modifiedCode.IsEmptyOrNull()) return;

            this.ModifiedCode = modifiedCode;
            this.ModifiedComposition = modifiedComposition;
            this.ModifiedFormula = MolecularFormulaUtility.SumFormulas(modifiedComposition, aa.Formula);
            this.Modifications = modifications;
        }
    }
}
