using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Msdial.Lcms.Dataprocess.Utility;
using Msdial.Lcms.Dataprocess.Algorithm;
using Rfx.Riken.OsakaUniv;
using Rfx.Riken.OsakaUniv.RetentionTimeCorrection;

namespace Msdial.Common.Utility
{
    public enum RtDiffLabel { id, rt, name };

    public class RtCorrection
    {
        #region make DrawVisual class

        #region Retention time difference, private function to show label
        
        private static float CalcRtDiff_SampleMinusAverage(AnalysisFileBean file, List<CommonStdData> list, int i) {
            return (float)(file.RetentionTimeCorrectionBean.StandardList[i].SamplePeakAreaBean.RtAtPeakTop - (float)list[i].AverageRetentionTime) * 60f;
        }
        #endregion


        #region Overlayed EIC, get smoothed peak list

        private static List<double[]> GetSmoothedRetentionTime(RetentionTimeCorrectionBean bean, AnalysisParametersBean param, List<double[]> peaks) {
            var correctedPeakList = new List<double[]>();
            for (var i = 0; i < peaks.Count; i++) {
                correctedPeakList.Add(new double[] { i, bean.PredictedRt[(int)peaks[i][0]], peaks[i][2], peaks[i][3] });
            }
            return DataAccessLcUtility.GetSmoothedPeaklist(correctedPeakList, param.SmoothingMethod, param.SmoothingLevel);
        }
        #endregion

        #region public methods to make DrawVisual component
        public static List<CommonStdData> MakeCommonStdList(ObservableCollection<AnalysisFileBean> analysisFiles, List<TextFormatCompoundInformationBean> iStdList) {
            var commonStdList = new List<CommonStdData>();
            var tmpStdList = iStdList.Where(x => x.IsTarget).OrderBy(x => x.RetentionTime);
            foreach (var std in tmpStdList) {
                commonStdList.Add(new CommonStdData(std));
            }
            for (var i = 0; i < analysisFiles.Count; i++) {
                for (var j = 0; j < commonStdList.Count; j++) {
                    commonStdList[j].SetStandard(analysisFiles[i].RetentionTimeCorrectionBean.StandardList[j]);
                }
            }
            foreach (var d in commonStdList) {
                d.CalcAverageRetentionTime();
            }
            return commonStdList;
        }

        #endregion

        #endregion
    }
}
