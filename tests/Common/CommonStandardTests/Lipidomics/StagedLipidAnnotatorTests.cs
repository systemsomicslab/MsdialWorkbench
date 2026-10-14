using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class StagedLipidAnnotatorTests
    {
        private static readonly AdductIon Adduct = AdductIon.GetAdductIon("[M+H]+");
        private static readonly ILipid Seed = new Lipid(LbmClass.PC, 785.5935, new TotalChain(36, 2, 0, 2, 0, 0));

        [TestMethod()]
        public void BestMolecularSpeciesIsReported() {
            var annotator = CreateAnnotator(ChainIonsBySn1Carbon(), Species((16, 0, 20, 2), (18, 1, 18, 1)));

            // Only the 18:1 chain ion is observed: PC 18:1_18:1 explains the spectrum, PC 16:0_20:2 doesn't.
            var result = annotator.Annotate(Seed, Adduct, Spectrum((184, 100), (1018, 50)));

            Assert.AreEqual(StagedLipidLevel.MolecularSpecies, result.Level);
            Assert.AreEqual("PC 18:1_18:1", result.Lipid.Name);
        }

        [TestMethod()]
        public void TiedMolecularSpeciesFallBackToClass() {
            // Chain ions that don't depend on the chains: every candidate explains the spectrum equally well.
            var annotator = CreateAnnotator(ConstantChainIon(), Species((16, 0, 20, 2), (18, 1, 18, 1)));

            var result = annotator.Annotate(Seed, Adduct, Spectrum((184, 100), (500, 50)));

            Assert.AreEqual(StagedLipidLevel.Class, result.Level);
            Assert.AreEqual(StagedLipidStopReason.MolecularSpeciesAmbiguous, result.StopReason);
            Assert.AreSame(Seed, result.Lipid);
        }

        [TestMethod()]
        public void TooManyCandidatesStopsAtClass() {
            var annotator = CreateAnnotator(ChainIonsBySn1Carbon(), Species((16, 0, 20, 2), (18, 1, 18, 1)), maxCandidates: 1);

            var result = annotator.Annotate(Seed, Adduct, Spectrum((184, 100), (1018, 50)));

            Assert.AreEqual(StagedLipidLevel.Class, result.Level);
            Assert.AreEqual(StagedLipidStopReason.TooManyCandidates, result.StopReason);
        }

        [TestMethod()]
        public void MissingClassIonGivesNoAnnotation() {
            var annotator = CreateAnnotator(ChainIonsBySn1Carbon(), Species((18, 1, 18, 1)));

            var result = annotator.Annotate(Seed, Adduct, Spectrum((1018, 50)));

            Assert.AreEqual(StagedLipidLevel.None, result.Level);
            Assert.IsNull(result.Lipid);
        }

        private static StagedLipidAnnotator CreateAnnotator(Func<Lipid, LipidFragment[]> fragments, ILipidGenerator generator, int maxCandidates = StagedLipidAnnotator.DefaultMaxCandidates) {
            return new StagedLipidAnnotator(new[] { new FakeModel(fragments) }, generator, new StagedLipidSpectrumEvaluator(0.025), maxCandidates);
        }

        private static readonly LipidFragmentRule ClassRule = new LipidFragmentRule(LipidFragmentKind.ClassIon, "class", 999d, true, false, null, null, 100d);
        private static readonly LipidFragmentRule ChainRule = new LipidFragmentRule(LipidFragmentKind.ChainIon, "chain", 500d, true, false, null, null, 10d);

        // A class ion at 184 and a chain ion at 1000 + the first chain's carbon count.
        private static Func<Lipid, LipidFragment[]> ChainIonsBySn1Carbon() {
            return lipid => new[] {
                new LipidFragment(ClassRule, 184d),
                new LipidFragment(ChainRule, 1000d + lipid.Chains.GetDeterminedChains()[0].CarbonCount),
            };
        }

        private static Func<Lipid, LipidFragment[]> ConstantChainIon() {
            return lipid => new[] {
                new LipidFragment(ClassRule, 184d),
                new LipidFragment(ChainRule, 500d),
            };
        }

        private static ILipidGenerator Species(params (int C1, int D1, int C2, int D2)[] chains) {
            return new FakeGenerator(chains.Select(c => (ILipid)new Lipid(LbmClass.PC, 785.5935, new MolecularSpeciesLevelChains(new AcylChain(c.C1, new DoubleBond(c.D1), new Oxidized(0)), new AcylChain(c.C2, new DoubleBond(c.D2), new Oxidized(0))))).ToArray());
        }

        private static List<SpectrumPeak> Spectrum(params (double Mz, double Intensity)[] peaks) {
            return peaks.Select(p => new SpectrumPeak(p.Mz, p.Intensity)).ToList();
        }

        private sealed class FakeModel : ILipidSpectrumModel
        {
            private readonly Func<Lipid, LipidFragment[]> _fragments;
            private readonly LipidSpectrumRuleSet _ruleSet = new LipidSpectrumRuleSet("PC", "[M+H]+", LipidSpectrumResolution.MolecularSpeciesLevel, new[] { ClassRule, ChainRule });

            public FakeModel(Func<Lipid, LipidFragment[]> fragments) {
                _fragments = fragments;
            }

            public string LipidClassName => "PC";
            public IReadOnlyList<LipidSpectrumRuleSet> RuleSets => new[] { _ruleSet };
            public LipidSpectrumRuleSet FindRuleSet(string adductIonName) => adductIonName == "[M+H]+" ? _ruleSet : null;
            public List<SpectrumPeak> Generate(Lipid lipid, AdductIon adduct) => _fragments(lipid).Select(f => new SpectrumPeak(f.Mz, f.Intensity)).ToList();
            public IReadOnlyList<LipidFragment> GenerateFragments(Lipid lipid, AdductIon adduct) => _fragments(lipid);
        }

        private sealed class FakeGenerator : ILipidGenerator
        {
            private readonly ILipid[] _lipids;

            public FakeGenerator(ILipid[] lipids) {
                _lipids = lipids;
            }

            public bool CanGenerate(ILipid lipid) => lipid.AnnotationLevel < 2;
            public IEnumerable<ILipid> Generate(ILipid lipid) => lipid.AnnotationLevel < 2 ? _lipids : Enumerable.Empty<ILipid>();
            public IEnumerable<ILipid> GenerateUntil(ILipid lipid, Func<ILipid, bool> predicate) => Generate(lipid);
        }
    }
}
