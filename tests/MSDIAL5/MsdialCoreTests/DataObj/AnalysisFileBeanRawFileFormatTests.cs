using CompMs.Common.Enum;
using CompMs.Common.MessagePack;
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
    /// The raw data format is recorded as MessagePack key 19 of <see cref="AnalysisFileBean"/>. Projects written before
    /// it must still load and read as <see cref="RawFileFormat.Unknown"/>, and an MS-DIAL that knows only keys 0-18
    /// (5.5, and the 5.6 line at the time of writing) must still open a project that has it.
    /// </summary>
    /// <remarks>
    /// Project IO goes through <see cref="MessagePackDefaultHandler"/> (LZ4 over the stock resolver), where an int-keyed
    /// [MessagePackObject] is an array indexed by key: a shorter array is read to its own length, and the elements past
    /// the keys a reader knows are skipped. These tests pin both directions for this type, as
    /// <see cref="MsScanMatchResultBackwardCompatibilityTests"/> does for match results.
    /// </remarks>
    [TestClass()]
    public class AnalysisFileBeanRawFileFormatTests
    {
        [TestMethod()]
        public void EveryFormatSurvivesSavingAndLoading() {
            foreach (var format in AllFormats()) {
                var bean = new AnalysisFileBean { AnalysisFileName = "sample", RawFileFormat = format, };

                var actual = RoundTrip(bean);

                Assert.AreEqual(format, actual.RawFileFormat);
                Assert.AreEqual("sample", actual.AnalysisFileName);
            }
        }

        [TestMethod()]
        public void ANewBeanRecordsNothing() {
            Assert.AreEqual(RawFileFormat.Unknown, new AnalysisFileBean().RawFileFormat);
        }

        /// <summary>
        /// A dataset saved on 2025-07-31, before key 19 existed: every file reads as Unknown, and keys 0-18 read exactly as
        /// an MS-DIAL without key 19 reads them.
        /// </summary>
        [TestMethod()]
        [DeploymentItem(@"Resources\Export\Dataset_2025_07_31_12_31_11.mddata", @"Resources\Export")]
        public void AProjectWrittenBeforeTheFormatWasRecordedReadsAsUnknown() {
            var bytes = File.ReadAllBytes(Path.Combine("Resources", "Export", "Dataset_2025_07_31_12_31_11.mddata"));

            var storage = LZ4MessagePackSerializer.Deserialize<MsdialDataStorage>(bytes);

            Assert.IsTrue(storage.AnalysisFiles.Count > 0);
            Assert.IsTrue(storage.AnalysisFiles.All(file => file.RawFileFormat == RawFileFormat.Unknown));

            var beans = AnalysisFileBlocks(LZ4MessagePackSerializer.Decode(bytes)).ToList();
            Assert.AreEqual(storage.AnalysisFiles.Count, beans.Count);
            foreach (var (block, actual) in beans.Zip(storage.AnalysisFiles, (block, actual) => (block, actual))) {
                Assert.AreEqual(19, MessagePackBinary.ReadArrayHeader(block, 0, out _), "the fixture must predate key 19");
                var old = MessagePackSerializer.Deserialize<AnalysisFileBeanWithoutRawFileFormat>(block);
                AssertSameKeys0To18(old, actual);
            }
        }

        /// <summary>
        /// The other direction: an MS-DIAL that knows keys 0-18 opens a project written with key 19. The beans sit in a list
        /// with more data after it, so a reader that skipped key 19 by the wrong length would misread what follows.
        /// </summary>
        [TestMethod()]
        public void AnOlderMsdialReadsAProjectWithTheFormatRecorded() {
            var written = new BeansThenMore
            {
                AnalysisFiles = new List<AnalysisFileBean> { CreateFullBean(0, RawFileFormat.BrukerTdf), CreateFullBean(1, RawFileFormat.HiveMzb), },
                After = "after the beans",
            };
            var stream = new MemoryStream();
            MessagePackDefaultHandler.SaveToStream(written, stream);

            stream.Position = 0;
            var read = MessagePackDefaultHandler.LoadFromStream<OldBeansThenMore>(stream);

            Assert.AreEqual(2, read.AnalysisFiles.Count);
            Assert.AreEqual("after the beans", read.After);
            for (int i = 0; i < 2; i++) {
                AssertSameKeys0To18(read.AnalysisFiles[i], written.AnalysisFiles[i]);
            }
        }

        [TestMethod()]
        public void AnOlderMsdialReadsASingleBeanWithTheFormatRecorded() {
            var bean = CreateFullBean(3, RawFileFormat.WatersRaw);
            var bytes = MessagePackSerializer.Serialize(bean);
            Assert.AreEqual(20, MessagePackBinary.ReadArrayHeader(bytes, 0, out _), "key 19 is written");

            var old = MessagePackSerializer.Deserialize<AnalysisFileBeanWithoutRawFileFormat>(bytes);

            AssertSameKeys0To18(old, bean);
        }

        /// <summary>
        /// Projects store the number, so a number once given to a format is never given to another.
        /// New formats are appended.
        /// </summary>
        [TestMethod()]
        public void TheFormatNumbersAreFixed() {
            var expected = new Dictionary<RawFileFormat, int>
            {
                [RawFileFormat.Unknown] = 0,
                [RawFileFormat.ThermoRaw] = 1,
                [RawFileFormat.WatersRaw] = 2,
                [RawFileFormat.AgilentMassHunterD] = 3,
                [RawFileFormat.BrukerTdf] = 4,
                [RawFileFormat.BrukerTsf] = 5,
                [RawFileFormat.BrukerBaf] = 6,
                [RawFileFormat.SciexWiff] = 7,
                [RawFileFormat.SciexWiff2] = 8,
                [RawFileFormat.ShimadzuLcd] = 9,
                [RawFileFormat.ShimadzuQgd] = 10,
                [RawFileFormat.MzML] = 11,
                [RawFileFormat.ImzML] = 12,
                [RawFileFormat.NetCdf] = 13,
                [RawFileFormat.Abf] = 14,
                [RawFileFormat.Ibf] = 15,
                [RawFileFormat.KanomaxLrp] = 16,
                [RawFileFormat.HiveHmd] = 17,
                [RawFileFormat.HiveMzb] = 18,
            };
            foreach (var pair in expected) {
                Assert.AreEqual(pair.Value, (int)pair.Key, pair.Key.ToString());
            }
            // A value added at the end needs a line above; one added anywhere else breaks a line above.
            CollectionAssert.AreEquivalent(expected.Keys.ToArray(), AllFormats().ToArray());
        }

        private static IEnumerable<RawFileFormat> AllFormats() {
            return System.Enum.GetValues(typeof(RawFileFormat)).Cast<RawFileFormat>();
        }

        private static AnalysisFileBean RoundTrip(AnalysisFileBean bean) {
            using var stream = new MemoryStream();
            MessagePackDefaultHandler.SaveToStream(bean, stream);
            stream.Position = 0;
            return MessagePackDefaultHandler.LoadFromStream<AnalysisFileBean>(stream);
        }

        /// <summary>
        /// The MessagePack block of each analysis file in a serialized <see cref="MsdialDataStorage"/>, whose key 0 is the
        /// list of analysis files.
        /// </summary>
        private static IEnumerable<byte[]> AnalysisFileBlocks(byte[] storage) {
            var offset = 0;
            MessagePackBinary.ReadArrayHeader(storage, offset, out var readSize);
            offset += readSize;
            var count = MessagePackBinary.ReadArrayHeader(storage, offset, out readSize);
            offset += readSize;
            for (int i = 0; i < count; i++) {
                var size = MessagePackBinary.ReadNextBlock(storage, offset);
                var block = new byte[size];
                Array.Copy(storage, offset, block, 0, size);
                offset += size;
                yield return block;
            }
        }

        private static AnalysisFileBean CreateFullBean(int id, RawFileFormat format) {
            return new AnalysisFileBean
            {
                AnalysisFilePath = $@"D:\data\sample_{id}.d",
                AnalysisFileName = $"sample_{id}",
                AnalysisFileType = AnalysisFileType.QC,
                AnalysisFileClass = "class A",
                AnalysisFileAnalyticalOrder = 10 + id,
                AnalysisFileId = id,
                AnalysisFileIncluded = true,
                DeconvolutionFilePath = $@"D:\data\sample_{id}.dcl",
                DeconvolutionFilePathList = new List<string> { $@"D:\data\sample_{id}_1000.dcl", $@"D:\data\sample_{id}_2000.dcl", },
                PeakAreaBeanInformationFilePath = $@"D:\data\sample_{id}.pai",
                RiDictionaryFilePath = $@"D:\data\ri_{id}.txt",
                AnalysisBatch = 2,
                ResponseVariable = 0.25,
                DilutionFactor = 4d,
                AnalysisFileSuperClass = "super",
                RetentionTimeCorrectionBean = new RetentionTimeCorrectionBean($@"D:\data\sample_{id}.rtc"),
                ProteinAssembledResultFilePath = $@"D:\data\sample_{id}.prf",
                AcquisitionType = AcquisitionType.SWATH,
                RawFileFormat = format,
            };
        }

        /// <summary>
        /// Keys 0-18 must be read the same by both. Each key is compared by its MessagePack encoding, so nested objects
        /// (keys 15 and 16) are compared whole.
        /// </summary>
        private static void AssertSameKeys0To18(AnalysisFileBeanWithoutRawFileFormat old, AnalysisFileBean actual) {
            var oldBytes = MessagePackSerializer.Serialize(old);
            var actualBytes = MessagePackSerializer.Serialize(actual);
            Assert.AreEqual(19, MessagePackBinary.ReadArrayHeader(oldBytes, 0, out var oldOffset));
            Assert.AreEqual(20, MessagePackBinary.ReadArrayHeader(actualBytes, 0, out var actualOffset));
            for (int key = 0; key < 19; key++) {
                var oldSize = MessagePackBinary.ReadNextBlock(oldBytes, oldOffset);
                var actualSize = MessagePackBinary.ReadNextBlock(actualBytes, actualOffset);
                CollectionAssert.AreEqual(
                    new ArraySegment<byte>(oldBytes, oldOffset, oldSize).ToArray(),
                    new ArraySegment<byte>(actualBytes, actualOffset, actualSize).ToArray(),
                    $"key {key}");
                oldOffset += oldSize;
                actualOffset += actualSize;
            }
            Assert.AreEqual(old.AnalysisFilePath, actual.AnalysisFilePath);
            Assert.AreEqual(old.AnalysisFileName, actual.AnalysisFileName);
            Assert.AreEqual(old.AnalysisFileId, actual.AnalysisFileId);
            Assert.AreEqual(old.AcquisitionType, actual.AcquisitionType);
        }

        /// <summary>
        /// <see cref="AnalysisFileBean"/> as an MS-DIAL without key 19 declares it.
        /// </summary>
        [MessagePackObject]
        public class AnalysisFileBeanWithoutRawFileFormat
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
            [Key(0)] public List<AnalysisFileBeanWithoutRawFileFormat> AnalysisFiles { get; set; } = new List<AnalysisFileBeanWithoutRawFileFormat>();
            [Key(1)] public string After { get; set; } = string.Empty;
        }
    }
}
