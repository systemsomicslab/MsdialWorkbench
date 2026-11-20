using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;

namespace CompMs.MsdialCore.DataObj.Tests
{
    [TestClass()]
    public class FixDatasetFolderTests
    {
        private string _root;

        [TestInitialize]
        public void Setup() {
            _root = Path.Combine(Path.GetTempPath(), $"msdial-fix-dataset-folder-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_root);
        }

        [TestCleanup]
        public void Cleanup() {
            if (Directory.Exists(_root)) {
                Directory.Delete(_root, recursive: true);
            }
        }

        [TestMethod()]
        public void PathInsideTheStoredFolderMovesWithTheProject() {
            var actual = MsdialDataStorageBase.ReplaceFolderPath(@"C:\old\project\sample1.dcl", @"C:\old\project", @"D:\new\project");
            Assert.AreEqual(@"D:\new\project\sample1.dcl", actual);
        }

        [TestMethod()]
        public void RelativePathIsTakenFromTheNewFolder() {
            var actual = MsdialDataStorageBase.ReplaceFolderPath("sample1.dcl", @"C:\old\project", @"D:\new\project");
            Assert.AreEqual(@"D:\new\project\sample1.dcl", actual);
        }

        [TestMethod()]
        public void SiblingFolderSharingThePrefixIsNotInsideTheStoredFolder() {
            var actual = MsdialDataStorageBase.ReplaceFolderPath(@"C:\old\project2\fames.txt", @"C:\old\project", @"D:\new\project");
            Assert.AreEqual(@"C:\old\project2\fames.txt", actual);
        }

        [TestMethod()]
        public void ExistingFileOutsideTheStoredFolderIsKept() {
            var ri = Path.Combine(_root, "ri", "fames_rt_info.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(ri));
            File.WriteAllText(ri, "C8\t3.5");
            var newFolder = Path.Combine(_root, "moved");
            Directory.CreateDirectory(newFolder);

            var actual = MsdialDataStorageBase.ReplaceFolderPath(ri, Path.Combine(_root, "input"), newFolder);

            Assert.AreEqual(ri, actual);
        }

        [TestMethod()]
        public void LinuxPathOutsideTheStoredFolderBecomesTheFileOfThatNameInTheNewFolder() {
            var newFolder = Path.Combine(_root, "copy");
            Directory.CreateDirectory(newFolder);
            File.WriteAllText(Path.Combine(newFolder, "fames_rt_info.txt"), "C8\t3.5");

            var actual = MsdialDataStorageBase.ReplaceFolderPath("/slurm-storage/run/ri/fames_rt_info.txt", "/slurm-storage/run/input", newFolder);

            Assert.AreEqual(Path.Combine(newFolder, "fames_rt_info.txt"), actual);
        }

        [TestMethod()]
        public void PathFoundNowhereIsKeptAsStored() {
            var actual = MsdialDataStorageBase.ReplaceFolderPath("/slurm-storage/run/ri/fames_rt_info.txt", "/slurm-storage/run/input", _root);
            Assert.AreEqual("/slurm-storage/run/ri/fames_rt_info.txt", actual);
        }

        [TestMethod()]
        public void LinuxProjectWithItsRiFileElsewhereOpensFromACopy() {
            // A GC-MS project from the Linux Console, whose FAME file was outside the input folder, copied to Windows.
            var copy = Path.Combine(_root, "copy");
            Directory.CreateDirectory(copy);
            var storage = new MsdialDataStorage
            {
                ParameterBase = new ParameterBase { ProjectFolderPath = "/slurm-storage/run/input", ProjectFileName = "/slurm-storage/run/input/run.mddata", },
                AnalysisFiles = new List<AnalysisFileBean>
                {
                    new AnalysisFileBean
                    {
                        AnalysisFileId = 0,
                        AnalysisFilePath = "/slurm-storage/run/input/sample1.cdf",
                        DeconvolutionFilePath = "/slurm-storage/run/input/sample1.dcl",
                        PeakAreaBeanInformationFilePath = "/slurm-storage/run/input/sample1.pai2",
                        RiDictionaryFilePath = "/slurm-storage/run/ri/fames_rt_info.txt",
                        DeconvolutionFilePathList = new List<string> { "/slurm-storage/run/input/sample1.dcl", },
                    },
                },
                AlignmentFiles = new List<AlignmentFileBean>
                {
                    new AlignmentFileBean { FilePath = "/slurm-storage/run/input/AlignmentResult.arf2", EicFilePath = "/slurm-storage/run/input/AlignmentResult.EIC.aef", SpectraFilePath = "/slurm-storage/run/input/AlignmentResult.dcl", },
                },
            };
            storage.ParameterBase.RefSpecMatchBaseParam.FileIdRiInfoDictionary[0] = new RiDictionaryInfo { DictionaryFilePath = "/slurm-storage/run/ri/fames_rt_info.txt", };

            ((IMsdialDataStorage<ParameterBase>)storage).FixDatasetFolder(copy);

            var file = storage.AnalysisFiles[0];
            Assert.AreEqual(copy, storage.ParameterBase.ProjectFolderPath);
            Assert.AreEqual(Path.Combine(copy, "sample1.cdf"), file.AnalysisFilePath);
            Assert.AreEqual(Path.Combine(copy, "sample1.dcl"), file.DeconvolutionFilePath);
            Assert.AreEqual(Path.Combine(copy, "sample1.pai2"), file.PeakAreaBeanInformationFilePath);
            Assert.AreEqual("/slurm-storage/run/ri/fames_rt_info.txt", file.RiDictionaryFilePath);
            Assert.AreEqual(Path.Combine(copy, "AlignmentResult.arf2"), storage.AlignmentFiles[0].FilePath);
        }
    }
}
