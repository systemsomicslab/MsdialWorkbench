using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Msdial.Lcms.Dataprocess.Algorithm;
using Msdial.Lcms.Dataprocess.Utility;
using Rfx.Riken.OsakaUniv;
using CompMs.Common.DataObj;

namespace Msdial.Common.Export
{
    public class PrivateMethodTargetCompoundExport
    {
        public static void ExportTargetResult(ProjectPropertyBean projectProperty, RdamPropertyBean rdamProperty, List<AnalysisFileBean> analysisFiles, AlignmentFileBean alignmentFileBean,
            AlignmentResultBean alignmentResult, List<MspFormatCompoundInformationBean> mspDB, List<TextFormatCompoundInformationBean> libList, AnalysisParametersBean param, float targetMz, bool withCorrDec = true) {
            var id = -1;
            var maxCorrel = -1.0;
            foreach (var spot in alignmentResult.AlignmentPropertyBeanCollection) {
                if (Math.Abs(spot.CentralAccurateMass - targetMz) < 0.015) {
                    double[] x = new double[spot.AlignedPeakPropertyBeanCollection.Count];
                    double[] y = new double[spot.AlignedPeakPropertyBeanCollection.Count];
                    for (var i = 0; i < spot.AlignedPeakPropertyBeanCollection.Count; i++) {
                        x[i] = i + 1;
                        y[i] = spot.AlignedPeakPropertyBeanCollection[i].Variable;
                    }
                    var correl = BasicMathematics.Coefficient(x, y);
                    if (correl > maxCorrel) {
                        id = spot.AlignmentID;
                        maxCorrel = correl;
                    }
                }
            }
            if (maxCorrel < 0.5) id = -1;
            if (id > -1) {
                Export2MsFinder(projectProperty, rdamProperty, analysisFiles, alignmentFileBean, alignmentResult, mspDB, param, id, withCorrDec);
            }
            ExportAsString(projectProperty, rdamProperty, analysisFiles, alignmentFileBean, alignmentResult, mspDB, param, libList, id, projectProperty.ProjectFolderPath);
        }

        public static void ExportTargetResult(ProjectPropertyBean projectProperty, RdamPropertyBean rdamProperty, List<AnalysisFileBean> analysisFiles, AlignmentFileBean alignmentFileBean,
    AlignmentResultBean alignmentResult, List<MspFormatCompoundInformationBean> mspDB, List<TextFormatCompoundInformationBean> libList, AnalysisParametersBean param, int id, 
    string inchikey, string smiles, string formula, string exportDir, int targetFileId, bool withCorrDec = true) {
            if (!Directory.Exists(exportDir)) Directory.CreateDirectory(exportDir);
            Export2MsFinder(projectProperty, rdamProperty, analysisFiles, alignmentFileBean, alignmentResult, mspDB, param, id, withCorrDec, formula, smiles, inchikey, exportDir, targetFileId);
            ExportAsString(projectProperty, rdamProperty, analysisFiles, alignmentFileBean, alignmentResult, mspDB, param, libList, id, exportDir);
        }


        public static void ExportAsString(ProjectPropertyBean projectProperty, RdamPropertyBean rdamProperty, List<AnalysisFileBean> analysisFiles, AlignmentFileBean alignmentFileBean,
            AlignmentResultBean alignmentResult, List<MspFormatCompoundInformationBean> mspDB, AnalysisParametersBean param, List<TextFormatCompoundInformationBean> libList, int id, string exportDir) {
            var stdSpotList = new List<AlignmentPropertyBean>();
            if (libList != null)
                stdSpotList = GetInternalStdSpot(alignmentResult.AlignmentPropertyBeanCollection, libList);

            if (id > -1) stdSpotList.Add(alignmentResult.AlignmentPropertyBeanCollection[id]);

            if (stdSpotList.Count == 0) return;

            var filePath = exportDir + "\\" + Path.GetFileNameWithoutExtension(projectProperty.ProjectFilePath) + "_TextFormatExportRes_";
            using (var sw = new StreamWriter(filePath + "PeakArea.txt", false, Encoding.ASCII)) {
                sw.WriteLine(GetHeader(stdSpotList[0]));
                foreach (var spot in stdSpotList) {
                    sw.Write(GetMetaInfo(spot));
                    sw.WriteLine(GetPeakAreaAsString(spot.AlignedPeakPropertyBeanCollection));
                }
            }

            using (var sw = new StreamWriter(filePath + "PeakHeight.txt", false, Encoding.ASCII)) {
                sw.WriteLine(GetHeader(stdSpotList[0]));
                foreach (var spot in stdSpotList) {
                    sw.Write(GetMetaInfo(spot));
                    sw.WriteLine(GetPeakHeightAsString(spot.AlignedPeakPropertyBeanCollection));
                }
            }

            filePath = exportDir + "\\" + Path.GetFileNameWithoutExtension(projectProperty.ProjectFilePath) + "_PeakAreaAndHeight.pdf";
            Msdial.Common.Export.DataExportAsPdf.ExportPeakAreaAndHightInTargetMode(filePath, stdSpotList, analysisFiles[0].AnalysisFilePropertyBean.AnalysisFileName);
        }

        public static void ExportRetentionTimeCorrectionResults(StreamWriter sw, List<AnalysisFileBean> analysisFileBeans) {
            foreach (var file in analysisFileBeans) {
                foreach (var s in file.RetentionTimeCorrectionBean.StandardList) {
                    sw.WriteLine(file.AnalysisFilePropertyBean.AnalysisFileName + "\t" + s.Reference.MetaboliteName + "\t" + s.Reference.RetentionTime + "\t" + s.SamplePeakAreaBean.RtAtPeakTop);
                }
            }
        }

        public static List<AlignmentPropertyBean> GetInternalStdSpot(ObservableCollection<AlignmentPropertyBean> alignmentProperty, List<TextFormatCompoundInformationBean> libList) {
            var newPropList = new List<AlignmentPropertyBean>();

            var ms1Tol = 0.015f;// param.Ms1LibrarySearchTolerance;
            var rtTol = 0.2;
            var intTol = 50000;

            foreach (var std in libList) {
                ms1Tol = std.AccurateMassTolerance;
                rtTol = std.RetentionTimeTolerance;
                intTol = (int)std.MinimumPeakHeight;
                foreach (var spot in alignmentProperty) {
                    if (Math.Abs(std.AccurateMass - spot.CentralAccurateMass) < ms1Tol &&
                        Math.Abs(std.RetentionTime - spot.CentralRetentionTime) < rtTol &&
                        spot.AverageValiable > intTol) {
                        spot.MetaboliteName = std.MetaboliteName;
                        newPropList.Add(spot);
                        break;
                    }
                }
            }

            return newPropList;
        }

        private static string GetHeader(AlignmentPropertyBean p) {
            string res = "Alignment ID\tAccurateMass\tRetentionTime\tAnnotation\tFillPercent\tAdduct";
            foreach (var s in p.AlignedPeakPropertyBeanCollection) {
                res = res + "\t" + s.FileName;
            }
            return res;
        }

        private static string GetMetaInfo(AlignmentPropertyBean p) {
            return p.AlignmentID + "\t" + p.CentralAccurateMass.ToString() + "\t" + p.CentralRetentionTime.ToString() + "\t" +
                        p.MetaboliteName + "\t" + p.FillParcentage.ToString() + "\t" + p.AdductIonName + "\t";
        }

        private static string GetPeakHeightAsString(ObservableCollection<AlignedPeakPropertyBean> props) {
            string res = "";
            foreach (var s in props) {
                res = res + "\t" + s.Variable;
            }
            return res;
        }

        private static string GetRetentionTimeAsString(ObservableCollection<AlignedPeakPropertyBean> props) {
            string res = "";

            foreach (var s in props) {
                res = res + "\t" + s.RetentionTime;
            }
            return res;
        }

        private static string GetPeakAreaAsString(ObservableCollection<AlignedPeakPropertyBean> props) {
            string res = "";
            foreach (var s in props) {
                res = res + "\t" + s.Area;
            }
            return res;
        }


        public static void Export2MsFinder(ProjectPropertyBean projectProperty, RdamPropertyBean rdamProperty, List<AnalysisFileBean> analysisFiles,
            AlignmentFileBean alignmentFileBean,
            AlignmentResultBean alignmentResult, List<MspFormatCompoundInformationBean> mspDB, AnalysisParametersBean param, int alignmentPeakID, bool withCorrDec) {
            var timeString = DateTime.Now.ToString("yy_MM_dd_HH_mm");
            var exportDir = Path.GetDirectoryName(projectProperty.ProjectFilePath) + "\\" + Path.GetFileNameWithoutExtension(projectProperty.ProjectFilePath) + "_" + timeString + "_MsFinderExport";
            if (exportDir.Length > 200) {
                exportDir = Path.GetDirectoryName(projectProperty.ProjectFilePath) + "\\MsFinder" + timeString;
            }
            if (exportDir.Length > 260) return;
            if (Directory.Exists(exportDir)) Directory.Delete(exportDir, true);
            Directory.CreateDirectory(exportDir);
            var alignmentProp = alignmentResult.AlignmentPropertyBeanCollection[alignmentPeakID];

            for (var i = 0; i < analysisFiles.Count; i++) {
                var id = alignmentResult.AlignmentPropertyBeanCollection[alignmentPeakID].AlignedPeakPropertyBeanCollection[i].PeakID;
                if (id < 0) continue;
                var spec = DataAccessLcUtility.GetRdamSpectrumCollection(projectProperty, rdamProperty, analysisFiles[i]);
                DataStorageLcUtility.SetPeakAreaBeanCollection(analysisFiles[i], analysisFiles[i].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);
                var peakArea = analysisFiles[i].PeakAreaBeanCollection[id];

                for (var j = 0; j < projectProperty.Ms2LevelIdList.Count; j++) {
                    using (var fs = new FileStream(analysisFiles[i].AnalysisFilePropertyBean.DeconvolutionFilePathList[j], FileMode.Open)) {
                        var seekpoint = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);
                        var res = SpectralDeconvolution.ReadMS2DecResult(fs, seekpoint, id);
                        var fileString = "";
                        var pathLength = analysisFiles[i].AnalysisFilePropertyBean.AnalysisFileName.Length + 25 + exportDir.Length;
                        if (pathLength < 250) {
                            fileString = analysisFiles[i].AnalysisFilePropertyBean.AnalysisFileName;
                        }
                        else {
                            var fileStrings = analysisFiles[i].AnalysisFilePropertyBean.AnalysisFileName.Split('_');
                            fileString = fileStrings[0] + "_" + fileStrings[2] + "_" + fileStrings[3];
                        }
                        var filePath = exportDir + "\\" + "MS2DecRes_" + i + "_" + projectProperty.ExperimentID_AnalystExperimentInformationBean[projectProperty.Ms2LevelIdList[j]].Name + "_" +
                            fileString + "_" + Math.Round(peakArea.RtAtPeakTop, 2).ToString("00.00") + "_" + Math.Round(peakArea.AccurateMass, 2).ToString("000.000") +
                             ".mat";

                        if (res.MassSpectra != null && res.MassSpectra.Count > 0)
                            using (var sw = new StreamWriter(filePath, false, Encoding.ASCII))
                                writeMsAnnotationTag(sw, projectProperty, mspDB, res, peakArea, j, analysisFiles[i], alignmentProp.MetaboliteName, MspDataRetrieve.GetInChIKey(peakArea.LibraryID, mspDB), MspDataRetrieve.GetSMILES(peakArea.LibraryID, mspDB), MspDataRetrieve.GetFormula(peakArea.LibraryID, mspDB));
                    }
                }
                DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(analysisFiles[i]);
            }
            if (withCorrDec) {
                for (var i = 0; i < projectProperty.Ms2LevelIdList.Count; i++) {
                    var fp = projectProperty.ProjectFolderPath + "\\" + System.IO.Path.GetFileNameWithoutExtension(alignmentFileBean.FilePath) + "_CorrelationBasedDecRes_Raw_" + i + ".cbd";
                    using (var fs = File.Open(fp, FileMode.Open)) {
                        var seekpoint = CorrDecHandler.ReadSeekPointsOfCorrelDec(fs);
                        var decRes = CorrDecHandler.ReadCorrelDecResult(fs, seekpoint, alignmentPeakID);
                        var filePath = exportDir + "\\" + "CorrelDecRes" + "_" + projectProperty.ExperimentID_AnalystExperimentInformationBean[projectProperty.Ms2LevelIdList[i]].Name + "_" +
                            Math.Round(alignmentProp.CentralRetentionTime, 2) + "_" + Math.Round(alignmentProp.CentralAccurateMass, 3) + ".mat";
                        List<Peak> masslist = CorrDecHandler.GetCorrDecSpectrumWithComment(decRes, param.AnalysisParamOfMsdialCorrDec, alignmentProp.CentralAccurateMass, (int)(alignmentProp.FillParcentage * alignmentProp.AlignedPeakPropertyBeanCollection.Count));
                        if (masslist.Count == 0) return;
                        var spot = alignmentResult.AlignmentPropertyBeanCollection[alignmentPeakID];
                        using (var sw = new StreamWriter(filePath, false, Encoding.ASCII))
                            writeMsAnnotationTag(sw, projectProperty, mspDB, masslist, spot, new ObservableCollection<AnalysisFileBean>(analysisFiles), i, alignmentProp.MetaboliteName,
                                MspDataRetrieve.GetInChIKey(spot.LibraryID, mspDB), MspDataRetrieve.GetSMILES(spot.LibraryID, mspDB), MspDataRetrieve.GetFormula(spot.LibraryID, mspDB));
                    }
                }
            }
        }

        public static void Export2MsFinder(ProjectPropertyBean projectProperty, RdamPropertyBean rdamProperty, List<AnalysisFileBean> analysisFiles,
             AlignmentFileBean alignmentFileBean,
             AlignmentResultBean alignmentResult, List<MspFormatCompoundInformationBean> mspDB, AnalysisParametersBean param, int alignmentPeakID, bool withCorrDec, string formula,
             string smiles, string inchikey, string exportDirTop, int targetId = -1) {

            var exportDir = exportDirTop + "\\All_" + analysisFiles[0].AnalysisFilePropertyBean.AnalysisFileName;
            var exportRawDir = exportDirTop + "\\Raw_" + analysisFiles[0].AnalysisFilePropertyBean.AnalysisFileName;
            var timeString = DateTime.Now.ToString("yy_MM_dd_HH_mm");
            if (Directory.Exists(exportDir)) Directory.Delete(exportDir, true);
            Directory.CreateDirectory(exportDir);
            if (Directory.Exists(exportRawDir)) Directory.Delete(exportRawDir, true);
            Directory.CreateDirectory(exportRawDir);
            var alignmentProp = alignmentResult.AlignmentPropertyBeanCollection[alignmentPeakID];

            for (var i = 0; i < analysisFiles.Count; i++) {
                var id = alignmentResult.AlignmentPropertyBeanCollection[alignmentPeakID].AlignedPeakPropertyBeanCollection[i].PeakID;
                if (id < 0) continue;
                var spec = DataAccessLcUtility.GetRdamSpectrumCollection(projectProperty, rdamProperty, analysisFiles[i]);
                DataStorageLcUtility.SetPeakAreaBeanCollection(analysisFiles[i], analysisFiles[i].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);
                var peakArea = analysisFiles[i].PeakAreaBeanCollection[id];

                for (var j = 0; j < projectProperty.Ms2LevelIdList.Count; j++) {
                    using (var fs = new FileStream(analysisFiles[i].AnalysisFilePropertyBean.DeconvolutionFilePathList[j], FileMode.Open)) {
                        var seekpoint = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);
                        var res = SpectralDeconvolution.ReadMS2DecResult(fs, seekpoint, id);
                        var fileString = "";
                        var pathLength = analysisFiles[i].AnalysisFilePropertyBean.AnalysisFileName.Length + 25 + exportDir.Length;
                        if (pathLength < 250) {
                            fileString = analysisFiles[i].AnalysisFilePropertyBean.AnalysisFileName;
                        }
                        else {
                            var fileStrings = analysisFiles[i].AnalysisFilePropertyBean.AnalysisFileName.Split('_');
                            fileString = fileStrings[0] + "_" + fileStrings[2] + "_" + fileStrings[3];
                        }

                        var fileNameRaw = "RawMS2_" + i + "_" + projectProperty.ExperimentID_AnalystExperimentInformationBean[projectProperty.Ms2LevelIdList[j]].Name + "_" +
                              fileString + "_" + Math.Round(peakArea.RtAtPeakTop, 2).ToString("00.00") + "_" + Math.Round(peakArea.AccurateMass, 2).ToString("000.000") +
                                ".mat";
                        var fileRawPath = exportRawDir + "\\" + fileNameRaw;
                        var rawSpectra = getMassSpectrogramBean(spec, peakArea.Ms2LevelDatapointNumberList[j], param.CentroidMs2Tolerance, param.PeakDetectionBasedCentroid, projectProperty.DataType);
                        if (rawSpectra != null && rawSpectra.Count > 0) {
                            if (File.Exists(fileRawPath)) File.Delete(fileRawPath);
                            using (var sw = new StreamWriter(fileRawPath, false, Encoding.ASCII)) {
                                writeMsAnnotationTag(sw, projectProperty, mspDB, rawSpectra, peakArea, j, analysisFiles[i], alignmentProp.MetaboliteName, inchikey, smiles, formula);
                            }
                        }

                        var fileName = "MS2DecRes_" + i + "_" + projectProperty.ExperimentID_AnalystExperimentInformationBean[projectProperty.Ms2LevelIdList[j]].Name + "_" +
                            fileString + "_" + Math.Round(peakArea.RtAtPeakTop, 2).ToString("00.00") + "_" + Math.Round(peakArea.AccurateMass, 2).ToString("000.000") +
                             ".mat";
                        var filePath = exportDir + "\\" + fileName;

                        if (res.MassSpectra != null && res.MassSpectra.Count > 0) {
                            if (File.Exists(filePath)) File.Delete(filePath);
                            using (var sw = new StreamWriter(filePath, false, Encoding.ASCII)) {
                                writeMsAnnotationTag(sw, projectProperty, mspDB, res, peakArea, j, analysisFiles[i], alignmentProp.MetaboliteName, inchikey, smiles, formula);
                            }
                        }

                        if (File.Exists(filePath) && targetId == i) {
                            if (File.Exists(exportDirTop + "\\" + fileName)) File.Delete(exportDirTop + "\\" + fileName);
                            File.Copy(filePath, exportDirTop + "\\" + fileName);
                        }
                    }
                }
                DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(analysisFiles[i]);
            }
            if (withCorrDec) {
                for (var i = 0; i < projectProperty.Ms2LevelIdList.Count; i++) {
                    var fp = projectProperty.ProjectFolderPath + "\\" + System.IO.Path.GetFileNameWithoutExtension(alignmentFileBean.FilePath) + "_CorrelationBasedDecRes_Raw_" + i + ".cbd";
                    using (var fs = File.Open(fp, FileMode.Open)) {
                        var seekpoint = CorrDecHandler.ReadSeekPointsOfCorrelDec(fs);
                        var decRes = CorrDecHandler.ReadCorrelDecResult(fs, seekpoint, alignmentPeakID);
                        var fileName = "CorrelDecRes" + "_" + projectProperty.ExperimentID_AnalystExperimentInformationBean[projectProperty.Ms2LevelIdList[i]].Name + "_" +
                            Math.Round(alignmentProp.CentralRetentionTime, 2) + "_" + Math.Round(alignmentProp.CentralAccurateMass, 3) + ".mat";
                        var filePath = exportDir + "\\" + fileName;
                        List<Peak> masslist = CorrDecHandler.GetCorrDecSpectrumWithComment(decRes, param.AnalysisParamOfMsdialCorrDec, alignmentProp.CentralAccurateMass, (int)(alignmentProp.FillParcentage * alignmentProp.AlignedPeakPropertyBeanCollection.Count));
                        if (masslist.Count == 0) return;

                        if (File.Exists(filePath)) File.Delete(filePath);
                        using (var sw = new StreamWriter(filePath, false, Encoding.ASCII)) {
                            writeMsAnnotationTag(sw, projectProperty, mspDB, masslist, alignmentResult.AlignmentPropertyBeanCollection[alignmentPeakID], new ObservableCollection<AnalysisFileBean>(analysisFiles), i, alignmentProp.MetaboliteName, inchikey, smiles, formula);
                        }
                        if (File.Exists(filePath)) {
                            if (File.Exists(exportDirTop + "\\" + fileName)) File.Delete(exportDirTop + "\\" + fileName);
                            File.Copy(filePath, exportDirTop + "\\" + fileName);
                        }
                    }
                }
            }
        }

        private static List<Peak> getMassSpectrogramBean(ObservableCollection<RawSpectrum> spectrumCollection,
            int msScanPoint, float massBin, bool peakDetectionBasedCentroid, DataType dataType) {
            if (msScanPoint < 0) return null;

            ObservableCollection<double[]> masslist = new ObservableCollection<double[]>();
            ObservableCollection<double[]> centroidedMassSpectra = new ObservableCollection<double[]>();
            RawSpectrum spectrum;
            RawPeakElement[] massSpectra;

            spectrum = spectrumCollection[msScanPoint];
            massSpectra = spectrum.Spectrum;

            for (int i = 0; i < massSpectra.Length; i++)
                masslist.Add(new double[] { massSpectra[i].Mz, massSpectra[i].Intensity });

            if (dataType == DataType.Profile) {
                centroidedMassSpectra = SpectralCentroiding.Centroid(masslist, massBin, peakDetectionBasedCentroid);
            }
            else {
                centroidedMassSpectra = masslist;
            }

            var output = new List<Peak>();
            foreach(var spec in centroidedMassSpectra) {
                output.Add(new Peak() { Mz = spec[0], Intensity = spec[1] });
            }
            return output;
        }
            /// Raw spectrum
            /// 
         private static void writeMsAnnotationTag(StreamWriter sw, ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB, List<Peak> rawSpectra, PeakAreaBean pab, int id, AnalysisFileBean file,
             string name, string inchikey, string smiles, string formula) {
            if (name == string.Empty || name.Contains("w/o")) name = "Unknown";
            var adduct = pab.AdductIonName;
            if ((adduct == null || adduct == string.Empty) && projectProp.IonMode == IonMode.Positive) adduct = "[M+H]+";
            else if ((adduct == null || adduct == string.Empty) && projectProp.IonMode == IonMode.Negative) adduct = "[M-H]-";

            sw.WriteLine("NAME: " + name);
            sw.WriteLine("SCANNUMBER: " + pab.PeakID);
            sw.WriteLine("RETENTIONTIME: " + pab.RtAtPeakTop);
            sw.WriteLine("PRECURSORMZ: " + pab.AccurateMass);
            sw.WriteLine("PRECURSORTYPE: " + adduct);
            sw.WriteLine("IONMODE: " + projectProp.IonMode);
            sw.WriteLine("SPECTRUMTYPE: Centroid");
            sw.WriteLine("INTENSITY: " + pab.IntensityAtPeakTop);
            sw.WriteLine("INCHIKEY: " + inchikey);
            sw.WriteLine("SMILES: " + smiles);
            sw.WriteLine("FORMULA: " + formula);

            if (projectProp.FinalSavedDate != null) {
                sw.WriteLine("DATE: " + projectProp.FinalSavedDate.Date);
            }

            if (projectProp.Authors != null && projectProp.Authors != string.Empty) {
                sw.WriteLine("AUTHORS: " + projectProp.Authors);
            }

            if (projectProp.License != null && projectProp.License != string.Empty) {
                sw.WriteLine("LICENSE: " + projectProp.License);
            }

            if (projectProp.CollisionEnergyList != null && projectProp.CollisionEnergyList[id] >= 0) {
                sw.WriteLine("COLLISIONENERGY: " + projectProp.CollisionEnergyList[projectProp.Ms2LevelIdList[id]]);
            }

            if (projectProp.InstrumentType != null && projectProp.InstrumentType != string.Empty) {
                sw.WriteLine("INSTRUMENTTYPE: " + projectProp.InstrumentType);
            }

            if (projectProp.Instrument != null && projectProp.Instrument != string.Empty) {
                sw.WriteLine("INSTRUMENT: " + projectProp.Instrument);
            }

            var com = "Raw spectrum; ";
            if ((pab.Comment != null && pab.Comment != string.Empty))
                com = pab.Comment + "; ";
            else
                com = "";
            if (projectProp.Comment != null && projectProp.Comment != string.Empty)
                com = com + System.IO.Path.GetFileNameWithoutExtension(file.AnalysisFilePropertyBean.AnalysisFilePath) + "; " + projectProp.Comment;
            else
                com = com + System.IO.Path.GetFileNameWithoutExtension(file.AnalysisFilePropertyBean.AnalysisFilePath);

            sw.WriteLine("COMMENT: " + com);

            sw.Write("MSTYPE: ");
            sw.WriteLine("MS1");

            sw.WriteLine("Num Peaks: 3");
            sw.WriteLine(Math.Round(pab.AccurateMass, 5) + "\t" + pab.IntensityAtPeakTop);
            sw.WriteLine(Math.Round(pab.AccurateMass + 1.00335, 5) + "\t" + pab.Ms1IsotopicIonM1PeakHeight);
            sw.WriteLine(Math.Round(pab.AccurateMass + 2.00671, 5) + "\t" + pab.Ms1IsotopicIonM2PeakHeight);

            sw.Write("MSTYPE: ");
            sw.WriteLine("MS2");

            sw.Write("Num Peaks: ");
            sw.WriteLine(rawSpectra.Count);

            for (int i = 0; i < rawSpectra.Count; i++)
                sw.WriteLine(Math.Round(rawSpectra[i].Mz, 5) + "\t" + Math.Round(rawSpectra[i].Intensity, 1));
        }

        ///  MS2Dec
        private static void writeMsAnnotationTag(StreamWriter sw, ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB, MS2DecResult ms2DecResult, PeakAreaBean pab, int id, AnalysisFileBean file,
            string name, string inchikey, string smiles, string formula) {
            if (name == string.Empty || name.Contains("w/o")) name = "Unknown";
            var adduct = pab.AdductIonName;
            if ((adduct == null || adduct == string.Empty) && projectProp.IonMode == IonMode.Positive) adduct = "[M+H]+";
            else if ((adduct == null || adduct == string.Empty) && projectProp.IonMode == IonMode.Negative) adduct = "[M-H]-";

            sw.WriteLine("NAME: " + name);
            sw.WriteLine("SCANNUMBER: " + pab.PeakID);
            sw.WriteLine("RETENTIONTIME: " + ms2DecResult.PeakTopRetentionTime);
            sw.WriteLine("PRECURSORMZ: " + ms2DecResult.Ms1AccurateMass);
            sw.WriteLine("PRECURSORTYPE: " + adduct);
            sw.WriteLine("IONMODE: " + projectProp.IonMode);
            sw.WriteLine("SPECTRUMTYPE: Centroid");
            sw.WriteLine("INTENSITY: " + ms2DecResult.Ms1PeakHeight);
            sw.WriteLine("INCHIKEY: " + inchikey);
            sw.WriteLine("SMILES: " + smiles);
            sw.WriteLine("FORMULA: " + formula);

            if (projectProp.FinalSavedDate != null) {
                sw.WriteLine("DATE: " + projectProp.FinalSavedDate.Date);
            }

            if (projectProp.Authors != null && projectProp.Authors != string.Empty) {
                sw.WriteLine("AUTHORS: " + projectProp.Authors);
            }

            if (projectProp.License != null && projectProp.License != string.Empty) {
                sw.WriteLine("LICENSE: " + projectProp.License);
            }

            if (projectProp.CollisionEnergyList != null && projectProp.CollisionEnergyList[id] >= 0) {
                sw.WriteLine("COLLISIONENERGY: " + projectProp.CollisionEnergyList[projectProp.Ms2LevelIdList[id]]);
            }

            if (projectProp.InstrumentType != null && projectProp.InstrumentType != string.Empty) {
                sw.WriteLine("INSTRUMENTTYPE: " + projectProp.InstrumentType);
            }

            if (projectProp.Instrument != null && projectProp.Instrument != string.Empty) {
                sw.WriteLine("INSTRUMENT: " + projectProp.Instrument);
            }

            var com = "MS2Dec; ";
            if ((pab.Comment != null && pab.Comment != string.Empty))
                com = pab.Comment + "; ";
            else
                com = "";
            if (projectProp.Comment != null && projectProp.Comment != string.Empty)
                com = com + System.IO.Path.GetFileNameWithoutExtension(file.AnalysisFilePropertyBean.AnalysisFilePath) + "; " + projectProp.Comment;
            else
                com = com + System.IO.Path.GetFileNameWithoutExtension(file.AnalysisFilePropertyBean.AnalysisFilePath);

            sw.WriteLine("COMMENT: " + com);

            sw.Write("MSTYPE: ");
            sw.WriteLine("MS1");

            sw.WriteLine("Num Peaks: 3");
            sw.WriteLine(Math.Round(ms2DecResult.Ms1AccurateMass, 5) + "\t" + ms2DecResult.Ms1PeakHeight);
            sw.WriteLine(Math.Round(ms2DecResult.Ms1AccurateMass + 1.00335, 5) + "\t" + ms2DecResult.Ms1IsotopicIonM1PeakHeight);
            sw.WriteLine(Math.Round(ms2DecResult.Ms1AccurateMass + 2.00671, 5) + "\t" + ms2DecResult.Ms1IsotopicIonM2PeakHeight);

            sw.Write("MSTYPE: ");
            sw.WriteLine("MS2");

            sw.Write("Num Peaks: ");
            sw.WriteLine(ms2DecResult.MassSpectra.Count);

            for (int i = 0; i < ms2DecResult.MassSpectra.Count; i++)
                sw.WriteLine(Math.Round(ms2DecResult.MassSpectra[i][0], 5) + "\t" + Math.Round(ms2DecResult.MassSpectra[i][1], 0));
        }

        // CbDec
        private static void writeMsAnnotationTag(StreamWriter sw, ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB, List<Peak> massList, 
            AlignmentPropertyBean alignmentProperty, ObservableCollection<AnalysisFileBean> files, int id,
            string name, string inchikey, string smiles, string formula) {
            if (name == string.Empty || name.Contains("w/o")) name = "Unknown";
            var adduct = alignmentProperty.AdductIonName;
            if ((adduct == null || adduct == string.Empty) && projectProp.IonMode == IonMode.Positive) adduct = "[M+H]+";
            else if ((adduct == null || adduct == string.Empty) && projectProp.IonMode == IonMode.Negative) adduct = "[M-H]-";

            sw.WriteLine("NAME: " + name);
            sw.WriteLine("SCANNUMBER: " + alignmentProperty.AlignmentID);
            sw.WriteLine("RETENTIONTIME: " + alignmentProperty.CentralRetentionTime);
            sw.WriteLine("PRECURSORMZ: " + alignmentProperty.CentralAccurateMass);
            sw.WriteLine("PRECURSORTYPE: " + adduct);
            sw.WriteLine("IONMODE: " + projectProp.IonMode);
            sw.WriteLine("SPECTRUMTYPE: Centroid");
            sw.WriteLine("INTENSITY: " + alignmentProperty.AverageValiable);
            sw.WriteLine("INCHIKEY: " + inchikey);
            sw.WriteLine("SMILES: " + smiles);
            sw.WriteLine("FORMULA: " + formula);

            if (projectProp.FinalSavedDate != null) {
                sw.WriteLine("DATE: " + projectProp.FinalSavedDate.Date);
            }

            if (projectProp.Authors != null && projectProp.Authors != string.Empty) {
                sw.WriteLine("AUTHORS: " + projectProp.Authors);
            }

            if (projectProp.License != null && projectProp.License != string.Empty) {
                sw.WriteLine("LICENSE: " + projectProp.License);
            }

            sw.WriteLine("COLLISIONENERGY: " + projectProp.CollisionEnergyList[projectProp.Ms2LevelIdList[id]]);

            if (projectProp.InstrumentType != null && projectProp.InstrumentType != string.Empty) {
                sw.WriteLine("INSTRUMENTTYPE: " + projectProp.InstrumentType);
            }

            if (projectProp.Instrument != null && projectProp.Instrument != string.Empty) {
                sw.WriteLine("INSTRUMENT: " + projectProp.Instrument);
            }

            var com = "CorrelDec; " + projectProp.ProjectFilePath;
            if ((alignmentProperty.Comment != null && alignmentProperty.Comment != string.Empty))
                com = com + ", " + alignmentProperty.Comment + "; ";
            if (projectProp.Comment != null && projectProp.Comment != string.Empty)
                com = com + "; " + projectProp.Comment;

            sw.WriteLine("COMMENT: " + com);
            sw.Write("MSTYPE: ");
            sw.WriteLine("MS1");
            sw.Write("MSTYPE: ");
            sw.WriteLine("MS2");

            sw.Write("Num Peaks: ");
            sw.WriteLine(massList.Count);

            for (int i = 0; i < massList.Count; i++) {
                if (string.IsNullOrEmpty(massList[i].Comment)) {
                    sw.WriteLine(Math.Round(massList[i].Mz, 5) + "\t" + Math.Round(massList[i].Intensity, 1));
                }
                else {
                    sw.WriteLine(Math.Round(massList[i].Mz, 5) + "\t" + Math.Round(massList[i].Intensity, 1) + "\t" + "\"" + massList[i].Comment + "\"");
                }
            }
        }
    }
}
