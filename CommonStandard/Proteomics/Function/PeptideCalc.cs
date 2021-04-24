using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Extension;
using CompMs.Common.Proteomics.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.Proteomics.Function {
    public sealed class PeptideCalc {
        private PeptideCalc() { }

        private static double OH = 17.002739652;
        private static double H = 1.00782503207;
        private static double H2O = 18.010564684;
       
        // N -> C, just return exactmass using default setting
        public static double Sequence2Mass(string sequence) {
            var mass = 0.0;
            var char2mass = AminoAcidDictionary.OneChar2Mass;
            var offsetMass = OH + H2O * (sequence.Length - 2) + H; // N-terminal, internal amino acids, C-terminal
            for (int i = 0; i < sequence.Length; i++) {
                var aaChar = sequence[i];
                if (char2mass.ContainsKey(aaChar)) {
                    mass += char2mass[aaChar];
                }
            }
            return mass - offsetMass;
        }

        // just return peptide obj containing exactmass using default setting
        public static Peptide Sequence2Peptide(string sequence) {
            
            var formula = Sequence2Formula(sequence);
            return new Peptide() { Sequence = sequence, ExactMass = formula.Mass, Formula = formula };
        }

        public static Peptide Sequence2Peptide(Peptide peptide) {
            var sequence = peptide.Sequence;
            var formula = Sequence2Formula(sequence);
            peptide.ExactMass = formula.Mass;
            peptide.Formula = formula;
            return peptide;
        }

        public static Peptide Sequence2Peptide(Peptide peptide, Dictionary<char, AminoAcid> aaDict, List<Modification> modifications) {
            var sequence = peptide.Sequence;
            if (aaDict.IsEmptyOrNull()) return Sequence2Peptide(peptide);

            var proteinNtermMod = modifications.Count(n => n.Position == "proteinNterm") > 0 ? modifications.Where(n => n.Position == "proteinNterm").ToList()[0] : null;
            var proteinCtermMod = modifications.Count(n => n.Position == "proteinCterm") > 0 ? modifications.Where(n => n.Position == "proteinCterm").ToList()[0] : null;
            var anyNtermMod = modifications.Count(n => n.Position == "anyNterm") > 0 ? modifications.Where(n => n.Position == "anyNterm").ToList()[0] : null;
            var anyCtermMod = modifications.Count(n => n.Position == "anyCterm") > 0 ? modifications.Where(n => n.Position == "anyCterm").ToList()[0] : null;
            var anywhereMods = modifications.Where(n => n.Position == "anywhere").ToList();
            var notCtermMods = modifications.Where(n => n.Position == "notCterm").ToList();

            var aaSequence = new List<AminoAcid>();
            var cnum = 0;
            var hnum = 0;
            var pnum = 0;
            var snum = 0;
            var onum = 0;
            var nnum = 0;
            var fnum = 0;
            var clnum = 0;
            var brnum = 0;
            var inum = 0;
            var sinum = 0;
            var c13num = 0;
            var h2num = 0;
            var n15num = 0;
            var o18num = 0;
            var s34num = 0;
            var cl37num = 0;
            var br81num = 0;

            var offsetHydrogen = (sequence.Length - 1) * 2;
            var offsetOxygen = sequence.Length - 1;

            for (int i = 0; i < sequence.Length; i++) {
                var aaChar = sequence[i];
                if (aaDict.ContainsKey(aaChar)) {
                    var aa = aaDict[aaChar];

                    foreach (var mod in anywhereMods) {

                    }














                    //if (i == sequence.Length - 1 && notCtermMod != null) { // meaning the use of original molecular formula
                    //    elementIncrements(aa.Formula, ref cnum, ref hnum, ref pnum, ref snum, ref onum, ref nnum,
                    //        ref fnum, ref clnum, ref brnum, ref inum, ref sinum, ref c13num,
                    //        ref h2num, ref n15num, ref o18num, ref s34num, ref cl37num, ref br81num);

                    //    aaSequence.Add(new AminoAcid() { Formula = aa.Formula.Clone(), OneLetter = aa.OneLetter, ThreeLetters = aa.ThreeLetters });
                    //}
                    //else {
                    //    var modAAFormula = aa.ModifiedFormula;
                    //    elementIncrements(modAAFormula, ref cnum, ref hnum, ref pnum, ref snum, ref onum, ref nnum,
                    //        ref fnum, ref clnum, ref brnum, ref inum, ref sinum, ref c13num,
                    //        ref h2num, ref n15num, ref o18num, ref s34num, ref cl37num, ref br81num);
                    //}
                }
            }

            if (proteinNtermMod != null && peptide.IsProteinNterminal) {
                elementIncrements(proteinNtermMod.Composition, ref cnum, ref hnum, ref pnum, ref snum, ref onum, ref nnum,
                            ref fnum, ref clnum, ref brnum, ref inum, ref sinum, ref c13num,
                            ref h2num, ref n15num, ref o18num, ref s34num, ref cl37num, ref br81num);
            } 
            else if (anyNtermMod != null) {
                elementIncrements(anyNtermMod.Composition, ref cnum, ref hnum, ref pnum, ref snum, ref onum, ref nnum,
                            ref fnum, ref clnum, ref brnum, ref inum, ref sinum, ref c13num,
                            ref h2num, ref n15num, ref o18num, ref s34num, ref cl37num, ref br81num);
            }

            if (proteinCtermMod != null && peptide.IsProteinCterminal) {
                elementIncrements(proteinCtermMod.Composition, ref cnum, ref hnum, ref pnum, ref snum, ref onum, ref nnum,
                            ref fnum, ref clnum, ref brnum, ref inum, ref sinum, ref c13num,
                            ref h2num, ref n15num, ref o18num, ref s34num, ref cl37num, ref br81num);
            }
            else if (anyCtermMod != null) {
                elementIncrements(anyCtermMod.Composition, ref cnum, ref hnum, ref pnum, ref snum, ref onum, ref nnum,
                            ref fnum, ref clnum, ref brnum, ref inum, ref sinum, ref c13num,
                            ref h2num, ref n15num, ref o18num, ref s34num, ref cl37num, ref br81num);
            }

            hnum -= offsetHydrogen;
            onum -= offsetOxygen;

            var formula = new Formula(cnum, hnum, nnum, onum, pnum, snum, fnum, clnum, brnum, inum, sinum,
                c13num, h2num, n15num, o18num, s34num, cl37num, br81num);

            peptide.ExactMass = formula.Mass;
            peptide.Formula = formula;
            return peptide;
        }

        private static void elementIncrements(Formula formula, ref int cnum, ref int hnum, ref int pnum, ref int snum,
            ref int onum, ref int nnum, ref int fnum, ref int clnum, ref int brnum, ref int inum, ref int sinum,
            ref int c13num, ref int h2num, ref int n15num, ref int o18num, ref int s34num, ref int cl37num, ref int br81num) {
            cnum += formula.Cnum;
            hnum += formula.Hnum;
            pnum += formula.Pnum;
            snum += formula.Snum;
            onum += formula.Onum;
            nnum += formula.Nnum;
            fnum += formula.Fnum;
            clnum += formula.Clnum;
            brnum += formula.Brnum;
            inum += formula.Inum;
            sinum += formula.Sinum;
            c13num += formula.C13num;
            h2num += formula.H2num;
            n15num += formula.N15num;
            o18num += formula.O18num;
            s34num += formula.S34num;
            cl37num += formula.Cl37num;
            br81num += formula.Br81num;
        }

        public static Formula Sequence2Formula(string sequence) {
            var carbon = 0;
            var hydrogen = 0;
            var nitrogen = 0;
            var oxygen = 0;
            var sulfur = 0;

            var char2formula = AminoAcidDictionary.OneChar2Formula;
            var offsetHydrogen = (sequence.Length - 1) * 2;
            var offsetOxygen = sequence.Length - 1;

            for (int i = 0; i < sequence.Length; i++) {
                var aaChar = sequence[i];
                if (char2formula.ContainsKey(aaChar)) {
                    carbon += char2formula[aaChar].Cnum;
                    hydrogen += char2formula[aaChar].Hnum;
                    nitrogen += char2formula[aaChar].Nnum;
                    oxygen += char2formula[aaChar].Onum;
                    sulfur += char2formula[aaChar].Snum;
                }
            }

            return new Formula(carbon, hydrogen - offsetHydrogen, nitrogen, oxygen - offsetOxygen, 0, sulfur, 0, 0, 0, 0, 0);
        }
    }
}
