using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.Common.FormulaGenerator.DataObj;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public static class LDGTSEadMsCharacterization
    {
        private static readonly double LdgtaFrag = new[] {
            MassDiffDictionary.CarbonMass * 7,
            MassDiffDictionary.HydrogenMass * 13,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 2,
        }.Sum();

        private static readonly double LdgtsFrag = new[] {
            MassDiffDictionary.CarbonMass * 6,
            MassDiffDictionary.HydrogenMass * 11,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 2,
        }.Sum();
        public static (ILipid, double[]) Characterize(
            IMSScanProperty scan, ILipid molecule, MoleculeMsReference reference,
            float tolerance, float mzBegin, float mzEnd)
        {
            var exp_spectrum = scan.Spectrum;
            var adduct = reference.AdductType.AdductIonAccurateMass;
            var class_cutoff = 1;

            if (CompairTwoFeagmentsIntensity.isFragment1GreaterThanFragment2(exp_spectrum, tolerance, LdgtaFrag + adduct, LdgtsFrag + adduct)) // reject LDGTA
            {
                class_cutoff = 10;
            }

            var defaultResult = EieioMsCharacterizationUtility.GetDefaultScore(
                    scan, reference, tolerance, mzBegin, mzEnd, class_cutoff, 2, 1, 0.5);
            return StandardMsCharacterizationUtility.GetDefaultCharacterizationResultForGlycerophospholipid(molecule, defaultResult);
        }
    }
}
