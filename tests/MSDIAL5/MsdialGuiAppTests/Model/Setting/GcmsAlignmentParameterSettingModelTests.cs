using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialGcMsApi.Parameter;
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
    }
}
