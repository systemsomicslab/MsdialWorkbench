using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public sealed class FacadeMatchResultEvaluator : IMatchResultEvaluator<MsScanMatchResult>
    {
        public FacadeMatchResultEvaluator() {
            evaluators = new Dictionary<string, IMatchResultEvaluator<MsScanMatchResult>>();
        }

        private readonly Dictionary<string, IMatchResultEvaluator<MsScanMatchResult>> evaluators;

        public void Add(string key, IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            evaluators.Add(key, evaluator);
        }

        public void Remove(string key) {
            evaluators.Remove(key);
        }

        private IMatchResultEvaluator<MsScanMatchResult> Get(string id) {
            return id != null && evaluators.TryGetValue(id, out var evaluator) ? evaluator : null;
        }

        public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results) {
            return results.SelectMany(result => Get(result?.AnnotatorID)?.FilterByThreshold(IEnumerableExtension.Return(result))).ToList();
        }

        public bool IsAnnotationSuggested(MsScanMatchResult result) {
            return Get(result?.AnnotatorID)?.IsAnnotationSuggested(result) ?? false;
        }

        public bool IsReferenceMatched(MsScanMatchResult result) {
            return Get(result?.AnnotatorID)?.IsReferenceMatched(result) ?? false;
        }

        public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results) {
            return results.SelectMany(result => Get(result?.AnnotatorID)?.SelectReferenceMatchResults(IEnumerableExtension.Return(result))).ToList();
        }

        public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results) {
            return results.DefaultIfEmpty()
                .Argmax(result => (result?.Source ?? SourceType.None, result?.Priority ?? -1, result?.TotalScore ?? 0));
        }

        [Obsolete("This method is for refactoring and will be removed in the near future.")]
        public static FacadeMatchResultEvaluator FromDataBaseMapper(DataBaseMapper mapper) {
            var evaluator = new FacadeMatchResultEvaluator();
            foreach (var annotator in mapper.MoleculeAnnotators) {
                evaluator.Add(annotator.AnnotatorID, annotator.Annotator);
            }
            foreach (var annotator in mapper.PeptideAnnotators) {
                evaluator.Add(annotator.AnnotatorID, annotator.Annotator);
            }
            return evaluator;
        }

        public static FacadeMatchResultEvaluator FromDataBases(DataBaseStorage storage) {
            var evaluator = new FacadeMatchResultEvaluator();
            foreach (var db in storage.MetabolomicsDataBases) {
                foreach (var pair in db.Pairs) {
                    evaluator.Add(pair.AnnotatorID, pair.SerializableAnnotator);
                }
            }
            foreach (var db in storage.ProteomicsDataBases) {
                foreach (var pair in db.Pairs) {
                    evaluator.Add(pair.AnnotatorID, pair.SerializableAnnotator);
                }
            }
            foreach (var db in storage.EadLipidomicsDatabases) {
                foreach (var pair in db.Pairs) {
                    evaluator.Add(pair.AnnotatorID, pair.SerializableAnnotator);
                }
            }
            return evaluator;
        }
    }
}
