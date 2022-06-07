using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Lipidomics;
using CompMs.Common.Mathematics.Basic;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Normalize
{
    public static class Normalization
    {
        public static void None(
            IReadOnlyList<AlignmentSpotProperty> globalSpots) {

            var targets = new NormalizationTargetSpotCollection(globalSpots);
            // initialize
            targets.Initialize();
            foreach (var target in targets.TargetSpots) {
                // finalization
                target.FillNormalizeProperties();
            }
        }

        public static void InternalStandardNormalize(
            IReadOnlyList<AlignmentSpotProperty> globalSpots,
            IonAbundanceUnit unit) {
            var targets = new NormalizationTargetSpotCollection(globalSpots);
            // initialize
            targets.Initialize();
            foreach (var target in targets.TargetSpots) {
                if (target.Spot.InternalStandardAlignmentID >= 0 && target.Spot.InternalStandardAlignmentID < targets.TargetSpots.Count) {
                    NormalizeByInternalStandard(target.Spot, targets.FindSpot(target.Spot.InternalStandardAlignmentID), unit);
                }
                else {
                    target.FillNormalizeProperties();
                }
            }
        }

        public static void NormalizeByMaxPeak(
            IReadOnlyList<AlignmentSpotProperty> globalSpots) {
            var ticValues = new List<double>();
            var filecount = globalSpots[0].AlignedPeakProperties.Count;
            for (int i = 0; i < filecount; i++) {
                var objs = globalSpots.Select(n => n.AlignedPeakProperties[i]).ToList();
                var maxHeight = objs.Max(n => n.PeakHeightTop);
                var maxAreaZero = objs.Max(n => n.PeakAreaAboveZero);
                var maxAreaBase = objs.Max(n => n.PeakAreaAboveBaseline);
                foreach (var obj in objs) {
                    obj.NormalizedPeakHeight = obj.PeakHeightTop / maxHeight * 100;
                    obj.NormalizedPeakAreaAboveZero = obj.PeakAreaAboveZero / maxAreaZero * 100;
                    obj.PeakAreaAboveBaseline = obj.PeakAreaAboveBaseline / maxAreaBase * 100;
                }
            }
            foreach (var spot in globalSpots) spot.IonAbundanceUnit = IonAbundanceUnit.NormalizedByMaxPeakOnTIC;
        }

        public static void NormalizeByMaxPeakOnNamedPeaks(
            IReadOnlyList<AlignmentSpotProperty> globalSpots,
            IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            var ticValues = new List<double>();
            var filecount = globalSpots[0].AlignedPeakProperties.Count;
            for (int i = 0; i < filecount; i++) {
                var objs = globalSpots.Where(n => n.IsReferenceMatched(evaluator)).Select(n => n.AlignedPeakProperties[i]).ToList();
                var maxHeight = objs.Max(n => n.PeakHeightTop);
                var maxAreaZero = objs.Max(n => n.PeakAreaAboveZero);
                var maxAreaBase = objs.Max(n => n.PeakAreaAboveBaseline);

                var allObjs = globalSpots.Select(n => n.AlignedPeakProperties[i]).ToList();
                foreach (var obj in allObjs) {
                    obj.NormalizedPeakHeight = obj.PeakHeightTop / maxHeight * 100;
                    obj.NormalizedPeakAreaAboveZero = obj.PeakAreaAboveZero / maxAreaZero * 100;
                    obj.PeakAreaAboveBaseline = obj.PeakAreaAboveBaseline / maxAreaBase * 100;
                }
            }
            foreach (var spot in globalSpots) spot.IonAbundanceUnit = IonAbundanceUnit.NormalizedByMaxPeakOnNamedPeaks;
        }

        public static void LowessNormalize(
            IReadOnlyList<AnalysisFileBean> files,
            IReadOnlyList<AlignmentSpotProperty> globalSpots,
            IonAbundanceUnit unit) {
            var targets = new NormalizationTargetSpotCollection(globalSpots);
            // initialize
            targets.Initialize();
            foreach (var target in targets.TargetSpots) {
                target.FillNormalizeProperties();
            }
            var optSpan = LowessSpanTune(files, targets.Spots);
            LowessNormalize(files, targets.Spots, optSpan);
        }

        public static void ISNormThenByLowessNormalize(
            IReadOnlyList<AnalysisFileBean> files,
            IReadOnlyList<AlignmentSpotProperty> globalSpots,
            IonAbundanceUnit unit) {
            var targets = new NormalizationTargetSpotCollection(globalSpots);
            InternalStandardNormalize(targets.Spots, unit);
            var optSpan = LowessSpanTune(files, targets.Spots);
            LowessNormalize(files, targets.Spots, optSpan);
        }

        public static void SplashNormalize(
            IReadOnlyList<AlignmentSpotProperty> globalSpots,
            IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer,
            IReadOnlyList<StandardCompound> splashLipids,
            IonAbundanceUnit unit,
            IMatchResultEvaluator<MsScanMatchResult> evaluator) {

            var targets = new NormalizationTargetSpotCollection(globalSpots);
            var compounds = new StandardCompoundSet(splashLipids);

            // initialize
            targets.Initialize();
            
            //normalization to 1 for IS spot

            // first try to normalize IS peaks
            foreach (var compound in compounds.Compounds) {
                targets.NormalizeInternalStandardSpot(compound, unit);
            }

            var lipidClasses = new HashSet<string>(LipidomicsConverter.GetLipidClasses());
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

        private static void NormalizeByInternalStandard(AlignmentSpotProperty spot,
            AlignmentSpotProperty isSpot, IonAbundanceUnit unit) {
            spot.InternalStandardAlignmentID = isSpot.MasterAlignmentID;
            spot.IonAbundanceUnit = unit;
            var targetProps = spot.AlignedPeakProperties;
            var isProps = isSpot.AlignedPeakProperties;
            var aveIsHeight = isProps.Average(n => n.PeakHeightTop);
            var aveIsAreaZero = isProps.Average(n => n.PeakAreaAboveZero);
            var aveIsAreaBase = isProps.Average(n => n.PeakAreaAboveBaseline);
            for (int i = 0; i < isProps.Count; i++) {
                var isProp = isProps[i];
                var targetProp = targetProps[i];

                var baseIntensity = isProp.PeakHeightTop > 0 ? isProp.PeakHeightTop : 1.0;
                var targetIntensity = targetProp.PeakHeightTop;
                targetProp.NormalizedPeakHeight = aveIsHeight * targetIntensity / baseIntensity;
                var baseArea = isProp.PeakAreaAboveBaseline > 0 ? isProp.PeakAreaAboveBaseline : 1.0;
                var targetArea = targetProp.PeakAreaAboveBaseline;
                targetProp.NormalizedPeakAreaAboveBaseline = aveIsAreaBase * targetArea / baseArea;
                var baseAreaZero = isProp.PeakAreaAboveZero > 0 ? isProp.PeakAreaAboveZero : 1.0;
                var targetAreaZero = targetProp.PeakAreaAboveZero;
                targetProp.NormalizedPeakAreaAboveZero = aveIsAreaZero * targetAreaZero / baseAreaZero;
            }
        }

        public static double GetMinimumLowessOptSize(IReadOnlyList<AnalysisFileBean> files) {
            var count = files.Count(n => n.AnalysisFileIncluded && n.AnalysisFileType == AnalysisFileType.QC);
            var minOptSize = SmootherMathematics.GetMinimumLowessSpan(count);
            return minOptSize;
        }

        public static void LowessNormalize(
            IReadOnlyList<AnalysisFileBean> files,
            IReadOnlyList<AlignmentSpotProperty> globalSpots,
            double lowessSpan) {

            var batchDict = files.GroupBy(item => item.AnalysisBatch).ToDictionary(grp => grp.Key, grp => grp.ToList());
            var medQcHeights = new List<double>();
            var medQcAreaZeros = new List<double>();
            var medQcAreaBases = new List<double>();
            foreach (var spot in globalSpots) {
                var qcHeights = new List<double>();
                var qcAreaZeros = new List<double>();
                var qcAreaBases = new List<double>();
                var targetProps = spot.AlignedPeakProperties;
                foreach (var prop in targetProps) {
                    var fileProp = files[prop.FileID];
                    if (fileProp.AnalysisFileType == AnalysisFileType.QC && fileProp.AnalysisFileIncluded) {
                        qcHeights.Add(prop.PeakHeightTop);
                        qcAreaZeros.Add(prop.PeakAreaAboveZero);
                        qcAreaBases.Add(prop.PeakAreaAboveBaseline);
                    }
                }
                medQcHeights.Add(BasicMathematics.Median(qcHeights.ToArray()));
                medQcAreaZeros.Add(BasicMathematics.Median(qcAreaZeros.ToArray()));
                medQcAreaBases.Add(BasicMathematics.Median(qcAreaBases.ToArray()));
            }

            foreach (var eachBatch in batchDict) {
                var analysisFileBeanCollectionPerBatch = eachBatch.Value;
                var index = 0;
                foreach (var spot in globalSpots) {
                    spot.IonAbundanceUnit = IonAbundanceUnit.NormalizedByQcPeakHeight;
                    LowessNormalize(spot, analysisFileBeanCollectionPerBatch, medQcHeights[index], lowessSpan, "height");
                    LowessNormalize(spot, analysisFileBeanCollectionPerBatch, medQcAreaZeros[index], lowessSpan, "areazero");
                    LowessNormalize(spot, analysisFileBeanCollectionPerBatch, medQcAreaBases[index], lowessSpan, "areabase");
                    index++;
                }
            }
        }

        private static void LowessNormalize(
            AlignmentSpotProperty spot, 
            List<AnalysisFileBean> files, 
            double medQCValue, 
            double lowessSpan,
            string type) {
            var qcList = new List<double[]>();
            var variableProps = spot.AlignedPeakProperties;
            foreach (var tFile in files) {
                if (tFile.AnalysisFileType == AnalysisFileType.QC && tFile.AnalysisFileIncluded) {

                    var addedValue = type.ToLower() == "height" ? variableProps[tFile.AnalysisFileId].NormalizedPeakHeight :
                        type.ToLower() == "areazero" ? variableProps[tFile.AnalysisFileId].NormalizedPeakAreaAboveZero :
                        variableProps[tFile.AnalysisFileId].NormalizedPeakAreaAboveBaseline;
                    qcList.Add(new double[] { tFile.AnalysisFileAnalyticalOrder, addedValue });
                }
            }

            qcList = qcList.OrderBy(n => n[0]).ToList();

            double[] xQcArray = new double[qcList.Count];
            double[] yQcArray = new double[qcList.Count];

            for (int j = 0; j < qcList.Count; j++) { xQcArray[j] = qcList[j][0]; yQcArray[j] = qcList[j][1]; }

            double[] yLoessPreArray = SmootherMathematics.Lowess(xQcArray, yQcArray, lowessSpan, 3);
            double[] ySplineDeviArray = SmootherMathematics.Spline(xQcArray, yLoessPreArray, double.MaxValue, double.MaxValue);
            double baseQcValue = yQcArray[0];
            double fittedValue = 0;

            foreach (var tFile in files) {
                if (!tFile.AnalysisFileIncluded) continue;
                fittedValue = SmootherMathematics.Splint(xQcArray, yLoessPreArray, ySplineDeviArray, tFile.AnalysisFileAnalyticalOrder);
                fittedValue = medQCValue > 0 ? fittedValue / medQCValue : 1;
                if (fittedValue <= 0) fittedValue = 1;
                if (fittedValue > 0) {
                    if (type.ToLower() == "height") {
                        variableProps[tFile.AnalysisFileId].NormalizedPeakHeight = variableProps[tFile.AnalysisFileId].NormalizedPeakHeight / (float)fittedValue;
                    }
                    else if (type.ToLower() == "areazero") {
                        variableProps[tFile.AnalysisFileId].NormalizedPeakAreaAboveZero = variableProps[tFile.AnalysisFileId].NormalizedPeakAreaAboveZero / (float)fittedValue;
                    }
                    else {
                        variableProps[tFile.AnalysisFileId].NormalizedPeakAreaAboveBaseline = variableProps[tFile.AnalysisFileId].NormalizedPeakAreaAboveBaseline / (float)fittedValue;
                    }
                }
                else
                    if (type.ToLower() == "height") {
                    variableProps[tFile.AnalysisFileId].NormalizedPeakHeight = 0.0;
                }
                else if (type.ToLower() == "areazero") {
                    variableProps[tFile.AnalysisFileId].NormalizedPeakAreaAboveZero = 0.0;
                }
                else {
                    variableProps[tFile.AnalysisFileId].NormalizedPeakAreaAboveBaseline = 0.0;
                }
            }

        }

        public static double LowessSpanTune(IReadOnlyList<AnalysisFileBean> files, IReadOnlyList<AlignmentSpotProperty> globalSpots) {

            var fileCollection = new AnalysisFileCollection(files);
            var minSpan = fileCollection.MinimumLowessSpan();
            var optSpanList = new List<double>();
            foreach (var eachBatch in files.GroupBy(item => item.AnalysisBatch)) {
                var analysisFileBeanCollectionPerBatch = eachBatch
                    .Where(bean => bean.AnalysisFileType == AnalysisFileType.QC && bean.AnalysisFileIncluded)
                    .OrderBy(bean => bean.AnalysisFileAnalyticalOrder)
                    .ToArray();
                var xQcArray = analysisFileBeanCollectionPerBatch.Select(bean => (double)bean.AnalysisFileAnalyticalOrder).ToArray();
                foreach (var spot in globalSpots) {
                    var yQcArray = analysisFileBeanCollectionPerBatch.Select(bean => spot.AlignedPeakProperties[bean.AnalysisFileId].PeakHeightTop).ToArray();

                    var recoSpan = SmootherMathematics.GetOptimalLowessSpanByCrossValidation(xQcArray, yQcArray, minSpan, 0.05, 3, 7);
                    optSpanList.Add(recoSpan);
                }
            }
            var optSpan = BasicMathematics.Mean(optSpanList);
            return Math.Round(optSpan, 2);
        }
    }
}
