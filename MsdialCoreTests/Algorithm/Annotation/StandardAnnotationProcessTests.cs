using CompMs.Common.Components;
using CompMs.Common.DataObj;
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
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm.Annotation.Tests
{
    [TestClass()]
    public class StandardAnnotationProcessTests
    {
        [TestMethod()]
        public void RunAnnotationSingleThreadTest() {
            var chromPeaks = new[]
            {
                new ChromatogramPeakFeature { },
                new ChromatogramPeakFeature { },
                new ChromatogramPeakFeature { },
                new ChromatogramPeakFeature { },
            };
            foreach (var peak in chromPeaks) peak.PeakCharacter.IsotopeWeightNumber = 0;
            var msdecResults = new[]
            {
                new MSDecResult { },
                new MSDecResult { },
                new MSDecResult { },
                new MSDecResult { },
            };
            var annotator = new MockAnnotator("Annotator");
            var process = new StandardAnnotationProcess<MockQuery>(new MockFactory(), new MockAnnotatorContainer(annotator));
            process.RunAnnotation(chromPeaks, msdecResults, new MockProvider(), 1);

            Assert.AreEqual(annotator.Dummy, chromPeaks[0].MatchResults.Representative);
            Assert.AreEqual(annotator.Dummy, chromPeaks[1].MatchResults.Representative);
            Assert.AreEqual(annotator.Dummy, chromPeaks[2].MatchResults.Representative);
            Assert.AreEqual(annotator.Dummy, chromPeaks[3].MatchResults.Representative);
        }

        [TestMethod()]
        public void RunAnnotationMultiThreadTest() {
            var chromPeaks = new[]
            {
                new ChromatogramPeakFeature { },
                new ChromatogramPeakFeature { },
                new ChromatogramPeakFeature { },
                new ChromatogramPeakFeature { },
            };
            foreach (var peak in chromPeaks) peak.PeakCharacter.IsotopeWeightNumber = 0;
            var msdecResults = new[]
            {
                new MSDecResult { },
                new MSDecResult { },
                new MSDecResult { },
                new MSDecResult { },
            };
            var annotator = new MockAnnotator("Annotator");
            var process = new StandardAnnotationProcess<MockQuery>(new MockFactory(), new MockAnnotatorContainer(annotator));
            process.RunAnnotation(chromPeaks, msdecResults, new MockProvider(), 4);

            Assert.AreEqual(annotator.Dummy, chromPeaks[0].MatchResults.Representative);
            Assert.AreEqual(annotator.Dummy, chromPeaks[1].MatchResults.Representative);
            Assert.AreEqual(annotator.Dummy, chromPeaks[2].MatchResults.Representative);
            Assert.AreEqual(annotator.Dummy, chromPeaks[3].MatchResults.Representative);
        }

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
            var msdecResults = new[]
            {
                new MSDecResult { },
                new MSDecResult { },
                new MSDecResult { },
                new MSDecResult { },
            };
            var annotator = new MockAnnotator("Annotator");
            var process = new StandardAnnotationProcess<MockQuery>(new MockFactory(), new MockAnnotatorContainer(annotator));
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
            var msdecResults = new[]
            {
                new MSDecResult { },
                new MSDecResult { },
                new MSDecResult { },
                new MSDecResult { },
            };
            var annotator = new MockAnnotator("Annotator");
            var process = new StandardAnnotationProcess<MockQuery>(new MockFactory(), new MockAnnotatorContainer(annotator));
            await process.RunAnnotationAsync(chromPeaks, msdecResults, new MockProvider(), 4);

            Assert.AreEqual(annotator.Dummy, chromPeaks[0].MatchResults.Representative);
            Assert.AreEqual(annotator.Dummy, chromPeaks[1].MatchResults.Representative);
            Assert.AreEqual(annotator.Dummy, chromPeaks[2].MatchResults.Representative);
            Assert.AreEqual(annotator.Dummy, chromPeaks[3].MatchResults.Representative);
        }

        class MockProvider : IDataProvider
        {
            public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() {
                return new List<RawSpectrum> { new RawSpectrum() }.AsReadOnly();
            }

            public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) {
                throw new NotImplementedException();
            }

            public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() {
                throw new NotImplementedException();
            }
        }

        class MockQuery
        {

        }

        class MockFactory : IAnnotationQueryFactory<MockQuery>
        {
            public MockQuery Create(IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<RawPeakElement> spectrum, IonFeatureCharacter ionFeature) {
                return new MockQuery();
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

        class MockAnnotator : IAnnotator<MockQuery, MoleculeMsReference, MsScanMatchResult>
        {
            public MockAnnotator(string key) {
                Key = key;
                Dummy = new MsScanMatchResult
                {
                    Name = "dummy", AnnotatorID = Key, Source = SourceType.MspDB,
                };
            }

            public string Key { get; }

            public MsScanMatchResult Annotate(MockQuery query) {
                throw new NotImplementedException();
            }

            public MsScanMatchResult CalculateScore(MockQuery query, MoleculeMsReference reference) {
                throw new NotImplementedException();
            }

            public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
                return results.ToList();
            }

            public MsScanMatchResult Dummy { get; }

            public List<MsScanMatchResult> FindCandidates(MockQuery query) {
                return new List<MsScanMatchResult> { Dummy, };
            }

            public bool IsAnnotationSuggested(MsScanMatchResult result, MsRefSearchParameterBase parameter = null) {
                return false;
            }

            public bool IsReferenceMatched(MsScanMatchResult result, MsRefSearchParameterBase parameter = null) {
                return true;
            }

            public MoleculeMsReference Refer(MsScanMatchResult result) {
                return new MoleculeMsReference { Name = "Dummy reference", };
            }

            public List<MoleculeMsReference> Search(MockQuery query) {
                throw new NotImplementedException();
            }

            public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
                return results.ToList();
            }

            public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
                return results.FirstOrDefault();
            }
        }
    }
}