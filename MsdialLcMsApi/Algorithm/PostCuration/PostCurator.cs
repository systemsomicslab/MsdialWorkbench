using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialLcMsApi.Algorithm.PostCuration {
    class PostCurator {
        public IReadOnlyList<AlignmentSpotProperty> MsCleanRCurator(
            IReadOnlyList<AlignmentSpotProperty> spots,
            IReadOnlyList<MSDecResult> msdecResults,
            IReadOnlyList<AnalysisFileBean> files, 
            ParameterBase param) {

            // process
            foreach (var spot in spots) {

                var frag = false;

                // filtering process
                #region
                var mz = spot.MassCenter;
                var rt = spot.TimesCenter.RT.Value;
                var alignedPeaks = spot.AlignedPeakProperties;

                foreach (var peak in alignedPeaks) {
                    var intensity = peak.PeakHeightTop;
                    var area = peak.PeakAreaAboveZero;
                    var mzOfPeak = peak.Mass;
                    var fileID = peak.FileID;

                    var fileObj = files[fileID];
                    var fileType = fileObj.AnalysisFileType;
                    if (fileType == Common.Enum.AnalysisFileType.Blank) {

                    }

                }
                #endregion

                if (frag == true) {
                    spot.IsFilteredByPostCurator = true;
                }

            }

            return spots;
        }
    }
}
