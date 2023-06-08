using CompMs.Common.MessagePack;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CompMs.MsdialCore.Parser
{
    public static class MsdialPeakSerializer
    {
        public static void SaveChromatogramPeakFeatures(string file, List<ChromatogramPeakFeature> chromPeakFeatures) {
            MessagePackHandler.SaveToFile<List<ChromatogramPeakFeature>>(chromPeakFeatures, file);
            var tagfile = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + "_tags.xml");
            var defsElement = new XElement("Definitions");
            foreach (var tag in PeakSpotTag.AllTypes()) {
                var tagElement = new XElement("Tag", new XElement("Id", tag.Id), new XElement("Label", tag.Label));
                defsElement.Add(tagElement);
            }
            var peaksElement = new XElement("Peaks");
            foreach (var peak in chromPeakFeatures.SelectMany(Flatten)) {
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
            var doc = new XElement("PeakSpotTags", defsElement, peaksElement);
            doc.Save(tagfile);
        }

        public static List<ChromatogramPeakFeature> LoadChromatogramPeakFeatures(string file) {
            var peaks = MessagePackHandler.LoadFromFile<List<ChromatogramPeakFeature>>(file);
            var tagfile = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + "_tags.xml");
            if (File.Exists(tagfile)) {
                var doc = XElement.Load(tagfile);
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
                foreach (var peak in peaks.SelectMany(Flatten)) {
                    if (dic.TryGetValue(peak.MasterPeakID, out var tags)) {
                        peak.TagCollection.Set(tags);
                    }
                }
            }
            return peaks;
        }

        private static IEnumerable<ChromatogramPeakFeature> Flatten(ChromatogramPeakFeature peak) {
            return (peak.DriftChromFeatures?.SelectMany(Flatten) ?? Enumerable.Empty<ChromatogramPeakFeature>()).Prepend(peak);
        }
    }
}
