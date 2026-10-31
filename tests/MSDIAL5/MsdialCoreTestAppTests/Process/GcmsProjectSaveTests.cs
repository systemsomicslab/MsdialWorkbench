using CompMs.App.MsdialConsole.Process;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialGcMsApi.DataObj;
using CompMs.MsdialGcMsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace MsdialCoreTestAppTests.Process;

[TestClass]
public sealed class GcmsProjectSaveTests
{
    [TestMethod]
    public async Task DatasetSaveFailureDoesNotReportSuccessfulProcessing() {
        var folder = Path.Combine(Path.GetTempPath(), "msdial-gc-save-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try {
            var storage = new FaultingStorage {
                MsdialGcmsParameter = new MsdialGcmsParameter {
                    ProjectFolderPath = folder, ProjectFileName = "SaveFailure.mddata", TogetherWithAlignment = false,
                },
                DataBases = DataBaseStorage.CreateEmpty(), DataBaseMapper = new DataBaseMapper(),
            };
            var execute = typeof(GcmsProcess).GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(execute);
            var error = await Assert.ThrowsExceptionAsync<IOException>(async () =>
                await (Task<int>)execute.Invoke(new GcmsProcess(), new object[] { storage, folder, true }));
            StringAssert.Contains(error.Message, "SaveFailure.mddata");
        }
        finally { Directory.Delete(folder, recursive: true); }
    }

    private sealed class FaultingStorage : MsdialGcmsDataStorage
    {
        protected override void SaveMspDB(Stream stream) => throw new IOException("Simulated library write failure.");
    }
}
