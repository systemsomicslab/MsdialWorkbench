using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Lipidomics;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Normalize
{
    internal class NormalizationTargetSpot
    {
        private readonly AlignmentSpotProperty _spot;

        public NormalizationTargetSpot(AlignmentSpotProperty spot) {
            _spot = spot ?? throw new ArgumentNullException(nameof(spot));
        }

        public AlignmentSpotProperty Spot => _spot;

        public bool IsNormalized() {
            return _spot.InternalStandardAlignmentID >= 0;
        }

        public string GetAnnotatedLipidClass(IMatchResultEvaluator<MsScanMatchResult> evaluator, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer, HashSet<string> lipidClasses) {
            if (!_spot.IsReferenceMatched(evaluator)) {
                return "Unknown";
            }

            var reference = refer.Refer(_spot.MatchResults.Representative);
            var lipidClass = reference?.CompoundClass;
            if (string.IsNullOrEmpty(lipidClass) && lipidClasses.Contains(reference?.Ontology)) {
                lipidClass = reference.Ontology;
            }

            if (!string.IsNullOrEmpty(lipidClass)) {
                var lbmClass = LipidomicsConverter.ConvertMsdialClassDefinitionToLbmClassEnumVS2(lipidClass);
                lipidClass = LipidomicsConverter.ConvertLbmClassEnumToMsdialClassDefinitionVS2(lbmClass);
            }

            return string.IsNullOrEmpty(lipidClass) ? "Unknown" : lipidClass;
        }

        public void NormalizeByInternalStandard(AlignmentSpotProperty isSpot, StandardCompound compound, IonAbundanceUnit unit) {
            _spot.InternalStandardAlignmentID = compound.PeakID;
            _spot.IonAbundanceUnit = unit;
            var targetProps = _spot.AlignedPeakProperties;
            var isProps = isSpot.AlignedPeakProperties;
            for (int i = 0; i < isProps.Count; i++) {
                var isProp = isProps[i];
                var targetProp = targetProps[i];

                var baseIntensity = isProp.PeakHeightTop > 0 ? isProp.PeakHeightTop : 1.0;
                var targetIntensity = targetProp.PeakHeightTop;
                targetProp.NormalizedPeakHeight = compound.Concentration *  targetIntensity / baseIntensity;
                var baseArea = isProp.PeakAreaAboveBaseline > 0 ? isProp.PeakAreaAboveBaseline : 1.0;
                var targetArea = targetProp.PeakAreaAboveBaseline;
                targetProp.NormalizedPeakAreaAboveBaseline = compound.Concentration *  targetArea / baseArea;
                var baseAreaZero = isProp.PeakAreaAboveZero > 0 ? isProp.PeakAreaAboveZero : 1.0;
                var targetAreaZero = targetProp.PeakAreaAboveZero;
                targetProp.NormalizedPeakAreaAboveZero = compound.Concentration *  targetAreaZero / baseAreaZero;
            }
        }

        public void FillNormalizeProperties() {
            _spot.IonAbundanceUnit = IonAbundanceUnit.Height;
            foreach (var peak in _spot.AlignedPeakProperties) {
                peak.NormalizedPeakHeight = peak.PeakHeightTop;
                peak.NormalizedPeakAreaAboveBaseline = peak.PeakAreaAboveBaseline;
                peak.NormalizedPeakAreaAboveZero = peak.PeakAreaAboveZero;
            }
        }
    }
}
