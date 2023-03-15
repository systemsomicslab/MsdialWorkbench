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
        public static IEnumerable<TResult> SelectTopN<TResult>(this IMatchResultEvaluator<TResult> evaluator, IEnumerable<TResult> results, int number) {
            var results_ = results.ToList();
            for (int i = 0; i < number; i++) {
                if (results_.Count == 0) {
                    yield break;
                }
                var result = evaluator.SelectTopHit(results_);
                yield return result;
                results_.Remove(result);
            }
        }

        public static IMatchResultEvaluator<TSource> Contramap<TSource, TTarget>(this IMatchResultEvaluator<TTarget> evaluator, Func<TSource, TTarget> consumer) {
            return new ConsumingEvaluatorImpl<TSource, TTarget>(evaluator, consumer);
        }

        class ConsumingEvaluatorImpl<TSource, TTarget> : IMatchResultEvaluator<TSource>
        {
            private readonly IMatchResultEvaluator<TTarget> _evaluator;
            private readonly Func<TSource, TTarget> _consumer;

            public ConsumingEvaluatorImpl(IMatchResultEvaluator<TTarget> evaluator, Func<TSource, TTarget> consumer) {
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

        public static IMatchResultEvaluator<TSource> Contramap<TSource, TTarget>(this IMatchResultEvaluator<TTarget> evaluator, Func<TSource, TTarget> consume, Func<IMatchResultEvaluator<TTarget>, TSource, bool> isRefMatched, Func<IMatchResultEvaluator<TTarget>, TSource, bool> isSuggested) {
            return new ContramapEvaluatorImpl<TSource>(evaluator.Contramap(consume), t => isRefMatched(evaluator, t), t => isSuggested(evaluator, t));
        }

        class ContramapEvaluatorImpl<TTarget> : IMatchResultEvaluator<TTarget> {
            private readonly IMatchResultEvaluator<TTarget> _evaluator;
            private readonly Func<TTarget, bool> _isRefMatchted;
            private readonly Func<TTarget, bool> _isSuggested;

            public ContramapEvaluatorImpl(IMatchResultEvaluator<TTarget> evaluator, Func<TTarget, bool> isRefMatchted, Func<TTarget, bool> isSuggested) {
                _evaluator = evaluator;
                _isRefMatchted = isRefMatchted;
                _isSuggested = isSuggested;
            }

            public List<TTarget> FilterByThreshold(IEnumerable<TTarget> results) => _evaluator.FilterByThreshold(results);
            public bool IsAnnotationSuggested(TTarget result) => _isSuggested(result);
            public bool IsReferenceMatched(TTarget result) => _isRefMatchted(result);
            public List<TTarget> SelectReferenceMatchResults(IEnumerable<TTarget> results) => _evaluator.SelectReferenceMatchResults(results);
            public TTarget SelectTopHit(IEnumerable<TTarget> results) => _evaluator.SelectTopHit(results);
        }
    }
}
