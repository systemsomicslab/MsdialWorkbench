using CompMs.Common.DataObj;
using Msdial.Lcms.Dataprocess.Utility;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Msdial.Lcms.Dataprocess.Algorithm
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
        /// <param name="spectrumCollection"></param>
        /// <param name="projectPropertyBean"></param>
        /// <param name="peakAreaBeanCollection"></param>
        /// <param name="analysisParametersBean"></param>
        public static void SetDataSummary(DataSummaryBean dataSummaryBean, ObservableCollection<RawSpectrum> spectrumCollection, ProjectPropertyBean projectPropertyBean,
            ObservableCollection<PeakAreaBean> peakAreaBeanCollection, AnalysisParametersBean analysisParametersBean)
        {
            if (peakAreaBeanCollection == null || peakAreaBeanCollection.Count == 0) return;

            var peakAreaBeanList = new List<PeakAreaBean>(peakAreaBeanCollection);

            setBasicDataSummary(dataSummaryBean, spectrumCollection, analysisParametersBean);
            setPeakAreaBeanDataSummary(dataSummaryBean, peakAreaBeanList);

            if (projectPropertyBean.MethodType == MethodType.diMSMS && projectPropertyBean.SeparationType != SeparationType.IonMobility) 
                setModelPeakDataSummary(dataSummaryBean, spectrumCollection, projectPropertyBean, peakAreaBeanCollection, analysisParametersBean);
        }

        private static void setBasicDataSummary(DataSummaryBean dataSummaryBean, ObservableCollection<RawSpectrum> spectrumCollection, AnalysisParametersBean param)
        {
            int minScanNumber = (int)spectrumCollection[0].ScanNumber, maxScanNumber = (int)spectrumCollection[spectrumCollection.Count - 1].ScanNumber;
            float minRT = (float)spectrumCollection[0].ScanStartTime, maxRT = (float)spectrumCollection[spectrumCollection.Count - 1].ScanStartTime;
            double minDriftTime = double.MaxValue, maxDriftTime = double.MinValue;
            double minMz = double.MaxValue, maxMz = double.MinValue, minIntensity = double.MaxValue, maxIntensity = double.MinValue;


            for (int i = 0; i < spectrumCollection.Count; i++)
            {
                if (minMz > spectrumCollection[i].LowestObservedMz) minMz = spectrumCollection[i].LowestObservedMz;
                if (maxMz < spectrumCollection[i].HighestObservedMz) maxMz = spectrumCollection[i].HighestObservedMz;
                if (minIntensity > spectrumCollection[i].MinIntensity) minIntensity = spectrumCollection[i].MinIntensity;
                if (maxIntensity < spectrumCollection[i].BasePeakIntensity) maxIntensity = spectrumCollection[i].BasePeakIntensity;
                if (param.IsIonMobility) {
                    if (minDriftTime > spectrumCollection[i].DriftTime) minDriftTime = spectrumCollection[i].DriftTime;
                    if (maxDriftTime < spectrumCollection[i].DriftTime) maxDriftTime = spectrumCollection[i].DriftTime;
                }
            }

            dataSummaryBean.MinScanNumber = minScanNumber;
            dataSummaryBean.MaxScanNumber = maxScanNumber;
            dataSummaryBean.MinRetentionTime = minRT;
            dataSummaryBean.MaxRetentionTime = maxRT;
            dataSummaryBean.MinMass = (float)minMz;
            dataSummaryBean.MaxMass = (float)maxMz;
            dataSummaryBean.MinIntensity = (int)minIntensity;
            dataSummaryBean.MaxIntensity = (int)maxIntensity;

            if (param.IsIonMobility) {
                dataSummaryBean.MinDriftTime = (float)minDriftTime;
                dataSummaryBean.MaxDriftTime = (float)maxDriftTime;
            }
        }

        private static void setPeakAreaBeanDataSummary(DataSummaryBean dataSummaryBean, List<PeakAreaBean> peakAreaBeanList)
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

            dataSummaryBean.MinPeakWidth = minPeakWidth;
            dataSummaryBean.AveragePeakWidth = averagePeakWidth;
            dataSummaryBean.MedianPeakWidth = medianPeakWidth;
            dataSummaryBean.MaxPeakWidth = maxPeakWidth;
            dataSummaryBean.StdevPeakWidth = (float)stdevPeakWidth;

            dataSummaryBean.MinPeakHeight = minHeight;
            dataSummaryBean.MedianPeakHeight = medianHeight;
            dataSummaryBean.AveragePeakHeight = averageHeight;
            dataSummaryBean.MaxPeakHeight = maxHeight;
            dataSummaryBean.StdevPeakHeight = (int)stdevHeight;

            dataSummaryBean.MinPeakTopRT = minPeakTopRT;
            dataSummaryBean.AverageminPeakTopRT = averagePeakTopRT;
            dataSummaryBean.MedianminPeakTopRT = medianPeakTopRT;
            dataSummaryBean.StdevPeakTopRT = (float)stdevPeakTopRT;
            dataSummaryBean.MaxPeakTopRT = maxPeakTopRT;

        }

        private static void setModelPeakDataSummary(DataSummaryBean dataSummaryBean, ObservableCollection<RawSpectrum> spectrumCollection, ProjectPropertyBean projectPropertyBean, ObservableCollection<PeakAreaBean> peakAreaBeanCollection, AnalysisParametersBean analysisParametersBean)
        {
            int candidateID;
            var shapnessValueList = new List<float[]>();

            for (int i = 0; i < peakAreaBeanCollection.Count; i++)
                if (peakAreaBeanCollection[i].SymmetryValue > 0.9 && peakAreaBeanCollection[i].BasePeakValue > 0.9 && peakAreaBeanCollection[i].IdealSlopeValue > 0.9 && Math.Abs(peakAreaBeanCollection[i].ScanNumberAtRightPeakEdge + peakAreaBeanCollection[i].ScanNumberAtLeftPeakEdge - 2 * peakAreaBeanCollection[i].ScanNumberAtPeakTop) < 2)
                    shapnessValueList.Add(new float[] { i, peakAreaBeanCollection[i].ShapenessValue });
            // very add hoc
            if (shapnessValueList.Count == 0) {
				dataSummaryBean.ModelPeakBean = getWorstCaseModelPeaklist(dataSummaryBean);
#if DEBUG
                Console.WriteLine("No model peak in ms1");
#endif
				return;
			}

            shapnessValueList = shapnessValueList.OrderBy(n => n[1]).ToList();
            candidateID = (int)shapnessValueList[(int)(shapnessValueList.Count / 2)][0];

            float focusedMass = peakAreaBeanCollection[candidateID].AccurateMass;
            var peaklist = DataAccessLcUtility.GetMs1Peaklist(spectrumCollection, projectPropertyBean, focusedMass, 0.01F, analysisParametersBean.RetentionTimeBegin, analysisParametersBean.RetentionTimeEnd);
            peaklist = DataAccessLcUtility.GetSmoothedPeaklist(peaklist, analysisParametersBean.SmoothingMethod, analysisParametersBean.SmoothingLevel);

            if (!modelPeakEvaluator(peaklist, peakAreaBeanCollection[candidateID]))
            {
                bool createriaChecker = false;
                int limitCounter = 1;
                while (true)
                {
                    if ((int)(shapnessValueList.Count / 2) - limitCounter < 0) break;
                    if ((int)(shapnessValueList.Count / 2) + limitCounter > shapnessValueList.Count - 1) break;

                    candidateID = (int)shapnessValueList[(int)(shapnessValueList.Count / 2 - limitCounter)][0];
                    focusedMass = peakAreaBeanCollection[candidateID].AccurateMass;
                    peaklist = DataAccessLcUtility.GetMs1Peaklist(spectrumCollection, projectPropertyBean, focusedMass, 0.01F, analysisParametersBean.RetentionTimeBegin, analysisParametersBean.RetentionTimeEnd);
                    peaklist = DataAccessLcUtility.GetSmoothedPeaklist(peaklist, analysisParametersBean.SmoothingMethod, analysisParametersBean.SmoothingLevel);

                    if (modelPeakEvaluator(peaklist, peakAreaBeanCollection[candidateID])) { createriaChecker = true; break; }

                    candidateID = (int)shapnessValueList[(int)(shapnessValueList.Count / 2 + limitCounter)][0];
                    focusedMass = peakAreaBeanCollection[candidateID].AccurateMass;
                    peaklist = DataAccessLcUtility.GetMs1Peaklist(spectrumCollection, projectPropertyBean, focusedMass, 0.01F, analysisParametersBean.RetentionTimeBegin, analysisParametersBean.RetentionTimeEnd);
                    peaklist = DataAccessLcUtility.GetSmoothedPeaklist(peaklist, analysisParametersBean.SmoothingMethod, analysisParametersBean.SmoothingLevel);

                    if (modelPeakEvaluator(peaklist, peakAreaBeanCollection[candidateID])) { createriaChecker = true; break; }

                    limitCounter++;
                }
                if (createriaChecker == false) { 
                    dataSummaryBean.ModelPeakBean = getWorstCaseModelPeaklist(dataSummaryBean);
#if DEBUG
                    Console.WriteLine("No model peak in ms1");
#endif
                    return; 
                }
            }
            dataSummaryBean.ModelPeakBean = getModelPeakBean(peakAreaBeanCollection[candidateID], peaklist);
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

        private static ModelpeakBean getWorstCaseModelPeaklist(DataSummaryBean dataSummaryBean)
        {
            //set gausian peak
            int medianPeakWidthScan = (int)(dataSummaryBean.MedianPeakWidth * (dataSummaryBean.MaxScanNumber - dataSummaryBean.MinScanNumber) / (dataSummaryBean.MaxRetentionTime - dataSummaryBean.MinRetentionTime));
            float medianPeakIntensity = dataSummaryBean.MedianPeakHeight;
            float stepRt = (float)(dataSummaryBean.MedianPeakWidth / (float)medianPeakWidthScan);

            var modelPeakBean = new ModelpeakBean();
            modelPeakBean.StartRt = 0;
            modelPeakBean.PeaktopRt = dataSummaryBean.MedianPeakWidth * 0.5F;
            modelPeakBean.EndRt = dataSummaryBean.MedianPeakWidth;
            modelPeakBean.StartScanNumber = 0;
            modelPeakBean.PeaktopScanNumber = (int)(medianPeakWidthScan * 0.5);
            modelPeakBean.EndScanNumber = medianPeakWidthScan;

            for (int i = 0; i <= medianPeakWidthScan; i++)
            {
                modelPeakBean.RtList.Add(stepRt * i);
                modelPeakBean.IntensityList.Add((int)BasicMathematics.GaussianFunction(medianPeakIntensity, modelPeakBean.PeaktopRt, dataSummaryBean.MedianPeakWidth * 0.5, stepRt * i));
            }

            return modelPeakBean;
        }
    }
}
