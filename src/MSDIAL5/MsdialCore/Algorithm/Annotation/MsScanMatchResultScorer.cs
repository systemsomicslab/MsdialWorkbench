using CompMs.Common.DataObj.Result;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation;

public sealed class MsScanMatchResultScorer<TQuery, TReference>(params IMatchScoreCalculator<TQuery, TReference, IMatchResult>[] calculators) : IReferenceScorer<TQuery, TReference, MsScanMatchResult> {
    public MsScanMatchResult Score(TQuery query, TReference reference) {
        var scores = new List<IMatchResult>();
        foreach (var calculator in calculators) {
            var score = calculator.Calculate(query, reference);
            scores.Add(score);
        }
        var result = new MsScanMatchResult();
        foreach (var score in scores) {
            score.Assign(result);
        }
        result.TotalScore = (float)scores.SelectMany(s => s.Scores).Average();
        return result;
    }
}
