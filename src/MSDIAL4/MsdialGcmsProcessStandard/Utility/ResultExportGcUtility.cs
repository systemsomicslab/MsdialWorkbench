using Msdial.Gcms.Dataprocess.Algorithm;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace Msdial.Gcms.Dataprocess.Utility
{
    public sealed class ResultExportGcUtility
    {
        private ResultExportGcUtility() { }

        #region //utility for peak list export
        public static void WriteAsMsp(StreamWriter sw, int id, MS1DecResult ms1DecResult, List<MspFormatCompoundInformationBean> mspDB, AlignmentPropertyBean alignedProp = null)
        {
            var metname = MspDataRetrieve.GetCompoundName(ms1DecResult.MspDbID, mspDB);
            if (alignedProp != null && alignedProp.MetaboliteName != null && alignedProp.MetaboliteName != string.Empty) {
                metname = alignedProp.MetaboliteName;
            }
            if (metname == "Unknown") {
                metname += "ID=" + id.ToString();
                metname += "|RT=" + Math.Round(ms1DecResult.RetentionTime, 3);
                if (ms1DecResult.RetentionIndex >= 0) {
                    metname += "|RI=" + Math.Round(ms1DecResult.RetentionIndex, 1);
                }
            }

            sw.WriteLine("NAME: " + metname);
            sw.WriteLine("SCANNUMBER: " + ms1DecResult.ScanNumber);
            sw.WriteLine("RETENTIONTIME: " + ms1DecResult.RetentionTime);
            if (ms1DecResult.RetentionIndex >= 0) {
                sw.WriteLine("RETENTIONINDEX: " + ms1DecResult.RetentionIndex);
            }
            sw.WriteLine("MODELION: " + ms1DecResult.BasepeakMz);
            sw.WriteLine("MODELIONHEIGHT: " + ms1DecResult.BasepeakHeight);
            sw.WriteLine("MODELIONAREA: " + ms1DecResult.BasepeakArea);
            sw.WriteLine("INTEGRATEDHEIGHT: " + ms1DecResult.IntegratedHeight);
            sw.WriteLine("INTEGRATEDAREA: " + ms1DecResult.IntegratedArea);

            //sw.WriteLine(string.Format("DOTPRODUCT: {0:0.00000}", ms1DecResult.DotProduct));
            //sw.WriteLine(string.Format("REVERSEDOTPRODUCT: {0:0.00000}", ms1DecResult.ReverseDotProduct));
            //sw.WriteLine(string.Format("CONTRIBUTION: {0:0.00000}", ms1DecResult.PresencePersentage));
            sw.WriteLine("COMMENT: " + "ID:" + id + "|RT:" + ms1DecResult.RetentionTime + "|RI:" + ms1DecResult.RetentionIndex);
            sw.WriteLine("Num Peaks: " + ms1DecResult.Spectrum.Count);

            for (int i = 0; i < ms1DecResult.Spectrum.Count; i++)
                sw.WriteLine(Math.Round(ms1DecResult.Spectrum[i].Mz, 5) + "\t" + Math.Round(ms1DecResult.Spectrum[i].Intensity, 0));

            sw.WriteLine();
        }

        public static void WriteAsMgf(StreamWriter sw, MS1DecResult ms1DecResult, List<MspFormatCompoundInformationBean> mspDB, AlignmentPropertyBean alignedProp = null)
        {
            sw.WriteLine("BEGIN IONS");

            var metname = MspDataRetrieve.GetCompoundName(ms1DecResult.MspDbID, mspDB);
            if (alignedProp != null && alignedProp.MetaboliteName != null && alignedProp.MetaboliteName != string.Empty) {
                metname = alignedProp.MetaboliteName;
            }
            sw.WriteLine("TITLE=" + metname);

            sw.WriteLine("SCANS=" + ms1DecResult.ScanNumber);
            sw.WriteLine("RTINMINUTES=" + ms1DecResult.RetentionTime);
            if (ms1DecResult.RetentionIndex >= 0) {
                sw.WriteLine("RETENTIONINDEX=" + ms1DecResult.RetentionIndex);
            }
            sw.WriteLine("MODELION=" + ms1DecResult.BasepeakMz);
            sw.WriteLine("MODELIONHEIGHT=" + ms1DecResult.BasepeakHeight);
            sw.WriteLine("MODELIONAREA=" + ms1DecResult.BasepeakArea);
            sw.WriteLine("INTEGRATEDHEIGHT=" + ms1DecResult.IntegratedHeight);
            sw.WriteLine("INTEGRATEDAREA=" + ms1DecResult.IntegratedArea);
            //sw.WriteLine(string.Format("DOTPRODUCT={0:0.00000}", ms1DecResult.DotProduct));
            //sw.WriteLine(string.Format("REVERSEDOTPRODUCT={0:0.00000}", ms1DecResult.ReverseDotProduct));
            //sw.WriteLine(string.Format("CONTRIBUTION={0:0.00000}", ms1DecResult.PresencePersentage));

            for (int i = 0; i < ms1DecResult.Spectrum.Count; i++)
                sw.WriteLine(Math.Round(ms1DecResult.Spectrum[i].Mz, 5) + "\t" + Math.Round(ms1DecResult.Spectrum[i].Intensity, 0));

            sw.WriteLine("END IONS");
            sw.WriteLine();
        }

      
        public static void WriteTxtHeader(StreamWriter sw)
        {
            //probably, by Diego?
            //sw.WriteLine("PeakID\tTitle\tScans\tRT(min)\tPrecursor m/z\tHeight\tArea\tMetaboliteName\tModel Masses" +
            //    "\tAdductIon\tIsotope\tSMILES\tInChIKey\tDot product\tReverse dot product\tFragment presence %" +
            //    "\tTotal score\tMS1 spectrum\tMSMS spectrum");

            sw.WriteLine("Name\tScan\tRT(min)\tRetention index\tModel Masses\tModel ion mz\tModel ion height\tModel ion area" +
                "\tIntegrated height\tIntegrated area\tSMILES\tInChIKey\tDot product\tReverse dot product\tFragment presence %" +
                "\tTotal score\tSpectrum");

            //sw.WriteLine("Name\tScan\tRT[min]\tRI\tModel Ion MZ\tModel Ion Height\tModel Ion Area\tIntegrated Height\tIntegrated Area" +
            //    "\tDot Product\tReverse Dot Product\tModelPeak Purity\tModel Peak Quality\tModelMasses\tSpectrum");
        }

        public static void WriteAsTxt(StreamWriter sw, MS1DecResult ms1DecResult, List<MspFormatCompoundInformationBean> mspDB, AlignmentPropertyBean alignedProp = null)
        {
            var metname = MspDataRetrieve.GetCompoundName(ms1DecResult.MspDbID, mspDB);
            if (alignedProp != null && alignedProp.MetaboliteName != null && alignedProp.MetaboliteName != string.Empty) {
                metname = alignedProp.MetaboliteName;
            }

            sw.Write(metname + "\t");		// Name (if identified)
            sw.Write(ms1DecResult.ScanNumber + "\t");											// Scan
            sw.Write(ms1DecResult.RetentionTime + "\t");										// RT
            sw.Write(((ms1DecResult.RetentionIndex == -1) ? "" : ms1DecResult.RetentionIndex.ToString()) + "\t");		// RI
            sw.Write(String.Format("[{0}]\t", String.Join(",", ms1DecResult.ModelMasses)));     // List of model massses
            sw.Write(ms1DecResult.BasepeakMz + "\t");											// Model Ion MZ
            sw.Write((double)ms1DecResult.BasepeakHeight + "\t");										// Model Ion Height
            sw.Write((double)ms1DecResult.BasepeakArea + "\t");                                       // Model Ion Area
            sw.Write((double)ms1DecResult.IntegratedHeight + "\t");                                     // Deconvoluted Height
            sw.Write((double)ms1DecResult.IntegratedArea + "\t");                                       // Deconvoluted Area
            sw.Write(MspDataRetrieve.GetSMILES(ms1DecResult.MspDbID, mspDB) + "\t");            // SMILES
            sw.Write(MspDataRetrieve.GetInChIKey(ms1DecResult.MspDbID, mspDB) + "\t");          // InchiKey
            sw.Write(String.Format("{0:0.00000}\t", ms1DecResult.DotProduct));					// Dot Product
            sw.Write(String.Format("{0:0.00000}\t", ms1DecResult.ReverseDotProduct));           // Reverse Dot Product
            sw.Write(String.Format("{0:0.00000}\t", ms1DecResult.PresencePersentage));          // precense % ??
            sw.Write(String.Format("{0:0.00000}\t", ms1DecResult.TotalSimilarity));             // Total similarity score


            //Diego's output-> To Diego, I think you adjusted the output format as used in LC-MS, right?
            //Some users asked me that this kind of output format is not suitable for GC-MS
            //So I tempolary change the format.
            //sw.Write(ms1DecResult.ScanNumber + "\t");											// Scan
            //sw.Write(MspDataRetrieve.GetCompoundName(ms1DecResult.MspDbID, mspDB) + "\t");		// Name (if identified)
            //sw.Write(ms1DecResult.RetentionTime + "\t");										// RT
            ////sw.Write(((ms1DecResult.RetentionIndex == -1) ? "\t" : ms1DecResult.RetentionIndex.ToString()) + "\t");		// RI
            //sw.Write("\t");                                                                     // Precursor/M+ Ion Mass
            //sw.Write(ms1DecResult.IntegratedHeight + "\t");                                     // Deconvoluted Height
            //sw.Write(ms1DecResult.IntegratedArea + "\t");                                       // Deconvoluted Area
            //sw.Write(MspDataRetrieve.GetCompoundName(ms1DecResult.MspDbID, mspDB) + "\t");		// Name (if identified)
            //sw.Write(String.Format("[{0}]\t", String.Join(",", ms1DecResult.ModelMasses)));     // List of model massses
            //sw.Write("\t");                                                                     // Adduct Ion
            //sw.Write("\t");                                                                     // Isotope
            //sw.Write(MspDataRetrieve.GetSMILES(ms1DecResult.MspDbID, mspDB) + "\t");            // SMILES
            //sw.Write(MspDataRetrieve.GetInChIKey(ms1DecResult.MspDbID, mspDB) + "\t");          // InchiKey
            ////sw.Write(ms1DecResult.BasepeakMz + "\t");											// Model Ion MZ
            ////sw.Write(ms1DecResult.BasepeakHeight + "\t");										// Model Ion Height
            ////sw.Write(ms1DecResult.BasepeakArea + "\t");                                       // Model Ion Area
            //sw.Write(String.Format("{0:0.00000}\t", ms1DecResult.DotProduct));					// Dot Product
            //sw.Write(String.Format("{0:0.00000}\t", ms1DecResult.ReverseDotProduct));           // Reverse Dot Product
            //sw.Write(String.Format("{0:0.00000}\t", ms1DecResult.PresencePersentage));          // precense % ??
            ////sw.Write(string.Format("{0:0.00000}\t", ms1DecResult.ModelPeakQuality));          // quality?
            //sw.Write(String.Format("{0:0.00000}\t", ms1DecResult.TotalSimilarity));             // Total similarity score

            // writing ms1 spectrum in a functional way
            var spectrumString = getSpectrumString(ms1DecResult.Spectrum);
            sw.WriteLine(spectrumString); // Placeholder for MSMS to keep consistency with LC Export format
        }

        #endregion

        #region //utility for alignment result export
        public static string GetSpotValue(AlignedPeakPropertyBean spotProperty, string exportType)
        {
            switch (exportType) {
                case "Height": return spotProperty.Variable.ToString();
                case "Normalized": return spotProperty.NormalizedVariable.ToString();
                case "Area": return spotProperty.Area.ToString();
                case "Id": return spotProperty.PeakID.ToString();
                case "RT": return spotProperty.RetentionTime.ToString();
                case "RI": return spotProperty.RetentionIndex.ToString();
                case "QuantMass": return spotProperty.QuantMass.ToString();
                case "SN": return spotProperty.SignalToNoise.ToString();
                default: return string.Empty;
            }
        }

        public static void WriteData(StreamWriter sw, ObservableCollection<AlignedPeakPropertyBean> peaks, string exportType, bool replaceZeroToHalf, float nonZeroMin) {
            for (int i = 0; i < peaks.Count; i++) {
                var spotValue = GetSpotValue(peaks[i], exportType);

                //converting
                if (replaceZeroToHalf && (exportType == "Height" || exportType == "Normalized" || exportType == "Area")) {
                    double doublevalue = 0.0;
                    double.TryParse(spotValue, out doublevalue);
                    if (doublevalue == 0)
                        doublevalue = nonZeroMin * 0.1;
                    spotValue = doublevalue.ToString();
                }

                if (i == peaks.Count - 1)
                    sw.WriteLine(spotValue);
                else
                    sw.Write(spotValue + "\t");
            }
        }

        public static void WriteData(StreamWriter sw, ObservableCollection<AlignedPeakPropertyBean> peaks, string exportType, bool replaceZeroToHalf, float nonZeroMin, List<BasicStats> statsList) {
            for (int i = 0; i < peaks.Count; i++) {
                var spotValue = GetSpotValue(peaks[i], exportType);

                //converting
                if (replaceZeroToHalf && (exportType == "Height" || exportType == "Normalized" || exportType == "Area")) {
                    double doublevalue = 0.0;
                    double.TryParse(spotValue, out doublevalue);
                    if (doublevalue == 0)
                        doublevalue = nonZeroMin * 0.1;
                    spotValue = doublevalue.ToString();
                }
                sw.Write(spotValue + "\t");
            }
            sw.WriteLine(String.Join("\t", statsList.Select(n => n.Average)) + "\t" + String.Join("\t", statsList.Select(n => n.Stdev)));
        }

        public static void WriteDataMatrixHeader(StreamWriter sw, ObservableCollection<AnalysisFileBean> analysisFiles)
        {
            var headers = new List<string>() {
                "Alignment ID", "Average Rt(min)", "Average RI", "Quant mass", "Metabolite name", "Fill %",
                "Reference RT", "Reference RI", "Formula", "Ontology", "INCHIKEY", "SMILES",
                "Annotation tag (VS1.0)", "RT/RI matched", "EI-MS matched",
                "Comment", "Manually modified for quantification", "Manually modified for annotation",
                "Total score", "RT similarity", "RI similarity",  "Total spectrum similarity",
                "Dot product", "Reverse dot product", "Fragment presence %", "S/N average",
                "Spectrum reference file name", "EI spectrum"
            };

            for (int i = 0; i < headers.Count - 1; i++) sw.Write("" + "\t");
            sw.Write("Class" + "\t");
            sw.WriteLine(String.Join("\t", analysisFiles.Select(n => n.AnalysisFilePropertyBean.AnalysisFileClass)));

            for (int i = 0; i < headers.Count - 1; i++) sw.Write("" + "\t");
            sw.Write("File type" + "\t");
            sw.WriteLine(String.Join("\t", analysisFiles.Select(n => n.AnalysisFilePropertyBean.AnalysisFileType)));

            for (int i = 0; i < headers.Count - 1; i++) sw.Write("" + "\t");
            sw.Write("Injection order" + "\t");
            sw.WriteLine(String.Join("\t", analysisFiles.Select(n => n.AnalysisFilePropertyBean.AnalysisFileAnalyticalOrder)));

            for (int i = 0; i < headers.Count - 1; i++) sw.Write("" + "\t");
            sw.Write("Batch ID" + "\t");
            sw.WriteLine(String.Join("\t", analysisFiles.Select(n => n.AnalysisFilePropertyBean.AnalysisBatch)));

            for (int i = 0; i < headers.Count; i++) sw.Write(headers[i] + "\t");
            sw.WriteLine(String.Join("\t", analysisFiles.Select(n => n.AnalysisFilePropertyBean.AnalysisFileName)));

        }

        public static void WriteDataMatrixHeader(StreamWriter sw, ObservableCollection<AnalysisFileBean> analysisFiles, List<BasicStats> StatsList) {
            var headers = new List<string>() {
                "Alignment ID", "Average Rt(min)", "Average RI", "Quant mass", "Metabolite name", "Fill %",
                "Reference RT", "Reference RI", "Formula", "Ontology", "INCHIKEY", "SMILES",
                "Annotation tag (VS1.0)", "RT/RI matched", "EI-MS matched",
                "Comment", "Manually modified for quantification", "Manually modified for annotation",
                "Total score", "RT similarity", "RI similarity",  "Total spectrum similarity",
                "Dot product", "Reverse dot product", "Fragment presence %", "S/N average",
                "Spectrum reference file name", "EI spectrum"
            };

            var naList = new List<string>();
            var aveName = new List<string>();
            var stdName = new List<string>();
            foreach (var stats in StatsList) {
                naList.Add("NA");
                aveName.Add("Average");
                stdName.Add("Stdev");
            }

            for (int i = 0; i < headers.Count - 1; i++) sw.Write("" + "\t");
            sw.Write("Class" + "\t");
            sw.WriteLine(String.Join("\t", analysisFiles.Select(n => n.AnalysisFilePropertyBean.AnalysisFileClass)) + "\t" + String.Join("\t", naList) + "\t" + String.Join("\t", naList));

            for (int i = 0; i < headers.Count - 1; i++) sw.Write("" + "\t");
            sw.Write("File type" + "\t");
            sw.WriteLine(String.Join("\t", analysisFiles.Select(n => n.AnalysisFilePropertyBean.AnalysisFileType)) + "\t" + String.Join("\t", naList) + "\t" + String.Join("\t", naList));

            for (int i = 0; i < headers.Count - 1; i++) sw.Write("" + "\t");
            sw.Write("Injection order" + "\t");
            sw.WriteLine(String.Join("\t", analysisFiles.Select(n => n.AnalysisFilePropertyBean.AnalysisFileAnalyticalOrder)) + "\t" + String.Join("\t", naList) + "\t" + String.Join("\t", naList));

            for (int i = 0; i < headers.Count - 1; i++) sw.Write("" + "\t");
            sw.Write("Batch ID" + "\t");
            sw.WriteLine(String.Join("\t", analysisFiles.Select(n => n.AnalysisFilePropertyBean.AnalysisBatch)) + "\t" + String.Join("\t", aveName) + "\t" + String.Join("\t", stdName));

            for (int i = 0; i < headers.Count; i++) sw.Write(headers[i] + "\t");
            sw.WriteLine(String.Join("\t", analysisFiles.Select(n => n.AnalysisFilePropertyBean.AnalysisFileName)) + "\t" + String.Join("\t", StatsList.Select(n => n.Legend)) + "\t" + String.Join("\t", StatsList.Select(n => n.Legend)));

        }

        public static void WriteDataMatrixMetaData(StreamWriter sw, AlignmentPropertyBean alignedSpot, MS1DecResult ms1DecResult, List<MspFormatCompoundInformationBean> mspDB, AnalysisParamOfMsdialGcms param)
        {
            var metaboliteName = "Unknown";
            var formula = "null";
            var ontology = "null";
            var inchiKey = "null";
            var smiles = "null";
            var comment = alignedSpot.Comment;
            var refRtString = "null";
            var refRiString = "null";
            var msiLevel = 4;
            var mspID = alignedSpot.LibraryID;
            var massSpectra = ms1DecResult.Spectrum;
            var isManuallyModified = alignedSpot.IsManuallyModified ? "TRUE" : "FALSE";
            var isManuallyModifiedForAnnotation = alignedSpot.IsManuallyModifiedForAnnotation ? "TRUE" : "FALSE";
            var rtMatched = "False";
            var msMatched = "False";

            if (alignedSpot.MetaboliteName != string.Empty) metaboliteName = alignedSpot.MetaboliteName;
            if (mspID >= 0 && mspDB != null && mspDB.Count != 0) {
                formula = mspDB[mspID].Formula;
                ontology = mspDB[mspID].Ontology;
                inchiKey = mspDB[mspID].InchiKey; smiles = mspDB[mspID].Smiles;

                var refRt = mspDB[mspID].RetentionTime;
                var refRi = mspDB[mspID].RetentionIndex;

                refRtString = Math.Round(refRt, 3).ToString();
                refRiString = Math.Round(refRi, 2).ToString();
                msiLevel = getMsiLevel(alignedSpot, refRt, refRi, param);

                if (msiLevel == 1) {
                    rtMatched = "True";
                    msMatched = "True";
                }else if (msiLevel == 2) {
                    msMatched = "True";
                }
            }

            var totalScore = alignedSpot.TotalSimilairty > 0 ? Math.Round(alignedSpot.TotalSimilairty * 0.1, 1).ToString() : "null";
            var rtScore = alignedSpot.RetentionTimeSimilarity > 0 ? Math.Round(alignedSpot.RetentionTimeSimilarity * 0.1, 1).ToString() : "null";
            var riScore = alignedSpot.RetentionIndexSimilarity > 0 ? Math.Round(alignedSpot.RetentionIndexSimilarity * 0.1, 1).ToString() : "null";
            var specSimilarity = alignedSpot.EiSpectrumSimilarity > 0 ? Math.Round(alignedSpot.EiSpectrumSimilarity * 0.1, 1).ToString() : "null";
            var dotProduct = alignedSpot.MassSpectraSimilarity > 0 ? Math.Round(alignedSpot.MassSpectraSimilarity * 0.1, 1).ToString() : "null";
            var revDotProd = alignedSpot.ReverseSimilarity > 0 ? Math.Round(alignedSpot.ReverseSimilarity * 0.1, 1).ToString() : "null";
            var precense = alignedSpot.FragmentPresencePercentage > 0 ? Math.Round(alignedSpot.FragmentPresencePercentage * 0.1, 1).ToString() : "null";

            var metadata = new List<string>() {
                alignedSpot.AlignmentID.ToString(), Math.Round(alignedSpot.CentralRetentionTime, 3).ToString(), Math.Round(alignedSpot.CentralRetentionIndex, 2).ToString(),
                alignedSpot.QuantMass.ToString(), metaboliteName, Math.Round(alignedSpot.FillParcentage, 3).ToString(),
                refRtString, refRiString, formula, ontology, inchiKey, smiles, msiLevel.ToString(), rtMatched, msMatched,
                comment, isManuallyModified.ToString(), isManuallyModifiedForAnnotation.ToString(),
                totalScore, rtScore, riScore, specSimilarity, dotProduct, revDotProd, precense, Math.Round(alignedSpot.SignalToNoiseAve, 2).ToString(),
                alignedSpot.AlignedPeakPropertyBeanCollection[alignedSpot.RepresentativeFileID].FileName
            };

            //var headers = new List<string>() {
            //    "Alignment ID", "Average Rt(min)", "Average RI", "Quant mass", "Metabolite name", "Fill %",
            //    "Reference RT", "Reference RI", "Formula", "Ontology", "INCHIKEY", "SMILES", "MSI level", "Comment", "Mannually modified",
            //    "Total score", "RT similarity", "RI similarity",  "Total spectrum similarity",
            //    "Dot product", "Reverse dot product", "Fragment presence %", "S/N average",
            //    "Spectrum reference file name", "EI spectrum"
            //};

            //sw.Write(alignedSpot.AlignmentID + "\t" +
            //    Math.Round(alignedSpot.CentralRetentionTime, 3) + "\t" +
            //    Math.Round(alignedSpot.CentralRetentionIndex, 2) + "\t" +
            //    alignedSpot.QuantMass + "\t" +
            //    metaboliteName + "\t" +
            //    alignedSpot.FillParcentage + "\t" +
            //    inchiKey + "\t" +
            //    smiles + "\t" +
            //    comments + "\t" +
            //    alignedSpot.TotalSimilairty + "\t" +
            //    refRtString + "\t" +
            //    refRiString + "\t" +
            //    alignedSpot.EiSpectrumSimilarity + "\t" +
            //    alignedSpot.MassSpectraSimilarity + "\t" +
            //    alignedSpot.ReverseSimilarity + "\t" +
            //    alignedSpot.FragmentPresencePercentage + "\t" +
            //    alignedSpot.SignalToNoiseAve + "\t" +
            //    alignedSpot.AlignedPeakPropertyBeanCollection[alignedSpot.RepresentativeFileID].FileName + "\t"
            //    );

            //for (int i = 0; i < massSpectra.Count; i++) {
            //    if (i == massSpectra.Count - 1)
            //        sw.Write(Math.Round(massSpectra[i].Mz, 5) + " " + Math.Round(massSpectra[i].Intensity, 0));
            //    else
            //        sw.Write(Math.Round(massSpectra[i].Mz, 5) + " " + Math.Round(massSpectra[i].Intensity, 0) + ";");
            //}

            var spectrumString = getSpectrumString(massSpectra);
            sw.Write(String.Join("\t", metadata) + "\t" + spectrumString + "\t");
            //sw.Write(spectrumString + "\t");
            //sw.Write(String.Join(" ", massSpectra.SelectMany(ion => Math.Round(ion.Mz, 5) + ":" + Math.Round(ion.Intensity, 0))));
            //sw.Write("\t");
        }

        private static int getMsiLevel(AlignmentPropertyBean alignedSpot, float refRt, float refRi, AnalysisParamOfMsdialGcms param) {
            var msiLevel = 999; // unknown
            if (alignedSpot.LibraryID >= 0) msiLevel = 440; //440: EI-MS matched

            if (!param.IsUseRetentionInfoForIdentificationFiltering && !param.IsUseRetentionInfoForIdentificationScoring) return msiLevel;
            if (param.RetentionType == RetentionType.RT) {
                var rtTol = param.RetentionTimeLibrarySearchTolerance <= 0.1 ? param.RetentionTimeLibrarySearchTolerance : 0.1;
                if (refRt > 0 && Math.Abs(refRt - alignedSpot.CentralRetentionTime) < rtTol && alignedSpot.LibraryID >= 0) msiLevel = 340; //340: RT/RI+EI-MS matched
            }
            else {
                if (param.RiCompoundType == RiCompoundType.Alkanes) {
                    var riTol = param.RetentionIndexLibrarySearchTolerance <= 20 ? param.RetentionTimeLibrarySearchTolerance : 20;
                    if (refRi > 0 && Math.Abs(refRi - alignedSpot.CentralRetentionIndex) < riTol && alignedSpot.LibraryID >= 0) msiLevel = 340;
                }
                else {
                    var riTol = param.RetentionIndexLibrarySearchTolerance <= 2000 ? param.RetentionTimeLibrarySearchTolerance : 2000;
                    if (refRi > 0 && Math.Abs(refRi - alignedSpot.CentralRetentionIndex) < riTol && alignedSpot.LibraryID >= 0) msiLevel = 340;
                }
            }
            return msiLevel;
        }

        private static string getSpectrumString(List<Peak> massSpectra) {
            if (massSpectra == null || massSpectra.Count == 0)
                return string.Empty;

            var specString = string.Empty;
            for (int i = 0; i < massSpectra.Count; i++) {
                var spec = massSpectra[i];
                var mz = Math.Round(spec.Mz, 5);
                var intensity = Math.Round(spec.Intensity, 0);
                var sString = mz + ":" + intensity;

                if (i == massSpectra.Count - 1)
                    specString += sString;
                else
                    specString += sString + " ";
            }

            return specString;
        }

        #endregion

        public static void WriteDataMatrixMztabSMLMetaData(StreamWriter sw, AlignmentPropertyBean alignedSpot, List<MspFormatCompoundInformationBean> mspDB, 
            AnalysisParamOfMsdialGcms param, List<List<string>>database, string idConfidenceDefault, string idConfidenceManual)
        {
            var mspID = alignedSpot.LibraryID;
            var inchi = "null";

            var smlPrefix = "SML";
            var smlID = alignedSpot.AlignmentID;
            var smfIDrefs = alignedSpot.AlignmentID;
            var databaseIdentifier = "null";
            var chemicalFormula = "null";
            var smiles = "null";
            var chemicalName = "null";
            var uri = "null";
            var adductIons = "[M]1+";
            var reliability = 999; // as msiLevel
            var bestIdConfidenceMeasure = "null";
            var theoreticalNeutralMass ="null";
            var databesePrefix = "";

            if (alignedSpot.MetaboliteName != string.Empty)
            {
                chemicalName = alignedSpot.MetaboliteName;
                databesePrefix = database[0][1];
            }
            if (mspID >= 0 && mspDB != null && mspDB.Count != 0)
            {
                chemicalFormula = mspDB[mspID].Formula;
                smiles = mspDB[mspID].Smiles;
                theoreticalNeutralMass = mspDB[mspID].FormulaBean.Mass.ToString();
                var refRt = mspDB[mspID].RetentionTime;
                var refRi = mspDB[mspID].RetentionIndex;
                databaseIdentifier = databesePrefix + ": " + mspDB[mspID].Name;
                bestIdConfidenceMeasure = idConfidenceDefault;
                 //if Manually Modified
                if (idConfidenceManual != "" && alignedSpot.IsManuallyModifiedForAnnotation == true)
                {
                    bestIdConfidenceMeasure = idConfidenceManual;
                };
               reliability = getMsiLevel(alignedSpot, refRt, refRi, param);
            }

            var totalScore = alignedSpot.TotalSimilairty > 0 ? Math.Round(alignedSpot.TotalSimilairty * 0.1, 1).ToString() : "null";

            var metadata = new List<string>() {
                smlPrefix,smlID.ToString(), smfIDrefs.ToString(), databaseIdentifier,
                chemicalFormula, smiles, inchi,chemicalName, uri ,theoreticalNeutralMass,
                adductIons, reliability.ToString(),bestIdConfidenceMeasure,totalScore,
            };
            sw.Write(String.Join("\t", metadata)+"\t");
        }

        public static void WriteMztabSMLData(StreamWriter sw, ObservableCollection<AlignedPeakPropertyBean> peaks, string exportType, bool replaceZeroToHalf, float nonZeroMin, List<BasicStats> statsList)
        {
            for (int i = 0; i < peaks.Count; i++)
            {
                var spotValue = GetSpotValue(peaks[i], exportType);

                //converting
                if (replaceZeroToHalf && (exportType == "Height" || exportType == "Normalized" || exportType == "Area"))
                {
                    double doublevalue = 0.0;
                    double.TryParse(spotValue, out doublevalue);
                    if (doublevalue == 0)
                        doublevalue = nonZeroMin * 0.1;
                    spotValue = doublevalue.ToString();
                }
                sw.Write(spotValue + "\t");
            }
            sw.Write(String.Join("\t", statsList.Select(n => n.Average)) + "\t" + String.Join("\t", statsList.Select(n => n.Stdev)));
            sw.Write("\t");

        }

        public static void WriteDataMatrixMztabSMEData(StreamWriter sw, AlignmentPropertyBean alignedSpot, List<MspFormatCompoundInformationBean> mspDB, AnalysisParamOfMsdialGcms param, 
            List<List<string>> database, int spectraRefscanNo, ObservableCollection<AnalysisFileBean> analysisFiles, string idConfidenceDefault, string idConfidenceManual)
        {
            var mspID = alignedSpot.LibraryID;

            var smePrefix = "SME";
            var smeID = alignedSpot.AlignmentID;
            var chemicalName = alignedSpot.MetaboliteName;
            var inchi = "null";
            var uri = "null";
            var adductIons = "[M]1+"; // GC case
            var expMassToCharge = alignedSpot.QuantMass.ToString(); // ? OK?
            var charge = "1"; //GC case
            var derivatizedForm = "null";
            var chemicalFormula = mspDB[mspID].Formula;
            var smiles = mspDB[mspID].Smiles;
            double theoreticalNeutralMass = mspDB[mspID].FormulaBean.Mass;
            double theoreticalMassToCharge = theoreticalNeutralMass - 0.00054858; // minus electron mass; GC case
            var evidenceInputID = alignedSpot.AlignmentID; // need to consider
            var databaseIdentifier = "null";
            var repfileId = alignedSpot.RepresentativeFileID;

            var identificationMethod = idConfidenceDefault;
            //var manualCurationScore = "null"; // if use manual curation score
            if (idConfidenceManual != "" && alignedSpot.IsManuallyModifiedForAnnotation == true)
            {
                //manualCurationScore = "100";
                identificationMethod = idConfidenceManual;
            };



            // get spectra reference
            // multiple files
            var properties = alignedSpot.AlignedPeakPropertyBeanCollection;
            var spectraRefList = new List<string>();
            for (int i = 0; i < properties.Count; i++)
            {
               if (properties[i].Variable > 0)
                {
                    if (properties[i].MetaboliteName != chemicalName) continue; 
                    var ms1DecSeekpoint = properties[i].SeekPoint;
                    if (ms1DecSeekpoint < 0) continue;
                    //var filePath = analysisFiles[i].AnalysisFilePropertyBean.DeconvolutionFilePath;
                    //var ms1DecResult = DataStorageGcUtility.ReadMS1DecResult(filePath, ms1DecSeekpoint);

                    //var scanNumber = ms1DecResult.ScanNumber;
                    var scanNumber = properties[i].Ms1ScanNumber;

                    spectraRefList.Add("ms_run[" + (i + 1) + "]:scanID =" + scanNumber);
                }
            }
            if (spectraRefList.Count == 0)
            {
                spectraRefList.Add("ms_run[" + (repfileId + 1) + "]:scanID=" + spectraRefscanNo);
            }

            var spectraRef = string.Join("| ", spectraRefList);

            ////single file
            //var spectraRefNo = alignedSpot.RepresentativeFileID;
            //var spectraRef = "ms_run[" + (spectraRefNo + 1) + "]:scanID =" + spectraRefscanNo;

            var msLevel = "[MS, MS:1000511, ms level, 1]"; // GC case
            var rank = "1";

            //if Manually Modified is TRUE case
            if (idConfidenceManual != "" && alignedSpot.IsManuallyModifiedForAnnotation == true)
            {
                identificationMethod = idConfidenceManual;
            };

            var databesePrefix = database[0][1];
            databaseIdentifier = databesePrefix + ":" + mspDB[mspID].Name;


            var totalScore = alignedSpot.TotalSimilairty > 0 ? Math.Round(alignedSpot.TotalSimilairty * 0.1, 1).ToString() : "null";
            var rtScore = alignedSpot.RetentionTimeSimilarity > 0 ? Math.Round(alignedSpot.RetentionTimeSimilarity * 0.1, 1).ToString() : "null";
            var riScore = alignedSpot.RetentionIndexSimilarity > 0 ? Math.Round(alignedSpot.RetentionIndexSimilarity * 0.1, 1).ToString() : "null";
            var specSimilarity = alignedSpot.EiSpectrumSimilarity > 0 ? Math.Round(alignedSpot.EiSpectrumSimilarity * 0.1, 1).ToString() : "null";
            var dotProduct = alignedSpot.MassSpectraSimilarity > 0 ? Math.Round(alignedSpot.MassSpectraSimilarity * 0.1, 1).ToString() : "null";
            var revDotProd = alignedSpot.ReverseSimilarity > 0 ? Math.Round(alignedSpot.ReverseSimilarity * 0.1, 1).ToString() : "null";
            var precense = alignedSpot.FragmentPresencePercentage > 0 ? Math.Round(alignedSpot.FragmentPresencePercentage * 0.1, 1).ToString() : "null";

            var dataSME01 = new List<string>() {
                    smePrefix,smeID.ToString(), evidenceInputID.ToString(), databaseIdentifier,
                    chemicalFormula, smiles, inchi, chemicalName,uri,derivatizedForm,adductIons,expMassToCharge,charge,theoreticalMassToCharge.ToString(),
                    spectraRef, identificationMethod, msLevel,
                    totalScore, rtScore, riScore, specSimilarity, dotProduct, revDotProd, precense 
                    };
            //if (idConfidenceManual != "")
            //{
            //    dataSME01.Add(manualCurationScore);
            //}

            dataSME01.Add(rank);

            var dataSME02 = new List<string>();
            foreach (string item in dataSME01)
            {
                var metadataMember = item;
                if (metadataMember == "")
                {
                    metadataMember = "null";
                }
                dataSME02.Add(metadataMember);
            }


            sw.Write(String.Join("\t", dataSME02) + "\t");

        }
    }
}

