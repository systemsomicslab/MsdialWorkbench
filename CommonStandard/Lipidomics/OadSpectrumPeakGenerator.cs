using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.FormulaGenerator.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public class OadSpectrumPeakGenerator
    {
        private static readonly double CH2 = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.CarbonMass,
        }.Sum();

        private IEnumerable<SpectrumPeak> GetOadDoubleBondSpectrum(ILipid lipid, IChain chain, AdductIon adduct, double nlMass, double abundance)
        {
            if (chain.DoubleBond.UnDecidedCount != 0 || chain.CarbonCount == 0)
            {
                return Enumerable.Empty<SpectrumPeak>();
            }
            var chainLoss = lipid.Mass - chain.Mass - nlMass;
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

                //Console.WriteLine(bond.Position);
            }
            for (int i = 1; i < chain.CarbonCount; i++)
            {
                diffs[i] += diffs[i - 1];
            }

            var peaks = new List<SpectrumPeak>();
            foreach (var bond in bondPositions)
            {
                if (bond != 1)
                {
                    var speccomment = SpectrumComment.doublebond;
                    var factor = 1.0;
                    var dbPeakHigher = diffs[bond] + MassDiffDictionary.OxygenMass;
                    var dbPeakLower = diffs[bond] - MassDiffDictionary.CarbonMass - CH2;
                    peaks.Add(new SpectrumPeak((float)adduct.ConvertToMz(chainLoss + dbPeakHigher), (float)(factor * abundance), $"{chain} C{bond} DB fragment higher") { SpectrumComment = speccomment });
                    peaks.Add(new SpectrumPeak((float)adduct.ConvertToMz(chainLoss + dbPeakHigher + MassDiffDictionary.HydrogenMass), (float)(factor * abundance * 0.5), $"{chain} C{bond} DB fragment higher+H") { SpectrumComment = speccomment });
                    peaks.Add(new SpectrumPeak((float)adduct.ConvertToMz(chainLoss + dbPeakHigher + MassDiffDictionary.HydrogenMass * 2), (float)(factor * abundance * 0.15), $"{chain} C{bond} DB fragment higher+2H") { SpectrumComment = speccomment });
                    peaks.Add(new SpectrumPeak((float)adduct.ConvertToMz(chainLoss + dbPeakLower), (float)(factor * abundance), $"{chain} C{bond} DB fragment lower") { SpectrumComment = speccomment });
                    peaks.Add(new SpectrumPeak((float)adduct.ConvertToMz(chainLoss + dbPeakLower + MassDiffDictionary.HydrogenMass), (float)(factor * abundance * 0.5), $"{chain} C{bond} DB fragment lower+H") { SpectrumComment = speccomment });
                    peaks.Add(new SpectrumPeak((float)adduct.ConvertToMz(chainLoss + dbPeakLower + MassDiffDictionary.HydrogenMass * 2), (float)(factor * abundance * 0.15), $"{chain} C{bond} DB fragment lower+2H") { SpectrumComment = speccomment });
                }
                else
                {
                    var speccomment = SpectrumComment.doublebond;
                    var factor = 1.0;
                    var dbPeakHigher = diffs[bond] + MassDiffDictionary.OxygenMass;
                    peaks.Add(new SpectrumPeak((float)adduct.ConvertToMz(chainLoss + dbPeakHigher), (float)(factor * abundance), $"{chain} C{bond} DB fragment higher") { SpectrumComment = speccomment });
                    peaks.Add(new SpectrumPeak((float)adduct.ConvertToMz(chainLoss + dbPeakHigher + MassDiffDictionary.HydrogenMass), (float)(factor * abundance * 0.5), $"{chain} C{bond} DB fragment higher+H") { SpectrumComment = speccomment });
                }
            }

            return peaks;
        }

        public IEnumerable<SpectrumPeak> GetAcylDoubleBondSpectrum(ILipid lipid, AcylChain acylChain, AdductIon adduct, double nlMass, double abundance)
            => GetOadDoubleBondSpectrum(lipid, acylChain, adduct, nlMass - MassDiffDictionary.OxygenMass + MassDiffDictionary.HydrogenMass * 2, abundance);

        public IEnumerable<SpectrumPeak> GetAlkylDoubleBondSpectrum(ILipid lipid, AlkylChain acylChain, AdductIon adduct, double nlMass, double abundance)
            => GetOadDoubleBondSpectrum(lipid, acylChain, adduct, nlMass, abundance);

        //public IEnumerable<SpectrumPeak> GetSphingoDoubleBondSpectrum(ILipid lipid, SphingoChain sphingo, AdductIon adduct, double nlMass, double abundance)
        //{
        //}
    }
}
