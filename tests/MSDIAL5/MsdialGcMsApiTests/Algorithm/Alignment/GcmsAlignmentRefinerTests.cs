using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parser;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialGcMsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialGcMsApi.Algorithm.Alignment.Tests
{
    [TestClass()]
    public class GcmsAlignmentRefinerTests
    {
        [TestMethod()]
        public void RefineBaseTest() {
            var param = new MsdialGcmsParameter
            {
                OnlyReportTopHitInMspSearch = false, OnlyReportTopHitInTextDBSearch = false,
                FileID_AnalysisFileType = new Dictionary<int, AnalysisFileType>
                {
                    { 0, AnalysisFileType.Blank }, { 1, AnalysisFileType.Sample },
                    { 2, AnalysisFileType.Sample }, { 3, AnalysisFileType.Sample }
                },
                FileID_ClassName = new Dictionary<int, string>
                {
                    { 0, "A" }, { 1, "B" },
                    { 2, "C" }, { 3, "D" }
                },
                RetentionTimeAlignmentTolerance = 0.05f,
                Ms1AlignmentTolerance = 0.015f,
                FoldChangeForBlankFiltering = 0.1f,
                BlankFiltering = BlankFiltering.SampleMaxOverBlankAve,
                IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = false,
                IsKeepRemovableFeaturesAndAssignedTagForChecking = false,
                IsKeepRefMatchedMetaboliteFeatures = false,
                IsKeepSuggestedMetaboliteFeatures = false,

                AlignmentIndexType = AlignmentIndexType.RT,
                // RiCompoundType = RiCompoundType.Alkanes,
            };
            var iupac = new IupacDatabase();
            var refiner = Create(param, iupac);

            var alignments = BatchBuildAlignmentSpotProperty(4, d_mass: param.CentroidMs1Tolerance, d_time: 0.025);

            var expects = BatchBuildAlignmentSpotProperty(4, d_mass: param.CentroidMs1Tolerance, d_time: 0.025);
            (var actuals, _) = refiner.Refine(alignments);

            Assert.AreEqual(expects.Count, actuals.Count);
            foreach ((var expect, var actual) in expects.Zip(actuals))
                AreEqual(expect, actual);
        }

        [TestMethod()]
        public void RefineRTSimilarTest() {
            var param = new MsdialGcmsParameter
            {
                OnlyReportTopHitInMspSearch = false, OnlyReportTopHitInTextDBSearch = false,
                FileID_AnalysisFileType = new Dictionary<int, AnalysisFileType>
                {
                    { 0, AnalysisFileType.Blank }, { 1, AnalysisFileType.Sample },
                    { 2, AnalysisFileType.Sample }, { 3, AnalysisFileType.Sample }
                },
                FileID_ClassName = new Dictionary<int, string>
                {
                    { 0, "A" }, { 1, "B" },
                    { 2, "C" }, { 3, "D" }
                },
                RetentionTimeAlignmentTolerance = 0.05f,
                Ms1AlignmentTolerance = 0.015f,
                FoldChangeForBlankFiltering = 0.1f,
                BlankFiltering = BlankFiltering.SampleMaxOverBlankAve,
                IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = false,
                IsKeepRemovableFeaturesAndAssignedTagForChecking = false,
                IsKeepRefMatchedMetaboliteFeatures = false,
                IsKeepSuggestedMetaboliteFeatures = false,

                AlignmentIndexType = AlignmentIndexType.RT,
                // RiCompoundType = RiCompoundType.Alkanes,
            };
            var iupac = new IupacDatabase();
            var refiner = Create(param, iupac);

            var alignments = BatchBuildAlignmentSpotProperty(4, d_mass: param.CentroidMs1Tolerance, d_time: 0.025);

            alignments[1].QuantMass = alignments[0].QuantMass + param.CentroidMs1Tolerance * 0.99;
            alignments[1].TimesCenter.RT = new RetentionTime(alignments[0].TimesCenter.RT.Value + 0.025 * 0.99, unit: alignments[1].TimesCenter.RT.Unit);
            alignments[3].QuantMass = alignments[2].QuantMass + param.CentroidMs1Tolerance * 0.99;
            alignments[3].TimesCenter.RT = new RetentionTime(alignments[2].TimesCenter.RT.Value + 0.025 * 0.99, unit: alignments[3].TimesCenter.RT.Unit);
            alignments[3].MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>();
            alignments[3].TextDbBasedMatchResult = null;
            alignments[3].MatchResults.ClearResults();

            var expects = BatchBuildAlignmentSpotProperty(4, d_mass: param.CentroidMs1Tolerance, d_time: 0.025);
            expects[1].QuantMass = expects[0].QuantMass + param.CentroidMs1Tolerance * 0.99;
            expects[1].TimesCenter.RT = new RetentionTime(alignments[0].TimesCenter.RT.Value + 0.025 * 0.99, unit: expects[1].TimesCenter.RT.Unit);
            expects.RemoveAt(3);
            expects.Sort((a, b) => (a.TimesCenter.RT.Value, a.QuantMass).CompareTo((b.TimesCenter.RT.Value, b.QuantMass)));
            for (int i = 0; i < expects.Count; i++) {
                expects[i].AlignmentID = expects[i].MasterAlignmentID = i;
            }

            (var actuals, _) = refiner.Refine(alignments);

            Assert.AreEqual(expects.Count, actuals.Count);
            foreach ((var expect, var actual) in expects.Zip(actuals))
                AreEqual(expect, actual);
        }

        [TestMethod()]
        public void RefineRISimilarAlkanesTest() {
            var param = new MsdialGcmsParameter
            {
                OnlyReportTopHitInMspSearch = false, OnlyReportTopHitInTextDBSearch = false,
                FileID_AnalysisFileType = new Dictionary<int, AnalysisFileType>
                {
                    { 0, AnalysisFileType.Blank }, { 1, AnalysisFileType.Sample },
                    { 2, AnalysisFileType.Sample }, { 3, AnalysisFileType.Sample }
                },
                FileID_ClassName = new Dictionary<int, string>
                {
                    { 0, "A" }, { 1, "B" },
                    { 2, "C" }, { 3, "D" }
                },
                RetentionTimeAlignmentTolerance = 0.05f,
                Ms1AlignmentTolerance = 0.015f,
                FoldChangeForBlankFiltering = 0.1f,
                BlankFiltering = BlankFiltering.SampleMaxOverBlankAve,
                IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = false,
                IsKeepRemovableFeaturesAndAssignedTagForChecking = false,
                IsKeepRefMatchedMetaboliteFeatures = false,
                IsKeepSuggestedMetaboliteFeatures = false,

                AlignmentIndexType = AlignmentIndexType.RI,
                RiCompoundType = RiCompoundType.Alkanes,
            };
            var iupac = new IupacDatabase();
            var refiner = Create(param, iupac);

            var alignments = BatchBuildAlignmentSpotProperty(4, d_mass: param.CentroidMs1Tolerance, d_index: 2.5);
            for (int i = 0; i < alignments.Count; i++) alignments[i].TimesCenter.MainType = ChromXType.RI;
            alignments[1].QuantMass = alignments[0].QuantMass - param.CentroidMs1Tolerance * 0.99;
            alignments[1].TimesCenter.RI = new RetentionIndex(alignments[0].TimesCenter.RI.Value + 2.5 * 0.99);
            alignments[3].QuantMass = alignments[2].QuantMass + param.CentroidMs1Tolerance * 0.99;
            alignments[3].TimesCenter.RI = new RetentionIndex(alignments[2].TimesCenter.RI.Value + 2.5 * 0.99);
            alignments[3].MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>();
            alignments[3].TextDbBasedMatchResult = null;
            alignments[3].MatchResults.ClearResults();

            var expects = BatchBuildAlignmentSpotProperty(4, d_mass: param.CentroidMs1Tolerance, d_index: 2.5);
            for (int i = 0; i < expects.Count; i++) expects[i].TimesCenter.MainType = ChromXType.RI;
            expects[1].QuantMass = expects[0].QuantMass - param.CentroidMs1Tolerance * 0.99;
            expects[1].TimesCenter.RI = new RetentionIndex(alignments[0].TimesCenter.RI.Value + 2.5 * 0.99);
            expects.RemoveAt(3);
            expects.Sort((a, b) => (a.TimesCenter.RI.Value, a.QuantMass).CompareTo((b.TimesCenter.RI.Value, b.QuantMass)));
            for (int i = 0; i < expects.Count; i++) {
                expects[i].AlignmentID = expects[i].MasterAlignmentID = i;
            }

            (var actuals, _) = refiner.Refine(alignments);

            Assert.AreEqual(expects.Count, actuals.Count);
            foreach ((var expect, var actual) in expects.Zip(actuals))
                AreEqual(expect, actual);
        }

        [TestMethod()]
        public void RefineRISimilarFamesTest() {
            var param = new MsdialGcmsParameter
            {
                OnlyReportTopHitInMspSearch = false, OnlyReportTopHitInTextDBSearch = false,
                FileID_AnalysisFileType = new Dictionary<int, AnalysisFileType>
                {
                    { 0, AnalysisFileType.Blank }, { 1, AnalysisFileType.Sample },
                    { 2, AnalysisFileType.Sample }, { 3, AnalysisFileType.Sample }
                },
                FileID_ClassName = new Dictionary<int, string>
                {
                    { 0, "A" }, { 1, "B" },
                    { 2, "C" }, { 3, "D" }
                },
                RetentionTimeAlignmentTolerance = 0.05f,
                Ms1AlignmentTolerance = 0.015f,
                FoldChangeForBlankFiltering = 0.1f,
                BlankFiltering = BlankFiltering.SampleMaxOverBlankAve,
                IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = false,
                IsKeepRemovableFeaturesAndAssignedTagForChecking = false,
                IsKeepRefMatchedMetaboliteFeatures = false,
                IsKeepSuggestedMetaboliteFeatures = false,

                AlignmentIndexType = AlignmentIndexType.RI,
                RiCompoundType = RiCompoundType.Fames,
            };
            var iupac = new IupacDatabase();
            var refiner = Create(param, iupac);

            var alignments = BatchBuildAlignmentSpotProperty(4, d_mass: param.CentroidMs1Tolerance, d_index: 1000);
            for (int i = 0; i < alignments.Count; i++) alignments[i].TimesCenter.MainType = ChromXType.RI;
            alignments[1].QuantMass = alignments[0].QuantMass - param.CentroidMs1Tolerance * 0.99;
            alignments[1].TimesCenter.RI = new RetentionIndex(alignments[0].TimesCenter.RI.Value - 1000 * 0.99);
            alignments[3].QuantMass = alignments[2].QuantMass + param.CentroidMs1Tolerance * 0.99;
            alignments[3].TimesCenter.RI = new RetentionIndex(alignments[2].TimesCenter.RI.Value + 1000 * 0.99);
            alignments[3].MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>();
            alignments[3].TextDbBasedMatchResult = null;
            alignments[3].MatchResults.ClearResults();

            var expects = BatchBuildAlignmentSpotProperty(4, d_mass: param.CentroidMs1Tolerance, d_index: 1000);
            for (int i = 0; i < expects.Count; i++) expects[i].TimesCenter.MainType = ChromXType.RI;
            expects[1].QuantMass = expects[0].QuantMass - param.CentroidMs1Tolerance * 0.99;
            expects[1].TimesCenter.RI = new RetentionIndex(alignments[0].TimesCenter.RI.Value - 1000 * 0.99);
            expects.RemoveAt(3);
            expects.Sort((a, b) => (a.TimesCenter.RI.Value, a.QuantMass).CompareTo((b.TimesCenter.RI.Value, b.QuantMass)));
            for (int i = 0; i < expects.Count; i++) {
                expects[i].AlignmentID = expects[i].MasterAlignmentID = i;
            }

            (var actuals, _) = refiner.Refine(alignments);

            Assert.AreEqual(expects.Count, actuals.Count);
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
        GcmsAlignmentRefiner Create(MsdialGcmsParameter parameter, IupacDatabase iupac) {
            return new GcmsAlignmentRefiner(parameter, iupac, new FacadeMatchResultEvaluator());
        }

        List<AlignmentSpotProperty> BatchBuildAlignmentSpotProperty(int n, double d_mass = 0, double d_time = 0, double d_index = 0) {
            return Enumerable.Range(0, n).Select(i => BuildAlignmentSpotProperty(id: i, d_mass: d_mass * i, d_time: d_time * i, d_index: d_index * i)).ToList();
        }

        List<AlignmentChromPeakFeature> BatchBuildAlignmentChromPeakFeature(int n, double d_peak = 0) {
            return Enumerable.Range(0, n).Select(i => BuildAlignmentChromPeakFeature(id: i, peak: 10000 + i * d_peak)).ToList();
        }

        AlignmentSpotProperty BuildAlignmentSpotProperty(int id = 0, double d_mass = 0, double d_time = 0, double d_index = 0) {
            var mspResults = new Dictionary<int, MsScanMatchResult>
            {
                { 0, BuildMsScanMatchResult(0, 0.1f) },
                { 1, BuildMsScanMatchResult(1, 0.3f) },
                { 2, BuildMsScanMatchResult(2, 0.9f) },
                { 3, BuildMsScanMatchResult(3, 0.5f) },
            };
            var textDbResult = BuildMsScanMatchResult(id: 5);
            var result = new AlignmentSpotProperty
            {
                MasterAlignmentID = id, AlignmentID = id, RepresentativeFileID = 2,
                QuantMass = 100 + d_mass,
                TimesCenter = new ChromXs
                {
                    RT = new RetentionTime(20 + d_time),
                    RI = new RetentionIndex(5 + d_index)
                },
                MSRawID2MspBasedMatchResult = mspResults,
                TextDbBasedMatchResult = textDbResult,
                PeakCharacter = BuildIonFeatureCharacter(),
                FeatureFilterStatus = new FeatureFilterStatus { IsBlankFiltered = false },
                AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                {
                    BuildAlignmentChromPeakFeature(fileid: 0, peak: 10000),
                    BuildAlignmentChromPeakFeature(fileid: 1, peak: 10200),
                    BuildAlignmentChromPeakFeature(fileid: 2, peak: 10300),
                    BuildAlignmentChromPeakFeature(fileid: 3, peak: 9500),
                }
            };
            result.SetAdductType(AdductIon.GetAdductIon("[M+H]+"));
            result.Name = result.TextDbBasedMatchResult.Name;
            result.HeightAverage = (float)result.AlignedPeakProperties.Average(peak => peak.PeakHeightTop);
            result.MatchResults.AddMspResults(mspResults);
            result.MatchResults.AddTextDbResult(textDbResult);
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
                AdductType = AdductIon.GetAdductIon("[M+H]+"),
            };
        }
        #endregion
        #endregion
    }
}