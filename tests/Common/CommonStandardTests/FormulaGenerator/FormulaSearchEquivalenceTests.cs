using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Ion;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.FormulaGenerator.Parser;
using CompMs.Common.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.FormulaGenerator.Tests
{
    /// <summary>
    /// The formula prediction speed-up must not change results. These tests compare the optimised code with the
    /// code it replaced, kept verbatim in ReferenceFragmentAssigner and ReferenceGoldenRules.
    /// </summary>
    [TestClass]
    public class FormulaSearchEquivalenceTests
    {
        private const string ResourceDir = @"Resources\FormulaGenerator\";

        [TestMethod]
        public void FirstNotBelowMatchesLinearScanTest()
        {
            var random = new Random(20261115);
            for (int trial = 0; trial < 200000; trial++)
            {
                var baseMass = random.NextDouble() * 1000.0;
                var unitMass = new[] { 1.00782503207, 12.0, 14.0030740048, 15.99491461956, 78.9183371, 126.904473 }[random.Next(6)];
                var remaining = random.Next(3) == 0 ? 0.0 : random.NextDouble() * 500.0;
                var lower = random.NextDouble() * 2000.0;
                var max = random.Next(-1, 300);
                if (trial % 7 == 0)
                {
                    // put the boundary exactly on a step
                    var k = random.Next(0, Math.Max(1, max + 1));
                    lower = baseMass + (double)k * unitMass + remaining;
                }

                var expected = 0;
                while (expected <= max && baseMass + (double)expected * unitMass + remaining < lower) expected++;
                if (max < 0) expected = 0;

                Assert.AreEqual(expected, EnumerationBound.FirstNotBelow(baseMass, unitMass, remaining, lower, max),
                    $"base={baseMass:R} unit={unitMass:R} rem={remaining:R} lower={lower:R} max={max}");
            }
        }

        [TestMethod]
        public void GoldenRulesOnCountsMatchReferenceTest()
        {
            var random = new Random(7);
            var adducts = new[] { "[M+H]+", "[M]+", "[M-H]-", "[M-2H]-", "[M+Na]+" }.Select(AdductIon.GetAdductIon).ToArray();
            var ranges = new[] { CoverRange.CommonRange, CoverRange.ExtendedRange, CoverRange.ExtremeRange };
            for (int trial = 0; trial < 50000; trial++)
            {
                var c = random.Next(1, 60);
                var formula = new Formula(c, random.Next(0, 4 * c + 2), random.Next(0, 12), random.Next(0, 30), random.Next(0, 5), random.Next(0, 6),
                    random.Next(0, 5), random.Next(0, 4), random.Next(0, 3), random.Next(0, 3), random.Next(0, 3));
                var adduct = adducts[random.Next(adducts.Length)];
                var range = ranges[random.Next(ranges.Length)];
                var valence = random.Next(2) == 0;
                var probability = random.Next(2) == 0;

                var expected = ReferenceGoldenRules.Check(formula, valence, range, probability, adduct);
                Assert.AreEqual(expected, SevenGoldenRulesCheck.Check(formula, valence, range, probability, adduct), formula.FormulaString);
                Assert.AreEqual(expected, SevenGoldenRulesCheck.Check(formula.Cnum, formula.Hnum, formula.Nnum, formula.Onum, formula.Pnum, formula.Snum,
                    formula.Fnum, formula.Clnum, formula.Brnum, formula.Inum, formula.Sinum, valence, range, probability,
                    SevenGoldenRulesCheck.IsHydrogenShiftAdduct(adduct)), formula.FormulaString);
                Assert.AreEqual(ReferenceGoldenRules.ValenceCheckByHydrogenShift(formula), SevenGoldenRulesCheck.ValenceCheckByHydrogenShift(formula));
            }
        }

        [TestMethod]
        [DeploymentItem(ResourceDir + "test_data1.msp", ResourceDir)]
        [DeploymentItem(ResourceDir + "ProductIonLib_vs1.pid", ResourceDir)]
        [DeploymentItem(ResourceDir + "NeutralLossDB_vs2.ndb", ResourceDir)]
        [DeploymentItem(ResourceDir + "MsfinderFormulaDB-VS13.efd", ResourceDir)]
        public void FragmentAssignmentMatchesReferenceTest()
        {
            var productIonDB = FragmentDbParser.GetProductIonDB(ResourceDir + "ProductIonLib_vs1.pid", out string _);
            var neutralLossDB = FragmentDbParser.GetNeutralLossDB(ResourceDir + "NeutralLossDB_vs2.ndb", out string _);
            var existFormulaDB = ExistFormulaDbParcer.ReadExistFormulaDB(ResourceDir + "MsfinderFormulaDB-VS13.efd", out string _);
            var param = new AnalysisParamOfMsfinder();
            var rawData = RawDataParcer.RawDataFileReader(ResourceDir + "test_data1.msp", param);

            foreach (var ionMode in new[] { "[M+H]+", "[M-H]-" })
            {
                var adduct = AdductIon.GetAdductIon(ionMode);
                var candidates = MolecularFormulaFinder.GetMolecularFormulaList(productIonDB, neutralLossDB, existFormulaDB, rawData, param)
                    .Select(r => r.Formula).ToList();
                var random = new Random(11);
                var peaks = getTestPeaks(rawData, param, candidates, random);
                var neutralLosses = FragmentAssigner.GetNeutralLossList(peaks, rawData.PrecursorMz, 0.01);

                foreach (var tolType in new[] { MassToleranceType.Ppm, MassToleranceType.Da })
                {
                    var ms2Tol = tolType == MassToleranceType.Ppm ? 20.0 : 0.01;
                    var productIonQuery = FragmentAssigner.PrepareProductIonQuery(peaks, productIonDB, ms2Tol, tolType, adduct);
                    var neutralLossQuery = FragmentAssigner.PrepareNeutralLossQuery(neutralLosses, neutralLossDB, ms2Tol, tolType, adduct);

                    // Candidates in a shuffled order, twice, so that the existence cache of the query is used across candidates.
                    var formulas = candidates.Concat(randomFormulas(random, 300)).ToList();
                    var order = formulas.Concat(formulas).OrderBy(_ => random.Next()).ToList();
                    foreach (var formula in order)
                    {
                        var expectedIons = ReferenceFragmentAssigner.FastFragmentAssigner(peaks, productIonDB, formula, ms2Tol, tolType, adduct);
                        assertSameProductIons(expectedIons, FragmentAssigner.FastFragmentAssigner(productIonQuery, formula), formula);
                        assertSameProductIons(expectedIons, FragmentAssigner.FastFragmentAssigner(peaks, productIonDB, formula, ms2Tol, tolType, adduct), formula);
                        Assert.AreEqual(expectedIons.Count, FragmentAssigner.CountProductIons(productIonQuery, formula), formula.FormulaString);

                        var expectedLosses = ReferenceFragmentAssigner.FastNeutralLossAssigner(neutralLosses, neutralLossDB, formula, ms2Tol, tolType, adduct);
                        assertSameNeutralLosses(expectedLosses, FragmentAssigner.FastNeutralLossAssigner(neutralLossQuery, formula), formula);
                        Assert.AreEqual(referenceUniqueNeutralLossCount(expectedLosses),
                            FragmentAssigner.CountUniqueNeutralLossFormulas(neutralLossQuery, formula), formula.FormulaString);
                    }
                }
            }
        }

        [TestMethod]
        [DeploymentItem(ResourceDir + "test_data1.msp", ResourceDir)]
        [DeploymentItem(ResourceDir + "ProductIonLib_vs1.pid", ResourceDir)]
        [DeploymentItem(ResourceDir + "NeutralLossDB_vs2.ndb", ResourceDir)]
        [DeploymentItem(ResourceDir + "MsfinderFormulaDB-VS13.efd", ResourceDir)]
        public void FormulaListOrderIsDeterministicTest()
        {
            var productIonDB = FragmentDbParser.GetProductIonDB(ResourceDir + "ProductIonLib_vs1.pid", out string _);
            var neutralLossDB = FragmentDbParser.GetNeutralLossDB(ResourceDir + "NeutralLossDB_vs2.ndb", out string _);
            var existFormulaDB = ExistFormulaDbParcer.ReadExistFormulaDB(ResourceDir + "MsfinderFormulaDB-VS13.efd", out string _);
            var param = new AnalysisParamOfMsfinder();
            var rawData = RawDataParcer.RawDataFileReader(ResourceDir + "test_data1.msp", param);

            var first = MolecularFormulaFinder.GetMolecularFormulaList(productIonDB, neutralLossDB, existFormulaDB, rawData, param);
            for (int i = 0; i < 5; i++)
            {
                var next = MolecularFormulaFinder.GetMolecularFormulaList(productIonDB, neutralLossDB, existFormulaDB, rawData, param);
                CollectionAssert.AreEqual(first.Select(describe).ToList(), next.Select(describe).ToList());
            }
        }

        private static string describe(FormulaResult r)
        {
            return string.Join("|", r.Formula.FormulaString, r.TotalScore.ToString("R"), r.IsSelected, r.ProductIonNum, r.ProductIonHits,
                r.NeutralLossNum, r.NeutralLossHits, r.ProductIonResult.Count, r.NeutralLossResult.Count);
        }

        /// <summary>
        /// The refined peaks of the spectrum plus monoisotopic peaks at the masses of sub-formulas of the candidates,
        /// shifted a little, so that many peaks miss the product-ion library and go to the valence-checked enumeration.
        /// </summary>
        private static List<SpectrumPeak> getTestPeaks(RawData rawData, AnalysisParamOfMsfinder param, List<Formula> candidates, Random random)
        {
            var refined = FragmentAssigner.GetRefinedPeaklist(FragmentAssigner.GetCentroidMsMsSpectrum(rawData), param.RelativeAbundanceCutOff, 0.0,
                rawData.PrecursorMz, param.Mass2Tolerance, param.MassTolType, 1000, false, true);
            var peaks = refined.Select(p => new SpectrumPeak { Mass = p.Mass, Intensity = p.Intensity, Comment = p.Comment }).ToList();
            foreach (var formula in candidates.Take(20))
            {
                var sub = new Formula(random.Next(formula.Cnum / 2, formula.Cnum + 1), random.Next(0, formula.Hnum + 1), random.Next(0, formula.Nnum + 1),
                    random.Next(0, formula.Onum + 1), random.Next(0, formula.Pnum + 1), random.Next(0, formula.Snum + 1), 0, 0, 0, 0, 0);
                peaks.Add(new SpectrumPeak { Mass = sub.Mass + 1.00727646677 + (random.NextDouble() - 0.5) * 0.004, Intensity = 10 + random.Next(100), Comment = "M" });
            }
            return peaks.OrderBy(p => p.Mass).ToList();
        }

        private static IEnumerable<Formula> randomFormulas(Random random, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var c = random.Next(5, 45);
                yield return new Formula(c, random.Next(0, 2 * c + 3), random.Next(0, 8), random.Next(0, 18), random.Next(0, 3), random.Next(0, 4),
                    random.Next(0, 3), random.Next(0, 3), random.Next(0, 2), random.Next(0, 2), 0);
            }
        }

        private static void assertSameProductIons(List<ProductIon> expected, List<ProductIon> actual, Formula formula)
        {
            Assert.AreEqual(expected.Count, actual.Count, formula.FormulaString);
            for (int i = 0; i < expected.Count; i++)
            {
                var e = expected[i];
                var a = actual[i];
                Assert.AreEqual(e.Formula.FormulaString, a.Formula.FormulaString, formula.FormulaString);
                Assert.AreEqual(e.Formula.Mass, a.Formula.Mass, 0.0, formula.FormulaString);
                Assert.AreEqual(e.Mass, a.Mass, 0.0);
                Assert.AreEqual(e.MassDiff, a.MassDiff, 0.0);
                Assert.AreEqual(e.Intensity, a.Intensity, 0.0);
                Assert.AreEqual(e.Frequency, a.Frequency, 0.0);
                Assert.AreSame(e.CandidateInChIKeys.Count == 0 ? null : e.CandidateInChIKeys, a.CandidateInChIKeys.Count == 0 ? null : a.CandidateInChIKeys);
                CollectionAssert.AreEqual(e.CandidateOntologies, a.CandidateOntologies);
            }
        }

        private static void assertSameNeutralLosses(List<NeutralLoss> expected, List<NeutralLoss> actual, Formula formula)
        {
            Assert.AreEqual(expected.Count, actual.Count, formula.FormulaString);
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.AreSame(expected[i].Formula, actual[i].Formula, formula.FormulaString);
                Assert.AreEqual(expected[i].MassLoss, actual[i].MassLoss, 0.0);
                Assert.AreEqual(expected[i].MassError, actual[i].MassError, 0.0);
                Assert.AreEqual(expected[i].PrecursorMz, actual[i].PrecursorMz, 0.0);
                Assert.AreEqual(expected[i].ProductMz, actual[i].ProductMz, 0.0);
            }
        }

        // FormulaGenerator.GetUniqueNeutralLossCount as it was.
        private static int referenceUniqueNeutralLossCount(List<NeutralLoss> neutralLosses)
        {
            if (neutralLosses.Count == 0) return 0;
            var formulas = new List<Formula>() { neutralLosses[0].Formula };
            if (neutralLosses.Count == 1) return 1;
            for (int i = 1; i < neutralLosses.Count; i++)
            {
                var lossFormula = neutralLosses[i].Formula;
                var flg = false;
                foreach (var formula in formulas)
                {
                    if (MolecularFormulaUtility.isFormulaMatch(formula, lossFormula, true))
                    {
                        flg = true;
                        break;
                    }
                }
                if (!flg) formulas.Add(lossFormula);
            }
            return formulas.Count;
        }
    }
}
