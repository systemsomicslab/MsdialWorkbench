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

        /// <summary>
        /// A reference that carries no retention time is exempt from the retention window rather
        /// than excluded by it.
        /// </summary>
        /// <remarks>
        /// This is the filtering half of the same defect the scoring half had. The behaviour used to
        /// flip on the tolerance, and neither side of the flip was defensible: with a narrow window
        /// every entry that had no retention time was silently dropped from the candidate set, while
        /// the 100-minute default admitted them all and handed them to the unguarded Gaussian to be
        /// scored against -1. A library where only some entries carry retention times -- the case
        /// this was reported for -- lost the rest entirely as soon as the analyst tightened the
        /// tolerance.
        ///
        /// Exempting them costs nothing: the mass range has already bounded the set this filters.
        /// </remarks>
        [TestMethod()]
        public void AReferenceWithNoRetentionTimeIsNotExcludedByTheWindow() {
            var withRetentionTime = new MockReference { PrecursorMz = 100, ChromXs = new ChromXs(100, ChromXType.RT, ChromXUnit.Min), };
            var farAway = new MockReference { PrecursorMz = 100, ChromXs = new ChromXs(50, ChromXType.RT, ChromXUnit.Min), };
            // ChromXs left at its default, whose RT value is -1: a library entry with no RT.
            var withoutRetentionTime = new MockReference { PrecursorMz = 100, ChromXs = new ChromXs(), };
            var db = new[] { withRetentionTime, farAway, withoutRetentionTime, };

            var searcher = new MassRtReferenceSearcher<MockReference>(db);
            var actuals = searcher.Search(MSSearchQuery.CreateMassRtQuery(100, 10, 100, 5)).ToArray();

            CollectionAssert.Contains(actuals, withoutRetentionTime,
                "no retention time means no retention evidence, which is not grounds for exclusion");
            CollectionAssert.Contains(actuals, withRetentionTime);
            CollectionAssert.DoesNotContain(actuals, farAway,
                "a reference that does carry a retention time is still filtered on it");
        }

        class MockReference : IMSProperty
        {
            public ChromXs ChromXs { get; set; }
            public double PrecursorMz { get; set; }
            public IonMode IonMode { get; set; }
        }
    }
}