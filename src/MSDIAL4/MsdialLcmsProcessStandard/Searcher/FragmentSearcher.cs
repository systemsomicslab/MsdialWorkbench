using Msdial.Lcms.Dataprocess.Algorithm;
using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.Common.Query;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace Msdial.Lcms.Dataprocess.Searcher {
    public sealed class FragmentSearcher {
        private FragmentSearcher() { }

        public static void SearchingFragmentQueries(ObservableCollection<PeakAreaBean> peakSpots, FileStream spotFS, 
            List<long> spotSeekPoints, AnalysisParametersBean param) {

            var queries = param.FragmentSearcherQueries;

            foreach (var spot in peakSpots) {
                if (param.IsIonMobility) {
                    var isFragmentExist = false;
                    spot.IsFragmentQueryExist = false;
                    foreach (var drift in spot.DriftSpots.Where(n => n.Ms2LevelDatapointNumber >= 0)) {
                        var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(spotFS, spotSeekPoints, drift.MasterPeakID);
                        var spectrum = ms2DecResult.MassSpectra;
                        if (spectrum.Count == 0) continue;

                        var precursorMz = spot.AccurateMass;
                        var basePeakIntensity = spectrum.Max(n => n[1]);
                        drift.IsFragmentQueryExist = false;
                        if (!param.IsAndAsFragmentSearcherOption)
                            drift.IsFragmentQueryExist = isFragmentFound(spectrum, queries, precursorMz, basePeakIntensity);
                        else
                            drift.IsFragmentQueryExist = isAllFragmentExist(spectrum, queries, precursorMz, basePeakIntensity);
                        if (drift.IsFragmentQueryExist) isFragmentExist = true;
                    }
                    spot.IsFragmentQueryExist = isFragmentExist;
                }
                else {
                    if (spot.Ms2LevelDatapointNumber < 0) continue;

                    var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(spotFS, spotSeekPoints, spot.PeakID);
                    var spectrum = ms2DecResult.MassSpectra;
                    if (spectrum.Count == 0) continue;

                    var precursorMz = spot.AccurateMass;
                    var basePeakIntensity = spectrum.Max(n => n[1]);
                    spot.IsFragmentQueryExist = false;

                    if (!param.IsAndAsFragmentSearcherOption)
                        spot.IsFragmentQueryExist = isFragmentFound(spectrum, queries, precursorMz, basePeakIntensity);
                    else
                        spot.IsFragmentQueryExist = isAllFragmentExist(spectrum, queries, precursorMz, basePeakIntensity);
                }
            }
        }

      
        public static void SearchingFragmentQueries(ObservableCollection<AlignmentPropertyBean> alignedSpots, FileStream spotFS,
            List<long> spotSeekPoints, AnalysisParametersBean param) {

            var queries = param.FragmentSearcherQueries;
            foreach (var spot in alignedSpots) {
                if (param.IsIonMobility) {
                    var isFragmentExist = false;
                    spot.IsFragmentQueryExist = false;
                    foreach (var drift in spot.AlignedDriftSpots.Where(n => n.MsmsIncluded)) {
                        var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(spotFS, spotSeekPoints, drift.MasterID);
                        var spectrum = ms2DecResult.MassSpectra;
                        if (spectrum.Count == 0) continue;

                        var precursorMz = spot.CentralAccurateMass;
                        var basePeakIntensity = spectrum.Max(n => n[1]);
                        drift.IsFragmentQueryExist = false;
                        if (!param.IsAndAsFragmentSearcherOption)
                            drift.IsFragmentQueryExist = isFragmentFound(spectrum, queries, precursorMz, basePeakIntensity);
                        else
                            drift.IsFragmentQueryExist = isAllFragmentExist(spectrum, queries, precursorMz, basePeakIntensity);
                        if (drift.IsFragmentQueryExist) isFragmentExist = true;
                    }
                    spot.IsFragmentQueryExist = isFragmentExist;
                }
                else {

                    if (!spot.MsmsIncluded) continue;
                    var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(spotFS, spotSeekPoints, spot.AlignmentID);
                    var spectrum = ms2DecResult.MassSpectra;
                    if (spectrum.Count == 0) continue;

                    var precursorMz = spot.CentralAccurateMass;
                    var basePeakIntensity = spectrum.Max(n => n[1]);
                    spot.IsFragmentQueryExist = false;

                    if (!param.IsAndAsFragmentSearcherOption) {
                        spot.IsFragmentQueryExist = isFragmentFound(spectrum, queries, precursorMz, basePeakIntensity);
                        #region
                        //foreach (var specPeak in spectrum) {
                        //    var productIon = specPeak[0];
                        //    var neutralLoss = precursorMz - productIon;
                        //    var relativeIntensity = specPeak[1] / basePeakIntensity * 100.0;

                        //    foreach (var query in queries) {
                        //        if (query.SearchType == SearchType.ProductIon) {
                        //            var qProduct = query.Mass;
                        //            var qIntensity = query.RelativeIntensity;

                        //            if (Math.Abs(qProduct - productIon) < query.MassTolerance && relativeIntensity > qIntensity) {
                        //                spot.IsFragmentQueryExist = true;
                        //                break;
                        //            }
                        //        }
                        //        else {
                        //            var qLoss = query.Mass;
                        //            var qIntensity = query.RelativeIntensity;

                        //            if (Math.Abs(qLoss - neutralLoss) < query.MassTolerance && relativeIntensity > qIntensity) {
                        //                spot.IsFragmentQueryExist = true;
                        //                break;
                        //            }
                        //        }
                        //    }
                        //    if (spot.IsFragmentQueryExist == true) break;
                        //}
                        #endregion
                    }
                    else {
                        spot.IsFragmentQueryExist = isAllFragmentExist(spectrum, queries, precursorMz, basePeakIntensity);
                        #region
                        //var isAllQueryFound = true;
                        //foreach (var query in queries) {
                        //    var qMass = query.Mass;
                        //    var qIntensity = query.RelativeIntensity;
                        //    var qType = query.SearchType;

                        //    var isQueryFound = false;
                        //    foreach (var specPeak in spectrum) {
                        //        var productIon = specPeak[0];
                        //        var neutralLoss = precursorMz - productIon;
                        //        var relativeIntensity = specPeak[1] / basePeakIntensity * 100.0;

                        //        if (qType == SearchType.ProductIon) {
                        //            if (Math.Abs(qMass - productIon) < query.MassTolerance && relativeIntensity > qIntensity) {
                        //                isQueryFound = true;
                        //                break;
                        //            }
                        //        }
                        //        else {
                        //            if (Math.Abs(qMass - neutralLoss) < query.MassTolerance && relativeIntensity > qIntensity) {
                        //                isQueryFound = true;
                        //                break;
                        //            }
                        //        }
                        //    }
                        //    if (isQueryFound == false) {
                        //        isAllQueryFound = false;
                        //        break;
                        //    }
                        //}

                        //if (isAllQueryFound)
                        //    spot.IsFragmentQueryExist = true;
                        #endregion
                    }
                }
            }
        }

        private static bool isAllFragmentExist(List<double[]> spectrum, List<FragmentSearcherQuery> queries, float precursorMz, double basePeakIntensity) {
            var isAllQueryFound = true;
            foreach (var query in queries) {
                var qMass = query.Mass;
                var qIntensity = query.RelativeIntensity;
                var qType = query.SearchType;

                var isQueryFound = false;
                foreach (var specPeak in spectrum) {
                    var productIon = specPeak[0];
                    var neutralLoss = precursorMz - productIon;
                    var relativeIntensity = specPeak[1] / basePeakIntensity * 100.0;

                    if (qType == SearchType.ProductIon) {
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

        private static bool isFragmentFound(List<double[]> spectrum, List<FragmentSearcherQuery> queries, float precursorMz, double basePeakIntensity) {
            var isFragmentExist = false;
            foreach (var specPeak in spectrum) {
                var productIon = specPeak[0];
                var neutralLoss = precursorMz - productIon;
                var relativeIntensity = specPeak[1] / basePeakIntensity * 100.0;

                foreach (var query in queries) {
                    if (query.SearchType == SearchType.ProductIon) {
                        var qProduct = query.Mass;
                        var qIntensity = query.RelativeIntensity;

                        if (Math.Abs(qProduct - productIon) < query.MassTolerance && relativeIntensity > qIntensity) {
                            isFragmentExist = true;
                            break;
                        }
                    }
                    else {
                        var qLoss = query.Mass;
                        var qIntensity = query.RelativeIntensity;

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
