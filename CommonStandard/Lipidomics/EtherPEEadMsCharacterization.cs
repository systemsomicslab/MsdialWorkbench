using CompMs.Common.Components;
using CompMs.Common.Interfaces;

namespace CompMs.Common.Lipidomics
{
    public static class EtherPEEadMsCharacterization {
        public static (ILipid, double[]) Characterize(
            IMSScanProperty scan, ILipid molecule, MoleculeMsReference reference,
            float tolerance, float mzBegin, float mzEnd) {

            var defaultResult = EadMsCharacterizationUtility.GetDefaultScoreForGlycerophospholipid(
                    scan, reference, tolerance, mzBegin, mzEnd, 2, 2, 1, 0.5);
            return EadMsCharacterizationUtility.GetDefaultCharacterizationResultForAlkylAcylGlycerols(molecule, defaultResult);
        } 
    }
}
