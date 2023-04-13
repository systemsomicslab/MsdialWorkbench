using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace CompMs.Common.Lipidomics
{
    public class OadSpectrumPeakGenerator : IOadSpectrumPeakGenerator
    {
        private static readonly double CH2 = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.CarbonMass,
        }.Sum();

        private static readonly double H2O = new[]
        {
            MassDiffDictionary.HydrogenMass * 2,
            MassDiffDictionary.OxygenMass,
        }.Sum();

        private static readonly double Electron = 0.00054858026;


        private IEnumerable<SpectrumPeak> GetOadDoubleBondSpectrum(ILipid lipid, IChain chain, AdductIon adduct, double nlMass, double abundance, string[] oadId)
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
                    var addPeaks = DoubleBondSpectrum(bond, diffs, chain, adduct, chainLoss, abundance, oadId);
                    peaks.AddRange(addPeaks);
                }
                else
                {
                    var speccomment = SpectrumComment.doublebond;
                    var factor = 1.0;
                    var dbPeakHigher = chainLoss + diffs[bond] + MassDiffDictionary.HydrogenMass + MassDiffDictionary.OxygenMass;
                    peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakHigher + MassDiffDictionary.HydrogenMass), (factor * abundance * 0.2), $"{chain} C{bond} +C +O +H OAD01") { SpectrumComment = speccomment });
                    peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakHigher), (factor * abundance * 0.5), $"{chain} C{bond} +O OAD02") { SpectrumComment = speccomment });
                    //peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakHigher - MassDiffDictionary.HydrogenMass), (factor * abundance), $"{chain} C{bond} +C +O -H OAD03") { SpectrumComment = speccomment | SpectrumComment.doublebond_high });
                }
            }
            return peaks;
        }

        private List<SpectrumPeak> DoubleBondSpectrum(int bond, double[] diffs, IChain chain, AdductIon adduct, double chainLoss, double abundance, string[] oadId)
        {
            var OadPeaks = DoubleBondSpectrumWithId(bond, diffs, chain, adduct, chainLoss, abundance);
            return OadPeaks.Where(p => oadId.Contains(p.OadId)).Select(p => p.spectrum).ToList();
        }
        //private List<SpectrumPeak> DoubleBondSpectrum(int bond, double[] diffs, IChain chain, AdductIon adduct, double chainLoss, double abundance, ILipid lipid)
        //{
        //    var peaks = new List<SpectrumPeak>();
        //    var speccomment = SpectrumComment.doublebond;
        //    var factor = 1.0;
        //    var dbPeakHigher = chainLoss + diffs[bond] + MassDiffDictionary.HydrogenMass + MassDiffDictionary.OxygenMass;
        //    var dbPeak = chainLoss + diffs[bond - 1];
        //    var dbPeakLower = chainLoss + diffs[bond - 2] + MassDiffDictionary.HydrogenMass;
        //    if (adduct.IonMode == IonMode.Positive)
        //    {
        //        peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakHigher + MassDiffDictionary.HydrogenMass), (factor * abundance * 0.3), $"{chain} C{bond} +C +O +H OAD01") { SpectrumComment = speccomment });
        //        peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakHigher), (factor * abundance), $"{chain} C{bond} +O OAD02") { SpectrumComment = speccomment | SpectrumComment.doublebond_high });
        //        if (lipid.LipidClass != LbmClass.PE)
        //        {
        //            peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakHigher + MassDiffDictionary.OxygenMass), (factor * abundance * 0.3), $"{chain} C{bond} +O OAD02+O") { SpectrumComment = speccomment });
        //        }
        //        peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakHigher - MassDiffDictionary.HydrogenMass), (factor * abundance * 0.5), $"{chain} C{bond} +C +O -H OAD03") { SpectrumComment = speccomment });
        //        peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakHigher - MassDiffDictionary.HydrogenMass * 2), (factor * abundance * 0.2), $"{chain} C{bond} +C +O -2H OAD04") { SpectrumComment = speccomment });

        //        //peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakHigher - H2O + MassDiffDictionary.HydrogenMass), (factor * abundance * 0.15), $"{chain} C{bond} +C +O +H -H2O OAD05") { SpectrumComment = speccomment });
        //        //peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakHigher - H2O), (factor * abundance * 0.3), $"{chain} C{bond} +C +O -H2O OAD06") { SpectrumComment = speccomment });
        //        //peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakHigher - H2O - MassDiffDictionary.HydrogenMass), (factor * abundance * 0.1), $"{chain} C{bond} +C +O -H -H2O OAD07") { SpectrumComment = speccomment });

        //        if (lipid.LipidClass != LbmClass.PE)
        //        {
        //            peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeak + MassDiffDictionary.OxygenMass), (factor * abundance * 0.2), $"{chain} C{bond} +O OAD08") { SpectrumComment = speccomment });
        //        }
        //        //peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeak + MassDiffDictionary.HydrogenMass * 2), (factor * abundance * 0.2), $"{chain} C{bond} OAD09") { SpectrumComment = speccomment });
        //        //peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeak + MassDiffDictionary.HydrogenMass), (factor * abundance * 0.3), $"{chain} C{bond} -H OAD10") { SpectrumComment = speccomment });
        //        //peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeak), (factor * abundance * 0.1), $"{chain} C{bond} -2H OAD11") { SpectrumComment = speccomment });

        //        if (lipid.LipidClass != LbmClass.PE)
        //        {
        //            peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakLower + MassDiffDictionary.OxygenMass - MassDiffDictionary.HydrogenMass), (factor * abundance * 0.3), $"{chain} C{bond} +O -H OAD12") { SpectrumComment = speccomment });
        //        }
        //        //peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakLower + MassDiffDictionary.OxygenMass - MassDiffDictionary.HydrogenMass * 2), (factor * abundance * 0.3), $"{chain} C{bond} +O -2H OAD13") { SpectrumComment = speccomment });
        //        peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakLower + MassDiffDictionary.HydrogenMass), (factor * abundance * 0.2), $"{chain} C{bond} -C +H OAD14") { SpectrumComment = speccomment });
        //        peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakLower), (factor * abundance * 0.8), $"{chain} C{bond} -C OAD15") { SpectrumComment = speccomment | SpectrumComment.doublebond_high });
        //        if (lipid.LipidClass != LbmClass.PE)
        //        {
        //            peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakLower + MassDiffDictionary.OxygenMass), (factor * abundance * 0.2), $"{chain} C{bond} -C OAD15+O") { SpectrumComment = speccomment });
        //        }
        //        peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakLower - MassDiffDictionary.HydrogenMass), (factor * abundance * 0.2), $"{chain} C{bond} -C -H OAD16") { SpectrumComment = speccomment });
        //        peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakLower - MassDiffDictionary.HydrogenMass * 2), (factor * abundance * 0.15), $"{chain} C{bond} -C -2H OAD17") { SpectrumComment = speccomment });

        //        //add 20230330
        //        peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakLower + MassDiffDictionary.OxygenMass * 2 - MassDiffDictionary.HydrogenMass), (factor * abundance * 0.3), $"{chain} C{bond} +O -H OAD12+O") { SpectrumComment = speccomment });
        //        peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakLower + MassDiffDictionary.OxygenMass * 2), (factor * abundance * 0.3), $"{chain} C{bond} +O -H OAD12+O+H") { SpectrumComment = speccomment });
        //        if (lipid.LipidClass != LbmClass.PE)
        //        {
        //            peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakLower + MassDiffDictionary.OxygenMass * 2 + MassDiffDictionary.HydrogenMass), (factor * abundance * 0.2), $"{chain} C{bond} +O -H OAD12+O+2H") { SpectrumComment = speccomment });
        //        }
        //        peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakHigher + MassDiffDictionary.HydrogenMass * 2), (factor * abundance * 0.2), $"{chain} C{bond} +C +O +H OAD01+H") { SpectrumComment = speccomment });
        //    }
        //    else if (adduct.IonMode == IonMode.Negative)
        //    {
        //        //add 20230330
        //        peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakHigher + MassDiffDictionary.HydrogenMass), (factor * abundance * 0.2), $"{chain} C{bond} +C +O +H OAD01") { SpectrumComment = speccomment });
        //        peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakHigher), (factor * abundance * 0.3), $"{chain} C{bond} +O OAD02") { SpectrumComment = speccomment | SpectrumComment.doublebond_high });
        //        //peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakHigher + MassDiffDictionary.OxygenMass), (factor * abundance * 0.3), $"{chain} C{bond} +O OAD02+O") { SpectrumComment = speccomment });
        //        peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakHigher - MassDiffDictionary.HydrogenMass), (factor * abundance * 0.5), $"{chain} C{bond} +C +O -H OAD03") { SpectrumComment = speccomment });
        //        peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakHigher - MassDiffDictionary.HydrogenMass * 2), (factor * abundance * 0.8), $"{chain} C{bond} +C +O -2H OAD04") { SpectrumComment = speccomment });

        //        if (lipid.LipidClass != LbmClass.PE)
        //        {
        //            peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakHigher - H2O + MassDiffDictionary.HydrogenMass), (factor * abundance * 0.2), $"{chain} C{bond} +C +O +H -H2O OAD05") { SpectrumComment = speccomment });
        //        }
        //        //peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakHigher - H2O), (factor * abundance * 0.3), $"{chain} C{bond} +C +O -H2O OAD06") { SpectrumComment = speccomment });
        //        //peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakHigher - H2O - MassDiffDictionary.HydrogenMass), (factor * abundance * 0.1), $"{chain} C{bond} +C +O -H -H2O OAD07") { SpectrumComment = speccomment });

        //        //peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeak + MassDiffDictionary.OxygenMass), (factor * abundance * 0.2), $"{chain} C{bond} +O OAD08") { SpectrumComment = speccomment });
        //        //peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeak + MassDiffDictionary.HydrogenMass * 2), (factor * abundance * 0.2), $"{chain} C{bond} OAD09") { SpectrumComment = speccomment });
        //        if (lipid.LipidClass != LbmClass.PE)
        //        {
        //            peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeak + MassDiffDictionary.HydrogenMass), (factor * abundance * 0.3), $"{chain} C{bond} -H OAD10") { SpectrumComment = speccomment });
        //        }
        //        //peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeak), (factor * abundance * 0.1), $"{chain} C{bond} -2H OAD11") { SpectrumComment = speccomment });

        //        if (lipid.LipidClass != LbmClass.PC)
        //        {
        //            peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakLower + MassDiffDictionary.OxygenMass - MassDiffDictionary.HydrogenMass), (factor * abundance * 0.3), $"{chain} C{bond} +O -H OAD12") { SpectrumComment = speccomment });
        //        }
        //        if (lipid.LipidClass != LbmClass.PE)
        //        {
        //            peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakLower + MassDiffDictionary.OxygenMass - MassDiffDictionary.HydrogenMass * 2), (factor * abundance * 0.3), $"{chain} C{bond} +O -2H OAD13") { SpectrumComment = speccomment });
        //        }
        //        if (lipid.LipidClass != LbmClass.PC)
        //        {
        //            peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakLower + MassDiffDictionary.HydrogenMass), (factor * abundance * 0.3), $"{chain} C{bond} -C +H OAD14") { SpectrumComment = speccomment });
        //        }
        //        peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakLower), (factor * abundance * 0.3), $"{chain} C{bond} -C OAD15") { SpectrumComment = speccomment | SpectrumComment.doublebond_high });
        //        if (lipid.LipidClass != LbmClass.PC)
        //        {
        //            peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakLower + MassDiffDictionary.OxygenMass), (factor * abundance * 0.2), $"{chain} C{bond} -C OAD15+O") { SpectrumComment = speccomment });
        //        }
        //        peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakLower - MassDiffDictionary.HydrogenMass), (factor * abundance * 0.3), $"{chain} C{bond} -C -H OAD16") { SpectrumComment = speccomment });
        //        //peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakLower - MassDiffDictionary.HydrogenMass * 2), (factor * abundance * 0.15), $"{chain} C{bond} -C -2H OAD17") { SpectrumComment = speccomment });

        //        peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakLower + MassDiffDictionary.OxygenMass * 2 - MassDiffDictionary.HydrogenMass), (factor * abundance * 0.5), $"{chain} C{bond} +O -H OAD12+O") { SpectrumComment = speccomment });
        //        peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakLower + MassDiffDictionary.OxygenMass * 2), (factor * abundance * 0.8), $"{chain} C{bond} +O -H OAD12+O+H") { SpectrumComment = speccomment });
        //        peaks.Add(new SpectrumPeak(adduct.ConvertToMz(dbPeakLower + MassDiffDictionary.OxygenMass * 2 + MassDiffDictionary.HydrogenMass), (factor * abundance * 0.2), $"{chain} C{bond} +O -H OAD12+O+2H") { SpectrumComment = speccomment });
        //    }

        //    return peaks;
        //}

        private List<OadFragmentPeaks> DoubleBondSpectrumWithId(int bond, double[] diffs, IChain chain, AdductIon adduct, double chainLoss, double abundance)
        {
            var OadPeaks = new List<OadFragmentPeaks>();
            var speccomment = SpectrumComment.doublebond;
            var factor = 1.0;
            var dbPeakHigher = chainLoss + diffs[bond] + MassDiffDictionary.HydrogenMass + MassDiffDictionary.OxygenMass;
            var dbPeak = chainLoss + diffs[bond - 1];
            var dbPeakLower = chainLoss + diffs[bond - 2] + MassDiffDictionary.HydrogenMass;
            OadPeaks.AddRange(new OadFragmentPeaks[]
            {
                    new OadFragmentPeaks
                    {
                        OadId = "OAD01",
                        spectrum = new SpectrumPeak(adduct.ConvertToMz(dbPeakHigher + MassDiffDictionary.HydrogenMass), (factor * abundance * 0.3), $"{chain} C{bond} +C +O +H OAD01") { SpectrumComment = speccomment }
                    },
                    new OadFragmentPeaks
                    {
                        OadId = "OAD02",
                        spectrum = new SpectrumPeak(adduct.ConvertToMz(dbPeakHigher), (factor * abundance), $"{chain} C{bond} +O OAD02") { SpectrumComment = speccomment | SpectrumComment.doublebond_high }
                    },
                    new OadFragmentPeaks
                    {
                        OadId = "OAD02+O",
                        spectrum = new SpectrumPeak(adduct.ConvertToMz(dbPeakHigher + MassDiffDictionary.OxygenMass), (factor * abundance * 0.3), $"{chain} C{bond} +O OAD02+O") { SpectrumComment = speccomment }
                    },
                    new OadFragmentPeaks
                    {
                        OadId = "OAD03",
                        spectrum = new SpectrumPeak(adduct.ConvertToMz(dbPeakHigher - MassDiffDictionary.HydrogenMass), (factor * abundance * 0.5), $"{chain} C{bond} +C +O -H OAD03") { SpectrumComment = speccomment }
                    },
                    new OadFragmentPeaks
                    {
                        OadId = "OAD04",
                        spectrum = new SpectrumPeak(adduct.ConvertToMz(dbPeakHigher - MassDiffDictionary.HydrogenMass * 2), (factor * abundance * 0.2), $"{chain} C{bond} +C +O -2H OAD04") { SpectrumComment = speccomment }
                    },

                    new OadFragmentPeaks
                    {
                        OadId = "OAD05",
                        spectrum = new SpectrumPeak(adduct.ConvertToMz(dbPeakHigher - H2O + MassDiffDictionary.HydrogenMass), (factor * abundance * 0.15), $"{chain} C{bond} +C +O +H -H2O OAD05") { SpectrumComment = speccomment }
                    },
                    new OadFragmentPeaks
                    {
                        OadId = "OAD06",
                        spectrum = new SpectrumPeak(adduct.ConvertToMz(dbPeakHigher - H2O), (factor * abundance * 0.3), $"{chain} C{bond} +C +O -H2O OAD06") { SpectrumComment = speccomment }
                    },
                    new OadFragmentPeaks
                    {
                        OadId = "OAD07",
                        spectrum = new SpectrumPeak(adduct.ConvertToMz(dbPeakHigher - H2O - MassDiffDictionary.HydrogenMass), (factor * abundance * 0.1), $"{chain} C{bond} +C +O -H -H2O OAD07") { SpectrumComment = speccomment }
                    },

                    new OadFragmentPeaks
                    {
                        OadId = "OAD08",
                        spectrum = new SpectrumPeak(adduct.ConvertToMz(dbPeak + MassDiffDictionary.OxygenMass), (factor * abundance * 0.2), $"{chain} C{bond} +O OAD08") { SpectrumComment = speccomment }
                    },
                    new OadFragmentPeaks
                    {
                        OadId = "OAD09",
                        spectrum = new SpectrumPeak(adduct.ConvertToMz(dbPeak + MassDiffDictionary.HydrogenMass * 2), (factor * abundance * 0.2), $"{chain} C{bond} OAD09") { SpectrumComment = speccomment }
                    },
                    new OadFragmentPeaks
                    {
                        OadId = "OAD10",
                        spectrum = new SpectrumPeak(adduct.ConvertToMz(dbPeak + MassDiffDictionary.HydrogenMass), (factor * abundance * 0.3), $"{chain} C{bond} -H OAD10") { SpectrumComment = speccomment }
                    },
                    new OadFragmentPeaks
                    {
                        OadId = "OAD11",
                        spectrum = new SpectrumPeak(adduct.ConvertToMz(dbPeak), (factor * abundance * 0.1), $"{chain} C{bond} -2H OAD11") { SpectrumComment = speccomment }
                    },

                    new OadFragmentPeaks
                    {
                        OadId = "OAD12",
                        spectrum = new SpectrumPeak(adduct.ConvertToMz(dbPeakLower + MassDiffDictionary.OxygenMass - MassDiffDictionary.HydrogenMass), (factor * abundance * 0.3), $"{chain} C{bond} +O -H OAD12") { SpectrumComment = speccomment }
                    },

                    new OadFragmentPeaks
                    {
                        OadId = "OAD13",
                        spectrum = new SpectrumPeak(adduct.ConvertToMz(dbPeakLower + MassDiffDictionary.OxygenMass - MassDiffDictionary.HydrogenMass * 2), (factor * abundance * 0.3), $"{chain} C{bond} +O -2H OAD13") { SpectrumComment = speccomment }
                    },
                    new OadFragmentPeaks
                    {
                        OadId = "OAD14",
                        spectrum = new SpectrumPeak(adduct.ConvertToMz(dbPeakLower + MassDiffDictionary.HydrogenMass), (factor * abundance * 0.2), $"{chain} C{bond} -C +H OAD14") { SpectrumComment = speccomment }
                    },
                    new OadFragmentPeaks
                    {
                        OadId = "OAD15",
                        spectrum = new SpectrumPeak(adduct.ConvertToMz(dbPeakLower), (factor * abundance * 0.8), $"{chain} C{bond} -C OAD15") { SpectrumComment = speccomment | SpectrumComment.doublebond_high }
                    },
                    new OadFragmentPeaks
                    {
                        OadId = "OAD15+O",
                        spectrum = new SpectrumPeak(adduct.ConvertToMz(dbPeakLower + MassDiffDictionary.OxygenMass), (factor * abundance * 0.2), $"{chain} C{bond} -C OAD15+O") { SpectrumComment = speccomment }
                    },
                    new OadFragmentPeaks
                    {
                        OadId = "OAD16",
                        spectrum = new SpectrumPeak(adduct.ConvertToMz(dbPeakLower - MassDiffDictionary.HydrogenMass), (factor * abundance * 0.2), $"{chain} C{bond} -C -H OAD16") { SpectrumComment = speccomment }
                    },
                    new OadFragmentPeaks
                    {
                        OadId = "OAD17",
                        spectrum = new SpectrumPeak(adduct.ConvertToMz(dbPeakLower - MassDiffDictionary.HydrogenMass * 2), (factor * abundance * 0.15), $"{chain} C{bond} -C -2H OAD17") { SpectrumComment = speccomment }
                    },

                    //add 20230330
                    new OadFragmentPeaks
                    {
                        OadId = "OAD12+O",
                        spectrum = new SpectrumPeak(adduct.ConvertToMz(dbPeakLower + MassDiffDictionary.OxygenMass * 2 - MassDiffDictionary.HydrogenMass), (factor * abundance * 0.3), $"{chain} C{bond} +O -H OAD12+O") { SpectrumComment = speccomment }
                    },
                    new OadFragmentPeaks
                    {
                        OadId = "OAD12+O+H",
                        spectrum = new SpectrumPeak(adduct.ConvertToMz(dbPeakLower + MassDiffDictionary.OxygenMass * 2), (factor * abundance * 0.3), $"{chain} C{bond} +O -H OAD12+O+H") { SpectrumComment = speccomment }
                    },
                    new OadFragmentPeaks
                    {
                        OadId = "OAD12+O+2H",
                        spectrum = new SpectrumPeak(adduct.ConvertToMz(dbPeakLower + MassDiffDictionary.OxygenMass * 2 + MassDiffDictionary.HydrogenMass), (factor * abundance * 0.2), $"{chain} C{bond} +O -H OAD12+O+2H") { SpectrumComment = speccomment }
                    },
                    new OadFragmentPeaks
                    {
                        OadId = "OAD01+H",
                        spectrum = new SpectrumPeak(adduct.ConvertToMz(dbPeakHigher + MassDiffDictionary.HydrogenMass * 2), (factor * abundance * 0.2), $"{chain} C{bond} +C +O +H OAD01+H") { SpectrumComment = speccomment }
                    }
            });
            return OadPeaks;
        }

        private List<OadFragmentPeaks> SphingoDoubleBondSpectrumWithId(int bond, double[] diffs, IChain sphingo, AdductIon adduct, double chainLoss, double abundance)
        {
            var OadPeaks = new List<OadFragmentPeaks>();
            var speccomment = SpectrumComment.doublebond;
            var factor = 1.0;
            var dbPeak = chainLoss + diffs[bond - 1 - 1] - MassDiffDictionary.HydrogenMass;
            OadPeaks.AddRange(new OadFragmentPeaks[]
            {
                    new OadFragmentPeaks
                    {
                        OadId = "SphOAD",
                        spectrum = new SpectrumPeak(adduct.ConvertToMz(dbPeak + Electron), (factor * abundance), $"{sphingo} C{bond} DB ") { SpectrumComment = speccomment }
                    },
                    new OadFragmentPeaks
                    {
                        OadId = "SphOAD+H",
                        spectrum = new SpectrumPeak(adduct.ConvertToMz(dbPeak + MassDiffDictionary.HydrogenMass), (factor * abundance * 0.5), $"{sphingo} C{bond} DB +H") { SpectrumComment = speccomment }
                    },
                    new OadFragmentPeaks
                    {
                        OadId = "SphOAD+2H",
                        spectrum = new SpectrumPeak(adduct.ConvertToMz(dbPeak + MassDiffDictionary.HydrogenMass * 2), (factor * abundance * 0.5), $"{sphingo} C{bond} DB +2H") { SpectrumComment = speccomment }
                    },
                   new OadFragmentPeaks
                    {
                        OadId = "SphOAD-CO",
                        spectrum = new SpectrumPeak(adduct.ConvertToMz(dbPeak + Electron-MassDiffDictionary.CarbonMass-MassDiffDictionary.OxygenMass), (factor * abundance), $"{sphingo} C{bond} DB -C=O ") { SpectrumComment = speccomment }
                    },
            });
            return OadPeaks;
        }

        public IEnumerable<SpectrumPeak> GetAcylDoubleBondSpectrum(ILipid lipid, AcylChain acylChain, AdductIon adduct, double nlMass, double abundance, string[] oadId)
            => GetOadDoubleBondSpectrum(lipid, acylChain, adduct, nlMass - MassDiffDictionary.OxygenMass + MassDiffDictionary.HydrogenMass * 2, abundance, oadId);

        public IEnumerable<SpectrumPeak> GetAlkylDoubleBondSpectrum(ILipid lipid, AlkylChain acylChain, AdductIon adduct, double nlMass, double abundance, string[] oadId)
            => GetOadDoubleBondSpectrum(lipid, acylChain, adduct, nlMass, abundance, oadId);

        public IEnumerable<SpectrumPeak> GetSphingoDoubleBondSpectrum(ILipid lipid, SphingoChain sphingo, AdductIon adduct, double nlMass, double abundance, string[] oadId)
        {
            if (sphingo.DoubleBond.UnDecidedCount != 0 || sphingo.CarbonCount == 0)
            {
                return Enumerable.Empty<SpectrumPeak>();
            }
            var chainLoss = lipid.Mass - sphingo.Mass - nlMass + MassDiffDictionary.NitrogenMass + MassDiffDictionary.OxygenMass * 2 + MassDiffDictionary.HydrogenMass * 1;
            var diffs = new double[sphingo.CarbonCount];
            for (int i = 0; i < sphingo.CarbonCount; i++)
            {
                diffs[i] = CH2;
            }

            var bondPositions = new List<int>();
            foreach (var bond in sphingo.DoubleBond.Bonds)
            {
                diffs[bond.Position - 1] -= MassDiffDictionary.HydrogenMass;
                diffs[bond.Position] -= MassDiffDictionary.HydrogenMass;
                bondPositions.Add(bond.Position);
            }
            for (int i = 1; i < sphingo.CarbonCount; i++)
            {
                diffs[i] += diffs[i - 1];
            }

            var peaks = new List<SpectrumPeak>();
            foreach (var bond in bondPositions)
            {
                if (bond != 4)
                {
                    var addPeaks = DoubleBondSpectrum(bond, diffs, sphingo, adduct, chainLoss, abundance, oadId);
                    peaks.AddRange(addPeaks);
                }
                else
                {
                    var SphOadPeaks = SphingoDoubleBondSpectrumWithId(bond, diffs, sphingo, adduct, chainLoss, abundance);
                    peaks.AddRange(SphOadPeaks.Where(p => oadId.Contains(p.OadId)).Select(p => p.spectrum).ToList());
                }
            }
            return peaks;
        }
    }
    public class OadFragmentPeaks
    {
        public string OadId { get; set; }
        public SpectrumPeak spectrum { get; set; }
    }

}
