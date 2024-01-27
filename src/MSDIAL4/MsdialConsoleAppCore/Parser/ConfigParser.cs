using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rfx.Riken.OsakaUniv;
using System.IO;
using Msdial.Gcms.Dataprocess.Utility;
using MsdialConsoleAppCore.Properties;
using CompMs.RawDataHandler.Core;

namespace Riken.Metabolomics.MsdialConsoleApp.Parser
{
    public sealed class ConfigParser
    {
        private ConfigParser() { }

        #region // to get analysisparamOfMsdialGcms
        public static AnalysisParamOfMsdialGcms ReadForGcms(string filepath)
        {
            var param = new AnalysisParamOfMsdialGcms() { MsdialVersionNumber = Resources.VERSION };
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

                    gcmsParamUpdate(param, method.ToLower(), value);
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
                case "data type": 
                    if (value == "Centroid" || value == "Profile") 
                        param.DataType = (DataType)Enum.Parse(typeof(DataType), value, true); 
                        return;

                case "ion mode": 
                    if (value == "Positive" || value == "Negative") 
                        param.IonMode = (IonMode)Enum.Parse(typeof(IonMode), value, true); 
                        return;

                case "accuracy type": 
                    if (value == "IsNominal" || value == "IsAccurate") 
                        param.AccuracyType = (AccuracyType)Enum.Parse(typeof(AccuracyType), value, true); 
                        return;
                
                //Data correction
                case "retention time begin": if (float.TryParse(value, out f)) param.RetentionTimeBegin = f; return;
                case "retention time end": if (float.TryParse(value, out f)) param.RetentionTimeEnd = f; return;
                case "mass range begin": if (float.TryParse(value, out f)) param.MassRangeBegin = f; return;
                case "mass range end": if (float.TryParse(value, out f)) param.MassRangeEnd = f; return;

                //Peak detection param
                case "smoothing method": 
                    if (value == "SimpleMovingAverage" || value == "LinearWeightedMovingAverage" || value == "SavitzkyGolayFilter" || value == "BinomialFilter")
                        param.SmoothingMethod = (SmoothingMethod)Enum.Parse(typeof(SmoothingMethod), value, true); 
                        return;
                case "smoothing level": if (int.TryParse(value, out i)) param.SmoothingLevel = i; return;
                case "average peak width": if (int.TryParse(value, out i)) param.AveragePeakWidth = i; return;
                case "minimum peak height": if (int.TryParse(value, out i)) param.MinimumAmplitude = i; return;
                case "mass slice width": if (float.TryParse(value, out f)) param.MassSliceWidth = f; return;
                case "mass accuracy": if (float.TryParse(value, out f)) param.MassAccuracy = f; return;

                //Deconvolution
                case "sigma window value": if (float.TryParse(value, out f)) param.SigmaWindowValue = f; return;
                case "amplitude cut off": if (float.TryParse(value, out f)) param.AmplitudeCutoff = f; return;

                //Identification
                case "msp file": param.MspFilePath = value; return;
                case "ri index file pathes": param.RiDictionaryFilePath = value; return;
                case "retention type": 
                    if (value == "RT" || value == "RI") 
                        param.RetentionType = (RetentionType)Enum.Parse(typeof(RetentionType), value, true); 
                        return;
                case "ri compound": 
                    if (value == "Fames" || value == "Alkanes") 
                        param.RiCompoundType = (RiCompoundType)Enum.Parse(typeof(RiCompoundType), value, true); 
                        return;

                case "retention time tolerance for identification": if (float.TryParse(value, out f)) param.RetentionTimeLibrarySearchTolerance = f; return;
                case "retention index tolerance for identification": if (float.TryParse(value, out f)) param.RetentionIndexLibrarySearchTolerance = f; return;
                case "ei similarity tolerance for identification": if (float.TryParse(value, out f)) param.EiSimilarityLibrarySearchCutOff = f; return;
                case "mz tolerance for identification": if (float.TryParse(value, out f)) param.MzLibrarySearchTolerance = f; return;
                case "identification score cut off": if (float.TryParse(value, out f)) param.IdentificationScoreCutOff = f; return;
                case "use retention information for identification scoring": if (value.ToUpper() == "TRUE" || value.ToUpper() == "FALSE") param.IsUseRetentionInfoForIdentificationScoring = bool.Parse(value); return;
                case "use retention information for identification filtering": if (value.ToUpper() == "TRUE" || value.ToUpper() == "FALSE") param.IsUseRetentionInfoForIdentificationFiltering = bool.Parse(value); return;
                case "only report top hit": if (value.ToUpper() == "TRUE" || value.ToUpper() == "FALSE") param.IsOnlyTopHitReport = bool.Parse(value); return;

                //Alignment parameters setting
                case "alignment index type": if (value == "RI") param.AlignmentIndexType = AlignmentIndexType.RI; else param.AlignmentIndexType = AlignmentIndexType.RT; return;
                case "retention time tolerance for alignment": if (float.TryParse(value, out f)) param.RetentionTimeAlignmentTolerance = f; return;
                case "retention index tolerance for alignment": if (float.TryParse(value, out f)) param.RetentionIndexAlignmentTolerance = f; return;
                case "ei similarity tolerance for alignment": if (float.TryParse(value, out f)) param.EiSimilarityLibrarySearchCutOff = f; return;
                case "retention time factor for alignment": if (float.TryParse(value, out f)) param.RetentionTimeAlignmentFactor = f; return;
                case "ei similarity factor for alignment": if (float.TryParse(value, out f)) param.EiSimilarityAlignmentFactor = f; return;
                case "peak count filter": if (float.TryParse(value, out f)) param.PeakCountFilter = f; return;
                //case "QC at least filter": if (value == "TRUE" || value == "FALSE") param.QcAtLeastFilter = bool.Parse(value); return;
                case "remove feature based on peak height fold-change": if (value.ToUpper() == "TRUE" || value.ToUpper() == "FALSE") param.IsRemoveFeatureBasedOnPeakHeightFoldChange = bool.Parse(value); return;
                case "n% detected in at least one group": if (float.TryParse(value, out f)) param.NPercentDetectedInOneGroup = f; return;
                case "sample max / blank average": if (float.TryParse(value, out f)) param.SampleMaxOverBlankAverage = f; return;
                case "sample average / blank average": if (float.TryParse(value, out f)) param.SampleAverageOverBlankAverage = f; return;
                case "keep identified and annotated metabolites": if (value.ToUpper() == "TRUE" || value.ToUpper() == "FALSE") param.IsKeepIdentifiedMetaboliteFeatures = bool.Parse(value); return;
                case "keep removable features and assign the tag for checking": if (value.ToUpper() == "TRUE" || value.ToUpper() == "FALSE") param.IsKeepRemovableFeaturesAndAssignedTagForChecking = bool.Parse(value); return;
                case "replace true zero values with 1/10 of minimum peak height over all samples": if (value.ToUpper() == "TRUE" || value.ToUpper() == "FALSE") param.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples = bool.Parse(value); return;

            }
        }
        #endregion

        #region // to get analysisparam for msdial lcms
        public static AnalysisParametersBean ReadForLcmsParameter(string filepath)
        {
            var param = new AnalysisParametersBean() { MsdialVersionNumber = Resources.VERSION }; 
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

                    lcmsParamUpdate(param, method.ToLower(), value);
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
                case "retention time begin": if (float.TryParse(value, out f)) param.RetentionTimeBegin = f; return;
                case "retention time end": if (float.TryParse(value, out f)) param.RetentionTimeEnd = f; return;
                case "mass range begin": if (float.TryParse(value, out f)) param.MassRangeBegin = f; return;
                case "mass range end": if (float.TryParse(value, out f)) param.MassRangeEnd = f; return;
                case "ms2 mass range begin": if (float.TryParse(value, out f)) param.Ms2MassRangeBegin = f; return;
                case "ms2 mass range end": if (float.TryParse(value, out f)) param.Ms2MassRangeEnd = f; return;

                //Centroid parameters
                case "ms1 tolerance for centroid": if (float.TryParse(value, out f)) param.CentroidMs1Tolerance = f; return;
                case "ms2 tolerance for centroid": if (float.TryParse(value, out f)) param.CentroidMs2Tolerance = f; return;

                //Isotope
                case "maximum charged number": if (int.TryParse(value, out i)) param.MaxChargeNumber = i; return;

                // max number of CPU threads
                case "number of threads": if (int.TryParse(value, out i)) param.NumThreads = i; Console.WriteLine("Asked for {0} threads", i); return;

                //Retentiontime correction
                case "excute rt correction": if (value.ToUpper() == "TRUE" || value.ToUpper() == "FALSE") param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExcuteRtCorrection = bool.Parse(value); return;
                case "rt correction with smoothing for rt diff": if (value.ToUpper() == "TRUE" || value.ToUpper() == "FALSE") param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.doSmoothing = bool.Parse(value); return;
                case "user setting intercept": if (float.TryParse(value, out f)) param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.UserSettingIntercept = f; return;
                case "rt diff calc method": if (value == "SampleMinusSampleAverage" || value == "SampleMinusReference")
                        param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.RtDiffCalcMethod = (Rfx.Riken.OsakaUniv.RetentionTimeCorrection.RtDiffCalcMethod)Enum.Parse(typeof(Rfx.Riken.OsakaUniv.RetentionTimeCorrection.RtDiffCalcMethod), value, true);
                    return;
                case "interpolation method":
                    if (value == "Linear")
                        param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.InterpolationMethod = Rfx.Riken.OsakaUniv.RetentionTimeCorrection.InterpolationMethod.Linear;
                    return;
                case "extrapolation method (begin)":
                    if (value == "UserSetting" || value == "FirstPoint" || value == "LinearExtrapolation")
                        param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExtrapolationMethodBegin = (Rfx.Riken.OsakaUniv.RetentionTimeCorrection.ExtrapolationMethodBegin)Enum.Parse(typeof(Rfx.Riken.OsakaUniv.RetentionTimeCorrection.ExtrapolationMethodBegin), value, true);
                    return;
                case "extrapolation method (end)":
                    if (value == "lastpoint" || value == "LinearExtrapolation")
                        param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExtrapolationMethodEnd = (Rfx.Riken.OsakaUniv.RetentionTimeCorrection.ExtrapolationMethodEnd)Enum.Parse(typeof(Rfx.Riken.OsakaUniv.RetentionTimeCorrection.ExtrapolationMethodEnd), value, true);
                    return;
                case "istd file":
                    if (System.IO.File.Exists(value)) {
                        var error = string.Empty;
                        param.RetentionTimeCorrectionCommon.StandardLibrary = TextLibraryParcer.StandardTextLibraryReader(value, out error);
                        if (error != string.Empty) {
                            Console.WriteLine(error);
                        }
                    }
                    return;

                //Peak detection param
                case "smoothing method":
                    if (value == "SimpleMovingAverage" || value == "LinearWeightedMovingAverage" || value == "SavitzkyGolayFilter" || value == "BinomialFilter")
                        param.SmoothingMethod = (SmoothingMethod)Enum.Parse(typeof(SmoothingMethod), value, true);
                    return;
                case "smoothing level": if (int.TryParse(value, out i)) param.SmoothingLevel = i; return;
                case "minimum peak width": if (int.TryParse(value, out i)) param.MinimumDatapoints = i; return;
                case "minimum peak height": if (int.TryParse(value, out i)) param.MinimumAmplitude = i; return;
                case "mass slice width": if (float.TryParse(value, out f)) param.MassSliceWidth = f; return;

                //Deconvolution
                case "sigma window value": if (float.TryParse(value, out f)) param.SigmaWindowValue = f; return;
                case "amplitude cut off": if (float.TryParse(value, out f)) param.AmplitudeCutoff = f; return;
                case "exclude after precursor": if (value.ToUpper() == "TRUE" || value.ToUpper() == "FALSE") param.RemoveAfterPrecursor = bool.Parse(value); return; 
                case "keep isotope until": if (float.TryParse(value, out f)) param.KeptIsotopeRange = f; return;
                case "keep original precursor isotopes": if (value.ToUpper() == "TRUE" || value.ToUpper() == "FALSE") param.KeepOriginalPrecursorIsotopes = bool.Parse(value); return;

                //Identification
                case "retention time tolerance for identification": if (float.TryParse(value, out f)) param.RetentionTimeLibrarySearchTolerance = f; return;
                case "accurate ms1 tolerance for identification": if (float.TryParse(value, out f)) param.Ms1LibrarySearchTolerance = f; return;
                case "accurate ms2 tolerance for identification": if (float.TryParse(value, out f)) param.Ms2LibrarySearchTolerance = f; return;
                case "identification score cut off": if (float.TryParse(value, out f)) param.IdentificationScoreCutOff = f; return;
                case "use retention information for identification scoring": if (value.ToUpper() == "TRUE" || value.ToUpper() == "FALSE") param.IsUseRetentionInfoForIdentificationScoring = bool.Parse(value); return;
                case "use retention information for identification filtering": if (value.ToUpper() == "TRUE" || value.ToUpper() == "FALSE") param.IsUseRetentionInfoForIdentificationFiltering = bool.Parse(value); return;

                //Post identification
                case "retention time tolerance for post identification": if (float.TryParse(value, out f)) param.RetentionTimeToleranceOfPostIdentification = f; return;
                case "accurate ms1 tolerance for post identification": if (float.TryParse(value, out f)) param.AccurateMassToleranceOfPostIdentification = f; return;
                case "post identification score cut off": if (float.TryParse(value, out f)) param.PostIdentificationScoreCutOff = f; return;

                //Alignment parameters setting
                case "retention time tolerance for alignment": if (float.TryParse(value, out f)) param.RetentionTimeAlignmentTolerance = f; return;
                case "ms1 tolerance for alignment": if (float.TryParse(value, out f)) param.Ms1AlignmentTolerance = f; return;
                case "retention time factor for alignment": if (float.TryParse(value, out f)) param.RetentionTimeAlignmentFactor = f; return;
                case "ms1 factor for alignment": if (float.TryParse(value, out f)) param.Ms1AlignmentFactor = f; return;
                case "peak count filter": if (float.TryParse(value, out f)) param.PeakCountFilter = f; return;
                case "gap filling by compulsion": if (value.ToUpper() == "TRUE" || value.ToUpper() == "FALSE") param.IsForceInsertForGapFilling = bool.Parse(value); return;
                //case "qc at least filter": if (value.ToUpper() == "TRUE" || value.ToUpper() == "FALSE") param.QcAtLeastFilter = bool.Parse(value); return;
                case "alignment reference file id": if (int.TryParse(value, out i)) param.AlignmentReferenceFileID = i; return;
                case "remove feature based on peak height fold-change": if (value.ToUpper() == "TRUE" || value.ToUpper() == "FALSE") param.IsRemoveFeatureBasedOnPeakHeightFoldChange = bool.Parse(value); return;
                case "n% detected in at least one group": if (float.TryParse(value, out f)) param.NPercentDetectedInOneGroup = f; return;
                case "sample max / blank average": if (float.TryParse(value, out f)) param.SampleMaxOverBlankAverage = f; return;
                case "sample average / blank average": if (float.TryParse(value, out f)) param.SampleAverageOverBlankAverage = f; return;
                case "keep identified and annotated metabolites": if (value.ToUpper() == "TRUE" || value.ToUpper() == "FALSE") param.IsKeepIdentifiedMetaboliteFeatures = bool.Parse(value); return;
                case "keep removable features and assign the tag for checking": if (value.ToUpper() == "TRUE" || value.ToUpper() == "FALSE") param.IsKeepRemovableFeaturesAndAssignedTagForChecking = bool.Parse(value); return;
                case "replace true zero values with 1/10 of minimum peak height over all samples": if (value.ToUpper() == "TRUE" || value.ToUpper() == "FALSE") param.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples = bool.Parse(value); return;

                //Isotope tracking setting
                case "tracking isotope label": if (value.ToUpper() == "TRUE" || value.ToUpper() == "FALSE") param.TrackingIsotopeLabels = bool.Parse(value); return;
                case "set fully labeled reference file": if (value.ToUpper() == "TRUE" || value.ToUpper() == "FALSE") param.SetFullyLabeledReferenceFile = bool.Parse(value); return;
                case "non labeled reference id": if (int.TryParse(value, out i)) param.NonLabeledReferenceID = i; return;
                case "fully labeled reference id": if (int.TryParse(value, out i)) param.FullyLabeledReferenceID = i; return;
                case "isotope tracking dictionary id": if (int.TryParse(value, out i)) param.IsotopeTrackingDictionary.SelectedID = i; return;

                //CorrDec settings
                case "corrdec excute":
                    if (value.ToUpper() == "FALSE") {
                        if (param.AnalysisParamOfMsdialCorrDec == null) param.AnalysisParamOfMsdialCorrDec = new AnalysisParamOfMsdialCorrDec();
                        param.AnalysisParamOfMsdialCorrDec.CanExcute = false;
                    }
                    return;                    
                case "corrdec ms2 tolerance": if (param.AnalysisParamOfMsdialCorrDec == null) param.AnalysisParamOfMsdialCorrDec = new AnalysisParamOfMsdialCorrDec();
                    if (float.TryParse(value, out f)) param.AnalysisParamOfMsdialCorrDec.MS2Tolerance = f; return;
                case "corrdec minimum ms2 peak height":
                    if (param.AnalysisParamOfMsdialCorrDec == null) param.AnalysisParamOfMsdialCorrDec = new AnalysisParamOfMsdialCorrDec();
                    if (int.TryParse(value, out i)) param.AnalysisParamOfMsdialCorrDec.MinMS2Intensity = i; return;
                case "corrdec minimum number of detected samples":
                    if (param.AnalysisParamOfMsdialCorrDec == null) param.AnalysisParamOfMsdialCorrDec = new AnalysisParamOfMsdialCorrDec();
                    if (int.TryParse(value, out i)) param.AnalysisParamOfMsdialCorrDec.MinNumberOfSample = i; return;
                case "corrdec exclude highly correlated spots":
                    if (param.AnalysisParamOfMsdialCorrDec == null) param.AnalysisParamOfMsdialCorrDec = new AnalysisParamOfMsdialCorrDec();
                    if (float.TryParse(value, out f)) param.AnalysisParamOfMsdialCorrDec.MinCorr_MS1 = f; return;
                case "corrdec minimum correlation coefficient (ms2)":
                    if (param.AnalysisParamOfMsdialCorrDec == null) param.AnalysisParamOfMsdialCorrDec = new AnalysisParamOfMsdialCorrDec();
                    if (float.TryParse(value, out f)) param.AnalysisParamOfMsdialCorrDec.MinCorr_MS2 = f; return;
                case "corrdec margin 1 (target precursor)":
                    if (param.AnalysisParamOfMsdialCorrDec == null) param.AnalysisParamOfMsdialCorrDec = new AnalysisParamOfMsdialCorrDec();
                    if (float.TryParse(value, out f)) param.AnalysisParamOfMsdialCorrDec.CorrDiff_MS1 = f; return;
                case "corrdec margin 2 (coeluted precursor)":
                    if (param.AnalysisParamOfMsdialCorrDec == null) param.AnalysisParamOfMsdialCorrDec = new AnalysisParamOfMsdialCorrDec();
                    if (float.TryParse(value, out f)) param.AnalysisParamOfMsdialCorrDec.CorrDiff_MS2 = f; return;
                case "corrdec minimum detected rate":
                    if (param.AnalysisParamOfMsdialCorrDec == null) param.AnalysisParamOfMsdialCorrDec = new AnalysisParamOfMsdialCorrDec();
                    if (float.TryParse(value, out f)) param.AnalysisParamOfMsdialCorrDec.MinDetectedPercentToVisualize = f; return;
                case "corrdec minimum ms2 relative intensity":
                    if (param.AnalysisParamOfMsdialCorrDec == null) param.AnalysisParamOfMsdialCorrDec = new AnalysisParamOfMsdialCorrDec();
                    if (float.TryParse(value, out f)) param.AnalysisParamOfMsdialCorrDec.MinMS2RelativeIntensity = f; return;
                case "corrdec remove peaks larger than precursor":
                    if (param.AnalysisParamOfMsdialCorrDec == null) param.AnalysisParamOfMsdialCorrDec = new AnalysisParamOfMsdialCorrDec();
                    if (value.ToUpper() == "TRUE" || value.ToUpper() == "FALSE") param.AnalysisParamOfMsdialCorrDec.RemoveAfterPrecursor = bool.Parse(value); return;

                // ion mobility setting
                case "accumulated rt ragne": if (float.TryParse(value, out f)) param.AccumulatedRtRagne = f; return;
                case "ccs search tolerance": if (float.TryParse(value, out f)) param.CcsSearchTolerance = f; return;
                case "mobility axis alignment tolerance": if (float.TryParse(value, out f)) param.DriftTimeAlignmentTolerance = f; return;
                case "use ccs for identification scoring": if (value.ToUpper() == "TRUE" || value.ToUpper() == "FALSE") param.IsUseCcsForIdentificationScoring = bool.Parse(value); return;
                case "use ccs for identification filtering": if (value.ToUpper() == "TRUE" || value.ToUpper() == "FALSE") param.IsUseCcsForIdentificationFiltering = bool.Parse(value); return;
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

                    var method = lineArray[0].Trim().ToLower();
                    var value = line.Substring(line.Split(':')[0].Length + 1).Trim();

                    if (method == "adduct list") {
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

        public static int FindAlignmentReferenceFileByName(String filepath, List<AnalysisFileBean> files) {

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

                    // match filename in configuration to loaded analysis files without suffix, case-sensitive
                    if (method.ToLower() == "alignment reference file name") {
                        foreach (var file in files) {
                            if (file.AnalysisFilePropertyBean.AnalysisFileName == value) {
                                Console.WriteLine("Setting alignment reference file to id {0}: {1}", file.AnalysisFilePropertyBean.AnalysisFileId, value);
                                return file.AnalysisFilePropertyBean.AnalysisFileId;
                            }
                        }

                        Console.WriteLine("Sample {0} not found, setting alignment reference id to 0", value);
                        return 0;
                    }
                }
            }

            return 0;
        }

        public static void SetGCMSAlignmentReferenceFileByFilename(String filepath, List<AnalysisFileBean> files, AnalysisParamOfMsdialGcms param) {
            param.AlignmentReferenceFileID = FindAlignmentReferenceFileByName(filepath, files);
        }

        public static void SetLCMSAlignmentReferenceFileByFilename(String filepath, List<AnalysisFileBean> files, AnalysisParametersBean param) {
            param.AlignmentReferenceFileID = FindAlignmentReferenceFileByName(filepath, files);
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

                    projectPropertyUpdate(projectProp, method.ToLower(), value);
                }
            }

            return projectProp;
        }

        private static void projectPropertyUpdate(ProjectPropertyBean projectProp, string method, string value)
        {
            switch (method) {
                //Data type
                case "ms1 data type":
                    if (value == "Centroid" || value == "Profile")
                        projectProp.DataType = (DataType)Enum.Parse(typeof(DataType), value, true);
                    return;

                case "ms2 data type":
                    if (value == "Centroid" || value == "Profile")
                        projectProp.DataTypeMS2 = (DataType)Enum.Parse(typeof(DataType), value, true);
                    return;

                case "ion mode":
                    if (value == "Positive" || value == "Negative")
                        projectProp.IonMode = (IonMode)Enum.Parse(typeof(IonMode), value, true);
                    return;

                //File paths
                case "dia file": projectProp.ExperimentFilePath = value; return;
                case "msp file": projectProp.LibraryFilePath = value; return;
                case "text file": projectProp.PostIdentificationLibraryFilePath = value; return;
                case "target compound file": projectProp.CompoundListInTargetModePath = value; return;

                // Private version
                case "is private version of Tada":
                    if (value.ToUpper() == "TRUE")
                        projectProp.IsLabPrivateVersionTada = true;                    
                    return;
            }
        }
        #endregion

        #region to obtain ccs calibration data

        public static void SetCalibrateInformation(AnalysisParametersBean param, List<AnalysisFileBean> files) {
            if (param.FileidToCcsCalibrantData != null && param.FileidToCcsCalibrantData.Count > 0) return;
            param.FileidToCcsCalibrantData = new Dictionary<int, CoefficientsForCcsCalculation>();

            var isAllCalibrantImported = true;
            foreach (var file in files) {
                var ibfpath = file.AnalysisFilePropertyBean.AnalysisFilePath;
                using (var access = new RawDataAccess(ibfpath, 0, false, false, true)) {
                    var calinfo = access.ReadIonmobilityCalibrationInfo();
                    var fileid = file.AnalysisFilePropertyBean.AnalysisFileId;
                    CoefficientsForCcsCalculation ccsCalinfo;
                    if (calinfo == null) {
                        ccsCalinfo = new CoefficientsForCcsCalculation() {
                            IsAgilentIM = false, AgilentBeta = -1, AgilentTFix = -1,
                            IsBrukerIM = false, IsWatersIM = false, WatersCoefficient = -1, WatersExponent = -1, WatersT0 = -1
                        };
                    }
                    else {
                        ccsCalinfo = new CoefficientsForCcsCalculation() {
                            IsAgilentIM = calinfo.IsAgilentIM, AgilentBeta = calinfo.AgilentBeta, AgilentTFix = calinfo.AgilentTFix,
                            IsBrukerIM = calinfo.IsBrukerIM, IsWatersIM = calinfo.IsWatersIM, WatersCoefficient = calinfo.WatersCoefficient, WatersExponent = calinfo.WatersExponent, WatersT0 = calinfo.WatersT0
                        };
                        if (calinfo.IsAgilentIM) {
                            param.IonMobilityType = IonMobilityType.Dtims;
                        }
                        else if (calinfo.IsWatersIM) {
                            param.IonMobilityType = IonMobilityType.Twims;
                        }
                        else {
                            param.IonMobilityType = IonMobilityType.Tims;
                        }
                    }

                    param.FileidToCcsCalibrantData[fileid] = ccsCalinfo;
                    if (ccsCalinfo.AgilentBeta == -1 && ccsCalinfo.AgilentTFix == -1 &&
                        ccsCalinfo.WatersCoefficient == -1 && ccsCalinfo.WatersExponent == -1 && ccsCalinfo.WatersT0 == -1) {
                        isAllCalibrantImported = false;
                    }
                }
            }
            param.IsAllCalibrantDataImported = isAllCalibrantImported;
            if (!isAllCalibrantImported) {
                var errorMessage = param.IonMobilityType == IonMobilityType.Dtims
                    ? "For Agilent single fieled-based CCS calculation, you have to set the coefficients for all files. "
                    : "For Waters CCS calculation, you have to set the coefficients for all files. ";
                errorMessage += "Otherwise, the Mason–Schamp equation using gasweight=28.0134 and temperature=305.0 is used for CCS calculation for all data. ";
                errorMessage += "Because the program does not find the calibration data file, the CCS calculation process is performed by the Mason-Schamp equation.";
                Console.WriteLine(errorMessage);
            }
        }
        #endregion
    }
}
