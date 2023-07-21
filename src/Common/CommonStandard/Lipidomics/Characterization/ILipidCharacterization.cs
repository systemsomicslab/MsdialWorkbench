using CompMs.Common.Interfaces;
using System;

namespace CompMs.Common.Lipidomics.Characterization
{
    internal interface ILipidCharacterization
    {
        LipidMolecule Apply(LipidMolecule lipid, IMSScanProperty scan, double tolerance);
    }

    internal sealed class LipidCharacterization<TLipidCandidate> : ILipidCharacterization where TLipidCandidate : ILipidCandidate
    {
        private readonly ILipidType<TLipidCandidate> _type;
        private readonly ILipidPreCondition _preCondition;
        private readonly Func<double, ILipidSearchSpace> _searcher;

        public LipidCharacterization(ILipidType<TLipidCandidate> type, ILipidPreCondition preCondition, Func<double, ILipidSearchSpace> searcher) {
            _type = type;
            _preCondition = preCondition;
            _searcher = searcher;
        }

        public LipidMolecule Apply(LipidMolecule lipid, IMSScanProperty scan, double tolerance) {
            var search = _searcher(tolerance);
            if (!_preCondition.Satisfy(lipid)) {
                return null;
            }
            var candidates = search.RetrieveAll(lipid, scan);
            if (candidates is null) {
                return null;
            }
            return _type.Convert(lipid, candidates);
        }
    }
}
