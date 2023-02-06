using CompMs.Common.DataObj;
using Msdial.Gcms.Dataprocess.Utility;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Msdial.Gcms.Dataprocess.Algorithm
{
    public sealed class DataSummarizer
    {
        private DataSummarizer() { }

        /// <summary>
        /// This method is to do 2 things,
        /// 1) to get the summary of peak detections including the average peak width, retention time, height, etc..
        /// 2) to get the 'insurance' model peak which will be used as the model peak in MS2Dec algorithm in the case that any model peaks cannot be found from the focused MS/MS spectrum.
        /// </summary>
        /// <param name="dataSummaryBean"></param>
        /// <param name="spectrumList"></param>
        /// <param name="projectPropertyBean"></param>
        /// <param name="peakAreaList"></param>
        /// <param name="param"></param>
        public static DataSummaryBean GetDataSummary(List<RawSpectrum> spectrumList, List<PeakAreaBean> peakAreaList, AnalysisParamOfMsdialGcms param)
        {
            var summary = new DataSummaryBean();
            if (peakAreaList == null || peakAreaList.Count == 0) return summary;

            setBasicDataSummary(summary, spectrumList);
            setPeakAreaBeanDataSummary(summary, peakAreaList);
            setModelPeakDataSummary(summary, spectrumList, peakAreaList, param);

            return summary;
        }

        private static void setBasicDataSummary(DataSummaryBean dataSummary, List<RawSpectrum> spectrumList)
        {
            int minScanNumber = (int)spectrumList[0].ScanNumber, maxScanNumber = (int)spectrumList[spectrumList.Count - 1].ScanNumber;
            float minRT = (float)spectrumList[0].ScanStartTime, maxRT = (float)spectrumList[spectrumList.Count - 1].ScanStartTime;
            double minMz = double.MaxValue, maxMz = double.MinValue, minIntensity = double.MaxValue, maxIntensity = double.MinValue;
            for (int i = 0; i < spectrumList.Count; i++)
            {
                if (spectrumList[i].MsLevel != 1) continue;
                if (minMz > spectrumList[i].LowestObservedMz) minMz = spectrumList[i].LowestObservedMz;
                if (maxMz < spectrumList[i].HighestObservedMz) maxMz = spectrumList[i].HighestObservedMz;
                if (minIntensity > spectrumList[i].MinIntensity) minIntensity = spectrumList[i].MinIntensity;
                if (maxIntensity < spectrumList[i].BasePeakIntensity) maxIntensity = spectrumList[i].BasePeakIntensity;
            }

            dataSummary.MinScanNumber = minScanNumber;
            dataSummary.MaxScanNumber = maxScanNumber;
            dataSummary.MinRetentionTime = minRT;
            dataSummary.MaxRetentionTime = maxRT;
            dataSummary.MinMass = (float)minMz;
            dataSummary.MaxMass = (float)maxMz;
            dataSummary.MinIntensity = (int)minIntensity;
            dataSummary.MaxIntensity = (int)maxIntensity;
        }

        private static void setPeakAreaBeanDataSummary(DataSummaryBean dataSummary, List<PeakAreaBean> peakAreaBeanList)
        {
            // except for median value
            float minPeakWidth = float.MaxValue, averagePeakWidth = 0, medianPeakWidth = 0, maxPeakWidth = float.MinValue, peakWidth = 0;
            float minHeight = float.MaxValue, averageHeight = 0, medianHeight = 0, maxHeight = float.MinValue, peakHeight;
            float minPeakTopRT = float.MaxValue, averagePeakTopRT = 0, medianPeakTopRT = 0, maxPeakTopRT = float.MinValue, peakTopRT = 0;
            double stdevPeakWidth = 0, stdevHeight = 0, stdevPeakTopRT = 0;

            for (int i = 0; i < peakAreaBeanList.Count; i++)
            {
                peakWidth = peakAreaBeanList[i].RtAtRightPeakEdge - peakAreaBeanList[i].RtAtLeftPeakEdge;
                peakHeight = peakAreaBeanList[i].IntensityAtPeakTop;
                peakTopRT = peakAreaBeanList[i].RtAtPeakTop;
                averagePeakWidth += peakWidth;
                averageHeight += peakHeight;
                averagePeakTopRT += peakTopRT;

                if (minPeakWidth > peakWidth) minPeakWidth = peakWidth;
                if (maxPeakWidth < peakWidth) maxPeakWidth = peakWidth;
                if (minHeight > peakHeight) minHeight = peakHeight;
                if (maxHeight < peakHeight) maxHeight = peakHeight;
                if (minPeakTopRT > peakTopRT) minPeakTopRT = peakTopRT;
                if (maxPeakTopRT < peakTopRT) maxPeakTopRT = peakTopRT;
            }

            averagePeakWidth = averagePeakWidth / (float)peakAreaBeanList.Count;
            averageHeight = averageHeight / peakAreaBeanList.Count;
            averagePeakTopRT = averagePeakTopRT / (float)peakAreaBeanList.Count;

            for (int i = 0; i < peakAreaBeanList.Count; i++)
            {
                stdevPeakWidth += Math.Pow(peakAreaBeanList[i].RtAtRightPeakEdge - peakAreaBeanList[i].RtAtLeftPeakEdge - averagePeakWidth, 2);
                stdevHeight += Math.Pow(peakAreaBeanList[i].IntensityAtPeakTop - averageHeight, 2);
                stdevPeakTopRT += Math.Pow(peakAreaBeanList[i].RtAtPeakTop - averagePeakTopRT, 2);
            }

            if (peakAreaBeanList.Count - 1 != 0)
            {
                stdevPeakWidth /= (peakAreaBeanList.Count - 1);
                stdevHeight /= (peakAreaBeanList.Count - 1);
                stdevPeakTopRT /= (peakAreaBeanList.Count - 1);
            }

            peakAreaBeanList = peakAreaBeanList.OrderBy(n => (n.RtAtRightPeakEdge - n.RtAtLeftPeakEdge)).ToList();
            medianPeakWidth = peakAreaBeanList[(int)(peakAreaBeanList.Count / 2)].RtAtRightPeakEdge - peakAreaBeanList[(int)(peakAreaBeanList.Count / 2)].RtAtLeftPeakEdge;
            peakAreaBeanList = peakAreaBeanList.OrderBy(n => n.IntensityAtPeakTop).ToList();
            medianHeight = peakAreaBeanList[(int)(peakAreaBeanList.Count / 2)].IntensityAtPeakTop;
            peakAreaBeanList = peakAreaBeanList.OrderBy(n => n.RtAtPeakTop).ToList();
            medianPeakTopRT = peakAreaBeanList[(int)(peakAreaBeanList.Count / 2)].RtAtPeakTop;

            dataSummary.MinPeakWidth = minPeakWidth;
            dataSummary.AveragePeakWidth = averagePeakWidth;
            dataSummary.MedianPeakWidth = medianPeakWidth;
            dataSummary.MaxPeakWidth = maxPeakWidth;
            dataSummary.StdevPeakWidth = (float)stdevPeakWidth;

            dataSummary.MinPeakHeight = minHeight;
            dataSummary.MedianPeakHeight = medianHeight;
            dataSummary.AveragePeakHeight = averageHeight;
            dataSummary.MaxPeakHeight = maxHeight;
            dataSummary.StdevPeakHeight = (int)stdevHeight;

            dataSummary.MinPeakTopRT = minPeakTopRT;
            dataSummary.AverageminPeakTopRT = averagePeakTopRT;
            dataSummary.MedianminPeakTopRT = medianPeakTopRT;
            dataSummary.StdevPeakTopRT = (float)stdevPeakTopRT;
            dataSummary.MaxPeakTopRT = maxPeakTopRT;

        }

        private static void setModelPeakDataSummary(DataSummaryBean dataSummary, List<RawSpectrum> spectrumList, List<PeakAreaBean> peakAreaBeanList, AnalysisParamOfMsdialGcms param)
        {
            int candidateID;
            var shapnessValueList = new List<float[]>();

            for (int i = 0; i < peakAreaBeanList.Count; i++)
                if (peakAreaBeanList[i].SymmetryValue > 0.9 && peakAreaBeanList[i].BasePeakValue > 0.9 && peakAreaBeanList[i].IdealSlopeValue > 0.9 && Math.Abs(peakAreaBeanList[i].ScanNumberAtRightPeakEdge + peakAreaBeanList[i].ScanNumberAtLeftPeakEdge - 2 * peakAreaBeanList[i].ScanNumberAtPeakTop) < 2)
                    shapnessValueList.Add(new float[] { i, peakAreaBeanList[i].ShapenessValue });
            // very add hoc
            if (shapnessValueList.Count == 0) { 
                dataSummary.ModelPeakBean = getWorstCaseModelPeaklist(dataSummary);
#if DEBUG
                Console.WriteLine("No model peak in ms1"); 
#endif
                return; }

            shapnessValueList = shapnessValueList.OrderBy(n => n[1]).ToList();
            candidateID = (int)shapnessValueList[(int)(shapnessValueList.Count / 2)][0];

            var focusedMass = peakAreaBeanList[candidateID].AccurateMass;
            var massAccuracy = param.MassAccuracy; if (param.AccuracyType == AccuracyType.IsNominal) massAccuracy = 0.5F;
            var peaklist = DataAccessGcUtility.GetMs1SlicePeaklist(spectrumList, focusedMass, massAccuracy, param.RetentionTimeBegin, param.RetentionTimeEnd, param.IonMode);
            peaklist = DataAccessGcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);

            if (!modelPeakEvaluator(peaklist, peakAreaBeanList[candidateID]))
            {
                bool createriaChecker = false;
                int limitCounter = 1;
                while (true)
                {
                    if ((int)(shapnessValueList.Count / 2) - limitCounter < 0) break;
                    if ((int)(shapnessValueList.Count / 2) + limitCounter > shapnessValueList.Count - 1) break;

                    candidateID = (int)shapnessValueList[(int)(shapnessValueList.Count / 2 - limitCounter)][0];
                    focusedMass = peakAreaBeanList[candidateID].AccurateMass;
                    peaklist = DataAccessGcUtility.GetMs1SlicePeaklist(spectrumList, focusedMass, massAccuracy, param.RetentionTimeBegin, param.RetentionTimeEnd, param.IonMode);
                    peaklist = DataAccessGcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);

                    if (modelPeakEvaluator(peaklist, peakAreaBeanList[candidateID])) { createriaChecker = true; break; }

                    candidateID = (int)shapnessValueList[(int)(shapnessValueList.Count / 2 + limitCounter)][0];
                    focusedMass = peakAreaBeanList[candidateID].AccurateMass;
                    peaklist = DataAccessGcUtility.GetMs1SlicePeaklist(spectrumList, focusedMass, massAccuracy, param.RetentionTimeBegin, param.RetentionTimeEnd, param.IonMode);
                    peaklist = DataAccessGcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);

                    if (modelPeakEvaluator(peaklist, peakAreaBeanList[candidateID])) { createriaChecker = true; break; }

                    limitCounter++;
                }
                if (createriaChecker == false) { 
                    dataSummary.ModelPeakBean = getWorstCaseModelPeaklist(dataSummary);
#if DEBUG
                    Console.WriteLine("No model peak in ms1"); 
#endif
                    return;
                }
            }
            dataSummary.ModelPeakBean = getModelPeakBean(peakAreaBeanList[candidateID], peaklist);
        }

        private static ModelpeakBean getModelPeakBean(PeakAreaBean peakAreaBean, List<double[]> peaklist)
        {
            var modelPeakBean = new ModelpeakBean();
            modelPeakBean.StartRt = peakAreaBean.RtAtLeftPeakEdge;
            modelPeakBean.PeaktopRt = peakAreaBean.RtAtPeakTop;
            modelPeakBean.EndRt = peakAreaBean.RtAtRightPeakEdge;
            modelPeakBean.StartScanNumber = peakAreaBean.ScanNumberAtLeftPeakEdge;
            modelPeakBean.PeaktopScanNumber = peakAreaBean.ScanNumberAtPeakTop;
            modelPeakBean.EndScanNumber = peakAreaBean.ScanNumberAtRightPeakEdge;
            modelPeakBean.UniqueMs = -1;

            for (int i = peakAreaBean.ScanNumberAtLeftPeakEdge; i <= peakAreaBean.ScanNumberAtRightPeakEdge; i++)
            {
                modelPeakBean.RtList.Add((float)peaklist[i][1]);
                modelPeakBean.IntensityList.Add((int)peaklist[i][3]);
            }

            return modelPeakBean;
        }

        private static bool modelPeakEvaluator(List<double[]> peaklist, PeakAreaBean peakAreaBean)
        {
            for (int i = peakAreaBean.ScanNumberAtLeftPeakEdge; i < peakAreaBean.ScanNumberAtPeakTop; i++)
                if (peaklist[i][3] > peaklist[i + 1][3]) return false;
            for (int i = peakAreaBean.ScanNumberAtPeakTop; i > peakAreaBean.ScanNumberAtRightPeakEdge; i++)
                if (peaklist[i][3] < peaklist[i + 1][3]) return false;
            return true;
        }

        private static ModelpeakBean getWorstCaseModelPeaklist(DataSummaryBean dataSummary)
        {
            //set gausian peak
            int medianPeakWidthScan = (int)(dataSummary.MedianPeakWidth * (dataSummary.MaxScanNumber - dataSummary.MinScanNumber) / (dataSummary.MaxRetentionTime - dataSummary.MinRetentionTime));
            float medianPeakIntensity = dataSummary.MedianPeakHeight;
            float stepRt = (float)(dataSummary.MedianPeakWidth / (float)medianPeakWidthScan);

            var modelPeakBean = new ModelpeakBean();
            modelPeakBean.StartRt = 0;
            modelPeakBean.PeaktopRt = dataSummary.MedianPeakWidth * 0.5F;
            modelPeakBean.EndRt = dataSummary.MedianPeakWidth;
            modelPeakBean.StartScanNumber = 0;
            modelPeakBean.PeaktopScanNumber = (int)(medianPeakWidthScan * 0.5);
            modelPeakBean.EndScanNumber = medianPeakWidthScan;

            for (int i = 0; i <= medianPeakWidthScan; i++)
            {
                modelPeakBean.RtList.Add(stepRt * i);
                modelPeakBean.IntensityList.Add((int)BasicMathematics.GaussianFunction(medianPeakIntensity, modelPeakBean.PeaktopRt, dataSummary.MedianPeakWidth * 0.5, stepRt * i));
            }

            return modelPeakBean;
        }
    }
}
