using System;
using System.Collections.Generic;
using System.Linq;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Mathematics.Basic;
using CompMs.Common.Parser;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialLcmsApi.Parameter;

namespace CompMs.MsdialLcMsApi.DataObj
{
    public class LcmsAlignmentRefiner : AlignmentRefiner
    {
        public LcmsAlignmentRefiner(MsdialLcmsParameter param) : base(param) { }

        protected override List<AlignmentSpotProperty> GetCleanedSpots(List<AlignmentSpotProperty> alignments) {
            var cSpots = new List<AlignmentSpotProperty>();
            var donelist = new HashSet<int>();

            foreach (var spot in alignments.Where(spot => spot.MspID >= 0 && !spot.Name.Contains("w/o"))) {
                TryMergeToMaster(spot, cSpots, donelist, _param);
            }

            foreach (var spot in alignments.Where(spot => spot.TextDbID >= 0 && !spot.Name.Contains("w/o"))) {
                TryMergeToMaster(spot, cSpots, donelist, _param);
            }

            foreach (var spot in alignments) {
                if (spot.MspID >= 0 && !spot.Name.Contains("w/o")) continue;
                if (spot.TextDbID >= 0 && !spot.Name.Contains("w/o")) continue;
                if (spot.PeakCharacter.IsotopeWeightNumber > 0) continue;
                TryMergeToMaster(spot, cSpots, donelist, _param);
            }

            return cSpots;
        }

        protected override void SetLinks(List<AlignmentSpotProperty> alignments) {
            alignments.Sort((x, y) => x.MassCenter.CompareTo(y.MassCenter));
            if (_param.IsIonMobility) {
                foreach (var spot in alignments) {
                    spot.AlignmentDriftSpotFeatures = new List<AlignmentSpotProperty>(spot.AlignmentDriftSpotFeatures.OrderBy(p => p.TimesCenter.Value));
                }
            }

            List<int> newIdList;
            if (_param.IsIonMobility) {
                newIdList = new List<int>();
                foreach (var spot in alignments) {
                    newIdList.Add(spot.MasterAlignmentID);
                    foreach (var dspot in spot.AlignmentDriftSpotFeatures) {
                        newIdList.Add(dspot.MasterAlignmentID);
                    }
                }
            }
            else {
                newIdList = alignments.Select(x => x.AlignmentID).ToList();
            }
            var masterID = 0;
            for (int i = 0; i < alignments.Count; i++) {
                alignments[i].AlignmentID = i;
                if (_param.IsIonMobility) {
                    alignments[i].MasterAlignmentID = masterID;
                    masterID++;
                    var driftSpots = alignments[i].AlignmentDriftSpotFeatures;
                    for (int j = 0; j < driftSpots.Count; j++) {
                        driftSpots[j].MasterAlignmentID = masterID;
                        driftSpots[j].AlignmentID = j;
                        driftSpots[j].ParentAlignmentID = i;
                        masterID++;
                    }
                }
            }

            //checking alignment spot variable correlations
            var rtMargin = 0.06F;
            AssignLinksByIonAbundanceCorrelations(alignments, rtMargin);

            // assigning peak characters from the identified spots
            AssignLinksByIdentifiedIonFeatures(alignments);

            // assigning peak characters from the representative file information
            alignments.Sort((x, y) => x.HeightAverage.CompareTo(y.HeightAverage));
            alignments.Reverse();
            foreach (var fcSpot in alignments) {

                var repFileID = fcSpot.RepresentativeFileID;
                var repIntensity = fcSpot.AlignedPeakProperties[repFileID].PeakHeightTop;
                for (int i = 0; i < fcSpot.AlignedPeakProperties.Count; i++) {
                    var peak = fcSpot.AlignedPeakProperties[i];
                    if (peak.PeakHeightTop > repIntensity) {
                        repFileID = i;
                        repIntensity = peak.PeakHeightTop;
                    }
                }

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

            #region // finalize adduct features
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
            #endregion

            // assign putative group IDs
            alignments.Sort((x, y) => x.AlignmentID.CompareTo(y.AlignmentID));
            AssignPutativePeakgroupIDs(alignments);
        }

        protected override void PostProcess(List<AlignmentSpotProperty> alignments) { }

        private static void TryMergeToMaster(AlignmentSpotProperty spot, List<AlignmentSpotProperty> cSpots, HashSet<int> donelist, ParameterBase param) {
            var spotRt = spot.TimesCenter.Value;
            var spotMz = spot.MassCenter;

            var flg = false;
            var rtTol = Math.Min(param.RetentionTimeAlignmentTolerance, 0.1);
            foreach (var cSpot in cSpots.Where(n => Math.Abs(n.MassCenter - spotMz) < param.Ms1AlignmentTolerance)) {
                var cSpotRt = cSpot.TimesCenter.Value;
                if (Math.Abs(cSpotRt - spotRt) < rtTol * 0.5) {
                    flg = true;
                    break;
                }
            }
            if (!flg && !donelist.Contains(spot.AlignmentID)) {
                cSpots.Add(spot);
                donelist.Add(spot.AlignmentID);
            }
        }
        private static void AssignLinksByIonAbundanceCorrelations(List<AlignmentSpotProperty> alignSpots, float rtMargin) {
            if (alignSpots == null || alignSpots.Count == 0) return;
            if (alignSpots[0].AlignedPeakProperties == null || alignSpots[0].AlignedPeakProperties.Count == 0) return;

            if (alignSpots[0].AlignedPeakProperties.Count() > 9) {
                alignSpots = alignSpots.OrderBy(n => n.TimesCenter.Value).ToList();
                foreach (var spot in alignSpots) {
                    if (spot.PeakCharacter.IsotopeWeightNumber > 0) continue;
                    var spotRt = spot.TimesCenter.Value;
                    var startScanIndex = SearchCollection.LowerBound(
                        alignSpots,
                        new AlignmentSpotProperty { TimesCenter = new ChromXs(spotRt - rtMargin - 0.01f) },
                        (a, b) => a.TimesCenter.Value.CompareTo(b.TimesCenter.Value)
                        );

                    var searchedSpots = new List<AlignmentSpotProperty>();

                    for (int i = startScanIndex; i < alignSpots.Count; i++) {
                        if (spot.AlignmentID == alignSpots[i].AlignmentID) continue;
                        if (alignSpots[i].TimesCenter.Value < spotRt - rtMargin) continue;
                        if (alignSpots[i].PeakCharacter.IsotopeWeightNumber > 0) continue;
                        if (alignSpots[i].TimesCenter.Value > spotRt + rtMargin) break;

                        searchedSpots.Add(alignSpots[i]);
                    }

                    AlignmentSpotVariableCorrelationSearcher(spot, searchedSpots);
                }
            }
        }

        private static void AlignmentSpotVariableCorrelationSearcher(AlignmentSpotProperty spot, List<AlignmentSpotProperty> searchedSpots)
        {
            var sampleCount = spot.AlignedPeakProperties.Count;
            var spotPeaks = spot.AlignedPeakProperties;
            var peaks = spotPeaks.Select(peak => peak.PeakHeightTop).ToArray();

            foreach (var searchSpot in searchedSpots) {

                var searchedSpotPeaks = searchSpot.AlignedPeakProperties;
                var correlation = BasicMathematics.Coefficient(peaks, searchedSpotPeaks.Select(peak => peak.PeakHeightTop).ToArray());
                if (correlation >= 0.95) {
                    spot.AlignmentSpotVariableCorrelations.Add(
                        new AlignmentSpotVariableCorrelation() {
                            CorrelateAlignmentID = searchSpot.AlignmentID,
                            CorrelationScore = (float)correlation
                        });
                    spot.PeakCharacter.IsLinked = true;
                    spot.PeakCharacter.PeakLinks.Add(new LinkedPeakFeature() {
                        LinkedPeakID = searchSpot.AlignmentID,
                        Character = PeakLinkFeatureEnum.CorrelSimilar
                    });
                }
            }

            if (spot.AlignmentSpotVariableCorrelations.Count > 1)
                spot.AlignmentSpotVariableCorrelations = spot.AlignmentSpotVariableCorrelations.OrderBy(n => n.CorrelateAlignmentID).ToList();
        }

        private static void AssignLinksByIdentifiedIonFeatures(List<AlignmentSpotProperty> cSpots) {
            foreach (var cSpot in cSpots) {
                if ((cSpot.MspID >= 0 || cSpot.TextDbID >= 0) && !cSpot.Name.Contains("w/o")) {

                    var repFileID = cSpot.RepresentativeFileID;
                    var repProp = cSpot.AlignedPeakProperties[repFileID];
                    var repLinks = repProp.PeakCharacter.PeakLinks;

                    foreach (var rLink in repLinks) {
                        var rLinkID = rLink.LinkedPeakID;
                        var rLinkProp = rLink.Character;
                        if (rLinkProp == PeakLinkFeatureEnum.Isotope) continue; // for isotope tracking
                        foreach (var rSpot in cSpots) {
                            if (rSpot.AlignedPeakProperties[repFileID].PeakID == rLinkID) {

                                if ((rSpot.MspID >= 0 || rSpot.TextDbID >= 0) && !rSpot.Name.Contains("w/o")) {
                                    if (rLinkProp == PeakLinkFeatureEnum.Adduct) {
                                        if (cSpot.AdductType.AdductIonName == rSpot.AdductType.AdductIonName) continue;
                                        RegisterLinks(cSpot, rSpot, rLinkProp);
                                    }
                                    else {
                                        RegisterLinks(cSpot, rSpot, rLinkProp);
                                    }
                                }
                                else {
                                    if (rLinkProp == PeakLinkFeatureEnum.Adduct) {
                                        var rAdductCharge = AdductIonParser.GetChargeNumber(rSpot.AlignedPeakProperties[repFileID].PeakCharacter.AdductType.AdductIonName);
                                        if (rAdductCharge == rSpot.PeakCharacter.Charge) {
                                            rSpot.AdductType.AdductIonName = rSpot.AlignedPeakProperties[repFileID].PeakCharacter.AdductType.AdductIonName;
                                            RegisterLinks(cSpot, rSpot, rLinkProp);
                                        }
                                    }
                                    else {
                                        RegisterLinks(cSpot, rSpot, rLinkProp);
                                    }
                                }

                                break;
                            }
                        }
                    }
                }
            }
        }

        private static void RegisterLinks(AlignmentSpotProperty cSpot, AlignmentSpotProperty rSpot, PeakLinkFeatureEnum rLinkProp) {
            if (cSpot.PeakCharacter.PeakLinks.Count(n => n.LinkedPeakID == rSpot.AlignmentID && n.Character == rLinkProp) == 0) {
                cSpot.PeakCharacter.PeakLinks.Add(new LinkedPeakFeature() {
                    LinkedPeakID = rSpot.AlignmentID,
                    Character = rLinkProp
                });
                cSpot.PeakCharacter.IsLinked = true;
            }
            if (rSpot.PeakCharacter.PeakLinks.Count(n => n.LinkedPeakID == cSpot.AlignmentID && n.Character == rLinkProp) == 0) {
                rSpot.PeakCharacter.PeakLinks.Add(new LinkedPeakFeature() {
                    LinkedPeakID = cSpot.AlignmentID,
                    Character = rLinkProp
                });
                rSpot.PeakCharacter.IsLinked = true;
            }
        }

        private static void AssignPutativePeakgroupIDs(List<AlignmentSpotProperty> alignedSpots) {
            var groupID = 0;
            foreach (var spot in alignedSpots) {
                if (spot.PeakCharacter.PeakGroupID >= 0) continue;
                if (spot.PeakCharacter.PeakLinks.Count == 0) {
                    spot.PeakCharacter.PeakGroupID = groupID;
                }
                else {
                    var crawledPeaks = new List<int>();
                    spot.PeakCharacter.PeakGroupID = groupID;
                    RecPeakGroupAssignment(spot, alignedSpots, groupID, crawledPeaks);
                }
                groupID++;
            }
        }

        private static void RecPeakGroupAssignment(AlignmentSpotProperty spot, List<AlignmentSpotProperty> alignedSpots, 
            int groupID, List<int> crawledPeaks) {
            if (spot.PeakCharacter.PeakLinks == null || spot.PeakCharacter.PeakLinks.Count == 0) return;

            foreach (var linkedPeak in spot.PeakCharacter.PeakLinks) {
                var linkedPeakID = linkedPeak.LinkedPeakID;
                var character = linkedPeak.Character;
                if (character == PeakLinkFeatureEnum.ChromSimilar) continue;
                if (character == PeakLinkFeatureEnum.CorrelSimilar) continue;
                if (character == PeakLinkFeatureEnum.FoundInUpperMsMs) continue;
                if (crawledPeaks.Contains(linkedPeakID)) continue;

                alignedSpots[linkedPeakID].PeakCharacter.PeakGroupID = groupID;
                crawledPeaks.Add(linkedPeakID);

                if (isCrawledPeaks(alignedSpots[linkedPeakID].PeakCharacter.PeakLinks, crawledPeaks, spot.AlignmentID)) continue;
                RecPeakGroupAssignment(alignedSpots[linkedPeakID], alignedSpots, groupID, crawledPeaks);
            }
        }

        private static bool isCrawledPeaks(List<LinkedPeakFeature> peakLinks, List<int> crawledPeaks, int peakID) {
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
    }
}
