using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.MSDec;
using MessagePack;
using System.IO;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public sealed class AnnotatedMSDecResult {
        public AnnotatedMSDecResult(MSDecResult mSDecResult, MsScanMatchResultContainer matchResults, MoleculeMsReference reference) {
            MSDecResult = mSDecResult;
            MatchResults = matchResults;
            Molecule = reference;
            QuantMass = reference.QuantMass != 0 ? reference.QuantMass : mSDecResult.ModelPeakMz;
        }

        public AnnotatedMSDecResult(MSDecResult mSDecResult, MsScanMatchResultContainer matchResults) {
            MSDecResult = mSDecResult;
            MatchResults = matchResults;
            QuantMass = mSDecResult.ModelPeakMz;
            Molecule = null;
        }

        [SerializationConstructor]
        public AnnotatedMSDecResult(MSDecResult mSDecResult, MsScanMatchResultContainer matchResults, IMoleculeProperty molecule, double quantMass) {
            MSDecResult = mSDecResult;
            MatchResults = matchResults;
            Molecule = molecule;
            QuantMass = quantMass;
        }

        [Key("MSDecResult")]
        public MSDecResult MSDecResult { get; }
        [Key("MatchResults")]
        public MsScanMatchResultContainer MatchResults { get; }
        [Key("Molecule")]
        public IMoleculeProperty Molecule { get; }
        [Key("QuantMass")]
        public double QuantMass { get; }
        [IgnoreMember]
        public bool IsUnknown => Molecule is null;
        public bool IsReferenceMatched(IMatchResultEvaluator<MsScanMatchResult> evaluator) => MatchResults.IsReferenceMatched(evaluator);

        public void Save(Stream stream) {
            MessagePackDefaultHandler.SaveToStream(this, stream);
        }

        public static AnnotatedMSDecResult Load(Stream stream) {
            return MessagePackDefaultHandler.LoadFromStream<AnnotatedMSDecResult>(stream);
        }
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
        public int MS1RawSpectrumIdTop { get; }
        public int MS1RawSpectrumIdLeft { get; }
        public int MS1RawSpectrumIdRight { get; }
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
