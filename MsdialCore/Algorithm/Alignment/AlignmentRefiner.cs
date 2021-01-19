using CompMs.Common.DataObj.Database;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Parser;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Alignment
{
    public abstract class AlignmentRefiner
    {
        protected ParameterBase _param;
        private readonly IupacDatabase _iupac;

        protected abstract List<AlignmentSpotProperty> GetCleanedSpots(List<AlignmentSpotProperty> alignments);
        protected abstract void SetLinks(List<AlignmentSpotProperty> alignments);

        public AlignmentRefiner(ParameterBase param, IupacDatabase iupac) {
            _param = param;
            _iupac = iupac;
        }

        public AlignmentRefiner(IupacDatabase iupac) : this(new ParameterBase(), iupac) { }

        public List<AlignmentSpotProperty> Refine(IList<AlignmentSpotProperty> alignments) {
            var spots = alignments.ToList();

            Deduplicate(spots);
            var cleaned = GetCleanedSpots(spots);
            var filtered = FilterByBlank(cleaned);
            SetAlignmentID(filtered);
            IsotopeAnalysis(filtered);
            SetLinks(filtered);
            PostProcess(filtered);

            return filtered;
        }

        protected virtual void Deduplicate(List<AlignmentSpotProperty> alignments) { // TODO: change deduplicate process (msp, textdb, metabolite name...)
            if (_param.OnlyReportTopHitInMspSearch) { //to remove duplicate identifications
                var mspDeduplicator = new MspAnnotationDeduplicator();
                mspDeduplicator.Process(alignments);
            }

            if (_param.OnlyReportTopHitInTextDBSearch) {
                var textDbDedupicator = new TextAnnotationDeduplicator();
                textDbDedupicator.Process(alignments);
            }
        }

        protected virtual List<AlignmentSpotProperty> FilterByBlank(List<AlignmentSpotProperty> alignments) {
            if (_param.IsRemoveFeatureBasedOnBlankPeakHeightFoldChange) {
                var blankFilter = new BlankFilter(
                    _param.FileID_AnalysisFileType,
                    _param.FoldChangeForBlankFiltering,
                    _param.BlankFiltering,
                    _param.IsKeepRefMatchedMetaboliteFeatures,
                    _param.IsKeepSuggestedMetaboliteFeatures,
                    _param.IsKeepRemovableFeaturesAndAssignedTagForChecking);

                return blankFilter.Filter(alignments).ToList();
            }
            return alignments;
        }

        protected virtual void SetAlignmentID(List<AlignmentSpotProperty> alignments) { }

        private void IsotopeAnalysis(IReadOnlyList<AlignmentSpotProperty> alignmentSpots) {
            foreach (var spot in alignmentSpots) {
                if (_param.TrackingIsotopeLabels || spot.IsReferenceMatched) {
                    spot.PeakCharacter.IsotopeParentPeakID = spot.AlignmentID;
                    spot.PeakCharacter.IsotopeWeightNumber = 0;
                }
                if (!spot.IsReferenceMatched) {
                    spot.AdductType.AdductIonName = string.Empty;
                }
            }
            if (_param.TrackingIsotopeLabels) return;

            IsotopeEstimator.Process(alignmentSpots, _param, _iupac);
        }

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

        protected void AssignLinksByIdentifiedIonFeatures(List<AlignmentSpotProperty> cSpots) {
            foreach (var cSpot in cSpots) {
                if (cSpot.IsReferenceMatched) {

                    var repFileID = cSpot.RepresentativeFileID;
                    var repProp = cSpot.AlignedPeakProperties[repFileID];
                    var repLinks = repProp.PeakCharacter.PeakLinks;

                    for (var i = 0; i < repLinks.Count; i++) {
                        var rLink = repLinks[i];
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
