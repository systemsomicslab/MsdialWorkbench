using Microsoft.Win32;
using Msdial.Lcms.Dataprocess.Algorithm;
using Msdial.Lcms.Dataprocess.Utility;
using Rfx.Riken.OsakaUniv.MsViewer;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using CompMs.Common.DataObj;

namespace Rfx.Riken.OsakaUniv
{
    class TimeRecorder {
        public Stopwatch StopWatchForWhole { get; set; }
        public Stopwatch StopWatchForPeak { get; set; }
        public Stopwatch StopWatchForAlignment { get; set; }

        public TimeRecorder() { }
        public void InitializeForWhole() {
            StopWatchForWhole.Start();
        }
        public void InitializeForPeak() {
            StopWatchForPeak.Start();
        }
        public void InitializeForAlignment() {
            StopWatchForAlignment.Start();
        }
    }

    class UtilityForAIF {

        #region MS finder Export
        // common
        private static void folderCheckInMsFinder(string msdialTempDir) {
            if (System.IO.Directory.Exists(msdialTempDir)) return;
            else {
                var di = System.IO.Directory.CreateDirectory(msdialTempDir);
            }
        }

        public static bool CheckFilePath(MsDialIniField iniField) {
            var path = iniField.MsfinderFilePath;
            while (!System.IO.File.Exists(path)) {
                if (MessageBox.Show("The file path of MS-FINDER program is not correct. Please select the correct path.", "Error", MessageBoxButton.OKCancel, MessageBoxImage.Error) == MessageBoxResult.Cancel)
                    return false;

                var ofd = new OpenFileDialog();
                ofd.Filter = "MS-FINDER Exe (*.exe)|*.exe";
                ofd.Title = "Select MS-FINDER Exe File";
                ofd.RestoreDirectory = true;

                if (ofd.ShowDialog() == true)
                    path = ofd.FileName;
                else
                    return false;
            }

            iniField.MsfinderFilePath = path;
            MsDialIniParcer.Write(iniField);

            return true;
        }


        // Alignment results
        public static void SendToMsFinderProgram(AifViewControlCommonProperties commonProp, MS2DecResult ms2DecRes, AlignmentPropertyBean alignmentProp, string name, int id) {
            var iniField = commonProp.MsDialIniField;
            if (!CheckFilePath(iniField)) return;

            var msdialTempDir = System.IO.Path.GetDirectoryName(iniField.MsfinderFilePath) + "\\MSDIAL_TEMP";
            folderCheckInMsFinder(msdialTempDir);

            var timeString = DateTime.Now.ToString("yy_MM_dd_HH_mm_ss");
            var fileString = commonProp.AnalysisFile.AnalysisFilePropertyBean.AnalysisFileName;
            var filePath = msdialTempDir + "\\" + timeString + "_" + fileString + "_" + Math.Round(alignmentProp.CentralRetentionTime, 2).ToString() + "_" + Math.Round(alignmentProp.CentralAccurateMass, 2).ToString() + "_AlignmentRes_" + name + "." + SaveFileFormat.mat;

            msAnnotationTagExport(commonProp.ProjectProperty, commonProp.Param, ms2DecRes, commonProp.MspDB, alignmentProp, filePath, commonProp.AnalysisFiles, id);
            var process = new Process();
            process.StartInfo.FileName = commonProp.MsDialIniField.MsfinderFilePath;
            process.StartInfo.Arguments = msdialTempDir;
            process.Start();
        }

        public static void SendToMsFinderProgram(AifViewControlCommonProperties commonProp, CorrDecResult correlDecRes, AlignmentPropertyBean alignmentProp, string name, int id) {
            var iniField = commonProp.MsDialIniField;
            if (!CheckFilePath(iniField)) return;

            var msdialTempDir = System.IO.Path.GetDirectoryName(iniField.MsfinderFilePath) + "\\MSDIAL_TEMP";
            folderCheckInMsFinder(msdialTempDir);

            var timeString = DateTime.Now.ToString("yy_MM_dd_HH_mm_ss");
            var fileString = commonProp.AnalysisFile.AnalysisFilePropertyBean.AnalysisFileName;
            var filePath = msdialTempDir + "\\" + timeString + "_" + fileString + "_" + Math.Round(alignmentProp.CentralRetentionTime, 2).ToString() + "_" + Math.Round(alignmentProp.CentralAccurateMass, 2).ToString() + "_AlignmentRes_" + name + "." + SaveFileFormat.mat;

            msAnnotationTagExport(commonProp.ProjectProperty, commonProp.Param, correlDecRes, commonProp.MspDB, alignmentProp, filePath, commonProp.AnalysisFiles, id);
            var process = new Process();
            process.StartInfo.FileName = commonProp.MsDialIniField.MsfinderFilePath;
            process.StartInfo.Arguments = msdialTempDir;
            process.Start();
        }

        //  Deconvoluted spectrum
        public static void SendToMsFinderProgram(AifViewControlCommonProperties commonProp, MS2DecResult ms2DecRes, PeakAreaBean peakArea, string name, int id) {
            var iniField = commonProp.MsDialIniField;
            if (!CheckFilePath(iniField)) return;

            var msdialTempDir = System.IO.Path.GetDirectoryName(iniField.MsfinderFilePath) + "\\MSDIAL_TEMP";
            folderCheckInMsFinder(msdialTempDir);

            var timeString = DateTime.Now.ToString("yy_MM_dd_HH_mm_ss");
            var fileString = commonProp.AnalysisFile.AnalysisFilePropertyBean.AnalysisFileName;
            var filePath = msdialTempDir + "\\" + timeString + "_" + fileString + "_" + Math.Round(peakArea.RtAtPeakTop, 2).ToString() + "_" + Math.Round(peakArea.AccurateMass, 2).ToString() + "_Dec_" + name + "." + SaveFileFormat.mat;

            msAnnotationTagExport(commonProp.Spectrum, commonProp.ProjectProperty, commonProp.Param, peakArea, ms2DecRes, commonProp.MspDB, ExportspectraType.deconvoluted, id, filePath, commonProp.AnalysisFile);
            var process = new Process();
            process.StartInfo.FileName = commonProp.MsDialIniField.MsfinderFilePath;
            process.StartInfo.Arguments = msdialTempDir;
            process.Start();
        }

        // bulk export
        public static void SendToMsFinderProgram(AifViewControlCommonProperties commonProp, MS2DecResult ms2DecRes, PeakAreaBean peakArea, int id, string filePath, bool isMsp = false) {
            msAnnotationTagExport(commonProp.ProjectProperty, commonProp.Param, ms2DecRes, commonProp.MspDB, peakArea, filePath, id, commonProp.AnalysisFile, isMsp);
        }

        // bulk export for alignment
        public static void SendToMsFinderProgram(AifViewControlCommonProperties commonProp, MS2DecResult ms2DecRes, AlignmentPropertyBean alignmentPropertyBean, int id, string filePath, bool isMsp = false) {
            msAnnotationTagExport(commonProp.ProjectProperty, commonProp.Param, ms2DecRes, commonProp.MspDB, alignmentPropertyBean, filePath, commonProp.AnalysisFiles, id, isMsp);
        }

        // CorrDec
        public static void SendToMsFinderProgram(AifViewControlCommonProperties commonProp, CorrDecResult correlDecRes, AlignmentPropertyBean alignmentPropertyBean, int id, string filePath, bool isMsp = false)
        {
            msAnnotationTagExport(commonProp.ProjectProperty, commonProp.Param, correlDecRes, commonProp.MspDB, alignmentPropertyBean, filePath, commonProp.AnalysisFiles, id, isMsp);
        }

        // Raw spectrum
        public static void SendToMsFinderProgram(AifViewControlCommonProperties commonProp, PeakAreaBean peakArea, string name, int id) {
            var iniField = commonProp.MsDialIniField;
            if (!CheckFilePath(iniField)) return;

            var msdialTempDir = System.IO.Path.GetDirectoryName(iniField.MsfinderFilePath) + "\\MSDIAL_TEMP";
            folderCheckInMsFinder(msdialTempDir);

            var timeString = DateTime.Now.ToString("yy_MM_dd_HH_mm_ss");
            var fileString = commonProp.AnalysisFile.AnalysisFilePropertyBean.AnalysisFileName;
            var filePath = msdialTempDir + "\\" + timeString + "_" + fileString + "_" + Math.Round(peakArea.RtAtPeakTop, 2).ToString() + "_" + Math.Round(peakArea.AccurateMass, 2).ToString() + "_Raw_" + name + "." + SaveFileFormat.mat;

            msAnnotationTagExport(commonProp.Spectrum, commonProp.ProjectProperty, commonProp.Param, peakArea, null, commonProp.MspDB, ExportspectraType.centroid, id, filePath, commonProp.AnalysisFile);
            var process = new Process();
            process.StartInfo.FileName = commonProp.MsDialIniField.MsfinderFilePath;
            process.StartInfo.Arguments = msdialTempDir;
            process.Start();
        }

        private static ObservableCollection<double[]> getMs1SpectrumCollection(ObservableCollection<RawSpectrum> LcmsSpectrumCollection, ProjectPropertyBean projectProperty, AnalysisParametersBean param, PeakAreaBean peakAreaBean, ExportspectraType spectrumType) {
            var ms1Spectrum = new ObservableCollection<double[]>();

            if (spectrumType != ExportspectraType.profile && projectProperty.DataType == DataType.Profile) ms1Spectrum = DataAccessLcUtility.GetCentroidMassSpectra(LcmsSpectrumCollection, projectProperty.DataType, peakAreaBean.Ms1LevelDatapointNumber, param.CentroidMs1Tolerance, param.PeakDetectionBasedCentroid);
            else ms1Spectrum = DataAccessLcUtility.GetProfileMassSpectra(LcmsSpectrumCollection, peakAreaBean.Ms1LevelDatapointNumber);

            return ms1Spectrum;
        }

        private static ObservableCollection<double[]> getMs2SpectrumCollection(ObservableCollection<RawSpectrum> LcmsSpectrumCollection, ProjectPropertyBean projectProperty, AnalysisParametersBean param, PeakAreaBean peakAreaBean,
            MS2DecResult ms2DecRes, ExportspectraType spectrumType, int id) {
            var ms2Spectrum = new ObservableCollection<double[]>();

            if ((spectrumType == ExportspectraType.deconvoluted && projectProperty.MethodType == MethodType.diMSMS)) {
                var ms2List = ms2DecRes.MassSpectra;
                if (ms2List == null || ms2List.Count == 0) return new ObservableCollection<double[]>();
                else return new ObservableCollection<double[]>(ms2List);
            }
            else if (spectrumType == ExportspectraType.centroid && projectProperty.MethodType == MethodType.diMSMS) {
                ms2Spectrum = DataAccessLcUtility.GetCentroidMassSpectra(LcmsSpectrumCollection, projectProperty.DataTypeMS2, peakAreaBean.Ms2LevelDatapointNumberList[id], param.CentroidMs2Tolerance, param.PeakDetectionBasedCentroid);
            }
            else {
                ms2Spectrum = DataAccessLcUtility.GetProfileMassSpectra(LcmsSpectrumCollection, peakAreaBean.Ms2LevelDatapointNumberList[id]);
            }

            return ms2Spectrum;
        }

        // raw
        public static void msAnnotationTagExport(ObservableCollection<RawSpectrum> LcmsSpectrumCollection, ProjectPropertyBean projectProperty, AnalysisParametersBean param, PeakAreaBean peakArea, MS2DecResult ms2DecRes,
            List<MspFormatCompoundInformationBean> mspDB, ExportspectraType spectrumType, int id, string filePath, AnalysisFileBean file) {
            var ms1Spectrum = getMs1SpectrumCollection(LcmsSpectrumCollection, projectProperty, param, peakArea, spectrumType);
            var ms2Spectrum = getMs2SpectrumCollection(LcmsSpectrumCollection, projectProperty, param, peakArea, ms2DecRes, spectrumType, id);

            if (ms2Spectrum.Count <= 0) return;

            using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII)) {
                writeMsAnnotationTag(sw, peakArea, ms1Spectrum, ms2Spectrum, projectProperty, mspDB, spectrumType, id, file);
            }
        }

        // dec
        public static void msAnnotationTagExport(ProjectPropertyBean projectProperty, AnalysisParametersBean param, MS2DecResult ms2DecRes,
    List<MspFormatCompoundInformationBean> mspDB, PeakAreaBean peakArea, string filePath, int id, AnalysisFileBean file, bool isMsp = false) {
            if (ms2DecRes.MassSpectra.Count == 0) return;
            using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII)) {
                writeMsAnnotationTag(sw, projectProperty, mspDB, ms2DecRes, peakArea, id, file, isMsp);
            }
        }

        // alignment
        public static void msAnnotationTagExport(ProjectPropertyBean projectProperty, AnalysisParametersBean param, MS2DecResult ms2DecRes,
    List<MspFormatCompoundInformationBean> mspDB, AlignmentPropertyBean alignmentProperty, string filePath, ObservableCollection<AnalysisFileBean> files, int id, bool isMsp = false) {
            if (ms2DecRes.MassSpectra.Count == 0) return;
            using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII)) {
                writeMsAnnotationTag(sw, projectProperty, mspDB, ms2DecRes, alignmentProperty, files, id, isMsp);
            }
        }

        // correl
        public static void msAnnotationTagExport(ProjectPropertyBean projectProperty, AnalysisParametersBean param, CorrDecResult correlDecRes,
List<MspFormatCompoundInformationBean> mspDB, AlignmentPropertyBean alignmentProperty, string filePath, ObservableCollection<AnalysisFileBean> files, int id, bool isMsp = false) {
            List<Peak> masslist = new List<Peak>();
            var mz = alignmentProperty.CentralAccurateMass;
            masslist = CorrDecHandler.GetCorrDecSpectrumWithComment(correlDecRes, param.AnalysisParamOfMsdialCorrDec, alignmentProperty.CentralAccurateMass, (int)(alignmentProperty.AlignedPeakPropertyBeanCollection.Count * alignmentProperty.FillParcentage));

            if (masslist.Count == 0) return;
            using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII)) {
                writeMsAnnotationTag(sw, projectProperty, mspDB, masslist, alignmentProperty, files, id, isMsp);
            }
        }


        public static void writeMsAnnotationTag(StreamWriter sw, ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB, MS2DecResult ms2DecResult, AlignmentPropertyBean alignmentProperty, ObservableCollection<AnalysisFileBean> files, int id, bool isMsp = false) {
            var name = alignmentProperty.MetaboliteName;
            if (name == string.Empty) name = "Unknown";
            else if (!isMsp && name.Contains("w/o")) name = "Unknown";
            var adduct = alignmentProperty.AdductIonName;
            if ((adduct == null || adduct == string.Empty) && projectProp.IonMode == IonMode.Positive) adduct = "[M+H]+";
            else if ((adduct == null || adduct == string.Empty) && projectProp.IonMode == IonMode.Negative) adduct = "[M-H]-";

            sw.WriteLine("NAME: " + name);
            sw.WriteLine("SCANNUMBER: " + alignmentProperty.AlignmentID);
            sw.WriteLine("RETENTIONTIME: " + ms2DecResult.PeakTopRetentionTime);
            sw.WriteLine("PRECURSORMZ: " + ms2DecResult.Ms1AccurateMass);
            sw.WriteLine("PRECURSORTYPE: " + adduct);
            sw.WriteLine("IONMODE: " + projectProp.IonMode);
            sw.WriteLine("SPECTRUMTYPE: Centroid");
            sw.WriteLine("INTENSITY: " + ms2DecResult.Ms1PeakHeight);
            sw.WriteLine("INCHIKEY: " + MspDataRetrieve.GetInChIKey(alignmentProperty.LibraryID, mspDB));
            sw.WriteLine("SMILES: " + MspDataRetrieve.GetSMILES(alignmentProperty.LibraryID, mspDB));
            sw.WriteLine("FORMULA: " + MspDataRetrieve.GetFormula(alignmentProperty.LibraryID, mspDB));

            if (projectProp.FinalSavedDate != null) {
                sw.WriteLine("DATE: " + projectProp.FinalSavedDate.Date);
            }

            if (projectProp.Authors != null && projectProp.Authors != string.Empty) {
                sw.WriteLine("AUTHORS: " + projectProp.Authors);
            }

            if (projectProp.License != null && projectProp.License != string.Empty) {
                sw.WriteLine("LICENSE: " + projectProp.License);
            }

            if (projectProp.CollisionEnergyList != null && projectProp.CollisionEnergyList.Count > id && projectProp.CollisionEnergyList[id] >= 0 && projectProp.Ms2LevelIdList.Count > id) {
                sw.WriteLine("COLLISIONENERGY: " + projectProp.CollisionEnergyList[projectProp.Ms2LevelIdList[id]]);
            }
            else if (projectProp.CollisionEnergy != null && projectProp.CollisionEnergy != string.Empty) {
                sw.WriteLine("COLLISIONENERGY: " + projectProp.CollisionEnergy);
            }

            if (projectProp.InstrumentType != null && projectProp.InstrumentType != string.Empty) {
                sw.WriteLine("INSTRUMENTTYPE: " + projectProp.InstrumentType);
            }

            if (projectProp.Instrument != null && projectProp.Instrument != string.Empty) {
                sw.WriteLine("INSTRUMENT: " + projectProp.Instrument);
            }

            var com = "";
            if ((alignmentProperty.Comment != null && alignmentProperty.Comment != string.Empty))
                com = alignmentProperty.Comment + "; ";
            else
                com = "";
            if (projectProp.Comment != null && projectProp.Comment != string.Empty)
                com = com + System.IO.Path.GetFileNameWithoutExtension(files[alignmentProperty.RepresentativeFileID].AnalysisFilePropertyBean.AnalysisFilePath) + "; " + projectProp.Comment;
            else
                com = com + System.IO.Path.GetFileNameWithoutExtension(files[alignmentProperty.RepresentativeFileID].AnalysisFilePropertyBean.AnalysisFilePath);

            if (!isMsp) {
                sw.WriteLine("COMMENT: " + com);
                sw.Write("MSTYPE: ");
                sw.WriteLine("MS1");

                sw.WriteLine("Num Peaks: 3");
                sw.WriteLine(Math.Round(ms2DecResult.Ms1AccurateMass, 5) + "\t" + ms2DecResult.Ms1PeakHeight);
                sw.WriteLine(Math.Round(ms2DecResult.Ms1AccurateMass + 1.00335, 5) + "\t" + ms2DecResult.Ms1IsotopicIonM1PeakHeight);
                sw.WriteLine(Math.Round(ms2DecResult.Ms1AccurateMass + 2.00671, 5) + "\t" + ms2DecResult.Ms1IsotopicIonM2PeakHeight);

                sw.Write("MSTYPE: ");
                sw.WriteLine("MS2");
            }
            sw.Write("Num Peaks: ");
            var peaks = ms2DecResult.MassSpectra.Where(x => x[1] >= 0.5).ToList();
            sw.WriteLine(peaks.Count);

            for (int i = 0; i < peaks.Count; i++)
                sw.WriteLine(Math.Round(peaks[i][0], 5) + "\t" + Math.Round(peaks[i][1], 0));
        }

        // correl
        public static void writeMsAnnotationTag(StreamWriter sw, ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB, List<Peak> massList, AlignmentPropertyBean alignmentProperty, ObservableCollection<AnalysisFileBean> files, int id, bool isMsp) {
            var name = alignmentProperty.MetaboliteName;
            if (name == string.Empty || name.Contains("w/o")) name = "Unknown";
            var adduct = alignmentProperty.AdductIonName;
            if ((adduct == null || adduct == string.Empty) && projectProp.IonMode == IonMode.Positive) adduct = "[M+H]+";
            else if ((adduct == null || adduct == string.Empty) && projectProp.IonMode == IonMode.Negative) adduct = "[M-H]-";

            sw.WriteLine("NAME: " + name);
            sw.WriteLine("ALIGNMENTID: " + alignmentProperty.AlignmentID);
            sw.WriteLine("RETENTIONTIME: " + alignmentProperty.CentralRetentionTime);
            sw.WriteLine("PRECURSORMZ: " + alignmentProperty.CentralAccurateMass);
            sw.WriteLine("PRECURSORTYPE: " + adduct);
            sw.WriteLine("IONMODE: " + projectProp.IonMode);
            sw.WriteLine("INTENSITY: " + alignmentProperty.AverageValiable);
            sw.WriteLine("INCHIKEY: " + MspDataRetrieve.GetInChIKey(alignmentProperty.LibraryID, mspDB));
            sw.WriteLine("SMILES: " + MspDataRetrieve.GetSMILES(alignmentProperty.LibraryID, mspDB));
            sw.WriteLine("FORMULA: " + MspDataRetrieve.GetFormula(alignmentProperty.LibraryID, mspDB));

            if (projectProp.FinalSavedDate != null) {
                sw.WriteLine("DATE: " + projectProp.FinalSavedDate.Date);
            }

            if (projectProp.Authors != null && projectProp.Authors != string.Empty) {
                sw.WriteLine("AUTHORS: " + projectProp.Authors);
            }

            if (projectProp.License != null && projectProp.License != string.Empty) {
                sw.WriteLine("LICENSE: " + projectProp.License);
            }

            if (projectProp.CollisionEnergyList != null && projectProp.CollisionEnergyList.Count > id && projectProp.CollisionEnergyList[id] >= 0 && projectProp.Ms2LevelIdList.Count > id) {
                sw.WriteLine("COLLISIONENERGY: " + projectProp.CollisionEnergyList[projectProp.Ms2LevelIdList[id]]);
            } else if (projectProp.CollisionEnergy != null && projectProp.CollisionEnergy != string.Empty) {
                sw.WriteLine("COLLISIONENERGY: " + projectProp.CollisionEnergy);
            }

            if (projectProp.InstrumentType != null && projectProp.InstrumentType != string.Empty) {
                sw.WriteLine("INSTRUMENTTYPE: " + projectProp.InstrumentType);
            }

            if (projectProp.Instrument != null && projectProp.Instrument != string.Empty) {
                sw.WriteLine("INSTRUMENT: " + projectProp.Instrument);
            }

            var com = "CorrDec; " + projectProp.ProjectFilePath;
            if ((alignmentProperty.Comment != null && alignmentProperty.Comment != string.Empty))
                com = com + ", " + alignmentProperty.Comment + "; ";
            if (projectProp.Comment != null && projectProp.Comment != string.Empty)
                com = com + "; " + projectProp.Comment;

            sw.WriteLine("COMMENT: " + com);

            if (!isMsp)
            {
                sw.Write("MSTYPE: ");
                sw.WriteLine("MS1");
                sw.Write("MSTYPE: ");
                sw.WriteLine("MS2");
            }
            sw.Write("Num Peaks: ");
            sw.WriteLine(massList.Count);

            for (int i = 0; i < massList.Count; i++) {
                if (massList[i].Comment == "") {
                    sw.WriteLine(Math.Round(massList[i].Mz, 5) + "\t" + Math.Round(massList[i].Intensity, 1));
                }
                else {
                    sw.WriteLine(Math.Round(massList[i].Mz, 5) + "\t" + Math.Round(massList[i].Intensity, 1) + "\t\"" + massList[i].Comment + "\"");
                }
            }
        }

        public static void writeMsAnnotationTag(StreamWriter sw, ProjectPropertyBean projectProp, List<MspFormatCompoundInformationBean> mspDB, MS2DecResult ms2DecResult, PeakAreaBean pab, int id, AnalysisFileBean file, bool isMsp = false) {
            var name = pab.MetaboliteName;
            if (name == string.Empty) name = "Unknown";
            else if (!isMsp && name.Contains("w/o")) name = "Unknown";
            var adduct = pab.AdductIonName;
            if ((adduct == null || adduct == string.Empty) && projectProp.IonMode == IonMode.Positive) adduct = "[M+H]+";
            else if ((adduct == null || adduct == string.Empty) && projectProp.IonMode == IonMode.Negative) adduct = "[M-H]-";

            sw.WriteLine("NAME: " + name);
            sw.WriteLine("SCANNUMBER: " + pab.ScanNumberAtPeakTop);
            sw.WriteLine("PEAKID: " + pab.PeakID);
            sw.WriteLine("RETENTIONTIME: " + ms2DecResult.PeakTopRetentionTime);
            sw.WriteLine("PRECURSORMZ: " + ms2DecResult.Ms1AccurateMass);
            sw.WriteLine("PRECURSORTYPE: " + adduct);
            sw.WriteLine("IONMODE: " + projectProp.IonMode);
            sw.WriteLine("INTENSITY: " + ms2DecResult.Ms1PeakHeight);
            if (pab.LibraryIDList != null && pab.LibraryIDList.Count > 0) {
                var libid = pab.LibraryIDList[id] >= 0 ? pab.LibraryIDList[id] : pab.LibraryID;
                sw.WriteLine("INCHIKEY: " + MspDataRetrieve.GetInChIKey(libid, mspDB));
                sw.WriteLine("SMILES: " + MspDataRetrieve.GetSMILES(libid, mspDB));
                sw.WriteLine("FORMULA: " + MspDataRetrieve.GetFormula(libid, mspDB));
            }

            if (projectProp.FinalSavedDate != null) {
                sw.WriteLine("DATE: " + projectProp.FinalSavedDate.Date);
            }

            if (projectProp.Authors != null && projectProp.Authors != string.Empty) {
                sw.WriteLine("AUTHORS: " + projectProp.Authors);
            }

            if (projectProp.License != null && projectProp.License != string.Empty) {
                sw.WriteLine("LICENSE: " + projectProp.License);
            }

            if (projectProp.CollisionEnergyList != null && projectProp.CollisionEnergyList.Count > id && projectProp.CollisionEnergyList[id] >= 0 && projectProp.Ms2LevelIdList.Count > id) {
                sw.WriteLine("COLLISIONENERGY: " + projectProp.CollisionEnergyList[projectProp.Ms2LevelIdList[id]]);
            } else if (projectProp.CollisionEnergy != null && projectProp.CollisionEnergy != string.Empty) {
                sw.WriteLine("COLLISIONENERGY: " + projectProp.CollisionEnergy);
            }

            if (projectProp.InstrumentType != null && projectProp.InstrumentType != string.Empty) {
                sw.WriteLine("INSTRUMENTTYPE: " + projectProp.InstrumentType);
            }

            if (projectProp.Instrument != null && projectProp.Instrument != string.Empty) {
                sw.WriteLine("INSTRUMENT: " + projectProp.Instrument);
            }

            var com = "";
            if ((pab.Comment != null && pab.Comment != string.Empty))
                com = pab.Comment + "; ";
            else
                com = "";
            if (projectProp.Comment != null && projectProp.Comment != string.Empty)
                com = com + System.IO.Path.GetFileNameWithoutExtension(file.AnalysisFilePropertyBean.AnalysisFilePath) + "; " + projectProp.Comment;
            else
                com = com + System.IO.Path.GetFileNameWithoutExtension(file.AnalysisFilePropertyBean.AnalysisFilePath);

            sw.WriteLine("COMMENT: " + com);

            if (!isMsp) {
                sw.Write("MSTYPE: ");
                sw.WriteLine("MS1");

                sw.WriteLine("Num Peaks: 3");
                sw.WriteLine(Math.Round(ms2DecResult.Ms1AccurateMass, 5) + "\t" + ms2DecResult.Ms1PeakHeight);
                sw.WriteLine(Math.Round(ms2DecResult.Ms1AccurateMass + 1.00335, 5) + "\t" + ms2DecResult.Ms1IsotopicIonM1PeakHeight);
                sw.WriteLine(Math.Round(ms2DecResult.Ms1AccurateMass + 2.00671, 5) + "\t" + ms2DecResult.Ms1IsotopicIonM2PeakHeight);

                sw.Write("MSTYPE: ");
                sw.WriteLine("MS2");
            }
            sw.Write("Num Peaks: ");
            var peaks = ms2DecResult.MassSpectra.Where(x => x[1] >= 0.5).ToList();
            sw.WriteLine(peaks.Count);

            for (int i = 0; i < peaks.Count; i++)
                sw.WriteLine(Math.Round(peaks[i][0], 5) + "\t" + Math.Round(peaks[i][1], 0));
        }

        public static void writeMsAnnotationTag(StreamWriter sw, PeakAreaBean peakAreaBean,
            ObservableCollection<double[]> ms1Spectrum, ObservableCollection<double[]> ms2Spectrum,
            ProjectPropertyBean projectProperty, List<MspFormatCompoundInformationBean> mspDB, ExportspectraType spectraType, int id, AnalysisFileBean file, bool isMsp = false) {

            var name = peakAreaBean.MetaboliteName;
            if (name == string.Empty) name = "Unknown";
            else if (!isMsp && name.Contains("w/o")) name = "Unknown";

            var adduct = peakAreaBean.AdductIonName;
            if ((adduct == null || adduct == string.Empty) && projectProperty.IonMode == IonMode.Positive) adduct = "[M+H]+";
            else if ((adduct == null || adduct == string.Empty) && projectProperty.IonMode == IonMode.Negative) adduct = "[M-H]-";

            sw.WriteLine("NAME: " + name);
            sw.WriteLine("SCANNUMBER: " + peakAreaBean.ScanNumberAtPeakTop);
            sw.WriteLine("RETENTIONTIME: " + peakAreaBean.RtAtPeakTop);
            sw.WriteLine("PRECURSORMZ: " + peakAreaBean.AccurateMass);
            sw.WriteLine("PRECURSORTYPE: " + adduct);
            sw.WriteLine("IONMODE: " + projectProperty.IonMode);
            if (peakAreaBean.LibraryIDList != null && peakAreaBean.LibraryIDList.Count > 0) {
                sw.WriteLine("INCHIKEY: " + MspDataRetrieve.GetInChIKey(peakAreaBean.LibraryIDList[id], mspDB));
                sw.WriteLine("SMILES: " + MspDataRetrieve.GetSMILES(peakAreaBean.LibraryIDList[id], mspDB));
                sw.WriteLine("FORMULA: " + MspDataRetrieve.GetFormula(peakAreaBean.LibraryIDList[id], mspDB));
            }

            sw.Write("SPECTRUMTYPE: ");
            if (spectraType != ExportspectraType.profile) sw.WriteLine("Centroid");
            else sw.WriteLine("Profile");

            sw.WriteLine("INTENSITY: " + peakAreaBean.IntensityAtPeakTop);

            if (projectProperty.FinalSavedDate != null) {
                sw.WriteLine("DATE: " + projectProperty.FinalSavedDate.Date);
            }

            if (projectProperty.Authors != null && projectProperty.Authors != string.Empty) {
                sw.WriteLine("AUTHORS: " + projectProperty.Authors);
            }

            if (projectProperty.License != null && projectProperty.License != string.Empty) {
                sw.WriteLine("LICENSE: " + projectProperty.License);
            }

            if (projectProperty.CollisionEnergyList != null && projectProperty.CollisionEnergyList.Count > id && projectProperty.CollisionEnergyList[id] >= 0 && projectProperty.Ms2LevelIdList.Count > id) {
                sw.WriteLine("COLLISIONENERGY: " + projectProperty.CollisionEnergyList[projectProperty.Ms2LevelIdList[id]]);
            } else if (projectProperty.CollisionEnergy != null && projectProperty.CollisionEnergy != string.Empty) {
                sw.WriteLine("COLLISIONENERGY: " + projectProperty.CollisionEnergy);
            }

            if (projectProperty.InstrumentType != null && projectProperty.InstrumentType != string.Empty) {
                sw.WriteLine("INSTRUMENTTYPE: " + projectProperty.InstrumentType);
            }

            if (projectProperty.Instrument != null && projectProperty.Instrument != string.Empty) {
                sw.WriteLine("INSTRUMENT: " + projectProperty.Instrument);
            }

            var com = "";
            if ((peakAreaBean.Comment != null && peakAreaBean.Comment != string.Empty))
                com = peakAreaBean.Comment + "; ";
            else
                com = "";
            if (projectProperty.Comment != null && projectProperty.Comment != string.Empty)
                com = com + System.IO.Path.GetFileNameWithoutExtension(file.AnalysisFilePropertyBean.AnalysisFilePath) + "; " + projectProperty.Comment;
            else
                com = com + System.IO.Path.GetFileNameWithoutExtension(file.AnalysisFilePropertyBean.AnalysisFilePath);

            sw.WriteLine("COMMENT: " + com);

            if (!isMsp) {
                sw.WriteLine("MSTYPE: MS1");
                sw.WriteLine("Num Peaks: " + ms1Spectrum.Count);

                for (int i = 0; i < ms1Spectrum.Count; i++)
                    sw.WriteLine(Math.Round(ms1Spectrum[i][0], 5) + "\t" + Math.Round(ms1Spectrum[i][1], 0));

                sw.WriteLine("MSTYPE: MS2");
            }
            sw.WriteLine("Num Peaks: " + ms2Spectrum.Count);

            for (int i = 0; i < ms2Spectrum.Count; i++)
                sw.WriteLine(Math.Round(ms2Spectrum[i][0], 5) + "\t" + Math.Round(ms2Spectrum[i][1], 0));
        }


        #endregion

        #region MsViewer for Mass Spectrogram VM
        public static MassSpectrogramViewModel GetMs2MassspectrogramViewModel(MS2DecResult deconvolutionResultBean, MassSpectrogramBean referenceSpectraBean, string graphTitle) {
            float targetRt = deconvolutionResultBean.PeakTopRetentionTime;
            MassSpectrogramBean massSpectrogramBean = getMassSpectrogramBean(deconvolutionResultBean);
            return new MassSpectrogramViewModel(massSpectrogramBean, referenceSpectraBean, MassSpectrogramIntensityMode.Relative, 0, targetRt, graphTitle);
        }

        public static MassSpectrogramViewModel GetMs2MassspectrogramViewModel(MS2DecResult deconvolutionResultBean) {
            float targetRt = deconvolutionResultBean.PeakTopRetentionTime;
            MassSpectrogramBean massSpectrogramBean = getMassSpectrogramBean(deconvolutionResultBean);
            string graphTitle = "Deconvoluted MS/MS spectrum";

            return new MassSpectrogramViewModel(massSpectrogramBean, MassSpectrogramIntensityMode.Absolute, 0, deconvolutionResultBean.Ms1AccurateMass, targetRt, graphTitle);
        }

        public static MassSpectrogramViewModel GetMs2MassspectrogramViewModel(CorrDecResult deconvolutionResultBean, AnalysisParamOfMsdialCorrDec corrDecParam, AlignmentPropertyBean alignmentProp, MassSpectrogramBean referenceSpectraBean, string graphTitle) {
            float targetRt = alignmentProp.CentralRetentionTime;
            MassSpectrogramBean massSpectrogramBean = getMassSpectrogramBean(deconvolutionResultBean, corrDecParam, alignmentProp.CentralAccurateMass, alignmentProp.FillParcentage * alignmentProp.AlignedPeakPropertyBeanCollection.Count);
            return new MassSpectrogramViewModel(massSpectrogramBean, referenceSpectraBean, MassSpectrogramIntensityMode.Relative, 0, targetRt, graphTitle);
        }


        private static MassSpectrogramBean getMassSpectrogramBean(MS2DecResult deconvolutionResultBean) {
            ObservableCollection<MassSpectrogramDisplayLabel> massSpectraDisplayLabelCollection = new ObservableCollection<MassSpectrogramDisplayLabel>();
            List<double[]> masslist = new List<double[]>();

            if (deconvolutionResultBean.MassSpectra.Count == 0) {
                masslist.Add(new double[] { 0, 0 });
            }
            else {
                for (int i = 0; i < deconvolutionResultBean.MassSpectra.Count; i++)
                    masslist.Add(new double[] { deconvolutionResultBean.MassSpectra[i][0], deconvolutionResultBean.MassSpectra[i][1] });
            }
            masslist = masslist.OrderBy(n => n[0]).ToList();

            for (int i = 0; i < masslist.Count; i++)
                massSpectraDisplayLabelCollection.Add(new MassSpectrogramDisplayLabel() { Mass = masslist[i][0], Intensity = masslist[i][1], Label = Math.Round(masslist[i][0], 4).ToString() });

            return new MassSpectrogramBean(Brushes.Blue, 1.0, new ObservableCollection<double[]>(masslist), massSpectraDisplayLabelCollection);
        }

        private static MassSpectrogramBean getMassSpectrogramBean(CorrDecResult correlDecRes, AnalysisParamOfMsdialCorrDec corrDecParam, float mz, float numDetectedSamples) {
            ObservableCollection<MassSpectrogramDisplayLabel> massSpectraDisplayLabelCollection = new ObservableCollection<MassSpectrogramDisplayLabel>();
            List<double[]> masslist = CorrDecHandler.GetCorrDecSpectrum(correlDecRes, corrDecParam, mz, (int)(numDetectedSamples));
            if (masslist.Count == 0) {
                masslist.Add(new double[] { 0, 0 });
                massSpectraDisplayLabelCollection.Add(new MassSpectrogramDisplayLabel() { Mass = 0, Intensity = 0, Label = "none" });
            }
            else {
                foreach(var peak in masslist) {
                    massSpectraDisplayLabelCollection.Add(new MassSpectrogramDisplayLabel() { Mass = peak[0], Intensity = peak[1], Label = peak[0].ToString("0.00") });
                }
            }
            masslist = masslist.OrderBy(n => n[0]).ToList();

            return new MassSpectrogramBean(Brushes.Blue, 1.0, new ObservableCollection<double[]>(masslist), massSpectraDisplayLabelCollection);
        }


        public static MassSpectrogramViewModel GetMs2RawMassspectrogramViewModel(AifViewControlCommonProperties commonProp, PeakAreaBean peakAreaBean, int id) {
            float targetRt = peakAreaBean.RtAtPeakTop;

            MassSpectrogramBean massSpectrogramBean = getMassSpectrogramBean(commonProp.Spectrum, peakAreaBean.Ms2LevelDatapointNumberList[id], commonProp.Param.CentroidMs2Tolerance, commonProp.Param.PeakDetectionBasedCentroid, commonProp.ProjectProperty.DataType);

            string graphTitle = "Raw MS/MS spectrum";

            return new MassSpectrogramViewModel(massSpectrogramBean, MassSpectrogramIntensityMode.Absolute, peakAreaBean.Ms2LevelDatapointNumberList[id], targetRt, peakAreaBean.AccurateMass, graphTitle);
        }

        private static MassSpectrogramBean getMassSpectrogramBean(ObservableCollection<RawSpectrum> spectrumCollection,
     int msScanPoint, float massBin, bool peakDetectionBasedCentroid, DataType dataType) {
            if (msScanPoint < 0) return null;

            ObservableCollection<double[]> masslist = new ObservableCollection<double[]>();
            ObservableCollection<double[]> centroidedMassSpectra = new ObservableCollection<double[]>();
            ObservableCollection<MassSpectrogramDisplayLabel> massSpectraDisplayLabelCollection = new ObservableCollection<MassSpectrogramDisplayLabel>();
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

            if (centroidedMassSpectra == null || centroidedMassSpectra.Count == 0) {
                return null;
            }
            else {
                for (int i = 0; i < centroidedMassSpectra.Count; i++)
                    massSpectraDisplayLabelCollection.Add(new MassSpectrogramDisplayLabel() { Mass = centroidedMassSpectra[i][0], Intensity = centroidedMassSpectra[i][1], Label = Math.Round(centroidedMassSpectra[i][0], 4).ToString() });

                return new MassSpectrogramBean(Brushes.Blue, 1.0, centroidedMassSpectra, massSpectraDisplayLabelCollection);
            }
        }


        public static void SetNoReferense(List<string> CompNameList, List<List<float>> ScoresList, List<MassSpectrogramBean> ReferenceMassSpectrogramList) {
            CompNameList.Add("No Hit");
            ScoresList.Add(nullScores());
            ReferenceMassSpectrogramList.Add(new MassSpectrogramBean(Brushes.Red, 1.0, null));
        }

        public static void SetNoReferense(string CompName, List<List<float>> ScoresList, MassSpectrogramBean ReferenceMassSpectrogram) {
            CompName = "No Hit";
            ScoresList.Add(nullScores());
            ReferenceMassSpectrogram  = new MassSpectrogramBean(Brushes.Red, 1.0, null);
        }


        private static List<float> nullScores() {
            var res = new List<float>();
            for (var i = 0; i < 8; i++) {
                res.Add(-1);
            }
            return res;
        }


        public static List<float> GetScores(AnalysisParametersBean param, MspFormatCompoundInformationBean msp, ObservableCollection<double[]> ms2Spectra,
            float accurateMass, float retentionTime, TargetOmics targetOmics = TargetOmics.Metablomics) {
            double totalSimilarity = 0, spectraSimilarity = 0, reverseSearchSimilarity = 0, presenseSimilarity = 0, accurateMassSimilarity = 0, rtSimilarity = 0, isotopeSimilarity = -1, simpleSim = 0;
            var res = new List<float>();
            spectraSimilarity = -1; reverseSearchSimilarity = -1; presenseSimilarity = -1; accurateMassSimilarity = -1; rtSimilarity = -1;
            var dotProductFactor = 3.0;
            var reverseDotProdFactor = 2.0;
            var presensePercentageFactor = 1.0;
            var masslist = new ObservableCollection<double[]>();
            var masslist2 = new List<double[]>();
            var massBegin = param.Ms2MassRangeBegin;
            var massEnd = param.Ms2MassRangeEnd;
            accurateMassSimilarity = Msdial.Lcms.Dataprocess.Scoring.LcmsScoring.GetGaussianSimilarity(accurateMass, msp.PrecursorMz, param.Ms1LibrarySearchTolerance);
            if (msp.RetentionTime >= 0) rtSimilarity = Msdial.Lcms.Dataprocess.Scoring.LcmsScoring.GetGaussianSimilarity(retentionTime, msp.RetentionTime, param.RetentionTimeLibrarySearchTolerance);

            var spectrumPenalty = false;
            if (msp.MzIntensityCommentBeanList != null && msp.MzIntensityCommentBeanList.Count <= 1) spectrumPenalty = true;

            if (ms2Spectra == null || ms2Spectra.Count == 0) {
                totalSimilarity = Msdial.Lcms.Dataprocess.Scoring.LcmsScoring.GetTotalSimilarity(accurateMassSimilarity, rtSimilarity, isotopeSimilarity, param.IsUseRetentionInfoForIdentificationScoring);
            }
            else {
                var maxInt = ms2Spectra.Max(x => x[1]);
                foreach (var peak in ms2Spectra) { masslist.Add(new double[] { peak[0], (peak[1] / maxInt * 100) }); }
                spectraSimilarity = Msdial.Lcms.Dataprocess.Scoring.LcmsScoring.GetMassSpectraSimilarity(masslist,
					msp.MzIntensityCommentBeanList, param.Ms2LibrarySearchTolerance, massBegin, massEnd);
                simpleSim = Msdial.Lcms.Dataprocess.Scoring.LcmsScoring.GetSimpleDotProductSimilarity(masslist, msp.MzIntensityCommentBeanList, param.Ms2LibrarySearchTolerance, param.Ms2MassRangeBegin, param.Ms2MassRangeEnd);
                var simpleSim2 = Msdial.Lcms.Dataprocess.Scoring.LcmsScoring.GetSimpleDotProductSimilarity(masslist, msp.MzIntensityCommentBeanList, param.Ms2LibrarySearchTolerance, param.Ms2MassRangeBegin, accurateMass + 0.5f);
                if (simpleSim < simpleSim2) simpleSim = simpleSim2;
                reverseSearchSimilarity = Msdial.Lcms.Dataprocess.Scoring.LcmsScoring.GetReverseSearchSimilarity(masslist, msp.MzIntensityCommentBeanList,
					param.Ms2LibrarySearchTolerance, massBegin, massEnd);
                presenseSimilarity = Msdial.Lcms.Dataprocess.Scoring.LcmsScoring.GetPresenceSimilarity(masslist, msp.MzIntensityCommentBeanList,
				param.Ms2LibrarySearchTolerance, massBegin, massEnd);
                totalSimilarity = Msdial.Lcms.Dataprocess.Scoring.LcmsScoring.GetTotalSimilarity(accurateMassSimilarity, rtSimilarity, isotopeSimilarity, spectraSimilarity, reverseSearchSimilarity, presenseSimilarity, spectrumPenalty, targetOmics, param.IsUseRetentionInfoForIdentificationScoring);

            }


            res.Add((float)totalSimilarity * 1000);
            res.Add((float)accurateMassSimilarity * 1000);
            res.Add((float)rtSimilarity * 1000);
            res.Add((float)(1000 * (dotProductFactor * spectraSimilarity + reverseDotProdFactor * reverseSearchSimilarity + presensePercentageFactor * presenseSimilarity) / (dotProductFactor + reverseDotProdFactor + presensePercentageFactor)));
            res.Add((float)spectraSimilarity * 1000);
            res.Add((float)reverseSearchSimilarity * 1000);
            res.Add((float)presenseSimilarity * 1000);
            res.Add((float)simpleSim * 1000);
            return res;
        }

        public static MassSpectrogramBean GetReferenceSpectra(MspFormatCompoundInformationBean msp, SolidColorBrush spectrumBrush) {
            ObservableCollection<double[]> masslist = new ObservableCollection<double[]>();
            ObservableCollection<MassSpectrogramDisplayLabel> massSpectrogramDisplayLabelCollection = new ObservableCollection<MassSpectrogramDisplayLabel>();

            for (int i = 0; i < msp.MzIntensityCommentBeanList.Count; i++) {
                masslist.Add(new double[] { (double)msp.MzIntensityCommentBeanList[i].Mz, (double)msp.MzIntensityCommentBeanList[i].Intensity });
                massSpectrogramDisplayLabelCollection.Add(new MassSpectrogramDisplayLabel() { Mass = (double)msp.MzIntensityCommentBeanList[i].Mz, Intensity = (double)msp.MzIntensityCommentBeanList[i].Intensity, Label = msp.MzIntensityCommentBeanList[i].Comment });
            }
            return new MassSpectrogramBean(spectrumBrush, 1.0, masslist, massSpectrogramDisplayLabelCollection);
        }


        #endregion

        #region Chromatogram viewer
        //PeakSpot
        public static ChromatogramMrmViewModel GetMs2ChromatogramViewModelForMsViewer(ObservableCollection<RawSpectrum> spectrumCollection, ProjectPropertyBean projectPropertyBean,
            PeakAreaBean peakAreaBean, AnalysisParametersBean analysisParametersBean, Dictionary<int, AnalystExperimentInformationBean> analystExperimentInformationBean,
            MS2DecResult deconvolutionResultBean, MrmChromatogramView mrmChromatogramView, List<SolidColorBrush> solidColorBrushList, int id) {

            int ms2LevelId = projectPropertyBean.Ms2LevelIdList[id];
            var datapoint = peakAreaBean.Ms2LevelDatapointNumberList[id];
            if (datapoint == -1) return null;
            if (analystExperimentInformationBean == null || analystExperimentInformationBean.Count == 0) return null;

            var graphTitle = "MS2 chromatogram, ";
            ObservableCollection<ChromatogramBean> chromatogramBeanCollection = new ObservableCollection<ChromatogramBean>();

            int experimentCycleNumber = analystExperimentInformationBean.Count;

            float startRt = peakAreaBean.RtAtPeakTop - (peakAreaBean.RtAtRightPeakEdge - peakAreaBean.RtAtLeftPeakEdge) * 1.5F;
            float endRt = peakAreaBean.RtAtPeakTop + (peakAreaBean.RtAtRightPeakEdge - peakAreaBean.RtAtLeftPeakEdge) * 1.5F;

            double focusedMass1 = peakAreaBean.AccurateMass;
            double focusedMass2;
            List<double[]> ms2Peaklist = new List<double[]>();

            ObservableCollection<double[]> centroidedSpectraCollection;
            List<double[]> centroidedSpectraList;

            if (mrmChromatogramView == MrmChromatogramView.raw) {
                graphTitle = "Raw " + graphTitle;
                centroidedSpectraCollection = DataAccessLcUtility.GetCentroidMassSpectra(spectrumCollection, projectPropertyBean.DataTypeMS2, datapoint, analysisParametersBean.CentroidMs2Tolerance, analysisParametersBean.PeakDetectionBasedCentroid);
                if (centroidedSpectraCollection == null || centroidedSpectraCollection.Count == 0) return null;
                centroidedSpectraList = new List<double[]>(centroidedSpectraCollection);
                centroidedSpectraList = centroidedSpectraList.OrderByDescending(n => n[1]).ToList();

                for (int i = 0; i < centroidedSpectraCollection.Count; i++) {
                    focusedMass2 = centroidedSpectraList[i][0];
                    ms2Peaklist = DataAccessLcUtility.GetMs2Peaklist(spectrumCollection, datapoint, ms2LevelId, experimentCycleNumber, startRt, endRt, focusedMass2, analysisParametersBean);
                    ms2Peaklist = DataAccessLcUtility.GetSmoothedPeaklist(ms2Peaklist, analysisParametersBean.SmoothingMethod, analysisParametersBean.SmoothingLevel);

                    if (i < 10)
                        chromatogramBeanCollection.Add(new ChromatogramBean(true, solidColorBrushList[i], 1.0, peakAreaBean.AccurateMass, (float)focusedMass2, new ObservableCollection<double[]>(ms2Peaklist), null));
                    else if (10 <= i && i < 30)
                        chromatogramBeanCollection.Add(new ChromatogramBean(false, solidColorBrushList[i], 1.0, peakAreaBean.AccurateMass, (float)focusedMass2, new ObservableCollection<double[]>(ms2Peaklist), null));
                    else
                        chromatogramBeanCollection.Add(new ChromatogramBean(false, solidColorBrushList[0], 1.0, peakAreaBean.AccurateMass, (float)focusedMass2, new ObservableCollection<double[]>(ms2Peaklist), null));
                }
            }
            else if (mrmChromatogramView == MrmChromatogramView.component) {
                graphTitle = "Dec " + graphTitle;
                if (deconvolutionResultBean.MassSpectra.Count == 0) return null;
                centroidedSpectraList = deconvolutionResultBean.MassSpectra;
                centroidedSpectraList = centroidedSpectraList.OrderByDescending(n => n[1]).ToList();

                for (int i = 0; i < centroidedSpectraList.Count; i++) {
                    focusedMass2 = centroidedSpectraList[i][0];
                    ms2Peaklist = DataAccessLcUtility.GetMatchedMs2Peaklist(deconvolutionResultBean.PeaklistList, focusedMass2);
                    ms2Peaklist.Insert(0, new double[] { 0, startRt, 0, 0 });
                    ms2Peaklist.Add(new double[] { 0, endRt, 0, 0 });

                    if (i < 10)
                        chromatogramBeanCollection.Add(new ChromatogramBean(true, solidColorBrushList[i], 1.5, peakAreaBean.AccurateMass, (float)focusedMass2, new ObservableCollection<double[]>(ms2Peaklist), null));
                    else if (10 <= i && i < 30)
                        chromatogramBeanCollection.Add(new ChromatogramBean(false, solidColorBrushList[i], 1.5, peakAreaBean.AccurateMass, (float)focusedMass2, new ObservableCollection<double[]>(ms2Peaklist), null));
                    else
                        chromatogramBeanCollection.Add(new ChromatogramBean(false, solidColorBrushList[0], 1.5, peakAreaBean.AccurateMass, (float)focusedMass2, new ObservableCollection<double[]>(ms2Peaklist), null));
                }
            }
            return new ChromatogramMrmViewModel(chromatogramBeanCollection, ChromatogramEditMode.Display, ChromatogramDisplayLabel.None, ChromatogramQuantitativeMode.Height, ChromatogramIntensityMode.Absolute, -1, -1, graphTitle, -1, "", "", "", "", -1, -1, peakAreaBean.RtAtPeakTop, null, -1, -1);
        }

        // aligned EIC chromatogram viewer
        public static List<ChromatogramXicViewModel> GetAlignedEicChromatogramList(AlignedData alignedData,
            AlignmentPropertyBean alignmentProp, ObservableCollection<AnalysisFileBean> files,
            ProjectPropertyBean projectPropety, AnalysisParametersBean param) {
            if (alignedData == null) return null;

            var chromatogramBeanCollection = new ObservableCollection<ChromatogramBean>();
            var targetMz = alignedData.Mz;
            var numAnalysisfiles = alignedData.NumAnalysisFiles;
            var vms = new ChromatogramXicViewModel[numAnalysisfiles];
            var classnameToBytes = projectPropety.ClassnameToColorBytes;
            var classnameToBrushes = MsDialStatistics.ConvertToSolidBrushDictionary(classnameToBytes);
            //var classIdColorDictionary = MsDialStatistics.GetClassIdColorDictionary(files, solidColorBrushList);

            System.Threading.Tasks.Parallel.For(0, numAnalysisfiles, (i) =>
            {
                //for (int i = 0; i < numAnalysisfiles; i++) { // draw the included samples
                var peaks = alignedData.PeakLists[i].PeakList;
                var peaklist = new List<double[]>();
                for (int j = 0; j < peaks.Count; j++)
                {
                    peaklist.Add(new double[] { j, (double)peaks[j][0], targetMz, (double)peaks[j][1] });
                }
                peaklist = DataAccessLcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);
                var chromatogramBean = new ChromatogramBean(true, classnameToBrushes[files[i].AnalysisFilePropertyBean.AnalysisFileClass], 1.0, files[i].AnalysisFilePropertyBean.AnalysisFileName,
                    targetMz, param.CentroidMs1Tolerance, new ObservableCollection<double[]>(peaklist));
                var vm = new ChromatogramXicViewModel(chromatogramBean, ChromatogramEditMode.Display,
                    ChromatogramDisplayLabel.None, ChromatogramQuantitativeMode.Height, ChromatogramIntensityMode.Absolute,
                    0, "", targetMz, param.CentroidMs1Tolerance,
                    alignmentProp.AlignedPeakPropertyBeanCollection[i].RetentionTime,
                    alignmentProp.AlignedPeakPropertyBeanCollection[i].RetentionTimeLeft,
                    alignmentProp.AlignedPeakPropertyBeanCollection[i].RetentionTimeRight);
                vms[i] = vm;
            });
            return vms.ToList();
        }

        // aligned EIC chromatogram viewer on ion mobility
        public static List<ChromatogramXicViewModel> GetAlignedEicChromatogramList(AlignedData alignedData,
            AlignedDriftSpotPropertyBean alignmentProp, ObservableCollection<AnalysisFileBean> files,
            ProjectPropertyBean projectPropety, AnalysisParametersBean param) {
            if (alignedData == null) return null;

            var chromatogramBeanCollection = new ObservableCollection<ChromatogramBean>();
            var targetMz = alignedData.Mz;
            var numAnalysisfiles = alignedData.NumAnalysisFiles;
            var vms = new List<ChromatogramXicViewModel>();
            //var classIdColorDictionary = MsDialStatistics.GetClassIdColorDictionary(files, solidColorBrushList);
            var classnameToBytes = projectPropety.ClassnameToColorBytes;
            var classnameToBrushes = MsDialStatistics.ConvertToSolidBrushDictionary(classnameToBytes);
            for (int i = 0; i < numAnalysisfiles; i++) { // draw the included samples
                var peaks = alignedData.PeakLists[i].PeakList;
                var peaklist = new List<double[]>();
                for (int j = 0; j < peaks.Count; j++) {
                    peaklist.Add(new double[] { j, (double)peaks[j][0], targetMz, (double)peaks[j][1] });
                }
                peaklist = DataAccessLcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);
                var chromatogramBean = new ChromatogramBean(true, classnameToBrushes[files[i].AnalysisFilePropertyBean.AnalysisFileClass], 1.0, files[i].AnalysisFilePropertyBean.AnalysisFileName,
                    targetMz, param.CentroidMs1Tolerance, new ObservableCollection<double[]>(peaklist));
                var vm = new ChromatogramXicViewModel(chromatogramBean, ChromatogramEditMode.Display,
                    ChromatogramDisplayLabel.None, ChromatogramQuantitativeMode.Height, ChromatogramIntensityMode.Absolute,
                    0, "", targetMz, param.CentroidMs1Tolerance,
                    alignmentProp.AlignedPeakPropertyBeanCollection[i].DriftTime,
                    alignmentProp.AlignedPeakPropertyBeanCollection[i].DriftTimeLeft,
                    alignmentProp.AlignedPeakPropertyBeanCollection[i].DriftTimeRight);
                vms.Add(vm);
            }
            return vms;
        }

        // aligned EIC chromatogram viewer
        public static List<ChromatogramXicViewModel> GetAlignedEicChromatogramList(AlignedData alignedData,
            AlignmentPropertyBean alignmentProp, ObservableCollection<AnalysisFileBean> files,
            ProjectPropertyBean projectPropety, AnalysisParamOfMsdialGcms param) {
            if (alignedData == null) return null;

            var chromatogramBeanCollection = new ObservableCollection<ChromatogramBean>();
            var targetMz = alignedData.Mz;
            var numAnalysisfiles = alignedData.NumAnalysisFiles;
            var vms = new List<ChromatogramXicViewModel>();
            //var classIdColorDictionary = MsDialStatistics.GetClassIdColorDictionary(files, solidColorBrushList);
            var classnameToBytes = projectPropety.ClassnameToColorBytes;
            var classnameToBrushes = MsDialStatistics.ConvertToSolidBrushDictionary(classnameToBytes);
            for (int i = 0; i < numAnalysisfiles; i++) { // draw the included samples
                var peaks = alignedData.PeakLists[i].PeakList;
                var peaklist = new List<double[]>();
                for (int j = 0; j < peaks.Count; j++) {
                    peaklist.Add(new double[] { j, (double)peaks[j][0], targetMz, (double)peaks[j][1] });
                    //Debug.WriteLine("X {0}, Y {1}", (float)peaks[j][0], (float)peaks[j][1]);
                }
                peaklist = DataAccessLcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);
                var chromatogramBean = new ChromatogramBean(true, classnameToBrushes[files[i].AnalysisFilePropertyBean.AnalysisFileClass], 1.0,
                    files[i].AnalysisFilePropertyBean.AnalysisFileName, targetMz, param.MassAccuracy, new ObservableCollection<double[]>(peaklist));

                var rttop = alignmentProp.AlignedPeakPropertyBeanCollection[i].RetentionTime;
                var rtleft = alignmentProp.AlignedPeakPropertyBeanCollection[i].RetentionTimeLeft;
                var rtright = alignmentProp.AlignedPeakPropertyBeanCollection[i].RetentionTimeRight;

                if (param.AlignmentIndexType == AlignmentIndexType.RI) {
                    rttop = alignmentProp.AlignedPeakPropertyBeanCollection[i].RetentionIndex;
                    rtleft = alignmentProp.AlignedPeakPropertyBeanCollection[i].RetentionIndexLeft;
                    rtright = alignmentProp.AlignedPeakPropertyBeanCollection[i].RetentionIndexRight;
                }

                var vm = new ChromatogramXicViewModel(chromatogramBean, ChromatogramEditMode.Display,
                    ChromatogramDisplayLabel.None, ChromatogramQuantitativeMode.Height, ChromatogramIntensityMode.Absolute,
                    0, "", targetMz, param.MassAccuracy,
                    rttop,
                    rtleft,
                    rtright);
                vms.Add(vm);
            }
            return vms;
        }

        public static ObservableCollection<SampleTableRow> GetSourceOfAlignedSampleTableViewer(AlignmentPropertyBean alignmentProp,
            AlignedData alignedData, ObservableCollection<AnalysisFileBean> files, ProjectPropertyBean projectPropety,
            AnalysisParametersBean param, List<SolidColorBrush> solidColorBrushList) {
            var source = new ObservableCollection<SampleTableRow>();
            var vms = GetAlignedEicChromatogramList(alignedData, alignmentProp, files, projectPropety, param);
            for (var i = 0; i < files.Count; i++) {
                var check = alignmentProp.RepresentativeFileID == i ? i : 0;
                source.Add(new SampleTableRow(alignmentProp, alignmentProp.AlignedPeakPropertyBeanCollection[i],
                    vms[i], files[i].AnalysisFilePropertyBean.AnalysisFileClass, check));
            }
            return source;
        }

        public static ObservableCollection<SampleTableRow> GetSourceOfAlignedSampleTableViewer(AlignedDriftSpotPropertyBean alignmentProp,
           AlignedData alignedData, ObservableCollection<AnalysisFileBean> files, ProjectPropertyBean projectPropety,
           AnalysisParametersBean param, List<SolidColorBrush> solidColorBrushList) {
            var source = new ObservableCollection<SampleTableRow>();
            var vms = GetAlignedEicChromatogramList(alignedData, alignmentProp, files, projectPropety, param);
            for (var i = 0; i < files.Count; i++) {
                var check = alignmentProp.RepresentativeFileID == i ? i : 0;
                source.Add(new SampleTableRow(alignmentProp, alignmentProp.AlignedPeakPropertyBeanCollection[i],
                    vms[i], files[i].AnalysisFilePropertyBean.AnalysisFileClass, check));
            }
            return source;
        }

        public static ObservableCollection<SampleTableRow> GetSourceOfAlignedSampleTableViewer(AlignmentPropertyBean alignmentProp,
            AlignedData alignedData, ObservableCollection<AnalysisFileBean> files, ProjectPropertyBean projectPropety,
            AnalysisParamOfMsdialGcms param, List<SolidColorBrush> solidColorBrushList) {
            var source = new ObservableCollection<SampleTableRow>();
            var vms = GetAlignedEicChromatogramList(alignedData, alignmentProp, files, projectPropety, param);
            for (var i = 0; i < files.Count; i++) {
                var check = alignmentProp.RepresentativeFileID == i ? i : 0;
                source.Add(new SampleTableRow(alignmentProp, alignmentProp.AlignedPeakPropertyBeanCollection[i],
                    vms[i], files[i].AnalysisFilePropertyBean.AnalysisFileClass, check));
            }
            return source;
        }

        #endregion

        #region CorrDec identification
        public List<PeakAreaBean> ConvertAlignmentSpot2PeakAreaBean()
        {
            var peakAreaBeanList = new List<PeakAreaBean>();


            return peakAreaBeanList;
        }


        #endregion

        #region various methods
        public static void ChangeComments(MainWindow m) {
            var rtDiff = 2;

            var alignedRes = m.FocusedAlignmentResult;
            foreach (var spot in alignedRes.AlignmentPropertyBeanCollection) {
                if (spot.MetaboliteName == string.Empty) {
                    spot.Comment = "Unknown";
                }
                if (spot.LibraryID > -1 && m.MspDB.Count > 0){
                    if (m.MspDB[spot.LibraryID].RetentionTime > 0) {
                        if (Math.Abs(spot.CentralRetentionTime - m.MspDB[spot.LibraryID].RetentionTime) > rtDiff) {
                            spot.Comment = "B2, miss annoatation, RT diff more than 5min";
                        }
                    }
                    else { spot.Comment = "C, no RT"; }
                }
            }
        }

        #endregion
    }

}
