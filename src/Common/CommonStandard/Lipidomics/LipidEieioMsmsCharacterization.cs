using System.Linq;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace CompMs.Common.Lipidomics
{
    public sealed class LipidEieioMsmsCharacterization
    {
        private LipidEieioMsmsCharacterization() { }

        private const double Electron = 0.00054858026;
        private const double Proton = 1.00727641974;
        private const double H2O = 18.010564684;
        private const double Sugar162 = 162.052823422;
        private const double Na = 22.98977;

        public static LipidMolecule JudgeIfPhosphatidylcholine(IMSScanProperty msScanProp, double ms2Tolerance,
           double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PC 46:6, totalCarbon = 46 and totalDoubleBond = 6
           int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double,
           AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            var candidates = new List<LipidMolecule>();
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
            double C5H15NO4P = new[] {
                        MassDiffDictionary.CarbonMass * 5,
                        MassDiffDictionary.HydrogenMass * 14,
                        MassDiffDictionary.NitrogenMass,
                        MassDiffDictionary.OxygenMass * 4,
                        MassDiffDictionary.PhosphorusMass,
                        Proton
                    }.Sum();

            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    if (sn1Carbon < 10 || sn2Carbon < 10) return null;
                    if (sn1Double > 6 || sn2Double > 6) return null;
                    // 
                    var threshold = 3.0;
                    var isClassIonFound1 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, C5H15NO4P, threshold);
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_C, threshold);
                    var isClassIonFound3 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_O, threshold);
                    if (!isClassIonFound1 || !isClassIonFound2 || !isClassIonFound3) return null;
                    // reject Na+ adduct
                    var diagnosticMz3 = theoreticalMz - 59.0735;
                    var isNaTypicalFound1 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz3, 10.0);
                    if (isNaTypicalFound1)
                    {
                        return null;
                    }


                    // for eieio
                    var PEHeaderLoss = theoreticalMz - 141.019094261;
                    var isClassIonFoundPe = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, PEHeaderLoss, 5.0);
                    //if (isClassIonFound2 && LipidMsmsCharacterizationUtility.isFragment1GreaterThanFragment2(spectrum, ms2Tolerance, PEHeaderLoss, diagnosticMz))
                    if (isClassIonFoundPe)
                    {
                        return null;
                    }

                    // from here, acyl level annotation is executed.
                    var nl_SN1 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) + MassDiffDictionary.HydrogenMass;
                    var nl_SN1_H2O = nl_SN1 - H2O;

                    var nl_SN2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) + MassDiffDictionary.HydrogenMass;
                    var nl_NS2_H2O = nl_SN2 - H2O;

                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = nl_SN1, Intensity = 0.1 },
                                new SpectrumPeak() { Mass = nl_SN1_H2O, Intensity = 0.1 },
                                new SpectrumPeak() { Mass = nl_SN2, Intensity = 0.1 },
                                new SpectrumPeak() { Mass = nl_NS2_H2O, Intensity = 0.1 }
                            };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount >= 2)
                    { // now I set 2 as the correct level
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PC", LbmClass.PC, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                    else
                    {
                        return null;
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PC", LbmClass.PC, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
                //addMT
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    //// seek 184.07332 (C5H15NO4P)
                    //var diagnosticMz = 184.07332;
                    // seek [M+Na -C5H14NO4P]+
                    var diagnosticMz2 = theoreticalMz - 183.06604;
                    // seek [M+Na -C3H9N]+
                    var diagnosticMz3 = theoreticalMz - 59.0735;
                    var threshold = 3.0;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz3, threshold);
                    if (isClassIonFound == false || isClassIon2Found == false) return null;

                    // from here, acyl level annotation is executed.
                    for (int sn1CarbonNum = 6; sn1CarbonNum <= totalCarbon; sn1CarbonNum++)
                    {
                        for (int sn1DoubleNum = 0; sn1DoubleNum <= totalDoubleBond; sn1DoubleNum++)
                        {
                            var sn2CarbonNum = totalCarbon - sn1CarbonNum;
                            var sn2DoubleNum = totalDoubleBond - sn1DoubleNum;
                            if (sn2DoubleNum >= 7) continue;

                            var nl_SN1 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1CarbonNum, sn1DoubleNum);
                            var nl_SN1_H2O = nl_SN1 - H2O;
                            var nl_SN2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2CarbonNum, sn2DoubleNum);
                            var nl_SN2_H2O = nl_SN2 - H2O;

                            var query = new List<SpectrumPeak>
                            {
                                new SpectrumPeak() { Mass = nl_SN1, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = nl_SN1_H2O, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = nl_SN2, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = nl_SN2_H2O, Intensity = 0.01 }
                            };
                            var foundCount = 0;
                            var averageIntensity = 0.0;
                            LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);
                            if (foundCount >= 2)
                            {
                                var molecule = LipidMsmsCharacterizationUtility.getDiacylglycerolMoleculeObjAsLevel2("PC", LbmClass.PC, sn1CarbonNum, sn1DoubleNum,
                                sn2CarbonNum, sn2DoubleNum,
                                averageIntensity);
                                candidates.Add(molecule);
                            }
                        }
                    }
                    if (candidates == null || candidates.Count == 0)
                        return null;

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PC", LbmClass.PC, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfPhosphatidylethanolamine(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PE 46:6, totalCarbon = 46 and totalDoubleBond = 6
            int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double,
            AdductIon adduct)
        {

            var spectrum = msScanProp.Spectrum;
            var candidates = new List<LipidMolecule>();

            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
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
                    double C2H8NO4P = new[]
                    {
                        MassDiffDictionary.CarbonMass * 2,
                        MassDiffDictionary.HydrogenMass * 8,
                        MassDiffDictionary.NitrogenMass,
                        MassDiffDictionary.OxygenMass * 4,
                        MassDiffDictionary.PhosphorusMass,
                    }.Sum();
                    // seek -141.019094261 (C2H8NO4P)
                    var threshold = 2.5;
                    var diagnosticMz = theoreticalMz - C2H8NO4P;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_C, threshold);
                    var isClassIonFound3 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_O, threshold);
                    if (!isClassIonFound || !isClassIonFound2 || !isClassIonFound3) return null;

                    // from here, acyl level annotation is executed.
                    var sn1 = LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) - Electron;
                    var sn2 = LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) - Electron;

                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = sn1, Intensity = 0.1 },
                                new SpectrumPeak() { Mass = sn2, Intensity = 0.1 }
                            };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2)
                    { // now I set 2 as the correct level
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PE", LbmClass.PE, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                    else
                    {
                        return null;
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PE", LbmClass.PE, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
                //addMT
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek -141.019094261 (C2H8NO4P)
                    var threshold = 3.0;
                    var diagnosticMz = theoreticalMz - 141.019094261;
                    // seek - 43.042199 (C2H5N)
                    var diagnosticMz2 = theoreticalMz - 43.042199;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // from here, acyl level annotation is executed.
                    for (int sn1CarbonNum = 6; sn1CarbonNum <= totalCarbon; sn1CarbonNum++)
                    {
                        for (int sn1DoubleNum = 0; sn1DoubleNum <= totalDoubleBond; sn1DoubleNum++)
                        {
                            var sn2CarbonNum = totalCarbon - sn1CarbonNum;
                            var sn2DoubleNum = totalDoubleBond - sn1DoubleNum;
                            if (sn2DoubleNum >= 7) continue;

                            var nl_SN1 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1CarbonNum, sn1DoubleNum) - MassDiffDictionary.HydrogenMass*2;
                            var nl_SN1_H2O = nl_SN1 - H2O;
                            var nl_SN2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2CarbonNum, sn2DoubleNum) - MassDiffDictionary.HydrogenMass * 2;
                            var nl_SN2_H2O = nl_SN2 - H2O;

                            var query = new List<SpectrumPeak>
                            {
                                new SpectrumPeak() { Mass = nl_SN1, Intensity = 0.1 },
                                new SpectrumPeak() { Mass = nl_SN1_H2O, Intensity = 0.1 },
                                new SpectrumPeak() { Mass = nl_SN2, Intensity = 0.1},
                                new SpectrumPeak() { Mass = nl_SN2_H2O, Intensity = 0.1 }
                            };
                            var foundCount = 0;
                            var averageIntensity = 0.0;
                            LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);
                            if (foundCount >= 2)
                            {
                                var molecule = LipidMsmsCharacterizationUtility.getDiacylglycerolMoleculeObjAsLevel2("PE", LbmClass.PE, sn1CarbonNum, sn1DoubleNum,
                                sn2CarbonNum, sn2DoubleNum,
                                averageIntensity);
                                candidates.Add(molecule);
                            }
                        }
                    }
                    if (candidates == null || candidates.Count == 0)
                        return null;

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PE", LbmClass.PE, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }

            return null;
        }

        public static LipidMolecule JudgeIfPhosphatidylglycerol(IMSScanProperty msScanProp, double ms2Tolerance,
           double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PS 46:6, totalCarbon = 46 and totalDoubleBond = 6
           int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double,
           AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            var candidates = new List<LipidMolecule>();

            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+NH4]+")
                {
                    // seek -189.040227 (C3H8O6P+NH4)
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - 189.040227;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // from here, acyl level annotation is executed.
                    var nl_SN1 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) + MassDiffDictionary.HydrogenMass;
                    var nl_SN2 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) + MassDiffDictionary.HydrogenMass;

                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = nl_SN1, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = nl_SN2, Intensity = 0.01 }
                            };

                    if (LipidMsmsCharacterizationUtility.isFragment1GreaterThanFragment2(spectrum, ms2Tolerance, nl_SN1, diagnosticMz) &&
                        LipidMsmsCharacterizationUtility.isFragment1GreaterThanFragment2(spectrum, ms2Tolerance, nl_SN2, diagnosticMz))
                    {
                        // meaning high possibility that the spectrum belongs to BMP
                        return null;
                    }

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2)
                    {
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PG", LbmClass.PG, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PG", LbmClass.PG, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek -171.005851 (C3H8O6P) - 22.9892207 (Na+)
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - 171.005851 - 22.9892207;// + MassDiffDictionary.HydrogenMass;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // from here, acyl level annotation is executed.
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PG", LbmClass.PG, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfBismonoacylglycerophosphate(IMSScanProperty msScanProp, double ms2Tolerance,
           double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PS 46:6, totalCarbon = 46 and totalDoubleBond = 6
           int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double,
           AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+NH4]+")
                {
                    // seek -189.040227 (C3H8O6P+NH4)
                    var threshold = 0.01;
                    var diagnosticMz = theoreticalMz - 189.040227;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    //if (isClassIonFound == false) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    var nl_SN1 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) + MassDiffDictionary.HydrogenMass;
                    var nl_SN2 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) + MassDiffDictionary.HydrogenMass;

                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = nl_SN1, Intensity = 10 },
                                new SpectrumPeak() { Mass = nl_SN2, Intensity = 10 }
                            };

                    if (LipidMsmsCharacterizationUtility.isFragment1GreaterThanFragment2(spectrum, ms2Tolerance, diagnosticMz, nl_SN1) &&
                        LipidMsmsCharacterizationUtility.isFragment1GreaterThanFragment2(spectrum, ms2Tolerance, diagnosticMz, nl_SN2))
                    {
                        // meaning high possibility that the spectrum belongs to PG
                        return null;
                    }

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2)
                    {
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

        public static LipidMolecule JudgeIfLysopc(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // 
            int snCarbon, int snDoubleBond,
            AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (snCarbon > totalCarbon) snCarbon = totalCarbon;
            if (snDoubleBond > totalDoubleBond) snDoubleBond = totalDoubleBond;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    if (totalCarbon > 28) return null; //  currently carbon > 28 is recognized as EtherPC
                    // seek 184.07332 (C5H15NO4P)
                    var threshold = 3.0;
                    var diagnosticMz = 184.07332;
                    var diagnosticMz2 = 104.106990;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIon1Found != true) return null;
                    // reject Na+ adduct
                    var diagnosticMz3 = theoreticalMz - 59.0735;
                    var isNaTypicalFound1 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz3, 10.0);
                    if (isNaTypicalFound1)
                    {
                        return null;
                    }

                    // for eieio
                    var PEHeaderLoss = theoreticalMz - 141.019094261 + MassDiffDictionary.ProtonMass;
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, PEHeaderLoss, 3.0);
                    if (isClassIonFound2 && LipidMsmsCharacterizationUtility.isFragment1GreaterThanFragment2(spectrum, ms2Tolerance, PEHeaderLoss, diagnosticMz))
                    {
                        return null;
                    }

                    var candidates = new List<LipidMolecule>();
                    var chainSuffix = "";
                    var diagnosticMzExist = 0.0;
                    var diagnosticMzIntensity = 0.0;
                    var diagnosticMzExist2 = 0.0;
                    var diagnosticMzIntensity2 = 0.0;

                    for (int i = 0; i < spectrum.Count; i++)
                    {
                        var mz = spectrum[i].Mass;
                        var intensity = spectrum[i].Intensity;

                        if (intensity > threshold && Math.Abs(mz - diagnosticMz) < ms2Tolerance)
                        {
                            diagnosticMzExist = mz;
                            diagnosticMzIntensity = intensity;
                        }
                        else if (intensity > threshold && Math.Abs(mz - diagnosticMz2) < ms2Tolerance)
                        {
                            diagnosticMzExist2 = mz;
                            diagnosticMzIntensity2 = intensity;
                        }
                    };

                    if (diagnosticMzIntensity2 / diagnosticMzIntensity > 0.3) //
                    {
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
            }

            return null;
        }

        public static LipidMolecule JudgeIfLysope(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // 
            int sn1Carbon, int sn1Double,
            AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
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
                    var threshold = 2.5;
                    var diagnosticMz = theoreticalMz - C2H8NO4P;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_C, threshold);
                    var isClassIonFound3 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_O, threshold);
                    if (!isClassIonFound || !isClassIonFound2 || !isClassIonFound3) return null;

                    //// seek PreCursor -141(C2H8NO4P)
                    //var threshold = 2.5;
                    //var diagnosticMz = theoreticalMz - 141.019094;

                    //var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    //if (isClassIon1Found == false) return null;
                    var sn1alkyl = (MassDiffDictionary.CarbonMass * sn1Carbon)
                                        + (MassDiffDictionary.HydrogenMass * ((sn1Carbon * 2) - (sn1Double * 2) + 1));//sn1(ether)

                    var NL_sn1 = diagnosticMz - sn1alkyl + Proton;
                    var sn1_rearrange = sn1alkyl + MassDiffDictionary.HydrogenMass * 2 + 139.00290;//sn1(ether) + C2H5NO4P + proton 

                    // reject EtherPE 
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, NL_sn1, threshold);
                    var isClassIon3Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, sn1_rearrange, threshold);
                    if (isClassIon2Found == true || isClassIon3Found == true) return null;

                    var candidates = new List<LipidMolecule>();
                    if (totalCarbon > 30)
                    {
                        return LipidMsmsCharacterizationUtility.returnAnnotationResult("PE", LbmClass.EtherPE, "e", theoreticalMz, adduct,
                           totalCarbon, totalDoubleBond + 1, 0, candidates, 1);
                    }
                    else
                    {
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
                    if (totalCarbon > 30)
                    {
                        return LipidMsmsCharacterizationUtility.returnAnnotationResult("PE", LbmClass.EtherPE, "e", theoreticalMz, adduct,
                           totalCarbon, totalDoubleBond + 1, 0, candidates, 2);
                    }
                    else
                    {
                        return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPE", LbmClass.LPE, "", theoreticalMz, adduct,
                           totalCarbon, totalDoubleBond, 0, candidates, 1);
                    }
                }
            }

            return null;
        }
        public static LipidMolecule JudgeIfLysopg(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PE 46:6, totalCarbon = 46 and totalDoubleBond = 6
            int snCarbon, int snDoubleBond,
            AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Negative)
            { //
                if (adduct.AdductIonName == "[M-H]-")
                {
                    // seek C3H6O5P-
                    var diagnosticMz1 = 152.99583;  // seek C3H6O5P-
                    var threshold1 = 1.0;
                    var diagnosticMz2 = LipidMsmsCharacterizationUtility.fattyacidProductIon(totalCarbon, totalDoubleBond); // seek [FA-H]-
                    var threshold2 = 10.0;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold1);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    if (isClassIon1Found != true || isClassIon2Found != true) return null;

                    //
                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPG", LbmClass.LPG, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            else if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    // seek Header loss (MG+ + chain Acyl) 
                    var threshold = 5.0;
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

        public static LipidMolecule JudgeIfLysopi(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PE 46:6, totalCarbon = 46 and totalDoubleBond = 6
            int snCarbon, int snDoubleBond,
            AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Negative)
            { //negative ion mode only
                if (adduct.AdductIonName == "[M-H]-")
                {
                    // seek C3H6O5P-
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

                    //
                    var candidates = new List<LipidMolecule>();
                    //var averageIntensity = 0.0;

                    //var molecule = getSingleacylchainMoleculeObjAsLevel2("LPI", LbmClass.LPI, totalCarbon, totalDoubleBond,
                    //averageIntensity);
                    //candidates.Add(molecule);
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPI", LbmClass.LPI, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            else if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    // seek Header loss (MG+ + chain Acyl) 
                    var threshold = 5.0;
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
        public static LipidMolecule JudgeIfLysops(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PE 46:6, totalCarbon = 46 and totalDoubleBond = 6
            int snCarbon, int snDoubleBond,
            AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Negative)
            { //negative ion mode only
                if (adduct.AdductIonName == "[M-H]-")
                {
                    // seek C3H6O5P-

                    var diagnosticMz1 = 152.99583;  // seek C3H6O5P-
                    var threshold1 = 10.0;
                    var diagnosticMz2 = theoreticalMz - 87.032029; // seek -C3H6NO2-H
                    var threshold2 = 5.0;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold1);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    if (isClassIon1Found != true || isClassIon2Found != true) return null;

                    //
                    var candidates = new List<LipidMolecule>();
                    //var averageIntensity = 0.0;

                    //var molecule = getSingleacylchainMoleculeObjAsLevel2("LPS", LbmClass.LPS, totalCarbon, totalDoubleBond,
                    //averageIntensity);
                    //candidates.Add(molecule);
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPS", LbmClass.LPS, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            else if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    // seek Header loss (MG+ + chain Acyl) 
                    var threshold = 5.0;
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

        public static LipidMolecule JudgeIfSphingomyelin(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PC 46:6, totalCarbon = 46 and totalDoubleBond = 6
            int sphCarbon, int acylCarbon, int sphDouble, int acylDouble,
            AdductIon adduct)
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
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    // seek 184.07332 (C5H15NO4P+)
                    var threshold = 10.0;
                    var diagnosticMz = C5H14NO4P + Proton;
                    var diagnosticMz1 = C5H14NO4P + Proton + 12 * 2 + MassDiffDictionary.HydrogenMass * 3 + MassDiffDictionary.NitrogenMass;
                    var diagnosticMz2 = C5H14NO4P + Proton + 12 * 3 + MassDiffDictionary.HydrogenMass * 3 + MassDiffDictionary.NitrogenMass + MassDiffDictionary.OxygenMass;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    var isClassIonFound1 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, 3.0);
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, 3.0);
                    if (!isClassIonFound || !isClassIonFound1 || !isClassIonFound2) return null;
                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    if (sphCarbon <= 13) return null;
                    if (sphCarbon == 16 && sphDouble >= 3) return null;
                    if (acylCarbon < 8) return null;

                    var diagnosChain1 = LipidMsmsCharacterizationUtility.acylCainMass(acylCarbon, acylDouble) + C2H2N + Proton;
                    var diagnosChain2 = diagnosChain1 + C5H14NO4P + Proton - MassDiffDictionary.HydrogenMass;

                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = diagnosChain1, Intensity = 0.5 },
                                new SpectrumPeak() { Mass = diagnosChain2, Intensity = 1.0 },
                            };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2)
                    { // the diagnostic acyl ion must be observed for level 2 annotation
                        var molecule = LipidMsmsCharacterizationUtility.getCeramideMoleculeObjAsLevel2("SM", LbmClass.SM, "d", sphCarbon, sphDouble,
                            acylCarbon, acylDouble, averageIntensity);
                        candidates.Add(molecule);
                    }
                    else
                    {
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
                    for (int sphCarbonNum = 6; sphCarbonNum <= totalCarbon; sphCarbonNum++)
                    {
                        for (int sphDoubleNum = 0; sphDoubleNum <= totalDoubleBond; sphDoubleNum++)
                        {
                            var acylCarbonNum = totalCarbon - sphCarbonNum;
                            var acylDoubleNum = totalDoubleBond - sphDoubleNum;
                            if (acylDoubleNum >= 7) continue;

                            var diagnosChain1 = LipidMsmsCharacterizationUtility.acylCainMass(acylCarbonNum, acylDoubleNum) + C2H2N + MassDiffDictionary.HydrogenMass + Na;
                            var diagnosChain2 = diagnosChain1 + C5H14NO4P - MassDiffDictionary.HydrogenMass;

                            var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = diagnosChain1, Intensity = 20.0 },
                                new SpectrumPeak() { Mass = diagnosChain2, Intensity = 20.0 },
                            };
                            var foundCount = 0;
                            var averageIntensity = 0.0;
                            LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);
                            if (foundCount == 2)
                            {
                                var molecule = LipidMsmsCharacterizationUtility.getCeramideMoleculeObjAsLevel2("SM", LbmClass.SM, "d", sphCarbonNum, sphDoubleNum,
                                acylCarbonNum, acylDoubleNum,
                                averageIntensity);
                                candidates.Add(molecule);
                            }
                        }
                    }
                    if (candidates == null || candidates.Count == 0)
                        return null;

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("SM", LbmClass.SM, "d", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
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
            { // positive ion mode 
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

                    if (!isClassIonFound && !isClassIonFound1) return null;
                    if (!isClassIonFound2 && !isClassIonFound3) return null;

                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("CAR", LbmClass.CAR, string.Empty, theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfEtherpe(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PE 46:6, totalCarbon = 46 and totalDoubleBond = 6
            int Sn1Carbon, int Sn2Carbon, int Sn1Double, int Sn2Double,
            AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
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
                    double C2H8NO4P = new[]
                    {
                        MassDiffDictionary.CarbonMass * 2,
                        MassDiffDictionary.HydrogenMass * 8,
                        MassDiffDictionary.NitrogenMass,
                        MassDiffDictionary.OxygenMass * 4,
                        MassDiffDictionary.PhosphorusMass,
                    }.Sum();
                    // seek -141.019094261 (C2H8NO4P)
                    var threshold = 2.5;
                    var diagnosticMz = theoreticalMz - C2H8NO4P;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_C, threshold);
                    var isClassIonFound3 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_O, threshold);
                    if (!isClassIonFound || !isClassIonFound2 || !isClassIonFound3) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    if (Sn1Double >= 5) return null;
                    var sn1alkyl = (MassDiffDictionary.CarbonMass * Sn1Carbon)
                                + (MassDiffDictionary.HydrogenMass * ((Sn1Carbon * 2) - (Sn1Double * 2) + 1));//sn1(carbon chain)

                    var NL_sn1 = theoreticalMz - sn1alkyl - MassDiffDictionary.OxygenMass;
                    var NL_sn2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(Sn2Carbon, Sn2Double) - H2O;


                    var query = new List<SpectrumPeak> {
                                    new SpectrumPeak() { Mass = NL_sn1, Intensity = 1 },
                                    new SpectrumPeak() { Mass = NL_sn2, Intensity = 1 },
                                };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2)
                    { // now I set 2 as the correct level

                        var etherSuffix = "e";
                        var sn1Double2 = Sn1Double;
                        if (Sn1Double > 0)
                        {
                            sn1Double2 = Sn1Double - 1;
                            etherSuffix = "p";
                        };

                        var molecule = LipidMsmsCharacterizationUtility.getEtherPhospholipidMoleculeObjAsLevel2("PE", LbmClass.EtherPE, Sn1Carbon, sn1Double2,
                            Sn2Carbon, Sn2Double, averageIntensity, etherSuffix);
                        candidates.Add(molecule);
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PE", LbmClass.EtherPE, "e", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            else
            {
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
                            new SpectrumPeak() { Mass = sn2, Intensity = 10.0 },
                            new SpectrumPeak() { Mass = NL_sn2, Intensity = 0.1 }
                        };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2)
                    { // now I set 2 as the correct level
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
        public static LipidMolecule JudgeIfShexcer(IMSScanProperty msScanProp, double ms2Tolerance,
        double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PC 46:6, totalCarbon = 46 and totalDoubleBond = 6
        int sphCarbon, int acylCarbon, int sphDouble, int acylDouble,
        AdductIon adduct, int totalOxidized)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
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
                    if (acylOxidized == 0)
                    {
                        var sph1 = diagnosticMz2 - LipidMsmsCharacterizationUtility.acylCainMass(acylCarbon, acylDouble) + MassDiffDictionary.HydrogenMass;
                        var sph2 = sph1 - H2O;
                        var sph3 = sph2 - 12;
                        var acylamide = acylCarbon * 12 + (((2 * acylCarbon) - (2 * acylDouble) + 2) * MassDiffDictionary.HydrogenMass) + MassDiffDictionary.OxygenMass + MassDiffDictionary.NitrogenMass - Electron;


                        var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = sph1, Intensity = 1 },
                                new SpectrumPeak() { Mass = sph2, Intensity = 1 },
                                new SpectrumPeak() { Mass = sph3, Intensity = 1 },
                               // new SpectrumPeak() { Mass = acylamide, Intensity = 0.01 }
                            };

                        var foundCount = 0;
                        var averageIntensity = 0.0;
                        LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                        if (foundCount >= 1)
                        { // 
                            var molecule = LipidMsmsCharacterizationUtility.getCeramideMoleculeObjAsLevel2("SHexCer", LbmClass.SHexCer, hydrogenString, sphCarbon, sphDouble,
                                acylCarbon, acylDouble, averageIntensity);
                            candidates.Add(molecule);
                        }
                        return LipidMsmsCharacterizationUtility.returnAnnotationResult("SHexCer", LbmClass.SHexCer, hydrogenString, theoreticalMz, adduct,
                            totalCarbon, totalDoubleBond, acylOxidized, candidates, 2);
                    }
                    else
                    {   // case of acyl chain have extra OH
                        var sph1 = diagnosticMz2 - LipidMsmsCharacterizationUtility.acylCainMass(acylCarbon, acylDouble) + MassDiffDictionary.HydrogenMass - MassDiffDictionary.OxygenMass * acylOxidized;
                        var sph2 = sph1 - H2O;
                        var sph3 = sph2 - 12;
                        var acylamide = acylCarbon * 12 + (((2 * acylCarbon) - (2 * acylDouble) + 2) * MassDiffDictionary.HydrogenMass) + MassDiffDictionary.OxygenMass + MassDiffDictionary.NitrogenMass - Electron;

                        //Console.WriteLine("SHexCer {0}:{1};2O/{2}:{3}, sph1={4}, sph2={5}, sph3={6}", sphCarbon, sphDouble, acylCarbon, acylDouble, sph1, sph2, sph3);

                        var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = sph1, Intensity = 1 },
                                new SpectrumPeak() { Mass = sph2, Intensity = 1 },
                                new SpectrumPeak() { Mass = sph3, Intensity = 1 },
                               // new SpectrumPeak() { Mass = acylamide, Intensity = 0.01 }
                            };

                        var foundCount = 0;
                        var averageIntensity = 0.0;
                        LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                        if (foundCount >= 1)
                        { // 
                            var molecule = LipidMsmsCharacterizationUtility.getCeramideoxMoleculeObjAsLevel2("SHexCer", LbmClass.SHexCer, hydrogenString, sphCarbon, sphDouble,
                                acylCarbon, acylDouble, acylOxidized, averageIntensity);
                            candidates.Add(molecule);
                        }
                        return LipidMsmsCharacterizationUtility.returnAnnotationResult("SHexCer", LbmClass.SHexCer, hydrogenString, theoreticalMz, adduct,
                            totalCarbon, totalDoubleBond, acylOxidized, candidates, 2);
                    }
                }
            }
            else
            {
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
                    //var score = 0;
                    //var molecule0 = LipidMsmsCharacterizationUtility.getCeramideMoleculeObjAsLevel2_0("SHexCer", LbmClass.SHexCer, "d", totalCarbon, totalDoubleBond,
                    //    score);
                    //candidates.Add(molecule0);
                    var hydrogenString = "d";
                    var sphOxidized = 2;
                    var acylOxidized = totalOxidized - sphOxidized;

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("SHexCer", LbmClass.SHexCer, hydrogenString, theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, acylOxidized, candidates, 2);
                }

            }
            return null;
        }
        public static LipidMolecule JudgeIfPicermide(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PE 46:6, totalCarbon = 46 and totalDoubleBond = 6
            int sphCarbon, int acylCarbon, int sphDouble, int acylDouble,
            AdductIon adduct, int totalOxdyzed)
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
                //to reject SHexCer
                // seek [M-SO3-H2O+H]+
                var threshold1 = 1.0;
                var diagnosticMz1 = theoreticalMz - MassDiffDictionary.SulfurMass - 3 * MassDiffDictionary.OxygenMass - H2O - Electron;
                var isClassIonFound1 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold1);
                if (isClassIonFound1 == true) return null;


                //   may be not found fragment to define sphingo and acyl chain
                var candidates = new List<LipidMolecule>();

                var hydrogenString = "d";
                var sphOxidized = 2;
                //if (diagnosticMz - (12 * totalCarbon + (totalCarbon * 2 - totalDoubleBond * 2) * MassDiffDictionary.HydrogenMass + MassDiffDictionary.NitrogenMass + MassDiffDictionary.OxygenMass * 3) > 1)
                //{
                //    hydrogenString = "t";
                //    sphOxidized = 3;
                //}
                var acylOxidyzed = totalOxdyzed - sphOxidized;
                return LipidMsmsCharacterizationUtility.returnAnnotationResult("PI-Cer", LbmClass.PI_Cer, hydrogenString, theoreticalMz, adduct,
                    totalCarbon, totalDoubleBond, acylOxidyzed, candidates, 2);
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
        public static LipidMolecule JudgeIfNAcylGlyOxFa(IMSScanProperty msScanProp, double ms2Tolerance,
       double theoreticalMz, int totalCarbon, int totalDoubleBond, int totalOxidized,
       AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // Positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    //  seek 76.039305(gly+)
                    //var threshold = 10.0;
                    //var diagnosticMz = 76.039305;
                    //var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    //if (isClassIonFound == false) return null;

                    var threshold1 = 5.0;
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

        public static LipidMolecule JudgeIfDag(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PS 46:6, totalCarbon = 46 and totalDoubleBond = 6
            int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double,
            AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (totalCarbon > 52) return null; // currently, very large DAG is excluded.
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
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

                    //Console.WriteLine(sn1Carbon + ":" + sn1Double + "-" + sn2Carbon + ":" + sn2Double + 
                    //    " " + nl_SN1 + " " + nl_SN2);

                    var query = new List<SpectrumPeak>
                    {
                                new SpectrumPeak() { Mass = nl_SN1, Intensity = 1 },
                                new SpectrumPeak() { Mass = nl_SN2, Intensity = 1 },
                            };
                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);
                    if (foundCount == 2)
                    {
                        var molecule = LipidMsmsCharacterizationUtility.getDiacylglycerolMoleculeObjAsLevel2("DG", LbmClass.DG, sn1Carbon, sn1Double,
                        sn2Carbon, sn2Double,
                        averageIntensity);
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
                    for (int sn1CarbonNum = 6; sn1CarbonNum <= totalCarbon; sn1CarbonNum++)
                    {
                        for (int sn1DoubleNum = 0; sn1DoubleNum <= totalDoubleBond; sn1DoubleNum++)
                        {
                            var sn2CarbonNum = totalCarbon - sn1CarbonNum;
                            var sn2DoubleNum = totalDoubleBond - sn1DoubleNum;
                            if (sn2DoubleNum >= 7) continue;

                            var nl_SN1 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1CarbonNum, sn1DoubleNum) - H2O + MassDiffDictionary.HydrogenMass * 2;
                            var nl_SN2 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2CarbonNum, sn2DoubleNum) - H2O + MassDiffDictionary.HydrogenMass * 2;

                            //Console.WriteLine(sn1Carbon + ":" + sn1Double + "-" + sn2Carbon + ":" + sn2Double + 
                            //    " " + nl_SN1 + " " + nl_SN2);

                            var query = new List<SpectrumPeak>
                            {
                                new SpectrumPeak() { Mass = nl_SN1, Intensity = 5 },
                                new SpectrumPeak() { Mass = nl_SN2, Intensity = 5 },
                            };
                            var foundCount = 0;
                            var averageIntensity = 0.0;
                            LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);
                            if (foundCount == 2)
                            {
                                var molecule = LipidMsmsCharacterizationUtility.getDiacylglycerolMoleculeObjAsLevel2("DG", LbmClass.DG, sn1CarbonNum, sn1DoubleNum,
                                sn2CarbonNum, sn2DoubleNum,
                                averageIntensity);
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
        public static LipidMolecule JudgeIfEtherpc(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PC 46:6, totalCarbon = 46 and totalDoubleBond = 6
            int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double,
            AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
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
                    // 
                    var threshold = 3.0;
                    var isClassIonFound1 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, C5H14NO4P, threshold);
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_C, threshold);
                    var isClassIonFound3 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_O, threshold);
                    if (!isClassIonFound1 || !isClassIonFound2 || !isClassIonFound3) return null;
                    // reject Na+ adduct
                    var diagnosticMz3 = theoreticalMz - 59.0735;
                    var isNaTypicalFound1 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz3, 10.0);
                    if (isNaTypicalFound1)
                    {
                        return null;
                    }


                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    var acylLoss = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) + MassDiffDictionary.HydrogenMass;

                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = acylLoss, Intensity = 0.1 },
                             };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);
                    if (foundCount == 1)
                    { // 
                        var molecule = LipidMsmsCharacterizationUtility.getEtherPhospholipidMoleculeObjAsLevel2("PC", LbmClass.EtherPC, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity, "e");
                        candidates.Add(molecule);
                    }
                    else { return null; }
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
            }
            else
            {
                if (adduct.AdductIonName == "[M+FA-H]-" || adduct.AdductIonName == "[M+Hac-H]-" ||
                    adduct.AdductIonName == "[M+HCOO]-" || adduct.AdductIonName == "[M+CH3COO]-")
                {
                    // seek [M-CH3]-
                    var threshold = 10.0;
                    var diagnosticMz = adduct.AdductIonName == "[M+CH3COO]-" || adduct.AdductIonName == "[M+Hac-H]-" ?
                        theoreticalMz - 74.036779433 : theoreticalMz - 60.021129369;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (!isClassIonFound) return null;

                    if (adduct.AdductIonName == "[M+CH3COO]-" || adduct.AdductIonName == "[M+Hac-H]-")
                    {
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
                                new SpectrumPeak() { Mass = sn2, Intensity = 30.0 },
                                //new SpectrumPeak() { Mass = NL_sn2, Intensity = 0.1 },
                                //new SpectrumPeak() { Mass = NL_sn2AndWater, Intensity = 0.1 }
                            };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 1)
                    { // 
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
            double skelton = new[]
            {
                MassDiffDictionary.CarbonMass * 27,
                MassDiffDictionary.HydrogenMass * 46,
                MassDiffDictionary.OxygenMass * 1,
            }.Sum();
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
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

        public static LipidMolecule JudgeIfPhosphatidylcholineD5(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PC 46:6, totalCarbon = 46 and totalDoubleBond = 6
            int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double,
            AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            var candidates = new List<LipidMolecule>();

            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    double C5H15NO4P = new[] {
                        MassDiffDictionary.CarbonMass * 5,
                        MassDiffDictionary.HydrogenMass * 14,
                        MassDiffDictionary.NitrogenMass,
                        MassDiffDictionary.OxygenMass * 4,
                        MassDiffDictionary.PhosphorusMass,
                        Proton
                    }.Sum();
                    double Gly_C = new[] {
                        MassDiffDictionary.CarbonMass * 8,
                        MassDiffDictionary.HydrogenMass * 13,
                        MassDiffDictionary.NitrogenMass,
                        MassDiffDictionary.OxygenMass * 4,
                        MassDiffDictionary.PhosphorusMass,
                        MassDiffDictionary.Hydrogen2Mass * 5,
                        Proton
                    }.Sum();

                    double Gly_O = new[] {
                        MassDiffDictionary.CarbonMass * 7,
                        MassDiffDictionary.HydrogenMass * 13,
                        MassDiffDictionary.NitrogenMass,
                        MassDiffDictionary.OxygenMass * 5,
                        MassDiffDictionary.PhosphorusMass,
                        MassDiffDictionary.Hydrogen2Mass * 3,
                        Proton
                    }.Sum();
                    //
                    var threshold = 3.0;
                    var isClassIonFound1 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, C5H15NO4P, threshold);
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_C, threshold);
                    var isClassIonFound3 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_O, threshold);
                    if (!isClassIonFound1 || !isClassIonFound2 || !isClassIonFound3) return null;
                    // reject Na+ adduct
                    var diagnosticMz3 = theoreticalMz - 59.0735;
                    var isNaTypicalFound1 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz3, 10.0);
                    if (isNaTypicalFound1)
                    {
                        return null;
                    }

                    // for eieio
                    var PEHeaderLoss = theoreticalMz - 141.019094261 + MassDiffDictionary.ProtonMass;
                    var isClassIonFoundPe = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, PEHeaderLoss, 5.0);
                    //if (isClassIonFound2 && LipidMsmsCharacterizationUtility.isFragment1GreaterThanFragment2(spectrum, ms2Tolerance, PEHeaderLoss, diagnosticMz))
                    if (isClassIonFoundPe)
                    {
                        return null;
                    }

                    // from here, acyl level annotation is executed.
                    if (sn1Carbon < 10 || sn2Carbon < 10) return null;
                    if (sn1Double > 6 || sn2Double > 6) return null;

                    var nl_SN1 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) + MassDiffDictionary.HydrogenMass;
                    var nl_SN1_H2O = nl_SN1 - H2O;

                    var nl_SN2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) + MassDiffDictionary.HydrogenMass;
                    var nl_SN2_H2O = nl_SN2 - H2O;

                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = nl_SN1, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = nl_SN1_H2O, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = nl_SN2, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = nl_SN2_H2O, Intensity = 0.01 }
                            };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount >= 2)
                    { // now I set 2 as the correct level
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PC_d5", LbmClass.PC_d5, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                    if (candidates == null || candidates.Count == 0)
                        return null;
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PC_d5", LbmClass.PC_d5, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);

                }
                //addMT
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    //// seek 184.07332 (C5H15NO4P)
                    //var diagnosticMz = 184.07332;
                    // seek [M+Na -C5H14NO4P]+
                    var diagnosticMz2 = theoreticalMz - 183.06604;
                    // seek [M+Na -C3H9N]+
                    var diagnosticMz3 = theoreticalMz - 59.0735;
                    var threshold = 3.0;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz3, threshold);
                    if (isClassIonFound == false || isClassIon2Found == false) return null;

                    // from here, acyl level annotation is executed.
                    for (int sn1CarbonNum = 6; sn1CarbonNum <= totalCarbon; sn1CarbonNum++)
                    {
                        for (int sn1DoubleNum = 0; sn1DoubleNum <= totalDoubleBond; sn1DoubleNum++)
                        {
                            var sn2CarbonNum = totalCarbon - sn1CarbonNum;
                            var sn2DoubleNum = totalDoubleBond - sn1DoubleNum;
                            if (sn2DoubleNum >= 7) continue;

                            var nl_SN1 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1CarbonNum, sn1DoubleNum);
                            var nl_SN1_H2O = nl_SN1 - H2O;
                            var nl_SN2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2CarbonNum, sn2DoubleNum);
                            var nl_SN2_H2O = nl_SN2 - H2O;

                            var query = new List<SpectrumPeak>
                            {
                                new SpectrumPeak() { Mass = nl_SN1, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = nl_SN1_H2O, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = nl_SN2, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = nl_SN2_H2O, Intensity = 0.01 }
                            };
                            var foundCount = 0;
                            var averageIntensity = 0.0;
                            LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);
                            if (foundCount >= 3)
                            {
                                var molecule = LipidMsmsCharacterizationUtility.getDiacylglycerolMoleculeObjAsLevel2("PC_d5", LbmClass.PC_d5, sn1CarbonNum, sn1DoubleNum,
                                sn2CarbonNum, sn2DoubleNum,
                                averageIntensity);
                                candidates.Add(molecule);
                            }
                        }
                    }

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PC_d5", LbmClass.PC_d5, "", theoreticalMz, adduct,
                            totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfPhosphatidylethanolamineD5(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PE 46:6, totalCarbon = 46 and totalDoubleBond = 6
            int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double,
            AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            var candidates = new List<LipidMolecule>();

            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
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
                    double C2H8NO4P = new[]
                    {
                        MassDiffDictionary.CarbonMass * 2,
                        MassDiffDictionary.HydrogenMass * 8,
                        MassDiffDictionary.NitrogenMass,
                        MassDiffDictionary.OxygenMass * 4,
                        MassDiffDictionary.PhosphorusMass,
                    }.Sum();
                    // seek -141.019094261 (C2H8NO4P)
                    var threshold = 2.5;
                    var diagnosticMz = theoreticalMz - C2H8NO4P;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_C, threshold);
                    var isClassIonFound3 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_O, threshold);
                    if (!isClassIonFound || !isClassIonFound2 || !isClassIonFound3) return null;

                    // from here, acyl level annotation is executed.
                    var sn1 = LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) - Electron;
                    var sn2 = LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) - Electron;

                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = sn1, Intensity = 0.1 },
                                new SpectrumPeak() { Mass = sn2, Intensity = 0.1 }
                            };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2)
                    { // now I set 2 as the correct level
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PE_d5", LbmClass.PE_d5, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PE_d5", LbmClass.PE_d5, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
                //addMT
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek -141.019094261 (C2H8NO4P)
                    var threshold = 3.0;
                    var diagnosticMz = theoreticalMz - 141.019094261;
                    // seek - 43.042199 (C2H5N)
                    var diagnosticMz2 = theoreticalMz - 43.042199;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // from here, acyl level annotation is executed.
                    for (int sn1CarbonNum = 6; sn1CarbonNum <= totalCarbon; sn1CarbonNum++)
                    {
                        for (int sn1DoubleNum = 0; sn1DoubleNum <= totalDoubleBond; sn1DoubleNum++)
                        {
                            var sn2CarbonNum = totalCarbon - sn1CarbonNum;
                            var sn2DoubleNum = totalDoubleBond - sn1DoubleNum;
                            if (sn2DoubleNum >= 7) continue;

                            var nl_SN1 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1CarbonNum, sn1DoubleNum) - MassDiffDictionary.HydrogenMass * 2;
                            var nl_SN1_H2O = nl_SN1 - MassDiffDictionary.OxygenMass;
                            var nl_SN2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2CarbonNum, sn2DoubleNum) - MassDiffDictionary.HydrogenMass * 2;
                            var nl_SN2_H2O = nl_SN2 - MassDiffDictionary.OxygenMass;

                            var query = new List<SpectrumPeak>
                            {
                                new SpectrumPeak() { Mass = nl_SN1, Intensity = 0.1 },
                                new SpectrumPeak() { Mass = nl_SN1_H2O, Intensity = 0.1 },
                                new SpectrumPeak() { Mass = nl_SN2, Intensity = 0.1},
                                new SpectrumPeak() { Mass = nl_SN2_H2O, Intensity = 0.1 }
                            };
                            var foundCount = 0;
                            var averageIntensity = 0.0;
                            LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);
                            if (foundCount >= 3)
                            {
                                var molecule = LipidMsmsCharacterizationUtility.getDiacylglycerolMoleculeObjAsLevel2("PE_d5", LbmClass.PE_d5, sn1CarbonNum, sn1DoubleNum,
                                sn2CarbonNum, sn2DoubleNum,
                                averageIntensity);
                                candidates.Add(molecule);
                            }
                        }
                    }
                    if (candidates == null || candidates.Count == 0)
                        return null;

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PE_d5", LbmClass.PE_d5, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            return null;
        }


        public static LipidMolecule JudgeIfPhosphatidylserineD5(IMSScanProperty msScanProp, double ms2Tolerance,
           double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PS 46:6, totalCarbon = 46 and totalDoubleBond = 6
           int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double,
           AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    // seek -185.008927 (C3H8NO6P)
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - 185.008927;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    var nl_SN1 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) + MassDiffDictionary.HydrogenMass;
                    var nl_SN1_H2O = nl_SN1 - H2O;

                    var nl_SN2 = theoreticalMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) + MassDiffDictionary.HydrogenMass;
                    var nl_NS2_H2O = nl_SN2 - H2O;

                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = nl_SN1, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = nl_SN1_H2O, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = nl_SN2, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = nl_NS2_H2O, Intensity = 0.01 }
                            };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount >= 2)
                    { // now I set 2 as the correct level
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PS_d5", LbmClass.PS_d5, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PS_d5", LbmClass.PS_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 2);

                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek -185.008927 (C3H8NO6P)
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - 185.008927;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // acyl level may be not able to annotate.
                    var candidates = new List<LipidMolecule>();
                    //var score = 0;
                    //var molecule = getLipidAnnotaionAsLevel1("PS_d5", LbmClass.PS_d5, totalCarbon, totalDoubleBond, score, "");
                    //candidates.Add(molecule);

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PS_d5", LbmClass.PS_d5, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfPhosphatidylglycerolD5(IMSScanProperty msScanProp, double ms2Tolerance,
           double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PS 46:6, totalCarbon = 46 and totalDoubleBond = 6
           int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double,
           AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+NH4]+")
                {
                    // seek -189.040227 (C3H8O6P+NH4)
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - 189.040227;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    var nl_SN1 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) + MassDiffDictionary.HydrogenMass;
                    var nl_SN2 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) + MassDiffDictionary.HydrogenMass;

                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = nl_SN1, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = nl_SN2, Intensity = 0.01 }
                            };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2 && averageIntensity < 30)
                    { // average intensity < 30 is nessesarry to distinguish it from BMP
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PG_d5", LbmClass.PG_d5, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PG_d5", LbmClass.PG_d5, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // seek -171.005851 (C3H8O6P) - 22.9892207 (Na+)
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - 171.005851 - 22.9892207;// + MassDiffDictionary.HydrogenMass;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PG_d5", LbmClass.PG_d5, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);

                }
            }
            return null;
        }
        public static LipidMolecule JudgeIfPhosphatidylinositolD5(IMSScanProperty msScanProp, double ms2Tolerance,
           double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PS 46:6, totalCarbon = 46 and totalDoubleBond = 6
           int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double,
           AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+NH4]+")
                {
                    // seek -277.056272 (C6H12O9P+NH4)
                    var threshold = 10.0;
                    var diagnosticMz = theoreticalMz - 277.056272;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PI_d5", LbmClass.PI_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // 
                    var threshold = 10.0;
                    var diagnosticMz1 = theoreticalMz - (259.021895 + 22.9892207);  // seek -(C6H12O9P +Na)
                    var diagnosticMz2 = theoreticalMz - (260.02972);                 // seek -(C6H12O9P + H)
                    var diagnosticMz3 = (260.02972 + 22.9892207);                   // seek (C6H13O9P +Na)

                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold);
                    var isClassIon3Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz3, threshold);
                    if (!isClassIon1Found || !isClassIon2Found || !isClassIon3Found) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    //var score = 0;
                    //var molecule = getLipidAnnotaionAsLevel1("PI_d5", LbmClass.PI_d5, totalCarbon, totalDoubleBond, score, "");
                    //candidates.Add(molecule);

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PI_d5", LbmClass.PI_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }

            }
            return null;
        }
        public static LipidMolecule JudgeIfLysopcD5(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // 
            int snCarbon, int snDoubleBond,
            AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    if (totalCarbon > 28) return null; //  currently carbon > 28 is recognized as EtherPC
                                                       // seek 184.07332 (C5H15NO4P)
                    var threshold = 3.0;
                    var diagnosticMz = 184.07332;
                    var diagnosticMz2 = 104.106990;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIon1Found != true) return null;
                    // reject Na+ adduct
                    var diagnosticMz3 = theoreticalMz - 59.0735;
                    var isNaTypicalFound1 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz3, 10.0);
                    if (isNaTypicalFound1)
                    {
                        return null;
                    }

                    // for eieio
                    var PEHeaderLoss = theoreticalMz - 141.019094261 + MassDiffDictionary.ProtonMass;
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, PEHeaderLoss, 3.0);
                    if (isClassIonFound2 && LipidMsmsCharacterizationUtility.isFragment1GreaterThanFragment2(spectrum, ms2Tolerance, PEHeaderLoss, diagnosticMz))
                    {
                        return null;
                    }
                    //
                    var candidates = new List<LipidMolecule>();
                    var chainSuffix = "";
                    var diagnosticMzExist = 0.0;
                    var diagnosticMzIntensity = 0.0;
                    var diagnosticMzExist2 = 0.0;
                    var diagnosticMzIntensity2 = 0.0;

                    for (int i = 0; i < spectrum.Count; i++)
                    {
                        var mz = spectrum[i].Mass;
                        var intensity = spectrum[i].Intensity;

                        if (intensity > threshold && Math.Abs(mz - diagnosticMz) < ms2Tolerance)
                        {
                            diagnosticMzExist = mz;
                            diagnosticMzIntensity = intensity;
                        }
                        else if (intensity > threshold && Math.Abs(mz - diagnosticMz2) < ms2Tolerance)
                        {
                            diagnosticMzExist2 = mz;
                            diagnosticMzIntensity2 = intensity;
                        }
                    };

                    if (diagnosticMzIntensity2 / diagnosticMzIntensity > 0.3) //
                    {
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
                    // seek 104.1070 (C5H14NO) maybe not found
                    //var threshold2 = 1.0;
                    //var diagnosticMz2 = 104.1070;

                    //
                    var candidates = new List<LipidMolecule>();
                    var score = 0.0;
                    if (totalCarbon < 30) score = score + 1.0;
                    var molecule = LipidMsmsCharacterizationUtility.getSingleacylchainMoleculeObjAsLevel2("LPC_d5", LbmClass.LPC_d5, totalCarbon, totalDoubleBond,
                    score);
                    candidates.Add(molecule);

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPC_d5", LbmClass.LPC_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfLysopeD5(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // 
            int snCarbon, int snDoubleBond,
            AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
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
                    double C2H8NO4P = new[]
                    {
                        MassDiffDictionary.CarbonMass * 2,
                        MassDiffDictionary.HydrogenMass * 8,
                        MassDiffDictionary.NitrogenMass,
                        MassDiffDictionary.OxygenMass * 4,
                        MassDiffDictionary.PhosphorusMass,
                    }.Sum();
                    // seek -141.019094261 (C2H8NO4P)
                    var threshold = 2.5;
                    var diagnosticMz = theoreticalMz - C2H8NO4P;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_C, threshold);
                    var isClassIonFound3 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, Gly_O, threshold);
                    if ((isClassIonFound ? 1 : 0) + (isClassIonFound2 ? 1 : 0) + (isClassIonFound3 ? 1 : 0) < 2)
                    {
                        return null;
                    }
                    //if (!isClassIonFound || !isClassIonFound2 || !isClassIonFound3) return null;

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

                    //
                    var candidates = new List<LipidMolecule>();
                    //var score = 0.0;
                    //if (totalCarbon < 30) score = score + 1.0;
                    //var molecule = getSingleacylchainMoleculeObjAsLevel2("LPE_d5", LbmClass.LPE_d5, totalCarbon, totalDoubleBond,
                    //score);
                    //candidates.Add(molecule);
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
            { //
                if (adduct.AdductIonName == "[M-H]-")
                {
                    // seek C3H6O5P-

                    var diagnosticMz1 = 152.99583;  // seek C3H6O5P-
                    var threshold1 = 1.0;
                    var diagnosticMz2 = LipidMsmsCharacterizationUtility.fattyacidProductIon(totalCarbon, totalDoubleBond); // seek [FA-H]-
                    var threshold2 = 10.0;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold1);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    if (isClassIon1Found != true || isClassIon2Found != true) return null;

                    //
                    var candidates = new List<LipidMolecule>();
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPG_d5", LbmClass.LPG_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            else if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    // seek Header loss (MG+ + chain Acyl) 
                    var threshold = 5.0;
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

        public static LipidMolecule JudgeIfLysopiD5(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PE 46:6, totalCarbon = 46 and totalDoubleBond = 6
            int snCarbon, int snDoubleBond,
            AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Negative)
            { //negative ion mode only
                if (adduct.AdductIonName == "[M-H]-")
                {
                    // seek C3H6O5P-

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

                    //
                    var candidates = new List<LipidMolecule>();
                    //var averageIntensity = 0.0;

                    //var molecule = getSingleacylchainMoleculeObjAsLevel2("LPI_d5", LbmClass.LPI_d5, totalCarbon, totalDoubleBond,
                    //averageIntensity);
                    //candidates.Add(molecule);
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPI_d5", LbmClass.LPI_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            else if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    // seek Header loss (MG+ + chain Acyl) 
                    var threshold = 5.0;
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
        public static LipidMolecule JudgeIfLysopsD5(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PE 46:6, totalCarbon = 46 and totalDoubleBond = 6
            int snCarbon, int snDoubleBond,
            AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Negative)
            { //negative ion mode only
                if (adduct.AdductIonName == "[M-H]-")
                {
                    // seek C3H6O5P-

                    var diagnosticMz1 = 152.99583;  // seek C3H6O5P-
                    var threshold1 = 10.0;
                    var diagnosticMz2 = theoreticalMz - 87.032029; // seek -C3H6NO2-H
                    var threshold2 = 5.0;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, threshold1);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold2);
                    if (isClassIon1Found != true || isClassIon2Found != true) return null;

                    //
                    var candidates = new List<LipidMolecule>();
                    //var averageIntensity = 0.0;

                    //var molecule = getSingleacylchainMoleculeObjAsLevel2("LPS_d5", LbmClass.LPS_d5, totalCarbon, totalDoubleBond,
                    //averageIntensity);
                    //candidates.Add(molecule);
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPS_d5", LbmClass.LPS_d5, "", theoreticalMz, adduct,
                       totalCarbon, totalDoubleBond, 0, candidates, 1);
                }
            }
            else if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {
                    // seek Header loss (MG+ + chain Acyl) 
                    var threshold = 5.0;
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
            { // positive ion mode 
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

                    //Console.WriteLine(sn1Carbon + ":" + sn1Double + "-" + sn2Carbon + ":" + sn2Double + 
                    //    " " + nl_SN1 + " " + nl_SN2);

                    var query = new List<SpectrumPeak>
                    {
                                new SpectrumPeak() { Mass = nl_SN1, Intensity = 5 },
                                new SpectrumPeak() { Mass = nl_SN2, Intensity = 5 },
                            };
                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);
                    if (foundCount == 2)
                    {
                        var molecule = LipidMsmsCharacterizationUtility.getDiacylglycerolMoleculeObjAsLevel2("DG_d5", LbmClass.DG_d5, sn1Carbon, sn1Double,
                        sn2Carbon, sn2Double,
                        averageIntensity);
                        candidates.Add(molecule);
                    }
                    if (candidates == null || candidates.Count == 0)
                        return null;

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("DG_d5", LbmClass.DG_d5, string.Empty, theoreticalMz, adduct,

                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
                else if (adduct.AdductIonName == "[M+Na]+")
                {
                    // from here, acyl level annotation is executed.
                    var diagnosticMz = theoreticalMz;
                    var candidates = new List<LipidMolecule>();
                    for (int sn1CarbonNum = 6; sn1CarbonNum <= totalCarbon; sn1CarbonNum++)
                    {
                        for (int sn1DoubleNum = 0; sn1DoubleNum <= totalDoubleBond; sn1DoubleNum++)
                        {
                            var sn2CarbonNum = totalCarbon - sn1CarbonNum;
                            var sn2DoubleNum = totalDoubleBond - sn1DoubleNum;
                            if (sn2DoubleNum >= 7) continue;

                            var nl_SN1 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn1CarbonNum, sn1DoubleNum) - H2O + MassDiffDictionary.HydrogenMass;
                            var nl_SN2 = diagnosticMz - LipidMsmsCharacterizationUtility.acylCainMass(sn2CarbonNum, sn2DoubleNum) - H2O + MassDiffDictionary.HydrogenMass;

                            //Console.WriteLine(sn1Carbon + ":" + sn1Double + "-" + sn2Carbon + ":" + sn2Double + 
                            //    " " + nl_SN1 + " " + nl_SN2);

                            var query = new List<SpectrumPeak>
                            {
                                new SpectrumPeak() { Mass = nl_SN1, Intensity = 1 },
                                new SpectrumPeak() { Mass = nl_SN2, Intensity = 1 },
                            };
                            var foundCount = 0;
                            var averageIntensity = 0.0;
                            LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);
                            if (foundCount == 2)
                            {
                                var molecule = LipidMsmsCharacterizationUtility.getDiacylglycerolMoleculeObjAsLevel2("DG", LbmClass.DG, sn1CarbonNum, sn1DoubleNum, sn2CarbonNum, sn2DoubleNum,
                                averageIntensity);
                                candidates.Add(molecule);
                            }
                        }
                    }
                    if (candidates == null || candidates.Count == 0)
                        return null;


                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("DG_d5", LbmClass.DG_d5, string.Empty, theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);

                }
            }
            return null;
        }
        public static LipidMolecule JudgeIfTriacylglycerolD5(IMSScanProperty msScanProp, double ms2Tolerance,
           double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PS 46:6, totalCarbon = 46 and totalDoubleBond = 6
           int sn1Carbon, int sn2Carbon, int sn3Carbon, int sn1Double, int sn2Double, int sn3Double,
           AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
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
                                        new SpectrumPeak() { Mass = nl_SN1, Intensity = 5 },
                                        new SpectrumPeak() { Mass = nl_SN2, Intensity = 5 },
                                        new SpectrumPeak() { Mass = nl_SN3, Intensity = 5 }
                                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 3)
                    { // these three chains must be observed.
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
                                        new SpectrumPeak() {  Mass = nl_SN1, Intensity = 0.1 },
                                        new SpectrumPeak() { Mass = nl_SN2, Intensity = 0.1 },
                                        new SpectrumPeak() { Mass = nl_SN3, Intensity = 0.1 }
                                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount < 3)
                    {
                        var diagnosticMzH = theoreticalMz - 22.9892207 + MassDiffDictionary.HydrogenMass;
                        var nl_SN1_H = diagnosticMzH - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) - H2O + MassDiffDictionary.HydrogenMass;
                        var nl_SN2_H = diagnosticMzH - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) - H2O + MassDiffDictionary.HydrogenMass;
                        var nl_SN3_H = diagnosticMzH - LipidMsmsCharacterizationUtility.acylCainMass(sn3Carbon, sn3Double) - H2O + MassDiffDictionary.HydrogenMass;
                        var query2 = new List<SpectrumPeak> {
                                        new SpectrumPeak() { Mass = nl_SN1_H, Intensity = 0.1 },
                                        new SpectrumPeak() { Mass = nl_SN2_H, Intensity = 0.1 },
                                        new SpectrumPeak() { Mass = nl_SN3_H, Intensity = 0.1 }
                                        };

                        var foundCount2 = 0;
                        var averageIntensity2 = 0.0;
                        LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity2);


                        if (foundCount2 == 3)
                        {
                            var molecule = LipidMsmsCharacterizationUtility.getTriacylglycerolMoleculeObjAsLevel2("TG_d5", LbmClass.TG_d5, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, sn3Carbon, sn3Double, averageIntensity2);
                            candidates.Add(molecule);
                        }
                    }
                    else
                    if (foundCount == 3)
                    { // these three chains must be observed.
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
        public static LipidMolecule JudgeIfSphingomyelinD9(IMSScanProperty msScanProp, double ms2Tolerance,
        double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PC 46:6, totalCarbon = 46 and totalDoubleBond = 6
        int sphCarbon, int acylCarbon, int sphDouble, int acylDouble,
        AdductIon adduct)
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
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+")
                {

                    // seek 184.07332 (C5H15NO4P) D9
                    var threshold = 10.0;
                    var diagnosticMz = C5H5D9NO4P + MassDiffDictionary.ProtonMass;
                    var diagnosticMz1 = C5H5D9NO4P + 12 * 2 + MassDiffDictionary.HydrogenMass * 3 + MassDiffDictionary.NitrogenMass + Proton;
                    var diagnosticMz2 = C5H5D9NO4P + 12 * 3 + MassDiffDictionary.HydrogenMass * 3 + MassDiffDictionary.NitrogenMass + MassDiffDictionary.OxygenMass + Proton;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    var isClassIonFound1 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz1, 1.0);
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, 1.0);
                    if (!isClassIonFound || !isClassIonFound1 || !isClassIonFound2) return null;

                    // from here, acyl level annotation is executed.
                    var candidates = new List<LipidMolecule>();
                    if (sphCarbon <= 13) return null;
                    if (sphCarbon == 16 && sphDouble >= 3) return null;
                    if (acylCarbon < 8) return null;

                    var diagnosChain1 = LipidMsmsCharacterizationUtility.acylCainMass(acylCarbon, acylDouble) + C2H2N + MassDiffDictionary.HydrogenMass + Proton;
                    var diagnosChain2 = diagnosChain1 + C5H5D9NO4P - MassDiffDictionary.HydrogenMass;

                    var query = new List<SpectrumPeak> {
                        new SpectrumPeak() { Mass = diagnosChain1, Intensity = 0.5 },
                        new SpectrumPeak() { Mass = diagnosChain2, Intensity = 1.0 },
                    };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2)
                    { // the diagnostic acyl ion must be observed for level 2 annotation
                        var molecule = LipidMsmsCharacterizationUtility.getCeramideMoleculeObjAsLevel2("SM_d9", LbmClass.SM_d9, "d", sphCarbon, sphDouble,
                            acylCarbon, acylDouble, averageIntensity);
                        candidates.Add(molecule);
                    }
                    else return null;
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
                    for (int sphCarbonNum = 6; sphCarbonNum <= totalCarbon; sphCarbonNum++)
                    {
                        for (int sphDoubleNum = 0; sphDoubleNum <= totalDoubleBond; sphDoubleNum++)
                        {
                            var acylCarbonNum = totalCarbon - sphCarbonNum;
                            var acylDoubleNum = totalDoubleBond - sphDoubleNum;
                            if (acylDoubleNum >= 7) continue;

                            var diagnosChain1 = LipidMsmsCharacterizationUtility.acylCainMass(acylCarbonNum, acylDoubleNum) + C2H2N + MassDiffDictionary.HydrogenMass + MassDiffDictionary.ProtonMass;
                            var diagnosChain2 = diagnosChain1 + C5H5D9NO4P - MassDiffDictionary.HydrogenMass;

                            var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = diagnosChain1, Intensity = 20.0 },
                                new SpectrumPeak() { Mass = diagnosChain2, Intensity = 20.0 },
                            };
                            var foundCount = 0;
                            var averageIntensity = 0.0;
                            LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);
                            if (foundCount == 2)
                            {
                                var molecule = LipidMsmsCharacterizationUtility.getDiacylglycerolMoleculeObjAsLevel2("SM", LbmClass.SM, sphCarbonNum, sphDoubleNum,
                                acylCarbonNum, acylDoubleNum,
                                averageIntensity);
                                candidates.Add(molecule);
                            }
                        }
                    }
                    if (candidates == null || candidates.Count == 0)
                        return null;

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("SM_d9", LbmClass.SM_d9, "d", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }

            return null;
        }
        public static LipidMolecule JudgeIfCeramidensD7(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PC 46:6, totalCarbon = 46 and totalDoubleBond = 6
            int sphCarbon, int acylCarbon, int sphDouble, int acylDouble,
            AdductIon adduct)
        {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
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
                    var queryMust = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = sph2, Intensity = 5 },
                            };
                    var foundCountMust = 0;
                    var averageIntensityMust = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, queryMust, ms2Tolerance, out foundCountMust, out averageIntensityMust);
                    if (foundCountMust == 0) return null;

                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = sph1, Intensity = 1 },
                                new SpectrumPeak() { Mass = sph3, Intensity = 1 },
                            };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);
                    var foundCountThresh = acylCarbon < 12 ? 2 : 1; // to exclude strange annotation

                    if (foundCount >= foundCountThresh)
                    { // 
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

                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = sph3, Intensity = 1 },
                            };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 1)
                    { // 
                        var molecule = LipidMsmsCharacterizationUtility.getCeramideMoleculeObjAsLevel2("Cer_d7", LbmClass.Cer_NS_d7, "d", sphCarbon, sphDouble,
                            acylCarbon, acylDouble, averageIntensity);
                        candidates.Add(molecule);
                    }
                    if (candidates.Count == 0) return null;
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("Cer_d7", LbmClass.Cer_NS_d7, "d", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }

            return null;
        }
        public static LipidMolecule JudgeIfCholesterylEsterD7(IMSScanProperty msScanProp, double ms2Tolerance, float theoreticalMz,
        int totalCarbon, int totalDoubleBond, AdductIon adduct)
        {
            double skelton = new[]
            {
                MassDiffDictionary.CarbonMass * 27,
                MassDiffDictionary.HydrogenMass * 46,
                MassDiffDictionary.OxygenMass * 1,
                MassDiffDictionary.Hydrogen2Mass * 7,
                - MassDiffDictionary.HydrogenMass * 7,
            }.Sum();
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive)
            { // positive ion mode 
                if (adduct.AdductIonName == "[M+NH4]+")
                {
                    // seek 369.3515778691 (C27H45+)+ MassDiffDictionary.HydrogenMass*7
                    var threshold = 20.0;
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

    }
}
