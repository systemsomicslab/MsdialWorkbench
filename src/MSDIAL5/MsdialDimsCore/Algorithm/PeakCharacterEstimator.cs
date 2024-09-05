using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Mathematics.Basic;
using CompMs.Common.Parser;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialDimsCore.Algorithm;

public sealed class PeakCharacterEstimator {
    public List<AdductIon> SearchedAdducts { get; set; } = new List<AdductIon>();

    public void Process(AnalysisFileBean file, IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
        IReadOnlyList<MSDecResult> msdecResults, IMatchResultEvaluator<MsScanMatchResult> evaluator, ParameterBase parameter, IDataProvider provider, ReportProgress reporeter) {
        
        // some adduct features are automatically insearted even if users did not select any type of adduct
        SearchedAdductInitialize(parameter);

        // collecting the same RT region spots
        chromPeakFeatures = chromPeakFeatures.OrderBy(n => n.PeakID).ToList();
        new MsdialCore.Algorithm.PeakCharacterEstimator(0d, 100d).ResetAdductAndLink(chromPeakFeatures, evaluator);

        CharacterAssigner(file, chromPeakFeatures, provider, msdecResults, evaluator, parameter);
        reporeter.Report(chromPeakFeatures.Count, chromPeakFeatures.Count);

        FinalizationForAdduct(chromPeakFeatures, parameter);
        AssignPutativePeakgroupIDs(chromPeakFeatures);
    }

    // currently, the links for same metabolite, isotope, and adduct are grouped.
    // the others such as found in upper msms and chromatogram correlation are not grouped.
    // in future, I have to create the merge GUI for user side
    private void AssignPutativePeakgroupIDs(IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures) {
        var groupID = 0;
        foreach (var peak in chromPeakFeatures) {
            var peakCharacter = peak.PeakCharacter;
            if (peakCharacter.PeakGroupID >= 0) continue;
            if (peakCharacter.PeakLinks.Count == 0) {
                peakCharacter.PeakGroupID = groupID;
            }
            else {
                var crawledPeaks = new List<int>();
                peakCharacter.PeakGroupID = groupID;
                recPeakGroupAssignment(peak, chromPeakFeatures, groupID, crawledPeaks);
            }
            groupID++;
        }
    }

    private void recPeakGroupAssignment(ChromatogramPeakFeature peak, IReadOnlyList<ChromatogramPeakFeature> peakSpots, int groupID, List<int> crawledPeaks) {
        var peakCharacter = peak.PeakCharacter;
        if (peakCharacter.PeakLinks == null || peakCharacter.PeakLinks.Count == 0) return;
        foreach (var linkedPeak in peak.PeakCharacter.PeakLinks) {
            var linkedPeakID = linkedPeak.LinkedPeakID;
            if (crawledPeaks.Contains(linkedPeakID)) continue;

            peakSpots[linkedPeakID].PeakCharacter.PeakGroupID = groupID;
            crawledPeaks.Add(linkedPeakID);

            if (isCrawledPeaks(peakSpots[linkedPeakID].PeakCharacter.PeakLinks, crawledPeaks, peak.PeakID)) continue;
            recPeakGroupAssignment(peakSpots[linkedPeakID], peakSpots, groupID, crawledPeaks);
        }
    }

    private bool isCrawledPeaks(List<LinkedPeakFeature> peakLinks, List<int> crawledPeaks, int peakID) {
        if (peakLinks.Count(n => n.LinkedPeakID != peakID) == 0) return true;
        var frag = false;
        foreach (var linkID in peakLinks.Select(n => n.LinkedPeakID)) {
            if (crawledPeaks.Contains(linkID)) continue;
            frag = true;
            break;
        }
        if (frag == true) return false;
        else return true;
    }

    private void SearchedAdductInitialize(ParameterBase param) {
        var paramAdducts = param.SearchedAdductIons;
        foreach (var adduct in paramAdducts.OrEmptyIfNull().Where(n => n.IsIncluded)) {
            SearchedAdducts.Add(adduct);
        }
        if (SearchedAdducts.Count == 0) {
            var protonAdduct = param.IonMode == IonMode.Positive ? AdductIon.GetAdductIon("[M+H]+") : AdductIon.GetAdductIon("[M-H]-");
            SearchedAdducts.Add(protonAdduct);
        }
    }

    private void FinalizationForAdduct(IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures, ParameterBase param) {
        var defaultAdduct = SearchedAdducts[0];

        foreach (var peak in chromPeakFeatures.Where(n => n.PeakCharacter.IsotopeWeightNumber == 0)) {
            if (peak.PeakCharacter.AdductParent < 0)
                peak.PeakCharacter.AdductParent = peak.PeakID;
            if (peak.IsAdductTypeFormatted) continue;

            if (peak.PeakCharacter.Charge >= 2) {

                var adductString = string.Empty;
                if (param.IonMode == IonMode.Positive) {
                    adductString = "[M+" + peak.PeakCharacter.Charge + "H]" + peak.PeakCharacter.Charge + "+";
                }
                else {
                    adductString = "[M-" + peak.PeakCharacter.Charge + "H]" + peak.PeakCharacter.Charge + "-";
                }
                var estimatedAdduct = AdductIon.GetAdductIon(adductString);
                peak.SetAdductType(estimatedAdduct);
            }
            else {
                peak.SetAdductType(defaultAdduct);
            }
        }

        foreach (var peak in chromPeakFeatures) {
            if (peak.PeakCharacter.IsotopeParentPeakID >= 0 && peak.PeakCharacter.IsotopeWeightNumber > 0) {

                var parentPeak = chromPeakFeatures[peak.PeakCharacter.IsotopeParentPeakID];

                peak.PeakCharacter.AdductParent = parentPeak.PeakCharacter.AdductParent;
                peak.SetAdductType(parentPeak.AdductType);
            }
        }

        //refine the dependency
        foreach (var peak in chromPeakFeatures) {
            var currentPeak = peak;
            var ancestorId = currentPeak.PeakCharacter.AdductParent;
            while (currentPeak.PeakID != ancestorId) {
                currentPeak = chromPeakFeatures[ancestorId];
                ancestorId = currentPeak.PeakCharacter.AdductParent;
            }
            peak.PeakCharacter.AdductParent = ancestorId;
        }

    }

    // the RT deviations of peakspots should be less than 0.03 min
    // here, each peak is evaluated.
    // the purpose is to group the ions which are recognized as the same metabolite
    private void CharacterAssigner(AnalysisFileBean file,
        IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures, IDataProvider provider, IReadOnlyList<MSDecResult> msdecResults, IMatchResultEvaluator<MsScanMatchResult> evaluator, ParameterBase param) {
        if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) return;

        // if the first inchikey is same, it's recognized as the same metabolite.
        assignLinksBasedOnInChIKeys(chromPeakFeatures, evaluator);

        // The identified compound is used for searching.
        assignLinksBasedOnIdentifiedCompound(chromPeakFeatures, evaluator, param);

        if (param.IonMode == IonMode.Negative)
            assignAdductByMsMs(chromPeakFeatures, msdecResults, param);

        // The identified adduct is used for searching
        assignLinksBasedOnDeterminedAdduct(chromPeakFeatures, evaluator, param);

        // adduct pairing method
        assignLinksBasedOnAdductPairingMethod(chromPeakFeatures, param);

        // linkage by chromatogram correlations
        assignLinksBasedOnChromatogramCorrelation(chromPeakFeatures, provider, param, file.AcquisitionType);

        // linked by partial matching of MS1 and MS2
        if (file.AcquisitionType == AcquisitionType.AIF) return;
        // In dims, there are too many peaks that are falsely detected.
        // assignLinksBasedOnPartialMatchingOfMS1MS2(chromPeakFeatures, msdecResults, param);
    }

    private void assignAdductByMsMs(IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures, IReadOnlyList<MSDecResult> msdecResults, ParameterBase param) {

        var isAcetateAdduct = false;
        var isFormateAdduct = false;

        AdductIon acetateAdduct = null;
        AdductIon formateAdduct = null;

        foreach (var adduct in SearchedAdducts) {
            if (Math.Abs(adduct.AdductIonAccurateMass - 44.998201) < param.CentroidMs2Tolerance) {
                isFormateAdduct = true;
                formateAdduct = adduct;
                break;
            }
            if (Math.Abs(adduct.AdductIonAccurateMass - 59.013851) < param.CentroidMs2Tolerance) {
                isAcetateAdduct = true;
                acetateAdduct = adduct;
                break;
            }
        }

        if (isAcetateAdduct == false && isFormateAdduct == false) return;

        foreach (var peak in chromPeakFeatures.Where(n => !n.IsAdductTypeFormatted && n.MS2RawSpectrumID >= 0 && n.PeakCharacter.IsotopeWeightNumber == 0)) {

            var spectrum = !peak.Spectrum.IsEmptyOrNull() 
                ? peak.Spectrum 
                : !msdecResults.IsEmptyOrNull() 
                    ? msdecResults[peak.GetMSDecResultID()].Spectrum 
                    : null;
            if (spectrum.IsEmptyOrNull()) continue;

            var precursorMz = peak.Mass;
            var precursorIntensity = 1.0;
            
            var maxIntensity = spectrum.Max(n => n.Intensity);
            var massSpec = spectrum.OrderByDescending(n => n.Mass).ToList();

            foreach (var spec in massSpec) {

                var neutralloss = precursorMz - spec.Mass;
                var relativeIntensity = spec.Intensity / maxIntensity * 100;
                if (neutralloss < param.CentroidMs2Tolerance) {
                    precursorIntensity = relativeIntensity;
                }

                if (isFormateAdduct && Math.Abs(neutralloss - 46.005477) < param.CentroidMs2Tolerance && relativeIntensity > 80.0 && precursorIntensity < 40.0) {
                    peak.PeakCharacter.AdductParent = peak.PeakID;
                    peak.SetAdductType(formateAdduct);
                    break;
                }

                if (isAcetateAdduct && Math.Abs(neutralloss - 60.021127) < param.CentroidMs2Tolerance && relativeIntensity > 80.0 && precursorIntensity < 40.0) {
                    peak.PeakCharacter.AdductParent = peak.PeakID;
                    peak.SetAdductType(acetateAdduct);
                    break;
                }
            }
        }
    }
    
    // currently, the method is very simple.
    // if a peak (at least 10% relative abundance) in MS/MS is found in MS1 spectrum,
    // the peak of MS1 is assigned as "Found in upper MSMS"
    private void assignLinksBasedOnPartialMatchingOfMS1MS2(
        List<ChromatogramPeakFeature> chromPeakFeatures,
        List<MSDecResult> msdecResults,
        ParameterBase param) {

        chromPeakFeatures = chromPeakFeatures.OrderBy(chromPeakFeature => chromPeakFeature.Mass).ToList();
        for (int i = chromPeakFeatures.Count - 1; i >= 0; i--) {
            var peak = chromPeakFeatures[i];
            if (peak.MS2RawSpectrumID < 0) continue;
            if (peak.PeakCharacter.IsotopeWeightNumber != 0) continue;

            var spectrum = !peak.Spectrum.IsEmptyOrNull()
                ? peak.Spectrum
                : !msdecResults.IsEmptyOrNull()
                    ? msdecResults[peak.GetMSDecResultID()].Spectrum
                    : null;
            if (spectrum.IsEmptyOrNull())
                continue;
            var maxIntensity = spectrum.Max(n => n.Intensity);
            spectrum = spectrum
                .Where(specPeak => specPeak.Intensity / maxIntensity >= 0.1)
                .OrderBy(specPeak => specPeak.Mass)
                .ToList();

            var k = 0;
            for (int j = 0; j < i; j++) {
                var cPeak = chromPeakFeatures[j];
                if (cPeak.PeakCharacter.IsotopeWeightNumber != 0)
                    continue;
                while (k < spectrum.Count && spectrum[k].Mass < cPeak.Mass - param.CentroidMs2Tolerance) {
                    k++;
                }
                if (k < spectrum.Count && Math.Abs(spectrum[k].Mass - cPeak.Mass) < param.CentroidMs2Tolerance) {
                    cPeak.PeakCharacter.IsLinked = true;
                    peak.PeakCharacter.IsLinked = true;

                    registerLinks(peak, cPeak, PeakLinkFeatureEnum.FoundInUpperMsMs);
                }
            }
        }
    }

    // currently, only pure peaks are evaluated by this way.
    private void assignLinksBasedOnChromatogramCorrelation(IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures, IDataProvider provider, ParameterBase param, AcquisitionType type) {
        if (chromPeakFeatures[0].ChromXs.RT.Value < 0) return;
        var rawSpectra = new RawSpectra(provider, param.IonMode, type);
        foreach (var peak in chromPeakFeatures.Where(n => n.PeakCharacter.IsotopeWeightNumber == 0 && n.PeakShape.PeakPureValue >= 0.9)) {
            
            var tTopRt = peak.ChromXsTop.RT.Value;
            var tLeftRt = peak.ChromXsLeft.RT.Value;
            var tRightRt = peak.ChromXsRight.RT.Value;
            var chromatogramRange = new ChromatogramRange(tLeftRt, tRightRt, ChromXType.RT, ChromXUnit.Min);
            var tPeaklist = rawSpectra.GetMS1ExtractedChromatogram(new MzRange(peak.Mass, param.CentroidMs1Tolerance), chromatogramRange);
            var tChrom = tPeaklist.ChromatogramSmoothing(param.SmoothingMethod, param.SmoothingLevel).AsPeakArray();

            foreach (var cPeak in chromPeakFeatures.Where(n => n.PeakCharacter.IsotopeWeightNumber == 0 
            && !n.PeakCharacter.IsLinked && n.PeakID != peak.PeakID && n.PeakShape.PeakPureValue >= 0.9)) {

                var cPeaklist = rawSpectra.GetMS1ExtractedChromatogram(new MzRange(cPeak.Mass, param.CentroidMs1Tolerance), chromatogramRange);
                var cChrom = cPeaklist.ChromatogramSmoothing(param.SmoothingMethod, param.SmoothingLevel).AsPeakArray();

                var col = BasicMathematics.Coefficient(cChrom.Select(chrom => chrom.Intensity).ToArray(), tChrom.Select(chrom => chrom.Intensity).ToArray());
                if (col > 0.95) {
                    cPeak.PeakCharacter.IsLinked = true;
                    peak.PeakCharacter.IsLinked = true;

                    registerLinks(peak, cPeak, PeakLinkFeatureEnum.ChromSimilar);
                }
            }
        }
    }

    // just copied from the previous adduct estimator, should be checked for the improvement
    private void assignLinksBasedOnAdductPairingMethod(IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures, ParameterBase param) {
        foreach (var peak in chromPeakFeatures.Where(n => n.PeakCharacter.IsotopeWeightNumber == 0 && !n.PeakCharacter.IsLinked && !n.IsAdductTypeFormatted)) {
            var flg = false;
            var ppm = MolecularFormulaUtility.PpmCalculator(200.0, 200.0 + param.CentroidMs1Tolerance); //based on m/z 200

            foreach (var centralAdduct in SearchedAdducts) {

                var rCentralAdduct = AdductIonParser.ConvertDifferentChargedAdduct(centralAdduct, peak.PeakCharacter.Charge);
                var centralExactMass = rCentralAdduct.ConvertToExactMass(peak.Mass);

                var searchedPrecursors = new List<SearchedPrecursor>();
                foreach (var searchedAdduct in SearchedAdducts) {
                    if (rCentralAdduct.AdductIonName == searchedAdduct.AdductIonName) continue;
                    var searchedPrecursorMz = searchedAdduct.ConvertToMz(centralExactMass);
                    searchedPrecursors.Add(new SearchedPrecursor() { PrecursorMz = searchedPrecursorMz, AdductIon = searchedAdduct });
                }

                foreach (var searchedPeak in chromPeakFeatures.Where(n => !n.PeakCharacter.IsLinked && peak.PeakID != n.PeakID && !n.IsAdductTypeFormatted)) {
                    foreach (var searchedPrecursor in searchedPrecursors) {

                        var adductTol = MolecularFormulaUtility.ConvertPpmToMassAccuracy(searchedPeak.Mass, ppm);

                        if (Math.Abs(searchedPeak.Mass - searchedPrecursor.PrecursorMz) < adductTol) {

                            var searchedAdduct = searchedPrecursor.AdductIon;

                            searchedPeak.SetAdductType(searchedAdduct);
                            searchedPeak.PeakCharacter.AdductParent = peak.PeakID;
                            searchedPeak.PeakCharacter.IsLinked = true;

                            registerLinks(peak, searchedPeak, PeakLinkFeatureEnum.Adduct);
                            flg = true;
                            break;
                        }
                    }
                }

                if (flg) {

                    peak.SetAdductType(rCentralAdduct);
                    peak.PeakCharacter.AdductParent = peak.PeakID;
                    peak.PeakCharacter.IsLinked = true;
                   
                    break;
                }
            }
        }
    }

    private void assignLinksBasedOnDeterminedAdduct(IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures, IMatchResultEvaluator<MsScanMatchResult> evaluator, ParameterBase param) {
        foreach (var peak in chromPeakFeatures.Where(n => n.PeakCharacter.IsotopeWeightNumber == 0 && n.IsAdductTypeFormatted)) {
            var centralAdduct = peak.AdductType;
            var centralExactMass = centralAdduct.ConvertToExactMass(peak.Mass);
            var ppm = MolecularFormulaUtility.PpmCalculator(200.0, 200.0 + param.CentroidMs1Tolerance); //based on m/z 200

            var searchedPrecursors = new List<SearchedPrecursor>();
            foreach (var searchedAdduct in SearchedAdducts) {
                if (centralAdduct.AdductIonName == searchedAdduct.AdductIonName) continue;
                var searchedPrecursorMz = searchedAdduct.ConvertToMz(centralExactMass);
                searchedPrecursors.Add(new SearchedPrecursor() { PrecursorMz = searchedPrecursorMz, AdductIon = searchedAdduct });
            }

            foreach (var searchedPeak in chromPeakFeatures.Where(n => n.PeakCharacter.IsotopeWeightNumber == 0 && !n.PeakCharacter.IsLinked && n.PeakID != peak.PeakID)) {
                foreach (var searchedPrecursor in searchedPrecursors) {
                    var adductTol = MolecularFormulaUtility.ConvertPpmToMassAccuracy(searchedPeak.Mass, ppm);
                    if (searchedPeak.IsReferenceMatched(evaluator)) {
                        continue;
                    }

                    if (Math.Abs(searchedPeak.Mass - searchedPrecursor.PrecursorMz) < adductTol) {
                        var searchedAdduct = searchedPrecursor.AdductIon;
                        searchedPeak.SetAdductType(searchedAdduct);
                        searchedPeak.PeakCharacter.AdductParent = peak.PeakID;
                        searchedPeak.PeakCharacter.IsLinked = true;

                        peak.PeakCharacter.AdductParent = peak.PeakID;
                        registerLinks(peak, searchedPeak, PeakLinkFeatureEnum.Adduct);

                        break;
                    }
                }
            }
        }
    }

    private void assignLinksBasedOnIdentifiedCompound(IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures, IMatchResultEvaluator<MsScanMatchResult> evaluator, ParameterBase param) {
        foreach (var peak in chromPeakFeatures.Where(n => n.PeakCharacter.IsotopeWeightNumber == 0)) {
            if (peak.IsUnknown || peak.IsAnnotationSuggested(evaluator)) continue;

            //adduct null check
            if (peak.AdductType == null) continue;
            var inchikey = peak.InChIKey;
            var centralAdduct = peak.AdductType;
            var centralExactMass = centralAdduct.ConvertToExactMass(peak.Mass);
            var ppm = MolecularFormulaUtility.PpmCalculator(200.0, 200.0 + param.CentroidMs1Tolerance); //based on m/z 200

            var searchedPrecursors = new List<SearchedPrecursor>();
            foreach (var searchedAdduct in SearchedAdducts) {
                if (centralAdduct.AdductIonName == searchedAdduct.AdductIonName) continue;
                var searchedPrecursorMz = searchedAdduct.ConvertToMz(centralExactMass);
                searchedPrecursors.Add(new SearchedPrecursor() { PrecursorMz = searchedPrecursorMz, AdductIon = searchedAdduct });
            }

            foreach (var searchedPeak in chromPeakFeatures.Where(n => n.PeakCharacter.IsotopeWeightNumber == 0 && !n.PeakCharacter.IsLinked)) {
                if (peak.PeakID == searchedPeak.PeakID) continue;
                foreach (var searchedPrecursor in searchedPrecursors) {

                    var adductTol = MolecularFormulaUtility.ConvertPpmToMassAccuracy(searchedPeak.Mass, ppm);

                    if (Math.Abs(searchedPeak.Mass - searchedPrecursor.PrecursorMz) < adductTol) {

                        if (searchedPeak.IsReferenceMatched(evaluator)) {
                            if (searchedPeak.AdductType.AdductIonName != searchedPrecursor.AdductIon.AdductIonName)
                                continue;
                        }

                        var searchedAdduct = searchedPrecursor.AdductIon;
                        searchedPeak.SetAdductType(searchedAdduct);
                        searchedPeak.PeakCharacter.AdductParent = peak.PeakID;
                        searchedPeak.PeakCharacter.IsLinked = true;

                        peak.PeakCharacter.AdductParent = peak.PeakID;
                        registerLinks(peak, searchedPeak, PeakLinkFeatureEnum.Adduct);

                        break;
                    }
                }
            }
        }
    }

    private void assignLinksBasedOnInChIKeys(IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures, IMatchResultEvaluator<MsScanMatchResult> evaluator) {
        foreach (var peak in chromPeakFeatures.Where(n => n.PeakCharacter.IsotopeWeightNumber == 0)) {
            
            if (peak.IsUnknown || peak.IsAnnotationSuggested(evaluator)) continue;
            if (!peak.IsValidInChIKey()) continue;
            var inchikey = peak.InChIKey;
            var shortInChIKey = inchikey.Substring(0, 14);

            foreach (var cPeak in chromPeakFeatures.Where(n => n.PeakCharacter.IsotopeWeightNumber == 0)) {

                var peakCharacter = peak.PeakCharacter;
                var cPeakCharacter = cPeak.PeakCharacter;
                
                if (peak.PeakID == cPeak.PeakID) continue;
                if (cPeakCharacter.IsLinked) continue;
                if (cPeak.IsUnknown || cPeak.IsAnnotationSuggested(evaluator)) continue;
                if (!cPeak.IsValidInChIKey()) continue;

                var cInchikey = cPeak.InChIKey;
                var cShortInChIKey = cInchikey.Substring(0, 14);

                if (shortInChIKey == cShortInChIKey) {
                    peakCharacter.IsLinked = true;
                    peakCharacter.AdductParent = peak.PeakID;

                    cPeakCharacter.IsLinked = true;
                    cPeakCharacter.AdductParent = peak.PeakID;
                    registerLinks(peak, cPeak, PeakLinkFeatureEnum.Adduct);
                }
            }
        }
    }

    private void registerLinks(ChromatogramPeakFeature cSpot, ChromatogramPeakFeature rSpot, PeakLinkFeatureEnum rLinkProp) {
        var cSpotCharacter = cSpot.PeakCharacter;
        var rSpotCharacter = rSpot.PeakCharacter;
        if (cSpotCharacter.PeakLinks.All(n => n.LinkedPeakID != rSpot.PeakID || n.Character != rLinkProp)) {
            cSpotCharacter.PeakLinks.Add(new LinkedPeakFeature()
            {
                LinkedPeakID = rSpot.PeakID,
                Character = rLinkProp
            });
            cSpotCharacter.IsLinked = true;
        }
        if (rSpotCharacter.PeakLinks.All(n => n.LinkedPeakID != cSpot.PeakID || n.Character != rLinkProp)) {
            rSpotCharacter.PeakLinks.Add(new LinkedPeakFeature() {
                LinkedPeakID = cSpot.PeakID,
                Character = rLinkProp
            });
            rSpotCharacter.IsLinked = true;
        }
    }
}
