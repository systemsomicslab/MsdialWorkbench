using CompMs.Common.Components;
using CompMs.MsdialCore.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CompMs.MsdialCore.DataObj.Tests
{
    /// <summary>
    /// The "_tags.xml" files with tag provenance (addendum B1): a Source attribute on a tool's tag and Suppressed elements,
    /// round-tripped through the peak serializer and the alignment container, and compatible both ways with MS-DIAL
    /// before provenance.
    /// </summary>
    /// <remarks>
    /// "The old logic" below is the reading and writing code of MsdialPeakSerializer and AlignmentResultContainer as it
    /// was before provenance (master 1cb207a44), copied verbatim, so that the tests say what an older MS-DIAL does with a
    /// file this version writes, and what this version does with a file an older one wrote.
    /// </remarks>
    [TestClass]
    public class PeakSpotTagXmlTests
    {
        private const string Tool = "MsdialLipidRtCheck/1.0";
        private const string Agent = "Agent/curator/2";

        private string _scratch = null!;

        [TestInitialize]
        public void Initialize() {
            _scratch = Path.Combine(Path.GetTempPath(), $"msdial-tags-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_scratch);
        }

        [TestCleanup]
        public void Cleanup() {
            if (Directory.Exists(_scratch)) {
                Directory.Delete(_scratch, recursive: true);
            }
        }

        [TestMethod]
        public void PeakTagsRoundTripWithSourcesAndSuppressions() {
            var peaks = new List<ChromatogramPeakFeature> { Peak(0), Peak(1), Peak(2), Peak(3) };
            peaks[0].TagCollection.Select(PeakSpotTag.MISANNOTATION);
            peaks[0].TagCollection.Select(PeakSpotTag.CONFIRMED, Tool);
            peaks[1].TagCollection.Suppress(PeakSpotTag.CONFIRMED);
            peaks[2].TagCollection.Select(PeakSpotTag.CONFIRMED, Agent);
            peaks[2].TagCollection.Suppress(PeakSpotTag.COELUTION, Agent);
            var file = Path.Combine(_scratch, "sample.pai");

            MsdialPeakSerializer.SaveChromatogramPeakFeatures(file, peaks);
            var loaded = MsdialPeakSerializer.LoadChromatogramPeakFeatures(file);

            AssertSameTags(peaks.Select(p => p.TagCollection).ToList(), loaded.Select(p => p.TagCollection).ToList());
            Assert.AreEqual(Tool, loaded[0].TagCollection.GetSource(PeakSpotTag.CONFIRMED));
            Assert.IsNull(loaded[0].TagCollection.GetSource(PeakSpotTag.MISANNOTATION));
            Assert.IsTrue(loaded[1].TagCollection.IsSuppressed(PeakSpotTag.CONFIRMED), "a peak with only a suppression is written and read");
            Assert.AreEqual(PeakSpotTagCollection.PersonSource, loaded[1].TagCollection.GetSuppressionSource(PeakSpotTag.CONFIRMED));
            Assert.AreEqual(Agent, loaded[2].TagCollection.GetSuppressionSource(PeakSpotTag.COELUTION));
            Assert.IsFalse(loaded[3].TagCollection.HasEntries());
        }

        [TestMethod]
        public void AlignmentTagsRoundTripWithSourcesAndSuppressions() {
            var spots = new[] { Spot(0), Spot(1), Spot(2) };
            spots[0].TagCollection.Select(PeakSpotTag.CONFIRMED, Tool);
            spots[1].TagCollection.Select(PeakSpotTag.LOW_QUALITY_SPECTRUM);
            spots[1].TagCollection.Suppress(PeakSpotTag.CONFIRMED);
            var container = new AlignmentResultContainer { AlignmentSpotProperties = new ObservableCollection<AlignmentSpotProperty>(spots) };
            var file = new AlignmentFileBean { FilePath = Path.Combine(_scratch, "AlignmentResult.arf"), FileName = "AlignmentResult" };

            container.Save(file);
            var loaded = AlignmentResultContainer.Load(file);
            var lazy = AlignmentResultContainer.LoadLazy(file);
            lazy.LoadAlginedPeakPropertiesTask.Wait();

            AssertSameTags(spots.Select(s => s.TagCollection).ToList(), loaded.AlignmentSpotProperties.Select(s => s.TagCollection).ToList());
            AssertSameTags(spots.Select(s => s.TagCollection).ToList(), lazy.AlignmentSpotProperties.Select(s => s.TagCollection).ToList());
            Assert.AreEqual(Tool, loaded.AlignmentSpotProperties[0].TagCollection.GetSource(PeakSpotTag.CONFIRMED));
            Assert.IsTrue(loaded.AlignmentSpotProperties[1].TagCollection.IsSuppressed(PeakSpotTag.CONFIRMED));
        }

        [TestMethod]
        public void TheFileLooksAsTheAddendumSays() {
            var peaks = new[] { Peak(123) };
            peaks[0].TagCollection.Select(PeakSpotTag.MISANNOTATION);
            peaks[0].TagCollection.Select(PeakSpotTag.CONFIRMED, Tool);
            var other = Peak(124);
            other.TagCollection.Suppress(PeakSpotTag.CONFIRMED);

            var doc = PeakSpotTagXml.Create(peaks.Append(other), p => p.MasterPeakID, p => p.TagCollection);

            var peak = doc.Descendants("Peak").Single(e => e.Attribute("Id")!.Value == "123");
            Assert.AreEqual("<Tag>3</Tag>", peak.Elements("Tag").ElementAt(0).ToString(SaveOptions.DisableFormatting), "a person's tag is written exactly as before");
            Assert.AreEqual("<Tag Source=\"MsdialLipidRtCheck/1.0\">1</Tag>", peak.Elements("Tag").ElementAt(1).ToString(SaveOptions.DisableFormatting));
            var suppressed = doc.Descendants("Peak").Single(e => e.Attribute("Id")!.Value == "124");
            Assert.AreEqual("<Peak Id=\"124\"><Suppressed Source=\"User\">1</Suppressed></Peak>", suppressed.ToString(SaveOptions.DisableFormatting));
        }

        [TestMethod]
        public void WithoutProvenanceTheFileIsWhatTheOldWriterWrote() {
            var peaks = new[] { Peak(0), Peak(1), Peak(2) };
            peaks[0].TagCollection.Select(PeakSpotTag.CONFIRMED);
            peaks[0].TagCollection.Select(PeakSpotTag.OVERANNOTATION);
            peaks[2].TagCollection.Select(PeakSpotTag.COELUTION);

            var now = PeakSpotTagXml.Create(peaks, p => p.MasterPeakID, p => p.TagCollection);

            Assert.AreEqual(WriteAsBeforeProvenance(peaks).ToString(), now.ToString());
        }

        [TestMethod]
        public void AFileWrittenBeforeProvenanceReadsAsTagsWithNoSourceAndNothingSuppressed() {
            var before = new[] { Peak(0), Peak(1) };
            before[0].TagCollection.Select(PeakSpotTag.CONFIRMED);
            before[0].TagCollection.Select(PeakSpotTag.MISANNOTATION);
            var file = Path.Combine(_scratch, "old.pai");
            MsdialPeakSerializer.SaveChromatogramPeakFeatures(file, before.ToList());
            // The tag file as the old writer wrote it.
            WriteAsBeforeProvenance(before).Save(Path.Combine(_scratch, "old_tags.xml"));

            var loaded = MsdialPeakSerializer.LoadChromatogramPeakFeatures(file);

            CollectionAssert.AreEqual(new[] { PeakSpotTag.CONFIRMED, PeakSpotTag.MISANNOTATION }, loaded[0].TagCollection.Selected.ToArray());
            Assert.IsNull(loaded[0].TagCollection.GetSource(PeakSpotTag.CONFIRMED));
            Assert.IsNull(loaded[0].TagCollection.GetSource(PeakSpotTag.MISANNOTATION));
            Assert.AreEqual(0, loaded[0].TagCollection.Suppressed.Count);
            Assert.IsFalse(loaded[1].TagCollection.HasEntries());
        }

        [TestMethod]
        public void TheOldReaderReadsANewFileAsTheSameSelectedTags() {
            var peaks = new[] { Peak(0), Peak(1), Peak(2), Peak(3) };
            peaks[0].TagCollection.Select(PeakSpotTag.MISANNOTATION);
            peaks[0].TagCollection.Select(PeakSpotTag.CONFIRMED, Tool);
            peaks[1].TagCollection.Suppress(PeakSpotTag.CONFIRMED);
            peaks[2].TagCollection.Select(PeakSpotTag.CONFIRMED, Agent);
            peaks[2].TagCollection.Select(PeakSpotTag.COELUTION);
            peaks[2].TagCollection.Suppress(PeakSpotTag.OVERANNOTATION);
            var file = Path.Combine(_scratch, "new.pai");
            MsdialPeakSerializer.SaveChromatogramPeakFeatures(file, peaks.ToList());

            var old = ReadAsBeforeProvenance(XElement.Load(Path.Combine(_scratch, "new_tags.xml")));

            foreach (var peak in peaks) {
                var oldTags = old.TryGetValue(peak.MasterPeakID, out var tags) ? tags : Array.Empty<PeakSpotTag>();
                CollectionAssert.AreEqual(peak.TagCollection.Selected.ToArray(), oldTags, $"peak {peak.MasterPeakID}");
            }
            Assert.AreEqual(0, old[1].Length, "a peak with only a suppression reads as a peak without tags");
        }

        [TestMethod]
        public void AnUnknownTagIdOrAMissingPeakIdIsSkipped() {
            var doc = XElement.Parse(
                "<PeakSpotTags><Peaks>" +
                "<Peak><Tag>1</Tag></Peak>" +
                "<Peak Id=\"5\"><Tag Source=\"x\">99</Tag><Tag>2</Tag><Suppressed>98</Suppressed><Suppressed>1</Suppressed></Peak>" +
                "</Peaks></PeakSpotTags>");
            var peaks = new[] { Peak(5) };

            PeakSpotTagXml.Apply(doc, peaks, p => p.MasterPeakID, p => p.TagCollection);

            CollectionAssert.AreEqual(new[] { PeakSpotTag.LOW_QUALITY_SPECTRUM }, peaks[0].TagCollection.Selected.ToArray());
            CollectionAssert.AreEqual(new[] { PeakSpotTag.CONFIRMED }, peaks[0].TagCollection.Suppressed.ToArray());
            Assert.IsNull(peaks[0].TagCollection.GetSuppressionSource(PeakSpotTag.CONFIRMED), "no Source attribute, no source");
        }

        private static void AssertSameTags(IReadOnlyList<PeakSpotTagCollection> expected, IReadOnlyList<PeakSpotTagCollection> actual) {
            Assert.AreEqual(expected.Count, actual.Count);
            for (var i = 0; i < expected.Count; i++) {
                CollectionAssert.AreEqual(expected[i].Selected.ToArray(), actual[i].Selected.ToArray(), $"selected, object {i}");
                CollectionAssert.AreEqual(expected[i].Selected.Select(expected[i].GetSource).ToArray(), actual[i].Selected.Select(actual[i].GetSource).ToArray(), $"sources, object {i}");
                CollectionAssert.AreEqual(expected[i].Suppressed.ToArray(), actual[i].Suppressed.ToArray(), $"suppressed, object {i}");
                CollectionAssert.AreEqual(expected[i].Suppressed.Select(expected[i].GetSuppressionSource).ToArray(), actual[i].Suppressed.Select(actual[i].GetSuppressionSource).ToArray(), $"suppression sources, object {i}");
            }
        }

        private static ChromatogramPeakFeature Peak(int id) {
            return new ChromatogramPeakFeature(new BaseChromatogramPeakFeature
            {
                Mass = 500d + id, PeakHeightTop = 1e5, ChromXsLeft = new ChromXs(1d), ChromXsTop = new ChromXs(1d + id), ChromXsRight = new ChromXs(2d + id),
            })
            {
                MasterPeakID = id,
            };
        }

        private static AlignmentSpotProperty Spot(int id) {
            return new AlignmentSpotProperty
            {
                MasterAlignmentID = id,
                AlignmentID = id,
                TimesCenter = new ChromXs(1d + id),
                MassCenter = 500d + id,
                AlignedPeakProperties = new List<AlignmentChromPeakFeature>(),
                AlignmentDriftSpotFeatures = new List<AlignmentSpotProperty>(),
            };
        }

        // ---- the old logic, verbatim from master 1cb207a44 ----

        private static XElement WriteAsBeforeProvenance(IEnumerable<ChromatogramPeakFeature> chromPeakFeatures) {
            var defsElement = new XElement("Definitions");
            foreach (var tag in PeakSpotTag.AllTypes()) {
                var tagElement = new XElement("Tag", new XElement("Id", tag.Id), new XElement("Label", tag.Label));
                defsElement.Add(tagElement);
            }
            var peaksElement = new XElement("Peaks");
            foreach (var peak in chromPeakFeatures) {
                if (!peak.TagCollection.Any()) {
                    continue;
                }
                var peakElement = new XElement("Peak");
                peakElement.SetAttributeValue("Id", peak.MasterPeakID);
                foreach (var tag in peak.TagCollection.Selected) {
                    peakElement.Add(new XElement("Tag", tag.Id));
                }
                peaksElement.Add(peakElement);
            }
            return new XElement("PeakSpotTags", defsElement, peaksElement);
        }

        private static Dictionary<int, PeakSpotTag[]> ReadAsBeforeProvenance(XElement doc) {
            var dic = new Dictionary<int, PeakSpotTag[]>();
            foreach (var peakElement in doc.Descendants("Peak")) {
                if (int.TryParse(peakElement.Attribute("Id").Value, out var id)) {
                    var tags = peakElement.Elements("Tag")
                        .Select(elem => int.TryParse(elem.Value, out var v) ? PeakSpotTag.GetById(v) : null)
                        .Where(v => v != null)
                        .ToArray();
                    dic[id] = tags;
                }
            }
            return dic;
        }
    }
}
