using CompMs.Common.Components;
using CompMs.Common.Interfaces;

namespace CompMs.Common.Lipidomics
{
    public static class Hex2CerEadMsCharacterization
    {
        public static (ILipid, double[]) Characterize(
            IMSScanProperty scan, ILipid molecule, MoleculeMsReference reference,
            float tolerance, float mzBegin, float mzEnd)
        {

            var defaultResult = EieioMsCharacterizationUtility.GetDefaultScore(
                    scan, reference, tolerance, mzBegin, mzEnd, 2, 2, 1, 0.5);
            return StandardMsCharacterizationUtility.GetDefaultCharacterizationResultForCeramides(molecule, defaultResult);
        }
    }
}
