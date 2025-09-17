using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Algorithm {
    public sealed class FragmentSearcher {
        private FragmentSearcher() { }
        
        public static void Search(List<ChromatogramPeakFeature> features, MSDecLoader decLoader, ParameterBase param) {
            if (features is null || features.Count == 0) {
                throw new ArgumentNullException(nameof(features));
            }

            if (decLoader is null) {
                throw new ArgumentNullException(nameof(decLoader));
            }

            if (param is null) {
                throw new ArgumentNullException(nameof(param));
            }

            var isIonMobility = features[0].DriftChromFeatures.IsEmptyOrNull() ? false : true;
            var queries = param.FragmentSearchSettingValues;
            if (queries.IsEmptyOrNull()) {
                return;
            }

            var maxIntensity = features.Max(n => n.PeakHeightTop);
            var maxIntensityOnDrift = isIonMobility ? features.SelectMany(n => n.DriftChromFeatures).Max(n => n.PeakHeightTop) : 0.0;
            var isAllQueriesFocusOnMS1 = queries.Count() == queries.Where(n => n.PeakFeatureQueryLevel == PeakFeatureQueryLevel.MS1).Count();
            foreach (var feature in features) {
                var featureStatus = feature.FeatureFilterStatus;
                featureStatus.IsFragmentExistFiltered = false;

                if (isIonMobility) {
                    var isFragmentExist = false;
                    foreach (var drift in feature.DriftChromFeatures.Where(n => n.IsMsmsContained)) {
                        var dFeatureStatus = drift.FeatureFilterStatus;
                        var isPeakDetectedByQuerySearch = isPeakDetectedByBasicMsPropertyQuery(drift, maxIntensityOnDrift, queries);
                        if (!isPeakDetectedByQuerySearch) continue;
                        if (isAllQueriesFocusOnMS1) {
                            dFeatureStatus.IsFragmentExistFiltered = true;
                            isFragmentExist = true;
                            continue;
                        }

                        var msdec = decLoader.LoadMSDecResult(drift.MasterPeakID);
                        var spectrum = msdec.Spectrum;
                        if (spectrum.Count == 0) continue;

                        var precursorMz = feature.PrecursorMz;
                        var basePeakIntensity = spectrum.Max(n => n.Intensity);
                        if (param.AndOrAtFragmentSearch == Common.Enum.AndOr.OR)
                            dFeatureStatus.IsFragmentExistFiltered = isFragmentFound(spectrum, queries, precursorMz, basePeakIntensity);
                        else
                            dFeatureStatus.IsFragmentExistFiltered = isAllFragmentExist(spectrum, queries, precursorMz, basePeakIntensity);
                        if (dFeatureStatus.IsFragmentExistFiltered) isFragmentExist = true;
                    }
                    featureStatus.IsFragmentExistFiltered = isFragmentExist;
                }
                else {
                    var isPeakDetectedByQuerySearch = isPeakDetectedByBasicMsPropertyQuery(feature, maxIntensity, queries);
                    if (!isPeakDetectedByQuerySearch) continue;
                    if (isAllQueriesFocusOnMS1) {
                        featureStatus.IsFragmentExistFiltered = true;
                        continue;
                    }

                    var msdec = decLoader.LoadMSDecResult(feature.PeakID);
                    var spectrum = msdec.Spectrum;
                    if (spectrum.Count == 0) continue;

                    var precursorMz = feature.PrecursorMz;
                    var basePeakIntensity = spectrum.Max(n => n.Intensity);

                    if (param.AndOrAtFragmentSearch == Common.Enum.AndOr.OR) {
                        featureStatus.IsFragmentExistFiltered = isFragmentFound(spectrum, queries, precursorMz, basePeakIntensity);
                    }
                    else {
                        featureStatus.IsFragmentExistFiltered = isAllFragmentExist(spectrum, queries, precursorMz, basePeakIntensity);
                    }
                }
            }

        }

        private static bool isPeakDetectedByBasicMsPropertyQuery(ChromatogramPeakFeature feature, double maxIntensity, List<PeakFeatureSearchValue> queries) {
            return IsMs1FeatureMatched(feature, maxIntensity, queries);
        }

        private static bool isPeakDetectedByBasicMsPropertyQuery(AlignmentSpotProperty feature, double maxIntensity, List<PeakFeatureSearchValue> queries) {
            return IsMs1SpotMatched(feature, maxIntensity, queries);
        }

        public static void Search(List<AlignmentSpotProperty> features, MSDecLoader decLoader, ParameterBase param) {

            if (features is null || features.Count == 0) {
                throw new ArgumentNullException(nameof(features));
            }

            if (decLoader is null) {
                throw new ArgumentNullException(nameof(decLoader));
            }

            if (param is null) {
                throw new ArgumentNullException(nameof(param));
            }

            var isIonMobility = features[0].AlignmentDriftSpotFeatures.IsEmptyOrNull() ? false : true;
            var queries = param.FragmentSearchSettingValues;
            if (queries.IsEmptyOrNull()) {
                return;
            }

            var maxIntensity = features.Max(n => n.HeightAverage);
            var maxIntensityOnDrift = isIonMobility ? features.SelectMany(n => n.AlignmentDriftSpotFeatures).Max(n => n.HeightAverage) : 0.0;
            var isAllQueriesFocusOnMS1 = queries.Count() == queries.Where(n => n.PeakFeatureQueryLevel == PeakFeatureQueryLevel.MS1).Count();

            foreach (var feature in features) {
                var featureStatus = feature.FeatureFilterStatus;
                
                featureStatus.IsFragmentExistFiltered = false;
                if (isIonMobility) {
                    var isFragmentExist = false;
                    foreach (var drift in feature.AlignmentDriftSpotFeatures) {
                        var dFeatureStatus = drift.FeatureFilterStatus;
                        dFeatureStatus.IsFragmentExistFiltered = false;
                        var isPeakDetectedByQuerySearch = isPeakDetectedByBasicMsPropertyQuery(drift, maxIntensityOnDrift, queries);
                        if (!isPeakDetectedByQuerySearch) continue;
                        if (isAllQueriesFocusOnMS1) {
                            dFeatureStatus.IsFragmentExistFiltered = true;
                            isFragmentExist = true;
                            continue;
                        }
                        var msdec = decLoader.LoadMSDecResult(drift.MasterAlignmentID);
                        var spectrum = msdec.Spectrum;
                        if (spectrum.Count == 0) continue;

                        var precursorMz = feature.MassCenter;
                        var basePeakIntensity = spectrum.Max(n => n.Intensity);
                        

                        if (param.AndOrAtFragmentSearch == Common.Enum.AndOr.OR)
                            dFeatureStatus.IsFragmentExistFiltered = isFragmentFound(spectrum, queries, precursorMz, basePeakIntensity);
                        else
                            dFeatureStatus.IsFragmentExistFiltered = isAllFragmentExist(spectrum, queries, precursorMz, basePeakIntensity);
                        if (dFeatureStatus.IsFragmentExistFiltered) isFragmentExist = true;
                    }
                    featureStatus.IsFragmentExistFiltered = isFragmentExist;
                }
                else {
                    var isPeakDetectedByQuerySearch = isPeakDetectedByBasicMsPropertyQuery(feature, maxIntensity, queries);
                    if (!isPeakDetectedByQuerySearch) continue;
                    if (isAllQueriesFocusOnMS1) {
                        featureStatus.IsFragmentExistFiltered = true;
                        continue;
                    }

                    var msdec = decLoader.LoadMSDecResult(feature.AlignmentID);
                    var spectrum = msdec.Spectrum;
                    if (spectrum.Count == 0) continue;

                    var precursorMz = feature.MassCenter;
                    var basePeakIntensity = spectrum.Max(n => n.Intensity);
                    //featureStatus.IsFragmentExistFiltered = false;

                    if (param.AndOrAtFragmentSearch == Common.Enum.AndOr.OR)
                        featureStatus.IsFragmentExistFiltered = isFragmentFound(spectrum, queries, precursorMz, basePeakIntensity);
                    else
                        featureStatus.IsFragmentExistFiltered = isAllFragmentExist(spectrum, queries, precursorMz, basePeakIntensity);
                }
            }
        }

        private static bool EvaluateCondition(string? conditionType, double featureValue, double queryValue) {
            return conditionType?.ToUpperInvariant() switch {
                "GREATERTHAN" => featureValue > queryValue,
                "LESSTHAN" => featureValue < queryValue,
                "EQUALS" => Math.Abs(featureValue - queryValue) < 0.01,
                _ => featureValue >= queryValue // default behavior (BETWEEN or unspecified)
            };
        }

        private static bool isAllFragmentExist(List<SpectrumPeak> peaks, List<PeakFeatureSearchValue> queries, double precursorMz, double basePeakIntensity) {
            foreach (var query in queries.Where(n => n.PeakFeatureQueryLevel == PeakFeatureQueryLevel.MS2)) {
                var isQueryFound = peaks.Any(peak => {
                    var productIon = peak.Mass;
                    var neutralLoss = precursorMz - productIon;
                    var relativeIntensity = peak.Intensity / basePeakIntensity * 100.0;
                    var targetMass = query.PeakFeatureSearchType == PeakFeatureSearchType.ProductIon ? productIon : neutralLoss;
                    return Math.Abs(targetMass - query.Mass) <= query.MassTolerance &&
                           EvaluateCondition(query.ConditionType, relativeIntensity, query.RelativeIntensityCutoff);
                });
                if (!isQueryFound) return false;
            }
            return true;
        }

        private static bool isFragmentFound(List<SpectrumPeak> peaks, List<PeakFeatureSearchValue> queries, double precursorMz, double basePeakIntensity) {
            return peaks.Any(peak => {
                var productIon = peak.Mass;
                var neutralLoss = precursorMz - productIon;
                var relativeIntensity = peak.Intensity / basePeakIntensity * 100.0;

                return queries.Where(q => q.PeakFeatureQueryLevel == PeakFeatureQueryLevel.MS2).Any(query => {
                    var targetMass = query.PeakFeatureSearchType == PeakFeatureSearchType.ProductIon ? productIon : neutralLoss;
                    return Math.Abs(targetMass - query.Mass) <= query.MassTolerance &&
                           EvaluateCondition(query.ConditionType, relativeIntensity, query.RelativeIntensityCutoff);
                });
            });
        }

        public static bool IsMs1FeatureMatched(ChromatogramPeakFeature feature, double maxIntensity, List<PeakFeatureSearchValue> queries) {

            var ms1Queries = queries.Where(q => q.PeakFeatureQueryLevel == PeakFeatureQueryLevel.MS1).ToList();
            if (ms1Queries.Count == 0) return true; // 

            var featureRt = feature.ChromXsTop.RT.Value;
            var featureDt = feature.ChromXsTop.HasDrift() ? feature.ChromXsTop.Drift.Value : 0.0;
            var featureMz = feature.Mass;
            var featureHeight = feature.PeakHeightTop;
            var relativeHeight = featureHeight / maxIntensity * 100.0;
            var charge = feature.PeakCharacter?.Charge ?? 0;

            foreach (var query in ms1Queries) {
                if (!EvaluateCondition(query.ConditionType, featureRt, query.TimeMin)) continue;
                if (!EvaluateCondition(query.ConditionType, featureDt, query.MobilityMin)) continue;
                if (!EvaluateCondition(query.ConditionType, featureHeight, query.AbsoluteIntensityCutoff)) continue;
                if (!EvaluateCondition(query.ConditionType, relativeHeight, query.RelativeIntensityCutoff)) continue;
                if (!EvaluateCondition(query.ConditionType, charge, query.Charge)) continue;
                if (query.Mass > 0 && query.MassTolerance > 0 && Math.Abs(featureMz - query.Mass) > query.MassTolerance) continue;
                return true;
            }
            return false;
        }

        public static bool IsMs1SpotMatched(AlignmentSpotProperty feature, double maxIntensity, List<PeakFeatureSearchValue> queries) {

            var ms1Queries = queries.Where(q => q.PeakFeatureQueryLevel == PeakFeatureQueryLevel.MS1).ToList();
            if (ms1Queries.Count == 0) return true; 

            var featureRt = feature.TimesCenter.RT.Value;
            var featureDt = feature.TimesCenter.HasDrift() ? feature.TimesCenter.Drift.Value : 0.0;
            var featureMz = feature.MassCenter;
            var featureHeight = feature.HeightAverage;
            var relativeHeight = featureHeight / maxIntensity * 100.0;
            var charge = feature.PeakCharacter?.Charge ?? 0;

            foreach (var query in ms1Queries) {
                if (!EvaluateCondition(query.ConditionType, featureRt, query.TimeMin)) continue;
                if (!EvaluateCondition(query.ConditionType, featureDt, query.MobilityMin)) continue;
                if (!EvaluateCondition(query.ConditionType, featureHeight, query.AbsoluteIntensityCutoff)) continue;
                if (!EvaluateCondition(query.ConditionType, relativeHeight, query.RelativeIntensityCutoff)) continue;
                if (!EvaluateCondition(query.ConditionType, charge, query.Charge)) continue;
                if (query.Mass > 0 && query.MassTolerance > 0 && Math.Abs(featureMz - query.Mass) > query.MassTolerance) continue;
                return true;
            }
            return false;
        }
    }
}
