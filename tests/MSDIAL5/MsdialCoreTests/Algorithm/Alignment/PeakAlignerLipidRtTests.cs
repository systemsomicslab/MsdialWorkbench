using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Annotation.LipidRt;
using CompMs.MsdialCore.Algorithm.Annotation.LipidRt.Tests;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Alignment.Tests
{
    /// <summary>
    /// Hook B of the lipid RT check inside <see cref="PeakAligner.Alignment"/>: it must see the annotations the alignment
    /// result is saved with, so it runs after every step that changes them. Isotope tracking is such a step: it clears the
    /// annotation of every spot represented by a file other than the non-labelled reference.
    /// </summary>
    /// <remarks>
    /// The spots are the TG grid of the reference's synthetic tests (C 48-56 even, DB 0-3, RT = 2 + 0.15 C - 0.25 DB,
    /// [M+NH4]+, chain-level LBM matches), handed to the aligner by its refiner, plus two unannotated spots at 0.5 and 19.5
    /// min for the run span. Every spot is represented by file 1, the labelled file; file 0 is the non-labelled reference.
    /// </remarks>
    [TestClass]
    public class PeakAlignerLipidRtTests
    {
        private const string CheckSource = "MsdialLipidRtCheck/1.1";

        [TestMethod]
        public void HookBConfirmsTheGridWithoutIsotopeTracking() {
            var aligner = CreateAligner(trackIsotopeLabels: false);

            var container = aligner.Alignment(Files(), new AlignmentFileBean { FileID = 0 }, null);

            var tg521 = Find(container.AlignmentSpotProperties, 52, 1);
            Assert.IsTrue(aligner.LipidRtRun.Ran, aligner.LipidRtRun.Describe());
            Assert.IsTrue(tg521.TagCollection.IsSelected(PeakSpotTag.CONFIRMED), "the grid is confirmed when nothing clears its annotations");
            Assert.AreEqual(CheckSource, tg521.TagCollection.GetSource(PeakSpotTag.CONFIRMED));
            Assert.AreEqual(LipidRtOutcome.Confirmed, tg521.LipidRtVerdict.Outcome);
        }

        [TestMethod]
        public void HookBRunsAfterIsotopeTrackingHasClearedTheAnnotations() {
            var aligner = CreateAligner(trackIsotopeLabels: true);

            var container = aligner.Alignment(Files(), new AlignmentFileBean { FileID = 0 }, null);

            var spots = container.AlignmentSpotProperties;
            Assert.AreEqual(22, spots.Count);
            Assert.IsTrue(spots.All(s => string.IsNullOrEmpty(s.Name)), "isotope tracking cleared every annotation");
            Assert.IsNotNull(aligner.LipidRtRun);
            Assert.IsNull(aligner.LipidRtRun.Error);
            Assert.AreEqual(0, aligner.LipidRtRun.Tags.Set, aligner.LipidRtRun.Describe());
            foreach (var spot in spots) {
                Assert.IsFalse(spot.TagCollection.IsSelected(PeakSpotTag.CONFIRMED), $"spot {spot.MasterAlignmentID} keeps no Confirmed of the check");
                Assert.IsNull(spot.LipidRtVerdict, $"spot {spot.MasterAlignmentID} keeps no verdict about an annotation it no longer has");
            }
        }

        /// <summary>
        /// The aligner passes the annotators of the project's libraries of predicted spectra to hook B, so a lipid named by
        /// one is not evaluated even when its record says RuleBased, as a match scored under lipidomics does (Q5).
        /// </summary>
        [TestMethod]
        public void HookBDoesNotEvaluateAMatchFromALibraryOfPredictedSpectra() {
            var aligner = CreateAligner(trackIsotopeLabels: false, predictedLipid: "TG 52:1|");
            aligner.PredictedLibraryAnnotators = new HashSet<string> { "glacier_lipid" };

            var container = aligner.Alignment(Files(), new AlignmentFileBean { FileID = 0 }, null);

            var tg521 = Find(container.AlignmentSpotProperties, 52, 1);
            Assert.IsTrue(aligner.LipidRtRun.Ran, aligner.LipidRtRun.Describe());
            Assert.AreEqual("glacier_lipid", tg521.MatchResults.Representative.AnnotatorID);
            Assert.IsFalse(tg521.TagCollection.IsSelected(PeakSpotTag.CONFIRMED));
            Assert.AreEqual(LipidRtOutcome.NotEvaluable, tg521.LipidRtVerdict.Outcome);
            StringAssert.Contains(tg521.LipidRtVerdict.Evidence, "in silico");
            Assert.IsTrue(Find(container.AlignmentSpotProperties, 52, 2).TagCollection.IsSelected(PeakSpotTag.CONFIRMED), "the LBM-named neighbour is still confirmed");
        }

        private static PeakAligner CreateAligner(bool trackIsotopeLabels, string predictedLipid = null) {
            var parameter = new ParameterBase
            {
                MachineCategory = MachineCategory.LCMS,
                TargetOmics = TargetOmics.Lipidomics,
                IonMode = IonMode.Positive,
                FileID_ClassName = new Dictionary<int, string> { { 0, "0" }, { 1, "1" }, },
                TrackingIsotopeLabels = trackIsotopeLabels,
                NonLabeledReferenceID = 0,
                RetentionTimeAlignmentTolerance = 0.01f,
            };
            var iupac = new IupacDatabase();
            var factory = new GridFactory(parameter, iupac, new MockFiller(parameter), new MockAccessor(new List<List<IMSScanProperty>>()), predictedLipid);
            var aligner = factory.CreatePeakAligner();
            aligner.ProviderFactory = new FakeDataProviderFactory();
            return aligner;
        }

        private static List<AnalysisFileBean> Files() {
            return new List<AnalysisFileBean>
            {
                new AnalysisFileBean { AnalysisFileId = 0, AnalysisFileClass = "0" },
                new AnalysisFileBean { AnalysisFileId = 1, AnalysisFileClass = "1" },
            };
        }

        private static AlignmentSpotProperty Find(IEnumerable<AlignmentSpotProperty> spots, int c, int db) {
            return spots.Single(s => s.Name.StartsWith($"TG {c}:{db}|", StringComparison.Ordinal));
        }

        /// <param name="predictedLipid">The start of the name of the spot whose match is the predicted library's
        /// annotator's ("glacier_lipid", RuleBased); null for none.</param>
        private static List<AlignmentSpotProperty> Grid(string predictedLipid) {
            var objects = new List<(double Rt, double Mz, string Name)> { (0.5, 300.1, null), (19.5, 301.1, null) };
            foreach (var c in new[] { 48, 50, 52, 54, 56 }) {
                foreach (var db in new[] { 0, 1, 2, 3 }) {
                    objects.Add((2 + 0.15 * c - 0.25 * db, LipidRtSyntheticRun.Mass("TG", c, db) + LipidRtSyntheticRun.AdductOf("[M+NH4]+"), $"TG {c}:{db}|TG 16:0_16:0_{c - 32}:{db}"));
                }
            }
            var spots = new List<AlignmentSpotProperty>();
            for (var id = 0; id < objects.Count; id++) {
                var (rt, mz, name) = objects[id];
                var spot = new AlignmentSpotProperty
                {
                    MasterAlignmentID = id,
                    AlignmentID = id,
                    TimesCenter = new ChromXs(rt),
                    MassCenter = mz,
                    HeightAverage = 1e5f,
                    HeightMax = 1e5f,
                    HeightMin = 1e5f,
                    RepresentativeFileID = 1,
                    Ontology = name is null ? string.Empty : "TG",
                    Name = name ?? string.Empty,
                    AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                    {
                        new AlignmentChromPeakFeature { FileID = 0, PeakID = id, MasterPeakID = id, PeakHeightTop = 1e5 },
                        new AlignmentChromPeakFeature { FileID = 1, PeakID = id, MasterPeakID = id, PeakHeightTop = 1e5 },
                    },
                    AlignmentDriftSpotFeatures = new List<AlignmentSpotProperty>(),
                };
                spot.SetAdductType(AdductIon.GetAdductIon("[M+NH4]+"));
                if (name != null) {
                    spot.MatchResults.AddResult(new MsScanMatchResult
                    {
                        Name = name,
                        IsPrecursorMzMatch = true,
                        IsSpectrumMatch = true,
                        IsLipidClassMatch = true,
                        IsLipidChainsMatch = true,
                        IsReferenceMatched = true,
                        Source = SourceType.MspDB,
                        AnnotatorID = predictedLipid != null && name.StartsWith(predictedLipid, StringComparison.Ordinal) ? "glacier_lipid" : "lbm",
                        EvidenceSource = AnnotationEvidenceSource.RuleBased,
                        WeightedDotProduct = 0.8f,
                        TotalScore = 0.8f,
                    });
                }
                spots.Add(spot);
            }
            return spots;
        }

        /// <summary>Hands the aligner the grid as its refined spots, whatever the joiner found.</summary>
        private sealed class GridRefiner : IAlignmentRefiner
        {
            private readonly string _predictedLipid;

            public GridRefiner(string predictedLipid) {
                _predictedLipid = predictedLipid;
            }

            public (List<AlignmentSpotProperty>, List<int>) Refine(IList<AlignmentSpotProperty> alignments) {
                var spots = Grid(_predictedLipid);
                return (spots, spots.Select(s => s.MasterAlignmentID).ToList());
            }
        }

        private sealed class NoPeakJoiner : IPeakJoiner
        {
            public List<AlignmentSpotProperty> Join(IReadOnlyList<AnalysisFileBean> analysisFiles, int referenceId, DataAccessor accessor) {
                return new List<AlignmentSpotProperty>();
            }
        }

        private sealed class GridFactory : AlignmentProcessFactory
        {
            private readonly MockFiller _filler;
            private readonly DataAccessor _accessor;
            private readonly string _predictedLipid;

            public GridFactory(ParameterBase param, IupacDatabase iupac, MockFiller filler, DataAccessor accessor, string predictedLipid) : base(param, iupac) {
                _filler = filler;
                _accessor = accessor;
                _predictedLipid = predictedLipid;
            }

            public override IAlignmentRefiner CreateAlignmentRefiner() => new GridRefiner(_predictedLipid);
            public override DataAccessor CreateDataAccessor() => _accessor;
            public override IGapFiller CreateGapFiller() => _filler;
            public override PeakAligner CreatePeakAligner() => new PeakAligner(this, null);
            public override IPeakJoiner CreatePeakJoiner() => new NoPeakJoiner();
        }
    }
}
