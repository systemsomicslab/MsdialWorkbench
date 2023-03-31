using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Lipidomics {
    public sealed class LipidEieioMsmsCharacterization {
        private LipidEieioMsmsCharacterization() { }

        private const double Electron = 0.00054858026;
        private const double Proton = 1.00727641974;
        private const double H2O = 18.010564684;
        private const double Sugar162 = 162.052823422;
        private const double Na = 22.98977;

        public static LipidMolecule JudgeIfPhosphatidylcholine(IMSScanProperty msScanProp, double ms2Tolerance,
           double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PC 46:6, totalCarbon = 46 and totalDoubleBond = 6
           int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double,
           AdductIon adduct) {

            var spectrum = msScanProp.Spectrum;
            var candidates = new List<LipidMolecule>();

            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive) { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+") {

                    if (sn1Carbon < 10 || sn2Carbon < 10)
                        return LipidMsmsCharacterizationUtility.returnAnnotationResult("PC", LbmClass.PC, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                    if (sn1Double > 6 || sn2Double > 6) return LipidMsmsCharacterizationUtility.returnAnnotationResult("PC", LbmClass.PC, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                    
                    // seek 184.07332 (C5H15NO4P)
                    var threshold = 3.0;
                    var diagnosticMz = 184.07332;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // for eieio
                    var PEHeaderLoss = theoreticalMz - 141.019094261;
                    var isClassIonFound2 = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, PEHeaderLoss, 5.0);
                    if (isClassIonFound2 && LipidMsmsCharacterizationUtility.isFragment1GreaterThanFragment2(spectrum, ms2Tolerance, PEHeaderLoss, diagnosticMz)) {
                        return null;
                    }

                    // from here, acyl level annotation is executed.
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

                    if (foundCount >= 2) { // now I set 2 as the correct level
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PC", LbmClass.PC, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                    
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PC", LbmClass.PC, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);

                }
                //addMT
                else if (adduct.AdductIonName == "[M+Na]+") {
                    // seek 184.07332 (C5H15NO4P)
                    var threshold = 3.0;
                    var diagnosticMz = 184.07332;
                    // seek [M+Na -C5H14NO4P]+
                    var diagnosticMz2 = theoreticalMz - 183.06604;
                    // seek [M+Na -C3H9N]+
                    var diagnosticMz3 = theoreticalMz - 59.0735;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz2, threshold);
                    var isClassIon2Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz3, threshold);
                    if (isClassIonFound == false || isClassIon2Found == false) return null;

                    // from here, acyl level annotation is executed.
                    var nl_SN1 = diagnosticMz3 - LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) - H2O + MassDiffDictionary.HydrogenMass;
                    var nl_SN2 = diagnosticMz3 - LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) - H2O + MassDiffDictionary.HydrogenMass;

                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = nl_SN1, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = nl_SN2, Intensity = 0.01 },
                            };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount >= 2) { // now I set 2 as the correct level
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PC", LbmClass.PC, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                    
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PC", LbmClass.PC, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            return null;
        }

        public static LipidMolecule JudgeIfPhosphatidylethanolamine(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // If the candidate PE 46:6, totalCarbon = 46 and totalDoubleBond = 6
            int sn1Carbon, int sn2Carbon, int sn1Double, int sn2Double,
            AdductIon adduct) {

            var spectrum = msScanProp.Spectrum;
            var candidates = new List<LipidMolecule>();

            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive) { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+") {
                    // seek -141.019094261 (C2H8NO4P)
                    var threshold = 2.5;
                    var diagnosticMz = theoreticalMz - 141.019094261;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

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

                    if (foundCount == 2) { // now I set 2 as the correct level
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PE", LbmClass.PE, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }
                    
                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PE", LbmClass.PE, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
                //addMT
                else if (adduct.AdductIonName == "[M+Na]+") {
                    // seek -141.019094261 (C2H8NO4P)
                    var threshold = 3.0;
                    var diagnosticMz = theoreticalMz - 141.019094261;
                    // seek - 43.042199 (C2H5N)
                    var diagnosticMz2 = theoreticalMz - 43.042199;
                    var isClassIonFound = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIonFound == false) return null;

                    // from here, acyl level annotation is executed.
                    var sn1 = LipidMsmsCharacterizationUtility.acylCainMass(sn1Carbon, sn1Double) - Electron;
                    var sn2 = LipidMsmsCharacterizationUtility.acylCainMass(sn2Carbon, sn2Double) - Electron;

                    var query = new List<SpectrumPeak> {
                                new SpectrumPeak() { Mass = sn1, Intensity = 0.01 },
                                new SpectrumPeak() { Mass = sn2, Intensity = 0.01 },
                            };

                    var foundCount = 0;
                    var averageIntensity = 0.0;
                    LipidMsmsCharacterizationUtility.countFragmentExistence(spectrum, query, ms2Tolerance, out foundCount, out averageIntensity);

                    if (foundCount == 2) { // now I set 2 as the correct level
                        var molecule = LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PE", LbmClass.PE, sn1Carbon, sn1Double,
                            sn2Carbon, sn2Double, averageIntensity);
                        candidates.Add(molecule);
                    }

                    return LipidMsmsCharacterizationUtility.returnAnnotationResult("PE", LbmClass.PE, "", theoreticalMz, adduct,
                        totalCarbon, totalDoubleBond, 0, candidates, 2);
                }
            }
            
            return null;
        }

        public static LipidMolecule JudgeIfLysopc(IMSScanProperty msScanProp, double ms2Tolerance,
            double theoreticalMz, int totalCarbon, int totalDoubleBond, // 
            int snCarbon, int snDoubleBond,
            AdductIon adduct) {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (snCarbon > totalCarbon) snCarbon = totalCarbon;
            if (snDoubleBond > totalDoubleBond) snDoubleBond = totalDoubleBond;
            if (adduct.IonMode == IonMode.Positive) { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+") {
                    if (totalCarbon > 28) return null; //  currently carbon > 28 is recognized as EtherPC
                    // seek 184.07332 (C5H15NO4P)
                    var threshold = 3.0;
                    var diagnosticMz = 184.07332;
                    var diagnosticMz2 = 104.106990;
                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIon1Found != true) return null;

                    // for eieio
                    var PEHeaderLoss = theoreticalMz - 141.019094261;
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
                else if (adduct.AdductIonName == "[M+Na]+") {
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
            AdductIon adduct) {
            var spectrum = msScanProp.Spectrum;
            if (spectrum == null || spectrum.Count == 0) return null;
            if (adduct.IonMode == IonMode.Positive) { // positive ion mode 
                if (adduct.AdductIonName == "[M+H]+") {
                    if (totalCarbon > 28) return null; //  currently carbon > 28 is recognized as EtherPE

                    // seek PreCursor -141(C2H8NO4P)
                    var threshold = 2.5;
                    var diagnosticMz = theoreticalMz - 141.019094;

                    var isClassIon1Found = LipidMsmsCharacterizationUtility.isDiagnosticFragmentExist(spectrum, ms2Tolerance, diagnosticMz, threshold);
                    if (isClassIon1Found == false) return null;
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
                    }
                    else {
                        return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPE", LbmClass.LPE, "", theoreticalMz, adduct,
                           totalCarbon, totalDoubleBond, 0, candidates, 1);
                    }


                }
                else if (adduct.AdductIonName == "[M+Na]+") {
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
                    }
                    else {
                        return LipidMsmsCharacterizationUtility.returnAnnotationResult("LPE", LbmClass.LPE, "", theoreticalMz, adduct,
                           totalCarbon, totalDoubleBond, 0, candidates, 1);
                    }
                }
            }
            
            return null;
        }

    }
}
