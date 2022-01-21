using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
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

                    if (MatchResults.Any()) {
                        var results = MatchResults.Where(n => !n.IsDecoy);
                        if (results.IsEmptyOrNull()) {
                            return null;
                        }
                        else {
                            cacheRepresentative = results.Argmax(result => Tuple.Create(result.Source, result.Priority, result.TotalScore));
                            return cacheRepresentative;
                        }
                    }
                    else {
                        cacheRepresentative = null;
                    }
                    //cacheRepresentative = MatchResults.Any()
                    //    ? MatchResults.Where(n => !n.IsDecoy).Argmax(result => Tuple.Create(result.Source, result.Priority, result.TotalScore))
                    //    : null;
                }
                return cacheRepresentative;
            }
        }
        private MsScanMatchResult cacheRepresentative = null;

        [IgnoreMember]
        public MsScanMatchResult DecoyRepresentative {
            get {
                if (cacheDecoyRepresentative is null) {
                    if (MatchResults.Any()) {
                        var decoyResults = MatchResults.Where(n => n.IsDecoy);
                        if (decoyResults.IsEmptyOrNull()) {
                            return null;
                        }
                        else {
                            cacheDecoyRepresentative = decoyResults.Argmax(result => Tuple.Create(result.Source, result.Priority, result.TotalScore));
                            return cacheDecoyRepresentative;
                        }
                    }
                    else {
                        cacheDecoyRepresentative = null;
                    }
                    //cacheDecoyRepresentative = MatchResults.Any()
                    //    ? MatchResults.Where(n => n.IsDecoy).Argmax(result => Tuple.Create(result.Source, result.Priority, result.TotalScore))
                    //    : null;
                }
                return cacheDecoyRepresentative;
            }
        }
        private MsScanMatchResult cacheDecoyRepresentative = null;

        [IgnoreMember]
        public bool IsMspBasedRepresentative => (Representative.Source & SourceType.MspDB) == SourceType.MspDB
            || MSRawID2MspBasedMatchResult.Values.Contains(Representative);
        [IgnoreMember]
        public bool IsTextDbBasedRepresentative => (Representative.Source & SourceType.TextDB) == SourceType.TextDB
            || TextDbBasedMatchResults.Contains(Representative);
        [IgnoreMember]
        public bool IsManuallyModifiedRepresentative => (Representative.Source & SourceType.Manual) == SourceType.Manual;

        public TReference GetRepresentativeReference<TReference>(IMatchResultRefer<TReference, MsScanMatchResult> refer) {
            return ReferCache<TReference>.Single.Get(refer, Representative);
        }

        private class ReferCache<TReference> {
            static ReferCache() {
                if (typeof(TReference) == typeof(PeptideMsReference)) {
                    Single = new ReferCache<TReference>(r => r.Source == SourceType.FastaDB);
                }
                else if (typeof(TReference) == typeof(MoleculeMsReference)) {
                    Single = new ReferCache<TReference>(r => r.Source != SourceType.FastaDB);
                }
                else {
                    Single = new ReferCache<TReference>(_ => false);
                }
            }

            private ReferCache(Func<MsScanMatchResult, bool> pred) {
                this.pred = pred;
            }

            private readonly Func<MsScanMatchResult, bool> pred;

            public static readonly ReferCache<TReference> Single;

            public TReference Get(IMatchResultRefer<TReference, MsScanMatchResult> refer, MsScanMatchResult result) {
                return pred(result) ? refer.Refer(result) : default;
            }
        }

        public string RepresentativeName(IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer) {
            return refer.Refer(Representative) is MoleculeMsReference db ? db.Name : "null";
        }

        public string RepresentativeSMILES(IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer) {
            var representative = Representative;
            if (representative.Source == SourceType.FastaDB) {
                return "null";
            }
            else {
                var db = refer.Refer(representative);
                return string.IsNullOrEmpty(db?.SMILES) ? "null" : db.SMILES;
            }
        }

        public string RepresentativeOntology(IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer) {
            var db = refer.Refer(Representative);
            if (!string.IsNullOrEmpty(db?.CompoundClass)) {
                return db.CompoundClass;
            }
            else {
                return string.IsNullOrEmpty(db?.Ontology) ? "null" : db.Ontology;
            }
        }

        public string RepresentativeInChIKey(IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer) {
            var representative = Representative;
            if (representative.Source == SourceType.FastaDB) {
                return "null";
            }
            else {
                var db = refer.Refer(representative);
                return string.IsNullOrEmpty(db?.InChIKey) ? "null" : db.InChIKey;
            }
        }

        public string RepresentativeFormula(IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer) {
            var db = refer.Refer(Representative);
            return string.IsNullOrEmpty(db?.Formula?.FormulaString) ? "null" : db.Formula.FormulaString;
        }

        public bool IsReferenceMatched(DataBaseMapper mapper) {
            var representative = Representative;
            if (representative.IsManuallyModified && !representative.IsUnknown) {
                return true; // confidense or unsettled
            }
            var evaluator = representative.Source == SourceType.FastaDB
                ? (IMatchResultEvaluator<MsScanMatchResult>)mapper.FindPeptideAnnotator(representative)?.Annotator
                : (IMatchResultEvaluator<MsScanMatchResult>)mapper.FindMoleculeAnnotator(representative)?.Annotator;
            return evaluator?.IsReferenceMatched(representative) ?? false;
        }

        public bool IsAnnotationSuggested(DataBaseMapper mapper) {
            var representative = Representative;
            if (representative.IsManuallyModified && !representative.IsUnknown) {
                return false; // confidense or unsettled
            }
            var evaluator = representative.Source == SourceType.FastaDB
                ? (IMatchResultEvaluator<MsScanMatchResult>)mapper.FindPeptideAnnotator(representative)?.Annotator
                : (IMatchResultEvaluator<MsScanMatchResult>)mapper.FindMoleculeAnnotator(representative)?.Annotator;
            return evaluator?.IsAnnotationSuggested(representative) ?? false;
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
