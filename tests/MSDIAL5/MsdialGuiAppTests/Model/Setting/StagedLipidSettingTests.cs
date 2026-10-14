using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.Model.Setting.Tests
{
    [TestClass()]
    public class StagedLipidSettingTests
    {
        [TestMethod()]
        public void DataBaseSettingModelReadsTheSeedFile() {
            var path = Path.GetTempFileName();
            try {
                File.WriteAllText(path, "Name\tRT\nPC 34:1\t5.0\nPE 36:2\t6.1\n");
                var model = new DataBaseSettingModel(new ParameterBase { TargetOmics = TargetOmics.Lipidomics })
                {
                    DataBasePath = path,
                    DataBaseID = "Seeds",
                    DBSource = DataBaseSource.StagedLipid,
                };

                var db = model.CreateStagedLipidDatabase();

                Assert.AreEqual("Seeds", db.Id);
                CollectionAssert.AreEqual(new[] { "PC 34:1", "PE 36:2" }, db.Seeds.Select(s => s.Name).ToArray());
                Assert.IsInstanceOfType(model.Create(), typeof(StagedLipidDatabase));
            }
            finally {
                File.Delete(path);
            }
        }

        [TestMethod()]
        public void RestoredDataBaseSettingModelKeepsTheDatabase() {
            var parameter = new ParameterBase { TargetOmics = TargetOmics.Lipidomics };
            var database = new StagedLipidDatabase("Seeds", new CompMs.Common.Lipidomics.LipidSeed[0]);

            var model = new DataBaseSettingModel(parameter, database);

            Assert.AreEqual(DataBaseSource.StagedLipid, model.DBSource);
            Assert.AreSame(database, model.CreateStagedLipidDatabase());
        }

        [TestMethod()]
        public void LcmsFactoryCreatesAStagedLipidAnnotatorSetting() {
            var parameter = new ParameterBase { TargetOmics = TargetOmics.Lipidomics };
            var dbModel = new DataBaseSettingModel(parameter) { DataBaseID = "Seeds", DBSource = DataBaseSource.StagedLipid };
            var factory = new LcmsAnnotatorSettingFactory(parameter);

            var annotator = factory.Create(dbModel, "Seeds_1", null) as IStagedLipidAnnotatorSettingModel;

            Assert.IsNotNull(annotator);
            Assert.AreEqual(SourceType.GeneratedLipid, annotator.AnnotationSource);
            var key = annotator.CreateRestorationKey(3);
            Assert.IsInstanceOfType(key, typeof(StagedLipidRestorationKey));
            Assert.AreEqual("Seeds_1", key.Key);
            Assert.AreEqual(3, key.Priority);

            var queryFactory = annotator.CreateAnnotationQueryFactory(3, new StagedLipidDatabase("Seeds", new CompMs.Common.Lipidomics.LipidSeed[0]), new DataBaseMapper());
            Assert.AreEqual("Seeds_1", queryFactory.AnnotatorId);
            Assert.AreEqual(3, queryFactory.Priority);
        }
    }
}
