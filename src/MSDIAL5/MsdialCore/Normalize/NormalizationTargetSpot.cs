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
    internal sealed class NormalizationTargetSpot
    {
        private readonly INormalizationTarget _target;

        public NormalizationTargetSpot(INormalizationTarget target) {
            _target = target ?? throw new ArgumentNullException(nameof(target));
        }

        public INormalizationTarget Target => _target;

        public bool IsNormalized() {
            return _target.InternalStandardId >= 0;
        }

        public string GetAnnotatedLipidClass(IMatchResultEvaluator<MsScanMatchResult> evaluator, IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer, HashSet<string> lipidClasses) {
            if (!_target.IsReferenceMatched(evaluator)) {
                return "Unknown";
            }

            var reference = _target.RetriveReference(refer);
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

        public void NormalizeByInternalStandard(INormalizationTarget isSpot, StandardCompound compound, IonAbundanceUnit unit) {
            _target.InternalStandardId = compound.PeakID;
            _target.IonAbundanceUnit = unit;
            var targetProps = _target.Values;
            var isProps = isSpot.Values;
            for (int i = 0; i < isProps.Count; i++) {
                var isProp = isProps[i];
                var targetProp = targetProps[i];

                var baseIntensity = isProp.PeakHeight > 0 ? isProp.PeakHeight : 1.0;
                var targetIntensity = targetProp.PeakHeight;
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
            _target.IonAbundanceUnit = IonAbundanceUnit.Intensity;
            foreach (var peak in _target.Values) {
                peak.NormalizedPeakHeight = peak.PeakHeight;
                peak.NormalizedPeakAreaAboveBaseline = peak.PeakAreaAboveBaseline;
                peak.NormalizedPeakAreaAboveZero = peak.PeakAreaAboveZero;
            }
        }

        public void LowessNormalize(
            List<AnalysisFileBean> files, 
            double medQCValue, 
            double lowessSpan,
            string type) {
            var qcList = new List<double[]>();
            var variableProps = _target.Values;
            foreach (var tFile in files) {
                if (tFile.AnalysisFileType == AnalysisFileType.QC && tFile.AnalysisFileIncluded) {

                    var addedValue = type.ToLower() == "height" ? variableProps[tFile.AnalysisFileId].NormalizedPeakHeight :
                        type.ToLower() == "areazero" ? variableProps[tFile.AnalysisFileId].NormalizedPeakAreaAboveZero :
                        variableProps[tFile.AnalysisFileId].NormalizedPeakAreaAboveBaseline;
                    qcList.Add(new double[] { tFile.AnalysisFileAnalyticalOrder, addedValue });
                }
            }

            qcList = qcList.OrderBy(n => n[0]).ToList();
            if (qcList.Count == 0) {
                foreach (var tFile in files) {
                    if (!tFile.AnalysisFileIncluded) continue;
                    if (type.ToLower() == "height") {
                        variableProps[tFile.AnalysisFileId].NormalizedPeakHeight = variableProps[tFile.AnalysisFileId].NormalizedPeakHeight;
                    }
                    else if (type.ToLower() == "areazero") {
                        variableProps[tFile.AnalysisFileId].NormalizedPeakAreaAboveZero = variableProps[tFile.AnalysisFileId].NormalizedPeakAreaAboveZero;
                    }
                    else {
                        variableProps[tFile.AnalysisFileId].NormalizedPeakAreaAboveBaseline = variableProps[tFile.AnalysisFileId].NormalizedPeakAreaAboveBaseline;
                    }
                }
            }

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
    }
}
