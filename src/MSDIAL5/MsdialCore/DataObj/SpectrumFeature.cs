using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.MSDec;

namespace CompMs.MsdialCore.DataObj
{
    public sealed class AnnotatedMSDecResult {
        public AnnotatedMSDecResult(MSDecResult mSDecResult, MsScanMatchResultContainer matchResults, IMoleculeProperty molecule, double quantMass) {
            MSDecResult = mSDecResult;
            MatchResults = matchResults;
            QuantMass = quantMass;
            Molecule = molecule;
        }

        public AnnotatedMSDecResult(MSDecResult mSDecResult, MsScanMatchResultContainer matchResults, double quantMass) {
            MSDecResult = mSDecResult;
            MatchResults = matchResults;
            QuantMass = quantMass; 
            Molecule = null;
        }

        public IMoleculeProperty Molecule { get; }
        public double QuantMass { get; }
        public MSDecResult MSDecResult { get; }
        public MsScanMatchResultContainer MatchResults { get; }
        public bool IsReferenceMatched(IMatchResultEvaluator<MsScanMatchResult> evaluator) => MatchResults.IsReferenceMatched(evaluator);
    }

    public sealed class QuantifiedChromatogramPeak {
        public QuantifiedChromatogramPeak(IChromatogramPeakFeature peakFeature, int mS1RawSpectrumIdTop, int mS1RawSpectrumIdLeft, int mS1RawSpectrumIdRight, ChromatogramPeakShape peakShape) {
            PeakFeature = peakFeature;
            MS1RawSpectrumIdTop = mS1RawSpectrumIdTop;
            MS1RawSpectrumIdLeft = mS1RawSpectrumIdLeft;
            MS1RawSpectrumIdRight = mS1RawSpectrumIdRight;
            PeakShape = peakShape;
        }

        public IChromatogramPeakFeature PeakFeature { get; }
        public int MS1RawSpectrumIdTop { get; set; }
        public int MS1RawSpectrumIdLeft { get; set; }
        public int MS1RawSpectrumIdRight { get; set; }
        public ChromatogramPeakShape PeakShape { get; }
    }

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
