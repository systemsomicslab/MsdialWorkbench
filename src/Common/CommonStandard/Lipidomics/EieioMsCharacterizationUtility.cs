using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataStructure;
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
        private readonly static IVisitor<ILipid, ILipid> SPECIES_LEVEL, POSITION_AND_DOUBLEBOND_LEVEL, POSITION_LEVEL, MOLECULAR_SPECIES_AND_DOUBLEBOND_LEVEL, MOLECULAR_SPECIES_LEVEL, CERAMIDE_POSITION_LEVEL, DOUBLEBONDPOSITION_LEVEL;

        static StandardMsCharacterizationUtility()
        {
            var builder = new LipidConverterBuilder();
            var director = new ShorthandNotationDirector(builder);
            director.SetSpeciesLevel();
            SPECIES_LEVEL = builder.Create();

            director.SetPositionLevel();
            director.SetDoubleBondPositionLevel();
            director.SetOxidizedPositionLevel();
            POSITION_AND_DOUBLEBOND_LEVEL = builder.Create();

            director.SetPositionLevel();
            director.SetDoubleBondPositionLevel();
            director.SetOxidizedNumberLevel();
            DOUBLEBONDPOSITION_LEVEL = builder.Create(); //fahfa

            director.SetPositionLevel();
            director.SetDoubleBondNumberLevel();
            director.SetOxidizedNumberLevel();
            POSITION_LEVEL = builder.Create();

            director.SetMolecularSpeciesLevel();
            director.SetDoubleBondPositionLevel();
            director.SetOxidizedPositionLevel();
            MOLECULAR_SPECIES_AND_DOUBLEBOND_LEVEL = builder.Create();

            director.SetMolecularSpeciesLevel();
            director.SetDoubleBondNumberLevel();
            director.SetOxidizedNumberLevel();
            MOLECULAR_SPECIES_LEVEL = builder.Create();

            director.SetPositionLevel();
            director.SetDoubleBondNumberLevel();
            director.SetOxidizedPositionLevel(); // if Ceramide acyl chain OH position is not determined, so check and fix here
            ((ILipidomicsVisitorBuilder)builder).SetSphingoOxidized(OxidizedIndeterminateState.Identity);
            CERAMIDE_POSITION_LEVEL = builder.Create();
        }

        public static (ILipid, double[]) GetDefaultCharacterizationResultForAlkylAcylGlycerols(ILipid molecule, LipidMsCharacterizationResult result)
        {
            IVisitor<ILipid, ILipid> converter;
            if (!result.IsChainIonsExisted)
            { // chain existed expected: PC O-36:2
                converter = SPECIES_LEVEL;
            }
            else if (result.IsPositionIonsExisted && result.IsDoubleBondIonsExisted)
            { // chain existed expected: PC O-18:0/18:2(9,12)
                converter = POSITION_AND_DOUBLEBOND_LEVEL;
            }
            else if (result.IsPositionIonsExisted)
            { // chain existed expected: PC O-18:0/18:2
                converter = POSITION_LEVEL;
            }
            else if (result.IsDoubleBondIonsExisted)
            { // chain existed expected: PC O-18:0_18:2(9,12)
                converter = MOLECULAR_SPECIES_AND_DOUBLEBOND_LEVEL;
            }
            else
            { // chain existed expected: PC O-18:0_18:2
                converter = MOLECULAR_SPECIES_LEVEL;
            }
            return (molecule.Accept(converter, IdentityDecomposer<ILipid, ILipid>.Instance), new double[2] { result.TotalScore, result.TotalMatchedIonCount });
        }

        public static (ILipid, double[]) GetDefaultCharacterizationResultForCeramides(ILipid molecule, LipidMsCharacterizationResult result)
        {
            IVisitor<ILipid, ILipid> converter;
            if (!result.IsChainIonsExisted)
            { // chain cannot determine
                converter = SPECIES_LEVEL;
            }
            else if (!result.IsDoubleBondIonsExisted)
            { // chain existed expected: SM 18:1;2O/18:1
                converter = CERAMIDE_POSITION_LEVEL;
            }
            else {
                converter = POSITION_AND_DOUBLEBOND_LEVEL;
            }
            return (molecule.Accept(converter, IdentityDecomposer<ILipid, ILipid>.Instance), new double[2] { result.TotalScore, result.TotalMatchedIonCount });
        }
        public static (ILipid, double[]) GetDefaultCharacterizationResultForFahfa(ILipid molecule, LipidMsCharacterizationResult result)
        {
            IVisitor<ILipid, ILipid> converter;
            if (!result.IsChainIonsExisted)
            { // chain cannot determine
                converter = SPECIES_LEVEL;
            }
            else if (!result.IsDoubleBondIonsExisted)
            {// chain existed expected: FAHFA 18:1/18:0;O
                converter = POSITION_LEVEL;
            }
            else
            {// chain existed expected: FAHFA 18:1(9)/18:0;O
                converter = DOUBLEBONDPOSITION_LEVEL;
            }
            return (molecule.Accept(converter, IdentityDecomposer<ILipid, ILipid>.Instance), new double[2] { result.TotalScore, result.TotalMatchedIonCount });
        }

        public static (ILipid, double[]) GetDefaultCharacterizationResultForGlycerophospholipid(ILipid molecule, LipidMsCharacterizationResult result)
        {
            IVisitor<ILipid, ILipid> converter;
            if (!result.IsChainIonsExisted)
            { // chain existed expected: PC 36:2
                converter = SPECIES_LEVEL;
            }
            else if (result.IsPositionIonsExisted && result.IsDoubleBondIonsExisted)
            { // chain existed expected: PC 18:0/18:2(9,12)
                converter = POSITION_AND_DOUBLEBOND_LEVEL;
            }
            else if (result.IsPositionIonsExisted)
            { // chain existed expected: PC 18:0/18:2
                converter = POSITION_LEVEL;
            }
            else if (result.IsDoubleBondIonsExisted)
            { // chain existed expected: PC 18:0_18:2(9,12)
                converter = MOLECULAR_SPECIES_AND_DOUBLEBOND_LEVEL;
            }
            else
            { // chain existed expected: PC 18:0_18:2
                converter = MOLECULAR_SPECIES_LEVEL;
            }
            return (molecule.Accept(converter, IdentityDecomposer<ILipid, ILipid>.Instance), new double[2] { result.TotalScore, result.TotalMatchedIonCount });
        }
        
        public static (ILipid, double[]) GetDefaultCharacterizationResultForTriacylGlycerols(ILipid molecule, LipidMsCharacterizationResult result)
        {
            IVisitor<ILipid, ILipid> converter;
            if (!result.IsChainIonsExisted)
            { // chain existed expected: TG 52:3
                converter = SPECIES_LEVEL;
            }
            else if (result.IsPositionIonsExisted && result.IsDoubleBondIonsExisted)
            { // chain existed expected: TG 16:0/18:1(11)/18:2(9,12)
                converter = POSITION_AND_DOUBLEBOND_LEVEL;
            }
            else if (result.IsPositionIonsExisted)
            { // chain existed expected: TG 16:0_18:1(sn2)_18:2 for TG, HBMP/DCL 16:0/18:0_20:4
                converter = POSITION_LEVEL;
            }
            else if (result.IsDoubleBondIonsExisted)
            { // chain existed expected:TG 16:0_18:1(11)_18:2(9,12)
                converter = MOLECULAR_SPECIES_AND_DOUBLEBOND_LEVEL;
            }
            else
            { // chain existed expected: TG 16:0_18:1_18:2
                converter = MOLECULAR_SPECIES_LEVEL;
            }
            return (molecule.Accept(converter, IdentityDecomposer<ILipid, ILipid>.Instance), new double[2] { result.TotalScore, result.TotalMatchedIonCount });
        }

        public static (ILipid, double[]) GetDefaultCharacterizationResultForSingleAcylChainLipid(ILipid molecule, LipidMsCharacterizationResult result) // CAR, steroidal ether etc.
        {
            var converter = SPECIES_LEVEL;
            if (result.IsDoubleBondIonsExisted)
            {
                converter = MOLECULAR_SPECIES_AND_DOUBLEBOND_LEVEL;
                if (molecule.Chains.OxidizedCount > 0) //TBC
                {
                    converter = MOLECULAR_SPECIES_LEVEL;
                }
            }
            return (molecule.Accept(converter, IdentityDecomposer<ILipid, ILipid>.Instance), new double[2] { result.TotalScore, result.TotalMatchedIonCount });
        }

        public static double GetMatchedCoefficient(List<SpectrumPeak> peaks)
        {
            double sum1 = 0, sum2 = 0, mean1 = 0, mean2 = 0, covariance = 0, sqrt1 = 0, sqrt2 = 0, counter = 0;
            for (int i = 0; i < peaks.Count; i++)
            {
                if (!peaks[i].IsMatched) continue;
                counter++;
                sum1 += peaks[i].Resolution;
                sum2 += peaks[i].Intensity;
            }
            if (counter == 0) return 0;
            mean1 = (double)(sum1 / counter);
            mean2 = (double)(sum2 / counter);

            for (int i = 0; i < peaks.Count; i++)
            {
                if (!peaks[i].IsMatched) continue;
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

        public static bool IsDiagnosticFragmentsExist(
            IReadOnlyList<SpectrumPeak> spectrum, 
            IReadOnlyList<DiagnosticIon> dIons, 
            double minimumIntensity,
            double minimumIntensityFactor) {
            var missedCounter = 0;
            var isAllExisted = true;
            if (dIons.IsEmptyOrNull()) return true;
            foreach (var ion in dIons) {
                if (!IsDiagnosticFragmentExist_ResolutionUsed4Intensity(spectrum, ion.MzTolerance, ion.Mz, ion.IonAbundanceCutOff, minimumIntensity, minimumIntensityFactor)) {
                    missedCounter++;
                    if (dIons.Count < 5 && missedCounter == 1) { isAllExisted = false; break; }
                    if (dIons.Count >= 5 && missedCounter == 2) { isAllExisted = false; break; }
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

        public static bool IsDiagnosticFragmentExist_ResolutionUsed4Intensity(
            IReadOnlyList<SpectrumPeak> spectrum,
            double mzTolerance,
            double diagnosticMz,
            double threshold,
            double minimumIntensity,
            double minimumIntensityFactor) {
            var internaladhoc_threshold = 12;
            if (minimumIntensityFactor == 0) internaladhoc_threshold = 0;
            for (int i = 0; i < spectrum.Count; i++) {
                var mz = spectrum[i].Mass;
                var relative_intensity = spectrum[i].Resolution; // should be normalized by max intensity to 100
                var original_intensity = spectrum[i].Charge; // this is temporal value

                if (relative_intensity > threshold &&
                    relative_intensity > minimumIntensity * minimumIntensityFactor &&
                    original_intensity > internaladhoc_threshold &&
                    Math.Abs(mz - diagnosticMz) < mzTolerance) {
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
            var minimumPeakIntensity = exp_spectrum.Min(n => n.Intensity);
            var minimumPeakIntensityFactor = 2.0;

            var matchedpeaks = MsScanMatching.GetMachedSpectralPeaks(exp_spectrum, ref_spectrum, tolerance, mzBegin, mzEnd);

            // check lipid class ion's existence
            var classions = matchedpeaks.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.metaboliteclass)).ToList();
            var isClassMustIonsExisted = classions.All(ion => !ion.IsAbsolutelyRequiredFragmentForAnnotation || ion.IsMatched);
            var isClassAdvancedFilter = StandardMsCharacterizationUtility.IsDiagnosticFragmentsExist(classions, dIons4class, minimumPeakIntensity, minimumPeakIntensityFactor);
            var classions_matched = classions.Where(n => n.IsMatched).ToList();
            var classionsDetected = classions_matched.Count();
            var isClassIonExisted = isClassMustIonsExisted && isClassAdvancedFilter && classionsDetected >= classIonCutoff
                ? true : false;

            result.ClassIonsDetected = classionsDetected;
            result.IsClassIonsExisted = isClassIonExisted;
            //result.ClassIonScore = isClassIonExisted ? classions_matched.Sum(n => n.Resolution) / 100.0 : 0.0;
            result.ClassIonScore = isClassIonExisted ? (double)classions_matched.Count / (double)classions.Count : 0.0;


            // check lipid chain ion's existence
            var chainIons = matchedpeaks.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.acylchain)).ToList();
            var isChainMustIonsExisted = chainIons.All(ion => !ion.IsAbsolutelyRequiredFragmentForAnnotation || ion.IsMatched);
            var isChainAdvancedFilter = StandardMsCharacterizationUtility.IsDiagnosticFragmentsExist(chainIons, dIons4chain, minimumPeakIntensity, minimumPeakIntensityFactor);
            var chainIons_matched = chainIons.Where(n => n.IsMatched).ToList();
            var chainIonsDetected = chainIons_matched.Count();
            var isChainIonExisted = isChainMustIonsExisted && isChainAdvancedFilter && chainIonsDetected >= chainIonCutoff
                ? true : false;

            result.ChainIonsDetected = chainIonsDetected;
            result.IsChainIonsExisted = isChainIonExisted;
            //result.ChainIonScore = isChainIonExisted ? chainIons_matched.Sum(n => n.Resolution) / 100.0 : 0.0;
            result.ChainIonScore = isChainIonExisted ? (double)chainIons_matched.Count / (double)chainIons.Count : 0.0;

            // check lipid position ion's existence
            var isPositionIonExisted = false;
            var positionIonsDetected = 0;
            if (positionIonCutoff > 0) {
                var positionIons = matchedpeaks.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.snposition)).ToList();
                var isPositionMustIonsExisted = positionIons.All(ion => !ion.IsAbsolutelyRequiredFragmentForAnnotation || ion.IsMatched);
                var isPositionAdvancedFilter = StandardMsCharacterizationUtility.IsDiagnosticFragmentsExist(positionIons, dIons4position, minimumPeakIntensity, minimumPeakIntensityFactor);
                var positionIons_matched = positionIons.Where(n => n.IsMatched).ToList();
                
                positionIonsDetected = positionIons_matched.Count();
                isPositionIonExisted = isPositionMustIonsExisted && isPositionAdvancedFilter && positionIonsDetected >= positionIonCutoff
                    ? true : false;
                result.PositionIonsDetected = positionIonsDetected;
                result.IsPositionIonsExisted = isPositionIonExisted;
                //result.PositionIonScore = isPositionIonExisted ? positionIons_matched.Sum(n => n.Resolution) / 100.0 : 0.0;
                result.PositionIonScore = isPositionIonExisted ? (double)positionIons_matched.Count / (double)positionIons.Count : 0.0;
            }

            // check the dtected ion nudouble bond position
            var doublebondIons = matchedpeaks.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.doublebond)).ToList();
            var doublebondIons_matched = doublebondIons.Where(n => n.IsMatched).ToList();

            var doublebondHighIons = 
                ref_spectrum
                .Where(n => n.SpectrumComment.HasFlag(SpectrumComment.doublebond_high))
                .Select(n => new DiagnosticIon() { Mz = n.Mass, IonAbundanceCutOff = 0.0000001, MzTolerance = tolerance })
                .ToList();

            //var doublebondHighAndLowIons =
            //   ref_spectrum
            //   .Where(n => n.SpectrumComment.HasFlag(SpectrumComment.doublebond_high) || n.SpectrumComment.HasFlag(SpectrumComment.doublebond_low))
            //   .Select(n => new DiagnosticIon() { Mz = n.Mass, IonAbundanceCutOff = 0.0000001, MzTolerance = tolerance })
            //   .ToList();

            //var doublebondLowIons =
            //    ref_spectrum
            //    .Where(n => n.SpectrumComment.HasFlag(SpectrumComment.doublebond_low))
            //    .Select(n => new DiagnosticIon() { Mz = n.Mass, IonAbundanceCutOff = 0.0000001, MzTolerance = tolerance })
            //    .ToList();

            var isDoublebondAdvancedFilter = StandardMsCharacterizationUtility.IsDiagnosticFragmentsExist(doublebondIons_matched, doublebondHighIons, minimumPeakIntensity, minimumPeakIntensityFactor);
            //var isDoublebondAdvancedFilter = StandardMsCharacterizationUtility.IsDiagnosticFragmentsExist(doublebondIons_matched, doublebondHighAndLowIons, minimumPeakIntensity, minimumPeakIntensityFactor);
            //var isDoublebondAdvancedFilter_low = StandardMsCharacterizationUtility.IsDiagnosticFragmentsExist(doublebondIons_matched, doublebondLowIons, 0, 0);
            //isDoublebondAdvancedFilter = isDoublebondAdvancedFilter && isDoublebondAdvancedFilter_low;
            //var isDoublebondAdvancedFilter = StandardMsCharacterizationUtility.IsDiagnosticFragmentsExist(doublebondIons_matched, dIons4db);
            var matchedCount = doublebondIons_matched.Count;
            var matchedPercent = matchedCount / (doublebondIons.Count + 1e-10);
            var matchedCoefficient = StandardMsCharacterizationUtility.GetMatchedCoefficient(doublebondIons_matched);

            var doublebondIons_matched_high_array = doublebondIons_matched.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.doublebond_high)).ToList();
            var doublebondIons_matched_low_array = doublebondIons_matched.Where(n => n.SpectrumComment.HasFlag(SpectrumComment.doublebond_low)).ToList();
            var doublebondIons_matched_high = doublebondIons_matched_high_array.Count() > 0 ? doublebondIons_matched_high_array.Average(n => n.Resolution) : 0;
            var doublebondIons_matched_low = doublebondIons_matched_low_array.Count() > 0 ? doublebondIons_matched_low_array.Average(n => n.Resolution) : 0;
            var high_low_bonus = doublebondIons_matched_high > doublebondIons_matched_low * 1.5 ? 0.5 : 0;

            var isDoubleBondIdentified = isDoublebondAdvancedFilter && matchedPercent > doublebondIonCutoff ? true : false;
            var isHGrainGeneratedPufaExist = doublebondIons.Count(n => n.SpectrumComment.HasFlag(SpectrumComment.doublebond_high) && n.Comment.Contains("+H_p3")) > 0;
            var hGrainBonusForPufa = false;
            if (isHGrainGeneratedPufaExist) {
                var hGrainFragments = doublebondIons.Where(n => n.Comment.Contains("+H_p3")).ToList();
                // the peaks are sorted by "oder by decessing with mz"
                var count = 0;
                var hGrainPeakAve = 0.0;
                //if (reference.Name == "PS_d5 17:0/20:3(8,11,14)") {
                //    Console.WriteLine();
                //}
                for (int i = hGrainFragments.Count - 1; i >= 0; i--) {
                    count++;
                    var peak = hGrainFragments[i];
                    if (peak.SpectrumComment.HasFlag(SpectrumComment.doublebond_high)) {
                        hGrainPeakAve /= (double)count;
                        if (peak.Resolution > hGrainPeakAve * 3.0) {
                            hGrainBonusForPufa = true;
                        }
                        break;
                    }
                    else {
                        hGrainPeakAve += peak.Resolution;
                    }
                }
            }

            result.DoubleBondIonsDetected = (int)matchedCount;
            result.DoubleBondMatchedPercent = matchedPercent;
            result.IsDoubleBondIonsExisted = isDoubleBondIdentified;
            result.DoubleBondIonScore = isDoubleBondIdentified ? matchedCoefficient + matchedPercent : 0;
            //result.DoubleBondIonScore = isDoubleBondIdentified ? matchedCoefficient + matchedPercent + high_low_bonus : 0;
            //result.DoubleBondIonScore = isDoubleBondIdentified ? matchedCoefficient : 0;
            if (!isHGrainGeneratedPufaExist && isDoubleBondIdentified) result.DoubleBondIonScore += high_low_bonus;
            if (hGrainBonusForPufa && isDoubleBondIdentified) result.DoubleBondIonScore += 0.5;

            // total score

            var score = result.ClassIonScore + result.ChainIonScore + result.PositionIonScore + result.DoubleBondIonScore;
            var counter = classionsDetected + chainIonsDetected + positionIonsDetected + matchedCount;
            result.TotalScore = score;
            result.TotalMatchedIonCount = counter;

            return result;

        }

    }
}
