using Msdial.Lcms.Dataprocess.Algorithm;
using Msdial.Lcms.Dataprocess.Utility;
using Rfx.Riken.OsakaUniv;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace Msdial.Lcms.Dataprocess.Test
{
    public class MatMetaData
    {
        private string abfFileName;
        private string matFileName;
        private string commonName;
        private string smiles;
        private string inchikey;
        private string formula;
        private string exactmass;
        private double retentionTime;
        private double precursorMz;
        private string adductString;
        private AdductIon adduct;

        public string AbfFileName
        {
            get { return abfFileName; }
            set { abfFileName = value; }
        }

        public string MatFileName
        {
            get { return matFileName; }
            set { matFileName = value; }
        }

        public string CommonName
        {
            get { return commonName; }
            set { commonName = value; }
        }

        public string Smiles
        {
            get { return smiles; }
            set { smiles = value; }
        }

        public string Inchikey
        {
            get { return inchikey; }
            set { inchikey = value; }
        }

        public string Formula
        {
            get { return formula; }
            set { formula = value; }
        }

        public string Exactmass
        {
            get { return exactmass; }
            set { exactmass = value; }
        }

        public double RetentionTime
        {
            get { return retentionTime; }
            set { retentionTime = value; }
        }
        

        public double PrecursorMz
        {
            get { return precursorMz; }
            set { precursorMz = value; }
        }

        public string AdductString
        {
            get { return adductString; }
            set { adductString = value; }
        }

        public AdductIon Adduct
        {
            get { return adduct; }
            set { adduct = value; }
        }
    }


    public sealed class ExtractMatFiles
    {
        private ExtractMatFiles() { }

        public static List<MatMetaData> GetMatMetaDataList(string input)
        {
            var metadataList = new List<MatMetaData>();
            using (var sr = new StreamReader(input, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) continue;
                    var lineArray = line.Split('\t');
                    var metadata = new MatMetaData() {
                         AbfFileName = lineArray[0], MatFileName = lineArray[1], CommonName = lineArray[2],
                         Smiles = lineArray[3], Inchikey = lineArray[4], Formula = lineArray[5], Exactmass = lineArray[6],
                         RetentionTime = double.Parse(lineArray[7]), PrecursorMz = double.Parse(lineArray[8]),
                         AdductString = lineArray[9], Adduct = AdductIonParcer.GetAdductIonBean(lineArray[9])
                    };

                    metadataList.Add(metadata);
                }
            }
            return metadataList;
        }

        public static void ExportMatFiles(string outputFolder, string libraryname, List<MatMetaData> metadataList, 
            ObservableCollection<AnalysisFileBean> analysisFileBeanCollection, 
            RdamPropertyBean rdamProperty, ProjectPropertyBean projectProperty, AnalysisParametersBean param)
        {
            var checkFilePath = Path.Combine(outputFolder, "Checklist-" + libraryname + ".txt");
            using (StreamWriter swC = new StreamWriter(checkFilePath, false, Encoding.ASCII)) {
                var count = 1;
                foreach (var metadata in metadataList) {

                    var abfFileName = metadata.AbfFileName;
                    var flg = false;
                    var flgProton = false;
                    var flgFormate = false;
                    foreach (var file in analysisFileBeanCollection) {

                        if (abfFileName == file.AnalysisFilePropertyBean.AnalysisFileName) {
                            var fileID = rdamProperty.RdamFilePath_RdamFileID[file.AnalysisFilePropertyBean.AnalysisFilePath];
                            var measurementID = rdamProperty.RdamFileContentBeanCollection[fileID].FileID_MeasurementID[file.AnalysisFilePropertyBean.AnalysisFileId];

                            using (var fs = File.Open(file.AnalysisFilePropertyBean.DeconvolutionFilePath, FileMode.Open, FileAccess.ReadWrite)) {
                                var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);
                                using (var rawDataAccess = new RawDataAccess(file.AnalysisFilePropertyBean.AnalysisFilePath, measurementID, false, false, true, file.RetentionTimeCorrectionBean.PredictedRt)) {
                                    var spectrumCollection = DataAccessLcUtility.GetRdamSpectrumCollection(rawDataAccess);

                                    DataStorageLcUtility.SetPeakAreaBeanCollection(file, file.AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);

                                    var mz = metadata.PrecursorMz;
                                    var rt = metadata.RetentionTime;
                                    var mz_formate = mz + 46.005479304;

                                    var protonIntensity = 0.0;
                                    var formateIntensity = 0.0;

                                    foreach (var peak in file.PeakAreaBeanCollection) {

                                        if (Math.Abs(peak.AccurateMass - mz) < 0.01 && Math.Abs(peak.RtAtPeakTop - rt) < 0.03) {
                                            protonIntensity = peak.IntensityAtPeakTop;
                                           
                                            #region
                                            var ms1Spectrum = DataAccessLcUtility.GetCentroidMassSpectra(spectrumCollection, projectProperty.DataType,
                                                peak.Ms1LevelDatapointNumber, param.CentroidMs1Tolerance, param.PeakDetectionBasedCentroid);

                                            var ms2Spectrum = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, peak.PeakID).MassSpectra;
                                            var outputFilePath = Path.Combine(outputFolder, metadata.AbfFileName + "_" + Math.Round(rt, 2).ToString() + "_"
                                                + Math.Round(mz, 5).ToString() + "_" + metadata.MatFileName + ".mat");
                                           
                                            using (StreamWriter sw = new StreamWriter(outputFilePath, false, Encoding.ASCII)) {
                                                sw.WriteLine("NAME: " + metadata.CommonName);
                                                sw.WriteLine("RETENTIONTIME: " + rt);
                                                sw.WriteLine("PRECURSORMZ: " + mz);
                                                sw.WriteLine("PRECURSORTYPE: " + metadata.Adduct.AdductIonName);
                                                sw.WriteLine("IONMODE: " + projectProperty.IonMode);
                                                sw.WriteLine("SPECTRUMTYPE: Centroid");
                                                sw.WriteLine("INTENSITY: " + peak.IntensityAtPeakTop);
                                                sw.WriteLine("INCHIKEY: " + metadata.Inchikey);
                                                sw.WriteLine("SMILES: " + metadata.Smiles);
                                                sw.WriteLine("FORMULA: " + metadata.Formula);

                                                sw.WriteLine("MSTYPE: MS1");
                                                sw.WriteLine("Num Peaks: " + ms1Spectrum.Count);

                                                for (int i = 0; i < ms1Spectrum.Count; i++)
                                                    sw.WriteLine(Math.Round(ms1Spectrum[i][0], 5) + "\t" + Math.Round(ms1Spectrum[i][1], 0));

                                                sw.WriteLine("MSTYPE: MS2");
                                                sw.WriteLine("Num Peaks: " + ms2Spectrum.Count);

                                                for (int i = 0; i < ms2Spectrum.Count; i++)
                                                    sw.WriteLine(Math.Round(ms2Spectrum[i][0], 5) + "\t" + Math.Round(ms2Spectrum[i][1], 0));
                                            }

                                            if (projectProperty.IonMode == IonMode.Positive) {
                                                flg = true;
                                                break;
                                            }
                                            #endregion
                                            if (projectProperty.IonMode == IonMode.Negative) flgProton = true;
                                        }

                                        if (projectProperty.IonMode == IonMode.Negative && metadata.AdductString == "[M-H]-" && 
                                            Math.Abs(peak.AccurateMass - mz_formate) < 0.01 && Math.Abs(peak.RtAtPeakTop - rt) < 0.015) {
                                            formateIntensity = peak.IntensityAtPeakTop;
                                           
                                            #region

                                            var ms1Spectrum = DataAccessLcUtility.GetCentroidMassSpectra(spectrumCollection, projectProperty.DataType,
                                                peak.Ms1LevelDatapointNumber, param.CentroidMs1Tolerance, param.PeakDetectionBasedCentroid);

                                            var ms2Spectrum = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, peak.PeakID).MassSpectra;
                                            var outputFilePath = Path.Combine(outputFolder, metadata.AbfFileName + "_" + Math.Round(rt, 2).ToString() + "_"
                                                + Math.Round(mz_formate, 5).ToString() + "_" + metadata.MatFileName + ".mat");

                                            using (StreamWriter sw = new StreamWriter(outputFilePath, false, Encoding.ASCII)) {
                                                
                                                sw.WriteLine("NAME: " + metadata.CommonName);
                                                sw.WriteLine("RETENTIONTIME: " + rt);
                                                sw.WriteLine("PRECURSORMZ: " + mz_formate);
                                                sw.WriteLine("PRECURSORTYPE: [M+FA-H]-");
                                                sw.WriteLine("IONMODE: " + projectProperty.IonMode);
                                                sw.WriteLine("SPECTRUMTYPE: Centroid");
                                                sw.WriteLine("INTENSITY: " + peak.IntensityAtPeakTop);
                                                sw.WriteLine("INCHIKEY: " + metadata.Inchikey);
                                                sw.WriteLine("SMILES: " + metadata.Smiles);
                                                sw.WriteLine("FORMULA: " + metadata.Formula);

                                                sw.WriteLine("MSTYPE: MS1");
                                                sw.WriteLine("Num Peaks: " + ms1Spectrum.Count);

                                                for (int i = 0; i < ms1Spectrum.Count; i++)
                                                    sw.WriteLine(Math.Round(ms1Spectrum[i][0], 5) + "\t" + Math.Round(ms1Spectrum[i][1], 0));

                                                sw.WriteLine("MSTYPE: MS2");
                                                sw.WriteLine("Num Peaks: " + ms2Spectrum.Count);

                                                for (int i = 0; i < ms2Spectrum.Count; i++)
                                                    sw.WriteLine(Math.Round(ms2Spectrum[i][0], 5) + "\t" + Math.Round(ms2Spectrum[i][1], 0));
                                            }
                                            #endregion
                                            flgFormate = true;
                                        }
                                        if (flgProton && flgFormate) flg = true;

                                        if (flg) break;
                                    }
                                    DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(file);
                                }
                            }
                        }
                    }

                    if (projectProperty.IonMode == IonMode.Positive) {
                        if (flg) {
                            swC.WriteLine(metadata.MatFileName + "\tTRUE");
                        }
                        else {
                            swC.WriteLine(metadata.MatFileName + "\tFALSE");
                        }
                    }
                    else {
                        if (flg) {
                            swC.WriteLine(metadata.MatFileName + "\tProton loss\tTRUE\tFormate adduct\tTRUE");
                        }
                        else if (flgProton && !flgFormate) {
                            swC.WriteLine(metadata.MatFileName + "\tProton loss\tTRUE\tFormate adduct\tFalse");
                        }
                        else if (!flgProton && flgFormate) {
                            swC.WriteLine(metadata.MatFileName + "\tProton loss\tFALSE\tFormate adduct\tTRUE");
                        }
                        else {
                            swC.WriteLine(metadata.MatFileName + "\tProton loss\tFALSE\tFormate adduct\tFALSE");
                        }
                    }
                    
                    Console.WriteLine(count + "/" + metadataList.Count);
                    count++;
                }
            }
        }
    }
}
