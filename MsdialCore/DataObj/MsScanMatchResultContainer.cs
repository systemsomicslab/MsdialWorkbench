using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public class MsScanMatchResultContainer
    {
        public static readonly MsScanMatchResult UnknownResult;

        static MsScanMatchResultContainer() {
            UnknownResult = new MsScanMatchResult
            {
                Source = SourceType.Unknown,
            };
        }

        public MsScanMatchResultContainer() {
            MatchResults = new List<MsScanMatchResult>
            {
                UnknownResult,
            };
        }

        // general match results
        [Key(0)]
        public List<MsScanMatchResult> MatchResults { get; set; }

        [IgnoreMember]
        public MsScanMatchResult Representative {
            get {
                if (cacheRepresentative is null) {
                    cacheRepresentative =  MatchResults.Any()
                        ?  MatchResults.Argmax(result => Tuple.Create(result.Source, result.TotalScore))
                        : null;
                }
                return cacheRepresentative;
            }
        }
        private MsScanMatchResult cacheRepresentative = null;

        [IgnoreMember]
        public bool IsMspBasedRepresentative => (Representative.Source & SourceType.MspDB) == SourceType.MspDB
            || MSRawID2MspBasedMatchResult.Values.Contains(Representative);
        [IgnoreMember]
        public bool IsTextDbBasedRepresentative => (Representative.Source & SourceType.TextDB) == SourceType.TextDB
            || TextDbBasedMatchResults.Contains(Representative);
        [IgnoreMember]
        public bool IsManuallyModifiedRepresentative => (Representative.Source & SourceType.Manual) == SourceType.Manual;

        public bool IsReferenceMatched(DataBaseMapper mapper) {
            var representative = Representative;
            if (representative.IsManuallyModified && !representative.IsUnknown) {
                return true; // confidense or unsettled
            }
            var annotator = mapper.FindAnnotator(representative)?.Annotator;
            if (annotator is null) {
                return false;
            }
            return annotator.IsReferenceMatched(representative);
        }

        public bool IsAnnotationSuggested(DataBaseMapper mapper) {
            var representative = Representative;
            if (representative.IsManuallyModified && !representative.IsUnknown) {
                return false; // confidense or unsettled
            }
            var annotator = mapper.FindAnnotator(representative)?.Annotator;
            if (annotator is null) {
                return false;
            }
            return annotator.IsAnnotationSuggested(representative);
        }

        [IgnoreMember]
        public bool IsUnknown => (Representative.Source & SourceType.Unknown) == SourceType.Unknown;

        public void AddResult(MsScanMatchResult result) {
            AddResultCore(result);
        }

        private void AddResultCore(MsScanMatchResult result) {
            MatchResults.Add(result);
            cacheRepresentative = null;
        }

        public void AddResults(IEnumerable<MsScanMatchResult> results) {
            AddResultsCore(results);
        }

        private void AddResultsCore(IEnumerable<MsScanMatchResult> results) {
            MatchResults.AddRange(results);
            cacheRepresentative = null;
        }

        public void ClearResults() {
            ClearResultsCore();
        }

        private void ClearResultsCore() {
            MatchResults.Clear();
            MatchResults.Add(UnknownResult);
            cacheRepresentative = null;
        }

        public void RemoveManuallyResults() {
            MatchResults.RemoveAll(result => (result.Source & SourceType.Manual) != SourceType.None);
            cacheRepresentative = null;
        }

        // Msp based match results
        // MS raw id corresponds to ms2 raw ID (in MS/MS) and ms1 raw id (in EI-MS).
        [Key(1)]
        public Dictionary<int, MsScanMatchResult> MSRawID2MspBasedMatchResult { get; set; } = new Dictionary<int, MsScanMatchResult>();

        [IgnoreMember]
        public Dictionary<int, int> MSRawID2MspIDs => MSRawID2MspBasedMatchResult.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.LibraryID);

        [IgnoreMember]
        // get result having max score
        public MsScanMatchResult MspBasedMatchResult => MSRawID2MspBasedMatchResult.IsEmptyOrNull() ? null : MSRawID2MspBasedMatchResult.Values.Argmax(result => result.TotalScore);

        [IgnoreMember]
        public int MspID => MSRawID2MspBasedMatchResult.IsEmptyOrNull()
                    ? -1
                    : MSRawID2MspBasedMatchResult.Values.Argmax(result => result.TotalScore).LibraryID;

        public void AddMspResult(int msRawID, MsScanMatchResult result) {
            AddResultCore(result);
            MSRawID2MspBasedMatchResult.Add(msRawID, result);
        }

        public void AddMspResults(IDictionary<int, MsScanMatchResult> results) {
            AddResults(results.Values);
            foreach (var kvp in results)
                MSRawID2MspBasedMatchResult.Add(kvp.Key, kvp.Value);
        }

        public void ClearMspResults() {
            foreach (var kvp in MSRawID2MspBasedMatchResult) {
                MatchResults.Remove(kvp.Value);
            }
            MSRawID2MspBasedMatchResult.Clear();
            cacheRepresentative = null;
        }


        // TextDB based matched results
        [Key(2)]
        public List<MsScanMatchResult> TextDbBasedMatchResults { get; set; } = new List<MsScanMatchResult>();

        // ID list having the metabolite candidates exceeding the threshold (optional)
        [IgnoreMember]
        public List<int> TextDbIDs => TextDbBasedMatchResults.Select(result => result.LibraryID).ToList();

        [IgnoreMember]
        public int TextDbID => TextDbBasedMatchResults.IsEmptyOrNull() ? -1 : TextDbBasedMatchResults.Argmax(result => result.TotalScore).LibraryID;

        public void AddTextDbResult(MsScanMatchResult result) {
            AddResultCore(result);
            TextDbBasedMatchResults.Add(result);
        }

        public void AddTextDbResults(IEnumerable<MsScanMatchResult> results) {
            AddResultsCore(results);
            TextDbBasedMatchResults.AddRange(results);
        }

        public void ClearTextDbResults() {
            foreach (var result in TextDbBasedMatchResults) {
                MatchResults.Remove(result);
            }
            TextDbBasedMatchResults.Clear();
            cacheRepresentative = null;
        }

        public void MergeContainers(MsScanMatchResultContainer other) {
            AddResults(other.MatchResults);
            foreach (var kvp in other.MSRawID2MspBasedMatchResult)
                MSRawID2MspBasedMatchResult.Add(kvp.Key, kvp.Value);
            TextDbBasedMatchResults.AddRange(other.TextDbBasedMatchResults);
        }
    }
}
