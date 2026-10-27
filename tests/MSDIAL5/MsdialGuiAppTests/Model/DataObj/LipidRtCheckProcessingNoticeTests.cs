using CompMs.App.Msdial.Model.Setting.Tests;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm.Annotation.LipidRt;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.DataObj.Tests
{
    /// <summary>
    /// After GUI processing, a person is told when the lipid RT check failed on an analysis file or on the alignment: the
    /// failure does not stop processing, and its results would otherwise look like ones where the check found nothing.
    /// </summary>
    [TestClass]
    public class LipidRtCheckProcessingNoticeTests
    {
        [TestMethod]
        public void NothingIsSaidWhenTheCheckFailedNowhere() {
            var ran = LipidRtCheckHooks.RunOnSpots(LipidRtTestSpots.Spots(), LipidRtTestSpots.Lipidomics(), null);
            var parameter = LipidRtTestSpots.Lipidomics();
            parameter.TargetOmics = TargetOmics.Metabolomics;
            var notRun = LipidRtCheckHooks.RunOnSpots(LipidRtTestSpots.Spots(), parameter, null);

            Assert.IsNull(LipidRtCheckDescription.ProcessingFailures(new List<(string, LipidRtUnitRun?)>
            {
                ("sample1", ran), ("sample2", notRun), ("alignment", null),
            }));
            Assert.IsNull(LipidRtCheckDescription.ProcessingFailures(new List<(string, LipidRtUnitRun?)>()));
        }

        [TestMethod]
        public void AFailureIsNamedWithItsUnitAndWhatTheCheckReported() {
            var spots = LipidRtTestSpots.Spots();
            spots[3].MasterAlignmentID = spots[2].MasterAlignmentID; // the check refuses repeated Ids
            var failed = LipidRtCheckHooks.RunOnSpots(spots, LipidRtTestSpots.Lipidomics(), null);
            Assert.IsNotNull(failed.Error);
            var ran = LipidRtCheckHooks.RunOnSpots(LipidRtTestSpots.Spots(), LipidRtTestSpots.Lipidomics(), null);

            var notice = LipidRtCheckDescription.ProcessingFailures(new List<(string, LipidRtUnitRun?)>
            {
                ("sample1", ran), ("AlignmentResult_2026", failed),
            });

            Assert.IsNotNull(notice);
            StringAssert.StartsWith(notice, "The lipid RT check failed during processing.");
            StringAssert.Contains(notice, "AlignmentResult_2026: failed (ArgumentException");
            Assert.IsFalse(notice.Contains("sample1"), "a unit where it ran is not listed");
        }
    }
}
