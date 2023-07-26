using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.Lipidomics
{
    public static class PSEadMsCharacterization
    {
        public static (ILipid, double[]) Characterize(
            IMSScanProperty scan, ILipid molecule, MoleculeMsReference reference,
            float tolerance, float mzBegin, float mzEnd)
        {
            var class_cutoff = 3;
            var chain_cutoff = 2;
            var position_cutoff = 1;
            var double_cutoff = 0.5;

            if (molecule.Chains.ChainCount > 1) {
                var deepChains = (SeparatedChains)molecule.Chains;
                if (molecule.Chains.GetAllChains()[0].CarbonCount == molecule.Chains.GetAllChains()[1].CarbonCount &&
                    molecule.Chains.GetAllChains()[0].DoubleBond == molecule.Chains.GetAllChains()[1].DoubleBond) {
                    chain_cutoff = 1;
                }
            }
            if (reference.AdductType.AdductIonName == "[M+H]+") {
                position_cutoff = 0;
            }

            var defaultResult = EieioMsCharacterizationUtility.GetDefaultScore(
                    scan, reference, tolerance, mzBegin, mzEnd, class_cutoff, chain_cutoff, position_cutoff, double_cutoff);
            return StandardMsCharacterizationUtility.GetDefaultCharacterizationResultForGlycerophospholipid(molecule, defaultResult);
        }
    }
}
