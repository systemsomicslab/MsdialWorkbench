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
        public static void Search() {

        }

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

            foreach (var feature in features.Where(n => n.IsMsmsContained)) {
                var featureStatus = feature.FeatureFilterStatus;
                featureStatus.IsFragmentExistFiltered = false;
                if (isIonMobility) {
                    var isFragmentExist = false;
                    foreach (var drift in feature.DriftChromFeatures.Where(n => n.IsMsmsContained)) {
                        var msdec = decLoader.LoadMSDecResult(drift.MasterPeakID);
                        var spectrum = msdec.Spectrum;
                        if (spectrum.Count == 0) continue;

                        var precursorMz = feature.PrecursorMz;
                        var basePeakIntensity = spectrum.Max(n => n.Intensity);
                        var dFeatureStatus = drift.FeatureFilterStatus;
                        dFeatureStatus.IsFragmentExistFiltered = false;
                        
                        if (param.AndOrAtFragmentSearch == Common.Enum.AndOr.OR)
                            dFeatureStatus.IsFragmentExistFiltered = isFragmentFound(spectrum, queries, precursorMz, basePeakIntensity);
                        else
                            dFeatureStatus.IsFragmentExistFiltered = isAllFragmentExist(spectrum, queries, precursorMz, basePeakIntensity);
                        if (dFeatureStatus.IsFragmentExistFiltered) isFragmentExist = true;
                    }
                    featureStatus.IsFragmentExistFiltered = isFragmentExist;
                }
                else {
                    var msdec = decLoader.LoadMSDecResult(feature.PeakID);
                    var spectrum = msdec.Spectrum;
                    if (spectrum.Count == 0) continue;

                    var precursorMz = feature.PrecursorMz;
                    var basePeakIntensity = spectrum.Max(n => n.Intensity);
                    featureStatus.IsFragmentExistFiltered = false;

                    if (param.AndOrAtFragmentSearch == Common.Enum.AndOr.OR)
                        featureStatus.IsFragmentExistFiltered = isFragmentFound(spectrum, queries, precursorMz, basePeakIntensity);
                    else
                        featureStatus.IsFragmentExistFiltered = isAllFragmentExist(spectrum, queries, precursorMz, basePeakIntensity);
                }
            }

        }

        public static void SearchingFragmentQueries(List<AlignmentSpotProperty> features, MSDecLoader decLoader, ParameterBase param) {

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

            foreach (var feature in features.Where(n => n.IsMsmsAssigned)) {
                var featureStatus = feature.FeatureFilterStatus;
                featureStatus.IsFragmentExistFiltered = false;
                if (isIonMobility) {
                    var isFragmentExist = false;
                    foreach (var drift in feature.AlignmentDriftSpotFeatures.Where(n => n.IsMsmsAssigned)) {
                        var msdec = decLoader.LoadMSDecResult(drift.MasterAlignmentID);
                        var spectrum = msdec.Spectrum;
                        if (spectrum.Count == 0) continue;

                        var precursorMz = feature.MassCenter;
                        var basePeakIntensity = spectrum.Max(n => n.Intensity);
                        var dFeatureStatus = drift.FeatureFilterStatus;
                        dFeatureStatus.IsFragmentExistFiltered = false;

                        if (param.AndOrAtFragmentSearch == Common.Enum.AndOr.OR)
                            dFeatureStatus.IsFragmentExistFiltered = isFragmentFound(spectrum, queries, precursorMz, basePeakIntensity);
                        else
                            dFeatureStatus.IsFragmentExistFiltered = isAllFragmentExist(spectrum, queries, precursorMz, basePeakIntensity);
                        if (dFeatureStatus.IsFragmentExistFiltered) isFragmentExist = true;
                    }
                    featureStatus.IsFragmentExistFiltered = isFragmentExist;
                }
                else {
                    var msdec = decLoader.LoadMSDecResult(feature.AlignmentID);
                    var spectrum = msdec.Spectrum;
                    if (spectrum.Count == 0) continue;

                    var precursorMz = feature.MassCenter;
                    var basePeakIntensity = spectrum.Max(n => n.Intensity);
                    featureStatus.IsFragmentExistFiltered = false;

                    if (param.AndOrAtFragmentSearch == Common.Enum.AndOr.OR)
                        featureStatus.IsFragmentExistFiltered = isFragmentFound(spectrum, queries, precursorMz, basePeakIntensity);
                    else
                        featureStatus.IsFragmentExistFiltered = isAllFragmentExist(spectrum, queries, precursorMz, basePeakIntensity);
                }
            }
        }

        private static bool isAllFragmentExist(List<SpectrumPeak> peaks, List<PeakFeatureSearchValue> queries, double precursorMz, double basePeakIntensity) {
            var isAllQueryFound = true;
            foreach (var query in queries) {
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

                foreach (var query in queries) {
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
