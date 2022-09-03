using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public interface IMatchResultEvaluator<TResult>
    {
        TResult SelectTopHit(IEnumerable<TResult> results);
        List<TResult> FilterByThreshold(IEnumerable<TResult> results);
        List<TResult> SelectReferenceMatchResults(IEnumerable<TResult> results);
        bool IsReferenceMatched(TResult result);
        bool IsAnnotationSuggested(TResult result);
    }

    public static class MatchResultEvaluatorExtension {
        public static IMatchResultEvaluator<TSource> Contramap<TSource, TTarget>(this IMatchResultEvaluator<TTarget> evaluator, Func<TSource, TTarget> consumer) {
            return new EvaluatorImpl<TSource, TTarget>(evaluator, consumer);
        }

        class EvaluatorImpl<TSource, TTarget> : IMatchResultEvaluator<TSource>
        {
            private readonly IMatchResultEvaluator<TTarget> _evaluator;
            private readonly Func<TSource, TTarget> _consumer;

            public EvaluatorImpl(IMatchResultEvaluator<TTarget> evaluator, Func<TSource, TTarget> consumer) {
                _evaluator = evaluator;
                _consumer = consumer;
            }

            public List<TSource> FilterByThreshold(IEnumerable<TSource> results) {
                var map = results.ToDictionary(result => _consumer(result), result => result);
                return _evaluator.FilterByThreshold(map.Keys).Select(result => map[result]).ToList();
            }

            public bool IsAnnotationSuggested(TSource result) {
                return _evaluator.IsAnnotationSuggested(_consumer(result));
            }

            public bool IsReferenceMatched(TSource result) {
                return _evaluator.IsReferenceMatched(_consumer(result));
            }

            public List<TSource> SelectReferenceMatchResults(IEnumerable<TSource> results) {
                var map = results.ToDictionary(result => _consumer(result), result => result);
                return _evaluator.SelectReferenceMatchResults(map.Keys).Select(result => map[result]).ToList();
            }

            public TSource SelectTopHit(IEnumerable<TSource> results) {
                var pairs = results.Select(result => (target: _consumer(result), result)).ToArray();
                var target = _evaluator.SelectTopHit(pairs.Select(pair => pair.target));
                return pairs.FirstOrDefault(pair => Equals(pair.target, target)).result;
            }
        }
    }
}
