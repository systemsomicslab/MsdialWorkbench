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

        private static bool isPeakDetectedByBasicMsPropertyQuery(ChromatogramPeakFeature feature, double maxIntensity,
            List<PeakFeatureSearchValue> queries) {
            var featureRt = feature.ChromXsTop.RT.Value;
            var featureDt = feature.ChromXsTop.HasDrift() ? feature.ChromXsTop.Drift.Value : 0.0;
            var featureHeight = feature.PeakHeightTop;
            var featureRelativeHeight = featureHeight / maxIntensity * 100.0;
            var featureMz = feature.Mass;
            var flag = false;
            foreach (var query in queries) {
                if (query.TimeMin > 0 && featureRt > 0 && featureRt < query.TimeMin) continue;
                if (query.TimeMax > 0 && featureRt > 0 && featureRt > query.TimeMax) continue;
                if (query.MobilityMin > 0 && featureDt > 0 && featureDt < query.MobilityMin) continue;
                if (query.MobilityMax > 0 && featureDt > 0 && featureDt > query.MobilityMax) continue;

                if (query.PeakFeatureQueryLevel == PeakFeatureQueryLevel.MS1) {
                    if (query.AbsoluteIntensityCutoff > 0 && featureHeight > 0 && query.AbsoluteIntensityCutoff > featureHeight) continue;
                    if (query.RelativeIntensityCutoff > 0 && featureRelativeHeight > 0 && featureRelativeHeight < query.RelativeIntensityCutoff) continue;
                    if (query.Mass > 0 && query.MassTolerance > 0 &&
                        Math.Abs(featureMz - query.Mass) > query.MassTolerance) continue;
                }
                flag = true;
                break;
            }
            return flag;
        }

        private static bool isPeakDetectedByAllBasicMsPropertyQuery(ChromatogramPeakFeature feature, double maxIntensity,
            List<PeakFeatureSearchValue> queries) {
            var featureRt = feature.ChromXsTop.RT.Value;
            var featureDt = feature.ChromXsTop.HasDrift() ? feature.ChromXsTop.Drift.Value : 0.0;
            var featureHeight = feature.PeakHeightTop;
            var featureRelativeHeight = featureHeight / maxIntensity * 100.0;
            var featureMz = feature.Mass;
            var flag = true;
            foreach (var query in queries) {
                if (query.TimeMin > 0 && featureRt > 0 && featureRt < query.TimeMin) {
                    flag = false; break;
                }
                if (query.TimeMax > 0 && featureRt > 0 && featureRt > query.TimeMax) {
                    flag = false; break;
                }
                if (query.MobilityMin > 0 && featureDt > 0 && featureDt < query.MobilityMin) {
                    flag = false; break;
                }
                if (query.MobilityMax > 0 && featureDt > 0 && featureDt > query.MobilityMax) {
                    flag = false; break;
                }
                if (query.AbsoluteIntensityCutoff > 0 && featureHeight > 0 && query.AbsoluteIntensityCutoff > featureHeight) {
                    flag = false; break;
                }
                if (query.RelativeIntensityCutoff > 0 && featureRelativeHeight > 0 && featureRelativeHeight < query.RelativeIntensityCutoff) {
                    flag = false; break;
                }

                if (query.PeakFeatureQueryLevel == PeakFeatureQueryLevel.MS1 &&
                    query.Mass > 0 && query.MassTolerance > 0 &&
                    Math.Abs(featureMz - query.Mass) > query.MassTolerance) {
                    flag = false; break;
                }
            }
            return flag;
        }


        private static bool isPeakDetectedByBasicMsPropertyQuery(AlignmentSpotProperty feature, double maxIntensity,
            List<PeakFeatureSearchValue> queries) {
            var featureRt = feature.TimesCenter.RT.Value;
            var featureDt = feature.TimesCenter.HasDrift() ? feature.TimesCenter.Drift.Value : 0.0;
            var featureHeight = feature.HeightAverage;
            var featureRelativeHeight = featureHeight / maxIntensity * 100.0;
            var featureMz = feature.MassCenter;
            var flag = false;
            foreach (var query in queries) {
                if (query.TimeMin > 0 && featureRt > 0 && featureRt < query.TimeMin) continue;
                if (query.TimeMax > 0 && featureRt > 0 && featureRt > query.TimeMax) continue;
                if (query.MobilityMin > 0 && featureDt > 0 && featureDt < query.MobilityMin) continue;
                if (query.MobilityMax > 0 && featureDt > 0 && featureDt > query.MobilityMax) continue;
                if (query.PeakFeatureQueryLevel == PeakFeatureQueryLevel.MS1) {
                    if (query.AbsoluteIntensityCutoff > 0 && featureHeight > 0 && query.AbsoluteIntensityCutoff > featureHeight) continue;
                    if (query.RelativeIntensityCutoff > 0 && featureRelativeHeight > 0 && featureRelativeHeight < query.RelativeIntensityCutoff) continue;
                    if (query.Mass > 0 && query.MassTolerance > 0 &&
                        Math.Abs(featureMz - query.Mass) > query.MassTolerance) continue;
                }
                flag = true;
                break;
            }
            return flag;
        }

        private static bool isPeakDetectedByAllBasicMsPropertyQuery(AlignmentSpotProperty feature, double maxIntensity,
            List<PeakFeatureSearchValue> queries) {
            var featureRt = feature.TimesCenter.RT.Value;
            var featureDt = feature.TimesCenter.HasDrift() ? feature.TimesCenter.Drift.Value : 0.0;
            var featureHeight = feature.HeightAverage;
            var featureRelativeHeight = featureHeight / maxIntensity * 100.0;
            var featureMz = feature.MassCenter;
            var flag = true;
            foreach (var query in queries) {
                if (query.TimeMin > 0 && featureRt > 0 && featureRt < query.TimeMin) {
                    flag = false; break;
                }
                if (query.TimeMax > 0 && featureRt > 0 && featureRt > query.TimeMax) {
                    flag = false; break;
                }
                if (query.MobilityMin > 0 && featureDt > 0 && featureDt < query.MobilityMin) {
                    flag = false; break;
                }
                if (query.MobilityMax > 0 && featureDt > 0 && featureDt > query.MobilityMax) {
                    flag = false; break;
                }
                if (query.AbsoluteIntensityCutoff > 0 && featureHeight > 0 && query.AbsoluteIntensityCutoff > featureHeight) {
                    flag = false; break;
                }
                if (query.RelativeIntensityCutoff > 0 && featureRelativeHeight > 0 && featureRelativeHeight < query.RelativeIntensityCutoff) {
                    flag = false; break;
                }

                if (query.PeakFeatureQueryLevel == PeakFeatureQueryLevel.MS1 &&
                    query.Mass > 0 && query.MassTolerance > 0 &&
                    Math.Abs(featureMz - query.Mass) > query.MassTolerance) {
                    flag = false; break;
                }
            }
            return flag;
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

        private static bool isAllFragmentExist(List<SpectrumPeak> peaks, List<PeakFeatureSearchValue> queries, double precursorMz, double basePeakIntensity) {
            var isAllQueryFound = true;
            foreach (var query in queries.Where(n => n.PeakFeatureQueryLevel == PeakFeatureQueryLevel.MS2)) {
                var qMass = query.Mass;
                var qIntensity = query.RelativeIntensityCutoff;
                var qType = query.PeakFeatureSearchType;

                var isQueryFound = false;
                foreach (var peak in peaks) {
                    var productIon = peak.Mass;
                    var neutralLoss = precursorMz - productIon;
                    var relativeIntensity = peak.Intensity / basePeakIntensity * 100.0;

                    if (qType == PeakFeatureSearchType.ProductIon) {
                        if (Math.Abs(qMass - productIon) < query.MassTolerance && relativeIntensity > qIntensity) {
                            isQueryFound = true;
                            break;
                        }
                    }
                    else {
                        if (Math.Abs(qMass - neutralLoss) < query.MassTolerance && relativeIntensity > qIntensity) {
                            isQueryFound = true;
                            break;
                        }
                    }
                }
                if (isQueryFound == false) {
                    isAllQueryFound = false;
                    break;
                }
            }

            if (isAllQueryFound)
                return true;
            else
                return false;
        }

        private static bool isFragmentFound(List<SpectrumPeak> peaks, List<PeakFeatureSearchValue> queries, double precursorMz, double basePeakIntensity) {
            var isFragmentExist = false;
            foreach (var peak in peaks) {
                var productIon = peak.Mass;
                var neutralLoss = precursorMz - productIon;
                var relativeIntensity = peak.Intensity / basePeakIntensity * 100.0;

                foreach (var query in queries.Where(n => n.PeakFeatureQueryLevel == PeakFeatureQueryLevel.MS2)) {
                    if (query.PeakFeatureSearchType == PeakFeatureSearchType.ProductIon) {
                        var qProduct = query.Mass;
                        var qIntensity = query.RelativeIntensityCutoff;

                        if (Math.Abs(qProduct - productIon) < query.MassTolerance && relativeIntensity > qIntensity) {
                            isFragmentExist = true;
                            break;
                        }
                    }
                    else {
                        var qLoss = query.Mass;
                        var qIntensity = query.RelativeIntensityCutoff;

                        if (Math.Abs(qLoss - neutralLoss) < query.MassTolerance && relativeIntensity > qIntensity) {
                            isFragmentExist = true;
                            break;
                        }
                    }
                }
                if (isFragmentExist == true) break;
            }
            return isFragmentExist;
        }
    }
}
