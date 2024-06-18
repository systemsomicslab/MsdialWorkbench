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
        public static void None(IReadOnlyList<AnalysisFileBean> files, IReadOnlyList<INormalizationTarget> globalSpots, bool applyDilutionFactor) {
            new NoneNormalize(files, applyDilutionFactor).Normalize(globalSpots);
        }

        public static void InternalStandardNormalize(IReadOnlyList<AnalysisFileBean> files, IReadOnlyList<INormalizationTarget> globalSpots, IonAbundanceUnit unit, bool applyDilutionFactor) {
            new InternalStandardNormalize(files, applyDilutionFactor).Normalize(globalSpots, unit);
        }

        public static void LowessNormalize(IReadOnlyList<AnalysisFileBean> files, IReadOnlyList<INormalizationTarget> globalSpots, IonAbundanceUnit unit, bool applyDilutionFactor) {
            new LowessNormalize(files, applyDilutionFactor).Normalize(files, globalSpots, unit);
        }

        public static void ISNormThenByLowessNormalize(IReadOnlyList<AnalysisFileBean> files, IReadOnlyList<INormalizationTarget> globalSpots, IonAbundanceUnit unit, bool applyDilutionFactor) {
            new InternalStandardLowessNormalize(files, applyDilutionFactor).Normalize(files, globalSpots, unit);
        }

        public static void NormalizeByMaxPeak(IReadOnlyList<AnalysisFileBean> files, IReadOnlyList<INormalizationTarget> globalSpots, bool applyDilutionFactor) {
            new TicNormalize(files, applyDilutionFactor).Normalize(globalSpots);
        }

        public static void NormalizeByMaxPeakOnNamedPeaks(IReadOnlyList<AnalysisFileBean> files, IReadOnlyList<INormalizationTarget> globalSpots, IMatchResultEvaluator<MsScanMatchResult> evaluator, bool applyDilutionFactor) {
            new MTicNormalize(files, applyDilutionFactor).Normalize(globalSpots, evaluator);
        }

        public static void SplashNormalize(IReadOnlyList<AnalysisFileBean> files, IReadOnlyList<INormalizationTarget> globalSpots, IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer, IReadOnlyList<StandardCompound> splashLipids, IonAbundanceUnit unit, IMatchResultEvaluator<MsScanMatchResult> evaluator, bool applyDilutionFactor) {
            new SplashNormalize(files, applyDilutionFactor).Normalize(globalSpots, refer, splashLipids, unit, evaluator);
        }
    }

    internal sealed class NoneNormalize {
        private readonly IReadOnlyList<AnalysisFileBean> _files;
        private readonly bool _applyDilutionFactor;

        public NoneNormalize(IReadOnlyList<AnalysisFileBean> files, bool applyDilutionFactor) {
            _files = files;
            _applyDilutionFactor = applyDilutionFactor;
        }

        public void Normalize(IReadOnlyList<INormalizationTarget> spots) {
            var targets = new NormalizationTargetSpotCollection(spots);
            // initialize
            targets.Initialize(initializeIntenralStandardId: true);
            foreach (var target in targets.TargetSpots) {
                // finalization
                target.FillNormalizeProperties();
            }
            if (_applyDilutionFactor) {
                foreach (var target in targets.TargetSpots) {
                    foreach (var (t, f) in target.Target.Values.Zip(_files, (t, f) => (t, f))) {
                        t.NormalizedPeakHeight *= f.DilutionFactor;
                        t.NormalizedPeakAreaAboveBaseline *= f.DilutionFactor;
                        t.NormalizedPeakAreaAboveZero *= f.DilutionFactor;
                    }
                }
            }
        }
    }

    internal sealed class InternalStandardNormalize {
        private readonly IReadOnlyList<AnalysisFileBean> _files;
        private readonly bool _applyDilutionFactor;

        public InternalStandardNormalize(IReadOnlyList<AnalysisFileBean> files, bool applyDilutionFactor) {
            _files = files;
            _applyDilutionFactor = applyDilutionFactor;
        }

        public void Normalize(IReadOnlyList<INormalizationTarget> globalSpots, IonAbundanceUnit unit) {
            var targets = new NormalizationTargetSpotCollection(globalSpots);
            // initialize
            targets.Initialize(initializeIntenralStandardId: false);
            foreach (var target in targets.TargetSpots) {
                var isSpot = targets.FindSpot(target.Target.InternalStandardId);
                if (isSpot is null) {
                    target.FillNormalizeProperties();
                }
                else {
                    NormalizeByInternalStandard(target.Target, isSpot, unit);
                }
            }
            if (_applyDilutionFactor) {
                foreach (var target in targets.TargetSpots) {
                    foreach (var (t, f) in target.Target.Values.Zip(_files, (t, f) => (t, f))) {
                        t.NormalizedPeakHeight *= f.DilutionFactor;
                        t.NormalizedPeakAreaAboveBaseline *= f.DilutionFactor;
                        t.NormalizedPeakAreaAboveZero *= f.DilutionFactor;
                    }
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
        private readonly IReadOnlyList<AnalysisFileBean> _files;
        private readonly bool _applyDilutionFactor;

        public LowessNormalize(IReadOnlyList<AnalysisFileBean> files, bool applyDilutionFactor) {
            _files = files;
            _applyDilutionFactor = applyDilutionFactor;
        }

        public void Normalize(IReadOnlyList<AnalysisFileBean> files, IReadOnlyList<INormalizationTarget> globalSpots, IonAbundanceUnit unit) {
            var targets = new NormalizationTargetSpotCollection(globalSpots);
            // initialize
            targets.Initialize(initializeIntenralStandardId: true);
            foreach (var target in targets.TargetSpots) {
                target.FillNormalizeProperties();
            }
            var optSpan = targets.LowessSpanTune(files);
            targets.LowessNormalize(files, optSpan);
            foreach (var spot in globalSpots) {
                spot.IonAbundanceUnit = unit;
            }
            if (_applyDilutionFactor) {
                foreach (var target in targets.TargetSpots) {
                    foreach (var (t, f) in target.Target.Values.Zip(_files, (t, f) => (t, f))) {
                        t.NormalizedPeakHeight *= f.DilutionFactor;
                        t.NormalizedPeakAreaAboveBaseline *= f.DilutionFactor;
                        t.NormalizedPeakAreaAboveZero *= f.DilutionFactor;
                    }
                }
            }
        }
    }

    internal sealed class InternalStandardLowessNormalize {
        private readonly IReadOnlyList<AnalysisFileBean> _files;
        private readonly bool _applyDilutionFactor;

        public InternalStandardLowessNormalize(IReadOnlyList<AnalysisFileBean> files, bool applyDilutionFactor) {
            _files = files;
            _applyDilutionFactor = applyDilutionFactor;
        }

        public void Normalize(IReadOnlyList<AnalysisFileBean> files, IReadOnlyList<INormalizationTarget> globalSpots, IonAbundanceUnit unit) {
            var targets = new NormalizationTargetSpotCollection(globalSpots);
            new InternalStandardNormalize(_files, _applyDilutionFactor).Normalize(targets.Spots, unit);
            var optSpan = targets.LowessSpanTune(files);
            targets.LowessNormalize(files, optSpan);
            if (_applyDilutionFactor) {
                foreach (var target in targets.TargetSpots) {
                    foreach (var (t, f) in target.Target.Values.Zip(_files, (t, f) => (t, f))) {
                        t.NormalizedPeakHeight *= f.DilutionFactor;
                        t.NormalizedPeakAreaAboveBaseline *= f.DilutionFactor;
                        t.NormalizedPeakAreaAboveZero *= f.DilutionFactor;
                    }
                }
            }
        }
    }

    internal sealed class TicNormalize {
        private readonly IReadOnlyList<AnalysisFileBean> _files;
        private readonly bool _applyDilutionFactor;

        public TicNormalize(IReadOnlyList<AnalysisFileBean> files, bool applyDilutionFactor) {
            _files = files;
            _applyDilutionFactor = applyDilutionFactor;
        }

        public void Normalize(IReadOnlyList<INormalizationTarget> globalSpots) {
            var ticValues = new List<double>();
            var filecount = globalSpots[0].Values.Count;
            for (int i = 0; i < filecount; i++) {
                var objs = globalSpots.Select(n => n.Values[i]).DefaultIfEmpty().ToList();
                var maxHeight = objs.Max(n => n?.PeakHeight) ?? 1d;
                var maxAreaZero = objs.Max(n => n?.PeakAreaAboveZero) ?? 1d;
                var maxAreaBase = objs.Max(n => n?.PeakAreaAboveBaseline) ?? 1d;
                foreach (var obj in objs) {
                    obj.NormalizedPeakHeight = obj.PeakHeight / maxHeight * 100;
                    obj.NormalizedPeakAreaAboveZero = obj.PeakAreaAboveZero / maxAreaZero * 100;
                    obj.NormalizedPeakAreaAboveBaseline = obj.PeakAreaAboveBaseline / maxAreaBase * 100;
                }
            }
            foreach (var spot in globalSpots) spot.IonAbundanceUnit = IonAbundanceUnit.NormalizedByMaxPeakOnTIC;
            if (_applyDilutionFactor) {
                foreach (var spot in globalSpots) {
                    foreach (var (t, f) in spot.Values.Zip(_files, (t, f) => (t, f))) {
                        t.NormalizedPeakHeight *= f.DilutionFactor;
                        t.NormalizedPeakAreaAboveBaseline *= f.DilutionFactor;
                        t.NormalizedPeakAreaAboveZero *= f.DilutionFactor;
                    }
                }
            }
        }
    }

    internal sealed class MTicNormalize {
        private readonly IReadOnlyList<AnalysisFileBean> _files;
        private readonly bool _applyDilutionFactor;

        public MTicNormalize(IReadOnlyList<AnalysisFileBean> files, bool applyDilutionFactor) {
            _files = files;
            _applyDilutionFactor = applyDilutionFactor;
        }

        public void Normalize(IReadOnlyList<INormalizationTarget> globalSpots, IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            var ticValues = new List<double>();
            var filecount = globalSpots[0].Values.Count;
            for (int i = 0; i < filecount; i++) {
                var objs = globalSpots.Where(n => n.IsReferenceMatched(evaluator)).Select(n => n.Values[i]).DefaultIfEmpty().ToList();
                var maxHeight = objs.Max(n => n?.PeakHeight) ?? 1d;
                var maxAreaZero = objs.Max(n => n?.PeakAreaAboveZero) ?? 1d;
                var maxAreaBase = objs.Max(n => n?.PeakAreaAboveBaseline) ?? 1d;

                var allObjs = globalSpots.Select(n => n.Values[i]).ToList();
                foreach (var obj in allObjs) {
                    obj.NormalizedPeakHeight = obj.PeakHeight / maxHeight * 100;
                    obj.NormalizedPeakAreaAboveZero = obj.PeakAreaAboveZero / maxAreaZero * 100;
                    obj.NormalizedPeakAreaAboveBaseline = obj.PeakAreaAboveBaseline / maxAreaBase * 100;
                }
            }
            foreach (var spot in globalSpots) spot.IonAbundanceUnit = IonAbundanceUnit.NormalizedByMaxPeakOnNamedPeaks;
            if (_applyDilutionFactor) {
                foreach (var spot in globalSpots) {
                    foreach (var (t, f) in spot.Values.Zip(_files, (t, f) => (t, f))) {
                        t.NormalizedPeakHeight *= f.DilutionFactor;
                        t.NormalizedPeakAreaAboveBaseline *= f.DilutionFactor;
                        t.NormalizedPeakAreaAboveZero *= f.DilutionFactor;
                    }
                }
            }
        }
    }

    internal sealed class SplashNormalize {
        private readonly IReadOnlyList<AnalysisFileBean> _files;
        private readonly bool _applyDilutionFactor;

        public SplashNormalize(IReadOnlyList<AnalysisFileBean> files, bool applyDilutionFactor) {
            _files = files;
            _applyDilutionFactor = applyDilutionFactor;
        }

        public void Normalize(IReadOnlyList<INormalizationTarget> globalSpots, IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer, IReadOnlyList<StandardCompound> splashLipids, IonAbundanceUnit unit, IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            var targets = new NormalizationTargetSpotCollection(globalSpots);
            var compounds = new StandardCompoundSet(splashLipids);
            var lipidClasses = new HashSet<string>(LipidomicsConverter.GetLipidClasses());

            if (unit == IonAbundanceUnit.NormalizedByInternalStandardPeakHeight) {
                foreach (var target in targets.TargetSpots) {
                    var lipidclass = target.GetAnnotatedLipidClass(evaluator, refer, lipidClasses);
                    var stdCompound = compounds.StdCompoundsTable[lipidclass].FirstOrDefault();
                    target.Target.InternalStandardId = -1;
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
                new InternalStandardNormalize(_files, _applyDilutionFactor).Normalize(globalSpots, unit);
                return;
            }

            // initialize
            targets.Initialize(initializeIntenralStandardId: true);
            
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
            if (_applyDilutionFactor) {
                foreach (var spot in globalSpots) {
                    foreach (var (t, f) in spot.Values.Zip(_files, (t, f) => (t, f))) {
                        t.NormalizedPeakHeight *= f.DilutionFactor;
                        t.NormalizedPeakAreaAboveBaseline *= f.DilutionFactor;
                        t.NormalizedPeakAreaAboveZero *= f.DilutionFactor;
                    }
                }
            }
        }
    }
}
