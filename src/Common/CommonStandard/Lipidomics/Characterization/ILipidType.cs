using CompMs.Common.Enum;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics.Characterization
{
    internal interface ILipidType
    {
        LipidMolecule Convert(LipidMolecule molecule, IEnumerable<LipidMolecule> candidates);
    }

    internal sealed class PCLipidType : ILipidType
    {
        public LipidMolecule Convert(LipidMolecule molecule, IEnumerable<LipidMolecule> candidates) {
            List<LipidMolecule> candidates_ = candidates.ToList();
            if (candidates_.Count == 0) {
                return null;
            }
            return LipidMsmsCharacterizationUtility.returnAnnotationResult("PC", LbmClass.PC, "", molecule.Mz, molecule.Adduct, molecule.TotalCarbonCount, molecule.TotalDoubleBondCount, 0, candidates_, 2);
        }
    }
}
