using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompMs.Common.Enum;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.DataObj.Tests
{
    [TestClass()]
    public class AnalysisFileBeanModelCollectionTests
    {
        [TestMethod()]
        [DynamicData(nameof(OrderUniqueTestData), DynamicDataSourceType.Property)]
        public void UniqueAnalyticalOrderTest(bool condition, AnalysisFileBeanModel[] models) {
            var collection = new AnalysisFileBeanModelCollection(models);
            Assert.AreEqual(condition, collection.IsAnalyticalOrderUnique.Value);
        }

        private static IEnumerable<object[]> OrderUniqueTestData {
            get {
                yield return new object[] {
                    true,
                    new[] {
                        Create(1, 1, AnalysisFileType.QC),
                        Create(1, 2, AnalysisFileType.QC),
                        Create(1, 3, AnalysisFileType.QC),
                        Create(1, 4, AnalysisFileType.QC),
                    } };
                yield return new object[] {
                    false,
                    new[] {
                        Create(1, 1, AnalysisFileType.QC),
                        Create(1, 2, AnalysisFileType.QC),
                        Create(1, 2, AnalysisFileType.QC),
                        Create(1, 4, AnalysisFileType.QC),
                    } };
                yield return new object[] {
                    true,
                    new[] {
                        Create(1, 1, AnalysisFileType.QC),
                        Create(1, 2, AnalysisFileType.QC),
                        Create(2, 2, AnalysisFileType.QC),
                        Create(2, 4, AnalysisFileType.QC),
                    } };
            }
        }

        [TestMethod()]
        [DynamicData(nameof(QualityCheckTestData), DynamicDataSourceType.Property)]
        public void QualityCheckConditionTest(bool condition, AnalysisFileBeanModel[] models) {
            var collection = new AnalysisFileBeanModelCollection(models);
            Assert.AreEqual(condition, collection.AreFirstAndLastQualityCheck.Value);
        }

        private static IEnumerable<object[]> QualityCheckTestData {
            get {
                yield return new object[] {
                    true,
                    new[] {
                        Create(1, 1, AnalysisFileType.QC),
                        Create(1, 2, AnalysisFileType.QC),
                    } };
                yield return new object[] {
                    false,
                    new[] {
                        Create(1, 1, AnalysisFileType.QC),
                        Create(1, 2, AnalysisFileType.QC),
                        Create(1, 3, AnalysisFileType.Sample),
                    } };
                yield return new object[] {
                    false,
                    new[] {
                        Create(1, 1, AnalysisFileType.Blank),
                        Create(1, 2, AnalysisFileType.QC),
                        Create(1, 3, AnalysisFileType.Sample),
                        Create(1, 4, AnalysisFileType.QC),
                    } };
                yield return new object[] {
                    false,
                    new[] {
                        Create(1, 1, AnalysisFileType.QC),
                        Create(2, 2, AnalysisFileType.Sample),
                        Create(1, 3, AnalysisFileType.QC),
                        Create(2, 4, AnalysisFileType.QC),
                    } };
            }
        }

        private static AnalysisFileBeanModel Create(int batch, int order, AnalysisFileType type) {
            var model = AnalysisFileBeanModelTestHelper.Create();
            model.AnalysisBatch = batch;
            model.AnalysisFileAnalyticalOrder = order;
            model.AnalysisFileType = type;
            model.AnalysisFileIncluded = true;
            return model;
        }
    }
}