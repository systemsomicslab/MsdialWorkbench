using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace CompMs.MsdialCore.DataObj
{
    /// <summary>
    /// The "_tags.xml" file written beside a peak result (.pai2), an alignment result and a spectrum feature result:
    /// the tags of every peak, who set each tag, and the tags a person removed after a tool had set them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The file is the human-readable entry point to the tags: the GUI reloads it, and external tools read and write
    /// tags through it. It looks like this:
    /// <code>
    /// &lt;PeakSpotTags&gt;
    ///   &lt;Definitions&gt;&lt;Tag&gt;&lt;Id&gt;1&lt;/Id&gt;&lt;Label&gt;Confirmed&lt;/Label&gt;&lt;/Tag&gt;...&lt;/Definitions&gt;
    ///   &lt;Peaks&gt;
    ///     &lt;Peak Id="123"&gt;
    ///       &lt;Tag&gt;3&lt;/Tag&gt;                                   (a person's tag: no source)
    ///       &lt;Tag Source="MsdialLipidRtCheck/1.0"&gt;1&lt;/Tag&gt;   (set by a tool)
    ///       &lt;Suppressed Source="User"&gt;1&lt;/Suppressed&gt;       (a person removed a tool's tag; no tool sets it again)
    ///     &lt;/Peak&gt;
    ///   &lt;/Peaks&gt;
    /// &lt;/PeakSpotTags&gt;
    /// </code>
    /// </para>
    /// <para>
    /// BACKWARD COMPATIBLE BOTH WAYS. MS-DIAL before tag provenance reads only the value of each <c>Tag</c> element of a
    /// <c>Peak</c>, so it ignores the Source attribute and the <c>Suppressed</c> elements and reads the same set of
    /// selected tags. A file it wrote has neither, and reads here as tags set by a person or by nobody recorded, with
    /// nothing suppressed. A tag without a source is written exactly as it always was.
    /// </para>
    /// <para>
    /// A peak with only suppressions is written too, because the suppression is what stops a tool from setting the tag
    /// again; an older reader sees it as a peak without tags.
    /// </para>
    /// </remarks>
    public static class PeakSpotTagXml
    {
        public const string SourceAttribute = "Source";

        /// <summary>The tag file for the given objects; only objects with a tag or a suppression get a Peak element.</summary>
        public static XElement Create<T>(IEnumerable<T> items, Func<T, int> id, Func<T, PeakSpotTagCollection> tags) {
            var defsElement = new XElement("Definitions");
            foreach (var tag in PeakSpotTag.AllTypes()) {
                var tagElement = new XElement("Tag", new XElement("Id", tag.Id), new XElement("Label", tag.Label));
                defsElement.Add(tagElement);
            }
            var peaksElement = new XElement("Peaks");
            foreach (var item in items) {
                var collection = tags(item);
                if (collection is null || !collection.HasEntries()) {
                    continue;
                }
                var peakElement = new XElement("Peak");
                peakElement.SetAttributeValue("Id", id(item));
                foreach (var tag in collection.Selected) {
                    var tagElement = new XElement("Tag", tag.Id);
                    var source = collection.GetSource(tag);
                    if (!string.IsNullOrEmpty(source)) {
                        tagElement.SetAttributeValue(SourceAttribute, source);
                    }
                    peakElement.Add(tagElement);
                }
                foreach (var tag in collection.Suppressed) {
                    var suppressedElement = new XElement("Suppressed", tag.Id);
                    var source = collection.GetSuppressionSource(tag);
                    if (!string.IsNullOrEmpty(source)) {
                        suppressedElement.SetAttributeValue(SourceAttribute, source);
                    }
                    peakElement.Add(suppressedElement);
                }
                peaksElement.Add(peakElement);
            }
            return new XElement("PeakSpotTags", defsElement, peaksElement);
        }

        /// <summary>
        /// Sets the tags the file records on the given objects. Objects the file does not name are left as they are, and
        /// a tag id this version does not know is skipped, as before.
        /// </summary>
        public static void Apply<T>(XElement document, IEnumerable<T> items, Func<T, int> id, Func<T, PeakSpotTagCollection> tags) {
            var records = Read(document);
            if (records.Count == 0) {
                return;
            }
            foreach (var item in items) {
                if (records.TryGetValue(id(item), out var record)) {
                    tags(item).Set(record.Selected, record.Suppressed);
                }
            }
        }

        internal sealed class Record
        {
            public List<(PeakSpotTag Tag, string Source)> Selected { get; } = new List<(PeakSpotTag, string)>();
            public List<(PeakSpotTag Tag, string Source)> Suppressed { get; } = new List<(PeakSpotTag, string)>();
        }

        internal static Dictionary<int, Record> Read(XElement document) {
            var records = new Dictionary<int, Record>();
            foreach (var peakElement in document.Descendants("Peak")) {
                if (!int.TryParse(peakElement.Attribute("Id")?.Value, out var id)) {
                    continue;
                }
                var record = new Record();
                foreach (var element in peakElement.Elements("Tag")) {
                    if (TryGetTag(element, out var tag)) {
                        record.Selected.Add((tag, element.Attribute(SourceAttribute)?.Value));
                    }
                }
                foreach (var element in peakElement.Elements("Suppressed")) {
                    if (TryGetTag(element, out var tag)) {
                        record.Suppressed.Add((tag, element.Attribute(SourceAttribute)?.Value));
                    }
                }
                records[id] = record;
            }
            return records;
        }

        private static bool TryGetTag(XElement element, out PeakSpotTag tag) {
            tag = int.TryParse(element.Value, out var value) ? PeakSpotTag.GetById(value) : null;
            return tag != null;
        }
    }
}
