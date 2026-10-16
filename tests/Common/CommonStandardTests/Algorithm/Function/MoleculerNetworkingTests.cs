using CompMs.Common.Components;
using CompMs.Common.DataObj.NodeEdge;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Algorithm.Function.Tests
{
    [TestClass]
    public class MoleculerNetworkingTests
    {
        private const string ONTOLOGY = "SM";
        private static readonly string ONTOLOGY_COLOR = MetaboliteColorCode.metabolite_colorcode[ONTOLOGY];
        private const string UNCHARACTERIZED_COLOR = "rgb(0,0,0)";

        private sealed class TestSpot : IMoleculeProperty, IChromatogramPeak
        {
            public int ID { get; set; }
            public ChromXs ChromXs { get; set; } = new ChromXs(1d);
            public double Mass { get; set; } = 700d;
            public double Intensity { get; set; }
            public string Name { get; set; }
            public Formula Formula { get; set; }
            public string Ontology { get; set; }
            public string SMILES { get; set; }
            public string InChIKey { get; set; }
        }

        /// <summary>
        /// Every node carries an ontology MetaboliteColorCode knows, so the background
        /// colour is decided by the name shape alone.
        /// </summary>
        private static Dictionary<string, string> GetNodeColorsByName(params string[] names) {
            var spots = names
                .Select((name, i) => new TestSpot { ID = i, Name = name, Ontology = ONTOLOGY, Intensity = 100d * (i + 1), })
                .ToList();
            var scans = spots
                .Select(spot => (IMSScanProperty)new MSScanProperty(spot.ID, spot.Mass, new RetentionTime(1d), IonMode.Positive))
                .ToList();

            var instance = new MoleculerNetworkingBase()
                .GetMolecularNetworkInstance(spots, scans, new MolecularNetworkingQuery(), report: null);

            return instance.Root.nodes.ToDictionary(node => node.data.Name, node => node.data.backgroundcolor);
        }

        /// <summary>
        /// Regression test for the ontology colour of the four MS-DIAL 5 name shapes.
        /// The predicate used to test only the MS-DIAL 4 "w/o MS2" spelling, so
        /// "no MS2: " and "low score: " suggestions were coloured as if they were
        /// accepted annotations.
        /// </summary>
        [TestMethod]
        public void OntologyColorIsGivenOnlyToAcceptedAnnotations() {
            var colors = GetNodeColorsByName(
                "SM 18:1;O2/16:0",
                "no MS2: SM 18:1;O2/16:0",
                "low score: SM 18:1;O2/16:0",
                "Unknown");

            Assert.AreEqual(ONTOLOGY_COLOR, colors["SM 18:1;O2/16:0"], "an accepted reference match keeps its ontology colour");
            Assert.AreEqual(UNCHARACTERIZED_COLOR, colors["no MS2: SM 18:1;O2/16:0"], "a precursor-only suggestion has no product-ion evidence");
            Assert.AreEqual(UNCHARACTERIZED_COLOR, colors["low score: SM 18:1;O2/16:0"], "a low-score suggestion failed the search criteria");
            Assert.AreEqual(UNCHARACTERIZED_COLOR, colors["Unknown"], "an unannotated feature has no ontology");
        }

        [TestMethod]
        public void OntologyColorIsWithheldFromPeptideSuggestions() {
            var colors = GetNodeColorsByName("SM 18:1;O2/16:0", "w/o MS2: SM 18:1;O2/16:0");

            Assert.AreEqual(ONTOLOGY_COLOR, colors["SM 18:1;O2/16:0"]);
            Assert.AreEqual(UNCHARACTERIZED_COLOR, colors["w/o MS2: SM 18:1;O2/16:0"]);
        }

        private static readonly double[] SHARED_FRAGMENTS = { 184.07, 264.27, 520.34, 682.59 };

        private static (List<TestSpot> spots, List<IMSScanProperty> scans) CreatePeakScans(int[] ids, double[] precursors, bool[] hasSpectrum) {
            var spots = ids
                .Select((id, i) => new TestSpot { ID = id, Name = "Unknown", Ontology = ONTOLOGY, Mass = precursors[i], Intensity = 100d * (i + 1), })
                .ToList();
            var scans = spots
                .Select((spot, i) => {
                    var scan = new MSScanProperty(spot.ID, spot.Mass, new RetentionTime(1d), IonMode.Positive);
                    if (hasSpectrum[i]) {
                        foreach (var mz in SHARED_FRAGMENTS) {
                            scan.AddPeak(mz, 1000d);
                        }
                        // A spot-specific fragment makes each spectrum distinct.
                        scan.AddPeak(100d + 10d * i, 500d);
                    }
                    return (IMSScanProperty)scan;
                })
                .ToList();
            return (spots, scans);
        }

        private static MolecularNetworkingQuery CreateQuery() {
            return new MolecularNetworkingQuery
            {
                MsmsSimilarityCalc = MsmsSimilarityCalc.ModDot,
                MassTolerance = 0.05,
                SpectrumSimilarityCutOff = 0d,
                MinimumPeakMatch = 1d,
                MaxEdgeNumberPerNode = 100d,
                MaxPrecursorDifference = 50d,
            };
        }

        /// <summary>
        /// A network built from one peak list emits each similar pair exactly once, from the
        /// spot listed earlier to the spot listed later, and never pairs a spot with itself.
        /// Spots without MS/MS and spots outside the precursor window are not connected.
        /// </summary>
        [TestMethod]
        public void SelfNetworkEmitsEachPairOnceFromEarlierToLaterSpot() {
            // IDs deliberately out of order so the direction is decided by list position, not by ID.
            var ids = new[] { 4, 2, 0, 3, 1, 5, };
            var precursors = new[] { 700d, 710d, 720d, 730d, 740d, 900d, };
            var hasSpectrum = new[] { true, true, false, true, true, true, };
            var (spots, scans) = CreatePeakScans(ids, precursors, hasSpectrum);

            var instance = new MoleculerNetworkingBase().GetMolecularNetworkInstance(spots, scans, CreateQuery(), report: null);

            var actual = instance.Root.edges.Select(edge => (edge.data.source, edge.data.target)).OrderBy(p => p).ToList();
            var expected = new List<(int, int)>
            {
                (4, 2), (4, 3), (4, 1),
                (2, 3), (2, 1),
                (3, 1),
            }.OrderBy(p => p).ToList();
            CollectionAssert.AreEqual(expected, actual);
            Assert.IsTrue(instance.Root.edges.All(edge => edge.data.score > 0d));
        }

        [TestMethod]
        public void SelfNetworkProgressIsMonotonicAndEndsAtOne() {
            var ids = new[] { 0, 1, 2, 3, 4, };
            var precursors = new[] { 700d, 710d, 720d, 730d, 740d, };
            var hasSpectrum = new[] { true, false, true, true, true, };
            var (spots, scans) = CreatePeakScans(ids, precursors, hasSpectrum);
            var reports = new List<double>();

            new MoleculerNetworkingBase().GetMolecularNetworkInstance(spots, scans, CreateQuery(), reports.Add);

            Assert.IsTrue(reports.Count > 0);
            Assert.IsTrue(reports.All(r => !double.IsNaN(r) && r >= 0d && r <= 1d), string.Join(", ", reports));
            Assert.IsTrue(reports.Zip(reports.Skip(1), (a, b) => a <= b).All(x => x), string.Join(", ", reports));
            Assert.AreEqual(1d, reports.Last(), 1e-12);
        }

        [TestMethod]
        public void SelfNetworkOfSingleSpotHasNoEdgesAndValidProgress() {
            var (spots, scans) = CreatePeakScans(new[] { 0, }, new[] { 700d, }, new[] { true, });
            var reports = new List<double>();

            var instance = new MoleculerNetworkingBase().GetMolecularNetworkInstance(spots, scans, CreateQuery(), reports.Add);

            Assert.AreEqual(0, instance.Root.edges.Count);
            Assert.IsTrue(reports.All(r => !double.IsNaN(r) && !double.IsInfinity(r)), string.Join(", ", reports));
        }

        /// <summary>
        /// The target-spot network still compares the target with every listed spot,
        /// including spots listed before it.
        /// </summary>
        [TestMethod]
        public void TargetSpotNetworkConnectsTargetToEverySimilarSpot() {
            var ids = new[] { 4, 2, 0, 3, 1, };
            var precursors = new[] { 700d, 710d, 720d, 730d, 900d, };
            var hasSpectrum = new[] { true, true, false, true, true, };
            var (spots, scans) = CreatePeakScans(ids, precursors, hasSpectrum);
            var reports = new List<double>();

            var instance = new MoleculerNetworkingBase().GetMoleculerNetworkInstanceForTargetSpot(spots[1], scans[1], spots, scans, CreateQuery(), reports.Add);

            var actual = instance.Root.edges.Select(edge => (edge.data.source, edge.data.target)).OrderBy(p => p).ToList();
            var expected = new List<(int, int)> { (2, 3), (2, 4), };
            CollectionAssert.AreEqual(expected, actual);
            Assert.AreEqual(1d, reports.Last(), 1e-12);
        }
    }
}
