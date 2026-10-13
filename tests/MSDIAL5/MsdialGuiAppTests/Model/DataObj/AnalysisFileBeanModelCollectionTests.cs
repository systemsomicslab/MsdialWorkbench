using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompMs.Common.Enum;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.Model.DataObj.Tests
{
    [TestClass()]
    public class AnalysisFileBeanModelCollectionTests
    {
        [TestMethod()]
        public void RefreshRawDataAvailabilityReturnsTheMissingSamplesInListOrder() {
            var folder = Path.Combine(Path.GetTempPath(), $"msdial-collection-{Guid.NewGuid():N}");
            Directory.CreateDirectory(folder);
            try {
                var present = Path.Combine(folder, "present.mzML");
                File.WriteAllBytes(present, new byte[0]);
                var goneFolder = Path.Combine(folder, "gone");
                var models = new[]
                {
                    CreateAt(Path.Combine(goneFolder, "a.mzML")),
                    CreateAt(present),
                    CreateAt(Path.Combine(folder, "missing.mzML")),
                    CreateAt(Path.Combine(goneFolder, "b.mzML")),
                };
                models[3].AnalysisFileIncluded = false;
                var collection = new AnalysisFileBeanModelCollection(models);

                var missing = collection.RefreshRawDataAvailability();

                CollectionAssert.AreEqual(new[] { models[0], models[2], models[3] }, missing.ToArray());
                Assert.IsFalse(models[0].IsRawDataAvailable);
                Assert.IsTrue(models[1].IsRawDataAvailable);
                Assert.IsFalse(models[3].IsRawDataAvailable);
            }
            finally {
                Directory.Delete(folder, recursive: true);
            }
        }

        private static AnalysisFileBeanModel CreateAt(string path) {
            var model = AnalysisFileBeanModelTestHelper.Create();
            model.AnalysisFilePath = path;
            model.AnalysisFileIncluded = true;
            return model;
        }

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