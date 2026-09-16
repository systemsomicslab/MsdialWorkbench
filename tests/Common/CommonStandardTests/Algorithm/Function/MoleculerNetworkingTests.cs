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
    }
}
