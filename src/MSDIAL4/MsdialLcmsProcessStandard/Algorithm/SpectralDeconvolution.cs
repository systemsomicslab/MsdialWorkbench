using CompMs.Common.DataObj;
using Msdial.Lcms.Dataprocess.Utility;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Msdial.Lcms.Dataprocess.Algorithm
{
    public sealed class SpectralDeconvolution
    {
        private SpectralDeconvolution() { }

        private const int versionNum = 6; //if you write this number, also change the version name of 'ProcessAlignmentFinalization.cs' to the same one
        
        private const double initialProgress = 30.0;
        private const double progressMax = 20.0;



        #region // writer of MS2DecResult
        /// <summary>
        /// This method performs two things:
        /// 1) MS2Dec procedure
        /// 2) Store the result of MS2Dec.
        /// The result of MS2Dec will be stored as binary. So seek pointers will be set in the header section.
        /// </summary>
        public static void WriteMS2DecResult(ObservableCollection<RawSpectrum> spectrumCollection, 
            AnalysisFilePropertyBean analysisFile, 
            ObservableCollection<PeakAreaBean> peakAreaCollection, 
            AnalysisParametersBean analysisParameters, 
            ProjectPropertyBean projectProperty, 
            DataSummaryBean dataSummary, 
            IupacReferenceBean iupac,
            Action<int> reportAction,
            System.Threading.CancellationToken token,
            int AifFlag = 0)
        {
            if (token.IsCancellationRequested)
                return;

            var totalPeakNumber = getTotalPeakNumber(peakAreaCollection, analysisParameters);
            var seekPointer = new List<long>();
            var deconvolutionFilePath = AifFlag == 0 ? analysisFile.DeconvolutionFilePath : analysisFile.DeconvolutionFilePathList[AifFlag - 1];
            using (var fs = File.Open(deconvolutionFilePath, FileMode.Create, FileAccess.ReadWrite))
            {
                //first header
                seekPointer.Add(fs.Position);
                fs.Write(BitConverter.GetBytes('V'), 0, 2);
                fs.Write(BitConverter.GetBytes(versionNum), 0, 4);

                //second header
                seekPointer.Add(fs.Position);
                fs.Write(BitConverter.GetBytes(totalPeakNumber), 0, 4);

                //third header
                var buffer = new byte[totalPeakNumber * 8];
                seekPointer.Add(fs.Position);
                fs.Write(buffer, 0, buffer.Length);

                //from forth header
                writeMS2DecResult(fs, seekPointer, spectrumCollection, peakAreaCollection, dataSummary, projectProperty, analysisParameters, iupac, reportAction, AifFlag, totalPeakNumber, token);

                //Finalize
                fs.Seek(seekPointer[2], SeekOrigin.Begin);
                for (int i = 3; i < seekPointer.Count; i++)
                    fs.Write(BitConverter.GetBytes(seekPointer[i]), 0, 8);
            }
        }

        private static int getTotalPeakNumber(ObservableCollection<PeakAreaBean> peakSpots, AnalysisParametersBean param) {
            if (param.IsIonMobility) {
                if (peakSpots.Count == 0) return 0;
                var lastSpot = peakSpots[peakSpots.Count - 1];
                var driftSpots = lastSpot.DriftSpots;
                if (driftSpots == null || driftSpots.Count == 0) return lastSpot.MasterPeakID + 1;
                else return driftSpots[driftSpots.Count - 1].MasterPeakID + 1;
            }
            else {
                return peakSpots.Count;
            }
        }

        private static void writeMS2DecResult(FileStream fs, List<long> seekPointer, 
            ObservableCollection<RawSpectrum> spectrumCollection, ObservableCollection<PeakAreaBean> peakSpots, 
            DataSummaryBean summary, ProjectPropertyBean project, 
            AnalysisParametersBean param, 
            IupacReferenceBean iupac,
            Action<int> reportAction,
            int AifFlag, int totalSpotCount, System.Threading.CancellationToken token)
        {
            var ms2DecResult = new MS2DecResult();

            double numDeconvolution = 0.0, progressMaxAif = 0.0, initialProgressAif = 0.0;
            if (project.CheckAIF) {
                numDeconvolution = (double)project.Ms2LevelIdList.Count;
                progressMaxAif = progressMax / numDeconvolution;
                initialProgressAif = initialProgress + progressMaxAif * (AifFlag - 1);
            }

            if (param.IsIonMobility) {
                // for IonMobility DIA;
                var accumulatedMs2Dic = new Rfx.Riken.OsakaUniv.IonMobility.Spectra();
                //

                foreach (var pSpot in peakSpots) {
                    //Debug.WriteLine(pSpot.MasterPeakID);
                    seekPointer.Add(fs.Position);
                    ms2DecResult = getMS2DecResultOnDataDependentAcquisition(spectrumCollection, pSpot, param, project, summary);
                    WriteDeconvolutedSpectraContents(fs, ms2DecResult);
                    progressReports(pSpot.MasterPeakID, totalSpotCount, reportAction);
                    if (pSpot.DriftSpots == null || pSpot.DriftSpots.Count == 0) continue;
                    foreach (var dSpot in pSpot.DriftSpots) {
                        //Debug.WriteLine(dSpot.MasterPeakID);
                        seekPointer.Add(fs.Position);
                        if (project.MethodType == MethodType.ddMSMS)
                            ms2DecResult = getMS2DecResultOnDriftAxisDataDependentAcquisition(spectrumCollection, dSpot, param, project, summary);
                        else
                            ms2DecResult = getMS2DecResultOnDriftAxisDataIndependentAcquisition(spectrumCollection, dSpot, pSpot, param, project, accumulatedMs2Dic, summary);
                        WriteDeconvolutedSpectraContents(fs, ms2DecResult);
                        progressReports(dSpot.MasterPeakID, totalSpotCount, reportAction);
                        if (token.IsCancellationRequested)
                            return;
                    }
                }
            }
            else {
                for (int i = 0; i < peakSpots.Count; i++) {
                    seekPointer.Add(fs.Position);
                    if (project.MethodType == MethodType.diMSMS)
                        ms2DecResult = getMS2DecResultOnDataIndependentAcquisition(spectrumCollection, peakSpots[i], param, project, summary, AifFlag);
                    else if (project.MethodType == MethodType.ddMSMS)
                        ms2DecResult = getMS2DecResultOnDataDependentAcquisition(spectrumCollection, peakSpots[i], param, project, summary);

                    deisotopingForMsmsSpectra(ms2DecResult, peakSpots[i], param, iupac);

                    WriteDeconvolutedSpectraContents(fs, ms2DecResult);
                    if (project.CheckAIF)
                        progressReports(i, totalSpotCount, initialProgressAif, progressMaxAif, reportAction);
                    else
                        progressReports(i, totalSpotCount, reportAction);

                    if (token.IsCancellationRequested)
                        return;
                }
            }
        }

        private static void deisotopingForMsmsSpectra(MS2DecResult ms2DecResult, PeakAreaBean spot, 
            AnalysisParametersBean param, IupacReferenceBean iupac) {

            if (ms2DecResult.MassSpectra.Count == 0) return;
            if (spot.ChargeNumber <= 2) return;

            var currentSpectra = ms2DecResult.MassSpectra;
            var peaks = new List<Peak>();

            foreach (var spec in currentSpectra) {
                peaks.Add(new Peak() { Mz = spec[0], Intensity = spec[1], Charge = 1, IsotopeFrag = false, Comment = "NA" });
            }

            var maxTraceNumber = 8;
            var maxCharge = spot.ChargeNumber;

            IsotopeEstimator.MsmsIsotopeRecognition(peaks, maxTraceNumber, maxCharge, param.CentroidMs2Tolerance, iupac);

            var modifiedSpectra = new List<double[]>();
            foreach (var peak in peaks) {
                var isotopeFragValue = peak.IsotopeFrag == false ? 0 : 1; // 1 means 'this peak is isotopic ion'
                var specInfo = new double[] { peak.Mz, peak.Intensity, peak.Charge, isotopeFragValue, double.Parse(peak.Comment) };
                modifiedSpectra.Add(specInfo);
            }

            ms2DecResult.MassSpectra = modifiedSpectra;
        }

        public static void WriteDeconvolutedSpectraContents(FileStream fs, MS2DecResult ms2DecResult) {
           
            //precursor properties
            fs.Write(BitConverter.GetBytes((float)ms2DecResult.PeakTopRetentionTime), 0, 4);
            fs.Write(BitConverter.GetBytes((float)ms2DecResult.Ms1AccurateMass), 0, 4);
            fs.Write(BitConverter.GetBytes((float)ms2DecResult.Ms1PeakHeight), 0, 4);
            fs.Write(BitConverter.GetBytes((float)ms2DecResult.Ms1IsotopicIonM1PeakHeight), 0, 4);
            fs.Write(BitConverter.GetBytes((float)ms2DecResult.Ms1IsotopicIonM2PeakHeight), 0, 4);

            //MS2 property
            fs.Write(BitConverter.GetBytes((float)ms2DecResult.UniqueMs), 0, 4);
            fs.Write(BitConverter.GetBytes((float)ms2DecResult.Ms2DecPeakHeight), 0, 4);
            fs.Write(BitConverter.GetBytes((float)ms2DecResult.Ms2DecPeakArea), 0, 4);

            //Spectra, base chromatogram, and unique masses
            fs.Write(BitConverter.GetBytes((int)ms2DecResult.MassSpectra.Count), 0, 4);
            fs.Write(BitConverter.GetBytes((int)ms2DecResult.BaseChromatogram.Count), 0, 4);
            fs.Write(BitConverter.GetBytes((int)ms2DecResult.ModelMasses.Count), 0, 4);

            if (ms2DecResult.MassSpectra.Count != 0)
                for (int i = 0; i < ms2DecResult.MassSpectra.Count; i++) {
                    fs.Write(BitConverter.GetBytes((float)ms2DecResult.MassSpectra[i][0]), 0, 4);
                    fs.Write(BitConverter.GetBytes((float)ms2DecResult.MassSpectra[i][1]), 0, 4);
                    if (ms2DecResult.MassSpectra[i].Length >= 5) {
                        fs.Write(BitConverter.GetBytes((int)ms2DecResult.MassSpectra[i][2]), 0, 4);
                        fs.Write(BitConverter.GetBytes((int)ms2DecResult.MassSpectra[i][3]), 0, 4);
                        fs.Write(BitConverter.GetBytes((int)ms2DecResult.MassSpectra[i][4]), 0, 4);
                    }
                    else {
                        fs.Write(BitConverter.GetBytes(0), 0, 4);
                        fs.Write(BitConverter.GetBytes(0), 0, 4);
                        fs.Write(BitConverter.GetBytes(0), 0, 4);
                    }
                }

            if (ms2DecResult.BaseChromatogram.Count != 0)
                for (int i = 0; i < ms2DecResult.BaseChromatogram.Count; i++) {
                    fs.Write(BitConverter.GetBytes((int)ms2DecResult.BaseChromatogram[i][0]), 0, 4);
                    fs.Write(BitConverter.GetBytes((float)ms2DecResult.BaseChromatogram[i][1]), 0, 4);
                    fs.Write(BitConverter.GetBytes((float)ms2DecResult.BaseChromatogram[i][2]), 0, 4);
                    fs.Write(BitConverter.GetBytes((float)ms2DecResult.BaseChromatogram[i][3]), 0, 4);
                }

            if (ms2DecResult.ModelMasses.Count != 0)
                for (int i = 0; i < ms2DecResult.ModelMasses.Count; i++) {
                    fs.Write(BitConverter.GetBytes((float)ms2DecResult.ModelMasses[i]), 0, 4);
                }
        }

        private static void progressReports(int currentProgress, int maxProgress, double initialProgressAif, double progressMaxAif, Action<int> reportAction) {
            var progress = initialProgressAif + (double)currentProgress / (double)maxProgress * progressMaxAif;
            reportAction?.Invoke((int)progress);
        }

        private static void progressReports(int currentProgress, int maxProgress, Action<int> reportAction)
        {
            var progress = initialProgress + (double)currentProgress / (double)maxProgress * progressMax;
            reportAction?.Invoke((int)progress);
        }
		#endregion

		#region // core souce code to do MS2Dec deconvolution process 
		/// <summary>
		/// This is to get the MS2Dec class variable for each detected peak in DDA (data dependent MS/MS acquisition).
		/// There is no specific source code here.
		/// Just to convert raw MS/MS spectra to MS2Dec class.
		/// </summary>
		private static MS2DecResult getMS2DecResultOnDataDependentAcquisition(ObservableCollection<RawSpectrum> spectrumCollection, 
            PeakAreaBean peakAreaBean, AnalysisParametersBean param, 
            ProjectPropertyBean projectPropertyBean, DataSummaryBean dataSummaryBean)
        {
            var ms2DecResult = new MS2DecResult();

            ms2DecResult.Ms1IsotopicIonM1PeakHeight = peakAreaBean.Ms1IsotopicIonM1PeakHeight;
            ms2DecResult.Ms1IsotopicIonM2PeakHeight = peakAreaBean.Ms1IsotopicIonM2PeakHeight;
            ms2DecResult.PeakTopRetentionTime = peakAreaBean.RtAtPeakTop;
            ms2DecResult.Ms1AccurateMass = peakAreaBean.AccurateMass;
            ms2DecResult.Ms2DecPeakArea = -1;
            ms2DecResult.Ms2DecPeakHeight = -1;
            ms2DecResult.Ms1PeakHeight = peakAreaBean.IntensityAtPeakTop;
            ms2DecResult.UniqueMs = -1;

            var peaklistList = new List<List<double[]>>();
            if (peakAreaBean.Ms2LevelDatapointNumber == -1)
            {	// no MS2 data
                ms2DecResult.MassSpectra = new List<double[]>();
                ms2DecResult.PeaklistList.Add(new List<double[]>());
            }
            else
            {	// MS2 data
                float startRt = peakAreaBean.RtAtPeakTop - (peakAreaBean.RtAtRightPeakEdge - peakAreaBean.RtAtLeftPeakEdge);
                float endRt = peakAreaBean.RtAtPeakTop + (peakAreaBean.RtAtRightPeakEdge - peakAreaBean.RtAtLeftPeakEdge);
                double precursorMz = peakAreaBean.AccurateMass;
                double productMz;

                var centroidedSpectraCollection = DataAccessLcUtility.GetCentroidMassSpectra(spectrumCollection,
                    projectPropertyBean.DataTypeMS2, peakAreaBean.Ms2LevelDatapointNumber, 
                    param.CentroidMs2Tolerance, param.PeakDetectionBasedCentroid);
                var centroidedSpectra = new List<double[]>(centroidedSpectraCollection);
                centroidedSpectra = centroidedSpectra.OrderBy(n => n[0]).ToList();

                if (centroidedSpectra.Count != 0)
                {
                    for (int i = 0; i < centroidedSpectra.Count; i++)
                    {
                        productMz = centroidedSpectra[i][0];

                        if (param.RemoveAfterPrecursor == true && peakAreaBean.AccurateMass + param.KeptIsotopeRange < productMz) continue;
                        if (param.AmplitudeCutoff > centroidedSpectra[i][1]) continue;

                        //var ms2Peaklist = DataAccessLcUtility.GetMs2Peaklist(spectrumCollection, precursorMz, productMz, startRt, endRt, param, projectPropertyBean.IonMode);
                        var ms2Peaklist = new List<double[]>() { new double[] { 0, peakAreaBean.RtAtPeakTop, productMz, centroidedSpectra[i][1] } };
                        ms2Peaklist.Insert(0, new double[] { 0, startRt, productMz, 0 });
                        ms2Peaklist.Add(new double[] { 0, endRt, productMz, 0 });
                        //ms2Peaklist = DataAccessLcUtility.GetSmoothedPeaklist(ms2Peaklist, param.SmoothingMethod, param.SmoothingLevel);
                        ms2DecResult.PeaklistList.Add(ms2Peaklist);
                        ms2DecResult.MassSpectra.Add(centroidedSpectra[i]);
                    }
                    if (ms2DecResult.MassSpectra.Count == 0) ms2DecResult = getMS2DecResultByOnlyMs1Information(peakAreaBean);
                }
                else
                {
                    ms2DecResult = getMS2DecResultByOnlyMs1Information(peakAreaBean);
                }
            }
            return ms2DecResult;
        }

        private static MS2DecResult getMS2DecResultOnDriftAxisDataDependentAcquisition(ObservableCollection<RawSpectrum> spectrumCollection,
        DriftSpotBean driftSpot, AnalysisParametersBean param,
        ProjectPropertyBean projectPropertyBean, DataSummaryBean dataSummaryBean) {
            var ms2DecResult = new MS2DecResult();

            ms2DecResult.Ms1IsotopicIonM1PeakHeight = driftSpot.Ms1IsotopicIonM1PeakHeight;
            ms2DecResult.Ms1IsotopicIonM2PeakHeight = driftSpot.Ms1IsotopicIonM2PeakHeight;
            ms2DecResult.PeakTopRetentionTime = driftSpot.DriftTimeAtPeakTop;
            ms2DecResult.Ms1AccurateMass = driftSpot.AccurateMass;
            ms2DecResult.Ms2DecPeakArea = -1;
            ms2DecResult.Ms2DecPeakHeight = -1;
            ms2DecResult.Ms1PeakHeight = driftSpot.IntensityAtPeakTop;
            ms2DecResult.UniqueMs = -1;

            var peaklistList = new List<List<double[]>>();
            if (driftSpot.Ms2LevelDatapointNumber == -1) {	// no MS2 data
                ms2DecResult.MassSpectra = new List<double[]>();
                ms2DecResult.PeaklistList.Add(new List<double[]>());
            }
            else {	// MS2 data
                float startRt = driftSpot.DriftTimeAtPeakTop - (driftSpot.DriftTimeAtRightPeakEdge - driftSpot.DriftTimeAtLeftPeakEdge);
                float endRt = driftSpot.DriftTimeAtPeakTop + (driftSpot.DriftTimeAtRightPeakEdge - driftSpot.DriftTimeAtLeftPeakEdge);
                double precursorMz = driftSpot.AccurateMass;
                double productMz;

                var centroidedSpectraCollection = DataAccessLcUtility.GetCentroidMassSpectra(spectrumCollection,
                    projectPropertyBean.DataTypeMS2, driftSpot.Ms2LevelDatapointNumber,
                    param.CentroidMs2Tolerance, param.PeakDetectionBasedCentroid);
                var centroidedSpectra = new List<double[]>(centroidedSpectraCollection);
                centroidedSpectra = centroidedSpectra.OrderBy(n => n[0]).ToList();

                if (centroidedSpectra.Count != 0) {
                    for (int i = 0; i < centroidedSpectra.Count; i++) {
                        productMz = centroidedSpectra[i][0];

                        if (param.RemoveAfterPrecursor == true && driftSpot.AccurateMass + param.KeptIsotopeRange < productMz) continue;
                        if (param.AmplitudeCutoff > centroidedSpectra[i][1]) continue;

                        //var ms2Peaklist = DataAccessLcUtility.GetMs2Peaklist(spectrumCollection, precursorMz, productMz, startRt, endRt, param, projectPropertyBean.IonMode);
                        var ms2Peaklist = new List<double[]>() { new double[] { 0, driftSpot.DriftTimeAtPeakTop, productMz, centroidedSpectra[i][1] } };
                        ms2Peaklist.Insert(0, new double[] { 0, startRt, productMz, 0 });
                        ms2Peaklist.Add(new double[] { 0, endRt, productMz, 0 });
                        //ms2Peaklist = DataAccessLcUtility.GetSmoothedPeaklist(ms2Peaklist, param.SmoothingMethod, param.SmoothingLevel);
                        ms2DecResult.PeaklistList.Add(ms2Peaklist);
                        ms2DecResult.MassSpectra.Add(centroidedSpectra[i]);
                    }
                    if (ms2DecResult.MassSpectra.Count == 0) ms2DecResult = getMS2DecResultByOnlyMs1Information(driftSpot);
                }
                else {
                    ms2DecResult = getMS2DecResultByOnlyMs1Information(driftSpot);
                }
            }
            return ms2DecResult;
        }


        private static MS2DecResult getMS2DecResultOnDriftAxisDataIndependentAcquisition(ObservableCollection<RawSpectrum> spectrumCollection,
        DriftSpotBean driftSpot, PeakAreaBean peakSpot, AnalysisParametersBean param,
        ProjectPropertyBean projectPropertyBean, Rfx.Riken.OsakaUniv.IonMobility.Spectra spectra, DataSummaryBean dataSummary)
        {
            var ms2DecResult = new MS2DecResult();

            ms2DecResult.Ms1IsotopicIonM1PeakHeight = driftSpot.Ms1IsotopicIonM1PeakHeight;
            ms2DecResult.Ms1IsotopicIonM2PeakHeight = driftSpot.Ms1IsotopicIonM2PeakHeight;
            ms2DecResult.PeakTopRetentionTime = driftSpot.DriftTimeAtPeakTop;
            ms2DecResult.Ms1AccurateMass = driftSpot.AccurateMass;
            ms2DecResult.Ms2DecPeakArea = -1;
            ms2DecResult.Ms2DecPeakHeight = -1;
            ms2DecResult.Ms1PeakHeight = driftSpot.IntensityAtPeakTop;
            ms2DecResult.UniqueMs = -1;

            if (driftSpot.Ms2LevelDatapointNumber < 0)
            {	// no MS2 data
                ms2DecResult.MassSpectra = new List<double[]>();
                ms2DecResult.PeaklistList.Add(new List<double[]>());
            }
            else
            {
                var useSnapshot = true;
                var centroidSpectrum = new List<double[]>();
                var ms2obj = spectrumCollection[driftSpot.Ms2LevelDatapointNumber];
                var isDiaPasef = Math.Max(ms2obj.Precursor.TimeEnd, ms2obj.Precursor.TimeBegin) > 0 ? true : false;
                
                if (useSnapshot)
                {
                    // MS2 data
                    peakSpot.Ms2LevelDatapointNumber = 1;
                    var spectrum = new List<double[]>();
                    foreach (var peak in ms2obj.Spectrum)
                    {
                        spectrum.Add(new double[] { peak.Mz, peak.Intensity });
                    }
                    centroidSpectrum = SpectralCentroiding.PeakDetectionBasedCentroid(spectrum);
                }
                else
                {
                    centroidSpectrum = DataAccessLcUtility.GetAccumulatedMs2Spectra(spectrumCollection, driftSpot, peakSpot, param, projectPropertyBean);
                }

                if (centroidSpectrum == null)
                {
                    ms2DecResult.MassSpectra = new List<double[]>();
                    ms2DecResult.PeaklistList.Add(new List<double[]>());
                    return ms2DecResult;
                }

                var curatedSpectra = new List<double[]>();
                for (int i = 0; i < centroidSpectrum.Count; i++)
                {
                    var productMz = centroidSpectrum[i][0];

                    if (param.RemoveAfterPrecursor == true && driftSpot.AccurateMass + param.KeptIsotopeRange < productMz) continue;
                    if (param.AmplitudeCutoff > centroidSpectrum[i][1]) continue;
                    if (centroidSpectrum[i][1] < 0.1) continue;
                    curatedSpectra.Add(centroidSpectrum[i]);
                }

                if (curatedSpectra.Count == 0)
                {
                    ms2DecResult.MassSpectra = new List<double[]>();
                    ms2DecResult.PeaklistList.Add(new List<double[]>());
                    return ms2DecResult;
                }

                if (isDiaPasef) {
                    ms2DecResult = getMS2DecResultByRawSpectrum(driftSpot, curatedSpectra);
                    if (ms2DecResult.MassSpectra.Count == 0) ms2DecResult = getMS2DecResultByOnlyMs1Information(driftSpot);
                    setDriftPropertyToMs2DecResult(ms2DecResult, driftSpot);
                    return ms2DecResult;
                }
                
                //preparing MS/MS chromatograms -> peaklistList
                var dtWidth = (driftSpot.DriftTimeAtRightPeakEdge - driftSpot.DriftTimeAtLeftPeakEdge) * 1.5;
                var minDt = driftSpot.DriftTimeAtPeakTop - dtWidth;
                var maxDt = driftSpot.DriftTimeAtPeakTop + dtWidth;

                var ms2peaklistlist = DataAccessLcUtility.GetAccumulatedMs2PeakListList(spectrumCollection, peakSpot, curatedSpectra, minDt, maxDt, projectPropertyBean.IonMode);
                if (ms2peaklistlist.Count == 0)
                {
                    ms2DecResult.MassSpectra = new List<double[]>();
                    ms2DecResult.PeaklistList.Add(new List<double[]>());
                    return ms2DecResult;
                }
                var smoothedMs2peaklistlist = new List<List<double[]>>();
                foreach (var peaklist in ms2peaklistlist)
                {
                    smoothedMs2peaklistlist.Add(DataAccessLcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel));
                }

                //Do MS2Dec deconvolution
                if (smoothedMs2peaklistlist.Count > 0)
                {
                    var focusedPeakTopScanNumber = 0;
                    var minDiff = 1000.0;
                    var minID = 0;
                    foreach (var tmpPeaklist in smoothedMs2peaklistlist[0])
                    {
                        var diff = Math.Abs(tmpPeaklist[1] - driftSpot.DriftTimeAtPeakTop);
                        if (diff < minDiff)
                        {
                            minDiff = diff;
                            minID = (int)(tmpPeaklist[0]);
                        }
                    }
                    focusedPeakTopScanNumber = minID;

                    ms2DecResult = MS2Dec.Process(smoothedMs2peaklistlist, param, dataSummary, focusedPeakTopScanNumber);
                    if (ms2DecResult == null) //if null (any pure chromatogram is not found.)
                        ms2DecResult = getMS2DecResultByRawSpectrum(driftSpot, curatedSpectra);
                    else
                    {
                        if (param.KeepOriginalPrecursorIsotopes)
                        { //replace deconvoluted precursor isotopic ions by the original precursor ions
                            replaceDeconvolutedIsopicIonsToOriginalPrecursorIons(ms2DecResult, curatedSpectra, peakSpot, param);
                        }
                    }
                }
                if (ms2DecResult.MassSpectra.Count == 0) ms2DecResult = getMS2DecResultByOnlyMs1Information(driftSpot);                                                        
            }
            setDriftPropertyToMs2DecResult(ms2DecResult, driftSpot);
            return ms2DecResult;

        }

        

        /// <summary>
        /// This is core source code of chromatogram deconvolution for DIA-MS data
        /// The result is stored in MS2Dec class.
        /// AMDIS-like mathematics is used for 'MS/MS' chromatograms of the respective precursor-window.
        /// To reduce computational time and to make the matrix calculation easy, 
        /// the retention time range to be considered is restricted to approximately 3 times of peak width of the target peak feature.
        /// </summary>
        private static MS2DecResult getMS2DecResultOnDataIndependentAcquisition(ObservableCollection<RawSpectrum> spectrumCollection, 
            PeakAreaBean peakAreaBean, AnalysisParametersBean param, ProjectPropertyBean projectProp, DataSummaryBean dataSummary, int AifFlag)
        {
            MS2DecResult ms2DecResult = null;

            //if (Math.Abs(peakAreaBean.AccurateMass - 357.12912) < 0.001 && Math.Abs(peakAreaBean.RtAtPeakTop - 8.013) < 0.01) {
            //    Console.WriteLine();
            //}

            //first, the MS/MS spectrum at the scan point of peak top is stored.
            var centroidedSpectra = DataAccessLcUtility.GetCentroidMassSpectra(spectrumCollection, projectProp.DataTypeMS2, peakAreaBean.Ms2LevelDatapointNumber, param.CentroidMs2Tolerance, param.PeakDetectionBasedCentroid);
            var peaklistList = new List<List<double[]>>();

            int experimentCycleNumber = projectProp.ExperimentID_AnalystExperimentInformationBean.Count;
            int ms1LevelId = 0, ms2LevelId = 0;

			//check the RT range to be considered for chromatogram deconvolution
            var startRt = peakAreaBean.RtAtPeakTop - (peakAreaBean.RtAtRightPeakEdge - peakAreaBean.RtAtLeftPeakEdge) * 1.5F;
            var endRt = peakAreaBean.RtAtPeakTop + (peakAreaBean.RtAtRightPeakEdge - peakAreaBean.RtAtLeftPeakEdge) * 1.5F;

			//preparing MS1 and MS/MS chromatograms
			//note that the MS1 chromatogram trace (i.e. EIC) is also used as the candidate of model chromatogram
            var precursorMz = peakAreaBean.AccurateMass;
            var ms1Peaklist = DataAccessLcUtility.GetMs1Peaklist(spectrumCollection, projectProp, (float)precursorMz, param.CentroidMs1Tolerance, startRt, endRt);

            var startScanNum = (int)ms1Peaklist[0][0];
            var endScanNum = (int)ms1Peaklist[ms1Peaklist.Count - 1][0];
            var minimumDiff = double.MaxValue;
            var minimumID = (int)(ms1Peaklist.Count / 2);

			// Define the scan number of peak top in the array of MS1 chromatogram restricted by the retention time range
            for (int i = 0; i < ms1Peaklist.Count; i++)
                if (Math.Abs(ms1Peaklist[i][1] - peakAreaBean.RtAtPeakTop) < minimumDiff) {
                    minimumDiff = Math.Abs(ms1Peaklist[i][1] - peakAreaBean.RtAtPeakTop);
                    minimumID = i;
                }
            int focusedPeakTopScanNumber = minimumID;

            //get scan dictionary ID for MS1 and MS2
            foreach (var value in projectProp.ExperimentID_AnalystExperimentInformationBean) { 
				if (value.Value.MsType == MsType.SCAN) {
					ms1LevelId = value.Key; 
					break; 
				} 
			}
            if (AifFlag == 0) {
                foreach (var value in projectProp.ExperimentID_AnalystExperimentInformationBean) {
                    if (value.Value.MsType == MsType.SWATH && value.Value.StartMz < precursorMz && precursorMz <= value.Value.EndMz) {
                        ms2LevelId = value.Key;
                        break;
                    }
                }
            }
            else {
                ms2LevelId = projectProp.Ms2LevelIdList[AifFlag - 1];
            }
            if (centroidedSpectra.Count != 0) {
                var curatedSpectra = new List<double[]>(); // used for normalization of MS/MS intensities
                for (int i = 0; i < centroidedSpectra.Count; i++) { //preparing MS/MS chromatograms -> peaklistList
                    var productMz = centroidedSpectra[i][0];

                    if (param.RemoveAfterPrecursor == true && peakAreaBean.AccurateMass + param.KeptIsotopeRange < productMz) continue;
                    if (param.AmplitudeCutoff > centroidedSpectra[i][1]) continue;                    
                    curatedSpectra.Add(new double[] { centroidedSpectra[i][0], centroidedSpectra[i][1] });
                }

                var ms2Peaklistlist = DataAccessLcUtility.GetMs2Peaklistlist(spectrumCollection, ms1LevelId, ms2LevelId, experimentCycleNumber, startScanNum, endScanNum, curatedSpectra.Select(x => x[0]).ToList(), param);
                for (var i = 0; i < curatedSpectra.Count; i++)
                {
                    peaklistList.Add(DataAccessLcUtility.GetSmoothedPeaklist(ms2Peaklistlist[i], param.SmoothingMethod, param.SmoothingLevel));
                }

                //Do MS2Dec deconvolution
                if (peaklistList.Count > 0) {
                    ms2DecResult = MS2Dec.Process(peaklistList, param, dataSummary, focusedPeakTopScanNumber);
					if (ms2DecResult == null) //if null (any pure chromatogram is not found.)
                        ms2DecResult = getMS2DecResultByRawSpectrum(peakAreaBean, peaklistList, focusedPeakTopScanNumber, curatedSpectra);  
                    else {
                        if (param.KeepOriginalPrecursorIsotopes) { //replace deconvoluted precursor isotopic ions by the original precursor ions
                            replaceDeconvolutedIsopicIonsToOriginalPrecursorIons(ms2DecResult, curatedSpectra, peakAreaBean, param);
                        }
                    }
                }
            }

            if (ms2DecResult == null) ms2DecResult = getMS2DecResultByOnlyMs1Information(peakAreaBean);
            else setPeakAreaInformationToMs2DecResult(ms2DecResult, peakAreaBean);

            return ms2DecResult;
        }

        private static void replaceDeconvolutedIsopicIonsToOriginalPrecursorIons(MS2DecResult ms2DecResult, List<double[]> curatedSpectra,
            PeakAreaBean peakAreaBean, AnalysisParametersBean param) {
            var isotopicRange = param.KeptIsotopeRange;
            var replacedSpectrum = new List<double[]>();

            foreach (var spec in ms2DecResult.MassSpectra) {
                if (spec[0] < peakAreaBean.AccurateMass - param.CentroidMs2Tolerance)
                    replacedSpectrum.Add(spec);
            }

            foreach (var spec in curatedSpectra) {
                if (spec[0] >= peakAreaBean.AccurateMass - param.CentroidMs2Tolerance)
                    replacedSpectrum.Add(spec);
            }

            replacedSpectrum = replacedSpectrum.OrderBy(n => n[0]).ToList();
            ms2DecResult.MassSpectra = replacedSpectrum;
        }

        private static MS2DecResult getMS2DecResultByRawSpectrum(PeakAreaBean peakAreaBean, List<List<double[]>> peaklistList, int focusedPeakTopScanNumber, List<double[]> spectra) {
            
            var ms2DecResult = new MS2DecResult();

            ms2DecResult.Ms1IsotopicIonM1PeakHeight = peakAreaBean.Ms1IsotopicIonM1PeakHeight;
            ms2DecResult.Ms1IsotopicIonM2PeakHeight = peakAreaBean.Ms1IsotopicIonM2PeakHeight;
            ms2DecResult.PeakTopRetentionTime = peakAreaBean.RtAtPeakTop;
            ms2DecResult.Ms1AccurateMass = peakAreaBean.AccurateMass;
            ms2DecResult.Ms1PeakHeight = peakAreaBean.IntensityAtPeakTop;
            ms2DecResult.UniqueMs = -1;
            ms2DecResult.Ms2DecPeakArea = -1;
            ms2DecResult.Ms2DecPeakHeight = -1;
            ms2DecResult.MassSpectra = spectra;
            ms2DecResult.ModelMasses = new List<float>();

            ms2DecResult.BaseChromatogram = new List<double[]>() {
                new double[]{ peaklistList[0][focusedPeakTopScanNumber][0], peaklistList[0][focusedPeakTopScanNumber][1], peaklistList[0][focusedPeakTopScanNumber][2], peaklistList[0][focusedPeakTopScanNumber][3] }
            };
            ms2DecResult.PeaklistList.Add(ms2DecResult.BaseChromatogram);

            return ms2DecResult;
        }

        private static MS2DecResult getMS2DecResultByRawSpectrum(DriftSpotBean driftSpot, List<double[]> spectra)
        {

            var ms2DecResult = new MS2DecResult();
            float startDriftTime = driftSpot.DriftTimeAtLeftPeakEdge - (driftSpot.DriftTimeAtPeakTop - driftSpot.DriftTimeAtLeftPeakEdge) * 0.5f;
            float endDriftTime = driftSpot.DriftTimeAtRightPeakEdge + (driftSpot.DriftTimeAtRightPeakEdge - driftSpot.DriftTimeAtPeakTop) * 0.5f;

            ms2DecResult.Ms1IsotopicIonM1PeakHeight = driftSpot.Ms1IsotopicIonM1PeakHeight;
            ms2DecResult.Ms1IsotopicIonM2PeakHeight = driftSpot.Ms1IsotopicIonM2PeakHeight;
            ms2DecResult.PeakTopRetentionTime = driftSpot.DriftTimeAtPeakTop;
            ms2DecResult.Ms1AccurateMass = driftSpot.AccurateMass;
            ms2DecResult.Ms1PeakHeight = driftSpot.IntensityAtPeakTop;
            ms2DecResult.UniqueMs = -1;
            ms2DecResult.Ms2DecPeakArea = -1;
            ms2DecResult.Ms2DecPeakHeight = -1;
            ms2DecResult.MassSpectra = spectra;
            ms2DecResult.ModelMasses = new List<float>();

             for (int i = 0; i < spectra.Count; i++)
            {
                var productMz = spectra[i][0];
                var ms2Peaklist = new List<double[]>() { new double[] { 0, driftSpot.DriftTimeAtPeakTop, productMz, spectra[i][1] } };
                ms2Peaklist.Insert(0, new double[] { 0, startDriftTime, productMz, 0 });
                ms2Peaklist.Add(new double[] { 0, endDriftTime, productMz, 0 });
                ms2DecResult.PeaklistList.Add(ms2Peaklist);            
            };

            return ms2DecResult;
        }


        private static MS2DecResult getMS2DecResultByOnlyMs1Information(PeakAreaBean peakSpot) {
            var ms2DecResult = new MS2DecResult();

            ms2DecResult.Ms1IsotopicIonM1PeakHeight = peakSpot.Ms1IsotopicIonM1PeakHeight;
            ms2DecResult.Ms1IsotopicIonM2PeakHeight = peakSpot.Ms1IsotopicIonM2PeakHeight;
            ms2DecResult.PeakTopRetentionTime = peakSpot.RtAtPeakTop;
            ms2DecResult.Ms1AccurateMass = peakSpot.AccurateMass;
            ms2DecResult.Ms1PeakHeight = peakSpot.IntensityAtPeakTop;
            ms2DecResult.UniqueMs = -1;
            ms2DecResult.Ms2DecPeakArea = -1;
            ms2DecResult.Ms2DecPeakHeight = -1;
            ms2DecResult.MassSpectra = new List<double[]>();
            ms2DecResult.BaseChromatogram = new List<double[]>();
            ms2DecResult.ModelMasses = new List<float>();
            ms2DecResult.PeaklistList.Add(new List<double[]>());

            return ms2DecResult;
        }

        private static MS2DecResult getMS2DecResultByOnlyMs1Information(DriftSpotBean driftSpot) {
            var ms2DecResult = new MS2DecResult();

            ms2DecResult.Ms1IsotopicIonM1PeakHeight = driftSpot.Ms1IsotopicIonM1PeakHeight;
            ms2DecResult.Ms1IsotopicIonM2PeakHeight = driftSpot.Ms1IsotopicIonM2PeakHeight;
            ms2DecResult.PeakTopRetentionTime = driftSpot.DriftTimeAtPeakTop;
            ms2DecResult.Ms1AccurateMass = driftSpot.AccurateMass;
            ms2DecResult.Ms1PeakHeight = driftSpot.IntensityAtPeakTop;
            ms2DecResult.UniqueMs = -1;
            ms2DecResult.Ms2DecPeakArea = -1;
            ms2DecResult.Ms2DecPeakHeight = -1;
            ms2DecResult.MassSpectra = new List<double[]>();
            ms2DecResult.BaseChromatogram = new List<double[]>();
            ms2DecResult.ModelMasses = new List<float>();
            ms2DecResult.PeaklistList.Add(new List<double[]>());

            return ms2DecResult;
        }

        private static void setPeakAreaInformationToMs2DecResult(MS2DecResult ms2DecResult, PeakAreaBean peakAreaBean)
        {
            ms2DecResult.PeakTopRetentionTime = peakAreaBean.RtAtPeakTop;
            ms2DecResult.Ms1AccurateMass = peakAreaBean.AccurateMass;
            ms2DecResult.Ms1PeakHeight = peakAreaBean.IntensityAtPeakTop;
            ms2DecResult.Ms1IsotopicIonM1PeakHeight = peakAreaBean.Ms1IsotopicIonM1PeakHeight;
            ms2DecResult.Ms1IsotopicIonM2PeakHeight = peakAreaBean.Ms1IsotopicIonM2PeakHeight;
        }

        private static void setDriftPropertyToMs2DecResult(MS2DecResult ms2DecResult, DriftSpotBean driftSpot) {
            ms2DecResult.PeakTopRetentionTime = driftSpot.DriftTimeAtPeakTop;
            ms2DecResult.Ms1AccurateMass = driftSpot.AccurateMass;
            ms2DecResult.Ms1PeakHeight = driftSpot.IntensityAtPeakTop;
            ms2DecResult.Ms1IsotopicIonM1PeakHeight = driftSpot.Ms1IsotopicIonM1PeakHeight;
            ms2DecResult.Ms1IsotopicIonM2PeakHeight = driftSpot.Ms1IsotopicIonM2PeakHeight;
        }

        #endregion

        #region // reader of MS2DecResult
        public static MS2DecResult ReadMS2DecResult(ObservableCollection<AnalysisFileBean> analysisFileBeanCollection, AlignmentPropertyBean alignmentPropertyBean) {
            int fileID = alignmentPropertyBean.RepresentativeFileID;
            int peakID = alignmentPropertyBean.AlignedPeakPropertyBeanCollection[fileID].PeakID;
            var deconvolutionResultBean = new MS2DecResult();

            using (FileStream fs = File.Open(analysisFileBeanCollection[fileID].AnalysisFilePropertyBean.DeconvolutionFilePath, FileMode.Open, FileAccess.ReadWrite)) {
                var seekpointList = ReadSeekPointsOfMS2DecResultFile(fs);
                deconvolutionResultBean = ReadMS2DecResult(fs, seekpointList, peakID);
            }

            return deconvolutionResultBean;
        }

        public static MS2DecResult ReadMS2DecResult(FileStream fs, List<long> seekpointList, int peakID) {
            var buffer = new byte[6];
            fs.Seek(0, SeekOrigin.Begin);
            fs.Read(buffer, 0, 6);

            if (BitConverter.ToChar(buffer, 0).Equals('V') && BitConverter.ToInt32(buffer, 2) == 2) {
                return getDeconvolutionResultBeanVer2(fs, seekpointList, peakID);
            }
            else if (BitConverter.ToChar(buffer, 0).Equals('V') && BitConverter.ToInt32(buffer, 2) == 3) {
                return getDeconvolutionResultBeanVer3(fs, seekpointList, peakID);
            }
            else if (BitConverter.ToChar(buffer, 0).Equals('V') && BitConverter.ToInt32(buffer, 2) == 4) {
                return getDeconvolutionResultBeanVer4(fs, seekpointList, peakID);
            }
            else if (BitConverter.ToChar(buffer, 0).Equals('V') && BitConverter.ToInt32(buffer, 2) == 5) {
                return getDeconvolutionResultBeanVer5(fs, seekpointList, peakID);
            }
            else if (BitConverter.ToChar(buffer, 0).Equals('V') && BitConverter.ToInt32(buffer, 2) == 6) {
                return getDeconvolutionResultBeanVer6(fs, seekpointList, peakID);
            }
            else {
                return getDeconvolutionResultBeanVer1(fs, seekpointList, peakID);
            }
        }

       
        private static MS2DecResult getDeconvolutionResultBeanVer1(FileStream fs, List<long> seekpointList, int peakID) {
            var deconvolutionResultBean = new MS2DecResult();
            fs.Seek(seekpointList[peakID], SeekOrigin.Begin);

            var buffer = new byte[28];
            fs.Read(buffer, 0, buffer.Length);

            float peakTopRetentionTime = BitConverter.ToSingle(buffer, 0);
            float ms1AccurateMass = BitConverter.ToSingle(buffer, 4);
            float uniqueMs = BitConverter.ToSingle(buffer, 8);
            int ms1PeakHeight = BitConverter.ToInt32(buffer, 12);
            int peakArea = BitConverter.ToInt32(buffer, 16);
            int spectraNumber = BitConverter.ToInt32(buffer, 20);
            int datapointNumber = BitConverter.ToInt32(buffer, 24);

            deconvolutionResultBean.PeakTopRetentionTime = peakTopRetentionTime;
            deconvolutionResultBean.Ms1AccurateMass = ms1AccurateMass;
            deconvolutionResultBean.UniqueMs = uniqueMs;
            deconvolutionResultBean.Ms1PeakHeight = ms1PeakHeight;
            deconvolutionResultBean.Ms2DecPeakArea = peakArea;

            if (spectraNumber == 0) {
                deconvolutionResultBean.MassSpectra = new List<double[]>();
                deconvolutionResultBean.PeaklistList.Add(new List<double[]>());
                return deconvolutionResultBean;
            }

            buffer = new byte[spectraNumber * 8];
            fs.Read(buffer, 0, buffer.Length);
            for (int i = 0; i < spectraNumber; i++)
                deconvolutionResultBean.MassSpectra.Add(new double[] { BitConverter.ToSingle(buffer, 8 * i), BitConverter.ToInt32(buffer, 8 * i + 4) });

            List<double[]> peaklist;
            for (int i = 0; i < spectraNumber; i++) {
                peaklist = new List<double[]>();
                buffer = new byte[datapointNumber * 16];
                fs.Read(buffer, 0, buffer.Length);
                for (int j = 0; j < datapointNumber; j++)
                    peaklist.Add(new double[] { BitConverter.ToInt32(buffer, 16 * j), BitConverter.ToSingle(buffer, 16 * j + 4), BitConverter.ToSingle(buffer, 16 * j + 8), BitConverter.ToInt32(buffer, 16 * j + 12) });
                deconvolutionResultBean.PeaklistList.Add(peaklist);
            }

            return deconvolutionResultBean;
        }

        private static MS2DecResult getDeconvolutionResultBeanVer2(FileStream fs, List<long> seekpointList, int peakID) {
            var deconvolutionResultBean = new MS2DecResult();
            fs.Seek(seekpointList[peakID], SeekOrigin.Begin);

            var buffer = new byte[28];
            fs.Read(buffer, 0, buffer.Length);

            float peakTopRetentionTime = BitConverter.ToSingle(buffer, 0);
            float ms1AccurateMass = BitConverter.ToSingle(buffer, 4);
            float uniqueMs = BitConverter.ToSingle(buffer, 8);
            int ms1PeakHeight = BitConverter.ToInt32(buffer, 12);
            int peakArea = BitConverter.ToInt32(buffer, 16);
            int spectraNumber = BitConverter.ToInt32(buffer, 20);
            int datapointNumber = BitConverter.ToInt32(buffer, 24);

            deconvolutionResultBean.PeakTopRetentionTime = peakTopRetentionTime;
            deconvolutionResultBean.Ms1AccurateMass = ms1AccurateMass;
            deconvolutionResultBean.UniqueMs = uniqueMs;
            deconvolutionResultBean.Ms1PeakHeight = ms1PeakHeight;
            deconvolutionResultBean.Ms2DecPeakArea = peakArea;

            if (spectraNumber == 0) {
                deconvolutionResultBean.MassSpectra = new List<double[]>();
                deconvolutionResultBean.PeaklistList.Add(new List<double[]>());
                return deconvolutionResultBean;
            }

            double maxIntensity = double.MinValue;

            buffer = new byte[spectraNumber * 8];
            fs.Read(buffer, 0, buffer.Length);
            for (int i = 0; i < spectraNumber; i++) {
                deconvolutionResultBean.MassSpectra.Add(new double[] { BitConverter.ToSingle(buffer, 8 * i), BitConverter.ToInt32(buffer, 8 * i + 4) });
                if (maxIntensity < deconvolutionResultBean.MassSpectra[i][1]) maxIntensity = deconvolutionResultBean.MassSpectra[i][1];
            }

            if (datapointNumber == 0) {
                deconvolutionResultBean.PeaklistList.Add(new List<double[]>());
                return deconvolutionResultBean;
            }

            var basePeaklist = new List<double[]>();
            buffer = new byte[datapointNumber * 16];
            fs.Read(buffer, 0, buffer.Length);

            for (int i = 0; i < datapointNumber; i++)
                basePeaklist.Add(new double[] { BitConverter.ToInt32(buffer, 16 * i), BitConverter.ToSingle(buffer, 16 * i + 4), BitConverter.ToSingle(buffer, 16 * i + 8), BitConverter.ToInt32(buffer, 16 * i + 12) });

            var peaklist = new List<double[]>();
            for (int i = 0; i < spectraNumber; i++) {
                peaklist = new List<double[]>();
                for (int j = 0; j < basePeaklist.Count; j++)
                    peaklist.Add(new double[] { basePeaklist[j][0], basePeaklist[j][1], deconvolutionResultBean.MassSpectra[i][0], basePeaklist[j][3] * deconvolutionResultBean.MassSpectra[i][1] / maxIntensity });

                deconvolutionResultBean.PeaklistList.Add(peaklist);
            }

            return deconvolutionResultBean;
        }

        private static MS2DecResult getDeconvolutionResultBeanVer3(FileStream fs, List<long> seekpointList, int peakID) {
            var deconvolutionResultBean = new MS2DecResult();
            fs.Seek(seekpointList[peakID], SeekOrigin.Begin);

            var buffer = new byte[28];
            fs.Read(buffer, 0, buffer.Length);

            float peakTopRetentionTime = BitConverter.ToSingle(buffer, 0);
            float ms1AccurateMass = BitConverter.ToSingle(buffer, 4);
            float uniqueMs = BitConverter.ToSingle(buffer, 8);
            float ms1PeakHeight = BitConverter.ToSingle(buffer, 12);
            float peakArea = BitConverter.ToSingle(buffer, 16);
            int spectraNumber = BitConverter.ToInt32(buffer, 20);
            int datapointNumber = BitConverter.ToInt32(buffer, 24);

            deconvolutionResultBean.PeakTopRetentionTime = peakTopRetentionTime;
            deconvolutionResultBean.Ms1AccurateMass = ms1AccurateMass;
            deconvolutionResultBean.UniqueMs = uniqueMs;
            deconvolutionResultBean.Ms1PeakHeight = ms1PeakHeight;
            deconvolutionResultBean.Ms2DecPeakArea = peakArea;

            if (spectraNumber == 0) {
                deconvolutionResultBean.MassSpectra = new List<double[]>();
                deconvolutionResultBean.PeaklistList.Add(new List<double[]>());
                return deconvolutionResultBean;
            }

            double maxIntensity = double.MinValue;

            buffer = new byte[spectraNumber * 8];
            fs.Read(buffer, 0, buffer.Length);
            for (int i = 0; i < spectraNumber; i++) {
                deconvolutionResultBean.MassSpectra.Add(new double[] { BitConverter.ToSingle(buffer, 8 * i), BitConverter.ToSingle(buffer, 8 * i + 4) });
                if (maxIntensity < deconvolutionResultBean.MassSpectra[i][1]) maxIntensity = deconvolutionResultBean.MassSpectra[i][1];
            }

            if (datapointNumber == 0) {
                deconvolutionResultBean.PeaklistList.Add(new List<double[]>());
                return deconvolutionResultBean;
            }

            var basePeaklist = new List<double[]>();
            buffer = new byte[datapointNumber * 16];
            fs.Read(buffer, 0, buffer.Length);

            for (int i = 0; i < datapointNumber; i++)
                basePeaklist.Add(new double[] { BitConverter.ToInt32(buffer, 16 * i), BitConverter.ToSingle(buffer, 16 * i + 4), BitConverter.ToSingle(buffer, 16 * i + 8), BitConverter.ToSingle(buffer, 16 * i + 12) });

            var peaklist = new List<double[]>();
            for (int i = 0; i < spectraNumber; i++) {
                peaklist = new List<double[]>();
                for (int j = 0; j < basePeaklist.Count; j++)
                    peaklist.Add(new double[] { basePeaklist[j][0], basePeaklist[j][1], deconvolutionResultBean.MassSpectra[i][0], basePeaklist[j][3] * deconvolutionResultBean.MassSpectra[i][1] / maxIntensity });

                deconvolutionResultBean.PeaklistList.Add(peaklist);
            }

            return deconvolutionResultBean;
        }

        private static MS2DecResult getDeconvolutionResultBeanVer4(FileStream fs, List<long> seekpointList, int peakID) {
            var deconvolutionResultBean = new MS2DecResult();
            fs.Seek(seekpointList[peakID], SeekOrigin.Begin);

            var buffer = new byte[36];
            fs.Read(buffer, 0, buffer.Length);

            float peakTopRetentionTime = BitConverter.ToSingle(buffer, 0);
            float ms1AccurateMass = BitConverter.ToSingle(buffer, 4);
            float uniqueMs = BitConverter.ToSingle(buffer, 8);
            float ms1PeakHeight = BitConverter.ToSingle(buffer, 12);
            float ms1IsotopicIonM1PeakHeight = BitConverter.ToSingle(buffer, 16);
            float ms1IsotopicIonM2PeakHeight = BitConverter.ToSingle(buffer, 20);
            float peakArea = BitConverter.ToSingle(buffer, 24);
            int spectraNumber = BitConverter.ToInt32(buffer, 28);
            int datapointNumber = BitConverter.ToInt32(buffer, 32);

            deconvolutionResultBean.PeakTopRetentionTime = peakTopRetentionTime;
            deconvolutionResultBean.Ms1AccurateMass = ms1AccurateMass;
            deconvolutionResultBean.UniqueMs = uniqueMs;
            deconvolutionResultBean.Ms1PeakHeight = ms1PeakHeight;
            deconvolutionResultBean.Ms1IsotopicIonM1PeakHeight = ms1IsotopicIonM1PeakHeight;
            deconvolutionResultBean.Ms1IsotopicIonM2PeakHeight = ms1IsotopicIonM2PeakHeight;
            deconvolutionResultBean.Ms2DecPeakArea = peakArea;

            if (spectraNumber == 0) {
                deconvolutionResultBean.MassSpectra = new List<double[]>();
                deconvolutionResultBean.PeaklistList.Add(new List<double[]>());
                return deconvolutionResultBean;
            }

            double maxIntensity = double.MinValue;

            buffer = new byte[spectraNumber * 8];
            fs.Read(buffer, 0, buffer.Length);
            for (int i = 0; i < spectraNumber; i++) {
                deconvolutionResultBean.MassSpectra.Add(new double[] { BitConverter.ToSingle(buffer, 8 * i), BitConverter.ToSingle(buffer, 8 * i + 4) });
                if (maxIntensity < deconvolutionResultBean.MassSpectra[i][1]) maxIntensity = deconvolutionResultBean.MassSpectra[i][1];
            }

            if (datapointNumber == 0) {
                deconvolutionResultBean.PeaklistList.Add(new List<double[]>());
                return deconvolutionResultBean;
            }

            var basePeaklist = new List<double[]>();
            buffer = new byte[datapointNumber * 16];
            fs.Read(buffer, 0, buffer.Length);

            for (int i = 0; i < datapointNumber; i++)
                basePeaklist.Add(new double[] { BitConverter.ToInt32(buffer, 16 * i), BitConverter.ToSingle(buffer, 16 * i + 4), BitConverter.ToSingle(buffer, 16 * i + 8), BitConverter.ToSingle(buffer, 16 * i + 12) });

            var peaklist = new List<double[]>();
            for (int i = 0; i < spectraNumber; i++) {
                peaklist = new List<double[]>();
                for (int j = 0; j < basePeaklist.Count; j++)
                    peaklist.Add(new double[] { basePeaklist[j][0], basePeaklist[j][1], deconvolutionResultBean.MassSpectra[i][0], basePeaklist[j][3] * deconvolutionResultBean.MassSpectra[i][1] / maxIntensity });

                deconvolutionResultBean.PeaklistList.Add(peaklist);
            }

            return deconvolutionResultBean;
        }

        private static MS2DecResult getDeconvolutionResultBeanVer5(FileStream fs, List<long> seekpointList, int peakID) {
            var ms2DecResult = new MS2DecResult();
            fs.Seek(seekpointList[peakID], SeekOrigin.Begin); // go to the target seek point

            var buffer = new byte[44];
            fs.Read(buffer, 0, buffer.Length); // store the meta field buffer

            //precursor properties
            ms2DecResult.PeakTopRetentionTime = BitConverter.ToSingle(buffer, 0); ;
            ms2DecResult.Ms1AccurateMass = BitConverter.ToSingle(buffer, 4);
            ms2DecResult.Ms1PeakHeight = BitConverter.ToSingle(buffer, 8);
            ms2DecResult.Ms1IsotopicIonM1PeakHeight = BitConverter.ToSingle(buffer, 12);
            ms2DecResult.Ms1IsotopicIonM2PeakHeight = BitConverter.ToSingle(buffer, 16);

            //MS2 properties
            ms2DecResult.UniqueMs = BitConverter.ToSingle(buffer, 20);
            ms2DecResult.Ms2DecPeakHeight = BitConverter.ToSingle(buffer, 24);
            ms2DecResult.Ms2DecPeakArea = BitConverter.ToSingle(buffer, 28); ;

            //Spectra, base chromatogram, unique masses
            var spectraNumber = BitConverter.ToInt32(buffer, 32);
            var datapointNumber = BitConverter.ToInt32(buffer, 36);
            var modelMasses = BitConverter.ToInt32(buffer, 40);

            if (spectraNumber != 0) {
                buffer = new byte[spectraNumber * 8];
                fs.Read(buffer, 0, buffer.Length);
                for (int i = 0; i < spectraNumber; i++) {
                    ms2DecResult.MassSpectra.Add(new double[] { BitConverter.ToSingle(buffer, 8 * i), BitConverter.ToSingle(buffer, 8 * i + 4) });
                }
            }

            if (datapointNumber != 0) {
                var basePeaklist = new List<double[]>();
                buffer = new byte[datapointNumber * 16];
                fs.Read(buffer, 0, buffer.Length);

                for (int i = 0; i < datapointNumber; i++)
                    basePeaklist.Add(new double[] { BitConverter.ToInt32(buffer, 16 * i), BitConverter.ToSingle(buffer, 16 * i + 4), BitConverter.ToSingle(buffer, 16 * i + 8), BitConverter.ToSingle(buffer, 16 * i + 12) });

                var peaklist = new List<double[]>();
                var maxIntensity = basePeaklist.Max(n => n[3]);
                for (int i = 0; i < spectraNumber; i++) {
                    peaklist = new List<double[]>();
                    for (int j = 0; j < basePeaklist.Count; j++)
                        peaklist.Add(new double[] { basePeaklist[j][0], basePeaklist[j][1], ms2DecResult.MassSpectra[i][0], basePeaklist[j][3] * ms2DecResult.MassSpectra[i][1] / maxIntensity });

                    ms2DecResult.PeaklistList.Add(peaklist);
                }
            }

            if (modelMasses != 0) {
                buffer = new byte[modelMasses * 4];
                fs.Read(buffer, 0, buffer.Length);
                for (int i = 0; i < modelMasses; i++) {
                    ms2DecResult.ModelMasses.Add(BitConverter.ToSingle(buffer, 4 * i));
                }
            }

            return ms2DecResult;
        }

        private static MS2DecResult getDeconvolutionResultBeanVer6(FileStream fs, List<long> seekpointList, int peakID) {
            var ms2DecResult = new MS2DecResult();
            fs.Seek(seekpointList[peakID], SeekOrigin.Begin); // go to the target seek point

            var buffer = new byte[44];
            fs.Read(buffer, 0, buffer.Length); // store the meta field buffer

            //precursor properties
            ms2DecResult.PeakTopRetentionTime = BitConverter.ToSingle(buffer, 0); ;
            ms2DecResult.Ms1AccurateMass = BitConverter.ToSingle(buffer, 4);
            ms2DecResult.Ms1PeakHeight = BitConverter.ToSingle(buffer, 8);
            ms2DecResult.Ms1IsotopicIonM1PeakHeight = BitConverter.ToSingle(buffer, 12);
            ms2DecResult.Ms1IsotopicIonM2PeakHeight = BitConverter.ToSingle(buffer, 16);

            //MS2 properties
            ms2DecResult.UniqueMs = BitConverter.ToSingle(buffer, 20);
            ms2DecResult.Ms2DecPeakHeight = BitConverter.ToSingle(buffer, 24);
            ms2DecResult.Ms2DecPeakArea = BitConverter.ToSingle(buffer, 28); ;

            //Spectra, base chromatogram, unique masses
            var spectraNumber = BitConverter.ToInt32(buffer, 32);
            var datapointNumber = BitConverter.ToInt32(buffer, 36);
            var modelMasses = BitConverter.ToInt32(buffer, 40);

            if (spectraNumber != 0) {
                buffer = new byte[spectraNumber * 20];
                fs.Read(buffer, 0, buffer.Length);
                for (int i = 0; i < spectraNumber; i++) {
                    ms2DecResult.MassSpectra.Add(new double[] { BitConverter.ToSingle(buffer, 20 * i), BitConverter.ToSingle(buffer, 20 * i + 4),
                   BitConverter.ToInt32(buffer, 20 * i + 8), BitConverter.ToInt32(buffer, 20 * i + 12), BitConverter.ToInt32(buffer, 20 * i + 16) });
                }
            }

            if (datapointNumber != 0) {
                var basePeaklist = new List<double[]>();
                buffer = new byte[datapointNumber * 16];
                fs.Read(buffer, 0, buffer.Length);

                for (int i = 0; i < datapointNumber; i++)
                    basePeaklist.Add(new double[] { BitConverter.ToInt32(buffer, 16 * i), BitConverter.ToSingle(buffer, 16 * i + 4), BitConverter.ToSingle(buffer, 16 * i + 8), BitConverter.ToSingle(buffer, 16 * i + 12) });

                var peaklist = new List<double[]>();
                var maxIntensity = basePeaklist.Max(n => n[3]);
                for (int i = 0; i < spectraNumber; i++) {
                    peaklist = new List<double[]>();
                    for (int j = 0; j < basePeaklist.Count; j++)
                        peaklist.Add(new double[] { basePeaklist[j][0], basePeaklist[j][1], ms2DecResult.MassSpectra[i][0], basePeaklist[j][3] * ms2DecResult.MassSpectra[i][1] / maxIntensity });

                    ms2DecResult.PeaklistList.Add(peaklist);
                }
            }

            if (modelMasses != 0) {
                buffer = new byte[modelMasses * 4];
                fs.Read(buffer, 0, buffer.Length);
                for (int i = 0; i < modelMasses; i++) {
                    ms2DecResult.ModelMasses.Add(BitConverter.ToSingle(buffer, 4 * i));
                }
            }

            return ms2DecResult;
        }


        public static List<long> ReadSeekPointsOfMS2DecResultFile(FileStream fs) {
            var buffer = new byte[6];
            fs.Seek(0, SeekOrigin.Begin);
            fs.Read(buffer, 0, 6);

            if (BitConverter.ToChar(buffer, 0).Equals('V') && (BitConverter.ToInt32(buffer, 2) == 2 || 
                BitConverter.ToInt32(buffer, 2) == 3 || BitConverter.ToInt32(buffer, 2) == 4
                || BitConverter.ToInt32(buffer, 2) == 5) || BitConverter.ToInt32(buffer, 2) == 6) {
                return getSeekpointListVer2(fs);
            }
            else {
                return getSeekpointListVer1(fs);
            }

        }

        private static List<long> getSeekpointListVer2(FileStream fs) {
            var seekpointList = new List<long>();
            int totalPeakNumber;
            byte[] buffer = new byte[4];

            fs.Seek(6, SeekOrigin.Begin);
            fs.Read(buffer, 0, 4);

            totalPeakNumber = BitConverter.ToInt32(buffer, 0);

            buffer = new byte[8 * totalPeakNumber];
            fs.Read(buffer, 0, buffer.Length);
            for (int i = 0; i < totalPeakNumber; i++)
                seekpointList.Add(BitConverter.ToInt64(buffer, 8 * i));

            return seekpointList;
        }

        private static List<long> getSeekpointListVer1(FileStream fs) {
            var seekpointList = new List<long>();
            int totalPeakNumber;
            byte[] buffer = new byte[4];

            fs.Seek(0, SeekOrigin.Begin);
            fs.Read(buffer, 0, 4);

            totalPeakNumber = BitConverter.ToInt32(buffer, 0);

            buffer = new byte[8 * totalPeakNumber];
            fs.Read(buffer, 0, buffer.Length);
            for (int i = 0; i < totalPeakNumber; i++)
                seekpointList.Add(BitConverter.ToInt64(buffer, 8 * i));

            return seekpointList;
        }


        #endregion
    }
}
