using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialGcMsApi.DataObj;
using CompMs.MsdialGcMsApi.Parameter;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.IO;

namespace CompMs.App.Msdial.Model.Setting.Tests
{
    /// <summary>
    /// The GC-MS alignment dialog's "EI similarity tolerance" and "EI similarity factor" are the
    /// spectrum similarity settings that GcmsPeakJoiner reads. The dialog used to write them into
    /// the MS1 alignment settings, which the joiner no longer reads.
    /// </summary>
    [TestClass()]
    public class GcmsAlignmentParameterSettingModelTests
    {
        [TestMethod()]
        public void TheEiSimilaritySettingsShowAndCommitTheSpectrumSimilaritySettings() {
            var parameter = new MsdialGcmsParameter();
            parameter.ProjectParam.ProjectFolderPath = Path.GetTempPath();
            parameter.AlignmentBaseParam.SpectrumSimilarityAlignmentTolerance = .75f;
            parameter.AlignmentBaseParam.SpectrumSimilarityAlignmentFactor = .4f;
            parameter.AlignmentBaseParam.Ms1AlignmentTolerance = .015f;
            parameter.AlignmentBaseParam.Ms1AlignmentFactor = .6f;
            var analysisFiles = new List<AnalysisFileBean>
            {
                new AnalysisFileBean { AnalysisFileId = 0, AnalysisFileName = "file0", },
                new AnalysisFileBean { AnalysisFileId = 1, AnalysisFileName = "file1", },
            };
            var files = new AnalysisFileBeanModelCollection(analysisFiles);
            var alignments = new AlignmentFileBeanModelCollection(new List<AlignmentFileBean>(), analysisFiles);
            var model = new GcmsAlignmentParameterSettingModel(parameter, DateTime.Now, files, alignments, ProcessOption.All, new MessageBroker());

            Assert.AreEqual(.75f, model.EiEqualityParameter.Tolerance);
            Assert.AreEqual(.4f, model.EiEqualityParameter.Factor);

            model.EiEqualityParameter.Tolerance = .65f;
            model.EiEqualityParameter.Factor = .35f;
            Assert.IsTrue(model.TryCommit());

            Assert.AreEqual(.65f, parameter.AlignmentBaseParam.SpectrumSimilarityAlignmentTolerance);
            Assert.AreEqual(.35f, parameter.AlignmentBaseParam.SpectrumSimilarityAlignmentFactor);
            Assert.AreEqual(.015f, parameter.AlignmentBaseParam.Ms1AlignmentTolerance, "the MS1 alignment settings are not the dialog's");
            Assert.AreEqual(.6f, parameter.AlignmentBaseParam.Ms1AlignmentFactor);
        }

        [TestMethod()]
        public void LoadingAParameterFileSavedByAnOlderVersionShowsTheGcmsDefaultAndItsEiWeight() {
            // An older version wrote the dialog's "EI similarity tolerance / factor" into the MS1 alignment
            // settings and had no key 157 in MsdialGcmsParameter.
            var older = new MsdialGcmsParameter();
            older.AlignmentBaseParam.Ms1AlignmentTolerance = .7f;
            older.AlignmentBaseParam.Ms1AlignmentFactor = .6f;
            older.AlignmentBaseParam.SpectrumSimilarityAlignmentTolerance = .8f;
            older.AlignmentBaseParam.SpectrumSimilarityAlignmentFactor = .5f;
            var saved = MessagePackSerializer.Serialize(older, MessagePackSerializerOptions.Standard.WithResolver(StandardResolver.Instance));
            var loaded = new MsdialGcmsDataStorage().LoadParameter(new MemoryStream(WithoutLastKey(saved)));

            var parameter = new MsdialGcmsParameter();
            parameter.ProjectParam.ProjectFolderPath = Path.GetTempPath();
            var analysisFiles = new List<AnalysisFileBean>
            {
                new AnalysisFileBean { AnalysisFileId = 0, AnalysisFileName = "file0", },
                new AnalysisFileBean { AnalysisFileId = 1, AnalysisFileName = "file1", },
            };
            var files = new AnalysisFileBeanModelCollection(analysisFiles);
            var alignments = new AlignmentFileBeanModelCollection(new List<AlignmentFileBean>(), analysisFiles);
            var model = new GcmsAlignmentParameterSettingModel(parameter, DateTime.Now, files, alignments, ProcessOption.All, new MessageBroker());

            model.LoadParameter(loaded);

            // The GC-MS default threshold, not the 0.7 the older dialog wrote; the EI weight it wrote is kept.
            Assert.AreEqual(.5f, model.EiEqualityParameter.Tolerance);
            Assert.AreEqual(.6f, model.EiEqualityParameter.Factor);
        }

        /// <summary>The int-keyed array without its last element, as a version without that key wrote it.</summary>
        private static byte[] WithoutLastKey(byte[] saved) {
            var reader = new MessagePackReader(saved);
            var count = reader.ReadArrayHeader();
            Assert.IsTrue(count > 15 && count <= ushort.MaxValue, "an array16 header is rewritten below");
            var headerLength = (int)reader.Consumed;
            for (var i = 0; i < count - 1; i++) {
                reader.Skip();
            }
            var bodyLength = (int)reader.Consumed - headerLength;
            var result = new byte[3 + bodyLength];
            result[0] = 0xdc;
            result[1] = (byte)((count - 1) >> 8);
            result[2] = (byte)(count - 1);
            Array.Copy(saved, headerLength, result, 3, bodyLength);
            return result;
        }
    }
}
