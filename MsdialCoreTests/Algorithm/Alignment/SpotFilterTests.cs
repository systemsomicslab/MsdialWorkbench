using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompMs.MsdialCore.Algorithm.Alignment;
using System;
using System.Collections.Generic;
using System.Text;
using CompMs.MsdialCore.DataObj;
using System.Linq;
using CompMs.Common.Enum;

namespace CompMs.MsdialCore.Algorithm.Alignment.Tests
{
    [TestClass()]
    public class SpotFilterTests
    {
        [TestMethod()]
        public void PeakCountFilterTest() {
            ISpotFilter filter = new PeakCountFilter(3);

            var props = new List<AlignmentSpotProperty>
            {
                new AlignmentSpotProperty
                {
                    AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                    {
                        new AlignmentChromPeakFeature { MasterPeakID = 0 },
                        new AlignmentChromPeakFeature { MasterPeakID = 0 },
                        new AlignmentChromPeakFeature { MasterPeakID = 0 },
                    }
                },

                new AlignmentSpotProperty
                {
                    AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                    {
                        new AlignmentChromPeakFeature { MasterPeakID = 0 },
                        new AlignmentChromPeakFeature { MasterPeakID = 0 },
                        new AlignmentChromPeakFeature { MasterPeakID = 0 },
                        new AlignmentChromPeakFeature { MasterPeakID = 0 },
                    }
                },

                new AlignmentSpotProperty
                {
                    AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                    {
                        new AlignmentChromPeakFeature { MasterPeakID = 0 },
                        new AlignmentChromPeakFeature { MasterPeakID = 1 },
                        new AlignmentChromPeakFeature { MasterPeakID = 2 },
                        new AlignmentChromPeakFeature { MasterPeakID = 3 },
                    }
                },

                new AlignmentSpotProperty
                {
                    AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                    {
                        new AlignmentChromPeakFeature { MasterPeakID = 0 },
                        new AlignmentChromPeakFeature { MasterPeakID = -1 },
                        new AlignmentChromPeakFeature { MasterPeakID = 2 },
                        new AlignmentChromPeakFeature { MasterPeakID = 3 },
                    }
                },

                new AlignmentSpotProperty
                {
                    AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                    {
                        new AlignmentChromPeakFeature { MasterPeakID = 0 },
                        new AlignmentChromPeakFeature { MasterPeakID = -1 },
                        new AlignmentChromPeakFeature { MasterPeakID = -2 },
                        new AlignmentChromPeakFeature { MasterPeakID = 3 },
                    }
                },
            };

            var expected = new List<AlignmentSpotProperty> { props[0], props[1], props[2], props[3], };

            var actual = filter.Filter(props);

            CollectionAssert.AreEqual(expected, actual.ToList());
        }

        [TestMethod]
        public void QcFilterTest() {
            var file2type = new Dictionary<int, AnalysisFileType>
            {
                { 0, AnalysisFileType.Blank },
                { 1, AnalysisFileType.QC },
                { 2, AnalysisFileType.Sample },
                { 3, AnalysisFileType.Sample },
                { 4, AnalysisFileType.Sample },
                { 5, AnalysisFileType.QC },
            };

            var spots = new List<AlignmentSpotProperty>
            {
                new AlignmentSpotProperty
                {
                    AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                    {
                        new AlignmentChromPeakFeature { MasterPeakID = 1, FileID = 0 },
                        new AlignmentChromPeakFeature { MasterPeakID = 1, FileID = 1 },
                        new AlignmentChromPeakFeature { MasterPeakID = 1, FileID = 2 },
                        new AlignmentChromPeakFeature { MasterPeakID = 1, FileID = 3 },
                        new AlignmentChromPeakFeature { MasterPeakID = 1, FileID = 4 },
                        new AlignmentChromPeakFeature { MasterPeakID = 1, FileID = 5 },
                    },
                },

                new AlignmentSpotProperty
                {
                    AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                    {
                        new AlignmentChromPeakFeature { MasterPeakID = -1, FileID = 0 },
                        new AlignmentChromPeakFeature { MasterPeakID = 1, FileID = 1 },
                        new AlignmentChromPeakFeature { MasterPeakID = -1, FileID = 2 },
                        new AlignmentChromPeakFeature { MasterPeakID = -1, FileID = 3 },
                        new AlignmentChromPeakFeature { MasterPeakID = -1, FileID = 4 },
                        new AlignmentChromPeakFeature { MasterPeakID = 1, FileID = 5 },
                    },
                },

                new AlignmentSpotProperty
                {
                    AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                    {
                        new AlignmentChromPeakFeature { MasterPeakID = 1, FileID = 0 },
                        new AlignmentChromPeakFeature { MasterPeakID = -1, FileID = 1 },
                        new AlignmentChromPeakFeature { MasterPeakID = 1, FileID = 2 },
                        new AlignmentChromPeakFeature { MasterPeakID = 1, FileID = 3 },
                        new AlignmentChromPeakFeature { MasterPeakID = 1, FileID = 4 },
                        new AlignmentChromPeakFeature { MasterPeakID = 1, FileID = 5 },
                    },
                },
            };

            var expected = new List<AlignmentSpotProperty> { spots[0], spots[1], };

            ISpotFilter filter = new QcFilter(file2type);
            var actual = filter.Filter(spots);

            CollectionAssert.AreEqual(expected, actual.ToList());
        }

        [TestMethod]
        public void DetectedNumberFilterTest() {
            var file2class = new Dictionary<int, string>
            {
                { 0, "1" },
                { 1, "2" }, { 2, "2" },
                { 3, "3" }, { 4, "3" }, { 5, "3" },
            };
            var threshold = 0.5;

            var spots = new List<AlignmentSpotProperty>
            {
                new AlignmentSpotProperty
                {
                    AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                    {
                        new AlignmentChromPeakFeature { MasterPeakID = 1, FileID = 0 },
                        new AlignmentChromPeakFeature { MasterPeakID = 1, FileID = 1 },
                        new AlignmentChromPeakFeature { MasterPeakID = 1, FileID = 2 },
                        new AlignmentChromPeakFeature { MasterPeakID = 1, FileID = 3 },
                        new AlignmentChromPeakFeature { MasterPeakID = 1, FileID = 4 },
                        new AlignmentChromPeakFeature { MasterPeakID = 1, FileID = 5 },
                    },
                },

                new AlignmentSpotProperty
                {
                    AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                    {
                        new AlignmentChromPeakFeature { MasterPeakID = -1, FileID = 0 },
                        new AlignmentChromPeakFeature { MasterPeakID = 1, FileID = 1 },
                        new AlignmentChromPeakFeature { MasterPeakID = -1, FileID = 2 },
                        new AlignmentChromPeakFeature { MasterPeakID = -1, FileID = 3 },
                        new AlignmentChromPeakFeature { MasterPeakID = -1, FileID = 4 },
                        new AlignmentChromPeakFeature { MasterPeakID = 1, FileID = 5 },
                    },
                },

                new AlignmentSpotProperty
                {
                    AlignedPeakProperties = new List<AlignmentChromPeakFeature>
                    {
                        new AlignmentChromPeakFeature { MasterPeakID = -1, FileID = 0 },
                        new AlignmentChromPeakFeature { MasterPeakID = -1, FileID = 1 },
                        new AlignmentChromPeakFeature { MasterPeakID = -1, FileID = 2 },
                        new AlignmentChromPeakFeature { MasterPeakID = 1, FileID = 3 },
                        new AlignmentChromPeakFeature { MasterPeakID = -1, FileID = 4 },
                        new AlignmentChromPeakFeature { MasterPeakID = -1, FileID = 5 },
                    },
                },
            };

            var expected = new List<AlignmentSpotProperty> { spots[0], spots[1] };

            ISpotFilter filter = new DetectedNumberFilter(file2class, threshold);
            var actual = filter.Filter(spots).ToList();

            Assert.AreEqual(2, actual.Count);
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void CompositeFilterTest() {
            var filter = new CompositeFilter();
            filter.Filters.Add(new MockFilter { X = 2 });
            filter.Filters.Add(new MockFilter { X = 3 });

            var spots = new List<AlignmentSpotProperty>
            {
                new AlignmentSpotProperty(), new AlignmentSpotProperty(), new AlignmentSpotProperty(),
                new AlignmentSpotProperty(), new AlignmentSpotProperty(), new AlignmentSpotProperty(),
                new AlignmentSpotProperty(), new AlignmentSpotProperty(), new AlignmentSpotProperty(),
                new AlignmentSpotProperty(), 
            };

            var expected = new List<AlignmentSpotProperty>
            {
                spots[0], spots[6],
            };
            var actual = filter.Filter(spots).ToList();
            CollectionAssert.AreEqual(expected, actual);
        }
        class MockFilter : ISpotFilter
        {
            public int X { get; set; }
            public IEnumerable<AlignmentSpotProperty> Filter(IEnumerable<AlignmentSpotProperty> spots) {
                foreach ((var spot, var idx) in spots.Select((spot, idx) => Tuple.Create(spot, idx))) {
                    if (idx % X == 0) yield return spot;
                }
            }
        }
    }
}