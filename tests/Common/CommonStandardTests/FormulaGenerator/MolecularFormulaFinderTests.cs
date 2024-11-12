using CompMs.Common.FormulaGenerator.Parser;
using CompMs.Common.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.FormulaGenerator.Tests
{
    [TestClass()]
    public class MolecularFormulaFinderTests
    {
        [TestMethod()]
        [DynamicData(nameof(testDatas), DynamicDataSourceType.Property)]
        [DeploymentItem(@"Resources\FormulaGenerator\test_data1.msp", @"Resources\FormulaGenerator\")]
        [DeploymentItem(@"Resources\FormulaGenerator\ProductIonLib_vs1.pid", @"Resources\FormulaGenerator\")]
        [DeploymentItem(@"Resources\FormulaGenerator\NeutralLossDB_vs2.ndb", @"Resources\FormulaGenerator\")]
        [DeploymentItem(@"Resources\FormulaGenerator\MsfinderFormulaDB-VS13.efd", @"Resources\FormulaGenerator\")]
        public void GetMolecularFormulaListTest(List<(string, double)> expectedResults)
        {
            var rawDataPath = @"Resources\FormulaGenerator\test_data1.msp";
            var productIonDB = FragmentDbParser.GetProductIonDB(@"Resources\FormulaGenerator\ProductIonLib_vs1.pid", out string _);
            var neutralLossDB = FragmentDbParser.GetNeutralLossDB(@"Resources\FormulaGenerator\NeutralLossDB_vs2.ndb", out string _);
            var existFormulaDB = ExistFormulaDbParcer.ReadExistFormulaDB(@"Resources\FormulaGenerator\MsfinderFormulaDB-VS13.efd", out string _);

            var param = new AnalysisParamOfMsfinder();
            var rawData = RawDataParcer.RawDataFileReader(rawDataPath, param);

            var formulaResults = MolecularFormulaFinder.GetMolecularFormulaList(productIonDB, neutralLossDB, existFormulaDB, rawData, param);

            List<(string, double)> results = formulaResults.Select(f => (f.Formula.FormulaString, f.TotalScore)).ToList();

            CollectionAssert.AreEquivalent(results, expectedResults);
        }

        public static IEnumerable<object[]> testDatas
        {
            get
            {
                yield return new object[]
                {
                    new List<(string, double)>{
                        ("C33H40N2O9", 4.29),
                        ("C17H50N6O11P2S", 3.28),
                        ("C18H44N10O9S2", 3.279),
                        ("C26H48N4O6S3", 3.257),
                        ("C10H40N16O12S", 3.233),
                        ("C12H47N14O6P3S", 3.231),
                        ("C13H49N14O3PS4", 3.212),
                        ("C20H41N12O6PS", 3.21),
                        ("C28H37N10O4P", 3.21),
                        ("C28H45N6O3PS2", 3.207),
                        ("C13H41N18O4PS2", 3.2),
                        ("C19H47N8O8P3", 3.187),
                        ("C18H52N6O8S4", 3.178),
                        ("C18H36N14O10", 3.161),
                        ("C19H64N2O2S8", 3.145),
                        ("C12H37N18O9P", 3.137),
                        ("C19H56N6O3S6", 3.13),
                        ("C25H44N4O11S", 3.102),
                        ("C25H46N4O9P2", 3.046),
                        ("C26H40N8O7S", 2.999),
                        ("C20H49N8O5PS3", 2.986),
                        ("C17H40N10O14", 2.986),
                        ("C28H53N2O2PS4", 2.965),
                        ("C11H44N16O7S3", 2.957),
                        ("C15H28N24O4", 2.956),
                        ("C18H60N2O7S6", 2.922),
                        ("C13H33N22O5P", 2.918),
                        ("C23H32N18OS", 2.913),
                        ("C12H45N14O8PS2", 2.876),
                        ("C16H24N28", 2.864),
                        ("C34H44N2O4S2", 2.858),
                        ("C19H48N10O4S4", 2.85),
                        ("C21H37N16O2PS", 2.7),
                        ("C25H54O8P2S2", 2.563),
                        ("C26H56O5S5", 2.544),
                        ("C24H50O13P2", 2.362),
                        ("C35H45O5PS", 2.273),
                        ("C27H60S7", 1.981)
                    },
                };
            }
        }
    }
}