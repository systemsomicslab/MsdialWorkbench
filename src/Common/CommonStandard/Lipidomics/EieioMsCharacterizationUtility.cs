using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{

    public class LipidMsCharacterizationResult
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

    public class DiagnosticIon {
        public double Mz { get; set; }
        public double MzTolerance { get; set; }
        public double IonAbundanceCutOff { get; set; }
    }

    public static class StandardMsCharacterizationUtility
    {

        public static (ILipid, double[]) GetDefaultCharacterizationResultForAlkylAcylGlycerols(
           ILipid molecule, LipidMsCharacterizationResult result)
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
                    var chain1 = deepChains.Chains[0].Accept<IChain>(ChainShorthandNotation.Default, new ChainDecomposer<IChain>());
                    var chain2 = deepChains.Chains[1].Accept<IChain>(ChainShorthandNotation.Default, new ChainDecomposer<IChain>());
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
                    var chain1 = deepChains.Chains[0].Accept<IChain>(ChainShorthandNotation.Default, new ChainDecomposer<IChain>());
                    var chain2 = deepChains.Chains[1].Accept<IChain>(ChainShorthandNotation.Default, new ChainDecomposer<IChain>());
                    var obj = new Lipid(molecule.LipidClass, molecule.Mass, new MolecularSpeciesLevelChains(chain1, chain2));
                    return (obj, new double[2] { score, counter });
                }
            }
        }

        public static (ILipid, double[]) GetDefaultCharacterizationResultForCeramides(
            ILipid molecule, LipidMsCharacterizationResult result)
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
                var nacyl = deepChains.Chains[1].Accept<IChain>(ChainShorthandNotation.Default, new ChainDecomposer<IChain>());
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
            ILipid molecule, LipidMsCharacterizationResult result)
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
                    var acyls = deepChains.Chains.OrderBy(n => n.DoubleBondCount).ThenBy(n => n.CarbonCount).ToList();
                    var obj = new Lipid(molecule.LipidClass, molecule.Mass, new MolecularSpeciesLevelChains(acyls[0], acyls[1]));
                    return (obj, new double[2] { score, counter });
                }
                else
                { // chain existed expected: PC 18:0_18:2
                    var acyls = deepChains.Chains.OrderBy(n => n.DoubleBondCount).ThenBy(n => n.CarbonCount).ToList();
                    var acyl1 = new AcylChain(acyls[0].CarbonCount,
                        new DoubleBond(acyls[0].DoubleBondCount),
                        new Oxidized(acyls[0].OxidizedCount));
                    var acyl2 = new AcylChain(acyls[1].CarbonCount,
                        new DoubleBond(acyls[1].DoubleBondCount),
                        new Oxidized(acyls[1].OxidizedCount));
                    var obj = new Lipid(molecule.LipidClass, molecule.Mass, new MolecularSpeciesLevelChains(acyl1, acyl2));
                    return (obj, new double[2] { score, counter });
                }
            }
        }

        

        public static (ILipid, double[]) GetDefaultCharacterizationResultForTriacylGlycerols(
            ILipid molecule, LipidMsCharacterizationResult result)
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
                    var acyls = deepChains.Chains.OrderBy(n => n.DoubleBondCount).ThenBy(n => n.CarbonCount).ToList();
                    var obj = new Lipid(molecule.LipidClass, molecule.Mass, new MolecularSpeciesLevelChains(acyls[0], acyls[1], acyls[2]));
                    return (obj, new double[2] { score, counter });
                }
                else
                { // chain existed expected: TG 16:0_18:1_18:2
                    var acyls = deepChains.Chains.OrderBy(n => n.DoubleBondCount).ThenBy(n => n.CarbonCount).ToList();
                    var acyl1 = new AcylChain(acyls[0].CarbonCount,
                                               new DoubleBond(acyls[0].DoubleBondCount),
                                               new Oxidized(acyls[0].OxidizedCount));
                    var acyl2 = new AcylChain(acyls[1].CarbonCount,
                        new DoubleBond(acyls[1].DoubleBondCount),
                        new Oxidized(acyls[1].OxidizedCount));
                    var acyl3 = new AcylChain(acyls[2].CarbonCount,
                        new DoubleBond(acyls[2].DoubleBondCount),
                        new Oxidized(acyls[2].OxidizedCount));
                    var obj = new Lipid(molecule.LipidClass, molecule.Mass, new MolecularSpeciesLevelChains(acyl1, acyl2, acyl3));
                    return (obj, new double[2] { score, counter });
                }
            }
        }

        public static (ILipid, double[]) GetDefaultCharacterizationResultForSingleAcylChainLipid( // CAR, steroidal ether etc.
            ILipid molecule, LipidMsCharacterizationResult result)
        {
            var chains = molecule.Chains;
            var deepChains = (SeparatedChains)chains;
            var acyl1 = deepChains.Chains[0];
            var score = result.TotalScore;
            var counter = result.TotalMatchedIonCount;
            if (chains.OxidizedCount > 0) //TBC
            {
                acyl1 = new AcylChain(deepChains.Chains[0].CarbonCount,
                        new DoubleBond(deepChains.Chains[0].DoubleBondCount),
                        new Oxidized(deepChains.Chains[0].OxidizedCount));
            }
            var obj = new Lipid(molecule.LipidClass, molecule.Mass, new MolecularSpeciesLevelChains(acyl1));
            return (obj, new double[2] { score, counter });
        }

        public static double GetMatchedCoefficient(List<SpectrumPeak> peaks)
        {
            double sum1 = 0, sum2 = 0, mean1 = 0, mean2 = 0, covariance = 0, sqrt1 = 0, sqrt2 = 0;
            for (int i = 0; i < peaks.Count; i++)
            {
                sum1 += peaks[i].Resolution;
                sum2 += peaks[i].Intensity;
            }
            mean1 = (double)(sum1 / peaks.Count);
            mean2 = (double)(sum2 / peaks.Count);

            for (int i = 0; i < peaks.Count; i++)
            {
                covariance += (peaks[i].Resolution - mean1) * (peaks[i].Intensity - mean2);
                sqrt1 += Math.Pow(peaks[i].Resolution - mean1, 2);
                sqrt2 += Math.Pow(peaks[i].Intensity - mean2, 2);
            }
            if (sqrt1 == 0 || sqrt2 == 0)
                return 0;
            else
                return (double)(covariance / Math.Sqrt(sqrt1 * sqrt2));
        }

        public static bool IsDiagnosticFragmentsExist(IReadOnlyList<SpectrumPeak> spectrum, IReadOnlyList<SpectrumPeak> refSpectrum, double mzTolerance)
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

        public static bool IsDiagnosticFragmentsExist(IReadOnlyList<SpectrumPeak> spectrum, IReadOnlyList<DiagnosticIon> dIons) {
            var isAllExisted = true;
            if (dIons.IsEmptyOrNull()) return true;
            foreach (var ion in dIons) {
                if (!IsDiagnosticFragmentExist_ResolutionUsed4Intensity(spectrum, ion.MzTolerance, ion.Mz, ion.IonAbundanceCutOff)) {
                    isAllExisted = false;
                    break;
                }
            }
            return isAllExisted;
        }

        public static bool IsDiagnosticFragmentExist(IReadOnlyList<SpectrumPeak> spectrum,
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

        public static bool IsDiagnosticFragmentExist_ResolutionUsed4Intensity(IReadOnlyList<SpectrumPeak> spectrum,
            double mzTolerance,
            double diagnosticMz,
            double threshold) {
            for (int i = 0; i < spectrum.Count; i++) {
                var mz = spectrum[i].Mass;
                var intensity = spectrum[i].Resolution; // should be normalized by max intensity to 100

                if (intensity > threshold && Math.Abs(mz - diagnosticMz) < mzTolerance) {
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
                if (IsDiagnosticFragmentExist(exp_spectrum, tolerance, ion.Mass, ion.Intensity * 0.0001))
                {
                    ionDetectedCounter++;
                }
            }
            return ionDetectedCounter;
        }
    }

    public static class OadMsCharacterizationUtility
    {
        public static LipidMsCharacterizationResult GetDefaultScore(
            IMSScanProperty scan, MoleculeMsReference reference,
            float tolerance, float mzBegin, float mzEnd,
            double classIonCutoff, double chainIonCutoff, double positionIonCutoff, double doublebondIonCutoff)
        {

            var exp_spectrum = scan.Spectrum;
            var ref_spectrum = reference.Spectrum;
            var adduct = reference.AdductType;

            var result = new LipidMsCharacterizationResult();

            var matchedpeaks = MsScanMatching.GetMachedSpectralPeaks(exp_spectrum, ref_spectrum, tolerance, mzBegin, mzEnd);

            // check lipid class ion's existence
            var classions = matchedpeaks.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.metaboliteclass)).ToList();
            var isClassMustIonsExisted = classions.All(ion => !ion.IsAbsolutelyRequiredFragmentForAnnotation || ion.IsMatched);
            var classionsDetected = classions.Count(n => n.IsMatched);
            var isClassIonExisted = isClassMustIonsExisted && classionsDetected >= classIonCutoff
                ? true : false;

            result.ClassIonsDetected = classionsDetected;
            result.IsClassIonsExisted = isClassIonExisted;


            // check lipid chain ion's existence
            var chainIons = matchedpeaks.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.acylchain)).ToList();
            var isChainMustIonsExisted = chainIons.All(ion => !ion.IsAbsolutelyRequiredFragmentForAnnotation || ion.IsMatched);
            var chainIonsDetected = chainIons.Count(n => n.IsMatched);
            var isChainIonExisted = isChainMustIonsExisted && chainIonsDetected >= chainIonCutoff
                ? true : false;

            result.ChainIonsDetected = chainIonsDetected;
            result.IsChainIonsExisted = isChainIonExisted;

            // check lipid position ion's existence
            //var positionIons = matchedpeaks.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.snposition)).ToList();
            //var isPositionMustIonsExisted = positionIons.All(ion => !ion.IsAbsolutelyRequiredFragmentForAnnotation || ion.IsMatched); 
            //var positionIonsDetected = positionIons.Count(n => n.IsMatched);
            //var isPositionIonExisted = isPositionMustIonsExisted && positionIonsDetected >= positionIonCutoff
            //    ? true : false;
            var positionIonsDetected = 0;
            var isPositionIonExisted = false;

            result.PositionIonsDetected = positionIonsDetected;
            result.IsPositionIonsExisted = isPositionIonExisted;

            // check the dtected ion nudouble bond position
            var doublebondIons = matchedpeaks.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.doublebond)).ToList();
            var doublebondIons_matched = doublebondIons.Where(n => n.IsMatched).ToList();
            var matchedCount = doublebondIons_matched.Count;
            var matchedPercent = matchedCount / (doublebondIons.Count + 1e-10);
            var matchedCoefficient = StandardMsCharacterizationUtility.GetMatchedCoefficient(doublebondIons_matched);

            var essentialDBIons = matchedpeaks.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.doublebond_high)).ToList();
            var essentialDBIons_matched = essentialDBIons.Where(n => n.IsMatched).ToList();
            if (essentialDBIons.Count == essentialDBIons_matched.Count)
            {
                matchedCoefficient += 1.5;
            }

            var isDoubleBondIdentified = essentialDBIons.Count == essentialDBIons_matched.Count ? true : false;

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
        }
    }

    public static class EieioMsCharacterizationUtility
    {
        public static LipidMsCharacterizationResult GetDefaultScore(
            IMSScanProperty scan, MoleculeMsReference reference,
            float tolerance, float mzBegin, float mzEnd,
            double classIonCutoff, double chainIonCutoff, double positionIonCutoff, double doublebondIonCutoff,
            IReadOnlyList<DiagnosticIon> dIons4class = null, IReadOnlyList<DiagnosticIon> dIons4chain = null,
            IReadOnlyList<DiagnosticIon> dIons4position = null, IReadOnlyList<DiagnosticIon> dIons4db = null)
        {

            var exp_spectrum = scan.Spectrum;
            var ref_spectrum = reference.Spectrum;
            var adduct = reference.AdductType;

            var result = new LipidMsCharacterizationResult();

            var matchedpeaks = MsScanMatching.GetMachedSpectralPeaks(exp_spectrum, ref_spectrum, tolerance, mzBegin, mzEnd);

            // check lipid class ion's existence
            var classions = matchedpeaks.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.metaboliteclass)).ToList();
            var isClassMustIonsExisted = classions.All(ion => !ion.IsAbsolutelyRequiredFragmentForAnnotation || ion.IsMatched);
            var isClassAdvancedFilter = StandardMsCharacterizationUtility.IsDiagnosticFragmentsExist(classions, dIons4class);
            var classions_matched = classions.Where(n => n.IsMatched).ToList();
            var classionsDetected = classions_matched.Count();
            var isClassIonExisted = isClassMustIonsExisted && isClassAdvancedFilter && classionsDetected >= classIonCutoff
                ? true : false;

            result.ClassIonsDetected = classionsDetected;
            result.IsClassIonsExisted = isClassIonExisted;
            result.ClassIonScore = isClassIonExisted ? classions_matched.Sum(n => n.Resolution) / 100.0 : 0.0;


            // check lipid chain ion's existence
            var chainIons = matchedpeaks.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.acylchain)).ToList();
            var isChainMustIonsExisted = chainIons.All(ion => !ion.IsAbsolutelyRequiredFragmentForAnnotation || ion.IsMatched);
            var isChainAdvancedFilter = StandardMsCharacterizationUtility.IsDiagnosticFragmentsExist(chainIons, dIons4chain);
            var chainIons_matched = chainIons.Where(n => n.IsMatched).ToList();
            var chainIonsDetected = chainIons_matched.Count();
            var isChainIonExisted = isChainMustIonsExisted && isChainAdvancedFilter && chainIonsDetected >= chainIonCutoff
                ? true : false;

            result.ChainIonsDetected = chainIonsDetected;
            result.IsChainIonsExisted = isChainIonExisted;
            result.ChainIonScore = isChainIonExisted ? chainIons_matched.Sum(n => n.Resolution) / 100.0 : 0.0;

            // check lipid position ion's existence
            var isPositionIonExisted = false;
            var positionIonsDetected = 0;
            if (positionIonCutoff > 0) {
                var positionIons = matchedpeaks.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.snposition)).ToList();
                var isPositionMustIonsExisted = positionIons.All(ion => !ion.IsAbsolutelyRequiredFragmentForAnnotation || ion.IsMatched);
                var isPositionAdvancedFilter = StandardMsCharacterizationUtility.IsDiagnosticFragmentsExist(positionIons, dIons4position);
                var positionIons_matched = positionIons.Where(n => n.IsMatched).ToList();
                
                positionIonsDetected = positionIons_matched.Count();
                isPositionIonExisted = isPositionMustIonsExisted && isPositionAdvancedFilter && positionIonsDetected >= positionIonCutoff
                    ? true : false;
                result.PositionIonsDetected = positionIonsDetected;
                result.IsPositionIonsExisted = isPositionIonExisted;
                result.PositionIonScore = isPositionIonExisted ? positionIons_matched.Sum(n => n.Resolution) / 100.0 : 0.0;
            }

            // check the dtected ion nudouble bond position
            var doublebondIons = matchedpeaks.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.doublebond)).ToList();
            var doublebondIons_matched = doublebondIons.Where(n => n.IsMatched).ToList();

            var doublebondHighIons = 
                ref_spectrum
                .Where(n => n.SpectrumComment.HasFlag(SpectrumComment.doublebond_high))
                .Select(n => new DiagnosticIon() { Mz = n.Mass, IonAbundanceCutOff = 0.0000001, MzTolerance = tolerance })
                .ToList();
            var doublebondHighAndLowIons =
               ref_spectrum
               .Where(n => n.SpectrumComment.HasFlag(SpectrumComment.doublebond_high) || n.SpectrumComment.HasFlag(SpectrumComment.doublebond_low))
               .Select(n => new DiagnosticIon() { Mz = n.Mass, IonAbundanceCutOff = 0.0000001, MzTolerance = tolerance })
               .ToList();

            var isDoublebondAdvancedFilter = StandardMsCharacterizationUtility.IsDiagnosticFragmentsExist(doublebondIons_matched, doublebondHighIons);
            //var isDoublebondAdvancedFilter = StandardMsCharacterizationUtility.IsDiagnosticFragmentsExist(doublebondIons_matched, doublebondHighAndLowIons);
            //var isDoublebondAdvancedFilter = StandardMsCharacterizationUtility.IsDiagnosticFragmentsExist(doublebondIons_matched, dIons4db);
            var matchedCount = doublebondIons_matched.Count;
            var matchedPercent = matchedCount / (doublebondIons.Count + 1e-10);
            var matchedCoefficient = StandardMsCharacterizationUtility.GetMatchedCoefficient(doublebondIons_matched);

            var isDoubleBondIdentified = isDoublebondAdvancedFilter && matchedPercent > doublebondIonCutoff * 0.5 ? true : false;

            result.DoubleBondIonsDetected = (int)matchedCount;
            result.DoubleBondMatchedPercent = matchedPercent;
            result.IsDoubleBondIonsExisted = isDoubleBondIdentified;
            result.DoubleBondIonScore = matchedCoefficient;

            // total score

            var score = result.ClassIonScore + result.ChainIonScore + result.PositionIonScore + result.DoubleBondIonScore;
            var counter = classionsDetected + chainIonsDetected + positionIonsDetected + matchedCount;
            result.TotalScore = score;
            result.TotalMatchedIonCount = counter;

            return result;

        }

    }
}
