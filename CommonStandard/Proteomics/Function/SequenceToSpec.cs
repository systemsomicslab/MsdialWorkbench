using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Proteomics.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.Proteomics.Function {
    public sealed class SequenceToSpec {
        private SequenceToSpec() { }

        private static double OH = 17.002739652;
        private static double H = 1.00782503207;
        private static double H2O = 18.010564684;
        private static double Proton = 1.00727646688;
        private static double Electron = 0.0005485799;

        public static MoleculeMsReference Convert2SpecObj(Peptide peptide, AdductIon adduct, CollisionType cType) {
            switch (cType) {
                case CollisionType.CID: return GetTheoreticalSpectrumByHCD(peptide, adduct);
                case CollisionType.HCD: return GetTheoreticalSpectrumByHCD(peptide, adduct);
                default: return GetTheoreticalSpectrumByHCD(peptide, adduct);
            }
        }

        public static MoleculeMsReference GetTheoreticalSpectrumByHCD(Peptide peptide, AdductIon adduct) {

            var sequence = peptide.SequenceObj;
            var msref = GetBasicMsRefProperty(peptide, adduct);

            var spectrum = new List<SpectrumPeak>() {
                new SpectrumPeak() {
                    Mass = msref.PrecursorMz, Intensity = 1000, Comment = "Precursor"
                }
            };
            var bMz = Proton;
            var yMz = msref.PrecursorMz;
            for (int i = 0; i < sequence.Count; i++) { // N -> C

                var aaResidueMass = sequence[i].ExactMass() - H2O;
                bMz += aaResidueMass;
                yMz -= aaResidueMass;

                spectrum.Add(new SpectrumPeak() { Mass = bMz, Intensity = 1000, Comment = "b_" + (i + 1).ToString() });
                spectrum.Add(new SpectrumPeak() { Mass = yMz, Intensity = 1000, Comment = "y_" + (sequence.Count - i).ToString() });
            }
            msref.Spectrum = spectrum.OrderBy(n => n.Mass).ToList();

            return msref;
        }

        public static MoleculeMsReference GetBasicMsRefProperty(Peptide peptide, AdductIon adduct) {
            var precursorMz = MolecularFormulaUtility.ConvertExactMassToPrecursorMz(adduct, peptide.ExactMass());
            var msref = new MoleculeMsReference() {
                PrecursorMz = precursorMz, IonMode = adduct.IonMode, Name = peptide.ModifiedSequence,
                Formula = peptide.Formula, Ontology = "Peptide"
            };
            return msref;
        }
    }
}
