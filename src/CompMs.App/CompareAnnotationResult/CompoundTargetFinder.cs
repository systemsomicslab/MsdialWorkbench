using CompMs.Common.Components;
using CompMs.MsdialCore.DataObj;

namespace CompMs.App.CompareAnnotationResult
{
    internal sealed class CompoundTargetFinder
    {
        private readonly MatchedSpotCandidateCalculator _calculator;
        private readonly List<MoleculeMsReference> _references;

        public CompoundTargetFinder(CommandLineData data, MatchedSpotCandidateCalculator candidateCalculator) {
            _calculator = candidateCalculator;
            _references = data.GetLibrary();
        }

        public List<MatchedSpotCandidate<AlignmentSpotProperty>> Find(IEnumerable<AlignmentSpotProperty> spots) {
            var candidates = new List<MatchedSpotCandidate<AlignmentSpotProperty>>();
            foreach (var spot in spots) {
                foreach (var reference in _references) {
                    var candidate = _calculator.Match(spot, reference);
                    if (candidate != null) {
                        candidates.Add(candidate);
                    }
                }
            }
            return candidates;
        }
    }
}
