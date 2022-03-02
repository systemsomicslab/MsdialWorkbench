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
        private static double NH3 = 17.026549101;
        private static double H3PO4 = 97.976895575;


        public static MoleculeMsReference Convert2SpecObj(Peptide peptide, AdductIon adduct, CollisionType cType, double minMz = 100, double maxMz = 1000000) {
            switch (cType) {
                case CollisionType.CID: return GetTheoreticalSpectrumByHCD(peptide, adduct);
                case CollisionType.HCD: return GetTheoreticalSpectrumByHCD(peptide, adduct);
                default: return GetTheoreticalSpectrumByHCD(peptide, adduct);
            }
        }

        public static List<SpectrumPeak> Convert2SpecPeaks(Peptide peptide, AdductIon adduct, CollisionType cType, double minMz = 100, double maxMz = 1000000) {
            switch (cType) {
                case CollisionType.CID: return GetSpectrumPeaksByHCD(peptide, adduct);
                case CollisionType.HCD: return GetSpectrumPeaksByHCD(peptide, adduct);
                default: return GetSpectrumPeaksByHCD(peptide, adduct);
            }
        }

        public static MoleculeMsReference GetTheoreticalSpectrumByHCD(Peptide peptide, AdductIon adduct, double minMz = 100, double maxMz = 1000000) {

            var msref = GetBasicMsRefProperty(peptide, adduct);
            var spectrumPeaks = GetSpectrumPeaksByHCD(peptide, adduct, minMz, maxMz);
            
            msref.Spectrum = spectrumPeaks;
            return msref;
        }

        public static List<SpectrumPeak> GetSpectrumPeaksByHCD(Peptide peptide, AdductIon adduct, double minMz = 100, double maxMz = 1000000) {

            var sequence = peptide.SequenceObj;
            var precursorMz = adduct.ConvertToMz(peptide.ExactMass);

            var spectrum = new List<SpectrumPeak>() {
                new SpectrumPeak() {
                    Mass = precursorMz, Intensity = 1000, SpectrumComment = SpectrumComment.precursor, PeakID = sequence.Count
                }
            };
            var bMz = Proton;
            var yMz = precursorMz;

            var bSequence = string.Empty;
            var ySequence = peptide.Sequence;

            var bModSequence = string.Empty;
            var yModSequence = peptide.ModifiedSequence;

            if (yModSequence.Contains("Y[Phospho]")) {
                spectrum.Add(new SpectrumPeak() { Mass = 216.042021256, Intensity = 50, SpectrumComment = SpectrumComment.tyrosinep, PeakID = 0 });
            }

            for (int i = 0; i < sequence.Count; i++) { // N -> C

                var aaResidueMass = sequence[i].ExactMass() - H2O;
                bMz += aaResidueMass;
                yMz -= aaResidueMass;
                if (i == sequence.Count - 1) bMz += H2O;

                bSequence += sequence[i].OneLetter;
                ySequence = ySequence.Substring(1);

                bModSequence += sequence[i].ModifiedCode;
                yModSequence = yModSequence.Substring(sequence[i].Code().Length);

                if (bMz >= minMz && bMz <= maxMz)
                    spectrum.Add(new SpectrumPeak() { Mass = bMz, Intensity = 1000, SpectrumComment = SpectrumComment.b, PeakID = i + 1 });
                if (yMz >= minMz && yMz <= maxMz)
                    spectrum.Add(new SpectrumPeak() { Mass = yMz, Intensity = 1000, SpectrumComment = SpectrumComment.y, PeakID = sequence.Count - i - 1 });

                if (bMz * 0.5 >= minMz && bMz * 0.5 <= maxMz)
                    spectrum.Add(new SpectrumPeak() { Mass = bMz * 0.5, Intensity = 100, SpectrumComment = SpectrumComment.b2, PeakID = i + 1 });
                if (yMz * 0.5 >= minMz && yMz * 0.5 <= maxMz)
                    spectrum.Add(new SpectrumPeak() { Mass = yMz * 0.5, Intensity = 100, SpectrumComment = SpectrumComment.y2, PeakID = sequence.Count - i - 1 });

                if (bSequence.Contains("D") || bSequence.Contains("E") || bSequence.Contains("S") || bSequence.Contains("T")) {
                    if (bMz - H2O >= minMz && bMz - H2O <= maxMz)
                        spectrum.Add(new SpectrumPeak() { Mass = bMz - H2O, Intensity = 200, SpectrumComment = SpectrumComment.b_h2o, PeakID = i + 1 });
                }
                if (ySequence.Contains("D") || ySequence.Contains("E") || ySequence.Contains("S") || ySequence.Contains("T")) {
                    if (yMz - H2O >= minMz && yMz - H2O <= maxMz)
                        spectrum.Add(new SpectrumPeak() { Mass = yMz - H2O, Intensity = 200, SpectrumComment = SpectrumComment.y_h2o, PeakID = sequence.Count - i - 1 });
                }

                if (bSequence.Contains("K") || bSequence.Contains("N") || bSequence.Contains("Q") || bSequence.Contains("R")) {
                    if (bMz - NH3 >= minMz && bMz - NH3 <= maxMz)
                        spectrum.Add(new SpectrumPeak() { Mass = bMz - NH3, Intensity = 200, SpectrumComment = SpectrumComment.b_nh3, PeakID = i + 1 });
                }
                if (ySequence.Contains("K") || ySequence.Contains("N") || ySequence.Contains("Q") || ySequence.Contains("R")) {
                    if (yMz - NH3 >= minMz && yMz - NH3 <= maxMz)
                        spectrum.Add(new SpectrumPeak() { Mass = yMz - NH3, Intensity = 200, SpectrumComment = SpectrumComment.y_nh3, PeakID = sequence.Count - i - 1 });
                }

                if (bModSequence.Contains("S[Phospho]") || bModSequence.Contains("T[Phospho]")) {
                    if (bMz - H3PO4 >= minMz && bMz - H3PO4 <= maxMz)
                        spectrum.Add(new SpectrumPeak() { Mass = bMz - H3PO4, Intensity = 400, SpectrumComment = SpectrumComment.b_h3po4, PeakID = i + 1 });
                }
                if (yModSequence.Contains("S[Phospho]") || yModSequence.Contains("T[Phospho]")) {
                    if (yMz - H3PO4 >= minMz && yMz - H3PO4 <= maxMz)
                        spectrum.Add(new SpectrumPeak() { Mass = yMz - H3PO4, Intensity = 400, SpectrumComment = SpectrumComment.y_h3po4, PeakID = sequence.Count - i - 1 });
                }
            }
            return spectrum.OrderBy(n => n.Mass).ToList();
        }

        public static MoleculeMsReference GetBasicMsRefProperty(Peptide peptide, AdductIon adduct) {
            var precursorMz = adduct.ConvertToMz(peptide.ExactMass);
            var msref = new MoleculeMsReference() {
                PrecursorMz = precursorMz, IonMode = adduct.IonMode, Name = peptide.ModifiedSequence,
                Formula = peptide.Formula, Ontology = "Peptide", DatabaseID = peptide.DatabaseOriginID, DatabaseUniqueIdentifier = peptide.DatabaseOrigin
            };
            return msref;
        }
    }
}
