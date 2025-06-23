using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public sealed class Ms2MatchCalculator : IMatchScoreCalculator<IMSScanMatchQuery, IMSScanProperty, Ms2MatchResult>
    {
        public Ms2MatchResult Calculate(IMSScanMatchQuery query, IMSScanProperty reference) {
            var sqweightedDotProduct = MsScanMatching.GetWeightedDotProduct(query.Scan, reference, query.Ms2Tolerance, query.Ms2RangeBegin, query.Ms2RangeEnd);
            var sqsimpleDotProduct = MsScanMatching.GetSimpleDotProduct(query.Scan, reference, query.Ms2Tolerance, query.Ms2RangeBegin, query.Ms2RangeEnd);
            var sqreverseDotProduct = MsScanMatching.GetReverseDotProduct(query.Scan, reference, query.Ms2Tolerance, query.Ms2RangeBegin, query.Ms2RangeEnd);
            var matchedPeaksScores = MsScanMatching.GetMatchedPeaksScores(query.Scan, reference, query.Ms2Tolerance, query.Ms2RangeBegin, query.Ms2RangeEnd);

            if (sqweightedDotProduct == -1
                || sqsimpleDotProduct == -1
                || sqreverseDotProduct == -1
                || matchedPeaksScores[0] == -1
                || matchedPeaksScores[1] == -1) {
                return Ms2MatchResult.Empty;
            }

            var isSpectrumMatch =
                sqweightedDotProduct >= query.WeightedDotProductCutOff
                && sqsimpleDotProduct >= query.SimpleDotProductCutOff
                && sqreverseDotProduct >= query.ReverseDotProductCutOff
                && matchedPeaksScores[0] >= query.MatchedPeaksPercentageCutOff
                && matchedPeaksScores[1] >= query.MinimumSpectrumMatch;
            return new Ms2MatchResult(
                sqweightedDotProduct, sqsimpleDotProduct, sqreverseDotProduct,
                matchedPeaksScores[0], (int)matchedPeaksScores[1],
                isSpectrumMatch);
        }
    }

    public interface IMSScanMatchQuery
    {
        IMSScanProperty Scan { get; }
        double Ms2Tolerance { get; }
        double Ms2RangeBegin { get; }
        double Ms2RangeEnd { get; }

        double WeightedDotProductCutOff { get; }
        double SimpleDotProductCutOff { get; }
        double ReverseDotProductCutOff { get; }
        double MatchedPeaksPercentageCutOff { get; }
        int MinimumSpectrumMatch { get; }
    }

    public sealed class MSScanMatchQuery : IMSScanMatchQuery
    {
        public IMSScanProperty Scan { get; }
        public double Ms2Tolerance { get; }
        public double Ms2RangeBegin { get; }
        public double Ms2RangeEnd { get; }

        public double WeightedDotProductCutOff { get; }
        public double SimpleDotProductCutOff { get; }
        public double ReverseDotProductCutOff { get; }
        public double MatchedPeaksPercentageCutOff { get; }
        public int MinimumSpectrumMatch { get; }

        public MSScanMatchQuery(
            IMSScanProperty scan,
            double ms2Tolerance, double ms2RangeBegin, double ms2RangeEnd,
            double weightedDotProductCutOff, double simpleDotProductCutOff, double reverseDotProductCutOff,
            double matchedPeaksPercentageCutOff, int minimumSpectrumMatch) {
            Scan = scan ?? throw new ArgumentNullException(nameof(scan));
            Ms2Tolerance = ms2Tolerance;
            Ms2RangeBegin = ms2RangeBegin;
            Ms2RangeEnd = ms2RangeEnd;
            WeightedDotProductCutOff = weightedDotProductCutOff;
            SimpleDotProductCutOff = simpleDotProductCutOff;
            ReverseDotProductCutOff = reverseDotProductCutOff;
            MatchedPeaksPercentageCutOff = matchedPeaksPercentageCutOff;
            MinimumSpectrumMatch = minimumSpectrumMatch;
        }

        public MSScanMatchQuery(IMSScanProperty scan, MsRefSearchParameterBase parameter)
            : this(
                  scan,
                  parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd,
                  parameter.SquaredWeightedDotProductCutOff, parameter.SquaredSimpleDotProductCutOff, parameter.SquaredReverseDotProductCutOff,
                  parameter.MatchedPeaksPercentageCutOff, (int)parameter.MinimumSpectrumMatch) {

        }
    }

    public interface IMs2MatchResult : IMatchResult
    {
        double SquaredWeightedDotProduct { get; }
        double SquaredSimpleDotProduct { get; }
        double SquaredReverseDotProduct { get; }
        int MatchedPeaksCount { get; }
        double MatchedPeaksPercentage { get; }

        bool IsSpectrumMatch { get; }
    }

    public class Ms2MatchResult : IMs2MatchResult
    {
        public double SquaredWeightedDotProduct { get; }
        public double SquaredSimpleDotProduct { get; }
        public double SquaredReverseDotProduct { get; }
        public int MatchedPeaksCount { get; }
        public double MatchedPeaksPercentage { get; }

        public double TotalScore => ((Math.Sqrt(SquaredWeightedDotProduct) + Math.Sqrt(SquaredSimpleDotProduct) + Math.Sqrt(SquaredReverseDotProduct)) / 3 + MatchedPeaksPercentage) / 2;
        public IEnumerable<double> Scores => new[] {
            (Math.Sqrt(SquaredWeightedDotProduct) + Math.Sqrt(SquaredSimpleDotProduct) + Math.Sqrt(SquaredReverseDotProduct)) / 3,
            MatchedPeaksPercentage,
        };

        public bool IsSpectrumMatch { get; }

        public Ms2MatchResult(
            double sqweightedDotProduct, double sqsimpleDotProduct, double sqreverseDotProduct,
            double matchedPeaksPercentage, int matchedPeaksCount,
            bool isSpectrumMatch) {
            SquaredWeightedDotProduct = sqweightedDotProduct;
            SquaredSimpleDotProduct = sqsimpleDotProduct;
            SquaredReverseDotProduct = sqreverseDotProduct;
            MatchedPeaksPercentage = matchedPeaksPercentage;
            MatchedPeaksCount = matchedPeaksCount;
            IsSpectrumMatch = isSpectrumMatch;
        }

        public virtual void Assign(MsScanMatchResult result) {
            result.SquaredWeightedDotProduct = (float)SquaredWeightedDotProduct;
            result.SquaredSimpleDotProduct = (float)SquaredSimpleDotProduct;
            result.SquaredReverseDotProduct = (float)SquaredReverseDotProduct;
            result.MatchedPeaksPercentage = (float)MatchedPeaksPercentage;
            result.MatchedPeaksCount = MatchedPeaksCount;
            result.IsSpectrumMatch = IsSpectrumMatch;
        }

        public static Ms2MatchResult Empty => empty ?? (empty = new Ms2MatchResult(0, 0, 0, 0, 0, false));
        private static Ms2MatchResult empty;
    }
}
