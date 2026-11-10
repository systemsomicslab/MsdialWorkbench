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
            var doc = PeakSpotTagXml.Create(chromPeakFeatures.SelectMany(Flatten), peak => peak.MasterPeakID, peak => peak.TagCollection);
            doc.Save(tagfile);
        }

        public static List<ChromatogramPeakFeature> LoadChromatogramPeakFeatures(string file) {
            var peaks = MessagePackHandler.LoadFromFile<List<ChromatogramPeakFeature>>(file);
            var tagfile = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + "_tags.xml");
            if (File.Exists(tagfile)) {
                var doc = XElement.Load(tagfile);
                PeakSpotTagXml.Apply(doc, peaks.SelectMany(Flatten), peak => peak.MasterPeakID, peak => peak.TagCollection);
            }
            return peaks;
        }

        private static IEnumerable<ChromatogramPeakFeature> Flatten(ChromatogramPeakFeature peak) {
            return (peak.DriftChromFeatures?.SelectMany(Flatten) ?? Enumerable.Empty<ChromatogramPeakFeature>()).Prepend(peak);
        }
    }
}
