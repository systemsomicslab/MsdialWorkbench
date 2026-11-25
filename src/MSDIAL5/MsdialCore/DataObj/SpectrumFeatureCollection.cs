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
                CreateTagDocument().Save(tagfile);
            }
        }

        public void Save(Stream stream, Stream tagStream) {
            LargeListMessagePack.Serialize(stream, _spectrumFeatures);
            CreateTagDocument().Save(tagStream);
        }

        private XElement CreateTagDocument() {
            return PeakSpotTagXml.Create(_spectrumFeatures, feature => feature.AnnotatedMSDecResult.MSDecResult.ScanID, feature => feature.TagCollection);
        }

        private static void ApplyTagDocument(XElement doc, SpectrumFeatureCollection features) {
            PeakSpotTagXml.Apply(doc, features._spectrumFeatures, feature => feature.AnnotatedMSDecResult.MSDecResult.ScanID, feature => feature.TagCollection);
        }

        public static SpectrumFeatureCollection Load(Stream stream) {
            var features = new SpectrumFeatureCollection(LargeListMessagePack.Deserialize<SpectrumFeature>(stream));
            if (stream is FileStream fs) {
                var file = fs.Name;
                var tagfile = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + "_tags.xml");
                if (File.Exists(tagfile)) {
                    ApplyTagDocument(XElement.Load(tagfile), features);
                }
            }
            return features;
        }

        public static SpectrumFeatureCollection Load(Stream stream, Stream tagStream) {
            var features = new SpectrumFeatureCollection(LargeListMessagePack.Deserialize<SpectrumFeature>(stream));
            if (!(tagStream is null)) {
                ApplyTagDocument(XElement.Load(tagStream), features);
            }
            return features;
        }
    }
}
