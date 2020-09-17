using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Parser;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    public abstract class AlignmentRefiner
    {
        protected ParameterBase _param;

        protected abstract List<AlignmentSpotProperty> GetCleanedSpots(List<AlignmentSpotProperty> alignments);
        protected abstract void SetLinks(List<AlignmentSpotProperty> alignments);

        public AlignmentRefiner(ParameterBase param) {
            _param = param;
        }

        public AlignmentRefiner() : this(new ParameterBase()) { }

        public List<AlignmentSpotProperty> Refine(IList<AlignmentSpotProperty> alignments) {
            var spots = alignments.ToList();

            Deduplicate(spots);
            var cleaned = GetCleanedSpots(spots);
            var filtered = FilterByBlank(cleaned);
            SetAlignmentID(filtered);
            SetLinks(filtered);
            PostProcess(filtered);

            return filtered;
        }

        protected virtual void Deduplicate(List<AlignmentSpotProperty> alignments) { // TODO: change deduplicate process (msp, textdb, metabolite name...)
            if (_param.OnlyReportTopHitInMspSearch) { //to remove duplicate identifications
                alignments = alignments.OrderByDescending(spot => spot.MspID).ToList();

                var currentPeakId = 0;
                var currentLibraryId = alignments[currentPeakId].MspID;

                for (int i = 1; i < alignments.Count; i++) {
                    if (alignments[i].MspID < 0) break;
                    if (alignments[i].MspID != currentLibraryId) {
                        currentPeakId = i;
                        currentLibraryId = alignments[currentPeakId].MspID;
                        continue;
                    }
                    else {
                        if (alignments[currentPeakId].MspBasedMatchResult.TotalScore < alignments[i].MspBasedMatchResult.TotalScore) {
                            SetDefaultCompoundInformationInMspSearch(alignments[currentPeakId]);
                            currentPeakId = i;
                        }
                        else {
                            SetDefaultCompoundInformationInMspSearch(alignments[i]);
                        }
                    }
                }
            }

            if (_param.OnlyReportTopHitInTextDBSearch) {
                alignments = alignments.OrderByDescending(n => n.TextDbID).ToList();

                var currentPeakId = 0;
                var currentLibraryId = alignments[currentPeakId].TextDbID;

                for (int i = 1; i < alignments.Count; i++) {
                    if (alignments[i].TextDbID < 0) break;
                    if (alignments[i].TextDbID != currentLibraryId) {
                        currentLibraryId = alignments[i].TextDbID;
                        currentPeakId = i;
                        continue;
                    }
                    else {
                        if (alignments[currentPeakId].TextDbBasedMatchResult.TotalScore < alignments[i].TextDbBasedMatchResult.TotalScore) {
                            SetDefaultCompoundInformationInTextSearch(alignments[currentPeakId]);
                            currentPeakId = i;
                        }
                        else {
                            SetDefaultCompoundInformationInTextSearch(alignments[i]);
                        }
                    }
                }
            }
        }

        protected virtual List<AlignmentSpotProperty> FilterByBlank(List<AlignmentSpotProperty> alignments) {
            var fcSpots = new List<AlignmentSpotProperty>();
            int blankNumber = _param.FileID_AnalysisFileType.Values.Count(v => v == AnalysisFileType.Blank);
            int sampleNumber = _param.FileID_AnalysisFileType.Values.Count(v => v == AnalysisFileType.Sample);

            if (blankNumber > 0 && _param.IsRemoveFeatureBasedOnBlankPeakHeightFoldChange) {
              
                foreach (var spot in alignments) {
                    var sampleMax = 0.0;
                    var sampleAve = 0.0;
                    var blankAve = 0.0;
                    var nonMinValue = double.MaxValue;

                    foreach (var peak in spot.AlignedPeakProperties) {
                        var filetype = _param.FileID_AnalysisFileType[peak.FileID];
                        if (filetype == AnalysisFileType.Blank) {
                            blankAve += peak.PeakHeightTop;
                        }
                        else if (filetype == AnalysisFileType.Sample) {
                            if (peak.PeakHeightTop > sampleMax)
                                sampleMax = peak.PeakHeightTop;
                            sampleAve += peak.PeakHeightTop;
                        }

                        if (nonMinValue > peak.PeakHeightTop && peak.PeakHeightTop > 0.0001) {
                            nonMinValue = peak.PeakHeightTop;
                        }
                    }

                    sampleAve /= sampleNumber;
                    blankAve /= blankNumber;
                    if (blankAve == 0) {
                        if (nonMinValue != double.MaxValue)
                            blankAve = nonMinValue * 0.1;
                        else
                            blankAve = 1.0;
                    }

                    var blankThresh = blankAve * _param.FoldChangeForBlankFiltering;
                    var sampleThresh = _param.BlankFiltering == BlankFiltering.SampleMaxOverBlankAve ? sampleMax : sampleAve;
                
                    if (sampleThresh < blankThresh) {

                        if (_param.IsKeepRefMatchedMetaboliteFeatures && spot.IsReferenceMatched) {

                        }
                        else if (_param.IsKeepSuggestedMetaboliteFeatures && spot.IsAnnotationSuggested) {

                        }
                        else if (_param.IsKeepRemovableFeaturesAndAssignedTagForChecking) {
                            spot.FeatureFilterStatus.IsBlankFiltered = true;
                        }
                        else {
                            continue;
                        }
                    }
                    fcSpots.Add(spot);
                }
            }
            else {
                fcSpots = alignments;
            }

            return fcSpots;
        }

        protected virtual void SetAlignmentID(List<AlignmentSpotProperty> alignments) { }

        protected virtual void PostProcess(List<AlignmentSpotProperty> alignments) {
            foreach (var fcSpot in alignments.Where(spot => spot.AdductType.AdductIonName == string.Empty)) {
                var chargeNum = fcSpot.PeakCharacter.Charge;
                if (_param.IonMode == IonMode.Positive) {
                    if (chargeNum >= 2) {
                        fcSpot.AdductType.AdductIonName = "[M+" + chargeNum + "H]" + chargeNum + "+";
                    }
                    else {
                        fcSpot.AdductType.AdductIonName = "[M+H]+";
                    }
                }
                else {
                    if (chargeNum >= 2) {
                        fcSpot.AdductType.AdductIonName = "[M-" + chargeNum + "H]" + chargeNum + "-";
                    }
                    else {
                        fcSpot.AdductType.AdductIonName = "[M-H]-";
                    }
                }
            }
        }

        static void SetDefaultCompoundInformationInMspSearch(AlignmentSpotProperty alignmentSpot) {
            var textdb = alignmentSpot.TextDbBasedMatchResult;
            if (textdb == null || textdb.Name != alignmentSpot.Name) {
                alignmentSpot.AdductType.AdductIonName = string.Empty;
                alignmentSpot.PeakCharacter.Charge = 1;
                alignmentSpot.Name = string.Empty;
            }

            alignmentSpot.MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>();
        }

        static void SetDefaultCompoundInformationInTextSearch(AlignmentSpotProperty alignmentSpot) {
            var mspdb = alignmentSpot.MspBasedMatchResult;
            if (mspdb == null || mspdb.Name != alignmentSpot.Name) {
                alignmentSpot.AdductType.AdductIonName = string.Empty;
                alignmentSpot.PeakCharacter.Charge = 1;
                alignmentSpot.Name = string.Empty;
            }

            alignmentSpot.TextDbBasedMatchResult = null;
        }
        protected void AssignLinksByIdentifiedIonFeatures(List<AlignmentSpotProperty> cSpots) {
            foreach (var cSpot in cSpots) {
                if (cSpot.IsReferenceMatched) {

                    var repFileID = cSpot.RepresentativeFileID;
                    var repProp = cSpot.AlignedPeakProperties[repFileID];
                    var repLinks = repProp.PeakCharacter.PeakLinks;

                    foreach (var rLink in repLinks) {
                        var rLinkID = rLink.LinkedPeakID;
                        var rLinkProp = rLink.Character;
                        if (rLinkProp == PeakLinkFeatureEnum.Isotope) continue; // for isotope tracking
                        foreach (var rSpot in cSpots) {
                            if (rSpot.AlignedPeakProperties[repFileID].PeakID == rLinkID) {
                                if (rLinkProp == PeakLinkFeatureEnum.Adduct) {
                                    if (rSpot.IsReferenceMatched) {
                                        if (cSpot.AdductType.AdductIonName == rSpot.AdductType.AdductIonName)
                                            continue;
                                    }
                                    else {
                                        var rAdductCharge = AdductIonParser.GetChargeNumber(rSpot.AlignedPeakProperties[repFileID].PeakCharacter.AdductType.AdductIonName);
                                        if (rAdductCharge != rSpot.PeakCharacter.Charge)
                                            break;
                                        rSpot.AdductType.AdductIonName = rSpot.AlignedPeakProperties[repFileID].PeakCharacter.AdductType.AdductIonName;
                                    }
                                }
                                RegisterLinks(cSpot, rSpot, rLinkProp);
                                break;
                            }
                        }
                    }
                }
            }
        }

        protected void AssignLinksByRepresentativeIonFeatures(List<AlignmentSpotProperty> alignments) {
            foreach (var fcSpot in alignments) {

                var repFileID = fcSpot.RepresentativeFileID;
                var repIntensity = fcSpot.AlignedPeakProperties[repFileID].PeakHeightTop;
                var maxIdx = fcSpot.AlignedPeakProperties.Select(peak => peak.PeakHeightTop).Argmax();
                if (repIntensity < fcSpot.AlignedPeakProperties[maxIdx].PeakHeightTop) repFileID = maxIdx;

                var repProp = fcSpot.AlignedPeakProperties[repFileID];
                var repLinks = repProp.PeakCharacter.PeakLinks;
                foreach (var rLink in repLinks) {
                    var rLinkID = rLink.LinkedPeakID;
                    var rLinkProp = rLink.Character;
                    if (rLinkProp == PeakLinkFeatureEnum.Isotope) continue; // for isotope labeled tracking
                    foreach (var rSpot in alignments) {
                        if (rSpot.AlignedPeakProperties[repFileID].PeakID == rLinkID) {
                            if (rLinkProp == PeakLinkFeatureEnum.Adduct) {
                                if (rSpot.PeakCharacter.AdductType.AdductIonName != string.Empty) continue;
                                var adductCharge = AdductIonParser.GetChargeNumber(rSpot.AlignedPeakProperties[repFileID].PeakCharacter.AdductType.AdductIonName);
                                if (rSpot.PeakCharacter.Charge != adductCharge) continue;
                                adductCharge = AdductIonParser.GetChargeNumber(fcSpot.AlignedPeakProperties[repFileID].PeakCharacter.AdductType.AdductIonName);
                                if (fcSpot.PeakCharacter.Charge != adductCharge) continue;

                                RegisterLinks(fcSpot, rSpot, rLinkProp);
                                rSpot.AdductType.AdductIonName = rSpot.AlignedPeakProperties[repFileID].PeakCharacter.AdductType.AdductIonName;
                                if (fcSpot.AdductType.AdductIonName == string.Empty) {
                                    fcSpot.AdductType.AdductIonName = fcSpot.AlignedPeakProperties[repFileID].PeakCharacter.AdductType.AdductIonName;
                                }
                            }
                            else {
                                RegisterLinks(fcSpot, rSpot, rLinkProp);
                            }
                            break;
                        }
                    }
                }
            }
        }

        protected void RegisterLinks(AlignmentSpotProperty cSpot, AlignmentSpotProperty rSpot, PeakLinkFeatureEnum rLinkProp) {
            if (cSpot.PeakCharacter.PeakLinks.All(n => n.LinkedPeakID != rSpot.AlignmentID || n.Character != rLinkProp)) {
                cSpot.PeakCharacter.PeakLinks.Add(new LinkedPeakFeature() {
                    LinkedPeakID = rSpot.AlignmentID,
                    Character = rLinkProp
                });
                cSpot.PeakCharacter.IsLinked = true;
            }
            if (rSpot.PeakCharacter.PeakLinks.All(n => n.LinkedPeakID != cSpot.AlignmentID || n.Character != rLinkProp)) {
                rSpot.PeakCharacter.PeakLinks.Add(new LinkedPeakFeature() {
                    LinkedPeakID = cSpot.AlignmentID,
                    Character = rLinkProp
                });
                rSpot.PeakCharacter.IsLinked = true;
            }
        }

        protected void AssignPutativePeakgroupIDs(List<AlignmentSpotProperty> alignedSpots) {
            var groupID = 0;
            foreach (var spot in alignedSpots) {
                if (spot.PeakCharacter.PeakGroupID >= 0) continue;
                RecPeakGroupAssignment(spot, alignedSpots, groupID++);
            }
        }

        protected void RecPeakGroupAssignment(AlignmentSpotProperty spot, List<AlignmentSpotProperty> spots, int groupID) {
            spot.PeakCharacter.PeakGroupID = groupID;

            if (spot.PeakCharacter.PeakLinks == null) return;
            foreach (var linkedPeak in spot.PeakCharacter.PeakLinks) {
                var linkedPeakID = linkedPeak.LinkedPeakID;
                var character = linkedPeak.Character;
                if (character == PeakLinkFeatureEnum.ChromSimilar) continue;
                if (character == PeakLinkFeatureEnum.CorrelSimilar) continue;
                if (character == PeakLinkFeatureEnum.FoundInUpperMsMs) continue;
                if (spots[linkedPeakID].PeakCharacter.PeakGroupID >= 0) continue;

                RecPeakGroupAssignment(spots[linkedPeakID], spots, groupID);
            }
        }
    }
}
