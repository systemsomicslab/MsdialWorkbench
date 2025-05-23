﻿using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics {
    public sealed class LipidOadMsmsCharacterization {
        private LipidOadMsmsCharacterization() { }

        private const double Electron = 0.00054858026;
        private const double Proton = 1.00727641974;
        private const double H2O = 18.010564684;
        private const double Na = 22.98977;

        private static readonly double CH2 = new[] {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.CarbonMass,
        }.Sum();

        private static readonly double NH3 = new[] {
            MassDiffDictionary.HydrogenMass * 3,
            MassDiffDictionary.NitrogenMass,
        }.Sum();

        private static readonly double CD2 = new[] {
            MassDiffDictionary.Hydrogen2Mass * 2,
            MassDiffDictionary.CarbonMass,
        }.Sum();

        public static LipidMolecule JudgeIfPhosphatidylcholine(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond, 
           int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double, AdductIon adduct) 
        {
            var spectrum = msScanProp.Spectrum;
            var candidates = new List<LipidMolecule>();
            double Gly_C = new[] {
                MassDiffDictionary.CarbonMass * 8,
                MassDiffDictionary.HydrogenMass * 18,
                MassDiffDictionary.NitrogenMass,
                MassDiffDictionary.OxygenMass * 4,
                MassDiffDictionary.PhosphorusMass,
            }.Sum();

            double Gly_O = new[] {
                MassDiffDictionary.CarbonMass * 7,
                MassDiffDictionary.HydrogenMass * 16,
                MassDiffDictionary.NitrogenMass,
                MassDiffDictionary.OxygenMass * 5,
                MassDiffDictionary.PhosphorusMass,
            }.Sum();

            double C5H14NO4P = new[] {
                MassDiffDictionary.CarbonMass * 5,
                MassDiffDictionary.HydrogenMass * 14,
                MassDiffDictionary.NitrogenMass,
                MassDiffDictionary.OxygenMass * 4,
                MassDiffDictionary.PhosphorusMass,
            }.Sum();

            double C3H9N = new[] { 
                MassDiffDictionary.CarbonMass * 3,
                MassDiffDictionary.HydrogenMass * 9,
                MassDiffDictionary.NitrogenMass,
            }.Sum();

            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive) 
            {
                if (adduct.AdductIonName == "[M+H]+") 
                {
                    if (sn1Carbon < 10 || sn2Carbon < 10) return null;
                    if (sn1Double > 6 || sn2Double > 6) return null;
                    //  seek PC diagnostic
                    var threshold = 3.0;
                    var isClassIonFound1 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, C5H14NO4P + Proton, threshold);
                    if (!isClassIonFound1) return null;
                    // reject Na+ adduct
                    var diagnosticMz = theoreticalMz - C3H9N;
                    var isNaTypicalFound1 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, 10.0);
                    // reject PE
                    var PEHeaderLoss = theoreticalMz - 141.019094261;
                    var isClassIonFoundPe = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, PEHeaderLoss, 5.0);

                    if (isNaTypicalFound1 || isClassIonFoundPe) { return null; }

                    // from here, acyl level annotation is executed.
                    var nl_SN1 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) + MassDiffDictionary.HydrogenMass;
                    var nl_SN1_H2O = nl_SN1 - H2O;

                    var nl_SN2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) + MassDiffDictionary.HydrogenMass;
                    var nl_SN2_H2O = nl_SN2 - H2O;

                    var query = new List<SpectrumPeak> {
                        new() { Mass = nl_SN1, Intensity = 0.1 },
                        new() { Mass = nl_SN1_H2O, Intensity = 0.1 },
                        new() { Mass = nl_SN2, Intensity = 0.1 },
                        new() { Mass = nl_SN2_H2O, Intensity = 0.1 }
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount >= 2) {
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PC", LbmClass.PC, sn1Carbon, sn1Double,
                                    sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PC", LbmClass.PC, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
                else if (adduct.AdductIonName == "[M+Na]+") 
                {
                    //// seek 184.07332 (C5H15NO4P)
                    //var diagnosticMz = 184.07332;
                    // seek [M+Na -C5H14NO4P]+
                    var diagnosticMz2 = theoreticalMz - C5H14NO4P;
                    // seek [M+Na -C3H9N]+
                    var diagnosticMz3 = theoreticalMz - C3H9N;
                    var threshold = 3.0;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz3, threshold);
                    if (!isClassIonFound || !isClassIon2Found) return null;
                    // seek PC diagnostic
                    var isClassIonFound1 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, C5H14NO4P + Na, threshold);
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_C + Na, threshold);
                    var isClassIonFound3 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_O + Na, threshold);
                    if (!isClassIonFound1 || !isClassIonFound2 || !isClassIonFound3) return null;

                    // from here, acyl level annotation is executed.
                    for (int sn1CarbonNum = 6; sn1CarbonNum <= totalCarbon; sn1CarbonNum++)
                    {
                        for (int sn1DoubleNum = 0; sn1DoubleNum <= totalDoubleBond; sn1DoubleNum++)
                        {
                            var sn2CarbonNum = totalCarbon - sn1CarbonNum;
                            var sn2DoubleNum = totalDoubleBond - sn1DoubleNum;
                            if (sn2DoubleNum >= 7) continue;
                            var nl_SN1 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1CarbonNum, sn1DoubleNum) + MassDiffDictionary.HydrogenMass;
                            var nl_SN1_H2O = nl_SN1 - H2O;

                            var nl_SN2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2CarbonNum, sn2DoubleNum) + MassDiffDictionary.HydrogenMass;
                            var nl_SN2_H2O = nl_SN2 - H2O;

                            var query = new List<SpectrumPeak> {
                                new() { Mass = nl_SN1, Intensity = 0.1 },
                                new() { Mass = nl_SN1_H2O, Intensity = 0.1 },
                                new() { Mass = nl_SN2, Intensity = 0.1 },
                                new() { Mass = nl_SN2_H2O, Intensity = 0.1 }
                            };

                            var foundCount = 0;
                            var averageIntensity = 0.0;
                            LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                            if (foundCount >= 2) {
                                var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PC", LbmClass.PC, sn1Carbon, sn1Double,
                                    sn2Carbon, sn2Double, averageIntensity);
                                candidates.Add(molecule);
                            }
                        }
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PC", LbmClass.PC, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            } else {
                if (adduct.AdductIonName == "[M+FA-H]-" || adduct.AdductIonName == "[M+Hac-H]-" || 
                    adduct.AdductIonName == "[M+HCOO]-" || adduct.AdductIonName == "M+CH3COO]-") 
                {
                    // seek [M-CH3]-
                    var threshold = 10.0;
                    var diagnosticMz = adduct.AdductIonName == "[M+CH3COO]-" || adduct.AdductIonName == "[M+Hac-H]-" ?
                        theoreticalMz - 74.036779433 : theoreticalMz - 60.021129369;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    if (adduct.AdductIonName == "[M+CH3COO]-" || adduct.AdductIonName == "[M+Hac-H]-") {
                        var diagnosticMz2 = theoreticalMz - 60.021129369; // in source check
                        var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold);
                        if (isClassIonFound2) return null;
                    }

                    // from here, acyl level annotation is executed.
                    var sn1 = LipidMsmsCharacterizationUtility.fattyacidProductIon(sn1Carbon, sn1Double);
                    var sn2 = LipidMsmsCharacterizationUtility.fattyacidProductIon(sn2Carbon, sn2Double);

                    var query = new List<SpectrumPeak> {
                        new() { Mass = sn1, Intensity = 0.1 },
                        new() { Mass = sn2, Intensity = 0.1 },
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2) { // now I set 2 as the correct level
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PC", LbmClass.PC, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PC", LbmClass.PC, "", theoreticalMz, adduct,
                           totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
                else if (adduct.AdductIonName == "[M+HCO3]-") 
                {
                    //"[M-H]- -C3H9N"
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - MassDiffDictionary.ProtonMass - (12 * 4 + MassDiffDictionary.HydrogenMass * 10 + MassDiffDictionary.NitrogenMass + MassDiffDictionary.OxygenMass * 3);
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    var sn1 = LipidMsmsCharacterizationUtility.fattyacidProductIon(sn1Carbon, sn1Double);
                    var sn2 = LipidMsmsCharacterizationUtility.fattyacidProductIon(sn2Carbon, sn2Double);

                    var query = new List<SpectrumPeak> {
                        new() { Mass = sn1, Intensity = 0.1 },
                        new() { Mass = sn2, Intensity = 0.1 },
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount >= 1) {
                        var sn1FlagFragment = sn1 + (12 * 5 + MassDiffDictionary.HydrogenMass * 9 + MassDiffDictionary.PhosphorusMass + MassDiffDictionary.OxygenMass * 4);
                        var query2 = new List<SpectrumPeak> { new() { Mass = sn1FlagFragment, Intensity = 0.1 } };
                        LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query2, ms2Tolerance, out int foundCount2, out double averageIntensity2);

                        if (foundCount2 == 1) {
                            var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel3("PC", LbmClass.PC, sn1Carbon, sn1Double,
                                sn2Carbon, sn2Double, averageIntensity2);
                            candidates.Add(molecule);
                        } else {
                            var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PC", LbmClass.PC, sn1Carbon, sn1Double,
                                sn2Carbon, sn2Double, averageIntensity);
                            candidates.Add(molecule);
                        }
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PC", LbmClass.PC, "", theoreticalMz, adduct,
                           totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfPhosphatidylethanolamine(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond, 
            int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double, AdductIon adduct) 
        {
            double Gly_C = new[] {
                        MassDiffDictionary.CarbonMass * 5,
                        MassDiffDictionary.HydrogenMass * 12,
                        MassDiffDictionary.NitrogenMass,
                        MassDiffDictionary.OxygenMass * 4,
                        MassDiffDictionary.PhosphorusMass,
            }.Sum();

            double Gly_O = new[] {
                        MassDiffDictionary.CarbonMass * 4,
                        MassDiffDictionary.HydrogenMass * 10,
                        MassDiffDictionary.NitrogenMass,
                        MassDiffDictionary.OxygenMass * 5,
                        MassDiffDictionary.PhosphorusMass,
            }.Sum();

            double C2H8NO4P = new[] {
                        MassDiffDictionary.CarbonMass * 2,
                        MassDiffDictionary.HydrogenMass * 8,
                        MassDiffDictionary.NitrogenMass,
                        MassDiffDictionary.OxygenMass * 4,
                        MassDiffDictionary.PhosphorusMass,
            }.Sum();

            var spectrum = msScanProp.Spectrum;
            var candidates = new List<LipidMolecule>();

            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive) 
            {
                if (adduct.AdductIonName == "[M+H]+") 
                {
                    if (sn1Carbon < 10 || sn2Carbon < 10) return null;
                    if (sn1Double > 6 || sn2Double > 6) return null;
                    // seek -141.019094261 (C2H8NO4P) and PE diagnostic
                    var threshold = 2.5;
                    var diagnosticMz = theoreticalMz - C2H8NO4P;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (!isClassIonFound) return null;

                    // from here, acyl level annotation is executed.
                    var sn1 = LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) - Electron;
                    var sn2 = LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) - Electron;

                    var query = new List<SpectrumPeak> {
                                new() { Mass = sn1, Intensity = 0.1 },
                                new() { Mass = sn2, Intensity = 0.1 }
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2) {
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PE", LbmClass.PE, sn1Carbon, sn1Double,
                                    sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PE", LbmClass.PE, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
                else if (adduct.AdductIonName == "[M+Na]+") 
                {
                    // seek -141.019094261 (C2H8NO4P)
                    var threshold = 3.0;
                    var diagnosticMz = theoreticalMz - C2H8NO4P;
                    // seek - 43.042199 (C2H5N)
                    //var diagnosticMz2 = theoreticalMz - C2H5N;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_C + Na, threshold);
                    var isClassIonFound3 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_O + Na, threshold);
                    if (!isClassIonFound || !isClassIonFound2 || !isClassIonFound3) return null;

                    // from here, acyl level annotation is executed.
                    for (int sn1CarbonNum = 6; sn1CarbonNum <= totalCarbon; sn1CarbonNum++) {
                        for (int sn1DoubleNum = 0; sn1DoubleNum <= totalDoubleBond; sn1DoubleNum++) {
                            var sn2CarbonNum = totalCarbon - sn1CarbonNum;
                            var sn2DoubleNum = totalDoubleBond - sn1DoubleNum;
                            if (sn2DoubleNum >= 7) continue;

                            var nl_SN1 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1CarbonNum, sn1DoubleNum) - MassDiffDictionary.HydrogenMass * 2;
                            var nl_SN1_H2O = nl_SN1 - MassDiffDictionary.OxygenMass;
                            var nl_SN2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2CarbonNum, sn2DoubleNum) - MassDiffDictionary.HydrogenMass * 2;
                            var nl_SN2_H2O = nl_SN2 - MassDiffDictionary.OxygenMass;


                            var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = nl_SN1, Intensity = 0.1 },
                                new SpectrumPeak() { Mass = nl_SN1_H2O, Intensity = 0.1 },
                                new SpectrumPeak() { Mass = nl_SN2, Intensity = 0.1},
                                new SpectrumPeak() { Mass = nl_SN2_H2O, Intensity = 0.1 }
                            };
                            var foundCount = 0;
                            var averageIntensity = 0.0;
                            LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                            if (foundCount >= 2)  {
                                var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PE", LbmClass.PE, sn1Carbon, sn1Double,
                                    sn2Carbon, sn2Double, averageIntensity);
                                candidates.Add(molecule);
                            }
                        }
                    }
                }
            } else {
                if (adduct.AdductIonName == "[M-H]-") 
                {
                    // seek C5H11NO5P-
                    var threshold = 5.0;
                    var diagnosticMz = 196.03803;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    var threshold2 = 5.0;
                    var diagnosticMz2 = 152.995833871; // seek C3H6O5P- (maybe LNAPE)
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    if (isClassIon2Found == true) return null;

                    var sn1 = LipidMsmsCharacterizationUtility.fattyacidProductIon(sn1Carbon, sn1Double);
                    var sn2 = LipidMsmsCharacterizationUtility.fattyacidProductIon(sn2Carbon, sn2Double);

                    var query = new List<SpectrumPeak> {
                            new() { Mass = sn1, Intensity = 1.0 },
                            new() { Mass = sn2, Intensity = 1.0 }
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2) {
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PE", LbmClass.PE, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                }
            }
            if (candidates == null || candidates.Count == 0)
                return null;

            return LipidMsmsCharacterizationUtility.returnAnnotationResult("PE", LbmClass.PE, "", theoreticalMz, adduct,
                totalCarbon, totalDoubleBond, 0, candidates, 2);
        }

        public static LipidMolecule JudgeIfPhosphatidylglycerol(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond,
           int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double, AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            var candidates = new List<LipidMolecule>();
            double Gly_C = new[] {
                MassDiffDictionary.CarbonMass * 6,
                MassDiffDictionary.HydrogenMass * 13,
                MassDiffDictionary.OxygenMass * 6,
                MassDiffDictionary.PhosphorusMass,
            }.Sum();

            double Gly_O = new[] {
                MassDiffDictionary.CarbonMass * 5,
                MassDiffDictionary.HydrogenMass * 11,
                MassDiffDictionary.OxygenMass * 7,
                MassDiffDictionary.PhosphorusMass,
            }.Sum();

            double C3H9O6P = new[] {
                MassDiffDictionary.CarbonMass * 3,
                MassDiffDictionary.HydrogenMass * 9,
                MassDiffDictionary.OxygenMass * 6,
                MassDiffDictionary.PhosphorusMass,
            }.Sum();

            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            {
                if (adduct.AdductIonName == "[M+NH4]+")
                {
                    // seek -189.040227 (C3H8O6P+NH4)
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - C3H9O6P - NH3;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    // PG diagnostic
                    if (!isClassIonFound) return null;

                    // from here, acyl level annotation is executed.
                    var nl_SN1 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) + MassDiffDictionary.HydrogenMass;
                    var nl_SN1_H2O = nl_SN1 - H2O;
                    var nl_SN2 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) + MassDiffDictionary.HydrogenMass;
                    var nl_SN2_H2O = nl_SN2 - H2O;

                    if (LipidMsmsCharacterizationUtility.isFragment1GreaterThanFragment2(spectrum, ms2Tolerance, nl_SN1, diagnosticMz) &&
                        LipidMsmsCharacterizationUtility.isFragment1GreaterThanFragment2(spectrum, ms2Tolerance, nl_SN2, diagnosticMz)) {
                        // meaning high possibility that the spectrum belongs to BMP
                        return null;
                    }

                    var query = new List<SpectrumPeak> {
                                new() { Mass = nl_SN1, Intensity = 1.0 },
                                new() { Mass = nl_SN1_H2O, Intensity = 1.0 },
                                new() { Mass = nl_SN2, Intensity = 1.0 },
                                new() { Mass = nl_SN2_H2O, Intensity = 1.0 }
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount >= 2) {
                        //check Sn2 position spectrum
                        var nl_SN1_H2O_CH2 = nl_SN1_H2O - CH2 + MassDiffDictionary.HydrogenMass;
                        var nl_SN2_H2O_CH2 = nl_SN1_H2O - CH2 + MassDiffDictionary.HydrogenMass;
                        var queryCh2 = new List<SpectrumPeak> {
                                new() { Mass = nl_SN1_H2O_CH2, Intensity = 1.0 },
                                new() { Mass = nl_SN2_H2O_CH2, Intensity = 1.0 },
                        };

                        var foundCountSN2 = 0;
                        var averageIntensitySN2 = 0.0;
                        LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, queryCh2, ms2Tolerance, out foundCountSN2, out averageIntensitySN2);

                        // average intensity < 30 is nessesarry to distinguish it from BMP?
                        if (foundCountSN2 > 0) {
                            var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PG", LbmClass.PG, sn1Carbon, sn1Double,
                                    sn2Carbon, sn2Double, averageIntensity);
                            candidates.Add(molecule);
                        }
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PG", LbmClass.PG, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek header loss
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - C3H9O6P;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    // PG diagnostic
                    var threshold2 = 5.0;
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_C + Na, threshold2);
                    var isClassIonFound3 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_O + Na, threshold2);
                    if (!isClassIonFound || !isClassIonFound2 || !isClassIonFound3) return null;

                    // from here, acyl level annotation is executed.
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PG", LbmClass.PG, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            } else {
                if (adduct.AdductIonName == "[M-H]-")
                {
                    // seek C3H6O5P-
                    var threshold = 0.01;
                    var diagnosticMz = 152.995833871;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);

                    // from here, acyl level annotation is executed.
                    var sn1 = LipidMsmsCharacterizationUtility.fattyacidProductIon(sn1Carbon, sn1Double);
                    var sn2 = LipidMsmsCharacterizationUtility.fattyacidProductIon(sn2Carbon, sn2Double);

                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = sn1, Intensity = 1.0 },
                                new SpectrumPeak() { Mass = sn2, Intensity = 1.0 }
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2) {
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PG", LbmClass.PG, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PG", LbmClass.PG, "", theoreticalMz, adduct,
                      totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfBismonoacylglycerophosphate(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond,
           int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double, AdductIon adduct)
        {
            double C3H9O6P = new[] {
                MassDiffDictionary.CarbonMass * 3,
                MassDiffDictionary.HydrogenMass * 9,
                MassDiffDictionary.OxygenMass * 6,
                MassDiffDictionary.PhosphorusMass,
            }.Sum();
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            {
                if (adduct.AdductIonName == "[M+NH4]+")
                {
                    // seek -189.040227 (C3H8O6P+NH4)
                    var threshold = 1.0;
                    var diagnosticMz = theoreticalMz - C3H9O6P - NH3;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    var nl_SN1 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) + MassDiffDictionary.HydrogenMass;
                    var nl_SN2 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) + MassDiffDictionary.HydrogenMass;

                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = nl_SN1, Intensity = 10 },
                                new SpectrumPeak() { Mass = nl_SN2, Intensity = 10 }
                    };

                    if (LipidMsmsCharacterizationUtility.isFragment1GreaterThanFragment2(spectrum, ms2Tolerance, diagnosticMz, nl_SN1) &&
                        LipidMsmsCharacterizationUtility.isFragment1GreaterThanFragment2(spectrum, ms2Tolerance, diagnosticMz, nl_SN2)) {
                        // meaning high possibility that the spectrum belongs to PG
                        return null;
                    }

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2) {
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("BMP", LbmClass.BMP, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                    if (candidates.Count == 0) return null;
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("BMP", LbmClass.BMP, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfPhosphatidylserine(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond,
           int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double, AdductIon adduct)
        {
            double Gly_C = new[] {
                MassDiffDictionary.CarbonMass * 6,
                MassDiffDictionary.HydrogenMass * 12,
                MassDiffDictionary.NitrogenMass,
                MassDiffDictionary.OxygenMass * 6,
                MassDiffDictionary.PhosphorusMass,
            }.Sum();

            double Gly_O = new[] {
                MassDiffDictionary.CarbonMass * 5,
                MassDiffDictionary.HydrogenMass * 10,
                MassDiffDictionary.NitrogenMass,
                MassDiffDictionary.OxygenMass * 7,
                MassDiffDictionary.PhosphorusMass,
            }.Sum();

            double C3H8NO6P = new[] {
                    MassDiffDictionary.CarbonMass * 3,
                    MassDiffDictionary.HydrogenMass * 8,
                    MassDiffDictionary.NitrogenMass,
                    MassDiffDictionary.OxygenMass * 6,
                    MassDiffDictionary.PhosphorusMass,
            }.Sum();
            var spectrum = msScanProp.Spectrum;
            var candidates = new List<LipidMolecule>();
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            {
                if (adduct.AdductIonName == "[M+H]+")
                {
                    // seek -185.008927 (C3H8NO6P)
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - C3H8NO6P;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    // PS diagnostic
                    if (!isClassIonFound) return null;

                    // from here, acyl level annotation is executed.
                    var nl_SN1 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) + MassDiffDictionary.HydrogenMass;
                    var nl_SN1_H2O = nl_SN1 - H2O;
                    var nl_SN2 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) + MassDiffDictionary.HydrogenMass;
                    var nl_NS2_H2O = nl_SN2 - H2O;

                    var query = new List<SpectrumPeak> {
                                new() { Mass = nl_SN1, Intensity = 1.0 },
                                new() { Mass = nl_SN1_H2O, Intensity = 1.0 },
                                new() { Mass = nl_SN2, Intensity = 1.0 },
                                new() { Mass = nl_NS2_H2O, Intensity = 1.0 }
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount >= 2) {
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PS", LbmClass.PS, sn1Carbon, sn1Double,
                                    sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PS", LbmClass.PS, "", theoreticalMz, adduct,
                      totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek -185.008927 (C3H8NO6P)
                    var threshold = 3.0;
                    var diagnosticMz = theoreticalMz - C3H8NO6P;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    // PS diagnostic
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_C + Na, threshold);
                    var isClassIonFound3 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_O + Na, threshold);
                    if (!isClassIonFound || !isClassIonFound2 || !isClassIonFound3) return null;

                    // from here, acyl level annotation is executed.
                    for (int sn1CarbonNum = 6; sn1CarbonNum <= totalCarbon; sn1CarbonNum++) {
                        for (int sn1DoubleNum = 0; sn1DoubleNum <= totalDoubleBond; sn1DoubleNum++) {
                            var sn2CarbonNum = totalCarbon - sn1CarbonNum;
                            var sn2DoubleNum = totalDoubleBond - sn1DoubleNum;
                            if (sn2DoubleNum >= 7) continue;

                            var nl_SN1 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1CarbonNum, sn1DoubleNum) + MassDiffDictionary.HydrogenMass;
                            var nl_SN1_H2O = nl_SN1 - MassDiffDictionary.OxygenMass - MassDiffDictionary.HydrogenMass;
                            var nl_SN2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2CarbonNum, sn2DoubleNum) + MassDiffDictionary.HydrogenMass;
                            var nl_SN2_H2O = nl_SN2 - MassDiffDictionary.OxygenMass - MassDiffDictionary.HydrogenMass;

                            var query = new List<SpectrumPeak> {
                                new() { Mass = nl_SN1, Intensity = 0.1 },
                                new() { Mass = nl_SN1_H2O, Intensity = 0.1 },
                                new() { Mass = nl_SN2, Intensity = 0.1},
                                new() { Mass = nl_SN2_H2O, Intensity = 0.1 }
                            };
                            var foundCount = 0;
                            var averageIntensity = 0.0;
                            LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                            if (foundCount >= 2) {
                                var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PS", LbmClass.PS, sn1Carbon, sn1Double,
                                    sn2Carbon, sn2Double, averageIntensity);
                                candidates.Add(molecule);
                            }
                        }
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PS", LbmClass.PS, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            } else {
                if (adduct.AdductIonName == "[M-H]-")
                {
                    // seek C3H5NO2 loss
                    var threshold = 3.0;
                    var diagnosticMz = theoreticalMz - 87.032029;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    var threshold2 = 30;
                    var diagnosticMz2 = theoreticalMz - 63.008491; // [M+C2H3N(ACN)+Na-2H]- adduct of PG [M-H]- 
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    if (isClassIonFound2) return null;

                    // from here, acyl level annotation is executed.
                    var sn1 = LipidMsmsCharacterizationUtility.fattyacidProductIon(sn1Carbon, sn1Double);
                    var sn2 = LipidMsmsCharacterizationUtility.fattyacidProductIon(sn2Carbon, sn2Double);

                    var query = new List<SpectrumPeak> {
                                new() { Mass = sn1, Intensity = 1.0 },
                                new() { Mass = sn2, Intensity = 1.0 }
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2) {
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PS", LbmClass.PS, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PS", LbmClass.PS, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfPhosphatidylinositol(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond,
           int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double, AdductIon adduct) {
            double Gly_C = new[] {
                MassDiffDictionary.CarbonMass * 9,
                MassDiffDictionary.HydrogenMass * 17,
                MassDiffDictionary.OxygenMass * 9,
                MassDiffDictionary.PhosphorusMass,
            }.Sum();

            double Gly_O = new[] {
                MassDiffDictionary.CarbonMass * 8,
                MassDiffDictionary.HydrogenMass * 15,
                MassDiffDictionary.OxygenMass * 10,
                MassDiffDictionary.PhosphorusMass,
            }.Sum();

            double C6H13O9P = new[] {
                MassDiffDictionary.CarbonMass * 6,
                MassDiffDictionary.HydrogenMass * 13,
                MassDiffDictionary.OxygenMass * 9,
                MassDiffDictionary.PhosphorusMass,
            }.Sum();
            
            double C6H10O5 = new[] {
                MassDiffDictionary.CarbonMass * 6,
                MassDiffDictionary.HydrogenMass * 10,
                MassDiffDictionary.OxygenMass * 5,
            }.Sum();
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            {
                if (adduct.AdductIonName == "[M+NH4]+")
                {
                    // seek -277.056272 (C6H12O9P+NH4)
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - C6H13O9P - NH3;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    // PI diagnostic
                    if (!isClassIonFound) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    for (int sn1CarbonNum = 6; sn1CarbonNum <= totalCarbon; sn1CarbonNum++)
                    {
                        for (int sn1DoubleNum = 0; sn1DoubleNum <= totalDoubleBond; sn1DoubleNum++)
                        {
                            var sn2CarbonNum = totalCarbon - sn1CarbonNum;
                            var sn2DoubleNum = totalDoubleBond - sn1DoubleNum;

                            var nl_SN1 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1CarbonNum, sn1DoubleNum) + MassDiffDictionary.HydrogenMass;
                            var nl_SN1_H2O = nl_SN1 - H2O;
                            var nl_SN2 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2CarbonNum, sn2DoubleNum) + MassDiffDictionary.HydrogenMass;
                            var nl_SN2_H2O = nl_SN2 - H2O;

                            var query = new List<SpectrumPeak> {
                                new() { Mass = nl_SN1, Intensity = 0.1 },
                                new() { Mass = nl_SN1_H2O, Intensity = 0.1 },
                                new() { Mass = nl_SN2, Intensity = 0.1 },
                                new() { Mass = nl_SN2_H2O, Intensity = 0.1 }
                            };

                            var foundCount = 0;
                            var averageIntensity = 0.0;
                            LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                            if (foundCount >= 2)
                            {
                                var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PI", LbmClass.PI, sn1CarbonNum, sn1DoubleNum,
                                sn2CarbonNum, sn2DoubleNum, averageIntensity);
                                candidates.Add(molecule);
                            }
                        }
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PI", LbmClass.PI, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek -C6H10O5
                    var threshold = 5.0;
                    var diagnosticMz = theoreticalMz - C6H10O5;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    // PI diagnostic
                    var threshold2 = 5.0;
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_C + Na, threshold2);
                    var isClassIonFound3 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_O + Na, threshold2);
                    if (!isClassIonFound || !isClassIonFound2 || !isClassIonFound3) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    for (int sn1CarbonNum = 6; sn1CarbonNum <= totalCarbon; sn1CarbonNum++)
                    {
                        for (int sn1DoubleNum = 0; sn1DoubleNum <= totalDoubleBond; sn1DoubleNum++)
                        {
                            var sn2CarbonNum = totalCarbon - sn1CarbonNum;
                            var sn2DoubleNum = totalDoubleBond - sn1DoubleNum;

                            var nl_SN1 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1CarbonNum, sn1DoubleNum) + MassDiffDictionary.HydrogenMass;
                            var nl_SN1_H2O = nl_SN1 - H2O;
                            var nl_SN2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2CarbonNum, sn2DoubleNum) + MassDiffDictionary.HydrogenMass;
                            var nl_SN2_H2O = nl_SN2 - H2O;

                            var query = new List<SpectrumPeak> {
                                new() { Mass = nl_SN1, Intensity = 0.1 },
                                new() { Mass = nl_SN1_H2O, Intensity = 0.1 },
                                new() { Mass = nl_SN2, Intensity = 0.1 },
                                new() { Mass = nl_SN2_H2O, Intensity = 0.1 }
                            };

                            var foundCount = 0;
                            var averageIntensity = 0.0;
                            LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                            if (foundCount >= 2)
                            {
                                var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PI", LbmClass.PI, sn1CarbonNum, sn1DoubleNum,
                                    sn2CarbonNum, sn2DoubleNum, averageIntensity);
                                candidates.Add(molecule);
                            }
                        }
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PI", LbmClass.PI, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            } else {
                if (adduct.AdductIonName == "[M-H]-")
                {
                    // seek 241.01188(C6H10O8P-) and 297.037548(C9H14O9P-)
                    var threshold = 0.01;
                    var diagnosticMz1 = 241.01188 + Electron;
                    var diagnosticMz2 = 297.037548 + Electron;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold);
                    if (!isClassIon1Found && !isClassIon2Found) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    var sn1 = LipidMsmsCharacterizationUtility.fattyacidProductIon(sn1Carbon, sn1Double);
                    var sn2 = LipidMsmsCharacterizationUtility.fattyacidProductIon(sn2Carbon, sn2Double);

                    var query = new List<SpectrumPeak> {
                        new() { Mass = sn1, Intensity = 0.01 },
                        new() { Mass = sn2, Intensity = 0.01 }
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2) {
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PI", LbmClass.PI, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PI", LbmClass.PI, "", theoreticalMz, adduct,
                      totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfLysopc(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond, 
            int snCarbon, int snDoubleBond, AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (snCarbon > totalCarbon) snCarbon = totalCarbon;
            if (snDoubleBond > totalDoubleBond) snDoubleBond = totalDoubleBond;
            if (adduct.IonMode == IonMode.Positive)
            {
                if (adduct.AdductIonName == "[M+H]+")
                {
                    if (totalCarbon > 28) return null; //  currently carbon > 28 is recognized as EtherPC
                                                       // seek 184.07332 (C5H15NO4P)
                    var threshold = 2.0;
                    var diagnosticMz = 184.07332;
                    var diagnosticMz2 = 104.106990;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIon1Found != true) return null;
                    // reject Na+ adduct
                    var diagnosticMz3 = theoreticalMz - 59.0735;
                    var isNaTypicalFound1 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz3, 10.0);
                    if (isNaTypicalFound1) {
                        return null;
                    }

                    // for eieio
                    var PEHeaderLoss = theoreticalMz - 141.019094261 + MassDiffDictionary.ProtonMass;
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, PEHeaderLoss, 3.0);
                    if (isClassIonFound2 && LipidMsmsCharacterizationUtility.isFragment1GreaterThanFragment2(spectrum, ms2Tolerance, PEHeaderLoss, diagnosticMz)) {
                        return null;
                    }

                    var candidates = new List<LipidMolecule>();
                    var chainSuffix = "";
                    var diagnosticMzExist = 0.0;
                    var diagnosticMzIntensity = 0.0;
                    var diagnosticMzExist2 = 0.0;
                    var diagnosticMzIntensity2 = 0.0;

                    for (int i = 0; i < spectrum.Count; i++) {
                        var mz = spectrum[i].Mass;
                        var intensity = spectrum[i].Intensity;

                        if (intensity > threshold && Math.Abs(mz - diagnosticMz) < ms2Tolerance) {
                            diagnosticMzExist = mz;
                            diagnosticMzIntensity = intensity;
                        }
                        else if (intensity > threshold && Math.Abs(mz - diagnosticMz2) < ms2Tolerance) {
                            diagnosticMzExist2 = mz;
                            diagnosticMzIntensity2 = intensity;
                        }
                    };

                    if (diagnosticMzIntensity2 / diagnosticMzIntensity > 0.3) {
                        chainSuffix = "/0:0";
                    }

                    var score = 0.0;
                    if (totalCarbon < 30) score = score + 1.0;
                    var molecule = LipidMsmsCharacterizationUtility.getSingleacylchainwithsuffixMoleculeObjAsLevel2("LPC", LbmClass.LPC, totalCarbon, totalDoubleBond,
                    score, chainSuffix);
                    candidates.Add(molecule);

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPC", LbmClass.LPC, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);

                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    if (totalCarbon > 28) return null; //  currently carbon > 28 is recognized as EtherPC
                                                       // seek PreCursor - 59 (C3H9N)
                    var threshold = 3.0;
                    var diagnosticMz = theoreticalMz - 59.072951;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    var candidates = new List<LipidMolecule>();
                    var score = 0.0;
                    if (totalCarbon < 30) score = score + 1.0;
                    var molecule = LipidMsmsCharacterizationUtility.getSingleacylchainMoleculeObjAsLevel2("LPC", LbmClass.LPC, totalCarbon, totalDoubleBond,
                    score);
                    candidates.Add(molecule);

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPC", LbmClass.LPC, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            } else {
                if (adduct.AdductIonName == "[M+FA-H]-" || adduct.AdductIonName == "[M+Hac-H]-" ||
                    adduct.AdductIonName == "[M+HCOO]-" || adduct.AdductIonName == "[M+CH3COO]-")
                {
                    if (totalCarbon > 28) return null; //  currently carbon > 28 is recognized as EtherPC

                    // seek [M-CH3]-
                    var threshold = 3.0;
                    var diagnosticMz = adduct.AdductIonName == "[M+CH3COO]-" || adduct.AdductIonName == "[M+Hac-H]-" ?
                        theoreticalMz - 74.036779433 : theoreticalMz - 60.021129369;
                    var diagnosticMz2 = LipidMsmsCharacterizationUtility.fattyacidProductIon(totalCarbon, totalDoubleBond);
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold);
                    if (isClassIon1Found != true || isClassIon2Found != true) return null;
                                        
                    var candidates = new List<LipidMolecule>();
                    //var score = 0.0;
                    //if (totalCarbon < 30) score = score + 1.0;
                    //var molecule = LipidMsmsCharacterizationUtility.getSingleacylchainMoleculeObjAsLevel2("LPC", LbmClass.LPC, totalCarbon, totalDoubleBond,
                    //score);
                    //candidates.Add(molecule);

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPC", LbmClass.LPC, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
                else if (adduct.AdductIonName == "[M+HCO3]-") 
                {
                    if (totalCarbon > 28) return null; //  currently carbon > 28 is recognized as EtherPC

                    // //"[M-H]- -C3H9N"
                    var threshold = 5.0;
                    var diagnosticMz = theoreticalMz - (12 + MassDiffDictionary.OxygenMass * 3 + MassDiffDictionary.HydrogenMass) - MassDiffDictionary.ProtonMass - (12 * 3 + MassDiffDictionary.HydrogenMass * 9 + MassDiffDictionary.NitrogenMass);
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    var candidates = new List<LipidMolecule>();

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPC", LbmClass.LPC, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            return null;
        }
        public static LipidMolecule JudgeIfEtherlysopc(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond,
            int minSnCarbon, int maxSnCarbon, int minSnDoubleBond, int maxSnDoubleBond, AdductIon adduct) {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (maxSnCarbon > totalCarbon) maxSnCarbon = totalCarbon;
            if (maxSnDoubleBond > totalDoubleBond) maxSnDoubleBond = totalDoubleBond;
            if (adduct.IonMode == IonMode.Positive)
            {
                if (adduct.AdductIonName == "[M+H]+")
                {
                    // seek 184.07332 (C5H15NO4P), 104.10699 (C5H12N+), 124.99982 (C2H5O4P + H+)
                    var threshold = 20.0;
                    var diagnosticMz1 = 184.07332;
                    var diagnosticMz2 = 104.106990;
                    var diagnosticMz3 = 124.99982;

                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold);
                    if (!isClassIon1Found) return null;

                    threshold = 1.0;
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold);
                    var isClassIon3Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz3, threshold);
                    if (isClassIon2Found != true && isClassIon3Found != true) return null;
                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPC", LbmClass.EtherLPC, "e", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            } else {
                if (adduct.AdductIonName == "[M+FA-H]-" || adduct.AdductIonName == "[M+Hac-H]-" ||
                    adduct.AdductIonName == "[M+HCOO]-" || adduct.AdductIonName == "[M+CH3COO]-")
                {
                    // seek [M-CH3]-
                    var threshold = 10.0;
                    var diagnosticMz = adduct.AdductIonName == "[M+CH3COO]-" || adduct.AdductIonName == "[M+Hac-H]-" ?
                        theoreticalMz - 74.036779433 : theoreticalMz - 60.021129369;
                    var diagnosticMz2 = diagnosticMz - 89.08461258; //[M-CH3 -C4H11NO]-
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold);
                    if (isClassIon1Found != true || isClassIon2Found != true) return null;

                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPC", LbmClass.EtherLPC, "e", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);

                }
                else if (adduct.AdductIonName == "[M+HCO3]-")
                {
                    if (totalCarbon > 28) return null; //  currently carbon > 28 is recognized as EtherPC

                    // //"[M-H]- -C3H9N"
                    var threshold = 5.0;
                    var diagnosticMz = theoreticalMz - (12 + MassDiffDictionary.OxygenMass * 3 + MassDiffDictionary.HydrogenMass) - MassDiffDictionary.ProtonMass - (12 * 3 + MassDiffDictionary.HydrogenMass * 9 + MassDiffDictionary.NitrogenMass);
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    var candidates = new List<LipidMolecule>();

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPC", LbmClass.EtherLPC, "e", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfLysope(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond, 
            int sn1Carbon, int sn1Double, AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            {
                if (adduct.AdductIonName == "[M+H]+")
                {
                    if (totalCarbon > 28) return null; //  currently carbon > 28 is recognized as EtherPE
                    double Gly_C = new[] {
                        MassDiffDictionary.CarbonMass * 5,
                        MassDiffDictionary.HydrogenMass * 12,
                        MassDiffDictionary.NitrogenMass,
                        MassDiffDictionary.OxygenMass * 4,
                        MassDiffDictionary.PhosphorusMass,
                        Proton
                    }.Sum();

                    double Gly_O = new[] {
                        MassDiffDictionary.CarbonMass * 4,
                        MassDiffDictionary.HydrogenMass * 10,
                        MassDiffDictionary.NitrogenMass,
                        MassDiffDictionary.OxygenMass * 5,
                        MassDiffDictionary.PhosphorusMass,
                        Proton
                    }.Sum();
                    double C2H8NO4P = new[]
                    {
                        MassDiffDictionary.CarbonMass * 2,
                        MassDiffDictionary.HydrogenMass * 8,
                        MassDiffDictionary.NitrogenMass,
                        MassDiffDictionary.OxygenMass * 4,
                        MassDiffDictionary.PhosphorusMass,
                    }.Sum();
                    // seek -141.019094261 (C2H8NO4P)
                    var threshold = 0.5;
                    var diagnosticMz = theoreticalMz - C2H8NO4P;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    //var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_C, threshold);
                    //var isClassIonFound3 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_O, threshold);
                    if (!isClassIonFound) return null;

                    var sn1alkyl = (MassDiffDictionary.CarbonMass * sn1Carbon)
                                        + (MassDiffDictionary.HydrogenMass * ((sn1Carbon * 2) - (sn1Double * 2) + 1));//sn1(ether)

                    var NL_sn1 = diagnosticMz - sn1alkyl + Proton;
                    var sn1_rearrange = sn1alkyl + MassDiffDictionary.HydrogenMass * 2 + 139.00290;//sn1(ether) + C2H5NO4P + proton 

                    // reject EtherPE 
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, NL_sn1, threshold);
                    var isClassIon3Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, sn1_rearrange, threshold);
                    if (isClassIon2Found == true || isClassIon3Found == true) return null;

                    var candidates = new List<LipidMolecule>();
                    if (totalCarbon > 30) {
                        return LipidMsmsCharacterizationUtility.returnAnnotationResult("PE", LbmClass.EtherPE, "e", theoreticalMz, adduct,
                           totalCarbon, totalDoubleBond + 1, 0, candidates, 1);
                    } else {
                        return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPE", LbmClass.LPE, "", theoreticalMz, adduct,
                           totalCarbon, totalDoubleBond, 0, candidates, 1);
                    }
                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek PreCursor -141(C2H8NO4P)
                    var threshold = 3.0;
                    var diagnosticMz = theoreticalMz - 141.019094;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // reject EtherPE 
                    var sn1alkyl = (MassDiffDictionary.CarbonMass * sn1Carbon)
                                        + (MassDiffDictionary.HydrogenMass * ((sn1Carbon * 2) - (sn1Double * 2) + 1));//sn1(ether)

                    var NL_sn1 = diagnosticMz - sn1alkyl + Proton;
                    var sn1_rearrange = sn1alkyl + 139.00290 + MassDiffDictionary.HydrogenMass * 2;//sn1(ether) + C2H5NO4P + proton 

                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, NL_sn1, threshold);
                    var isClassIon3Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, sn1_rearrange, threshold);
                    if (isClassIon2Found == true || isClassIon3Found == true) return null;

                    var candidates = new List<LipidMolecule>();
                    if (totalCarbon > 30) {
                        return LipidMsmsCharacterizationUtility.returnAnnotationResult("PE", LbmClass.EtherPE, "e", theoreticalMz, adduct,
                           totalCarbon, totalDoubleBond + 1, 0, candidates, 2);
                    } else {
                        return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPE", LbmClass.LPE, "", theoreticalMz, adduct,
                           totalCarbon, totalDoubleBond, 0, candidates, 1);
                    }
                }
            } else {
                if (adduct.AdductIonName == "[M+FA-H]-" || adduct.AdductIonName == "[M+Hac-H]-" ||
                    adduct.AdductIonName == "[M+HCOO]-" || adduct.AdductIonName == "[M+CH3COO]-")
                {
                    // seek [M-CH3]-
                    var threshold = 10.0;
                    var diagnosticMz = adduct.AdductIonName == "[M+CH3COO]-" || adduct.AdductIonName == "[M+Hac-H]-" ?
                        theoreticalMz - 74.036779433 : theoreticalMz - 60.021129369;
                    var diagnosticMz2 = diagnosticMz - 89.08461258; //[M-CH3 -C4H11NO]-
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold);
                    if (isClassIon1Found != true || isClassIon2Found != true) return null;

                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPC", LbmClass.EtherLPC, "e", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);

                }
                else if (adduct.AdductIonName == "[M+HCO3]-")
                {
                    if (totalCarbon > 28) return null; //  currently carbon > 28 is recognized as EtherPC

                    //"[M-H]- -C3H9N"
                    var threshold = 5.0;
                    var diagnosticMz = theoreticalMz - (12 + MassDiffDictionary.OxygenMass * 3 + MassDiffDictionary.HydrogenMass) - MassDiffDictionary.ProtonMass - (12 * 3 + MassDiffDictionary.HydrogenMass * 9 + MassDiffDictionary.NitrogenMass);
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPC", LbmClass.EtherLPC, "e", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            return null;
        }
        public static LipidMolecule JudgeIfLysopg(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond,
            int snCarbon, int snDoubleBond, AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Negative)
            {
                if (adduct.AdductIonName == "[M-H]-")
                {
                    // seek C3H6O5P-
                    var diagnosticMz1 = 152.99583;
                    var threshold1 = 1.0;
                    var diagnosticMz2 = LipidMsmsCharacterizationUtility.fattyacidProductIon(totalCarbon, totalDoubleBond); // seek [FA-H]-
                    var threshold2 = 1.0;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold1);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    if (isClassIon1Found != true || isClassIon2Found != true) return null;

                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPG", LbmClass.LPG, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            else if (adduct.IonMode == IonMode.Positive)
            {
                if (adduct.AdductIonName == "[M+H]+" || adduct.AdductIonName == "[M+NH4]+")
                {
                    // seek Header loss (MG+ + chain Acyl) 
                    var threshold = 1.0;
                    var diagnosticMz = LipidMsmsCharacterizationUtility.acylCainMass(snCarbon, snDoubleBond) + (12 * 3 + MassDiffDictionary.HydrogenMass * 5 + MassDiffDictionary.OxygenMass * 2) + MassDiffDictionary.ProtonMass;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPG", LbmClass.LPG, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfLysopi(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond, 
            int snCarbon, int snDoubleBond, AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Negative)
            {
                if (adduct.AdductIonName == "[M-H]-")
                {
                    var diagnosticMz1 = 241.0118806 + Electron;  // seek C3H6O5P-
                    var threshold1 = 1.0;
                    var diagnosticMz2 = 315.048656; // seek C9H16O10P-
                    var threshold2 = 1.0;
                    var diagnosticMz3 = LipidMsmsCharacterizationUtility.fattyacidProductIon(totalCarbon, totalDoubleBond); // seek [FA-H]-
                    var threshold3 = 10.0;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold1);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    var isClassIon3Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz3, threshold3);
                    if (isClassIon1Found != true || isClassIon2Found != true || isClassIon3Found != true) return null;
                    
                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPI", LbmClass.LPI, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            else if (adduct.IonMode == IonMode.Positive)
            {
                if (adduct.AdductIonName == "[M+H]+" || adduct.AdductIonName == "[M+NH4]+")
                {
                    // seek Header loss (MG+ + chain Acyl) 
                    var threshold = 1.0;
                    var diagnosticMz = LipidMsmsCharacterizationUtility.acylCainMass(snCarbon, snDoubleBond) + (12 * 3 + MassDiffDictionary.HydrogenMass * 5 + MassDiffDictionary.OxygenMass * 2) + MassDiffDictionary.ProtonMass;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPI", LbmClass.LPI, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            return null;
        }
        public static LipidMolecule JudgeIfLysops(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond, 
            int snCarbon, int snDoubleBond, AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Negative)
            { 
                if (adduct.AdductIonName == "[M-H]-")
                {                    
                    var diagnosticMz1 = 152.99583;  // seek C3H6O5P-
                    var threshold1 = 5.0;
                    var diagnosticMz2 = theoreticalMz - 87.032029; // seek -C3H6NO2-H
                    var threshold2 = 1.0;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold1);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    if (isClassIon1Found != true || isClassIon2Found != true) return null;

                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPS", LbmClass.LPS, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            else if (adduct.IonMode == IonMode.Positive)
            { 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    // seek Header loss (MG+ + chain Acyl) 
                    var threshold = 1.0;
                    var diagnosticMz = LipidMsmsCharacterizationUtility.acylCainMass(snCarbon, snDoubleBond) + (12 * 3 + MassDiffDictionary.HydrogenMass * 5 + MassDiffDictionary.OxygenMass * 2) + MassDiffDictionary.ProtonMass;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPS", LbmClass.LPS, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfSphingomyelin(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond, 
            int sphCarbon, int acylCarbon, int sphDouble, int acylDouble, AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            double C5H14NO4P = new[] {
                        MassDiffDictionary.CarbonMass * 5,
                        MassDiffDictionary.HydrogenMass * 14,
                        MassDiffDictionary.NitrogenMass,
                        MassDiffDictionary.OxygenMass * 4,
                        MassDiffDictionary.PhosphorusMass,
            }.Sum();
            double C2H2N = 12 * 2 + MassDiffDictionary.HydrogenMass * 2 + MassDiffDictionary.NitrogenMass;
            if (adduct.IonMode == IonMode.Positive)
            { 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    // seek 184.07332 (C5H15NO4P+)
                    var threshold = 5;
                    var diagnosticMz = C5H14NO4P + Proton;
                    var diagnosticMz1 = C5H14NO4P + Proton + 12 * 2 + MassDiffDictionary.HydrogenMass * 3 + MassDiffDictionary.NitrogenMass;
                    var diagnosticMz2 = C5H14NO4P + Proton + 12 * 3 + MassDiffDictionary.HydrogenMass * 3 + MassDiffDictionary.NitrogenMass + MassDiffDictionary.OxygenMass;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    var isClassIonFound1 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, 1.0);
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, 0.5);
                    if (!isClassIonFound || !isClassIonFound1 || !isClassIonFound2) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    if (sphCarbon <= 13) return null;
                    if (sphCarbon == 16 && sphDouble >= 3) return null;
                    if (acylCarbon < 8) return null;

                    var diagnosChain1 = LipidMsmsCharacterizationUtility.acylCainMass(acylCarbon, acylDouble) + C2H2N + Proton;
                    var diagnosChain2 = diagnosChain1 + C5H14NO4P + Proton - MassDiffDictionary.HydrogenMass;

                    var query = new List<SpectrumPeak> {
                        new() { Mass = diagnosChain1, Intensity = 0.1 },
                        new() { Mass = diagnosChain2, Intensity = 1.0 },
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount >= 1)
                    { // the diagnostic acyl ion must be observed for level 2 annotation
                        var molecule = LipidMsmsCharacterizationUtility.getCeramideMoleculeObjAsLevel2("SM", LbmClass.SM, "d", sphCarbon, sphDouble,
                            acylCarbon, acylDouble, averageIntensity);
                        candidates.Add(molecule);
                    } else {
                        return null;
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("SM", LbmClass.SM, "d", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek -59.0735 [M-C3H9N+Na]+
                    var threshold = 20.0;
                    var diagnosticMz = theoreticalMz - 59.0735;
                    // seek C5H15NO4P + Na+
                    var threshold2 = 30.0;
                    var diagnosticMz2 = C5H14NO4P + Na;

                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    if (isClassIonFound == false || isClassIon2Found == false) return null;

                    var candidates = new List<LipidMolecule>();
                    // from here, acyl level annotation is executed.
                    for (int sphCarbonNum = 6; sphCarbonNum <= totalCarbon; sphCarbonNum++) {
                        for (int sphDoubleNum = 0; sphDoubleNum <= totalDoubleBond; sphDoubleNum++) {
                            var acylCarbonNum = totalCarbon - sphCarbonNum;
                            var acylDoubleNum = totalDoubleBond - sphDoubleNum;
                            if (acylDoubleNum >= 7) continue;

                            var diagnosChain1 = LipidMsmsCharacterizationUtility.acylCainMass(acylCarbonNum, acylDoubleNum) + C2H2N + MassDiffDictionary.HydrogenMass + Na;
                            var diagnosChain2 = diagnosChain1 + C5H14NO4P - MassDiffDictionary.HydrogenMass;

                            var query = new List<SpectrumPeak> {
                                new() { Mass = diagnosChain1, Intensity = 20.0 },
                                new() { Mass = diagnosChain2, Intensity = 20.0 },
                            };
                            var foundCount = 0;
                            var averageIntensity = 0.0;
                            LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);
                            if (foundCount >= 1) {
                                var molecule = LipidMsmsCharacterizationUtility.getCeramideMoleculeObjAsLevel2("SM", LbmClass.SM, "d", sphCarbonNum, sphDoubleNum,
                                    acylCarbonNum, acylDoubleNum, averageIntensity);
                                candidates.Add(molecule);
                            }
                        }
                    }
                    if (candidates == null || candidates.Count == 0) return null;

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("SM", LbmClass.SM, "d", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            } else {
                if (adduct.AdductIonName == "[M+FA-H]-" || adduct.AdductIonName == "[M+Hac-H]-" ||
                    adduct.AdductIonName == "[M+HCOO]-" || adduct.AdductIonName == "[M+CH3COO]-")
                {
                    // seek [M-CH3]-
                    var threshold1 = 50.0;
                    var threshold2 = 0.01;
                    var diagnosticMz1 = adduct.AdductIonName == "[M+CH3COO]-" || adduct.AdductIonName == "[M+Hac-H]-" ?
                        theoreticalMz - 74.036779433 : theoreticalMz - 60.021129369;
                    var diagnosticMz2 = 168.042572 + Electron;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold1);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    if (isClassIon1Found != true || isClassIon2Found != true) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    if (acylCarbon < 8) return null;

                    var sphFragment = diagnosticMz1 - LipidMsmsCharacterizationUtility.acylCainMass(acylCarbon, acylDouble) + MassDiffDictionary.HydrogenMass;
                    var query = new List<SpectrumPeak> { new() { Mass = sphFragment, Intensity = 0.01 } };
                    
                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 1)
                    { // the diagnostic acyl ion must be observed for level 2 annotation
                        var molecule = LipidMsmsCharacterizationUtility.getCeramideMoleculeObjAsLevel2("SM", LbmClass.SM, "d", sphCarbon, sphDouble,
                            acylCarbon, acylDouble, averageIntensity);
                        candidates.Add(molecule);
                    }

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("SM", LbmClass.SM, "d", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
                else if (adduct.AdductIonName == "[M+HCO3]-")
                {
                    // //"[M-H]- -C3H9N"
                    var threshold1 = 5.0;
                    var threshold2 = 0.5;
                    var diagnosticMz1 = theoreticalMz - (12 + MassDiffDictionary.OxygenMass * 3 + MassDiffDictionary.HydrogenMass) - MassDiffDictionary.ProtonMass - (12 * 3 + MassDiffDictionary.HydrogenMass * 9 + MassDiffDictionary.NitrogenMass);
                    var diagnosticMz2 = 182.057671;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold1);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    if (isClassIon1Found != true || isClassIon2Found != true) return null;
                    var candidates = new List<LipidMolecule>();
                    if (acylCarbon < 8) return null;

                    var sphFragment = diagnosticMz1 - LipidMsmsCharacterizationUtility.SphingoChainMass(sphCarbon, sphDouble) + 12 * 2 + MassDiffDictionary.HydrogenMass * 4 + MassDiffDictionary.NitrogenMass + MassDiffDictionary.OxygenMass;
                    var query = new List<SpectrumPeak> { new() { Mass = sphFragment, Intensity = 0.01 } };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 1)
                    { // the diagnostic acyl ion must be observed for level 2 annotation
                        var molecule = LipidMsmsCharacterizationUtility.getCeramideMoleculeObjAsLevel2("SM", LbmClass.SM, "d", sphCarbon, sphDouble,
                            acylCarbon, acylDouble, averageIntensity);
                        candidates.Add(molecule);
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("SM", LbmClass.SM, "d", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfAcylsm(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond, 
            int sphCarbon, int extCarbon, int sphDouble, int extDouble, AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;

            if (adduct.IonMode == IonMode.Positive)
            {
                if (adduct.AdductIonName == "[M+H]+")
                {
                    var threshold1 = 1.0;
                    var diagnosticMz1 = 184.073;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold1);
                    if (isClassIon1Found != true) return null;
                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    var acylCarbon = totalCarbon - sphCarbon - extCarbon;
                    var acylDouble = totalDoubleBond - sphDouble - extDouble;

                    var extAcylloss = theoreticalMz - LipidMsmsCharacterizationUtility.fattyacidProductIon(extCarbon, extDouble) - MassDiffDictionary.HydrogenMass + Electron;
                    var query = new List<SpectrumPeak> { new() { Mass = extAcylloss, Intensity = 1 } };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 1) { 
                        var molecule = LipidMsmsCharacterizationUtility.getAsmMoleculeObjAsLevel2_0("SM", LbmClass.ASM, "d", sphCarbon + acylCarbon,
                               sphDouble + acylDouble, extCarbon, extDouble, averageIntensity);
                        candidates.Add(molecule);
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("SM", LbmClass.ASM, "d", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 3);
                }
            } else { 
                if (adduct.AdductIonName == "[M+FA-H]-" || adduct.AdductIonName == "[M+Hac-H]-" ||
                    adduct.AdductIonName == "[M+HCOO]-" || adduct.AdductIonName == "[M+CH3COO]-")
                {
                    // seek [M-CH3]-
                    var threshold1 = 50.0;
                    var diagnosticMz1 = adduct.AdductIonName == "[M+CH3COO]-" || adduct.AdductIonName == "[M+Hac-H]-" ?
                        theoreticalMz - 74.036779433 : theoreticalMz - 60.021129369;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold1);
                    if (isClassIon1Found != true) return null;

                    if (adduct.AdductIonName == "[M+CH3COO]-" || adduct.AdductIonName == "[M+Hac-H]-") {
                        var diagnosticMz2 = theoreticalMz - 60.021129369;
                        var threshold2 = 50.0;
                        var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                        if (isClassIon2Found) return null;
                    }

                    var diagnosticMz3 = 168.0431;
                    var threshold3 = 0.01;
                    var isClassIon3Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz3, threshold3);

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    var acylCarbon = totalCarbon - sphCarbon - extCarbon;
                    var acylDouble = totalDoubleBond - sphDouble - extDouble;

                    var extAcylloss = diagnosticMz1 - LipidMsmsCharacterizationUtility.fattyacidProductIon(extCarbon, extDouble) - MassDiffDictionary.HydrogenMass;  // 
                    var extFa = LipidMsmsCharacterizationUtility.fattyacidProductIon(extCarbon, extDouble);

                    var query = new List<SpectrumPeak> {
                        new() { Mass = extAcylloss, Intensity = 0.01 },
                        new() { Mass = extFa, Intensity = 0.01 },
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2) {
                        var acylamide = LipidMsmsCharacterizationUtility.fattyacidProductIon(acylCarbon, acylDouble) + MassDiffDictionary.NitrogenMass + MassDiffDictionary.HydrogenMass + Electron + MassDiffDictionary.OxygenMass;
                        var query2 = new List<SpectrumPeak> { new() { Mass = acylamide, Intensity = 0.01 } };
                        var foundCount2 = 0;
                        var averageIntensity2 = 0.0;
                        LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query2, ms2Tolerance, out foundCount2, out averageIntensity2);

                        if (foundCount2 == 1) {
                            var molecule = LipidMsmsCharacterizationUtility.getAsmMoleculeObjAsLevel2("SM", LbmClass.ASM, "d", sphCarbon, sphDouble,
                            acylCarbon, acylDouble, extCarbon, extDouble, averageIntensity);
                            candidates.Add(molecule);
                        } else {
                            var molecule = LipidMsmsCharacterizationUtility.getAsmMoleculeObjAsLevel2_0("SM", LbmClass.ASM, "d", sphCarbon + acylCarbon,
                                sphDouble + acylDouble, extCarbon, extDouble, averageIntensity);
                            candidates.Add(molecule);
                        }
                    }
                    if (candidates.Count == 0 && !isClassIon3Found) return null;

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("SM", LbmClass.ASM, "d", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 3);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfAcylcarnitine(IMSScanProperty msScanProp, double ms2Tolerance, float theoreticalMz,
            int totalCarbon, int totalDoubleBond, AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { 
                if (adduct.AdductIonName == "[M+H]+" || adduct.AdductIonName == "[M]+")
                {
                    // seek 85.028405821 (C4H5O2+)
                    var threshold = 1.0;
                    var diagnosticMz = 85.028405821;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);

                    // seek -CHO2
                    var diagnosticMz1 = theoreticalMz - (MassDiffDictionary.CarbonMass + MassDiffDictionary.HydrogenMass + MassDiffDictionary.OxygenMass * 2);
                    var isClassIonFound1 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold);

                    // seek Acyl loss
                    var acyl = LipidMsmsCharacterizationUtility.acylCainMass(totalCarbon, totalDoubleBond);
                    var diagnosticMz2 = theoreticalMz - acyl + MassDiffDictionary.ProtonMass;
                    var threshold2 = 0.01;
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);

                    // seek 144.1019 (Acyl and H2O loss) // not found at PasefOn case
                    var diagnosticMz3 = diagnosticMz2 - H2O;
                    var threshold3 = 0.01;
                    var isClassIonFound3 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz3, threshold3);

                    //if (!isClassIonFound && !isClassIonFound1) return null;
                    if (!isClassIonFound2 && !isClassIonFound3) return null;

                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("CAR", LbmClass.CAR, string.Empty, theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfEtherpe(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond, 
            int Sn1Carbon, int Sn2Carbon, int Sn1Double, int Sn2Double, AdductIon adduct)
        {
            if (totalCarbon <= 28) return null; // currently carbon <= 28 is recognized as Lyso PE
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            {
                if (adduct.AdductIonName == "[M+H]+")
                {
                    double Gly_C = new[] {
                        MassDiffDictionary.CarbonMass * 5,
                        MassDiffDictionary.HydrogenMass * 12,
                        MassDiffDictionary.NitrogenMass,
                        MassDiffDictionary.OxygenMass * 4,
                        MassDiffDictionary.PhosphorusMass,
                        Proton
                    }.Sum();

                    double Gly_O = new[] {
                        MassDiffDictionary.CarbonMass * 4,
                        MassDiffDictionary.HydrogenMass * 10,
                        MassDiffDictionary.NitrogenMass,
                        MassDiffDictionary.OxygenMass * 5,
                        MassDiffDictionary.PhosphorusMass,
                        Proton
                    }.Sum();

                    double C2H8NO4P = new[] {
                        MassDiffDictionary.CarbonMass * 2,
                        MassDiffDictionary.HydrogenMass * 8,
                        MassDiffDictionary.NitrogenMass,
                        MassDiffDictionary.OxygenMass * 4,
                        MassDiffDictionary.PhosphorusMass,
                    }.Sum();
                    // seek -141.019094261 (C2H8NO4P)
                    var threshold = 1.0;
                    var diagnosticMz = theoreticalMz - C2H8NO4P;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (!isClassIonFound) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    if (Sn1Double >= 5) return null;
                    var sn1alkyl = (MassDiffDictionary.CarbonMass * Sn1Carbon)
                                + (MassDiffDictionary.HydrogenMass * ((Sn1Carbon * 2) - (Sn1Double * 2) + 1));//(Ether chain)

                    var NL_sn1 = theoreticalMz - sn1alkyl - MassDiffDictionary.OxygenMass;
                    var NL_sn2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(Sn2Carbon, Sn2Double) - H2O;
                    var sn1_rearrange = sn1alkyl + C2H8NO4P; //sn1(carbon chain) + C2H8NO4P 

                    var query = new List<SpectrumPeak> {
                        new() { Mass = NL_sn1, Intensity = 1.0 },
                        new() { Mass = NL_sn2, Intensity = 1.0 },
                        new() { Mass = sn1_rearrange, Intensity = 1.0 },
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount >= 2) {
                        var etherSuffix = "e";
                        var sn1Double2 = Sn1Double;
                        var EtherString = "O-" + Sn1Carbon + ":" + Sn1Double;
                        var EsterString = Sn2Carbon + ":" + Sn2Double;
                        if (LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, sn1_rearrange, 5.0)) {
                            sn1Double2 = Sn1Double - 1;
                            etherSuffix = "p";
                            EtherString = "P-" + Sn1Carbon + ":" + sn1Double2; 
                        };

                        var nl_SN1_H2O_CH2 = NL_sn1 - CH2;
                        var nl_SN2_H2O_CH2 = NL_sn2 - CH2 + MassDiffDictionary.HydrogenMass * 2;
                        var queryCh2 = new List<SpectrumPeak> {
                            new() { Mass = nl_SN1_H2O_CH2, Intensity = 2.0 },
                            new() { Mass = nl_SN2_H2O_CH2, Intensity = 2.0 },
                        };

                        var foundCountSN2 = 0;
                        var averageIntensitySN2 = 0.0;
                        LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, queryCh2, ms2Tolerance, out foundCountSN2, out averageIntensitySN2);
                        if (foundCountSN2 > 0) {
                            var molecule = LipidMsmsCharacterizationUtility.getEtherPhospholipidMoleculeObjAsLevel2("PE", LbmClass.EtherPE, Sn1Carbon, sn1Double2,
                                Sn2Carbon, Sn2Double, averageIntensity, etherSuffix);
                            candidates.Add(molecule);
                        }
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PE", LbmClass.EtherPE, "e", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            } else {
                if (adduct.AdductIonName == "[M-H]-")
                {
                    // seek C5H11NO5P-
                    var threshold = 5.0;
                    var diagnosticMz = 196.03803;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    if (Sn1Carbon >= 24 && Sn1Double >= 5) return null;

                    var sn2 = LipidMsmsCharacterizationUtility.fattyacidProductIon(Sn2Carbon, Sn2Double);
                    var NL_sn2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(Sn2Carbon, Sn2Double) + MassDiffDictionary.HydrogenMass;

                    var query = new List<SpectrumPeak> {
                        new() { Mass = sn2, Intensity = 10.0 },
                        new() { Mass = NL_sn2, Intensity = 0.1 }
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2) { 
                        var molecule = LipidMsmsCharacterizationUtility.getEtherPhospholipidMoleculeObjAsLevel2("PE", LbmClass.EtherPE, Sn1Carbon, Sn1Double,
                            Sn2Carbon, Sn2Double, averageIntensity, "e");
                        candidates.Add(molecule);
                    }

                    if (candidates.Count == 0) return null;
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PE", LbmClass.EtherPE, "e", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfShexcer(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond, 
        int sphCarbon, int acylCarbon, int sphDouble, int acylDouble, AdductIon adduct, int totalOxidized)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { 
                if (adduct.AdductIonName == "[M+H]+" || adduct.AdductIonName == "[M+NH4]+")
                {
                    var diagnosticMz = adduct.AdductIonName == "[M+NH4]+" ?
                        theoreticalMz - (MassDiffDictionary.NitrogenMass + MassDiffDictionary.HydrogenMass * 3) : theoreticalMz;
                    // seek [M-SO3-H2O+H]+
                    var threshold = 1.0;
                    var diagnosticMz1 = diagnosticMz - MassDiffDictionary.SulfurMass - 3 * MassDiffDictionary.OxygenMass - H2O - Electron;
                    // seek [M-H2O-SO3-C6H10O5+H]+
                    var threshold2 = 1.0;
                    var diagnosticMz2 = diagnosticMz1 - 162.052833;

                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    if (isClassIon1Found == false && isClassIon2Found == false) return null;

                    var hydrogenString = "d";
                    var sphOxidized = 2;
                    var acylOxidized = totalOxidized - sphOxidized;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    if (acylOxidized == 0) {
                        var sph1 = diagnosticMz2 - LipidMsmsCharacterizationUtility.acylCainMass(acylCarbon, acylDouble) + MassDiffDictionary.HydrogenMass;
                        var sph2 = sph1 - H2O;
                        var sph3 = sph2 - 12;

                        var query = new List<SpectrumPeak> {
                            new() { Mass = sph1, Intensity = 1 },
                            new() { Mass = sph2, Intensity = 1 },
                            new() { Mass = sph3, Intensity = 1 }
                        };

                        var foundCount = 0;
                        var averageIntensity = 0.0;
                        LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                        if (foundCount >= 1) { 
                            var molecule = LipidMsmsCharacterizationUtility.getCeramideMoleculeObjAsLevel2("SHexCer", LbmClass.SHexCer, hydrogenString, sphCarbon, sphDouble,
                                acylCarbon, acylDouble, averageIntensity);
                            candidates.Add(molecule);
                        }
                        return LipidMsmsCharacterizationUtility.returnAnnotationResult("SHexCer", LbmClass.SHexCer, hydrogenString, theoreticalMz, adduct,
                            totalCarbon, totalDoubleBond, acylOxidized, candidates, 2);
                    } else {   
                        // case of acyl chain have extra OH
                        var sph1 = diagnosticMz2 - LipidMsmsCharacterizationUtility.acylCainMass(acylCarbon, acylDouble) + MassDiffDictionary.HydrogenMass - MassDiffDictionary.OxygenMass * acylOxidized;
                        var sph2 = sph1 - H2O;
                        var sph3 = sph2 - 12;
                        
                        var query = new List<SpectrumPeak> {
                            new() { Mass = sph1, Intensity = 1 },
                            new() { Mass = sph2, Intensity = 1 },
                            new() { Mass = sph3, Intensity = 1 }
                        };

                        var foundCount = 0;
                        var averageIntensity = 0.0;
                        LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                        if (foundCount >= 1) { 
                            var molecule = LipidMsmsCharacterizationUtility.getCeramideoxMoleculeObjAsLevel2("SHexCer", LbmClass.SHexCer, hydrogenString, sphCarbon, sphDouble,
                                acylCarbon, acylDouble, acylOxidized, averageIntensity);
                            candidates.Add(molecule);
                        }
                        return LipidMsmsCharacterizationUtility.returnAnnotationResult("SHexCer", LbmClass.SHexCer, hydrogenString, theoreticalMz, adduct,
                            totalCarbon, totalDoubleBond, acylOxidized, candidates, 2);
                    }
                }
            } else {
                if (adduct.AdductIonName == "[M-H]-")
                {
                    // seek [H2SO4-H]-
                    var threshold = 0.1;
                    var diagnosticMz = 96.960103266;

                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound != true) return null;

                    // from here, acyl level annotation is executed.
                    //   may be not found fragment to define sphingo and acyl chain
                    var candidates = new List<LipidMolecule>();
                    var hydrogenString = "d";
                    var sphOxidized = 2;
                    var acylOxidized = totalOxidized - sphOxidized;

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("SHexCer", LbmClass.SHexCer, hydrogenString, theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, acylOxidized, candidates, 2);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfPicermide(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond, 
            int sphCarbon, int acylCarbon, int sphDouble, int acylDouble, AdductIon adduct, int totalOxdyzed)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.AdductIonName == "[M+H]+")
            {
                // seek Header loss (-C6H13O9P)
                var threshold = 1.0;
                var diagnosticMz = theoreticalMz - 260.029722;
                var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                if (isClassIonFound != true) return null;
                // to reject SHexCer
                // seek [M-SO3-H2O+H]+
                var threshold1 = 1.0;
                var diagnosticMz1 = theoreticalMz - MassDiffDictionary.SulfurMass - 3 * MassDiffDictionary.OxygenMass - H2O - Electron;
                var isClassIonFound1 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold1);
                if (isClassIonFound1 == true) return null;

                // maybe no fragment to define sphingo and acyl chain
                var candidates = new List<LipidMolecule>();

                var hydrogenString = "d";
                var sphOxidized = 2;
                var acylOxidyzed = totalOxdyzed - sphOxidized;
                return LipidMsmsCharacterizationUtility.returnAnnotationResult("PI-Cer", LbmClass.PI_Cer, hydrogenString, theoreticalMz, adduct,
                    totalCarbon, totalDoubleBond, acylOxidyzed, candidates, 2);
            } else {
                if (adduct.AdductIonName == "[M-H]-")
                {
                    // seek C6H10O8P-
                    var threshold = 5.0;
                    var diagnosticMz = 241.01188;
                    // seek Inositol loss (-C6H10O5)
                    var threshold2 = 1.0;
                    var diagnosticMz2 = theoreticalMz - 162.05282;

                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    if (isClassIon1Found != true || isClassIon2Found != true) return null;

                    var hydrogenString = "d";
                    var sphOxidized = 2;
                    var acylOxidyzed = totalOxdyzed - sphOxidized;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    if (acylOxidyzed == 0) {
                        var acylLoss = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(acylCarbon, acylDouble) + Proton;

                        var query = new List<SpectrumPeak> { new() { Mass = acylLoss, Intensity = 0.1 } };

                        var foundCount = 0;
                        var averageIntensity = 0.0;
                        LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);
                        var hydrogenString1 = "d";

                        if (foundCount == 1) { 
                            var molecule = LipidMsmsCharacterizationUtility.getCeramideMoleculeObjAsLevel2("PI-Cer", LbmClass.PI_Cer, hydrogenString1, sphCarbon, sphDouble,
                                acylCarbon, acylDouble, averageIntensity);
                            candidates.Add(molecule);
                        }

                        return LipidMsmsCharacterizationUtility.returnAnnotationResult("PI-Cer", LbmClass.PI_Cer, hydrogenString, theoreticalMz, adduct,
                            totalCarbon, totalDoubleBond, acylOxidyzed, candidates, 2);
                    } else { 
                        // oxidyzed PI-Cer case
                        var acylOxidized = totalOxdyzed - sphOxidized;

                        var acylLoss = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(acylCarbon, acylDouble) - acylOxidized * MassDiffDictionary.OxygenMass + Proton;

                        var query = new List<SpectrumPeak> { new() { Mass = acylLoss, Intensity = 0.1 } };

                        var foundCount = 0;
                        var averageIntensity = 0.0;
                        LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                        if (foundCount == 1) { 
                            var molecule = LipidMsmsCharacterizationUtility.getCeramideoxMoleculeObjAsLevel2("PI-Cer", LbmClass.PI_Cer, hydrogenString, sphCarbon, sphDouble,
                                acylCarbon, acylDouble, acylOxidized, averageIntensity);
                            candidates.Add(molecule);
                        }
                        return LipidMsmsCharacterizationUtility.returnAnnotationResult("PI-Cer", LbmClass.PI_Cer, hydrogenString, theoreticalMz, adduct,
                            totalCarbon, totalDoubleBond, acylOxidyzed, candidates, 2);
                    }
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfSphingosine(IMSScanProperty msScanProp, double ms2Tolerance, float theoreticalMz,
            int totalCarbon, int totalDoubleBond, AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    // seek -H2O 
                    var threshold1 = 10.0;
                    var diagnosticMz1 = theoreticalMz - H2O;
                    // seek -2H2O 
                    var threshold2 = 1.0;
                    var diagnosticMz2 = diagnosticMz1 - H2O;
                    // seek -H2O -CH2O
                    var threshold3 = 1.0;
                    var diagnosticMz3 = diagnosticMz2 - 12;

                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold1);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    var isClassIon3Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz3, threshold3);
                    var trueCount = 0;
                    if (isClassIon1Found) trueCount++;
                    if (isClassIon2Found) trueCount++;
                    if (isClassIon3Found) trueCount++;
                    if (trueCount < 3) return null;

                    var candidates = new List<LipidMolecule>();

                    var sphOHCount = 2;

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("SPB", LbmClass.Sph, string.Empty, theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, sphOHCount, candidates, 1);
                }

            }
            return null;
        }

        public static LipidMolecule JudgeIfNAcylGlyOxFa(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, 
            int totalCarbon, int totalDoubleBond, int totalOxidized, AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    //  seek 76.039305(gly+)
                    //var threshold = 10.0;
                    //var diagnosticMz = 76.039305;
                    //var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    //if (isClassIonFound == false) return null;

                    var threshold1 = 1.0;
                    var diagnosticMz1 = theoreticalMz - (12 + MassDiffDictionary.OxygenMass * 2 + MassDiffDictionary.HydrogenMass);
                    var isClassIonFound1 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold1);
                    if (isClassIonFound1 == false) return null;
                    var candidates = new List<LipidMolecule>();

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("NAGly", LbmClass.NAGly, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, totalOxidized, candidates, 1);
                }
            }
            else if (adduct.AdductIonName == "[M-H]-")
            {
                //  74.024(C2H5NO2-H- Gly)
                var threshold = 10.0;
                var diagnosticMz = 74.024752;

                var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                if (isClassIonFound == false) return null;

                var candidates = new List<LipidMolecule>();

                return LipidMsmsCharacterizationUtility.returnAnnotationResult("NAGly", LbmClass.NAGly, "", theoreticalMz, adduct,
                    totalCarbon, totalDoubleBond, totalOxidized, candidates, 1);
            }
            return null;
        }

        public static LipidMolecule JudgeIfDag(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond, 
            int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double, AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (totalCarbon > 52) return null; // currently, very large DAG is excluded.
            if (adduct.IonMode == IonMode.Positive)
            { 
                if (adduct.AdductIonName == "[M+NH4]+")
                {
                    // seek -17.026549 (NH3)
                    var threshold = 1.0;
                    var diagnosticMz = theoreticalMz - 17.026549;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    if (sn2Double <= 7) return null;

                    var nl_SN1 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) - H2O + MassDiffDictionary.HydrogenMass;
                    var nl_SN2 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) - H2O + MassDiffDictionary.HydrogenMass;
                                        
                    var query = new List<SpectrumPeak> {
                        new() { Mass = nl_SN1, Intensity = 1 },
                        new() { Mass = nl_SN2, Intensity = 1 },
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);
                    if (foundCount == 2) {
                        var molecule = LipidMsmsCharacterizationUtility.getDiacylglycerolMoleculeObjAsLevel2("DG", LbmClass.DG, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                    if (candidates == null || candidates.Count == 0)
                        return null;

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("DG", LbmClass.DG, string.Empty, theoreticalMz, adduct, 
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // from here, acyl level annotation is executed.
                    var diagnosticMz = theoreticalMz;
                    var candidates = new List<LipidMolecule>();
                    if (sn2Double >= 7) return null;
                    for (int sn1CarbonNum = 6; sn1CarbonNum <= totalCarbon; sn1CarbonNum++) {
                        for (int sn1DoubleNum = 0; sn1DoubleNum <= totalDoubleBond; sn1DoubleNum++) {
                            var sn2CarbonNum = totalCarbon - sn1CarbonNum;
                            var sn2DoubleNum = totalDoubleBond - sn1DoubleNum;
                            if (sn2DoubleNum >= 7) continue;

                            var nl_SN1 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1CarbonNum, sn1DoubleNum) - H2O + MassDiffDictionary.HydrogenMass * 2;
                            var nl_SN2 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2CarbonNum, sn2DoubleNum) - H2O + MassDiffDictionary.HydrogenMass * 2;

                            var query = new List<SpectrumPeak> {
                                new() { Mass = nl_SN1, Intensity = 5 },
                                new() { Mass = nl_SN2, Intensity = 5 },
                            };
                            var foundCount = 0;
                            var averageIntensity = 0.0;
                            LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);
                            if (foundCount == 2) {
                                var molecule = LipidMsmsCharacterizationUtility.getDiacylglycerolMoleculeObjAsLevel2("DG", LbmClass.DG, sn1CarbonNum, sn1DoubleNum,
                                    sn2CarbonNum, sn2DoubleNum, averageIntensity);
                                candidates.Add(molecule);
                            }
                        }
                    }
                    if (candidates == null || candidates.Count == 0)
                        return null;
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("DG", LbmClass.DG, string.Empty, theoreticalMz, adduct,
                         totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfEtherpc(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond, 
            int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double, AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    double Gly_C = new[] {
                        MassDiffDictionary.CarbonMass * 8,
                        MassDiffDictionary.HydrogenMass * 18,
                        MassDiffDictionary.NitrogenMass,
                        MassDiffDictionary.OxygenMass * 4,
                        MassDiffDictionary.PhosphorusMass,
                        Proton
                    }.Sum();
                    
                    double Gly_O = new[] {
                        MassDiffDictionary.CarbonMass * 7,
                        MassDiffDictionary.HydrogenMass * 16,
                        MassDiffDictionary.NitrogenMass,
                        MassDiffDictionary.OxygenMass * 5,
                        MassDiffDictionary.PhosphorusMass,
                        Proton
                    }.Sum();
                    
                    double C5H14NO4P = new[] {
                        MassDiffDictionary.CarbonMass * 5,
                        MassDiffDictionary.HydrogenMass * 14,
                        MassDiffDictionary.NitrogenMass,
                        MassDiffDictionary.OxygenMass * 4,
                        MassDiffDictionary.PhosphorusMass,
                        Proton
                    }.Sum();
                    var threshold = 1.0;
                    var isClassIonFound1 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, C5H14NO4P, threshold);
                    if (!isClassIonFound1) return null;
                    // reject Na+ adduct
                    var diagnosticMz3 = theoreticalMz - 59.0735;
                    var isNaTypicalFound1 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz3, 10.0);
                    if (isNaTypicalFound1) {
                        return null;
                    }

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    var AcylLoss = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) - H2O;
                    var EtherLoss = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) - MassDiffDictionary.HydrogenMass * 2;

                    var query = new List<SpectrumPeak> {
                        new() { Mass = AcylLoss, Intensity = 0.1 },
                        new() { Mass = EtherLoss, Intensity = 1.0 },
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);
                    if (foundCount >= 1) {
                        var EtherString = "O-" + sn1Carbon + ":" + sn1Double;
                        var AcylString = sn2Carbon + ":" + sn2Double;

                        var molecule = LipidMsmsCharacterizationUtility.getEtherPhospholipidMoleculeObjAsLevel2("PC", LbmClass.EtherPC, sn1Carbon, sn1Double,
                                    sn2Carbon, sn2Double, averageIntensity, "e");
                        candidates.Add(molecule);
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PC", LbmClass.EtherPC, "e", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek PreCursor - 59 (C3H9N)
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - 59.072951;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;  // must or not?

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    //var averageIntensity = 0.0;
                    //var molecule = LipidMsmsCharacterizationUtility.getSingleacylchainwithsuffixMoleculeObjAsLevel2("PC", LbmClass.EtherPC, totalCarbon,
                    //                totalDoubleBond, averageIntensity, "e");
                    //candidates.Add(molecule);

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PC", LbmClass.EtherPC, "e", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            } else {
                if (adduct.AdductIonName == "[M+FA-H]-" || adduct.AdductIonName == "[M+Hac-H]-" ||
                    adduct.AdductIonName == "[M+HCOO]-" || adduct.AdductIonName == "[M+CH3COO]-")
                {
                    // seek [M-CH3]-
                    var threshold = 10.0;
                    var diagnosticMz = adduct.AdductIonName == "[M+CH3COO]-" || adduct.AdductIonName == "[M+Hac-H]-" ?
                        theoreticalMz - 74.036779433 : theoreticalMz - 60.021129369;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (!isClassIonFound) return null;

                    if (adduct.AdductIonName == "[M+CH3COO]-" || adduct.AdductIonName == "[M+Hac-H]-") {
                        var formateMz = theoreticalMz - 60.021129369;
                        var threshold2 = 30;
                        var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, formateMz, threshold2);
                        if (isClassIonFound2) return null;
                    }

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    var sn1 = LipidMsmsCharacterizationUtility.fattyacidProductIon(sn1Carbon, sn1Double);
                    var sn2 = LipidMsmsCharacterizationUtility.fattyacidProductIon(sn2Carbon, sn2Double);
                    var NL_sn2 = diagnosticMz - sn2 - Proton;
                    var NL_sn2AndWater = NL_sn2 + 18.0105642;

                    var query = new List<SpectrumPeak> {
                        new() { Mass = sn2, Intensity = 30.0 },
                        //new() { Mass = NL_sn2, Intensity = 0.1 },
                        //new() { Mass = NL_sn2AndWater, Intensity = 0.1 }
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 1) {
                        var molecule = LipidMsmsCharacterizationUtility.getEtherPhospholipidMoleculeObjAsLevel2("PC", LbmClass.EtherPC, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity, "e");
                        candidates.Add(molecule);
                    }
                    if (candidates.Count == 0) return null;

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PC", LbmClass.EtherPC, "e", theoreticalMz, adduct,
                           totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
                else if (adduct.AdductIonName == "[M+HCO3]-")
                {
                    // //"[M-H]- -C3H9N"
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - MassDiffDictionary.ProtonMass - (12 * 4 + MassDiffDictionary.HydrogenMass * 10 + MassDiffDictionary.NitrogenMass + MassDiffDictionary.OxygenMass * 3);
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    var candidates = new List<LipidMolecule>();
                    var sn1 = LipidMsmsCharacterizationUtility.fattyacidProductIon(sn1Carbon, sn1Double);
                    var sn2 = LipidMsmsCharacterizationUtility.fattyacidProductIon(sn2Carbon, sn2Double);
                    var NL_sn2 = diagnosticMz - sn2 - Proton;
                    var NL_sn2AndWater = NL_sn2 + 18.0105642;

                    var query = new List<SpectrumPeak> {
                        new() { Mass = sn2, Intensity = 30.0 },
                        //new() { Mass = NL_sn2, Intensity = 0.1 },
                        //new() { Mass = NL_sn2AndWater, Intensity = 0.1 }
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 1) { 
                        var molecule = LipidMsmsCharacterizationUtility.getEtherPhospholipidMoleculeObjAsLevel2("PC", LbmClass.EtherPC, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity, "e");
                        candidates.Add(molecule);
                    }
                    if (candidates.Count == 0) return null;
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PC", LbmClass.EtherPC, "e", theoreticalMz, adduct,
                           totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfCholesterylEster(IMSScanProperty msScanProp, double ms2Tolerance, float theoreticalMz,
            int totalCarbon, int totalDoubleBond, AdductIon adduct)
        {
            double skelton = new[] {
                MassDiffDictionary.CarbonMass * 27,
                MassDiffDictionary.HydrogenMass * 46,
                MassDiffDictionary.OxygenMass * 1,
            }.Sum();
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { 
                if (adduct.AdductIonName == "[M+NH4]+")
                {
                    // seek 369.3515778691 (C27H45+)+ MassDiffDictionary.HydrogenMass*7
                    var threshold = 20.0;
                    var diagnosticMz = skelton - H2O + Proton;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;
                    if (totalCarbon >= 41 && totalDoubleBond >= 4) return null;

                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("CE", LbmClass.CE, string.Empty, theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek 368.3515778691 (C27H44)+ MassDiffDictionary.HydrogenMass*7
                    var threshold = 10.0;
                    var diagnosticMz = skelton - H2O;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("CE", LbmClass.CE, string.Empty, theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfSphingomyelinPhyto(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond, 
            int minSphCarbon, int maxSphCarbon, int minSphDoubleBond, int maxSphDoubleBond, AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (maxSphCarbon > totalCarbon) maxSphCarbon = totalCarbon;
            if (maxSphDoubleBond > totalDoubleBond) maxSphDoubleBond = totalDoubleBond;
            if (adduct.IonMode == IonMode.Positive)
            {
                if (adduct.AdductIonName == "[M+H]+")
                {
                    // seek 184.07332 (C5H15NO4P)
                    var threshold = 30.0;
                    var diagnosticMz = 184.07332;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("SM", LbmClass.SM, "t", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            } else {
                if (adduct.AdductIonName == "[M+FA-H]-" || adduct.AdductIonName == "[M+Hac-H]-" ||
                    adduct.AdductIonName == "[M+HCOO]-" || adduct.AdductIonName == "[M+CH3COO]-")
                {
                    // seek [M-CH3]-
                    var threshold1 = 50.0;
                    var threshold2 = 0.01;
                    var diagnosticMz1 = adduct.AdductIonName == "[M+CH3COO]-" || adduct.AdductIonName == "[M+Hac-H]-" ?
                        theoreticalMz - 74.036779433 : theoreticalMz - 60.021129369;
                    var diagnosticMz2 = 168.042572 + Electron;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold1);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    if (isClassIon1Found != true || isClassIon2Found != true) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("SM", LbmClass.SM, "t", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfPhosphatidylcholineD5(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond, 
            int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double, AdductIon adduct)
        {
            double Gly_C = new[] {
                MassDiffDictionary.CarbonMass * 8,
                MassDiffDictionary.HydrogenMass * 13,
                MassDiffDictionary.NitrogenMass,
                MassDiffDictionary.OxygenMass * 4,
                MassDiffDictionary.PhosphorusMass,
                MassDiffDictionary.Hydrogen2Mass * 5,
            }.Sum();

            double Gly_O = new[] {
                MassDiffDictionary.CarbonMass * 7,
                MassDiffDictionary.HydrogenMass * 13,
                MassDiffDictionary.NitrogenMass,
                MassDiffDictionary.OxygenMass * 5,
                MassDiffDictionary.PhosphorusMass,
                MassDiffDictionary.Hydrogen2Mass * 3,
            }.Sum();

            double C5H14NO4P = new[] {
                MassDiffDictionary.CarbonMass * 5,
                MassDiffDictionary.HydrogenMass * 14,
                MassDiffDictionary.NitrogenMass,
                MassDiffDictionary.OxygenMass * 4,
                MassDiffDictionary.PhosphorusMass,
            }.Sum();

            double C3H9N = new[] {
                MassDiffDictionary.CarbonMass * 3,
                MassDiffDictionary.HydrogenMass * 9,
                MassDiffDictionary.NitrogenMass,
            }.Sum();

            var spectrum = msScanProp.Spectrum;
            var candidates = new List<LipidMolecule>();

            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            {
                if (adduct.AdductIonName == "[M+H]+")
                {
                    if (sn1Carbon < 10 || sn2Carbon < 10) return null;
                    if (sn1Double > 6 || sn2Double > 6) return null;
                    //  seek PC diagnostic
                    var threshold = 3.0;
                    var isClassIonFound1 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, C5H14NO4P + Proton, threshold);
                    if (!isClassIonFound1) return null;
                    // reject Na+ adduct
                    var diagnosticMz3 = theoreticalMz - C3H9N;
                    var isNaTypicalFound1 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz3, 10.0);
                    // reject PE
                    var PEHeaderLoss = theoreticalMz - 141.019094261;
                    var isClassIonFoundPe = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, PEHeaderLoss, 5.0);
                    if (isNaTypicalFound1 || isClassIonFoundPe) {
                        return null;
                    }

                    // from here, acyl level annotation is executed.
                    var nl_SN1 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) + MassDiffDictionary.HydrogenMass;
                    var nl_SN1_H2O = nl_SN1 - H2O;

                    var nl_SN2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) + MassDiffDictionary.HydrogenMass;
                    var nl_SN2_H2O = nl_SN2 - H2O;

                    var query = new List<SpectrumPeak> {
                        new() { Mass = nl_SN1, Intensity = 0.1 },
                        new() { Mass = nl_SN1_H2O, Intensity = 0.1 },
                        new() { Mass = nl_SN2, Intensity = 0.1 },
                        new() { Mass = nl_SN2_H2O, Intensity = 0.1 }
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);
                    if (foundCount >= 2) {
                        var nl_SN1_H2O_CH2 = nl_SN1_H2O - CD2;
                        var nl_SN2_H2O_CH2 = nl_SN2_H2O - CD2;
                        var queryCh2 = new List<SpectrumPeak> {
                            new() { Mass = nl_SN1_H2O_CH2, Intensity = 1.0 },
                            new() { Mass = nl_SN2_H2O_CH2, Intensity = 1.0 },
                        };

                        var foundCountSN2 = 0;
                        var averageIntensitySN2 = 0.0;
                        LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, queryCh2, ms2Tolerance, out foundCountSN2, out averageIntensitySN2);
                        if (foundCountSN2 > 0) {
                            var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PC_d5", LbmClass.PC_d5, sn1Carbon, sn1Double,
                                sn2Carbon, sn2Double, averageIntensity);
                            candidates.Add(molecule);
                        }
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PC_d5", LbmClass.PC_d5, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    //var diagnosticMz = 184.07332;
                    // seek [M+Na -C5H14NO4P]+
                    var diagnosticMz2 = theoreticalMz - C5H14NO4P;
                    // seek [M+Na -C3H9N]+
                    var diagnosticMz3 = theoreticalMz - C3H9N;
                    var threshold = 3.0;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz3, threshold);
                    if (!isClassIonFound || !isClassIon2Found) return null;
                    // seek PC diagnostic
                    var isClassIonFound1 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, C5H14NO4P + Na, threshold);
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_C + Na, threshold);
                    var isClassIonFound3 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_O + Na, threshold);
                    if (!isClassIonFound1 || !isClassIonFound2 || !isClassIonFound3) return null;

                    // from here, acyl level annotation is executed.
                    for (int sn1CarbonNum = 6; sn1CarbonNum <= totalCarbon; sn1CarbonNum++) {
                        for (int sn1DoubleNum = 0; sn1DoubleNum <= totalDoubleBond; sn1DoubleNum++) {
                            var sn2CarbonNum = totalCarbon - sn1CarbonNum;
                            var sn2DoubleNum = totalDoubleBond - sn1DoubleNum;
                            if (sn2DoubleNum >= 7) continue;

                            var nl_SN1 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1CarbonNum, sn1DoubleNum) + MassDiffDictionary.HydrogenMass;
                            var nl_SN1_H2O = nl_SN1 - H2O;
                            var nl_SN2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2CarbonNum, sn2DoubleNum) + MassDiffDictionary.HydrogenMass;
                            var nl_SN2_H2O = nl_SN2 - H2O;

                            var query = new List<SpectrumPeak> {
                                new() { Mass = nl_SN1, Intensity = 0.1 },
                                new() { Mass = nl_SN1_H2O, Intensity = 0.1 },
                                new() { Mass = nl_SN2, Intensity = 0.1 },
                                new() { Mass = nl_SN2_H2O, Intensity = 0.1 }
                            };

                            var foundCount = 0;
                            var averageIntensity = 0.0;
                            LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                            if (foundCount >= 2) {
                                var nl_SN1_H2O_CH2 = nl_SN1_H2O + MassDiffDictionary.HydrogenMass - CD2;
                                var nl_SN2_H2O_CH2 = nl_SN2_H2O + MassDiffDictionary.HydrogenMass - CD2;
                                var queryCh2 = new List<SpectrumPeak> {
                                    new() { Mass = nl_SN1_H2O_CH2, Intensity = 1.0 },
                                    new() { Mass = nl_SN2_H2O_CH2, Intensity = 1.0 },
                                };

                                var foundCountSN2 = 0;
                                var averageIntensitySN2 = 0.0;
                                LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, queryCh2, ms2Tolerance, out foundCountSN2, out averageIntensitySN2);
                                if (foundCountSN2 > 0) {
                                    var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PC_d5", LbmClass.PC_d5, sn1CarbonNum, sn1DoubleNum,
                                        sn2CarbonNum, sn2DoubleNum, averageIntensity);
                                    candidates.Add(molecule);
                                }
                            }
                        }
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PC_d5", LbmClass.PC_d5, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            } else {
                if (adduct.AdductIonName == "[M+FA-H]-" || adduct.AdductIonName == "[M+Hac-H]-" ||
                    adduct.AdductIonName == "[M+HCOO]-" || adduct.AdductIonName == "[M+CH3COO]-")
                {
                    // seek [M-CH3]-
                    var threshold = 10.0;
                    var diagnosticMz = adduct.AdductIonName == "[M+CH3COO]-" || adduct.AdductIonName == "[M+Hac-H]-" ?
                        theoreticalMz - 74.036779433 : theoreticalMz - 60.021129369;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (!isClassIonFound) return null;
                    if (adduct.AdductIonName == "[M+CH3COO]-" || adduct.AdductIonName == "[M+Hac-H]-") {
                        var diagnosticMz2 = theoreticalMz - 60.021129369; // in source check
                        var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold);
                        if (isClassIonFound2) return null;
                    }

                    // from here, acyl level annotation is executed.
                    var sn1 = LipidMsmsCharacterizationUtility.fattyacidProductIon(sn1Carbon, sn1Double);
                    var sn2 = LipidMsmsCharacterizationUtility.fattyacidProductIon(sn2Carbon, sn2Double);

                    var query = new List<SpectrumPeak> {
                            new() { Mass = sn1, Intensity = 0 },
                            new() { Mass = sn2, Intensity = 0 }
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2) {
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PC_d5", LbmClass.PC_d5, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PC_d5", LbmClass.PC_d5, "", theoreticalMz, adduct,
                           totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfPhosphatidylethanolamineD5(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond, 
            int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double, AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            var candidates = new List<LipidMolecule>();

            double Gly_C = new[] {
                MassDiffDictionary.CarbonMass * 5,
                MassDiffDictionary.HydrogenMass * 7,
                MassDiffDictionary.NitrogenMass,
                MassDiffDictionary.OxygenMass * 4,
                MassDiffDictionary.PhosphorusMass,
                MassDiffDictionary.Hydrogen2Mass * 5,
            }.Sum();

            double Gly_O = new[] {
                MassDiffDictionary.CarbonMass * 4,
                MassDiffDictionary.HydrogenMass * 7,
                MassDiffDictionary.NitrogenMass,
                MassDiffDictionary.OxygenMass * 5,
                MassDiffDictionary.PhosphorusMass,
                MassDiffDictionary.Hydrogen2Mass * 3,
            }.Sum();

            double C2H8NO4P = new[] {
                MassDiffDictionary.CarbonMass * 2,
                MassDiffDictionary.HydrogenMass * 8,
                MassDiffDictionary.NitrogenMass,
                MassDiffDictionary.OxygenMass * 4,
                MassDiffDictionary.PhosphorusMass,
            }.Sum();
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            {
                if (adduct.AdductIonName == "[M+H]+")
                {
                    // seek -141.019094261 (C2H8NO4P)and PE diagnostic
                    var threshold = 2.5;
                    var diagnosticMz = theoreticalMz - C2H8NO4P;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (!isClassIonFound) return null;

                    // from here, acyl level annotation is executed.
                    var sn1 = LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) - Electron;
                    var sn2 = LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) - Electron;

                    var query = new List<SpectrumPeak> {
                        new() { Mass = sn1, Intensity = 0.1 },
                        new() { Mass = sn2, Intensity = 0.1 }
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2) {
                        var nl_SN1_H2O_CH2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) + MassDiffDictionary.HydrogenMass - H2O - CH2;
                        var nl_SN2_H2O_CH2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) + MassDiffDictionary.HydrogenMass - H2O - CH2;
                        var queryCh2 = new List<SpectrumPeak> {
                            new() { Mass = nl_SN1_H2O_CH2, Intensity = 1.0 },
                            new() { Mass = nl_SN2_H2O_CH2, Intensity = 1.0 },
                        };

                        var foundCountSN2 = 0;
                        var averageIntensitySN2 = 0.0;
                        LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, queryCh2, ms2Tolerance, out foundCountSN2, out averageIntensitySN2);
                        if (foundCountSN2 > 0) {
                            var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PE_d5", LbmClass.PE_d5, sn1Carbon, sn1Double,
                                sn2Carbon, sn2Double, averageIntensity);
                            candidates.Add(molecule);
                        }
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PE_d5", LbmClass.PE_d5, "", theoreticalMz, adduct,
                            totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek -141.019094261 (C2H8NO4P)
                    var threshold = 3.0;
                    var diagnosticMz = theoreticalMz - C2H8NO4P;
                    // seek - 43.042199 (C2H5N)
                    //var diagnosticMz2 = theoreticalMz - C2H5N;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_C + Na, threshold);
                    var isClassIonFound3 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_O + Na, threshold);
                    if (!isClassIonFound || !isClassIonFound2 || !isClassIonFound3) return null;

                    // from here, acyl level annotation is executed.
                    for (int sn1CarbonNum = 6; sn1CarbonNum <= totalCarbon; sn1CarbonNum++) {
                        for (int sn1DoubleNum = 0; sn1DoubleNum <= totalDoubleBond; sn1DoubleNum++) {
                            var sn2CarbonNum = totalCarbon - sn1CarbonNum;
                            var sn2DoubleNum = totalDoubleBond - sn1DoubleNum;
                            if (sn2DoubleNum >= 7) continue;

                            var nl_SN1 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1CarbonNum, sn1DoubleNum) - MassDiffDictionary.HydrogenMass;
                            var nl_SN1_H2O = nl_SN1 - MassDiffDictionary.OxygenMass;
                            var nl_SN2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2CarbonNum, sn2DoubleNum) - MassDiffDictionary.HydrogenMass;
                            var nl_SN2_H2O = nl_SN2 - MassDiffDictionary.OxygenMass;

                            var query = new List<SpectrumPeak> {
                                new() { Mass = nl_SN1, Intensity = 0.1 },
                                new() { Mass = nl_SN1_H2O, Intensity = 0.1 },
                                new() { Mass = nl_SN2, Intensity = 0.1},
                                new() { Mass = nl_SN2_H2O, Intensity = 0.1 }
                            };
                            var foundCount = 0;
                            var averageIntensity = 0.0;
                            LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);
                            if (foundCount >= 2) {
                                //check Sn2 position spectrum
                                var nl_SN1_H2O_CH2 = nl_SN1_H2O - CD2;
                                var nl_SN2_H2O_CH2 = nl_SN2_H2O - CD2;

                                var queryCh2 = new List<SpectrumPeak> {
                                    new() { Mass = nl_SN1_H2O_CH2, Intensity = 1.0 },
                                    new() { Mass = nl_SN2_H2O_CH2, Intensity = 1.0 },
                                };

                                var foundCountSN2 = 0;
                                var averageIntensitySN2 = 0.0;
                                LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, queryCh2, ms2Tolerance, out foundCountSN2, out averageIntensitySN2);

                                if (foundCountSN2 > 0) {                                    
                                    var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PE_d5", LbmClass.PE_d5, sn1CarbonNum, sn1DoubleNum,
                                        sn2CarbonNum, sn2DoubleNum, averageIntensity);
                                    candidates.Add(molecule);
                                }
                            }
                        }
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PE_d5", LbmClass.PE_d5, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            } else {
                if (adduct.AdductIonName == "[M-H]-")
                {
                    // seek C5H11NO5P-
                    var threshold = 5.0;
                    var diagnosticMz = 196.03803;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);

                    var threshold2 = 5.0;
                    var diagnosticMz2 = 152.995833871; // seek C3H6O5P- (maybe LNAPE)
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    if (isClassIon2Found == true) return null;

                    // from here, acyl level annotation is executed.
                    var sn1 = LipidMsmsCharacterizationUtility.fattyacidProductIon(sn1Carbon, sn1Double);
                    var sn2 = LipidMsmsCharacterizationUtility.fattyacidProductIon(sn2Carbon, sn2Double);

                    var query = new List<SpectrumPeak> {
                        new() { Mass = sn1, Intensity = 1.0 },
                        new() { Mass = sn2, Intensity = 1.0 }
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2) {
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PE_d5", LbmClass.PE_d5, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }

                    //if (isClassIonFound == false && candidates.Count == 0) return null;
                    if (foundCount == 0) return null;
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PE_d5", LbmClass.PE_d5, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfPhosphatidylglycerolD5(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond, 
           int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double, AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            double Gly_C = new[] {
                MassDiffDictionary.CarbonMass * 6,
                MassDiffDictionary.HydrogenMass * 8,
                MassDiffDictionary.OxygenMass * 6,
                MassDiffDictionary.PhosphorusMass,
                MassDiffDictionary.Hydrogen2Mass * 5,
            }.Sum();

            double Gly_O = new[] {
                MassDiffDictionary.CarbonMass * 5,
                MassDiffDictionary.HydrogenMass * 8,
                MassDiffDictionary.OxygenMass * 7,
                MassDiffDictionary.PhosphorusMass,
                MassDiffDictionary.Hydrogen2Mass * 3,
            }.Sum();

            double C3H9O6P = new[] {
                MassDiffDictionary.CarbonMass * 3,
                MassDiffDictionary.HydrogenMass * 9,
                MassDiffDictionary.OxygenMass * 6,
                MassDiffDictionary.PhosphorusMass,
            }.Sum();
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            {
                if (adduct.AdductIonName == "[M+NH4]+")
                {
                    // seek -189.040227 (C3H8O6P+NH4)
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - C3H9O6P - NH3;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (!isClassIonFound) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    var nl_SN1 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) + MassDiffDictionary.HydrogenMass;
                    var nl_SN1_H2O = nl_SN1 - H2O;
                    var nl_SN2 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) + MassDiffDictionary.HydrogenMass;
                    var nl_SN2_H2O = nl_SN2 - H2O;

                    var query = new List<SpectrumPeak> {
                        new() { Mass = nl_SN1, Intensity = 0.1 },
                        new() { Mass = nl_SN1_H2O, Intensity = 0.1 },
                        new() { Mass = nl_SN2, Intensity = 0.1 },
                        new() { Mass = nl_SN2_H2O, Intensity = 0.1 }
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount >= 2 && averageIntensity < 30) { 
                        // average intensity < 30 is nessesarry to distinguish it from BMP
                        //check Sn2 position spectrum
                        var nl_SN1_H2O_CH2 = nl_SN1_H2O - CD2;
                        var nl_SN2_H2O_CH2 = nl_SN1_H2O - CD2;
                        var queryCh2 = new List<SpectrumPeak> {
                            new() { Mass = nl_SN1_H2O_CH2, Intensity = 1.0 },
                            new() { Mass = nl_SN2_H2O_CH2, Intensity = 1.0 },
                        };

                        var foundCountSN2 = 0;
                        var averageIntensitySN2 = 0.0;
                        LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, queryCh2, ms2Tolerance, out foundCountSN2, out averageIntensitySN2);
                        if (foundCountSN2 > 0) {
                            var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PG_d5", LbmClass.PG_d5, sn1Carbon, sn1Double,
                                sn2Carbon, sn2Double, averageIntensity);
                            candidates.Add(molecule);
                        }
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PG_d5", LbmClass.PG_d5, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek header loss
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - C3H9O6P;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    // PG diagnostic
                    var threshold2 = 5.0;
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_C + Na, threshold2);
                    var isClassIonFound3 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_O + Na, threshold2);
                    if (!isClassIonFound || !isClassIonFound2 || !isClassIonFound3) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PG_d5", LbmClass.PG_d5, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            } else {
                if (adduct.AdductIonName == "[M-H]-")
                {
                    // seek C3H6O5P-
                    var threshold = 0;
                    var diagnosticMz = 152.995833871;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    var sn1 = LipidMsmsCharacterizationUtility.fattyacidProductIon(sn1Carbon, sn1Double);
                    var sn2 = LipidMsmsCharacterizationUtility.fattyacidProductIon(sn2Carbon, sn2Double);

                    var query = new List<SpectrumPeak> {
                        new() { Mass = sn1, Intensity = 1.0 },
                        new() { Mass = sn2, Intensity = 1.0 }
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);
                    if (isClassIonFound == false) return null;

                    if (foundCount == 2) { 
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PG_d5", LbmClass.PG_d5, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PG_d5", LbmClass.PG_d5, "", theoreticalMz, adduct,
                      totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfPhosphatidylserineD5(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond, 
           int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double, AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            double Gly_C = new[] {
                MassDiffDictionary.CarbonMass * 6,
                MassDiffDictionary.HydrogenMass * 7,
                MassDiffDictionary.NitrogenMass,
                MassDiffDictionary.OxygenMass * 6,
                MassDiffDictionary.PhosphorusMass,
                MassDiffDictionary.Hydrogen2Mass * 5,
            }.Sum();

            double Gly_O = new[] {
                MassDiffDictionary.CarbonMass * 5,
                MassDiffDictionary.HydrogenMass * 7,
                MassDiffDictionary.NitrogenMass,
                MassDiffDictionary.OxygenMass * 7,
                MassDiffDictionary.PhosphorusMass,
                MassDiffDictionary.Hydrogen2Mass * 3,
            }.Sum();

            double C3H8NO6P = new[]
                    {
                    MassDiffDictionary.CarbonMass * 3,
                    MassDiffDictionary.HydrogenMass * 8,
                    MassDiffDictionary.NitrogenMass,
                    MassDiffDictionary.OxygenMass * 6,
                    MassDiffDictionary.PhosphorusMass,
            }.Sum();
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    // seek -185.008927 (C3H8NO6P)
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - C3H8NO6P;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (!isClassIonFound) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    var nl_SN1 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) + MassDiffDictionary.HydrogenMass;
                    var nl_SN1_H2O = nl_SN1 - H2O;
                    var nl_SN2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) + MassDiffDictionary.HydrogenMass;
                    var nl_NS2_H2O = nl_SN2 - H2O;

                    var query = new List<SpectrumPeak> {
                        new() { Mass = nl_SN1, Intensity = 0.1 },
                        new() { Mass = nl_SN1_H2O, Intensity = 0.1 },
                        new() { Mass = nl_SN2, Intensity = 0.1 },
                        new() { Mass = nl_NS2_H2O, Intensity = 0.1 }
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount >= 2) {
                        //check Sn2 position spectrum
                        var nl_SN1_H2O_CH2 = nl_SN1 - H2O - CD2;
                        var nl_SN2_H2O_CH2 = nl_SN2 - H2O - CD2;
                        var queryCh2 = new List<SpectrumPeak> {
                            new() { Mass = nl_SN1_H2O_CH2, Intensity = 1.0 },
                            new() { Mass = nl_SN2_H2O_CH2, Intensity = 1.0 },
                        };

                        var foundCountSN2 = 0;
                        var averageIntensitySN2 = 0.0;
                        LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, queryCh2, ms2Tolerance, out foundCountSN2, out averageIntensitySN2);
                        if (foundCountSN2 > 0) {
                            var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PS_d5", LbmClass.PS_d5, sn1Carbon, sn1Double,
                                sn2Carbon, sn2Double, averageIntensity);
                            candidates.Add(molecule);
                        }
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PS_d5", LbmClass.PS_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 2);

                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek -185.008927 (C3H8NO6P)
                    var threshold = 3.0;
                    var diagnosticMz = theoreticalMz - C3H8NO6P;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    // PS diagnostic
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_C + Na, threshold);
                    var isClassIonFound3 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_O + Na, threshold);
                    if (!isClassIonFound || !isClassIonFound2 || !isClassIonFound3) return null;

                    var candidates = new List<LipidMolecule>();
                    // from here, acyl level annotation is executed.
                    for (int sn1CarbonNum = 6; sn1CarbonNum <= totalCarbon; sn1CarbonNum++) {
                        for (int sn1DoubleNum = 0; sn1DoubleNum <= totalDoubleBond; sn1DoubleNum++) {
                            var sn2CarbonNum = totalCarbon - sn1CarbonNum;
                            var sn2DoubleNum = totalDoubleBond - sn1DoubleNum;
                            if (sn2DoubleNum >= 7) continue;

                            var nl_SN1 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1CarbonNum, sn1DoubleNum) + MassDiffDictionary.HydrogenMass;
                            var nl_SN1_H2O = nl_SN1 - MassDiffDictionary.OxygenMass - MassDiffDictionary.HydrogenMass;
                            var nl_SN2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2CarbonNum, sn2DoubleNum) + MassDiffDictionary.HydrogenMass;
                            var nl_SN2_H2O = nl_SN2 - MassDiffDictionary.OxygenMass - MassDiffDictionary.HydrogenMass;
                            var query = new List<SpectrumPeak> {
                                new() { Mass = nl_SN1, Intensity = 0.1 },
                                new() { Mass = nl_SN1_H2O, Intensity = 0.1 },
                                new() { Mass = nl_SN2, Intensity = 0.1},
                                new() { Mass = nl_SN2_H2O, Intensity = 0.1 }
                            };

                            var foundCount = 0;
                            var averageIntensity = 0.0;
                            LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);
                            if (foundCount >= 2) {
                                //check Sn2 position spectrum
                                var nl_SN1_H2O_CH2 = nl_SN1_H2O - CD2;
                                var nl_SN2_H2O_CH2 = nl_SN2_H2O - CD2;
                                var queryCh2 = new List<SpectrumPeak> {
                                    new() { Mass = nl_SN1_H2O_CH2, Intensity = 1.0 },
                                    new() { Mass = nl_SN2_H2O_CH2, Intensity = 1.0 },
                                };

                                var foundCountSN2 = 0;
                                var averageIntensitySN2 = 0.0;
                                LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, queryCh2, ms2Tolerance, out foundCountSN2, out averageIntensitySN2);
                                if (foundCountSN2 > 0) {
                                    var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PS_d5", LbmClass.PS_d5, sn1CarbonNum, sn1DoubleNum,
                                        sn2CarbonNum, sn2DoubleNum, averageIntensity);
                                    candidates.Add(molecule);
                                }
                            }
                        }
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PS_d5", LbmClass.PS_d5, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            } else {
                if (adduct.AdductIonName == "[M-H]-")
                {
                    // seek C3H5NO2 loss
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - 87.032029;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    var threshold2 = 30;
                    var diagnosticMz2 = theoreticalMz - 63.008491; // [M+C2H3N(ACN)+Na-2H]- adduct of PG [M-H]- 
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    if (isClassIonFound2) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();

                    var sn1 = LipidMsmsCharacterizationUtility.fattyacidProductIon(sn1Carbon, sn1Double);
                    var sn2 = LipidMsmsCharacterizationUtility.fattyacidProductIon(sn2Carbon, sn2Double);
                    var query = new List<SpectrumPeak> {
                        new() { Mass = sn1, Intensity = 0.01 },
                        new() { Mass = sn2, Intensity = 0.01 }
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2) { 
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PS_d5", LbmClass.PS_d5, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PS_d5", LbmClass.PS_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfPhosphatidylinositolD5(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond, 
           int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double, AdductIon adduct)
        {
            double Gly_C = new[] {
                MassDiffDictionary.CarbonMass * 9,
                MassDiffDictionary.HydrogenMass * 12,
                MassDiffDictionary.OxygenMass * 9,
                MassDiffDictionary.PhosphorusMass,
                MassDiffDictionary.Hydrogen2Mass * 5,
            }.Sum();

            double Gly_O = new[] {
                MassDiffDictionary.CarbonMass * 8,
                MassDiffDictionary.HydrogenMass * 12,
                MassDiffDictionary.OxygenMass * 10,
                MassDiffDictionary.PhosphorusMass,
                MassDiffDictionary.Hydrogen2Mass * 3,
            }.Sum();
            
            double C6H13O9P = new[] {
                MassDiffDictionary.CarbonMass * 6,
                MassDiffDictionary.HydrogenMass * 13,
                MassDiffDictionary.OxygenMass * 9,
                MassDiffDictionary.PhosphorusMass,
            }.Sum();
            
            double C6H10O5 = new[]
                    {
                MassDiffDictionary.CarbonMass * 6,
                MassDiffDictionary.HydrogenMass * 10,
                MassDiffDictionary.OxygenMass * 5,
            }.Sum();
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { 
                if (adduct.AdductIonName == "[M+NH4]+")
                {
                    // seek -277.056272 (C6H12O9P+NH4)
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - C6H13O9P - NH3;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (!isClassIonFound) return null;
                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    for (int sn1CarbonNum = 6; sn1CarbonNum <= totalCarbon; sn1CarbonNum++) {
                        for (int sn1DoubleNum = 0; sn1DoubleNum <= totalDoubleBond; sn1DoubleNum++) {
                            var sn2CarbonNum = totalCarbon - sn1CarbonNum;
                            var sn2DoubleNum = totalDoubleBond - sn1DoubleNum;

                            var nl_SN1 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1CarbonNum, sn1DoubleNum) + MassDiffDictionary.HydrogenMass;
                            var nl_SN1_H2O = nl_SN1 - H2O;
                            var nl_SN2 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2CarbonNum, sn2DoubleNum) + MassDiffDictionary.HydrogenMass;
                            var nl_SN2_H2O = nl_SN2 - H2O;
                            var query = new List<SpectrumPeak> {
                                new() { Mass = nl_SN1, Intensity = 0.1 },
                                new() { Mass = nl_SN1_H2O, Intensity = 0.1 },
                                new() { Mass = nl_SN2, Intensity = 0.1 },
                                new() { Mass = nl_SN2_H2O, Intensity = 0.1 }
                            };

                            var foundCount = 0;
                            var averageIntensity = 0.0;
                            LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                            if (foundCount >= 2) {
                                //check Sn2 position spectrum
                                var nl_SN1_H2O_CH2 = nl_SN1_H2O - CD2;
                                var nl_SN2_H2O_CH2 = nl_SN2_H2O - CD2;
                                var queryCh2 = new List<SpectrumPeak> {
                                    new() { Mass = nl_SN1_H2O_CH2, Intensity = 0.5 },
                                    new() { Mass = nl_SN2_H2O_CH2, Intensity = 0.5 },
                                };

                                var foundCountSN2 = 0;
                                var averageIntensitySN2 = 0.0;
                                LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, queryCh2, ms2Tolerance, out foundCountSN2, out averageIntensitySN2);
                                if (foundCountSN2 > 0) {
                                    var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PI_d5", LbmClass.PI_d5, sn1CarbonNum, sn1DoubleNum,
                                        sn2CarbonNum, sn2DoubleNum, averageIntensity);
                                    candidates.Add(molecule);
                                }
                            }
                        }
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PI_d5", LbmClass.PI_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek -C6H10O5
                    var threshold = 5.0;
                    var diagnosticMz = theoreticalMz - C6H10O5;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    // PI diagnostic
                    var threshold2 = 5.0;
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_C + Na, threshold2);
                    var isClassIonFound3 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_O + Na, threshold2);
                    if (!isClassIonFound || !isClassIonFound2 || !isClassIonFound3) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    for (int sn1CarbonNum = 6; sn1CarbonNum <= totalCarbon; sn1CarbonNum++) {
                        for (int sn1DoubleNum = 0; sn1DoubleNum <= totalDoubleBond; sn1DoubleNum++) {
                            var sn2CarbonNum = totalCarbon - sn1CarbonNum;
                            var sn2DoubleNum = totalDoubleBond - sn1DoubleNum;

                            var nl_SN1 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1CarbonNum, sn1DoubleNum) + MassDiffDictionary.HydrogenMass;
                            var nl_SN1_H2O = nl_SN1 - H2O;
                            var nl_SN2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2CarbonNum, sn2DoubleNum) + MassDiffDictionary.HydrogenMass;
                            var nl_SN2_H2O = nl_SN2 - H2O;

                            var query = new List<SpectrumPeak> {
                                new() { Mass = nl_SN1, Intensity = 0.1 },
                                new() { Mass = nl_SN1_H2O, Intensity = 0.1 },
                                new() { Mass = nl_SN2, Intensity = 0.1 },
                                new() { Mass = nl_SN2_H2O, Intensity = 0.1 }
                            };

                            var foundCount = 0;
                            var averageIntensity = 0.0;
                            LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                            if (foundCount >= 2) {
                                //check Sn2 position spectrum
                                var nl_SN1_H2O_CH2 = nl_SN1_H2O - CD2 + MassDiffDictionary.HydrogenMass; ;
                                var nl_SN2_H2O_CH2 = nl_SN2_H2O - CD2 + MassDiffDictionary.HydrogenMass; ;
                                var queryCh2 = new List<SpectrumPeak> {
                                    new() { Mass = nl_SN1_H2O_CH2, Intensity = 1.0 },
                                    new() { Mass = nl_SN2_H2O_CH2, Intensity = 1.0 },
                                };

                                var foundCountSN2 = 0;
                                var averageIntensitySN2 = 0.0;
                                LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, queryCh2, ms2Tolerance, out foundCountSN2, out averageIntensitySN2);
                                if (foundCountSN2 > 0) {
                                    var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PI_d5", LbmClass.PI_d5, sn1CarbonNum, sn1DoubleNum,
                                        sn2CarbonNum, sn2DoubleNum, averageIntensity);
                                    candidates.Add(molecule);
                                }
                            }
                        }
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PI_d5", LbmClass.PI_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            } else {
                if (adduct.AdductIonName == "[M-H]-")
                {
                    // seek 241.01188(C6H10O8P-) and 297.037548(C9H14O9P-)
                    var threshold = 0;
                    var diagnosticMz1 = 241.01188 + Electron;
                    var diagnosticMz2 = 297.037548 + Electron;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold);
                    if (!isClassIon1Found && !isClassIon2Found) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();

                    var sn1 = LipidMsmsCharacterizationUtility.fattyacidProductIon(sn1Carbon, sn1Double);
                    var sn2 = LipidMsmsCharacterizationUtility.fattyacidProductIon(sn2Carbon, sn2Double);
                    var query = new List<SpectrumPeak> {
                        new() { Mass = sn1, Intensity = 0 },
                        new() { Mass = sn2, Intensity = 0 }
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2) { 
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PI_d5", LbmClass.PI_d5, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PI_d5", LbmClass.PI_d5, "", theoreticalMz, adduct,
                      totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            return null;
        }
        public static LipidMolecule JudgeIfLysopcD5(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond, 
            int snCarbon, int snDoubleBond, AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    if (totalCarbon > 28) return null; //  currently carbon > 28 is recognized as EtherPC
                                                       // seek 184.07332 (C5H15NO4P)
                    var threshold = 2.0;
                    var diagnosticMz = 184.07332;
                    var diagnosticMz2 = 104.106990;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIon1Found != true) return null;
                    // reject Na+ adduct
                    var diagnosticMz3 = theoreticalMz - 59.0735;
                    var isNaTypicalFound1 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz3, 10.0);
                    if (isNaTypicalFound1) {
                        return null;
                    }

                    // for eieio
                    var PEHeaderLoss = theoreticalMz - 141.019094261 + MassDiffDictionary.ProtonMass;
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, PEHeaderLoss, 3.0);
                    if (isClassIonFound2 && LipidMsmsCharacterizationUtility.isFragment1GreaterThanFragment2(spectrum, ms2Tolerance, PEHeaderLoss, diagnosticMz)) {
                        return null;
                    }
                    
                    var candidates = new List<LipidMolecule>();
                    var chainSuffix = "";
                    var diagnosticMzExist = 0.0;
                    var diagnosticMzIntensity = 0.0;
                    var diagnosticMzExist2 = 0.0;
                    var diagnosticMzIntensity2 = 0.0;

                    for (int i = 0; i < spectrum.Count; i++) {
                        var mz = spectrum[i].Mass;
                        var intensity = spectrum[i].Intensity;

                        if (intensity > threshold && Math.Abs(mz - diagnosticMz) < ms2Tolerance) {
                            diagnosticMzExist = mz;
                            diagnosticMzIntensity = intensity;
                        }
                        else if (intensity > threshold && Math.Abs(mz - diagnosticMz2) < ms2Tolerance) {
                            diagnosticMzExist2 = mz;
                            diagnosticMzIntensity2 = intensity;
                        }
                    };

                    if (diagnosticMzIntensity2 / diagnosticMzIntensity > 0.3) {
                        chainSuffix = "/0:0";
                    }

                    var score = 0.0;
                    if (totalCarbon < 30) score = score + 1.0;
                    var molecule = LipidMsmsCharacterizationUtility.getSingleacylchainwithsuffixMoleculeObjAsLevel2("LPC_d5", LbmClass.LPC_d5, totalCarbon, totalDoubleBond,
                    score, chainSuffix);
                    candidates.Add(molecule);

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPC_d5", LbmClass.LPC_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);

                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    if (totalCarbon > 28) return null; //  currently carbon > 28 is recognized as EtherPC
                                                       // seek PreCursor - 59 (C3H9N)
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - 59.072951;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    var candidates = new List<LipidMolecule>();
                    var score = 0.0;
                    if (totalCarbon < 30) score = score + 1.0;
                    var molecule = LipidMsmsCharacterizationUtility.getSingleacylchainMoleculeObjAsLevel2("LPC_d5", LbmClass.LPC_d5, totalCarbon, totalDoubleBond, score);
                    candidates.Add(molecule);

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPC_d5", LbmClass.LPC_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            } else {
                if (adduct.AdductIonName == "[M+FA-H]-" || adduct.AdductIonName == "[M+Hac-H]-" ||
                    adduct.AdductIonName == "[M+HCOO]-" || adduct.AdductIonName == "[M+CH3COO]-")
                {
                    if (totalCarbon > 28) return null; //  currently carbon > 28 is recognized as EtherPC

                    // seek [M-CH3]-
                    var threshold = 1.0;
                    var diagnosticMz = adduct.AdductIonName == "[M+CH3COO]-" || adduct.AdductIonName == "[M+Hac-H]-" ?
                        theoreticalMz - 74.036779433 : theoreticalMz - 60.021129369;
                    var diagnosticMz2 = LipidMsmsCharacterizationUtility.fattyacidProductIon(totalCarbon, totalDoubleBond);
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold);
                    if (isClassIon1Found != true || isClassIon2Found != true) return null;

                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPC_d5", LbmClass.LPC_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfLysopeD5(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond, 
            int snCarbon, int snDoubleBond, AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    if (totalCarbon > 28) return null; //  currently carbon > 28 is recognized as EtherPE
                    double Gly_C = new[] {
                        MassDiffDictionary.CarbonMass * 5,
                        MassDiffDictionary.HydrogenMass * 7,
                        MassDiffDictionary.NitrogenMass,
                        MassDiffDictionary.OxygenMass * 4,
                        MassDiffDictionary.PhosphorusMass,
                        MassDiffDictionary.Hydrogen2Mass * 5,
                        Proton
                    }.Sum();

                    double Gly_O = new[] {
                        MassDiffDictionary.CarbonMass * 4,
                        MassDiffDictionary.HydrogenMass * 7,
                        MassDiffDictionary.NitrogenMass,
                        MassDiffDictionary.OxygenMass * 5,
                        MassDiffDictionary.PhosphorusMass,
                        MassDiffDictionary.Hydrogen2Mass * 3,
                        Proton
                    }.Sum();

                    double C2H8NO4P = new[] {
                        MassDiffDictionary.CarbonMass * 2,
                        MassDiffDictionary.HydrogenMass * 8,
                        MassDiffDictionary.NitrogenMass,
                        MassDiffDictionary.OxygenMass * 4,
                        MassDiffDictionary.PhosphorusMass,
                    }.Sum();
                    // seek -141.019094261 (C2H8NO4P)
                    var threshold = 0.5;
                    var diagnosticMz = theoreticalMz - C2H8NO4P;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (!isClassIonFound) return null;
                    // reject EtherPE 
                    var sn1alkyl = (MassDiffDictionary.CarbonMass * snCarbon)
                                        + (MassDiffDictionary.HydrogenMass * ((snCarbon * 2) - (snDoubleBond * 2) + 1));//sn1(ether)
                    var NL_sn1 = diagnosticMz - sn1alkyl + Proton;
                    var sn1_rearrange = sn1alkyl + MassDiffDictionary.HydrogenMass * 2 + 139.00290;//sn1(ether) + C2H5NO4P + proton 

                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, NL_sn1, threshold);
                    var isClassIon3Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, sn1_rearrange, threshold);
                    if (isClassIon2Found == true || isClassIon3Found == true) return null;

                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPE_d5", LbmClass.LPE_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek PreCursor -141(C2H8NO4P)
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - 141.019094;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;
                    // reject EtherPE 
                    var sn1alkyl = (MassDiffDictionary.CarbonMass * snCarbon)
                                       + (MassDiffDictionary.HydrogenMass * ((snCarbon * 2) - (snDoubleBond * 2) + 1));//sn1(ether)

                    var NL_sn1 = diagnosticMz - sn1alkyl + Proton;
                    var sn1_rearrange = sn1alkyl + 139.00290 + MassDiffDictionary.HydrogenMass * 2;//sn1(ether) + C2H5NO4P + proton 

                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, NL_sn1, threshold);
                    var isClassIon3Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, sn1_rearrange, threshold);
                    if (isClassIon2Found == true || isClassIon3Found == true) return null;

                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPE_d5", LbmClass.LPE_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            } else {
                if (adduct.AdductIonName == "[M-H]-")
                {
                    if (totalCarbon > 28) return null; //  currently carbon > 28 is recognized as EtherPE
                    // seek PreCursor -197(C5H12NO5P)
                    var threshold = 10.0;
                    //var diagnosticMz = theoreticalMz - 197.04475958;
                    var diagnosticMz = theoreticalMz - 159.0691;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPE_d5", LbmClass.LPE_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            return null;
        }
        public static LipidMolecule JudgeIfLysopgD5(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PE 46:6, totalCarbon = 46 and totalDoubleBond = 6
            int snCarbon, int snDoubleBond,
            AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Negative)
            {
                if (adduct.AdductIonName == "[M-H]-")
                {
                    var diagnosticMz1 = 152.99583;  // seek C3H6O5P-
                    var threshold1 = 1.0;
                    var diagnosticMz2 = LipidMsmsCharacterizationUtility.fattyacidProductIon(totalCarbon, totalDoubleBond); // seek [FA-H]-
                    var threshold2 = 1.0;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold1);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    if (isClassIon1Found != true || isClassIon2Found != true) return null;

                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPG_d5", LbmClass.LPG_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            } else {
                if (adduct.AdductIonName == "[M+H]+" || adduct.AdductIonName == "[M+NH4]+")
                {
                    // seek Header loss (MG+ + chain Acyl) 
                    var threshold = 1.0;
                    var diagnosticMz = LipidMsmsCharacterizationUtility.acylCainMass(snCarbon, snDoubleBond) + (12 * 3 + MassDiffDictionary.Hydrogen2Mass * 5 + MassDiffDictionary.OxygenMass * 2) + MassDiffDictionary.ProtonMass;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPG_d5", LbmClass.LPG_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfLysopiD5(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond, 
            int snCarbon, int snDoubleBond, AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Negative)
            {
                if (adduct.AdductIonName == "[M-H]-")
                {
                    var diagnosticMz1 = 241.0118806 + Electron;  // seek C3H6O5P-
                    var threshold1 = 0;
                    //var diagnosticMz2 = 315.048656; // seek C9H16O10P-
                    var diagnosticMz2 = 320.087237; // seek C9H11D5O10P-
                    var threshold2 = 0;
                    var diagnosticMz3 = LipidMsmsCharacterizationUtility.fattyacidProductIon(totalCarbon, totalDoubleBond); // seek [FA-H]-
                    var threshold3 = 0;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold1);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    var isClassIon3Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz3, threshold3);
                    if (isClassIon1Found != true || isClassIon2Found != true) return null;

                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPI_d5", LbmClass.LPI_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            } else {
                if (adduct.AdductIonName == "[M+H]+" || adduct.AdductIonName == "[M+NH4]+")
                {
                    // seek Header loss (MG+ + chain Acyl) 
                    var threshold = 1.0;
                    var diagnosticMz = LipidMsmsCharacterizationUtility.acylCainMass(snCarbon, snDoubleBond) + (12 * 3 + MassDiffDictionary.Hydrogen2Mass * 5 + MassDiffDictionary.OxygenMass * 2) + MassDiffDictionary.ProtonMass;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPI_d5", LbmClass.LPI_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            return null;
        }
        public static LipidMolecule JudgeIfLysopsD5(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond, 
            int snCarbon, int snDoubleBond, AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Negative)
            {
                if (adduct.AdductIonName == "[M-H]-")
                {
                    //var diagnosticMz1 = 152.99583;  // seek C3H6O5P-
                    var diagnosticMz1 = 158.0249;
                    var threshold1 = 5.0;
                    var diagnosticMz2 = theoreticalMz - 87.032029; // seek -C3H6NO2-H
                    var threshold2 = 1.0;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold1);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    if (isClassIon1Found != true || isClassIon2Found != true) return null;

                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPS_d5", LbmClass.LPS_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            } else { 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    // seek Header loss (MG+ + chain Acyl) 
                    var threshold = 1.0;
                    var diagnosticMz = LipidMsmsCharacterizationUtility.acylCainMass(snCarbon, snDoubleBond) + (12 * 3 + MassDiffDictionary.Hydrogen2Mass * 5 + MassDiffDictionary.OxygenMass * 2) + MassDiffDictionary.ProtonMass;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPS_d5", LbmClass.LPS_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            return null;
        }
        public static LipidMolecule JudgeIfDagD5(IMSScanProperty msScanProp, double ms2Tolerance,
           double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PS 46:6, totalCarbon = 46 and totalDoubleBond = 6
           int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double,
           AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (totalCarbon > 52) return null; // currently, very large DAG is excluded.
            if (adduct.IonMode == IonMode.Positive)
            {
                if (adduct.AdductIonName == "[M+NH4]+")
                {
                    // seek -17.026549 (NH3)
                    var threshold = 1.0;
                    var diagnosticMz = theoreticalMz - 17.026549;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    if (sn2Double >= 7) return null;

                    var nl_SN1 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) - H2O + MassDiffDictionary.HydrogenMass;
                    var nl_SN2 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) - H2O + MassDiffDictionary.HydrogenMass;

                    var query = new List<SpectrumPeak> {
                        new() { Mass = nl_SN1, Intensity = 1 },
                        new() { Mass = nl_SN2, Intensity = 1 },
                    };
                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2) {
                        var molecule = LipidMsmsCharacterizationUtility.getDiacylglycerolMoleculeObjAsLevel2("DG_d5", LbmClass.DG_d5, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                    if (candidates == null || candidates.Count == 0)　return null;
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("DG_d5", LbmClass.DG_d5, string.Empty, theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // from here, acyl level annotation is executed.
                    var diagnosticMz = theoreticalMz;
                    var candidates = new List<LipidMolecule>();
                    for (int sn1CarbonNum = 6; sn1CarbonNum <= totalCarbon; sn1CarbonNum++)　{
                        for (int sn1DoubleNum = 0; sn1DoubleNum <= totalDoubleBond; sn1DoubleNum++)　{
                            var sn2CarbonNum = totalCarbon - sn1CarbonNum;
                            var sn2DoubleNum = totalDoubleBond - sn1DoubleNum;
                            if (sn2DoubleNum >= 7) continue;

                            var nl_SN1 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1CarbonNum, sn1DoubleNum) - H2O + MassDiffDictionary.HydrogenMass;
                            var nl_SN2 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2CarbonNum, sn2DoubleNum) - H2O + MassDiffDictionary.HydrogenMass;

                            var query = new List<SpectrumPeak>　{
                                new() { Mass = nl_SN1, Intensity = 1 },
                                new() { Mass = nl_SN2, Intensity = 1 },
                            };
                            var foundCount = 0;
                            var averageIntensity = 0.0;
                            LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                            if (foundCount == 2)　{
                                var molecule = LipidMsmsCharacterizationUtility.getDiacylglycerolMoleculeObjAsLevel2("DG", LbmClass.DG, sn1CarbonNum, sn1DoubleNum, 
                                    sn2CarbonNum, sn2DoubleNum,　averageIntensity);
                                candidates.Add(molecule);
                            }
                        }
                    }
                    if (candidates == null || candidates.Count == 0)　return null;
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("DG_d5", LbmClass.DG_d5, string.Empty, theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            return null;
        }
        public static LipidMolecule JudgeIfTriacylglycerolD5(IMSScanProperty msScanProp, double ms2Tolerance,　double theoreticalMz, int totalCarbon, int totalDoubleBond, 
           int sn1Carbon, int sn2Carbon, int sn3Carbon, int sn1Double, int sn2Double, int sn3Double,　AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { 
                if (adduct.AdductIonName == "[M+NH4]+")
                {
                    // seek -17.026549 (NH3)
                    var threshold = 1.0;
                    var diagnosticMz = theoreticalMz - 17.026549;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    if ((sn1Carbon == 18 && sn1Double == 5) || (sn2Carbon == 18 && sn2Double == 5) || (sn3Carbon == 18 && sn3Double == 5)) return null;

                    var nl_SN1 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) - H2O + MassDiffDictionary.HydrogenMass;
                    var nl_SN2 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) - H2O + MassDiffDictionary.HydrogenMass;
                    var nl_SN3 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn3Carbon, sn3Double) - H2O + MassDiffDictionary.HydrogenMass;
                    var query = new List<SpectrumPeak> {
                        new() { Mass = nl_SN1, Intensity = 3 },
                        new() { Mass = nl_SN2, Intensity = 3 },
                        new() { Mass = nl_SN3, Intensity = 3 }
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 3)　{                        
                        var molecule = LipidMsmsCharacterizationUtility.getTriacylglycerolMoleculeObjAsLevel2("TG_d5", LbmClass.TG_d5, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, sn3Carbon, sn3Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                    if (candidates == null || candidates.Count == 0) return null;
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("TG_d5", LbmClass.TG_d5, string.Empty, theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 3);
                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {   //add MT
                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    var diagnosticMz = theoreticalMz; // - 22.9892207 + MassDiffDictionary.HydrogenMass; //if want to choose [M+H]+
                    var nl_SN1 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) - H2O + MassDiffDictionary.HydrogenMass;
                    var nl_SN2 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) - H2O + MassDiffDictionary.HydrogenMass;
                    var nl_SN3 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn3Carbon, sn3Double) - H2O + MassDiffDictionary.HydrogenMass;
                    var query = new List<SpectrumPeak> {
                        new() { Mass = nl_SN1, Intensity = 0.1 },
                        new() { Mass = nl_SN2, Intensity = 0.1 },
                        new() { Mass = nl_SN3, Intensity = 0.1 }
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount < 3)　{
                        var diagnosticMzH = theoreticalMz - 22.9892207 + MassDiffDictionary.HydrogenMass;
                        var nl_SN1_H = diagnosticMzH - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) - H2O + MassDiffDictionary.HydrogenMass;
                        var nl_SN2_H = diagnosticMzH - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) - H2O + MassDiffDictionary.HydrogenMass;
                        var nl_SN3_H = diagnosticMzH - LipidMsmsCharacterizationUtility.acylCainMass(sn3Carbon, sn3Double) - H2O + MassDiffDictionary.HydrogenMass;
                        var query2 = new List<SpectrumPeak> {
                            new() { Mass = nl_SN1_H, Intensity = 0.1 },
                            new() { Mass = nl_SN2_H, Intensity = 0.1 },
                            new() { Mass = nl_SN3_H, Intensity = 0.1 }
                        };

                        var foundCount2 = 0;
                        var averageIntensity2 = 0.0;
                        LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query2, ms2Tolerance, out foundCount2, out averageIntensity2);

                        if (foundCount2 == 3)　{
                            var molecule = LipidMsmsCharacterizationUtility.getTriacylglycerolMoleculeObjAsLevel2("TG_d5", LbmClass.TG_d5, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, sn3Carbon, sn3Double, averageIntensity2);
                            candidates.Add(molecule);
                        }
                    }
                    else　if (foundCount == 3)　{ 
                        // these three chains must be observed.
                        var molecule = LipidMsmsCharacterizationUtility.getTriacylglycerolMoleculeObjAsLevel2("TG_d5", LbmClass.TG_d5, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, sn3Carbon, sn3Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                    if (candidates == null || candidates.Count == 0) return null;
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("TG_d5", LbmClass.TG_d5, string.Empty, theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 3);
                }
            }
            return null;
        }
        public static LipidMolecule JudgeIfSphingomyelinD9(IMSScanProperty msScanProp, double ms2Tolerance,　double theoreticalMz, int totalCarbon, int totalDoubleBond, 
        int sphCarbon, int acylCarbon, int sphDouble, int acylDouble,　AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            var C5H5D9NO4P = new[] {
                MassDiffDictionary.CarbonMass * 5,
                MassDiffDictionary.HydrogenMass * 5,
                MassDiffDictionary.NitrogenMass,
                MassDiffDictionary.OxygenMass * 4,
                MassDiffDictionary.PhosphorusMass,
                MassDiffDictionary.Hydrogen2Mass * 9,
            }.Sum();

            var C2H2N = 12 * 2 + MassDiffDictionary.HydrogenMass * 2 + MassDiffDictionary.NitrogenMass;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            {
                if (adduct.AdductIonName == "[M+H]+")
                {
                    // seek 184.07332 (C5H15NO4P) D9
                    var threshold = 1.0;
                    var diagnosticMz = C5H5D9NO4P + MassDiffDictionary.ProtonMass;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    //if (!isClassIonFound || !isClassIonFound1 || !isClassIonFound2) return null;
                    if (!isClassIonFound) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    if (sphCarbon <= 13) return null;
                    if (sphCarbon == 16 && sphDouble >= 3) return null;
                    if (acylCarbon < 8) return null;

                    var diagnosChain1 = LipidMsmsCharacterizationUtility.acylCainMass(acylCarbon, acylDouble) + C2H2N + MassDiffDictionary.HydrogenMass + Proton;
                    var diagnosChain2 = diagnosChain1 + C5H5D9NO4P - MassDiffDictionary.HydrogenMass;
                    var query = new List<SpectrumPeak> {
                        new() { Mass = diagnosChain1, Intensity = 0.5 },
                        new() { Mass = diagnosChain2, Intensity = 1.0 },
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2) { 
                        var molecule = LipidMsmsCharacterizationUtility.getCeramideMoleculeObjAsLevel2("SM_d9", LbmClass.SM_d9, "d", sphCarbon, sphDouble,
                            acylCarbon, acylDouble, averageIntensity);
                        candidates.Add(molecule);
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("SM_d9", LbmClass.SM_d9, "d", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);

                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek -59.0735 [M-C3H9N+Na]+
                    var threshold = 20.0;
                    var diagnosticMz = theoreticalMz - 59.0735;
                    // seek C5H15NO4P + Na+
                    var threshold2 = 30.0;
                    var diagnosticMz2 = C5H5D9NO4P + Na;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    if (isClassIonFound == false || isClassIon2Found == false) return null;

                    var candidates = new List<LipidMolecule>();
                    // from here, acyl level annotation is executed.
                    for (int sphCarbonNum = 6; sphCarbonNum <= totalCarbon; sphCarbonNum++) {
                        for (int sphDoubleNum = 0; sphDoubleNum <= totalDoubleBond; sphDoubleNum++) {
                            var acylCarbonNum = totalCarbon - sphCarbonNum;
                            var acylDoubleNum = totalDoubleBond - sphDoubleNum;
                            if (acylDoubleNum >= 7) continue;

                            var diagnosChain1 = LipidMsmsCharacterizationUtility.acylCainMass(acylCarbonNum, acylDoubleNum) + C2H2N + MassDiffDictionary.HydrogenMass + MassDiffDictionary.ProtonMass;
                            var diagnosChain2 = diagnosChain1 + C5H5D9NO4P - MassDiffDictionary.HydrogenMass;

                            var query = new List<SpectrumPeak> {
                                new() { Mass = diagnosChain1, Intensity = 20.0 },
                                new() { Mass = diagnosChain2, Intensity = 20.0 },
                            };
                            var foundCount = 0;
                            var averageIntensity = 0.0;
                            LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);
                            if (foundCount == 2) {
                                var molecule = LipidMsmsCharacterizationUtility.getCeramideMoleculeObjAsLevel2("SM_d9", LbmClass.SM_d9, "d", sphCarbon, sphDouble,
                                   acylCarbon, acylDouble, averageIntensity);
                                candidates.Add(molecule);
                            }
                        }
                    }
                    if (candidates == null || candidates.Count == 0) return null;
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("SM_d9", LbmClass.SM_d9, "d", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            } else {
                if (adduct.AdductIonName == "[M+FA-H]-" || adduct.AdductIonName == "[M+Hac-H]-" ||
                    adduct.AdductIonName == "[M+HCOO]-" || adduct.AdductIonName == "[M+CH3COO]-")
                {
                    // seek [M-CH3]-
                    var threshold1 = 50.0;
                    var threshold2 = 0.01;
                    // seek [M-CD3]-
                    var diagnosticMz1 = adduct.AdductIonName == "[M+CH3COO]-" || adduct.AdductIonName == "[M+Hac-H]-" ?
                        theoreticalMz - 77.060255 : theoreticalMz - 60.021129369;
                    var diagnosticMz2 = 174.089522;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold1);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    if (isClassIon1Found != true || isClassIon2Found != true) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();

                    var sphFragment = diagnosticMz1 - LipidMsmsCharacterizationUtility.acylCainMass(acylCarbon, acylDouble) + MassDiffDictionary.HydrogenMass;
                    var query = new List<SpectrumPeak> { new() { Mass = sphFragment, Intensity = 0.01 } };
                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 1) {
                        var molecule = LipidMsmsCharacterizationUtility.getCeramideMoleculeObjAsLevel2("SM_d9", LbmClass.SM_d9, "d", sphCarbon, sphDouble,
                            acylCarbon, acylDouble, averageIntensity);
                        candidates.Add(molecule);
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("SM_d9", LbmClass.SM_d9, "d", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfCeramidensD7(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond, 
            int sphCarbon, int acylCarbon, int sphDouble, int acylDouble, AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            {
                var adductform = adduct.AdductIonName;
                if (adductform == "[M+H]+" || adductform == "[M+H-H2O]+")
                {
                    // seek -H2O
                    var threshold = 5.0;
                    var diagnosticMz = adductform == "[M+H]+" ? theoreticalMz - H2O : theoreticalMz;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    if (acylDouble >= 7) return null;
                    var sph1 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(acylCarbon, acylDouble) + MassDiffDictionary.HydrogenMass;
                    var sph2 = sph1 - H2O;
                    var sph3 = sph2 - 12; //[Sph-CH4O2+H]+
                    var acylamide = acylCarbon * 12 + (((2 * acylCarbon) - (2 * acylDouble) + 2) * MassDiffDictionary.HydrogenMass) + MassDiffDictionary.OxygenMass + MassDiffDictionary.NitrogenMass;

                    // must query
                    var queryMust = new List<SpectrumPeak> { new() { Mass = sph2, Intensity = 5 } };
                    var foundCountMust = 0;
                    var averageIntensityMust = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, queryMust, ms2Tolerance, out foundCountMust, out averageIntensityMust);
                    if (foundCountMust == 0) return null;

                    var query = new List<SpectrumPeak> {
                        new() { Mass = sph1, Intensity = 1 },
                        new() { Mass = sph3, Intensity = 1 },
                    };
                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);
                    var foundCountThresh = acylCarbon < 12 ? 2 : 1; // to exclude strange annotation

                    if (foundCount >= foundCountThresh) { 
                        var molecule = LipidMsmsCharacterizationUtility.getCeramideMoleculeObjAsLevel2("Cer_d7", LbmClass.Cer_NS_d7, "d", sphCarbon, sphDouble,
                            acylCarbon, acylDouble, averageIntensity);
                        candidates.Add(molecule);
                    }
                    if (candidates.Count == 0) return null;
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("Cer_d7", LbmClass.Cer_NS_d7, "d", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
                else if (adductform == "[M+Na]+")
                {
                    // reject HexCer
                    var threshold = 1.0;
                    var diagnosticMz = theoreticalMz - 162.052833 - H2O;
                    if (LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold)) { return null; }
                    var candidates = new List<LipidMolecule>();
                    var sph1 = LipidMsmsCharacterizationUtility.SphingoChainMass(sphCarbon, sphDouble) + MassDiffDictionary.HydrogenMass - MassDiffDictionary.OxygenMass + (MassDiffDictionary.HydrogenMass * 7);
                    var sph3 = sph1 - H2O + Proton;

                    var query = new List<SpectrumPeak> { new() { Mass = sph3, Intensity = 1 } };
                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 1) {
                        var molecule = LipidMsmsCharacterizationUtility.getCeramideMoleculeObjAsLevel2("Cer_d7", LbmClass.Cer_NS_d7, "d", sphCarbon, sphDouble,
                            acylCarbon, acylDouble, averageIntensity);
                        candidates.Add(molecule);
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("Cer_d7", LbmClass.Cer_NS_d7, "d", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            } else {
                if (adduct.AdductIonName == "[M-H]-" || adduct.AdductIonName == "[M+FA-H]-" || adduct.AdductIonName == "[M+Hac-H]-" ||
                    adduct.AdductIonName == "[M+HCOO]-" || adduct.AdductIonName == "[M+CH3COO]-")
                {
                    // calc [M-H]-
                    var diagnosticMz = adduct.AdductIonName == "[M-H]-" ? theoreticalMz :
                        adduct.AdductIonName == "[M+CH3COO]-" || adduct.AdductIonName == "[M+Hac-H]-" ?
                        theoreticalMz - MassDiffDictionary.HydrogenMass - 59.013864 : theoreticalMz - MassDiffDictionary.HydrogenMass - 44.998214;
                    // seek [M-CH2O-H]-
                    var threshold1 = 0;
                    var diagnosticMz1 = diagnosticMz - 12 - H2O;
                    // seek [M-CH2O-H2O-H]-
                    var threshold2 = 0;
                    var diagnosticMz2 = diagnosticMz1 - H2O;

                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold1);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    if (isClassIon1Found != true && isClassIon2Found != true) return null;

                    if (adduct.AdductIonName == "[M+CH3COO]-" || adduct.AdductIonName == "[M+Hac-H]-") {
                        var diagnosticMz3 = theoreticalMz - MassDiffDictionary.HydrogenMass - 44.998214;
                        var threshold3 = 50.0;
                        var isClassIon3Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz3, threshold3);
                        if (isClassIon3Found) return null;
                    }

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();

                    var sphChain_loss = diagnosticMz - ((sphCarbon - 2) * 12) - (MassDiffDictionary.HydrogenMass * ((sphCarbon - 2) * 2 - sphDouble * 2 + 1 + (MassDiffDictionary.HydrogenMass * 7))) -
                             2 * MassDiffDictionary.OxygenMass - MassDiffDictionary.HydrogenMass; // as [FA+NCCO-3H]- on Excel(may be not sure)
                    var sphFragment = ((sphCarbon - 2) * 12) + (MassDiffDictionary.HydrogenMass * ((sphCarbon - 2) * 2 - sphDouble * 2) - 1) + MassDiffDictionary.OxygenMass + (MassDiffDictionary.HydrogenMass * 7); // [Sph-NCC-3H]-
                    var acylFragment = LipidMsmsCharacterizationUtility.fattyacidProductIon(acylCarbon, acylDouble) - MassDiffDictionary.OxygenMass - 2 * MassDiffDictionary.HydrogenMass; // 

                    var query = new List<SpectrumPeak> {
                        new() { Mass = sphChain_loss, Intensity = 5 },
                        new() { Mass = sphFragment, Intensity = 1 },
                        new() { Mass = acylFragment, Intensity = 1 }
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount >= 2) { 
                        var molecule = LipidMsmsCharacterizationUtility.getCeramideMoleculeObjAsLevel2("Cer_d7", LbmClass.Cer_NS_d7, "d", sphCarbon, sphDouble,
                            acylCarbon, acylDouble, averageIntensity);
                        candidates.Add(molecule);
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("Cer_d7", LbmClass.Cer_NS_d7, "d", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            return null;
        }
        public static LipidMolecule JudgeIfCholesterylEsterD7(IMSScanProperty msScanProp, double ms2Tolerance, float theoreticalMz,
        int totalCarbon, int totalDoubleBond, AdductIon adduct)
        {
            double skelton = new[] {
                MassDiffDictionary.CarbonMass * 27,
                MassDiffDictionary.HydrogenMass * 46,
                MassDiffDictionary.OxygenMass * 1,
                MassDiffDictionary.Hydrogen2Mass * 7,
                - MassDiffDictionary.HydrogenMass * 7,
            }.Sum();
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            {
                if (adduct.AdductIonName == "[M+NH4]+")
                {
                    // seek 369.3515778691 (C27H45+)+ MassDiffDictionary.HydrogenMass*7
                    var threshold = 1.0;
                    var diagnosticMz = skelton - H2O + Proton;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;
                    if (totalCarbon >= 41 && totalDoubleBond >= 4) return null;

                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("CE_d7", LbmClass.CE_d7, string.Empty, theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek 368.3515778691 (C27H44)+ MassDiffDictionary.HydrogenMass*7
                    var threshold = 10.0;
                    var diagnosticMz = skelton - H2O;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("CE_d7", LbmClass.CE_d7, string.Empty, theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            return null;
        }
        public static LipidMolecule JudgeIfDgts(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond, 
        int minSnCarbon, int maxSnCarbon, int minSnDoubleBond, int maxSnDoubleBond, AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (maxSnCarbon > totalCarbon) maxSnCarbon = totalCarbon;
            if (maxSnDoubleBond > totalDoubleBond) maxSnDoubleBond = totalDoubleBond;
            if (adduct.IonMode == IonMode.Positive)
            {
                if (adduct.AdductIonName == "[M+H]+")
                {
                    double Gly_C = new[] {
                        MassDiffDictionary.CarbonMass * 10,
                        MassDiffDictionary.HydrogenMass * 19,
                        MassDiffDictionary.NitrogenMass,
                        MassDiffDictionary.OxygenMass * 3,
                        Proton
                    }.Sum();

                    double Gly_O = new[] {
                        MassDiffDictionary.CarbonMass * 9,
                        MassDiffDictionary.HydrogenMass * 17,
                        MassDiffDictionary.NitrogenMass,
                        MassDiffDictionary.OxygenMass * 4,
                        Proton
                    }.Sum();
                    var threshold = 1.0;
                    var isClassIonFound1 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_C, threshold);
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_O, threshold);
                    if (!isClassIonFound1 && !isClassIonFound2) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    for (int sn1Carbon = minSnCarbon; sn1Carbon <= maxSnCarbon; sn1Carbon++) {
                        for (int sn1Double = minSnDoubleBond; sn1Double <= maxSnDoubleBond; sn1Double++) {
                            var sn2Carbon = totalCarbon - sn1Carbon;
                            var sn2Double = totalDoubleBond - sn1Double;

                            var nl_SN1 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) + MassDiffDictionary.HydrogenMass;
                            var nl_SN1_H2O = nl_SN1 - H2O;
                            var nl_SN2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) + MassDiffDictionary.HydrogenMass;
                            var nl_NS2_H2O = nl_SN2 - H2O;
                            var query = new List<SpectrumPeak> {
                                new() { Mass = nl_SN1, Intensity = 0.01 },
                                new() { Mass = nl_SN1_H2O, Intensity = 0.01 },
                                new() { Mass = nl_SN2, Intensity = 0.01 },
                                new() { Mass = nl_NS2_H2O, Intensity = 0.01 }
                            };
                            var foundCount = 0;
                            var averageIntensity = 0.0;
                            LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                            if (foundCount >= 2) {
                                var DgtsFrag = 130.0862; //130.0862 (C6H12NO2+)DGTS
                                var DgtaFrag = 144.10191; //144.10191 (C7H14NO2+) DGTA

                                var molecule = new LipidMolecule();
                                if (LipidMsmsCharacterizationUtility.isFragment1GreaterThanFragment2(spectrum, ms2Tolerance, DgtsFrag, DgtaFrag)) {
                                    molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("DGTS", LbmClass.DGTS, sn1Carbon, sn1Double,
                                        sn2Carbon, sn2Double, averageIntensity);
                                } else {
                                    molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("DGTA", LbmClass.DGTA, sn1Carbon, sn1Double,
                                        sn2Carbon, sn2Double, averageIntensity);
                                }
                                candidates.Add(molecule);
                            }
                        }
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("DGTS", LbmClass.DGTS, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfLdgts(IMSScanProperty msScanProp, double ms2Tolerance, double theoreticalMz, int totalCarbon, int totalDoubleBond, 
            int minSnCarbon, int maxSnCarbon, int minSnDoubleBond, int maxSnDoubleBond, AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (maxSnCarbon > totalCarbon) maxSnCarbon = totalCarbon;
            if (maxSnDoubleBond > totalDoubleBond) maxSnDoubleBond = totalDoubleBond;
            if (adduct.IonMode == IonMode.Positive)
            {
                if (adduct.AdductIonName == "[M+H]+")
                {
                    double Gly_C = new[] {
                        MassDiffDictionary.CarbonMass * 10,
                        MassDiffDictionary.HydrogenMass * 19,
                        MassDiffDictionary.NitrogenMass,
                        MassDiffDictionary.OxygenMass * 3,
                        Proton
                    }.Sum();

                    double Gly_O = new[] {
                        MassDiffDictionary.CarbonMass * 9,
                        MassDiffDictionary.HydrogenMass * 17,
                        MassDiffDictionary.NitrogenMass,
                        MassDiffDictionary.OxygenMass * 4,
                        Proton
                    }.Sum();
                    var threshold = 1.0;
                    var isClassIonFound1 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_C, threshold);
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_O, threshold);
                    if (!isClassIonFound1 && !isClassIonFound2) return null;

                    var candidates = new List<LipidMolecule>();
                    var DgtsFrag = 130.0862; //130.0862 (C6H12NO2+)DGTS
                    var DgtaFrag = 144.10191; //144.10191 (C7H14NO2+) DGTA

                    if (LipidMsmsCharacterizationUtility.isFragment1GreaterThanFragment2(spectrum, ms2Tolerance, DgtsFrag, DgtaFrag)) {
                        return LipidMsmsCharacterizationUtility.returnAnnotationResult("LDGTS", LbmClass.LDGTS, "", theoreticalMz, adduct,
                             totalCarbon, totalDoubleBond, 0, candidates, 1);
                    } else {
                        return LipidMsmsCharacterizationUtility.returnAnnotationResult("LDGTA", LbmClass.LDGTA, "", theoreticalMz, adduct,
                             totalCarbon, totalDoubleBond, 0, candidates, 1);
                    }
                }
            }
            if (adduct.AdductIonName == "[M+FA-H]-" || adduct.AdductIonName == "[M+Hac-H]-" ||
                    adduct.AdductIonName == "[M+HCOO]-" || adduct.AdductIonName == "[M+CH3COO]-")
            {
                // calc [M-H]- 
                var diagnosticMz = adduct.AdductIonName == "[M+CH3COO]-" || adduct.AdductIonName == "[M+Hac-H]-" ?
                    theoreticalMz - 60.02167792 : theoreticalMz - 46.00602785;
                //seek [M-C3H5]-
                var threshold = 50.0;
                var diagnosticMz2 = diagnosticMz - 12 * 3 - MassDiffDictionary.HydrogenMass * 5;
                var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold);
                if (isClassIonFound == false) return null;
                // from here, acyl level annotation is executed.
                var candidates = new List<LipidMolecule>();
                for (int sn1Carbon = minSnCarbon; sn1Carbon <= maxSnCarbon; sn1Carbon++)
                {
                    for (int sn1Double = minSnDoubleBond; sn1Double <= maxSnDoubleBond; sn1Double++)
                    {
                        var sn2Carbon = totalCarbon - sn1Carbon;
                        var sn2Double = totalDoubleBond - sn1Double;
                        var SN1 = LipidMsmsCharacterizationUtility.fattyacidProductIon(sn1Carbon, sn1Double);

                        var query = new List<SpectrumPeak> { new() { Mass = SN1, Intensity = 0.01 } };
                        var foundCount = 0;
                        var averageIntensity = 0.0;
                        LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);
                        if (foundCount >= 1) {
                            return LipidMsmsCharacterizationUtility.returnAnnotationResult("LDGTS", LbmClass.LDGTS, "", theoreticalMz, adduct,
                               totalCarbon, totalDoubleBond, 0, candidates, 1);
                        }
                    }
                }
            }
            return null;
        }
    }
}
