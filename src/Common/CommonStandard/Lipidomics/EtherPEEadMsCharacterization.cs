using CompMs.Common.Components;
using CompMs.Common.Interfaces;

namespace CompMs.Common.Lipidomics
{
    public static class EtherPEEadMsCharacterization {
        public static (ILipid, double[]) Characterize(
            IMSScanProperty scan, ILipid molecule, MoleculeMsReference reference,
            float tolerance, float mzBegin, float mzEnd) {
            var class_cutoff = 2;
            var chain_cutoff = 2;
            var position_cutoff = 1;
            var double_cutoff = 0.5;
            if (reference.AdductType.AdductIonName == "[M+H]+") {
                position_cutoff = 0;
            }
            var defaultResult = EieioMsCharacterizationUtility.GetDefaultScore(
                    scan, reference, tolerance, mzBegin, mzEnd, class_cutoff, chain_cutoff, position_cutoff, double_cutoff);
            return StandardMsCharacterizationUtility.GetDefaultCharacterizationResultForAlkylAcylGlycerols(molecule, defaultResult);
        } 
    }
}
