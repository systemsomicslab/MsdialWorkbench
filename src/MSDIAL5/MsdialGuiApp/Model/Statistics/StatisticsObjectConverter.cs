using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Mathematics.Statistics;
using CompMs.Common.Ontology;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;


namespace CompMs.App.Msdial.Model.Statistics {
    public sealed class StatisticsObjectConverter {
        private StatisticsObjectConverter() { }

        public static StatisticsObject? GetStatisticsObject(
            IReadOnlyList<AnalysisFileBean> files,
            IReadOnlyList<AlignmentSpotPropertyModel> alignedSpots,
            ParameterBase parameter,
            IMatchResultEvaluator<MsScanMatchResult> evaluator, ref ObservableCollection<AlignmentSpotPropertyModel> observableSpots) {

            var responses = new ObservableCollection<double>();
            var fileIDs = new ObservableCollection<int>();
            var fileNames = new ObservableCollection<string>();
            var fileClasses = new ObservableCollection<string>();
            var fileBrushs = new ObservableCollection<byte[]>();
            var metaboliteIDs = new ObservableCollection<int>();
            var metaboliteNames = new ObservableCollection<string>();
            var metaboliteBrushs = new ObservableCollection<byte[]>();
            var statsparam = parameter.StatisticsBaseParam;
            var methodoption = statsparam.MultivariateAnalysisOption;

            var isPlsda = methodoption == MultivariateAnalysisOption.Plsda || methodoption == MultivariateAnalysisOption.Oplsda ? true : false;
            for (int i = 0; i < files.Count; i++) {
                var fileProp = files[i];
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

            //var globalSpots = DataAccess.GetGlobalAlignmentSpotProperties4Statistics(alignedSpots);
            var globalSpots = alignedSpots;
            var isIdentified = parameter.StatisticsBaseParam.IsIdentifiedImportedInStatistics;
            var isAnnotated = parameter.StatisticsBaseParam.IsAnnotatedImportedInStatistics;
            var isUnknown = parameter.StatisticsBaseParam.IsUnknownImportedInStatistics;
            //observableSpots = new ObservableCollection<AlignmentSpotPropertyModel>();
            for (int i = 0; i < globalSpots.Count; i++) {
                var alignProp = globalSpots[i];
                var internalStandardID = alignProp.innerModel.InternalStandardAlignmentID;
                var metaboliteName = alignProp.Name;
                var spotID = alignProp.MasterAlignmentID;
                var ontology = alignProp.Ontology;

                if (i != internalStandardID) {
                    var metName = metaboliteName == null || metaboliteName == string.Empty ? "Unknown" : metaboliteName;
                    if (metName.Contains("(d")) continue; // should be internal standards
                    var rgba = new byte[] { 128, 128, 128, 255 }; // gray
                    if (!(ontology is null) && ChemOntologyColorConverter.Ontology2RgbString.ContainsKey(ontology)) {
                        var colorcode = ChemOntologyColorConverter.Ontology2RgbString[ontology];
                        var rgb = colorcode.Trim(new char[] { 'r', 'g', 'b', '(', ')' }).Split(',').Select(s => Convert.ToByte(s)).ToArray();
                        Array.Copy(rgb, 0, rgba, 0, 3);
                    }

                    if (isIdentified && evaluator.IsReferenceMatched(alignProp.MatchResultsModel.Representative)) {
                        metaboliteNames.Add("ID: " + spotID + "_" + metName);
                        metaboliteIDs.Add(spotID);
                        metaboliteBrushs.Add(rgba);
                        observableSpots.Add(alignProp);
                    }
                    if (isAnnotated && evaluator.IsAnnotationSuggested(alignProp.MatchResultsModel.Representative)) {
                        metaboliteNames.Add("ID: " + spotID + "_" + metName);
                        metaboliteIDs.Add(spotID);
                        metaboliteBrushs.Add(rgba);
                        observableSpots.Add(alignProp);
                    }
                    if (isUnknown && !evaluator.IsReferenceMatched(alignProp.MatchResultsModel.Representative) && !evaluator.IsAnnotationSuggested(alignProp.MatchResultsModel.Representative)) {
                        metaboliteNames.Add("ID: " + spotID + "_" + metName);
                        metaboliteIDs.Add(spotID);
                        metaboliteBrushs.Add(rgba);
                        observableSpots.Add(alignProp);
                    }
                }
            }

            var classnameToBytes = parameter.ClassnameToColorBytes;
            for (int i = 0; i < files.Count; i++) {
                if (files[i].AnalysisFileIncluded) {
                    var brush = classnameToBytes[files[i].AnalysisFileClass];
                    fileBrushs.Add(brush.ToArray());
                }
            }

            if (fileNames.Count == 0 || metaboliteNames.Count == 0) {
                Console.WriteLine("No information available for statistical analysis");
                return null;
            }

            var dataArray = new double[fileNames.Count, metaboliteNames.Count];
            int counterSample, counterMetabolite = 0;
            for (int i = 0; i < globalSpots.Count; i++) {
                var alignProp = globalSpots[i];
                var spotID = alignProp.MasterAlignmentID;
                if (!metaboliteIDs.Contains(spotID)) continue;
                counterSample = 0;
                var properties = alignProp.AlignedPeakPropertiesModelProperty.Value;
                if (properties is null) {
                    continue;
                }
                for (int j = 0; j < files.Count; j++) {
                    if (files[j].AnalysisFileIncluded) {
                        dataArray[counterSample, counterMetabolite] = properties[j].NormalizedPeakHeight;
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
                Scale = statsparam.Scale,
                Transform = statsparam.Transform
            };
            statObject.StatInitialization();
            return statObject;
        }

        public static MultivariateAnalysisResult? PartialLeastSquares(
            IReadOnlyList<AnalysisFileBean> files,
            IReadOnlyList<AlignmentSpotPropertyModel> alignedSpots,
            ParameterBase parameter,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            ref ObservableCollection<AlignmentSpotPropertyModel> observableSpots
            ) {
            var statsparam = parameter.StatisticsBaseParam;
            var plsOption = statsparam.MultivariateAnalysisOption;
            var statObject = GetStatisticsObject(files, alignedSpots, parameter, evaluator, ref observableSpots);
            if (statObject == null) return null;
            var component = statsparam.IsAutoFitPls == true ? -1 : statsparam.ComponentPls;
            return StatisticsMathematics.PartialLeastSquares(statObject, plsOption, component);
        }

        public static MultivariateAnalysisResult? PrincipalComponentAnalysis(
            IReadOnlyList<AnalysisFileBean> files,
            IReadOnlyList<AlignmentSpotPropertyModel> alignedSpots,
            ParameterBase parameter,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            ref ObservableCollection<AlignmentSpotPropertyModel> observableSpots
            ) {
            var statsparam = parameter.StatisticsBaseParam;
            var statObject = GetStatisticsObject(files, alignedSpots, parameter, evaluator, ref observableSpots);
            if (statObject == null) return null;
            return StatisticsMathematics.PrincipalComponentAnalysis(
                statObject, MultivariateAnalysisOption.Pca, statsparam.MaxComponent);
        }

        public static MultivariateAnalysisResult? HierarchicalClusteringAnalysis(
            IReadOnlyList<AnalysisFileBean> files,
            IReadOnlyList<AlignmentSpotPropertyModel> alignedSpots,
            ParameterBase parameter,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            ref ObservableCollection<AlignmentSpotPropertyModel> observableSpots
            ) {
            var statsparam = parameter.StatisticsBaseParam;
            var statObject = GetStatisticsObject(files, alignedSpots, parameter, evaluator, ref observableSpots);
            if (statObject == null) return null;
            return StatisticsMathematics.HierarchicalClusterAnalysis(statObject);
        }
    }
}
