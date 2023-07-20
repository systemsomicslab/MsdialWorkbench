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

            if (molecule.Chains.ChainCount > 1) {
                var deepChains = (SeparatedChains)molecule.Chains;
                if (deepChains.GetChains()[0].CarbonCount == deepChains.GetChains()[1].CarbonCount &&
                    deepChains.GetChains()[1].CarbonCount == deepChains.GetChains()[2].CarbonCount &&
                    deepChains.GetChains()[0].DoubleBond == deepChains.GetChains()[1].DoubleBond &&
                    deepChains.GetChains()[1].DoubleBond == deepChains.GetChains()[2].DoubleBond) {
                    chain_cutoff = 1;
                } 
                else if (deepChains.GetChains()[0].CarbonCount == deepChains.GetChains()[1].CarbonCount &&
                    deepChains.GetChains()[1].CarbonCount != deepChains.GetChains()[2].CarbonCount &&
                    deepChains.GetChains()[0].DoubleBond == deepChains.GetChains()[1].DoubleBond &&
                    deepChains.GetChains()[1].DoubleBond != deepChains.GetChains()[2].DoubleBond) {
                    chain_cutoff = 2;
                }
                else if (deepChains.GetChains()[0].CarbonCount != deepChains.GetChains()[1].CarbonCount &&
                    deepChains.GetChains()[1].CarbonCount == deepChains.GetChains()[2].CarbonCount &&
                    deepChains.GetChains()[0].DoubleBond != deepChains.GetChains()[1].DoubleBond &&
                    deepChains.GetChains()[1].DoubleBond == deepChains.GetChains()[2].DoubleBond) {
                    chain_cutoff = 2;
                }
                else if (deepChains.GetChains()[0].CarbonCount == deepChains.GetChains()[2].CarbonCount &&
                    deepChains.GetChains()[1].CarbonCount != deepChains.GetChains()[2].CarbonCount &&
                    deepChains.GetChains()[0].DoubleBond == deepChains.GetChains()[2].DoubleBond &&
                    deepChains.GetChains()[1].DoubleBond != deepChains.GetChains()[2].DoubleBond) {
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
    }
}
