using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.FormulaGenerator.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.Lipidomics {
    public sealed class PCCidSpectrumGenerator {

        private static readonly double H2O = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();

        private static readonly double C = MassDiffDictionary.CarbonMass;
        private static readonly double H = MassDiffDictionary.HydrogenMass;
        private static readonly double N = MassDiffDictionary.NitrogenMass;
        private static readonly double O = MassDiffDictionary.OxygenMass;
        private static readonly double S = MassDiffDictionary.SulfurMass;
        private static readonly double P = MassDiffDictionary.PhosphorusMass;
        private static readonly double Proton = MassDiffDictionary.ProtonMass;
        private static readonly double Electron = MassDiffDictionary.HydrogenMass - MassDiffDictionary.ProtonMass;
        private static readonly double HCOO = H * 2.0 + C * 1 + O * 2.0 - Proton;
        private static readonly double CH3COO = H * 4.0 + C * 2 + O * 2.0 - Proton;
        private static readonly double Na = 22.98976928 - Electron;


        private PCCidSpectrumGenerator() { }
        public static List<SpectrumPeak> Generate(Lipid lipid, AdductIon adduct) {
            var peaks = new List<SpectrumPeak>();
            switch (adduct.AdductIonName) {
                case "[M+H]+":
                    var M = lipid.Mass;
                    var chains = lipid.Chains.GetDeterminedChains();
                    var SN1 = chains[0].Mass;
                    var SN2 = chains[1].Mass;

                    // chain ions
                    peaks.Add(new SpectrumPeak(M - SN1 + H + Proton, 50, "M-SN1+H+Proton"));
                    peaks.Add(new SpectrumPeak(M - SN2 + H + Proton, 50, "M-SN2+H+Proton"));
                    peaks.Add(new SpectrumPeak(M - SN1 - H2O + H + Proton, 50, "M-SN1-H2O+H+Proton"));
                    peaks.Add(new SpectrumPeak(M - SN2 - H2O + H + Proton, 50, "M-SN2-H2O+H+Proton"));
                    
                    // precursors
                    peaks.Add(new SpectrumPeak(M + Proton, 300, "[M+H]+"));

                    // class ions
                    peaks.Add(new SpectrumPeak(
                        adduct.ConvertToMz(
                            C * 5 + H * 14 + O * 4 + N * 1 + P * 1 + Proton
                            ), 999, "C5H15NO4P+"));
                    return peaks;

                case "[M+Na]+":
                    M = lipid.Mass;

                    // precursors
                    peaks.Add(new SpectrumPeak(M + Na, 999, "[M+Na]+"));

                    // class ions
                    peaks.Add(new SpectrumPeak(
                       adduct.ConvertToMz(
                           M + Na - (C * 3 + H * 9 + N * 1)
                           ), 500, "[M+Na-C3H9N]+"));

                    peaks.Add(new SpectrumPeak(
                        adduct.ConvertToMz(
                            M + Na - (C * 5 + H * 14 + O * 4 + N * 1 + P * 1)
                            ), 600, "[M+Na-C5H14NO4P]+"));

                    peaks.Add(new SpectrumPeak(
                        adduct.ConvertToMz(
                            M + Proton - (C * 5 + H * 15 + O * 4 + N * 1 + P * 1)
                            ), 400, "[M+H-C5H15NO4P]+"));

                    peaks.Add(new SpectrumPeak(
                       adduct.ConvertToMz(
                           C * 5 + H * 11 + N * 1 + Proton
                           ), 100, "C5H12N+"));

                    peaks.Add(new SpectrumPeak(
                        adduct.ConvertToMz(
                            C * 2 + H * 6 + O * 4 + N * 1 + P * 1 + Na
                            ), 400, "C2H5O4PNa+"));

                    return peaks;

                case "[M+HCOO]-":

                    M = lipid.Mass;
                    chains = lipid.Chains.GetDeterminedChains();
                    SN1 = chains[0].Mass;
                    SN2 = chains[1].Mass;

                    // chain ions
                    peaks.Add(new SpectrumPeak(SN1 - Proton, 600, "SN1-H"));
                    peaks.Add(new SpectrumPeak(SN2 - Proton, 600, "SN1-H"));
                    peaks.Add(new SpectrumPeak(M - SN1 - (C * 1 + H * 2 + Proton * 1), 50, "M-CH3-SN1"));
                    peaks.Add(new SpectrumPeak(M - SN1 - H2O - (C * 1 + H * 2 + Proton * 1), 50, "M-CH3-SN1-H2O"));
                    peaks.Add(new SpectrumPeak(M - SN2 - (C * 1 + H * 2 + Proton * 1), 50, "M-CH3-SN2"));
                    peaks.Add(new SpectrumPeak(M - SN2 - H2O - (C * 1 + H * 2 + Proton * 1), 50, "M-CH3-SN2-H2O"));

                    // precursors
                    peaks.Add(new SpectrumPeak(M + HCOO, 999, "[M+HCOO]-"));

                    // class ions
                    peaks.Add(new SpectrumPeak(
                        adduct.ConvertToMz(
                            M - (C * 1 + H * 2 + Proton * 1)
                            ), 999, "[M-CH3]-"));

                    peaks.Add(new SpectrumPeak(
                       adduct.ConvertToMz(
                           C * 7 + H * 15 + O * 5 + N * 1 + P * 1 + Electron * 1
                           ), 100, "C7H15NO5P-"));

                    return peaks;

                case "[M+CH3COO]-":

                    M = lipid.Mass;
                    chains = lipid.Chains.GetDeterminedChains();
                    SN1 = chains[0].Mass;
                    SN2 = chains[1].Mass;

                    // chain ions
                    peaks.Add(new SpectrumPeak(SN1 - Proton, 600, "SN1-H"));
                    peaks.Add(new SpectrumPeak(SN2 - Proton, 600, "SN1-H"));
                    peaks.Add(new SpectrumPeak(M - SN1 - (C * 1 + H * 2 + Proton * 1), 50, "M-CH3-SN1"));
                    peaks.Add(new SpectrumPeak(M - SN1 - H2O - (C * 1 + H * 2 + Proton * 1), 50, "M-CH3-SN1-H2O"));
                    peaks.Add(new SpectrumPeak(M - SN2 - (C * 1 + H * 2 + Proton * 1), 50, "M-CH3-SN2"));
                    peaks.Add(new SpectrumPeak(M - SN2 - H2O - (C * 1 + H * 2 + Proton * 1), 50, "M-CH3-SN2-H2O"));

                    // precursors
                    peaks.Add(new SpectrumPeak(M + CH3COO, 999, "[M+CH3COO]-"));

                    // class ions
                    peaks.Add(new SpectrumPeak(
                        adduct.ConvertToMz(
                            M - (C * 1 + H * 2 + Proton * 1)
                            ), 999, "[M-CH3]-"));

                    peaks.Add(new SpectrumPeak(
                       adduct.ConvertToMz(
                           C * 7 + H * 15 + O * 5 + N * 1 + P * 1 + Electron * 1
                           ), 100, "C7H15NO5P-"));

                    return peaks;

                default: return null;
            }
        }
    }
}
