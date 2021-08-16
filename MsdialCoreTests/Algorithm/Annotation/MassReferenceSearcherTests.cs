using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation.Tests
{
    [TestClass()]
    public class MassReferenceSearcherTests
    {
        [TestMethod()]
        public void MassReferenceSearcherTest() {
            var db = new[]
            {
                new MockReference { PrecursorMz = 100, },
                new MockReference { PrecursorMz = 120, },
                new MockReference { PrecursorMz = 90, },
                new MockReference { PrecursorMz = 200, },
                new MockReference { PrecursorMz = 120, },
                new MockReference { PrecursorMz = 130, },
            };
            var searcher = new MassReferenceSearcher<MockReference>(db);

            var actuals = searcher.Search(new MassSearchQuery(130, 20));
            var expected = new[] { db[1], db[4], db[5], };

            CollectionAssert.AreEquivalent(expected, actuals.ToArray());
        }
    }

    class MockReference : IMSProperty
    {
        public ChromXs ChromXs { get; set; }
        public IonMode IonMode { get; set; }
        public double PrecursorMz { get; set; }
    }
}