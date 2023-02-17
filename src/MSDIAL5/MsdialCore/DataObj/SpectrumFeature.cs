using CompMs.Common.Interfaces;
using CompMs.MsdialCore.MSDec;

namespace CompMs.MsdialCore.DataObj
{
    public sealed class QuantifiedMSDecResult {
        public QuantifiedMSDecResult(AnnotatedMSDecResult annotatedResult, QuantifiedChromatogramPeak quantifiedChromatogramPeak) {
            AnnotatedMSDecResult = annotatedResult;
            QuantifiedChromatogramPeak = quantifiedChromatogramPeak;
        }

        public AnnotatedMSDecResult AnnotatedMSDecResult { get; }
        public double QuantMass => AnnotatedMSDecResult.QuantMass;
        public MSDecResult MSDecResult => AnnotatedMSDecResult.MSDecResult;
        public MsScanMatchResultContainer MatchResults => AnnotatedMSDecResult.MatchResults;
        public IMoleculeProperty Molecule => AnnotatedMSDecResult.Molecule;

        public QuantifiedChromatogramPeak QuantifiedChromatogramPeak { get; }
        public IChromatogramPeakFeature PeakFeature => QuantifiedChromatogramPeak.PeakFeature;
        public int MS1RawSpectrumIdTop => QuantifiedChromatogramPeak.MS1RawSpectrumIdTop;
        public int MS1RawSpectrumIdLeft => QuantifiedChromatogramPeak.MS1RawSpectrumIdLeft;
        public int MS1RawSpectrumIdRight => QuantifiedChromatogramPeak.MS1RawSpectrumIdRight;
        public ChromatogramPeakShape PeakShape => QuantifiedChromatogramPeak.PeakShape;

        public FeatureFilterStatus FeatureFilterStatus { get; } = new FeatureFilterStatus();
        public string Comment { get; set; } = string.Empty;
    }


}
