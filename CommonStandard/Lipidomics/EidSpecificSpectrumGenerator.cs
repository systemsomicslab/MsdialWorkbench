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
        public static SpectrumPeak[] EidSpecificSpectrumGen(ILipid lipid, IChain chain, AdductIon adduct, double nlMass, int dbPosition, double intensity)
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
            var defaultMass = acylChainLoss - MassDiffDictionary.HydrogenMass + adductMass;
            var biginMass = acylChainLoss + diffs[dbPosition - 2] - MassDiffDictionary.HydrogenMass + adductMass;
            var diffMass = 0.0;

            double[] intArray = { 0.5, 0.5, 0.7, 1, 0.7, 0.5 };
            for (int i = dbPosition - 2; i < Math.Min(diffs.Length, dbPosition + 3); i++)
            {
                if (i <= 0 || i + 2 >= diffs.Length) continue;
                var n = i - (dbPosition - 2);
                //diffMass = defaultMass + diffs[dbPosition - 2] + (diffs[i + 2] - diffs[dbPosition]); //?
                diffMass = i < dbPosition ? defaultMass + diffs[i] - MassDiffDictionary.HydrogenMass : i > dbPosition ? defaultMass + diffs[i] + MassDiffDictionary.HydrogenMass : defaultMass + diffs[i];
                peaks.Add(new SpectrumPeak(diffMass, intensity * intArray[n], $"{chain} db{dbPosition} EID db{i - dbPosition}") { SpectrumComment = SpectrumComment.acylchain });
                //peaks.Add(new SpectrumPeak(biginMass + CH2 * n, intensity * intArray[n], $"{chain} db{dbPosition} EID db{i - dbPosition}") { SpectrumComment = SpectrumComment.acylchain });
            }

            return peaks.ToArray();
        }
    }
}
