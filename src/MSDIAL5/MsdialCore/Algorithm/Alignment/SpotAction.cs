using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Alignment
{
    public class RepresentativeSetter : ISpotAction
    {
        public void Process(IEnumerable<AlignmentSpotProperty> spots) {
            foreach (var spot in spots) {
                foreach (var child in spot.AlignmentDriftSpotFeatures) {
                    DataObjConverter.SetRepresentativeProperty(child);
                }
                DataObjConverter.SetRepresentativeProperty(spot);
            }
        }
    }

    public class MatchResultAnnotationDeduplicator : ISpotAction 
    {
        IMatchResultEvaluator<MsScanMatchResult> _evaluator;
        public MatchResultAnnotationDeduplicator(IMatchResultEvaluator<MsScanMatchResult> evaluator)
        {
            _evaluator = evaluator;
        }

        public void Process(IEnumerable<AlignmentSpotProperty> spots) {
            var spotList = spots.Where(n => n.IsReferenceMatched(_evaluator) && !n.Name.StartsWith("Putative")).OrderByDescending(spot => spot.MatchResults.Representative.LibraryID).ToList();
            var currentPeakId = 0;
            var currentLibraryId = spotList[currentPeakId].MatchResults.Representative.LibraryID;

            // by ID
            for (int i = 1; i < spotList.Count; i++) {
                var libID = spotList[i].MatchResults.Representative.LibraryID;
                if (libID < 0) break;
                if (libID != currentLibraryId) {
                    currentPeakId = i;
                    currentLibraryId = spotList[currentPeakId].MatchResults.Representative.LibraryID;
                    continue;
                }
                else {
                    if (spotList[currentPeakId].MatchResults.Representative.TotalScore < spotList[i].MatchResults.Representative.TotalScore) {
                        ChangeAnnotationToLowScore(spotList[currentPeakId]);
                        currentPeakId = i;
                    }
                    else {
                        ChangeAnnotationToLowScore(spotList[i]);
                    }
                }
            }

            // by InChIKey
            spotList = spots
                       .Where(n => n.IsReferenceMatched(_evaluator) && !n.Name.StartsWith("Putative"))
                       .Where(n => !string.IsNullOrEmpty(n.MatchResults?.Representative?.InChIKey) && n.MatchResults.Representative.InChIKey.Length > 1)
                       .OrderByDescending(spot => spot.MatchResults.Representative.InChIKey)
                       .ToList();
            currentPeakId = 0;
            if (spotList.Count > 0) {
                var currentInChIKey = spotList[currentPeakId].MatchResults.Representative.InChIKey;
                for (int i = 1; i < spotList.Count; i++) {
                    if (spotList[i].MatchResults.Representative.InChIKey != currentInChIKey) {
                        currentPeakId = i;
                        currentInChIKey = spotList[currentPeakId].MatchResults.Representative.InChIKey;
                        continue;
                    }
                    else {
                        if (spotList[currentPeakId].MatchResults.Representative.TotalScore < spotList[i].MatchResults.Representative.TotalScore) {
                            ChangeAnnotationToLowScore(spotList[currentPeakId]);
                            currentPeakId = i;
                        }
                        else {
                            ChangeAnnotationToLowScore(spotList[i]);
                        }
                    }
                }
            }

            // by Name
            spotList = spots
                .Where(n => n.IsReferenceMatched(_evaluator) && !n.Name.StartsWith("Putative"))
                .Where(n => !n.MatchResults.Representative.Name.IsEmptyOrNull())
                .OrderByDescending(spot => spot.MatchResults.Representative.Name)
                .ToList();
            currentPeakId = 0;
            if (spotList.Count > 0) {
                var currentName = spotList[currentPeakId].MatchResults.Representative.Name.ToLower();
                for (int i = 1; i < spotList.Count; i++) {
                    if (spotList[i].MatchResults.Representative.Name.ToLower() != currentName) {
                        currentPeakId = i;
                        currentName = spotList[currentPeakId].MatchResults.Representative.Name;
                        continue;
                    }
                    else {
                        if (spotList[currentPeakId].MatchResults.Representative.TotalScore < spotList[i].MatchResults.Representative.TotalScore) {
                            ChangeAnnotationToLowScore(spotList[currentPeakId]);
                            currentPeakId = i;
                        }
                        else {
                            ChangeAnnotationToLowScore(spotList[i]);
                        }
                    }
                }
            }
        }

        static void ChangeAnnotationToLowScore(AlignmentSpotProperty spot) {
            //spot.MatchResults.Representative.IsReferenceMatched = false;
            spot.Name = "Putative: " + spot.Name;
        }
    }

    public class MspAnnotationDeduplicator : ISpotAction
    {
        public void Process(IEnumerable<AlignmentSpotProperty> spots) {
            var spotList = spots.OrderByDescending(spot => spot.MspID).ToList();

            var currentPeakId = 0;
            var currentLibraryId = spotList[currentPeakId].MspID;
            
            // by ID
            for (int i = 1; i < spotList.Count; i++) {
                if (spotList[i].MspID < 0) break;
                if (spotList[i].MspID != currentLibraryId) {
                    currentPeakId = i;
                    currentLibraryId = spotList[currentPeakId].MspID;
                    continue;
                }
                else {
                    if (spotList[currentPeakId].MspBasedMatchResult.TotalScore < spotList[i].MspBasedMatchResult.TotalScore) {
                        SetDefaultCompoundInformationInMspSearch(spotList[currentPeakId]);
                        currentPeakId = i;
                    }
                    else {
                        SetDefaultCompoundInformationInMspSearch(spotList[i]);
                    }
                }
            }

            // by InChIKey
            spotList = spots.Where(n => n.MspBasedMatchResult.InChIKey.Length > 1).OrderByDescending(spot => spot.MspBasedMatchResult.InChIKey).ToList();
            currentPeakId = 0;
            if (spotList.Count > 0)
            {
                var currentInChIKey = spotList[currentPeakId].MspBasedMatchResult.InChIKey;
                for (int i = 1; i < spotList.Count; i++) {
                    if (spotList[i].MspBasedMatchResult.InChIKey != currentInChIKey) {
                        currentPeakId = i;
                        currentInChIKey = spotList[currentPeakId].MspBasedMatchResult.InChIKey;
                        continue;
                    }
                    else {
                        if (spotList[currentPeakId].MspBasedMatchResult.TotalScore < spotList[i].MspBasedMatchResult.TotalScore) {
                            SetDefaultCompoundInformationInMspSearch(spotList[currentPeakId]);
                            currentPeakId = i;
                        }
                        else {
                            SetDefaultCompoundInformationInMspSearch(spotList[i]);
                        }
                    }
                }
            }

            // by Name
            spotList = spots.Where(n => !n.MspBasedMatchResult.Name.IsEmptyOrNull()).OrderByDescending(spot => spot.MspBasedMatchResult.Name).ToList();
            currentPeakId = 0;
            if (spotList.Count > 0)
            {
                var currentName = spotList[currentPeakId].MspBasedMatchResult.Name;
                for (int i = 1; i < spotList.Count; i++) {
                    if (spotList[i].MspBasedMatchResult.Name != currentName) {
                        currentPeakId = i;
                        currentName = spotList[currentPeakId].MspBasedMatchResult.Name;
                        continue;
                    }
                    else {
                        if (spotList[currentPeakId].MspBasedMatchResult.TotalScore < spotList[i].MspBasedMatchResult.TotalScore) {
                            SetDefaultCompoundInformationInMspSearch(spotList[currentPeakId]);
                            currentPeakId = i;
                        }
                        else {
                            SetDefaultCompoundInformationInMspSearch(spotList[i]);
                        }
                    }
                }
            }
        }

        static void SetDefaultCompoundInformationInMspSearch(AlignmentSpotProperty spot) {
            var mspdb = spot.MspBasedMatchResult;
            var textdb = spot.TextDbBasedMatchResult;

            // if spot.Name is based on msp match result.
            if (spot.Name == mspdb.Name && spot.Name != textdb.Name) {
                spot.SetAdductType(AdductIon.Default);
                spot.PeakCharacter.Charge = 1;
                spot.Name = string.Empty;
            }

            spot.MSRawID2MspBasedMatchResult = new Dictionary<int, Common.DataObj.Result.MsScanMatchResult>();
            spot.MatchResults.ClearMspResults();
        }
    }

    public class TextAnnotationDeduplicator : ISpotAction
    {
        public void Process(IEnumerable<AlignmentSpotProperty> spots) {
            var spotList = spots.OrderByDescending(spot => spot.TextDbID).ToList();

            var currentPeakId = 0;
            var currentLibraryId = spotList[currentPeakId].TextDbID;

            for (int i = 1; i < spotList.Count; i++) {
                if (spotList[i].TextDbID < 0) break;
                if (spotList[i].TextDbID != currentLibraryId) {
                    currentPeakId = i;
                    currentLibraryId = spotList[currentPeakId].TextDbID;
                    continue;
                }
                else {
                    if (spotList[currentPeakId].TextDbBasedMatchResult.TotalScore < spotList[i].TextDbBasedMatchResult.TotalScore) {
                        SetDefaultCompoundInformationInTextSearch(spotList[currentPeakId]);
                        currentPeakId = i;
                    }
                    else {
                        SetDefaultCompoundInformationInTextSearch(spotList[i]);
                    }
                }
            }
        }

        static void SetDefaultCompoundInformationInTextSearch(AlignmentSpotProperty spot) {
            var mspdb = spot.MspBasedMatchResult;
            var textdb = spot.TextDbBasedMatchResult;

            // if spot.Name is based on text db match result.
            if (spot.Name == textdb.Name && spot.Name != mspdb.Name) {
                spot.SetAdductType(AdductIon.Default);
                spot.PeakCharacter.Charge = 1;
                spot.Name = string.Empty;
            }

            spot.TextDbBasedMatchResult = null;
            spot.MatchResults.ClearTextDbResults();
        }
    }

}
