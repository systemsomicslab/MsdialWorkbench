using CompMs.Common.Enum;
using CompMs.Common.Lipidomics;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Normalize
{
    public sealed class SplashNormalization
    {
        public static void Normalize(
            IReadOnlyList<AlignmentSpotProperty> globalSpots,
            IMatchResultRefer refer,
            IReadOnlyList<StandardCompound> splashLipids,
            IonAbundanceUnit unit) {


            // initialize
            InitializeNormalizationProcess(globalSpots);
            
            //normalization to 1 for IS spot

            // first try to normalize IS peaks
            foreach (var compound in splashLipids) {
                NormalizeInternalStandard(globalSpots[compound.PeakID], compound, unit);
            }

            var lipidClasses = new HashSet<string>(LipidomicsConverter.GetLipidClasses());
            var stdCompoundsTable = splashLipids.Where(lipid => lipid.TargetClass != "Any others").ToLookup(compound => compound.TargetClass);
            var otherCompound = splashLipids.FirstOrDefault(lipid => lipid.TargetClass == "Any others");
            foreach (var spot in globalSpots) {

                // first try to normalize except for "any others" property
                if (IsNormalized(spot)) {
                    continue;
                }
                var lipidclass = GetAnnotatedLipidClass(spot, refer, lipidClasses);
                var stdCompound = stdCompoundsTable[lipidclass].FirstOrDefault();
                if (stdCompound != null) {
                    NormalizeByInternalStandard(spot, globalSpots[stdCompound.PeakID], stdCompound, unit);
                    continue;
                }

                // second, normalized by any other tagged compounds
                if (otherCompound != null) {
                    NormalizeByInternalStandard(spot, globalSpots[otherCompound.PeakID], otherCompound, unit);
                    continue;
                }

                // finalization
                SetRawHeightData(spot);
            }
        }

        private static void SetRawHeightData(AlignmentSpotProperty spot) {
            spot.IonAbundanceUnit = IonAbundanceUnit.Height;
            foreach (var targetProp in spot.AlignedPeakProperties) {
                targetProp.NormalizedPeakHeight = targetProp.PeakHeightTop;
                targetProp.NormalizedPeakAreaAboveBaseline = targetProp.PeakAreaAboveBaseline;
                targetProp.NormalizedPeakAreaAboveZero = targetProp.PeakAreaAboveZero;
            }
        }

        private static void NormalizeInternalStandard(AlignmentSpotProperty spot, StandardCompound compound, IonAbundanceUnit unit) {
            spot.IonAbundanceUnit = unit;
            spot.InternalStandardAlignmentID = compound.PeakID;
            foreach (var prop in spot.AlignedPeakProperties) {
                prop.NormalizedPeakHeight = compound.Concentration;
                prop.NormalizedPeakAreaAboveBaseline = compound.Concentration;
                prop.NormalizedPeakAreaAboveZero = compound.Concentration;
            }
        }

        private static bool IsNormalized(AlignmentSpotProperty spot) {
            return spot.InternalStandardAlignmentID >= 0;
        }

        private static void NormalizeByInternalStandard(AlignmentSpotProperty spot, AlignmentSpotProperty isSpot, StandardCompound compound, IonAbundanceUnit unit) {
            spot.InternalStandardAlignmentID = compound.PeakID;
            spot.IonAbundanceUnit = unit;
            var targetProps = spot.AlignedPeakProperties;
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

        private static void InitializeNormalizationProcess(IReadOnlyList<AlignmentSpotProperty> spots) {
            foreach (var spot in spots) {
                spot.InternalStandardAlignmentID = -1;
                foreach (var prop in spot.AlignedPeakProperties) {
                    prop.NormalizedPeakHeight = -1d;
                    prop.NormalizedPeakAreaAboveBaseline = -1d;
                    prop.NormalizedPeakAreaAboveZero = -1d;
                }
            }
        }

        private static string GetAnnotatedLipidClass(AlignmentSpotProperty spot, IMatchResultRefer refer, HashSet<string> lipidClasses) {

            if (string.IsNullOrEmpty(spot.Name) || spot.IsAnnotationSuggested) {
                return "Unknown";
            }

            var reference = refer.Refer(spot.MatchResults.Representative);
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
    }
}
