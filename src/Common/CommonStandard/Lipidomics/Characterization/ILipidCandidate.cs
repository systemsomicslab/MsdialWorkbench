using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using System;

namespace CompMs.Common.Lipidomics.Characterization
{
    internal interface ILipidCandidate
    {
        LipidMolecule ToMolecule(LipidScore score);
        int GetCarbon(int snPosition);
        int GetDoubleBond(int snPosition);
        int GetOxidized(int snPosition);
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

        public int GetCarbon(int snPosition) {
            if (snPosition == 1) {
                return Sn1Carbon;
            }
            if (snPosition == 2) {
                return Sn2Carbon;
            }
            return 0;
        }

        public int GetDoubleBond(int snPosition) {
            if (snPosition == 1) {
                return Sn1DoubleBond;
            }
            if (snPosition == 2) {
                return Sn2DoubleBond;
            }
            return 0;
        }

        public int GetOxidized(int snPosition) {
            return 0;
        }

        public LipidMolecule ToMolecule(LipidScore score) {
            if (score.Count >= 2) {
                return LipidMsmsCharacterizationUtility.getPhospholipidMoleculeObjAsLevel2("PC", LbmClass.PC, Sn1Carbon, Sn1DoubleBond, Sn2Carbon, Sn2DoubleBond, score.Score);
            }
            return null;
        }
    }

    internal sealed class SHexCerCandidate : ILipidCandidate
    {
        public SHexCerCandidate(LipidMolecule lipid, int sn1Carbon, int sn1DoubleBond) {
            Lipid = lipid;
            Sn1Carbon = sn1Carbon;
            Sn1DoubleBond = sn1DoubleBond;
            Sn1Oxidized = 2;
            Sn2Carbon = lipid.TotalCarbonCount - sn1Carbon;
            Sn2DoubleBond = lipid.TotalDoubleBondCount - sn1DoubleBond;
            Sn2Oxidized = lipid.TotalOxidizedCount - Sn1Oxidized;
        }

        public LipidMolecule Lipid { get; }
        public int Sn1Carbon { get; }
        public int Sn1DoubleBond { get; }
        public int Sn1Oxidized { get; }
        public int Sn2Carbon { get; }
        public int Sn2DoubleBond { get; }
        public int Sn2Oxidized { get; }

        public int GetCarbon(int snPosition) {
            if (snPosition == 1) {
                return Sn1Carbon;
            }
            if (snPosition == 2) {
                return Sn2Carbon;
            }
            return 0;
        }

        public int GetDoubleBond(int snPosition) {
            if (snPosition == 1) {
                return Sn1DoubleBond;
            }
            if (snPosition == 2) {
                return Sn2DoubleBond;
            }
            return 0;
        }

        public int GetOxidized(int snPosition) {
            if (snPosition == 1) {
                return Sn1Oxidized;
            }
            if (snPosition == 2) {
                return Sn2Oxidized;
            }
            return 0;
        }

        public LipidMolecule ToMolecule(LipidScore score) {
            if (score.Count < 1) {
                return null;
            }
            if (Sn2Oxidized == 0) {
                return LipidMsmsCharacterizationUtility.getCeramideMoleculeObjAsLevel2("SHexCer", LbmClass.SHexCer, "d", Sn1Carbon, Sn1DoubleBond, Sn2Carbon, Sn2DoubleBond, score.Score);
            }
            else {
                return LipidMsmsCharacterizationUtility.getCeramideoxMoleculeObjAsLevel2("SHexCer", LbmClass.SHexCer, "d", Sn1Carbon, Sn1DoubleBond, Sn2Carbon, Sn2DoubleBond, Sn2Oxidized, score.Score);
            }
        }
    }

    internal static class LipidCandidateFactories {
        static LipidCandidateFactories() {
            FactoryCache<PCCandidate>.Factory = (m, _) => new PCCandidate(m, m.Sn1CarbonCount, m.Sn1DoubleBondCount);
            FactoryCache<SHexCerCandidate>.Factory = (m, _) => new SHexCerCandidate(m, m.Sn1CarbonCount, m.Sn1DoubleBondCount);
        }

        public static Func<LipidMolecule, IMSScanProperty, T> GetFactory<T>() where T: ILipidCandidate {
            return FactoryCache<T>.Factory;
        }

        private static class FactoryCache<T> where T: ILipidCandidate {
            public static Func<LipidMolecule, IMSScanProperty, T> Factory;
        }
    }
}
