using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Accord.Statistics.Testing;

namespace Rfx.Riken.OsakaUniv
{
    public class TableViewerUtility {
        public TableViewerUtility() { }
        public static void CalcStatisticsForParticularAlignmentSpot(AlignmentPropertyBean alignmentPropertyBean, ObservableCollection<AnalysisFileBean> analysisFiles) {
            if (alignmentPropertyBean == null) return;
            var fileIdAndFileClassDictionary = getFileIdAndFileClassDictionary(analysisFiles);
            var fileClassAndValueDictionary = getFileClassAndValueDictionary(alignmentPropertyBean, fileIdAndFileClassDictionary);

            if (fileClassAndValueDictionary.Count > 1) {
                var numSamples = 0;
                foreach (var d in fileClassAndValueDictionary) numSamples += d.Value.Count;

                var isAnovaTarget = numSamples <= fileClassAndValueDictionary.Count ? false : true;
                fileClassAndValueDictionary = getFileClassAndValueDictionary(alignmentPropertyBean, fileIdAndFileClassDictionary);
                if (isAnovaTarget) {
                    calcMinVsMaxRatio(fileClassAndValueDictionary, alignmentPropertyBean);
                    calcAnova(fileClassAndValueDictionary, alignmentPropertyBean);
                }
                else {
                    calcMinVsMaxRatio(fileClassAndValueDictionary, alignmentPropertyBean);
                }
            }
        }

        public static void CalcStatisticsForParticularAlignmentSpot(AlignedDriftSpotPropertyBean alignedDriftSpotPropertyBean, ObservableCollection<AnalysisFileBean> analysisFiles) {
            if (alignedDriftSpotPropertyBean == null) return;
            var fileIdAndFileClassDictionary = getFileIdAndFileClassDictionary(analysisFiles);
            var fileClassAndValueDictionary = getFileClassAndValueDictionary(alignedDriftSpotPropertyBean, fileIdAndFileClassDictionary);

            if (fileClassAndValueDictionary.Count > 1) {
                var numSamples = 0;
                foreach (var d in fileClassAndValueDictionary) numSamples += d.Value.Count;

                var isAnovaTarget = numSamples <= fileClassAndValueDictionary.Count ? false : true;
                fileClassAndValueDictionary = getFileClassAndValueDictionary(alignedDriftSpotPropertyBean, fileIdAndFileClassDictionary);
                if (isAnovaTarget) {
                    calcMinVsMaxRatio(fileClassAndValueDictionary, alignedDriftSpotPropertyBean);
                    calcAnova(fileClassAndValueDictionary, alignedDriftSpotPropertyBean);
                }
                else {
                    calcMinVsMaxRatio(fileClassAndValueDictionary, alignedDriftSpotPropertyBean);
                }
            }
        }

        public static void CalcStatisticsForAlignmentProperty(ObservableCollection<AlignmentPropertyBean> alignmentPropertyBeans, ObservableCollection<AnalysisFileBean> analysisFiles, bool isIonMobility) {
            if (alignmentPropertyBeans == null || alignmentPropertyBeans.Count == 0) return;
            var fileIdAndFileClassDictionary = getFileIdAndFileClassDictionary(analysisFiles);
            var fileClassAndValueDictionary = getFileClassAndValueDictionary(alignmentPropertyBeans[0], fileIdAndFileClassDictionary);

            if (fileClassAndValueDictionary.Count > 1) {
                var numSamples = 0;
                foreach (var d in fileClassAndValueDictionary) numSamples += d.Value.Count;

                var isAnovaTarget = numSamples <= fileClassAndValueDictionary.Count ? false : true;
                if (isAnovaTarget) {
                    foreach (var alignmentProp in alignmentPropertyBeans) {
                        fileClassAndValueDictionary = getFileClassAndValueDictionary(alignmentProp, fileIdAndFileClassDictionary);
                        calcMinVsMaxRatio(fileClassAndValueDictionary, alignmentProp);
                        calcAnova(fileClassAndValueDictionary, alignmentProp);
                        if (isIonMobility) {
                            foreach (var driftProp in alignmentProp.AlignedDriftSpots) {
                                fileClassAndValueDictionary = getFileClassAndValueDictionary(driftProp, fileIdAndFileClassDictionary);
                                calcMinVsMaxRatio(fileClassAndValueDictionary, driftProp);
                                calcAnova(fileClassAndValueDictionary, driftProp);
                            }
                        }
                    }
                }
                else {
                    foreach (var alignmentProp in alignmentPropertyBeans) {
                        fileClassAndValueDictionary = getFileClassAndValueDictionary(alignmentProp, fileIdAndFileClassDictionary);
                        calcMinVsMaxRatio(fileClassAndValueDictionary, alignmentProp);
                        if (isIonMobility) {
                            foreach (var driftProp in alignmentProp.AlignedDriftSpots) {
                                fileClassAndValueDictionary = getFileClassAndValueDictionary(driftProp, fileIdAndFileClassDictionary);
                                calcMinVsMaxRatio(fileClassAndValueDictionary, driftProp);
                            }
                        }
                    }
                }
            }
        }

        private static Dictionary<int, string> getFileIdAndFileClassDictionary(ObservableCollection<AnalysisFileBean> analysisFiles) {
            var fileIdAndFileClassDictionary = new Dictionary<int, string>();
            foreach (var file in analysisFiles) {
                if (file.AnalysisFilePropertyBean.AnalysisFileIncluded && file.AnalysisFilePropertyBean.AnalysisFileType == AnalysisFileType.Sample) {
                    fileIdAndFileClassDictionary[file.AnalysisFilePropertyBean.AnalysisFileId] = file.AnalysisFilePropertyBean.AnalysisFileClass;
                }
            }
            return fileIdAndFileClassDictionary;
        }

        private static Dictionary<string, List<double>> getFileClassAndValueDictionary(AlignmentPropertyBean alignmentPropertyBean, Dictionary<int, string> fileIdAndFileClassDictionary) {
            var fileClassAndValueDictionary = new Dictionary<string, List<double>>();
            foreach (var alignedPeak in alignmentPropertyBean.AlignedPeakPropertyBeanCollection) {
                if (fileIdAndFileClassDictionary.ContainsKey(alignedPeak.FileID)) {
                    var fileClass = fileIdAndFileClassDictionary[alignedPeak.FileID];
                    if (fileClassAndValueDictionary.ContainsKey(fileClass))
                        fileClassAndValueDictionary[fileClass].Add(alignedPeak.Variable);
                    else
                        fileClassAndValueDictionary[fileClass] = new List<double>() { alignedPeak.Variable };
                }
            }
        return fileClassAndValueDictionary;

        }

        private static void calcMinVsMaxRatio(Dictionary<string, List<double>> fileClassAndValueDictionary, AlignmentPropertyBean alignmentProperty, bool isLog2 = false) {
            float minVal = float.MaxValue; float maxVal = float.MinValue;
            foreach (var dict in fileClassAndValueDictionary) {
                var average = dict.Value.Average();
                if (average < minVal) { minVal = (float)average; }
                if (average > maxVal) { maxVal = (float)average; }
            }
            if (minVal < 1) minVal = 1;
            if (isLog2)
                alignmentProperty.FoldChange = (float)(Math.Log(maxVal / minVal, 2));
            else
                alignmentProperty.FoldChange = maxVal / minVal;
        }

        private static void calcAnova(Dictionary<string, List<double>> fileClassAndValueDictionary, AlignmentPropertyBean alignmentProperty) {
            var numClass = fileClassAndValueDictionary.Count;
            var counter = 0;
            double[][] input = new double[numClass][];
            foreach (var dict in fileClassAndValueDictionary) {
                input[counter] = dict.Value.ToArray();
                counter++;
            }
            var oneWayAnova = new OneWayAnova(input);
            alignmentProperty.AnovaPval = (float)oneWayAnova.FTest.PValue;
        }

        private static Dictionary<string, List<double>> getFileClassAndValueDictionary(AlignedDriftSpotPropertyBean alignmentPropertyBean, Dictionary<int, string> fileIdAndFileClassDictionary) {
            var fileClassAndValueDictionary = new Dictionary<string, List<double>>();
            foreach (var alignedPeak in alignmentPropertyBean.AlignedPeakPropertyBeanCollection) {
                if (fileIdAndFileClassDictionary.ContainsKey(alignedPeak.FileID)) {
                    var fileClass = fileIdAndFileClassDictionary[alignedPeak.FileID];
                    if (fileClassAndValueDictionary.ContainsKey(fileClass))
                        fileClassAndValueDictionary[fileClass].Add(alignedPeak.Variable);
                    else
                        fileClassAndValueDictionary[fileClass] = new List<double>() { alignedPeak.Variable };
                }
            }
            return fileClassAndValueDictionary;

        }

        private static void calcMinVsMaxRatio(Dictionary<string, List<double>> fileClassAndValueDictionary, AlignedDriftSpotPropertyBean alignmentProperty, bool isLog2 = false) {
            float minVal = float.MaxValue; float maxVal = float.MinValue;
            foreach (var dict in fileClassAndValueDictionary) {
                var average = dict.Value.Average();
                if (average < minVal) { minVal = (float)average; }
                if (average > maxVal) { maxVal = (float)average; }
            }
            if (minVal < 1) minVal = 1;
            if (isLog2)
                alignmentProperty.FoldChange = (float)(Math.Log(maxVal / minVal, 2));
            else
                alignmentProperty.FoldChange = maxVal / minVal;
        }

        private static void calcAnova(Dictionary<string, List<double>> fileClassAndValueDictionary, AlignedDriftSpotPropertyBean alignmentProperty) {
            var numClass = fileClassAndValueDictionary.Count;
            var counter = 0;
            double[][] input = new double[numClass][];
            foreach (var dict in fileClassAndValueDictionary) {
                input[counter] = dict.Value.ToArray();
                counter++;
            }
            var oneWayAnova = new OneWayAnova(input);
            alignmentProperty.AnovaPval = (float)oneWayAnova.FTest.PValue;
        }

    }
}
