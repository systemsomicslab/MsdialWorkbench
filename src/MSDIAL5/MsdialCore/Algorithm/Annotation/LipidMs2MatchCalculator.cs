using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public sealed class LipidMs2MatchCalculator : IMatchScoreCalculator<IMSScanMatchQuery, MoleculeMsReference, LipidMs2MatchResult>
    {
        public LipidMs2MatchResult Calculate(IMSScanMatchQuery query, MoleculeMsReference reference) {

            var scan = query.Scan;
            var weightedDotProduct = MsScanMatching.GetWeightedDotProduct(scan, reference, query.Ms2Tolerance, query.Ms2RangeBegin, query.Ms2RangeEnd);
            var simpleDotProduct = MsScanMatching.GetSimpleDotProduct(scan, reference, query.Ms2Tolerance, query.Ms2RangeBegin, query.Ms2RangeEnd);
            var reverseDotProduct = MsScanMatching.GetReverseDotProduct(scan, reference, query.Ms2Tolerance, query.Ms2RangeBegin, query.Ms2RangeEnd);
            var matchedPeaksScores = MsScanMatching.GetLipidomicsMatchedPeaksScores(scan, reference, query.Ms2Tolerance, query.Ms2RangeBegin, query.Ms2RangeEnd);

            if (weightedDotProduct == -1
                || simpleDotProduct == -1
                || reverseDotProduct == -1
                || matchedPeaksScores[0] == -1
                || matchedPeaksScores[1] == -1) {
                return LipidMs2MatchResult.Empty;
            }

            var name = MsScanMatching.GetRefinedLipidAnnotationLevel(
                scan, reference, query.Ms2Tolerance,
                out var isLipidClassMatch, out var isLipidChainsMatch, out var isLipidPositionMatch, out var isOtherLipidMatch);

            var isSpectrumMatch =
                weightedDotProduct >= query.WeightedDotProductCutOff
                && simpleDotProduct >= query.SimpleDotProductCutOff
                && reverseDotProduct >= query.ReverseDotProductCutOff
                && matchedPeaksScores[0] >= query.MatchedPeaksPercentageCutOff
                && matchedPeaksScores[1] >= query.MinimumSpectrumMatch
                && (isLipidClassMatch || isLipidChainsMatch || isLipidPositionMatch || isOtherLipidMatch);
            return new LipidMs2MatchResult(
                name,
                weightedDotProduct, simpleDotProduct, reverseDotProduct,
                matchedPeaksScores[0], (int)matchedPeaksScores[1],
                isSpectrumMatch, isLipidClassMatch, isLipidChainsMatch, isLipidPositionMatch, isOtherLipidMatch);
        }
    }

    public interface ILipidMatchResult : IMatchResult
    {
        string Name { get; }
        bool IsLipidClassMatch { get; }
        bool IsLipidChainsMatch { get; }
        bool IsLipidPositionMatch { get; }
        bool IsOtherLipidMatch { get; }
    }

    public sealed class LipidMs2MatchResult : Ms2MatchResult, ILipidMatchResult
    {
        public LipidMs2MatchResult(
            string name,
            double weightedDotProduct, double simpleDotProduct, double reverseDotProduct,
            double matchedPeaksPercentage, int matchedPeaksCount,
            bool isSpectrumMatch, bool isLipidClassMatch, bool isLipidChainsMatch, bool isLipidPositionMatch, bool isOtherLipidMatch)
            : base(weightedDotProduct, simpleDotProduct, reverseDotProduct, matchedPeaksPercentage, matchedPeaksCount, isSpectrumMatch) {
            IsLipidClassMatch = isLipidClassMatch;
            IsLipidChainsMatch = isLipidChainsMatch;
            IsLipidPositionMatch = isLipidPositionMatch;
            IsOtherLipidMatch = isOtherLipidMatch;
        }

        public string Name { get; }
        public bool IsLipidClassMatch { get; }
        public bool IsLipidChainsMatch { get; }
        public bool IsLipidPositionMatch { get; }
        public bool IsOtherLipidMatch { get; }

        public override void Assign(MsScanMatchResult result) {
            base.Assign(result);
            AssignLipidMatch(result);
        }

        private void AssignLipidMatch(MsScanMatchResult result) {
            result.IsLipidClassMatch = IsLipidClassMatch;
            result.IsLipidChainsMatch = IsLipidChainsMatch;
            result.IsLipidPositionMatch = IsLipidPositionMatch;
            result.IsOtherLipidMatch = IsOtherLipidMatch;
        }

        public static LipidMs2MatchResult Empty =>
            empty ?? (empty = new LipidMs2MatchResult(string.Empty, 0, 0, 0, 0, 0, false, false, false, false, false));
        private static LipidMs2MatchResult empty;
    }
}
