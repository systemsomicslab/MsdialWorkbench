using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.Common.Components;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Setting {
    /// <summary>
    /// Applies RT edits from the sample table back into the RT correction state.
    /// </summary>
    public static class RetentionTimeCorrectionEditSynchronizer {
        /// <summary>
        /// Copies sample table RT edits into the analysis files and rebuilds the common standard summary.
        /// </summary>
        public static List<CommonStdData> SynchronizeSampleRtEdits(
            IReadOnlyList<AnalysisFileBean> analysisFiles,
            IReadOnlyList<SampleListVM> sampleListVMs,
            RetentionTimeCorrectionCommon rtCorrectionCommon) {
            rtCorrectionCommon.SampleCellInfoListList = new List<List<SampleListCellInfo>>();
            for (var i = 0; i < sampleListVMs.Count; i++) {
                var sampleVm = sampleListVMs[i];
                var cellInfos = new List<SampleListCellInfo>();
                for (var j = 0; j < sampleVm.Values.Count; j++) {
                    var cell = sampleVm.Values[j];
                    cellInfos.Add(cell.SampleListCellInfo);
                    analysisFiles[i].RetentionTimeCorrectionBean.StandardList[j].SamplePeakAreaBean.ChromXs.RT =
                        new RetentionTime(cell.Rt, analysisFiles[i].RetentionTimeCorrectionBean.StandardList[j].SamplePeakAreaBean.ChromXs.RT.Unit);
                }
                rtCorrectionCommon.SampleCellInfoListList.Add(cellInfos);
            }

            return RetentionTimeCorrectionMethod.MakeCommonStdList(
                new List<AnalysisFileBean>(analysisFiles),
                rtCorrectionCommon.StandardLibrary);
        }
    }
}
