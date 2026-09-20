using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialGcMsApi.Algorithm.Tests
{
    /// <summary>
    /// A GC-MS match records which kind of EI library it was compared against.
    /// </summary>
    /// <remarks>
    /// The author's point of 2026-09-15: the EI libraries in practice are Wiley, NIST and MassBank
    /// on one side, and in-silico generators such as NEIMS on the other, and the evidence record
    /// should tell them apart. MS-DIAL searches both identically -- same parser, same comparison,
    /// same score -- so the tag is the only place the difference exists.
    ///
    /// These go through CalculateMatchScore rather than calling the policy directly, because the
    /// wiring is the thing at risk: the library kind has to survive from the MoleculeDataBase, past
    /// the constructor that keeps only an ordered array of references, and into the result.
    /// </remarks>
    [TestClass()]
    public class EiLibraryQualityTests
    {
        [TestMethod()]
        public void AMatchAgainstAnAcquiredEiLibraryCitesAReferenceSpectrum() {
            var result = MatchAgainst(DataBaseSource.Msp);

            Assert.IsTrue(result.MeasuredTerms.HasFlag(MeasuredTerms.Spectrum), "a spectrum was compared");
            Assert.AreEqual(AnnotationEvidenceSource.ReferenceSpectrum, result.EvidenceSource);
        }

        [TestMethod()]
        public void AMatchAgainstAGeneratedEiLibrarySaysSo() {
            var result = MatchAgainst(DataBaseSource.PredictedMsp);

            Assert.AreEqual(AnnotationEvidenceSource.BySpectrumPredictionTool, result.EvidenceSource,
                "NEIMS and its kin compute the spectrum from a structure; the match is against a calculation");
        }

        /// <summary>
        /// Both kinds are still searched and scored identically.
        /// </summary>
        /// <remarks>
        /// The boundary of the change. The tag must not become a back door into the scoring: a
        /// generated library is searched with the same comparison and earns the same number, and
        /// what the tag buys is that the record says what was on the other side of it -- and, through
        /// the evidence rank, that the acquired match wins a tie.
        /// </remarks>
        [TestMethod()]
        public void TheLibraryKindChangesTheRecordAndNotTheScore() {
            var acquired = MatchAgainst(DataBaseSource.Msp);
            var predicted = MatchAgainst(DataBaseSource.PredictedMsp);

            Assert.AreEqual(acquired.TotalScore, predicted.TotalScore);
            Assert.AreEqual(acquired.IsSpectrumMatch, predicted.IsSpectrumMatch);
            Assert.AreEqual(acquired.IsReferenceMatched, predicted.IsReferenceMatched);
            Assert.IsTrue(
                AnnotationEvidence.Rank(predicted.EvidenceSource) < AnnotationEvidence.Rank(acquired.EvidenceSource),
                "at equal score the acquired library wins, and that is where the difference is spent");
        }

        /// <summary>
        /// The kind survives the parameter copy that the GC-MS pipeline makes per run.
        /// </summary>
        /// <remarks>
        /// With() rebuilds the calculator through a private constructor that does not see the
        /// database, only the ordered reference array, so the library kind has to be carried across
        /// explicitly. It is the kind of field that gets dropped there and is noticed much later.
        /// </remarks>
        [TestMethod()]
        public void TheLibraryKindSurvivesASearchParameterCopy() {
            var predicted = Calculator(DataBaseSource.PredictedMsp);
            var copied = predicted.With(predicted.CopySearchParameter());

            var result = copied.CalculateMatches(Scan()).Single();

            Assert.AreEqual(AnnotationEvidenceSource.BySpectrumPredictionTool, result.EvidenceSource);
        }

        private static MsScanMatchResult MatchAgainst(DataBaseSource library) {
            return Calculator(library).CalculateMatches(Scan()).Single();
        }

        private static CalculateMatchScore Calculator(DataBaseSource library) {
            var parameter = new MsRefSearchParameterBase
            {
                RiTolerance = 100f,
                Ms1Tolerance = 0.5f,
                Ms2Tolerance = 0.5f,
                IsUseTimeForAnnotationScoring = true,
                IsUseTimeForAnnotationFiltering = false,
            };
            var reference = new MoleculeMsReference
            {
                ScanID = 0,
                Name = "a reference",
                InChIKey = "DUMMYINCHIKEY",
                ChromXs = new ChromXs(1500d, ChromXType.RI, ChromXUnit.None),
                Spectrum = Spectrum(),
            };
            var database = new MoleculeDataBase(
                new List<MoleculeMsReference> { reference, }, "MspDB", library, SourceType.MspDB, "MspPath");
            var item = new DataBaseItem<MoleculeDataBase>(database, new List<IAnnotatorParameterPair<MoleculeDataBase>>());
            return new CalculateMatchScore(item, parameter, RetentionType.RI, RiCompoundType.Alkanes);
        }

        private static MSScanProperty Scan() {
            return new MSScanProperty
            {
                ScanID = 0,
                ChromXs = new ChromXs(1500d, ChromXType.RI, ChromXUnit.None),
                Spectrum = Spectrum(),
            };
        }

        private static List<SpectrumPeak> Spectrum() {
            return new List<SpectrumPeak>
            {
                new SpectrumPeak { Mass = 73, Intensity = 100, },
                new SpectrumPeak { Mass = 147, Intensity = 60, },
                new SpectrumPeak { Mass = 205, Intensity = 40, },
                new SpectrumPeak { Mass = 291, Intensity = 20, },
            };
        }
    }
}
