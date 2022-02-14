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

            // initialize
            InitializeNormalizationProcess(globalSpots);
            foreach (var spot in globalSpots) {
                // finalization
                SetRawHeightData(spot);
            }
        }

        public static void InternalStandardNormalize(
            IReadOnlyList<AlignmentSpotProperty> globalSpots,
            IonAbundanceUnit unit) {
            // initialize
            InitializeNormalizationProcess(globalSpots);
            foreach (var spot in globalSpots) {
                var isID = spot.InternalStandardAlignmentID;
                if (isID >= 0 && isID < globalSpots.Count) {
                    NormalizeByInternalStandard(spot, globalSpots[isID], unit);
                }
                else {
                    SetRawHeightData(spot);
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
            // initialize
            InitializeNormalizationProcess(globalSpots);
            foreach (var spot in globalSpots) {
                SetRawHeightData(spot);
            }
            var optSpan = LowessSpanTune(files, globalSpots);
            LowessNormalize(files, globalSpots, optSpan);
        }

        public static void ISNormThenByLowessNormalize(
            IReadOnlyList<AnalysisFileBean> files,
            IReadOnlyList<AlignmentSpotProperty> globalSpots,
            IonAbundanceUnit unit) {
            InternalStandardNormalize(globalSpots, unit);
            var optSpan = LowessSpanTune(files, globalSpots);
            LowessNormalize(files, globalSpots, optSpan);
        }

        public static void SplashNormalize(
            IReadOnlyList<AlignmentSpotProperty> globalSpots,
            IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer,
            IReadOnlyList<StandardCompound> splashLipids,
            IonAbundanceUnit unit,
            IMatchResultEvaluator<MsScanMatchResult> evaluator) {

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
                var lipidclass = GetAnnotatedLipidClass(spot, refer, lipidClasses, evaluator);
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

        private static void NormalizeByInternalStandard(AlignmentSpotProperty spot, 
            AlignmentSpotProperty isSpot, StandardCompound compound, IonAbundanceUnit unit) {
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

        private static string GetAnnotatedLipidClass(AlignmentSpotProperty spot, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer, HashSet<string> lipidClasses, IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            if (string.IsNullOrEmpty(spot.Name) || spot.IsAnnotationSuggested(evaluator)) {
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

        public static (double, double) GetMinimumLowessOptSize(IReadOnlyList<AnalysisFileBean> files) {
            var count = files.Count(n => n.AnalysisFileIncluded && n.AnalysisFileType == AnalysisFileType.QC);
            var minOptSize = SmootherMathematics.GetMinimumLowessSpan(count);

            return (count, minOptSize);
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

        public static double LowessSpanTune(
            IReadOnlyList<AnalysisFileBean> files,
            IReadOnlyList<AlignmentSpotProperty> globalSpots) {

            var qcList = new List<double[]>();
            var items = GetMinimumLowessOptSize(files);
            var minSpan = items.Item2;
            var optSpanList = new List<double>();

            var batchDict = files.GroupBy(item => item.AnalysisBatch).ToDictionary(grp => grp.Key, grp => grp.ToList());
            foreach (var eachBatch in batchDict) {
                var analysisFileBeanCollectionPerBatch = eachBatch.Value;
                for (int i = 0; i < globalSpots.Count; i++) {
                    qcList = new List<double[]>();
                    for (int j = 0; j < analysisFileBeanCollectionPerBatch.Count; j++) {
                        var alignedProp = analysisFileBeanCollectionPerBatch[j];
                        if (alignedProp.AnalysisFileType == AnalysisFileType.QC && alignedProp.AnalysisFileIncluded) {
                            var variable = globalSpots[i].AlignedPeakProperties[alignedProp.AnalysisFileId].PeakHeightTop;
                            qcList.Add(new double[] { alignedProp.AnalysisFileAnalyticalOrder, variable });
                        }
                    }

                    qcList = qcList.OrderBy(n => n[0]).ToList();

                    var xQcArray = new double[qcList.Count];
                    var yQcArray = new double[qcList.Count];

                    for (int j = 0; j < qcList.Count; j++) { xQcArray[j] = qcList[j][0]; yQcArray[j] = qcList[j][1]; }


                    var recoSpan = SmootherMathematics.GetOptimalLowessSpanByCrossValidation(xQcArray, yQcArray, minSpan, 0.05, 3, 7);
                    optSpanList.Add(recoSpan);
                }
            }
            var optSpanArray = optSpanList.ToArray();
            var optSpan = BasicMathematics.Mean(optSpanArray);

            return Math.Round(optSpan, 2);
        }

    }
}
