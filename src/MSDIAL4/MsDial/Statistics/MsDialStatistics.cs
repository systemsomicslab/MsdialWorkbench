using Common.BarChart;
using Msdial.Lcms.Dataprocess.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class MsDialStatistics
    {
        private MsDialStatistics() { }

        public static StatisticsObject GetStatisticsObject(MainWindow mainWindow,
            ScaleMethod scale, TransformMethod transform,
            bool isIdentified, bool isAnnotated, bool isUnknown,
            bool isPlsda = false) {

            var files = mainWindow.AnalysisFiles;
            var alignmentResult = mainWindow.FocusedAlignmentResult;
            var solidColors = mainWindow.SolidColorBrushList;
            var project = mainWindow.ProjectProperty;

            var responses = new ObservableCollection<double>();
            var fileIDs = new ObservableCollection<int>();
            var fileNames = new ObservableCollection<string>();
            var fileClasses = new ObservableCollection<string>();
            var fileBrushs = new ObservableCollection<byte[]>();
            var metaboliteIDs = new ObservableCollection<int>();
            var metaboliteNames = new ObservableCollection<string>();
            var metaboliteBrushs = new ObservableCollection<byte[]>();

            for (int i = 0; i < files.Count; i++) {
                var fileProp = files[i].AnalysisFilePropertyBean;
                if (fileProp.AnalysisFileIncluded) {
                    fileNames.Add(fileProp.AnalysisFileName);
                    fileIDs.Add(fileProp.AnalysisFileId);
                    fileClasses.Add(fileProp.AnalysisFileClass);
                    if (isPlsda == true) {
                        if (fileProp.ResponseVariable != 0.0) {
                            responses.Add(1.0);
                        }
                        else {
                            responses.Add(0.0);
                        }
                    }
                    else {
                        responses.Add(fileProp.ResponseVariable);
                    }
                }
            }

            var projectProp = mainWindow.ProjectProperty;
            var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection;
            var globalSpots = DataAccessLcUtility.GetAlignmentPropertyBeanAndAlignedDriftSpotBeanMergedList(alignedSpots);

            for (int i = 0; i < globalSpots.Count; i++) {
                var alignProp = globalSpots[i];
                var internalStandardID = DataAccessLcUtility.GetInternalStanderdId(alignProp);
                var metaboliteName = DataAccessLcUtility.GetMetaboliteNameOfAlignmentObj(alignProp);
                var spotID = DataAccessLcUtility.GetSpotIdOfAlignmentObj(alignProp);
                if (alignProp.GetType() == typeof(AlignmentPropertyBean) &&
                    ((AlignmentPropertyBean)alignProp).AlignedDriftSpots != null && ((AlignmentPropertyBean)alignProp).AlignedDriftSpots.Count > 0) {
                    continue;
                }
                var ontology = DataAccessLcUtility.GetOntologyOfAlignmentObj(alignProp, mainWindow.PostIdentificationTxtDB, mainWindow.MspDB);

                if (i != internalStandardID) {
                    var metName = metaboliteName == null || metaboliteName == string.Empty ? "Unknown" : metaboliteName;
                    if (metName.Contains("(d")) continue; // should be internal standards
                    var color = Colors.Gray;
                    var rgba = new byte[] { color.R, color.G, color.B, color.A }; 
                    if (MetaboliteColorCode.metabolite_colorcode.ContainsKey(ontology))
                    {
                        var colorcode = MetaboliteColorCode.metabolite_colorcode[ontology];
                        var rgb = colorcode.Trim(new char[] { 'r', 'g', 'b', '(', ')' }).Split(',').Select(s => Convert.ToByte(s)).ToArray();
                        Array.Copy(rgb, 0, rgba, 0, 3);
                    }

                    if (isIdentified && !metName.Contains("Unknown") && !metName.Contains("w/o") && !metName.Contains("RIKEN")) {
                        metaboliteNames.Add("ID: " + spotID + "_" + metName);
                        metaboliteIDs.Add(spotID);
                        metaboliteBrushs.Add(rgba);
                    }
                    if (isAnnotated && metName.Contains("w/o")) {
                        metaboliteNames.Add("ID: " + spotID + "_" + metName);
                        metaboliteIDs.Add(spotID);
                        metaboliteBrushs.Add(rgba);
                    }
                    if (isUnknown && (metName.Contains("Unknown") || metName.Contains("RIKEN"))) {
                        metaboliteNames.Add("ID: " + spotID + "_" + metName);
                        metaboliteIDs.Add(spotID);
                        metaboliteBrushs.Add(rgba);
                    }
                }
            }

            //var classId_SolidColorBrush_Dictionary = GetClassIdColorDictionary(files, solidColors);
            var classnameToBytes = project.ClassnameToColorBytes;
            var classnameToBrushes = ConvertToSolidBrushDictionary(classnameToBytes);
            for (int i = 0; i < files.Count; i++) {
                if (files[i].AnalysisFilePropertyBean.AnalysisFileIncluded) {
                    var brush = classnameToBrushes[files[i].AnalysisFilePropertyBean.AnalysisFileClass];
                    var rgba = new byte[] { brush.Color.R, brush.Color.G, brush.Color.B, brush.Color.A };
                    fileBrushs.Add(rgba);
                }
            }

            if (fileNames.Count == 0 || metaboliteNames.Count == 0) {
                MessageBox.Show("No information available for statistical analysis", "Error", MessageBoxButton.OK);
                return null;
            }

            var dataArray = new double[fileNames.Count, metaboliteNames.Count];
            int counterSample, counterMetabolite = 0;
            for (int i = 0; i < globalSpots.Count; i++) {
                var alignProp = globalSpots[i];
                var spotID = DataAccessLcUtility.GetSpotIdOfAlignmentObj(alignProp);
                if (!metaboliteIDs.Contains(spotID)) continue;
                counterSample = 0;
                var properties = DataAccessLcUtility.GetAlignedPeakPropertyBeanCollection(alignProp);
                for (int j = 0; j < files.Count; j++) {
                    if (files[j].AnalysisFilePropertyBean.AnalysisFileIncluded == true) {
                        dataArray[counterSample, counterMetabolite] = properties[j].NormalizedVariable;
                        counterSample++;
                    }
                }
                counterMetabolite++;
            }

            var statObject = new StatisticsObject() {
                YVariables = responses.ToArray(),
                XDataMatrix = dataArray,
                YIndexes = fileIDs,
                XIndexes = metaboliteIDs,
                YLabels = fileNames,
                YLabels2 = fileClasses,
                XLabels = metaboliteNames,
                YColors = fileBrushs,
                XColors = metaboliteBrushs,
                Scale = scale,
                Transform = transform
            };
            statObject.StatInitialization();
            return statObject;
        }

        public static void PartialLeastSquares(MainWindow mainWindow, ScaleMethod scale, TransformMethod transform,
            bool isAutofit, int componentNumber, bool isIdentified, bool isAnnotated, bool isUnknown, MultivariateAnalysisOption plsOption) {
            var isPlsda = plsOption == MultivariateAnalysisOption.Oplsda || plsOption == MultivariateAnalysisOption.Plsda ? true : false;
            var statObject = GetStatisticsObject(mainWindow, scale, transform, isIdentified, isAnnotated, isUnknown, isPlsda);
            if (statObject == null) return;
            var component = isAutofit == true ? -1 : componentNumber;
            mainWindow.MultivariateAnalysisResult = StatisticsMathematics.PartialLeastSquares(statObject, plsOption, component);
        }

        public static void PrincipalComponentAnalysis(MainWindow mainWindow, ScaleMethod scale, TransformMethod transform,
            bool isIdentified, bool isAnnotated, bool isUnknown, int maxPcNumber = 5)
        {
            var statObject = GetStatisticsObject(mainWindow, scale, transform, isIdentified, isAnnotated, isUnknown);
            if (statObject == null) return;
            mainWindow.MultivariateAnalysisResult = StatisticsMathematics.PrincipalComponentAnalysis(
                statObject, MultivariateAnalysisOption.Pca, maxPcNumber);
        }

        public static void HierarchicalClusteringAnalysis(MainWindow mainWindow, ScaleMethod scale, TransformMethod transform,
            bool isIdentified, bool isAnnotated, bool isUnknown)
        {
            var statObject = GetStatisticsObject(mainWindow, scale, transform, isIdentified, isAnnotated, isUnknown);
            if (statObject == null) return;
            mainWindow.MultivariateAnalysisResult = StatisticsMathematics.HierarchicalClusterAnalysis(statObject);
        }

        public static Dictionary<string, SolidColorBrush> GetClassIdColorDictionary(ObservableCollection<AnalysisFileBean> files, List<SolidColorBrush> brushes)
        {
            Dictionary<string, SolidColorBrush> classId_SolidColorBrush = new Dictionary<string, SolidColorBrush>();

            //Initialize
            int counter = 0;
            string classId = files[0].AnalysisFilePropertyBean.AnalysisFileClass;

            if (counter <= brushes.Count - 1)
                classId_SolidColorBrush[classId] = brushes[counter];
            else
                classId_SolidColorBrush[classId] = brushes[0];

            counter++;

            for (int i = 0; i < files.Count; i++)
            {
                classId = files[i].AnalysisFilePropertyBean.AnalysisFileClass;
                if (!classId_SolidColorBrush.ContainsKey(classId))
                {
                    if (counter <= brushes.Count - 1)
                        classId_SolidColorBrush[classId] = brushes[counter];
                    else
                        classId_SolidColorBrush[classId] = brushes[0];
                    counter++;
                }
            }

            return classId_SolidColorBrush;
        }


        public static List<BasicStats> AverageStdevProperties(AlignmentPropertyBean spotProperty, ObservableCollection<AnalysisFileBean> analysisFiles, 
            ProjectPropertyBean projectProp, BarChartDisplayMode mode, bool isAppliedAnalysisFileIncludedProperty = true) {
           
            var fileClassAndValueDictionary = new Dictionary<string, List<double>>();
            var fileIdAndFileClassDictionary = new Dictionary<int, string>();

            foreach (var file in analysisFiles) {
                fileIdAndFileClassDictionary[file.AnalysisFilePropertyBean.AnalysisFileId] = file.AnalysisFilePropertyBean.AnalysisFileClass;
            }

            fileClassAndValueDictionary = GetFileClassAndValueDictionary(spotProperty, analysisFiles, fileIdAndFileClassDictionary, mode, isAppliedAnalysisFileIncludedProperty);

            var classnameToOrder = projectProp.ClassnameToOrder;
            var statsList = new List<BasicStats>();
            foreach (var dict in fileClassAndValueDictionary) {
                if (dict.Value == null || dict.Value.Count == 0) continue;
                double average = 0, stdev = 0, minValue = 0, twentyfive = 0, median = 0, seventyfive = 0, maxValue = 0;
                calculateChartProperties(dict.Value, out average, out stdev, out minValue,
                    out twentyfive, out median, out seventyfive, out maxValue);

                var stats = new BasicStats() {
                    ID = classnameToOrder[dict.Key],
                    Average = average,
                    Stdev = stdev,
                    Legend = dict.Key,
                    MinValue = minValue,
                    TwentyFiveValue = twentyfive,
                    Median = median,
                    SeventyFiveValue = seventyfive,
                    MaxValue = maxValue
                };
                statsList.Add(stats);
            }

            // reordering by classname to order dict
            statsList = statsList.OrderBy(n => n.ID).ToList();
            return statsList;
        }

        public static List<BasicStats> AverageStdevProperties(AlignedDriftSpotPropertyBean spotProperty, ObservableCollection<AnalysisFileBean> analysisFiles,
            ProjectPropertyBean projectProp, BarChartDisplayMode mode, bool isAppliedAnalysisFileIncludedProperty = true) {

            var fileClassAndValueDictionary = new Dictionary<string, List<double>>();
            var fileIdAndFileClassDictionary = new Dictionary<int, string>();

            foreach (var file in analysisFiles) {
                fileIdAndFileClassDictionary[file.AnalysisFilePropertyBean.AnalysisFileId] = file.AnalysisFilePropertyBean.AnalysisFileClass;
            }

            fileClassAndValueDictionary = GetFileClassAndValueDictionary(spotProperty, analysisFiles, fileIdAndFileClassDictionary, mode, isAppliedAnalysisFileIncludedProperty);

            var classnameToOrder = projectProp.ClassnameToOrder;
            var statsList = new List<BasicStats>();
            foreach (var dict in fileClassAndValueDictionary) {
                if (dict.Value == null || dict.Value.Count == 0) continue;
                double average = 0, stdev = 0, minValue = 0, twentyfive = 0, median = 0, seventyfive = 0, maxValue = 0;
                calculateChartProperties(dict.Value, out average, out stdev, out minValue,
                    out twentyfive, out median, out seventyfive, out maxValue);

                var stats = new BasicStats() {
                    ID = classnameToOrder[dict.Key],
                    Average = average,
                    Stdev = stdev,
                    Legend = dict.Key,
                    MinValue = minValue,
                    TwentyFiveValue = twentyfive,
                    Median = median,
                    SeventyFiveValue = seventyfive,
                    MaxValue = maxValue
                };
                statsList.Add(stats);
            }

            // reordering by classname to order dict
            statsList = statsList.OrderBy(n => n.ID).ToList();
            return statsList;
        }

        public static BarChartBean GetBarChartBean(AlignmentPropertyBean spotProperty, ObservableCollection<AnalysisFileBean> analysisFiles, 
            ProjectPropertyBean projectProp, BarChartDisplayMode mode, bool isBoxPlot) {

            //var fileClassAndBrushDictionary = GetClassIdColorDictionary(analysisFiles, colorBrushes);
            var fileClassAndBytesDictionary = projectProp.ClassnameToColorBytes;
            var fileClassAndBrushDictionary = ConvertToSolidBrushDictionary(fileClassAndBytesDictionary);
            var fileClassAndValueDictionary = new Dictionary<string, List<double>>();
            var fileIdAndFileClassDictionary = new Dictionary<int, string>();

            foreach (var file in analysisFiles) {
                 fileIdAndFileClassDictionary[file.AnalysisFilePropertyBean.AnalysisFileId] = file.AnalysisFilePropertyBean.AnalysisFileClass;
            }

            fileClassAndValueDictionary = GetFileClassAndValueDictionary(spotProperty, analysisFiles, fileIdAndFileClassDictionary, mode);

            var classnameToOrder = projectProp.ClassnameToOrder;
            var barElements = new List<BarElement>();
            foreach (var dict in fileClassAndValueDictionary)
            {
                if (dict.Value == null || dict.Value.Count == 0) continue;
                double average = 0, stdev = 0, minValue = 0, twentyfive = 0, median = 0, seventyfive = 0, maxValue = 0;
                calculateChartProperties(dict.Value, out average, out stdev, out minValue,
                    out twentyfive, out median, out seventyfive, out maxValue);
                if (!classnameToOrder.ContainsKey(dict.Key)) continue;
                var barElement = new BarElement()
                {
                    Id = classnameToOrder[dict.Key],
                    Value = average,
                    Error = stdev,
                    Legend = dict.Key,
                    MinValue = minValue,
                    TwentyFiveValue = twentyfive, 
                    Median = median,
                    SeventyFiveValue = seventyfive,
                    MaxValue = maxValue,
                    IsBoxPlot = isBoxPlot,
                    Brush = fileClassAndBrushDictionary[dict.Key]
                };
                barElements.Add(barElement);
            }

            // reordering by classname to order dict
            barElements = barElements.OrderBy(n => n.Id).ToList();

            if (spotProperty.CentralRetentionTime > 0) {
                var metaboliteName = spotProperty.MetaboliteName == null || spotProperty.MetaboliteName == string.Empty 
                    ? "Unknown" : spotProperty.MetaboliteName;
                var unit = "Intensity";
                if (mode == BarChartDisplayMode.OriginalHeight) {
                    unit = "Height";
                } else if (mode == BarChartDisplayMode.OriginalArea) {
                    unit = "Area";
                } else if (mode == BarChartDisplayMode.NormalizedHeight) {
                    unit = GetAbundanceUnitString(spotProperty.IonAbundanceUnit);
                }
                else if (mode == BarChartDisplayMode.NormalizedArea) {
                    unit = GetAbundanceUnitString(spotProperty.IonAbundanceUnit);
                }

                if (spotProperty.QuantMass > 0)
                    return new BarChartBean(barElements, metaboliteName + 
                        " RT[min]=" + Math.Round(spotProperty.CentralRetentionTime, 3) +
                        " RI=" + Math.Round(spotProperty.CentralRetentionIndex, 2) + 
                        " m/z=" + Math.Round(spotProperty.QuantMass, 4), "Class", unit);
                else
                    return new BarChartBean(barElements, metaboliteName +
                        "RT[min]=" + Math.Round(spotProperty.CentralRetentionTime, 2) + 
                        " m/z=" + Math.Round(spotProperty.CentralAccurateMass, 4), "Class", unit);
            }
            else {
                var unit = spotProperty.IonAbundanceUnit.ToString();
                return new BarChartBean(barElements, spotProperty.MetaboliteName, "Class", unit);
            }
        }

        private static void calculateChartProperties(List<double> value, out double average, out double stdev, out double minValue, out double twentyfive, out double median, out double seventyfive, out double maxValue) {
            average = value.Average();
            stdev = 0.0;
            var ave = average;
            if (value.Count > 1) {
                var sumOfSquares = value.Select(val => (val - ave) * (val - ave)).Sum();
                stdev = Math.Sqrt(sumOfSquares / (double)(value.Count - 1));
            }

            BasicMathematics.BoxPlotProperties(value.ToArray(), out minValue, out twentyfive, out median, out seventyfive, out maxValue);
        }

        public static BarChartBean GetBarChartBean(AlignedDriftSpotPropertyBean spotProperty, ObservableCollection<AnalysisFileBean> analysisFiles,
            ProjectPropertyBean projectProp, BarChartDisplayMode mode, bool isBoxPlot) {
            //var fileClassAndBrushDictionary = GetClassIdColorDictionary(analysisFiles, colorBrushes);
            var fileClassAndBytesDictionary = projectProp.ClassnameToColorBytes;
            var fileClassAndBrushDictionary = ConvertToSolidBrushDictionary(fileClassAndBytesDictionary);
            var fileClassAndValueDictionary = new Dictionary<string, List<double>>();
            var fileIdAndFileClassDictionary = new Dictionary<int, string>();

            foreach (var file in analysisFiles) {
                fileIdAndFileClassDictionary[file.AnalysisFilePropertyBean.AnalysisFileId] = file.AnalysisFilePropertyBean.AnalysisFileClass;
            }

            fileClassAndValueDictionary = GetFileClassAndValueDictionary(spotProperty, analysisFiles, fileIdAndFileClassDictionary, mode);
            var classnameToOrder = projectProp.ClassnameToOrder;
            var barElements = new List<BarElement>();
            foreach (var dict in fileClassAndValueDictionary) {
                if (dict.Value == null || dict.Value.Count == 0) continue;
                double average = 0, stdev = 0, minValue = 0, twentyfive = 0, median = 0, seventyfive = 0, maxValue = 0;
                calculateChartProperties(dict.Value, out average, out stdev, out minValue,
                    out twentyfive, out median, out seventyfive, out maxValue);

                var barElement = new BarElement() {
                    Id = classnameToOrder[dict.Key],
                    Value = average,
                    Error = stdev,
                    Legend = dict.Key,
                    MinValue = minValue,
                    TwentyFiveValue = twentyfive,
                    Median = median,
                    SeventyFiveValue = seventyfive,
                    MaxValue = maxValue,
                    IsBoxPlot = isBoxPlot,
                    Brush = fileClassAndBrushDictionary[dict.Key]
                };
                barElements.Add(barElement);
            }

            // reordering by classname to order dict
            barElements = barElements.OrderBy(n => n.Id).ToList();

            if (spotProperty.CentralDriftTime > 0) {
                var metaboliteName = spotProperty.MetaboliteName == null || spotProperty.MetaboliteName == string.Empty
                    ? "Unknown" : spotProperty.MetaboliteName;
                var unit = "Intensity";
                if (mode == BarChartDisplayMode.OriginalHeight) {
                    unit = "Height";
                }
                else if (mode == BarChartDisplayMode.OriginalArea) {
                    unit = "Area";
                }
                else if (mode == BarChartDisplayMode.NormalizedHeight) {
                    unit = GetAbundanceUnitString(spotProperty.IonAbundanceUnit);
                }
                else if (mode == BarChartDisplayMode.NormalizedArea) {
                    unit = GetAbundanceUnitString(spotProperty.IonAbundanceUnit);
                }
                return new BarChartBean(barElements, metaboliteName +
                        "Mobility=" + Math.Round(spotProperty.CentralDriftTime, 2) +
                        " m/z=" + Math.Round(spotProperty.CentralAccurateMass, 4), "Class", unit);

            }
            else {
                var unit = spotProperty.IonAbundanceUnit.ToString();
                return new BarChartBean(barElements, "Class=" + spotProperty.MetaboliteName, "Class", unit);
            }
        }

        public static IonAbundanceUnit GetAbundanceUnitEnum(string unitString) {

            switch (unitString) {
                case "nmol/μL plasma": return IonAbundanceUnit.nmol_per_microL_plasma;
                case "pmol/μL plasma": return IonAbundanceUnit.pmol_per_microL_plasma;
                case "fmol/μL plasma": return IonAbundanceUnit.fmol_per_microL_plasma;
                case "nmol/mg tissue": return IonAbundanceUnit.nmol_per_mg_tissue;
                case "pmol/mg tissue": return IonAbundanceUnit.pmol_per_mg_tissue;
                case "fmol/mg tissue": return IonAbundanceUnit.fmol_per_mg_tissue;
                case "nmol/10^6 cells": return IonAbundanceUnit.nmol_per_10E6_cells;
                case "pmol/10^6 cells": return IonAbundanceUnit.pmol_per_10E6_cells;
                case "fmol/10^6 cells": return IonAbundanceUnit.fmol_per_10E6_cells;
                case "nmol/individual": return IonAbundanceUnit.nmol_per_individual;
                case "pmol/individual": return IonAbundanceUnit.pmol_per_individual;
                case "fmol/individual": return IonAbundanceUnit.fmol_per_individual;
                case "nmol/μg protein": return IonAbundanceUnit.nmol_per_microG_protein;
                case "pmol/μg protein": return IonAbundanceUnit.pmol_per_microG_protein;
                case "fmol/μg protein": return IonAbundanceUnit.fmol_per_microG_protein;
                default: return IonAbundanceUnit.pmol_per_microL_plasma;
            }
        }

        public static string GetAbundanceUnitString(IonAbundanceUnit unitEnum) {

            switch (unitEnum) {
                case IonAbundanceUnit.nmol_per_microL_plasma: return "nmol/μL plasma";
                case IonAbundanceUnit.pmol_per_microL_plasma: return "pmol/μL plasma";
                case IonAbundanceUnit.fmol_per_microL_plasma: return "fmol/μL plasma";
                case IonAbundanceUnit.nmol_per_mg_tissue: return "nmol/mg tissue";
                case IonAbundanceUnit.pmol_per_mg_tissue: return "pmol/mg tissue";
                case IonAbundanceUnit.fmol_per_mg_tissue: return "fmol/mg tissue";
                case IonAbundanceUnit.nmol_per_10E6_cells: return "nmol/10^6 cells";
                case IonAbundanceUnit.pmol_per_10E6_cells: return "pmol/10^6 cells";
                case IonAbundanceUnit.fmol_per_10E6_cells: return "fmol/10^6 cells";
                case IonAbundanceUnit.nmol_per_individual: return "nmol/individual";
                case IonAbundanceUnit.pmol_per_individual: return "pmol/individual";
                case IonAbundanceUnit.fmol_per_individual: return "fmol/individual";
                case IonAbundanceUnit.nmol_per_microG_protein: return "nmol/μg protein";
                case IonAbundanceUnit.pmol_per_microG_protein: return "pmol/μg protein";
                case IonAbundanceUnit.fmol_per_microG_protein: return "fmol/μg protein";
                case IonAbundanceUnit.NormalizedByInternalStandardPeakHeight: return "Peak height/IS peak";
                case IonAbundanceUnit.NormalizedByQcPeakHeight: return "Peak height/QC peak";
                case IonAbundanceUnit.NormalizedByTIC: return "Peak height/TIC";
                case IonAbundanceUnit.NormalizedByMTIC: return "Peak height/MTIC";
                case IonAbundanceUnit.Height: return "Height";
                case IonAbundanceUnit.Area: return "Area";
                case IonAbundanceUnit.Intensity: return "Intensity";
                default: return "pmol/μL plasma";
            }
        }

        public static Dictionary<string, List<double>> GetFileClassAndValueDictionary(AlignmentPropertyBean spotProperty, ObservableCollection<AnalysisFileBean> analysisFiles,
            Dictionary<int, string> fileIdAndFileClassDictionary, BarChartDisplayMode mode, bool isAppliedAnalysisFileIncludedProperty = true) {
            var fileClassAndValueDictionary = new Dictionary<string, List<double>>();

            if (mode == BarChartDisplayMode.NormalizedHeight) {
                foreach (var alignedPeak in spotProperty.AlignedPeakPropertyBeanCollection) {

                    if (isAppliedAnalysisFileIncludedProperty && !analysisFiles[alignedPeak.FileID].AnalysisFilePropertyBean.AnalysisFileIncluded) continue;
                    var fileClass = fileIdAndFileClassDictionary[alignedPeak.FileID];
                    if (fileClassAndValueDictionary.ContainsKey(fileClass))
                        fileClassAndValueDictionary[fileClass].Add(alignedPeak.NormalizedVariable);
                    else
                        fileClassAndValueDictionary[fileClass] = new List<double>() { alignedPeak.NormalizedVariable };
                }
                return fileClassAndValueDictionary;
            }
            else if (mode == BarChartDisplayMode.OriginalHeight) {
                foreach (var alignedPeak in spotProperty.AlignedPeakPropertyBeanCollection) {

                    if (isAppliedAnalysisFileIncludedProperty && !analysisFiles[alignedPeak.FileID].AnalysisFilePropertyBean.AnalysisFileIncluded) continue;
                    var fileClass = fileIdAndFileClassDictionary[alignedPeak.FileID];
                    if (fileClassAndValueDictionary.ContainsKey(fileClass))
                        fileClassAndValueDictionary[fileClass].Add(alignedPeak.Variable);
                    else
                        fileClassAndValueDictionary[fileClass] = new List<double>() { alignedPeak.Variable };
                }
                return fileClassAndValueDictionary;
            }
            else if (mode == BarChartDisplayMode.OriginalArea) {
                foreach (var alignedPeak in spotProperty.AlignedPeakPropertyBeanCollection) {

                    if (isAppliedAnalysisFileIncludedProperty && !analysisFiles[alignedPeak.FileID].AnalysisFilePropertyBean.AnalysisFileIncluded) continue;
                    var fileClass = fileIdAndFileClassDictionary[alignedPeak.FileID];
                    if (fileClassAndValueDictionary.ContainsKey(fileClass))
                        fileClassAndValueDictionary[fileClass].Add(alignedPeak.Area);
                    else
                        fileClassAndValueDictionary[fileClass] = new List<double>() { alignedPeak.Area };
                }
                return fileClassAndValueDictionary;


            }
            return fileClassAndValueDictionary;
        }

        public static Dictionary<string, List<double>> GetFileClassAndValueDictionary(AlignedDriftSpotPropertyBean spotProperty, ObservableCollection<AnalysisFileBean> analysisFiles,
            Dictionary<int, string> fileIdAndFileClassDictionary, BarChartDisplayMode mode, bool isAppliedAnalysisFileIncludedProperty = true) {
            var fileClassAndValueDictionary = new Dictionary<string, List<double>>();

            if (mode == BarChartDisplayMode.NormalizedHeight) {
                foreach (var alignedPeak in spotProperty.AlignedPeakPropertyBeanCollection) {

                    if (isAppliedAnalysisFileIncludedProperty && !analysisFiles[alignedPeak.FileID].AnalysisFilePropertyBean.AnalysisFileIncluded) continue;
                    var fileClass = fileIdAndFileClassDictionary[alignedPeak.FileID];
                    if (fileClassAndValueDictionary.ContainsKey(fileClass))
                        fileClassAndValueDictionary[fileClass].Add(alignedPeak.NormalizedVariable);
                    else
                        fileClassAndValueDictionary[fileClass] = new List<double>() { alignedPeak.NormalizedVariable };
                }
                return fileClassAndValueDictionary;
            }
            else if (mode == BarChartDisplayMode.OriginalHeight) {
                foreach (var alignedPeak in spotProperty.AlignedPeakPropertyBeanCollection) {

                    if (isAppliedAnalysisFileIncludedProperty && !analysisFiles[alignedPeak.FileID].AnalysisFilePropertyBean.AnalysisFileIncluded) continue;
                    var fileClass = fileIdAndFileClassDictionary[alignedPeak.FileID];
                    if (fileClassAndValueDictionary.ContainsKey(fileClass))
                        fileClassAndValueDictionary[fileClass].Add(alignedPeak.Variable);
                    else
                        fileClassAndValueDictionary[fileClass] = new List<double>() { alignedPeak.Variable };
                }
                return fileClassAndValueDictionary;
            }
            else if (mode == BarChartDisplayMode.OriginalArea) {
                foreach (var alignedPeak in spotProperty.AlignedPeakPropertyBeanCollection) {

                    if (isAppliedAnalysisFileIncludedProperty && !analysisFiles[alignedPeak.FileID].AnalysisFilePropertyBean.AnalysisFileIncluded) continue;
                    var fileClass = fileIdAndFileClassDictionary[alignedPeak.FileID];
                    if (fileClassAndValueDictionary.ContainsKey(fileClass))
                        fileClassAndValueDictionary[fileClass].Add(alignedPeak.Area);
                    else
                        fileClassAndValueDictionary[fileClass] = new List<double>() { alignedPeak.Area };
                }
                return fileClassAndValueDictionary;


            }
            return fileClassAndValueDictionary;
        }

        public static void ClassColorDictionaryInitialization(ObservableCollection<AnalysisFileBean> files, ProjectPropertyBean project, 
            List<SolidColorBrush> colorBrushes) {
            var fileClassAndBrushDictionary = GetClassIdColorDictionary(files, colorBrushes);
            var fileClassAndColorBytesDictionary = ConvertToBytesDictionary(fileClassAndBrushDictionary);
            project.ClassnameToColorBytes = fileClassAndColorBytesDictionary;
            project.ClassnameToOrder = new Dictionary<string, int>();
            var counter = 0;
            foreach (var pair in fileClassAndBrushDictionary) {
                var key = pair.Key;
                project.ClassnameToOrder[key] = counter;
                counter++;
            }
        }

        public static Dictionary<string, List<byte>> ConvertToBytesDictionary(Dictionary<string, SolidColorBrush> classToBrush) {
            var classToBytes = new Dictionary<string, List<byte>>();
            foreach (var pair in classToBrush) {
                var classname = pair.Key;
                var bytes = new List<byte>() { pair.Value.Color.R, pair.Value.Color.G, pair.Value.Color.B, pair.Value.Color.A };
                classToBytes[classname] = bytes;
            }
            return classToBytes;
        }

        public static Dictionary<string, SolidColorBrush> ConvertToSolidBrushDictionary(Dictionary<string, List<byte>> classToBytes) {
            var classToBrush = new Dictionary<string, SolidColorBrush>();
            foreach (var pair in classToBytes) {
                var classname = pair.Key;
                var bytes = pair.Value;
                classToBrush[classname] = new SolidColorBrush() { Color = new Color() { R = bytes[0], G = bytes[1], B = bytes[2], A = bytes[3] }  };
            }
            return classToBrush;
        }
    }
}
