using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm.Annotation.Tests
{
    [TestClass()]
    public class StandardAnnotationProcessTests
    {
        [TestMethod()]
        public async Task RunAnnotationAsyncSingleThreadTest() {
            var chromPeaks = new[]
            {
                new ChromatogramPeakFeature { },
                new ChromatogramPeakFeature { },
                new ChromatogramPeakFeature { },
                new ChromatogramPeakFeature { },
            };
            foreach (var peak in chromPeaks) peak.PeakCharacter.IsotopeWeightNumber = 0;
            var msdecResults = new MSDecResultCollection([
                new MSDecResult { },
                new MSDecResult { },
                new MSDecResult { },
                new MSDecResult { },
            ], 0d);
            var annotator = new MockAnnotator("Annotator");
            var process = new StandardAnnotationProcess(new MockFactory(annotator.Id, annotator), annotator, annotator);
            await process.RunAnnotationAsync(chromPeaks, msdecResults, new MockProvider(), 1);

            Assert.AreEqual(annotator.Dummy, chromPeaks[0].MatchResults.Representative);
            Assert.AreEqual(annotator.Dummy, chromPeaks[1].MatchResults.Representative);
            Assert.AreEqual(annotator.Dummy, chromPeaks[2].MatchResults.Representative);
            Assert.AreEqual(annotator.Dummy, chromPeaks[3].MatchResults.Representative);
        }

        [TestMethod()]
        public async Task RunAnnotationAsyncMultiThreadTest() {
            var chromPeaks = new[]
            {
                new ChromatogramPeakFeature { },
                new ChromatogramPeakFeature { },
                new ChromatogramPeakFeature { },
                new ChromatogramPeakFeature { },
            };
            foreach (var peak in chromPeaks) peak.PeakCharacter.IsotopeWeightNumber = 0;
            var msdecResults = new MSDecResultCollection([
                new MSDecResult { },
                new MSDecResult { },
                new MSDecResult { },
                new MSDecResult { },
            ], 0d);
            var annotator = new MockAnnotator("Annotator");
            var process = new StandardAnnotationProcess(new MockFactory(annotator.Id, annotator), annotator, annotator);
            await process.RunAnnotationAsync(chromPeaks, msdecResults, new MockProvider(), 4);

            Assert.AreEqual(annotator.Dummy, chromPeaks[0].MatchResults.Representative);
            Assert.AreEqual(annotator.Dummy, chromPeaks[1].MatchResults.Representative);
            Assert.AreEqual(annotator.Dummy, chromPeaks[2].MatchResults.Representative);
            Assert.AreEqual(annotator.Dummy, chromPeaks[3].MatchResults.Representative);
        }

        [TestMethod()]
        public async Task RunAnnotationAsyncSetSomeHighScoreResultsTest() {
            var chromPeaks = new[]
            {
                new ChromatogramPeakFeature { },
            };
            foreach (var peak in chromPeaks) peak.PeakCharacter.IsotopeWeightNumber = 0;
            var msdecResults = new MSDecResultCollection([
                new MSDecResult { },
            ], 0d);
            var annotator = new MockAnnotator("Annotator");
            var process = new StandardAnnotationProcess(new MockFactory(annotator.Id, annotator), annotator, annotator);
            await process.RunAnnotationAsync(chromPeaks, msdecResults, new MockProvider(), 4);

            await Console.Out.WriteLineAsync(string.Join(",", chromPeaks[0].MatchResults.MatchResults.Select(r => r.Name))).ConfigureAwait(false);
            var highScoreSubset = annotator.Dummies.Where(r => annotator.IsReferenceMatched(r) || annotator.IsAnnotationSuggested(r)).ToArray();
            CollectionAssert.IsSubsetOf(highScoreSubset, chromPeaks[0].MatchResults.MatchResults);
        }

        [TestMethod()]
        public async Task TheCandidatePopulationSurvivesTruncation() {
            // The whole reason keys 41-43 exist. The mock annotator scores four candidates and
            // NUMBER_OF_ANNOTATION_RESULTS is 3, so one is discarded before anything is stored --
            // and the discarded one leaves no trace anywhere else. Each stored result carries the
            // size of the population it was selected from.
            var chromPeaks = new[] { new ChromatogramPeakFeature { }, };
            chromPeaks[0].PeakCharacter.IsotopeWeightNumber = 0;
            var msdecResults = new MSDecResultCollection([new MSDecResult { },], 0d);
            var annotator = new MockAnnotator("Annotator");
            var process = new StandardAnnotationProcess(new MockFactory(annotator.Id, annotator), annotator, annotator);

            await process.RunAnnotationAsync(chromPeaks, msdecResults, new MockProvider(), 1);

            var stored = chromPeaks[0].MatchResults.MatchResults;
            Assert.AreEqual(3, stored.Count, "three survived: the cap, and the mock's own threshold");
            foreach (var result in stored) {
                Assert.AreEqual(4, result.CandidatesFound,
                    "every stored result reports the population it was selected from, not the "
                    + "number of results that were stored");
                Assert.AreEqual(3, result.CandidatesAboveThreshold,
                    "the mock names the three that are matched or suggested, dropping the 0.6");
                Assert.AreEqual(2, result.CandidatesReferenceMatched,
                    "and its IsReferenceMatched predicate accepts the two scoring 0.8 or better");
            }
        }

        [TestMethod()]
        public async Task ADiscardedCandidateIsNotStamped() {
            // The counts describe the population, so they must not be written onto candidates the
            // process threw away: a stamped-but-unstored object could later be picked up by a
            // manual search and would then carry numbers for a selection it never survived.
            var chromPeaks = new[] { new ChromatogramPeakFeature { }, };
            chromPeaks[0].PeakCharacter.IsotopeWeightNumber = 0;
            var msdecResults = new MSDecResultCollection([new MSDecResult { },], 0d);
            var annotator = new MockAnnotator("Annotator");
            var process = new StandardAnnotationProcess(new MockFactory(annotator.Id, annotator), annotator, annotator);

            await process.RunAnnotationAsync(chromPeaks, msdecResults, new MockProvider(), 1);

            var discarded = annotator.Dummies.Single(d => d.TotalScore == 0.60f);
            Assert.IsFalse(chromPeaks[0].MatchResults.MatchResults.Contains(discarded), "fixture precondition");
            Assert.IsNull(discarded.CandidatesFound);
            Assert.IsNull(discarded.CandidatesAboveThreshold);
            Assert.IsNull(discarded.CandidatesReferenceMatched);
        }

        class MockProvider : IDataProvider
        {
            public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() {
                return new List<RawSpectrum> { new RawSpectrum() }.AsReadOnly();
            }

            public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) {
                return Task.FromResult(LoadMs1Spectrums());
            }

            public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) {
                return new List<RawSpectrum>().AsReadOnly();
            }

            public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) {
                return Task.FromResult(LoadMsNSpectrums(level));
            }

            public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() {
                return new List<RawSpectrum> { new RawSpectrum() }.AsReadOnly();
            }

            public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) {
                return Task.FromResult(new List<RawSpectrum> { new RawSpectrum() }.AsReadOnly());
            }
        }

        class MockQuery : IAnnotationQuery<MsScanMatchResult>
        {
            private readonly MockAnnotator _annotator;

            public MockQuery(MockAnnotator annotator) {
                _annotator = annotator;
            }

            public IMSIonProperty Property => throw new NotImplementedException();

            public IMSScanProperty Scan => throw new NotImplementedException();

            public IMSScanProperty NormalizedScan => throw new NotImplementedException();

            public IReadOnlyList<IsotopicPeak> Isotopes => throw new NotImplementedException();

            public IonFeatureCharacter IonFeature => throw new NotImplementedException();

            public MsRefSearchParameterBase Parameter => throw new NotImplementedException();

            public IEnumerable<MsScanMatchResult> FindCandidates(bool forceFind = false) {
                return _annotator.FindCandidates(this);
            }
        }

        class MockFactory : IAnnotationQueryFactory<MsScanMatchResult>
        {
            private readonly MockAnnotator _annotator;

            public string AnnotatorId { get; }

            int IAnnotationQueryFactory<MsScanMatchResult>.Priority => _annotator.Priority;

            public MockFactory(string id, MockAnnotator annotator) {
                AnnotatorId = id;
                _annotator = annotator;
            }

            public IAnnotationQuery<MsScanMatchResult> Create(IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<RawPeakElement> spectrum, IonFeatureCharacter ionFeature, MsRefSearchParameterBase parameter) {
                return new MockQuery(_annotator);
            }

            MsRefSearchParameterBase IAnnotationQueryFactory<MsScanMatchResult>.PrepareParameter() {
                return new MsRefSearchParameterBase();
            }

            public IMatchResultEvaluator<MsScanMatchResult> CreateEvaluator() {
                return new MsScanMatchResultEvaluator(new MsRefSearchParameterBase());
            }
        }

        class MockAnnotatorContainer : IAnnotatorContainer<MockQuery, MoleculeMsReference, MsScanMatchResult>
        {
            public MockAnnotatorContainer(IAnnotator<MockQuery, MoleculeMsReference, MsScanMatchResult> annotator) {
                AnnotatorID = annotator.Key;
                Annotator = annotator;
            }

            public IAnnotator<MockQuery, MoleculeMsReference, MsScanMatchResult> Annotator { get; }

            public string AnnotatorID { get; }

            public MsRefSearchParameterBase Parameter => null;
        }

        class MockAnnotator : IAnnotator<MockQuery, MoleculeMsReference, MsScanMatchResult>, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>, IMatchResultEvaluator<MsScanMatchResult>
        {
            public MockAnnotator(string key) {
                Id = key;
                Dummy = new MsScanMatchResult
                {
                    Name = "dummy", AnnotatorID = key, Source = SourceType.MspDB, TotalScore = 0.90f,
                };
                Dummies = new List<MsScanMatchResult>
                {
                    Dummy,
                    new MsScanMatchResult
                    {
                        Name = "dummy2", AnnotatorID = key, Source = SourceType.MspDB, TotalScore = 0.80f,
                    },
                    new MsScanMatchResult
                    {
                        Name = "dummy3", AnnotatorID = key, Source = SourceType.MspDB, TotalScore = 0.70f,
                    },
                    new MsScanMatchResult
                    {
                        Name = "dummy4", AnnotatorID = key, Source = SourceType.MspDB, TotalScore = 0.60f,
                    },
                };
            }

            public string Id { get; }
            public string Key => Id;

            public int Priority { get; } = 0;

            public MsScanMatchResult Annotate(MockQuery query) {
                throw new NotImplementedException();
            }

            public MsScanMatchResult CalculateScore(MockQuery query, MoleculeMsReference reference) {
                throw new NotImplementedException();
            }

            // Matches what the real MsScanMatchResultEvaluator does -- suggested or matched --
            // rather than passing everything through. It has to: with a pass-through the scored
            // population and the named subset are the same number, so no test could tell
            // CandidatesFound from CandidatesAboveThreshold. With this the four dummies give three
            // distinct figures: 4 scored, 3 named, 2 reference-matched.
            public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results) {
                return results.Where(result => IsReferenceMatched(result) || IsAnnotationSuggested(result)).ToList();
            }

            public MsScanMatchResult Dummy { get; }

            public List<MsScanMatchResult> Dummies { get; }

            public List<MsScanMatchResult> FindCandidates(MockQuery query) {
                return Dummies.ToList();
            }

            public bool IsAnnotationSuggested(MsScanMatchResult result) {
                return 0.8f > result.TotalScore && result.TotalScore >= 0.7f;
            }

            public bool IsReferenceMatched(MsScanMatchResult result) {
                return result.TotalScore >= 0.8f;
            }

            public MoleculeMsReference Refer(MsScanMatchResult result) {
                return new MoleculeMsReference { Name = "Dummy reference", };
            }

            public List<MoleculeMsReference> Search(MockQuery query) {
                throw new NotImplementedException();
            }

            public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results) {
                return results.ToList();
            }

            public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results) {
                return results.FirstOrDefault();
            }
        }
    }
}