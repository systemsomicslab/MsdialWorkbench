using CompMs.Common.Enum;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.Algorithm.MassCalibration;
using CompMs.MsdialCore.Enum;
using MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.DataObj.Tests
{
    /// <summary>
    /// The mass recalibration is recorded as MessagePack key 20 of <see cref="AnalysisFileBean"/>. A project written
    /// before it must read as no recalibration, and an MS-DIAL that knows only keys 0-19 must still open a project that
    /// has one, as <see cref="AnalysisFileBeanRawFileFormatTests"/> pins for key 19.
    /// </summary>
    [TestClass()]
    public class AnalysisFileBeanMassRecalibrationTests
    {
        [TestMethod()]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11.mddata", @"Resources\Export")]
        public void AProjectWrittenBeforeTheRecalibrationWasRecordedReadsAsNone() {
            var bytes = File.ReadAllBytes(Path.Combine("Resources", "Export", "Dataset_2025_07_31_12_31_11.mddata"));

            var storage = LZ4MessagePackSerializer.Deserialize<MsdialDataStorage>(bytes);

            Assert.IsTrue(storage.AnalysisFiles.Count > 0);
            Assert.IsTrue(storage.AnalysisFiles.All(file => file.MassRecalibration is null));
        }

        [TestMethod()]
        public void ARecalibrationSurvivesSavingAndLoading() {
            var bean = CreateBean(0);

            using var stream = new MemoryStream();
            MessagePackDefaultHandler.SaveToStream(bean, stream);
            stream.Position = 0;
            var read = MessagePackDefaultHandler.LoadFromStream<AnalysisFileBean>(stream);

            Assert.AreEqual(-3.04d, read.MassRecalibration!.Ms1!.ErrorAtReferencePpm);
            Assert.AreEqual(MassErrorTerms.Offset | MassErrorTerms.InverseSqrtMz | MassErrorTerms.Window, read.MassRecalibration.Ms2!.Terms);
            Assert.AreEqual(600d, read.MassRecalibration.Ms2.InverseSqrtMzCoefficient);
            Assert.AreEqual(12.5d, read.MassRecalibration.Ms2.Windows.Single().OffsetPpm);
            Assert.AreEqual("1", read.MassRecalibration.AlgorithmVersion);
            Assert.AreEqual(RawFileFormat.MzML, read.RawFileFormat);
        }

        /// <summary>
        /// An MS-DIAL that knows keys 0-19 opens a project written with key 20. The beans sit in a list with more data after
        /// it, so a reader that skipped key 20 by the wrong length would misread what follows.
        /// </summary>
        [TestMethod()]
        public void AnOlderMsdialReadsAProjectWithARecalibrationRecorded() {
            var written = new BeansThenMore
            {
                AnalysisFiles = new List<AnalysisFileBean> { CreateBean(0), CreateBean(1), },
                After = "after the beans",
            };
            var stream = new MemoryStream();
            MessagePackDefaultHandler.SaveToStream(written, stream);

            stream.Position = 0;
            var read = MessagePackDefaultHandler.LoadFromStream<OldBeansThenMore>(stream);

            Assert.AreEqual(2, read.AnalysisFiles.Count);
            Assert.AreEqual("after the beans", read.After);
            for (int i = 0; i < 2; i++) {
                Assert.AreEqual(written.AnalysisFiles[i].AnalysisFileName, read.AnalysisFiles[i].AnalysisFileName);
                Assert.AreEqual(written.AnalysisFiles[i].RawFileFormat, read.AnalysisFiles[i].RawFileFormat);
            }
        }

        private static AnalysisFileBean CreateBean(int id) {
            return new AnalysisFileBean
            {
                AnalysisFilePath = $@"D:\data\sample_{id}.mzML",
                AnalysisFileName = $"sample_{id}",
                AnalysisFileId = id,
                AcquisitionType = AcquisitionType.SWATH,
                RawFileFormat = RawFileFormat.MzML,
                MassRecalibration = new MassRecalibrationBean
                {
                    Ms1 = new MassErrorModelBean { Terms = MassErrorTerms.Offset, ErrorAtReferencePpm = -3.04d, ReferenceMz = 700d, ReferenceRetentionTime = 10d, MinimumMz = 200d, MaximumMz = 1200d, MaximumRetentionTime = 20d },
                    Ms2 = new MassErrorModelBean
                    {
                        Terms = MassErrorTerms.Offset | MassErrorTerms.InverseSqrtMz | MassErrorTerms.Window,
                        ErrorAtReferencePpm = -25d,
                        InverseSqrtMzCoefficient = 600d,
                        ReferenceMz = 400d,
                        MinimumMz = 100d,
                        MaximumMz = 900d,
                        Windows = new List<MassErrorWindowBean> { new MassErrorWindowBean { LowerMz = 300d, UpperMz = 325d, OffsetPpm = 12.5d } },
                    },
                    AlgorithmVersion = MassCalibrationDecision.AlgorithmVersion,
                },
            };
        }

        /// <summary><see cref="AnalysisFileBean"/> as an MS-DIAL without key 20 declares it.</summary>
        [MessagePackObject]
        public class AnalysisFileBeanWithoutMassRecalibration
        {
            [Key(0)] public string AnalysisFilePath { get; set; } = string.Empty;
            [Key(1)] public string AnalysisFileName { get; set; } = string.Empty;
            [Key(2)] public AnalysisFileType AnalysisFileType { get; set; }
            [Key(3)] public string AnalysisFileClass { get; set; } = string.Empty;
            [Key(4)] public int AnalysisFileAnalyticalOrder { get; set; }
            [Key(5)] public int AnalysisFileId { get; set; }
            [Key(6)] public bool AnalysisFileIncluded { get; set; }
            [Key(7)] public string DeconvolutionFilePath { get; set; } = string.Empty;
            [Key(8)] public List<string> DeconvolutionFilePathList { get; set; } = new List<string>();
            [Key(9)] public string PeakAreaBeanInformationFilePath { get; set; } = string.Empty;
            [Key(10)] public string RiDictionaryFilePath { get; set; } = string.Empty;
            [Key(11)] public int AnalysisBatch { get; set; } = 1;
            [Key(12)] public double ResponseVariable { get; set; } = 0;
            [Key(13)] public double DilutionFactor { get; set; } = 1.0;
            [Key(14)] public string AnalysisFileSuperClass { get; set; } = string.Empty;
            [Key(15)] public RetentionTimeCorrectionBean RetentionTimeCorrectionBean { get; set; } = new RetentionTimeCorrectionBean();
            [Key(16)] public ChromatogramPeaksDataSummaryDto ChromPeakFeaturesSummary { get; set; } = new ChromatogramPeaksDataSummaryDto();
            [Key(17)] public string ProteinAssembledResultFilePath { get; set; }
            [Key(18)] public AcquisitionType AcquisitionType { get; set; } = AcquisitionType.None;
            [Key(19)] public RawFileFormat RawFileFormat { get; set; }
        }

        [MessagePackObject]
        public class BeansThenMore
        {
            [Key(0)] public List<AnalysisFileBean> AnalysisFiles { get; set; } = new List<AnalysisFileBean>();
            [Key(1)] public string After { get; set; } = string.Empty;
        }

        [MessagePackObject]
        public class OldBeansThenMore
        {
            [Key(0)] public List<AnalysisFileBeanWithoutMassRecalibration> AnalysisFiles { get; set; } = new List<AnalysisFileBeanWithoutMassRecalibration>();
            [Key(1)] public string After { get; set; } = string.Empty;
        }
    }
}
