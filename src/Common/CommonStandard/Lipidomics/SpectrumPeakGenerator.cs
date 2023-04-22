using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.FormulaGenerator.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

namespace CompMs.Common.Lipidomics
{
    public class SpectrumPeakGenerator : ISpectrumPeakGenerator
    {
        private static readonly double CH2 = new[]
       {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.CarbonMass,
        }.Sum();

        private IEnumerable<SpectrumPeak> GetDoubleBondSpectrum(ILipid lipid, IChain chain, AdductIon adduct, double nlMass, double abundance)
        {
            if (chain.DoubleBond.UnDecidedCount != 0 || chain.CarbonCount == 0 || chain.Oxidized.UnDecidedCount != 0)
            {
                return Enumerable.Empty<SpectrumPeak>();
            }
            var chainLoss = lipid.Mass - chain.Mass - nlMass;
            var diffs = new double[chain.CarbonCount];
            for (int i = 0; i < chain.CarbonCount; i++) // numbering from COOH. 18:2(9,12) -> 9 is 8 and 12 is 11 
            {
                diffs[i] = CH2;
            }

            if (chain.Oxidized != null)
            {
                foreach (var ox in chain.Oxidized.Oxidises)
                {
                        diffs[ox - 1] = diffs[ox - 1] + MassDiffDictionary.OxygenMass;
                }
            }

            var bondPositions = new List<int>();
            foreach (var bond in chain.DoubleBond.Bonds) // double bond 18:2(9,12) -> 9 is 9 and 12 is 12 
            {
                //if (bond.Position > diffs.Length - 1) return Enumerable.Empty<SpectrumPeak>();
                diffs[bond.Position - 1] -= MassDiffDictionary.HydrogenMass;
                diffs[bond.Position] -= MassDiffDictionary.HydrogenMass;
                bondPositions.Add(bond.Position);

                //Console.WriteLine(bond.Position);
            }
            for (int i = 1; i < chain.CarbonCount; i++)
            {
                diffs[i] += diffs[i - 1];
            }

            var peaks = new List<SpectrumPeak>();
            for (int i = 0; i < chain.CarbonCount - 1; i++)
            {
                var factor = 1.0;
                var factorHLoss = 0.5;
                var factorHGain = 0.2;
                var speccomment_hloss = SpectrumComment.doublebond;
                var speccomment_radical = SpectrumComment.doublebond;
                var speccomment_hgain = SpectrumComment.doublebond;

                if (bondPositions.Contains(i - 1))
                { // in the case of 18:2(9,12), Radical is big, and H loss is next
                    factor = 4.0;
                    factorHLoss = 2.0;
                    speccomment_radical |= SpectrumComment.doublebond_high;

                }
                else if (bondPositions.Contains(i))
                {
                    // now no modification
                }
                else if (bondPositions.Contains(i + 1))
                {
                    factor = 0.5;
                    factorHLoss = 0.25;
                    factorHGain = 0.05;
                    speccomment_radical |= SpectrumComment.doublebond_low;
                }
                else if (bondPositions.Contains(i + 2))
                {
                    // now no modification
                }
                else if (bondPositions.Contains(i + 3))
                {
                    if (bondPositions.Contains(i))
                    {
                        factor = 4.0;
                        factorHLoss = 0.5;
                        factorHGain = 2.0;
                        speccomment_radical |= SpectrumComment.doublebond_high;
                    }
                    else
                    {
                        factorHLoss = 4.0;
                        speccomment_hloss |= SpectrumComment.doublebond_high;

                    }
                }

                if (i == 2)
                {
                    factor = 0.75;
                    factorHLoss = 2.0;
                    factorHGain = 0.5;
                }
                if (i == 1)
                {
                    factor = 1.5;
                    factorHLoss = 0.5;
                    factorHGain = 2.0;
                }
                
                if (factorHGain == 4.0) {
                    speccomment_hgain |= SpectrumComment.doublebond_high;
                }

                peaks.Add(new SpectrumPeak(adduct.ConvertToMz(chainLoss + diffs[i] - MassDiffDictionary.HydrogenMass), factorHLoss * abundance, $"{chain} C{i + 1}-H") { SpectrumComment = speccomment_hloss });
                peaks.Add(new SpectrumPeak(adduct.ConvertToMz(chainLoss + diffs[i]), factor * abundance, $"{chain} C{i + 1}") { SpectrumComment = speccomment_radical });
                peaks.Add(new SpectrumPeak(adduct.ConvertToMz(chainLoss + diffs[i] + MassDiffDictionary.HydrogenMass), factorHGain * abundance, $"{chain} C{i + 1}+H") { SpectrumComment = speccomment_hgain });
            }

            return peaks;
        }

        public IEnumerable<SpectrumPeak> GetAcylDoubleBondSpectrum(ILipid lipid, AcylChain acylChain, AdductIon adduct, double nlMass, double abundance)
            => GetDoubleBondSpectrum(lipid, acylChain, adduct, nlMass - MassDiffDictionary.OxygenMass + MassDiffDictionary.HydrogenMass * 2, abundance);

        public IEnumerable<SpectrumPeak> GetAlkylDoubleBondSpectrum(ILipid lipid, AlkylChain acylChain, AdductIon adduct, double nlMass, double abundance)
            => GetDoubleBondSpectrum(lipid, acylChain, adduct, nlMass, abundance);

        public IEnumerable<SpectrumPeak> GetSphingoDoubleBondSpectrum(ILipid lipid, SphingoChain sphingo, AdductIon adduct, double nlMass, double abundance)
        {
            if (sphingo.DoubleBond.UnDecidedCount != 0 || sphingo.CarbonCount == 0 || sphingo.Oxidized.UnDecidedCount != 0)
            {
                return Enumerable.Empty<SpectrumPeak>();
            }

            var chainLoss = lipid.Mass - sphingo.Mass - nlMass
                + MassDiffDictionary.NitrogenMass
                + 12 * 2
                + MassDiffDictionary.OxygenMass * (sphingo.OxidizedCount > 2 ? 2 : sphingo.OxidizedCount)
                + MassDiffDictionary.HydrogenMass * 5;
            var diffs = new double[sphingo.CarbonCount];
            for (int i = 0; i < sphingo.CarbonCount; i++)
            {
                diffs[i] = CH2;
            }

            if (sphingo.Oxidized != null)
            {
                foreach (var ox in sphingo.Oxidized.Oxidises)
                {
                    if (ox > 3)
                    {
                        diffs[ox - 1] = diffs[ox - 1] + MassDiffDictionary.OxygenMass;
                    }
                }
            }

            foreach (var bond in sphingo.DoubleBond.Bonds)
            {
                diffs[bond.Position - 1] -= MassDiffDictionary.HydrogenMass;
                diffs[bond.Position] -= MassDiffDictionary.HydrogenMass;
            }
            for (int i = 3; i < sphingo.CarbonCount; i++)
            {
                diffs[i] += diffs[i - 1];
            }

            var peaks = new List<SpectrumPeak>();
            for (int i = 2; i < sphingo.CarbonCount - 1; i++)
            {
                peaks.Add(new SpectrumPeak(adduct.ConvertToMz(chainLoss + diffs[i] - MassDiffDictionary.HydrogenMass), abundance * 0.5, $"{sphingo} C{i + 1}-H") { SpectrumComment = SpectrumComment.doublebond });
                peaks.Add(new SpectrumPeak(adduct.ConvertToMz(chainLoss + diffs[i]), abundance, $"{sphingo} C{i + 1}") { SpectrumComment = SpectrumComment.doublebond });
                peaks.Add(new SpectrumPeak(adduct.ConvertToMz(chainLoss + diffs[i] + MassDiffDictionary.HydrogenMass), abundance * 0.5, $"{sphingo} C{i + 1}+H") { SpectrumComment = SpectrumComment.doublebond });
            }

            return peaks;
        }
    }
}
