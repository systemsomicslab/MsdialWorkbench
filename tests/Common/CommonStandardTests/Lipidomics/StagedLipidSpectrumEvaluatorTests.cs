using CompMs.Common.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class StagedLipidSpectrumEvaluatorTests
    {
        private const double Tolerance = 0.025;

        // Thresholds are on the XML scale (x10 of the normalized one).
        private static readonly LipidFragment Precursor = Fragment(LipidFragmentKind.Precursor, 500d, isDiagnostic: false, threshold: null);
        private static readonly LipidFragment ClassIon = Fragment(LipidFragmentKind.ClassIon, 184d, isDiagnostic: true, threshold: 100d);
        private static readonly LipidFragment NonDiagnosticClassIon = Fragment(LipidFragmentKind.ClassIon, 200d, isDiagnostic: false, threshold: null);
        private static readonly LipidFragment ChainIon1 = Fragment(LipidFragmentKind.ChainIon, 300d, isDiagnostic: true, threshold: 10d);
        private static readonly LipidFragment ChainIon2 = Fragment(LipidFragmentKind.ChainIon, 320d, isDiagnostic: true, threshold: 10d);
        private static readonly LipidFragment PositionIon = Fragment(LipidFragmentKind.ChainIon, 340d, isDiagnostic: true, threshold: 10d, isPositionDiagnostic: true);
        private static readonly LipidFragment Prohibited = new LipidFragment(new LipidFragmentRule(LipidFragmentKind.ProhibitedIon, "prohibited", 50d, false, false, null, null, null), 400d);

        [TestMethod()]
        public void AllDiagnosticIonsObservedResolvesMolecularSpecies() {
            var result = Evaluate(Spectrum((500, 100), (184, 50), (300, 5), (320, 5)), LipidSpectrumResolution.MolecularSpeciesLevel, Precursor, ClassIon, NonDiagnosticClassIon, ChainIon1, ChainIon2);

            Assert.AreEqual(StagedLipidLevel.MolecularSpecies, result.Level);
            Assert.AreEqual(StagedLipidStopReason.NoPositionDiagnosticIon, result.StopReason);
        }

        [TestMethod()]
        public void MissingClassDiagnosticIonStopsBeforeClass() {
            var result = Evaluate(Spectrum((500, 100), (300, 5), (320, 5)), LipidSpectrumResolution.MolecularSpeciesLevel, Precursor, ClassIon, ChainIon1, ChainIon2);

            Assert.AreEqual(StagedLipidLevel.None, result.Level);
            Assert.AreEqual(StagedLipidStopReason.ClassDiagnosticIonMissing, result.StopReason);
        }

        [TestMethod()]
        public void IonAtExactlyTheThresholdIsNotObserved() {
            // ClassIon threshold 100 on the XML scale is 10 on the normalized one; comparison is strict.
            var result = Evaluate(Spectrum((500, 100), (184, 10)), LipidSpectrumResolution.SpeciesLevel, Precursor, ClassIon);

            Assert.AreEqual(StagedLipidStopReason.ClassDiagnosticIonMissing, result.StopReason);
        }

        [TestMethod()]
        public void ObservedIntensityIsNormalizedToMaximum100() {
            // 1000 -> 100, 110 -> 11 > 10.
            var result = Evaluate(Spectrum((500, 1000), (184, 110)), LipidSpectrumResolution.SpeciesLevel, Precursor, ClassIon);

            Assert.AreEqual(StagedLipidLevel.Class, result.Level);
            Assert.AreEqual(11d, result.Matches.Single(m => m.Fragment == ClassIon).ObservedIntensity, 1e-9);
        }

        [TestMethod()]
        public void NonDiagnosticIonsAreNotRequired() {
            var result = Evaluate(Spectrum((184, 100)), LipidSpectrumResolution.SpeciesLevel, Precursor, ClassIon, NonDiagnosticClassIon);

            Assert.AreEqual(StagedLipidLevel.Class, result.Level);
        }

        [TestMethod()]
        public void ProhibitedIonRejectsTheClass() {
            var result = Evaluate(Spectrum((184, 100), (400, 6)), LipidSpectrumResolution.SpeciesLevel, ClassIon, Prohibited);

            Assert.AreEqual(StagedLipidLevel.None, result.Level);
            Assert.AreEqual(StagedLipidStopReason.ProhibitedIonObserved, result.StopReason);
        }

        [TestMethod()]
        public void ProhibitedIonBelowItsThresholdIsIgnored() {
            var result = Evaluate(Spectrum((184, 100), (400, 5)), LipidSpectrumResolution.SpeciesLevel, ClassIon, Prohibited);

            Assert.AreEqual(StagedLipidLevel.Class, result.Level);
        }

        [TestMethod()]
        public void SpeciesLevelRuleSetStopsAtClass() {
            var result = Evaluate(Spectrum((184, 100), (300, 50), (320, 50)), LipidSpectrumResolution.SpeciesLevel, ClassIon, ChainIon1, ChainIon2);

            Assert.AreEqual(StagedLipidLevel.Class, result.Level);
            Assert.AreEqual(StagedLipidStopReason.ResolutionLimitedToSpecies, result.StopReason);
        }

        [TestMethod()]
        public void CandidateWithoutChainsStopsAtClass() {
            var result = Evaluate(Spectrum((184, 100), (300, 50), (320, 50)), LipidSpectrumResolution.MolecularSpeciesLevel, hasDeterminedChains: false, ClassIon, ChainIon1, ChainIon2);

            Assert.AreEqual(StagedLipidLevel.Class, result.Level);
            Assert.AreEqual(StagedLipidStopReason.ChainsNotDetermined, result.StopReason);
        }

        [TestMethod()]
        public void MissingChainDiagnosticIonStopsAtClass() {
            var result = Evaluate(Spectrum((184, 100), (300, 50)), LipidSpectrumResolution.MolecularSpeciesLevel, ClassIon, ChainIon1, ChainIon2);

            Assert.AreEqual(StagedLipidLevel.Class, result.Level);
            Assert.AreEqual(StagedLipidStopReason.ChainDiagnosticIonMissing, result.StopReason);
        }

        [TestMethod()]
        public void PositionDiagnosticIonResolvesPosition() {
            var result = Evaluate(Spectrum((184, 100), (300, 50), (320, 50), (340, 50)), LipidSpectrumResolution.MolecularSpeciesLevel, ClassIon, ChainIon1, ChainIon2, PositionIon);

            Assert.AreEqual(StagedLipidLevel.Position, result.Level);
            Assert.AreEqual(StagedLipidStopReason.Resolved, result.StopReason);
        }

        [TestMethod()]
        public void MissingPositionDiagnosticIonStopsAtMolecularSpecies() {
            // The position-diagnostic ion is also a diagnostic chain ion here, as in the XML, so give it its
            // own non-diagnostic copy to isolate the position stage.
            var positionOnly = Fragment(LipidFragmentKind.ChainIon, 340d, isDiagnostic: false, threshold: 10d, isPositionDiagnostic: true);
            var result = Evaluate(Spectrum((184, 100), (300, 50), (320, 50)), LipidSpectrumResolution.MolecularSpeciesLevel, ClassIon, ChainIon1, ChainIon2, positionOnly);

            Assert.AreEqual(StagedLipidLevel.MolecularSpecies, result.Level);
            Assert.AreEqual(StagedLipidStopReason.PositionDiagnosticIonMissing, result.StopReason);
        }

        [TestMethod()]
        public void NoneResolutionIsNotEvaluated() {
            var result = Evaluate(Spectrum((184, 100)), LipidSpectrumResolution.None, ClassIon);

            Assert.AreEqual(StagedLipidLevel.None, result.Level);
            Assert.AreEqual(StagedLipidStopReason.NotAnnotatable, result.StopReason);
        }

        [TestMethod()]
        public void PeaksOutsideToleranceAreNotMatched() {
            var result = Evaluate(Spectrum((184.03, 100)), LipidSpectrumResolution.SpeciesLevel, ClassIon);

            Assert.AreEqual(StagedLipidStopReason.ClassDiagnosticIonMissing, result.StopReason);
        }

        private static StagedLipidEvaluation Evaluate(List<SpectrumPeak> spectrum, LipidSpectrumResolution resolution, params LipidFragment[] fragments) {
            return Evaluate(spectrum, resolution, hasDeterminedChains: true, fragments);
        }

        private static StagedLipidEvaluation Evaluate(List<SpectrumPeak> spectrum, LipidSpectrumResolution resolution, bool hasDeterminedChains, params LipidFragment[] fragments) {
            return new StagedLipidSpectrumEvaluator(Tolerance).Evaluate(spectrum, resolution, fragments, hasDeterminedChains);
        }

        private static List<SpectrumPeak> Spectrum(params (double Mz, double Intensity)[] peaks) {
            return peaks.Select(p => new SpectrumPeak(p.Mz, p.Intensity)).ToList();
        }

        private static LipidFragment Fragment(LipidFragmentKind kind, double mz, bool isDiagnostic, double? threshold, bool isPositionDiagnostic = false) {
            return new LipidFragment(new LipidFragmentRule(kind, kind.ToString(), 100d, isDiagnostic, isPositionDiagnostic, null, null, threshold), mz);
        }
    }
}
