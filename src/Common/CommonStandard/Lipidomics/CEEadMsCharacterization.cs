using CompMs.Common.Components;
using CompMs.Common.Interfaces;

namespace CompMs.Common.Lipidomics
{
    internal class CEEadMsCharacterization
    {
        public static (ILipid, double[]) Characterize(
            IMSScanProperty scan, ILipid molecule, MoleculeMsReference reference,
            float tolerance, float mzBegin, float mzEnd)
        {

            var defaultResult = EieioMsCharacterizationUtility.GetDefaultScore(
                    scan, reference, tolerance, mzBegin, mzEnd, 1, 1, 0, 2); // doublebond position cannot be determined
            return StandardMsCharacterizationUtility.GetDefaultCharacterizationResultForSingleAcylChainLipid(molecule, defaultResult);
        }
    }
}
