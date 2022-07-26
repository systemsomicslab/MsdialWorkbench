using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{

    public class EadMsCharacterizationResult
    {
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

    public static class EadMsCharacterizationUtility
    {

        public static (ILipid, double[]) GetDefaultCharacterizationResultForAlkylAcylGlycerols(
            ILipid molecule, EadMsCharacterizationResult result)
        {

            var chains = molecule.Chains;
            var score = result.TotalScore;
            var counter = result.TotalMatchedIonCount;

            if (!result.IsChainIonsExisted)
            { // chain existed expected: PC O-36:2
                var obj = new Lipid(molecule.LipidClass, molecule.Mass, new TotalChain(chains.CarbonCount, chains.DoubleBondCount, chains.OxidizedCount, 1, 1, 0));
                return (obj, new double[2] { score, counter });
            }
            else
            { // chain existed expected: PC O-18:0_18:2
                var deepChains = (SeparatedChains)chains;
                if (result.IsPositionIonsExisted && result.IsDoubleBondIonsExisted)
                { // chain existed expected: PC O-18:0/18:2(9,12)
                    return (molecule, new double[2] { score, counter });
                }
                else if (result.IsPositionIonsExisted)
                { // chain existed expected: PC O-18:0/18:2
                    var chain1 = deepChains.Chains[0].Accept(ChainShorthandNotation.Default, new ChainDecomposer<IChain>());
                    var chain2 = deepChains.Chains[1].Accept(ChainShorthandNotation.Default, new ChainDecomposer<IChain>());
                    var obj = new Lipid(molecule.LipidClass, molecule.Mass, new PositionLevelChains(chain1, chain2));
                    return (obj, new double[2] { score, counter });
                }
                else if (result.IsDoubleBondIonsExisted)
                { // chain existed expected: PC O-18:0_18:2(9,12)
                    var alkyl1 = deepChains.Chains[0];
                    var acyl2 = deepChains.Chains[1];
                    var obj = new Lipid(molecule.LipidClass, molecule.Mass, new MolecularSpeciesLevelChains(alkyl1, acyl2));
                    return (obj, new double[2] { score, counter });
                }
                else
                { // chain existed expected: PC O-18:0_18:2
                    var chain1 = deepChains.Chains[0].Accept(ChainShorthandNotation.Default, new ChainDecomposer<IChain>());
                    var chain2 = deepChains.Chains[1].Accept(ChainShorthandNotation.Default, new ChainDecomposer<IChain>());
                    var obj = new Lipid(molecule.LipidClass, molecule.Mass, new MolecularSpeciesLevelChains(chain1, chain2));
                    return (obj, new double[2] { score, counter });
                }
            }
        }

        public static (ILipid, double[]) GetDefaultCharacterizationResultForCeramides(
            ILipid molecule, EadMsCharacterizationResult result)
        {

            var chains = molecule.Chains;
            var score = result.TotalScore;
            var counter = result.TotalMatchedIonCount;
            var obj = new Lipid(molecule.LipidClass, molecule.Mass, chains);

            if (!result.IsChainIonsExisted)
            { // chain cannot determine
                obj = new Lipid(molecule.LipidClass, molecule.Mass, new TotalChain(chains.CarbonCount, chains.DoubleBondCount, chains.OxidizedCount, 1, 0, 1));
            }
            else if (!result.IsDoubleBondIonsExisted)
            { // chain existed expected: SM 18:1;2O/18:1
                var deepChains = (SeparatedChains)chains;
                var sphingo = deepChains.Chains[0];
                // need to consider (MT)
                //var sphingo = new SphingoChain(deepChains.Chains[0].CarbonCount,
                //            new DoubleBond(deepChains.Chains[0].DoubleBondCount),
                //            new Oxidized(deepChains.Chains[0].OxidizedCount));
                var nacyl = deepChains.Chains[1].Accept(ChainShorthandNotation.Default, new ChainDecomposer<IChain>());
                obj = new Lipid(molecule.LipidClass, molecule.Mass, new PositionLevelChains(sphingo, nacyl));
            }
            return (obj, new double[2] { score, counter });

            //if (!result.IsChainIonsExisted) { // chain existed expected: SM 18:1(4);2O/18:1(9)
            //    var obj = new Lipid(molecule.LipidClass, molecule.Mass, new TotalChain(chains.CarbonCount, chains.DoubleBondCount, chains.OxidizedCount, 1, 0, 1));
            //    return (obj, new double[2] { score, counter });
            //}
            //else { // chain existed expected: PC O-18:0_18:2
            //    var deepChains = (SeparatedChains)chains;
            //    if (result.IsPositionIonsExisted && result.IsDoubleBondIonsExisted) { // chain existed expected: SM 18:1(4);2O/18:1(9)
            //        return (molecule, new double[2] { score, counter });
            //    }
            //    else if (result.IsPositionIonsExisted) { // chain existed expected: SM 18:1;2O/18:1
            //        var sphingo = new SphingoChain(deepChains.Chains[0].CarbonCount,
            //            new DoubleBond(deepChains.Chains[0].DoubleBondCount),
            //            new Oxidized(deepChains.Chains[0].OxidizedCount));
            //        var nacyl = new AcylChain(deepChains.Chains[1].CarbonCount,
            //            new DoubleBond(deepChains.Chains[1].DoubleBondCount),
            //            new Oxidized(deepChains.Chains[1].OxidizedCount));
            //        var obj = new Lipid(molecule.LipidClass, molecule.Mass, new PositionLevelChains(sphingo, nacyl));
            //        return (obj, new double[2] { score, counter });
            //    }
            //    else if (result.IsDoubleBondIonsExisted) { // chain existed expected: SM 18:1(4);2O_18:1(9)
            //        var sphingo = deepChains.Chains[0];
            //        var nacyl = deepChains.Chains[1];
            //        var obj = new Lipid(molecule.LipidClass, molecule.Mass, new MolecularSpeciesLevelChains(sphingo, nacyl));
            //        return (obj, new double[2] { score, counter });
            //    }
            //    else { // chain existed expected: SM 18:1;2O_18:1
            //        var sphingo = new SphingoChain(deepChains.Chains[0].CarbonCount,
            //            new DoubleBond(deepChains.Chains[0].DoubleBondCount),
            //            new Oxidized(deepChains.Chains[0].OxidizedCount));
            //        var nacyl = new AcylChain(deepChains.Chains[1].CarbonCount,
            //            new DoubleBond(deepChains.Chains[1].DoubleBondCount),
            //            new Oxidized(deepChains.Chains[1].OxidizedCount));
            //        var obj = new Lipid(molecule.LipidClass, molecule.Mass, new MolecularSpeciesLevelChains(sphingo, nacyl));
            //        return (obj, new double[2] { score, counter });
            //    }
            //}
        }



        public static (ILipid, double[]) GetDefaultCharacterizationResultForGlycerophospholipid(
            ILipid molecule, EadMsCharacterizationResult result)
        {

            var chains = molecule.Chains;
            var score = result.TotalScore;
            var counter = result.TotalMatchedIonCount;

            if (!result.IsChainIonsExisted)
            { // chain existed expected: PC 36:2
                var obj = new Lipid(molecule.LipidClass, molecule.Mass, new TotalChain(chains.CarbonCount, chains.DoubleBondCount, chains.OxidizedCount, 2, 0, 0));
                return (obj, new double[2] { score, counter });
            }
            else
            { // chain existed expected: PC 18:0_18:2
                var deepChains = (SeparatedChains)chains;
                if (result.IsPositionIonsExisted && result.IsDoubleBondIonsExisted)
                { // chain existed expected: PC 18:0/18:2(9,12)
                    return (molecule, new double[2] { score, counter });
                }
                else if (result.IsPositionIonsExisted)
                { // chain existed expected: PC 18:0/18:2
                    var acyl1 = new AcylChain(deepChains.Chains[0].CarbonCount,
                        new DoubleBond(deepChains.Chains[0].DoubleBondCount),
                        new Oxidized(deepChains.Chains[0].OxidizedCount));
                    var acyl2 = new AcylChain(deepChains.Chains[1].CarbonCount,
                        new DoubleBond(deepChains.Chains[1].DoubleBondCount),
                        new Oxidized(deepChains.Chains[1].OxidizedCount));
                    var obj = new Lipid(molecule.LipidClass, molecule.Mass, new PositionLevelChains(acyl1, acyl2));
                    return (obj, new double[2] { score, counter });
                }
                else if (result.IsDoubleBondIonsExisted)
                { // chain existed expected: PC 18:0_18:2(9,12)
                    var acyl1 = deepChains.Chains[0];
                    var acyl2 = deepChains.Chains[1];
                    var obj = new Lipid(molecule.LipidClass, molecule.Mass, new MolecularSpeciesLevelChains(acyl1, acyl2));
                    return (obj, new double[2] { score, counter });
                }
                else
                { // chain existed expected: PC 18:0_18:2
                    var acyl1 = new AcylChain(deepChains.Chains[0].CarbonCount,
                        new DoubleBond(deepChains.Chains[0].DoubleBondCount),
                        new Oxidized(deepChains.Chains[0].OxidizedCount));
                    var acyl2 = new AcylChain(deepChains.Chains[1].CarbonCount,
                        new DoubleBond(deepChains.Chains[1].DoubleBondCount),
                        new Oxidized(deepChains.Chains[1].OxidizedCount));
                    var obj = new Lipid(molecule.LipidClass, molecule.Mass, new MolecularSpeciesLevelChains(acyl1, acyl2));
                    return (obj, new double[2] { score, counter });
                }
            }
        }

        public static (ILipid, double[]) GetDefaultCharacterizationResultForTriacylGlycerols(
            ILipid molecule, EadMsCharacterizationResult result)
        {

            var chains = molecule.Chains;
            var score = result.TotalScore;
            var counter = result.TotalMatchedIonCount;

            if (!result.IsChainIonsExisted)
            { // chain existed expected: TG 52:3
                var obj = new Lipid(molecule.LipidClass, molecule.Mass, new TotalChain(chains.CarbonCount, chains.DoubleBondCount, chains.OxidizedCount, 3, 0, 0));
                return (obj, new double[2] { score, counter });
            }
            else
            { // chain existed expected: TG 16:0_18:1_18:2
                var deepChains = (SeparatedChains)chains;
                if (result.IsPositionIonsExisted && result.IsDoubleBondIonsExisted)
                { // chain existed expected: TG 16:0/18:1(11)/18:2(9,12)
                    return (molecule, new double[2] { score, counter });
                }
                else if (result.IsPositionIonsExisted)
                { // chain existed expected: TG 16:0/18:1/18:2
                    var acyl1 = new AcylChain(deepChains.Chains[0].CarbonCount,
                        new DoubleBond(deepChains.Chains[0].DoubleBondCount),
                        new Oxidized(deepChains.Chains[0].OxidizedCount));
                    var acyl2 = new AcylChain(deepChains.Chains[1].CarbonCount,
                        new DoubleBond(deepChains.Chains[1].DoubleBondCount),
                        new Oxidized(deepChains.Chains[1].OxidizedCount));
                    var acyl3 = new AcylChain(deepChains.Chains[2].CarbonCount,
                        new DoubleBond(deepChains.Chains[2].DoubleBondCount),
                        new Oxidized(deepChains.Chains[2].OxidizedCount));
                    var obj = new Lipid(molecule.LipidClass, molecule.Mass, new PositionLevelChains(acyl1, acyl2, acyl3));
                    return (obj, new double[2] { score, counter });
                }
                else if (result.IsDoubleBondIonsExisted)
                { // chain existed expected:TG 16:0_18:1(11)_18:2(9,12)
                    var acyl1 = deepChains.Chains[0];
                    var acyl2 = deepChains.Chains[1];
                    var acyl3 = deepChains.Chains[2];
                    var obj = new Lipid(molecule.LipidClass, molecule.Mass, new MolecularSpeciesLevelChains(acyl1, acyl2, acyl3));
                    return (obj, new double[2] { score, counter });
                }
                else
                { // chain existed expected: TG 16:0_18:1_18:2
                    var acyl1 = new AcylChain(deepChains.Chains[0].CarbonCount,
                                               new DoubleBond(deepChains.Chains[0].DoubleBondCount),
                                               new Oxidized(deepChains.Chains[0].OxidizedCount));
                    var acyl2 = new AcylChain(deepChains.Chains[1].CarbonCount,
                        new DoubleBond(deepChains.Chains[1].DoubleBondCount),
                        new Oxidized(deepChains.Chains[1].OxidizedCount));
                    var acyl3 = new AcylChain(deepChains.Chains[2].CarbonCount,
                        new DoubleBond(deepChains.Chains[2].DoubleBondCount),
                        new Oxidized(deepChains.Chains[2].OxidizedCount));
                    var obj = new Lipid(molecule.LipidClass, molecule.Mass, new MolecularSpeciesLevelChains(acyl1, acyl2, acyl3));
                    return (obj, new double[2] { score, counter });
                }
            }
        }


        public static EadMsCharacterizationResult GetDefaultScoreForGlycerophospholipid(
            IMSScanProperty scan, MoleculeMsReference reference,
            float tolerance, float mzBegin, float mzEnd,
            double classIonCutoff, double chainIonCutoff, double positionIonCutoff, double doublebondIonCutoff)
        {

            var exp_spectrum = scan.Spectrum;
            var ref_spectrum = reference.Spectrum;
            var adduct = reference.AdductType;

            var result = new EadMsCharacterizationResult();

            var matchedpeaks = MsScanMatching.GetMachedSpectralPeaks(exp_spectrum, ref_spectrum, tolerance, mzBegin, mzEnd);

            // check lipid class ion's existence
            var classions = matchedpeaks.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.metaboliteclass)).ToList();
            var isClassMustIonsExisted = classions.All(ion => !ion.IsAbsolutelyRequiredFragmentForAnnotation || ion.IsMatched);
            // classions.Where(n => n.IsAbsolutelyRequiredFragmentForAnnotation).Count()
            // == classions.Where(n => n.IsAbsolutelyRequiredFragmentForAnnotation && n.IsMatched).Count()
            // ? true : false;
            var classionsDetected = classions.Count(n => n.IsMatched);
            var isClassIonExisted = isClassMustIonsExisted && classionsDetected >= classIonCutoff
                ? true : false;

            result.ClassIonsDetected = classionsDetected;
            result.IsClassIonsExisted = isClassIonExisted;


            // check lipid chain ion's existence
            var chainIons = matchedpeaks.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.acylchain)).ToList();
            var isChainMustIonsExisted = chainIons.All(ion => !ion.IsAbsolutelyRequiredFragmentForAnnotation || ion.IsMatched);
            // chainIons.Where(n => n.IsAbsolutelyRequiredFragmentForAnnotation).Count()
            // == chainIons.Where(n => n.IsAbsolutelyRequiredFragmentForAnnotation && n.IsMatched).Count()
            // ? true : false;
            var chainIonsDetected = chainIons.Count(n => n.IsMatched);
            var isChainIonExisted = isChainMustIonsExisted && chainIonsDetected >= chainIonCutoff
                ? true : false;

            result.ChainIonsDetected = chainIonsDetected;
            result.IsChainIonsExisted = isChainIonExisted;

            // check lipid position ion's existence
            var positionIons = matchedpeaks.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.snposition)).ToList();
            var isPositionMustIonsExisted = positionIons.All(ion => !ion.IsAbsolutelyRequiredFragmentForAnnotation || ion.IsMatched);
            // positionIons.Where(n => n.IsAbsolutelyRequiredFragmentForAnnotation).Count()
            // == positionIons.Where(n => n.IsAbsolutelyRequiredFragmentForAnnotation && n.IsMatched).Count()
            // ? true : false;
            var positionIonsDetected = positionIons.Count(n => n.IsMatched);
            var isPositionIonExisted = isPositionMustIonsExisted && positionIonsDetected >= positionIonCutoff
                ? true : false;

            result.PositionIonsDetected = positionIonsDetected;
            result.IsPositionIonsExisted = isPositionIonExisted;

            // check the dtected ion nudouble bond position
            var doublebondIons = matchedpeaks.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.doublebond)).ToList();
            var doublebondIons_matched = doublebondIons.Where(n => n.IsMatched).ToList();
            var matchedCount = doublebondIons_matched.Count;
            var matchedPercent = matchedCount / (doublebondIons.Count + 1e-10);
            var matchedCoefficient = GetMatchedCoefficient(doublebondIons_matched);

            //var dbhigh_enriched = doublebondIons_matched.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.doublebond_high)).Sum(n => n.Resolution);
            //var dblow_enriched = doublebondIons_matched.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.doublebond_low)).Sum(n => n.Resolution);
            //var dbBonusScore = dbhigh_enriched > 3.0 * dblow_enriched ? 0.5 : 0.0;

            var isDoubleBondIdentified = matchedPercent > doublebondIonCutoff ? true : false;

            result.DoubleBondIonsDetected = (int)matchedCount;
            result.DoubleBondMatchedPercent = matchedPercent;
            result.IsDoubleBondIonsExisted = isDoubleBondIdentified;

            // total score
            result.ClassIonScore = isClassIonExisted ? 1.0 : 0.0;
            result.ChainIonScore = isChainIonExisted ? 1.0 : 0.0;
            result.PositionIonScore = isPositionIonExisted ? 1.0 : 0.0;
            result.DoubleBondIonScore = matchedPercent + matchedCoefficient;

            var score = result.ClassIonScore + result.ChainIonScore + result.PositionIonScore + result.DoubleBondIonScore;
            var counter = classionsDetected + chainIonsDetected + positionIonsDetected + matchedCount;
            result.TotalScore = score;
            result.TotalMatchedIonCount = counter;

            return result;

            //if (adduct.AdductIonName == "[M+H]+") {

            //    // check lipid class ion's existence
            //    var classions = ref_spectrum.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.metaboliteclass)).ToList();
            //    var classionsDetected = EadMsCharacterizationUtility.CountDetectedIons(exp_spectrum, classions, tolerance);
            //    var isClassIonExisted = false;

            //    var classMustIons = classions.Where(n => n.IsAbsolutelyRequiredFragmentForAnnotation == true).ToList();
            //    var isClassMustIonsExisted = IsDiagnosticFragmentsExist(exp_spectrum, classMustIons, tolerance);

            //    if (isClassMustIonsExisted && classionsDetected >= classIonCutoff) {
            //        isClassIonExisted = true;
            //    }
            //    result.ClassIonsDetected = classionsDetected;
            //    result.IsClassIonsExisted = isClassIonExisted;

            //    // check lipid chain ion's existence
            //    var chainIons = ref_spectrum.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.acylchain)).ToList();
            //    var chainIonsDetected = EadMsCharacterizationUtility.CountDetectedIons(exp_spectrum, chainIons, tolerance);
            //    var isChainIonExisted = false;
            //    if (chainIonsDetected >= chainIonCutoff) {
            //        isChainIonExisted = true;
            //    }
            //    result.ChainIonsDetected = chainIonsDetected;
            //    result.IsChainIonsExisted = isChainIonExisted;

            //    // check lipid position ion's existence
            //    var positionIons = ref_spectrum.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.snposition)).ToList();
            //    var positionIonsDetected = EadMsCharacterizationUtility.CountDetectedIons(exp_spectrum, positionIons, tolerance); ;
            //    var isPositionIonExisted = false;
            //    if (positionIonsDetected >= positionIonCutoff) {
            //        isPositionIonExisted = true;
            //    }

            //    result.PositionIonsDetected = positionIonsDetected;
            //    result.IsPositionIonsExisted = isPositionIonExisted;

            //    // check the dtected ion nudouble bond position
            //    var doublebondIons = ref_spectrum.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.doublebond)).ToList();

            //    var matchedpeaks = MsScanMatching.GetMachedSpectralPeaks(exp_spectrum, doublebondIons, tolerance, mzBegin, mzEnd);
            //    var matchedpeaks_matched = matchedpeaks.Where(n => n.IsMatched).ToList();

            //    //var matchedions = MsScanMatching.GetMatchedPeaksScores(exp_spectrum, doublebondIons, tolerance, mzBegin, mzEnd);
            //    var matchedCount = matchedpeaks_matched.Count;
            //    var matchedPercent = (double)matchedCount / (double)doublebondIons.Count;

            //    var dbhigh_enriched = matchedpeaks_matched.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.doublebond_high)).Sum(n => n.Resolution);
            //    var dblow_enriched = matchedpeaks_matched.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.doublebond_low)).Sum(n => n.Resolution);
            //    var dbBonusScore = dbhigh_enriched > 3.0 * dblow_enriched ? 0.5 : 0.0;

            //    var isDoubleBondIdentified = matchedPercent > doublebondIonCutoff ? true : false;

            //    result.DoubleBondIonsDetected = (int)matchedCount;
            //    result.DoubleBondMatchedPercent = matchedPercent;
            //    result.IsDoubleBondIonsExisted = isDoubleBondIdentified;

            //    result.ClassIonScore = isClassIonExisted ? 1.0 : 0.0;
            //    result.ChainIonScore = isChainIonExisted ? 1.0 : 0.0;
            //    result.PositionIonScore = isPositionIonExisted ? 1.0 : 0.0;
            //    result.DoubleBondIonScore = matchedPercent + dbBonusScore;

            //    var score = result.ClassIonScore + result.ChainIonScore + result.PositionIonScore + result.DoubleBondIonScore;
            //    var counter = classionsDetected + chainIonsDetected + positionIonsDetected + matchedCount;
            //    result.TotalScore = score;
            //    result.TotalMatchedIonCount = counter;

            //}
            //return result;
        }

        private static double GetMatchedCoefficient(List<SpectrumPeak> peaks) {
            double sum1 = 0, sum2 = 0, mean1 = 0, mean2 = 0, covariance = 0, sqrt1 = 0, sqrt2 = 0;
            for (int i = 0; i < peaks.Count; i++) {
                sum1 += peaks[i].Resolution;
                sum2 += peaks[i].Intensity;
            }
            mean1 = (double)(sum1 / peaks.Count);
            mean2 = (double)(sum2 / peaks.Count);

            for (int i = 0; i < peaks.Count; i++) {
                covariance += (peaks[i].Resolution - mean1) * (peaks[i].Intensity - mean2);
                sqrt1 += Math.Pow(peaks[i].Resolution - mean1, 2);
                sqrt2 += Math.Pow(peaks[i].Intensity - mean2, 2);
            }
            if (sqrt1 == 0 || sqrt2 == 0)
                return 0;
            else
                return (double)(covariance / Math.Sqrt(sqrt1 * sqrt2));
        }

        public static bool IsDiagnosticFragmentsExist(List<SpectrumPeak> spectrum, List<SpectrumPeak> refSpectrum, double mzTolerance)
        {
            var isAllExisted = true;
            if (refSpectrum.IsEmptyOrNull()) return true;
            foreach (var refpeak in refSpectrum)
            {
                if (!IsDiagnosticFragmentExist(spectrum, mzTolerance, refpeak.Mass, refpeak.Intensity * 0.01))
                {
                    isAllExisted = false;
                    break;
                }
            }
            return isAllExisted;
        }


        public static bool IsDiagnosticFragmentExist(List<SpectrumPeak> spectrum,
            double mzTolerance,
            double diagnosticMz,
            double threshold)
        {
            for (int i = 0; i < spectrum.Count; i++)
            {
                var mz = spectrum[i].Mass;
                var intensity = spectrum[i].Intensity; // should be normalized by max intensity to 100

                if (intensity > threshold && Math.Abs(mz - diagnosticMz) < mzTolerance)
                {
                    return true;
                }
            }
            return false;
        }

        public static int CountDetectedIons(
            List<SpectrumPeak> exp_spectrum,
            List<SpectrumPeak> ref_spectrum,
            double tolerance)
        {
            var ionDetectedCounter = 0;
            foreach (var ion in ref_spectrum)
            {
                if (EadMsCharacterizationUtility.IsDiagnosticFragmentExist(exp_spectrum, tolerance, ion.Mass, ion.Intensity * 0.0001))
                {
                    ionDetectedCounter++;
                }
            }
            return ionDetectedCounter;
        }

    }
}
