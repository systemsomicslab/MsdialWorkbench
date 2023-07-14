using CompMs.Common.MessagePack;
using MessagePack;
using System.IO;
namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public sealed class SpectrumFeature {
        public SpectrumFeature(AnnotatedMSDecResult annotatedMSDecResult, QuantifiedChromatogramPeak quantifiedChromatogramPeak) {
            AnnotatedMSDecResult = annotatedMSDecResult;
            QuantifiedChromatogramPeak = quantifiedChromatogramPeak;
        }

        [SerializationConstructor]
        public SpectrumFeature(AnnotatedMSDecResult annotatedMSDecResult, QuantifiedChromatogramPeak quantifiedChromatogramPeak, FeatureFilterStatus featureFilterStatus) {
            AnnotatedMSDecResult = annotatedMSDecResult;
            QuantifiedChromatogramPeak = quantifiedChromatogramPeak;
            FeatureFilterStatus = featureFilterStatus;
        }

        [Key("AnnotatedMSDecResult")]
        public AnnotatedMSDecResult AnnotatedMSDecResult { get; }

        [Key("QuantifiedChromatogramPeak")]
        public QuantifiedChromatogramPeak QuantifiedChromatogramPeak { get; }

        [Key("FeatureFilterStatus")]
        public FeatureFilterStatus FeatureFilterStatus { get; } = new FeatureFilterStatus();
        [Key("Comment")]
        public string Comment { get; set; } = string.Empty;
        [IgnoreMember]
        public PeakSpotTagCollection TagCollection { get; } = new PeakSpotTagCollection();

        public void Save(Stream stream) {
            MessagePackDefaultHandler.SaveToStream(this, stream);
        }

        public static SpectrumFeature Load(Stream stream) {
            return MessagePackDefaultHandler.LoadFromStream<SpectrumFeature>(stream);
        }
    }
}
