using CompMs.Common.DataObj.Property;
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

    public class MspAnnotationDeduplicator : ISpotAction
    {
        public void Process(IEnumerable<AlignmentSpotProperty> spots) {
            var spotList = spots.OrderByDescending(spot => spot.MspID).ToList();

            var currentPeakId = 0;
            var currentLibraryId = spotList[currentPeakId].MspID;

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
