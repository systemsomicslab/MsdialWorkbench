using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.Lipidomics {

    public class EadMsCharacterizationResult {
        public int ClassIonsDetected { get; set; }
        public int ChainIonsDetected { get; set; }
        public int PositionIonsDetected { get; set; }
        public int DoubleBondIonsDetected { get; set; }
        public double DoubleBondMatchedPercent { get; set; }
        public bool IsClassIonsExisted { get; set; }
        public bool IsChainIonsExisted { get; set; }
        public bool IsPositionIonsExisted { get; set; }
        public bool IsDoubleBondIonsExisted { get; set; }
        public double ClassIonScore { get; set; }
        public double ChainIonScore { get; set; }
        public double PositionIonScore { get; set; }
        public double DoubleBondIonScore { get; set; }
        public double TotalMatchedIonCount { get; set; }
        public double TotalScore { get; set; }
    }

    public static class EadMsCharacterizationUtility {

        public static (ILipid, double[]) GetDefaultCharacterizationResultForGlycerophospholipid(
            ILipid molecule, EadMsCharacterizationResult result) {

            var chains = molecule.Chains;
            var score = result.TotalScore;
            var counter = result.TotalMatchedIonCount;

            if (!result.IsChainIonsExisted) { // chain existed expected: PC 36:2
                var obj = new Lipid(LbmClass.PC, molecule.Mass, new TotalChain(chains.CarbonCount, chains.DoubleBondCount, chains.OxidizedCount,
                    2, 0, 0));
                return (obj, new double[2] { score, counter });
            }
            else { // chain existed expected: PC 18:0_18:2
                var deepChains = (SeparatedChains)chains;
                if (result.IsPositionIonsExisted && result.IsDoubleBondIonsExisted) { // chain existed expected: PC 18:0/18:2(9,12)
                    return (molecule, new double[2] { score, counter });
                }
                else if (result.IsPositionIonsExisted) { // chain existed expected: PC 18:0/18:2
                    var acyl1 = new AcylChain(deepChains.Chains[0].CarbonCount,
                        new DoubleBond(deepChains.Chains[0].DoubleBondCount),
                        new Oxidized(0));
                    var acyl2 = new AcylChain(deepChains.Chains[1].CarbonCount,
                        new DoubleBond(deepChains.Chains[1].DoubleBondCount),
                        new Oxidized(0));
                    var obj = new Lipid(LbmClass.PC, molecule.Mass, new PositionLevelChains(acyl1, acyl2));
                    return (obj, new double[2] { score, counter });
                }
                else if (result.IsDoubleBondIonsExisted) { // chain existed expected: PC 18:0_18:2(9,12)
                    var acyl1 = deepChains.Chains[0];
                    var acyl2 = deepChains.Chains[1];
                    var obj = new Lipid(LbmClass.PC, molecule.Mass, new MolecularSpeciesLevelChains(acyl1, acyl2));
                    return (obj, new double[2] { score, counter });
                }
                else { // chain existed expected: PC 18:0_18:2
                    var acyl1 = new AcylChain(deepChains.Chains[0].CarbonCount,
                                               new DoubleBond(deepChains.Chains[0].DoubleBondCount),
                                               new Oxidized(0));
                    var acyl2 = new AcylChain(deepChains.Chains[1].CarbonCount,
                        new DoubleBond(deepChains.Chains[1].DoubleBondCount),
                        new Oxidized(0));
                    var obj = new Lipid(LbmClass.PC, molecule.Mass, new MolecularSpeciesLevelChains(acyl1, acyl2));
                    return (obj, new double[2] { score, counter });
                }
            }
        }


        public static EadMsCharacterizationResult GetDefaultScoreForGlycerophospholipid(
            IMSScanProperty scan, MoleculeMsReference reference,
            float tolerance, float mzBegin, float mzEnd, 
            double classIonCutoff, double chainIonCutoff, double positionIonCutoff, double doublebondIonCutoff) {

            var exp_spectrum = scan.Spectrum;
            var ref_spectrum = reference.Spectrum;
            var adduct = reference.AdductType;

            var result = new EadMsCharacterizationResult();

            if (adduct.AdductIonName == "[M+H]+") {
                // check lipid class ion's existence
                var classions = ref_spectrum.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.metaboliteclass)).ToList();
                var classionsDetected = EadMsCharacterizationUtility.CountDetectedIons(exp_spectrum, classions, tolerance);
                var isClassIonExisted = false;

                var classMustIons = classions.Where(n => n.IsAbsolutelyRequiredFragmentForAnnotation == true).ToList();
                var isClassMustIonsExisted = IsDiagnosticFragmentsExist(exp_spectrum, classMustIons, tolerance);

                if (isClassMustIonsExisted && classionsDetected >= classIonCutoff) {
                    isClassIonExisted = true;
                }
                result.ClassIonsDetected = classionsDetected;
                result.IsClassIonsExisted = isClassIonExisted;

                // check lipid chain ion's existence
                var chainIons = ref_spectrum.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.acylchain)).ToList();
                var chainIonsDetected = EadMsCharacterizationUtility.CountDetectedIons(exp_spectrum, chainIons, tolerance);
                var isChainIonExisted = false;
                if (chainIonsDetected >= chainIonCutoff) {
                    isChainIonExisted = true;
                }
                result.ChainIonsDetected = chainIonsDetected;
                result.IsChainIonsExisted = isChainIonExisted;

                // check lipid position ion's existence
                var positionIons = ref_spectrum.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.snposition)).ToList();
                var positionIonsDetected = EadMsCharacterizationUtility.CountDetectedIons(exp_spectrum, positionIons, tolerance); ;
                var isPositionIonExisted = false;
                if (positionIonsDetected >= positionIonCutoff) {
                    isPositionIonExisted = true;
                }

                result.PositionIonsDetected = positionIonsDetected;
                result.IsPositionIonsExisted = isPositionIonExisted;

                // check the dtected ion nudouble bond position
                var doublebondIons = ref_spectrum.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.doublebond)).ToList();
                var matchedions = MsScanMatching.GetMatchedPeaksScores(exp_spectrum, doublebondIons, tolerance, mzBegin, mzEnd);
                var matchedPercent = matchedions[0];
                var matchedCount = matchedions[1];
                var isDoubleBondIdentified = matchedPercent > doublebondIonCutoff ? true : false;

                result.DoubleBondIonsDetected = (int)matchedCount;
                result.DoubleBondMatchedPercent = matchedPercent;
                result.IsDoubleBondIonsExisted = isDoubleBondIdentified;

                result.ClassIonScore = isClassIonExisted ? 1.0 : 0.0;
                result.ChainIonScore = isChainIonExisted ? 1.0 : 0.0;
                result.PositionIonScore = isPositionIonExisted ? 1.0 : 0.0;
                result.DoubleBondIonScore = matchedPercent;
                
                var score = result.ClassIonScore + result.ChainIonScore + result.PositionIonScore + result.DoubleBondIonScore;
                var counter = classionsDetected + chainIonsDetected + positionIonsDetected + matchedCount;
                result.TotalScore = score;
                result.TotalMatchedIonCount = counter;

            }
            return result;
        }

        public static bool IsDiagnosticFragmentsExist(List<SpectrumPeak> spectrum, List<SpectrumPeak> refSpectrum, double mzTolerance) {
            var isAllExisted = true;
            if (refSpectrum.IsEmptyOrNull()) return true;
            foreach (var refpeak in refSpectrum) {
                if (!IsDiagnosticFragmentExist(spectrum, mzTolerance, refpeak.Mass, refpeak.Intensity * 0.01)) {
                    isAllExisted = false;
                    break;
                }
            }
            return isAllExisted;
        }


        public static bool IsDiagnosticFragmentExist(List<SpectrumPeak> spectrum, 
            double mzTolerance,
            double diagnosticMz, 
            double threshold) {
            for (int i = 0; i < spectrum.Count; i++) {
                var mz = spectrum[i].Mass;
                var intensity = spectrum[i].Intensity; // should be normalized by max intensity to 100

                if (intensity > threshold && Math.Abs(mz - diagnosticMz) < mzTolerance) {
                    return true;
                }
            }
            return false;
        }

        public static int CountDetectedIons(
            List<SpectrumPeak> exp_spectrum,
            List<SpectrumPeak> ref_spectrum,
            double tolerance) {
            var ionDetectedCounter = 0;
            foreach (var ion in ref_spectrum) {
                if (EadMsCharacterizationUtility.IsDiagnosticFragmentExist(exp_spectrum, tolerance, ion.Mass, ion.Intensity * 0.0001)) {
                    ionDetectedCounter++;
                }
            }
            return ionDetectedCounter;
        }

    }
}
