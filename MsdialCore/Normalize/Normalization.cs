using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Lipidomics;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Normalize
{
    public static class Normalization
    {
        public static void None(IReadOnlyList<AlignmentSpotProperty> globalSpots) {
            new NoneNormalize().Normalize(globalSpots);
        }

        public static void InternalStandardNormalize(IReadOnlyList<INormalizationTarget> globalSpots, IonAbundanceUnit unit) {
            new InternalStandardNormalize().Normalize(globalSpots, unit);
        }

        public static void LowessNormalize(IReadOnlyList<AnalysisFileBean> files, IReadOnlyList<AlignmentSpotProperty> globalSpots, IonAbundanceUnit unit) {
            new LowessNormalize().Normalize(files, globalSpots, unit);
        }

        public static void ISNormThenByLowessNormalize(IReadOnlyList<AnalysisFileBean> files, IReadOnlyList<AlignmentSpotProperty> globalSpots, IonAbundanceUnit unit) {
            new InternalStandardLowessNormalize().Normalize(files, globalSpots, unit);
        }

        public static void NormalizeByMaxPeak(IReadOnlyList<AlignmentSpotProperty> globalSpots) {
            new TicNormalize().Normalize(globalSpots);
        }

        public static void NormalizeByMaxPeakOnNamedPeaks(IReadOnlyList<AlignmentSpotProperty> globalSpots, IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            new MTicNormalize().Normalize(globalSpots, evaluator);
        }

        public static void SplashNormalize(IReadOnlyList<AlignmentSpotProperty> globalSpots, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer, IReadOnlyList<StandardCompound> splashLipids, IonAbundanceUnit unit, IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            new SplashNormalize().Normalize(globalSpots, refer, splashLipids, unit, evaluator);
        }
    }

    internal sealed class NoneNormalize {
        public void Normalize(IReadOnlyList<INormalizationTarget> spots) {
            var targets = new NormalizationTargetSpotCollection(spots);
            // initialize
            targets.Initialize();
            foreach (var target in targets.TargetSpots) {
                // finalization
                target.FillNormalizeProperties();
            }
        }
    }

    internal sealed class InternalStandardNormalize {
        public void Normalize(IReadOnlyList<INormalizationTarget> globalSpots, IonAbundanceUnit unit) {
            var targets = new NormalizationTargetSpotCollection(globalSpots);
            // initialize
            targets.Initialize();
            foreach (var target in targets.TargetSpots) {
                var isSpot = targets.FindSpot(target.Target.InternalStandardId);
                if (isSpot is null) {
                    target.FillNormalizeProperties();
                }
                else {
                    NormalizeByInternalStandard(target.Target, isSpot, unit);
                }
            }
        }

        private void NormalizeByInternalStandard(INormalizationTarget spot, INormalizationTarget isSpot, IonAbundanceUnit unit) {
            spot.InternalStandardId = isSpot.Id;
            spot.IonAbundanceUnit = unit;
            var targetProps = spot.Values;
            var isProps = isSpot.Values;
            var aveIsHeight = isProps.Average(n => n.PeakHeight);
            var aveIsAreaZero = isProps.Average(n => n.PeakAreaAboveZero);
            var aveIsAreaBase = isProps.Average(n => n.PeakAreaAboveBaseline);
            for (int i = 0; i < isProps.Count; i++) {
                var isProp = isProps[i];
                var targetProp = targetProps[i];

                var baseIntensity = isProp.PeakHeight > 0 ? isProp.PeakHeight : 1.0;
                var targetIntensity = targetProp.PeakHeight;
                targetProp.NormalizedPeakHeight = aveIsHeight * targetIntensity / baseIntensity;
                var baseArea = isProp.PeakAreaAboveBaseline > 0 ? isProp.PeakAreaAboveBaseline : 1.0;
                var targetArea = targetProp.PeakAreaAboveBaseline;
                targetProp.NormalizedPeakAreaAboveBaseline = aveIsAreaBase * targetArea / baseArea;
                var baseAreaZero = isProp.PeakAreaAboveZero > 0 ? isProp.PeakAreaAboveZero : 1.0;
                var targetAreaZero = targetProp.PeakAreaAboveZero;
                targetProp.NormalizedPeakAreaAboveZero = aveIsAreaZero * targetAreaZero / baseAreaZero;
            }
        }
    }

    internal sealed class LowessNormalize {
        public void Normalize(IReadOnlyList<AnalysisFileBean> files, IReadOnlyList<INormalizationTarget> globalSpots, IonAbundanceUnit unit) {
            var targets = new NormalizationTargetSpotCollection(globalSpots);
            // initialize
            targets.Initialize();
            foreach (var target in targets.TargetSpots) {
                target.FillNormalizeProperties();
            }
            var optSpan = targets.LowessSpanTune(files);
            targets.LowessNormalize(files, optSpan);
        }
    }

    internal sealed class InternalStandardLowessNormalize {
        public void Normalize(IReadOnlyList<AnalysisFileBean> files, IReadOnlyList<INormalizationTarget> globalSpots, IonAbundanceUnit unit) {
            var targets = new NormalizationTargetSpotCollection(globalSpots);
            new InternalStandardNormalize().Normalize(targets.Spots, unit);
            var optSpan = targets.LowessSpanTune(files);
            targets.LowessNormalize(files, optSpan);
        }
    }

    internal sealed class TicNormalize {
        public void Normalize(IReadOnlyList<INormalizationTarget> globalSpots) {
            var ticValues = new List<double>();
            var filecount = globalSpots[0].Values.Count;
            for (int i = 0; i < filecount; i++) {
                var objs = globalSpots.Select(n => n.Values[i]).ToList();
                var maxHeight = objs.Max(n => n.PeakHeight);
                var maxAreaZero = objs.Max(n => n.PeakAreaAboveZero);
                var maxAreaBase = objs.Max(n => n.PeakAreaAboveBaseline);
                foreach (var obj in objs) {
                    obj.NormalizedPeakHeight = obj.PeakHeight / maxHeight * 100;
                    obj.NormalizedPeakAreaAboveZero = obj.PeakAreaAboveZero / maxAreaZero * 100;
                    obj.NormalizedPeakAreaAboveBaseline = obj.PeakAreaAboveBaseline / maxAreaBase * 100;
                }
            }
            foreach (var spot in globalSpots) spot.IonAbundanceUnit = IonAbundanceUnit.NormalizedByMaxPeakOnTIC;
        }
    }

    internal sealed class MTicNormalize {
        public void Normalize(IReadOnlyList<INormalizationTarget> globalSpots, IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            var ticValues = new List<double>();
            var filecount = globalSpots[0].Values.Count;
            for (int i = 0; i < filecount; i++) {
                var objs = globalSpots.Where(n => n.IsReferenceMatched(evaluator)).Select(n => n.Values[i]).ToList();
                var maxHeight = objs.Max(n => n.PeakHeight);
                var maxAreaZero = objs.Max(n => n.PeakAreaAboveZero);
                var maxAreaBase = objs.Max(n => n.PeakAreaAboveBaseline);

                var allObjs = globalSpots.Select(n => n.Values[i]).ToList();
                foreach (var obj in allObjs) {
                    obj.NormalizedPeakHeight = obj.PeakHeight / maxHeight * 100;
                    obj.NormalizedPeakAreaAboveZero = obj.PeakAreaAboveZero / maxAreaZero * 100;
                    obj.NormalizedPeakAreaAboveBaseline = obj.PeakAreaAboveBaseline / maxAreaBase * 100;
                }
            }
            foreach (var spot in globalSpots) spot.IonAbundanceUnit = IonAbundanceUnit.NormalizedByMaxPeakOnNamedPeaks;
        }
    }

    internal sealed class SplashNormalize {
        public void Normalize(IReadOnlyList<INormalizationTarget> globalSpots, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer, IReadOnlyList<StandardCompound> splashLipids, IonAbundanceUnit unit, IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            var targets = new NormalizationTargetSpotCollection(globalSpots);
            var compounds = new StandardCompoundSet(splashLipids);
            var lipidClasses = new HashSet<string>(LipidomicsConverter.GetLipidClasses());

            if (unit == IonAbundanceUnit.NormalizedByInternalStandardPeakHeight) {
                foreach (var target in targets.TargetSpots) {
                    var lipidclass = target.GetAnnotatedLipidClass(evaluator, refer, lipidClasses);
                    var stdCompound = compounds.StdCompoundsTable[lipidclass].FirstOrDefault();
                    if (!(stdCompound is null)) {
                        if (targets.FindSpot(stdCompound.PeakID) is INormalizationTarget std) {
                            target.Target.InternalStandardId = std.Id;
                        }
                    }
                    else if (!(compounds.OtherCompound is null)) {
                        if (targets.FindSpot(compounds.OtherCompound.PeakID) is INormalizationTarget std) {
                            target.Target.InternalStandardId = std.Id;
                        }
                    }

                }
                new InternalStandardNormalize().Normalize(globalSpots, unit);
                return;
            }

            // initialize
            targets.Initialize();
            
            //normalization to 1 for IS spot

            // first try to normalize IS peaks
            foreach (var compound in compounds.Compounds) {
                targets.NormalizeInternalStandardSpot(compound, unit);
            }

            foreach (var target in targets.TargetSpots) {
                // first try to normalize except for "any others" property
                if (target.IsNormalized()) {
                    continue;
                }
                var lipidclass = target.GetAnnotatedLipidClass(evaluator, refer, lipidClasses);
                var stdCompound = compounds.StdCompoundsTable[lipidclass].FirstOrDefault();
                if (stdCompound != null) {
                    target.NormalizeByInternalStandard(targets.FindSpot(stdCompound.PeakID), stdCompound, unit);
                    continue;
                }

                // second, normalized by any other tagged compounds
                if (compounds.OtherCompound != null) {
                    target.NormalizeByInternalStandard(targets.FindSpot(compounds.OtherCompound.PeakID), compounds.OtherCompound, unit);
                    continue;
                }

                // finalization
                target.FillNormalizeProperties();
            }
        }
    }
}
