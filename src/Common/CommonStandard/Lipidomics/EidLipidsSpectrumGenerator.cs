using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Accord.Math.Random;

namespace CompMs.Common.Lipidomics
{
    public class EidLipidSpectrumGenerator
    {
        public List<SpectrumPeak> GetClassEidFragmentSpectrum(Lipid lipid, AdductIon adduct)
        {
            var spectrum = new List<SpectrumPeak>();

            switch (lipid.LipidClass)
            {

                case LbmClass.TG:
                    var TGSpectrumGenerator = new TGSpectrumGenerator();
                    spectrum.AddRange(TGSpectrumGenerator.Generate(lipid, adduct).Spectrum);
                    break;
                case LbmClass.DG:
                    var DGSpectrumGenerator = new DGSpectrumGenerator();
                    spectrum.AddRange(DGSpectrumGenerator.Generate(lipid, adduct).Spectrum);
                    break;
                case LbmClass.MG:
                    var MGSpectrumGenerator = new MGSpectrumGenerator();
                    spectrum.AddRange(MGSpectrumGenerator.Generate(lipid, adduct).Spectrum);
                    break;
                case LbmClass.BMP:
                    var BMPSpectrumGenerator = new BMPSpectrumGenerator();
                    spectrum.AddRange(BMPSpectrumGenerator.Generate(lipid, adduct).Spectrum);
                    break;
                case LbmClass.HBMP:
                    var HBMPSpectrumGenerator = new HBMPSpectrumGenerator();
                    spectrum.AddRange(HBMPSpectrumGenerator.Generate(lipid, adduct).Spectrum);
                    break;
                case LbmClass.CL:
                    var CLSpectrumGenerator = new CLEidSpectrumGenerator(); // Eid 
                    spectrum.AddRange(CLSpectrumGenerator.Generate(lipid, adduct).Spectrum);
                    break;
                case LbmClass.Cer_NS:
                    var CeramideSpectrumGenerator = new CeramideSpectrumGenerator();
                    spectrum.AddRange(CeramideSpectrumGenerator.Generate(lipid, adduct).Spectrum);
                    break;
                case LbmClass.HexCer_NS:
                    var HexCerSpectrumGenerator = new HexCerSpectrumGenerator();
                    spectrum.AddRange(HexCerSpectrumGenerator.Generate(lipid, adduct).Spectrum);
                    break;
                case LbmClass.SM:
                    var SMSpectrumGenerator = new SMSpectrumGenerator();
                    spectrum.AddRange(SMSpectrumGenerator.Generate(lipid, adduct).Spectrum);

                    spectrum.AddRange
                    (
                        new[] {
                            new SpectrumPeak(C5H14NO, 50d, "C5H14NO") { SpectrumComment = SpectrumComment.metaboliteclass}
                        }
                    );
                    break;

                default:
                    spectrum.Add(new SpectrumPeak(adduct.ConvertToMz(lipid.Mass), 999d, "Precursor") { SpectrumComment = SpectrumComment.precursor });
                    break;
            }
            return spectrum;
        }

        private static List<int> doublebondPositions(IChain chain)
        {
            var bondPositions = new List<int>();
            var dbPosition = chain.DoubleBond.Bonds;
            bondPositions.AddRange(from bond in dbPosition
                                   select bond.Position);
            return bondPositions;
        }

        private static SpectrumPeak[] EidSpecificSpectrum(Lipid lipid, AdductIon adduct, double nlMass, double intensity)
        {
            var spectrum = new List<SpectrumPeak>();
            if (lipid.Chains is SeparatedChains chains)
            {
                foreach (var chain in chains.GetDeterminedChains())
                {
                    if (chain.DoubleBond.Count == 0 || chain.DoubleBond.UnDecidedCount > 0) continue;
                    var dbPosition = doublebondPositions(chain);
                    intensity = intensity / dbPosition.Count;
                    foreach (var db in dbPosition)
                    {
                        spectrum.AddRange(EidSpecificSpectrumGenerator.EidSpecificSpectrumGen(lipid, chain, adduct, nlMass, intensity));
                    }
                }
            }
            return spectrum.ToArray();
        }


        private static readonly double Electron = 0.00054858026;

        private static readonly double H2O = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();
        private static readonly double NH3 = new[]
        {
            MassDiffDictionary.HydrogenMass * 3,
            MassDiffDictionary.NitrogenMass,
        }.Sum();
        private static readonly double C5H14NO4P = new[] //PC SM Header
        {
            MassDiffDictionary.CarbonMass * 5,
            MassDiffDictionary.HydrogenMass * 14,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();
        private static readonly double C5H14NO = new[] //PC 104.107
        {
            MassDiffDictionary.CarbonMass * 5,
            MassDiffDictionary.HydrogenMass * 13,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass,
            MassDiffDictionary.ProtonMass
        }.Sum();

        private static readonly double C2H8NO4P = new[] //PE Header
        {
            MassDiffDictionary.CarbonMass * 2,
            MassDiffDictionary.HydrogenMass * 8,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 4,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();
        private static readonly double C3H9O6P = new[] // PG Header
        {
            MassDiffDictionary.CarbonMass * 3,
            MassDiffDictionary.HydrogenMass * 9,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();
        private static readonly double C3H8NO6P = new[] // PS Header
        {
            MassDiffDictionary.CarbonMass * 3,
            MassDiffDictionary.HydrogenMass * 8,
            MassDiffDictionary.NitrogenMass,
            MassDiffDictionary.OxygenMass * 6,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();
        private static readonly double C6H13O9P = new[] // PI Header
        {
            MassDiffDictionary.CarbonMass * 6,
            MassDiffDictionary.HydrogenMass * 13,
            MassDiffDictionary.OxygenMass * 9,
            MassDiffDictionary.PhosphorusMass,
        }.Sum();

    }
}
