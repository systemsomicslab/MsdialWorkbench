using CompMs.Common.Enum;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics.Characterization
{
    internal interface ILipidType
    {
        LipidMolecule Convert(LipidMolecule molecule, IEnumerable<LipidMolecule> candidates);
    }

    internal interface ILipidType<TLipidCandidate> : ILipidType where TLipidCandidate : ILipidCandidate
    {
        TLipidCandidate Create(LipidMolecule lipid, (int carbon, int doubleBond, int oxidized)[] chains);
    }

    internal sealed class PCLipidType : ILipidType<PCCandidate>
    {
        public LipidMolecule Convert(LipidMolecule molecule, IEnumerable<LipidMolecule> candidates) {
            List<LipidMolecule> candidates_ = candidates.ToList();
            if (candidates_.Count == 0) {
                return null;
            }
            return LipidMsmsCharacterizationUtility.returnAnnotationResult("PC", LbmClass.PC, "", molecule.Mz, molecule.Adduct, molecule.TotalCarbonCount, molecule.TotalDoubleBondCount, 0, candidates_, 2);
        }

        public PCCandidate Create(LipidMolecule lipid, (int carbon, int doubleBond, int oxidized)[] chains) {
            if (chains.Length == 0) {
                return new PCCandidate(lipid, lipid.Sn1CarbonCount, lipid.Sn1DoubleBondCount);
            }
            return new PCCandidate(lipid, chains[0].carbon, chains[0].doubleBond);
        }
    }

    internal sealed class SHexCerLipidType : ILipidType<SHexCerCandidate>
    {
        public LipidMolecule Convert(LipidMolecule molecule, IEnumerable<LipidMolecule> candidates) {
            List<LipidMolecule> candidates_ = candidates.ToList();
            return LipidMsmsCharacterizationUtility.returnAnnotationResult("SHexCer", LbmClass.SHexCer, "d", molecule.Mz, molecule.Adduct, molecule.TotalCarbonCount, molecule.TotalDoubleBondCount, molecule.Sn2Oxidizedount, candidates_, 2);
        }

        public SHexCerCandidate Create(LipidMolecule lipid, (int carbon, int doubleBond, int oxidized)[] chains) {
            if (chains.Length == 0) {
                return new SHexCerCandidate(lipid, lipid.Sn1CarbonCount, lipid.Sn1DoubleBondCount);
            }
            return new SHexCerCandidate(lipid, chains[0].carbon, chains[0].doubleBond);
        }
    }
}
