using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.FormulaGenerator.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.Lipidomics
{
    public class EidSpecificSpectrumGenerator
    {
        private static readonly double CH2 = new[]
       {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.CarbonMass,
        }.Sum();
        public static SpectrumPeak[] EidSpecificSpectrum(ILipid lipid, IChain chain, AdductIon adduct, double nlMass, int dbPosition, double intensity)
        {
            var peaks = new List<SpectrumPeak>();
            var acylChainLoss = lipid.Mass - chain.Mass - (nlMass - MassDiffDictionary.OxygenMass + MassDiffDictionary.HydrogenMass * 2);
            var diffs = new double[chain.CarbonCount];
            for (int i = 0; i < chain.CarbonCount; i++) // numbering from COOH. 18:2(9,12) -> 9 is 8 and 12 is 11 
            {
                diffs[i] = CH2;
            }

            var bondPositions = new List<int>();
            foreach (var bond in chain.DoubleBond.Bonds) // double bond 18:2(9,12) -> 9 is 9 and 12 is 12 
            {
                diffs[bond.Position - 1] -= MassDiffDictionary.HydrogenMass;
                diffs[bond.Position] -= MassDiffDictionary.HydrogenMass;
                bondPositions.Add(bond.Position);
            }
            for (int i = 1; i < chain.CarbonCount; i++)
            {
                diffs[i] += diffs[i - 1];
            }
            var adductMass = adduct.AdductIonName == "[M+NH4]+" ? MassDiffDictionary.ProtonMass : adduct.AdductIonAccurateMass;
            var biginMass = acylChainLoss + diffs[dbPosition - 2] - MassDiffDictionary.HydrogenMass+ adductMass;
            peaks.Add(new SpectrumPeak(biginMass, intensity * 0.5, $"{chain} EID C{dbPosition - 1}") { SpectrumComment = SpectrumComment.acylchain });
            peaks.Add(new SpectrumPeak(biginMass + CH2, intensity * 0.5, $"{chain} EID C{dbPosition - 1}") { SpectrumComment = SpectrumComment.acylchain });
            peaks.Add(new SpectrumPeak(biginMass + CH2 * 2, intensity * 0.7, $"{chain} EID C{dbPosition}") { SpectrumComment = SpectrumComment.acylchain });
            peaks.Add(new SpectrumPeak(biginMass + CH2 * 3, intensity, $"{chain} EID C{dbPosition + 1}") { SpectrumComment = SpectrumComment.acylchain });
            peaks.Add(new SpectrumPeak(biginMass + CH2 * 4, intensity * 0.7, $"{chain} EID C{dbPosition + 2}") { SpectrumComment = SpectrumComment.acylchain });
            peaks.Add(new SpectrumPeak(biginMass + CH2 * 5, intensity * 0.5, $"{chain} EID C{dbPosition + 3}") { SpectrumComment = SpectrumComment.acylchain });

            return peaks.ToArray();
        }
    }
}
