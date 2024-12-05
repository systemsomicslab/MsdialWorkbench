using CompMs.Common.MessagePack;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CompMs.MsdialCore.DataObj
{
    public sealed class SpectrumFeatureCollection
    {
        private readonly List<SpectrumFeature> _spectrumFeatures;

        public SpectrumFeatureCollection(List<SpectrumFeature> spectrumFeatures) {
            _spectrumFeatures = spectrumFeatures;
            Items = _spectrumFeatures.AsReadOnly();
        }

        public ReadOnlyCollection<SpectrumFeature> Items { get; }

        public void Save(Stream stream) {
            LargeListMessagePack.Serialize(stream, _spectrumFeatures);
            if (stream is FileStream fs) {
                var file = fs.Name;
                var tagfile = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + "_tags.xml");
                var defsElement = new XElement("Definitions");
                foreach (var tag in PeakSpotTag.AllTypes()) {
                    var tagElement = new XElement("Tag", new XElement("Id", tag.Id), new XElement("Label", tag.Label));
                    defsElement.Add(tagElement);
                }
                var peaksElement = new XElement("Peaks");
                foreach (var feature in _spectrumFeatures) {
                    if (!feature.TagCollection.Any()) {
                        continue;
                    }
                    var peakElement = new XElement("Peak");
                    peakElement.SetAttributeValue("Id", feature.AnnotatedMSDecResult.MSDecResult.ScanID);
                    foreach (var tag in feature.TagCollection.Selected) {
                        peakElement.Add(new XElement("Tag", tag.Id));
                    }
                    peaksElement.Add(peakElement);
                }
                var doc = new XElement("PeakSpotTags", defsElement, peaksElement);
                doc.Save(tagfile);
            }
        }

        public void Save(Stream stream, Stream tagStream) {
            LargeListMessagePack.Serialize(stream, _spectrumFeatures);
            var defsElement = new XElement("Definitions");
            foreach (var tag in PeakSpotTag.AllTypes()) {
                var tagElement = new XElement("Tag", new XElement("Id", tag.Id), new XElement("Label", tag.Label));
                defsElement.Add(tagElement);
            }
            var peaksElement = new XElement("Peaks");
            foreach (var feature in _spectrumFeatures) {
                if (!feature.TagCollection.Any()) {
                    continue;
                }
                var peakElement = new XElement("Peak");
                peakElement.SetAttributeValue("Id", feature.AnnotatedMSDecResult.MSDecResult.ScanID);
                foreach (var tag in feature.TagCollection.Selected) {
                    peakElement.Add(new XElement("Tag", tag.Id));
                }
                peaksElement.Add(peakElement);
            }
            var doc = new XElement("PeakSpotTags", defsElement, peaksElement);
            doc.Save(tagStream);
        }

        public static SpectrumFeatureCollection Load(Stream stream) {
            var features = new SpectrumFeatureCollection(LargeListMessagePack.Deserialize<SpectrumFeature>(stream));
            if (stream is FileStream fs) {
                var file = fs.Name;
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
                    foreach (var feature in features._spectrumFeatures) {
                        if (dic.TryGetValue(feature.AnnotatedMSDecResult.MSDecResult.ScanID, out var tags)) {
                            feature.TagCollection.Set(tags);
                        }
                    }
                }
            }
            return features;
        }

        public static SpectrumFeatureCollection Load(Stream stream, Stream tagStream) {
            var features = new SpectrumFeatureCollection(LargeListMessagePack.Deserialize<SpectrumFeature>(stream));
            if (!(tagStream is null)) {
                var doc = XElement.Load(tagStream);
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
                foreach (var feature in features._spectrumFeatures) {
                    if (dic.TryGetValue(feature.AnnotatedMSDecResult.MSDecResult.ScanID, out var tags)) {
                        feature.TagCollection.Set(tags);
                    }
                }
            }
            return features;
        }
    }
}
