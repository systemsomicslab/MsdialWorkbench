using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;

namespace CompMs.Common.Lipidomics.Characterization
{
    internal interface ILipidSearchSpace
    {
        IEnumerable<LipidMolecule> RetrieveAll(LipidMolecule lipid, IMSScanProperty scan);
    }

    internal sealed class IdentitySearchSpace<TLipidCandidate> : ILipidSearchSpace where TLipidCandidate : ILipidCandidate
    {
        private readonly Func<LipidMolecule, IMSScanProperty, TLipidCandidate> _factory;
        private readonly ILipidCondition<TLipidCandidate> _condition;
        private readonly ILipidScoring<TLipidCandidate> _scorer;

        public IdentitySearchSpace(Func<LipidMolecule, IMSScanProperty, TLipidCandidate> factory, ILipidCondition<TLipidCandidate> condition, ILipidScoring<TLipidCandidate> scorer) {
            _factory = factory;
            _condition = condition;
            _scorer = scorer;
        }

        public IEnumerable<LipidMolecule> RetrieveAll(LipidMolecule lipid, IMSScanProperty scan) {
            var candidate = _factory.Invoke(lipid, scan);
            if (!_condition.Satisfy(candidate, scan)) {
                return null;
            }
            var score = _scorer.Score(candidate, scan);
            if (candidate.ToMolecule(score) is LipidMolecule m) {
                return new[] { m, };
            }
            return Array.Empty<LipidMolecule>();
        }
    }

    internal sealed class PCSearchSpace : ILipidSearchSpace
    {
        private readonly int _carbonMinimum;
        private readonly int _doubleBondMaximum;
        private readonly ILipidCondition<PCCandidate> _condition;
        private readonly ILipidScoring<PCCandidate> _scorer;

        public PCSearchSpace(int carbonMinimum, int doubleBondMaximum, ILipidCondition<PCCandidate> condition, ILipidScoring<PCCandidate> scorer) {
            _carbonMinimum = carbonMinimum;
            _doubleBondMaximum = doubleBondMaximum;
            _condition = condition;
            _scorer = scorer;
        }

        public IEnumerable<LipidMolecule> RetrieveAll(LipidMolecule lipid, IMSScanProperty scan) {
            for (var sn1Carbon = _carbonMinimum; sn1Carbon < lipid.TotalCarbonCount; sn1Carbon++) {
                if (lipid.TotalCarbonCount - sn1Carbon < _carbonMinimum) {
                    break;
                }
                for (var sn1DoubleBond = 0; sn1DoubleBond < lipid.TotalDoubleBondCount; sn1DoubleBond++) {
                    if (sn1DoubleBond > _doubleBondMaximum) {
                        break;
                    }
                    if (lipid.TotalDoubleBondCount - sn1DoubleBond > _doubleBondMaximum) {
                        continue;
                    }
                    var candidate = new PCCandidate(lipid, sn1Carbon, sn1DoubleBond);
                    if (!_condition.Satisfy(candidate, scan)) {
                        continue;
                    }
                    var score = _scorer.Score(candidate, scan);
                    if (candidate.ToMolecule(score) is LipidMolecule m) {
                        yield return m;
                    }
                }
            }
        }
    }
}
