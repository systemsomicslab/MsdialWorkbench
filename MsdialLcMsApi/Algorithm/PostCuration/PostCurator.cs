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

            // process
            foreach (var spot in spots) {

                var frag = false;

                double avgBlank = 0;
                List<AlignmentChromPeakFeature> blankProps = spot.AlignedPeakProperties.Where(x => param.FileID_ClassName[x.FileID] == "Blank").ToList();
                //Console.Write(blankProps.Average(x => x.PeakHeightTop));
                avgBlank = blankProps.Average(x => x.PeakHeightTop);

                double avgQC = 0;
                List<AlignmentChromPeakFeature> qcProps = spot.AlignedPeakProperties.Where(x => param.FileID_ClassName[x.FileID] == "QC").ToList();
                //Console.Write(qcProps.Average(x => x.PeakHeightTop));
                avgQC = qcProps.Average(x => x.PeakHeightTop);

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

                double filterBlankThreshold = 0.8;
                double ratioBlank = avgBlank / avgQC;
                if (ratioBlank >= filterBlankThreshold) {
                    spot.IsFilteredByPostCurator = true;
                }

                // if (frag == true) {
                //     spot.IsFilteredByPostCurator = true;
                // }

            }

            return spots;
        }
    }
}
