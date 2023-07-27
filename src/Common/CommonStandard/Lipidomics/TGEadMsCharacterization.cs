using CompMs.Common.Components;
using CompMs.Common.Interfaces;

namespace CompMs.Common.Lipidomics
{
    public static class TGEadMsCharacterization
    {
        public static (ILipid, double[]) Characterize(
            IMSScanProperty scan, ILipid molecule, MoleculeMsReference reference,
            float tolerance, float mzBegin, float mzEnd)
        {
            var class_cutoff = 0;
            var chain_cutoff = 2;
            var position_cutoff = 1;
            var double_cutoff = 0.5;

            var chains = molecule.Chains.GetDeterminedChains();
            if (chains.Length == 3) {
                if (ChainsEqual(chains[0], chains[1]) && ChainsEqual(chains[1], chains[2])) {
                    chain_cutoff = 1;
                } 
                else if (ChainsEqual(chains[0], chains[1]) && !ChainsEqual(chains[1], chains[2])) {
                    chain_cutoff = 2;
                }
                else if (!ChainsEqual(chains[0], chains[1]) && ChainsEqual(chains[1], chains[2])) {
                    chain_cutoff = 2;
                }
                else if (ChainsEqual(chains[0], chains[2]) && !ChainsEqual(chains[1], chains[2])) {
                    chain_cutoff = 2;
                }
                else {
                    chain_cutoff = 3;
                }
            }
            if (reference.AdductType.AdductIonName == "[M+NH4]+") {
                position_cutoff = 0;
            }

            var defaultResult = EieioMsCharacterizationUtility.GetDefaultScore(
                    scan, reference, tolerance, mzBegin, mzEnd, class_cutoff, chain_cutoff, position_cutoff, double_cutoff);
            return StandardMsCharacterizationUtility.GetDefaultCharacterizationResultForTriacylGlycerols(molecule, defaultResult);
        }

        private static bool ChainsEqual(IChain a, IChain b) {
            return a.CarbonCount == b.CarbonCount && a.DoubleBond == b.DoubleBond;
        }
    }
}
