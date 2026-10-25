using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation.LipidRt.Tests
{
    /// <summary>
    /// The check's input built from MS-DIAL's own peaks and spots: what is read, and the eligibility rule of SPEC 2.1
    /// with addendum A6 (manual annotations, text-library matches and internal standards are not evaluated).
    /// </summary>
    [TestClass]
    public class LipidRtInputBuilderTests
    {
        private static MsScanMatchResult LbmMatch(string name, bool chains = true, bool referenceMatched = true) {
            return new MsScanMatchResult
            {
                Name = name,
                IsPrecursorMzMatch = true,
                IsSpectrumMatch = true,
                IsLipidClassMatch = true,
                IsLipidChainsMatch = chains,
                IsReferenceMatched = referenceMatched,
                Source = SourceType.MspDB,
                AnnotatorID = "lbm",
                WeightedDotProduct = 0.8f,
            };
        }

        private static ChromatogramPeakFeature Peak(int id, double rt, double mz, double height, string adduct, string ontology, MsScanMatchResult result, string name = null) {
            var peak = new ChromatogramPeakFeature(new BaseChromatogramPeakFeature
            {
                Mass = mz, PeakHeightTop = height, ChromXsLeft = new ChromXs(rt), ChromXsTop = new ChromXs(rt), ChromXsRight = new ChromXs(rt),
            })
            {
                MasterPeakID = id,
                Ontology = ontology ?? string.Empty,
                Name = name ?? result?.Name ?? string.Empty,
            };
            peak.SetAdductType(AdductIon.GetAdductIon(adduct));
            if (result != null) {
                peak.MatchResults.AddResult(result);
            }
            return peak;
        }

        [TestMethod]
        public void PeaksAreReadWithTheirRepresentativeMatch() {
            var peaks = new List<ChromatogramPeakFeature>
            {
                Peak(0, 10.5, 760.5851, 1e6, "[M+H]+", "PC", LbmMatch("PC 34:1|PC 16:0_18:1")),
                Peak(1, 10.9, 762.6007, 5e5, "[M+H]+", "PC", LbmMatch("PC 34:0", chains: false)),
                Peak(2, 1.2, 300.1, 1e4, "[M+H]+", null, null),
            };
            var input = LipidRtInputBuilder.FromPeaks(peaks, IonMode.Positive);

            Assert.AreEqual(2, input.Candidates.Count, "an unannotated peak is context only");
            Assert.AreEqual(3, input.Context.Count, "every peak is context for the artefact layer");
            Assert.AreEqual(1.2, input.RtMin, 1e-9, "the RT range runs over every peak, not only lipids");
            Assert.AreEqual(10.9, input.RtMax, 1e-9);

            var chain = input.Candidates.Single(c => c.Id == 0);
            Assert.IsTrue(chain.IsEligible, chain.IneligibleReason);
            Assert.AreEqual(LipidRtTagLevel.Chain, chain.TagLevel);
            Assert.AreEqual(new LipidRtComposition("PC", 34, 1, 0), chain.Composition);
            Assert.AreEqual("PC 34:1", chain.Name);
            Assert.AreEqual("PC 16:0_18:1", chain.Molecular);
            Assert.AreEqual(10.5, chain.Rt, 1e-9);
            Assert.AreEqual(760.5851, chain.Mz, 1e-9);
            Assert.AreEqual(1e6, chain.Height, 1e-9);
            Assert.AreEqual("[M+H]+", chain.Adduct);
            Assert.AreEqual(0.8, chain.WeightedDot, 1e-6);

            var classLevel = input.Candidates.Single(c => c.Id == 1);
            Assert.IsTrue(classLevel.IsEligible, "D3: class-level matches are eligible");
            Assert.AreEqual(LipidRtTagLevel.Class, classLevel.TagLevel);
        }

        [TestMethod]
        public void SpotsAreReadOnTheAlignedAxisWithTheAverageHeight() {
            var spot = new AlignmentSpotProperty
            {
                MasterAlignmentID = 42,
                TimesCenter = new ChromXs(12.25),
                MassCenter = 868.7372,
                HeightAverage = 2.5e5f,
                Ontology = "TG",
            };
            spot.SetAdductType(AdductIon.GetAdductIon("[M+NH4]+"));
            spot.MatchResults.AddResult(LbmMatch("TG 52:2|TG 16:0_18:1_18:1"));
            var input = LipidRtInputBuilder.FromSpots(new[] { spot }, IonMode.Positive);
            var c = input.Candidates.Single();
            Assert.AreEqual(42, c.Id);
            Assert.AreEqual(12.25, c.Rt, 1e-9);
            Assert.AreEqual(868.7372, c.Mz, 1e-9);
            Assert.AreEqual(2.5e5, c.Height, 1e-3);
            Assert.AreEqual("[M+NH4]+", c.Adduct);
            Assert.AreEqual(new LipidRtComposition("TG", 52, 2, 0), c.Composition);
            Assert.IsTrue(c.IsEligible);
        }

        [TestMethod]
        public void PeopleTextLibrariesAndInternalStandardsAreNotEvaluated() {
            var manual = LbmMatch("PC 34:1|PC 16:0_18:1");
            manual.Source |= SourceType.Manual;
            var text = new MsScanMatchResult
            {
                Name = "PC 34:1", IsPrecursorMzMatch = true, IsRtMatch = true, IsReferenceMatched = true, Source = SourceType.TextDB, AnnotatorID = "text",
            };
            var peaks = new List<ChromatogramPeakFeature>
            {
                Peak(0, 10.5, 760.5851, 1e6, "[M+H]+", "PC", manual),
                Peak(1, 10.5, 760.5851, 1e6, "[M+H]+", "PC", text),
                Peak(2, 10.4, 753.6134, 1e6, "[M+H]+", "PC", LbmMatch("PC 33:1(d7)|PC 15:0_18:1(d7)")),
                Peak(3, 10.2, 758.5694, 1e6, "[M+H]+", "PC", LbmMatch("PC 34:2|PC 16:0_18:2"), name: "Unsettled: PC 34:2"),
                Peak(4, 10.1, 758.5694, 1e6, "[M+H]+", "PC", LbmMatch("PC 34:2|PC 16:0_18:2", referenceMatched: false)),
                Peak(5, 3.1, 285.2, 1e6, "[M+H]+", "Others", LbmMatch("Dioctyl phthalate", chains: false)),
            };
            var input = LipidRtInputBuilder.FromPeaks(peaks, IonMode.Positive);
            string Reason(int id) => input.Candidates.Single(c => c.Id == id).IneligibleReason;
            Assert.IsTrue(input.Candidates.All(c => !c.IsEligible));
            StringAssert.Contains(Reason(0), "manual");
            StringAssert.Contains(Reason(1), "no_spectrum_match");
            StringAssert.Contains(Reason(1), "not_spectral_library");
            StringAssert.Contains(Reason(2), "internal_standard");
            StringAssert.Contains(Reason(3), "unsettled");
            StringAssert.Contains(Reason(4), "not_ref_matched");
            StringAssert.Contains(Reason(5), "nonlipid");
            Assert.IsTrue(input.Candidates.Single(c => c.Id == 2).IsLipid, "an internal standard is still a brighter partner for the artefact layer");

            var result = new LipidRtCheck(new LipidRtCheckParameter()).Evaluate(input);
            Assert.IsTrue(result.Results.All(r => r.Reason == LipidRtReason.Ineligible && r.Score is null));
        }

        /// <summary>
        /// A match against a computed spectrum is not evaluated: retention-time rules agreeing with it would confirm a name
        /// that no acquired spectrum supports. The evidence record says so for a metabolomics-scored annotator; for one
        /// scored as lipidomics, which records RuleBased as the LBM does, the library does.
        /// </summary>
        [TestMethod]
        public void InSilicoMatchesAreNotEvaluated() {
            var spectrumPredicted = LbmMatch("PC 34:1|PC 16:0_18:1");
            spectrumPredicted.AnnotatorID = "glacier";
            spectrumPredicted.EvidenceSource = AnnotationEvidenceSource.BySpectrumPredictionTool;
            var structurePredicted = LbmMatch("PC 34:2|PC 16:0_18:2");
            structurePredicted.AnnotatorID = "msfinder";
            structurePredicted.EvidenceSource = AnnotationEvidenceSource.ByStructurePredictionTool;
            var ruleBasedFromAPredictedLibrary = LbmMatch("PC 36:1|PC 18:0_18:1");
            ruleBasedFromAPredictedLibrary.AnnotatorID = "glacier_lipid";
            ruleBasedFromAPredictedLibrary.EvidenceSource = AnnotationEvidenceSource.RuleBased;
            var lbm = LbmMatch("PC 36:2|PC 18:1_18:1");
            lbm.EvidenceSource = AnnotationEvidenceSource.RuleBased;
            var peaks = new List<ChromatogramPeakFeature>
            {
                Peak(0, 10.5, 760.5851, 1e6, "[M+H]+", "PC", spectrumPredicted),
                Peak(1, 10.1, 758.5694, 1e6, "[M+H]+", "PC", structurePredicted),
                Peak(2, 11.5, 788.6164, 1e6, "[M+H]+", "PC", ruleBasedFromAPredictedLibrary),
                Peak(3, 11.1, 786.6007, 1e6, "[M+H]+", "PC", lbm),
            };
            var input = LipidRtInputBuilder.FromPeaks(peaks, IonMode.Positive, new HashSet<string> { "glacier_lipid" });
            LipidRtCandidate Candidate(int id) => input.Candidates.Single(c => c.Id == id);

            foreach (var id in new[] { 0, 1, 2 }) {
                Assert.IsFalse(Candidate(id).IsEligible, peaks[id].Name);
                Assert.AreEqual("in_silico", Candidate(id).IneligibleReason, peaks[id].Name);
            }
            Assert.IsTrue(Candidate(3).IsEligible, "the LBM's rule-based match is what the check evaluates: " + Candidate(3).IneligibleReason);
            Assert.IsTrue(LipidRtInputBuilder.FromPeaks(peaks, IonMode.Positive).Candidates.Single(c => c.Id == 2).IsEligible,
                "without the library, a rule-based record cannot be told apart from the LBM's");
            var result = new LipidRtCheck(new LipidRtCheckParameter()).Evaluate(input);
            Assert.IsTrue(result.Results.Where(r => r.Id != 3).All(r => r.Outcome == LipidRtOutcome.NotEvaluable && r.Reason == LipidRtReason.Ineligible));
        }

        /// <summary>
        /// The annotators of an MSP library declared in silico, and no others: the LBM is generated too, but its matches are
        /// the lipid rule set's.
        /// </summary>
        [TestMethod]
        public void ThePredictedLibraryAnnotatorsAreThoseOfAPredictedMspLibrary() {
            var databases = DataBaseStorage.CreateEmpty();
            AddLibrary(databases, "strict_db", DataBaseSource.Msp, "strict", "broad");
            AddLibrary(databases, "glacier_db", DataBaseSource.PredictedMsp, "glacier");
            AddLibrary(databases, "lbm_db", DataBaseSource.Lbm, "lbm");

            CollectionAssert.AreEquivalent(new[] { "glacier" }, LipidRtInputBuilder.PredictedLibraryAnnotators(databases).ToArray());
            Assert.AreEqual(0, LipidRtInputBuilder.PredictedLibraryAnnotators(null).Count);
        }

        private static void AddLibrary(DataBaseStorage databases, string id, DataBaseSource kind, params string[] annotatorIds) {
            var database = new MoleculeDataBase(new List<MoleculeMsReference>(), id, kind, SourceType.MspDB, id + ".msp");
            var parameter = new ParameterBase { TargetOmics = TargetOmics.Metabolomics, };
            var pairs = new List<IAnnotatorParameterPair<MoleculeDataBase>>();
            foreach (var annotatorId in annotatorIds) {
                var annotator = new MassAnnotator(database, parameter.MspSearchParam, TargetOmics.Metabolomics, SourceType.MspDB, annotatorId, 1);
                pairs.Add(new MetabolomicsAnnotatorParameterPair(annotator.Save(), new AnnotationQueryFactory(annotator, parameter.PeakPickBaseParam, parameter.MspSearchParam, ignoreIsotopicPeak: true)));
            }
            databases.AddMoleculeDataBase(database, pairs);
        }
    }
}
