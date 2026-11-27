using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialGcMsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.MsdialGcMsApi.Algorithm.Alignment.Tests {
    /// <summary>
    /// The quant mass the joiner gives each spot: the library QUANTMASS of a reference-matched spot when "Replace quant mass by
    /// user defined value" is on, otherwise the representative's base peak when "Is quant mass based on base peak mz" is on.
    /// </summary>
    [TestClass()]
    public class GcmsPeakJoinerQuantMassTests {
        private const double RiTolerance = 10d;
        private const int LibraryId = 0;

        // A TMS-like spectrum whose base peak is 73; the library entry names 147 as its quant mass.
        private static readonly (double Mz, double Intensity)[] Spectrum = [(73d, 1000d), (147d, 600d), (221d, 300d), (295d, 150d)];

        private string _directory = string.Empty;

        [TestInitialize]
        public void Initialize() {
            _directory = Path.Combine(Path.GetTempPath(), "GcmsPeakJoinerQuantMassTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        [TestCleanup]
        public void Cleanup() {
            if (Directory.Exists(_directory)) {
                Directory.Delete(_directory, recursive: true);
            }
        }

        [TestMethod]
        public void AReferenceMatchedSpotTakesTheLibraryQuantMassOverTheBasePeak() {
            var spots = Join(annotated: true, libraryQuantMass: 147d, replace: true, basePeak: true);

            Assert.AreEqual(1, spots.Count);
            Assert.AreEqual(147d, spots[0].QuantMass, 1e-6);
            Assert.AreEqual(147d, spots[0].MassCenter, 1e-6);
        }

        [TestMethod]
        public void AReferenceMatchedSpotTakesTheLibraryQuantMassOverTheVote() {
            // Every file votes for 73 (its model ion); the library names 147.
            var spots = Join(annotated: true, libraryQuantMass: 147d, replace: true, basePeak: false);

            Assert.AreEqual(147d, spots[0].QuantMass, 1e-6);
        }

        [TestMethod]
        public void WithoutTheReplaceOptionTheBasePeakIsTheQuantMass() {
            var spots = Join(annotated: true, libraryQuantMass: 147d, replace: false, basePeak: true);

            Assert.AreEqual(73d, spots[0].QuantMass, 1e-6);
        }

        [TestMethod]
        public void AnUnannotatedSpotTakesTheBasePeak() {
            var spots = Join(annotated: false, libraryQuantMass: 147d, replace: true, basePeak: true);

            Assert.AreEqual(73d, spots[0].QuantMass, 1e-6);
        }

        [TestMethod]
        [DataRow(-1d, DisplayName = "unreadable QUANTMASS (the MSP parser's -1)")]
        [DataRow(0d, DisplayName = "no QUANTMASS")]
        [DataRow(2500d, DisplayName = "above the mass range")]
        public void ALibraryQuantMassOutsideTheMassRangeIsIgnored(double libraryQuantMass) {
            var spots = Join(annotated: true, libraryQuantMass: libraryQuantMass, replace: true, basePeak: true);

            Assert.AreEqual(73d, spots[0].QuantMass, 1e-6);
        }

        [TestMethod]
        [DataRow(85d, 600d, 84.9d, false)]
        [DataRow(85d, 600d, 85d, true)]
        [DataRow(85d, 600d, 300d, true)]
        [DataRow(85d, 600d, 600d, true)]
        [DataRow(85d, 600d, 600.1d, false)]
        [DataRow(85d, 600d, -1d, false)]
        public void IsInMassRangeIsInclusiveAtBothEnds(double begin, double end, double mz, bool expected) {
            var parameter = new PeakPickBaseParameter { MassRangeBegin = (float)begin, MassRangeEnd = (float)end, };

            Assert.AreEqual(expected, parameter.IsInMassRange(mz));
        }

        private List<AlignmentSpotProperty> Join(bool annotated, double libraryQuantMass, bool replace, bool basePeak) {
            var data = new List<List<SpectrumFeature>>
            {
                new List<SpectrumFeature> { CreateFeature(ri: 1500d, height: 1000d, annotated) },
                new List<SpectrumFeature> { CreateFeature(ri: 1501d, height: 2000d, annotated) },
            };
            var files = new List<AnalysisFileBean>();
            for (int i = 0; i < data.Count; i++) {
                var dcl = Path.Combine(_directory, $"file{i}.dcl");
                MsdecResultsWriter.Write(dcl, data[i].Select(f => f.AnnotatedMSDecResult.MSDecResult).ToList());
                files.Add(new AnalysisFileBean { AnalysisFileId = i, AnalysisFileName = $"file{i}", DeconvolutionFilePath = dcl, });
            }
            var parameter = new MsdialGcmsParameter
            {
                AlignmentIndexType = AlignmentIndexType.RI,
                RetentionIndexAlignmentTolerance = (float)RiTolerance,
                IsReplaceQuantmassByUserDefinedValue = replace,
                IsRepresentativeQuantMassBasedOnBasePeakMz = basePeak,
            };
            var refer = new StubRefer(new MoleculeMsReference { ScanID = LibraryId, Name = "library entry", QuantMass = libraryQuantMass, });
            var joiner = GcmsPeakJoiner.CreateRIJoiner(RiTolerance, parameter, new StubEvaluator(), refer, new StubAccessor(data));
            return joiner.Join(files, referenceId: 0, null);
        }

        private static SpectrumFeature CreateFeature(double ri, double height, bool annotated) {
            var modelIon = Spectrum[0].Mz;
            ChromXs Times() => new ChromXs(new RetentionIndex(ri)) { RT = new RetentionTime(ri / 100d), };
            var msdec = new MSDecResult
            {
                ScanID = 0,
                RawSpectrumID = 0,
                ChromXs = Times(),
                ModelPeakMz = modelIon,
                ModelPeakHeight = height,
                Spectrum = Spectrum.Select(p => new SpectrumPeak(p.Mz, p.Intensity)).ToList(),
            };
            var peak = new BaseChromatogramPeakFeature
            {
                ChromXsLeft = Times(),
                ChromXsTop = Times(),
                ChromXsRight = Times(),
                PeakHeightTop = height,
                Mass = modelIon,
            };
            var container = new MsScanMatchResultContainer();
            if (annotated) {
                container.AddResult(new MsScanMatchResult
                {
                    LibraryID = LibraryId,
                    Name = "library entry",
                    AnnotatorID = StubEvaluator.AnnotatorId,
                    Source = SourceType.MspDB,
                    IsSpectrumMatch = true,
                    TotalScore = 0.9f,
                });
            }
            return new SpectrumFeature(
                new AnnotatedMSDecResult(msdec, container, new MoleculeProperty(), modelIon),
                new QuantifiedChromatogramPeak(peak, new ChromatogramPeakShape(), 0, 0, 0));
        }

        private sealed class StubEvaluator : IMatchResultEvaluator<MsScanMatchResult> {
            public const string AnnotatorId = "msp";

            public bool IsReferenceMatched(MsScanMatchResult result) => result.AnnotatorID == AnnotatorId && result.IsSpectrumMatch;
            public bool IsAnnotationSuggested(MsScanMatchResult result) => false;
            public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results) => results.Argmax(r => r.TotalScore);
            public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results) => results.ToList();
            public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results) => results.Where(IsReferenceMatched).ToList();
        }

        private sealed class StubRefer : IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> {
            private readonly MoleculeMsReference _reference;

            public StubRefer(MoleculeMsReference reference) {
                _reference = reference;
            }

            public string Key => StubEvaluator.AnnotatorId;

            public MoleculeMsReference Refer(MsScanMatchResult result) => result.LibraryID == _reference.ScanID ? _reference : null;
        }

        private sealed class StubAccessor : IFeatureAccessor<SpectrumFeature> {
            private readonly List<List<SpectrumFeature>> _data;

            public StubAccessor(List<List<SpectrumFeature>> data) {
                _data = data;
            }

            public List<SpectrumFeature> GetMSScanProperties(AnalysisFileBean analysisFile) {
                return _data[analysisFile.AnalysisFileId];
            }

            public ChromatogramPeakInfo AccumulateChromatogram(AlignmentChromPeakFeature peak, AlignmentSpotProperty spot, Ms1Spectra ms1Spectra, float ms1MassTolerance) {
                throw new NotSupportedException();
            }
        }
    }
}
