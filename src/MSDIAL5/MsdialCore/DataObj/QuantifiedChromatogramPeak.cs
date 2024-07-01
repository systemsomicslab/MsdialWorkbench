using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.Common.MessagePack;
using MessagePack;
using System.IO;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public sealed class QuantifiedChromatogramPeak {
        [SerializationConstructor]
        public QuantifiedChromatogramPeak(IChromatogramPeakFeature peakFeature, ChromatogramPeakShape peakShape, int mS1RawSpectrumIdTop, int mS1RawSpectrumIdLeft, int mS1RawSpectrumIdRight) {
            PeakFeature = peakFeature;
            MS1RawSpectrumIdTop = mS1RawSpectrumIdTop;
            MS1RawSpectrumIdLeft = mS1RawSpectrumIdLeft;
            MS1RawSpectrumIdRight = mS1RawSpectrumIdRight;
            PeakShape = peakShape;
        }

        public QuantifiedChromatogramPeak(IChromatogramPeakFeature peakFeature, ChromatogramPeakShape peakShape, PeakDetectionResult peakDetectionResult, ExtractedIonChromatogram chromatogram)
            : this(peakFeature, peakShape, chromatogram.Id(peakDetectionResult.ScanNumAtPeakTop), chromatogram.Id(peakDetectionResult.ScanNumAtLeftPeakEdge), chromatogram.Id(peakDetectionResult.ScanNumAtRightPeakEdge)) {
            
        }

        [Key("PeakFeature")]
        [MessagePackFormatter(typeof(ChromatogramPeakFeatureInterfaceFormatter))]
        public IChromatogramPeakFeature PeakFeature { get; }
        [Key("PeakShape")]
        public ChromatogramPeakShape PeakShape { get; }
        [Key("MS1RawSpectrumIdTop")]
        public int MS1RawSpectrumIdTop { get; }
        [Key("MS1RawSpectrumIdLeft")]
        public int MS1RawSpectrumIdLeft { get; }
        [Key("MS1RawSpectrumIdRight")]
        public int MS1RawSpectrumIdRight { get; }

        public void Save(Stream stream) {
            MessagePackDefaultHandler.SaveToStream(this, stream);
        }

        public static QuantifiedChromatogramPeak Load(Stream stream) {
            return MessagePackDefaultHandler.LoadFromStream<QuantifiedChromatogramPeak>(stream);
        }
    }
}
