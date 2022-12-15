using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
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

        public static FacadeMatchResultEvaluator FromDataBases(DataBaseStorage storage) {
            var evaluator = new FacadeMatchResultEvaluator();
            foreach (var db in storage.MetabolomicsDataBases) {
                foreach (var pair in db.Pairs) {
                    evaluator.Add(pair.AnnotatorID, pair.AnnotationQueryFactory.CreateEvaluator());
                }
            }
            foreach (var db in storage.ProteomicsDataBases) {
                foreach (var pair in db.Pairs) {
                    evaluator.Add(pair.AnnotatorID, pair.AnnotationQueryFactory.CreateEvaluator());
                }
            }
            foreach (var db in storage.EadLipidomicsDatabases) {
                foreach (var pair in db.Pairs) {
                    evaluator.Add(pair.AnnotatorID, pair.AnnotationQueryFactory.CreateEvaluator());
                }
            }
            return evaluator;
        }

        public static FacadeMatchResultEvaluator FromDataBases(DataBaseStorage storage, IAnnotationQueryFactoryGenerationVisitor factoryVisitor, ILoadAnnotatorVisitor annotatorVisitor) {
            var evaluator = new FacadeMatchResultEvaluator();
            foreach (var db in storage.MetabolomicsDataBases) {
                foreach (var pair in db.Pairs) {
                    evaluator.Add(pair.AnnotatorID, new MsScanMatchResultEvaluator(pair.AnnotationQueryFactory.PrepareParameter()));
                }
            }
            foreach (var db in storage.ProteomicsDataBases) {
                foreach (var pair in db.Pairs) {
                    evaluator.Add(pair.AnnotatorID, new MsScanMatchResultEvaluator(pair.AnnotationQueryFactory.PrepareParameter()));
                }
            }
            foreach (var db in storage.EadLipidomicsDatabases) {
                foreach (var pair in db.Pairs) {
                    evaluator.Add(pair.AnnotatorID, new MsScanMatchResultEvaluator(pair.AnnotationQueryFactory.PrepareParameter()));
                }
            }
            return evaluator;
        }
    }
}
