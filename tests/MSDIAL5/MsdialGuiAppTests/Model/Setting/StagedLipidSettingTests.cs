using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Lipidomics;
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
        // Keeps the tests off the real user settings and preset folder.
        private static LipidAdductPresetStore TemporaryStore() {
            return new LipidAdductPresetStore(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()), _ => null, (_, __) => { });
        }

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
                    AdductPresetStore = TemporaryStore(),
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
        public void PresetDefaultsToRecentThenUserThenMsdial() {
            var folder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try {
                string recent = null;
                var store = new LipidAdductPresetStore(folder, _ => recent, (_, name) => recent = name);

                Assert.AreEqual(LipidAdductPreset.NegativeHcoohName, store.GetDefault(IonMode.Negative).Name, "nothing used or added: MS-DIAL default");
                Assert.AreEqual(LipidAdductPreset.PositiveName, store.GetDefault(IonMode.Positive).Name);

                Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, "lab.txt"), "# Name: Lab negative\n*\t[M-H]-\n");
                Assert.AreEqual("Lab negative", store.GetDefault(IonMode.Negative).Name, "a user preset comes before the MS-DIAL one");
                Assert.AreEqual(LipidAdductPreset.PositiveName, store.GetDefault(IonMode.Positive).Name, "only presets of the polarity count");
                CollectionAssert.AreEqual(
                    new[] { "Lab negative", LipidAdductPreset.NegativeHcoohName, LipidAdductPreset.NegativeCh3coohName, LipidAdductPreset.NegativeNh4hco3Name },
                    store.GetPresets(IonMode.Negative).Select(p => p.Name).ToArray());

                store.MarkUsed(LipidAdductPreset.BuiltIn.Single(p => p.Name == LipidAdductPreset.NegativeCh3coohName), IonMode.Negative);
                Assert.AreEqual(LipidAdductPreset.NegativeCh3coohName, store.GetDefault(IonMode.Negative).Name, "the last used one wins");
            }
            finally {
                if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
            }
        }

        [TestMethod()]
        public void ImportedPresetIsCopiedIntoTheUserFolder() {
            var folder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var source = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".txt");
            try {
                File.WriteAllText(source, "# Name: Shared\n*\t[M+H]+\n");
                var store = new LipidAdductPresetStore(folder, _ => null, (_, __) => { });

                var preset = store.Import(source, out var errors);

                Assert.AreEqual("Shared", preset.Name);
                Assert.AreEqual(0, errors.Count);
                Assert.IsTrue(File.Exists(Path.Combine(folder, Path.GetFileName(source))));
                Assert.IsTrue(store.GetPresets(IonMode.Positive).Any(p => p.Name == "Shared"));
            }
            finally {
                File.Delete(source);
                if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
            }
        }

        [TestMethod()]
        public void NewDatabaseAppliesTheDefaultPresetAndRemembersIt() {
            var parameter = new ParameterBase { TargetOmics = TargetOmics.Lipidomics };
            parameter.ProjectParam.IonMode = IonMode.Negative;
            string recent = null;
            var path = Path.GetTempFileName();
            try {
                File.WriteAllText(path, "Name\nPC 34:1\n");
                var model = new DataBaseSettingModel(parameter)
                {
                    DataBasePath = path,
                    DataBaseID = "Seeds",
                    DBSource = DataBaseSource.StagedLipid,
                    AdductPresetStore = new LipidAdductPresetStore(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()), _ => recent, (_, name) => recent = name),
                };

                var db = model.CreateStagedLipidDatabase();

                Assert.AreEqual(LipidAdductPreset.NegativeHcoohName, db.AdductPresetName);
                Assert.IsTrue(db.AllowedAdducts.Any(r => r.Adduct == "[M+HCOO]-"));
                Assert.IsFalse(db.AllowedAdducts.Any(r => r.Adduct == "[M+CH3COO]-"));
                Assert.AreEqual(LipidAdductPreset.NegativeHcoohName, recent);
            }
            finally {
                File.Delete(path);
            }
        }

        [TestMethod()]
        public void RestoredDatabaseKeepsItsAdductsUntilThePresetIsChanged() {
            var parameter = new ParameterBase { TargetOmics = TargetOmics.Lipidomics };
            parameter.ProjectParam.IonMode = IonMode.Negative;
            var database = new StagedLipidDatabase("Seeds", new CompMs.Common.Lipidomics.LipidSeed[0]);
            database.ApplyAdductPreset(LipidAdductPreset.BuiltIn.Single(p => p.Name == LipidAdductPreset.NegativeNh4hco3Name));
            database.AllowedAdducts.RemoveAt(0); // stands for a preset file edited since the project was built
            var stored = database.AllowedAdducts.Count;
            var model = new DataBaseSettingModel(parameter, database) { AdductPresetStore = new LipidAdductPresetStore(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()), _ => null, (_, __) => { }) };

            Assert.AreEqual(LipidAdductPreset.NegativeNh4hco3Name, model.AdductPreset.Name);
            model.CreateStagedLipidDatabase();
            Assert.AreEqual(stored, database.AllowedAdducts.Count, "not re-applied while unchanged");

            model.AdductPreset = LipidAdductPreset.BuiltIn.Single(p => p.Name == LipidAdductPreset.NegativeHcoohName);
            model.CreateStagedLipidDatabase();
            Assert.AreEqual(LipidAdductPreset.NegativeHcoohName, database.AdductPresetName);
            Assert.IsTrue(database.AllowedAdducts.Any(r => r.Adduct == "[M+HCOO]-"));
        }

        [TestMethod()]
        public void RestoredDataBaseSettingModelKeepsTheDatabase() {
            var parameter = new ParameterBase { TargetOmics = TargetOmics.Lipidomics };
            var database = new StagedLipidDatabase("Seeds", new CompMs.Common.Lipidomics.LipidSeed[0]);

            var model = new DataBaseSettingModel(parameter, database) { AdductPresetStore = TemporaryStore() };

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
