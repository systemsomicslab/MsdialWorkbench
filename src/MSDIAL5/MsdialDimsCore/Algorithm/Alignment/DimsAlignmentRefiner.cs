using CompMs.Common.DataObj.Database;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Parser;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialDimsCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialDimsCore.Algorithm.Alignment
{
    public class DimsAlignmentRefiner : IAlignmentRefiner
    {

        private MsdialDimsParameter _param;
        private readonly IupacDatabase _iupac;
        private readonly IMatchResultEvaluator<MsScanMatchResult> evaluator;

        public DimsAlignmentRefiner(MsdialDimsParameter param, IupacDatabase iupac, IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            _param = param;
            _iupac = iupac;
            this.evaluator = evaluator;
        }

        public (List<AlignmentSpotProperty>, List<int>) Refine(IList<AlignmentSpotProperty> alignments) {

            //foreach (var spot in alignments.Where(n => n.IsReferenceMatched)) {
            //    Console.WriteLine(spot.MspBasedMatchResult.Name + "\t" + spot.AdductType.AdductIonName);
            //}

            Deduplicate(alignments);
            var cleaned = GetCleanedSpots(alignments);
            var filtered = FilterByBlank(cleaned);
            var ids = SetAlignmentID(filtered);
            SetIsotopes(filtered);
            SetStatProperty(filtered);
            SetLinks(filtered);
            SetAdducts(filtered.Where(spot => !spot.AdductType.HasAdduct));

            return (filtered, ids);
        }

        private void Deduplicate(IList<AlignmentSpotProperty> alignments) { // TODO: change deduplicate process (msp, textdb, metabolite name...)
            if (_param.OnlyReportTopHitInMspSearch) { //to remove duplicate identifications
                var mspDeduplicator = new MspAnnotationDeduplicator();
                mspDeduplicator.Process(alignments);
            }

            if (_param.OnlyReportTopHitInTextDBSearch) {
                var textDbDedupicator = new TextAnnotationDeduplicator();
                textDbDedupicator.Process(alignments);
            }
        }

        private List<AlignmentSpotProperty> GetCleanedSpots(IList<AlignmentSpotProperty> spots) {
            var master = new Dictionary<int, List<AlignmentSpotProperty>>();
            var ms1Tol = _param.Ms1AlignmentTolerance;

            MergeToMaster(spots.Where(spot => spot.MspID >= 0 && spot.IsReferenceMatched(evaluator)).OrderByDescending(n => n.MspBasedMatchResult.TotalScore), master, ms1Tol);
            MergeToMaster(spots.Where(spot => spot.IsReferenceMatched(evaluator)).OrderByDescending(spot => spot.MatchResults.Representative.TotalScore), master, ms1Tol);
            MergeToMaster(spots.Where(spot => spot.TextDbID >= 0 && spot.IsReferenceMatched(evaluator)).OrderByDescending(n => n.TextDbBasedMatchResult.TotalScore), master, ms1Tol);
            MergeToMaster(spots.Where(spot => !spot.IsReferenceMatched(evaluator)).OrderByDescending(n => n.HeightAverage), master, ms1Tol);

            return master.Values.SelectMany(props => props).OrderBy(spot => spot.MassCenter).ToList();
        }

        private static void MergeToMaster(IEnumerable<AlignmentSpotProperty> spots, Dictionary<int, List<AlignmentSpotProperty>> master, double ms1Tol) {
            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + ms1Tol));
            var tol = ms1Tol;
            foreach (var spot in spots) {

                #region // practical parameter changes
                if (spot.MassCenter > 500) {
                    tol = (float)MolecularFormulaUtility.ConvertPpmToMassAccuracy(spot.MassCenter, ppm);
                }
                #endregion

                if (!ExistsSimilar(spot.MassCenter, master, tol)) {
                    var massKey = (int)spot.MassCenter;
                    if (!master.ContainsKey(massKey))
                        master[massKey] = new List<AlignmentSpotProperty>();
                    master[massKey].Add(spot);
                }
            }
        }

        private static bool ExistsSimilar(double mass, Dictionary<int, List<AlignmentSpotProperty>> master, double tol) {
            var massKey = (int)mass;
            var t = (int)Math.Ceiling(tol);
            foreach (var keyTol in Enumerable.Range(-t, t * 2 + 1)) { // -t to +t
                if (!master.ContainsKey(massKey + keyTol))
                    continue;
                foreach (var prop in master[massKey + keyTol]) {
                    if (prop.MassCenter - tol <= mass && mass <= prop.MassCenter + tol) {
                        return true;
                    }
                }
            }
            return false;
        }

        private List<AlignmentSpotProperty> FilterByBlank(List<AlignmentSpotProperty> alignments) {
            if (_param.IsRemoveFeatureBasedOnBlankPeakHeightFoldChange) {
                var blankFilter = new BlankFilter(
                    _param.FileID_AnalysisFileType,
                    _param.FoldChangeForBlankFiltering,
                    _param.BlankFiltering,
                    _param.IsKeepRefMatchedMetaboliteFeatures,
                    _param.IsKeepSuggestedMetaboliteFeatures,
                    _param.IsKeepRemovableFeaturesAndAssignedTagForChecking,
                    evaluator);

                return blankFilter.Filter(alignments).ToList();
            }
            return alignments;
        }

        private static List<int> SetAlignmentID(ICollection<AlignmentSpotProperty> alignments) {
            var id = 0;
            var ids = new List<int>();
            foreach (var spot in alignments) {
                ids.Add(spot.MasterAlignmentID);
                spot.MasterAlignmentID = spot.AlignmentID = id++;
            }
            return ids;
        }

        private static void SetIsotopes(IEnumerable<AlignmentSpotProperty> spots) {
            foreach (var spot in spots) {
                spot.PeakCharacter.IsotopeWeightNumber = 0; 
            }
        }

        private void SetStatProperty(List<AlignmentSpotProperty> alignments) {
            var sampleIds = _param.FileID_AnalysisFileType.Where(kvp => kvp.Value == AnalysisFileType.Sample).Select(kvp => kvp.Key);
            var id2class = sampleIds.ToDictionary(id => id, id => _param.FileID_ClassName[id]);
            alignments.AsParallel().ForAll(spot => spot.CalculateFoldChange(id2class));
            var nClass = id2class.Values.Distinct().Count();
            if (nClass > 1 && nClass < id2class.Count) {
                alignments.AsParallel().ForAll(spot => spot.CalculateAnovaPvalue(id2class));
            }
        }

        private void SetLinks(List<AlignmentSpotProperty> alignments) {
            AssignLinksByIdentifiedIonFeatures(alignments);

            alignments.Sort((x, y) => y.HeightAverage.CompareTo(x.HeightAverage)); // descending
            AssignLinksByRepresentativeIonFeatures(alignments);

            alignments.Sort((x, y) => x.AlignmentID.CompareTo(y.AlignmentID));
            AssignPutativePeakgroupIDs(alignments);
        }

        private void AssignLinksByIdentifiedIonFeatures(List<AlignmentSpotProperty> cSpots) {
            foreach (var cSpot in cSpots) {
                if (cSpot.IsReferenceMatched(evaluator)) {

                    //Console.WriteLine(cSpot.MspBasedMatchResult.Name + "\t" + cSpot.AdductType.AdductIonName);

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
                                    if (rSpot.IsReferenceMatched(evaluator)) {
                                        if (cSpot.AdductType.AdductIonName == rSpot.AdductType.AdductIonName)
                                            continue;
                                    }
                                    else {
                                        var rAdductCharge = AdductIonParser.GetChargeNumber(rSpot.AlignedPeakProperties[repFileID].PeakCharacter.AdductType.AdductIonName);
                                        if (rAdductCharge != rSpot.PeakCharacter.Charge)
                                            break;
                                        rSpot.SetAdductType(rSpot.AlignedPeakProperties[repFileID].PeakCharacter.AdductType);
                                    }
                                }
                                RegisterLinks(cSpot, rSpot, rLinkProp);
                                break;
                            }
                        }
                    }
                }
            }
            //Console.WriteLine();
        }

        private void AssignLinksByRepresentativeIonFeatures(List<AlignmentSpotProperty> alignments) {
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
                                if (rSpot.PeakCharacter.AdductType.HasAdduct) continue;
                                var adductCharge = AdductIonParser.GetChargeNumber(rSpot.AlignedPeakProperties[repFileID].PeakCharacter.AdductType.AdductIonName);
                                if (rSpot.PeakCharacter.Charge != adductCharge) continue;
                                adductCharge = AdductIonParser.GetChargeNumber(fcSpot.AlignedPeakProperties[repFileID].PeakCharacter.AdductType.AdductIonName);
                                if (fcSpot.PeakCharacter.Charge != adductCharge) continue;

                                RegisterLinks(fcSpot, rSpot, rLinkProp);
                                rSpot.SetAdductType(rSpot.AlignedPeakProperties[repFileID].PeakCharacter.AdductType);
                                if (!fcSpot.AdductType.HasAdduct) {
                                    fcSpot.SetAdductType(fcSpot.AlignedPeakProperties[repFileID].PeakCharacter.AdductType);
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

        private void RegisterLinks(AlignmentSpotProperty cSpot, AlignmentSpotProperty rSpot, PeakLinkFeatureEnum rLinkProp) {
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

        private static readonly PeakLinkFeatureEnum[] IgnoreFeatures = new PeakLinkFeatureEnum[] {
            PeakLinkFeatureEnum.ChromSimilar,
            PeakLinkFeatureEnum.CorrelSimilar,
            PeakLinkFeatureEnum.FoundInUpperMsMs,
        };

        private static void AssignPutativePeakgroupIDs(List<AlignmentSpotProperty> spots) {
            var groupID = 0;
            foreach (var spot in spots) {
                if (spot.PeakCharacter.PeakGroupID >= 0)
                    continue;

                var q = new Queue<AlignmentSpotProperty>();
                q.Enqueue(spot);
                while (q.Count != 0) {
                    var current = q.Dequeue();
                    current.PeakCharacter.PeakGroupID = groupID;

                    var links = current.PeakCharacter.PeakLinks ?? Enumerable.Empty<LinkedPeakFeature>();
                    foreach (var linkedPeak in links) {
                        var character = linkedPeak.Character;
                        if (IgnoreFeatures.Contains(character))
                            continue;

                        var next = spots[linkedPeak.LinkedPeakID];
                        if (next.PeakCharacter.PeakGroupID >= 0)
                            continue;

                        q.Enqueue(next);
                    }
                }

            }
        }

        private static void SetAdducts(IEnumerable<AlignmentSpotProperty> spots) {
            foreach (var spot in spots) {
                spot.SetAdductType(AdductIon.GetStandardAdductIon(spot.AdductType.ChargeNumber, spot.AdductType.IonMode));
            }
        }
    }
}
