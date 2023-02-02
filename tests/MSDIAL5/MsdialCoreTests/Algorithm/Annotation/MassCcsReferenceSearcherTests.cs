using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation.Tests
{
    [TestClass()]
    public class MassCcsReferenceSearcherTests
    {
        [TestMethod()]
        public void SearchTest() {
            var db = new[]
            {
                new MockReference { PrecursorMz = 100, CollisionCrossSection = 100, },
                new MockReference { PrecursorMz = 89, CollisionCrossSection = 100, },
                new MockReference { PrecursorMz = 111, CollisionCrossSection = 100, },
                new MockReference { PrecursorMz = 94, CollisionCrossSection = 100, },
                new MockReference { PrecursorMz = 106, CollisionCrossSection = 100, },
                new MockReference { PrecursorMz = 100, CollisionCrossSection = 94, },
                new MockReference { PrecursorMz = 100, CollisionCrossSection = 106, },
                new MockReference { PrecursorMz = 100, CollisionCrossSection = 96, },
                new MockReference { PrecursorMz = 100, CollisionCrossSection = 104, },
                new MockReference { PrecursorMz = 89, CollisionCrossSection = 94, },
                new MockReference { PrecursorMz = 89, CollisionCrossSection = 106, },
                new MockReference { PrecursorMz = 100, CollisionCrossSection = 100, },
                new MockReference { PrecursorMz = 111, CollisionCrossSection = 94, },
                new MockReference { PrecursorMz = 111, CollisionCrossSection = 106, },
                new MockReference { PrecursorMz = 100, CollisionCrossSection = 100, },
            };

            var searcher = new MassCcsReferenceSearcher<MockReference>(db);

            var actuals = searcher.Search(MSIonSearchQuery.CreateMassCcsQuery(100, 10, 100, 5));
            var expected = new[] { db[0], db[3], db[4], db[7], db[8], db[11], db[14], };

            CollectionAssert.AreEquivalent(expected, actuals.ToArray());
        }

        class MockReference : IMSIonProperty
        {
            public ChromXs ChromXs { get; set; }
            public IonMode IonMode { get; set; }
            public double PrecursorMz { get; set; }
            public AdductIon AdductType { get; set; }
            public double CollisionCrossSection { get; set; }

            public void SetAdductType(AdductIon adduct) {
                AdductType = adduct;
            }
        }
    }
}