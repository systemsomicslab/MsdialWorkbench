using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.FormulaGenerator.Parser;
using CompMs.Common.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Linq;

namespace CompMs.Common.FormulaGenerator.Tests
{
    /// <summary>
    /// <see cref="AnalysisParamOfMsfinder.FormulaPredictionTimeOut"/> bounds the formula search of one spectrum.
    /// </summary>
    [TestClass]
    public class FormulaPredictionTimeOutTests
    {
        private const string ResourceDir = @"Resources\FormulaGenerator\";

        [TestMethod]
        [DeploymentItem(ResourceDir + "test_data1.msp", ResourceDir)]
        [DeploymentItem(ResourceDir + "ProductIonLib_vs1.pid", ResourceDir)]
        [DeploymentItem(ResourceDir + "NeutralLossDB_vs2.ndb", ResourceDir)]
        [DeploymentItem(ResourceDir + "MsfinderFormulaDB-VS13.efd", ResourceDir)]
        public void TimeOutBoundsTheSearchOfOneSpectrumTest()
        {
            var productIonDB = FragmentDbParser.GetProductIonDB(ResourceDir + "ProductIonLib_vs1.pid", out string _);
            var neutralLossDB = FragmentDbParser.GetNeutralLossDB(ResourceDir + "NeutralLossDB_vs2.ndb", out string _);
            var existFormulaDB = ExistFormulaDbParcer.ReadExistFormulaDB(ResourceDir + "MsfinderFormulaDB-VS13.efd", out string _);

            // A doubly charged precursor of about 1362 Da with every halogen allowed: the full search takes many
            // minutes even on 64 threads.
            var param = new AnalysisParamOfMsfinder
            {
                MassTolType = MassToleranceType.Ppm,
                Mass1Tolerance = 10,
                Mass2Tolerance = 20,
                IsElementProbabilityCheck = false,
                IsFcheck = true,
                IsClCheck = true,
                IsBrCheck = true,
                IsIcheck = true,
                FormulaPredictionTimeOut = 0.02, // 1.2 s
            };
            var rawData = RawDataParcer.RawDataFileReader(ResourceDir + "test_data1.msp", param);
            rawData.PrecursorMz = 682.36371;
            rawData.PrecursorType = "[M+2H]2+";

            var stopwatch = Stopwatch.StartNew();
            var results = MolecularFormulaFinder.GetMolecularFormulaList(productIonDB, neutralLossDB, existFormulaDB, rawData, param);
            stopwatch.Stop();

            // The search stops within milliseconds of the time-out; the margin is for slow or busy test machines.
            Assert.IsTrue(stopwatch.Elapsed.TotalSeconds < 1.2 + 10.0, $"{stopwatch.Elapsed.TotalSeconds:F1} s");
            Assert.IsTrue(results.Count <= param.FormulaMaximumReportNumber);
            Assert.IsTrue(results.All(r => r.ProductIonResult != null && r.NeutralLossResult != null));
        }

        [TestMethod]
        [DeploymentItem(ResourceDir + "test_data1.msp", ResourceDir)]
        [DeploymentItem(ResourceDir + "ProductIonLib_vs1.pid", ResourceDir)]
        [DeploymentItem(ResourceDir + "NeutralLossDB_vs2.ndb", ResourceDir)]
        [DeploymentItem(ResourceDir + "MsfinderFormulaDB-VS13.efd", ResourceDir)]
        public void SearchThatFinishesBeforeTheTimeOutIsUnchangedTest()
        {
            var productIonDB = FragmentDbParser.GetProductIonDB(ResourceDir + "ProductIonLib_vs1.pid", out string _);
            var neutralLossDB = FragmentDbParser.GetNeutralLossDB(ResourceDir + "NeutralLossDB_vs2.ndb", out string _);
            var existFormulaDB = ExistFormulaDbParcer.ReadExistFormulaDB(ResourceDir + "MsfinderFormulaDB-VS13.efd", out string _);

            var param = new AnalysisParamOfMsfinder { FormulaMaximumReportNumber = int.MaxValue, FormulaPredictionTimeOut = -1 };
            var rawData = RawDataParcer.RawDataFileReader(ResourceDir + "test_data1.msp", param);
            var expected = MolecularFormulaFinder.GetMolecularFormulaList(productIonDB, neutralLossDB, existFormulaDB, rawData, param)
                .Select(describe).ToList();

            param.FormulaPredictionTimeOut = 60;
            var actual = MolecularFormulaFinder.GetMolecularFormulaList(productIonDB, neutralLossDB, existFormulaDB, rawData, param)
                .Select(describe).ToList();

            Assert.IsTrue(expected.Count > 0);
            CollectionAssert.AreEqual(expected, actual);
        }

        private static string describe(FormulaResult r)
        {
            return string.Join("|", r.Formula.FormulaString, r.TotalScore.ToString("R"), r.IsSelected, r.ProductIonNum, r.ProductIonHits,
                r.NeutralLossNum, r.NeutralLossHits, r.ProductIonResult.Count, r.NeutralLossResult.Count);
        }
    }
}
