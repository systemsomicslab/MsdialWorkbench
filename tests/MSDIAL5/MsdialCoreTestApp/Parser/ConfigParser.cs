using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Parser;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using CompMs.Common.Query;
using System.Linq;

namespace CompMs.App.MsdialConsole.Parser
{
    public sealed class ConfigParser
    {
        private ConfigParser() { }

        #region // to get analysisparamOfMsdialGcms
        public static MsdialGcmsParameter ReadForGcms(string filepath)
        {
            var param = new MsdialGcmsParameter();
            using (var sr = new StreamReader(filepath, Encoding.ASCII))
            {
                while (sr.Peek() > -1)
                {
                    readFieldValues(sr.ReadLine(), out string method, out string value, out bool isReadable);
                    if (isReadable) {
                        if (!ReadCommonParameter(param, method, value)) {
                            ReadGcmsSpecificParameter(param, method, value);
                        }
                    }
                }
            }
            if (param.AccuracyType == AccuracyType.IsNominal) {
                param.MassSliceWidth = 0.5F;
                param.CentroidMs1Tolerance = 0.5F;
            }
            
            return param;
        }

     
        public static MsdialLcmsParameter ReadForLcmsParameter(string filepath) {
            var param = new MsdialLcmsParameter();
            using (var sr = new StreamReader(filepath, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    readFieldValues(sr.ReadLine(), out string method, out string value, out bool isReadable);
                    if (isReadable) {
                        if (!ReadCommonParameter(param, method, value)) {
                            // write something if needed
                        }
                    }
                }
            }
            return param;
        }

        public static MolecularSpectrumNetworkingBaseParameter ReadForMoleculerNetworkingParameter(string filepath) {
            var param = new MolecularSpectrumNetworkingBaseParameter();
            using (var sr = new StreamReader(filepath, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    readFieldValues(sr.ReadLine(), out string method, out string value, out bool isReadable);
                    if (isReadable) {
                        if (!ReadMoleculerNetworkingParameter(param, method, value)) {
                            // write something if needed
                        }
                    }
                }
            }
            return param;
        }

       

        public static MsdialDimsParameter ReadForDimsParameter(string filepath) {
            var param = new MsdialDimsParameter();
            using (var sr = new StreamReader(filepath, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    readFieldValues(sr.ReadLine(), out string method, out string value, out bool isReadable);
                    if (isReadable) {
                        if (!ReadCommonParameter(param, method, value)) {
                            // write something if needed
                        }
                    }
                }
            }
            return param;
        }

        public static MsdialLcImMsParameter ReadForLcImMsParameter(string filepath) {
            var param = new MsdialLcImMsParameter();
            using (var sr = new StreamReader(filepath, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    readFieldValues(sr.ReadLine(), out string method, out string value, out bool isReadable);
                    if (isReadable) {
                        if (!ReadCommonParameter(param, method, value)) {
                            ReadLcImMsSpecificParameter(param, method, value);
                        }
                    }
                }
            }
            return param;
        }

        public static MsdialImmsParameter ReadForImmsParameter(string filepath) {
            var param = new MsdialImmsParameter();
            using (var sr = new StreamReader(filepath, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    readFieldValues(sr.ReadLine(), out string method, out string value, out bool isReadable);
                    if (isReadable) {
                        if (!ReadCommonParameter(param, method, value)) {
                            ReadImmsSpecificParameter(param, method, value);
                        }
                    }
                }
            }
            return param;
        }

        private static void readFieldValues(string? line, out string method, out string value, out bool isReadable) {
            method = string.Empty; value = string.Empty; isReadable = false;
            if (string.IsNullOrEmpty(line)) return;
            if (line!.Length < 2) return;
            if (line[0] == '#') return;

            var lineArray = line.Split(':');
            if (lineArray.Length < 2) return;
            method = lineArray[0].Trim();
            value = line.Substring(line.Split(':')[0].Length + 1).Trim();
            isReadable = true;
        }

        public static bool ReadGcmsSpecificParameter(MsdialGcmsParameter param, string method, string value) {
            if (value.IsEmptyOrNull()) return false;
            if (method.IsEmptyOrNull()) return false;
            method = method.ToLower();
            value = value.ToLower();
            switch (method) {
                case "ri index file pathes": param.RiDictionaryFilePath = value; return true;
                case "retention type":
                    if (value == "rt" || value == "ri")
                        param.RetentionType = (RetentionType)Enum.Parse(typeof(RetentionType), value, true);
                    return true;
                case "ri compound":
                    if (value == "fames" || value == "alkanes")
                        param.RiCompoundType = (RiCompoundType)Enum.Parse(typeof(RiCompoundType), value, true);
                    return true;
                case "alignment index type": if (value == "ri") param.AlignmentIndexType = AlignmentIndexType.RI; else param.AlignmentIndexType = AlignmentIndexType.RT; return true;
                case "retention index tolerance for alignment": if (float.TryParse(value, out float ritol_align)) param.RetentionIndexAlignmentTolerance = ritol_align; return true;
                case "replace quant mass by user defined value":
                    if (value == "true")
                        param.IsReplaceQuantmassByUserDefinedValue = true; return true;
                case "is quant mass based on base peak mz":
                    if (value == "true")
                        param.IsRepresentativeQuantMassBasedOnBasePeakMz = true; return true;
                default: return false;
            }
        }

        public static bool ReadLcImMsSpecificParameter(MsdialLcImMsParameter param, string method, string value) {
            if (value.IsEmptyOrNull()) return false;
            if (method.IsEmptyOrNull()) return false;
            method = method.ToLower();
            value = value.ToLower();
            switch (method) {
                case "drift time begin": if (float.TryParse(value, out float dtBegin)) param.DriftTimeBegin = dtBegin; return true;
                case "drift time end": if (float.TryParse(value, out float dtEnd)) param.DriftTimeEnd = dtEnd; return true;
                case "accumulated rt ragne": if (float.TryParse(value, out float accumulatedRtRange)) param.AccumulatedRtRange = accumulatedRtRange; return true;
                case "accumulate ms2 spectra":
                    if (value == "true")
                        param.IsAccumulateMS2Spectra = true;
                    return true;
                case "drift time alignment tolerance": if (float.TryParse(value, out float dtaligntol)) param.DriftTimeAlignmentTolerance = dtaligntol; return true;
                case "drift time alignment factor": if (float.TryParse(value, out float dtalignfactor)) param.DriftTimeAlignmentFactor = dtalignfactor; return true;
                case "ion mobility type":
                    if (value == "tims" || value == "dtims" || value == "twims" || value == "ccs")
                        param.IonMobilityType = (IonMobilityType)Enum.Parse(typeof(IonMobilityType), value, true); return true;
                default: return false;
            }
        }

        public static bool ReadImmsSpecificParameter(MsdialImmsParameter param, string method, string value) {
            if (value.IsEmptyOrNull()) return false;
            if (method.IsEmptyOrNull()) return false;
            method = method.ToLower();
            value = value.ToLower();
            switch (method) {
                case "drift time begin": if (float.TryParse(value, out float dtBegin)) param.DriftTimeBegin = dtBegin; return true;
                case "drift time end": if (float.TryParse(value, out float dtEnd)) param.DriftTimeEnd = dtEnd; return true;
                case "drift time alignment tolerance": if (float.TryParse(value, out float dtaligntol)) param.DriftTimeAlignmentTolerance = dtaligntol; return true;
                case "drift time alignment factor": if (float.TryParse(value, out float dtalignfactor)) param.DriftTimeAlignmentFactor = dtalignfactor; return true;
                case "ion mobility type":
                    if (value == "tims" || value == "dtims" || value == "twims" || value == "ccs")
                        param.IonMobilityType = (IonMobilityType)Enum.Parse(typeof(IonMobilityType), value, true); return true;
                default: return false;
            }
        }

        private static bool ReadMoleculerNetworkingParameter(MolecularSpectrumNetworkingBaseParameter param, string method, string value) {
            if (value.IsEmptyOrNull()) return false;
            if (method.IsEmptyOrNull()) return false;
            method = method.ToLower();
            var valueLower = value.ToLower();
            switch (method) {
                case "mnrttolerance":
                    if (float.TryParse(valueLower, out float mnrttolerance)) param.MnRtTolerance = mnrttolerance; return true;
                case "mnioncorrelationsimilaritycutoff":
                    if (float.TryParse(valueLower, out float mnioncorrelationsimilaritycutoff)) param.MnIonCorrelationSimilarityCutOff = mnioncorrelationsimilaritycutoff; return true;
                case "mnspectrumsimilaritycutoff":
                    if (float.TryParse(valueLower, out float mnspectrumsimilaritycutoff)) param.MnSpectrumSimilarityCutOff = mnspectrumsimilaritycutoff; return true;
                case "mnrelativeabundancecutoff":
                    if (float.TryParse(valueLower, out float mnrelativeabundancecutoff)) param.MnRelativeAbundanceCutOff = mnrelativeabundancecutoff; return true;
                case "mnmasstolerance":
                    if (float.TryParse(valueLower, out float mnmasstolerance)) param.MnMassTolerance = mnmasstolerance; return true;
                case "minimumpeakmatch":
                    if (float.TryParse(valueLower, out float minimumpeakmatch)) param.MinimumPeakMatch = minimumpeakmatch; return true;
                case "maxedgenumberpernode":
                    if (float.TryParse(valueLower, out float maxedgenumberpernode)) param.MaxEdgeNumberPerNode = maxedgenumberpernode; return true;
                case "maxprecursordifference":
                    if (float.TryParse(valueLower, out float maxprecursordifference)) param.MaxPrecursorDifference = maxprecursordifference; return true;
                case "mnabsoluteabundancecutoff":
                    if (float.TryParse(valueLower, out float mnabsoluteabundancecutoff)) param.MnAbsoluteAbundanceCutOff = mnabsoluteabundancecutoff; return true;
                case "msmssimilaritycalc":
                    if (value == "Bonanza" || value == "ModDot" || value == "Cosine" || value == "All")
                        param.MsmsSimilarityCalc = (MsmsSimilarityCalc)Enum.Parse(typeof(MsmsSimilarityCalc), value, true); return true;
                case "mnisexportioncorrelation":
                    if (valueLower == "true" || valueLower == "false") param.MnIsExportIonCorrelation = bool.Parse(valueLower); return true;
                default: return false;
            }
        }

        public static bool ReadCommonParameter(ParameterBase param, string method, string value) {
            if (value.IsEmptyOrNull()) return false;
            if (method.IsEmptyOrNull()) return false;
            method = method.ToLower();
            var valueLower = value.ToLower();
            switch (method) {
                //Data type
                case "ms1 data type":
                    if (valueLower == "centroid" || valueLower == "profile")
                        param.MSDataType = (MSDataType)Enum.Parse(typeof(MSDataType), valueLower, true);
                    return true;

                case "ms2 data type":
                    if (valueLower == "centroid" || valueLower == "profile")
                        param.MS2DataType = (MSDataType)Enum.Parse(typeof(MSDataType), valueLower, true);
                    return true;

                case "ion mode":
                    if (valueLower == "positive" || valueLower == "negative")
                        param.IonMode = (IonMode)Enum.Parse(typeof(IonMode), valueLower, true);
                    return true;
                
                case "target omics":
                    if (valueLower == "metabolomics" || valueLower == "lipidomics")
                        param.TargetOmics = (TargetOmics)Enum.Parse(typeof(TargetOmics), valueLower, true);
                    return true;

                case "acquisition type":
                    if (valueLower == "dda" || valueLower == "swath" || valueLower == "aif")
#pragma warning disable CS0618 // Type or member is obsolete
                        // ProjectBaseParameter.AcquisitionType is obsolete, but is used because it is not possible to set the AcquisitionType of individual files in the Console application.
                        param.ProjectParam.AcquisitionType = (AcquisitionType)Enum.Parse(typeof(AcquisitionType), valueLower, true);
#pragma warning restore CS0618 // Type or member is obsolete
                    return true;

                //{ GCMS, LCMS, IMMS, LCIMMS, IFMS, IIMMS, IDIMS, }
                case "machine category":
                    if (value == "GCMS" || value == "LCMS" || value == "IMMS" || value == "LCIMMS" || value == "IFMS" || value == "IIMMS" || value == "IDIMS")
                        param.ProjectParam.MachineCategory = (MachineCategory)Enum.Parse(typeof(MachineCategory), valueLower, true);
                    return true;

                case "slovent type":
                    if (value == "CH3COONH4" || value == "HCOONH4")
                        param.LipidQueryContainer.SolventType = (SolventType)Enum.Parse(typeof(SolventType), valueLower, true);
                    return true;

                case "searched lipid class":
                    if (!value.IsEmptyOrNull()) {
                        param.LipidQueryContainer = new LipidQueryBean() {
                            SolventType = SolventType.CH3COONH4,
                            LbmQueries = LbmQueryParcer.GetLbmQueries(isLabUseOnly: param.IsLabPrivate)
                        };

                        param.LipidQueryContainer.LbmQueries = param.LipidQueryContainer.LbmQueries.Where(n => n.IonMode == param.IonMode).ToList();
                        foreach (var l in param.LipidQueryContainer.LbmQueries) l.IsSelected = false;

                        var aStrings = value.Split(';');
                        foreach (var lipidString in aStrings) {
                            if (lipidString.Split(' ').Length >= 2) {
                                var lipidclass = lipidString.Split(' ')[0];
                                var adducttype = lipidString.Split(' ')[1];
                                if (!Enum.IsDefined(typeof(LbmClass), lipidclass)) continue;
                                var adductObj = AdductIon.GetAdductIon(adducttype);
                                if (!adductObj.FormatCheck) continue;

                                foreach (var l in param.LipidQueryContainer.LbmQueries) {
                                    if (l.LbmClass.ToString() == lipidclass && adductObj.ToString() == l.AdductType.ToString()) {
                                        l.IsSelected = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    return true;

                //File paths
                case "msp file path": param.MspFilePath = value; return true;
                case "lbm file path": param.LbmFilePath = value; return true;
                case "text db file path": param.TextDBFilePath = value; return true;
                case "isotope text db file path": param.IsotopeTextDBFilePath = value; return true;
                case "compounds library file path for target detection": param.CompoundListInTargetModePath = value; return true;
                case "compounds library file path for rt correction":
                    param.CompoundListForRtCorrectionPath = value;
                    if (System.IO.File.Exists(value)) {
                        var error = string.Empty;
                        param.RetentionTimeCorrectionCommon.StandardLibrary = TextLibraryParser.CompoundListInTargetModeReader(valueLower, out error);
                        if (error != string.Empty) {
                            Console.WriteLine(error);
                        }
                    }
                    return true;

                // Private version
                case "is private version of tada":
                    if (valueLower == "true")
                        param.IsLabPrivateVersionTada = true;
                    return true;
                case "is private version":
                    if (valueLower == "true")
                        param.IsLabPrivate = true;
                    return true;

                //Data correction
                case "retention time begin": if (float.TryParse(valueLower, out float rtbegin)) param.RetentionTimeBegin = rtbegin; return true;
                case "retention time end": if (float.TryParse(valueLower, out float rtend)) param.RetentionTimeEnd = rtend; return true;
                case "ms1 mass range begin": if (float.TryParse(valueLower, out float ms1begin)) param.MassRangeBegin = ms1begin; return true;
                case "ms1 mass range end": if (float.TryParse(valueLower, out float ms1end)) param.MassRangeEnd = ms1end; return true;
                case "ms2 mass range begin": if (float.TryParse(valueLower, out float ms2begin)) param.Ms2MassRangeBegin = ms2begin; return true;
                case "ms2 mass range end": if (float.TryParse(valueLower, out float ms2end)) param.Ms2MassRangeEnd = ms2end; return true;
                case "accuracy type":
                    if (valueLower == "isnominal" || valueLower == "isaccurate")
                        param.AccuracyType = (AccuracyType)Enum.Parse(typeof(AccuracyType), valueLower, true);
                    return true;

                //Centroid parameters
                case "ms1 tolerance for centroid": if (float.TryParse(valueLower, out float centMs1Tol)) param.CentroidMs1Tolerance = centMs1Tol; return true;
                case "ms2 tolerance for centroid": if (float.TryParse(valueLower, out float centMs2Tol)) param.CentroidMs2Tolerance = centMs2Tol; return true;

                //Peak detection param
                case "smoothing method":
                    if (valueLower == "simplemovingaverage" || valueLower == "linearweightedmovingaverage" || valueLower == "savitzkygolayfilter" || valueLower == "binomialfilter")
                        param.SmoothingMethod = (SmoothingMethod)Enum.Parse(typeof(SmoothingMethod), valueLower, true);
                    return true;
                case "smoothing level": if (int.TryParse(valueLower, out int smoothlevel)) param.SmoothingLevel = smoothlevel; return true;
                case "average peak width": if (int.TryParse(valueLower, out int avepeakwidth)) param.AveragePeakWidth = avepeakwidth; return true;
                case "minimum peak width": if (int.TryParse(valueLower, out int minpeakwidth)) param.MinimumDatapoints = minpeakwidth; return true;
                case "minimum peak height": if (int.TryParse(valueLower, out int minpeakheight)) param.MinimumAmplitude = minpeakheight; return true;
                case "mass slice width": if (float.TryParse(valueLower, out float massSliceWidth)) param.MassSliceWidth = massSliceWidth; return true;
                case "mass accuracy": if (float.TryParse(valueLower, out float ms1accuracy)) param.CentroidMs1Tolerance = ms1accuracy; return true;
                case "max charge number": if (int.TryParse(valueLower, out int maxchargenum)) param.MaxChargeNumber = maxchargenum; return true;
                case "searched adduct ions": 
                    if (!value.IsEmptyOrNull()) {
                        param.SearchedAdductIons = new List<AdductIon>();
                        var aStrings = value.Split(',');
                        foreach (var adductString in aStrings) {
                            var adductObj = AdductIon.GetAdductIon(adductString);
                            if (adductObj.FormatCheck) param.SearchedAdductIons.Add(adductObj);
                        }
                    }
                    return true;

                


                //Deconvolution
                case "sigma window valueLower": if (float.TryParse(valueLower, out float sigmaWindow)) param.SigmaWindowValue = sigmaWindow; return true;
                case "amplitude cut off": if (float.TryParse(valueLower, out float ms2ampthreshold)) param.AmplitudeCutoff = ms2ampthreshold; return true;
                case "keep isotope range": if (float.TryParse(valueLower, out float keepisotoperange)) param.KeptIsotopeRange = keepisotoperange; return true;
                case "exclude after precursor": if (valueLower == "false") param.RemoveAfterPrecursor = false; return true;
                case "keep original precursor isotopes": if (valueLower == "false") param.KeepOriginalPrecursorIsotopes = false; return true;
                case "target ce": if (double.TryParse(valueLower, out double targetce)) param.TargetCE = targetce; return true;

                //Identification
                case "rt tolerance for msp-based annotation": if (float.TryParse(valueLower, out float rttol_ident)) param.MspSearchParam.RtTolerance = rttol_ident; return true;
                case "ri tolerance for msp-based annotation": if (float.TryParse(valueLower, out float ritol_ident)) param.MspSearchParam.RiTolerance = ritol_ident; return true;
                case "ccs tolerance for msp-based annotation": if (float.TryParse(valueLower, out float ccstol_ident)) param.MspSearchParam.CcsTolerance = ccstol_ident; return true;
                case "mass range begin for msp-based annotation": if (float.TryParse(valueLower, out float msbegin_ident)) param.MspSearchParam.MassRangeBegin = msbegin_ident; return true;
                case "mass range end for msp-based annotation": if (float.TryParse(valueLower, out float msend_ident)) param.MspSearchParam.MassRangeEnd = msend_ident; return true;
                case "relative amplitude cutoff for msp-based annotation": if (float.TryParse(valueLower, out float relamp_ident)) param.MspSearchParam.RelativeAmpCutoff = relamp_ident; return true;
                case "absolute amplitude cutoff for msp-based annotation": if (float.TryParse(valueLower, out float absamp_ident)) param.MspSearchParam.AbsoluteAmpCutoff = absamp_ident; return true;
                case "weighted dot product cutoff for msp-based annotation": if (float.TryParse(valueLower, out float dotproduct)) param.MspSearchParam.WeightedDotProductCutOff = dotproduct; return true;
                case "simple dot product cutoff for msp-based annotation": if (float.TryParse(valueLower, out float simpleproduct)) param.MspSearchParam.SimpleDotProductCutOff = simpleproduct; return true;
                case "reverse dot product cutoff for msp-based annotation": if (float.TryParse(valueLower, out float revdotproduct)) param.MspSearchParam.ReverseDotProductCutOff = revdotproduct; return true;
                case "matched peaks percentage cutoff for msp-based annotation": if (float.TryParse(valueLower, out float matchedpeakspercent)) param.MspSearchParam.MatchedPeaksPercentageCutOff = matchedpeakspercent; return true;
                case "minimum spectrum match for msp-based annotation": if (float.TryParse(valueLower, out float minpeakmatch)) param.MspSearchParam.MinimumSpectrumMatch = minpeakmatch; return true;
                case "total score cutoff for msp-based annotation": if (float.TryParse(valueLower, out float cutoff_ident)) param.MspSearchParam.TotalScoreCutoff = cutoff_ident; return true;
                case "ms1 tolerance for msp-based annotation": if (float.TryParse(valueLower, out float ms1tol_ident)) param.MspSearchParam.Ms1Tolerance = ms1tol_ident; return true;
                case "ms2 tolerance for msp-based annotation": if (float.TryParse(valueLower, out float ms2tol_ident)) param.MspSearchParam.Ms2Tolerance = ms2tol_ident; return true;
                case "use retention information for msp-based annotation scoring": if (valueLower == "true" || valueLower == "false") param.MspSearchParam.IsUseTimeForAnnotationScoring = bool.Parse(valueLower); return true;
                case "use retention information for msp-based annotation filtering": if (valueLower == "true" || valueLower == "false") param.MspSearchParam.IsUseTimeForAnnotationFiltering = bool.Parse(valueLower); return true;
                case "use ccs for msp-based annotation scoring": if (valueLower == "true" || valueLower == "false") param.MspSearchParam.IsUseCcsForAnnotationScoring = bool.Parse(valueLower); return true;
                case "use ccs for msp-based annotation filtering": if (valueLower == "true" || valueLower == "false") param.MspSearchParam.IsUseCcsForAnnotationFiltering = bool.Parse(valueLower); return true;
                case "only report top hit for msp-based annotation": if (valueLower == "true" || valueLower == "false") param.OnlyReportTopHitInMspSearch = bool.Parse(valueLower); return true;
                case "execute annotation process only for alignment file for msp-based annotation": if (valueLower == "true" || valueLower == "false") param.IsIdentificationOnlyPerformedForAlignmentFile = bool.Parse(valueLower); return true;

                //Identification
                case "rt tolerance for lbm-based annotation": if (float.TryParse(valueLower, out float rttol_lbm_ident)) param.LbmSearchParam.RtTolerance = rttol_lbm_ident; return true;
                case "ri tolerance for lbm-based annotation": if (float.TryParse(valueLower, out float ritol_lbm_ident)) param.LbmSearchParam.RiTolerance = ritol_lbm_ident; return true;
                case "ccs tolerance for lbm-based annotation": if (float.TryParse(valueLower, out float ccstol_lbm_ident)) param.LbmSearchParam.CcsTolerance = ccstol_lbm_ident; return true;
                case "mass range begin for lbm-based annotation": if (float.TryParse(valueLower, out float msbegin_lbm_ident)) param.LbmSearchParam.MassRangeBegin = msbegin_lbm_ident; return true;
                case "mass range end for lbm-based annotation": if (float.TryParse(valueLower, out float msend_lbm_ident)) param.LbmSearchParam.MassRangeEnd = msend_lbm_ident; return true;
                case "relative amplitude cutoff for lbm-based annotation": if (float.TryParse(valueLower, out float relamp_lbm_ident)) param.LbmSearchParam.RelativeAmpCutoff = relamp_lbm_ident; return true;
                case "absolute amplitude cutoff for lbm-based annotation": if (float.TryParse(valueLower, out float absamp_lbm_ident)) param.LbmSearchParam.AbsoluteAmpCutoff = absamp_lbm_ident; return true;
                case "weighted dot product cutoff for lbm-based annotation": if (float.TryParse(valueLower, out float lbm_dotproduct)) param.LbmSearchParam.WeightedDotProductCutOff = lbm_dotproduct; return true;
                case "simple dot product cutoff for lbm-based annotation": if (float.TryParse(valueLower, out float lbm_simpleproduct)) param.LbmSearchParam.SimpleDotProductCutOff = lbm_simpleproduct; return true;
                case "reverse dot product cutoff for lbm-based annotation": if (float.TryParse(valueLower, out float lbm_revdotproduct)) param.LbmSearchParam.ReverseDotProductCutOff = lbm_revdotproduct; return true;
                case "matched peaks percentage cutoff for lbm-based annotation": if (float.TryParse(valueLower, out float lbm_matchedpeakspercent)) param.LbmSearchParam.MatchedPeaksPercentageCutOff = lbm_matchedpeakspercent; return true;
                case "minimum spectrum match for lbm-based annotation": if (float.TryParse(valueLower, out float lbm_minpeakmatch)) param.LbmSearchParam.MinimumSpectrumMatch = lbm_minpeakmatch; return true;
                case "total score cutoff for lbm-based annotation": if (float.TryParse(valueLower, out float cutoff_lbm_ident)) param.LbmSearchParam.TotalScoreCutoff = cutoff_lbm_ident; return true;
                case "ms1 tolerance for lbm-based annotation": if (float.TryParse(valueLower, out float ms1tol_lbm_ident)) param.LbmSearchParam.Ms1Tolerance = ms1tol_lbm_ident; return true;
                case "ms2 tolerance for lbm-based annotation": if (float.TryParse(valueLower, out float ms2tol_lbm_ident)) param.LbmSearchParam.Ms2Tolerance = ms2tol_lbm_ident; return true;
                case "use retention information for lbm-based annotation scoring": if (valueLower == "true" || valueLower == "false") param.LbmSearchParam.IsUseTimeForAnnotationScoring = bool.Parse(valueLower); return true;
                case "use retention information for lbm-based annotation filtering": if (valueLower == "true" || valueLower == "false") param.LbmSearchParam.IsUseTimeForAnnotationFiltering = bool.Parse(valueLower); return true;
                case "use ccs for lbm-based annotation scoring": if (valueLower == "true" || valueLower == "false") param.LbmSearchParam.IsUseCcsForAnnotationScoring = bool.Parse(valueLower); return true;
                case "use ccs for lbm-based annotation filtering": if (valueLower == "true" || valueLower == "false") param.MspSearchParam.IsUseCcsForAnnotationFiltering = bool.Parse(valueLower); return true;
                case "execute annotation process only for alignment file for lbm-based annotation": if (valueLower == "true" || valueLower == "false") param.IsIdentificationOnlyPerformedForAlignmentFile = bool.Parse(valueLower); return true;


                //Post identification
                case "rt tolerance for text-based annotation": if (float.TryParse(valueLower, out float rttol_textident)) param.TextDbSearchParam.RtTolerance = rttol_textident; return true;
                case "ri tolerance for text-based annotation": if (float.TryParse(valueLower, out float ritol_textident)) param.TextDbSearchParam.RiTolerance = ritol_textident; return true;
                case "ccs tolerance for text-based annotation": if (float.TryParse(valueLower, out float ccstol_textident)) param.TextDbSearchParam.CcsTolerance = ccstol_textident; return true;
                case "total score cutoff for text-based annotation": if (float.TryParse(valueLower, out float cutoff_textident)) param.TextDbSearchParam.TotalScoreCutoff = cutoff_textident; return true;
                case "accurate ms1 tolerance for text-based annotation": if (float.TryParse(valueLower, out float ms1tol_textident)) param.TextDbSearchParam.Ms1Tolerance = ms1tol_textident; return true;
                case "use retention information for text-based annotation scoring": if (valueLower == "true" || valueLower == "false") param.TextDbSearchParam.IsUseTimeForAnnotationScoring = bool.Parse(valueLower); return true;
                case "use retention information for text-based annotation filtering": if (valueLower == "true" || valueLower == "false") param.TextDbSearchParam.IsUseTimeForAnnotationFiltering = bool.Parse(valueLower); return true;
                case "use ccs for text-based annotation scoring": if (valueLower == "true" || valueLower == "false") param.TextDbSearchParam.IsUseCcsForAnnotationScoring = bool.Parse(valueLower); return true;
                case "use ccs for text-based annotation filtering": if (valueLower == "true" || valueLower == "false") param.TextDbSearchParam.IsUseCcsForAnnotationFiltering = bool.Parse(valueLower); return true;
                case "only report top hit for text-based annotation": if (valueLower == "true" || valueLower == "false") param.OnlyReportTopHitInTextDBSearch = bool.Parse(valueLower); return true;

                //Alignment parameters setting
                case "alignment reference file id": if (int.TryParse(valueLower, out int refID)) param.AlignmentReferenceFileID = refID; return true;
                case "retention time tolerance for alignment": if (float.TryParse(valueLower, out float rttol_align)) param.RetentionTimeAlignmentTolerance = rttol_align; return true;
                case "retention time factor for alignment": if (float.TryParse(valueLower, out float rtfactor_align)) param.RetentionTimeAlignmentFactor = rtfactor_align; return true;
                case "spectrum similarity tolerance for alignment": if (float.TryParse(valueLower, out float specsim_align)) param.SpectrumSimilarityAlignmentTolerance = specsim_align; return true;
                case "spectrum similarity factor for alignment": if (float.TryParse(valueLower, out float specsimfactor_align)) param.SpectrumSimilarityAlignmentFactor = specsimfactor_align; return true;
                case "ms1 tolerance for alignment": if (float.TryParse(valueLower, out float ms1aligntol)) param.Ms1AlignmentTolerance = ms1aligntol; return true;
                case "ms1 factor for alignment": if (float.TryParse(valueLower, out float ms1alignfactor)) param.Ms1AlignmentFactor = ms1alignfactor; return true;
                case "force insert peaks in gap filling": if (valueLower == "true" || valueLower == "false") param.IsForceInsertForGapFilling = bool.Parse(valueLower); return true;
                case "together with alignment": if (valueLower == "true" || valueLower == "false") param.TogetherWithAlignment = bool.Parse(valueLower); return true;

                //Filtering
                case "peak count filter": if (float.TryParse(valueLower, out float peakcountfilter)) param.PeakCountFilter = peakcountfilter; return true;
                case "n percent detected in one group": if (float.TryParse(valueLower, out float nPercentDetectedInOneGroup)) param.NPercentDetectedInOneGroup = nPercentDetectedInOneGroup; return true;
                case "remove feature based on peak height fold-change": if (valueLower == "true" || valueLower == "false") param.IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = bool.Parse(valueLower); return true;
                case "blank filtering":
                    if (valueLower.ToLower() == "samplemaxoverblankave")
                        param.BlankFiltering = (BlankFiltering)Enum.Parse(typeof(BlankFiltering), valueLower, true);
                    return true;
                case "sample max / blank average": if (float.TryParse(valueLower, out float sampleMaxOverBlankAverage)) param.SampleMaxOverBlankAverage = sampleMaxOverBlankAverage; return true;
                case "sample average / blank average": if (float.TryParse(valueLower, out float sampleAverageOverBlankAverage)) param.SampleAverageOverBlankAverage = sampleAverageOverBlankAverage; return true;
                case "keep reference matched metabolites": if (valueLower == "true" || valueLower == "false") param.IsKeepRefMatchedMetaboliteFeatures = bool.Parse(valueLower); return true;
                case "keep suggested metabolites": if (valueLower == "true" || valueLower == "false") param.IsKeepSuggestedMetaboliteFeatures = bool.Parse(valueLower); return true;
                case "keep removable features and assigned tag for checking": if (valueLower == "true" || valueLower == "false") param.IsKeepRemovableFeaturesAndAssignedTagForChecking = bool.Parse(valueLower); return true;
                case "replace true zero valueLowers with 1/2 of minimum peak height over all samples": if (valueLower == "true" || valueLower == "false") param.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples = bool.Parse(valueLower); return true;

                //Retentiontime correction
                case "execute rt correction": if (valueLower == "true" || valueLower == "false") param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExcuteRtCorrection = bool.Parse(valueLower); return true;
                case "rt correction with smoothing for rt diff": if (valueLower == "true" || valueLower == "false") param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.doSmoothing = bool.Parse(valueLower); return true;
                case "user setting intercept": if (float.TryParse(valueLower, out float userintercept)) param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.UserSettingIntercept = userintercept; return true;
                case "rt diff calc method":
                    if (valueLower == "sampleminussampleaverage" || valueLower == "sampleminusreference")
                        param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.RtDiffCalcMethod = (RtDiffCalcMethod)Enum.Parse(typeof(RtDiffCalcMethod), valueLower, true);
                    return true;
                case "interpolation method":
                    if (valueLower == "linear")
                        param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.InterpolationMethod = InterpolationMethod.Linear;
                    return true;
                case "extrapolation method (begin)":
                    if (valueLower == "usersetting" || valueLower == "firstpoint" || valueLower == "linearextrapolation")
                        param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExtrapolationMethodBegin = (ExtrapolationMethodBegin)Enum.Parse(typeof(ExtrapolationMethodBegin), valueLower, true);
                    return true;
                case "extrapolation method (end)":
                    if (valueLower == "lastpoint" || valueLower == "linearextrapolation")
                        param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExtrapolationMethodEnd = (ExtrapolationMethodEnd)Enum.Parse(typeof(ExtrapolationMethodEnd), valueLower, true);
                    return true;

                //Isotope tracking setting
                case "tracking isotope label": if (valueLower == "true" || valueLower == "false") param.TrackingIsotopeLabels = bool.Parse(valueLower); return true;
                case "set fully labeled reference file": if (valueLower == "true" || valueLower == "false") param.SetFullyLabeledReferenceFile = bool.Parse(valueLower); return true;
                case "non labeled reference id": if (int.TryParse(valueLower, out int nonlabeledrefid)) param.NonLabeledReferenceID = nonlabeledrefid; return true;
                case "fully labeled reference id": if (int.TryParse(valueLower, out int fulllabeledrefid)) param.FullyLabeledReferenceID = fulllabeledrefid; return true;
                case "isotope tracking dictionary id": if (int.TryParse(valueLower, out int isotopetrackdictionaryid)) param.IsotopeTrackingDictionary.SelectedID = isotopetrackdictionaryid; return true;

                //CorrDec settings
                case "corrdec execute":
                    if (valueLower.ToLower() == "false") {
                        param.CorrDecParam.CanExcute = false;
                    }
                    return true;
                case "corrdec ms2 tolerance":
                    if (float.TryParse(valueLower, out float corrdecms2tol)) param.CorrDecParam.MS2Tolerance = corrdecms2tol; return true;
                case "corrdec minimum ms2 peak height":
                    if (int.TryParse(valueLower, out int corrdecminms2int)) param.CorrDecParam.MinMS2Intensity = corrdecminms2int; return true;
                case "corrdec minimum number of detected samples":
                    if (int.TryParse(valueLower, out int corrdecminnumberofsample)) param.CorrDecParam.MinNumberOfSample = corrdecminnumberofsample; return true;
                case "corrdec exclude highly correlated spots":
                    if (float.TryParse(valueLower, out float corrdecmincorr_ms1)) param.CorrDecParam.MinCorr_MS1 = corrdecmincorr_ms1; return true;
                case "corrdec minimum correlation coefficient (ms2)":
                    if (float.TryParse(valueLower, out float corrdecmincorr_ms2)) param.CorrDecParam.MinCorr_MS2 = corrdecmincorr_ms2; return true;
                case "corrdec margin 1 (target precursor)":
                    if (float.TryParse(valueLower, out float corrdeccorrdiff_ms1)) param.CorrDecParam.CorrDiff_MS1 = corrdeccorrdiff_ms1; return true;
                case "corrdec margin 2 (coeluted precursor)":
                    if (float.TryParse(valueLower, out float corrdeccorrdiff_ms2)) param.CorrDecParam.CorrDiff_MS2 = corrdeccorrdiff_ms2; return true;
                case "corrdec minimum detected rate":
                    if (float.TryParse(valueLower, out float corrdecmindetectedrate)) param.CorrDecParam.MinDetectedPercentToVisualize = corrdecmindetectedrate; return true;
                case "corrdec minimum ms2 relative intensity":
                    if (float.TryParse(valueLower, out float corrdecminms2relativeint)) param.CorrDecParam.MinMS2RelativeIntensity = corrdecminms2relativeint; return true;
                case "corrdec remove peaks larger than precursor":
                    if (valueLower == "true" || valueLower == "false") param.CorrDecParam.CorrDecRemoveAfterPrecursor = bool.Parse(valueLower); return true;
                default: return false;
            }
        }
      
        #endregion
    }
}
