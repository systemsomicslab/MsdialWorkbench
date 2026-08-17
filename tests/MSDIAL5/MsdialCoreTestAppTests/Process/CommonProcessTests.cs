using CompMs.App.MsdialConsole.Process;
using CompMs.Common.Enum;
using CompMs.MsdialLcmsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace MsdialCoreTestAppTests.Process;

[TestClass]
public sealed class CommonProcessTests
{
    [TestMethod]
    public void SetProjectProperty_PreservesPerFileAcquisitionTypesForCsvInput()
    {
        using var directory = new TemporaryDirectory();
        var dda = directory.CreateFile("dda.mzML");
        var swath = directory.CreateFile("swath.mzML");
        var csv = directory.CreateFile(
            "analysis_files.csv",
            $"""
            file_path,file_name,file_type,class_id,acquisition_type,batch_order,analytical_order,factor
            {dda},dda,Sample,DDA,DDA,1,1,1
            {swath},swath,Sample,SWATH,SWATH,1,2,1
            """);
        var parameter = new MsdialLcmsParameter();
#pragma warning disable CS0618
        parameter.ProjectParam.AcquisitionType = AcquisitionType.AIF;
#pragma warning restore CS0618

        var result = CommonProcess.SetProjectProperty(parameter, csv, out var files, out _);

        Assert.IsTrue(result);
        CollectionAssert.AreEqual(
            new[] { AcquisitionType.DDA, AcquisitionType.SWATH },
            files.Select(file => file.AcquisitionType).ToArray());
    }

    [TestMethod]
    public void SetProjectProperty_AppliesProjectAcquisitionTypeForFolderInput()
    {
        using var directory = new TemporaryDirectory();
        directory.CreateFile("first.mzML");
        directory.CreateFile("second.mzML");
        var parameter = new MsdialLcmsParameter();
#pragma warning disable CS0618
        parameter.ProjectParam.AcquisitionType = AcquisitionType.AIF;
#pragma warning restore CS0618

        var result = CommonProcess.SetProjectProperty(parameter, directory.Path, out var files, out _);

        Assert.IsTrue(result);
        Assert.IsTrue(files.Count > 0);
        Assert.IsTrue(files.All(file => file.AcquisitionType == AcquisitionType.AIF));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "MsdialCoreTestAppTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string CreateFile(string name, string content = "")
        {
            var path = System.IO.Path.Combine(Path, name);
            File.WriteAllText(path, content);
            return path;
        }

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
