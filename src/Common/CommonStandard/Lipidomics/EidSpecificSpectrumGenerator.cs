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
        public static SpectrumPeak[] EidSpecificSpectrumGen(ILipid lipid, IChain chain, AdductIon adduct, double nlMass, double intensity)
        {
            var peaks = new List<SpectrumPeak>();
            if (chain.GetType() == typeof(AcylChain))
            {
                nlMass = nlMass - MassDiffDictionary.OxygenMass + MassDiffDictionary.HydrogenMass * 2;
            }
            var chainLoss = lipid.Mass - chain.Mass - nlMass;
            var diffs = new double[chain.CarbonCount];
            var bondPositions = new List<int>();
            for (int i = 0; i < chain.CarbonCount; i++) // numbering from COOH. 18:2(9,12) -> 9 is 8 and 12 is 11 
            {
                diffs[i] = CH2;
            }
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

            if (chain.DoubleBond.Count < 3)
            {
                foreach (var dbPosition in bondPositions)
                {
                    if (dbPosition == 1) continue;
                    double[] intArray = { 0.5, 0.5, 0.5, 0.7, 1, 0.7, 0.5 };

                    for (int i = (dbPosition - 1) - 2; i < Math.Min(diffs.Length, (dbPosition - 1) + 5); i++)
                    {
                        if (i <= 0) continue;
                        var n = i - ((dbPosition - 1) - 2);
                        var diffMass = i == (dbPosition - 1) ? diffs[i] : i >= (dbPosition - 1) ? diffs[i] + MassDiffDictionary.HydrogenMass : diffs[i] - MassDiffDictionary.HydrogenMass;
                        peaks.Add(new SpectrumPeak(adduct.ConvertToMz(chainLoss + diffMass), intensity * intArray[n], $"{chain} db{dbPosition} EID specific(c{i + 1})") { SpectrumComment = SpectrumComment.doublebond });
                    }
                }
            }
            else if (chain.DoubleBond.Count >= 3 && bondPositions.Contains(bondPositions.Max() - 3) && bondPositions.Contains(bondPositions.Max() - 6))
            {
                var dbPosition = bondPositions.Max() - 6;
                if (bondPositions.Count == 4)
                {
                    var spectrum = new List<SpectrumPeak>{
                        new SpectrumPeak(adduct.ConvertToMz(chainLoss + diffs[dbPosition -1 - 2] + MassDiffDictionary.HydrogenMass), intensity * 0.5, $"{chain} EID specific") { SpectrumComment = SpectrumComment.doublebond } ,
                        new SpectrumPeak(adduct.ConvertToMz(chainLoss + diffs[dbPosition -1 - 1]) , intensity * 0.5, $"{chain} EID specific") { SpectrumComment = SpectrumComment.doublebond } ,
                        new SpectrumPeak(adduct.ConvertToMz(chainLoss + diffs[dbPosition -1]), intensity * 0.75, $"{chain} EID specific") { SpectrumComment = SpectrumComment.doublebond } ,
                        new SpectrumPeak(adduct.ConvertToMz(chainLoss + diffs[dbPosition -1 + 1]) + MassDiffDictionary.HydrogenMass, intensity * 1, $"{chain} EID specific") { SpectrumComment = SpectrumComment.doublebond } ,
                        new SpectrumPeak(adduct.ConvertToMz(chainLoss + diffs[dbPosition -1 + 2]) + MassDiffDictionary.HydrogenMass, intensity * 0.5, $"{chain} EID specific") { SpectrumComment = SpectrumComment.doublebond } ,
                    };
                    peaks.AddRange(spectrum);
                }
                else
                {
                    var spectrum = new List<SpectrumPeak>{
                        new SpectrumPeak(adduct.ConvertToMz(chainLoss + diffs[dbPosition -1]), intensity * 0.25, $"{chain} EID specific") { SpectrumComment = SpectrumComment.doublebond } ,
                        new SpectrumPeak(adduct.ConvertToMz(chainLoss + diffs[dbPosition -1 + 1] + MassDiffDictionary.HydrogenMass), intensity * 1, $"{chain} EID specific") { SpectrumComment = SpectrumComment.doublebond } ,
                    };
                    peaks.AddRange(spectrum);
                }
            }
            if (bondPositions.Contains(5) && bondPositions.Contains(8) && bondPositions.Contains(11))
            {
                peaks.Add(
                    new SpectrumPeak(adduct.ConvertToMz(chainLoss + diffs[2] - MassDiffDictionary.HydrogenMass), intensity * 0.5, $"{chain} C3 specific") { SpectrumComment = SpectrumComment.doublebond }
                    );
            }
            return peaks.ToArray();
        }
    }
}
