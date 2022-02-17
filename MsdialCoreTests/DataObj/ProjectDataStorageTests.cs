using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.DataObj.Tests
{
    [TestClass()]
    public class ProjectDataStorageTests
    {
        [TestMethod()]
        public async Task LoadTest() {
            var projectParameter = new ProjectParameter(DateTime.Parse("2021/12/16 23:14:30").ToUniversalTime(), "Folder", "TestProject");
            var proj = new ProjectDataStorage(projectParameter, new List<IMsdialDataStorage<ParameterBase>>());
            var storage1 = new MockStorage { Parameter = new ParameterBase { ProjectFileName = "Test1", } };
            var storage2 = new MockStorage { Parameter = new ParameterBase { ProjectFileName = "Test2", } };
            var storage3 = new MockStorage { Parameter = new ParameterBase { ProjectFileName = "Test3", } };
            proj.AddStorage(storage1);
            proj.AddStorage(storage2);
            proj.AddStorage(storage3);

            var serializer = new MockSerializer();
            var stream = new MemoryStream();
            using (var streamManager = ZipStreamManager.OpenCreate(stream)) {
                await proj.Save(streamManager, serializer);
            }

            using (var streamManager = ZipStreamManager.OpenGet(stream)) {
                var actual = await ProjectDataStorage.Load(streamManager, serializer);

                Assert.AreEqual(projectParameter.StartDate, actual.ProjectParameter.StartDate);
                Assert.AreEqual(projectParameter.FinalSavedDate, actual.ProjectParameter.FinalSavedDate);
                Assert.AreEqual(projectParameter.FolderPath, actual.ProjectParameter.FolderPath);
                Assert.AreEqual(projectParameter.Title, actual.ProjectParameter.Title);
                CollectionAssert.AreEqual(proj.ProjectPaths, actual.ProjectPaths);
                Assert.AreEqual(proj.Storages.Count, actual.Storages.Count);
                foreach ((var exp, var act) in proj.Storages.Zip(actual.Storages)) {
                    Assert.AreEqual(exp.Parameter.ProjectFileName, act.Parameter.ProjectFileName);
                }
            }
        }
    }

    class MockStorage : IMsdialDataStorage<ParameterBase> {
        public List<AnalysisFileBean> AnalysisFiles { get; set; }
        public List<AlignmentFileBean> AlignmentFiles { get; set; }
        public List<MoleculeMsReference> MspDB { get; set; }
        public List<MoleculeMsReference> TextDB { get; set; }
        public List<MoleculeMsReference> IsotopeTextDB { get; set; }
        public IupacDatabase IupacDatabase { get; set; }

        public ParameterBase Parameter { get; set; }

        public DataBaseMapper DataBaseMapper { get; set; }
        public DataBaseStorage DataBases { get; set; }

        public Task Save(IStreamManager streamManager, string projectTitle, string prefix) {
            throw new NotImplementedException();
        }
    }

    class MockSerializer : IMsdialSerializer
    {
        // private List<IMsdialDataStorage<ParameterBase>> storages = new List<IMsdialDataStorage<ParameterBase>>();
        private Dictionary<string, IMsdialDataStorage<ParameterBase>> storages = new Dictionary<string, IMsdialDataStorage<ParameterBase>>();
        public Task<IMsdialDataStorage<ParameterBase>> LoadAsync(IStreamManager streamManager, string projectTitle, string projectFolderPath, string prefix) {
            var storage = storages[$"{prefix}/{projectTitle}"];
            storages.Remove(projectTitle);
            return Task.FromResult(storage);
        }

        public Task SaveAsync(IMsdialDataStorage<ParameterBase> dataStorage, IStreamManager streamManager, string projectTitle, string prefix) {
            storages.Add($"{prefix}/{projectTitle}", dataStorage);
            return Task.CompletedTask;
        }
    }
}