using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            InnerMatchResults = new List<MsScanMatchResult> { };
        }

        [SerializationConstructor]
        public MsScanMatchResultContainer(ReadOnlyCollection<MsScanMatchResult> matchResults, Dictionary<int, MsScanMatchResult> mSRawID2MspBasedMatchResult, List<MsScanMatchResult> textDbBasedMatchResults) {
            var innerMatchResults = matchResults.ToList();
            innerMatchResults.RemoveAll(r => r.Source == SourceType.Unknown);
            InnerMatchResults = innerMatchResults;
            MSRawID2MspBasedMatchResult = mSRawID2MspBasedMatchResult;
            TextDbBasedMatchResults = textDbBasedMatchResults;
        }

        public MsScanMatchResultContainer(MsScanMatchResultContainer source)
            : this(source.MatchResults, source.MSRawID2MspBasedMatchResult.ToDictionary(kvp => kvp.Key, kvp => kvp.Value), source.TextDbBasedMatchResults.ToList()) {
        }

        // general match results
        [Key(0)]
        public ReadOnlyCollection<MsScanMatchResult> MatchResults => (InnerMatchResults.Any() ? InnerMatchResults : new List<MsScanMatchResult> { UnknownResult }).AsReadOnly();

        private List<MsScanMatchResult> InnerMatchResults { get; } 

        [IgnoreMember]
        public MsScanMatchResult Representative {
            get {
                if (cacheRepresentative is null) {
                    var results = InnerMatchResults.Where(result => !result.IsDecoy);
                    cacheRepresentative = results.DefaultIfEmpty(UnknownResult).Argmax(ResultOrder);
                }
                return cacheRepresentative;
            }
        }
        private MsScanMatchResult cacheRepresentative = null;

        [IgnoreMember]
        public IEnumerable<MsScanMatchResult> TopResults => MatchResults.Where(result => !result.IsDecoy).OrderByDescending(ResultOrder);

        [IgnoreMember]
        public MsScanMatchResult DecoyRepresentative {
            get {
                if (cacheDecoyRepresentative is null) {
                    var decoyResults = MatchResults.Where(result => result.IsDecoy).ToArray();
                    cacheDecoyRepresentative = decoyResults.Length > 0
                        ? decoyResults.Argmax(ResultOrder)
                        : null;
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

        public string RepresentativeName(IMatchResultRefer<PeptideMsReference, MsScanMatchResult> refer) {
            return refer.Refer(Representative) is PeptideMsReference db ? db.Peptide.ModifiedSequence : "null";
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

        public string RepresentativeFormula(IMatchResultRefer<PeptideMsReference, MsScanMatchResult> refer) {
            var db = refer.Refer(Representative);
            return string.IsNullOrEmpty(db?.Peptide?.Formula?.FormulaString) ? "null" : db.Peptide.Formula.FormulaString;
        }

        public string RepresentativeProtein(IMatchResultRefer<PeptideMsReference, MsScanMatchResult> refer) {
            return refer.Refer(Representative) is PeptideMsReference db ? db.Peptide.DatabaseOrigin + "; " + db.Peptide.Position.Start + "-" + db.Peptide.Position.End : "null";
        }

        public bool IsReferenceMatched(IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            var representative = Representative;
            if (representative.IsManuallyModified) {
                return !representative.IsUnknown; // confidense or unsettled
            }
            return evaluator?.IsReferenceMatched(representative) ?? false;
        }

        public bool IsAnnotationSuggested(IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            var representative = Representative;
            if (representative.IsManuallyModified && !representative.IsUnknown) {
                return false; // confidense or unsettled
            }
            else {
                return evaluator?.IsAnnotationSuggested(representative) ?? false;
            }
        }

        [IgnoreMember]
        public bool IsUnknown => (Representative.Source & SourceType.Unknown) == SourceType.Unknown;

        public void AddResult(MsScanMatchResult result) {
            AddResultCore(result);
        }

        private void AddResultCore(MsScanMatchResult result) {
            InnerMatchResults.Add(result);
            cacheRepresentative = null;
        }

        public void AddResults(IEnumerable<MsScanMatchResult> results) {
            AddResultsCore(results);
        }

        private void AddResultsCore(IEnumerable<MsScanMatchResult> results) {
            InnerMatchResults.AddRange(results.Where(result => !InnerMatchResults.Contains(result)));
            cacheRepresentative = null;
        }

        public void RemoveResult(MsScanMatchResult result) {
            if (!InnerMatchResults.Contains(result)) {
                return;
            }
            InnerMatchResults.Remove(result);
            if (cacheRepresentative == result) {
                cacheRepresentative = null;
            }
        }

        public void ClearResults() {
            ClearResultsCore();
        }

        private void ClearResultsCore() {
            InnerMatchResults.Clear();
            cacheRepresentative = null;
        }

        public List<MsScanMatchResult> GetManuallyResults() {
            return InnerMatchResults.Where(result => (result.Source & SourceType.Manual) != SourceType.None).ToList();
        }

        public void RemoveManuallyResults() {
            InnerMatchResults.RemoveAll(result => (result.Source & SourceType.Manual) != SourceType.None);
            cacheRepresentative = null;
        }

        // Msp based match results
        // MS raw id corresponds to ms2 raw ID (in MS/MS) and ms1 raw id (in EI-MS).
        [Key(1)]
        public Dictionary<int, MsScanMatchResult> MSRawID2MspBasedMatchResult { get; } = new Dictionary<int, MsScanMatchResult>();

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
                InnerMatchResults.Remove(kvp.Value);
            }
            MSRawID2MspBasedMatchResult.Clear();
            cacheRepresentative = null;
        }


        // TextDB based matched results
        [Key(2)]
        public List<MsScanMatchResult> TextDbBasedMatchResults { get; } = new List<MsScanMatchResult>();

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
                InnerMatchResults.Remove(result);
            }
            TextDbBasedMatchResults.Clear();
            cacheRepresentative = null;
        }

        public void MergeContainers(MsScanMatchResultContainer other) {
            AddResults(other.InnerMatchResults);
            foreach (var kvp in other.MSRawID2MspBasedMatchResult)
                MSRawID2MspBasedMatchResult.Add(kvp.Key, kvp.Value);
            TextDbBasedMatchResults.AddRange(other.TextDbBasedMatchResults);
        }

        private static Tuple<bool, bool, bool, int, float> ResultOrder(MsScanMatchResult result) {
            return Tuple.Create(result.IsManuallyModified, result.IsReferenceMatched, result.IsAnnotationSuggested, result.Priority, result.TotalScore);
        }
    }
}
