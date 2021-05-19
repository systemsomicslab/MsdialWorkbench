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

            var lipidClasses = new HashSet<string>(LipidomicsConverter.GetLipidClasses());

            // initialize
            initializeNormalizationProcess(globalSpots);
            
            //normalization to 1 for IS spot
            foreach (var compound in splashLipids) { // first try to normalize IS peaks
                var baseSpot = globalSpots[compound.PeakID];
                baseSpot.IonAbundanceUnit = unit;
                baseSpot.InternalStandardAlignmentID = compound.PeakID;
                var baseProps = baseSpot.AlignedPeakProperties;
                foreach (var prop in baseProps) {
                    prop.NormalizedPeakHeight = compound.Concentration;
                    prop.NormalizedPeakAreaAboveBaseline = compound.Concentration;
                    prop.NormalizedPeakAreaAboveZero = compound.Concentration;
                }
            }

            foreach (var compound in splashLipids.Where(lipid => lipid.TargetClass != "Any others")) { // first try to normalize except for "any others" property
                foreach (var spot in globalSpots) {
                    var lipidclass = string.Empty;
                    lipidclass = GetAnnotatedLipidClass(spot, refer, lipidClasses);
                    var targetProps = spot.AlignedPeakProperties;
                    if (targetProps[0].NormalizedPeakHeight >= 0)
                        continue;

                    if (compound.TargetClass == lipidclass) {
                        var baseSpot = globalSpots[compound.PeakID];
                        var baseProps = baseSpot.AlignedPeakProperties;
                        spot.InternalStandardAlignmentID = compound.PeakID;
                        spot.IonAbundanceUnit = unit;

                        for (int i = 0; i < baseProps.Count; i++) {

                            var baseIntensity = baseProps[i].PeakHeightTop > 0 ? baseProps[i].PeakHeightTop : 1.0;
                            var targetIntensity = targetProps[i].PeakHeightTop;
                            targetProps[i].NormalizedPeakHeight = compound.Concentration *  targetIntensity / baseIntensity;
                            var baseArea = baseProps[i].PeakAreaAboveBaseline > 0 ? baseProps[i].PeakAreaAboveBaseline : 1.0;
                            var targetArea = targetProps[i].PeakAreaAboveBaseline;
                            targetProps[i].NormalizedPeakAreaAboveBaseline = compound.Concentration *  targetArea / baseArea;
                            var baseAreaZero = baseProps[i].PeakAreaAboveZero > 0 ? baseProps[i].PeakAreaAboveZero : 1.0;
                            var targetAreaZero = targetProps[i].PeakAreaAboveZero;
                            targetProps[i].NormalizedPeakAreaAboveZero = compound.Concentration *  targetAreaZero / baseAreaZero;
                        }
                    }
                    else {
                        continue;
                    }
                }
            }

            foreach (var compound in splashLipids.Where(lipid => lipid.TargetClass == "Any others")) { // second, normalized by any other tagged compounds
                foreach (var spot in globalSpots) {
                    var lipidclass = string.Empty;
                    lipidclass = GetAnnotatedLipidClass(spot, refer, lipidClasses);
                    var targetProps = spot.AlignedPeakProperties;
                    if (targetProps[0].NormalizedPeakHeight >= 0)
                        continue;

                    var baseSpot = globalSpots[compound.PeakID];
                    var baseProps = baseSpot.AlignedPeakProperties;
                    spot.InternalStandardAlignmentID = compound.PeakID;
                    spot.IonAbundanceUnit = unit;

                    for (int i = 0; i < baseProps.Count; i++) {

                        var baseIntensity = baseProps[i].PeakHeightTop > 0 ? baseProps[i].PeakHeightTop : 1.0;
                        var targetIntensity = targetProps[i].PeakHeightTop;
                        targetProps[i].NormalizedPeakHeight = compound.Concentration * targetIntensity / baseIntensity;
                        var baseArea = baseProps[i].PeakAreaAboveBaseline > 0 ? baseProps[i].PeakAreaAboveBaseline : 1.0;
                        var targetArea = targetProps[i].PeakAreaAboveBaseline;
                        targetProps[i].NormalizedPeakAreaAboveBaseline = compound.Concentration *  targetArea / baseArea;
                        var baseAreaZero = baseProps[i].PeakAreaAboveZero > 0 ? baseProps[i].PeakAreaAboveZero : 1.0;
                        var targetAreaZero = targetProps[i].PeakAreaAboveZero;
                        targetProps[i].NormalizedPeakAreaAboveZero = compound.Concentration *  targetAreaZero / baseAreaZero;
                    }
                }
            }

            // finalization
            foreach (var spot in globalSpots) {

                var targetProps = spot.AlignedPeakProperties;
                if (targetProps[0].NormalizedPeakHeight >= 0)
                    continue;
                spot.IonAbundanceUnit = IonAbundanceUnit.Height;

                for (int i = 0; i < targetProps.Count; i++) {
                    targetProps[i].NormalizedPeakHeight = targetProps[i].PeakHeightTop;
                    targetProps[i].NormalizedPeakAreaAboveBaseline = targetProps[i].PeakAreaAboveBaseline;
                    targetProps[i].NormalizedPeakAreaAboveZero = targetProps[i].PeakAreaAboveZero;
                }
            }
        }

        private static void initializeNormalizationProcess(IReadOnlyList<AlignmentSpotProperty> spots) {
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
