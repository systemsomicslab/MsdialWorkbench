using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompMs.MsdialLcMsApi.Algorithm.Alignment;
using System;
using System.Collections.Generic;
using System.Linq;

using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;

namespace CompMs.MsdialLcMsApi.Algorithm.Alignment.Tests
{
    [TestClass()]
    public class LcmsAlignmentRefinerTests
    {
        [TestMethod()]
        public void RefineBaseTest() {
            var param = new MsdialLcmsParameter
            {
                OnlyReportTopHitInMspSearch = false, OnlyReportTopHitInTextDBSearch = false,
                FileID_AnalysisFileType = new Dictionary<int, AnalysisFileType>
                {
                    { 0, AnalysisFileType.Blank }, { 1, AnalysisFileType.Sample },
                    { 2, AnalysisFileType.Sample }, { 3, AnalysisFileType.Sample }
                },
                RetentionTimeAlignmentTolerance = 0.05f,
                Ms1AlignmentTolerance = 0.015f,
                FoldChangeForBlankFiltering = 0.1f,
                BlankFiltering = BlankFiltering.SampleMaxOverBlankAve,
                IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = true,
                IsKeepRemovableFeaturesAndAssignedTagForChecking = true,
                IsKeepRefMatchedMetaboliteFeatures = true,
                IsKeepSuggestedMetaboliteFeatures = true,
            };
            var refiner = new LcmsAlignmentRefiner(param);

            var alignments = BatchBuildAlignmentSpotProperty(4, d_mass: param.Ms1AlignmentTolerance, d_time: param.RetentionTimeAlignmentTolerance);

            var expects = BatchBuildAlignmentSpotProperty(4, d_mass: param.Ms1AlignmentTolerance, d_time: param.RetentionTimeAlignmentTolerance);
            for (int i = 0; i < expects.Count; i++) expects[i].PeakCharacter.PeakGroupID = i;
            var actuals = refiner.Refine(alignments);

            Assert.AreEqual(expects.Count, actuals.Count);
            foreach ((var expect, var actual) in expects.Zip(actuals))
                AreEqual(expect, actual);
        }

        [TestMethod()]
        public void RefineMspDuplicateTest() {
            var param = new MsdialLcmsParameter
            {
                OnlyReportTopHitInMspSearch = true, OnlyReportTopHitInTextDBSearch = false,
                FileID_AnalysisFileType = new Dictionary<int, AnalysisFileType>
                {
                    { 0, AnalysisFileType.Blank }, { 1, AnalysisFileType.Sample },
                    { 2, AnalysisFileType.Sample }, { 3, AnalysisFileType.Sample }
                },
                RetentionTimeAlignmentTolerance = 0.05f,
                Ms1AlignmentTolerance = 0.015f,
                FoldChangeForBlankFiltering = 0.1f,
                BlankFiltering = BlankFiltering.SampleMaxOverBlankAve,
                IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = true,
                IsKeepRemovableFeaturesAndAssignedTagForChecking = true,
                IsKeepRefMatchedMetaboliteFeatures = true,
                IsKeepSuggestedMetaboliteFeatures = true,
            };
            var refiner = new LcmsAlignmentRefiner(param);

            var alignments = BatchBuildAlignmentSpotProperty(4, d_mass: param.Ms1AlignmentTolerance, d_time: param.RetentionTimeAlignmentTolerance);
            alignments[0].MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>
            {
                {0, BuildMsScanMatchResult(0, 0.8f) }, {1, BuildMsScanMatchResult(1, 0.2f) },
            };
            alignments[0].Name = alignments[0].MspBasedMatchResult.Name;
            alignments[1].MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>
            {
                {0, BuildMsScanMatchResult(0, 0.2f) }, {1, BuildMsScanMatchResult(1, 0.8f) },
            };
            alignments[1].Name = alignments[1].MspBasedMatchResult.Name;
            alignments[2].MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>
            {
                {0, BuildMsScanMatchResult(0, 0.7f) }, {1, BuildMsScanMatchResult(1, 0.1f) },
            };
            alignments[2].Name = alignments[2].MspBasedMatchResult.Name;
            alignments[3].MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>
            {
                {0, BuildMsScanMatchResult(0, 0.9f) }, {1, BuildMsScanMatchResult(1, 0.1f) }, {2, BuildMsScanMatchResult(2, 1.0f) },
            };
            alignments[3].Name = alignments[3].MspBasedMatchResult.Name;

            var expects = BatchBuildAlignmentSpotProperty(4, d_mass: param.Ms1AlignmentTolerance, d_time: param.RetentionTimeAlignmentTolerance);
            for (int i = 0; i < expects.Count; i++) expects[i].PeakCharacter.PeakGroupID = i;
            expects[0].MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>
            {
                {0, BuildMsScanMatchResult(0, 0.8f) }, {1, BuildMsScanMatchResult(1, 0.2f) },
            };
            expects[0].Name = expects[0].MspBasedMatchResult.Name;
            expects[1].MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>
            {
                {0, BuildMsScanMatchResult(0, 0.2f) }, {1, BuildMsScanMatchResult(1, 0.8f) },
            };
            expects[1].Name = expects[1].MspBasedMatchResult.Name;
            alignments[2].MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>
            {
                {0, BuildMsScanMatchResult(0, 0.7f) }, {1, BuildMsScanMatchResult(1, 0.1f) },
            };
            expects[2].Name = expects[2].MspBasedMatchResult.Name;
            expects[3].MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>
            {
                {0, BuildMsScanMatchResult(0, 0.9f) }, {1, BuildMsScanMatchResult(1, 0.1f) }, {2, BuildMsScanMatchResult(2, 1.0f) },
            };
            expects[3].Name = expects[3].MspBasedMatchResult.Name;

            expects[2].Name = string.Empty;
            expects[2].MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>();

            var actuals = refiner.Refine(alignments);

            actuals.ForEach(actual => Console.WriteLine($"{actual.MasterAlignmentID}"));
            actuals.ForEach(actual => Console.WriteLine($"{actual.AlignmentID}"));
            expects.ForEach(expect => Console.WriteLine($"{expect.MasterAlignmentID}"));
            Assert.AreEqual(4, actuals.Count);
            foreach ((var expect, var actual) in expects.Zip(actuals))
                AreEqual(expect, actual);
        }

        [TestMethod()]
        public void RefineTextDuplicateTest() {
            var param = new MsdialLcmsParameter
            {
                OnlyReportTopHitInMspSearch = false, OnlyReportTopHitInTextDBSearch = true,
                FileID_AnalysisFileType = new Dictionary<int, AnalysisFileType>
                {
                    { 0, AnalysisFileType.Blank }, { 1, AnalysisFileType.Sample },
                    { 2, AnalysisFileType.Sample }, { 3, AnalysisFileType.Sample }
                },
                RetentionTimeAlignmentTolerance = 0.05f,
                Ms1AlignmentTolerance = 0.015f,
                FoldChangeForBlankFiltering = 0.1f,
                BlankFiltering = BlankFiltering.SampleMaxOverBlankAve,
                IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = true,
                IsKeepRemovableFeaturesAndAssignedTagForChecking = true,
                IsKeepRefMatchedMetaboliteFeatures = true,
                IsKeepSuggestedMetaboliteFeatures = true,
            };
            var refiner = new LcmsAlignmentRefiner(param);

            var alignments = BatchBuildAlignmentSpotProperty(4, d_mass: param.Ms1AlignmentTolerance, d_time: param.RetentionTimeAlignmentTolerance);
            alignments[0].TextDbBasedMatchResult = BuildMsScanMatchResult(id: 100);
            alignments[0].Name = alignments[0].TextDbBasedMatchResult.Name;
            alignments[1].TextDbBasedMatchResult = BuildMsScanMatchResult(id: 102, score: 0.8f);
            alignments[1].Name = alignments[1].TextDbBasedMatchResult.Name;
            alignments[2].TextDbBasedMatchResult = BuildMsScanMatchResult(id: 102, score: 0.9f);
            alignments[2].Name = alignments[2].TextDbBasedMatchResult.Name;
            alignments[3].TextDbBasedMatchResult = BuildMsScanMatchResult(id: 103);
            alignments[3].Name = alignments[3].TextDbBasedMatchResult.Name;

            var expects = BatchBuildAlignmentSpotProperty(4, d_mass: param.Ms1AlignmentTolerance, d_time: param.RetentionTimeAlignmentTolerance);
            for (int i = 0; i < expects.Count; i++) expects[i].PeakCharacter.PeakGroupID = i;
            expects[0].TextDbBasedMatchResult = BuildMsScanMatchResult(id: 100);
            expects[0].Name = expects[0].TextDbBasedMatchResult.Name;
            expects[1].TextDbBasedMatchResult = BuildMsScanMatchResult(id: 102, score: 0.8f);
            expects[1].Name = expects[1].TextDbBasedMatchResult.Name;
            expects[2].TextDbBasedMatchResult = BuildMsScanMatchResult(id: 102, score: 0.9f);
            expects[2].Name = expects[2].TextDbBasedMatchResult.Name;
            expects[3].TextDbBasedMatchResult = BuildMsScanMatchResult(id: 103);
            expects[3].Name = expects[3].TextDbBasedMatchResult.Name;

            expects[1].Name = string.Empty;
            expects[1].TextDbBasedMatchResult = null;

            var actuals = refiner.Refine(alignments);

            Assert.AreEqual(4, actuals.Count);
            foreach ((var expect, var actual) in expects.Zip(actuals))
                AreEqual(expect, actual);
        }

        [TestMethod()]
        public void RefineBlankFilterBySampleMax() {
            var param = new MsdialLcmsParameter
            {
                OnlyReportTopHitInMspSearch = false, OnlyReportTopHitInTextDBSearch = false,
                FileID_AnalysisFileType = new Dictionary<int, AnalysisFileType>
                {
                    { 0, AnalysisFileType.Blank }, { 1, AnalysisFileType.Blank },
                    { 2, AnalysisFileType.Sample }, { 3, AnalysisFileType.Sample }
                },
                RetentionTimeAlignmentTolerance = 0.05f,
                Ms1AlignmentTolerance = 0.015f,
                FoldChangeForBlankFiltering = 0.1f,
                BlankFiltering = BlankFiltering.SampleMaxOverBlankAve,
                IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = true,
                IsKeepRemovableFeaturesAndAssignedTagForChecking = true,
                IsKeepRefMatchedMetaboliteFeatures = true,
                IsKeepSuggestedMetaboliteFeatures = true,
            };
            var refiner = new LcmsAlignmentRefiner(param);

            var alignments = BatchBuildAlignmentSpotProperty(6, d_mass: param.Ms1AlignmentTolerance, d_time: param.RetentionTimeAlignmentTolerance);
            foreach (var alignment in alignments) alignment.MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>();
            foreach (var alignment in alignments) alignment.TextDbBasedMatchResult = null;
            alignments[0].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 10000), BuildAlignmentChromPeakFeature(fileid: 1, peak: 10000), 
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 900), BuildAlignmentChromPeakFeature(fileid: 3, peak: 900), 
            };
            alignments[1].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 10000), BuildAlignmentChromPeakFeature(fileid: 1, peak: 10000), 
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 1100), BuildAlignmentChromPeakFeature(fileid: 3, peak: 700), 
            };
            alignments[2].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 10000), BuildAlignmentChromPeakFeature(fileid: 1, peak: 10000), 
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 1100), BuildAlignmentChromPeakFeature(fileid: 3, peak: 1100), 
            };
            alignments[3].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 15000), BuildAlignmentChromPeakFeature(fileid: 1, peak: 5000), 
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 900), BuildAlignmentChromPeakFeature(fileid: 3, peak: 900), 
            };
            alignments[4].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 15000), BuildAlignmentChromPeakFeature(fileid: 1, peak: 5000), 
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 1100), BuildAlignmentChromPeakFeature(fileid: 3, peak: 700), 
            };
            alignments[5].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 15000), BuildAlignmentChromPeakFeature(fileid: 1, peak: 5000), 
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 1100), BuildAlignmentChromPeakFeature(fileid: 3, peak: 1100), 
            };

            var expects = BatchBuildAlignmentSpotProperty(6, d_mass: param.Ms1AlignmentTolerance, d_time: param.RetentionTimeAlignmentTolerance);
            foreach (var expect in expects) expect.MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>();
            foreach (var expect in expects) expect.TextDbBasedMatchResult = null;
            expects[0].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 10000), BuildAlignmentChromPeakFeature(fileid: 1, peak: 10000), 
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 900), BuildAlignmentChromPeakFeature(fileid: 3, peak: 900), 
            };
            expects[1].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 10000), BuildAlignmentChromPeakFeature(fileid: 1, peak: 10000), 
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 1100), BuildAlignmentChromPeakFeature(fileid: 3, peak: 700), 
            };
            expects[2].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 10000), BuildAlignmentChromPeakFeature(fileid: 1, peak: 10000), 
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 1100), BuildAlignmentChromPeakFeature(fileid: 3, peak: 1100), 
            };
            expects[3].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 15000), BuildAlignmentChromPeakFeature(fileid: 1, peak: 5000), 
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 900), BuildAlignmentChromPeakFeature(fileid: 3, peak: 900), 
            };
            expects[4].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 15000), BuildAlignmentChromPeakFeature(fileid: 1, peak: 5000), 
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 1100), BuildAlignmentChromPeakFeature(fileid: 3, peak: 700), 
            };
            expects[5].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 15000), BuildAlignmentChromPeakFeature(fileid: 1, peak: 5000), 
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 1100), BuildAlignmentChromPeakFeature(fileid: 3, peak: 1100), 
            };

            expects[0].FeatureFilterStatus.IsBlankFiltered = true;
            expects[3].FeatureFilterStatus.IsBlankFiltered = true;
            for (int i = 0; i < expects.Count; i++) expects[i].PeakCharacter.PeakGroupID = i;
            for (int i = 0; i < expects.Count; i++) expects[i].AlignmentID = i;
            for (int i = 0; i < expects.Count; i++) expects[i].MasterAlignmentID = i;
            var actuals = refiner.Refine(alignments);

            Assert.AreEqual(expects.Count, actuals.Count);
            foreach ((var expect, var actual) in expects.Zip(actuals))
                AreEqual(expect, actual);
        }

        [TestMethod()]
        public void RefineBlankFilterBySampleAve() {
            var param = new MsdialLcmsParameter
            {
                OnlyReportTopHitInMspSearch = false, OnlyReportTopHitInTextDBSearch = false,
                FileID_AnalysisFileType = new Dictionary<int, AnalysisFileType>
                {
                    { 0, AnalysisFileType.Blank }, { 1, AnalysisFileType.Blank },
                    { 2, AnalysisFileType.Sample }, { 3, AnalysisFileType.Sample }
                },
                RetentionTimeAlignmentTolerance = 0.05f,
                Ms1AlignmentTolerance = 0.015f,
                FoldChangeForBlankFiltering = 0.1f,
                BlankFiltering = BlankFiltering.SampleAveOverBlankAve,
                IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = true,
                IsKeepRemovableFeaturesAndAssignedTagForChecking = true,
                IsKeepRefMatchedMetaboliteFeatures = true,
                IsKeepSuggestedMetaboliteFeatures = true,
            };
            var refiner = new LcmsAlignmentRefiner(param);

            var alignments = BatchBuildAlignmentSpotProperty(6, d_mass: param.Ms1AlignmentTolerance, d_time: param.RetentionTimeAlignmentTolerance);
            foreach (var alignment in alignments) alignment.MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>();
            foreach (var alignment in alignments) alignment.TextDbBasedMatchResult = null;
            alignments[0].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 10000), BuildAlignmentChromPeakFeature(fileid: 1, peak: 10000), 
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 900), BuildAlignmentChromPeakFeature(fileid: 3, peak: 900), 
            };
            alignments[1].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 10000), BuildAlignmentChromPeakFeature(fileid: 1, peak: 10000), 
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 1100), BuildAlignmentChromPeakFeature(fileid: 3, peak: 700), 
            };
            alignments[2].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 10000), BuildAlignmentChromPeakFeature(fileid: 1, peak: 10000), 
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 1100), BuildAlignmentChromPeakFeature(fileid: 3, peak: 1100), 
            };
            alignments[3].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 15000), BuildAlignmentChromPeakFeature(fileid: 1, peak: 5000), 
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 900), BuildAlignmentChromPeakFeature(fileid: 3, peak: 900), 
            };
            alignments[4].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 15000), BuildAlignmentChromPeakFeature(fileid: 1, peak: 5000), 
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 1100), BuildAlignmentChromPeakFeature(fileid: 3, peak: 700), 
            };
            alignments[5].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 15000), BuildAlignmentChromPeakFeature(fileid: 1, peak: 5000), 
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 1100), BuildAlignmentChromPeakFeature(fileid: 3, peak: 1100), 
            };

            var expects = BatchBuildAlignmentSpotProperty(6, d_mass: param.Ms1AlignmentTolerance, d_time: param.RetentionTimeAlignmentTolerance);
            foreach (var expect in expects) expect.MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>();
            foreach (var expect in expects) expect.TextDbBasedMatchResult = null;
            expects[0].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 10000), BuildAlignmentChromPeakFeature(fileid: 1, peak: 10000), 
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 900), BuildAlignmentChromPeakFeature(fileid: 3, peak: 900), 
            };
            expects[1].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 10000), BuildAlignmentChromPeakFeature(fileid: 1, peak: 10000), 
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 1100), BuildAlignmentChromPeakFeature(fileid: 3, peak: 700), 
            };
            expects[2].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 10000), BuildAlignmentChromPeakFeature(fileid: 1, peak: 10000), 
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 1100), BuildAlignmentChromPeakFeature(fileid: 3, peak: 1100), 
            };
            expects[3].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 15000), BuildAlignmentChromPeakFeature(fileid: 1, peak: 5000), 
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 900), BuildAlignmentChromPeakFeature(fileid: 3, peak: 900), 
            };
            expects[4].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 15000), BuildAlignmentChromPeakFeature(fileid: 1, peak: 5000), 
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 1100), BuildAlignmentChromPeakFeature(fileid: 3, peak: 700), 
            };
            expects[5].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 15000), BuildAlignmentChromPeakFeature(fileid: 1, peak: 5000), 
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 1100), BuildAlignmentChromPeakFeature(fileid: 3, peak: 1100), 
            };

            expects[0].FeatureFilterStatus.IsBlankFiltered = true;
            expects[1].FeatureFilterStatus.IsBlankFiltered = true;
            expects[3].FeatureFilterStatus.IsBlankFiltered = true;
            expects[4].FeatureFilterStatus.IsBlankFiltered = true;
            for (int i = 0; i < expects.Count; i++) expects[i].PeakCharacter.PeakGroupID = i;
            for (int i = 0; i < expects.Count; i++) expects[i].AlignmentID = i;
            for (int i = 0; i < expects.Count; i++) expects[i].MasterAlignmentID = i;
            var actuals = refiner.Refine(alignments);

            Assert.AreEqual(expects.Count, actuals.Count);
            foreach ((var expect, var actual) in expects.Zip(actuals))
                AreEqual(expect, actual);
        }

        [TestMethod()]
        public void RefineMergeOrderTest() {
            var param = new MsdialLcmsParameter
            {
                OnlyReportTopHitInMspSearch = false, OnlyReportTopHitInTextDBSearch = false,
                FileID_AnalysisFileType = new Dictionary<int, AnalysisFileType>
                {
                    { 0, AnalysisFileType.Blank }, { 1, AnalysisFileType.Sample },
                    { 2, AnalysisFileType.Sample }, { 3, AnalysisFileType.Sample },
                },
                RetentionTimeAlignmentTolerance = 0.05f,
                Ms1AlignmentTolerance = 0.015f,
                FoldChangeForBlankFiltering = 0.1f,
                BlankFiltering = BlankFiltering.SampleMaxOverBlankAve,
                IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = true,
                IsKeepRemovableFeaturesAndAssignedTagForChecking = true,
                IsKeepRefMatchedMetaboliteFeatures = true,
                IsKeepSuggestedMetaboliteFeatures = true,
            };
            var refiner = new LcmsAlignmentRefiner(param);

            var alignments = BatchBuildAlignmentSpotProperty(8, d_mass: -param.Ms1AlignmentTolerance, d_time: param.RetentionTimeAlignmentTolerance);
            alignments[0].MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>();
            alignments[1].MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>();
            alignments[2].MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>();
            alignments[3].MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>();

            alignments[0].TextDbBasedMatchResult = null;
            alignments[1].TextDbBasedMatchResult = null;
            alignments[4].TextDbBasedMatchResult = null;
            alignments[5].TextDbBasedMatchResult = null;

            alignments[0].PeakCharacter.IsotopeWeightNumber = 1;
            alignments[2].PeakCharacter.IsotopeWeightNumber = 1;
            alignments[4].PeakCharacter.IsotopeWeightNumber = 1;
            alignments[6].PeakCharacter.IsotopeWeightNumber = 1;

            var expects = BatchBuildAlignmentSpotProperty(8, d_mass: -param.Ms1AlignmentTolerance, d_time: param.RetentionTimeAlignmentTolerance);
            expects[0].MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>();
            expects[1].MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>();
            expects[2].MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>();
            expects[3].MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>();

            expects[0].TextDbBasedMatchResult = null;
            expects[1].TextDbBasedMatchResult = null;
            expects[4].TextDbBasedMatchResult = null;
            expects[5].TextDbBasedMatchResult = null;

            expects[0].PeakCharacter.IsotopeWeightNumber = 1;
            expects[2].PeakCharacter.IsotopeWeightNumber = 1;
            expects[4].PeakCharacter.IsotopeWeightNumber = 1;
            expects[6].PeakCharacter.IsotopeWeightNumber = 1;

            expects.RemoveAt(0);
            expects.Sort((x, y) => x.MassCenter.CompareTo(y.MassCenter));
            for (int i = 0; i < expects.Count; i++) expects[i].AlignmentID = i;
            for (int i = 0; i < expects.Count; i++) expects[i].MasterAlignmentID = i;
            for (int i = 0; i < expects.Count; i++) expects[i].PeakCharacter.PeakGroupID = i;

            var actuals = refiner.Refine(alignments);

            Assert.AreEqual(7, actuals.Count);
            foreach ((var expect, var actual) in expects.Zip(actuals))
                AreEqual(expect, actual);
        }

        [TestMethod()]
        public void RefineMergeCompressSpotsTest() {
            var param = new MsdialLcmsParameter
            {
                OnlyReportTopHitInMspSearch = false, OnlyReportTopHitInTextDBSearch = false,
                FileID_AnalysisFileType = new Dictionary<int, AnalysisFileType>
                {
                    { 0, AnalysisFileType.Blank }, { 1, AnalysisFileType.Sample },
                    { 2, AnalysisFileType.Sample }, { 3, AnalysisFileType.Sample }
                },
                RetentionTimeAlignmentTolerance = 0.05f,
                Ms1AlignmentTolerance = 0.015f,
                FoldChangeForBlankFiltering = 0.1f,
                BlankFiltering = BlankFiltering.SampleMaxOverBlankAve,
                IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = false,
                IsKeepRemovableFeaturesAndAssignedTagForChecking = false,
                IsKeepRefMatchedMetaboliteFeatures = false,
                IsKeepSuggestedMetaboliteFeatures = false,
            };
            var refiner = new LcmsAlignmentRefiner(param);

            var alignments = BatchBuildAlignmentSpotProperty(10, d_time: 10, d_mass: 10);
            alignments[1].MassCenter = alignments[0].MassCenter + param.Ms1AlignmentTolerance * 0.99;
            alignments[1].TimesCenter = alignments[0].TimesCenter;
            alignments[3].MassCenter = alignments[2].MassCenter;
            alignments[3].TimesCenter.RT.Value = alignments[2].TimesCenter.RT.Value + param.RetentionTimeAlignmentTolerance * 0.99;
            alignments[5].MassCenter = alignments[4].MassCenter;
            alignments[5].TimesCenter.RT.Value = alignments[4].TimesCenter.RT.Value + param.RetentionTimeAlignmentTolerance * 0.5 * 0.99;
            alignments[7].TimesCenter.RT.Value = alignments[6].TimesCenter.RT.Value + param.RetentionTimeAlignmentTolerance * 0.5 * 0.99;
            alignments[9].MassCenter = alignments[8].MassCenter + param.Ms1AlignmentTolerance * 0.99;
            

            var expects = BatchBuildAlignmentSpotProperty(10, d_time: 10, d_mass: 10);
            expects[1].MassCenter = expects[0].MassCenter + param.Ms1AlignmentTolerance * 0.99;
            expects[1].TimesCenter = expects[0].TimesCenter;
            expects[3].MassCenter = expects[2].MassCenter;
            expects[3].TimesCenter.RT.Value = expects[2].TimesCenter.RT.Value + param.RetentionTimeAlignmentTolerance * 0.99;
            expects[5].MassCenter = expects[4].MassCenter;
            expects[5].TimesCenter.RT.Value = expects[4].TimesCenter.RT.Value + param.RetentionTimeAlignmentTolerance * 0.5 * 0.99;
            expects[7].TimesCenter.RT.Value = expects[6].TimesCenter.RT.Value + param.RetentionTimeAlignmentTolerance * 0.5 * 0.99;
            expects[9].MassCenter = expects[8].MassCenter + param.Ms1AlignmentTolerance * 0.99;

            expects.RemoveAt(5);
            expects.RemoveAt(1);
            for (int i = 0; i < expects.Count; i++) expects[i].PeakCharacter.PeakGroupID = i;
            for (int i = 0; i < expects.Count; i++) expects[i].AlignmentID = i;
            for (int i = 0; i < expects.Count; i++) expects[i].MasterAlignmentID = i;
            
            var actuals = refiner.Refine(alignments);

            Assert.AreEqual(expects.Count, actuals.Count);
            foreach ((var expect, var actual) in expects.Zip(actuals))
                AreEqual(expect, actual);
        }

        [TestMethod()]
        public void RefineLinkCorrelationTest() {
            var param = new MsdialLcmsParameter
            {
                OnlyReportTopHitInMspSearch = false, OnlyReportTopHitInTextDBSearch = false,
                FileID_AnalysisFileType = new Dictionary<int, AnalysisFileType>
                {
                    { 0, AnalysisFileType.Blank }, { 1, AnalysisFileType.Sample },
                    { 2, AnalysisFileType.Sample }, { 3, AnalysisFileType.Sample },
                    { 4, AnalysisFileType.Sample }, { 5, AnalysisFileType.Sample },
                    { 6, AnalysisFileType.Sample }, { 7, AnalysisFileType.Sample },
                    { 8, AnalysisFileType.Sample }, { 9, AnalysisFileType.Sample },
                },
                RetentionTimeAlignmentTolerance = 0.05f,
                Ms1AlignmentTolerance = 0.015f,
                FoldChangeForBlankFiltering = 0.1f,
                BlankFiltering = BlankFiltering.SampleMaxOverBlankAve,
                IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = true,
                IsKeepRemovableFeaturesAndAssignedTagForChecking = true,
                IsKeepRefMatchedMetaboliteFeatures = true,
                IsKeepSuggestedMetaboliteFeatures = true,
            };
            var refiner = new LcmsAlignmentRefiner(param);

            var alignments = BatchBuildAlignmentSpotProperty(6, d_mass: param.Ms1AlignmentTolerance, d_time: param.RetentionTimeAlignmentTolerance);
            alignments[0].AlignedPeakProperties = BatchBuildAlignmentChromPeakFeature(10, 100);
            alignments[1].AlignedPeakProperties = BatchBuildAlignmentChromPeakFeature(10, 100);
            alignments[2].AlignedPeakProperties = BatchBuildAlignmentChromPeakFeature(10, 100);
            alignments[3].AlignedPeakProperties = BatchBuildAlignmentChromPeakFeature(10, 100);
            alignments[4].AlignedPeakProperties = BatchBuildAlignmentChromPeakFeature(10, 100);
            alignments[5].AlignedPeakProperties = BatchBuildAlignmentChromPeakFeature(10, 100);

            var expects = BatchBuildAlignmentSpotProperty(6, d_mass: param.Ms1AlignmentTolerance, d_time: param.RetentionTimeAlignmentTolerance);
            for (int i = 0; i < expects.Count; i++) expects[i].PeakCharacter.PeakGroupID = i;
            expects[0].AlignedPeakProperties = BatchBuildAlignmentChromPeakFeature(10, 100);
            expects[0].PeakCharacter.PeakLinks = new List<LinkedPeakFeature> {
                new LinkedPeakFeature { Character = PeakLinkFeatureEnum.CorrelSimilar, LinkedPeakID = 1},
            };
            expects[0].AlignmentSpotVariableCorrelations = new List<AlignmentSpotVariableCorrelation> {
                new AlignmentSpotVariableCorrelation { CorrelateAlignmentID = 1, CorrelationScore = 1f},
            };
            expects[1].AlignedPeakProperties = BatchBuildAlignmentChromPeakFeature(10, 100);
            expects[1].PeakCharacter.PeakLinks = new List<LinkedPeakFeature> {
                new LinkedPeakFeature { Character = PeakLinkFeatureEnum.CorrelSimilar, LinkedPeakID = 0},
                new LinkedPeakFeature { Character = PeakLinkFeatureEnum.CorrelSimilar, LinkedPeakID = 2},
            };
            expects[1].AlignmentSpotVariableCorrelations = new List<AlignmentSpotVariableCorrelation> {
                new AlignmentSpotVariableCorrelation { CorrelateAlignmentID = 0, CorrelationScore = 1f},
                new AlignmentSpotVariableCorrelation { CorrelateAlignmentID = 2, CorrelationScore = 1f},
            };
            expects[2].AlignedPeakProperties = BatchBuildAlignmentChromPeakFeature(10, 100);
            expects[2].PeakCharacter.PeakLinks = new List<LinkedPeakFeature> {
                new LinkedPeakFeature { Character = PeakLinkFeatureEnum.CorrelSimilar, LinkedPeakID = 1},
                new LinkedPeakFeature { Character = PeakLinkFeatureEnum.CorrelSimilar, LinkedPeakID = 3},
            };
            expects[2].AlignmentSpotVariableCorrelations = new List<AlignmentSpotVariableCorrelation> {
                new AlignmentSpotVariableCorrelation { CorrelateAlignmentID = 1, CorrelationScore = 1f},
                new AlignmentSpotVariableCorrelation { CorrelateAlignmentID = 3, CorrelationScore = 1f},
            };
            expects[3].AlignedPeakProperties = BatchBuildAlignmentChromPeakFeature(10, 100);
            expects[3].PeakCharacter.PeakLinks = new List<LinkedPeakFeature> {
                new LinkedPeakFeature { Character = PeakLinkFeatureEnum.CorrelSimilar, LinkedPeakID = 2},
                new LinkedPeakFeature { Character = PeakLinkFeatureEnum.CorrelSimilar, LinkedPeakID = 4},
            };
            expects[3].AlignmentSpotVariableCorrelations = new List<AlignmentSpotVariableCorrelation> {
                new AlignmentSpotVariableCorrelation { CorrelateAlignmentID = 2, CorrelationScore = 1f},
                new AlignmentSpotVariableCorrelation { CorrelateAlignmentID = 4, CorrelationScore = 1f},
            };
            expects[4].AlignedPeakProperties = BatchBuildAlignmentChromPeakFeature(10, 100);
            expects[4].PeakCharacter.PeakLinks = new List<LinkedPeakFeature> {
                new LinkedPeakFeature { Character = PeakLinkFeatureEnum.CorrelSimilar, LinkedPeakID = 3},
                new LinkedPeakFeature { Character = PeakLinkFeatureEnum.CorrelSimilar, LinkedPeakID = 5},
            };
            expects[4].AlignmentSpotVariableCorrelations = new List<AlignmentSpotVariableCorrelation> {
                new AlignmentSpotVariableCorrelation { CorrelateAlignmentID = 3, CorrelationScore = 1f},
                new AlignmentSpotVariableCorrelation { CorrelateAlignmentID = 5, CorrelationScore = 1f},
            };
            expects[5].AlignedPeakProperties = BatchBuildAlignmentChromPeakFeature(10, 100);
            expects[5].PeakCharacter.PeakLinks = new List<LinkedPeakFeature> {
                new LinkedPeakFeature { Character = PeakLinkFeatureEnum.CorrelSimilar, LinkedPeakID = 4},
            };
            expects[5].AlignmentSpotVariableCorrelations = new List<AlignmentSpotVariableCorrelation> {
                new AlignmentSpotVariableCorrelation { CorrelateAlignmentID = 4, CorrelationScore = 1f},
            };

            var actuals = refiner.Refine(alignments);

            Assert.AreEqual(expects.Count, actuals.Count);
            foreach ((var expect, var actual) in expects.Zip(actuals))
                AreEqual(expect, actual);
        }

        [TestMethod()]
        public void RefineLinkCorrelationTest2() {
            var param = new MsdialLcmsParameter
            {
                OnlyReportTopHitInMspSearch = false, OnlyReportTopHitInTextDBSearch = false,
                FileID_AnalysisFileType = new Dictionary<int, AnalysisFileType>
                {
                    { 0, AnalysisFileType.Blank }, { 1, AnalysisFileType.Sample },
                    { 2, AnalysisFileType.Sample }, { 3, AnalysisFileType.Sample },
                    { 4, AnalysisFileType.Sample }, { 5, AnalysisFileType.Sample },
                    { 6, AnalysisFileType.Sample }, { 7, AnalysisFileType.Sample },
                    { 8, AnalysisFileType.Sample }, { 9, AnalysisFileType.Sample },
                },
                RetentionTimeAlignmentTolerance = 0.05f,
                Ms1AlignmentTolerance = 0.015f,
                FoldChangeForBlankFiltering = 0.1f,
                BlankFiltering = BlankFiltering.SampleMaxOverBlankAve,
                IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = true,
                IsKeepRemovableFeaturesAndAssignedTagForChecking = true,
                IsKeepRefMatchedMetaboliteFeatures = true,
                IsKeepSuggestedMetaboliteFeatures = true,
            };
            var refiner = new LcmsAlignmentRefiner(param);

            var alignments = BatchBuildAlignmentSpotProperty(5, d_mass: param.Ms1AlignmentTolerance, d_time: param.RetentionTimeAlignmentTolerance);
            alignments[0].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 11300), BuildAlignmentChromPeakFeature(fileid: 1, peak: 10600),
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 10200), BuildAlignmentChromPeakFeature(fileid: 3, peak: 11600),
                BuildAlignmentChromPeakFeature(fileid: 4, peak:  9100), BuildAlignmentChromPeakFeature(fileid: 5, peak: 10200),
                BuildAlignmentChromPeakFeature(fileid: 6, peak: 10400), BuildAlignmentChromPeakFeature(fileid: 7, peak: 10800),
                BuildAlignmentChromPeakFeature(fileid: 8, peak: 10900), BuildAlignmentChromPeakFeature(fileid: 9, peak:  9100),
            };
            alignments[1].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 11300), BuildAlignmentChromPeakFeature(fileid: 1, peak: 10600),
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 10200), BuildAlignmentChromPeakFeature(fileid: 3, peak: 11600),
                BuildAlignmentChromPeakFeature(fileid: 4, peak:  9100), BuildAlignmentChromPeakFeature(fileid: 5, peak: 10200),
                BuildAlignmentChromPeakFeature(fileid: 6, peak: 10400), BuildAlignmentChromPeakFeature(fileid: 7, peak: 10800),
                BuildAlignmentChromPeakFeature(fileid: 8, peak: 10900), BuildAlignmentChromPeakFeature(fileid: 9, peak:  9100),
            };
            alignments[2].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 11000), BuildAlignmentChromPeakFeature(fileid: 1, peak: 10400),
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 10100), BuildAlignmentChromPeakFeature(fileid: 3, peak: 11500),
                BuildAlignmentChromPeakFeature(fileid: 4, peak:  9800), BuildAlignmentChromPeakFeature(fileid: 5, peak: 10500),
                BuildAlignmentChromPeakFeature(fileid: 6, peak: 10300), BuildAlignmentChromPeakFeature(fileid: 7, peak: 10600),
                BuildAlignmentChromPeakFeature(fileid: 8, peak: 10900), BuildAlignmentChromPeakFeature(fileid: 9, peak:  9400),
            };
            alignments[3].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 11300), BuildAlignmentChromPeakFeature(fileid: 1, peak: 10600),
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 10200), BuildAlignmentChromPeakFeature(fileid: 3, peak: 11600),
                BuildAlignmentChromPeakFeature(fileid: 4, peak:  9100), BuildAlignmentChromPeakFeature(fileid: 5, peak: 10200),
                BuildAlignmentChromPeakFeature(fileid: 6, peak: 10400), BuildAlignmentChromPeakFeature(fileid: 7, peak: 11000),
                BuildAlignmentChromPeakFeature(fileid: 8, peak: 10900), BuildAlignmentChromPeakFeature(fileid: 9, peak:  9100),
            };
            alignments[4].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 11300), BuildAlignmentChromPeakFeature(fileid: 1, peak: 10600),
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 10200), BuildAlignmentChromPeakFeature(fileid: 3, peak: 11600),
                BuildAlignmentChromPeakFeature(fileid: 4, peak:  9100), BuildAlignmentChromPeakFeature(fileid: 5, peak: 10200),
                BuildAlignmentChromPeakFeature(fileid: 6, peak: 10400), BuildAlignmentChromPeakFeature(fileid: 7, peak: 11000),
                BuildAlignmentChromPeakFeature(fileid: 8, peak: 10900), BuildAlignmentChromPeakFeature(fileid: 9, peak:  9100),
            };

            var expects = BatchBuildAlignmentSpotProperty(5, d_mass: param.Ms1AlignmentTolerance, d_time: param.RetentionTimeAlignmentTolerance);
            for (int i = 0; i < expects.Count; i++) expects[i].PeakCharacter.PeakGroupID = i;
            expects[0].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 11300), BuildAlignmentChromPeakFeature(fileid: 1, peak: 10600),
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 10200), BuildAlignmentChromPeakFeature(fileid: 3, peak: 11600),
                BuildAlignmentChromPeakFeature(fileid: 4, peak:  9100), BuildAlignmentChromPeakFeature(fileid: 5, peak: 10200),
                BuildAlignmentChromPeakFeature(fileid: 6, peak: 10400), BuildAlignmentChromPeakFeature(fileid: 7, peak: 10800),
                BuildAlignmentChromPeakFeature(fileid: 8, peak: 10900), BuildAlignmentChromPeakFeature(fileid: 9, peak:  9100),
            };
            expects[0].PeakCharacter.PeakLinks = new List<LinkedPeakFeature> {
                new LinkedPeakFeature { Character = PeakLinkFeatureEnum.CorrelSimilar, LinkedPeakID = 1},
            };
            expects[0].AlignmentSpotVariableCorrelations = new List<AlignmentSpotVariableCorrelation> {
                new AlignmentSpotVariableCorrelation { CorrelateAlignmentID = 1, CorrelationScore = 1f},
            };
            expects[1].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 11300), BuildAlignmentChromPeakFeature(fileid: 1, peak: 10600),
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 10200), BuildAlignmentChromPeakFeature(fileid: 3, peak: 11600),
                BuildAlignmentChromPeakFeature(fileid: 4, peak:  9100), BuildAlignmentChromPeakFeature(fileid: 5, peak: 10200),
                BuildAlignmentChromPeakFeature(fileid: 6, peak: 10400), BuildAlignmentChromPeakFeature(fileid: 7, peak: 10800),
                BuildAlignmentChromPeakFeature(fileid: 8, peak: 10900), BuildAlignmentChromPeakFeature(fileid: 9, peak:  9100),
            };
            expects[1].PeakCharacter.PeakLinks = new List<LinkedPeakFeature> {
                new LinkedPeakFeature { Character = PeakLinkFeatureEnum.CorrelSimilar, LinkedPeakID = 0},
                new LinkedPeakFeature { Character = PeakLinkFeatureEnum.CorrelSimilar, LinkedPeakID = 2},
            };
            expects[1].AlignmentSpotVariableCorrelations = new List<AlignmentSpotVariableCorrelation> {
                new AlignmentSpotVariableCorrelation { CorrelateAlignmentID = 0, CorrelationScore = 1f},
                new AlignmentSpotVariableCorrelation { CorrelateAlignmentID = 2, CorrelationScore = 0.9533083962667448f},
            };
            expects[2].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 11000), BuildAlignmentChromPeakFeature(fileid: 1, peak: 10400),
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 10100), BuildAlignmentChromPeakFeature(fileid: 3, peak: 11500),
                BuildAlignmentChromPeakFeature(fileid: 4, peak:  9800), BuildAlignmentChromPeakFeature(fileid: 5, peak: 10500),
                BuildAlignmentChromPeakFeature(fileid: 6, peak: 10300), BuildAlignmentChromPeakFeature(fileid: 7, peak: 10600),
                BuildAlignmentChromPeakFeature(fileid: 8, peak: 10900), BuildAlignmentChromPeakFeature(fileid: 9, peak:  9400),
            };
            expects[2].PeakCharacter.PeakLinks = new List<LinkedPeakFeature> {
                new LinkedPeakFeature { Character = PeakLinkFeatureEnum.CorrelSimilar, LinkedPeakID = 1},
            };
            expects[2].AlignmentSpotVariableCorrelations = new List<AlignmentSpotVariableCorrelation> {
                new AlignmentSpotVariableCorrelation { CorrelateAlignmentID = 1, CorrelationScore = 0.9533083962667448f},
            };
            expects[3].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 11300), BuildAlignmentChromPeakFeature(fileid: 1, peak: 10600),
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 10200), BuildAlignmentChromPeakFeature(fileid: 3, peak: 11600),
                BuildAlignmentChromPeakFeature(fileid: 4, peak:  9100), BuildAlignmentChromPeakFeature(fileid: 5, peak: 10200),
                BuildAlignmentChromPeakFeature(fileid: 6, peak: 10400), BuildAlignmentChromPeakFeature(fileid: 7, peak: 11000),
                BuildAlignmentChromPeakFeature(fileid: 8, peak: 10900), BuildAlignmentChromPeakFeature(fileid: 9, peak:  9100),
            };
            expects[3].PeakCharacter.PeakLinks = new List<LinkedPeakFeature> {
                new LinkedPeakFeature { Character = PeakLinkFeatureEnum.CorrelSimilar, LinkedPeakID = 4},
            };
            expects[3].AlignmentSpotVariableCorrelations = new List<AlignmentSpotVariableCorrelation> {
                new AlignmentSpotVariableCorrelation { CorrelateAlignmentID = 4, CorrelationScore = 1f},
            };
            expects[4].AlignedPeakProperties = new List<AlignmentChromPeakFeature>
            {
                BuildAlignmentChromPeakFeature(fileid: 0, peak: 11300), BuildAlignmentChromPeakFeature(fileid: 1, peak: 10600),
                BuildAlignmentChromPeakFeature(fileid: 2, peak: 10200), BuildAlignmentChromPeakFeature(fileid: 3, peak: 11600),
                BuildAlignmentChromPeakFeature(fileid: 4, peak:  9100), BuildAlignmentChromPeakFeature(fileid: 5, peak: 10200),
                BuildAlignmentChromPeakFeature(fileid: 6, peak: 10400), BuildAlignmentChromPeakFeature(fileid: 7, peak: 11000),
                BuildAlignmentChromPeakFeature(fileid: 8, peak: 10900), BuildAlignmentChromPeakFeature(fileid: 9, peak:  9100),
            };
            expects[4].PeakCharacter.PeakLinks = new List<LinkedPeakFeature> {
                new LinkedPeakFeature { Character = PeakLinkFeatureEnum.CorrelSimilar, LinkedPeakID = 3},
            };
            expects[4].AlignmentSpotVariableCorrelations = new List<AlignmentSpotVariableCorrelation> {
                new AlignmentSpotVariableCorrelation { CorrelateAlignmentID = 3, CorrelationScore = 1f},
            };

            var actuals = refiner.Refine(alignments);

            Assert.AreEqual(expects.Count, actuals.Count);
            foreach ((var expect, var actual) in expects.Zip(actuals))
                AreEqual(expect, actual);
        }

        [TestMethod()]
        public void RefineLinkCorrelationIsotopeTest() {
            var param = new MsdialLcmsParameter
            {
                OnlyReportTopHitInMspSearch = false, OnlyReportTopHitInTextDBSearch = false,
                FileID_AnalysisFileType = new Dictionary<int, AnalysisFileType>
                {
                    { 0, AnalysisFileType.Blank }, { 1, AnalysisFileType.Sample },
                    { 2, AnalysisFileType.Sample }, { 3, AnalysisFileType.Sample },
                    { 4, AnalysisFileType.Sample }, { 5, AnalysisFileType.Sample },
                    { 6, AnalysisFileType.Sample }, { 7, AnalysisFileType.Sample },
                    { 8, AnalysisFileType.Sample }, { 9, AnalysisFileType.Sample },
                },
                RetentionTimeAlignmentTolerance = 0.05f,
                Ms1AlignmentTolerance = 0.015f,
                FoldChangeForBlankFiltering = 0.1f,
                BlankFiltering = BlankFiltering.SampleMaxOverBlankAve,
                IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = true,
                IsKeepRemovableFeaturesAndAssignedTagForChecking = true,
                IsKeepRefMatchedMetaboliteFeatures = true,
                IsKeepSuggestedMetaboliteFeatures = true,
            };
            var refiner = new LcmsAlignmentRefiner(param);

            var alignments = BatchBuildAlignmentSpotProperty(4, d_mass: param.Ms1AlignmentTolerance, d_time: param.RetentionTimeAlignmentTolerance);
            alignments[0].AlignedPeakProperties = BatchBuildAlignmentChromPeakFeature(10, 100);
            alignments[1].AlignedPeakProperties = BatchBuildAlignmentChromPeakFeature(10, 100);
            alignments[2].AlignedPeakProperties = BatchBuildAlignmentChromPeakFeature(10, 100);
            alignments[2].PeakCharacter.IsotopeWeightNumber = 1;
            alignments[3].AlignedPeakProperties = BatchBuildAlignmentChromPeakFeature(10, 100);


            var expects = BatchBuildAlignmentSpotProperty(4, d_mass: param.Ms1AlignmentTolerance, d_time: param.RetentionTimeAlignmentTolerance);
            for (int i = 0; i < expects.Count; i++) expects[i].PeakCharacter.PeakGroupID = i;
            expects[0].AlignedPeakProperties = BatchBuildAlignmentChromPeakFeature(10, 100);
            expects[0].PeakCharacter.PeakLinks = new List<LinkedPeakFeature> {
                new LinkedPeakFeature { Character = PeakLinkFeatureEnum.CorrelSimilar, LinkedPeakID = 1},
            };
            expects[0].AlignmentSpotVariableCorrelations = new List<AlignmentSpotVariableCorrelation> {
                new AlignmentSpotVariableCorrelation { CorrelateAlignmentID = 1, CorrelationScore = 1f},
            };
            expects[1].AlignedPeakProperties = BatchBuildAlignmentChromPeakFeature(10, 100);
            expects[1].PeakCharacter.PeakLinks = new List<LinkedPeakFeature> {
                new LinkedPeakFeature { Character = PeakLinkFeatureEnum.CorrelSimilar, LinkedPeakID = 0},
            };
            expects[1].AlignmentSpotVariableCorrelations = new List<AlignmentSpotVariableCorrelation> {
                new AlignmentSpotVariableCorrelation { CorrelateAlignmentID = 0, CorrelationScore = 1f},
            };
            expects[2].AlignedPeakProperties = BatchBuildAlignmentChromPeakFeature(10, 100);
            expects[2].PeakCharacter.PeakLinks = new List<LinkedPeakFeature> { };
            expects[2].AlignmentSpotVariableCorrelations = new List<AlignmentSpotVariableCorrelation> { };
            expects[2].PeakCharacter.IsotopeWeightNumber = 1;
            expects[3].AlignedPeakProperties = BatchBuildAlignmentChromPeakFeature(10, 100);
            expects[3].PeakCharacter.PeakLinks = new List<LinkedPeakFeature> { };
            expects[3].AlignmentSpotVariableCorrelations = new List<AlignmentSpotVariableCorrelation> { };

            var actuals = refiner.Refine(alignments);

            Assert.AreEqual(expects.Count, actuals.Count);
            foreach ((var expect, var actual) in expects.Zip(actuals))
                AreEqual(expect, actual);
        }

        [TestMethod()]
        public void RefineRegisterLinksTest() {
            var param = new MsdialLcmsParameter
            {
                OnlyReportTopHitInMspSearch = false, OnlyReportTopHitInTextDBSearch = false,
                FileID_AnalysisFileType = new Dictionary<int, AnalysisFileType>
                {
                    { 0, AnalysisFileType.Blank }, { 1, AnalysisFileType.Sample },
                    { 2, AnalysisFileType.Sample }, { 3, AnalysisFileType.Sample }
                },
                RetentionTimeAlignmentTolerance = 0.05f,
                Ms1AlignmentTolerance = 0.015f,
                FoldChangeForBlankFiltering = 0.1f,
                BlankFiltering = BlankFiltering.SampleMaxOverBlankAve,
                IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = false,
                IsKeepRemovableFeaturesAndAssignedTagForChecking = false,
                IsKeepRefMatchedMetaboliteFeatures = false,
                IsKeepSuggestedMetaboliteFeatures = false,
            };
            var refiner = new LcmsAlignmentRefiner(param);

            var alignments = BatchBuildAlignmentSpotProperty(5, d_mass: param.Ms1AlignmentTolerance, d_time: param.RetentionTimeAlignmentTolerance);
            foreach (var alignment in alignments) alignment.AlignedPeakProperties = new List<AlignmentChromPeakFeature>();
            for (int i = 0; i < 4; i++) {
                foreach ((var spots, var peak) in alignments.Zip(BatchBuildAlignmentChromPeakFeature(5, param.RetentionTimeAlignmentTolerance))) {
                    spots.AlignedPeakProperties.Add(peak);
                }
            }
            alignments[0].RepresentativeFileID = 0;
            alignments[0].AlignedPeakProperties[0].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 1, Character = PeakLinkFeatureEnum.SameFeature },
                new LinkedPeakFeature {LinkedPeakID = 2, Character = PeakLinkFeatureEnum.Isotope },
                new LinkedPeakFeature {LinkedPeakID = 2, Character = PeakLinkFeatureEnum.Adduct },
                new LinkedPeakFeature {LinkedPeakID = 3, Character = PeakLinkFeatureEnum.CorrelSimilar },
                new LinkedPeakFeature {LinkedPeakID = 4, Character = PeakLinkFeatureEnum.ChromSimilar },
                new LinkedPeakFeature {LinkedPeakID = 4, Character = PeakLinkFeatureEnum.FoundInUpperMsMs },
                new LinkedPeakFeature {LinkedPeakID = 4, Character = PeakLinkFeatureEnum.Adduct },
            };
            alignments[1].RepresentativeFileID = 1;
            alignments[1].AlignedPeakProperties[1].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 3, Character = PeakLinkFeatureEnum.Adduct },
                new LinkedPeakFeature {LinkedPeakID = 4, Character = PeakLinkFeatureEnum.FoundInUpperMsMs },
            };
            alignments[1].AdductType.AdductIonName = "[M+Na]+";
            alignments[2].RepresentativeFileID = 2;
            alignments[2].AlignedPeakProperties[2].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 3, Character = PeakLinkFeatureEnum.Isotope },
                new LinkedPeakFeature {LinkedPeakID = 1, Character = PeakLinkFeatureEnum.Adduct },
            };
            alignments[2].AdductType.AdductIonName = "[M+Na]+";
            alignments[3].RepresentativeFileID = 3;
            alignments[3].AlignedPeakProperties[3].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 4, Character = PeakLinkFeatureEnum.SameFeature },
                new LinkedPeakFeature {LinkedPeakID = 4, Character = PeakLinkFeatureEnum.Adduct },
            };
            alignments[3].PeakCharacter.Charge = 1;
            alignments[3].AlignedPeakProperties[1].PeakCharacter.AdductType.AdductIonName = "[M+2H]2+";
            alignments[3].MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>();
            alignments[3].TextDbBasedMatchResult = null;
            alignments[4].RepresentativeFileID = 2;
            alignments[4].AlignedPeakProperties[2].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 3, Character = PeakLinkFeatureEnum.Isotope },
            };
            alignments[4].MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>();
            alignments[4].TextDbBasedMatchResult = null;

            var expects = BatchBuildAlignmentSpotProperty(5, d_mass: param.Ms1AlignmentTolerance, d_time: param.RetentionTimeAlignmentTolerance);
            foreach (var expect in expects) expect.AlignedPeakProperties = new List<AlignmentChromPeakFeature>();
            for (int i = 0; i < 4; i++) {
                foreach ((var spots, var peak) in expects.Zip(BatchBuildAlignmentChromPeakFeature(5, param.RetentionTimeAlignmentTolerance))) {
                    spots.AlignedPeakProperties.Add(peak);
                }
            }
            expects[0].RepresentativeFileID = 0;
            expects[0].AlignedPeakProperties[0].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 1, Character = PeakLinkFeatureEnum.SameFeature },
                new LinkedPeakFeature {LinkedPeakID = 2, Character = PeakLinkFeatureEnum.Isotope },
                new LinkedPeakFeature {LinkedPeakID = 2, Character = PeakLinkFeatureEnum.Adduct },
                new LinkedPeakFeature {LinkedPeakID = 3, Character = PeakLinkFeatureEnum.CorrelSimilar },
                new LinkedPeakFeature {LinkedPeakID = 4, Character = PeakLinkFeatureEnum.ChromSimilar },
                new LinkedPeakFeature {LinkedPeakID = 4, Character = PeakLinkFeatureEnum.FoundInUpperMsMs },
                new LinkedPeakFeature {LinkedPeakID = 4, Character = PeakLinkFeatureEnum.Adduct },
            };
            expects[0].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 1, Character = PeakLinkFeatureEnum.SameFeature },
                new LinkedPeakFeature {LinkedPeakID = 2, Character = PeakLinkFeatureEnum.Adduct },
                new LinkedPeakFeature {LinkedPeakID = 3, Character = PeakLinkFeatureEnum.CorrelSimilar },
                new LinkedPeakFeature {LinkedPeakID = 4, Character = PeakLinkFeatureEnum.ChromSimilar },
                new LinkedPeakFeature {LinkedPeakID = 4, Character = PeakLinkFeatureEnum.FoundInUpperMsMs },
                new LinkedPeakFeature {LinkedPeakID = 4, Character = PeakLinkFeatureEnum.Adduct },
            };
            expects[0].PeakCharacter.PeakGroupID = 0;
            expects[1].RepresentativeFileID = 1;
            expects[1].AlignedPeakProperties[1].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 3, Character = PeakLinkFeatureEnum.Adduct },
                new LinkedPeakFeature {LinkedPeakID = 4, Character = PeakLinkFeatureEnum.FoundInUpperMsMs },
            };
            expects[1].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 4, Character = PeakLinkFeatureEnum.FoundInUpperMsMs },
                new LinkedPeakFeature {LinkedPeakID = 0, Character = PeakLinkFeatureEnum.SameFeature },
            };
            expects[1].AdductType.AdductIonName = "[M+Na]+";
            expects[1].PeakCharacter.PeakGroupID = 0;
            expects[2].RepresentativeFileID = 2;
            expects[2].AlignedPeakProperties[2].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 3, Character = PeakLinkFeatureEnum.Isotope },
                new LinkedPeakFeature {LinkedPeakID = 1, Character = PeakLinkFeatureEnum.Adduct },
            };
            expects[2].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 0, Character = PeakLinkFeatureEnum.Adduct },
            };
            expects[2].AdductType.AdductIonName = "[M+Na]+";
            expects[2].PeakCharacter.PeakGroupID = 0;
            expects[3].RepresentativeFileID = 3;
            expects[3].AlignedPeakProperties[3].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 4, Character = PeakLinkFeatureEnum.SameFeature },
                new LinkedPeakFeature {LinkedPeakID = 4, Character = PeakLinkFeatureEnum.Adduct },
            };
            expects[3].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 0, Character = PeakLinkFeatureEnum.CorrelSimilar },
                new LinkedPeakFeature {LinkedPeakID = 4, Character = PeakLinkFeatureEnum.SameFeature },
            };
            expects[3].PeakCharacter.Charge = 1;
            expects[3].AlignedPeakProperties[1].PeakCharacter.AdductType.AdductIonName = "[M+2H]2+";
            expects[3].MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>();
            expects[3].TextDbBasedMatchResult = null;
            expects[3].PeakCharacter.PeakGroupID = 0;
            expects[4].RepresentativeFileID = 2;
            expects[4].AlignedPeakProperties[2].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 3, Character = PeakLinkFeatureEnum.Isotope },
            };
            expects[4].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 0, Character = PeakLinkFeatureEnum.Adduct },
                new LinkedPeakFeature {LinkedPeakID = 0, Character = PeakLinkFeatureEnum.ChromSimilar },
                new LinkedPeakFeature {LinkedPeakID = 0, Character = PeakLinkFeatureEnum.FoundInUpperMsMs },
                new LinkedPeakFeature {LinkedPeakID = 1, Character = PeakLinkFeatureEnum.FoundInUpperMsMs },
                new LinkedPeakFeature {LinkedPeakID = 3, Character = PeakLinkFeatureEnum.SameFeature },
            };
            expects[4].MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>();
            expects[4].TextDbBasedMatchResult = null;
            expects[4].PeakCharacter.PeakGroupID = 0;

            var actuals = refiner.Refine(alignments);
            actuals[4].PeakCharacter.PeakLinks.ForEach(link => Console.WriteLine($"{link.Character}, {link.LinkedPeakID}"));

            Assert.AreEqual(5, actuals.Count);
            foreach ((var expect, var actual) in expects.Zip(actuals))
                AreEqual(expect, actual);
        }

        [TestMethod()]
        public void RefineRegisterLinksOnlyRefMatchedTest() {
            var param = new MsdialLcmsParameter
            {
                OnlyReportTopHitInMspSearch = false, OnlyReportTopHitInTextDBSearch = false,
                FileID_AnalysisFileType = new Dictionary<int, AnalysisFileType>
                {
                    { 0, AnalysisFileType.Blank }, { 1, AnalysisFileType.Sample },
                    { 2, AnalysisFileType.Sample }, { 3, AnalysisFileType.Sample }
                },
                RetentionTimeAlignmentTolerance = 0.05f,
                Ms1AlignmentTolerance = 0.015f,
                FoldChangeForBlankFiltering = 0.1f,
                BlankFiltering = BlankFiltering.SampleMaxOverBlankAve,
                IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = false,
                IsKeepRemovableFeaturesAndAssignedTagForChecking = false,
                IsKeepRefMatchedMetaboliteFeatures = false,
                IsKeepSuggestedMetaboliteFeatures = false,
            };
            var refiner = new LcmsAlignmentRefiner(param);

            var alignments = BatchBuildAlignmentSpotProperty(7, d_mass: param.Ms1AlignmentTolerance, d_time: param.RetentionTimeAlignmentTolerance);
            foreach (var alignment in alignments) alignment.AlignedPeakProperties = new List<AlignmentChromPeakFeature>();
            for (int i = 0; i < 4; i++) {
                foreach ((var spots, var peak) in alignments.Zip(BatchBuildAlignmentChromPeakFeature(n: 7, d_peak: 100 + i))) {
                    spots.AlignedPeakProperties.Add(peak);
                }
            }
            foreach (var alignment in alignments) alignment.RepresentativeFileID = 1;
            alignments[1].MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>();
            alignments[2].MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>();
            alignments[1].TextDbBasedMatchResult = null;
            alignments[2].TextDbBasedMatchResult = null;
            alignments[1].Name = alignments[2].Name = "Unknown";
            foreach (var kvp in alignments[3].MSRawID2MspBasedMatchResult) kvp.Value.IsSpectrumMatch = false;
            foreach (var kvp in alignments[4].MSRawID2MspBasedMatchResult) kvp.Value.IsSpectrumMatch = false;
            alignments[3].TextDbBasedMatchResult = null;
            alignments[4].TextDbBasedMatchResult = null;
            alignments[3].Name = "w/o MS2: " + alignments[3].Name;
            alignments[4].Name = "w/o MS2: " + alignments[4].Name;

            alignments[1].AlignedPeakProperties[1].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 2, Character = PeakLinkFeatureEnum.ChromSimilar },
                new LinkedPeakFeature {LinkedPeakID = 4, Character = PeakLinkFeatureEnum.ChromSimilar },
                new LinkedPeakFeature {LinkedPeakID = 6, Character = PeakLinkFeatureEnum.ChromSimilar },
            };
            alignments[2].AlignedPeakProperties[1].PeakCharacter.PeakLinks = new List<LinkedPeakFeature> { };
            alignments[3].AlignedPeakProperties[1].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 2, Character = PeakLinkFeatureEnum.ChromSimilar },
                new LinkedPeakFeature {LinkedPeakID = 4, Character = PeakLinkFeatureEnum.ChromSimilar },
                new LinkedPeakFeature {LinkedPeakID = 6, Character = PeakLinkFeatureEnum.ChromSimilar },
            };
            alignments[4].AlignedPeakProperties[1].PeakCharacter.PeakLinks = new List<LinkedPeakFeature> { };
            alignments[5].AlignedPeakProperties[1].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 2, Character = PeakLinkFeatureEnum.ChromSimilar },
                new LinkedPeakFeature {LinkedPeakID = 4, Character = PeakLinkFeatureEnum.ChromSimilar },
                new LinkedPeakFeature {LinkedPeakID = 6, Character = PeakLinkFeatureEnum.ChromSimilar },
            };
            alignments[6].AlignedPeakProperties[1].PeakCharacter.PeakLinks = new List<LinkedPeakFeature> { };

            var expects = BatchBuildAlignmentSpotProperty(n: 7, d_mass: param.Ms1AlignmentTolerance, d_time: param.RetentionTimeAlignmentTolerance);
            foreach (var expect in expects) expect.RepresentativeFileID = 1;
            foreach (var expect in expects) expect.AlignedPeakProperties = new List<AlignmentChromPeakFeature>();
            for (int i = 0; i < 4; i++) {
                foreach ((var spots, var peak) in expects.Zip(BatchBuildAlignmentChromPeakFeature(n: 7, d_peak: 100 + i))) {
                    spots.AlignedPeakProperties.Add(peak);
                }
            }
            // Unknown
            expects[1].MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>();
            expects[2].MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>();
            expects[1].TextDbBasedMatchResult = null;
            expects[2].TextDbBasedMatchResult = null;
            expects[1].Name = expects[2].Name = "Unknown";
            // Suggested
            foreach (var kvp in expects[3].MSRawID2MspBasedMatchResult) kvp.Value.IsSpectrumMatch = false;
            foreach (var kvp in expects[4].MSRawID2MspBasedMatchResult) kvp.Value.IsSpectrumMatch = false;
            expects[3].TextDbBasedMatchResult = null;
            expects[4].TextDbBasedMatchResult = null;
            expects[3].Name = "w/o MS2: " + expects[3].Name;
            expects[4].Name = "w/o MS2: " + expects[4].Name;

            for (int i = 0; i < expects.Count; i++) expects[i].PeakCharacter.PeakGroupID = i;

            expects[1].AlignedPeakProperties[1].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 2, Character = PeakLinkFeatureEnum.ChromSimilar },
                new LinkedPeakFeature {LinkedPeakID = 4, Character = PeakLinkFeatureEnum.ChromSimilar },
                new LinkedPeakFeature {LinkedPeakID = 6, Character = PeakLinkFeatureEnum.ChromSimilar },
            };
            expects[1].PeakCharacter.PeakLinks = new List<LinkedPeakFeature> { };
            expects[2].AlignedPeakProperties[1].PeakCharacter.PeakLinks = new List<LinkedPeakFeature> { };
            expects[2].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 5, Character = PeakLinkFeatureEnum.ChromSimilar },
            };
            expects[3].AlignedPeakProperties[1].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 2, Character = PeakLinkFeatureEnum.ChromSimilar },
                new LinkedPeakFeature {LinkedPeakID = 4, Character = PeakLinkFeatureEnum.ChromSimilar },
                new LinkedPeakFeature {LinkedPeakID = 6, Character = PeakLinkFeatureEnum.ChromSimilar },
            };
            expects[3].PeakCharacter.PeakLinks = new List<LinkedPeakFeature> { };
            expects[4].AlignedPeakProperties[1].PeakCharacter.PeakLinks = new List<LinkedPeakFeature> { };
            expects[4].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 5, Character = PeakLinkFeatureEnum.ChromSimilar },
            };
            expects[5].AlignedPeakProperties[1].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 2, Character = PeakLinkFeatureEnum.ChromSimilar },
                new LinkedPeakFeature {LinkedPeakID = 4, Character = PeakLinkFeatureEnum.ChromSimilar },
                new LinkedPeakFeature {LinkedPeakID = 6, Character = PeakLinkFeatureEnum.ChromSimilar },
            };
            expects[5].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 2, Character = PeakLinkFeatureEnum.ChromSimilar },
                new LinkedPeakFeature {LinkedPeakID = 4, Character = PeakLinkFeatureEnum.ChromSimilar },
                new LinkedPeakFeature {LinkedPeakID = 6, Character = PeakLinkFeatureEnum.ChromSimilar },
            };
            expects[6].AlignedPeakProperties[1].PeakCharacter.PeakLinks = new List<LinkedPeakFeature> { };
            expects[6].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 5, Character = PeakLinkFeatureEnum.ChromSimilar },
            };

            alignments.ForEach(alignment => Console.WriteLine($"{alignment.Name}"));
            var actuals = refiner.Refine(alignments);

            actuals.ForEach(actual => Console.WriteLine($"{actual.Name}"));
            Assert.AreEqual(expects.Count, actuals.Count);
            foreach ((var expect, var actual) in expects.Zip(actuals))
                AreEqual(expect, actual);
        }

        [TestMethod()]
        public void RefinePeakGroupTest() {
            var param = new MsdialLcmsParameter
            {
                OnlyReportTopHitInMspSearch = false, OnlyReportTopHitInTextDBSearch = false,
                FileID_AnalysisFileType = new Dictionary<int, AnalysisFileType>
                {
                    { 0, AnalysisFileType.Blank }, { 1, AnalysisFileType.Sample },
                    { 2, AnalysisFileType.Sample }, { 3, AnalysisFileType.Sample }
                },
                RetentionTimeAlignmentTolerance = 0.05f,
                Ms1AlignmentTolerance = 0.015f,
                FoldChangeForBlankFiltering = 0.1f,
                BlankFiltering = BlankFiltering.SampleMaxOverBlankAve,
                IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = true,
                IsKeepRemovableFeaturesAndAssignedTagForChecking = true,
                IsKeepRefMatchedMetaboliteFeatures = true,
                IsKeepSuggestedMetaboliteFeatures = true,
            };
            var refiner = new LcmsAlignmentRefiner(param);

            var alignments = BatchBuildAlignmentSpotProperty(8, d_mass: param.Ms1AlignmentTolerance, d_time: param.RetentionTimeAlignmentTolerance);
            foreach (var alignment in alignments) alignment.AlignedPeakProperties = new List<AlignmentChromPeakFeature>();
            for (int i = 0; i < 4; i++) {
                foreach ((var spots, var peak) in alignments.Zip(BatchBuildAlignmentChromPeakFeature(n: 8, d_peak: 100))) {
                    spots.AlignedPeakProperties.Add(peak);
                }
            }
            foreach (var alignment in alignments) alignment.RepresentativeFileID = 0;
            alignments[0].AlignedPeakProperties[0].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 1, Character = PeakLinkFeatureEnum.ChromSimilar },
                new LinkedPeakFeature {LinkedPeakID = 2, Character = PeakLinkFeatureEnum.CorrelSimilar },
                new LinkedPeakFeature {LinkedPeakID = 3, Character = PeakLinkFeatureEnum.FoundInUpperMsMs },
            };
            alignments[1].AlignedPeakProperties[0].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 2, Character = PeakLinkFeatureEnum.Isotope },
            };
            alignments[2].AlignedPeakProperties[0].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 3, Character = PeakLinkFeatureEnum.Adduct },
            };
            alignments[2].AdductType.AdductIonName = "[M+Na]+";
            alignments[3].AlignedPeakProperties[0].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 1, Character = PeakLinkFeatureEnum.SameFeature },
            };
            alignments[4].AlignedPeakProperties[0].PeakCharacter.PeakLinks = new List<LinkedPeakFeature> { };
            alignments[4].AdductType.AdductIonName = "[M+Na]+";
            alignments[5].AlignedPeakProperties[0].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 4, Character = PeakLinkFeatureEnum.Isotope },
            };
            alignments[6].AlignedPeakProperties[0].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 4, Character = PeakLinkFeatureEnum.Adduct },
            };
            alignments[7].AlignedPeakProperties[0].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 6, Character = PeakLinkFeatureEnum.SameFeature },
                new LinkedPeakFeature {LinkedPeakID = 1, Character = PeakLinkFeatureEnum.CorrelSimilar },
            };

            var expects = BatchBuildAlignmentSpotProperty(8, d_mass: param.Ms1AlignmentTolerance, d_time: param.RetentionTimeAlignmentTolerance);
            foreach (var expect in expects) expect.RepresentativeFileID = 0;
            foreach (var expect in expects) expect.AlignedPeakProperties = new List<AlignmentChromPeakFeature>();
            for (int i = 0; i < 4; i++) {
                foreach ((var spots, var peak) in expects.Zip(BatchBuildAlignmentChromPeakFeature(n: 8, d_peak: 100))) {
                    spots.AlignedPeakProperties.Add(peak);
                }
            }
            expects[0].AlignedPeakProperties[0].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 1, Character = PeakLinkFeatureEnum.ChromSimilar },
                new LinkedPeakFeature {LinkedPeakID = 2, Character = PeakLinkFeatureEnum.CorrelSimilar },
                new LinkedPeakFeature {LinkedPeakID = 3, Character = PeakLinkFeatureEnum.FoundInUpperMsMs },
            };
            expects[0].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 1, Character = PeakLinkFeatureEnum.ChromSimilar },
                new LinkedPeakFeature {LinkedPeakID = 2, Character = PeakLinkFeatureEnum.CorrelSimilar },
                new LinkedPeakFeature {LinkedPeakID = 3, Character = PeakLinkFeatureEnum.FoundInUpperMsMs },
            };
            expects[0].PeakCharacter.PeakGroupID = 0;
            expects[1].AlignedPeakProperties[0].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 2, Character = PeakLinkFeatureEnum.Isotope },
            };
            expects[1].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 0, Character = PeakLinkFeatureEnum.ChromSimilar },
                new LinkedPeakFeature {LinkedPeakID = 7, Character = PeakLinkFeatureEnum.CorrelSimilar },
                new LinkedPeakFeature {LinkedPeakID = 3, Character = PeakLinkFeatureEnum.SameFeature },
            };
            expects[1].PeakCharacter.PeakGroupID = 1;
            expects[2].AlignedPeakProperties[0].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 3, Character = PeakLinkFeatureEnum.Adduct },
            };
            expects[2].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 3, Character = PeakLinkFeatureEnum.Adduct },
                new LinkedPeakFeature {LinkedPeakID = 0, Character = PeakLinkFeatureEnum.CorrelSimilar },
            };
            expects[2].PeakCharacter.PeakGroupID = 1;
            expects[2].AdductType.AdductIonName = "[M+Na]+";
            expects[3].AlignedPeakProperties[0].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 1, Character = PeakLinkFeatureEnum.SameFeature },
            };
            expects[3].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 1, Character = PeakLinkFeatureEnum.SameFeature },
                new LinkedPeakFeature {LinkedPeakID = 2, Character = PeakLinkFeatureEnum.Adduct },
                new LinkedPeakFeature {LinkedPeakID = 0, Character = PeakLinkFeatureEnum.FoundInUpperMsMs },
            };
            expects[3].PeakCharacter.PeakGroupID = 1;
            expects[4].AlignedPeakProperties[0].PeakCharacter.PeakLinks = new List<LinkedPeakFeature> { };
            expects[4].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 6, Character = PeakLinkFeatureEnum.Adduct },
            };
            expects[4].PeakCharacter.PeakGroupID = 2;
            expects[4].AdductType.AdductIonName = "[M+Na]+";
            expects[5].AlignedPeakProperties[0].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 4, Character = PeakLinkFeatureEnum.Isotope },
            };
            expects[5].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
            };
            expects[5].PeakCharacter.PeakGroupID = 3;
            expects[6].AlignedPeakProperties[0].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 4, Character = PeakLinkFeatureEnum.Adduct },
            };
            expects[6].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 4, Character = PeakLinkFeatureEnum.Adduct },
                new LinkedPeakFeature {LinkedPeakID = 7, Character = PeakLinkFeatureEnum.SameFeature },
            };
            expects[6].PeakCharacter.PeakGroupID = 2;
            expects[7].AlignedPeakProperties[0].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 6, Character = PeakLinkFeatureEnum.SameFeature },
                new LinkedPeakFeature {LinkedPeakID = 1, Character = PeakLinkFeatureEnum.CorrelSimilar },
            };
            expects[7].PeakCharacter.PeakLinks = new List<LinkedPeakFeature>
            {
                new LinkedPeakFeature {LinkedPeakID = 6, Character = PeakLinkFeatureEnum.SameFeature },
                new LinkedPeakFeature {LinkedPeakID = 1, Character = PeakLinkFeatureEnum.CorrelSimilar },
            };
            expects[7].PeakCharacter.PeakGroupID = 2;
            var actuals = refiner.Refine(alignments);

            actuals[2].PeakCharacter.PeakLinks.ForEach(link => Console.WriteLine($"{link.LinkedPeakID}, {link.Character}"));
            Assert.AreEqual(8, actuals.Count);
            foreach ((var expect, var actual) in expects.Zip(actuals))
                AreEqual(expect, actual);
        }

        #region helper
        #region equality test method
        void AreEqual(AlignmentSpotProperty expected, AlignmentSpotProperty actual) {
            Assert.AreEqual(expected.MasterAlignmentID, actual.MasterAlignmentID,
                            $"MasterAlignmentID different. id: expected {expected.MasterAlignmentID}, actual {actual.MasterAlignmentID}");
            Assert.AreEqual(expected.AlignmentID, actual.AlignmentID,
                            $"AlignmentID different. id: expected {expected.MasterAlignmentID}, actual {actual.MasterAlignmentID}");
            Assert.AreEqual(expected.Name, actual.Name,
                            $"Name different. id: expected {expected.MasterAlignmentID}, actual {actual.MasterAlignmentID}");
            Assert.AreEqual(expected.MassCenter, actual.MassCenter,
                            $"MassCenter different. id: expected {expected.MasterAlignmentID}, actual {actual.MasterAlignmentID}");
            Assert.AreEqual(expected.TimesCenter.Value, actual.TimesCenter.Value,
                            $"TimesCenter value different. id: expected {expected.MasterAlignmentID}, actual {actual.MasterAlignmentID}");
            Assert.AreEqual(expected.TimesCenter.MainType, actual.TimesCenter.MainType,
                            $"TimesCenter type different. id: expected {expected.MasterAlignmentID}, actual {actual.MasterAlignmentID}");
            Assert.AreEqual(expected.TimesCenter.Unit, actual.TimesCenter.Unit,
                            $"TimesCenter unit different. id: expected {expected.MasterAlignmentID}, actual {actual.MasterAlignmentID}");
            Assert.AreEqual(expected.HeightAverage, actual.HeightAverage,
                            $"HeightAverage different. id: expected {expected.MasterAlignmentID}, actual {actual.MasterAlignmentID}");
            CollectionAssert.AreEquivalent(expected.MSRawID2MspBasedMatchResult.Keys, actual.MSRawID2MspBasedMatchResult.Keys,
                            $"MsRawID2MspBasedMatchResult key different. id: expected {expected.MasterAlignmentID}, actual {actual.MasterAlignmentID}");
            foreach (var key in expected.MSRawID2MspBasedMatchResult.Keys)
                AreEqual(expected.MSRawID2MspBasedMatchResult[key], actual.MSRawID2MspBasedMatchResult[key],
                         $"MSRawID2MspBasedMatchResult different. id: expected {expected.MasterAlignmentID}, actual {actual.MasterAlignmentID}");
            AreEqual(expected.TextDbBasedMatchResult, actual.TextDbBasedMatchResult,
                     $"TextDbBasedMatchResult different. id: expected {expected.MasterAlignmentID}, actual {actual.MasterAlignmentID}");
            AreEqual(expected.PeakCharacter, actual.PeakCharacter,
                     $"PeakCharacter different. id: expected {expected.MasterAlignmentID}, actual {actual.MasterAlignmentID}");
            Assert.AreEqual(expected.AdductType.AdductIonName, actual.AdductType.AdductIonName,
                            $"AdductIonName different. id: expected {expected.MasterAlignmentID}, actual {actual.MasterAlignmentID}");
            Assert.AreEqual(expected.FeatureFilterStatus.IsBlankFiltered, actual.FeatureFilterStatus.IsBlankFiltered,
                     $"IsBlankFiltered different. id: expected {expected.MasterAlignmentID}, actual {actual.MasterAlignmentID}");
            Assert.AreEqual(expected.AlignedPeakProperties.Count, actual.AlignedPeakProperties.Count);
            foreach ((var exp, var act) in expected.AlignedPeakProperties.Zip(actual.AlignedPeakProperties))
                AreEqual(exp, act, $"AlignedPeakProperties different. id: expected {expected.MasterAlignmentID}, actual {actual.MasterAlignmentID}");
            foreach ((var exp, var act) in expected.AlignmentSpotVariableCorrelations.Zip(actual.AlignmentSpotVariableCorrelations))
                Assert.AreEqual(exp.CorrelateAlignmentID, act.CorrelateAlignmentID,
                    $"AlignmentSpotVariableCorrelations CorrelateAlignmentID different. id: expected {expected.MasterAlignmentID}, actual {actual.MasterAlignmentID}");
            foreach ((var exp, var act) in expected.AlignmentSpotVariableCorrelations.Zip(actual.AlignmentSpotVariableCorrelations))
                Assert.AreEqual(exp.CorrelationScore, act.CorrelationScore,
                    $"AlignmentSpotVariableCorrelations CorrelationScore different. id: expected {expected.MasterAlignmentID}, actual {actual.MasterAlignmentID}");
            CollectionAssert.AreEqual(
                expected.AlignmentSpotVariableCorrelations.Select(corr => corr.CorrelateAlignmentID).ToList(),
                actual.AlignmentSpotVariableCorrelations.Select(corr => corr.CorrelateAlignmentID).ToList(),
                $"AlignmentSpotVariableCorrelations CorrelateAlignmentID different. id: expected {expected.MasterAlignmentID}, actual {actual.MasterAlignmentID}"
                );
            CollectionAssert.AreEqual(
                expected.AlignmentSpotVariableCorrelations.Select(corr => corr.CorrelationScore).ToList(),
                actual.AlignmentSpotVariableCorrelations.Select(corr => corr.CorrelationScore).ToList(),
                $"AlignmentSpotVariableCorrelations CorrelationScore different. id: expected {expected.MasterAlignmentID}, actual {actual.MasterAlignmentID}"
                );
        }
        void AreEqual(AlignmentChromPeakFeature expected, AlignmentChromPeakFeature actual, string message = "") {
            Assert.AreEqual(expected.FileID, actual.FileID, "FileID in " + message);
            Assert.AreEqual(expected.PeakHeightTop, actual.PeakHeightTop, "PeakHeightTop in " + message);
            AreEqual(expected.PeakCharacter, actual.PeakCharacter, "PeakCharacter in " + message);
        }
        void AreEqual(MsScanMatchResult expected, MsScanMatchResult actual, string message = "") {
            if (expected == null && actual == null) return;
            if (expected == null || actual == null) Assert.Fail(message);
            Assert.AreEqual(expected.TotalScore, actual.TotalScore, "TotalScore in " + message);
            Assert.AreEqual(expected.LibraryID, actual.LibraryID, "LibraryID in " + message);
        }
        void AreEqual(IonFeatureCharacter expected, IonFeatureCharacter actual, string message = "") {
            Assert.AreEqual(expected.IsotopeWeightNumber, actual.IsotopeWeightNumber, "IsotopeWightNumber in " + message);
            Assert.AreEqual(expected.Charge, actual.Charge, "Charge in " + message);
            CollectionAssert.AreEquivalent(
                expected.PeakLinks.Select(p => p.Character).ToList(),
                actual.PeakLinks.Select(p => p.Character).ToList(),
                "Character in " + message
                );
            CollectionAssert.AreEquivalent(
                expected.PeakLinks.Select(p => p.LinkedPeakID).ToList(),
                actual.PeakLinks.Select(p => p.LinkedPeakID).ToList(),
                "LinkedPeakID in " + message
                );
            Assert.AreEqual(expected.AdductType.AdductIonName, actual.AdductType.AdductIonName, "AdductIonName in " + message);
            Assert.AreEqual(expected.PeakGroupID, actual.PeakGroupID, "PeakGroupID in " + message);
        }
        #endregion

        #region builder
        List<AlignmentSpotProperty> BatchBuildAlignmentSpotProperty(int n, double d_mass = 0, double d_time = 0) {
            return Enumerable.Range(0, n).Select(i => BuildAlignmentSpotProperty(i, d_mass * i, d_time * i)).ToList();
        }

        List<AlignmentChromPeakFeature> BatchBuildAlignmentChromPeakFeature(int n, double d_peak = 0) {
            return Enumerable.Range(0, n).Select(i => BuildAlignmentChromPeakFeature(id: i, peak: 10000 + i * d_peak)).ToList();
        }

        AlignmentSpotProperty BuildAlignmentSpotProperty(int id = 0, double d_mass = 0, double d_time = 0) {
            var result = new AlignmentSpotProperty
            {
                MasterAlignmentID = id, AlignmentID = id, RepresentativeFileID = 2,
                MassCenter = 100 + d_mass, TimesCenter = new ChromXs(20 + d_time),
                MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>
                {
                    {0, BuildMsScanMatchResult(0, 0.1f) },
                    {1, BuildMsScanMatchResult(1, 0.3f) },
                    {2, BuildMsScanMatchResult(2, 0.9f) },
                    {3, BuildMsScanMatchResult(3, 0.5f) },
                },
                TextDbBasedMatchResult = BuildMsScanMatchResult(id:5),
                PeakCharacter = BuildIonFeatureCharacter(),
                AdductType = new AdductIon { AdductIonName = "[M+H]+" },
                FeatureFilterStatus = new FeatureFilterStatus { IsBlankFiltered = false },
                AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                {
                    BuildAlignmentChromPeakFeature(fileid: 0, peak: 10000),
                    BuildAlignmentChromPeakFeature(fileid: 1, peak: 10200),
                    BuildAlignmentChromPeakFeature(fileid: 2, peak: 10300),
                    BuildAlignmentChromPeakFeature(fileid: 3, peak: 9500),
                }
            };
            result.Name = result.TextDbBasedMatchResult.Name;
            result.HeightAverage = (float)result.AlignedPeakProperties.Average(peak => peak.PeakHeightTop);
            return result;
        }

        AlignmentChromPeakFeature BuildAlignmentChromPeakFeature(int id = 0, int fileid = 0, double peak = 10000) {
            return new AlignmentChromPeakFeature
            {
                PeakID = id, FileID = fileid, PeakHeightTop = peak,
                PeakCharacter = BuildIonFeatureCharacter(),
            };
        }

        MsScanMatchResult BuildMsScanMatchResult(int id = 0, float score = 1) {
            return new MsScanMatchResult { TotalScore = score, LibraryID = id, Name = "Metabolite" + id, IsSpectrumMatch = true };
        }

        IonFeatureCharacter BuildIonFeatureCharacter(int weight = 0, int charge = 1) {
            return new IonFeatureCharacter
            {
                IsotopeWeightNumber = weight, Charge = charge,
                PeakLinks = new List<LinkedPeakFeature>(),
                AdductType = new AdductIon { AdductIonName = "[M+H]+" },
            };
        }
        #endregion
        #endregion
    }
}