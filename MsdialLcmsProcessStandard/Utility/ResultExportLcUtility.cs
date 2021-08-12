using CompMs.Common.DataObj;
using Msdial.Lcms.Dataprocess.Algorithm;
using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.Lipidomics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Msdial.Lcms.Dataprocess.Utility {
    public sealed class ResultExportLcUtility {
        private ResultExportLcUtility() { }

        #region //utility for peak list export
        public static void WritePeaklistTextHeader(StreamWriter sw, bool isIonMobility) {
            if (isIonMobility) {
                var header = new List<string>() { "PeakID", "Title", "Scans", "RT left(min)", "RT (min)", "RT right (min)", "Mobility left", "Mobility", "Mobility right",
                    "CCS", "Precursor m/z", "Height", "Area", "Model masses", "Adduct", "Isotope", "Comment", 
                    "Reference RT", "Reference CCS", "Reference m/z", "Formula", "Ontology", "InChIKey", "SMILES",
                    "Annotation tag (VS1.0)", "RT matched", "CCS matched", "m/z matched", "MS/MS matched",
                    "RT similarity", "CCS similarity", "Dot product", "Reverse dot product", "Fragment presence %", "Total score", 
                    "S/N", "MS1 isotopes", "MSMS spectrum" };
                sw.WriteLine(String.Join("\t", header.ToArray()));
            }
            else {
                var header = new List<string>() { "PeakID", "Title", "Scans", "RT left(min)", "RT (min)", "RT right (min)", 
                    "Precursor m/z", "Height", "Area", "Model masses", "Adduct", "Isotope", "Comment",
                    "Reference RT", "Reference m/z", "Formula", "Ontology", "InChIKey", "SMILES",
                    "Annotation tag (VS1.0)", "RT matched", "m/z matched", "MS/MS matched",
                    "RT similarity", "Dot product", "Reverse dot product", "Fragment presence %", "Total score",
                    "S/N", "MS1 isotopes", "MSMS spectrum" };
                sw.WriteLine(String.Join("\t", header.ToArray()));
            }
        }


        public static void WriteProfileMsp(StreamWriter sw, ObservableCollection<RawSpectrum> spectrumCollection, PeakAreaBean peakAreaBean) {
            var massSpectra = DataAccessLcUtility.GetMsMsSpectra(spectrumCollection, peakAreaBean.Ms2LevelDatapointNumber);

            var metabolitename = "Unknown";
            if (peakAreaBean.MetaboliteName != string.Empty) metabolitename = peakAreaBean.MetaboliteName;
            sw.WriteLine("NAME: " + metabolitename);

            sw.WriteLine("PRECURSORMZ: " + peakAreaBean.AccurateMass);
            sw.WriteLine("PRECURSORTYPE: " + peakAreaBean.AdductIonName);
            sw.WriteLine("SCANNUMBER: " + peakAreaBean.PeakID);
            sw.WriteLine("RETENTIONTIME: " + peakAreaBean.RtAtPeakTop);
            sw.WriteLine("INTENSITY: " + peakAreaBean.IntensityAtPeakTop);

            //sw.Write("METABOLITENAME: ");
            //sw.WriteLine(peakAreaBean.MetaboliteName);

            sw.WriteLine("ISOTOPE: " + "M + " + peakAreaBean.IsotopeWeightNumber.ToString());
            sw.WriteLine("Num Peaks: " + massSpectra.Count);

            for (int i = 0; i < massSpectra.Count; i++)
                sw.WriteLine(Math.Round(massSpectra[i][0], 5) + "\t" + Math.Round(massSpectra[i][1], 0));

            sw.WriteLine();
        }

        public static void WriteProfileMsp(StreamWriter sw, ObservableCollection<RawSpectrum> spectrumCollection, PeakAreaBean peakSpot, DriftSpotBean driftSpot) {
            var massSpectra = DataAccessLcUtility.GetMsMsSpectra(spectrumCollection, driftSpot.Ms2LevelDatapointNumber);

            var metabolitename = "Unknown";
            if (driftSpot.MetaboliteName != string.Empty) metabolitename = driftSpot.MetaboliteName;
            sw.WriteLine("NAME: " + metabolitename);

            sw.WriteLine("PRECURSORMZ: " + driftSpot.AccurateMass);
            sw.WriteLine("PRECURSORTYPE: " + driftSpot.AdductIonName);
            sw.WriteLine("SCANNUMBER: " + driftSpot.MasterPeakID);
            sw.WriteLine("RETENTIONTIME: " + peakSpot.RtAtPeakTop);
            sw.WriteLine("MOBILITY: " + driftSpot.DriftTimeAtPeakTop);
            sw.WriteLine("CCS: " + driftSpot.Ccs);
            sw.WriteLine("INTENSITY: " + driftSpot.IntensityAtPeakTop);
            sw.WriteLine("ISOTOPE: " + "M + " + driftSpot.IsotopeWeightNumber.ToString());
            sw.WriteLine("Num Peaks: " + massSpectra.Count);

            for (int i = 0; i < massSpectra.Count; i++)
                sw.WriteLine(Math.Round(massSpectra[i][0], 5) + "\t" + Math.Round(massSpectra[i][1], 0));

            sw.WriteLine();
        }

        public static void WriteCentroidMsp(StreamWriter sw, ObservableCollection<RawSpectrum> spectrumCollection, PeakAreaBean peakAreaBean, DataType datatype,
            AnalysisParametersBean param) {
            ObservableCollection<double[]> massSpectra = DataAccessLcUtility.GetCentroidMassSpectra(spectrumCollection, datatype,
                peakAreaBean.Ms2LevelDatapointNumber, param.CentroidMs2Tolerance, param.PeakDetectionBasedCentroid);

            var metabolitename = "Unknown";
            if (peakAreaBean.MetaboliteName != string.Empty) metabolitename = peakAreaBean.MetaboliteName;
            sw.WriteLine("NAME: " + metabolitename);

            sw.WriteLine("PRECURSORMZ: " + peakAreaBean.AccurateMass);
            sw.WriteLine("PRECURSORTYPE: " + peakAreaBean.AdductIonName);
            sw.WriteLine("SCANNUMBER: " + peakAreaBean.PeakID);
            sw.WriteLine("RETENTIONTIME: " + peakAreaBean.RtAtPeakTop);
            sw.WriteLine("INTENSITY: " + peakAreaBean.IntensityAtPeakTop);
            sw.WriteLine("ISOTOPE: " + "M + " + peakAreaBean.IsotopeWeightNumber.ToString());
            sw.WriteLine("Num Peaks: " + massSpectra.Count);

            for (int i = 0; i < massSpectra.Count; i++)
                sw.WriteLine(Math.Round(massSpectra[i][0], 5) + "\t" + Math.Round(massSpectra[i][1], 0));

            sw.WriteLine();
        }

        public static void WriteCentroidMsp(StreamWriter sw, ObservableCollection<RawSpectrum> spectrumCollection, PeakAreaBean peakSpot,
            DriftSpotBean driftSpot, DataType datatype,
           AnalysisParametersBean param) {
            ObservableCollection<double[]> massSpectra = DataAccessLcUtility.GetCentroidMassSpectra(spectrumCollection, datatype,
                driftSpot.Ms2LevelDatapointNumber, param.CentroidMs2Tolerance, param.PeakDetectionBasedCentroid);

            var metabolitename = "Unknown";
            if (driftSpot.MetaboliteName != string.Empty) metabolitename = driftSpot.MetaboliteName;
            sw.WriteLine("NAME: " + metabolitename);

            sw.WriteLine("PRECURSORMZ: " + driftSpot.AccurateMass);
            sw.WriteLine("PRECURSORTYPE: " + driftSpot.AdductIonName);
            sw.WriteLine("SCANNUMBER: " + driftSpot.MasterPeakID);
            sw.WriteLine("RETENTIONTIME: " + peakSpot.RtAtPeakTop);
            sw.WriteLine("MOBILITY: " + driftSpot.DriftTimeAtPeakTop);
            sw.WriteLine("CCS: " + driftSpot.Ccs);
            sw.WriteLine("INTENSITY: " + driftSpot.IntensityAtPeakTop);
            sw.WriteLine("ISOTOPE: " + "M + " + driftSpot.IsotopeWeightNumber.ToString());
            sw.WriteLine("Num Peaks: " + massSpectra.Count);

            for (int i = 0; i < massSpectra.Count; i++)
                sw.WriteLine(Math.Round(massSpectra[i][0], 5) + "\t" + Math.Round(massSpectra[i][1], 0));

            sw.WriteLine();
        }

        public static void WriteCentroidedMsp(StreamWriter sw, FileStream fs, List<long> seekpointList, PeakAreaBean peakAreaBean) {
            MS2DecResult deconvolutionResultBean = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, peakAreaBean.PeakID);
            List<double[]> massSpectra = deconvolutionResultBean.MassSpectra;

            var metabolitename = "Unknown";
            if (peakAreaBean.MetaboliteName != string.Empty) metabolitename = peakAreaBean.MetaboliteName;
            sw.WriteLine("NAME: " + metabolitename);

            sw.WriteLine("PRECURSORMZ: " + peakAreaBean.AccurateMass);
            sw.WriteLine("PRECURSORTYPE: " + peakAreaBean.AdductIonName);
            sw.WriteLine("SCANNUMBER: " + peakAreaBean.PeakID);
            sw.WriteLine("RETENTIONTIME: " + peakAreaBean.RtAtPeakTop);
            sw.WriteLine("INTENSITY: " + peakAreaBean.IntensityAtPeakTop);
            sw.WriteLine("ISOTOPE: " + "M + " + peakAreaBean.IsotopeWeightNumber.ToString());
            sw.WriteLine("Num Peaks: " + massSpectra.Count);

            if (massSpectra.Count > 0) {
                for (int i = 0; i < massSpectra.Count; i++)
                    sw.WriteLine(Math.Round(massSpectra[i][0], 5) + "\t" + Math.Round(massSpectra[i][1], 0));
            }

            sw.WriteLine();
        }

        public static void WriteDeconvolutedMsp(StreamWriter sw, FileStream fs, List<long> seekpointList, PeakAreaBean peakAreaBean) {
            MS2DecResult ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, peakAreaBean.PeakID);
            List<double[]> massSpectra = ms2DecResult.MassSpectra;

            var metabolitename = "Unknown";
            if (peakAreaBean.MetaboliteName != string.Empty) metabolitename = peakAreaBean.MetaboliteName;
            sw.WriteLine("NAME: " + metabolitename);

            sw.WriteLine("PRECURSORMZ: " + peakAreaBean.AccurateMass);
            sw.WriteLine("PRECURSORTYPE: " + peakAreaBean.AdductIonName);
            sw.WriteLine("SCANNUMBER: " + peakAreaBean.PeakID);
            sw.WriteLine("RETENTIONTIME: " + peakAreaBean.RtAtPeakTop);
            sw.WriteLine("INTENSITY: " + peakAreaBean.IntensityAtPeakTop);
            sw.WriteLine("ISOTOPE: " + "M + " + peakAreaBean.IsotopeWeightNumber.ToString());
            sw.WriteLine("DECONVOLUTED_PEAKHEIGHT: " + ms2DecResult.Ms2DecPeakHeight);
            sw.WriteLine("DECONVOLUTED_PEAKAREA: " + ms2DecResult.Ms2DecPeakArea);
            sw.WriteLine("UNIQUEMS: " + ms2DecResult.UniqueMs);
            sw.WriteLine("Num Peaks: " + massSpectra.Count);

            for (int i = 0; i < massSpectra.Count; i++)
                sw.WriteLine(Math.Round(massSpectra[i][0], 5) + "\t" + Math.Round(massSpectra[i][1], 0));

            sw.WriteLine();
        }

        public static void WriteDeconvolutedMsp(StreamWriter sw, FileStream fs, List<long> seekpointList, PeakAreaBean peakSpot, DriftSpotBean driftSpot) {
            MS2DecResult ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, driftSpot.MasterPeakID);
            List<double[]> massSpectra = ms2DecResult.MassSpectra;

            var metabolitename = "Unknown";
            if (driftSpot.MetaboliteName != string.Empty) metabolitename = driftSpot.MetaboliteName;
            sw.WriteLine("NAME: " + metabolitename);

            sw.WriteLine("PRECURSORMZ: " + driftSpot.AccurateMass);
            sw.WriteLine("PRECURSORTYPE: " + driftSpot.AdductIonName);
            sw.WriteLine("SCANNUMBER: " + driftSpot.MasterPeakID);
            sw.WriteLine("RETENTIONTIME: " + peakSpot.RtAtPeakTop);
            sw.WriteLine("MOBILITY: " + driftSpot.DriftTimeAtPeakTop);
            sw.WriteLine("CCS: " + driftSpot.Ccs);
            sw.WriteLine("INTENSITY: " + driftSpot.IntensityAtPeakTop);
            sw.WriteLine("ISOTOPE: " + "M + " + driftSpot.IsotopeWeightNumber.ToString());
            sw.WriteLine("DECONVOLUTED_PEAKHEIGHT: " + ms2DecResult.Ms2DecPeakHeight);
            sw.WriteLine("DECONVOLUTED_PEAKAREA: " + ms2DecResult.Ms2DecPeakArea);
            sw.WriteLine("UNIQUEMS: " + ms2DecResult.UniqueMs);
            sw.WriteLine("Num Peaks: " + massSpectra.Count);

            for (int i = 0; i < massSpectra.Count; i++)
                sw.WriteLine(Math.Round(massSpectra[i][0], 5) + "\t" + Math.Round(massSpectra[i][1], 0));

            sw.WriteLine();
        }


        public static void WriteCentroidMgf(StreamWriter sw, ObservableCollection<RawSpectrum> spectrumCollection, DataType datatype,
            PeakAreaBean peakAreaBean, AnalysisParametersBean param) {
            var massSpectra = DataAccessLcUtility.GetCentroidMassSpectra(spectrumCollection, datatype, peakAreaBean.Ms2LevelDatapointNumber, 
                param.CentroidMs2Tolerance, param.PeakDetectionBasedCentroid);

            sw.WriteLine("BEGIN IONS");

            var titleString = "Unknown"; if (peakAreaBean.MetaboliteName != string.Empty) titleString = peakAreaBean.MetaboliteName;
            titleString += "; Ms1ScanNumber: " + peakAreaBean.Ms1LevelDatapointNumber + "; Ms2ScanNumber: " + peakAreaBean.Ms2LevelDatapointNumber;

            var adduct = AdductIonParcer.GetAdductIonBean(peakAreaBean.AdductIonName);
            var chargeChar = adduct.IonMode == IonMode.Positive ? "+" : "-";
            var chargeString = peakAreaBean.ChargeNumber + chargeChar;

            sw.WriteLine("TITLE=" + titleString);
            sw.WriteLine("SCANS=" + peakAreaBean.PeakID);
            sw.WriteLine("RTINMINUTES=" + peakAreaBean.RtAtPeakTop);
            sw.WriteLine("PEPMASS=" + peakAreaBean.AccurateMass);
            sw.WriteLine("ION=" + peakAreaBean.AdductIonName);
            sw.WriteLine("CHARGE=" + chargeString);

            for (int i = 0; i < massSpectra.Count; i++) {

                var specString = "1";
                if (massSpectra[i].Length == 5) {
                    var isotopeFrag = (int)massSpectra[i][3] == 1 ? "TRUE" : "FALSE";
                    specString = "Charge=" + (int)massSpectra[i][2] + chargeChar + "; IsotopeFrag=" + isotopeFrag + "; ParentID=" + (int)massSpectra[i][4];
                }
                sw.WriteLine(Math.Round(massSpectra[i][0], 5) + "\t" + Math.Round(massSpectra[i][1], 0));
            }

            sw.WriteLine("END IONS");
            sw.WriteLine();
        }

        public static void WriteCentroidMgf(StreamWriter sw, ObservableCollection<RawSpectrum> spectrumCollection, ProjectPropertyBean project, PeakAreaBean peakspot,
            DriftSpotBean driftspot, AnalysisParametersBean param) {
            var massSpectra = DataAccessLcUtility.GetAccumulatedMs2Spectra(spectrumCollection, driftspot, peakspot,
                param, project);

            sw.WriteLine("BEGIN IONS");

            var titleString = "Unknown"; if (driftspot.MetaboliteName != string.Empty) titleString = driftspot.MetaboliteName;
            titleString += "; Ms1ScanNumber: " + driftspot.Ms1LevelDatapointNumber + "; Ms2ScanNumber: " + driftspot.Ms2LevelDatapointNumber;

            var adduct = AdductIonParcer.GetAdductIonBean(driftspot.AdductIonName);
            var chargeChar = adduct.IonMode == IonMode.Positive ? "+" : "-";
            var chargeString = driftspot.ChargeNumber + chargeChar;

            sw.WriteLine("TITLE=" + titleString);
            sw.WriteLine("SCANS=" + driftspot.MasterPeakID);
            sw.WriteLine("RTINMINUTES=" + peakspot.RtAtPeakTop);
            sw.WriteLine("MOBILITY=" + driftspot.DriftTimeAtPeakTop);
            sw.WriteLine("CCS=" + driftspot.Ccs);
            sw.WriteLine("PEPMASS=" + driftspot.AccurateMass);
            sw.WriteLine("ION=" + driftspot.AdductIonName);
            sw.WriteLine("CHARGE=" + chargeString);

            for (int i = 0; i < massSpectra.Count; i++) {

                var specString = "1";
                if (massSpectra[i].Length == 5) {
                    var isotopeFrag = (int)massSpectra[i][3] == 1 ? "TRUE" : "FALSE";
                    specString = "Charge=" + (int)massSpectra[i][2] + chargeChar + "; IsotopeFrag=" + isotopeFrag + "; ParentID=" + (int)massSpectra[i][4];
                }
                sw.WriteLine(Math.Round(massSpectra[i][0], 5) + "\t" + Math.Round(massSpectra[i][1], 0));
            }

            sw.WriteLine("END IONS");
            sw.WriteLine();
        }

        public static void WriteProfileMgf(StreamWriter sw, ObservableCollection<RawSpectrum> spectrumCollection, PeakAreaBean peakAreaBean) {
            var massSpectra = DataAccessLcUtility.GetMsMsSpectra(spectrumCollection, peakAreaBean.Ms2LevelDatapointNumber);

            sw.WriteLine("BEGIN IONS");

            var titleString = "Unknown"; if (peakAreaBean.MetaboliteName != string.Empty) titleString = peakAreaBean.MetaboliteName;
            titleString += "; Ms1ScanNumber: " + peakAreaBean.Ms1LevelDatapointNumber + "; Ms2ScanNumber: " + peakAreaBean.Ms2LevelDatapointNumber;

            var adduct = AdductIonParcer.GetAdductIonBean(peakAreaBean.AdductIonName);
            var chargeChar = adduct.IonMode == IonMode.Positive ? "+" : "-";
            var chargeString = peakAreaBean.ChargeNumber + chargeChar;

            sw.WriteLine("TITLE=" + titleString);
            sw.WriteLine("SCANS=" + peakAreaBean.PeakID);
            sw.WriteLine("RTINMINUTES=" + peakAreaBean.RtAtPeakTop);
            sw.WriteLine("PEPMASS=" + peakAreaBean.AccurateMass);
            sw.WriteLine("ION=" + peakAreaBean.AdductIonName);
            sw.WriteLine("CHARGE=" + chargeString);

            for (int i = 0; i < massSpectra.Count; i++) {

                var specString = "1";
                if (massSpectra[i].Length == 5) {
                    var isotopeFrag = (int)massSpectra[i][3] == 1 ? "TRUE" : "FALSE";
                    specString = "Charge=" + (int)massSpectra[i][2] + chargeChar + "; IsotopeFrag=" + isotopeFrag + "; ParentID=" + (int)massSpectra[i][4];
                }
                //sw.WriteLine(Math.Round(massSpectra[i][0], 5) + "\t" + Math.Round(massSpectra[i][1], 0) + "\t" + "\"" + specString + "\"");
                sw.WriteLine(Math.Round(massSpectra[i][0], 5) + "\t" + Math.Round(massSpectra[i][1], 0));
            }

            sw.WriteLine("END IONS");
            sw.WriteLine();
        }

        public static void WriteProfileMgf(StreamWriter sw, ObservableCollection<RawSpectrum> spectrumCollection, PeakAreaBean peakSpot, DriftSpotBean driftspot) {
            var massSpectra = DataAccessLcUtility.GetMsMsSpectra(spectrumCollection, driftspot.Ms2LevelDatapointNumber);

            sw.WriteLine("BEGIN IONS");

            var titleString = "Unknown"; if (driftspot.MetaboliteName != string.Empty) titleString = driftspot.MetaboliteName;
            titleString += "; Ms1ScanNumber: " + driftspot.Ms1LevelDatapointNumber + "; Ms2ScanNumber: " + driftspot.Ms2LevelDatapointNumber;

            var adduct = AdductIonParcer.GetAdductIonBean(driftspot.AdductIonName);
            var chargeChar = adduct.IonMode == IonMode.Positive ? "+" : "-";
            var chargeString = driftspot.ChargeNumber + chargeChar;

            sw.WriteLine("TITLE=" + titleString);
            sw.WriteLine("SCANS=" + driftspot.MasterPeakID);
            sw.WriteLine("RTINMINUTES=" + peakSpot.RtAtPeakTop);
            sw.WriteLine("MOBILITY=" + driftspot.DriftTimeAtPeakTop);
            sw.WriteLine("CCS=" + driftspot.Ccs);
            sw.WriteLine("PEPMASS=" + driftspot.AccurateMass);
            sw.WriteLine("ION=" + driftspot.AdductIonName);
            sw.WriteLine("CHARGE=" + chargeString);

            for (int i = 0; i < massSpectra.Count; i++) {

                var specString = "1";
                if (massSpectra[i].Length == 5) {
                    var isotopeFrag = (int)massSpectra[i][3] == 1 ? "TRUE" : "FALSE";
                    specString = "Charge=" + (int)massSpectra[i][2] + chargeChar + "; IsotopeFrag=" + isotopeFrag + "; ParentID=" + (int)massSpectra[i][4];
                }
                //sw.WriteLine(Math.Round(massSpectra[i][0], 5) + "\t" + Math.Round(massSpectra[i][1], 0) + "\t" + "\"" + specString + "\"");
                sw.WriteLine(Math.Round(massSpectra[i][0], 5) + "\t" + Math.Round(massSpectra[i][1], 0));
            }

            sw.WriteLine("END IONS");
            sw.WriteLine();
        }


        public static void WriteCentroidedMgf(StreamWriter sw, FileStream fs, List<long> seekpointList, PeakAreaBean peakAreaBean) {
            var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, peakAreaBean.PeakID);
            var massSpectra = ms2DecResult.MassSpectra;

            sw.WriteLine("BEGIN IONS");

            var titleString = "Unknown"; if (peakAreaBean.MetaboliteName != string.Empty) titleString = peakAreaBean.MetaboliteName;
            titleString += "; Ms1ScanNumber: " + peakAreaBean.Ms1LevelDatapointNumber + "; Ms2ScanNumber: " + peakAreaBean.Ms2LevelDatapointNumber;

            var adduct = AdductIonParcer.GetAdductIonBean(peakAreaBean.AdductIonName);
            var chargeChar = adduct.IonMode == IonMode.Positive ? "+" : "-";
            var chargeString = peakAreaBean.ChargeNumber + chargeChar;

            sw.WriteLine("TITLE=" + titleString);
            sw.WriteLine("SCANS=" + peakAreaBean.PeakID);
            sw.WriteLine("RTINMINUTES=" + peakAreaBean.RtAtPeakTop);
            sw.WriteLine("PEPMASS=" + peakAreaBean.AccurateMass);
            //sw.WriteLine("INTENSITY=" + peakAreaBean.IntensityAtPeakTop);
            sw.WriteLine("ION=" + peakAreaBean.AdductIonName);
            sw.WriteLine("CHARGE=" + chargeString);
            //sw.WriteLine("ISOTOPE=" + peakAreaBean.IsotopeWeightNumber.ToString());

            if (massSpectra.Count > 0) {
                for (int i = 0; i < massSpectra.Count; i++) {

                    var specString = "1";
                    if (massSpectra[i].Length == 5) {
                        var isotopeFrag = (int)massSpectra[i][3] == 1 ? "TRUE" : "FALSE";
                        specString = "Charge=" + (int)massSpectra[i][2] + chargeChar + "; IsotopeFrag=" + isotopeFrag + "; ParentID=" + (int)massSpectra[i][4];
                    }

                    //sw.WriteLine(Math.Round(massSpectra[i][0], 5) + "\t" + Math.Round(massSpectra[i][1], 0) + "\t" + "\"" + specString + "\"");
                    sw.WriteLine(Math.Round(massSpectra[i][0], 5) + "\t" + Math.Round(massSpectra[i][1], 0));
                }
            }

            sw.WriteLine("END IONS");

            sw.WriteLine();
        }

        public static void WriteDeconvolutedMgf(StreamWriter sw, FileStream fs, List<long> seekpointList, PeakAreaBean peakAreaBean) {
            var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, peakAreaBean.PeakID);
            var massSpectra = ms2DecResult.MassSpectra;

            sw.WriteLine("BEGIN IONS");

            var titleString = "Unknown"; if (peakAreaBean.MetaboliteName != string.Empty) titleString = peakAreaBean.MetaboliteName;
            titleString += "; Ms1ScanNumber: " + peakAreaBean.Ms1LevelDatapointNumber + "; Ms2ScanNumber: " + peakAreaBean.Ms2LevelDatapointNumber;

            var adduct = AdductIonParcer.GetAdductIonBean(peakAreaBean.AdductIonName);
            var chargeChar = adduct.IonMode == IonMode.Positive ? "+" : "-";
            var chargeString = peakAreaBean.ChargeNumber + chargeChar;

            sw.WriteLine("TITLE=" + titleString);
            sw.WriteLine("SCANS=" + peakAreaBean.PeakID);
            sw.WriteLine("RTINMINUTES=" + peakAreaBean.RtAtPeakTop);
            sw.WriteLine("PEPMASS=" + peakAreaBean.AccurateMass);
            sw.WriteLine("ION=" + peakAreaBean.AdductIonName);
            sw.WriteLine("CHARGE=" + chargeString);

            for (int i = 0; i < massSpectra.Count; i++) {

                var specString = "1";
                if (massSpectra[i].Length == 5) {
                    var isotopeFrag = (int)massSpectra[i][3] == 1 ? "TRUE" : "FALSE";
                    specString = "Charge=" + (int)massSpectra[i][2] + chargeChar + "; IsotopeFrag=" + isotopeFrag + "; ParentID=" + (int)massSpectra[i][4];
                }
                //sw.WriteLine(Math.Round(massSpectra[i][0], 5) + "\t" + Math.Round(massSpectra[i][1], 0) + "\t" + "\"" + specString + "\"");
                sw.WriteLine(Math.Round(massSpectra[i][0], 5) + "\t" + Math.Round(massSpectra[i][1], 0));
            }
            sw.WriteLine("END IONS");

            sw.WriteLine();
        }

        public static void WriteDeconvolutedMgf(StreamWriter sw, FileStream fs, List<long> seekpointList, PeakAreaBean peakSpot, DriftSpotBean driftspot) {
            var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, driftspot.MasterPeakID);
            var massSpectra = ms2DecResult.MassSpectra;

            sw.WriteLine("BEGIN IONS");

            var titleString = "Unknown"; if (driftspot.MetaboliteName != string.Empty) titleString = driftspot.MetaboliteName;
            titleString += "; Ms1ScanNumber: " + driftspot.Ms1LevelDatapointNumber + "; Ms2ScanNumber: " + driftspot.Ms2LevelDatapointNumber;

            var adduct = AdductIonParcer.GetAdductIonBean(driftspot.AdductIonName);
            var chargeChar = adduct.IonMode == IonMode.Positive ? "+" : "-";
            var chargeString = driftspot.ChargeNumber + chargeChar;

            sw.WriteLine("TITLE=" + titleString);
            sw.WriteLine("SCANS=" + driftspot.MasterPeakID);
            sw.WriteLine("RTINMINUTES=" + peakSpot.RtAtPeakTop);
            sw.WriteLine("MOBILITY=" + driftspot.DriftTimeAtPeakTop);
            sw.WriteLine("CCS=" + driftspot.Ccs);
            sw.WriteLine("PEPMASS=" + driftspot.AccurateMass);
            sw.WriteLine("ION=" + driftspot.AdductIonName);
            sw.WriteLine("CHARGE=" + chargeString);

            for (int i = 0; i < massSpectra.Count; i++) {

                var specString = "1";
                if (massSpectra[i].Length == 5) {
                    var isotopeFrag = (int)massSpectra[i][3] == 1 ? "TRUE" : "FALSE";
                    specString = "Charge=" + (int)massSpectra[i][2] + chargeChar + "; IsotopeFrag=" + isotopeFrag + "; ParentID=" + (int)massSpectra[i][4];
                }
                //sw.WriteLine(Math.Round(massSpectra[i][0], 5) + "\t" + Math.Round(massSpectra[i][1], 0) + "\t" + "\"" + specString + "\"");
                sw.WriteLine(Math.Round(massSpectra[i][0], 5) + "\t" + Math.Round(massSpectra[i][1], 0));
            }
            sw.WriteLine("END IONS");

            sw.WriteLine();
        }

        public static void WriteProfileTxt(StreamWriter sw, ObservableCollection<RawSpectrum> spectrumCollection,
            PeakAreaBean peakAreaBean, ObservableCollection<PeakAreaBean> peakSpots, List<MspFormatCompoundInformationBean> mspDB, 
            List<PostIdentificatioinReferenceBean> textDB, AnalysisParametersBean param, float isotopeExportMax) {
            var ms1Spectra = DataAccessLcUtility.GetProfileMassSpectra(spectrumCollection, peakAreaBean.Ms1LevelDatapointNumber);
            var msmsSpectra = DataAccessLcUtility.GetProfileMassSpectra(spectrumCollection, peakAreaBean.Ms2LevelDatapointNumber);
            WriteMetaDataAsTxt(sw, peakAreaBean, peakSpots, mspDB, new List<float>(), textDB, param);
            WriteMs1IsotopeSpectrum(sw, ms1Spectra, peakAreaBean.AccurateMass, isotopeExportMax);
            sw.Write("\t");
            WriteSpectraAsTxt(sw, msmsSpectra);
            sw.WriteLine();
        }

        public static void WriteCentroidTxt(StreamWriter sw, ObservableCollection<RawSpectrum> spectrumCollection, PeakAreaBean peakAreaBean, ObservableCollection<PeakAreaBean> peakSpots,
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> textDB, AnalysisParametersBean param, ProjectPropertyBean projectProperty, float isotopeExportMax) {
            var ms1Spectra = DataAccessLcUtility.GetCentroidMassSpectra(spectrumCollection, projectProperty.DataType, peakAreaBean.Ms1LevelDatapointNumber, param.CentroidMs1Tolerance, true);
            var msmsSpectra = DataAccessLcUtility.GetCentroidMassSpectra(spectrumCollection, projectProperty.DataTypeMS2, peakAreaBean.Ms2LevelDatapointNumber, param.CentroidMs2Tolerance, true);
            WriteMetaDataAsTxt(sw, peakAreaBean, peakSpots, mspDB, new List<float>(), textDB, param);
            WriteMs1IsotopeSpectrum(sw, ms1Spectra, peakAreaBean.AccurateMass, isotopeExportMax);
            sw.Write("\t");
            WriteSpectraAsTxt(sw, msmsSpectra);
            sw.WriteLine();
        }


        public static void WriteMs2decResultAsTxt(StreamWriter sw, ObservableCollection<RawSpectrum> spectrumCollection, FileStream fs, List<long> seekpointList, PeakAreaBean peakAreaBean, ObservableCollection<PeakAreaBean> peakSpots,
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> textDB, AnalysisParametersBean param, ProjectPropertyBean projectProperty, float isotopeExportMax, bool isConsoleApp = false) {
            var ms1Spectra = DataAccessLcUtility.GetCentroidMassSpectra(spectrumCollection, projectProperty.DataType, peakAreaBean.Ms1LevelDatapointNumber, param.CentroidMs1Tolerance, true);
            var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, peakAreaBean.PeakID);
            var msmsSpectra = ms2DecResult.MassSpectra;
            
            WriteMetaDataAsTxt(sw, peakAreaBean, peakSpots, mspDB, ms2DecResult.ModelMasses, textDB, param);

            if (isConsoleApp)
                WriteSpectraAsTxt(sw, ms1Spectra, param.RelativeAbundanceCutOffForSpectrumExport);
            else
                WriteMs1IsotopeSpectrum(sw, ms1Spectra, peakAreaBean.AccurateMass, isotopeExportMax);

            sw.Write("\t");
            WriteSpectraAsTxt(sw, new ObservableCollection<double[]>(msmsSpectra));

            sw.WriteLine();
        }

        public static void WriteMs2decResultAsTxt(StreamWriter sw, ObservableCollection<RawSpectrum> accumulatedSpectra, ObservableCollection<RawSpectrum> spectrumCollection, 
            FileStream fs, List<long> seekpointList, PeakAreaBean peakAreaBean, ObservableCollection<PeakAreaBean> peakSpots, DriftSpotBean driftSpot, List<DriftSpotBean> driftSpots,
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> textDB, AnalysisParametersBean param, ProjectPropertyBean projectProperty, float isotopeExportMax, bool isConsoleApp = false) {
           
            ObservableCollection<double[]> ms1Spectra;
            MS2DecResult ms2DecResult;
            if (driftSpot == null) {
                Debug.WriteLine("rt axis {0}", peakAreaBean.Ms1LevelDatapointNumberAtAcculateMs1);
                ms1Spectra = DataAccessLcUtility.GetCentroidMassSpectra(accumulatedSpectra, projectProperty.DataType, peakAreaBean.Ms1LevelDatapointNumberAtAcculateMs1, param.CentroidMs1Tolerance, true);
                ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, peakAreaBean.MasterPeakID);
            }
            else {
                Debug.WriteLine("im axis {0}", driftSpot.Ms1LevelDatapointNumber);
                ms1Spectra = DataAccessLcUtility.GetCentroidMassSpectra(spectrumCollection, projectProperty.DataType, driftSpot.Ms1LevelDatapointNumber, param.CentroidMs1Tolerance, true);
                ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, driftSpot.MasterPeakID);
            }
            var msmsSpectra = ms2DecResult.MassSpectra;
            WriteMetaDataAsTxt(sw, peakAreaBean, peakSpots, driftSpot, driftSpots, mspDB, textDB, param, ms2DecResult.ModelMasses);

            if (isConsoleApp)
                WriteSpectraAsTxt(sw, ms1Spectra, param.RelativeAbundanceCutOffForSpectrumExport);
            else
                WriteMs1IsotopeSpectrum(sw, ms1Spectra, peakAreaBean.AccurateMass, isotopeExportMax);

            sw.Write("\t");
            WriteSpectraAsTxt(sw, new ObservableCollection<double[]>(msmsSpectra));

            sw.WriteLine();
        }


        public static string PeakSpotComment(PeakAreaBean peakSpot, ObservableCollection<PeakAreaBean> peakSpots) {
            var comment = peakSpot.Comment;
            if (peakSpot.AdductFromAmalgamation != null && peakSpot.AdductFromAmalgamation.FormatCheck) {
                if (comment == null || comment == string.Empty)
                    comment += peakSpot.AdductFromAmalgamation.AdductIonName + " (Amalgamation)";
                else
                    comment += "; " + peakSpot.AdductFromAmalgamation.AdductIonName + " (Amalgamation)";
            }
            if (peakSpot.PeakLinks != null && peakSpot.PeakLinks.Count > 0) {
                foreach (var peak in peakSpot.PeakLinks) {
                    var linkedID = peak.LinkedPeakID;
                    var linkedProp = peak.Character;

                    var linkedSpot = peakSpots[linkedID];

                    if (linkedProp == PeakLinkFeatureEnum.Isotope && peakSpot.AccurateMass > linkedSpot.AccurateMass) {
                        if (comment == null || comment == string.Empty)
                            comment += "isotope of " + linkedID;
                        else
                            comment += "; isotope of " + linkedID;
                    }
                    else if (linkedProp == PeakLinkFeatureEnum.Adduct) {
                        if (comment == null || comment == string.Empty)
                            comment += "adduct linked to " + linkedID + "_" + linkedSpot.AdductIonName;
                        else
                            comment += "; adduct linked to " + linkedID + "_" + linkedSpot.AdductIonName;
                    }
                    else if (linkedProp == PeakLinkFeatureEnum.ChromSimilar && peakSpot.AccurateMass < linkedSpot.AccurateMass) {
                        if (comment == null || comment == string.Empty)
                            comment += "similar chromatogram in higher mz_" + linkedID;
                        else
                            comment += "; similar chromatogram in higher mz_" + linkedID;
                    }
                    else if (linkedProp == PeakLinkFeatureEnum.FoundInUpperMsMs && peakSpot.AccurateMass < linkedSpot.AccurateMass) {
                        if (comment == null || comment == string.Empty)
                            comment += "found in higher mz's MsMs_" + linkedID;
                        else
                            comment += "; found in higher mz's MsMs_" + linkedID;
                    }
                }
            }

            return comment;
        }

        public static string AlignedSpotComment(AlignmentPropertyBean peakSpot, ObservableCollection<AlignmentPropertyBean> peakSpots) {
            var comment = string.Empty;
            if (peakSpot.AdductIonNameFromAmalgamation != null && peakSpot.AdductIonNameFromAmalgamation != string.Empty) {
                if (comment == null || comment == string.Empty) {
                    comment += peakSpot.AdductIonNameFromAmalgamation + " (Amalgamation)";
                }
                else {
                    comment += "; " + peakSpot.AdductIonNameFromAmalgamation + " (Amalgamation)";
                }
            }

            //if (peakSpot.IsManuallyModified) {
            //    if (comment == null || comment == string.Empty) {
            //        comment += "Areas are manually modified";
            //    }
            //    else {
            //        comment += "; Areas are manually modified";
            //    }
            //}
            if (peakSpot.PeakLinks != null && peakSpot.PeakLinks.Count > 0) {
                foreach (var peak in peakSpot.PeakLinks) {
                    var linkedID = peak.LinkedPeakID;
                    var linkedProp = peak.Character;
                    if (peakSpots.Count - 1 < linkedID) continue;
                    var linkedSpot = peakSpots[linkedID];

                    if (linkedProp == PeakLinkFeatureEnum.CorrelSimilar) {
                        if (comment == null || comment == string.Empty)
                            comment += "ion correlated with " + linkedID;
                        else
                            comment += "; ion correlated with " + linkedID;
                    }
                    else if (linkedProp == PeakLinkFeatureEnum.Adduct) {
                        if (comment == null || comment == string.Empty)
                            comment += "adduct linked to " + linkedID + "_" + linkedSpot.AdductIonName;
                        else
                            comment += "; adduct linked to " + linkedID + "_" + linkedSpot.AdductIonName;
                    }
                    else if (linkedProp == PeakLinkFeatureEnum.ChromSimilar && peakSpot.CentralAccurateMass < linkedSpot.CentralAccurateMass) {
                        if (comment == null || comment == string.Empty)
                            comment += "similar chromatogram in higher mz_" + linkedID;
                        else
                            comment += "; similar chromatogram in higher mz_" + linkedID;
                    }
                    else if (linkedProp == PeakLinkFeatureEnum.FoundInUpperMsMs && peakSpot.CentralAccurateMass < linkedSpot.CentralAccurateMass) {
                        if (comment == null || comment == string.Empty)
                            comment += "found in higher mz's MsMs_" + linkedID;
                        else
                            comment += "; found in higher mz's MsMs_" + linkedID;
                    }
                }
            }

            return comment;
        }

        public static string AlignedSpotComment(AlignmentPropertyBean peakSpot, List<AlignmentPropertyBean> peakSpots) {

            var comment = string.Empty;
            if (peakSpot.AdductIonNameFromAmalgamation != null && peakSpot.AdductIonNameFromAmalgamation != string.Empty) {
                if (comment == null || comment == string.Empty) {
                    comment += peakSpot.AdductIonNameFromAmalgamation + " (Amalgamation)";
                }
                else {
                    comment += "; " + peakSpot.AdductIonNameFromAmalgamation + " (Amalgamation)";
                }
            }

            //if (peakSpot.IsManuallyModified) {
            //    if (comment == null || comment == string.Empty) {
            //        comment += "Areas are manually modified";
            //    }
            //    else {
            //        comment += "; Areas are manually modified";
            //    }
            //}
            if (peakSpot.PeakLinks != null && peakSpot.PeakLinks.Count > 0) {
                foreach (var peak in peakSpot.PeakLinks) {
                    var linkedID = peak.LinkedPeakID;
                    var linkedProp = peak.Character;
                    var linkedSpot = peakSpots[linkedID];

                    if (linkedProp == PeakLinkFeatureEnum.CorrelSimilar) {
                        if (comment == null || comment == string.Empty)
                            comment += "ion correlated with " + linkedID;
                        else
                            comment += "; ion correlated with " + linkedID;
                    }
                    else if (linkedProp == PeakLinkFeatureEnum.Adduct) {
                        if (comment == null || comment == string.Empty)
                            comment += "adduct linked to " + linkedID + "_" + linkedSpot.AdductIonName;
                        else
                            comment += "; adduct linked to " + linkedID + "_" + linkedSpot.AdductIonName;
                    }
                    else if (linkedProp == PeakLinkFeatureEnum.ChromSimilar && peakSpot.CentralAccurateMass < linkedSpot.CentralAccurateMass) {
                        if (comment == null || comment == string.Empty)
                            comment += "similar chromatogram in higher mz_" + linkedID;
                        else
                            comment += "; similar chromatogram in higher mz_" + linkedID;
                    }
                    else if (linkedProp == PeakLinkFeatureEnum.FoundInUpperMsMs && peakSpot.CentralAccurateMass < linkedSpot.CentralAccurateMass) {
                        if (comment == null || comment == string.Empty)
                            comment += "found in higher mz's MsMs_" + linkedID;
                        else
                            comment += "; found in higher mz's MsMs_" + linkedID;
                    }
                }
            }

            return comment;

                //var comment = peakSpot.Comment;
                //if (peakSpot.PeakLinks != null && peakSpot.PeakLinks.Count > 0) {
                //    foreach (var peak in peakSpot.PeakLinks) {
                //        var linkedID = peak.LinkedPeakID;
                //        var linkedProp = peak.Character;

                //        var matchedSpots = peakSpots.Where(n => n.AlignmentID == linkedID).ToList();
                //        if (matchedSpots == null || matchedSpots.Count != 1) continue;

                //        //var linkedSpot = peakSpots[linkedID];
                //        var linkedSpot = matchedSpots[0];

                //        if (linkedProp == PeakLinkFeatureEnum.CorrelSimilar) {
                //            if (comment == null || comment == string.Empty)
                //                comment += "ion correlated with " + linkedID;
                //            else
                //                comment += "; ion correlated with " + linkedID;
                //        }
                //        else if (linkedProp == PeakLinkFeatureEnum.Adduct) {
                //            if (comment == null || comment == string.Empty)
                //                comment += "adduct linked to " + linkedID + "_" + linkedSpot.AdductIonName;
                //            else
                //                comment += "; adduct linked to " + linkedID + "_" + linkedSpot.AdductIonName;
                //        }
                //        else if (linkedProp == PeakLinkFeatureEnum.ChromSimilar && peakSpot.CentralAccurateMass < linkedSpot.CentralAccurateMass) {
                //            if (comment == null || comment == string.Empty)
                //                comment += "similar chromatogram in higher mz_" + linkedID;
                //            else
                //                comment += "; similar chromatogram in higher mz_" + linkedID;
                //        }
                //        else if (linkedProp == PeakLinkFeatureEnum.FoundInUpperMsMs && peakSpot.CentralAccurateMass < linkedSpot.CentralAccurateMass) {
                //            if (comment == null || comment == string.Empty)
                //                comment += "found in higher mz's MsMs_" + linkedID;
                //            else
                //                comment += "; found in higher mz's MsMs_" + linkedID;
                //        }
                //    }
                //}

                //return comment;
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

        public static void WriteData(StreamWriter sw, ObservableCollection<AlignedPeakPropertyBean> peaks, string exportType,
            bool replaceZeroToHalf, float nonZeroMin, List<BasicStats> statsList) {
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

        private static string GetSpotValue(AlignedPeakPropertyBean spotProperty, string exportType) {
            switch (exportType) {
                case "Height": return Math.Round(spotProperty.Variable, 0).ToString();
                case "Normalized": return spotProperty.NormalizedVariable.ToString();
                case "Area": return Math.Round(spotProperty.Area, 0).ToString();
                case "Id": return spotProperty.DriftTime > 0 ? spotProperty.MasterPeakID.ToString() : spotProperty.PeakID.ToString();
                case "RT": return Math.Round(spotProperty.RetentionTime, 3).ToString();
                case "Mobility": return Math.Round(spotProperty.DriftTime, 5).ToString();
                case "CCS": return Math.Round(spotProperty.Ccs, 3).ToString();
                case "MZ": return Math.Round(spotProperty.AccurateMass, 5).ToString();
                case "SN": return Math.Round(spotProperty.SignalToNoise, 1).ToString();
                case "MSMS": return spotProperty.Ms2ScanNumber >= 0 ? "TRUE" : "FALSE";
                default: return string.Empty;
            }
        }

        private static void WriteMs1IsotopeSpectrum(StreamWriter sw, ObservableCollection<double[]> spectra, float mass, float isotopeTraceMax) {
            if (spectra == null || spectra.Count == 0)
                return;

            if (isotopeTraceMax < 5) isotopeTraceMax = 5.0F;
            var startId = DataAccessLcUtility.GetMs1StartIndex(mass, 0.5F, spectra);
            var spectrumString = getIsotopicIonsString(spectra, mass, isotopeTraceMax);
            sw.Write(spectrumString);
        }

        private static string getIsotopicIonsString(ObservableCollection<double[]> spectra, float mass, float isotopeTraceMax) {
            if (spectra == null || spectra.Count == 0)
                return string.Empty;

            if (isotopeTraceMax < 5) isotopeTraceMax = 5.0F;
            var startId = DataAccessLcUtility.GetMs1StartIndex(mass, 0.5F, spectra);
            var spectrumString = string.Empty;
            for (int i = startId; i < spectra.Count; i++) {
                var mz = spectra[i][0];
                var intensity = spectra[i][1];

                if (mz < mass - 0.5) continue;
                if (mz > mass + isotopeTraceMax) break;

                spectrumString += Math.Round(mz, 5) + ":" + Math.Round(intensity, 0) + " ";
            }
            if (spectrumString != string.Empty)
                spectrumString = spectrumString.Trim();

            return spectrumString;
        }

        private static ObservableCollection<double[]> getIsotopicIonSpectra(ObservableCollection<double[]> spectra, float mass, float isotopeTraceMax) {
            if (spectra == null || spectra.Count == 0)
                return new ObservableCollection<double[]>();

            if (isotopeTraceMax < 5) isotopeTraceMax = 5.0F;
            var startId = DataAccessLcUtility.GetMs1StartIndex(mass, 0.5F, spectra);
            var isotopes = new ObservableCollection<double[]>();
            for (int i = startId; i < spectra.Count; i++) {
                var mz = spectra[i][0];
                var intensity = spectra[i][1];

                if (mz < mass - 0.5) continue;
                if (mz > mass + isotopeTraceMax) break;

                isotopes.Add(spectra[i]);
            }
            return isotopes;
        }

        public static void WriteSpectraAsTxt(StreamWriter sw, ObservableCollection<double[]> spectra, double relAbsCutOff = 0.0) {
            if (spectra == null || spectra.Count == 0)
                return;
            var max = spectra.Max(n => n[1]);
            sw.Write(String.Join(" ", spectra
                .Where(n => n[1] > max * relAbsCutOff)
                .Select(ion => Math.Round(ion[0], 5) + ":" + Math.Round(ion[1], 0))));
        }

        public static void WriteMetaDataAsTxt(StreamWriter sw, PeakAreaBean peakAreaBean, ObservableCollection<PeakAreaBean> peakSpots,
            List<MspFormatCompoundInformationBean> mspDB, List<float> modelMasses, List<PostIdentificatioinReferenceBean> textDB, AnalysisParametersBean param) {

            var peakid = peakAreaBean.PeakID; // peak spot id
            var metname = peakAreaBean.MetaboliteName == string.Empty ? "Unknown" : peakAreaBean.MetaboliteName;
            var scanid = peakAreaBean.ScanNumberAtPeakTop; // scan number of EIC chromatogram
            var rtLeft = peakAreaBean.RtAtLeftPeakEdge; // retention time of left edge
            var rt = peakAreaBean.RtAtPeakTop; // retention time of peak top
            var rtRight = peakAreaBean.RtAtRightPeakEdge; // retention time of right edge
            var mz = peakAreaBean.AccurateMass; // precursor m/z
            var height = (double)peakAreaBean.IntensityAtPeakTop; // peak height of a peak spot (MS1)
            var area = (double)peakAreaBean.AreaAboveZero; // peak area of a peak spot (MS1)
            var models = String.Join(",", modelMasses) == string.Empty ? "null" : String.Join(",", modelMasses);
            var adduct = peakAreaBean.AdductIonName; // one annotated adduct info from AdductIonEstimator.cs
            var isotope = "M + " + peakAreaBean.IsotopeWeightNumber.ToString(); // mono isotopic ion should be assigned as 'M+0'. This result is coming from IsotopeEstimator.cs
            var comment = PeakSpotComment(peakAreaBean, peakSpots);

            var formula = "null";
            var ontology = "null";
            var inchiKey = "null";
            var smiles = "null";
            var refRtString = "null";
            var refMzString = "null";
            var msiLevel = 999;
            var msiLevelString = "999";
            var lsiLevel = string.Empty;

            var libraryID = peakAreaBean.LibraryID;
            var textLibId = peakAreaBean.PostIdentificationLibraryId;


            if (textLibId >= 0 && textDB != null && textDB.Count != 0) {
                var refRt = textDB[textLibId].RetentionTime;
                if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
                refMzString = Math.Round(textDB[textLibId].AccurateMass, 5).ToString();
                if (textDB[textLibId].Formula != null && textDB[textLibId].Formula.FormulaString != null && textDB[textLibId].Formula.FormulaString != string.Empty)
                    formula = textDB[textLibId].Formula.FormulaString;
                if (textDB[textLibId].Inchikey != null && textDB[textLibId].Inchikey != string.Empty)
                    inchiKey = textDB[textLibId].Inchikey;
                if (textDB[textLibId].Smiles != null && textDB[textLibId].Smiles != string.Empty)
                    smiles = textDB[textLibId].Smiles;
                if (textDB[textLibId].Ontology != null && textDB[textLibId].Ontology != string.Empty)
                    ontology = textDB[textLibId].Ontology;
                msiLevelString = "annotated by user-defined text library";
            }
            else if (libraryID >= 0 && mspDB != null && mspDB.Count != 0) {
                var refRt = mspDB[libraryID].RetentionTime;
                if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
                refMzString = Math.Round(mspDB[libraryID].PrecursorMz, 5).ToString();

                formula = mspDB[libraryID].Formula;
                inchiKey = mspDB[libraryID].InchiKey;
                smiles = mspDB[libraryID].Smiles;
                ontology = mspDB[libraryID].Ontology;
                if (ontology == null || ontology == string.Empty) {
                    ontology = mspDB[libraryID].CompoundClass;
                }
                msiLevel = getMsiLevel(peakAreaBean, refRt, param);
                msiLevelString = msiLevel.ToString();
                //msiLevelString = lsiLevel != string.Empty ? msiLevel.ToString() + "_" + lsiLevel.ToString() : msiLevel.ToString();
            }

            var totalScore = peakAreaBean.TotalScore > 0
               ? peakAreaBean.TotalScore > 1000
               ? "100"
               : Math.Round(peakAreaBean.TotalScore * 0.1, 1).ToString()
               : "null";
            var rtScore = peakAreaBean.RtSimilarityValue > 0 ? Math.Round(peakAreaBean.RtSimilarityValue * 0.1, 1).ToString() : "null";
            var dotProduct = peakAreaBean.MassSpectraSimilarityValue > 0 ? Math.Round(peakAreaBean.MassSpectraSimilarityValue * 0.1, 1).ToString() : "null";
            var revDotProd = peakAreaBean.ReverseSearchSimilarityValue > 0 ? Math.Round(peakAreaBean.ReverseSearchSimilarityValue * 0.1, 1).ToString() : "null";
            var precense = peakAreaBean.PresenseSimilarityValue > 0
                ? peakAreaBean.PresenseSimilarityValue > 1000
                ? "100"
                : Math.Round(peakAreaBean.PresenseSimilarityValue * 0.1, 1).ToString()
                : "null";

            var rtMatched = "False";
            var mzMatched = "False";
            var ms2Matched = "False";

            if (peakAreaBean.IsRtMatch) rtMatched = "True";
            if (peakAreaBean.IsMs1Match) mzMatched = "True";
            if (peakAreaBean.IsMs2Match) ms2Matched = "True";

            var sn = peakAreaBean.SignalToNoise;

            //var header = new List<string>() { "PeakID", "Title", "Scans", "RT left(min)", "RT (min)", "RT right (min)",
            //        "Precursor m/z", "Height", "Area", "Model masses", "Adduct", "Isotope", "Comment",
            //        "Reference RT", "Reference m/z", "Formula", "Ontology", "InChIKey", "SMILES",
            //        "Annotation tag (VS1.0)", "RT matched", "m/z matched", "MS/MS matched",
            //        "RT similarity", "Dot product", "Reverse dot product", "Fragment presence %", "Total score",
            //        "S/N", "MS1 isotopes", "MSMS spectrum" };

            var field = new List<string>() { peakid.ToString(), metname, scanid.ToString(), rtLeft.ToString(), rt.ToString(), rtRight.ToString(),
                mz.ToString(), height.ToString(), area.ToString(), models, adduct, isotope, comment,
                refRtString, refMzString, formula, ontology, inchiKey, smiles, 
                msiLevelString, rtMatched, mzMatched, ms2Matched,
                rtScore, dotProduct, revDotProd, precense, totalScore, sn.ToString() };
            sw.Write(String.Join("\t", field.ToArray()) + "\t");

            //sw.Write(peakAreaBean.ScanNumberAtPeakTop + "\t"); // scan number of EIC chromatogram
            //sw.Write(peakAreaBean.RtAtLeftPeakEdge + "\t"); // retention time
            //sw.Write(peakAreaBean.RtAtPeakTop + "\t"); // retention time
            //sw.Write(peakAreaBean.RtAtRightPeakEdge + "\t"); // retention time
            //sw.Write(peakAreaBean.AccurateMass + "\t"); // precursor m/z
            //sw.Write(peakAreaBean.IntensityAtPeakTop + "\t"); // peak height of a peak spot (MS1)
            //sw.Write(peakAreaBean.AreaAboveZero + "\t"); // peak area of a peak spot (MS1)
            //sw.Write(peakAreaBean.MetaboliteName + "\t"); // same as name
            //sw.Write(String.Join(",", modelMasses) + "\t");
            //sw.Write(peakAreaBean.AdductIonName + "\t"); // one annotated adduct info from AdductIonEstimator.cs
            //sw.Write("M + " + peakAreaBean.IsotopeWeightNumber.ToString() + "\t"); // mono isotopic ion should be assigned as 'M+0'. This result is coming from IsotopeEstimator.cs
            //sw.Write(comment + "\t");
            //sw.Write(smiles + "\t");
            //sw.Write(inchikey + "\t");
            //sw.Write(peakAreaBean.MassSpectraSimilarityValue + "\t");
            //sw.Write(peakAreaBean.ReverseSearchSimilarityValue + "\t");
            //sw.Write(peakAreaBean.PresenseSimilarityValue + "\t");
            //sw.Write(peakAreaBean.SignalToNoise + "\t");
            //sw.Write(peakAreaBean.TotalScore + "\t");
        }

        public static void WriteMetaDataAsTxt(StreamWriter sw, PeakAreaBean peakAreaBean, ObservableCollection<PeakAreaBean> peakSpots, DriftSpotBean driftSpot, List<DriftSpotBean> driftSpots,
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> textDB, AnalysisParametersBean param, List<float> modelMasses) {

            var peakid = driftSpot == null ? peakAreaBean.MasterPeakID : driftSpot.MasterPeakID; // peak spot id
            var metname = driftSpot == null 
                ? peakAreaBean.MetaboliteName == string.Empty ? "Unknown" : peakAreaBean.MetaboliteName 
                : driftSpot.MetaboliteName == string.Empty ? "Unknown" : driftSpot.MetaboliteName;
            var scanid = driftSpot == null ? peakAreaBean.ScanNumberAtPeakTop : driftSpot.DriftScanAtPeakTop; // scan number of EIC chromatogram
            var rtLeft = peakAreaBean.RtAtLeftPeakEdge; // retention time of left edge
            var rt = peakAreaBean.RtAtPeakTop; // retention time of peak top
            var rtRight = peakAreaBean.RtAtRightPeakEdge; // retention time of right edge
            var dtLeft = driftSpot == null ? -1.0F : driftSpot.DriftTimeAtLeftPeakEdge; // retention time of left edge
            var dt = driftSpot == null ? -1.0F : driftSpot.DriftTimeAtPeakTop; // retention time of peak top
            var dtRight = driftSpot == null ? -1.0F : driftSpot.DriftTimeAtRightPeakEdge; // retention time of right edge
            var ccs = driftSpot == null ? -1.0F : driftSpot.Ccs; // retention time of peak top
            var mz = peakAreaBean.AccurateMass; // precursor m/z
            var height = driftSpot == null ? peakAreaBean.IntensityAtPeakTop : driftSpot.IntensityAtPeakTop; // peak height of a peak spot (MS1)
            var area = driftSpot == null ? peakAreaBean.AreaAboveZero : driftSpot.AreaAboveZero; // peak area of a peak spot (MS1)
            var models = String.Join(",", modelMasses);
            var adduct = driftSpot == null ? peakAreaBean.AdductIonName : driftSpot.AdductIonName; // one annotated adduct info from AdductIonEstimator.cs
            var isotope = "M + " + peakAreaBean.IsotopeWeightNumber.ToString(); // mono isotopic ion should be assigned as 'M+0'. This result is coming from IsotopeEstimator.cs
            var comment = PeakSpotComment(peakAreaBean, peakSpots);
            var formula = "null";
            var ontology = "null";
            var inchiKey = "null";
            var smiles = "null";
            var refRtString = "null";
            var refDtString = "null";
            var refCcsString = "null";
            var refMzString = "null";
            var msiLevel = 999;
            var msiLevelString = "999";
            var lsiLevel = string.Empty;
            var totalScore = "null";
            var rtScore = "null";
            var ccsScore = "null";
            var dotProduct = "null";
            var revDotProd = "null";
            var precense = "null";
            var rtMatched = "False";
            var ccsMatched = "False";
            var mzMatched = "False";
            var ms2Matched = "False";
            var repFilename = string.Empty;
            var libraryID = driftSpot == null ? peakAreaBean.LibraryID : driftSpot.LibraryID;
            var textLibId = driftSpot == null ? peakAreaBean.PostIdentificationLibraryId : driftSpot.PostIdentificationLibraryId;
            var sn = driftSpot == null ? peakAreaBean.SignalToNoise : driftSpot.SignalToNoise;

            var isMobility = driftSpot == null ? false : true;

            if (isMobility) {
                totalScore = driftSpot.TotalScore > 0
                ? driftSpot.TotalScore > 1000
                ? "100"
                : Math.Round(driftSpot.TotalScore * 0.1, 1).ToString()
                : "null";
                rtScore = driftSpot.RtSimilarityValue > 0 ? Math.Round(driftSpot.RtSimilarityValue * 0.1, 1).ToString() : "null";
                ccsScore = driftSpot.CcsSimilarity > 0 ? Math.Round(driftSpot.CcsSimilarity * 0.1, 1).ToString() : "null";
                dotProduct = driftSpot.MassSpectraSimilarityValue > 0 ? Math.Round(driftSpot.MassSpectraSimilarityValue * 0.1, 1).ToString() : "null";
                revDotProd = driftSpot.ReverseSearchSimilarityValue > 0 ? Math.Round(driftSpot.ReverseSearchSimilarityValue * 0.1, 1).ToString() : "null";
                precense = driftSpot.PresenseSimilarityValue > 0
                ? driftSpot.PresenseSimilarityValue > 1000
                ? "100"
                : Math.Round(driftSpot.PresenseSimilarityValue * 0.1, 1).ToString()
                : "null";

                ccs = driftSpot.Ccs;

                if (driftSpot.MetaboliteName != string.Empty) metname = driftSpot.MetaboliteName;
                if (textLibId >= 0 && textDB != null && textDB.Count != 0) {
                    var refRt = textDB[textLibId].RetentionTime;
                    if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
                    refMzString = Math.Round(textDB[textLibId].AccurateMass, 5).ToString();
                    if (textDB[textLibId].Formula != null && textDB[textLibId].Formula.FormulaString != null && textDB[textLibId].Formula.FormulaString != string.Empty)
                        formula = textDB[textLibId].Formula.FormulaString;
                    if (textDB[textLibId].Inchikey != null && textDB[textLibId].Inchikey != string.Empty)
                        inchiKey = textDB[textLibId].Inchikey;
                    if (textDB[textLibId].Smiles != null && textDB[textLibId].Smiles != string.Empty)
                        smiles = textDB[textLibId].Smiles;
                    if (textDB[textLibId].Ontology != null && textDB[textLibId].Ontology != string.Empty)
                        ontology = textDB[textLibId].Ontology;
                    msiLevelString = "annotated by user-defined text library";
                }
                else if (libraryID >= 0 && mspDB != null && mspDB.Count != 0) {
                    var refRt = mspDB[libraryID].RetentionTime;
                    if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
                    refMzString = Math.Round(mspDB[libraryID].PrecursorMz, 5).ToString();

                    var refCcs = mspDB[libraryID].CollisionCrossSection;
                    if (refCcs > 0) refCcsString = Math.Round(refCcs, 3).ToString();

                    formula = mspDB[libraryID].Formula;
                    inchiKey = mspDB[libraryID].InchiKey;
                    smiles = mspDB[libraryID].Smiles;
                    ontology = mspDB[libraryID].Ontology;
                    if (ontology == null || ontology == string.Empty) {
                        ontology = mspDB[libraryID].CompoundClass;
                    }
                    msiLevel = getMsiLevel(peakAreaBean, refRt, driftSpot, refCcs, param);
                    msiLevelString = msiLevel.ToString();
                    //msiLevelString = lsiLevel != string.Empty ? msiLevel.ToString() + "_" + lsiLevel.ToString() : msiLevel.ToString();
                }
                comment = driftSpot.Comment != null && driftSpot.Comment != string.Empty ? driftSpot.Comment : string.Empty;
                if (driftSpot.IsRtMatch) rtMatched = "True";
                if (driftSpot.IsCcsMatch) ccsMatched = "True";
                if (driftSpot.IsMs1Match) mzMatched = "True";
                if (driftSpot.IsMs2Match) ms2Matched = "True";
            }
            else {
                if (peakAreaBean.MetaboliteName != string.Empty) metname = peakAreaBean.MetaboliteName;
                if (textLibId >= 0 && textDB != null && textDB.Count != 0) {
                    var refRt = textDB[textLibId].RetentionTime;
                    if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
                    refMzString = Math.Round(textDB[textLibId].AccurateMass, 5).ToString();
                    if (textDB[textLibId].Formula != null && textDB[textLibId].Formula.FormulaString != null && textDB[textLibId].Formula.FormulaString != string.Empty)
                        formula = textDB[textLibId].Formula.FormulaString;
                    if (textDB[textLibId].Inchikey != null && textDB[textLibId].Inchikey != string.Empty)
                        inchiKey = textDB[textLibId].Inchikey;
                    if (textDB[textLibId].Smiles != null && textDB[textLibId].Smiles != string.Empty)
                        smiles = textDB[textLibId].Smiles;
                    if (textDB[textLibId].Ontology != null && textDB[textLibId].Ontology != string.Empty)
                        ontology = textDB[textLibId].Ontology;
                    msiLevelString = "annotated by user-defined text library";
                }
                else if (libraryID >= 0 && mspDB != null && mspDB.Count != 0) {
                    var refRt = mspDB[libraryID].RetentionTime;
                    if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
                    refMzString = Math.Round(mspDB[libraryID].PrecursorMz, 5).ToString();
                    formula = mspDB[libraryID].Formula;
                    inchiKey = mspDB[libraryID].InchiKey;
                    smiles = mspDB[libraryID].Smiles;
                    ontology = mspDB[libraryID].Ontology;
                    if (ontology == null || ontology == string.Empty) {
                        ontology = mspDB[libraryID].CompoundClass;
                    }
                    msiLevel = getMsiLevel(peakAreaBean, refRt, param);
                    msiLevelString = msiLevel.ToString();
                }
                totalScore = peakAreaBean.TotalScore > 0
                ? peakAreaBean.TotalScore > 1000
                ? "100"
                : Math.Round(peakAreaBean.TotalScore * 0.1, 1).ToString()
                : "null";
                rtScore = peakAreaBean.RtSimilarityValue > 0 ? Math.Round(peakAreaBean.RtSimilarityValue * 0.1, 1).ToString() : "null";
                ccsScore = peakAreaBean.CcsSimilarity > 0 ? Math.Round(peakAreaBean.CcsSimilarity * 0.1, 1).ToString() : "null";
                dotProduct = peakAreaBean.MassSpectraSimilarityValue > 0 ? Math.Round(peakAreaBean.MassSpectraSimilarityValue * 0.1, 1).ToString() : "null";
                revDotProd = peakAreaBean.ReverseSearchSimilarityValue > 0 ? Math.Round(peakAreaBean.ReverseSearchSimilarityValue * 0.1, 1).ToString() : "null";
                precense = peakAreaBean.PresenseSimilarityValue > 0
                ? peakAreaBean.PresenseSimilarityValue > 1000
                ? "100"
                : Math.Round(peakAreaBean.PresenseSimilarityValue * 0.1, 1).ToString()
                : "null";

                if (peakAreaBean.IsRtMatch) rtMatched = "True";
                if (peakAreaBean.IsCcsMatch) ccsMatched = "True";
                if (peakAreaBean.IsMs1Match) mzMatched = "True";
                if (peakAreaBean.IsMs2Match) ms2Matched = "True";
            }



            //var header = new List<string>() { "PeakID", "Title", "Scans", "RT left(min)", "RT (min)", "RT right (min)", "Mobility left", "Mobility", "Mobility right",
            //        "CCS", "Precursor m/z", "Height", "Area", "Model masses", "Adduct", "Isotope", "Comment",
            //        "Reference RT", "Reference CCS", "Reference m/z", "Formula", "Ontology", "InChIKey", "SMILES",
            //        "Annotation tag (VS1.0)", "RT matched", "CCS matched", "m/z matched", "MS/MS matched",
            //        "RT similarity", "CCS similarity", "Dot product", "Reverse dot product", "Fragment presence %", "Total score",
            //        "S/N", "MS1 isotopes", "MSMS spectrum" };

            var field = new List<string>() { peakid.ToString(), metname, scanid.ToString(), rtLeft.ToString(), rt.ToString(), rtRight.ToString(), dtLeft.ToString(), dt.ToString(), dtRight.ToString(),
                ccs.ToString(), mz.ToString(), height.ToString(), area.ToString(), models, adduct, isotope, comment, 
                refRtString, refCcsString, refMzString, formula, ontology, inchiKey, smiles,
                msiLevelString, rtMatched, ccsMatched, mzMatched, ms2Matched,
                rtScore, ccsScore, dotProduct, revDotProd, precense, totalScore, sn.ToString() };
            sw.Write(String.Join("\t", field.ToArray()) + "\t");
        }
        #endregion

        #region //utility for alignment result export




        public static void WriteDeconvolutedTxt(StreamWriter sw, FileStream fs, List<long> seekpointList,
            AlignmentPropertyBean alignmentPropertyBean, List<MspFormatCompoundInformationBean> mspDB) {
            MS2DecResult deconvolutionResultBean = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, alignmentPropertyBean.AlignmentID);
            List<double[]> massSpectra = deconvolutionResultBean.MassSpectra;

            if (alignmentPropertyBean.MetaboliteName == string.Empty) sw.Write("Unknown\t");
            else sw.Write(alignmentPropertyBean.MetaboliteName + "\t");

            sw.Write(alignmentPropertyBean.CentralRetentionTime + "\t");
            sw.Write(alignmentPropertyBean.CentralAccurateMass + "\t");
            sw.Write(alignmentPropertyBean.MetaboliteName + "\t");
            sw.Write(alignmentPropertyBean.AdductIonName + "\t");
            sw.Write(massSpectra.Count + "\t");

            for (int i = 0; i < massSpectra.Count; i++)
                sw.Write(Math.Round(massSpectra[i][0], 5) + " " + Math.Round(massSpectra[i][1], 0) + ";");

            sw.Write("\t");
            sw.Write(alignmentPropertyBean.MassSpectraSimilarity + "\t");
            sw.Write(alignmentPropertyBean.ReverseSimilarity + "\t");
            sw.Write(alignmentPropertyBean.AccurateMassSimilarity + "\t");
            sw.Write(alignmentPropertyBean.IsotopeSimilarity + "\t");
            sw.Write(alignmentPropertyBean.RetentionTimeSimilarity + "\t");
            sw.Write(alignmentPropertyBean.TotalSimilairty + "\t");

            if (alignmentPropertyBean.LibraryID > 0 && mspDB != null && mspDB.Count != 0) {
                sw.Write(mspDB[alignmentPropertyBean.LibraryID].PrecursorMz + "\t");
                sw.Write(mspDB[alignmentPropertyBean.LibraryID].RetentionTime + "\t");
                sw.Write(mspDB[alignmentPropertyBean.LibraryID].Formula + "\t");
            }
            else {
                sw.Write("" + "\t");
                sw.Write("" + "\t");
                sw.Write("" + "\t");
            }

            for (int i = 0; i < alignmentPropertyBean.AlignedPeakPropertyBeanCollection.Count; i++) {
                if (i == alignmentPropertyBean.AlignedPeakPropertyBeanCollection.Count - 1) sw.WriteLine(alignmentPropertyBean.AlignedPeakPropertyBeanCollection[i].Variable);
                else sw.Write(alignmentPropertyBean.AlignedPeakPropertyBeanCollection[i].Variable + "\t");
            }
        }

        public static void WriteDeconvolutedMsp(StreamWriter sw, FileStream fs, List<long> seekpointList, AlignmentPropertyBean alignedSpot) {
            var ms2Dec = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, alignedSpot.AlignmentID);
            var massSpectra = ms2Dec.MassSpectra;

            var metabolitename = alignedSpot.MetaboliteName;
            if (alignedSpot.MetaboliteName == string.Empty) metabolitename = "Unknown";
            var rt = alignedSpot.CentralRetentionTime;
            var mz = alignedSpot.CentralAccurateMass;
            var adduct = alignedSpot.AdductIonName;
            var comment = alignedSpot.AlignmentID;

            sw.WriteLine("NAME: " + metabolitename);
            sw.WriteLine("PRECURSORMZ: " + alignedSpot.CentralAccurateMass);
            sw.WriteLine("PRECURSORTYPE: " + alignedSpot.AdductIonName);
            sw.WriteLine("RETENTIONTIME: " + alignedSpot.CentralRetentionTime);
            sw.WriteLine("Comment: " + "\t" + alignedSpot.AlignmentID); //
            sw.WriteLine("Num Peaks: " + massSpectra.Count);

            for (int i = 0; i < massSpectra.Count; i++)
                sw.WriteLine(Math.Round(massSpectra[i][0], 5) + "\t" + Math.Round(massSpectra[i][1], 0));

            sw.WriteLine();
        }

        public static void WriteDeconvolutedMsp(StreamWriter sw, FileStream fs, List<long> seekpointList, 
            AlignmentPropertyBean alignedSpot, AlignedDriftSpotPropertyBean driftSpot) {
            var ms2Dec = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, driftSpot.MasterID);
            var massSpectra = ms2Dec.MassSpectra;

            var metabolitename = driftSpot.MetaboliteName;
            if (driftSpot.MetaboliteName == string.Empty) metabolitename = "Unknown";
            var rt = alignedSpot.CentralRetentionTime;
            var mz = alignedSpot.CentralAccurateMass;
            var adduct = driftSpot.AdductIonName;
            var comment = driftSpot.MasterID;

            sw.WriteLine("NAME: " + metabolitename);
            sw.WriteLine("PRECURSORMZ: " + alignedSpot.CentralAccurateMass);
            sw.WriteLine("PRECURSORTYPE: " + driftSpot.AdductIonName);
            sw.WriteLine("RETENTIONTIME: " + alignedSpot.CentralRetentionTime);
            sw.WriteLine("MOBILIRY: " + driftSpot.CentralDriftTime);
            sw.WriteLine("CCS: " + driftSpot.CentralCcs);
            sw.WriteLine("Comment: " + "\t" + driftSpot.MasterID); //
            sw.WriteLine("Num Peaks: " + massSpectra.Count);

            for (int i = 0; i < massSpectra.Count; i++)
                sw.WriteLine(Math.Round(massSpectra[i][0], 5) + "\t" + Math.Round(massSpectra[i][1], 0));

            sw.WriteLine();
        }

        public static void WriteMs2DecDataAsMsp(StreamWriter sw, AlignmentPropertyBean alignedSpot, 
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> textDB,
            FileStream fs, List<long> seekpointList, AnalysisParametersBean param, bool isNormalized = false) {
            var metaboliteName = "Unknown";
            var adduct = alignedSpot.AdductIonName;
            var formula = "null";
            var ontology = "null";
            var inchiKey = "null";
            var smiles = "null";
            var comment = "";
            var libraryID = alignedSpot.LibraryID;
            var textLibId = alignedSpot.PostIdentificationLibraryID;
            var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, alignedSpot.AlignmentID);
            //var ms1IsotopeSpectrum = getMs1IsotopicSpectrumString(ms2DecResult);
            var massSpectra = ms2DecResult.MassSpectra;

            if (alignedSpot.MetaboliteName != string.Empty) metaboliteName = alignedSpot.MetaboliteName;
            if (textLibId >= 0 && textDB != null && textDB.Count != 0) {
                var refRt = textDB[textLibId].RetentionTime;
                if (textDB[textLibId].Formula != null && textDB[textLibId].Formula.FormulaString != null && textDB[textLibId].Formula.FormulaString != string.Empty)
                    formula = textDB[textLibId].Formula.FormulaString;
                if (textDB[textLibId].Inchikey != null && textDB[textLibId].Inchikey != string.Empty)
                    inchiKey = textDB[textLibId].Inchikey;
                if (textDB[textLibId].Smiles != null && textDB[textLibId].Smiles != string.Empty)
                    smiles = textDB[textLibId].Smiles;
                if (textDB[textLibId].Ontology != null && textDB[textLibId].Ontology != string.Empty)
                    ontology = textDB[textLibId].Ontology;
            }
            else if (libraryID >= 0 && mspDB != null && mspDB.Count != 0) {
                var refRt = mspDB[libraryID].RetentionTime;
                formula = mspDB[libraryID].Formula;
                inchiKey = mspDB[libraryID].InchiKey;
                smiles = mspDB[libraryID].Smiles;
                ontology = mspDB[libraryID].Ontology;
                if (ontology == null || ontology == string.Empty) {
                    ontology = mspDB[libraryID].CompoundClass;
                }
                adduct = mspDB[libraryID].AdductIonBean.AdductIonName;
            }
            comment = alignedSpot.Comment != null && alignedSpot.Comment != string.Empty ? alignedSpot.Comment : string.Empty;
            if (isNormalized == true) {
                if (comment == string.Empty) comment = "Normalized unit " + alignedSpot.IonAbundanceUnit.ToString();
                else comment += "; Normalized unit " + alignedSpot.IonAbundanceUnit.ToString();
            }

            if (metaboliteName.Contains("HexHexHex"))
                metaboliteName = metaboliteName.Replace("HexHexHex", "Hex3");
            else if (ontology.Contains("HexHex"))
                metaboliteName = metaboliteName.Replace("HexHex", "Hex2");

            if (ontology.Contains("HexHexHex"))
                ontology = "Hex3Cer";
            else if (ontology.Contains("HexHex"))
                ontology = "Hex2Cer";

            comment = alignedSpot.Comment != null && alignedSpot.Comment != string.Empty ? alignedSpot.Comment : string.Empty;
            if (comment.ToLower().Contains("is (y") || comment.ToLower().Contains("is(y"))
                ontology = "IS (Yes); " + metaboliteName.Split(' ')[0];
            else if (comment.ToLower().Contains("is (n") || comment.ToLower().Contains("is(n"))
                ontology = "IS (NO); " + metaboliteName.Split(' ')[0];
            if (comment.Contains("BMP")) {
                ontology = "BMP";
            }
            if (comment.Contains("p/")) {
                ontology = "EtherPE(Plasmalogen)";
                if (adduct == "[M+H]+") {
                    metaboliteName = alignedSpot.Comment;
                }
            }

            var adductObj = AdductIonParcer.GetAdductIonBean(adduct);

            sw.WriteLine("NAME: " + metaboliteName);
            sw.WriteLine("SCANNUMBER: " + alignedSpot.AlignmentID);
            sw.WriteLine("RETENTIONTIME: " + alignedSpot.CentralRetentionTime);
            sw.WriteLine("PRECURSORMZ: " + alignedSpot.CentralAccurateMass);
            sw.WriteLine("PRECURSORTYPE: " + adduct);
            sw.WriteLine("IONMODE: " + adductObj.IonMode);
            sw.WriteLine("FORMULA: " + formula);
            sw.WriteLine("ONTOLOGY: " + ontology);
            sw.WriteLine("INCHIKEY: " + inchiKey);
            sw.WriteLine("SMILES: " + smiles);
            sw.WriteLine("Num Peaks: " + massSpectra.Count);

            foreach (var ion in massSpectra) {
                sw.WriteLine(Math.Round(ion[0], 5) + "\t" + Math.Round(ion[1], 0));
            }
            sw.WriteLine();
        }

        public static void WriteMs2DecDataAsMsp(StreamWriter sw, AlignmentPropertyBean alignedSpot, AlignedDriftSpotPropertyBean driftSpot,
           List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> textDB,
           FileStream fs, List<long> seekpointList, AnalysisParametersBean param, bool isNormalized = false) {
            var metaboliteName = "Unknown";
            var adduct = driftSpot.AdductIonName;
            var formula = "null";
            var ontology = "null";
            var inchiKey = "null";
            var smiles = "null";
            var comment = "";
            var libraryID = driftSpot.LibraryID;
            var textLibId = driftSpot.PostIdentificationLibraryID;
            var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, driftSpot.MasterID);
            //var ms1IsotopeSpectrum = getMs1IsotopicSpectrumString(ms2DecResult);
            var massSpectra = ms2DecResult.MassSpectra;

            if (driftSpot.MetaboliteName != string.Empty) metaboliteName = driftSpot.MetaboliteName;
            if (textLibId >= 0 && textDB != null && textDB.Count != 0) {
                var refRt = textDB[textLibId].RetentionTime;
                if (textDB[textLibId].Formula != null && textDB[textLibId].Formula.FormulaString != null && textDB[textLibId].Formula.FormulaString != string.Empty)
                    formula = textDB[textLibId].Formula.FormulaString;
                if (textDB[textLibId].Inchikey != null && textDB[textLibId].Inchikey != string.Empty)
                    inchiKey = textDB[textLibId].Inchikey;
                if (textDB[textLibId].Smiles != null && textDB[textLibId].Smiles != string.Empty)
                    smiles = textDB[textLibId].Smiles;
                if (textDB[textLibId].Ontology != null && textDB[textLibId].Ontology != string.Empty)
                    ontology = textDB[textLibId].Ontology;
            }
            else if (libraryID >= 0 && mspDB != null && mspDB.Count != 0) {
                var refRt = mspDB[libraryID].RetentionTime;
                formula = mspDB[libraryID].Formula;
                inchiKey = mspDB[libraryID].InchiKey;
                smiles = mspDB[libraryID].Smiles;
                ontology = mspDB[libraryID].Ontology;
                if (ontology == null || ontology == string.Empty) {
                    ontology = mspDB[libraryID].CompoundClass;
                }
                adduct = mspDB[libraryID].AdductIonBean.AdductIonName;
            }
            comment = driftSpot.Comment != null && driftSpot.Comment != string.Empty ? driftSpot.Comment : string.Empty;
            if (isNormalized == true) {
                if (comment == string.Empty) comment = "Normalized unit " + driftSpot.IonAbundanceUnit.ToString();
                else comment += "; Normalized unit " + driftSpot.IonAbundanceUnit.ToString();
            }

            if (metaboliteName.Contains("HexHexHex"))
                metaboliteName = metaboliteName.Replace("HexHexHex", "Hex3");
            else if (ontology.Contains("HexHex"))
                metaboliteName = metaboliteName.Replace("HexHex", "Hex2");

            if (ontology.Contains("HexHexHex"))
                ontology = "Hex3Cer";
            else if (ontology.Contains("HexHex"))
                ontology = "Hex2Cer";

            comment = driftSpot.Comment != null && driftSpot.Comment != string.Empty ? driftSpot.Comment : string.Empty;
            if (comment.ToLower().Contains("is (y") || comment.ToLower().Contains("is(y"))
                ontology = "IS (Yes); " + metaboliteName.Split(' ')[0];
            else if (comment.ToLower().Contains("is (n") || comment.ToLower().Contains("is(n"))
                ontology = "IS (NO); " + metaboliteName.Split(' ')[0];
            if (comment.Contains("BMP")) {
                ontology = "BMP";
            }
            if (comment.Contains("p/") || metaboliteName.Contains("p/")) {
                ontology = "EtherPE(Plasmalogen)";
                if (adduct == "[M+H]+") {
                    //metaboliteName = driftSpot.Comment;
                }
            }

            var adductObj = AdductIonParcer.GetAdductIonBean(adduct);

            sw.WriteLine("NAME: " + metaboliteName);
            sw.WriteLine("SCANNUMBER: " + driftSpot.MasterID);
            sw.WriteLine("RETENTIONTIME: " + alignedSpot.CentralRetentionTime);
            sw.WriteLine("PRECURSORMZ: " + alignedSpot.CentralAccurateMass);
            sw.WriteLine("PRECURSORTYPE: " + adduct);
            sw.WriteLine("CCS: " + driftSpot.CentralCcs);
            sw.WriteLine("IONMODE: " + adductObj.IonMode);
            sw.WriteLine("FORMULA: " + formula);
            sw.WriteLine("ONTOLOGY: " + ontology);
            sw.WriteLine("INCHIKEY: " + inchiKey);
            sw.WriteLine("SMILES: " + smiles);
            sw.WriteLine("Num Peaks: " + massSpectra.Count);

            foreach (var ion in massSpectra) {
                sw.WriteLine(Math.Round(ion[0], 5) + "\t" + Math.Round(ion[1], 0));
            }
            sw.WriteLine();
        }



        public static void WriteDeconvolutedGnpsMgf(StreamWriter sw, FileStream fs, List<long> seekpointList,
            AlignmentPropertyBean alignmentPropertyBean, int id) {
            var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, alignmentPropertyBean.AlignmentID);
            var massSpectra = ms2DecResult.MassSpectra;
            if (massSpectra == null || massSpectra.Count == 0)
                return;

            var adduct = AdductIonParcer.GetAdductIonBean(alignmentPropertyBean.AdductIonName);
            var charge = adduct.ChargeNumber;
            var chargeString = charge > 0 ? Math.Abs(charge) + "+" : Math.Abs(charge) + "-";

            sw.WriteLine("BEGIN IONS");
            sw.WriteLine("SCANS=" + id);
            sw.WriteLine("PEPMASS=" + alignmentPropertyBean.CentralAccurateMass);
            sw.WriteLine("MSLEVEL=2");
            sw.WriteLine("CHARGE=" + chargeString);
            sw.WriteLine("RTINMINUTES=" + alignmentPropertyBean.CentralRetentionTime);
            sw.WriteLine("ION=" + alignmentPropertyBean.AdductIonName);

            for (int i = 0; i < massSpectra.Count; i++)
                sw.WriteLine(Math.Round(massSpectra[i][0], 5) + " " + Math.Round(massSpectra[i][1], 0));

            sw.WriteLine("END IONS");

            sw.WriteLine();
        }

        public static void WriteDeconvolutedGnpsMgf(StreamWriter sw, FileStream fs, List<long> seekpointList,
         AlignmentPropertyBean alignmentPropertyBean, AlignedDriftSpotPropertyBean driftProperty, int id) {
            var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, driftProperty.MasterID);
            var massSpectra = ms2DecResult.MassSpectra;
            if (massSpectra == null || massSpectra.Count == 0)
                return;

            var adduct = AdductIonParcer.GetAdductIonBean(alignmentPropertyBean.AdductIonName);
            var charge = adduct.ChargeNumber;
            var chargeString = charge > 0 ? Math.Abs(charge) + "+" : Math.Abs(charge) + "-";

            sw.WriteLine("BEGIN IONS");
            sw.WriteLine("SCANS=" + id);
            sw.WriteLine("PEPMASS=" + alignmentPropertyBean.CentralAccurateMass);
            sw.WriteLine("MSLEVEL=2");
            sw.WriteLine("CHARGE=" + chargeString);
            sw.WriteLine("RTINMINUTES=" + alignmentPropertyBean.CentralRetentionTime);
            sw.WriteLine("DRIFTTIME=" + driftProperty.CentralDriftTime);
            sw.WriteLine("CCS=" + driftProperty.CentralCcs);
            sw.WriteLine("ION=" + alignmentPropertyBean.AdductIonName);

            for (int i = 0; i < massSpectra.Count; i++)
                sw.WriteLine(Math.Round(massSpectra[i][0], 5) + " " + Math.Round(massSpectra[i][1], 0));

            sw.WriteLine("END IONS");

            sw.WriteLine();
        }



        public static void WriteDeconvolutedMgf(StreamWriter sw, FileStream fs, List<long> seekpointList, AlignmentPropertyBean alignmentPropertyBean) {
            var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, alignmentPropertyBean.AlignmentID);
            var massSpectra = ms2DecResult.MassSpectra;
            if (massSpectra == null || massSpectra.Count == 0)
                return;

            var adduct = AdductIonParcer.GetAdductIonBean(alignmentPropertyBean.AdductIonName);
            var charge = adduct.ChargeNumber;
            var chargeString = charge > 0 ? Math.Abs(charge) + "+" : Math.Abs(charge) + "-";

            sw.WriteLine("BEGIN IONS");
            sw.WriteLine("SCANS=" + alignmentPropertyBean.AlignmentID);
            sw.WriteLine("PEPMASS=" + alignmentPropertyBean.CentralAccurateMass);
            sw.WriteLine("MSLEVEL=2");
            sw.WriteLine("CHARGE=" + chargeString);
            sw.WriteLine("RTINMINUTES=" + alignmentPropertyBean.CentralRetentionTime);
            sw.WriteLine("ION=" + alignmentPropertyBean.AdductIonName);

            for (int i = 0; i < massSpectra.Count; i++)
                sw.WriteLine(Math.Round(massSpectra[i][0], 5) + " " + Math.Round(massSpectra[i][1], 0));

            sw.WriteLine("END IONS");

            sw.WriteLine();
        }

        public static void WriteDeconvolutedMgf(StreamWriter sw, FileStream fs, List<long> seekpointList,
            AlignmentPropertyBean alignedSpot, AlignedDriftSpotPropertyBean driftSpot) {
            var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, driftSpot.MasterID);
            var massSpectra = ms2DecResult.MassSpectra;
            if (massSpectra == null || massSpectra.Count == 0)
                return;

            var adduct = AdductIonParcer.GetAdductIonBean(driftSpot.AdductIonName);
            var charge = adduct.ChargeNumber;
            var chargeString = charge > 0 ? Math.Abs(charge) + "+" : Math.Abs(charge) + "-";

            sw.WriteLine("BEGIN IONS");
            sw.WriteLine("SCANS=" + driftSpot.MasterID);
            sw.WriteLine("PEPMASS=" + alignedSpot.CentralAccurateMass);
            sw.WriteLine("MSLEVEL=2");
            sw.WriteLine("CHARGE=" + chargeString);
            sw.WriteLine("RTINMINUTES=" + alignedSpot.CentralRetentionTime);
            sw.WriteLine("MOBILITY=" + driftSpot.CentralDriftTime);
            sw.WriteLine("CCS=" + driftSpot.CentralCcs);
            sw.WriteLine("ION=" + driftSpot.AdductIonName);

            for (int i = 0; i < massSpectra.Count; i++)
                sw.WriteLine(Math.Round(massSpectra[i][0], 5) + " " + Math.Round(massSpectra[i][1], 0));

            sw.WriteLine("END IONS");

            sw.WriteLine();
        }


        public static void WriteDataMatrixMetaData(StreamWriter sw, AlignmentPropertyBean alignedSpot, ObservableCollection<AlignmentPropertyBean> alignedSpots,
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> textDB,
            FileStream fs, List<long> seekpointList, AnalysisParametersBean param, bool isNormalized = false) {
            var metaboliteName = "Unknown";
            var formula = "null";
            var ontology = "null";
            var inchiKey = "null";
            var smiles = "null";
            var refRtString = "null";
            var refMzString = "null";
            var msiLevel = 999;
            var msiLevelString = "999";
            var lsiLevel = string.Empty;
            var comment = "";
            var isManuallyModified = alignedSpot.IsManuallyModified;
            var isManuallyModifiedForAnnotation = alignedSpot.IsManuallyModifiedForAnnotation;
            var libraryID = alignedSpot.LibraryID;
            var textLibId = alignedSpot.PostIdentificationLibraryID;
            var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, alignedSpot.AlignmentID);
            var ms1IsotopeSpectrum = getMs1IsotopicSpectrumString(ms2DecResult);
            var massSpectra = ms2DecResult.MassSpectra;
            var postCurationResult = AlignedSpotComment(alignedSpot, alignedSpots);
            var isotopeTrackingParentID =
               alignedSpot.IsotopeTrackingParentID >= 0
               ? alignedSpot.IsotopeTrackingParentID.ToString()
               : "null";

            var isotopeTrackingWeightNumber =
               alignedSpot.IsotopeTrackingWeightNumber >= 0
               ? alignedSpot.IsotopeTrackingWeightNumber.ToString()
               : "null";
            var totalScore = alignedSpot.TotalSimilairty > 0
                ? alignedSpot.TotalSimilairty > 1000
                ? "100"
                : Math.Round(alignedSpot.TotalSimilairty * 0.1, 1).ToString()
                : "null";
            var rtScore = alignedSpot.RetentionTimeSimilarity > 0 ? Math.Round(alignedSpot.RetentionTimeSimilarity * 0.1, 1).ToString() : "null";
            var dotProduct = alignedSpot.MassSpectraSimilarity > 0 ? Math.Round(alignedSpot.MassSpectraSimilarity * 0.1, 1).ToString() : "null";
            var revDotProd = alignedSpot.ReverseSimilarity > 0 ? Math.Round(alignedSpot.ReverseSimilarity * 0.1, 1).ToString() : "null";
            var precense = alignedSpot.FragmentPresencePercentage > 0
                ? alignedSpot.FragmentPresencePercentage > 1000
                ? "100"
                : Math.Round(alignedSpot.FragmentPresencePercentage * 0.1, 1).ToString()
                : "null";

            var rtMatched = "False";
            var mzMatched = "False";
            var ms2Matched = "False";

            if (alignedSpot.MetaboliteName != string.Empty) metaboliteName = alignedSpot.MetaboliteName;
            if (textLibId >= 0 && textDB != null && textDB.Count != 0) {
                var refRt = textDB[textLibId].RetentionTime;
                if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
                refMzString = Math.Round(textDB[textLibId].AccurateMass, 5).ToString();
                if (textDB[textLibId].Formula != null && textDB[textLibId].Formula.FormulaString != null && textDB[textLibId].Formula.FormulaString != string.Empty)
                    formula = textDB[textLibId].Formula.FormulaString;
                if (textDB[textLibId].Inchikey != null && textDB[textLibId].Inchikey != string.Empty)
                    inchiKey = textDB[textLibId].Inchikey;
                if (textDB[textLibId].Smiles != null && textDB[textLibId].Smiles != string.Empty)
                    smiles = textDB[textLibId].Smiles;
                if (textDB[textLibId].Ontology != null && textDB[textLibId].Ontology != string.Empty)
                    ontology = textDB[textLibId].Ontology;
                msiLevelString = "annotated by user-defined text library";
            }
            else if (libraryID >= 0 && mspDB != null && mspDB.Count != 0) {
                var refRt = mspDB[libraryID].RetentionTime;
                if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
                refMzString = Math.Round(mspDB[libraryID].PrecursorMz, 5).ToString();

                formula = mspDB[libraryID].Formula;
                inchiKey = mspDB[libraryID].InchiKey;
                smiles = mspDB[libraryID].Smiles;
                ontology = mspDB[libraryID].Ontology;
                if (ontology == null || ontology == string.Empty) {
                    ontology = mspDB[libraryID].CompoundClass;
                }
                msiLevel = getMsiLevel(alignedSpot, refRt, param);
                msiLevelString = msiLevel.ToString();
                //msiLevelString = lsiLevel != string.Empty ? msiLevel.ToString() + "_" + lsiLevel.ToString() : msiLevel.ToString();
            }
            comment = alignedSpot.Comment != null && alignedSpot.Comment != string.Empty ? alignedSpot.Comment : string.Empty;
            if (isNormalized == true) {
                if (comment == string.Empty) {
                    comment = "Unit " + alignedSpot.IonAbundanceUnit.ToString() + " by alignment ID=" + alignedSpot.InternalStandardAlignmentID;
                }
                else {
                    comment += "; Unit " + alignedSpot.IonAbundanceUnit.ToString() + " by alignment ID=" + alignedSpot.InternalStandardAlignmentID;
                }
            }
            if (alignedSpot.IsRtMatch) rtMatched = "True";
            if (alignedSpot.IsMs1Match) mzMatched = "True";
            if (alignedSpot.IsMs2Match) ms2Matched = "True";

           

            // should be included
            //var headers = new List<string> {
            //    "Alignment ID", "Average Rt(min)", "Average Mz", "Metabolite name", "Adduct type", "Post curation result", "Fill %", "MS/MS assigned",
            //     "Reference RT", "Reference m/z", "Formula", "Ontology", "INCHIKEY", "SMILES", "MSI level", "RT matched", "m/z matched", "MS/MS matched", 
            // "Comment", "Mannually modified for quantification","Mannually modified for annotation",
            //     "Isotope tracking parent ID",  "Isotope tracking weight number", "Dot product", "Reverse dot product", "Fragment presence %", "S/N average",
            //     "Spectrum reference file name", "MS1 isotopic spectrum", "MS/MS spectrum"
            //};

            var metadata = new List<string>() {
                alignedSpot.AlignmentID.ToString(), Math.Round(alignedSpot.CentralRetentionTime, 3).ToString(), Math.Round(alignedSpot.CentralAccurateMass, 5).ToString(),
                metaboliteName, alignedSpot.AdductIonName, postCurationResult, Math.Round(alignedSpot.FillParcentage, 3).ToString(), alignedSpot.MsmsIncluded.ToString(),
                refRtString, refMzString, formula, ontology, inchiKey, smiles, msiLevelString, rtMatched, mzMatched, ms2Matched, comment,
                isManuallyModified.ToString(), isManuallyModifiedForAnnotation.ToString(),
                isotopeTrackingParentID, isotopeTrackingWeightNumber, totalScore, rtScore, dotProduct, revDotProd, precense, Math.Round(alignedSpot.SignalToNoiseAve, 2).ToString(),
                alignedSpot.AlignedPeakPropertyBeanCollection[alignedSpot.RepresentativeFileID].FileName
            };
            sw.Write(String.Join("\t", metadata) + "\t" + ms1IsotopeSpectrum + "\t");

            var spectrumString = GetSpectrumString(massSpectra);
            sw.Write(spectrumString + "\t");
        }

        public static void WriteLipidomicsDataMatrixMetaData(StreamWriter sw, AlignmentPropertyBean alignedSpot, 
            List<AlignmentPropertyBean> alignedSpots,
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> textDB,
            FileStream fs, List<long> seekpointList, AnalysisParametersBean param, bool isNormalized = false)
        {
            var metaboliteName = "Unknown";
            var formula = "null";
            var ontology = "null";
            var inchiKey = "null";
            var smiles = "null";
            var refRtString = "null";
            var refMzString = "null";
            var msiLevel = 999;
            var msiLevelString = "999";
            var lsiLevel = string.Empty;
            var comment = "";
            var isManuallyModified = alignedSpot.IsManuallyModified;
            var isManuallyModifiedForAnnotation = alignedSpot.IsManuallyModifiedForAnnotation;
            var libraryID = alignedSpot.LibraryID;
            var textLibId = alignedSpot.PostIdentificationLibraryID;
            var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, alignedSpot.AlignmentID);
            var ms1IsotopeSpectrum = getMs1IsotopicSpectrumString(ms2DecResult);
            var massSpectra = ms2DecResult.MassSpectra;
            var postCurationResult = AlignedSpotComment(alignedSpot, alignedSpots);
            var isotopeTrackingParentID =
               alignedSpot.IsotopeTrackingParentID >= 0
               ? alignedSpot.IsotopeTrackingParentID.ToString()
               : "null";

            var isotopeTrackingWeightNumber =
               alignedSpot.IsotopeTrackingWeightNumber >= 0
               ? alignedSpot.IsotopeTrackingWeightNumber.ToString()
               : "null";
            var totalScore = alignedSpot.TotalSimilairty > 0
                ? alignedSpot.TotalSimilairty > 1000
                ? "100"
                : Math.Round(alignedSpot.TotalSimilairty * 0.1, 1).ToString()
                : "null";
            var rtScore = alignedSpot.RetentionTimeSimilarity > 0 ? Math.Round(alignedSpot.RetentionTimeSimilarity * 0.1, 1).ToString() : "null";
            var dotProduct = alignedSpot.MassSpectraSimilarity > 0 ? Math.Round(alignedSpot.MassSpectraSimilarity * 0.1, 1).ToString() : "null";
            var revDotProd = alignedSpot.ReverseSimilarity > 0 ? Math.Round(alignedSpot.ReverseSimilarity * 0.1, 1).ToString() : "null";
            var precense = alignedSpot.FragmentPresencePercentage > 0
                ? alignedSpot.FragmentPresencePercentage > 1000
                ? "100"
                : Math.Round(alignedSpot.FragmentPresencePercentage * 0.1, 1).ToString()
                : "null";

            var rtMatched = "False";
            var mzMatched = "False";
            var ms2Matched = "False";

            if (alignedSpot.MetaboliteName != string.Empty) metaboliteName = alignedSpot.MetaboliteName;

            if (textLibId >= 0 && textDB != null && textDB.Count != 0)
            {
                var refRt = textDB[textLibId].RetentionTime;
                if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
                refMzString = Math.Round(textDB[textLibId].AccurateMass, 5).ToString();
                if (textDB[textLibId].Formula != null && textDB[textLibId].Formula.FormulaString != null && textDB[textLibId].Formula.FormulaString != string.Empty)
                    formula = textDB[textLibId].Formula.FormulaString;
                if (textDB[textLibId].Inchikey != null && textDB[textLibId].Inchikey != string.Empty)
                    inchiKey = textDB[textLibId].Inchikey;
                if (textDB[textLibId].Smiles != null && textDB[textLibId].Smiles != string.Empty)
                    smiles = textDB[textLibId].Smiles;
                if (textDB[textLibId].Ontology != null && textDB[textLibId].Ontology != string.Empty)
                    ontology = textDB[textLibId].Ontology;

                msiLevelString = "annotated by user-defined text library";
            }
            else if (libraryID >= 0 && mspDB != null && mspDB.Count != 0)
            {
                var refRt = mspDB[libraryID].RetentionTime;
                if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
                refMzString = Math.Round(mspDB[libraryID].PrecursorMz, 5).ToString();

                formula = mspDB[libraryID].Formula;
                inchiKey = mspDB[libraryID].InchiKey;
                smiles = mspDB[libraryID].Smiles;
                ontology = mspDB[libraryID].Ontology;
                if (ontology == null || ontology == string.Empty)
                {
                    ontology = mspDB[libraryID].CompoundClass;
                }
                msiLevelString = LipidomicsConverter.LipidomicsAnnotationLevel(metaboliteName, mspDB[libraryID], mspDB[libraryID].AdductIonBean.AdductIonName);
            }
            //if (metaboliteName.Contains("FAHFA 17:0/2:0")) {
            //    Console.WriteLine();
            //}

            if (metaboliteName.Contains("HexHexHex"))
                metaboliteName = metaboliteName.Replace("HexHexHex", "Hex3");
            else if (ontology.Contains("HexHex"))
                metaboliteName = metaboliteName.Replace("HexHex", "Hex2");

            if (ontology.Contains("HexHexHex"))
                ontology = "Hex3Cer";
            else if (ontology.Contains("HexHex"))
                ontology = "Hex2Cer";

            if (ontology == "PI_Cer" && (metaboliteName.Contains("+O") || metaboliteName.Contains("+1O")))
                ontology = "PI_Cer+O";
            else if (ontology == "PE_Cer" && metaboliteName.Contains("+O"))
                ontology = "PE_Cer+O";
            else if (ontology == "PE_Cer" && metaboliteName.Contains("t"))
                ontology = "PE_Cer+O";
            else if (ontology == "SHexCer" && metaboliteName.Contains("+O"))
                ontology = "SHexCer+O";
            else if (ontology == "SL" && metaboliteName.Contains("+O"))
                ontology = "SL+O";
            else if (ontology == "SM" && metaboliteName.Contains("t"))
                ontology = "SM+O";

            if (metaboliteName.Contains("-HS") && (ontology == "Cer_AS" || ontology == "Cer_BS"))
                ontology = "Cer_HS";
            else if (metaboliteName.Contains("-HDS") && (ontology == "Cer_ADS" || ontology == "Cer_BDS"))
                ontology = "Cer_HDS";

            comment = alignedSpot.Comment != null && alignedSpot.Comment != string.Empty ? alignedSpot.Comment : string.Empty;
            if (comment.ToLower().Contains("is (y") || comment.ToLower().Contains("is(y"))
                ontology = "IS (Yes); " + metaboliteName.Split(' ')[0];
            else if (comment.ToLower().Contains("is (n") || comment.ToLower().Contains("is(n"))
                ontology = "IS (NO); " + metaboliteName.Split(' ')[0];
            if (comment.Contains("BMP")) {
                ontology = "BMP";
            }
            if (comment.Contains("p/")) {
                ontology = "EtherPE(Plasmalogen)";
                if (alignedSpot.AdductIonName == "[M+H]+") {
                    metaboliteName = alignedSpot.Comment;
                }
            }

            if (comment.Contains("HexCer-EOS")) {
                metaboliteName = alignedSpot.Comment + "; [M+CH3COO]-"; 
            }

            var colonCount = metaboliteName.Where(c => c == ';').Count();
            if (colonCount >= 1) {
                metaboliteName = metaboliteName.Split(';')[colonCount - 1].Trim();
            }

            if (isNormalized == true)
            {
                if (comment == string.Empty) comment = "Normalized unit " + alignedSpot.IonAbundanceUnit.ToString();
                else comment += "; Normalized unit " + alignedSpot.IonAbundanceUnit.ToString();
            }

            if (alignedSpot.IsRtMatch) rtMatched = "True";
            if (alignedSpot.IsMs1Match) mzMatched = "True";
            if (alignedSpot.IsMs2Match) ms2Matched = "True";

            var metadata = new List<string>() {
                alignedSpot.AlignmentID.ToString(), Math.Round(alignedSpot.CentralRetentionTime, 3).ToString(), Math.Round(alignedSpot.CentralAccurateMass, 5).ToString(),
                metaboliteName, alignedSpot.AdductIonName, postCurationResult, Math.Round(alignedSpot.FillParcentage, 3).ToString(), alignedSpot.MsmsIncluded.ToString(),
                refRtString, refMzString, formula, ontology, inchiKey, smiles, msiLevelString, rtMatched, mzMatched, ms2Matched,
                comment, isManuallyModified.ToString(), isManuallyModifiedForAnnotation.ToString(),
                isotopeTrackingParentID, isotopeTrackingWeightNumber, totalScore, rtScore, dotProduct, revDotProd, precense, Math.Round(alignedSpot.SignalToNoiseAve, 2).ToString(),
                alignedSpot.AlignedPeakPropertyBeanCollection[alignedSpot.RepresentativeFileID].FileName
            };
            sw.Write(String.Join("\t", metadata) + "\t" + ms1IsotopeSpectrum + "\t");

            var spectrumString = GetSpectrumString(massSpectra);
            sw.Write(spectrumString + "\t");
        }

        public static void WriteDataMatrixMetaDataAtIonMobility(StreamWriter sw, AlignmentPropertyBean alignedSpot, AlignedDriftSpotPropertyBean driftSpot, 
            ObservableCollection<AlignmentPropertyBean> alignedSpots,
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> textDB, 
            FileStream fs, List<long> seekpointList, 
            AnalysisParametersBean param, bool isNormalized = false) {
            var id = -1;
            var metaboliteName = "Unknown";
            var formula = "null";
            var ontology = "null";
            var inchiKey = "null";
            var smiles = "null";
            var refRtString = "null";
            var refCcsString = "null";
            var refMzString = "null";
            var msiLevel = 999;
            var msiLevelString = "999";
            var lsiLevel = string.Empty;
            var ccs = -1.0;
            var dt = -1.0;
            var isManuallyModified = "false";
            var isManuallyModifiedForAnnotation = "false";
            var comment = "";
            var postCurationResult = "";
            var isotopeTrackingParentID = "null";
            var isotopeTrackingWeightNumber = "null";
            var ms1IsotopeSpectrum = "";
            var massSpectra = new List<double[]>();
            var adductName = string.Empty;
            var fillPercent = 0F;
            //var massSpecSimilarity = -1F;
            //var revSpecSimilarity = -1F;
            //var fragPresence = -1F;
            var sn = -1F;
            var repFilename = string.Empty;
            var totalScore = "null";
            var rtScore = "null";
            var ccsScore = "null";
            var dotProduct = "null";
            var revDotProd = "null";
            var precense = "null";
            var rtMatched = "False";
            var ccsMatched = "False";
            var mzMatched = "False";
            var ms2Matched = "False";

            var isMobility = driftSpot.CentralCcs > 0 ? true : false;
            if (isMobility) {
                id = driftSpot.MasterID;
                var libraryID = driftSpot.LibraryID;
                var textLibId = driftSpot.PostIdentificationLibraryID;
                var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, driftSpot.MasterID);
                ms1IsotopeSpectrum = getMs1IsotopicSpectrumString(ms2DecResult);
                massSpectra = ms2DecResult.MassSpectra;

                var sumDt = 0.0;
                var detectedCount = 0.0;
                foreach (var peak in driftSpot.AlignedPeakPropertyBeanCollection) {
                    if (peak.Variable > 200) {
                        sumDt += peak.DriftTime;
                        detectedCount++;
                    }
                }
                dt = detectedCount > 0 ? sumDt / detectedCount : driftSpot.CentralDriftTime;

                var adduct = AdductIonParcer.GetAdductIonBean(driftSpot.AdductIonName);
                adductName = adduct.AdductIonName;
                fillPercent = driftSpot.FillParcentage;

                totalScore = driftSpot.TotalSimilairty > 0
                ? driftSpot.TotalSimilairty > 1000
                ? "100"
                : Math.Round(driftSpot.TotalSimilairty * 0.1, 1).ToString()
                : "null";
                rtScore = driftSpot.RetentionTimeSimilarity > 0 ? Math.Round(driftSpot.RetentionTimeSimilarity * 0.1, 1).ToString() : "null";
                ccsScore = driftSpot.CcsSimilarity > 0 ? Math.Round(driftSpot.CcsSimilarity * 0.1, 1).ToString() : "null";
                dotProduct = driftSpot.MassSpectraSimilarity > 0 ? Math.Round(driftSpot.MassSpectraSimilarity * 0.1, 1).ToString() : "null";
                revDotProd = driftSpot.ReverseSimilarity > 0 ? Math.Round(driftSpot.ReverseSimilarity * 0.1, 1).ToString() : "null";
                precense = driftSpot.FragmentPresencePercentage > 0
                ? driftSpot.FragmentPresencePercentage > 1000
                ? "100"
                : Math.Round(driftSpot.FragmentPresencePercentage * 0.1, 1).ToString()
                : "null";

                //massSpecSimilarity = driftSpot.MassSpectraSimilarity;
                //revSpecSimilarity = driftSpot.ReverseSimilarity;
                //fragPresence = driftSpot.FragmentPresencePercentage;
                sn = driftSpot.SignalToNoiseAve;
                repFilename = driftSpot.AlignedPeakPropertyBeanCollection[driftSpot.RepresentativeFileID].FileName;
                var repfileid = driftSpot.RepresentativeFileID;
                var calinfo = param.FileidToCcsCalibrantData[repfileid];  
                //var exactmass = MolecularFormulaUtility.ConvertPrecursorMzToExactMass(adduct, alignedSpot.CentralAccurateMass);

                ccs = (float)IonMobilityUtility.MobilityToCrossSection(param.IonMobilityType, dt, Math.Abs(adduct.ChargeNumber), alignedSpot.CentralAccurateMass,
                    calinfo, param.IsAllCalibrantDataImported);

                //var postCurationResult = GetPostCurationResult(alignedSpot);
                postCurationResult = AlignedSpotComment(alignedSpot, alignedSpots);
                isotopeTrackingParentID =
                alignedSpot.IsotopeTrackingParentID >= 0
                ? alignedSpot.IsotopeTrackingParentID.ToString()
                : "null";

                isotopeTrackingWeightNumber =
                   alignedSpot.IsotopeTrackingWeightNumber >= 0
                   ? alignedSpot.IsotopeTrackingWeightNumber.ToString()
                   : "null";

                isManuallyModified = driftSpot.IsManuallyModified.ToString();
                isManuallyModifiedForAnnotation = driftSpot.IsManuallyModifiedForAnnotation.ToString();

                if (driftSpot.MetaboliteName != string.Empty) metaboliteName = driftSpot.MetaboliteName;
                if (textLibId >= 0 && textDB != null && textDB.Count != 0) {
                    var refRt = textDB[textLibId].RetentionTime;
                    if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
                    refMzString = Math.Round(textDB[textLibId].AccurateMass, 5).ToString();
                    if (textDB[textLibId].Formula != null && textDB[textLibId].Formula.FormulaString != null && textDB[textLibId].Formula.FormulaString != string.Empty)
                        formula = textDB[textLibId].Formula.FormulaString;
                    if (textDB[textLibId].Inchikey != null && textDB[textLibId].Inchikey != string.Empty)
                        inchiKey = textDB[textLibId].Inchikey;
                    if (textDB[textLibId].Smiles != null && textDB[textLibId].Smiles != string.Empty)
                        smiles = textDB[textLibId].Smiles;
                    if (textDB[textLibId].Ontology != null && textDB[textLibId].Ontology != string.Empty)
                        ontology = textDB[textLibId].Ontology;
                    msiLevelString = "annotated by user-defined text library";
                }
                else if (libraryID >= 0 && mspDB != null && mspDB.Count != 0) {
                    var refRt = mspDB[libraryID].RetentionTime;
                    if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
                    refMzString = Math.Round(mspDB[libraryID].PrecursorMz, 5).ToString();

                    var refCcs = mspDB[libraryID].CollisionCrossSection;
                    if (refCcs > 0) refCcsString = Math.Round(refCcs, 3).ToString();

                    formula = mspDB[libraryID].Formula;
                    inchiKey = mspDB[libraryID].InchiKey;
                    smiles = mspDB[libraryID].Smiles;
                    ontology = mspDB[libraryID].Ontology;
                    if (ontology == null || ontology == string.Empty) {
                        ontology = mspDB[libraryID].CompoundClass;
                    }
                    msiLevel = getMsiLevel(alignedSpot, refRt, driftSpot, refCcs, param);
                    msiLevelString = msiLevel.ToString();
                    //msiLevelString = lsiLevel != string.Empty ? msiLevel.ToString() + "_" + lsiLevel.ToString() : msiLevel.ToString();
                }
                comment = driftSpot.Comment != null && driftSpot.Comment != string.Empty ? driftSpot.Comment : string.Empty;
                if (isNormalized == true) {
                    if (comment == string.Empty) {
                        comment = "Unit " + driftSpot.IonAbundanceUnit.ToString() + " by alignment ID=" + driftSpot.InternalStandardAlignmentID;
                    }
                    else {
                        comment += "; Unit " + driftSpot.IonAbundanceUnit.ToString() + " by alignment ID=" + driftSpot.InternalStandardAlignmentID;
                    }
                }
                if (driftSpot.IsRtMatch) rtMatched = "True";
                if (driftSpot.IsCcsMatch) ccsMatched = "True";
                if (driftSpot.IsMs1Match) mzMatched = "True";
                if (driftSpot.IsMs2Match) ms2Matched = "True";
            }
            else {
                id = alignedSpot.MasterID;
                var libraryID = alignedSpot.LibraryID;
                var textLibId = alignedSpot.PostIdentificationLibraryID;
                var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, alignedSpot.MasterID);
                ms1IsotopeSpectrum = getMs1IsotopicSpectrumString(ms2DecResult);
                massSpectra = ms2DecResult.MassSpectra;


                //var postCurationResult = GetPostCurationResult(alignedSpot);
                postCurationResult = AlignedSpotComment(alignedSpot, alignedSpots);
                isotopeTrackingParentID =
                  alignedSpot.IsotopeTrackingParentID >= 0
                  ? alignedSpot.IsotopeTrackingParentID.ToString()
                  : "null";

                isotopeTrackingWeightNumber =
                   alignedSpot.IsotopeTrackingWeightNumber >= 0
                   ? alignedSpot.IsotopeTrackingWeightNumber.ToString()
                   : "null";

                isManuallyModified = alignedSpot.IsManuallyModified.ToString();
                isManuallyModifiedForAnnotation = alignedSpot.IsManuallyModifiedForAnnotation.ToString();

                if (alignedSpot.MetaboliteName != string.Empty) metaboliteName = alignedSpot.MetaboliteName;
                if (textLibId >= 0 && textDB != null && textDB.Count != 0) {
                    var refRt = textDB[textLibId].RetentionTime;
                    if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
                    refMzString = Math.Round(textDB[textLibId].AccurateMass, 5).ToString();
                    if (textDB[textLibId].Formula != null && textDB[textLibId].Formula.FormulaString != null && textDB[textLibId].Formula.FormulaString != string.Empty)
                        formula = textDB[textLibId].Formula.FormulaString;
                    if (textDB[textLibId].Inchikey != null && textDB[textLibId].Inchikey != string.Empty)
                        inchiKey = textDB[textLibId].Inchikey;
                    if (textDB[textLibId].Smiles != null && textDB[textLibId].Smiles != string.Empty)
                        smiles = textDB[textLibId].Smiles;
                    if (textDB[textLibId].Ontology != null && textDB[textLibId].Ontology != string.Empty)
                        ontology = textDB[textLibId].Ontology;
                    msiLevelString = "annotated by user-defined text library";
                }
                else if (libraryID >= 0 && mspDB != null && mspDB.Count != 0) {
                    var refRt = mspDB[libraryID].RetentionTime;
                    if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
                    refMzString = Math.Round(mspDB[libraryID].PrecursorMz, 5).ToString();
                    formula = mspDB[libraryID].Formula;
                    inchiKey = mspDB[libraryID].InchiKey;
                    smiles = mspDB[libraryID].Smiles;
                    //links = mspDB[libraryID].Links;
                    ontology = mspDB[libraryID].Ontology;
                    if (ontology == null || ontology == string.Empty) {
                        ontology = mspDB[libraryID].CompoundClass;
                    }
                    msiLevel = getMsiLevel(alignedSpot, refRt, param);
                    msiLevelString = msiLevel.ToString();
                    //msiLevelString = lsiLevel != string.Empty ? msiLevel.ToString() + "_" + lsiLevel.ToString() : msiLevel.ToString();
                }
                comment = alignedSpot.Comment != null && alignedSpot.Comment != string.Empty ? alignedSpot.Comment : string.Empty;
                if (isNormalized == true) {
                    if (comment == string.Empty) {
                        comment = "Unit " + alignedSpot.IonAbundanceUnit.ToString() + " by alignment ID=" + alignedSpot.InternalStandardAlignmentID;
                    }
                    else {
                        comment += "; Unit " + alignedSpot.IonAbundanceUnit.ToString() + " by alignment ID=" + alignedSpot.InternalStandardAlignmentID;
                    }
                }

                adductName = alignedSpot.AdductIonName;
                fillPercent = alignedSpot.FillParcentage;
                totalScore = alignedSpot.TotalSimilairty > 0
                ? alignedSpot.TotalSimilairty > 1000
                ? "100"
                : Math.Round(alignedSpot.TotalSimilairty * 0.1, 1).ToString()
                : "null";
                rtScore = alignedSpot.RetentionTimeSimilarity > 0 ? Math.Round(alignedSpot.RetentionTimeSimilarity * 0.1, 1).ToString() : "null";
                ccsScore = alignedSpot.CcsSimilarity > 0 ? Math.Round(alignedSpot.CcsSimilarity * 0.1, 1).ToString() : "null";
                dotProduct = alignedSpot.MassSpectraSimilarity > 0 ? Math.Round(alignedSpot.MassSpectraSimilarity * 0.1, 1).ToString() : "null";
                revDotProd = alignedSpot.ReverseSimilarity > 0 ? Math.Round(alignedSpot.ReverseSimilarity * 0.1, 1).ToString() : "null";
                precense = alignedSpot.FragmentPresencePercentage > 0
                ? alignedSpot.FragmentPresencePercentage > 1000
                ? "100"
                : Math.Round(alignedSpot.FragmentPresencePercentage * 0.1, 1).ToString()
                : "null";
                //massSpecSimilarity = alignedSpot.MassSpectraSimilarity;
                //revSpecSimilarity = alignedSpot.ReverseSimilarity;
                //fragPresence = alignedSpot.FragmentPresencePercentage;
                sn = alignedSpot.SignalToNoiseAve;
                repFilename = alignedSpot.AlignedPeakPropertyBeanCollection[alignedSpot.RepresentativeFileID].FileName;

                if (alignedSpot.IsRtMatch) rtMatched = "True";
                if (alignedSpot.IsCcsMatch) ccsMatched = "True";
                if (alignedSpot.IsMs1Match) mzMatched = "True";
                if (alignedSpot.IsMs2Match) ms2Matched = "True";
            }


            //var headers = new List<string>() {
            //    "Alignment ID", "Average Rt(min)", "Average Mz","Average mobility", "Average CCS",
            //    "Metabolite name", "Adduct type", "Post curation result", "Fill %", "MS/MS assigned",
            //    "Reference RT", "Reference CCS", "Reference m/z", "Formula", "Ontology", "INCHIKEY", "SMILES", 
            //    "MSI level", "RT matched", "CCS matched", "m/z matched", "MS/MS matched", "Comment", "Mannually modified for quant", "Mannually modified for annotation",
            //    "Isotope tracking parent ID",  "Isotope tracking weight number", "Dot product", "Reverse dot product", "Fragment presence %", "S/N average",
            //    "Spectrum reference file name", "MS1 isotopic spectrum", "MS/MS spectrum"
            //};

            var metadata = new List<string>() {
                id.ToString(), Math.Round(alignedSpot.CentralRetentionTime, 3).ToString(), Math.Round(alignedSpot.CentralAccurateMass, 5).ToString(),
                Math.Round(dt, 4).ToString() ,Math.Round(ccs, 4).ToString(),
                metaboliteName, alignedSpot.AdductIonName, postCurationResult, Math.Round(alignedSpot.FillParcentage, 3).ToString(), alignedSpot.MsmsIncluded.ToString(),
                refRtString, refCcsString, refMzString, formula, ontology, inchiKey, smiles, msiLevelString, rtMatched, ccsMatched, mzMatched, ms2Matched,
                comment, isManuallyModified.ToString(), isManuallyModifiedForAnnotation.ToString(),
                isotopeTrackingParentID, isotopeTrackingWeightNumber, totalScore, rtScore, ccsScore,
                dotProduct, revDotProd, precense, Math.Round(alignedSpot.SignalToNoiseAve, 2).ToString(),
                alignedSpot.AlignedPeakPropertyBeanCollection[alignedSpot.RepresentativeFileID].FileName
            };
            sw.Write(String.Join("\t", metadata) + "\t" + ms1IsotopeSpectrum + "\t");

            //sw.Write(id + "\t" +

            var spectrumString = GetSpectrumString(massSpectra);
            sw.Write(spectrumString + "\t");
        }

        public static void WriteLipidomicsDataMatrixMetaDataAtIonMobility(StreamWriter sw, AlignmentPropertyBean alignedSpot, AlignedDriftSpotPropertyBean driftSpot,
            List<AlignmentPropertyBean> alignedSpots,
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> textDB,
            FileStream fs, List<long> seekpointList,
            AnalysisParametersBean param, bool isNormalized = false) {
            var id = -1;
            var metaboliteName = "Unknown";
            var formula = "null";
            var ontology = "null";
            var inchiKey = "null";
            var smiles = "null";
            var refRtString = "null";
            var refCcsString = "null";
            var refMzString = "null";
            var msiLevel = 999;
            var msiLevelString = "999";
            var lsiLevel = string.Empty;
            var ccs = -1.0;
            var dt = -1.0;
            var isManuallyModified = "false";
            var isManuallyModifiedForAnnotation = "false";
            var comment = "";
            var postCurationResult = "";
            var isotopeTrackingParentID = "null";
            var isotopeTrackingWeightNumber = "null";
            var ms1IsotopeSpectrum = "";
            var massSpectra = new List<double[]>();
            var adductName = string.Empty;
            var fillPercent = 0F;
            var sn = -1F;
            var repFilename = string.Empty;
            var totalScore = "null";
            var rtScore = "null";
            var ccsScore = "null";
            var dotProduct = "null";
            var revDotProd = "null";
            var precense = "null";
            var rtMatched = "False";
            var ccsMatched = "False";
            var mzMatched = "False";
            var ms2Matched = "False";

            var isMobility = driftSpot.CentralCcs > 0 ? true : false;
            if (isMobility) {
                id = driftSpot.MasterID;
                var libraryID = driftSpot.LibraryID;
                var textLibId = driftSpot.PostIdentificationLibraryID;
                var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, driftSpot.MasterID);
                ms1IsotopeSpectrum = getMs1IsotopicSpectrumString(ms2DecResult);
                massSpectra = ms2DecResult.MassSpectra;

                var sumDt = 0.0;
                var detectedCount = 0.0;
                foreach (var peak in driftSpot.AlignedPeakPropertyBeanCollection) {
                    if (peak.Variable > 200) {
                        sumDt += peak.DriftTime;
                        detectedCount++;
                    }
                }
                dt = detectedCount > 0 ? sumDt / detectedCount : driftSpot.CentralDriftTime;

                var adduct = AdductIonParcer.GetAdductIonBean(driftSpot.AdductIonName);
                adductName = adduct.AdductIonName;
                fillPercent = driftSpot.FillParcentage;

                totalScore = driftSpot.TotalSimilairty > 0
                ? driftSpot.TotalSimilairty > 1000
                ? "100"
                : Math.Round(driftSpot.TotalSimilairty * 0.1, 1).ToString()
                : "null";
                rtScore = driftSpot.RetentionTimeSimilarity > 0 ? Math.Round(driftSpot.RetentionTimeSimilarity * 0.1, 1).ToString() : "null";
                ccsScore = driftSpot.CcsSimilarity > 0 ? Math.Round(driftSpot.CcsSimilarity * 0.1, 1).ToString() : "null";
                revDotProd = driftSpot.ReverseSimilarity > 0 ? Math.Round(driftSpot.ReverseSimilarity * 0.1, 1).ToString() : "null";
                precense = driftSpot.FragmentPresencePercentage > 0
                ? driftSpot.FragmentPresencePercentage > 1000
                ? "100"
                : Math.Round(driftSpot.FragmentPresencePercentage * 0.1, 1).ToString()
                : "null";

                sn = driftSpot.SignalToNoiseAve;
                repFilename = driftSpot.AlignedPeakPropertyBeanCollection[driftSpot.RepresentativeFileID].FileName;
                var repfileid = driftSpot.RepresentativeFileID;
                var calinfo = param.FileidToCcsCalibrantData[repfileid];
                ccs = (float)IonMobilityUtility.MobilityToCrossSection(param.IonMobilityType, dt, Math.Abs(adduct.ChargeNumber), alignedSpot.CentralAccurateMass, calinfo, param.IsAllCalibrantDataImported);

                //var postCurationResult = GetPostCurationResult(alignedSpot);
                postCurationResult = AlignedSpotComment(alignedSpot, alignedSpots);
                isotopeTrackingParentID =
                alignedSpot.IsotopeTrackingParentID >= 0
                ? alignedSpot.IsotopeTrackingParentID.ToString()
                : "null";

                isotopeTrackingWeightNumber =
                   alignedSpot.IsotopeTrackingWeightNumber >= 0
                   ? alignedSpot.IsotopeTrackingWeightNumber.ToString()
                   : "null";

                isManuallyModified = driftSpot.IsManuallyModified.ToString();
                isManuallyModifiedForAnnotation = driftSpot.IsManuallyModifiedForAnnotation.ToString();

                if (driftSpot.MetaboliteName != string.Empty) metaboliteName = driftSpot.MetaboliteName;
                if (textLibId >= 0 && textDB != null && textDB.Count != 0) {
                    var refRt = textDB[textLibId].RetentionTime;
                    if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
                    refMzString = Math.Round(textDB[textLibId].AccurateMass, 5).ToString();
                    if (textDB[textLibId].Formula != null && textDB[textLibId].Formula.FormulaString != null && textDB[textLibId].Formula.FormulaString != string.Empty)
                        formula = textDB[textLibId].Formula.FormulaString;
                    if (textDB[textLibId].Inchikey != null && textDB[textLibId].Inchikey != string.Empty)
                        inchiKey = textDB[textLibId].Inchikey;
                    if (textDB[textLibId].Smiles != null && textDB[textLibId].Smiles != string.Empty)
                        smiles = textDB[textLibId].Smiles;
                    if (textDB[textLibId].Ontology != null && textDB[textLibId].Ontology != string.Empty)
                        ontology = textDB[textLibId].Ontology;
                    msiLevelString = "annotated by user-defined text library";
                }
                else if (libraryID >= 0 && mspDB != null && mspDB.Count != 0) {
                    var refRt = mspDB[libraryID].RetentionTime;
                    if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
                    refMzString = Math.Round(mspDB[libraryID].PrecursorMz, 5).ToString();

                    var refCcs = mspDB[libraryID].CollisionCrossSection;
                    if (refCcs > 0) refCcsString = Math.Round(refCcs, 3).ToString();

                    formula = mspDB[libraryID].Formula;
                    inchiKey = mspDB[libraryID].InchiKey;
                    smiles = mspDB[libraryID].Smiles;
                    ontology = mspDB[libraryID].Ontology;
                    if (ontology == null || ontology == string.Empty) {
                        ontology = mspDB[libraryID].CompoundClass;
                    }
                    msiLevelString = LipidomicsConverter.LipidomicsAnnotationLevel(metaboliteName, mspDB[libraryID], mspDB[libraryID].AdductIonBean.AdductIonName);
                }
                comment = driftSpot.Comment != null && driftSpot.Comment != string.Empty ? driftSpot.Comment : string.Empty;
                if (isNormalized == true) {
                    if (comment == string.Empty) comment = "Normalized unit " + driftSpot.IonAbundanceUnit.ToString();
                    else comment += "; Normalized unit " + driftSpot.IonAbundanceUnit.ToString();
                }

                if (driftSpot.IsRtMatch) rtMatched = "True";
                if (driftSpot.IsCcsMatch) ccsMatched = "True";
                if (driftSpot.IsMs1Match) mzMatched = "True";
                if (driftSpot.IsMs2Match) ms2Matched = "True";
            }
            else {
                id = alignedSpot.MasterID;
                var libraryID = alignedSpot.LibraryID;
                var textLibId = alignedSpot.PostIdentificationLibraryID;
                var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, alignedSpot.MasterID);
                ms1IsotopeSpectrum = getMs1IsotopicSpectrumString(ms2DecResult);
                massSpectra = ms2DecResult.MassSpectra;


                //var postCurationResult = GetPostCurationResult(alignedSpot);
                postCurationResult = AlignedSpotComment(alignedSpot, alignedSpots);
                isotopeTrackingParentID =
                  alignedSpot.IsotopeTrackingParentID >= 0
                  ? alignedSpot.IsotopeTrackingParentID.ToString()
                  : "null";

                isotopeTrackingWeightNumber =
                   alignedSpot.IsotopeTrackingWeightNumber >= 0
                   ? alignedSpot.IsotopeTrackingWeightNumber.ToString()
                   : "null";

                isManuallyModified = alignedSpot.IsManuallyModified.ToString();
                isManuallyModifiedForAnnotation = alignedSpot.IsManuallyModifiedForAnnotation.ToString();

                if (alignedSpot.MetaboliteName != string.Empty) metaboliteName = alignedSpot.MetaboliteName;
                if (textLibId >= 0 && textDB != null && textDB.Count != 0) {
                    var refRt = textDB[textLibId].RetentionTime;
                    if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
                    refMzString = Math.Round(textDB[textLibId].AccurateMass, 5).ToString();
                    if (textDB[textLibId].Formula != null && textDB[textLibId].Formula.FormulaString != null && textDB[textLibId].Formula.FormulaString != string.Empty)
                        formula = textDB[textLibId].Formula.FormulaString;
                    if (textDB[textLibId].Inchikey != null && textDB[textLibId].Inchikey != string.Empty)
                        inchiKey = textDB[textLibId].Inchikey;
                    if (textDB[textLibId].Smiles != null && textDB[textLibId].Smiles != string.Empty)
                        smiles = textDB[textLibId].Smiles;
                    if (textDB[textLibId].Ontology != null && textDB[textLibId].Ontology != string.Empty)
                        ontology = textDB[textLibId].Ontology;
                    msiLevelString = "annotated by user-defined text library";
                }
                else if (libraryID >= 0 && mspDB != null && mspDB.Count != 0) {
                    var refRt = mspDB[libraryID].RetentionTime;
                    if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
                    refMzString = Math.Round(mspDB[libraryID].PrecursorMz, 5).ToString();
                    formula = mspDB[libraryID].Formula;
                    inchiKey = mspDB[libraryID].InchiKey;
                    smiles = mspDB[libraryID].Smiles;
                    //links = mspDB[libraryID].Links;
                    ontology = mspDB[libraryID].Ontology;
                    if (ontology == null || ontology == string.Empty) {
                        ontology = mspDB[libraryID].CompoundClass;
                    }
                    msiLevelString = LipidomicsConverter.LipidomicsAnnotationLevel(metaboliteName, mspDB[libraryID], mspDB[libraryID].AdductIonBean.AdductIonName);
                }
                comment = alignedSpot.Comment != null && alignedSpot.Comment != string.Empty ? alignedSpot.Comment : string.Empty;
                if (isNormalized == true) {
                    if (comment == string.Empty) comment = "Normalized unit " + alignedSpot.IonAbundanceUnit.ToString();
                    else comment += "; Normalized unit " + alignedSpot.IonAbundanceUnit.ToString();
                }

                adductName = alignedSpot.AdductIonName;
                fillPercent = alignedSpot.FillParcentage;
                totalScore = alignedSpot.TotalSimilairty > 0
                ? alignedSpot.TotalSimilairty > 1000
                ? "100"
                : Math.Round(alignedSpot.TotalSimilairty * 0.1, 1).ToString()
                : "null";
                rtScore = alignedSpot.RetentionTimeSimilarity > 0 ? Math.Round(alignedSpot.RetentionTimeSimilarity * 0.1, 1).ToString() : "null";
                ccsScore = alignedSpot.CcsSimilarity > 0 ? Math.Round(alignedSpot.CcsSimilarity * 0.1, 1).ToString() : "null";
                dotProduct = alignedSpot.MassSpectraSimilarity > 0 ? Math.Round(alignedSpot.MassSpectraSimilarity * 0.1, 1).ToString() : "null";
                revDotProd = alignedSpot.ReverseSimilarity > 0 ? Math.Round(alignedSpot.ReverseSimilarity * 0.1, 1).ToString() : "null";
                precense = alignedSpot.FragmentPresencePercentage > 0
                ? alignedSpot.FragmentPresencePercentage > 1000
                ? "100"
                : Math.Round(alignedSpot.FragmentPresencePercentage * 0.1, 1).ToString()
                : "null";
                sn = alignedSpot.SignalToNoiseAve;
                repFilename = alignedSpot.AlignedPeakPropertyBeanCollection[alignedSpot.RepresentativeFileID].FileName;

                if (alignedSpot.IsRtMatch) rtMatched = "True";
                if (alignedSpot.IsCcsMatch) ccsMatched = "True";
                if (alignedSpot.IsMs1Match) mzMatched = "True";
                if (alignedSpot.IsMs2Match) ms2Matched = "True";
            }

            if (metaboliteName.Contains("HexHexHex"))
                metaboliteName = metaboliteName.Replace("HexHexHex", "Hex3");
            else if (ontology.Contains("HexHex"))
                metaboliteName = metaboliteName.Replace("HexHex", "Hex2");

            if (ontology.Contains("HexHexHex"))
                ontology = "Hex3Cer";
            else if (ontology.Contains("HexHex"))
                ontology = "Hex2Cer";

            if (metaboliteName.Contains("-HS") && (ontology == "Cer_AS" || ontology == "Cer_BS"))
                ontology = "Cer_HS";
            else if (metaboliteName.Contains("-HDS") && (ontology == "Cer_ADS" || ontology == "Cer_BDS"))
                ontology = "Cer_HDS";

            if (ontology == "PI_Cer" && metaboliteName.Contains("+O"))
                ontology = "PI_Cer+O";
            else if (ontology == "PE_Cer" && metaboliteName.Contains("+O"))
                ontology = "PE_Cer+O";
            else if (ontology == "PE_Cer" && metaboliteName.Contains("t"))
                ontology = "PE_Cer+O";
            else if (ontology == "SHexCer" && metaboliteName.Contains("+O"))
                ontology = "SHexCer+O";
            else if (ontology == "SL" && metaboliteName.Contains("+O"))
                ontology = "SL+O";
            else if (ontology == "SM" && metaboliteName.Contains("t"))
                ontology = "SM+O";

            if (comment.ToLower().Contains("is (y") || comment.ToLower().Contains("is(y"))
                ontology = "IS (Yes); " + metaboliteName.Split(' ')[0];
            else if (comment.ToLower().Contains("is (n") || comment.ToLower().Contains("is(n"))
                ontology = "IS (NO); " + metaboliteName.Split(' ')[0];
            if (comment.Contains("BMP")) {
                ontology = "BMP";
            }
            if (comment.Contains("p/")) {
                ontology = "EtherPE(Plasmalogen)";
                if (alignedSpot.AdductIonName == "[M+H]+") {
                    metaboliteName = alignedSpot.Comment;
                }
            }

            var colonCount = metaboliteName.Where(c => c == ';').Count();
            if (colonCount >= 1) {
                metaboliteName = metaboliteName.Split(';')[colonCount - 1].Trim();
            }

            var metadata = new List<string>() {
                id.ToString(), Math.Round(alignedSpot.CentralRetentionTime, 3).ToString(), Math.Round(alignedSpot.CentralAccurateMass, 5).ToString(),
                Math.Round(dt, 4).ToString() ,Math.Round(ccs, 4).ToString(),
                metaboliteName, alignedSpot.AdductIonName, postCurationResult, Math.Round(alignedSpot.FillParcentage, 3).ToString(), alignedSpot.MsmsIncluded.ToString(),
                refRtString, refCcsString, refMzString, formula, ontology, inchiKey, smiles, msiLevelString, rtMatched, ccsMatched, mzMatched, ms2Matched,
                comment, isManuallyModified.ToString(), isManuallyModifiedForAnnotation.ToString(),
                isotopeTrackingParentID, isotopeTrackingWeightNumber, totalScore, rtScore, ccsScore,
                dotProduct, revDotProd, precense, Math.Round(alignedSpot.SignalToNoiseAve, 2).ToString(),
                alignedSpot.AlignedPeakPropertyBeanCollection[alignedSpot.RepresentativeFileID].FileName
            };
            sw.Write(String.Join("\t", metadata) + "\t" + ms1IsotopeSpectrum + "\t");

            var spectrumString = GetSpectrumString(massSpectra);
            sw.Write(spectrumString + "\t");
        }

        public static void WriteGnpsDataMatrixMetaDataAtIonMobility(StreamWriter sw, AlignmentPropertyBean alignedSpot, AlignedDriftSpotPropertyBean driftSpot,
            ObservableCollection<AlignmentPropertyBean> alignedSpots,
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> textDB,
            FileStream fs, List<long> seekpointList,
            AnalysisParametersBean param, int counter, bool isNormalized = false) {
            var id = -1;
            var metaboliteName = "Unknown";
            var formula = "null";
            var ontology = "null";
            var inchiKey = "null";
            var smiles = "null";
            var refRtString = "null";
            var refCcsString = "null";
            var refMzString = "null";
            var msiLevel = 999;
            var msiLevelString = "999";
            var lsiLevel = string.Empty;
            var ccs = -1.0;
            var dt = -1.0;
            var isManuallyModified = "false";
            //var comment = "Original ID: " + driftSpot.MasterID;
            var comment = string.Empty;
            var postCurationResult = "";
            var isotopeTrackingParentID = "null";
            var isotopeTrackingWeightNumber = "null";
            var ms1IsotopeSpectrum = "";
            var massSpectra = new List<double[]>();
            var adductName = string.Empty;
            var fillPercent = 0F;
            //var massSpecSimilarity = -1F;
            //var revSpecSimilarity = -1F;
            //var fragPresence = -1F;
            var sn = -1F;
            var repFilename = string.Empty;
            var totalScore = "null";
            var rtScore = "null";
            var ccsScore = "null";
            var dotProduct = "null";
            var revDotProd = "null";
            var precense = "null";

            var isMobility = driftSpot.CentralCcs > 0 ? true : false;
            if (isMobility) {
                //comment = "Original ID: " + driftSpot.MasterID;
                id = driftSpot.MasterID;
                var libraryID = driftSpot.LibraryID;
                var textLibId = driftSpot.PostIdentificationLibraryID;
                var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, driftSpot.MasterID);
                ms1IsotopeSpectrum = getMs1IsotopicSpectrumString(ms2DecResult);
                massSpectra = ms2DecResult.MassSpectra;

                var sumDt = 0.0;
                var detectedCount = 0.0;
                foreach (var peak in driftSpot.AlignedPeakPropertyBeanCollection) {
                    if (peak.Variable > 200) {
                        sumDt += peak.DriftTime;
                        detectedCount++;
                    }
                }
                dt = detectedCount > 0 ? sumDt / detectedCount : driftSpot.CentralDriftTime;

                var adduct = AdductIonParcer.GetAdductIonBean(driftSpot.AdductIonName);
                adductName = adduct.AdductIonName;
                fillPercent = driftSpot.FillParcentage;

                totalScore = driftSpot.TotalSimilairty > 0
                ? driftSpot.TotalSimilairty > 1000
                ? "100"
                : Math.Round(driftSpot.TotalSimilairty * 0.1, 1).ToString()
                : "null";
                rtScore = driftSpot.RetentionTimeSimilarity > 0 ? Math.Round(driftSpot.RetentionTimeSimilarity * 0.1, 1).ToString() : "null";
                ccsScore = driftSpot.CcsSimilarity > 0 ? Math.Round(driftSpot.CcsSimilarity * 0.1, 1).ToString() : "null";
                revDotProd = driftSpot.ReverseSimilarity > 0 ? Math.Round(driftSpot.ReverseSimilarity * 0.1, 1).ToString() : "null";
                precense = driftSpot.FragmentPresencePercentage > 0
                ? driftSpot.FragmentPresencePercentage > 1000
                ? "100"
                : Math.Round(driftSpot.FragmentPresencePercentage * 0.1, 1).ToString()
                : "null";

                //massSpecSimilarity = driftSpot.MassSpectraSimilarity;
                //revSpecSimilarity = driftSpot.ReverseSimilarity;
                //fragPresence = driftSpot.FragmentPresencePercentage;
                sn = driftSpot.SignalToNoiseAve;
                repFilename = driftSpot.AlignedPeakPropertyBeanCollection[driftSpot.RepresentativeFileID].FileName;

                //var exactmass = MolecularFormulaUtility.ConvertPrecursorMzToExactMass(adduct, alignedSpot.CentralAccurateMass);
                var fileid = driftSpot.RepresentativeFileID;
                var calinfo = param.FileidToCcsCalibrantData[fileid];
                ccs = (float)IonMobilityUtility.MobilityToCrossSection(param.IonMobilityType, dt, Math.Abs(adduct.ChargeNumber), alignedSpot.CentralAccurateMass, calinfo, param.IsAllCalibrantDataImported);

                //var postCurationResult = GetPostCurationResult(alignedSpot);
                postCurationResult = AlignedSpotComment(alignedSpot, alignedSpots);
                isotopeTrackingParentID =
                alignedSpot.IsotopeTrackingParentID >= 0
                ? alignedSpot.IsotopeTrackingParentID.ToString()
                : "null";

                isotopeTrackingWeightNumber =
                   alignedSpot.IsotopeTrackingWeightNumber >= 0
                   ? alignedSpot.IsotopeTrackingWeightNumber.ToString()
                   : "null";

                isManuallyModified = driftSpot.IsManuallyModified.ToString();

                if (driftSpot.MetaboliteName != string.Empty) metaboliteName = driftSpot.MetaboliteName;
                if (textLibId >= 0 && textDB != null && textDB.Count != 0) {
                    var refRt = textDB[textLibId].RetentionTime;
                    if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
                    refMzString = Math.Round(textDB[textLibId].AccurateMass, 5).ToString();
                    if (textDB[textLibId].Formula != null && textDB[textLibId].Formula.FormulaString != null && textDB[textLibId].Formula.FormulaString != string.Empty)
                        formula = textDB[textLibId].Formula.FormulaString;
                    if (textDB[textLibId].Inchikey != null && textDB[textLibId].Inchikey != string.Empty)
                        inchiKey = textDB[textLibId].Inchikey;
                    if (textDB[textLibId].Smiles != null && textDB[textLibId].Smiles != string.Empty)
                        smiles = textDB[textLibId].Smiles;
                    if (textDB[textLibId].Ontology != null && textDB[textLibId].Ontology != string.Empty)
                        ontology = textDB[textLibId].Ontology;
                    msiLevelString = "annotated by user-defined text library";
                }
                else if (libraryID >= 0 && mspDB != null && mspDB.Count != 0) {
                    var refRt = mspDB[libraryID].RetentionTime;
                    if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
                    refMzString = Math.Round(mspDB[libraryID].PrecursorMz, 5).ToString();

                    var refCcs = mspDB[libraryID].CollisionCrossSection;
                    if (refCcs > 0) refCcsString = Math.Round(refCcs, 3).ToString();

                    formula = mspDB[libraryID].Formula;
                    inchiKey = mspDB[libraryID].InchiKey;
                    smiles = mspDB[libraryID].Smiles;
                    ontology = mspDB[libraryID].Ontology;
                    if (ontology == null || ontology == string.Empty) {
                        ontology = mspDB[libraryID].CompoundClass;
                    }
                    msiLevel = getMsiLevel(alignedSpot, refRt, driftSpot, refCcs, param);
                    msiLevelString = msiLevel.ToString();
                    //msiLevelString = lsiLevel != string.Empty ? msiLevel.ToString() + "_" + lsiLevel.ToString() : msiLevel.ToString();
                }
                comment = driftSpot.Comment != null && driftSpot.Comment != string.Empty ? driftSpot.Comment : string.Empty;
                if (isNormalized == true) {
                    if (comment == string.Empty) comment = "Normalized unit " + driftSpot.IonAbundanceUnit.ToString();
                    else comment += "; Normalized unit " + driftSpot.IonAbundanceUnit.ToString();
                }
            }
            else {
                //comment = "Original ID: " + alignedSpot.MasterID;
                id = alignedSpot.MasterID;
                var libraryID = alignedSpot.LibraryID;
                var textLibId = alignedSpot.PostIdentificationLibraryID;
                var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, alignedSpot.MasterID);
                ms1IsotopeSpectrum = getMs1IsotopicSpectrumString(ms2DecResult);
                massSpectra = ms2DecResult.MassSpectra;


                //var postCurationResult = GetPostCurationResult(alignedSpot);
                postCurationResult = AlignedSpotComment(alignedSpot, alignedSpots);
                isotopeTrackingParentID =
                  alignedSpot.IsotopeTrackingParentID >= 0
                  ? alignedSpot.IsotopeTrackingParentID.ToString()
                  : "null";

                isotopeTrackingWeightNumber =
                   alignedSpot.IsotopeTrackingWeightNumber >= 0
                   ? alignedSpot.IsotopeTrackingWeightNumber.ToString()
                   : "null";

                isManuallyModified = alignedSpot.IsManuallyModified.ToString();


                if (alignedSpot.MetaboliteName != string.Empty) metaboliteName = alignedSpot.MetaboliteName;
                if (textLibId >= 0 && textDB != null && textDB.Count != 0) {
                    var refRt = textDB[textLibId].RetentionTime;
                    if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
                    refMzString = Math.Round(textDB[textLibId].AccurateMass, 5).ToString();
                    if (textDB[textLibId].Formula != null && textDB[textLibId].Formula.FormulaString != null && textDB[textLibId].Formula.FormulaString != string.Empty)
                        formula = textDB[textLibId].Formula.FormulaString;
                    if (textDB[textLibId].Inchikey != null && textDB[textLibId].Inchikey != string.Empty)
                        inchiKey = textDB[textLibId].Inchikey;
                    if (textDB[textLibId].Smiles != null && textDB[textLibId].Smiles != string.Empty)
                        smiles = textDB[textLibId].Smiles;
                    if (textDB[textLibId].Ontology != null && textDB[textLibId].Ontology != string.Empty)
                        ontology = textDB[textLibId].Ontology;
                    msiLevelString = "annotated by user-defined text library";
                }
                else if (libraryID >= 0 && mspDB != null && mspDB.Count != 0) {
                    var refRt = mspDB[libraryID].RetentionTime;
                    if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
                    refMzString = Math.Round(mspDB[libraryID].PrecursorMz, 5).ToString();
                    formula = mspDB[libraryID].Formula;
                    inchiKey = mspDB[libraryID].InchiKey;
                    smiles = mspDB[libraryID].Smiles;
                    //links = mspDB[libraryID].Links;
                    ontology = mspDB[libraryID].Ontology;
                    if (ontology == null || ontology == string.Empty) {
                        ontology = mspDB[libraryID].CompoundClass;
                    }
                    msiLevel = getMsiLevel(alignedSpot, refRt, param);
                    msiLevelString = msiLevel.ToString();
                    //msiLevelString = lsiLevel != string.Empty ? msiLevel.ToString() + "_" + lsiLevel.ToString() : msiLevel.ToString();
                }
                comment = alignedSpot.Comment != null && alignedSpot.Comment != string.Empty ? alignedSpot.Comment : string.Empty;
                if (isNormalized == true) {
                    if (comment == string.Empty) comment = "Normalized unit " + alignedSpot.IonAbundanceUnit.ToString();
                    else comment += "; Normalized unit " + alignedSpot.IonAbundanceUnit.ToString();
                }

                adductName = alignedSpot.AdductIonName;
                fillPercent = alignedSpot.FillParcentage;
                totalScore = alignedSpot.TotalSimilairty > 0
                ? alignedSpot.TotalSimilairty > 1000
                ? "100"
                : Math.Round(alignedSpot.TotalSimilairty * 0.1, 1).ToString()
                : "null";
                rtScore = alignedSpot.RetentionTimeSimilarity > 0 ? Math.Round(alignedSpot.RetentionTimeSimilarity * 0.1, 1).ToString() : "null";
                ccsScore = alignedSpot.CcsSimilarity > 0 ? Math.Round(alignedSpot.CcsSimilarity * 0.1, 1).ToString() : "null";
                dotProduct = alignedSpot.MassSpectraSimilarity > 0 ? Math.Round(alignedSpot.MassSpectraSimilarity * 0.1, 1).ToString() : "null";
                revDotProd = alignedSpot.ReverseSimilarity > 0 ? Math.Round(alignedSpot.ReverseSimilarity * 0.1, 1).ToString() : "null";
                precense = alignedSpot.FragmentPresencePercentage > 0
                ? alignedSpot.FragmentPresencePercentage > 1000
                ? "100"
                : Math.Round(alignedSpot.FragmentPresencePercentage * 0.1, 1).ToString()
                : "null";
                //massSpecSimilarity = alignedSpot.MassSpectraSimilarity;
                //revSpecSimilarity = alignedSpot.ReverseSimilarity;
                //fragPresence = alignedSpot.FragmentPresencePercentage;
                sn = alignedSpot.SignalToNoiseAve;
                repFilename = alignedSpot.AlignedPeakPropertyBeanCollection[alignedSpot.RepresentativeFileID].FileName;
            }


            //var headers = new List<string>() {
            //    "Alignment ID", "Average Rt(min)", "Average Mz","Average mobility", "Average CCS",
            //    "Metabolite name", "Adduct type", "Post curation result", "Fill %", "MS/MS assigned",
            //    "Reference RT", "Reference CCS", "Reference m/z", "Formula", "Ontology", "INCHIKEY", "SMILES", "MSI level", "Comment", "Mannually modified",
            //    "Isotope tracking parent ID",  "Isotope tracking weight number", "Dot product", "Reverse dot product", "Fragment presence %", "S/N average",
            //    "Spectrum reference file name", "MS1 isotopic spectrum", "MS/MS spectrum"
            //};

            var metadata = new List<string>() {
                counter.ToString(), Math.Round(alignedSpot.CentralRetentionTime, 3).ToString(), Math.Round(alignedSpot.CentralAccurateMass, 5).ToString(),
                Math.Round(dt, 4).ToString() ,Math.Round(ccs, 4).ToString(),
                metaboliteName, alignedSpot.AdductIonName, postCurationResult, Math.Round(alignedSpot.FillParcentage, 3).ToString(), alignedSpot.MsmsIncluded.ToString(),
                formula, ontology, inchiKey, smiles, comment, isotopeTrackingParentID, isotopeTrackingWeightNumber,
                dotProduct, revDotProd, precense, Math.Round(alignedSpot.SignalToNoiseAve, 2).ToString(),
                alignedSpot.AlignedPeakPropertyBeanCollection[alignedSpot.RepresentativeFileID].FileName
            };
            
            sw.Write(String.Join("\t", metadata) + "\t" + ms1IsotopeSpectrum + "\t");

            var spectrumString = GetSpectrumString(massSpectra);
            sw.Write(spectrumString + "\t");
        }



        public static string GetSpectrumString(List<double[]> massSpectra) {
            if (massSpectra == null || massSpectra.Count == 0)
                return string.Empty;

            var specString = string.Empty;
            for (int i = 0; i < massSpectra.Count; i++) {
                var spec = massSpectra[i];
                var mz = Math.Round(spec[0], 5);
                var intensity = Math.Round(spec[1], 0);
                var sString = mz + ":" + intensity;

                if (i == massSpectra.Count - 1)
                    specString += sString;
                else
                    specString += sString + " ";

                if (specString.Length >= 30000) break;
            }

            return specString;
        }

        public static void WriteDataMatrixMetaDataForGnps(StreamWriter sw, AlignmentPropertyBean alignedSpot, List<AlignmentPropertyBean> alignedSpots,
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> textDB, 
            FileStream fs, List<long> seekpointList, int id, AnalysisParametersBean param) {
            var metaboliteName = "Unknown";
            var formula = "null";
            var ontology = "null";
            var inchiKey = "null";
            var smiles = "null";
            var refRtString = "null";
            var refMzString = "null";
            var msiLevel = 999;
            var msiLevelString = "999";
            var lsiLevel = string.Empty;
            //var links = "No record";
            //var comment = "Original ID: " + alignedSpot.AlignmentID;
            var comment = string.Empty;
            comment += alignedSpot.Comment != null ? alignedSpot.Comment : string.Empty;
            var libraryID = alignedSpot.LibraryID;
            var textLibId = alignedSpot.PostIdentificationLibraryID;
            var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, alignedSpot.AlignmentID);
            var ms1IsotopeSpectrum = getMs1IsotopicSpectrumString(ms2DecResult);
            var massSpectra = ms2DecResult.MassSpectra;
            var postCurationResult = AlignedSpotComment(alignedSpot, alignedSpots);
            var isManuallyModified = alignedSpot.IsManuallyModified;
           // var postCurationResult = GetPostCurationResult(alignedSpot);
            var isotopeTrackingParentID =
                alignedSpot.IsotopeTrackingParentID >= 0
                ? alignedSpot.IsotopeTrackingParentID.ToString()
                : "null";

            var isotopeTrackingWeightNumber =
               alignedSpot.IsotopeTrackingWeightNumber >= 0
               ? alignedSpot.IsotopeTrackingWeightNumber.ToString()
               : "null";

            var totalScore = alignedSpot.TotalSimilairty > 0 
                ? alignedSpot.TotalSimilairty > 1000
                ? "100"
                : Math.Round(alignedSpot.TotalSimilairty * 0.1, 1).ToString()
                : "null";
            var rtScore = alignedSpot.RetentionTimeSimilarity > 0 ? Math.Round(alignedSpot.RetentionTimeSimilarity * 0.1, 1).ToString() : "null";
            var dotProduct = alignedSpot.MassSpectraSimilarity > 0 ? Math.Round(alignedSpot.MassSpectraSimilarity * 0.1, 1).ToString() : "null";
            var revDotProd = alignedSpot.ReverseSimilarity > 0 ? Math.Round(alignedSpot.ReverseSimilarity * 0.1, 1).ToString() : "null";
            var precense = alignedSpot.FragmentPresencePercentage > 0
                ? alignedSpot.FragmentPresencePercentage > 1000
                ? "100"
                : Math.Round(alignedSpot.FragmentPresencePercentage * 0.1, 1).ToString()
                : "null";

            if (alignedSpot.MetaboliteName != string.Empty) metaboliteName = alignedSpot.MetaboliteName;
            if (textLibId >= 0 && textDB != null && textDB.Count != 0) {
                var refRt = textDB[textLibId].RetentionTime;
                if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
                refMzString = Math.Round(textDB[textLibId].AccurateMass, 5).ToString();
                if (textDB[textLibId].Formula != null && textDB[textLibId].Formula.FormulaString != null && textDB[textLibId].Formula.FormulaString != string.Empty)
                    formula = textDB[textLibId].Formula.FormulaString;
                if (textDB[textLibId].Inchikey != null && textDB[textLibId].Inchikey != string.Empty)
                    inchiKey = textDB[textLibId].Inchikey;
                if (textDB[textLibId].Smiles != null && textDB[textLibId].Smiles != string.Empty)
                    smiles = textDB[textLibId].Smiles;
                if (textDB[textLibId].Ontology != null && textDB[textLibId].Ontology != string.Empty)
                    ontology = textDB[textLibId].Ontology;
                msiLevelString = "annotated by user-defined text library";
            }
            else if (libraryID >= 0 && mspDB != null && mspDB.Count != 0) {
                var refRt = mspDB[libraryID].RetentionTime;
                if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
                refMzString = Math.Round(mspDB[libraryID].PrecursorMz, 5).ToString();
                formula = mspDB[libraryID].Formula;
                inchiKey = mspDB[libraryID].InchiKey;
                smiles = mspDB[libraryID].Smiles;
                ontology = mspDB[libraryID].Ontology;
                if (ontology == null || ontology == string.Empty) {
                    ontology = mspDB[libraryID].CompoundClass;
                }
                msiLevel = getMsiLevel(alignedSpot, refRt, param);
                msiLevelString = msiLevel.ToString();
                //msiLevelString = lsiLevel != string.Empty ? msiLevel.ToString() + "_" + lsiLevel.ToString() : msiLevel.ToString();
            }

            // should be included
            //var headers = new List<string>() {
            //    "Alignment ID", "Average Rt(min)", "Average Mz", "Metabolite name", "Adduct ion name", "Post curation result", "Fill %", "MS/MS included",
            //     "Formula", "Ontology", "INCHIKEY", "SMILES", "Comment", "Isotope tracking parent ID",  "Isotope tracking weight number",
            //     "Dot product", "Reverse dot product", "Fragment presence %", "S/N average",
            //     "Spectrum reference file name", "MS1 isotopic spectrum", "MS/MS spectrum"
            //};

            var metadata = new List<string>() {
                id.ToString(), Math.Round(alignedSpot.CentralRetentionTime, 3).ToString(), Math.Round(alignedSpot.CentralAccurateMass, 5).ToString(),
                metaboliteName, alignedSpot.AdductIonName, postCurationResult, Math.Round(alignedSpot.FillParcentage, 3).ToString(), alignedSpot.MsmsIncluded.ToString(),
                formula, ontology, inchiKey, smiles, comment, isotopeTrackingParentID, isotopeTrackingWeightNumber,
                dotProduct, revDotProd, precense, Math.Round(alignedSpot.SignalToNoiseAve, 2).ToString(),
                alignedSpot.AlignedPeakPropertyBeanCollection[alignedSpot.RepresentativeFileID].FileName
            };
            sw.Write(String.Join("\t", metadata) + "\t" + ms1IsotopeSpectrum + "\t");
            WriteSpectraAsTxt(sw, new ObservableCollection<double[]>(massSpectra));

            sw.Write("\t");
        }

        private static int getMsiLevel(AlignmentPropertyBean alignedSpot, float refRt,
            AnalysisParametersBean param) {
            var msiLevel = 999; // unknown
            var lsiLevel = string.Empty;
            if (alignedSpot.IsMs1Match) msiLevel = 530; // 430: m/z+MS/MS matched
            if (alignedSpot.IsMs2Match) msiLevel = 430; // 430: m/z+MS/MS matched
            if (msiLevel == 430) {
                if (alignedSpot.IsLipidPositionMatch) {
                    msiLevel = 400; // 400: MS/MS matched, lipid acyl position resolved
                } else if (alignedSpot.IsLipidChainsMatch) {
                    msiLevel = 410; //410: MS/MS matched, lipid acyl chain resolved
                } else if (alignedSpot.IsLipidClassMatch) {
                    msiLevel = 420; //420: MS/MS matched, lipid class resolved
                }
            }

            if (!param.IsUseRetentionInfoForIdentificationFiltering && !param.IsUseRetentionInfoForIdentificationScoring) return msiLevel;

            var rtTol = param.RetentionTimeLibrarySearchTolerance <= 0.5 
                ? param.RetentionTimeLibrarySearchTolerance : 0.5;

            if (refRt > 0 && Math.Abs(refRt - alignedSpot.CentralRetentionTime) < rtTol) {
                if (alignedSpot.IsMs2Match) {
                    switch (msiLevel) {
                        case 400: return 300;
                        case 410: return 310;
                        case 420: return 320;
                        case 430: return 330;
                    }
                }
                else if (alignedSpot.IsMs1Match) {
                    return 520;
                }
            }

            return msiLevel;
        }

        private static int getMsiLevel(AlignmentPropertyBean alignedSpot, float refRt, AlignedDriftSpotPropertyBean driftSpot, float refCcs, AnalysisParametersBean param) {
            var msiLevel = 999;

            if (driftSpot.IsMs1Match) msiLevel = 530; // 430: m/z+MS/MS matched
            if (driftSpot.IsMs2Match) msiLevel = 430; // 430: m/z+MS/MS matched
            if (msiLevel == 430) {
                if (driftSpot.IsLipidPositionMatch) {
                    msiLevel = 400; // 400: MS/MS matched, lipid acyl position resolved
                }
                else if (driftSpot.IsLipidChainsMatch) {
                    msiLevel = 410; //410: MS/MS matched, lipid acyl chain resolved
                }
                else if (driftSpot.IsLipidClassMatch) {
                    msiLevel = 420; //420: MS/MS matched, lipid class resolved
                }
            }

            var isCcsCheck = false;
            if (param.IsUseCcsForIdentificationFiltering || param.IsUseCcsForIdentificationScoring) {
                var ccsTol = param.CcsSearchTolerance <= 5.0 ? param.CcsSearchTolerance : 5.0;
                if (refCcs > 0 && Math.Abs(refCcs - driftSpot.CentralCcs) < ccsTol) {
                    isCcsCheck = true;
                }
            }

            var isRtCheck = false;
            if (param.IsUseRetentionInfoForIdentificationFiltering || param.IsUseRetentionInfoForIdentificationScoring) {
                var rtTol = param.RetentionTimeLibrarySearchTolerance <= 0.5
                          ? param.RetentionTimeLibrarySearchTolerance : 0.5;
                isRtCheck = true;
            }

            if (isRtCheck && isCcsCheck) {
                if (driftSpot.IsMs2Match) {
                    switch (msiLevel) {
                        case 400: return 100;
                        case 410: return 110;
                        case 420: return 120;
                        case 430: return 130;
                    }
                }
                else if (driftSpot.IsMs1Match) {
                    return 500;
                }
            }
            else if (isRtCheck) {
                if (driftSpot.IsMs2Match) {
                    switch (msiLevel) {
                        case 400: return 300;
                        case 410: return 310;
                        case 420: return 320;
                        case 430: return 330;
                    }
                }
                else if (driftSpot.IsMs1Match) {
                    return 520;
                }
            }
            else if (isCcsCheck) {
                if (driftSpot.IsMs2Match) {
                    switch (msiLevel) {
                        case 400: return 200;
                        case 410: return 210;
                        case 420: return 220;
                        case 430: return 230;
                    }
                }
                else if (driftSpot.IsMs1Match) {
                    return 510;
                }
            }

            return msiLevel;
        }

        private static int getMsiLevel(PeakAreaBean peakSpot, float refRt,
           AnalysisParametersBean param) {
            var msiLevel = 999; // unknown
            var lsiLevel = string.Empty;
            if (peakSpot.IsMs1Match) msiLevel = 530; // 430: m/z+MS/MS matched
            if (peakSpot.IsMs2Match) msiLevel = 430; // 430: m/z+MS/MS matched
            if (msiLevel == 430) {
                if (peakSpot.IsLipidPositionMatch) {
                    msiLevel = 400; // 400: MS/MS matched, lipid acyl position resolved
                }
                else if (peakSpot.IsLipidChainsMatch) {
                    msiLevel = 410; //410: MS/MS matched, lipid acyl chain resolved
                }
                else if (peakSpot.IsLipidClassMatch) {
                    msiLevel = 420; //420: MS/MS matched, lipid class resolved
                }
            }

            if (!param.IsUseRetentionInfoForIdentificationFiltering && !param.IsUseRetentionInfoForIdentificationScoring) return msiLevel;

            var rtTol = param.RetentionTimeLibrarySearchTolerance <= 0.5
                ? param.RetentionTimeLibrarySearchTolerance : 0.5;

            if (refRt > 0 && Math.Abs(refRt - peakSpot.RtAtPeakTop) < rtTol) {
                if (peakSpot.IsMs2Match) {
                    switch (msiLevel) {
                        case 400: return 300;
                        case 410: return 310;
                        case 420: return 320;
                        case 430: return 330;
                    }
                }
                else if (peakSpot.IsMs1Match) {
                    return 520;
                }
            }

            return msiLevel;
        }

        private static int getMsiLevel(PeakAreaBean peakSpot, float refRt, DriftSpotBean driftSpot, float refCcs, AnalysisParametersBean param) {
            var msiLevel = 999;

            if (driftSpot.IsMs1Match) msiLevel = 530; // 430: m/z+MS/MS matched
            if (driftSpot.IsMs2Match) msiLevel = 430; // 430: m/z+MS/MS matched
            if (msiLevel == 430) {
                if (driftSpot.IsLipidPositionMatch) {
                    msiLevel = 400; // 400: MS/MS matched, lipid acyl position resolved
                }
                else if (driftSpot.IsLipidChainsMatch) {
                    msiLevel = 410; //410: MS/MS matched, lipid acyl chain resolved
                }
                else if (driftSpot.IsLipidClassMatch) {
                    msiLevel = 420; //420: MS/MS matched, lipid class resolved
                }
            }

            var isCcsCheck = false;
            if (param.IsUseCcsForIdentificationFiltering || param.IsUseCcsForIdentificationScoring) {
                var ccsTol = param.CcsSearchTolerance <= 5.0 ? param.CcsSearchTolerance : 5.0;
                if (refCcs > 0 && Math.Abs(refCcs - driftSpot.Ccs) < ccsTol) {
                    isCcsCheck = true;
                }
            }

            var isRtCheck = false;
            if (param.IsUseRetentionInfoForIdentificationFiltering || param.IsUseRetentionInfoForIdentificationScoring) {
                var rtTol = param.RetentionTimeLibrarySearchTolerance <= 0.5
                          ? param.RetentionTimeLibrarySearchTolerance : 0.5;
                isRtCheck = true;
            }

            if (isRtCheck && isCcsCheck) {
                if (driftSpot.IsMs2Match) {
                    switch (msiLevel) {
                        case 400: return 100;
                        case 410: return 110;
                        case 420: return 120;
                        case 430: return 130;
                    }
                }
                else if (driftSpot.IsMs1Match) {
                    return 500;
                }
            }
            else if (isRtCheck) {
                if (driftSpot.IsMs2Match) {
                    switch (msiLevel) {
                        case 400: return 300;
                        case 410: return 310;
                        case 420: return 320;
                        case 430: return 330;
                    }
                }
                else if (driftSpot.IsMs1Match) {
                    return 520;
                }
            }
            else if (isCcsCheck) {
                if (driftSpot.IsMs2Match) {
                    switch (msiLevel) {
                        case 400: return 200;
                        case 410: return 210;
                        case 420: return 220;
                        case 430: return 230;
                    }
                }
                else if (driftSpot.IsMs1Match) {
                    return 510;
                }
            }

            return msiLevel;
        }



        private static string getMs1IsotopicSpectrumString(MS2DecResult ms2DecResult) {
            var spectrumString = Math.Round(ms2DecResult.Ms1AccurateMass, 5) + ":" + Math.Round(ms2DecResult.Ms1PeakHeight, 0) + " " + 
                Math.Round(ms2DecResult.Ms1AccurateMass + 1.00335, 5) + ":" + Math.Round(ms2DecResult.Ms1IsotopicIonM1PeakHeight, 0) + " " +
                Math.Round(ms2DecResult.Ms1AccurateMass + 2.00671, 5) + ":" + Math.Round(ms2DecResult.Ms1IsotopicIonM2PeakHeight, 0);
            return spectrumString;
        }

        public static object GetPostCurationResult(AlignmentPropertyBean prop)
        {
            if (prop.AlignmentSpotVariableCorrelations == null) return string.Empty;

            var postCurateResult = string.Empty;

            var isotopeNum = prop.PostDefinedIsotopeWeightNumber;
            if (isotopeNum < 0)
                isotopeNum = prop.IsotopeTrackingWeightNumber;

            if (isotopeNum > 0) {
                var isotopeParent = prop.PostDefinedIsotopeParentID;
                if (isotopeParent == -1)
                    isotopeParent = prop.IsotopeTrackingParentID;

                postCurateResult += "May be M + " + isotopeNum + " of Alignment ID: " + isotopeParent + "; ";
            }

            if (prop.AlignmentID != prop.PostDefinedAdductParentID && isotopeNum == 0) {
                postCurateResult += "May be " + prop.AdductIonName + " of Alignment ID: " + prop.PostDefinedAdductParentID + "; ";
            }

            if (prop.AlignmentSpotVariableCorrelations.Count > 0 && isotopeNum == 0) {
                postCurateResult += "Highly correlated with ";
                foreach (var corrl in prop.AlignmentSpotVariableCorrelations) {
                    postCurateResult += corrl.CorrelateAlignmentID + "(" + Math.Round(corrl.CorrelationScore, 2) + ") ";
                }
            }

            return postCurateResult;
        }

        public static void WriteGnpsDataMatrixHeader(StreamWriter sw, ObservableCollection<AnalysisFileBean> analysisFiles) {
            var headers = new List<string>() {
                "Alignment ID", "Average Rt(min)", "Average Mz", "Metabolite name", "Adduct ion name", "Post curation result", "Fill %", "MS/MS included",
                 "Formula", "Ontology", "INCHIKEY", "SMILES", "Comment", "Isotope tracking parent ID",  "Isotope tracking weight number", 
                 "Dot product", "Reverse dot product", "Fragment presence %", "S/N average",
                 "Spectrum reference file name", "MS1 isotopic spectrum", "MS/MS spectrum"
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

            for (int i = 0; i < headers.Count; i++) sw.Write(headers[i] + "\t");
            sw.WriteLine(String.Join("\t", analysisFiles.Select(n => n.AnalysisFilePropertyBean.AnalysisFileName)));
        }

        public static void WriteGnpsIonmobilityDataMatrixHeader(StreamWriter sw, ObservableCollection<AnalysisFileBean> analysisFiles) {
            var headers = new List<string>() {
                "Alignment ID", "Average Rt(min)", "Average Mz", "Average drift time", "Average CCS",
                "Metabolite name", "Adduct ion name", "Post curation result", "Fill %", "MS/MS included",
                 "Formula", "Ontology", "INCHIKEY", "SMILES", "Comment", "Isotope tracking parent ID",  "Isotope tracking weight number",
                 "Dot product", "Reverse dot product", "Fragment presence %", "S/N average",
                 "Spectrum reference file name", "MS1 isotopic spectrum", "MS/MS spectrum"
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

            for (int i = 0; i < headers.Count; i++) sw.Write(headers[i] + "\t");
            sw.WriteLine(String.Join("\t", analysisFiles.Select(n => n.AnalysisFilePropertyBean.AnalysisFileName)));
        }


        public static void WriteDataMatrixHeader(StreamWriter sw, ObservableCollection<AnalysisFileBean> analysisFiles)
        {
            var headers = new List<string>() {
                "Alignment ID", "Average Rt(min)", "Average Mz", "Metabolite name", "Adduct type", "Post curation result", "Fill %", "MS/MS assigned",
                "Reference RT", "Reference m/z", "Formula", "Ontology", "INCHIKEY", "SMILES",
                "Annotation tag (VS1.0)", "RT matched", "m/z matched", "MS/MS matched",
                "Comment", "Manually modified for quantification", "Manually modified for annotation",
                "Isotope tracking parent ID",  "Isotope tracking weight number", "Total score", "RT similarity", "Dot product", "Reverse dot product", "Fragment presence %", "S/N average",
                "Spectrum reference file name", "MS1 isotopic spectrum", "MS/MS spectrum"
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
                "Alignment ID", "Average Rt(min)", "Average Mz", "Metabolite name", "Adduct type", "Post curation result", "Fill %", "MS/MS assigned",
                "Reference RT", "Reference m/z", "Formula", "Ontology", "INCHIKEY", "SMILES",
                "Annotation tag (VS1.0)", "RT matched", "m/z matched", "MS/MS matched",
                "Comment", "Manually modified for quantification", "Manually modified for annotation",
                "Isotope tracking parent ID",  "Isotope tracking weight number", "Total score", "RT similarity", "Dot product", "Reverse dot product", "Fragment presence %", "S/N average",
                "Spectrum reference file name", "MS1 isotopic spectrum", "MS/MS spectrum"
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

        public static void WriteDataMatrixHeaderAtIonMobility(StreamWriter sw, ObservableCollection<AnalysisFileBean> analysisFiles) {

            var headers = new List<string>() {
                "Alignment ID", "Average Rt(min)", "Average Mz","Average mobility", "Average CCS",
                "Metabolite name", "Adduct type", "Post curation result", "Fill %", "MS/MS assigned",
                "Reference RT", "Reference CCS", "Reference m/z", "Formula", "Ontology", "INCHIKEY", "SMILES",
                "Annotation tag (VS1.0)", "RT matched", "CCS matched", "m/z matched", "MS/MS matched",
                "Comment", "Manually modified for quantification", "Manually modified for annotation",
                "Isotope tracking parent ID",  "Isotope tracking weight number", "Total score", "RT similarity", "CCS similarity",
                "Dot product", "Reverse dot product", "Fragment presence %", "S/N average",
                "Spectrum reference file name", "MS1 isotopic spectrum", "MS/MS spectrum"
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

        public static void WriteDataMatrixHeaderAtIonMobility(StreamWriter sw, ObservableCollection<AnalysisFileBean> analysisFiles, List<BasicStats> StatsList) {

            var headers = new List<string>() {
                "Alignment ID", "Average Rt(min)", "Average Mz","Average mobility", "Average CCS",
                "Metabolite name", "Adduct type", "Post curation result", "Fill %", "MS/MS assigned",
                "Reference RT", "Reference CCS", "Reference m/z", "Formula", "Ontology", "INCHIKEY", "SMILES", "Annotation tag (VS1.0)", "RT matched", "CCS matched", "m/z matched", "MS/MS matched",
                "Comment", "Manually modified for quantification", "Manually modified for annotation",
                "Isotope tracking parent ID",  "Isotope tracking weight number", "Total score", "RT similarity", "CCS similarity",
                "Dot product", "Reverse dot product", "Fragment presence %", "S/N average",
                "Spectrum reference file name", "MS1 isotopic spectrum", "MS/MS spectrum"
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

        public static ObservableCollection<AlignmentPropertyBean> GetFilteredAlignedSpotsByIsotpeTrackingResult(
            ObservableCollection<AlignmentPropertyBean> alignedSpots, AnalysisParametersBean param, int targetIsotopeTrackFileID) {
            var alignedSpotList = new List<AlignmentPropertyBean>(alignedSpots).OrderBy(n => n.IsotopeTrackingParentID).ThenBy(n => n.IsotopeTrackingWeightNumber).ToList();
            var filteredSpots = new ObservableCollection<AlignmentPropertyBean>();

            var tempSpots = new ObservableCollection<AlignmentPropertyBean>() { alignedSpotList[0] };
            var nonlabelID = param.NonLabeledReferenceID;
            var labeledID = param.FullyLabeledReferenceID;

            for (int i = 0; i < alignedSpotList.Count - 1; i++) {
                if (alignedSpotList[i].IsotopeTrackingParentID == alignedSpotList[i + 1].IsotopeTrackingParentID)
                    tempSpots.Add(alignedSpotList[i + 1]);
                else {
                    StoringTempSpotsToMaster(tempSpots, filteredSpots, param.SetFullyLabeledReferenceFile, labeledID, nonlabelID);
                    tempSpots = new ObservableCollection<AlignmentPropertyBean>() { alignedSpotList[i + 1] };
                }
            }

            //reminder
            StoringTempSpotsToMaster(tempSpots, filteredSpots, param.SetFullyLabeledReferenceFile, labeledID, nonlabelID);

            if (filteredSpots.Count > 0)
                return filteredSpots;
            else
                return alignedSpots;
        }

        public static void StoringTempSpotsToMaster(ObservableCollection<AlignmentPropertyBean> tempSpots,
            ObservableCollection<AlignmentPropertyBean> filteredSpots, bool isSetFullyLabeledReferenceFile, int labeledID, int nonlabelID) {
            if (tempSpots.Count > 1) {
                foreach (var tSpot in tempSpots) {
                    if (tSpot.IsotopeTrackingWeightNumber >= 0 && tSpot.IsotopeTrackingWeightNumber <= 1000) {
                        filteredSpots.Add(tSpot);
                    }
                }

                //var firstSpot = tempSpots[0];

                //var offset = 0;
                //// first spot initialization. until 2 Da higher range, the intensity is checked.
                //if (tempSpots.Count > 3) {
                //    if (tempSpots[1].IsotopeTrackingWeightNumber == 1 || tempSpots[1].IsotopeTrackingWeightNumber == 2) {
                //        if (tempSpots[1].AlignedPeakPropertyBeanCollection[nonlabelID].Variable > firstSpot.AlignedPeakPropertyBeanCollection[nonlabelID].Variable * 5) {
                //            firstSpot = tempSpots[1];
                //            offset = firstSpot.IsotopeTrackingWeightNumber;
                //        }
                //    }

                //    if (tempSpots[2].IsotopeTrackingWeightNumber == 1 || tempSpots[2].IsotopeTrackingWeightNumber == 2) {
                //        if (tempSpots[2].AlignedPeakPropertyBeanCollection[nonlabelID].Variable > firstSpot.AlignedPeakPropertyBeanCollection[nonlabelID].Variable * 5) {
                //            firstSpot = tempSpots[2];
                //            offset = firstSpot.IsotopeTrackingWeightNumber;
                //        }
                //    }
                //}

                //var lastSpot = tempSpots[tempSpots.Count - 1];
                //var lastWeightNum = lastSpot.IsotopeTrackingWeightNumber;
                //var lastWeightID = tempSpots.Count - 1;

                //if (isSetFullyLabeledReferenceFile) {

                //    if (lastSpot.IsotopeTrackingWeightNumber - firstSpot.IsotopeTrackingWeightNumber > 3) {

                //        for (int i = tempSpots.Count - 2; i >= 0; i--) {
                //            var tSpot = tempSpots[i];
                //            var tSpotID = i;

                //            if (lastWeightNum - tSpot.IsotopeTrackingWeightNumber > 2) break;

                //            if (lastSpot.AlignedPeakPropertyBeanCollection[labeledID].Variable * 5 <
                //                tSpot.AlignedPeakPropertyBeanCollection[labeledID].Variable) {
                //                lastSpot = tSpot;
                //                lastWeightNum = tSpot.IsotopeTrackingWeightNumber;
                //                lastWeightID = tSpotID;
                //            }
                //        }
                //    }

                //    var nonLabelIntensity = lastSpot.AlignedPeakPropertyBeanCollection[nonlabelID].Variable;
                //    var labeledIntensity = lastSpot.AlignedPeakPropertyBeanCollection[labeledID].Variable;

                //    var isLabeledDetected =
                //       lastSpot.AlignedPeakPropertyBeanCollection[labeledID].PeakID < 0
                //       ? false : true;

                //    var isNonlabeledDetected =
                //      lastSpot.AlignedPeakPropertyBeanCollection[nonlabelID].PeakID < 0
                //      ? false : true;

                //    if ((isLabeledDetected && !isNonlabeledDetected) || labeledIntensity > nonLabelIntensity * 4.0) {
                //        foreach (var tSpot in tempSpots) {
                //            //if ((tSpot.LibraryID >= 0 || tSpot.PostIdentificationLibraryID >= 0) && tSpot.MetaboliteName.Contains("w/o")) {
                //            //    tSpot.MetaboliteName = "Unknown";
                //            //    tSpot.LibraryID = -1;
                //            //    tSpot.PostIdentificationLibraryID = -1;
                //            //}
                //            if (tSpot.IsotopeTrackingWeightNumber - offset <= lastWeightNum && tSpot.IsotopeTrackingWeightNumber >= offset) {
                //                tSpot.IsotopeTrackingWeightNumber = tSpot.IsotopeTrackingWeightNumber - offset;
                //                filteredSpots.Add(tSpot);
                //            }
                //        }
                //    }
                //}
                //else {
                //    foreach (var tSpot in tempSpots) {
                //        //if ((tSpot.LibraryID >= 0 || tSpot.PostIdentificationLibraryID >= 0) && tSpot.MetaboliteName.Contains("w/o")) {
                //        //    tSpot.MetaboliteName = "Unknown";
                //        //    tSpot.LibraryID = -1;
                //        //    tSpot.PostIdentificationLibraryID = -1;
                //        //}
                //        if (tSpot.IsotopeTrackingWeightNumber >= offset) {
                //            tSpot.IsotopeTrackingWeightNumber = tSpot.IsotopeTrackingWeightNumber - offset;
                //            filteredSpots.Add(tSpot);
                //        }
                //        //filteredSpots.Add(tSpot);
                //    }
                //}
            }
        }

        public static void ExportIsotopeTrackingResultAsMatFormatFile(string outputFolder,
            ProjectPropertyBean projectProp,
            AnalysisParametersBean param,
            List<MspFormatCompoundInformationBean> mspDB,
            AlignmentResultBean alignmentResultBean,
            FileStream fs, List<long> seekpointList) {

            var targetIsotopeTrackFileID = param.NonLabeledReferenceID;
            if (targetIsotopeTrackFileID < 0)
                return;

            //var currentSpotID = mainWindow.FocusedAlignmentPeakID;
            var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;
            alignedSpots = GetFilteredAlignedSpotsByIsotpeTrackingResult(alignedSpots, param, targetIsotopeTrackFileID);

            var tempSpots = new ObservableCollection<AlignmentPropertyBean>() { alignedSpots[0] };

            for (int i = 0; i < alignedSpots.Count - 1; i++) {
                if (alignedSpots[i].IsotopeTrackingParentID == alignedSpots[i + 1].IsotopeTrackingParentID)
                    tempSpots.Add(alignedSpots[i + 1]);
                else {
                    if (tempSpots.Count > 1) {
                        writeIsotopeTrackingResultAsMatFile(projectProp, param, mspDB, tempSpots, outputFolder, fs, seekpointList);
                    }
                    tempSpots = new ObservableCollection<AlignmentPropertyBean>() { alignedSpots[i + 1] };
                }
            }

            //reminder
            if (tempSpots.Count > 1)
                writeIsotopeTrackingResultAsMatFile(projectProp, param, mspDB, tempSpots, outputFolder, fs, seekpointList);
            // mainWindow.FocusedAlignmentPeakID = currentSpotID;
        }

        private static void writeIsotopeTrackingResultAsMatFile(
            ProjectPropertyBean projectProp,
            AnalysisParametersBean param,
            List<MspFormatCompoundInformationBean> mspDB,
            ObservableCollection<AlignmentPropertyBean> tempSpots,
            string outputfolder,
            FileStream fs, List<long> seekPoints) {

            var filename = tempSpots[0].IsotopeTrackingParentID + "_"
                + Math.Round(tempSpots[0].CentralRetentionTime, 3) + "_"
                + Math.Round(tempSpots[0].CentralAccurateMass, 5) + "_"
                + Math.Round(tempSpots[tempSpots.Count - 1].CentralAccurateMass, 5) + "_"
                + tempSpots[tempSpots.Count - 1].IsotopeTrackingWeightNumber;

            var filepath = Path.Combine(outputfolder, filename + ".mat");
            var basicProperty = tempSpots[0];
            var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekPoints, basicProperty.AlignmentID);
            var isotopeLabel = param.IsotopeTrackingDictionary;
            var labelType = isotopeLabel.IsotopeElements[isotopeLabel.SelectedID].ElementName;

            using (var sw = new StreamWriter(filepath, false, Encoding.ASCII)) {

                var name = basicProperty.MetaboliteName;
                if (name == string.Empty || name.Contains("w/o"))
                    name = "Unknown";
                var adduct = basicProperty.AdductIonName;
                if ((adduct == null || adduct == string.Empty) && projectProp.IonMode == IonMode.Positive)
                    adduct = "[M+H]+";
                else if ((adduct == null || adduct == string.Empty) && projectProp.IonMode == IonMode.Negative)
                    adduct = "[M-H]-";

                var libraryID = basicProperty.LibraryID;
                if (name == "Unknown")
                    libraryID = -1;

                sw.WriteLine("NAME: " + filename);
                sw.WriteLine("SCANNUMBER: " + basicProperty.AlignmentID);
                sw.WriteLine("RETENTIONTIME: " + ms2DecResult.PeakTopRetentionTime);
                sw.WriteLine("PRECURSORMZ: " + ms2DecResult.Ms1AccurateMass);
                sw.WriteLine("PRECURSORTYPE: " + adduct);
                sw.WriteLine("IONMODE: " + projectProp.IonMode);
                sw.WriteLine("SPECTRUMTYPE: Centroid");
                sw.WriteLine("INTENSITY: " + ms2DecResult.Ms1PeakHeight);
                sw.WriteLine("INCHIKEY: " + MspDataRetrieve.GetInChIKey(libraryID, mspDB));
                sw.WriteLine("SMILES: " + MspDataRetrieve.GetSMILES(libraryID, mspDB));
                sw.WriteLine("FORMULA: " + MspDataRetrieve.GetFormula(libraryID, mspDB));

                if (projectProp.FinalSavedDate != null) {
                    sw.WriteLine("DATE: " + projectProp.FinalSavedDate.Date);
                }

                if (projectProp.Authors != null && projectProp.Authors != string.Empty) {
                    sw.WriteLine("AUTHORS: " + projectProp.Authors);
                }

                if (projectProp.License != null && projectProp.License != string.Empty) {
                    sw.WriteLine("LICENSE: " + projectProp.License);
                }

                if (projectProp.CollisionEnergy != null && projectProp.CollisionEnergy != string.Empty) {
                    sw.WriteLine("COLLISIONENERGY: " + projectProp.CollisionEnergy);
                }

                if (projectProp.InstrumentType != null && projectProp.InstrumentType != string.Empty) {
                    sw.WriteLine("INSTRUMENTTYPE: " + projectProp.InstrumentType);
                }

                if (projectProp.Instrument != null && projectProp.Instrument != string.Empty) {
                    sw.WriteLine("INSTRUMENT: " + projectProp.Instrument);
                }

                sw.WriteLine("#Specific field for labeled experiment");
                switch (labelType) {
                    case "13C":
                        sw.WriteLine("CarbonCount: " + tempSpots[tempSpots.Count - 1].IsotopeTrackingWeightNumber);
                        break;
                    case "15N":
                        sw.WriteLine("NitrogenCount: " + tempSpots[tempSpots.Count - 1].IsotopeTrackingWeightNumber);
                        break;
                    case "34S":
                        sw.WriteLine("SulfurCount: " + tempSpots[tempSpots.Count - 1].IsotopeTrackingWeightNumber);
                        break;
                    case "18O":
                        sw.WriteLine("OxygenCount: " + tempSpots[tempSpots.Count - 1].IsotopeTrackingWeightNumber);
                        break;
                    case "13C+15N":
                        sw.WriteLine("CarbonNitrogenCount: " + tempSpots[tempSpots.Count - 1].IsotopeTrackingWeightNumber);
                        break;
                    case "13C+34S":
                        sw.WriteLine("CarbonSulfurCount: " + tempSpots[tempSpots.Count - 1].IsotopeTrackingWeightNumber);
                        break;
                    case "15N+34S":
                        sw.WriteLine("NitrogenSulfurCount: " + tempSpots[tempSpots.Count - 1].IsotopeTrackingWeightNumber);
                        break;
                    case "13C+15N+34S":
                        sw.WriteLine("CarbonNitrogenSulfurCount: " + tempSpots[tempSpots.Count - 1].IsotopeTrackingWeightNumber);
                        break;
                }

                sw.WriteLine("Comment: annotated in MS-DIAL as " + name);

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
        }

        public static void WriteProfileAsMat(StreamWriter sw, ObservableCollection<RawSpectrum> spectrumCollection, 
            PeakAreaBean peakAreaBean, List<MspFormatCompoundInformationBean> mspDB, float isotopeExportMax) {
            var ms1Spectra = DataAccessLcUtility.GetProfileMassSpectra(spectrumCollection, peakAreaBean.Ms1LevelDatapointNumber);
            var isotopes = getIsotopicIonSpectra(ms1Spectra, peakAreaBean.AccurateMass, isotopeExportMax);
            var msmsSpectra = DataAccessLcUtility.GetProfileMassSpectra(spectrumCollection, peakAreaBean.Ms2LevelDatapointNumber);

            WriteMspMetadata(sw, peakAreaBean, null, mspDB);

            sw.WriteLine("MSTYPE: MS1");
            sw.WriteLine("Num Peaks: " + isotopes.Count);
            foreach (var ion in isotopes) {
                sw.WriteLine(Math.Round(ion[0], 5) + "\t" + Math.Round(ion[1], 0));
            }

            sw.WriteLine("MSTYPE: MS2");
            sw.WriteLine("Num Peaks: " + msmsSpectra.Count);

            foreach (var ion in msmsSpectra) {
                sw.WriteLine(Math.Round(ion[0], 5) + "\t" + Math.Round(ion[1], 0));
            }

            sw.WriteLine();
        }

        public static void WriteProfileAsMat(StreamWriter sw, ObservableCollection<RawSpectrum> spectrumCollection,
           PeakAreaBean peakSpot, DriftSpotBean driftSpot, List<MspFormatCompoundInformationBean> mspDB, float isotopeExportMax) {
            var ms1Spectra = DataAccessLcUtility.GetProfileMassSpectra(spectrumCollection, driftSpot.Ms1LevelDatapointNumber);
            var isotopes = getIsotopicIonSpectra(ms1Spectra, driftSpot.AccurateMass, isotopeExportMax);
            var msmsSpectra = DataAccessLcUtility.GetProfileMassSpectra(spectrumCollection, driftSpot.Ms2LevelDatapointNumber);

            WriteMspMetadata(sw, peakSpot, driftSpot, mspDB);

            sw.WriteLine("MSTYPE: MS1");
            sw.WriteLine("Num Peaks: " + isotopes.Count);
            foreach (var ion in isotopes) {
                sw.WriteLine(Math.Round(ion[0], 5) + "\t" + Math.Round(ion[1], 0));
            }

            sw.WriteLine("MSTYPE: MS2");
            sw.WriteLine("Num Peaks: " + msmsSpectra.Count);

            foreach (var ion in msmsSpectra) {
                sw.WriteLine(Math.Round(ion[0], 5) + "\t" + Math.Round(ion[1], 0));
            }

            sw.WriteLine();
        }

        public static void WriteMs2DecAsMat(StreamWriter sw, ObservableCollection<RawSpectrum> spectrumCollection, 
            FileStream fs, List<long> seekpointList, PeakAreaBean peakAreaBean,
            List<MspFormatCompoundInformationBean> mspDB, AnalysisParametersBean param, 
            ProjectPropertyBean projectProperty, float isotopeExportMax) {

            var ms1Spectra = DataAccessLcUtility.GetCentroidMassSpectra(spectrumCollection, projectProperty.DataType,
                peakAreaBean.Ms1LevelDatapointNumber, param.CentroidMs1Tolerance, true);
            var isotopes = getIsotopicIonSpectra(ms1Spectra, peakAreaBean.AccurateMass, isotopeExportMax);
            var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, peakAreaBean.PeakID);
            var msmsSpectra = ms2DecResult.MassSpectra;
            
            WriteMspMetadata(sw, peakAreaBean, null, mspDB);

            sw.WriteLine("MSTYPE: MS1");
            sw.WriteLine("Num Peaks: " + isotopes.Count);
            foreach (var ion in isotopes) {
                sw.WriteLine(Math.Round(ion[0], 5) + "\t" + Math.Round(ion[1], 0));
            }

            sw.WriteLine("MSTYPE: MS2");
            sw.WriteLine("Num Peaks: " + msmsSpectra.Count);

            foreach (var ion in msmsSpectra) {
                sw.WriteLine(Math.Round(ion[0], 5) + "\t" + Math.Round(ion[1], 0));
            }

            sw.WriteLine();
        }

        public static void WriteMs2DecAsMat(StreamWriter sw, ObservableCollection<RawSpectrum> spectrumCollection,
          FileStream fs, List<long> seekpointList, PeakAreaBean peakSpot, DriftSpotBean driftSpot,
          List<MspFormatCompoundInformationBean> mspDB, AnalysisParametersBean param,
          ProjectPropertyBean projectProperty, float isotopeExportMax) {

            var ms1Spectra = DataAccessLcUtility.GetCentroidMassSpectra(spectrumCollection, projectProperty.DataType,
                driftSpot.Ms1LevelDatapointNumber, param.CentroidMs1Tolerance, true);
            var isotopes = getIsotopicIonSpectra(ms1Spectra, driftSpot.AccurateMass, isotopeExportMax);
            var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, driftSpot.MasterPeakID);
            var msmsSpectra = ms2DecResult.MassSpectra;

            WriteMspMetadata(sw, peakSpot, driftSpot, mspDB);

            sw.WriteLine("MSTYPE: MS1");
            sw.WriteLine("Num Peaks: " + isotopes.Count);
            foreach (var ion in isotopes) {
                sw.WriteLine(Math.Round(ion[0], 5) + "\t" + Math.Round(ion[1], 0));
            }

            sw.WriteLine("MSTYPE: MS2");
            sw.WriteLine("Num Peaks: " + msmsSpectra.Count);

            foreach (var ion in msmsSpectra) {
                sw.WriteLine(Math.Round(ion[0], 5) + "\t" + Math.Round(ion[1], 0));
            }

            sw.WriteLine();
        }

        public static void WriteProfileAsSiriusMs(StreamWriter sw, ObservableCollection<RawSpectrum> spectrumCollection,
            PeakAreaBean peakAreaBean, List<MspFormatCompoundInformationBean> mspDB, float isotopeExportMax) {
            var ms1Spectra = DataAccessLcUtility.GetProfileMassSpectra(spectrumCollection, peakAreaBean.Ms1LevelDatapointNumber);
            var isotopes = getIsotopicIonSpectra(ms1Spectra, peakAreaBean.AccurateMass, isotopeExportMax);
            var msmsSpectra = DataAccessLcUtility.GetProfileMassSpectra(spectrumCollection, peakAreaBean.Ms2LevelDatapointNumber);

            WriteSiriusMsMetadata(sw, peakAreaBean, null, mspDB);

            sw.WriteLine();

            sw.WriteLine(">ms1");
            foreach (var ion in isotopes) {
                sw.WriteLine(Math.Round(ion[0], 5) + " " + Math.Round(ion[1], 0));
            }

            sw.WriteLine();

            sw.WriteLine(">ms2");
            foreach (var ion in msmsSpectra) {
                sw.WriteLine(Math.Round(ion[0], 5) + " " + Math.Round(ion[1], 0));
            }
        }

        public static void WriteProfileAsSiriusMs(StreamWriter sw, ObservableCollection<RawSpectrum> spectrumCollection,
           PeakAreaBean peakSpot, DriftSpotBean driftSpot, List<MspFormatCompoundInformationBean> mspDB, float isotopeExportMax) {
            var ms1Spectra = DataAccessLcUtility.GetProfileMassSpectra(spectrumCollection, driftSpot.Ms1LevelDatapointNumber);
            var isotopes = getIsotopicIonSpectra(ms1Spectra, driftSpot.AccurateMass, isotopeExportMax);
            var msmsSpectra = DataAccessLcUtility.GetProfileMassSpectra(spectrumCollection, driftSpot.Ms2LevelDatapointNumber);

            WriteSiriusMsMetadata(sw, peakSpot, driftSpot, mspDB);

            sw.WriteLine();

            sw.WriteLine(">ms1");
            foreach (var ion in isotopes) {
                sw.WriteLine(Math.Round(ion[0], 5) + " " + Math.Round(ion[1], 0));
            }

            sw.WriteLine();

            sw.WriteLine(">ms2");
            foreach (var ion in msmsSpectra) {
                sw.WriteLine(Math.Round(ion[0], 5) + " " + Math.Round(ion[1], 0));
            }
        }

        public static void WriteMs2DecAsSiriusMs(StreamWriter sw, ObservableCollection<RawSpectrum> spectrumCollection,
            FileStream fs, List<long> seekpointList, PeakAreaBean peakAreaBean,
            List<MspFormatCompoundInformationBean> mspDB, AnalysisParametersBean param,
            ProjectPropertyBean projectProperty, float isotopeExportMax) {

            var ms1Spectra = DataAccessLcUtility.GetCentroidMassSpectra(spectrumCollection, projectProperty.DataType,
                peakAreaBean.Ms1LevelDatapointNumber, param.CentroidMs1Tolerance, true);
            var isotopes = getIsotopicIonSpectra(ms1Spectra, peakAreaBean.AccurateMass, isotopeExportMax);
            var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, peakAreaBean.PeakID);
            var msmsSpectra = ms2DecResult.MassSpectra;

            WriteSiriusMsMetadata(sw, peakAreaBean, null, mspDB);

            sw.WriteLine();

            sw.WriteLine(">ms1");
            foreach (var ion in isotopes) {
                sw.WriteLine(Math.Round(ion[0], 5) + " " + Math.Round(ion[1], 0));
            }

            sw.WriteLine();

            sw.WriteLine(">ms2");
            foreach (var ion in msmsSpectra) {
                sw.WriteLine(Math.Round(ion[0], 5) + " " + Math.Round(ion[1], 0));
            }
        }

        public static void WriteMs2DecAsSiriusMs(StreamWriter sw, ObservableCollection<RawSpectrum> spectrumCollection,
          FileStream fs, List<long> seekpointList, PeakAreaBean peakSpot, DriftSpotBean driftSpot,
          List<MspFormatCompoundInformationBean> mspDB, AnalysisParametersBean param,
          ProjectPropertyBean projectProperty, float isotopeExportMax) {

            var ms1Spectra = DataAccessLcUtility.GetCentroidMassSpectra(spectrumCollection, projectProperty.DataType,
                driftSpot.Ms1LevelDatapointNumber, param.CentroidMs1Tolerance, true);
            var isotopes = getIsotopicIonSpectra(ms1Spectra, driftSpot.AccurateMass, isotopeExportMax);
            var ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, driftSpot.MasterPeakID);
            var msmsSpectra = ms2DecResult.MassSpectra;

            WriteSiriusMsMetadata(sw, peakSpot, driftSpot, mspDB);

            sw.WriteLine();

            sw.WriteLine(">ms1");
            foreach (var ion in isotopes) {
                sw.WriteLine(Math.Round(ion[0], 5) + " " + Math.Round(ion[1], 0));
            }

            sw.WriteLine();

            sw.WriteLine(">ms2");
            foreach (var ion in msmsSpectra) {
                sw.WriteLine(Math.Round(ion[0], 5) + " " + Math.Round(ion[1], 0));
            }
        }


        private static void WriteSiriusMsMetadata(StreamWriter sw, PeakAreaBean peakSpot, DriftSpotBean driftSpot, List<MspFormatCompoundInformationBean> mspDB) {
            if (driftSpot == null) {
                var titleString = "Unknown"; if (peakSpot.MetaboliteName != string.Empty) titleString = peakSpot.MetaboliteName;
                titleString += "_" + peakSpot.PeakID;

                var adductString = peakSpot.AdductIonName;
                var adduct = AdductIonParcer.GetAdductIonBean(adductString);

                sw.WriteLine(">compound " + titleString);
                sw.WriteLine(">parentmass " + peakSpot.AccurateMass);
                sw.WriteLine(">formula " + MspDataRetrieve.GetFormula(peakSpot.LibraryID, mspDB));
                sw.WriteLine(">ionization " + adductString);
            }
            else {
                var titleString = "Unknown"; if (driftSpot.MetaboliteName != string.Empty) titleString = driftSpot.MetaboliteName;
                titleString += "_" + driftSpot.MasterPeakID;

                var adductString = driftSpot.AdductIonName;
                var adduct = AdductIonParcer.GetAdductIonBean(adductString);

                sw.WriteLine(">compound " + titleString);
                sw.WriteLine(">parentmass " + peakSpot.AccurateMass);
                sw.WriteLine(">ionization " + adductString);
                sw.WriteLine(">formula " + MspDataRetrieve.GetFormula(driftSpot.LibraryID, mspDB));
            }
        }



        private static void WriteMspMetadata(StreamWriter sw, PeakAreaBean peakSpot, DriftSpotBean driftSpot, List<MspFormatCompoundInformationBean> mspDB) {
            if (driftSpot == null) {
                var titleString = "Unknown"; if (peakSpot.MetaboliteName != string.Empty) titleString = peakSpot.MetaboliteName;
                titleString += "; Ms1ScanNumber: " + peakSpot.Ms1LevelDatapointNumber + "; Ms2ScanNumber: " + peakSpot.Ms2LevelDatapointNumber;

                var adductString = peakSpot.AdductIonName;
                var adduct = AdductIonParcer.GetAdductIonBean(adductString);

                sw.WriteLine("NAME: " + titleString);
                sw.WriteLine("SCANNUMBER: " + peakSpot.PeakID);
                sw.WriteLine("RETENTIONTIME: " + peakSpot.RtAtPeakTop);
                sw.WriteLine("PRECURSORMZ: " + peakSpot.AccurateMass);
                sw.WriteLine("PRECURSORTYPE: " + adductString);
                sw.WriteLine("IONMODE: " + adduct.IonMode);
                sw.WriteLine("SPECTRUMTYPE: Profile");
                sw.WriteLine("INTENSITY: " + peakSpot.IntensityAtPeakTop);
                sw.WriteLine("FORMULA: " + MspDataRetrieve.GetFormula(peakSpot.LibraryID, mspDB));
                sw.WriteLine("ONTOLOGY: " + MspDataRetrieve.GetOntology(peakSpot.LibraryID, mspDB));
                sw.WriteLine("INCHIKEY: " + MspDataRetrieve.GetInChIKey(peakSpot.LibraryID, mspDB));
                sw.WriteLine("SMILES: " + MspDataRetrieve.GetSMILES(peakSpot.LibraryID, mspDB));
            }
            else {
                var titleString = "Unknown"; if (driftSpot.MetaboliteName != string.Empty) titleString = driftSpot.MetaboliteName;
                titleString += "; Ms1ScanNumber: " + driftSpot.Ms1LevelDatapointNumber + "; Ms2ScanNumber: " + driftSpot.Ms2LevelDatapointNumber;

                var adductString = driftSpot.AdductIonName;
                var adduct = AdductIonParcer.GetAdductIonBean(adductString);

                sw.WriteLine("NAME: " + titleString);
                sw.WriteLine("SCANNUMBER: " + driftSpot.MasterPeakID);
                sw.WriteLine("RETENTIONTIME: " + peakSpot.RtAtPeakTop);
                sw.WriteLine("MOBILITY: " + driftSpot.DriftTimeAtPeakTop);
                sw.WriteLine("CCS: " + driftSpot.Ccs);
                sw.WriteLine("PRECURSORMZ: " + peakSpot.AccurateMass);
                sw.WriteLine("PRECURSORTYPE: " + adductString);
                sw.WriteLine("IONMODE: " + adduct.IonMode);
                sw.WriteLine("SPECTRUMTYPE: Profile");
                sw.WriteLine("INTENSITY: " + driftSpot.IntensityAtPeakTop);
                sw.WriteLine("FORMULA: " + MspDataRetrieve.GetFormula(driftSpot.LibraryID, mspDB));
                sw.WriteLine("ONTOLOGY: " + MspDataRetrieve.GetOntology(driftSpot.LibraryID, mspDB));
                sw.WriteLine("INCHIKEY: " + MspDataRetrieve.GetInChIKey(driftSpot.LibraryID, mspDB));
                sw.WriteLine("SMILES: " + MspDataRetrieve.GetSMILES(driftSpot.LibraryID, mspDB));
            }
        }

        #endregion

        public static void WriteDataMatrixMztabSMLData(StreamWriter sw, AlignmentPropertyBean alignedSpot,List<MspFormatCompoundInformationBean> mspDB,
            List<PostIdentificatioinReferenceBean> textDB, AnalysisParametersBean param, List<List<string>> database, string idConfidenceDefault, string idConfidenceManual)
        {
            var inchi = "null";
            var smlPrefix = "SML";
            var smlID = alignedSpot.AlignmentID;
            var smfIDrefs = alignedSpot.AlignmentID;
            var databaseIdentifier = "null";
            var chemicalFormula = "null";
            var smiles = "null";
            var chemicalName = "null";
            var chemicalNameDB = "null";

            var uri = "null";
            var reliability = "999"; // as msiLevel
            var bestIdConfidenceMeasure = "null";
            var theoreticalNeutralMass = "null";
            var textLibId = alignedSpot.PostIdentificationLibraryID;
            var libraryID = alignedSpot.LibraryID;
            var refRtString = "null";
            var refMzString = "null";

            var adductIons = alignedSpot.AdductIonName;
            if (alignedSpot.AdductIonName.Substring(alignedSpot.AdductIonName.Length - 2, 1) == "]")
            {
                adductIons = alignedSpot.AdductIonName.Substring(0, alignedSpot.AdductIonName.IndexOf("]") + 1) + alignedSpot.ChargeNumber + alignedSpot.AdductIonName.Substring(alignedSpot.AdductIonName.Length - 1, 1);
            }


            if (textLibId >= 0 && textDB != null && textDB.Count != 0)
            {
                var refRt = textDB[textLibId].RetentionTime;
                if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
                refMzString = Math.Round(textDB[textLibId].AccurateMass, 5).ToString();
                if (textDB[textLibId].MetaboliteName != null)
                {
                    chemicalName = alignedSpot.MetaboliteName;
                    chemicalNameDB = textDB[textLibId].MetaboliteName;
                }
                if (textDB[textLibId].Formula != null && textDB[textLibId].Formula.FormulaString != null && textDB[textLibId].Formula.FormulaString != string.Empty)
                    chemicalFormula = textDB[textLibId].Formula.FormulaString;
                if (textDB[textLibId].Smiles != null && textDB[textLibId].Smiles != string.Empty)
                    smiles = textDB[textLibId].Smiles;
                databaseIdentifier = database[2][1] + ": " + chemicalNameDB ;
                //reliability = getMsiLevel(alignedSpot, refRt, param).ToString();
                reliability = "annotated by user-defined text library";
                bestIdConfidenceMeasure = idConfidenceDefault;

            }
            else if (libraryID >= 0 && mspDB != null && mspDB.Count != 0)
            {
                var refRt = mspDB[libraryID].RetentionTime;
                if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
                refMzString = Math.Round(mspDB[libraryID].PrecursorMz, 5).ToString();
                chemicalName = alignedSpot.MetaboliteName;
                chemicalNameDB = mspDB[libraryID].Name;
                chemicalFormula = mspDB[libraryID].Formula;
                smiles = mspDB[libraryID].Smiles;
                if (mspDB[libraryID].CompoundClass != null && mspDB[libraryID].CompoundClass != string.Empty && chemicalName.Contains("|")) {
                    chemicalName = chemicalName.Split('|')[chemicalName.Split('|').Length - 1];
                }


                databaseIdentifier = database[0][1] + ": " + chemicalNameDB ;
                // theoreticalNeutralMass = mspDB[mspID].PrecursorMz.ToString(); //// need neutral mass. null ok
                reliability = getMsiLevel(alignedSpot, refRt, param).ToString();
                bestIdConfidenceMeasure = idConfidenceDefault;
            }

            if (idConfidenceManual !="" && alignedSpot.IsManuallyModifiedForAnnotation ==true)
            {
                bestIdConfidenceMeasure = idConfidenceManual;
            };

            var totalScore = alignedSpot.TotalSimilairty > 0 ? alignedSpot.TotalSimilairty > 1000 ? "100" : Math.Round(alignedSpot.TotalSimilairty * 0.1, 1).ToString() : "null";

            var metadata = new List<string>() {
                smlPrefix,smlID.ToString(), smfIDrefs.ToString(), databaseIdentifier,
                chemicalFormula, smiles, inchi,chemicalName, uri ,theoreticalNeutralMass,
                adductIons, reliability.ToString(),bestIdConfidenceMeasure,totalScore,
            };
            var metadata2 = new List<string>();
            foreach (string item in metadata)
            {
                var metadataMember = item;
                if (metadataMember == "")
                {
                    metadataMember = "null";
                }
                metadata2.Add(metadataMember);
            }
            sw.Write(String.Join("\t", metadata2) + "\t");

            //sw.Write(String.Join("\t", metadata) + "\t");
        }

        public static void WriteDataMatrixIonmobilityMztabSMLData(StreamWriter sw, AlignmentPropertyBean alignedSpot, AlignedDriftSpotPropertyBean driftSpot, List<MspFormatCompoundInformationBean> mspDB,
    List<PostIdentificatioinReferenceBean> textDB, AnalysisParametersBean param, List<List<string>> database, string idConfidenceDefault, string idConfidenceManual)
        {
            var id = -1;
            var libraryID = driftSpot.LibraryID;
            var textLibId = driftSpot.PostIdentificationLibraryID;
            var inchi = "null";
            var smlPrefix = "SML";
            var databaseIdentifier = "null";
            var chemicalFormula = "null";
            var smiles = "null";
            var chemicalName = "null";
            var uri = "null";
            var reliability = "999"; // as msiLevel
            var bestIdConfidenceMeasure = "null";
            var theoreticalNeutralMass = "null";
            var databesePrefix = database[0][1];
            var refRtString = "null";
            var refMzString = "null";
            var refCcsString = "null";
            var totalScore = "null";
 
            var adductIons = alignedSpot.AdductIonName;
            if (alignedSpot.AdductIonName.Substring(alignedSpot.AdductIonName.Length - 2, 1) == "]")
            {
                adductIons = alignedSpot.AdductIonName.Substring(0, alignedSpot.AdductIonName.IndexOf("]") + 1) + alignedSpot.ChargeNumber + alignedSpot.AdductIonName.Substring(alignedSpot.AdductIonName.Length - 1, 1);
            }

            var isMobility = driftSpot.CentralCcs > 0 ? true : false;
            if (isMobility)
            {
                id = driftSpot.MasterID;

                var adduct = AdductIonParcer.GetAdductIonBean(driftSpot.AdductIonName);
                //adductIons = adduct.AdductIonName.Substring(0, adduct.AdductIonName.IndexOf("]") + 1) + driftSpot.ChargeNumber + adduct.AdductIonName.Substring(adduct.AdductIonName.Length - 1,1);

                totalScore = driftSpot.TotalSimilairty > 0
                ? driftSpot.TotalSimilairty > 1000
                ? "100"
                : Math.Round(driftSpot.TotalSimilairty * 0.1, 1).ToString()
                : "null";

                if (driftSpot.MetaboliteName != string.Empty) chemicalName = driftSpot.MetaboliteName;
                if (textLibId >= 0 && textDB != null && textDB.Count != 0)
                {
                    var refRt = textDB[textLibId].RetentionTime;
                    if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
                    refMzString = Math.Round(textDB[textLibId].AccurateMass, 5).ToString();
                    if (textDB[textLibId].Formula != null && textDB[textLibId].Formula.FormulaString != null && textDB[textLibId].Formula.FormulaString != string.Empty)
                        chemicalFormula = textDB[textLibId].Formula.FormulaString;
                    if (textDB[textLibId].Smiles != null && textDB[textLibId].Smiles != string.Empty)
                        smiles = textDB[textLibId].Smiles;
                    databesePrefix = database[2][1];
                    databaseIdentifier = databesePrefix + ": " + textDB[textLibId].MetaboliteName;
                    bestIdConfidenceMeasure = idConfidenceDefault;

                    reliability = "annotated by user-defined text library";
                }
                else if (libraryID >= 0 && mspDB != null && mspDB.Count != 0)
                {
                    var refRt = mspDB[libraryID].RetentionTime;
                    if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
                    refMzString = Math.Round(mspDB[libraryID].PrecursorMz, 5).ToString();

                    var refCcs = mspDB[libraryID].CollisionCrossSection;
                    if (refCcs > 0) refCcsString = Math.Round(refCcs, 3).ToString();

                    chemicalFormula = mspDB[libraryID].Formula;
                    smiles = mspDB[libraryID].Smiles;
                    var reliabilityCalc = getMsiLevel(alignedSpot, refRt, driftSpot, refCcs, param);
                    reliability = reliabilityCalc.ToString();
                    databaseIdentifier = databesePrefix + ": " + mspDB[libraryID].Name;
                    bestIdConfidenceMeasure = idConfidenceDefault;
                    if (mspDB[libraryID].CompoundClass != null && mspDB[libraryID].CompoundClass != string.Empty && chemicalName.Contains("|")) {
                        chemicalName = chemicalName.Split('|')[chemicalName.Split('|').Length - 1];
                    }
                }
            }
            else
            {
                id = alignedSpot.MasterID;
                libraryID = alignedSpot.LibraryID;
                textLibId = alignedSpot.PostIdentificationLibraryID;

                if (alignedSpot.MetaboliteName != "" ) chemicalName = alignedSpot.MetaboliteName;
                if (textLibId >= 0 && textDB != null && textDB.Count != 0)
                {
                    var refRt = textDB[textLibId].RetentionTime;
                    if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
                    refMzString = Math.Round(textDB[textLibId].AccurateMass, 5).ToString();

                    if (textDB[textLibId].Formula != null && textDB[textLibId].Formula.FormulaString != null && textDB[textLibId].Formula.FormulaString != string.Empty)
                        chemicalFormula = textDB[textLibId].Formula.FormulaString;

                    if (textDB[textLibId].Smiles != null && textDB[textLibId].Smiles != string.Empty)
                        smiles = textDB[textLibId].Smiles;

                    databesePrefix = database[2][1];
                    databaseIdentifier = databesePrefix + ": " + textDB[textLibId].MetaboliteName;
                    bestIdConfidenceMeasure = idConfidenceDefault;
                    reliability = "annotated by user-defined text library";
                }
                else if (libraryID >= 0 && mspDB != null && mspDB.Count != 0)
                {
                    var refRt = mspDB[libraryID].RetentionTime;
                    if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
                    refMzString = Math.Round(mspDB[libraryID].PrecursorMz, 5).ToString();
                    chemicalFormula = mspDB[libraryID].Formula;
                    smiles = mspDB[libraryID].Smiles;
                    reliability = getMsiLevel(alignedSpot, refRt, param).ToString();
                    databaseIdentifier = databesePrefix + ": " + mspDB[libraryID].Name;
                    bestIdConfidenceMeasure = idConfidenceDefault;
                    if (mspDB[libraryID].CompoundClass != null && mspDB[libraryID].CompoundClass != string.Empty && chemicalName.Contains("|")) {
                        chemicalName = chemicalName.Split('|')[chemicalName.Split('|').Length - 1];
                    }
                }

                //adductIons = alignedSpot.AdductIonName.Substring(0, alignedSpot.AdductIonName.IndexOf("]") + 1) + alignedSpot.ChargeNumber + alignedSpot.AdductIonName.Substring(alignedSpot.AdductIonName.Length - 1, 1);
                totalScore = alignedSpot.TotalSimilairty > 0
                ? alignedSpot.TotalSimilairty > 1000
                ? "100"
                : Math.Round(alignedSpot.TotalSimilairty * 0.1, 1).ToString()
                : "null";
            }

            if (idConfidenceManual != "" && alignedSpot.IsManuallyModifiedForAnnotation == true)
            {
                bestIdConfidenceMeasure = idConfidenceManual;
            };

            var metadata = new List<string>() {
                smlPrefix,id.ToString(), id.ToString(), databaseIdentifier,
                chemicalFormula, smiles, inchi,chemicalName, uri ,theoreticalNeutralMass,
                adductIons, reliability.ToString(),bestIdConfidenceMeasure,totalScore,
            };
            var metadata2 = new List<string>();
            foreach (string item in metadata) 
            {
                var metadataMember = item;
                if (metadataMember == "") 
                {
                    metadataMember = "null";
                }
                metadata2.Add(metadataMember);
            }
            sw.Write(String.Join("\t", metadata2) + "\t");
         }

        public static void WriteDataMatrixMztabSMEData(StreamWriter sw, AlignmentPropertyBean alignedSpot, List<MspFormatCompoundInformationBean> mspDB,
            List<PostIdentificatioinReferenceBean> textDB,
            ProjectPropertyBean param, List<List<string>> database, string idConfidenceDefault, string idConfidenceManual, ObservableCollection<AnalysisFileBean> analysisFiles)
        {
            var smePrefix = "SME";
            var smeID = alignedSpot.AlignmentID;
            var inchi = "null";
            var uri = "null";
            var adductIons = alignedSpot.AdductIonName.Substring(0, alignedSpot.AdductIonName.IndexOf("]") + 1) + alignedSpot.ChargeNumber + alignedSpot.AdductIonName.Substring(alignedSpot.AdductIonName.Length - 1, 1);
            var expMassToCharge = alignedSpot.CentralAccurateMass.ToString(); // 
            var derivatizedForm = "null";
            var identificationMethod = idConfidenceDefault;
            var manualCurationScore = "null";
            if (idConfidenceManual != "" && alignedSpot.IsManuallyModifiedForAnnotation == true)
            {
                manualCurationScore = "100";
                identificationMethod = idConfidenceManual;
            };


            var charge = alignedSpot.ChargeNumber.ToString();

            if (param.IonMode == IonMode.Negative)
            {
                charge = "-" + alignedSpot.ChargeNumber.ToString();
            }

            var properties = alignedSpot.AlignedPeakPropertyBeanCollection;
            var repfileId = alignedSpot.RepresentativeFileID;
            //var repProperty = properties[repfileId];
            var repLibraryID = alignedSpot.LibraryID;

            var spectraRefList = new List<string>();  //  multiple files
            for (int i = 0; i < properties.Count; i++) 
            {

                if (properties[i].Variable > 0)
                {
                    //// list of m/z rt pair
                    //spectraRefList.Add("ms_run[" + (i + 1) + "]:m/z =" + alignedSpot.AlignedPeakPropertyBeanCollection[i].AccurateMass + " rt =" + alignedSpot.AlignedPeakPropertyBeanCollection[i].RetentionTime);

                    /// to get file id, peak id in aligned spots

                    var peakID = properties[i].PeakID;
                    if (peakID < 0) continue;
                    if (properties[i].LibraryID != repLibraryID || properties[i].MetaboliteName.Contains("w/o")) continue;

                    //var peakAreaBean = DataAccessLcUtility.GetPeakAreaBean(analysisFiles, i, peakID);
                    //var ms1ScanID2 = peakAreaBean.Ms1LevelDatapointNumber;
                    //var ms2ScanID2 = peakAreaBean.Ms2LevelDatapointNumber;
                    var ms1ScanID2 = properties[i].Ms1ScanNumber;
                    var ms2ScanID2 = properties[i].Ms2ScanNumber;
                    ///

                    // list of ms1 ms2 id pair
                    if (alignedSpot.IsMs2Match == true)
                    {
                        spectraRefList.Add("ms_run[" + (i + 1) + "]:ms1scanID=" + ms1ScanID2 + " ms2scanID=" + ms2ScanID2);
                    }
                    else
                    {
                        spectraRefList.Add("ms_run[" + (i + 1) + "]:ms1scanID=" + ms1ScanID2);
                    }
                }
            };

            if (spectraRefList.Count == 0)
            {
                spectraRefList.Add("ms_run[" + (repfileId + 1) + "]:ms2scanID=" + properties[repfileId].Ms2ScanNumber);
            }

            var spectraRef = string.Join("| ", spectraRefList);

            //var spectraRefNo = alignedSpot.RepresentativeFileID;  // single file 
            //var spectraRef = "ms_run[" + (spectraRefNo + 1) + "]:scan =" + spectraRefscanNo;

            var msLevel = "[MS, MS:1000511, ms level, 1]"; 
            if (alignedSpot.IsMs2Match == true)
            {
                msLevel = "[MS, MS:1000511, ms level, 2]";
            }

            var evidenceInputID = alignedSpot.AlignmentID; // need to consider
            var rank = "1"; // need to consider

            var chemicalName = "null";
            var chemicalNameDB = "null";
            var chemicalFormula = "null";
            var smiles = "null";
            double theoreticalMassToCharge =0;

            var textLibId = alignedSpot.PostIdentificationLibraryID;
            var libraryID = alignedSpot.LibraryID;
            var databesePrefix = database[0][1];

            if (textLibId >= 0 && textDB != null && textDB.Count != 0)
            {
                if (textDB[textLibId].MetaboliteName != null)
                {
                    chemicalName = alignedSpot.MetaboliteName;
                    chemicalNameDB = textDB[textLibId].MetaboliteName;
                }
                if (textDB[textLibId].Formula != null && textDB[textLibId].Formula.FormulaString != null && textDB[textLibId].Formula.FormulaString != string.Empty)
                    chemicalFormula = textDB[textLibId].Formula.FormulaString;
                if (textDB[textLibId].Smiles != null && textDB[textLibId].Smiles != string.Empty)
                    smiles = textDB[textLibId].Smiles;
                if (textDB[textLibId].AccurateMass != 0)
                    theoreticalMassToCharge = textDB[textLibId].AccurateMass;
                databesePrefix = database[2][1];
            }
            else if (libraryID >= 0 && mspDB != null && mspDB.Count != 0)
            {
                chemicalName = alignedSpot.MetaboliteName;
                chemicalNameDB = mspDB[libraryID].Name;
                chemicalFormula = mspDB[libraryID].Formula;
                smiles = mspDB[libraryID].Smiles;
                theoreticalMassToCharge = mspDB[libraryID].PrecursorMz;
                if (mspDB[libraryID].CompoundClass != null && mspDB[libraryID].CompoundClass != string.Empty && chemicalName.Contains("|")) {
                    chemicalName = chemicalName.Split('|')[chemicalName.Split('|').Length - 1];
                }
            }

            var databaseIdentifier = databesePrefix + ":" + chemicalNameDB;
            var totalScore = alignedSpot.TotalSimilairty > 0 ? alignedSpot.TotalSimilairty > 1000 ? "100" : Math.Round(alignedSpot.TotalSimilairty * 0.1, 1).ToString() : "null";
            var rtScore = alignedSpot.RetentionTimeSimilarity > 0 ? Math.Round(alignedSpot.RetentionTimeSimilarity * 0.1, 1).ToString() : "null";
            var dotProduct = alignedSpot.MassSpectraSimilarity > 0 ? Math.Round(alignedSpot.MassSpectraSimilarity * 0.1, 1).ToString() : "null";
            var revDotProd = alignedSpot.ReverseSimilarity > 0 ? Math.Round(alignedSpot.ReverseSimilarity * 0.1, 1).ToString() : "null";
            var precense = alignedSpot.FragmentPresencePercentage > 0
                ? alignedSpot.FragmentPresencePercentage > 1000
                ? "100"
                : Math.Round(alignedSpot.FragmentPresencePercentage * 0.1, 1).ToString()
                : "null";

            var dataSME01 = new List<string>() {
                    smePrefix,smeID.ToString(), evidenceInputID.ToString(), databaseIdentifier,
                    chemicalFormula, smiles, inchi, chemicalName,uri,derivatizedForm,adductIons,expMassToCharge,charge,theoreticalMassToCharge.ToString(),
                    spectraRef, identificationMethod, msLevel,
                    totalScore, rtScore, dotProduct, revDotProd, precense
                    };
            //// if manual curation score use
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
        public static void WriteDataMztab(StreamWriter sw, ObservableCollection<AlignedPeakPropertyBean> peaks, string exportType, bool replaceZeroToHalf, float nonZeroMin)
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

                if (i == peaks.Count - 1)
                    sw.Write(spotValue);
                else
                    sw.Write(spotValue + "\t");
            }
        }

        public static void WriteDataMztab(StreamWriter sw, ObservableCollection<AlignedPeakPropertyBean> peaks, string exportType, bool replaceZeroToHalf, float nonZeroMin, List<BasicStats> statsList)
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
        }
        public static void WriteDataMatrixIonmobilityMztabSMFData
            (StreamWriter sw, AlignmentPropertyBean alignedSpot, AlignedDriftSpotPropertyBean driftSpot, string ionMode)
        {
            var id = -1;
            var smfPrefix = "SMF";
            var smeIDrefs = "null";
            var smeIDrefAmbiguity_code = "null";
            var isotopomer = "null";
            var expMassToCharge = alignedSpot.CentralAccurateMass.ToString(); // OK
            var retentionTimeSeconds = alignedSpot.CentralRetentionTime * 60;
            var retentionTimeStart = (alignedSpot.CentralRetentionTime - (alignedSpot.AveragePeakWidth / 2)) * 60;
            var retentionTimeEnd = (alignedSpot.CentralRetentionTime + (alignedSpot.AveragePeakWidth / 2)) * 60;
            var isManuallyModified = "false";

            var charge = alignedSpot.ChargeNumber.ToString();

            var adductIons = alignedSpot.AdductIonName;
            if (alignedSpot.AdductIonName.Substring(alignedSpot.AdductIonName.Length - 2, 1) == "]")
            {
                adductIons = alignedSpot.AdductIonName.Substring(0, alignedSpot.AdductIonName.IndexOf("]") + 1) + alignedSpot.ChargeNumber + alignedSpot.AdductIonName.Substring(alignedSpot.AdductIonName.Length - 1, 1);
            }


            var isMobility = driftSpot.CentralCcs > 0 ? true : false;
            if (isMobility)
            {
                id = driftSpot.MasterID;

                var adduct = AdductIonParcer.GetAdductIonBean(driftSpot.AdductIonName);
                charge = driftSpot.ChargeNumber.ToString();
                if (ionMode == "Negative")
                {
                    charge = "-" + charge;
                }
                isManuallyModified = driftSpot.IsManuallyModified.ToString();

                if (driftSpot.MetaboliteName != "" && driftSpot.IsMs2Match == true)
                {
                    smeIDrefs = id.ToString();
                }
            }
            else
            {
                id = alignedSpot.MasterID;
                isManuallyModified = alignedSpot.IsManuallyModified.ToString();
                if (ionMode == "Negative")
                {
                    charge = "-" + charge;
                }

                if (alignedSpot.MetaboliteName != ""&& alignedSpot.AlignedDriftSpots[0].IsMs2Match==true)
                {
                    smeIDrefs = alignedSpot.AlignedDriftSpots[0].MasterID.ToString();
                }

                // if (alignedSpot.MetaboliteName != "" && alignedSpot.IsMs2Match == true) smeIDrefs = id.ToString();
            }

            var metadata = new List<string>() {
                    smfPrefix,id.ToString(), smeIDrefs.ToString(), smeIDrefAmbiguity_code,
                    adductIons, isotopomer, expMassToCharge, charge , retentionTimeSeconds.ToString(),retentionTimeStart.ToString(),retentionTimeEnd.ToString()
                    };

            var metadata2 = new List<string>();
            foreach (string item in metadata)
            {
                var metadataMember = item;
                if (metadataMember == "")
                {
                    metadataMember = "null";
                }
                metadata2.Add(metadataMember);
            }
            sw.Write(String.Join("\t", metadata2) + "\t");

            //sw.Write(String.Join("\t", metadata) + "\t");

        }
        public static void WriteDataMatrixIonmobilityMztabSMEData(StreamWriter sw, AlignmentPropertyBean alignedSpot, AlignedDriftSpotPropertyBean driftSpot, List<MspFormatCompoundInformationBean> mspDB,
    List<PostIdentificatioinReferenceBean> textDB,
    ProjectPropertyBean param, List<List<string>> database, string idConfidenceDefault, string idConfidenceManual, ObservableCollection<AnalysisFileBean> analysisFiles)
        {
            var smePrefix = "SME";
            var smeID = alignedSpot.AlignmentID;
            var inchi = "null";
            var uri = "null";
            var adductIons = alignedSpot.AdductIonName.Substring(0, alignedSpot.AdductIonName.IndexOf("]") + 1) + alignedSpot.ChargeNumber + alignedSpot.AdductIonName.Substring(alignedSpot.AdductIonName.Length - 1, 1);
            var expMassToCharge = alignedSpot.CentralAccurateMass.ToString(); // 
            var derivatizedForm = "null";

            var charge = alignedSpot.ChargeNumber.ToString();
            if (param.IonMode == IonMode.Negative)
            {
                charge = "-" + charge;
            }
            var msLevel = "[MS, MS:1000511, ms level, 1]";
            var chemicalName = "null";
            var chemicalNameDB = "null";
            var chemicalFormula = "null";
            var smiles = "null";
            double theoreticalMassToCharge = 0;
            var rank = "1"; // need to consider
            var totalScore = "null";
            var rtScore =  "null";
            var ccsScore = "null";
            var dotProduct =  "null";
            var revDotProd =  "null";
            var precense = "null";

            var spectraRefList = new List<string>();  
            var spectraRef = "null";

            var properties = alignedSpot.AlignedPeakPropertyBeanCollection;
            var repfileId = alignedSpot.RepresentativeFileID;
            var repProperty = properties[repfileId];

            var textLibId = alignedSpot.PostIdentificationLibraryID;
            var libraryID = alignedSpot.LibraryID;
            var databesePrefix = database[0][1];
            var identificationMethod = idConfidenceDefault;
            var manualCurationScore = "null";
            if (idConfidenceManual != "" && alignedSpot.IsManuallyModifiedForAnnotation == true)
            {
                manualCurationScore = "100";
                identificationMethod = idConfidenceManual;
            };

            var databaseIdentifier = "null";


            var isMobility = driftSpot.CentralCcs > 0 ? true : false;
            if (isMobility)
            {
                smeID = driftSpot.MasterID;
                if (driftSpot.IsMs2Match == true)
                {
                    msLevel = "[MS, MS:1000511, ms level, 2]";
                }
                charge = driftSpot.ChargeNumber.ToString();
                if (param.IonMode == IonMode.Negative)
                {
                    charge = "-" + charge;
                }

                var adduct = AdductIonParcer.GetAdductIonBean(driftSpot.AdductIonName);
                adductIons = adduct.AdductIonName.Substring(0, adduct.AdductIonName.IndexOf("]") + 1) + alignedSpot.ChargeNumber + adduct.AdductIonName.Substring(adduct.AdductIonName.Length - 1, 1);

                //properties = driftSpot.AlignedPeakPropertyBeanCollection;
                repfileId = driftSpot.RepresentativeFileID;
                repProperty = properties[repfileId];

                for (int i = 0; i < properties.Count; i++)
                {
                    if (properties[i].Variable > 0)
                    {
                        /// to get file id, peak id in aligned spots
                        libraryID = driftSpot.LibraryID;
                        textLibId = driftSpot.PostIdentificationLibraryID;

                        var peakID = properties[i].PeakID;
                        if (peakID < 0) continue;
                        if (properties[i].LibraryID != libraryID || properties[i].MetaboliteName.Contains("w/o")) continue;

                        //var peakAreaBean = DataAccessLcUtility.GetPeakAreaBean(analysisFiles, i, peakID);
                        //var ms1ScanID2 = peakAreaBean.Ms1LevelDatapointNumber;
                        var ms1ScanID2 = driftSpot.AlignedPeakPropertyBeanCollection[i].Ms1ScanNumber;
                        var ms2ScanNumber = driftSpot.AlignedPeakPropertyBeanCollection[i].Ms2ScanNumber;
                        
                        // list of ms1 ms2 id pair
                        if (driftSpot.IsMs2Match == true)
                        {
                            spectraRefList.Add("ms_run[" + (i + 1) + "]:ms1scanID=" + ms1ScanID2 + " ms2scanID=" + ms2ScanNumber);
                        }
                    }
                };

                if (spectraRefList.Count == 0)
                {
                    spectraRefList.Add("ms_run[" + (repfileId + 1) + "]:ms2scanID=" + properties[repfileId].Ms2ScanNumber);
                }

                spectraRef = string.Join("| ", spectraRefList);

                totalScore = driftSpot.TotalSimilairty > 0
                ? driftSpot.TotalSimilairty > 1000
                ? "100"
                : Math.Round(driftSpot.TotalSimilairty * 0.1, 1).ToString()
                : "null";
                rtScore = driftSpot.RetentionTimeSimilarity > 0 ? Math.Round(driftSpot.RetentionTimeSimilarity * 0.1, 1).ToString() : "null";
                ccsScore = driftSpot.CcsSimilarity > 0 ? Math.Round(driftSpot.CcsSimilarity * 0.1, 1).ToString() : "null";
                dotProduct = driftSpot.MassSpectraSimilarity > 0 ? Math.Round(driftSpot.MassSpectraSimilarity * 0.1, 1).ToString() : "null";
                revDotProd = driftSpot.ReverseSimilarity > 0 ? Math.Round(driftSpot.ReverseSimilarity * 0.1, 1).ToString() : "null";
                precense = driftSpot.FragmentPresencePercentage > 0
                ? driftSpot.FragmentPresencePercentage > 1000
                ? "100"
                : Math.Round(driftSpot.FragmentPresencePercentage * 0.1, 1).ToString()
                : "null";

                if (driftSpot.MetaboliteName != string.Empty) chemicalName = driftSpot.MetaboliteName;
                if (textLibId >= 0 && textDB != null && textDB.Count != 0)
                {
                    if (textDB[textLibId].MetaboliteName != null)
                    {
                        chemicalName = driftSpot.MetaboliteName;
                        chemicalNameDB = textDB[textLibId].MetaboliteName;
                    }
                    if (textDB[textLibId].Formula != null && textDB[textLibId].Formula.FormulaString != null && textDB[textLibId].Formula.FormulaString != string.Empty)
                        chemicalFormula = textDB[textLibId].Formula.FormulaString;
                    if (textDB[textLibId].Smiles != null && textDB[textLibId].Smiles != string.Empty)
                        smiles = textDB[textLibId].Smiles;
                    if (textDB[textLibId].AccurateMass != 0)
                        theoreticalMassToCharge = textDB[textLibId].AccurateMass;
                    databesePrefix = database[2][1];
                    databaseIdentifier = databesePrefix + ":" + chemicalNameDB;
                }
                else if (libraryID >= 0 && mspDB != null && mspDB.Count != 0)
                {
                    chemicalName = driftSpot.MetaboliteName;
                    chemicalNameDB = mspDB[libraryID].Name;
                    chemicalFormula = mspDB[libraryID].Formula;
                    smiles = mspDB[libraryID].Smiles;
                    theoreticalMassToCharge = mspDB[libraryID].PrecursorMz;
                    databaseIdentifier = databesePrefix + ":" + chemicalNameDB;
                    if (mspDB[libraryID].CompoundClass != null && mspDB[libraryID].CompoundClass != string.Empty && chemicalName.Contains("|")) {
                        chemicalName = chemicalName.Split('|')[chemicalName.Split('|').Length - 1];
                    }
                }
            }
            else
            {
                smeID = alignedSpot.MasterID;
                if (alignedSpot.IsMs2Match == true)
                {
                    msLevel = "[MS, MS:1000511, ms level, 2]";
                }

                libraryID = alignedSpot.LibraryID;
                textLibId = alignedSpot.PostIdentificationLibraryID;
                adductIons = alignedSpot.AdductIonName.Substring(0, alignedSpot.AdductIonName.IndexOf("]") + 1) + alignedSpot.ChargeNumber + alignedSpot.AdductIonName.Substring(alignedSpot.AdductIonName.Length - 1, 1);

                for (int i = 0; i < properties.Count; i++)
                {
                    if (properties[i].Variable > 0)
                    {
                        /// to get file id, peak id in aligned spots

                        var peakID = properties[i].PeakID;
                        if (peakID < 0) continue;
                        if (properties[i].LibraryID != libraryID || properties[i].MetaboliteName.Contains("w/o")) continue;

                        var ms1ScanNumber = properties[i].Ms2ScanNumber;
                        
                        spectraRefList.Add("ms_run[" + (i + 1) + "]:ms1scanID=" + ms1ScanNumber);
                    }
                };

                if (spectraRefList.Count == 0)
                {
                    spectraRefList.Add("ms_run[" + (repfileId + 1) + "]:ms2scanID=" + properties[repfileId].Ms2ScanNumber);
                }

                spectraRef = string.Join("| ", spectraRefList);

                totalScore = alignedSpot.TotalSimilairty > 0
                ? alignedSpot.TotalSimilairty > 1000
                ? "100"
                : Math.Round(alignedSpot.TotalSimilairty * 0.1, 1).ToString()
                : "null";
                rtScore = alignedSpot.RetentionTimeSimilarity > 0 ? Math.Round(alignedSpot.RetentionTimeSimilarity * 0.1, 1).ToString() : "null";
                ccsScore = alignedSpot.CcsSimilarity > 0 ? Math.Round(alignedSpot.CcsSimilarity * 0.1, 1).ToString() : "null";
                dotProduct = alignedSpot.MassSpectraSimilarity > 0 ? Math.Round(alignedSpot.MassSpectraSimilarity * 0.1, 1).ToString() : "null";
                revDotProd = alignedSpot.ReverseSimilarity > 0 ? Math.Round(alignedSpot.ReverseSimilarity * 0.1, 1).ToString() : "null";
                precense = alignedSpot.FragmentPresencePercentage > 0
                ? alignedSpot.FragmentPresencePercentage > 1000
                ? "100"
                : Math.Round(alignedSpot.FragmentPresencePercentage * 0.1, 1).ToString()
                : "null";

                if (alignedSpot.MetaboliteName != "") chemicalName = alignedSpot.MetaboliteName;
                if (textLibId >= 0 && textDB != null && textDB.Count != 0)
                {
                    if (textDB[textLibId].MetaboliteName != null)
                    {
                        chemicalName = alignedSpot.MetaboliteName;
                        chemicalNameDB = textDB[textLibId].MetaboliteName;
                    }
                    if (textDB[textLibId].Formula != null && textDB[textLibId].Formula.FormulaString != null && textDB[textLibId].Formula.FormulaString != string.Empty)
                        chemicalFormula = textDB[textLibId].Formula.FormulaString;
                    if (textDB[textLibId].Smiles != null && textDB[textLibId].Smiles != string.Empty)
                        smiles = textDB[textLibId].Smiles;
                    if (textDB[textLibId].AccurateMass != 0)
                        theoreticalMassToCharge = textDB[textLibId].AccurateMass;
                    databesePrefix = database[2][1];
                    databaseIdentifier = databesePrefix + ":" + chemicalNameDB;

                }
                else if (libraryID >= 0 && mspDB != null && mspDB.Count != 0)
                {
                    chemicalName = alignedSpot.MetaboliteName;
                    chemicalNameDB = mspDB[libraryID].Name;
                    chemicalFormula = mspDB[libraryID].Formula;
                    smiles = mspDB[libraryID].Smiles;
                    theoreticalMassToCharge = mspDB[libraryID].PrecursorMz;
                    databaseIdentifier = databesePrefix + ":" + chemicalNameDB;
                    if (mspDB[libraryID].CompoundClass != null && mspDB[libraryID].CompoundClass != string.Empty && chemicalName.Contains("|")) {
                        chemicalName = chemicalName.Split('|')[chemicalName.Split('|').Length - 1];
                    }
                }

            }

            var evidenceInputID = smeID; // need to consider

            var dataSME01 = new List<string>() {
                    smePrefix,smeID.ToString(), evidenceInputID.ToString(), databaseIdentifier,
                    chemicalFormula, smiles, inchi, chemicalName,uri,derivatizedForm,adductIons,expMassToCharge,charge,theoreticalMassToCharge.ToString(),
                    spectraRef, identificationMethod, msLevel,
                    totalScore, rtScore, ccsScore, dotProduct, revDotProd, precense
                    };
            //// if manual curation score use
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
        public static void ExportIonmobilityDtCcsData(StreamWriter sw, AlignmentPropertyBean alignedSpot, AlignedDriftSpotPropertyBean driftSpot, AnalysisParametersBean param)
        {
            var id = driftSpot.MasterID;
            var adduct = AdductIonParcer.GetAdductIonBean(driftSpot.AdductIonName);

            var detectedCount = 0.0;
            var sumDt = 0.0;
            foreach (var peak in driftSpot.AlignedPeakPropertyBeanCollection)
            {
                if (peak.Variable > 200)
                {
                    sumDt += peak.DriftTime;
                    detectedCount++;
                }
            }
            var dt = detectedCount > 0 ? sumDt / detectedCount : driftSpot.CentralDriftTime;

            var repfileid = driftSpot.RepresentativeFileID;
            var calinfo = param.FileidToCcsCalibrantData[repfileid];
            var ccs = (float)IonMobilityUtility.MobilityToCrossSection(param.IonMobilityType, dt, Math.Abs(adduct.ChargeNumber),
                alignedSpot.CentralAccurateMass, calinfo, param.IsAllCalibrantDataImported);

            sw.Write(Math.Round(dt, 4) + "\t");
            sw.Write(Math.Round(ccs, 4) + "\t");

        }

    }
}

