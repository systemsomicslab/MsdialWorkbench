using CompMs.Common.MessagePack;
using System.Collections.Generic;
using System.IO;

namespace CompMs.MsdialCore.DataObj
{
    public sealed class SpectrumFeatureCollection
    {
        private readonly List<SpectrumFeature> _spectrumFeatures;

        public SpectrumFeatureCollection(List<SpectrumFeature> spectrumFeatures) {
            _spectrumFeatures = spectrumFeatures;
        }

        public void Save(Stream stream) {
            LargeListMessagePack.Serialize(stream, _spectrumFeatures);
        }

        public static SpectrumFeatureCollection Load(Stream stream) {
            return new SpectrumFeatureCollection(LargeListMessagePack.Deserialize<SpectrumFeature>(stream));
        }
    }
}
