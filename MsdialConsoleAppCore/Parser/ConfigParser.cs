using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rfx.Riken.OsakaUniv;
using System.IO;
using Msdial.Gcms.Dataprocess.Utility;

namespace Riken.Metabolomics.MsdialConsoleApp.Parser
{
    public sealed class ConfigParser
    {
        private ConfigParser() { }

        #region // to get analysisparamOfMsdialGcms
        public static AnalysisParamOfMsdialGcms ReadForGcms(string filepath)
        {
            var param = new AnalysisParamOfMsdialGcms();
            using (var sr = new StreamReader(filepath, Encoding.ASCII))
            {
                while (sr.Peek() > -1)
                {
                    var line = sr.ReadLine();
                    if (line == string.Empty) continue;
                    if (line.Length < 2) continue;
                    if (line[0] == '#') continue;

                    var lineArray = line.Split(':');
                    if (lineArray.Length < 2) continue;

                    var method = lineArray[0].Trim();
                    var value = line.Substring(line.Split(':')[0].Length + 1).Trim();

                    gcmsParamUpdate(param, method, value);
                }
            }
            if (param.AccuracyType == AccuracyType.IsNominal)
            {
                param.MassSliceWidth = 0.5F;
                param.MassAccuracy = 0.5F;
            }
            param.AlignmentReferenceFileID = 0;

            return param;
        }

        private static void gcmsParamUpdate(AnalysisParamOfMsdialGcms param, string method, string value)
        {
            var f = 0.0F;
            var i = 0;
            switch (method)
            {
                //Data type
                case "Data type": 
                    if (value == "Centroid" || value == "Profile") 
                        param.DataType = (DataType)Enum.Parse(typeof(DataType), value, true); 
                        return;

                case "Ion mode": 
                    if (value == "Positive" || value == "Negative") 
                        param.IonMode = (IonMode)Enum.Parse(typeof(IonMode), value, true); 
                        return;

                case "Accuracy type": 
                    if (value == "IsNominal" || value == "IsAccurate") 
                        param.AccuracyType = (AccuracyType)Enum.Parse(typeof(AccuracyType), value, true); 
                        return;
                
                //Data correction
                case "Retention time begin": if (float.TryParse(value, out f)) param.RetentionTimeBegin = f; return;
                case "Retention time end": if (float.TryParse(value, out f)) param.RetentionTimeEnd = f; return;
                case "Mass range begin": if (float.TryParse(value, out f)) param.MassRangeBegin = f; return;
                case "Mass range end": if (float.TryParse(value, out f)) param.MassRangeEnd = f; return;

                //Peak detection param
                case "Smoothing method": 
                    if (value == "SimpleMovingAverage" || value == "LinearWeightedMovingAverage" || value == "SavitzkyGolayFilter" || value == "BinomialFilter")
                        param.SmoothingMethod = (SmoothingMethod)Enum.Parse(typeof(SmoothingMethod), value, true); 
                        return;
                case "Smoothing level": if (int.TryParse(value, out i)) param.SmoothingLevel = i; return;
                case "Average peak width": if (int.TryParse(value, out i)) param.AveragePeakWidth = i; return;
                case "Minimum peak height": if (int.TryParse(value, out i)) param.MinimumAmplitude = i; return;
                case "Mass slice width": if (float.TryParse(value, out f)) param.MassSliceWidth = f; return;
                case "Mass accuracy": if (float.TryParse(value, out f)) param.MassAccuracy = f; return;

                //Deconvolution
                case "Sigma window value": if (float.TryParse(value, out f)) param.SigmaWindowValue = f; return;
                case "Amplitude cut off": if (float.TryParse(value, out f)) param.AmplitudeCutoff = f; return;

                //Identification
                case "MSP file": param.MspFilePath = value; return;
                case "RI index file pathes": param.RiDictionaryFilePath = value; return;
                case "Retention type": 
                    if (value == "RT" || value == "RI") 
                        param.RetentionType = (RetentionType)Enum.Parse(typeof(RetentionType), value, true); 
                        return;
                case "RI compound": 
                    if (value == "Fames" || value == "Alkanes") 
                        param.RiCompoundType = (RiCompoundType)Enum.Parse(typeof(RiCompoundType), value, true); 
                        return;

                case "Retention time tolerance for identification": if (float.TryParse(value, out f)) param.RetentionTimeLibrarySearchTolerance = f; return;
                case "Retention index tolerance for identification": if (float.TryParse(value, out f)) param.RetentionIndexLibrarySearchTolerance = f; return;
                case "EI similarity tolerance for identification": if (float.TryParse(value, out f)) param.EiSimilarityLibrarySearchCutOff = f; return;
                case "Mz tolerance for identification": if (float.TryParse(value, out f)) param.MzLibrarySearchTolerance = f; return;
                case "Identification score cut off": if (float.TryParse(value, out f)) param.IdentificationScoreCutOff = f; return;

                //Alignment parameters setting
                case "Alignment index type": if (value == "RI") param.AlignmentIndexType = AlignmentIndexType.RI; else param.AlignmentIndexType = AlignmentIndexType.RT; return;
                case "Retention time tolerance for alignment": if (float.TryParse(value, out f)) param.RetentionTimeAlignmentTolerance = f; return;
                case "Retention index tolerance for alignment": if (float.TryParse(value, out f)) param.RetentionIndexAlignmentTolerance = f; return;
                case "EI similarity tolerance for alignment": if (float.TryParse(value, out f)) param.EiSimilarityLibrarySearchCutOff = f; return;
                case "Retention time factor for alignment": if (float.TryParse(value, out f)) param.RetentionTimeAlignmentFactor = f; return;
                case "EI similarity factor for alignment": if (float.TryParse(value, out f)) param.EiSimilarityAlignmentFactor = f; return;
                case "Peak count filter": if (float.TryParse(value, out f)) param.PeakCountFilter = f; return;
                //case "QC at least filter": if (value == "TRUE" || value == "FALSE") param.QcAtLeastFilter = bool.Parse(value); return;
                case "Remove feature based on peak height fold-change": if (value == "TRUE" || value == "FALSE") param.IsRemoveFeatureBasedOnPeakHeightFoldChange = bool.Parse(value); return;
                case "Sample max / blank average": if (float.TryParse(value, out f)) param.SampleMaxOverBlankAverage = f; return;
                case "Sample average / blank average": if (float.TryParse(value, out f)) param.SampleAverageOverBlankAverage = f; return;
                case "Keep identified and annotated metabolites": if (value == "TRUE" || value == "FALSE") param.IsKeepIdentifiedMetaboliteFeatures = bool.Parse(value); return;
                case "Replace true zero values with 1/2 of minimum peak height over all samples": if (value == "TRUE" || value == "FALSE") param.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples = bool.Parse(value); return;
          
            }
        }
        #endregion

        #region // to get analysisparam for msdial lcms
        public static AnalysisParametersBean ReadForLcmsParameter(string filepath)
        {
            var param = new AnalysisParametersBean();
            using (var sr = new StreamReader(filepath, Encoding.ASCII))
            {
                while (sr.Peek() > -1)
                {
                    var line = sr.ReadLine();
                    if (line == string.Empty) continue;
                    if (line.Length < 2) continue;
                    if (line[0] == '#') continue;

                    var lineArray = line.Split(':');
                    if (lineArray.Length < 2) continue;

                    var method = lineArray[0].Trim();
                    var value = line.Substring(line.Split(':')[0].Length + 1).Trim();

                    lcmsParamUpdate(param, method, value);
                }
            }
            return param;
        }

        private static void lcmsParamUpdate(AnalysisParametersBean param, string method, string value)
        {
            var f = 0.0F;
            var i = 0;
            switch (method)
            {
                //Data correction
                case "Retention time begin": if (float.TryParse(value, out f)) param.RetentionTimeBegin = f; return;
                case "Retention time end": if (float.TryParse(value, out f)) param.RetentionTimeEnd = f; return;
                case "Mass range begin": if (float.TryParse(value, out f)) param.MassRangeBegin = f; return;
                case "Mass range end": if (float.TryParse(value, out f)) param.MassRangeEnd = f; return;
                case "MS2 mass range begin": if (float.TryParse(value, out f)) param.Ms2MassRangeBegin = f; return;
                case "MS2 mass range end": if (float.TryParse(value, out f)) param.Ms2MassRangeEnd = f; return;

                //Centroid parameters
                case "MS1 tolerance for centroid": if (float.TryParse(value, out f)) param.CentroidMs1Tolerance = f; return;
                case "MS2 tolerance for centroid": if (float.TryParse(value, out f)) param.CentroidMs2Tolerance = f; return;

                //Retentiontime correction
                case "Excute RT correction": if (value == "TRUE" || value == "FALSE") param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExcuteRtCorrection = bool.Parse(value); return;
                case "RT correction with smoothing for RT diff": if (value == "TRUE" || value == "FALSE") param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.doSmoothing = bool.Parse(value); return;
                case "User setting intercept": if (float.TryParse(value, out f)) param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.UserSettingIntercept = f; return;
                case "RT diff calc method": if (value == "SampleMinusSampleAverage" || value == "SampleMinusReference")
                        param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.RtDiffCalcMethod = (Rfx.Riken.OsakaUniv.RetentionTimeCorrection.RtDiffCalcMethod)Enum.Parse(typeof(Rfx.Riken.OsakaUniv.RetentionTimeCorrection.RtDiffCalcMethod), value, true);
                    return;
                case "Interpolation Method":
                    if (value == "Linear")
                        param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.InterpolationMethod = Rfx.Riken.OsakaUniv.RetentionTimeCorrection.InterpolationMethod.Linear;
                    return;
                case "Extrapolation method (begin)":
                    if (value == "UserSetting" || value == "FirstPoint" || value == "LinearExtrapolation")
                        param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExtrapolationMethodBegin = (Rfx.Riken.OsakaUniv.RetentionTimeCorrection.ExtrapolationMethodBegin)Enum.Parse(typeof(Rfx.Riken.OsakaUniv.RetentionTimeCorrection.ExtrapolationMethodBegin), value, true);
                    return;
                case "Extrapolation method (end)":
                    if (value == "LastPoint" || value == "LinearExtrapolation")
                        param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExtrapolationMethodEnd = (Rfx.Riken.OsakaUniv.RetentionTimeCorrection.ExtrapolationMethodEnd)Enum.Parse(typeof(Rfx.Riken.OsakaUniv.RetentionTimeCorrection.ExtrapolationMethodEnd), value, true);
                    return;
                case "iSTD file":
                    if (System.IO.File.Exists(value)) {
                        var error = string.Empty;
                        param.RetentionTimeCorrectionCommon.StandardLibrary = TextLibraryParcer.StandardTextLibraryReader(value, out error);
                        if (error != string.Empty) {
                            Console.WriteLine(error);
                        }
                    }
                    return;

                //Peak detection param
                case "Smoothing method":
                    if (value == "SimpleMovingAverage" || value == "LinearWeightedMovingAverage" || value == "SavitzkyGolayFilter" || value == "BinomialFilter")
                        param.SmoothingMethod = (SmoothingMethod)Enum.Parse(typeof(SmoothingMethod), value, true);
                    return;
                case "Smoothing level": if (int.TryParse(value, out i)) param.SmoothingLevel = i; return;
                case "Minimum peak width": if (int.TryParse(value, out i)) param.MinimumDatapoints = i; return;
                case "Minimum peak height": if (int.TryParse(value, out i)) param.MinimumAmplitude = i; return;
                case "Mass slice width": if (float.TryParse(value, out f)) param.MassSliceWidth = f; return;

                //Deconvolution
                case "Sigma window value": if (float.TryParse(value, out f)) param.SigmaWindowValue = f; return;
                case "Amplitude cut off": if (float.TryParse(value, out f)) param.AmplitudeCutoff = f; return;
                case "Exclude after precursor": if (value.ToUpper() == "FALSE") param.RemoveAfterPrecursor = false; return;

                //Identification
                case "Retention time tolerance for identification": if (float.TryParse(value, out f)) param.RetentionTimeLibrarySearchTolerance = f; return;
                case "Accurate ms1 tolerance for identification": if (float.TryParse(value, out f)) param.Ms1LibrarySearchTolerance = f; return;
                case "Accurate ms2 tolerance for identification": if (float.TryParse(value, out f)) param.Ms2LibrarySearchTolerance = f; return;
                case "Identification score cut off": if (float.TryParse(value, out f)) param.IdentificationScoreCutOff = f; return;
                case "Use retention information for identification score": if (value == "TRUE" || value == "FALSE") param.IsUseRetentionInfoForIdentificationScoring = bool.Parse(value); return;

                //Post identification
                case "Retention time tolerance for post identification": if (float.TryParse(value, out f)) param.RetentionTimeToleranceOfPostIdentification = f; return;
                case "Accurate ms1 tolerance for post identification": if (float.TryParse(value, out f)) param.AccurateMassToleranceOfPostIdentification = f; return;
                case "Post identification score cut off": if (float.TryParse(value, out f)) param.PostIdentificationScoreCutOff = f; return;

                //Alignment parameters setting
                case "Retention time tolerance for alignment": if (float.TryParse(value, out f)) param.RetentionTimeAlignmentTolerance = f; return;
                case "MS1 tolerance for alignment": if (float.TryParse(value, out f)) param.Ms1AlignmentTolerance = f; return;
                case "Retention time factor for alignment": if (float.TryParse(value, out f)) param.RetentionTimeAlignmentFactor = f; return;
                case "MS1 factor for alignment": if (float.TryParse(value, out f)) param.Ms1AlignmentFactor = f; return;
                case "Peak count filter": if (float.TryParse(value, out f)) param.PeakCountFilter = f; return;
                case "QC at least filter": if (value == "TRUE" || value == "FALSE") param.QcAtLeastFilter = bool.Parse(value); return;
                case "Alignment reference file ID": if (int.TryParse(value, out i)) param.AlignmentReferenceFileID = i; return;
                case "Remove feature based on peak height fold-change": if (value == "TRUE" || value == "FALSE") param.IsRemoveFeatureBasedOnPeakHeightFoldChange = bool.Parse(value); return;
                case "Sample max / blank average": if (float.TryParse(value, out f)) param.SampleMaxOverBlankAverage = f; return;
                case "Sample average / blank average": if (float.TryParse(value, out f)) param.SampleAverageOverBlankAverage = f; return;
                case "Keep identified and annotated metabolites": if (value == "TRUE" || value == "FALSE") param.IsKeepIdentifiedMetaboliteFeatures = bool.Parse(value); return;
                case "Replace true zero values with 1/2 of minimum peak height over all samples": if (value == "TRUE" || value == "FALSE") param.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples = bool.Parse(value); return;

                //Isotope tracking setting
                case "Tracking isotope label": if (value == "TRUE" || value == "FALSE") param.TrackingIsotopeLabels = bool.Parse(value); return;
                case "Set fully labeled reference file": if (value == "TRUE" || value == "FALSE") param.SetFullyLabeledReferenceFile = bool.Parse(value); return;
                case "Non labeled reference ID": if (int.TryParse(value, out i)) param.NonLabeledReferenceID = i; return;
                case "Fully labeled reference ID": if (int.TryParse(value, out i)) param.FullyLabeledReferenceID = i; return;
                case "Isotope tracking dictionary ID": if (int.TryParse(value, out i)) param.IsotopeTrackingDictionary.SelectedID = i; return;

                //CorrDec settings
                case "CorrDec excute":
                    if (value.ToUpper() == "FALSE") {
                        if (param.AnalysisParamOfMsdialCorrDec == null) param.AnalysisParamOfMsdialCorrDec = new AnalysisParamOfMsdialCorrDec();
                        param.AnalysisParamOfMsdialCorrDec.CanExcute = false;
                    }
                    return;                    
                case "CorrDec MS2 tolerance": if (param.AnalysisParamOfMsdialCorrDec == null) param.AnalysisParamOfMsdialCorrDec = new AnalysisParamOfMsdialCorrDec();
                    if (float.TryParse(value, out f)) param.AnalysisParamOfMsdialCorrDec.MS2Tolerance = f; return;
                case "CorrDec minimum MS2 peak height":
                    if (param.AnalysisParamOfMsdialCorrDec == null) param.AnalysisParamOfMsdialCorrDec = new AnalysisParamOfMsdialCorrDec();
                    if (int.TryParse(value, out i)) param.AnalysisParamOfMsdialCorrDec.MinMS2Intensity = i; return;
                case "CorrDec minimum number of detected samples":
                    if (param.AnalysisParamOfMsdialCorrDec == null) param.AnalysisParamOfMsdialCorrDec = new AnalysisParamOfMsdialCorrDec();
                    if (int.TryParse(value, out i)) param.AnalysisParamOfMsdialCorrDec.MinNumberOfSample = i; return;
                case "CorrDec exclude highly correlated spots":
                    if (param.AnalysisParamOfMsdialCorrDec == null) param.AnalysisParamOfMsdialCorrDec = new AnalysisParamOfMsdialCorrDec();
                    if (float.TryParse(value, out f)) param.AnalysisParamOfMsdialCorrDec.MinCorr_MS1 = f; return;
                case "CorrDec minimum correlation coefficient (MS2)":
                    if (param.AnalysisParamOfMsdialCorrDec == null) param.AnalysisParamOfMsdialCorrDec = new AnalysisParamOfMsdialCorrDec();
                    if (float.TryParse(value, out f)) param.AnalysisParamOfMsdialCorrDec.MinCorr_MS2 = f; return;
                case "CorrDec margin 1 (target precursor)":
                    if (param.AnalysisParamOfMsdialCorrDec == null) param.AnalysisParamOfMsdialCorrDec = new AnalysisParamOfMsdialCorrDec();
                    if (float.TryParse(value, out f)) param.AnalysisParamOfMsdialCorrDec.CorrDiff_MS1 = f; return;
                case "CorrDec margin 2 (coeluted precursor)":
                    if (param.AnalysisParamOfMsdialCorrDec == null) param.AnalysisParamOfMsdialCorrDec = new AnalysisParamOfMsdialCorrDec();
                    if (float.TryParse(value, out f)) param.AnalysisParamOfMsdialCorrDec.CorrDiff_MS2 = f; return;
                case "CorrDec minimum detected rate":
                    if (param.AnalysisParamOfMsdialCorrDec == null) param.AnalysisParamOfMsdialCorrDec = new AnalysisParamOfMsdialCorrDec();
                    if (float.TryParse(value, out f)) param.AnalysisParamOfMsdialCorrDec.MinDetectedPercentToVisualize = f; return;
                case "CorrDec minimum MS2 relative intensity":
                    if (param.AnalysisParamOfMsdialCorrDec == null) param.AnalysisParamOfMsdialCorrDec = new AnalysisParamOfMsdialCorrDec();
                    if (float.TryParse(value, out f)) param.AnalysisParamOfMsdialCorrDec.MinMS2RelativeIntensity = f; return;
                case "CorrDec remove peaks larger than precursor":
                    if (param.AnalysisParamOfMsdialCorrDec == null) param.AnalysisParamOfMsdialCorrDec = new AnalysisParamOfMsdialCorrDec();
                    if (value == "TRUE" || value == "FALSE") param.AnalysisParamOfMsdialCorrDec.RemoveAfterPrecursor = bool.Parse(value); return;

            }
        }


        public static void ReadAdductIonInfo(List<AdductIonInformationBean> adductList, string filepath)
        {
            using (var sr = new StreamReader(filepath, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) continue;
                    if (line.Length < 2) continue;
                    if (line[0] == '#') continue;

                    var lineArray = line.Split(':');
                    if (lineArray.Length < 2) continue;

                    var method = lineArray[0].Trim();
                    var value = line.Substring(line.Split(':')[0].Length + 1).Trim();

                    if (method == "Adduct list") {
                        var adductStrings = value.Split(',').ToList();
                        foreach (var adduct in adductList) {
                            foreach (var adductString in adductStrings) {
                                if (adduct.AdductName == adductString) {
                                    adduct.Included = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region // to make projectpropertybean for lcms project
        public static ProjectPropertyBean ReadForLcmsProjectProperty(string filepath, string inputFolder)
        {
            var dt = DateTime.Now;
            var projectFile = "Project-" + dt.Year.ToString() + dt.Month.ToString() + dt.Day.ToString() + dt.Hour.ToString() + dt.Minute.ToString();
            var projectProp = new ProjectPropertyBean() {
                ProjectDate = dt, ProjectFolderPath = inputFolder, ProjectFilePath = Path.Combine(inputFolder, projectFile + ".mtd")
            };

            using (var sr = new StreamReader(filepath, Encoding.ASCII))
            {
                while (sr.Peek() > -1)
                {
                    var line = sr.ReadLine();
                    if (line == string.Empty) continue;
                    if (line.Length < 2) continue;
                    if (line[0] == '#') continue;

                    var lineArray = line.Split(':');
                    if (lineArray.Length < 2) continue;

                    var method = lineArray[0].Trim();
                    var value = line.Substring(line.Split(':')[0].Length + 1).Trim();

                    projectPropertyUpdate(projectProp, method, value);
                }
            }

            return projectProp;
        }

        private static void projectPropertyUpdate(ProjectPropertyBean projectProp, string method, string value)
        {
            switch (method) {
                //Data type
                case "MS1 data type":
                    if (value == "Centroid" || value == "Profile")
                        projectProp.DataType = (DataType)Enum.Parse(typeof(DataType), value, true);
                    return;

                case "MS2 data type":
                    if (value == "Centroid" || value == "Profile")
                        projectProp.DataTypeMS2 = (DataType)Enum.Parse(typeof(DataType), value, true);
                    return;

                case "Ion mode":
                    if (value == "Positive" || value == "Negative")
                        projectProp.IonMode = (IonMode)Enum.Parse(typeof(IonMode), value, true);
                    return;

                //File paths
                case "DIA file": projectProp.ExperimentFilePath = value; return;
                case "MSP file": projectProp.LibraryFilePath = value; return;
                case "Text file": projectProp.PostIdentificationLibraryFilePath = value; return;
                case "Target compound file": projectProp.CompoundListInTargetModePath = value; return;

                // Private version
                case "Is private version of Tada":
                    if (value.ToUpper() == "TRUE")
                        projectProp.IsLabPrivateVersionTada = true;                    
                    return;
            }
        }
        #endregion
    }
}
