using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.Common.Lipidomics;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation.Tests
{
    [TestClass()]
    public class StagedLipidDatabaseAnnotatorTests
    {
        private static readonly AdductIon ProtonAdduct = AdductIon.GetAdductIon("[M+H]+");

        [TestMethod()]
        public void SpectrumOfAMolecularSpeciesIsAnnotatedAtThatLevel() {
            var (annotator, database) = CreateAnnotator(Seed("PC 34:1", 5.0));
            var query = Query("PC 16:0_18:1", rt: 5.0, spectrumOf: "PC 16:0_18:1");

            var result = annotator.FindCandidates(query).First();

            Assert.AreEqual("PC 16:0_18:1", result.Name);
            Assert.IsTrue(result.IsLipidClassMatch);
            Assert.IsTrue(result.IsLipidChainsMatch);
            Assert.IsFalse(result.IsLipidPositionMatch);
            Assert.IsTrue(result.IsPrecursorMzMatch);
            Assert.IsTrue(result.IsReferenceMatched);
            Assert.AreEqual(SourceType.GeneratedLipid, result.Source);
            Assert.AreEqual("StagedLipid", result.AnnotatorID);

            var reference = annotator.Refer(result);
            Assert.AreEqual("PC 16:0_18:1", reference.Name);
            Assert.AreEqual("[M+H]+", reference.AdductType.AdductIonName);
            Assert.IsTrue(reference.Spectrum.Count > 0);
            Assert.AreSame(reference, database.Refer(result));
        }

        [TestMethod()]
        public void SpectrumWithoutClassIonsIsOnlySuggested() {
            var (annotator, _) = CreateAnnotator(Seed("PC 34:1", 5.0));
            var lipid = FacadeLipidParser.Default.Parse("PC 34:1");
            var query = Query(ProtonAdduct.ConvertToMz(lipid.Mass), 5.0, new List<SpectrumPeak> { new SpectrumPeak(ProtonAdduct.ConvertToMz(lipid.Mass), 100) });

            var result = annotator.FindCandidates(query).Single();

            Assert.AreEqual("PC 34:1", result.Name);
            Assert.IsFalse(result.IsLipidClassMatch);
            Assert.IsFalse(result.IsReferenceMatched);
            Assert.IsTrue(result.IsAnnotationSuggested);
        }

        [TestMethod()]
        public void SeedOutsideTheRetentionTimeWindowIsNotSearched() {
            var (annotator, _) = CreateAnnotator(Seed("PC 34:1", 9.0));
            var query = Query("PC 16:0_18:1", rt: 5.0, spectrumOf: "PC 16:0_18:1");

            Assert.AreEqual(0, annotator.FindCandidates(query).Count);
        }

        [TestMethod()]
        public void RetentionTimeIsOptionalAsForTheOtherAnnotators() {
            var (annotator, _) = CreateAnnotator(Seed("PC 34:1", 9.0));

            // RT filtering off: the seed 4 min away is still searched; RT scoring off: RT neither counts nor blocks.
            var result = annotator.FindCandidates(Query("PC 16:0_18:1", rt: 5.0, spectrumOf: "PC 16:0_18:1", Parameter(rtFiltering: false, rtScoring: false))).Single();
            Assert.IsTrue(result.IsReferenceMatched);
            Assert.IsFalse(result.IsRtMatch);
            Assert.AreEqual(0f, result.RtSimilarity);

            // RT scoring on: the same seed is found but its RT now keeps it from a reference match.
            var scored = annotator.FindCandidates(Query("PC 16:0_18:1", rt: 5.0, spectrumOf: "PC 16:0_18:1", Parameter(rtFiltering: false, rtScoring: true))).Single();
            Assert.IsFalse(scored.IsRtMatch);
            Assert.IsFalse(scored.IsReferenceMatched);
        }

        [TestMethod()]
        public void SeedWithoutRetentionTimeIsNotBlockedByRtScoring() {
            var (annotator, _) = CreateAnnotator(new LipidSeed("PC 34:1", FacadeLipidParser.Default.Parse("PC 34:1")));

            var result = annotator.FindCandidates(Query("PC 16:0_18:1", rt: 5.0, spectrumOf: "PC 16:0_18:1")).Single();

            Assert.IsTrue(result.IsReferenceMatched);
            Assert.IsFalse(result.IsRtMatch);
        }

        [TestMethod()]
        public void SameStructureReusesItsReference() {
            var (annotator, _) = CreateAnnotator(Seed("PC 34:1", 5.0));
            var query = Query("PC 16:0_18:1", rt: 5.0, spectrumOf: "PC 16:0_18:1");

            var first = annotator.FindCandidates(query).First();
            var second = annotator.FindCandidates(query).First();

            Assert.AreEqual(first.LibraryID, second.LibraryID);
        }

        [TestMethod()]
        public void SaveGivesAKeyThatRecreatesTheAnnotator() {
            var (annotator, database) = CreateAnnotator(Seed("PC 34:1", 5.0));

            var key = annotator.Save();

            Assert.IsInstanceOfType(key, typeof(Parser.StagedLipidRestorationKey));
            Assert.AreEqual(annotator.Id, key.Key);
            Assert.AreEqual(annotator.Priority, key.Priority);
        }

        private static (StagedLipidDatabaseAnnotator, StagedLipidDatabase) CreateAnnotator(params LipidSeed[] seeds) {
            var database = new StagedLipidDatabase("StagedLipidDB", seeds);
            return (new StagedLipidDatabaseAnnotator(database, "StagedLipid", 1, Parameter()), database);
        }

        private static MsRefSearchParameterBase Parameter(bool rtFiltering = true, bool rtScoring = true) {
            return new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.025f,
                RtTolerance = 0.5f,
                IsUseTimeForAnnotationFiltering = rtFiltering,
                IsUseTimeForAnnotationScoring = rtScoring,
                MassRangeBegin = 0,
                MassRangeEnd = 2000,
            };
        }

        private static LipidSeed Seed(string name, double rt) {
            return new LipidSeed(name, FacadeLipidParser.Default.Parse(name), rt);
        }

        private static IAnnotationQuery<MsScanMatchResult> Query(string precursorOf, double rt, string spectrumOf, MsRefSearchParameterBase parameter = null) {
            var precursor = (Lipid)FacadeLipidParser.Default.Parse(precursorOf);
            var generating = (Lipid)FacadeLipidParser.Default.Parse(spectrumOf);
            var spectrum = CidLipidSpectrumModels.Find("PC").Generate(generating, ProtonAdduct);
            return Query(ProtonAdduct.ConvertToMz(precursor.Mass), rt, spectrum, parameter);
        }

        private static IAnnotationQuery<MsScanMatchResult> Query(double mz, double rt, List<SpectrumPeak> spectrum, MsRefSearchParameterBase parameter = null) {
            var property = new MSIonProperty(mz, new ChromXs(rt), IonMode.Positive, ProtonAdduct, 0d);
            var scan = new MSScanProperty { PrecursorMz = mz, IonMode = IonMode.Positive, Spectrum = spectrum };
            return new FakeQuery(property, scan, parameter ?? Parameter());
        }

        private sealed class FakeQuery : IAnnotationQuery<MsScanMatchResult>
        {
            public FakeQuery(IMSIonProperty property, IMSScanProperty scan, MsRefSearchParameterBase parameter) {
                Property = property;
                Scan = scan;
                NormalizedScan = scan;
                Parameter = parameter;
            }

            public IMSIonProperty Property { get; }
            public IMSScanProperty Scan { get; }
            public IMSScanProperty NormalizedScan { get; }
            public IReadOnlyList<IsotopicPeak> Isotopes => Array.Empty<IsotopicPeak>();
            public IonFeatureCharacter IonFeature => null;
            public MsRefSearchParameterBase Parameter { get; }
            public IEnumerable<MsScanMatchResult> FindCandidates(bool forceFind = false) => throw new NotSupportedException();
        }
    }
}
