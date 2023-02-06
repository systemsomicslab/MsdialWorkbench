using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using CompMs.Common.Interfaces;
using CompMs.Common.Components;
using System.Linq;
using CompMs.Common.Enum;

namespace CompMs.MsdialCore.Algorithm.Annotation.Tests
{
    [TestClass()]
    public class MassRtReferenceSearcherTests
    {
        [TestMethod()]
        public void SearchTest() {
            var db = new[]
            {
                new MockReference { PrecursorMz = 100, ChromXs = new ChromXs(100, ChromXType.RT, ChromXUnit.Min), },
                new MockReference { PrecursorMz = 89, ChromXs = new ChromXs(100, ChromXType.RT, ChromXUnit.Min), },
                new MockReference { PrecursorMz = 111, ChromXs = new ChromXs(100, ChromXType.RT, ChromXUnit.Min), },
                new MockReference { PrecursorMz = 94, ChromXs = new ChromXs(100, ChromXType.RT, ChromXUnit.Min), },
                new MockReference { PrecursorMz = 106, ChromXs = new ChromXs(100, ChromXType.RT, ChromXUnit.Min), },
                new MockReference { PrecursorMz = 100, ChromXs = new ChromXs(94, ChromXType.RT, ChromXUnit.Min), },
                new MockReference { PrecursorMz = 100, ChromXs = new ChromXs(106, ChromXType.RT, ChromXUnit.Min), },
                new MockReference { PrecursorMz = 100, ChromXs = new ChromXs(96, ChromXType.RT, ChromXUnit.Min), },
                new MockReference { PrecursorMz = 100, ChromXs = new ChromXs(104, ChromXType.RT, ChromXUnit.Min), },
                new MockReference { PrecursorMz = 89, ChromXs = new ChromXs(94, ChromXType.RT, ChromXUnit.Min), },
                new MockReference { PrecursorMz = 89, ChromXs = new ChromXs(106, ChromXType.RT, ChromXUnit.Min), },
                new MockReference { PrecursorMz = 100, ChromXs = new ChromXs(100, ChromXType.RT, ChromXUnit.Min), },
                new MockReference { PrecursorMz = 111, ChromXs = new ChromXs(94, ChromXType.RT, ChromXUnit.Min), },
                new MockReference { PrecursorMz = 111, ChromXs = new ChromXs(106, ChromXType.RT, ChromXUnit.Min), },
                new MockReference { PrecursorMz = 100, ChromXs = new ChromXs(100, ChromXType.RT, ChromXUnit.Min), },
            };

            var searcher = new MassRtReferenceSearcher<MockReference>(db);

            var actuals = searcher.Search(MSSearchQuery.CreateMassRtQuery(100, 10, 100, 5));
            var expected = new[] { db[0], db[3], db[4], db[7], db[8], db[11], db[14], };

            CollectionAssert.AreEquivalent(expected, actuals.ToArray());
        }

        class MockReference : IMSProperty
        {
            public ChromXs ChromXs { get; set; }
            public double PrecursorMz { get; set; }
            public IonMode IonMode { get; set; }
        }
    }
}