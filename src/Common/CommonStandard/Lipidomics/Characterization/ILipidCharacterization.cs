using CompMs.Common.Interfaces;

namespace CompMs.Common.Lipidomics.Characterization
{
    internal interface ILipidCharacterization
    {
        LipidMolecule Apply(LipidMolecule lipid, IMSScanProperty scan);
    }

    internal sealed class LipidCharacterization<TLipidCandidate> : ILipidCharacterization where TLipidCandidate : ILipidCandidate
    {
        private readonly ILipidType<TLipidCandidate> _type;
        private readonly ILipidPreCondition _preCondition;
        private readonly ILipidSearchSpace _searcher;

        public LipidCharacterization(ILipidType<TLipidCandidate> type, ILipidPreCondition preCondition, ILipidSearchSpace searcher) {
            _type = type;
            _preCondition = preCondition;
            _searcher = searcher;
        }

        public LipidMolecule Apply(LipidMolecule lipid, IMSScanProperty scan) {
            var search = _searcher;
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
