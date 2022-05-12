using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Mathematics.Basic;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.MsdialLcMsApi.Algorithm.PostCuration {
    public class PostCurator {
        public IReadOnlyList<AlignmentSpotProperty> MsCleanRCurator(
            IReadOnlyList<AlignmentSpotProperty> spots,
            IReadOnlyList<MSDecResult> msdecResults,
            IReadOnlyList<AnalysisFileBean> files, 
            ParameterBase param) {

            var ghosts = new List<double>();
            var postcurparam = param.PostCurationParameter;

            // process
            foreach (var spot in spots) {
                var frag = false;

                var blankProps = spot.AlignedPeakProperties.Where(x => param.FileID_AnalysisFileType[x.FileID] == AnalysisFileType.Blank).ToList();
                var isBlankDataAvailable = blankProps.IsEmptyOrNull() ? false : true;
                var avgBlank = isBlankDataAvailable ? blankProps.Average(x => x.PeakHeightTop) : 0.0;

                var qcProps = spot.AlignedPeakProperties.Where(x => param.FileID_AnalysisFileType[x.FileID] == AnalysisFileType.QC).ToList();
                var isQcDataAvailable = qcProps.IsEmptyOrNull() ? false : true;
                var avgQC = isQcDataAvailable ? qcProps.Average(x => x.PeakHeightTop) : 0.0;

                var sampleProps = spot.AlignedPeakProperties.Where(x => param.FileID_AnalysisFileType[x.FileID] == AnalysisFileType.Sample).ToList();
                var isSampleDataAvailable = sampleProps.IsEmptyOrNull() ? false : true;
                var avgSample = isSampleDataAvailable ? sampleProps.Average(x => x.PeakHeightTop) : 0.0;

                if (postcurparam.IsBlankFilter && isBlankDataAvailable && (isQcDataAvailable || isSampleDataAvailable)) {
                    var ratioBlank = avgBlank / Math.Max(avgQC, avgSample);
                    if (ratioBlank >= postcurparam.FilterBlankThreshold) {
                        spot.IsBlankFilteredByPostCurator = true;
                        ghosts.Add(Math.Round(spot.MassCenter, 3));
                    }
                }

                if (postcurparam.IsMzFilter) {
                    if (spot.MassCenter - Math.Floor(spot.MassCenter) >= (spot.MassCenter * 0.00111 + 0.039)) {
                        spot.IsMzFilteredByPostCurator = true;
                    }
                }

                if (postcurparam.IsRsdFilter) {
                    var classToHeights = new Dictionary<string, List<double>>();
                    var classToRsd = new Dictionary<string, double>();
                    foreach (var class_ in param.FileID_ClassName.Values.Distinct()) {
                        classToHeights[class_] = new List<double>();
                    }
                    foreach (var props in spot.AlignedPeakProperties) {
                        if (param.FileID_AnalysisFileType[props.FileID] != AnalysisFileType.Blank) {
                            classToHeights[param.FileID_ClassName[props.FileID]].Add(props.PeakHeightTop);
                        }
                    }
                    foreach (var class_ in classToHeights.Keys) {
                        if (classToHeights[class_].Any()) {
                            var avg = BasicMathematics.Mean(classToHeights[class_]);
                            var sd = BasicMathematics.Stdev(classToHeights[class_]);
                            classToRsd[class_] = sd * 100 / avg;
                        }
                    }
                    if (BasicMathematics.Min(classToRsd.Values.ToArray()) >= postcurparam.FilterRsdThreshold) {
                        spot.IsRsdFilteredByPostCurator = true;
                    }
                }

                if (postcurparam.IsRmdFilter) {
                    var rmd = (spot.MassCenter - Math.Floor(spot.MassCenter)) / spot.MassCenter * 1000000;
                    if (rmd < postcurparam.FilterMinRmdThreshold | rmd > postcurparam.FilterMaxRmdThreshold) {
                        spot.IsRmdFilteredByPostCurator = true;
                    }
                }

                //if (postcurparam.IsRsdFilter) {
                //    foreach (scriptclass in param.FileID_ClassName)
                //}

                // filtering process
                #region
                // var mz = spot.MassCenter;
                // var rt = spot.TimesCenter.RT.Value;
                // var alignedPeaks = spot.AlignedPeakProperties;

                // foreach (var peak in alignedPeaks) {
                //     var intensity = peak.PeakHeightTop;
                //     var area = peak.PeakAreaAboveZero;
                //     var mzOfPeak = peak.Mass;
                //     var fileID = peak.FileID;

                //     var fileObj = files[fileID];
                //     var fileType = fileObj.AnalysisFileType;
                //     if (fileType == Common.Enum.AnalysisFileType.Blank) {

                //     }

                // }
                #endregion                

                // if (frag == true) {
                //     spot.IsFilteredByPostCurator = true;
                // }

            }

            if (postcurparam.IsBlankGhostFilter) {
                foreach (var spot in spots) {
                    if (ghosts.Contains(Math.Round(spot.MassCenter, 3))) {
                        spot.IsBlankGhostFilteredByPostCurator = true;
                    }
                }
            }

            return spots;
        }
    }
}
