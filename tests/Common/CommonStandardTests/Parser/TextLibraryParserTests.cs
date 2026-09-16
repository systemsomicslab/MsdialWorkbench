using CompMs.Common.DataObj.Property;
using CompMs.Common.Algorithm.IsotopeCalc;
#if  NETSTANDARD || NETFRAMEWORK
using CompMs.Common.Extension;
#endif
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;

namespace CompMs.Common.Parser.Tests
{
    [TestClass()]
    public class TextLibraryParserTests
    {
        private readonly string data = @"NAME	MZ	RT	Adduct	InChIKey	Formula	SMILES	Ontology	CCS
PC 33:1(d7)|PC 15:0_18:1(d7)	753.6134	9.5	[M+H]+	ZEWLMKXMNQOCOQ-GCHPQBSENA-N	C41H73D7NO8P	[C@](COP(=O)([O-])OCC[N+](C)(C)C)([H])(OC(CCCCCCC/C=C\CCCCCC([2H])([2H])C([2H])([2H])C([2H])([2H])[2H])=O)COC(CCCCCCCCCCCCCC)=O	PC	285.9533
PE 33:1(d7)|PE 15:0_18:1(d7)	711.56647	9.64	[M+H]+	ADCNXGARWPJRBV-RGLIIYCRNA-N	C38H67D7NO8P	[C@](COP(=O)(O)OCCN)([H])(OC(CCCCCCC/C=C\CCCCCC([2H])([2H])C([2H])([2H])C([2H])([2H])[2H])=O)COC(CCCCCCCCCCCCCC)=O	PE	272.6542
PS 33:1(d7)|PS 15:0_18:1(d7)	755.55635	8.38	[M+H]+	KVBAVKWITJZQEG-UDKXCJCZNA-N	C39H67D7NO10P	C(O)(=O)[C@@]([H])(N)COP(OC[C@]([H])(OC(CCCCCCC/C=C\CCCCCC([2H])([2H])C([2H])([2H])C([2H])([2H])[2H])=O)COC(CCCCCCCCCCCCCC)=O)(=O)O	PS	280.763
PG 33:1(d7)|PG 15:0_18:1(d7)	759.58765	8.42	[M+NH4]+	CAKDJPLPYOYWLK-AHOXJELVNA-N	C39H68D7O10P	[H][C@](O)(CO)COP(OC[C@]([H])(OC(CCCCCCC/C=C\CCCCCC([2H])([2H])C([2H])([2H])C([2H])([2H])[2H])=O)COC(CCCCCCCCCCCCCC)=O)(=O)O	PG	286.2431
PI 33:1(d7)|PI 15:0_18:1(d7)	847.604124	8.35	[M+NH4]+	XCKYASHMOHAUQB-OAFUKSMZNA-N	C42H72D7O13P	[C@]([H])(OC(CCCCCCC/C=C\CCCCCC([2H])([2H])C([2H])([2H])C([2H])([2H])[2H])=O)(COP(=O)(O)O[C@@H]1C(O)[C@H](O)C(O)C(O)C1O)COC(CCCCCCCCCCCCCC)=O	PI	294.3615
LPC 18:1(d7)	529.39934	6.33	[M+H]+	YAMUFBLWGFFICM-HNNXNMBSNA-N	C26H45D7NO7P	C(COP(=O)([O-])OCC[N+](C)(C)C)([H])(O)COC(CCCCCCC/C=C\CCCCCC([2H])([2H])C([2H])([2H])C([2H])([2H])[2H])=O	LPC	231.1935
LPE 18:1(d7)	487.35238	6.4	[M+H]+	PYVRVRFVLRNJLY-CCLUNVSZNA-N	C23H39D7NO7P	[C@](COP(=O)(O)OCCN)([H])(O)COC(CCCCCCC/C=C\CCCCCC([2H])([2H])C([2H])([2H])C([2H])([2H])[2H])=O	LPE	216.9472
CE 18:1(d7)	675.67785	14.94	[M+NH4]+	RJECHNNFRHZQKU-LCUGTLGDNA-N	C45H71D7O2	[C@]12(CC=C3C[C@@H](OC(=O)CCCCCCC/C=C\CCCCCC([2H])([2H])C([2H])([2H])C([2H])([2H])[2H])CC[C@]3(C)[C@@]1([H])CC[C@]1(C)[C@@]([H])([C@@](C)([H])CCCC(C)C)CC[C@@]21[H])[H]	CE	287.669
MG 18:1(d7)	381.37036	6.95	[M+NH4]+	RZRNAYUHWVFMIP-IJGLUQEONA-N	C21H33D7O4	OCC([H])(O)COC(CCCCCCC/C=C\CCCCCC([2H])([2H])C([2H])([2H])C([2H])([2H])[2H])=O	MG	206.191
DG 33:1(d7)|DG 15:0_18:1(d7)	605.58449	11.42	[M+NH4]+	GWAPRYUVSHVZHN-ZYYJJESQNA-N	C36H61D7O5	OC[C@]([H])(OC(CCCCCCC/C=C\CCCCCC([2H])([2H])C([2H])([2H])C([2H])([2H])[2H])=O)COC(CCCCCCCCCCCCCC)=O	DG	260.628
TG 48:1(d7)|TG 15:0_18:1(d7)_15:0	829.79854	14.45	[M+NH4]+	YUNYDLOKHYJQAT-OTEPLKBXSA-N	C51H89D7O6	C(OC(=O)CCCCCCCCCCCCCC)[C@](OC(CCCCCCC/C=C\CCCCCC([2H])([2H])C([2H])([2H])C([2H])([2H])[2H])=O)COC(CCCCCCCCCCCCCC)=O	TG	310.2784
SM 36:1;2O(d9)|SM 18:1;2O/18:1(d9)	738.647	9.11	[M+H]+	NBEADXWAAWCCDG-KYPZZJCONA-N	C41H72D9N2O6P	[C@](COP(=O)([O-])OCC[N+](C)(C)C)([H])(NC(CCCCCCC/C=C\CCCCC([2H])([2H])C([2H])([2H])C([2H])([2H])C([2H])([2H])[2H])=O)[C@]([H])(O)/C=C/CCCCCCCCCCCCC	SM	288.9422
Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)	531.5476588	9.34	[M+H]+	HBULQAPKKLNTLT-BXLQGFJZSA-N	C33H58D7NO3	[H][C@@](O)(\C=C\CCCCCCCCCCCCC)[C@]([H])(CO)NC(=O)CCCCCCCCCCCC([2H])([2H])C([2H])([2H])C([2H])([2H])[2H]	Cer-NS	254.166";

        [TestMethod()]
        public void TextLibraryReaderValidNameTest()
        {
            var sr = new StringReader(data);
            var references = TextLibraryParser.TextLibraryReader(sr, out _);
            var results = references.Select(result => result.Name).ToList();
            var expected = data.Split('\n').Skip(1).Select(row => row.TrimEnd('\r').Split('\t')[0]).ToList();
            results.Sort();
            expected.Sort();
            CollectionAssert.AreEqual(expected, results);
        }

        [TestMethod()]
        public void TextLibraryReaderValidScanIDTest()
        {
            var sr = new StringReader(data);
            var references = TextLibraryParser.TextLibraryReader(sr, out _);
            var n = data.Split('\n').Length;
            var results = references.Select(result => result.ScanID).ToList();
            results.Sort();
            CollectionAssert.AreEqual(Enumerable.Range(0, n-1).ToArray(), results);
        }

        [TestMethod()]
        public void TextLibraryReaderValidMzTest()
        {
            var sr = new StringReader(data);
            var references = TextLibraryParser.TextLibraryReader(sr, out _);
            var results = references.Select(result => result.PrecursorMz).ToList();
            var expected = data.Split('\n').Skip(1).Select(row => double.Parse(row.TrimEnd('\r').Split('\t')[1])).ToList();
            results.Sort();
            expected.Sort();
            CollectionAssert.AreEqual(expected, results);
        }

        [TestMethod()]
        public void TextLibraryReaderValidRtTest()
        {
            var sr = new StringReader(data);
            var references = TextLibraryParser.TextLibraryReader(sr, out _);
            var expected = data.Split('\n').Skip(1).Select(row => double.Parse(row.TrimEnd('\r').Split('\t')[2])).ToList();
            var results = references.Select(reference => reference.ChromXs.RT.Value).ToList();
            expected.Sort();
            results.Sort();
            CollectionAssert.AreEqual(expected, results);
        }

        [TestMethod()]
        public void TextLibraryReaderValidAdductIonTest()
        {
            var sr = new StringReader(data);
            var references = TextLibraryParser.TextLibraryReader(sr, out _);
            var results = references.Select(reference => reference.AdductType).ToList();
            var expected = data.Split('\n').Skip(1).Select(row => AdductIon.GetAdductIon(row.TrimEnd('\r').Split('\t')[3])).ToList();

            foreach ((AdductIon a, AdductIon b) in expected.ZipInternal(results))
            {
                Debug.WriteLine($"{a.AdductIonName}\t{b.AdductIonName}");
                Assert.AreEqual(a.AdductIonAccurateMass, b.AdductIonAccurateMass);
                Assert.AreEqual(a.AdductIonXmer, b.AdductIonXmer);
                Assert.AreEqual(a.AdductIonName, b.AdductIonName);
                Assert.AreEqual(a.ChargeNumber, b.ChargeNumber);
                Assert.AreEqual(a.IonMode, b.IonMode);
                Assert.AreEqual(a.FormatCheck, b.FormatCheck);
                Assert.AreEqual(a.M1Intensity, b.M1Intensity);
                Assert.AreEqual(a.M2Intensity, b.M2Intensity);
                Assert.AreEqual(a.IsRadical, b.IsRadical);
                Assert.AreEqual(a.IsIncluded, b.IsIncluded);
            }
        }

        [TestMethod()]
        public void TextLibraryReaderValidInChIKeyTest()
        {
            var sr = new StringReader(data);
            var references = TextLibraryParser.TextLibraryReader(sr, out _);
            var results = references.Select(reference => reference.InChIKey).ToList();
            var expected = data.Split('\n').Skip(1).Select(row => row.TrimEnd('\r').Split('\t')[4]).ToList();
            results.Sort();
            expected.Sort();
            CollectionAssert.AreEqual(expected, results);
        }

        [TestMethod()]
        public void TextLibraryReaderValidFormulaTest()
        {
            var sr = new StringReader(data);
            var references = TextLibraryParser.TextLibraryReader(sr, out _);
            var results = references.Select(rs => rs.Formula).ToList();
            var expected = data.Split('\n').Skip(1).Select(row => FormulaGenerator.Parser.FormulaStringParcer.OrganicElementsReader(row.TrimEnd('\r').Split('\t')[5])).ToList();
            expected.ForEach(formula =>
            {
                formula.M1IsotopicAbundance = FormulaGenerator.Function.SevenGoldenRulesCheck.GetM1IsotopicAbundance(formula);
                formula.M2IsotopicAbundance = FormulaGenerator.Function.SevenGoldenRulesCheck.GetM2IsotopicAbundance(formula);
            });
            results.Sort((a, b) => a.Mass.CompareTo(b.Mass));
            expected.Sort((a, b) => a.Mass.CompareTo(b.Mass));
            foreach ((Formula a, Formula b) in expected.ZipInternal(results))
            {
                Assert.AreEqual(a.FormulaString, b.FormulaString);
                Assert.AreEqual(a.Mass, b.Mass);
                Assert.AreEqual(a.M1IsotopicAbundance, b.M1IsotopicAbundance);
                Assert.AreEqual(a.M2IsotopicAbundance, b.M2IsotopicAbundance);
                Assert.AreEqual(a.Cnum, b.Cnum);
                Assert.AreEqual(a.Nnum, b.Nnum);
                Assert.AreEqual(a.Hnum, b.Hnum);
                Assert.AreEqual(a.Onum, b.Onum);
                Assert.AreEqual(a.Snum, b.Snum);
                Assert.AreEqual(a.Pnum, b.Pnum);
                Assert.AreEqual(a.Fnum, b.Fnum);
                Assert.AreEqual(a.Clnum, b.Clnum);
                Assert.AreEqual(a.Brnum, b.Brnum);
                Assert.AreEqual(a.Inum, b.Inum);
                Assert.AreEqual(a.Sinum, b.Sinum);
                Assert.AreEqual(a.TmsCount, b.TmsCount);
                Assert.AreEqual(a.MeoxCount, b.MeoxCount);
            }
        }

        [TestMethod()]
        public void TextLibraryReaderValidIsotopicPeakTest()
        {
            var sr = new StringReader(data);
            var references = TextLibraryParser.TextLibraryReader(sr, out _);
            var results = references.Select(reference => reference.IsotopicPeaks).ToList();
            var iupacDb = IupacResourceParser.GetIUPACDatabase();
            var formulas = data.Split('\n').Skip(1).Select(row => FormulaGenerator.Parser.FormulaStringParcer.OrganicElementsReader(row.TrimEnd('\r').Split('\t')[5])).ToList();
            var expected = formulas.Select(formula => IsotopeCalculator.GetAccurateIsotopeProperty(formula.FormulaString, 2, iupacDb).IsotopeProfile).ToList();
            results.Sort((a, b) => a.Sum(e => e.MassDifferenceFromMonoisotopicIon).CompareTo(b.Sum(e => e.MassDifferenceFromMonoisotopicIon)));
            expected.Sort((a, b) => a.Sum(e => e.MassDifferenceFromMonoisotopicIon).CompareTo(b.Sum(e => e.MassDifferenceFromMonoisotopicIon)));
            foreach ((List<IsotopicPeak> a, List<IsotopicPeak> b) in expected.ZipInternal(results))
            {
                CollectionAssert.AreEqual(a.Select(peak => peak.RelativeAbundance).ToArray(), b.Select(peak => peak.RelativeAbundance).ToArray());
                CollectionAssert.AreEqual(a.Select(peak => peak.Mass).ToArray(), b.Select(peak => peak.Mass).ToArray());
                CollectionAssert.AreEqual(a.Select(peak => peak.MassDifferenceFromMonoisotopicIon).ToArray(), b.Select(peak => peak.MassDifferenceFromMonoisotopicIon).ToArray());
                CollectionAssert.AreEqual(a.Select(peak => peak.Comment).ToArray(), b.Select(peak => peak.Comment).ToArray());
            }
        }

        [TestMethod()]
        public void TextLibraryReaderValidSMILESTest()
        {
            var sr = new StringReader(data);
            var references = TextLibraryParser.TextLibraryReader(sr, out _);
            var results = references.Select(reference => reference.SMILES).ToList();
            var expected = data.Split('\n').Skip(1).Select(row => row.TrimEnd('\r').Split('\t')[6]).ToList();
            results.Sort();
            expected.Sort();
            CollectionAssert.AreEqual(expected, results);
        }

        [TestMethod()]
        public void TextLibraryReaderValidOntologyTest()
        {
            var sr = new StringReader(data);
            var references = TextLibraryParser.TextLibraryReader(sr, out _);
            var results = references.Select(reference => reference.Ontology).ToList();
            var expected = data.Split('\n').Skip(1).Select(row => row.TrimEnd('\r').Split('\t')[7]).ToList();
            results.Sort();
            expected.Sort();
            CollectionAssert.AreEqual(expected, results);
        }

        [TestMethod()]
        public void TextLibraryReaderValidCCSTest()
        {
            var sr = new StringReader(data);
            var references = TextLibraryParser.TextLibraryReader(sr, out _);
            var results = references.Select(reference => reference.CollisionCrossSection).ToList();
            var expected = data.Split('\n').Skip(1).Select(row => double.Parse(row.TrimEnd('\r').Split('\t')[8])).ToList();
            results.Sort();
            expected.Sort();
            CollectionAssert.AreEqual(expected, results);
        }

        [TestMethod()]
        public void TestLibraryReaderNoOptionFieldTest()
        {
            string no_option_data = string.Join("\r\n", new string[]
            {
                "NAME\tMZ",
                "PC 33:1(d7)|PC 15:0_18:1(d7)\t753.6134",
                "PE 33:1(d7)|PE 15:0_18:1(d7)\t711.56647",
                "PS 33:1(d7)|PS 15:0_18:1(d7)\t755.55635",
                "PG 33:1(d7)|PG 15:0_18:1(d7)\t759.58765",
                "PI 33:1(d7)|PI 15:0_18:1(d7)\t847.604124",
                "LPC 18:1(d7)\t529.39934",
                "LPE 18:1(d7)\t487.35238",
                "CE 18:1(d7)\t675.67785",
                "MG 18:1(d7)\t381.37036",
                "DG 33:1(d7)|DG 15:0_18:1(d7)\t605.58449",
                "TG 48:1(d7)|TG 15:0_18:1(d7)_15:0\t829.79854",
                "SM 36:1;2O(d9)|SM 18:1;2O/18:1(d9)\t738.647",
                "Cer 33:1;2O(d7)|Cer 18:1;2O/15:0(d7)\t531.5476588"
            });

            var sr = new StringReader(no_option_data);
            try
            {
                TextLibraryParser.TextLibraryReader(sr, out _);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Exception occured: {ex.Message}");
            }
        }

        [TestMethod()]
        public void TextLibraryReaderInvalidLessFieldTest()
        {
            string less_field_data = string.Join("\r\n", new string[]
            {
                "NAME",
                "PC 33:1(d7)|PC 15:0_18:1(d7)",
                "PE 33:1(d7)|PE 15:0_18:1(d7)",
                "PS 33:1(d7)|PS 15:0_18:1(d7)",
                "PG 33:1(d7)|PG 15:0_18:1(d7)",
                "PI 33:1(d7)|PI 15:0_18:1(d7)",
            });

            var sr = new StringReader(less_field_data);
            var result = TextLibraryParser.TextLibraryReader(sr, out string message);
            var n = less_field_data.Split('\n').Length;
            var expected = Enumerable.Range(1, n - 1).Select(i => $"Error type 1: line {i} is not suitable.\r\n");
            foreach (var expect in expected)
            {
                StringAssert.Contains(message, expect);
            }
            Assert.IsNull(result);
        }

        [TestMethod()]
        public void TextLibraryReaderInvalidNonNumericalMzTest()
        {
            string non_numerical_mz_data = string.Join("\r\n", new string[]
            {
                "NAME\tMZ",
                "PC 33:1(d7)|PC 15:0_18:1(d7)\ta",
                "PE 33:1(d7)|PE 15:0_18:1(d7)\t ",
                "PS 33:1(d7)|PS 15:0_18:1(d7)\t755.55635",
                "PG 33:1(d7)|PG 15:0_18:1(d7)\t+",
                "PI 33:1(d7)|PI 15:0_18:1(d7)\t847.604124",
                "LPC 18:1(d7)\t-",
                "CE 18:1(d7)\t675.67785",
            });

            var sr = new StringReader(non_numerical_mz_data);
            var result = TextLibraryParser.TextLibraryReader(sr, out string message);
            var n = non_numerical_mz_data.Split('\n').Length;
            var expected = new int[] { 1, 2, 4, 6 }.Select(i => $"Error type 2: line {i} includes non-numerical value for accurate mass information.\r\n");
            foreach (var expect in expected)
            {
                StringAssert.Contains(message, expect);
            }
            Assert.IsNull(result);
        }

        [TestMethod()]
        public void TextLibraryReaderInvalidNonNumericalRtTest()
        {
            string non_numerical_rt_data = string.Join("\r\n", new string[]
            {
                "NAME\tMZ\tRT",
                "PC 33:1(d7)|PC 15:0_18:1(d7)\t753.6134\ta",
                "PE 33:1(d7)|PE 15:0_18:1(d7)\t711.56647\t ",
                "LPE 18:1(d7)\t487.35238\t6.4",
                "PS 33:1(d7)|PS 15:0_18:1(d7)\t755.55635\t+",
                "CE 18:1(d7)\t675.67785\t14.94",
                "PG 33:1(d7)|PG 15:0_18:1(d7)\t759.58765\t-",
                "MG 18:1(d7)\t381.37036\t6.95",
            });

            var sr = new StringReader(non_numerical_rt_data);
            var result = TextLibraryParser.TextLibraryReader(sr, out string message);
            var n = non_numerical_rt_data.Split('\n').Length;
            var expected = new int[] { 1, 2, 4, 6 }.Select(i => $"Error type 2: line {i} includes non-numerical value for retention time information.\r\n").ToArray();
            foreach (var expect in expected)
            {
                StringAssert.Contains(message, expect);
            }
            Assert.IsNull(result);
        }

        [TestMethod()]
        public void TextLIbraryReaderInvalidNonNumericalCCSTest()
        {

            string non_numerical_ccs_data = string.Join("\r\n", new string[]
            {
                @"NAME	MZ	RT	Adduct	InChIKey	Formula	SMILES	Ontology	CCS",
                @"PC 33:1(d7)|PC 15:0_18:1(d7)	753.6134	9.5	[M+H]+	ZEWLMKXMNQOCOQ-GCHPQBSENA-N	C41H73D7NO8P	[C@](COP(=O)([O-])OCC[N+](C)(C)C)([H])(OC(CCCCCCC/C=C\CCCCCC([2H])([2H])C([2H])([2H])C([2H])([2H])[2H])=O)COC(CCCCCCCCCCCCCC)=O	PC	a",
                @"PE 33:1(d7)|PE 15:0_18:1(d7)	711.56647	9.64	[M+H]+	ADCNXGARWPJRBV-RGLIIYCRNA-N	C38H67D7NO8P	[C@](COP(=O)(O)OCCN)([H])(OC(CCCCCCC/C=C\CCCCCC([2H])([2H])C([2H])([2H])C([2H])([2H])[2H])=O)COC(CCCCCCCCCCCCCC)=O	PE	 ",
                @"PS 33:1(d7)|PS 15:0_18:1(d7)	755.55635	8.38	[M+H]+	KVBAVKWITJZQEG-UDKXCJCZNA-N	C39H67D7NO10P	C(O)(=O)[C@@]([H])(N)COP(OC[C@]([H])(OC(CCCCCCC/C=C\CCCCCC([2H])([2H])C([2H])([2H])C([2H])([2H])[2H])=O)COC(CCCCCCCCCCCCCC)=O)(=O)O	PS	280.763",
                @"PG 33:1(d7)|PG 15:0_18:1(d7)	759.58765	8.42	[M+NH4]+	CAKDJPLPYOYWLK-AHOXJELVNA-N	C39H68D7O10P	[H][C@](O)(CO)COP(OC[C@]([H])(OC(CCCCCCC/C=C\CCCCCC([2H])([2H])C([2H])([2H])C([2H])([2H])[2H])=O)COC(CCCCCCCCCCCCCC)=O)(=O)O	PG	+",
                @"PI 33:1(d7)|PI 15:0_18:1(d7)	847.604124	8.35	[M+NH4]+	XCKYASHMOHAUQB-OAFUKSMZNA-N	C42H72D7O13P	[C@]([H])(OC(CCCCCCC/C=C\CCCCCC([2H])([2H])C([2H])([2H])C([2H])([2H])[2H])=O)(COP(=O)(O)O[C@@H]1C(O)[C@H](O)C(O)C(O)C1O)COC(CCCCCCCCCCCCCC)=O	PI	294.3615",
                @"LPC 18:1(d7)	529.39934	6.33	[M+H]+	YAMUFBLWGFFICM-HNNXNMBSNA-N	C26H45D7NO7P	C(COP(=O)([O-])OCC[N+](C)(C)C)([H])(O)COC(CCCCCCC/C=C\CCCCCC([2H])([2H])C([2H])([2H])C([2H])([2H])[2H])=O	LPC	-",
                @"LPE 18:1(d7)	487.35238	6.4	[M+H]+	PYVRVRFVLRNJLY-CCLUNVSZNA-N	C23H39D7NO7P	[C@](COP(=O)(O)OCCN)([H])(O)COC(CCCCCCC/C=C\CCCCCC([2H])([2H])C([2H])([2H])C([2H])([2H])[2H])=O	LPE	216.9472",
            });

            var sr = new StringReader(non_numerical_ccs_data);
            var result = TextLibraryParser.TextLibraryReader(sr, out string message);
            var n = non_numerical_ccs_data.Split('\n').Length;
            var expected = new int[] { 1, 2, 4, 6 }.Select(i => $"Error type 2: line {i} includes non-numerical value for CCS information.\r\n");
            foreach (var expect in expected)
            {
                StringAssert.Contains(message, expect);
            }
            Assert.IsNull(result);
        }

        [TestMethod()]
        public void TextLibraryReaderInvalidNegativeMzTest()
        {
            string negative_mz_data = string.Join("\r\n", new string[]
            {
                "NAME\tMZ",
                "PC 33:1(d7)|PC 15:0_18:1(d7)\t753.6134",
                "PE 33:1(d7)|PE 15:0_18:1(d7)\t-711.56647",
                "PS 33:1(d7)|PS 15:0_18:1(d7)\t755.55635",
                "PG 33:1(d7)|PG 15:0_18:1(d7)\t+759.58765",
                "PI 33:1(d7)|PI 15:0_18:1(d7)\t-847.604124",
                "LPC 18:1(d7)\t529.39934",
                "CE 18:1(d7)\t675.648",
            });

            var sr = new StringReader(negative_mz_data);
            var result = TextLibraryParser.TextLibraryReader(sr, out string message);
            var n = negative_mz_data.Split('\n').Length;
            var expected = new int[] { 2, 5 }.Select(i => $"Error type 3: line {i} includes negative value for accurate mass information.\r\n");
            foreach (var expect in expected)
            {
                StringAssert.Contains(message, expect);
            }
            Assert.IsNull(result);
        }

        [TestMethod()]
        public void TextLibraryReaderInvalidNoInfomationTest()
        {
            string negative_mz_data = string.Join("\r\n", new string[]
            {
                "NAME\tMZ",
                "a\t-1.02",
                "b\tc"
            });

            var sr = new StringReader(negative_mz_data);
            var result = TextLibraryParser.TextLibraryReader(sr, out string message);
            var expected = "Error type 4: This library doesn't include suitable information.\r\n";
            StringAssert.Contains(message, expected);
            Assert.IsNull(result);
        }

        [TestMethod()]
        public void TextLibraryReaderInvalidHelpMessageTest()
        {
            string negative_mz_data = string.Join("\r\n", new string[]
            {
                "NAME\tMZ",
                "a\t-1.02",
                "b\tc"
            });

            var sr = new StringReader(negative_mz_data);
            var result = TextLibraryParser.TextLibraryReader(sr, out string message);
            var expected = string.Join("\r\n", new string[] {
                "",
                "You should write the following information as the reference library for post identification method.",
                "First- and second columns are required, and the others are option.",
                "[0]Compound Name\t[1]m/z\t[2]Retention time[min]\t[3]adduct\t[4]inchikey\t[5]formula\t[6]smiles\t[7]ontology\t[8]CCS",
                "Metabolite A\t100.000\t5.0\t[M+H]+\tAAAAAAAAAAAAA-BBBBBBBB\tC6H12O6\tCOCOCOCOC\tSugar\t210.2",
                "Metabolite B\t200.000\t6.0\t[M+H]+\tAAAAAAAAAAAAA-BBBBBBBB\tC6H12O6\tCOCOCOCOC\tSugar\t210.2",
                "Metabolite C\t300.000\t7.0\t[M+H]+\tAAAAAAAAAAAAA-BBBBBBBB\tC6H12O6\tCOCOCOCOC\tSugar\t210.2",
                "Metabolite D\t400.000\t8.0\t[M+H]+\tAAAAAAAAAAAAA-BBBBBBBB\tC6H12O6\tCOCOCOCOC\tSugar\t210.2",
                "Metabolite E\t500.000\t9.0\t[M+H]+\tAAAAAAAAAAAAA-BBBBBBBB\tC6H12O6\tCOCOCOCOC\tSugar\t210.2",
            });
            StringAssert.Contains(message, expected);
            Assert.IsNull(result);
        }

        [TestMethod()]
        public void TextLibraryReaderRemoveQuatationTest()
        {
            string data = string.Join("\r\n", new string[]
            {
                "NAME\tMZ",
                "\"PC 36:2|PC 18:0_18:2(9,12)\"\t753.6134",
            });

            var sr = new StringReader(data);
            var results = TextLibraryParser.TextLibraryReader(sr, out _);
            Assert.AreEqual("PC 36:2|PC 18:0_18:2(9,12)", results[0].Name);
        }


        [TestMethod]
        [DeploymentItem(@"Resources\Parser\txt_invalid_formula.txt", @"Resources\Parser\")]
        [DataRow(@"Resources\Parser\txt_invalid_formula.txt")]
        public void TextLibraryReaderWorks(string library) {
            var results = TextLibraryParser.TextLibraryReader(library, out var error);
            Debug.WriteLine($"Error message: {error}");
            Debug.WriteLine($"Count: {results.Count}");
        }

        [TestMethod]
        [DeploymentItem(@"Resources\Parser\txt_with_blank_end_line.txt", @"Resources\Parser\")]
        [DataRow(@"Resources\Parser\txt_with_blank_end_line.txt", 2)]
        public void SuccessIfReadBlankLineInEnd(string library, int rowCnt)
        {
            var results = TextLibraryParser.TextLibraryReader(library, out var error);
            Assert.AreEqual(string.Empty, error);
            Assert.IsNotNull(results);
            Assert.AreEqual(rowCnt, results.Count);

        }

    }

}