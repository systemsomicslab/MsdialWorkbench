using CompMs.Common.Enum;

namespace CompMs.Common.Lipidomics.Characterization
{
    internal interface ILipidCandidate
    {
        LipidMolecule ToMolecule(LipidScore score);
    }

    internal sealed class PCCandidate : ILipidCandidate
    {
        public PCCandidate(LipidMolecule lipid, int sn1Carbon, int sn1DoubleBond) {
            Lipid = lipid;
            Sn1Carbon = sn1Carbon;
            Sn1DoubleBond = sn1DoubleBond;
            Sn2Carbon = lipid.TotalCarbonCount - sn1Carbon;
            Sn2DoubleBond = lipid.TotalDoubleBondCount - sn1DoubleBond;
        }

        public LipidMolecule Lipid { get; }
        public int Sn1Carbon { get; }
        public int Sn1DoubleBond { get; }
        public int Sn2Carbon { get; }
        public int Sn2DoubleBond { get; }

        public LipidMolecule ToMolecule(LipidScore score) {
            if (score.Count >= 2) {
                return LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PC", LbmClass.PC, Sn1Carbon, Sn1DoubleBond, Sn2Carbon, Sn2DoubleBond, score.Score);
            }
            return null;
        }
    }
}
