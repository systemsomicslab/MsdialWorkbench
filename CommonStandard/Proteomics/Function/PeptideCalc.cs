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
            var char2mass = AminoAcidObjUtility.OneChar2Mass;
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
            return new Peptide() { Sequence = sequence, Formula = formula };
        }

        public static Peptide Sequence2Peptide(Peptide peptide) {
            var sequence = peptide.Sequence;
            var formula = Sequence2Formula(sequence);
            peptide.Formula = formula;
            return peptide;
        }

        public static Peptide Sequence2ModifiedPeptide(Peptide peptide, ModificationContainer container) {
            var sequence = peptide.Sequence;
            if (container.IsEmptyOrNull()) return Sequence2Peptide(peptide);

            var isProteinNTerminal = peptide.IsProteinNterminal;
            var isProteinCTerminal = peptide.IsProteinCterminal;
            var aaSequence = new List<AminoAcid>();
            for (int i = 0; i < sequence.Length; i++) {
                var aaChar = sequence[i];
                var modseq = new List<Modification>();
                if (i == 0 && isProteinNTerminal && container.ProteinNterm2Mod[aaChar].IsModified()) {
                    setModificationSequence(modseq, container.ProteinNterm2Mod[aaChar]);
                }
                else if (i == 0 && container.AnyNtermSite2Mod[aaChar].IsModified()) {
                    setModificationSequence(modseq, container.AnyNtermSite2Mod[aaChar]);
                }

                if (i != 0 && i != sequence.Length - 1 && container.NotCtermSite2Mod[aaChar].IsModified()) {
                    setModificationSequence(modseq, container.NotCtermSite2Mod[aaChar]);
                }

                if (i == sequence.Length - 1 && isProteinCTerminal && container.ProteinCtermSite2Mod[aaChar].IsModified()) {
                    setModificationSequence(modseq, container.ProteinCtermSite2Mod[aaChar]);
                }
                else if (i == sequence.Length - 1 && container.AnyCtermSite2Mod[aaChar].IsModified()) {
                    setModificationSequence(modseq, container.AnyCtermSite2Mod[aaChar]);
                }

                if (container.AnywehereSite2Mod[aaChar].IsModified()) {
                    setModificationSequence(modseq, container.AnywehereSite2Mod[aaChar]);
                }

                var compositions = ModificationUtility.GetModifiedCompositions(aaChar.ToString(), modseq);
                var aa = new AminoAcid(container.AnywehereSite2Mod[aaChar].OriginalAA, compositions.Item1, compositions.Item2);
                aaSequence.Add(aa);
            }
            if (peptide.Sequence == "AQACHFITMCIFTCTAQHSSIHLGQLDWFYWVPNAPCTMR") {
                Console.WriteLine();
            }
            peptide.SequenceObj = aaSequence;
            peptide.Formula = CalculatePeptideFormula(aaSequence);
            peptide.ModifiedSequence = String.Join("", aaSequence.Select(n => n.Code()));
           
            return peptide;
        }

        private static Formula CalculatePeptideFormula(List<AminoAcid> aaSequence) {
            var dict = new Dictionary<string, int>();
            foreach (var aa in aaSequence) {
                var formula = aa.GetFormula();
                foreach (var pair in formula.Element2Count) {
                    if (dict.ContainsKey(pair.Key)) {
                        dict[pair.Key] += pair.Value;
                    }
                    else {
                        dict[pair.Key] = pair.Value;
                    }
                }
            }

            var offsetHydrogen = (aaSequence.Count - 1) * 2;
            var offsetOxygen = aaSequence.Count - 1;

            dict["H"] -= offsetHydrogen;
            dict["O"] -= offsetOxygen;

            return new Formula(dict);
        }

        private static void setModificationSequence(List<Modification> modseq, ModificationProtocol protocol) {
            var mods = protocol.ModSequence;
            foreach (var mod in mods) {
                modseq.Add(mod);
            }
        }

        public static Formula Sequence2Formula(string sequence) {
            var carbon = 0;
            var hydrogen = 0;
            var nitrogen = 0;
            var oxygen = 0;
            var sulfur = 0;

            var char2formula = AminoAcidObjUtility.OneChar2Formula;
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
