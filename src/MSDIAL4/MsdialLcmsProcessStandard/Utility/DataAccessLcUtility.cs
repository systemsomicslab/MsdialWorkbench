using Msdial.Lcms.Dataprocess.Algorithm;
using Rfx.Riken.OsakaUniv;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using CompMs.Common.DataObj;

namespace Msdial.Lcms.Dataprocess.Utility
{
    public sealed class DataAccessLcUtility
    {
        private DataAccessLcUtility() { }

        public static ObservableCollection<RawSpectrum> GetRdamSpectrumCollection(RawDataAccess rawDataAccess)
        {
            var mes = rawDataAccess.GetMeasurement();
            if (mes == null) return null;
            if (mes.SpectrumList == null) return null;
            else return new ObservableCollection<RawSpectrum>(mes.SpectrumList);
        }

        // more secure access
        public static ObservableCollection<RawSpectrum> GetRdamSpectrumCollection(ProjectPropertyBean projectProperty, RdamPropertyBean rdamProperty, AnalysisFileBean analysisFile) {
            ObservableCollection<RawSpectrum> spectrumCollection;
            var fileID = rdamProperty.RdamFilePath_RdamFileID[analysisFile.AnalysisFilePropertyBean.AnalysisFilePath];
            var measurementID = rdamProperty.RdamFileContentBeanCollection[fileID].FileID_MeasurementID[analysisFile.AnalysisFilePropertyBean.AnalysisFileId];
            using (var rawDataAccess = new RawDataAccess(analysisFile.AnalysisFilePropertyBean.AnalysisFilePath, measurementID, false, false, true, analysisFile.RetentionTimeCorrectionBean.PredictedRt)) {
                spectrumCollection = GetRdamSpectrumCollection(rawDataAccess);
                var counter = 0;
                while (spectrumCollection == null) {
                    System.Threading.Thread.Sleep(2000);
                    spectrumCollection = GetRdamSpectrumCollection(rawDataAccess);
                    counter++;
                    if (counter > 5) break;
                }
            }
            return spectrumCollection;
        }

        public static RawMeasurement GetRawDataMeasurement(RawDataAccess rawDataAccess) {
            var mes = rawDataAccess.GetMeasurement();
            return mes;
        }

        public static ObservableCollection<RawSpectrum> GetAllSpectrumCollection(RawMeasurement mes) {
            if (mes == null) return null;
            if (mes.SpectrumList == null) return null;
            else return new ObservableCollection<RawSpectrum>(mes.SpectrumList);
        }

        public static ObservableCollection<RawSpectrum> GetAccumulatedMs1SpectrumCollection(RawMeasurement mes) {
            if (mes == null) return null;
            if (mes.AccumulatedSpectrumList == null) return null;
            if (mes.AccumulatedSpectrumList.Count == 0) return null;
            else return new ObservableCollection<RawSpectrum>(mes.AccumulatedSpectrumList);
        }

        public static List<double[]> GetChromatogramPeaklist(AnalysisFileBean file, float rtBegin, float rtEnd, float targetMz, float mzTolerance, IonMode ionmode, RdamPropertyBean rdamProperty)
        {
            var peaklist = new List<double[]>();
            var filepath = file.AnalysisFilePropertyBean.AnalysisFilePath;
            var fileID = rdamProperty.RdamFilePath_RdamFileID[filepath];
            var measurementID = rdamProperty.RdamFileContentBeanCollection[fileID].FileID_MeasurementID[file.AnalysisFilePropertyBean.AnalysisFileId];

            using (var rawDataAccess = new RawDataAccess(filepath, measurementID, false, false, true, file.RetentionTimeCorrectionBean.PredictedRt)) {
                var mes = rawDataAccess.GetMeasurement();
                var spectrumCollection = new ObservableCollection<RawSpectrum>(mes.SpectrumList);
                peaklist = getMs1Peaklist(spectrumCollection, targetMz, mzTolerance, rtBegin, rtEnd, ionmode);
            }

            return peaklist;
        }

        public static List<double[]> GetTicPeaklist(ObservableCollection<RawSpectrum> spectrumCollection, ProjectPropertyBean projectPropertyBean)
        {
            var peaklist = new List<double[]>();
            if (projectPropertyBean.MethodType == MethodType.ddMSMS || projectPropertyBean.SeparationType == SeparationType.IonMobility)
            {
                peaklist = getTicPeaklist(spectrumCollection, projectPropertyBean.IonMode);
            }
            else if (projectPropertyBean.MethodType == MethodType.diMSMS)
            {
                peaklist = getTicPeaklist(spectrumCollection, projectPropertyBean.ExperimentID_AnalystExperimentInformationBean);
            }
            return peaklist;
        }

        private static List<double[]> getTicPeaklist(ObservableCollection<RawSpectrum> spectrumCollection, Dictionary<int, AnalystExperimentInformationBean> experimentID_AnalystExperimentInformationBean)
        {
            var peaklist = new List<double[]>();
            RawSpectrum spectrum;
            RawPeakElement[] massSpectra;
            double sum = 0, maxIntensityMz, maxMass;

            int ms1LevelId = 0, experimentNumber = experimentID_AnalystExperimentInformationBean.Count, counter;
            foreach (var value in experimentID_AnalystExperimentInformationBean) { if (value.Value.MsType == MsType.SCAN) { ms1LevelId = value.Key; break; } }
            counter = ms1LevelId;

            while (counter < spectrumCollection.Count)
            {
                spectrum = spectrumCollection[counter];

                sum = 0;
                massSpectra = spectrum.Spectrum;
                maxIntensityMz = double.MinValue;
                maxMass = -1;

                for (int i = 0; i < massSpectra.Length; i++)
                {
                    sum += massSpectra[i].Intensity;
                    if (maxIntensityMz < massSpectra[i].Intensity) { maxIntensityMz = massSpectra[i].Intensity; maxMass = massSpectra[i].Mz; }
                }

                peaklist.Add(new double[] { counter, spectrum.ScanStartTime, maxMass, sum });

                counter += experimentNumber;
            }
            return peaklist;
        }

        private static List<double[]> getTicPeaklist(ObservableCollection<RawSpectrum> spectrumCollection, IonMode ionmode)
        {
            var peaklist = new List<double[]>();
            RawSpectrum spectrum;
            RawPeakElement[] massSpectra;
            double sum = 0, maxIntensityMz = double.MinValue, maxMass = -1;
            var scanPolarity = ionmode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;
            var lastScanId = -1;

            for (int i = 0; i < spectrumCollection.Count; i++)
            {
                spectrum = spectrumCollection[i];

                if (spectrum.MsLevel > 1) continue;
                if (spectrum.ScanPolarity != scanPolarity) continue;

                massSpectra = spectrum.Spectrum;

                if (spectrum.ScanNumber != lastScanId) {
                    sum = 0;
                    maxIntensityMz = double.MinValue;
                    maxMass = -1;
                }

                for (int j = 0; j < massSpectra.Length; j++)
                {
                    sum += massSpectra[j].Intensity;
                    if (maxIntensityMz < massSpectra[j].Intensity) {
                        maxIntensityMz = massSpectra[j].Intensity;
                        maxMass = massSpectra[j].Mz;
                    }
                }

                if (lastScanId == spectrum.ScanNumber) {
                    peaklist[peaklist.Count - 1][2] = maxMass;
                    peaklist[peaklist.Count - 1][3] = sum;
                }
                else
                    peaklist.Add(new double[] { i, spectrum.ScanStartTime, maxMass, sum });

                lastScanId = spectrum.ScanNumber;
            }
            return peaklist;
        }

        public static List<double[]> GetMs1Peaklist(ObservableCollection<RawSpectrum> spectrumCollection, ProjectPropertyBean projectPropertyBean, float focusedMass, float ms1Tolerance, float retentionTimeBegin, float retentionTimeEnd)
        {
            var peaklist = new List<double[]>();

            if (projectPropertyBean.SeparationType == SeparationType.IonMobility)
            {
                peaklist = getMs1Peaklist(spectrumCollection, focusedMass, ms1Tolerance, retentionTimeBegin, retentionTimeEnd, projectPropertyBean.IonMode);
            }
            else
            {
                if (projectPropertyBean.MethodType == MethodType.ddMSMS)
                {
                    peaklist = getMs1Peaklist(spectrumCollection, focusedMass, ms1Tolerance, retentionTimeBegin, retentionTimeEnd, projectPropertyBean.IonMode);
                }
                else if (projectPropertyBean.MethodType == MethodType.diMSMS)
                {
                    peaklist = getMs1Peaklist(spectrumCollection, focusedMass, ms1Tolerance, retentionTimeBegin, retentionTimeEnd, projectPropertyBean.ExperimentID_AnalystExperimentInformationBean);
                }
            }
            return peaklist;
        }

        public static List<double[]> GetMs1PeaklistAsBPC(ObservableCollection<RawSpectrum> spectrumCollection, ProjectPropertyBean projectPropertyBean, float focusedMass, float ms1Tolerance, float retentionTimeBegin, float retentionTimeEnd)
        {
            var peaklist = new List<double[]>();
            if (projectPropertyBean.MethodType == MethodType.ddMSMS || projectPropertyBean.SeparationType == SeparationType.IonMobility)
            {
                peaklist = getMs1PeaklistAsBPC(spectrumCollection, focusedMass, ms1Tolerance, retentionTimeBegin, retentionTimeEnd, projectPropertyBean.IonMode);
            }
            else if (projectPropertyBean.MethodType == MethodType.diMSMS)
            {
                peaklist = getMs1PeaklistAsBPC(spectrumCollection, focusedMass, ms1Tolerance, retentionTimeBegin, retentionTimeEnd, projectPropertyBean.ExperimentID_AnalystExperimentInformationBean);
            }
            return peaklist;
        }

        public static List<double[]> GetMs1PeaklistAsBPC(ObservableCollection<RawSpectrum> spectrumCollection, 
            ProjectPropertyBean projectPropertyBean, float ms1Tolerance) {
            var peaklist = new List<double[]>();
            RawSpectrum spectrum;
            RawPeakElement[] massSpectra;
            int startIndex = 0;
            double sum = 0, maxIntensityMz = double.MinValue, maxMass = -1;
            var scanPolarity = projectPropertyBean.IonMode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;
            var lastScanId = -1;

            for (int i = 0; i < spectrumCollection.Count; i++) {
                spectrum = spectrumCollection[i];

                if (spectrum.MsLevel > 1) continue;
                if (spectrum.ScanPolarity != scanPolarity) continue;
                massSpectra = spectrum.Spectrum;
                var focusedMass = (float)spectrum.BasePeakMz;
                if (spectrum.ScanNumber != lastScanId) {
                    sum = 0;
                    maxIntensityMz = double.MinValue;
                    maxMass = -1;
                }
                startIndex = GetMs1StartIndex(focusedMass, ms1Tolerance, massSpectra);

                for (int j = startIndex; j < massSpectra.Length; j++) {
                    if (massSpectra[j].Mz < focusedMass - ms1Tolerance) continue;
                    else if (focusedMass - ms1Tolerance <= massSpectra[j].Mz 
                        && massSpectra[j].Mz <= focusedMass + ms1Tolerance) {
                        sum += massSpectra[j].Intensity; if (maxIntensityMz < massSpectra[j].Intensity) {
                            maxIntensityMz = massSpectra[j].Intensity; maxMass = massSpectra[j].Mz; } }
                    else if (massSpectra[j].Mz > focusedMass + ms1Tolerance) break;
                }

                if (maxIntensityMz < 0) maxIntensityMz = 0;

                if (lastScanId == spectrum.ScanNumber) {
                    peaklist[peaklist.Count - 1][2] = maxMass;
                    peaklist[peaklist.Count - 1][3] = maxIntensityMz;
                }
                else
                    peaklist.Add(new double[] { i, spectrum.ScanStartTime, focusedMass, sum });

                lastScanId = spectrum.ScanNumber;
            }
            return peaklist;
        }



        public static ObservableCollection<double[]> GetAccumulatedMS1SpectrumFromDriftArrays(ObservableCollection<RawSpectrum> spectrumCollection, int scan) {

            var rSpec = spectrumCollection[scan];
            var scanID = rSpec.ScanNumber;
            var mslevel = rSpec.MsLevel;

            var aMassArray = new double[3000000]; // 3000000 is enough for conventional mass spec.
            foreach (var spec in spectrumCollection.Where(n => n.ScanNumber == scanID && n.MsLevel == mslevel)) {
                var spectrum = spec.Spectrum;
                foreach (var mzint in spectrum) {
                    aMassArray[(int)(mzint.Mz * 1000)] += mzint.Intensity;
                }
            }

            var aSpecta = new ObservableCollection<double[]>();
            for (int i = 0; i < aMassArray.Length; i++) {
                if (aMassArray[i] <= 1) continue;
                aSpecta.Add(new double[] { i * 0.001, aMassArray[i] });
            }

            return aSpecta;
        }

        private static List<double[]> getMs1Peaklist(ObservableCollection<RawSpectrum> spectrumCollection, 
            float focusedMass, float ms1Tolerance, float retentionTimeBegin,
            float retentionTimeEnd, IonMode ionmode)
        {
            var peaklist = new List<double[]>();
            RawSpectrum spectrum;
            RawPeakElement[] massSpectra;
            int startIndex = 0;
            double sum = 0, maxIntensityMz = double.MinValue, maxMass = -1;
            var scanPolarity = ionmode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;
            var lastScanId = -1;

            for (int i = 0; i < spectrumCollection.Count; i++)
            {
                spectrum = spectrumCollection[i];

                if (spectrum.MsLevel > 1) continue;
                if (spectrum.ScanPolarity != scanPolarity) continue;
                if (spectrum.ScanStartTime < retentionTimeBegin) continue;
                if (spectrum.ScanStartTime > retentionTimeEnd) break;

                massSpectra = spectrum.Spectrum;

                //sum = 0;
                //maxIntensityMz = double.MinValue;
                //maxMass = focusedMass;

                if (spectrum.ScanNumber != lastScanId) {
                    sum = 0;
                    maxIntensityMz = double.MinValue;
                    maxMass = focusedMass;
                }
                startIndex = GetMs1StartIndex(focusedMass, ms1Tolerance, massSpectra);

				//bin intensities for focused MZ +- ms1Tolerance
				for (int j = startIndex; j < massSpectra.Length; j++)
                {
                    if (massSpectra[j].Mz < focusedMass - ms1Tolerance) continue;
                    else if (focusedMass - ms1Tolerance <= massSpectra[j].Mz && massSpectra[j].Mz <= focusedMass + ms1Tolerance)
                    {
						sum += massSpectra[j].Intensity;
                        if (maxIntensityMz < massSpectra[j].Intensity)
                        {
							maxIntensityMz = massSpectra[j].Intensity;
							maxMass = massSpectra[j].Mz;
						}
					}
                    else if (massSpectra[j].Mz > focusedMass + ms1Tolerance) break;
				}

                if (lastScanId == spectrum.ScanNumber) {
                    peaklist[peaklist.Count - 1][2] = maxMass;
                    peaklist[peaklist.Count - 1][3] = sum;
                }
                else
                    peaklist.Add(new double[] { i, spectrum.ScanStartTime, maxMass, sum });
               // Console.WriteLine("Last id {0}, Scan num {1}", lastScanId, spectrum.ScanNumber);

                lastScanId = spectrum.ScanNumber;
            }
			return peaklist;
        }

        public static int GetPeakAreaBeanIdMostlyMatchedWithMasterID(ObservableCollection<PeakAreaBean> peakSpots, int masterID) {
            if (peakSpots == null || peakSpots.Count == 0) return 0;
            int startIndex = 0, endIndex = peakSpots.Count - 1;
            int counter = 0;
            if (masterID > peakSpots[endIndex].MasterPeakID) return endIndex;

            while (counter < 10) {
                if (peakSpots[startIndex].MasterPeakID <= masterID && masterID < peakSpots[(startIndex + endIndex) / 2].MasterPeakID) {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (peakSpots[(startIndex + endIndex) / 2].MasterPeakID <= masterID && masterID < peakSpots[endIndex].MasterPeakID) {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            
            for (int i = startIndex; i < peakSpots.Count; i++) {
                if (i > peakSpots.Count - 1) return peakSpots[peakSpots.Count - 1].PeakID;
                var peakSpot = peakSpots[i];
                var nextPeak = peakSpots[i + 1];
                if (peakSpot.MasterPeakID <= masterID && masterID < nextPeak.MasterPeakID) return peakSpot.PeakID;
            }
            return 0;
        }

        public static object GetPeakAreaBeanOrDriftSpotBeanMatchedWithMasterID(ObservableCollection<PeakAreaBean> peakSpots, int masterID, out bool onDrift) {
            onDrift = false;
            for (int i = masterID; i < peakSpots.Count; i++) {
                var pSpot = peakSpots[i];
                if (pSpot.MasterPeakID == masterID) return pSpot;
                foreach (var dSpot in pSpot.DriftSpots.OrEmptyIfNull()) {
                    if (dSpot.MasterPeakID == masterID) {
                        onDrift = true;
                        return dSpot;
                    }
                }
            }
            return null;
        }

        public static int GetAlignmentSpotIdMostlyMatchedWithMasterID(ObservableCollection<AlignmentPropertyBean> peakSpots, int masterID) {
            if (peakSpots == null || peakSpots.Count == 0) return 0;
            int startIndex = 0, endIndex = peakSpots.Count - 1;
            int counter = 0;
            if (masterID > peakSpots[endIndex].MasterID) return endIndex;

            while (counter < 10) {
                if (peakSpots[startIndex].MasterID <= masterID && masterID < peakSpots[(startIndex + endIndex) / 2].MasterID) {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (peakSpots[(startIndex + endIndex) / 2].MasterID <= masterID && masterID < peakSpots[endIndex].MasterID) {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }

            for (int i = startIndex; i < peakSpots.Count; i++) {
                if (i > peakSpots.Count - 1) return peakSpots[peakSpots.Count - 1].AlignmentID;
                var peakSpot = peakSpots[i];
                var nextPeak = peakSpots[i + 1];
                if (peakSpot.MasterID <= masterID && masterID < nextPeak.MasterID) return peakSpot.AlignmentID;
            }
            return 0;
        }

        public static object GetAlignmentPropertyBeanOrAlignedDriftSpotBeanMatchedWithMasterID(
            ObservableCollection<AlignmentPropertyBean> alignedSpots, int masterID, out bool onDrift) {
            onDrift = false;
            for (int i = masterID; i < alignedSpots.Count; i++) {
                var pSpot = alignedSpots[i];
                if (pSpot.MasterID == masterID) return pSpot;
                foreach (var dSpot in pSpot.AlignedDriftSpots.OrEmptyIfNull()) {
                    if (dSpot.MasterID == masterID) {
                        onDrift = true;
                        return dSpot;
                    }
                }
            }
            return null;
        }

        public static List<object> GetAlignmentPropertyBeanAndAlignedDriftSpotBeanMergedList(ObservableCollection<AlignmentPropertyBean> alignedSpots) {
            var objects = new List<object>();
            foreach (var spot in alignedSpots) {
                objects.Add(spot);
                foreach (var dSpot in spot.AlignedDriftSpots.OrEmptyIfNull()) {
                    objects.Add(dSpot);
                }
            }
            return objects;
        }

        public static ObservableCollection<AlignedPeakPropertyBean> GetAlignedPeakPropertyBeanCollection(object spotProp) {
            if (spotProp.GetType() == typeof(AlignmentPropertyBean)) {
                var rSpot = (AlignmentPropertyBean)spotProp;
                return rSpot.AlignedPeakPropertyBeanCollection;
            }
            else {
                var dSpot = (AlignedDriftSpotPropertyBean)spotProp;
                return dSpot.AlignedPeakPropertyBeanCollection;
            }
        }

        public static int GetInternalStanderdId(object spotProp) {
            if (spotProp.GetType() == typeof(AlignmentPropertyBean)) {
                var rSpot = (AlignmentPropertyBean)spotProp;
                return rSpot.InternalStandardAlignmentID;
            }
            else {
                var dSpot = (AlignedDriftSpotPropertyBean)spotProp;
                return dSpot.InternalStandardAlignmentID;
            }
        }

        public static int GetSpotIdOfAlignmentObj(object spotProp) {
            if (spotProp.GetType() == typeof(AlignmentPropertyBean)) {
                var rSpot = (AlignmentPropertyBean)spotProp;
                if (rSpot.AlignedDriftSpots != null && rSpot.AlignedDriftSpots.Count > 0)
                    return rSpot.MasterID;
                else
                    return rSpot.AlignmentID;
            }
            else {
                var dSpot = (AlignedDriftSpotPropertyBean)spotProp;
                return dSpot.MasterID;
            }
        }

        public static string GetMetaboliteNameOfAlignmentObj(object spotProp) {
            if (spotProp.GetType() == typeof(AlignmentPropertyBean)) {
                var rSpot = (AlignmentPropertyBean)spotProp;
                return rSpot.MetaboliteName;
            }
            else {
                var dSpot = (AlignedDriftSpotPropertyBean)spotProp;
                return dSpot.MetaboliteName;
            }
        }

        public static string GetOntologyOfAlignmentObj(object spotProp, List<PostIdentificatioinReferenceBean> txtDB, List<MspFormatCompoundInformationBean> mspDB)
        {
            int libTxtId = -1, MspId = -1;
            if (spotProp is AlignmentPropertyBean rSpot)
            {
                libTxtId = rSpot.PostIdentificationLibraryID;
                MspId = rSpot.LibraryID;
            }
            else if (spotProp is AlignedDriftSpotPropertyBean dSpot)
            {
                libTxtId = dSpot.PostIdentificationLibraryID;
                MspId = dSpot.LibraryID;
            }
            return RefDataRetrieve.GetOntology(MspId, mspDB, libTxtId, txtDB);
        }

        public static double GetIonAbundanceOfMzInSpectrum(RawPeakElement[] massSpectra, 
            float mz, float mztol, out double basepeakMz, out double basepeakIntensity) {
            var startIndex = GetMs1StartIndex(mz, mztol, massSpectra);
            double sum = 0, maxIntensityMz = 0.0, maxMass = mz;

            //bin intensities for focused MZ +- ms1Tolerance
            for (int j = startIndex; j < massSpectra.Length; j++) {
                if (massSpectra[j].Mz < mz - mztol) continue;
                else if (mz - mztol <= massSpectra[j].Mz &&
                    massSpectra[j].Mz <= mz + mztol) {
                    sum += massSpectra[j].Intensity;
                    if (maxIntensityMz < massSpectra[j].Intensity) {
                        maxIntensityMz = massSpectra[j].Intensity;
                        maxMass = massSpectra[j].Mz;
                    }
                }
                else if (massSpectra[j].Mz > mz + mztol) break;
            }
            basepeakMz = maxMass;
            basepeakIntensity = maxIntensityMz;
            return sum;
        }

        private static List<double[]> getMs1Peaklist(ObservableCollection<RawSpectrum> spectrumCollection, float focusedMass, float ms1Tolerance, float retentionTimeBegin, float retentionTimeEnd, Dictionary<int, AnalystExperimentInformationBean> experimentID_AnalystExperimentInformationBean)
        {
            var peaklist = new List<double[]>();
            RawSpectrum spectrum;
            RawPeakElement[] massSpectra;
            int startIndex = 0;
            double sum = 0, maxIntensityMz, maxMass;

            int ms1LevelId = 0, experimentNumber = experimentID_AnalystExperimentInformationBean.Count, counter;
            foreach (var value in experimentID_AnalystExperimentInformationBean) { if (value.Value.MsType == MsType.SCAN) { ms1LevelId = value.Key; break; } }
            counter = ms1LevelId;

            while (counter < spectrumCollection.Count)
            {
                spectrum = spectrumCollection[counter];

                if (spectrum.ScanStartTime < retentionTimeBegin) { counter += experimentNumber; continue; }
                if (spectrum.ScanStartTime > retentionTimeEnd) break;

                sum = 0;
                massSpectra = spectrum.Spectrum;
                maxIntensityMz = double.MinValue;
                maxMass = focusedMass;
                startIndex = GetMs1StartIndex(focusedMass, ms1Tolerance, massSpectra);

                for (int k = startIndex; k < massSpectra.Length; k++)
                {
                    if (massSpectra[k].Mz < focusedMass - ms1Tolerance) continue;
                    else if (focusedMass - ms1Tolerance <= massSpectra[k].Mz && massSpectra[k].Mz <= focusedMass + ms1Tolerance)
                    {
						sum += massSpectra[k].Intensity;
                        if (maxIntensityMz < massSpectra[k].Intensity)
                        {
							maxIntensityMz = massSpectra[k].Intensity; 
							maxMass = massSpectra[k].Mz;
						}
					}
                    else if (massSpectra[k].Mz > focusedMass + ms1Tolerance) break;
                }

                peaklist.Add(new double[] { counter, spectrum.ScanStartTime, maxMass, sum });

                counter += experimentNumber;
            }
            return peaklist;
        }

        private static List<double[]> getMs1PeaklistAsBPC(ObservableCollection<RawSpectrum> spectrumCollection, 
            float focusedMass, float ms1Tolerance, float retentionTimeBegin, float retentionTimeEnd, IonMode ionmode)
        {
            var peaklist = new List<double[]>();
            RawSpectrum spectrum;
            RawPeakElement[] massSpectra;
            int startIndex = 0;
            double sum = 0, maxIntensityMz = double.MinValue, maxMass = -1;
            var scanPolarity = ionmode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;
            var lastScanId = -1;

            for (int i = 0; i < spectrumCollection.Count; i++)
            {
                spectrum = spectrumCollection[i];

                if (spectrum.MsLevel > 1) continue;
                if (spectrum.ScanPolarity != scanPolarity) continue;
                if (spectrum.ScanStartTime < retentionTimeBegin) continue;
                if (spectrum.ScanStartTime > retentionTimeEnd) break;

                massSpectra = spectrum.Spectrum;
                if (spectrum.ScanNumber != lastScanId) {
                    sum = 0;
                    maxIntensityMz = double.MinValue;
                    maxMass = -1;
                }
                startIndex = GetMs1StartIndex(focusedMass, ms1Tolerance, massSpectra);

                for (int j = startIndex; j < massSpectra.Length; j++)
                {
                    if (massSpectra[j].Mz < focusedMass - ms1Tolerance) continue;
                    else if (focusedMass - ms1Tolerance <= massSpectra[j].Mz && massSpectra[j].Mz <= focusedMass + ms1Tolerance) { sum += massSpectra[j].Intensity; if (maxIntensityMz < massSpectra[j].Intensity) { maxIntensityMz = massSpectra[j].Intensity; maxMass = massSpectra[j].Mz; } }
                    else if (massSpectra[j].Mz > focusedMass + ms1Tolerance) break;
                }

                if (maxIntensityMz < 0) maxIntensityMz = 0;

                if (lastScanId == spectrum.ScanNumber) {
                    peaklist[peaklist.Count - 1][2] = maxMass;
                    peaklist[peaklist.Count - 1][3] = maxIntensityMz;
                }
                else
                    peaklist.Add(new double[] { i, spectrum.ScanStartTime, maxMass, maxIntensityMz });

                lastScanId = spectrum.ScanNumber;
            }
            return peaklist;
        }

        private static List<double[]> getMs1PeaklistAsBPC(ObservableCollection<RawSpectrum> spectrumCollection, float focusedMass, float ms1Tolerance, float retentionTimeBegin, float retentionTimeEnd, Dictionary<int, AnalystExperimentInformationBean> experimentID_AnalystExperimentInformationBean)
        {
            var peaklist = new List<double[]>();
            RawSpectrum spectrum;
            RawPeakElement[] massSpectra;
            int startIndex = 0;
            double sum = 0, maxIntensityMz, maxMass;

            int ms1LevelId = 0, experimentNumber = experimentID_AnalystExperimentInformationBean.Count, counter;
            foreach (var value in experimentID_AnalystExperimentInformationBean)
            {
                if (value.Value.MsType == MsType.SCAN)
                {
					ms1LevelId = value.Key;
					break;
				}
			}
            counter = ms1LevelId;

            while (counter < spectrumCollection.Count)
            {
                spectrum = spectrumCollection[counter];

                if (spectrum.ScanStartTime < retentionTimeBegin) { counter += experimentNumber; continue; }
                if (spectrum.ScanStartTime > retentionTimeEnd) break;

                sum = 0;
                massSpectra = spectrum.Spectrum;
                maxIntensityMz = double.MinValue;
                maxMass = focusedMass;
                startIndex = GetMs1StartIndex(focusedMass, ms1Tolerance, massSpectra);

                for (int k = startIndex; k < massSpectra.Length; k++)
                {
                    if (massSpectra[k].Mz < focusedMass - ms1Tolerance) continue;
                    else if (focusedMass - ms1Tolerance <= massSpectra[k].Mz && massSpectra[k].Mz <= focusedMass + ms1Tolerance) { sum += massSpectra[k].Intensity; if (maxIntensityMz < massSpectra[k].Intensity) { maxIntensityMz = massSpectra[k].Intensity; maxMass = massSpectra[k].Mz; } }
                    else if (massSpectra[k].Mz > focusedMass + ms1Tolerance) break;
                }

                if (maxIntensityMz < 0) maxIntensityMz = 0;

                peaklist.Add(new double[] { counter, spectrum.ScanStartTime, maxMass, maxIntensityMz });

                counter += experimentNumber;
            }
            return peaklist;
        }

        public static List<double[]> GetMatchedMs2Peaklist(List<List<double[]>> peaklistList, double focusedMass)
        {
            if (peaklistList.Count == 0) return new List<double[]>();
            var peaklist = new List<double[]>();
            int peakID = 0;
            for (int i = 0; i < peaklistList.Count; i++)
                if (Math.Abs(peaklistList[i][0][2] - focusedMass) < 0.0001) { peakID = i; break; }

            for (int i = 0; i < peaklistList[peakID].Count; i++)
                peaklist.Add(new double[] { peaklistList[peakID][i][0], peaklistList[peakID][i][1], peaklistList[peakID][i][2], peaklistList[peakID][i][3] });

            return peaklist;
        }

        public static float[] GetMs1Range(ObservableCollection<RawSpectrum> spectrumCollection, IonMode ionmode)
        {
            float minMz = float.MaxValue, maxMz = float.MinValue;
            var scanPolarity = ionmode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;

            for (int i = 0; i < spectrumCollection.Count; i++)
            {
                if (spectrumCollection[i].MsLevel > 1) continue;
                if (spectrumCollection[i].ScanPolarity != scanPolarity) continue;
                //if (spectrumCollection[i].DriftScanNumber > 0) continue;

                if (spectrumCollection[i].LowestObservedMz < minMz) minMz = (float)spectrumCollection[i].LowestObservedMz;
                if (spectrumCollection[i].HighestObservedMz > maxMz) maxMz = (float)spectrumCollection[i].HighestObservedMz;
            }
            return new float[] { minMz, maxMz };
        }

        public static int GetMs1StartIndex(float focusedMass, float ms1Tolerance, RawPeakElement[] massSpectra)
        {
            if (massSpectra == null || massSpectra.Length == 0) return 0;
            float targetMass = focusedMass - ms1Tolerance;
            int startIndex = 0, endIndex = massSpectra.Length - 1;
            int counter = 0;
            if (targetMass > massSpectra[endIndex].Mz) return endIndex;

            while (counter < 10)
            {
                if (massSpectra[startIndex].Mz <= targetMass && targetMass < massSpectra[(startIndex + endIndex) / 2].Mz)
                {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (massSpectra[(startIndex + endIndex) / 2].Mz <= targetMass && targetMass < massSpectra[endIndex].Mz)
                {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        public static List<double[]> GetSmoothedPeaklist(List<double[]> peaklist, SmoothingMethod smoothingMethod, int smoothingLevel)
        {
            var smoothedPeaklist = new List<double[]>();

            switch (smoothingMethod)
            {
                case SmoothingMethod.SimpleMovingAverage:
                    smoothedPeaklist = Smoothing.SimpleMovingAverage(peaklist, smoothingLevel);
                    break;
                case SmoothingMethod.LinearWeightedMovingAverage:
                    smoothedPeaklist = Smoothing.LinearWeightedMovingAverage(peaklist, smoothingLevel);
                    break;
                case SmoothingMethod.SavitzkyGolayFilter:
                    smoothedPeaklist = Smoothing.SavitxkyGolayFilter(peaklist, smoothingLevel);
                    break;
                case SmoothingMethod.BinomialFilter:
                    smoothedPeaklist = Smoothing.BinomialFilter(peaklist, smoothingLevel);
                    break;
                case SmoothingMethod.LowessFilter:
                    smoothedPeaklist = Smoothing.LowessFilter(peaklist, smoothingLevel);
                    break;
                case SmoothingMethod.LoessFilter:
                    smoothedPeaklist = Smoothing.LoessFilter(peaklist, smoothingLevel);
                    break;
                default:
                    smoothedPeaklist = Smoothing.LinearWeightedMovingAverage(peaklist, smoothingLevel);
                    break;
            }

            return smoothedPeaklist;
        }

        public static PeakAreaBean GetPeakAreaBean(PeakDetectionResult result)
        {
            if (result == null) return null;

            var peakAreaBean = new PeakAreaBean() {
                AmplitudeOrderValue = result.AmplitudeOrderValue,
                AmplitudeScoreValue = result.AmplitudeScoreValue,
                AreaAboveBaseline = result.AreaAboveBaseline,
                AreaAboveZero = result.AreaAboveZero,
                BasePeakValue = result.BasePeakValue,
                GaussianSimilarityValue = result.GaussianSimilarityValue,
                IdealSlopeValue = result.IdealSlopeValue,
                IntensityAtLeftPeakEdge = result.IntensityAtLeftPeakEdge,
                IntensityAtPeakTop = result.IntensityAtPeakTop,
                IntensityAtRightPeakEdge = result.IntensityAtRightPeakEdge,
                PeakID = result.PeakID,
                PeakPureValue = result.PeakPureValue,
                RtAtLeftPeakEdge = result.RtAtLeftPeakEdge,
                RtAtPeakTop = result.RtAtPeakTop,
                RtAtRightPeakEdge = result.RtAtRightPeakEdge,
                ScanNumberAtLeftPeakEdge = result.ScanNumAtLeftPeakEdge,
                ScanNumberAtPeakTop = result.ScanNumAtPeakTop,
                ScanNumberAtRightPeakEdge = result.ScanNumAtRightPeakEdge,
                ShapenessValue = result.ShapnessValue,
                SymmetryValue = result.SymmetryValue,
                EstimatedNoise = result.EstimatedNoise,
                SignalToNoise = result.SignalToNoise,
                NormalizedValue = -1,
                AlignedRetentionTime = -1,
                TotalScore = -1,
                AccurateMass = -1,
                MetaboliteName = string.Empty,
                AdductIonName = string.Empty,
                LibraryID = -1,
                IsotopeWeightNumber = -1,
                IsotopeParentPeakID = -1,
                AdductParent = -1,
                RtSimilarityValue = -1,
                IsotopeSimilarityValue = -1,
                MassSpectraSimilarityValue = -1,
                ReverseSearchSimilarityValue = -1,
                PresenseSimilarityValue = -1,
                Ms1LevelDatapointNumber = -1,
                Ms1LevelDatapointNumberAtAcculateMs1 = -1,
                Ms2LevelDatapointNumber = -1,
                AdductIonAccurateMass = -1,
                AdductIonXmer = -1,
                AdductIonChargeNumber = -1,
                DriftSpots = new List<DriftSpotBean>(),
                PeakLinks = new List<LinkedPeakFeature>(),
                PeakGroupID = -1
            };

            return peakAreaBean;
        }

        public static DriftSpotBean GetPeakAreaForDriftTime(PeakDetectionResult detectedPeakInformationBean) {
            if (detectedPeakInformationBean == null) return null;

            var peak = new DriftSpotBean() {
                AmplitudeOrderValue = detectedPeakInformationBean.AmplitudeOrderValue,
                AmplitudeScoreValue = detectedPeakInformationBean.AmplitudeScoreValue,
                AreaAboveBaseline = detectedPeakInformationBean.AreaAboveBaseline,
                AreaAboveZero = detectedPeakInformationBean.AreaAboveZero,
                BasePeakValue = detectedPeakInformationBean.BasePeakValue,
                GaussianSimilarityValue = detectedPeakInformationBean.GaussianSimilarityValue,
                IdealSlopeValue = detectedPeakInformationBean.IdealSlopeValue,
                IntensityAtLeftPeakEdge = detectedPeakInformationBean.IntensityAtLeftPeakEdge,
                IntensityAtPeakTop = detectedPeakInformationBean.IntensityAtPeakTop,
                IntensityAtRightPeakEdge = detectedPeakInformationBean.IntensityAtRightPeakEdge,
                PeakID = detectedPeakInformationBean.PeakID,
                PeakPureValue = detectedPeakInformationBean.PeakPureValue,
                DriftTimeAtLeftPeakEdge = detectedPeakInformationBean.RtAtLeftPeakEdge,
                DriftTimeAtPeakTop = detectedPeakInformationBean.RtAtPeakTop,
                DriftTimeAtRightPeakEdge = detectedPeakInformationBean.RtAtRightPeakEdge,
                DriftScanAtLeftPeakEdge = detectedPeakInformationBean.ScanNumAtLeftPeakEdge,
                DriftScanAtPeakTop = detectedPeakInformationBean.ScanNumAtPeakTop,
                DriftScanAtRightPeakEdge = detectedPeakInformationBean.ScanNumAtRightPeakEdge,
                ShapenessValue = detectedPeakInformationBean.ShapnessValue,
                SymmetryValue = detectedPeakInformationBean.SymmetryValue,
                SignalToNoise = detectedPeakInformationBean.SignalToNoise,
                NormalizedValue = -1,
                AlignedRetentionTime = -1,
                TotalScore = -1,
                AccurateMass = -1,
                MetaboliteName = string.Empty,
                AdductIonName = string.Empty,
                LibraryID = -1,
                IsotopeWeightNumber = -1,
                IsotopeParentPeakID = -1,
                AdductParent = -1,
                RtSimilarityValue = -1,
                IsotopeSimilarityValue = -1,
                MassSpectraSimilarityValue = -1,
                ReverseSearchSimilarityValue = -1,
                PresenseSimilarityValue = -1,
                Ms1LevelDatapointNumber = -1,
                Ms2LevelDatapointNumber = -1,
                AdductIonAccurateMass = -1,
                AdductIonXmer = -1,
                AdductIonChargeNumber = -1
            };

            return peak;
        }

        public static int GetMs2DatapointNumber(int startPoint, int endPoint, float accurateMass,
            AnalysisParametersBean param, ObservableCollection<RawSpectrum> spectrumCollection, IonMode ionmode)
        {
            var maxIntensity = double.MinValue;
            var maxID = -1;
            var scanPolarity = ionmode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;
            if (startPoint < 0) startPoint = 0;
            var ms1Tol = param.CentroidMs2Tolerance;
            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + ms1Tol));
            #region // practical parameter changes
            if (accurateMass > 500) {
                ms1Tol = (float)MolecularFormulaUtility.ConvertPpmToMassAccuracy(accurateMass, ppm);
            }
            #endregion

            for (int i = startPoint; i < endPoint; i++)
            {
                var spec = spectrumCollection[i];
                if (spec.MsLevel <= 1) continue;
                if (spec.MsLevel == 2 && spec.Precursor != null && scanPolarity == spec.ScanPolarity)
                {
                    //if (Math.Abs(spec.ScanStartTime - 8.62) < 1.5 && Math.Abs(accurateMass - 686.4753) < 0.005 && Math.Abs(spec.Precursor.SelectedIonMz - 686.4753) < 0.1) {
                    //    Debug.WriteLine("RT {0}, mass {1}", spec.ScanStartTime, spec.Precursor.SelectedIonMz);
                    //}
                    //if ((int)spec.Precursor.CollisionEnergy != 60) continue;

                    if (accurateMass - ms1Tol <= spec.Precursor.SelectedIonMz && 
                        spec.Precursor.SelectedIonMz <= accurateMass + ms1Tol)
                    {
                        if (maxIntensity < spec.BasePeakIntensity)
                        {
                            maxIntensity = spec.BasePeakIntensity;
                            maxID = i;
                        }
                    }
                }
            }
            return maxID;
        }

        public static void GetMs2DatapointNumberDIA(Dictionary<int, AnalystExperimentInformationBean> experimentID_AnalystExperimentInformationBean, PeakAreaBean peakAreaBean, bool isAif) {
            int ms1LevelId = -1;
            foreach (var value in experimentID_AnalystExperimentInformationBean) { if (value.Value.MsType == MsType.SCAN) { ms1LevelId = value.Key; break; } }

            if (isAif) {
                peakAreaBean.Ms2LevelDatapointNumberList = new List<int>();
                var ms2LevelIdList = new List<int>();
                foreach (var value in experimentID_AnalystExperimentInformationBean) { if (value.Value.CheckDecTarget == 1) { ms2LevelIdList.Add(value.Key); } }
                for (var j = 0; j < ms2LevelIdList.Count; j++) {
                    peakAreaBean.Ms2LevelDatapointNumberList.Add((int)peakAreaBean.Ms1LevelDatapointNumber + ms2LevelIdList[j] - ms1LevelId);
                }
                if (peakAreaBean.Ms2LevelDatapointNumberList != null && peakAreaBean.Ms2LevelDatapointNumberList.Count > 0) { peakAreaBean.Ms2LevelDatapointNumber = peakAreaBean.Ms2LevelDatapointNumberList[0]; }
                else peakAreaBean.Ms2LevelDatapointNumber = -1;
            }
            else {
                var ms2LevelId = -1;
                foreach (var value in experimentID_AnalystExperimentInformationBean) { if (value.Value.MsType == MsType.SWATH && value.Value.StartMz < peakAreaBean.AccurateMass && peakAreaBean.AccurateMass <= value.Value.EndMz) { ms2LevelId = value.Key; break; } }
                //defined by the DIA-dictionary file (SCAN number-Precursor window)
                if (ms2LevelId >= 0
                    && ms2LevelId < experimentID_AnalystExperimentInformationBean.Count
                    && experimentID_AnalystExperimentInformationBean[ms2LevelId].MsType == MsType.SWATH
                    && experimentID_AnalystExperimentInformationBean[ms2LevelId].StartMz <= peakAreaBean.AccurateMass
                    && peakAreaBean.AccurateMass <= experimentID_AnalystExperimentInformationBean[ms2LevelId].EndMz)
                    peakAreaBean.Ms2LevelDatapointNumber = (int)peakAreaBean.Ms1LevelDatapointNumber + ms2LevelId;
                else
                    peakAreaBean.Ms2LevelDatapointNumber = -1;
            }
        }

        public static int GetMs2DatapointNumberFromRt(ObservableCollection<RawSpectrum> spectrumCollection, Dictionary<int, AnalystExperimentInformationBean> experimentID_AnalystExperimentInformationBean, float rt, float mz)
        {
            int ms1LevelId = -1;
            int ms2LevelId = -1;
            foreach (var value in experimentID_AnalystExperimentInformationBean) { if(value.Value.MsType == MsType.SCAN) { ms1LevelId = value.Key; } if (value.Value.MsType == MsType.SWATH && value.Value.StartMz < mz && mz <= value.Value.EndMz) { ms2LevelId = value.Key; break; } }

            var ms1datapoint = GetMs1DatapointNumberFromRetentionTime(spectrumCollection, rt, ms1LevelId, null, experimentID_AnalystExperimentInformationBean.Count);

            if (ms2LevelId >= 0 && ms2LevelId >= 0
                && ms2LevelId < experimentID_AnalystExperimentInformationBean.Count)
                return ms1datapoint + ms2LevelId;

            return -1;
        }


        public static List<int> GetMs2DatapointNumberFromRtAif(ObservableCollection<RawSpectrum> spectrumCollection, Dictionary<int, AnalystExperimentInformationBean> experimentID_AnalystExperimentInformationBean, float rt) {
            int ms1LevelId = 0;
            var ms2LevelIdList = new List<int>();
            foreach (var value in experimentID_AnalystExperimentInformationBean) { if (value.Value.MsType == MsType.SCAN) { ms1LevelId = value.Key; } if (value.Value.CheckDecTarget == 1) { ms2LevelIdList.Add(value.Key); } }

            var id = GetMs1DatapointNumberFromRetentionTime(spectrumCollection, rt, ms1LevelId, ms2LevelIdList, experimentID_AnalystExperimentInformationBean.Count);

            var ms2LevelDatapointNumberList = new List<int>();
            for (var j = 0; j < ms2LevelIdList.Count; j++) {
                ms2LevelDatapointNumberList.Add((int)id + ms2LevelIdList[j] - ms1LevelId);
            }
            return ms2LevelDatapointNumberList;
        }

        public static int GetMs1DatapointNumberFromRetentionTime(ObservableCollection<RawSpectrum> spectrumCollection, float rt, int ms1levelId, List<int> ms2levelIdList, int numExperimentId) {
            int id = ms1levelId;
            float maxRtDiff = 1.0f;
            float rtDiff = maxRtDiff;
            for (var i = ms1levelId; i < spectrumCollection.Count; i += numExperimentId) {
                if (spectrumCollection[i].ScanStartTime < rt - maxRtDiff) continue;
                if (spectrumCollection[i].ScanStartTime > rt + maxRtDiff) break;
                if (rtDiff > Math.Abs(spectrumCollection[i].ScanStartTime - rt)) {
                    id = i;
                    rtDiff = (float)Math.Abs(spectrumCollection[i].ScanStartTime - rt);
                }
            }
            return id;
        }

        public static int GetRawSpectrumObjectsStartIndex(int targetScan, ObservableCollection<RawSpectrum> rawSpectrumObjects, bool isAccumulatedSpectra) {
            int startIndex = 0, endIndex = rawSpectrumObjects.Count - 1;
            int counter = 0;

            if (isAccumulatedSpectra) {
                while (counter < 5) {
                    if (rawSpectrumObjects[startIndex].OriginalIndex <= targetScan && targetScan < rawSpectrumObjects[(startIndex + endIndex) / 2].OriginalIndex) {
                        endIndex = (startIndex + endIndex) / 2;
                    }
                    else if (rawSpectrumObjects[(startIndex + endIndex) / 2].OriginalIndex <= targetScan && targetScan < rawSpectrumObjects[endIndex].OriginalIndex) {
                        startIndex = (startIndex + endIndex) / 2;
                    }
                    counter++;
                }
            }
            else {
                while (counter < 5) {
                    if (rawSpectrumObjects[startIndex].ScanNumber <= targetScan && targetScan < rawSpectrumObjects[(startIndex + endIndex) / 2].ScanNumber) {
                        endIndex = (startIndex + endIndex) / 2;
                    }
                    else if (rawSpectrumObjects[(startIndex + endIndex) / 2].ScanNumber <= targetScan && targetScan < rawSpectrumObjects[endIndex].ScanNumber) {
                        startIndex = (startIndex + endIndex) / 2;
                    }
                    counter++;
                }
            }
            
            return startIndex;
        }

        public static int GetScanStartIndexByScan(int targetScan, List<PeakAreaBean> peakAreaBeanList)
        {
            int startIndex = 0, endIndex = peakAreaBeanList.Count - 1;

            int counter = 0;
            while (counter < 5)
            {
                if (peakAreaBeanList[startIndex].ScanNumberAtPeakTop <= targetScan && targetScan < peakAreaBeanList[(startIndex + endIndex) / 2].ScanNumberAtPeakTop)
                {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (peakAreaBeanList[(startIndex + endIndex) / 2].ScanNumberAtPeakTop <= targetScan && targetScan < peakAreaBeanList[endIndex].ScanNumberAtPeakTop)
                {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        public static int GetScanStartIndexByRt(float targeRt, List<PeakAreaBean> peakAreaBeanList)
        {
            int startIndex = 0, endIndex = peakAreaBeanList.Count - 1;

            int counter = 0;
            while (counter < 5) {
                if (peakAreaBeanList[startIndex].RtAtPeakTop <= targeRt && 
                    targeRt < peakAreaBeanList[(startIndex + endIndex) / 2].RtAtPeakTop) {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (peakAreaBeanList[(startIndex + endIndex) / 2].RtAtPeakTop <= targeRt && 
                    targeRt < peakAreaBeanList[endIndex].RtAtPeakTop) {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        /// <summary>
        /// the collection must be sorted by accurate mass
        /// </summary>
        public static int GetScanStartIndexByMz(float targetMass, List<PeakAreaBean> peakAreaBeans)
        {
            int startIndex = 0, endIndex = peakAreaBeans.Count - 1;

            int counter = 0;
            while (counter < 5) {
                if (peakAreaBeans[startIndex].AccurateMass <= targetMass && 
                    targetMass < peakAreaBeans[(startIndex + endIndex) / 2].AccurateMass) {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (peakAreaBeans[(startIndex + endIndex) / 2].AccurateMass <= targetMass &&
                    targetMass < peakAreaBeans[endIndex].AccurateMass) {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        /// <summary>
        /// the collection must be sorted by accurate mass
        /// </summary>
        public static int GetScanStartIndexByMz(float targetMass, List<AlignmentPropertyBean> alignedSpots)
        {
            int startIndex = 0, endIndex = alignedSpots.Count - 1;

            int counter = 0;
            while (counter < 5) {
                if (alignedSpots[startIndex].CentralAccurateMass <= targetMass &&
                    targetMass < alignedSpots[(startIndex + endIndex) / 2].CentralAccurateMass) {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (alignedSpots[(startIndex + endIndex) / 2].CentralAccurateMass <= targetMass &&
                    targetMass < alignedSpots[endIndex].CentralAccurateMass) {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        /// <summary>
        /// the collection must be sorted by accurate mass
        /// </summary>
        public static int GetScanStartIndexByRt(float targetRt, List<AlignmentPropertyBean> alignedSpots)
        {
            int startIndex = 0, endIndex = alignedSpots.Count - 1;

            int counter = 0;
            while (counter < 5) {
                if (alignedSpots[startIndex].CentralRetentionTime <= targetRt &&
                    targetRt < alignedSpots[(startIndex + endIndex) / 2].CentralRetentionTime) {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (alignedSpots[(startIndex + endIndex) / 2].CentralRetentionTime <= targetRt &&
                    targetRt < alignedSpots[endIndex].CentralRetentionTime) {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        public static void AdductIonInformationRefresh(ObservableCollection<PeakAreaBean> peakAreaBeanCollection)
        {
            for (int i = 0; i < peakAreaBeanCollection.Count; i++)
            {
                peakAreaBeanCollection[i].AdductParent = -1;
                peakAreaBeanCollection[i].AdductIonName = string.Empty;
            }
        }

        public static ObservableCollection<double[]> GetCentroidMassSpectra(ObservableCollection<RawSpectrum> spectrumCollection, DataType dataType, 
            int msScanPoint, float massBin, bool peakDetectionBasedCentroid)
        {
            var spectra = new ObservableCollection<double[]>();
            var centroidedSpectra = new ObservableCollection<double[]>();
            RawSpectrum spectrum;
            RawPeakElement[] massSpectra;

            if (msScanPoint < 0) return new ObservableCollection<double[]>();

            spectrum = spectrumCollection[msScanPoint];
            massSpectra = spectrum.Spectrum;
            if (massSpectra == null || massSpectra.Length == 0) return new ObservableCollection<double[]>();

            for (int k = 0; k < massSpectra.Length; k++)
                spectra.Add(new double[] { massSpectra[k].Mz, massSpectra[k].Intensity });

            if (dataType == DataType.Centroid) return spectra;

            if (spectra.Count == 0) return new ObservableCollection<double[]>();

            centroidedSpectra = SpectralCentroiding.Centroid(spectra, massBin, peakDetectionBasedCentroid);

            if (centroidedSpectra != null && centroidedSpectra.Count != 0)
                return centroidedSpectra;
            else
                return spectra;
        }

        public static ObservableCollection<double[]> GetProfileMassSpectra(ObservableCollection<RawSpectrum> spectrumCollection, int msScanPoint)
        {
            var spectra = new ObservableCollection<double[]>();
            RawSpectrum spectrum;
            RawPeakElement[] massSpectra;

            if (msScanPoint < 0) return new ObservableCollection<double[]>();

            spectrum = spectrumCollection[msScanPoint];
            massSpectra = spectrum.Spectrum;

            for (int i = 0; i < massSpectra.Length; i++)
                spectra.Add(new double[] { massSpectra[i].Mz, massSpectra[i].Intensity });
            return spectra;
        }

        // for DIA
        public static List<double[]> GetMs2Peaklist(ObservableCollection<RawSpectrum> spectrumCollection, int focusedScanNumber, int ms1LevelId, int ms2LevelId, int experimentCycleNumber, int startScanNum, int endScanNum, double focusedMass, AnalysisParametersBean analysisParametersBean)
        {
            var peaklist = new List<double[]>();
            RawSpectrum spectrum;
            RawPeakElement[] massSpectra;

            float massTolerance = analysisParametersBean.CentroidMs2Tolerance;
            double sum;

            int startMsIndex;
            int startRtIndex = startScanNum + ms2LevelId - ms1LevelId;
            int endRtIndex = endScanNum + ms2LevelId - ms1LevelId;

            if (startRtIndex < 0) startRtIndex = 0;
            if (endRtIndex > endScanNum) endRtIndex = endScanNum;

            int counter = startRtIndex;

            while (counter <= endRtIndex)
            {
                spectrum = spectrumCollection[counter];
                massSpectra = spectrum.Spectrum;
                sum = 0;

                startMsIndex = GetMs2StartIndex(focusedMass - massTolerance, massSpectra);

                for (int j = startMsIndex; j < massSpectra.Length; j++)
                {
                    if (massSpectra[j].Mz < focusedMass - massTolerance) continue;
                    else if (focusedMass - massTolerance <= massSpectra[j].Mz && massSpectra[j].Mz <= focusedMass + massTolerance) { sum += massSpectra[j].Intensity; }
                    else break;
                }

                peaklist.Add(new double[] { counter, spectrum.ScanStartTime, focusedMass, sum });
                counter += experimentCycleNumber;
            }

            return peaklist;
        }

        // for DIA
        public static List<List<double[]>> GetMs2Peaklistlist(ObservableCollection<RawSpectrum> spectrumCollection, int ms1LevelId, int ms2LevelId, int experimentCycleNumber, int startScanNum, int endScanNum, List<double> focusedMassList, AnalysisParametersBean analysisParametersBean)
        {
            var peaklistlist = new List<List<double[]>>();
            foreach(var i in focusedMassList)
            {
                peaklistlist.Add(new List<double[]>());
            }
            RawSpectrum spectrum;
            RawPeakElement[] massSpectra;

            float massTolerance = analysisParametersBean.CentroidMs2Tolerance;
            double sum;

            int startMsIndex;
            int startRtIndex = startScanNum + ms2LevelId - ms1LevelId;
            int endRtIndex = endScanNum + ms2LevelId - ms1LevelId;

            if (startRtIndex < 0) startRtIndex = 0;
            if (endRtIndex > endScanNum) endRtIndex = endScanNum;

            int counter = startRtIndex;

            while (counter <= endRtIndex)
            {
                spectrum = spectrumCollection[counter];
                massSpectra = spectrum.Spectrum;

                for(var i = 0; i < focusedMassList.Count; i++)
                {
                    sum = 0;
                    var focusedMass = focusedMassList[i];
                    startMsIndex = GetMs2StartIndex(focusedMass - massTolerance, massSpectra);

                    for (int j = startMsIndex; j < massSpectra.Length; j++)
                    {
                        if (massSpectra[j].Mz < focusedMass - massTolerance) continue;
                        else if (focusedMass - massTolerance <= massSpectra[j].Mz && massSpectra[j].Mz <= focusedMass + massTolerance) { sum += massSpectra[j].Intensity; }
                        else break;
                    }

                    peaklistlist[i].Add(new double[] { counter, spectrum.ScanStartTime, focusedMass, sum });
                }
                counter += experimentCycleNumber;
            }

            return peaklistlist;
        }



        // returns a list of arrays with the following info:
        //		[scan#, RT, m/z, intensity]
        public static List<double[]> GetMs2Peaklist(ObservableCollection<RawSpectrum> spectrumCollection, 
            double precursorMz, double productMz, float startRt, float endRt, AnalysisParametersBean analysisParametersBean, IonMode ionmode)
        {
            var peaklist = new List<double[]>();
            RawSpectrum spectrum;
            RawPeakElement[] massSpectra;
            var scanPolarity = ionmode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;

            float massTolerance = analysisParametersBean.CentroidMs2Tolerance;
            double sum;

            int startRtIndex = GetRtStartIndex(startRt, spectrumCollection);

            for (int i = startRtIndex; i < spectrumCollection.Count; i++)
            {
                if (spectrumCollection[i].MsLevel <= 1) continue;
                if (spectrumCollection[i].ScanPolarity != scanPolarity) continue;
                //if (spectrumCollection[i].DriftScanNumber > 0) continue;
                if (spectrumCollection[i].ScanStartTime < startRt) continue;
                if (spectrumCollection[i].ScanStartTime > endRt) break;
                if (spectrumCollection[i].Precursor == null) continue;
            
                if (precursorMz - analysisParametersBean.CentroidMs1Tolerance <= spectrumCollection[i].Precursor.SelectedIonMz && spectrumCollection[i].Precursor.SelectedIonMz <= precursorMz + analysisParametersBean.CentroidMs1Tolerance)
                {
                    spectrum = spectrumCollection[i];
                    massSpectra = spectrum.Spectrum;
                    sum = 0;

                    if (massSpectra != null && massSpectra.Length != 0) {
                        var startMsIndex = GetMs2StartIndex(productMz - massTolerance, massSpectra);

                        for (int j = startMsIndex; j < massSpectra.Length; j++) {
                            if (massSpectra[j].Mz < productMz - massTolerance) continue;
                            else if (productMz - massTolerance <= massSpectra[j].Mz && massSpectra[j].Mz <= productMz + massTolerance) { sum += massSpectra[j].Intensity; }
                            else break;
                        }
                    }

                    peaklist.Add(new double[] { i, spectrum.ScanStartTime, 0, sum });
                }

            }

            return peaklist;
        }

		// returns a list of arrays with the following info:
		//		[scan#, RT, m/z, intensity]
		public static List<double[]> GetMs2Peaklist(ObservableCollection<RawSpectrum> spectrumCollection, int focusedScanNumber, int ms2LevelId, int experimentCycleNumber, float startRt, float endRt, double focusedMass, AnalysisParametersBean analysisParametersBean)
        {
            var peaklist = new List<double[]>();
            RawSpectrum spectrum;
            RawPeakElement[] massSpectra;

            float massTolerance = analysisParametersBean.CentroidMs2Tolerance;
            double sum;

            int startMsIndex;
            int startRtIndex = GetRtStartIndex(startRt, spectrumCollection);

            for (int i = startRtIndex; i < spectrumCollection.Count; i++)
            {
                if (spectrumCollection[i].ScanStartTime < startRt) continue;
                if (spectrumCollection[i].ScanStartTime > endRt) break;
                //if (spectrumCollection[i].DriftScanNumber > 0) continue;

                if (i % experimentCycleNumber != focusedScanNumber % experimentCycleNumber) continue;

                spectrum = spectrumCollection[i];
                massSpectra = spectrum.Spectrum;
                sum = 0;

                startMsIndex = GetMs2StartIndex(focusedMass - massTolerance, massSpectra);

                for (int j = startMsIndex; j < massSpectra.Length; j++)
                {
                    if (massSpectra[j].Mz < focusedMass - massTolerance) continue;
                    else if (focusedMass - massTolerance <= massSpectra[j].Mz && massSpectra[j].Mz <= focusedMass + massTolerance) { sum += massSpectra[j].Intensity; }
                    else break;
                }

                peaklist.Add(new double[] { i, spectrum.ScanStartTime, focusedMass, sum });
            }
            return peaklist;
        }

        // for IM-DIA
        // accumulating RT window
        // returns a list of arrays with the following info:
        // [scan#, RT, m/z, intensity]
        public static List<List<double[]>> GetAccumulatedMs2Peaklist(ObservableCollection<RawSpectrum> spectrumCollection,
            List<double[]> ms2spectrum, float startRt, float endRt, AnalysisParametersBean analysisParametersBean, IonMode ionmode)
        {
            int binMultiplyFactor = 1000;
            float massTolerance = analysisParametersBean.CentroidMs2Tolerance;

            var peaklistlist = new List<List<double[]>>();
            for (var j = 0; j < ms2spectrum.Count; j++)
            {
                peaklistlist.Add(new List<double[]>());
            }

            RawSpectrum spectrum;
            RawPeakElement[] massSpectra;
            
            var scanPolarity = ionmode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;

            var rtIndexDic = new Dictionary<int, int>();

            double sum;
            int startRtIndex = GetRtStartIndex(startRt, spectrumCollection);
            int rtCounter = 0, targetIndex = 0;
            for (int i = startRtIndex; i < spectrumCollection.Count; i++)
            {
                if (spectrumCollection[i].MsLevel <= 1) continue;
                if (spectrumCollection[i].ScanPolarity != scanPolarity) continue;
                //if (spectrumCollection[i].DriftScanNumber > 1) continue;
                if (spectrumCollection[i].ScanStartTime < startRt) continue;
                if (spectrumCollection[i].ScanStartTime > endRt) break;

                var rtBin = (int)(spectrumCollection[i].ScanStartTime * binMultiplyFactor + 0.5);
                var shouldAddList = false;
                if (rtIndexDic.ContainsKey(rtBin))
                {
                    targetIndex = rtIndexDic[rtBin];
                }
                else
                {
                    shouldAddList = true;
                    targetIndex = rtCounter++;
                    rtIndexDic.Add(rtBin, targetIndex);
                }
            
                spectrum = spectrumCollection[i];
                massSpectra = spectrum.Spectrum;
                if (massSpectra != null && massSpectra.Length != 0)
                {
                    for (var j = 0; j < ms2spectrum.Count; j++)
                    {
                        var productMz = ms2spectrum[j][0];
                        var startMsIndex = GetMs2StartIndex(productMz - massTolerance, massSpectra);
                        sum = 0;
                        for (int k = startMsIndex; k < massSpectra.Length; k++)
                        {
                            if (massSpectra[k].Mz < productMz - massTolerance) continue;
                            else if (productMz - massTolerance <= massSpectra[k].Mz && massSpectra[k].Mz <= productMz + massTolerance) { sum += massSpectra[k].Intensity; }
                            else break;
                        }

                        if (shouldAddList)
                        {
                            peaklistlist[j].Add(new double[] { targetIndex, spectrum.ScanStartTime, productMz, sum });
                        }
                        else
                        {
                            peaklistlist[j][targetIndex][3] += sum;
                        }
                    }
                }
            }
            return peaklistlist;
        }


        public static int GetMs1StartIndex(float focusedMass, float ms1Tolerance, ObservableCollection<double[]> ms1Spectra)
        {
            if (ms1Spectra.Count == 0) return 0;
            float targetMass = focusedMass - ms1Tolerance;
            int startIndex = 0, endIndex = ms1Spectra.Count - 1;
            int counter = 0;

            if (targetMass > ms1Spectra[endIndex][0]) return endIndex;
           
            while (counter < 10)
            {
                if (ms1Spectra[startIndex][0] <= targetMass && targetMass < ms1Spectra[(startIndex + endIndex) / 2][0])
                {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (ms1Spectra[(startIndex + endIndex) / 2][0] <= targetMass && targetMass < ms1Spectra[endIndex][0])
                {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        public static int GetMs2StartIndex(double targetMass, RawPeakElement[] massSpectra)
        {
            if (massSpectra == null || massSpectra.Length == 0) return 0;
            int startIndex = 0, endIndex = massSpectra.Length - 1;
            int counter = 0;
            if (targetMass > massSpectra[endIndex].Mz) return endIndex;
            while (counter < 10)
            {
                if (massSpectra[startIndex].Mz == targetMass) return startIndex;
                if (massSpectra[endIndex].Mz == targetMass) return endIndex;
                if (massSpectra[startIndex].Mz <= targetMass && targetMass < massSpectra[(startIndex + endIndex) / 2].Mz)
                {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (massSpectra[(startIndex + endIndex) / 2].Mz <= targetMass && targetMass < massSpectra[endIndex].Mz)
                {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        public static int GetMs2StartIndex(double targetMass, ObservableCollection<double[]> massSpectra)
        {
            if (massSpectra.Count == 0) return 0;
            int startIndex = 0, endIndex = massSpectra.Count - 1;
            int counter = 0;
            if (targetMass > massSpectra[endIndex][0]) return endIndex;
            while (counter < 5)
            {
                if (massSpectra[startIndex][0] <= targetMass && targetMass < massSpectra[(startIndex + endIndex) / 2][0])
                {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (massSpectra[(startIndex + endIndex) / 2][0] <= targetMass && targetMass < massSpectra[endIndex][0])
                {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        public static int GetMs2StartIndex(double targetMass, List<MzIntensityCommentBean> massSpectra)
        {
            if (massSpectra.Count == 0) return 0;
            int startIndex = 0, endIndex = massSpectra.Count - 1;
            int counter = 0;
            if (targetMass > massSpectra[endIndex].Mz) return endIndex;
            
            while (counter < 3)
            {
                if (massSpectra[startIndex].Mz <= targetMass && targetMass < massSpectra[(startIndex + endIndex) / 2].Mz)
                {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (massSpectra[(startIndex + endIndex) / 2].Mz <= targetMass && targetMass < massSpectra[endIndex].Mz)
                {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        public static int GetRtStartIndex(double targetRt, ObservableCollection<RawSpectrum> spectrumCollection)
        {
            int startIndex = 0, endIndex = spectrumCollection.Count - 1;

            int counter = 0;
            while (counter < 10)
            {
                if (spectrumCollection[startIndex].ScanStartTime <= targetRt && targetRt < spectrumCollection[(startIndex + endIndex) / 2].ScanStartTime)
                {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (spectrumCollection[(startIndex + endIndex) / 2].ScanStartTime <= targetRt && targetRt < spectrumCollection[endIndex].ScanStartTime)
                {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        public static ObservableCollection<double[]> GetMsMsSpectra(ObservableCollection<RawSpectrum> spectrumCollection, int scanNumber)
        {
            if (scanNumber < 0) return new ObservableCollection<double[]>();

            var masslist = new ObservableCollection<double[]>();

            RawSpectrum spectrum;
            RawPeakElement[] massSpectra;

            spectrum = spectrumCollection[scanNumber];
            massSpectra = spectrum.Spectrum;

            for (int i = 0; i < massSpectra.Length; i++)
                masslist.Add(new double[] { massSpectra[i].Mz, massSpectra[i].Intensity });

            return masslist;
        }

        
        public static int GetDatabaseStartIndex(float accurateMass, float ms1Tolerance, List<MspFormatCompoundInformationBean> mspFormatCompoundInformationBeanList)
        {
            float targetMass = accurateMass - ms1Tolerance;
            int startIndex = 0, endIndex = mspFormatCompoundInformationBeanList.Count - 1;
            if (targetMass > mspFormatCompoundInformationBeanList[endIndex].PrecursorMz) return endIndex;

            int counter = 0;
            while (counter < 10)
            {
                if (mspFormatCompoundInformationBeanList[startIndex].PrecursorMz <= targetMass && targetMass < mspFormatCompoundInformationBeanList[(startIndex + endIndex) / 2].PrecursorMz)
                {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (mspFormatCompoundInformationBeanList[(startIndex + endIndex) / 2].PrecursorMz <= targetMass && targetMass < mspFormatCompoundInformationBeanList[endIndex].PrecursorMz)
                {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        public static int GetDatabaseStartIndex(float accurateMass, float ms1Tolerance, List<PostIdentificatioinReferenceBean> postIdentificationReferenceBeanList)
        {
            float targetMass = accurateMass - ms1Tolerance;
            int startIndex = 0, endIndex = postIdentificationReferenceBeanList.Count - 1;
            if (targetMass > postIdentificationReferenceBeanList[endIndex].AccurateMass) return endIndex;

            int counter = 0;
            while (counter < 5)
            {
                if (postIdentificationReferenceBeanList[startIndex].AccurateMass <= targetMass && targetMass < postIdentificationReferenceBeanList[(startIndex + endIndex) / 2].AccurateMass)
                {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (postIdentificationReferenceBeanList[(startIndex + endIndex) / 2].AccurateMass <= targetMass && targetMass < postIdentificationReferenceBeanList[endIndex].AccurateMass)
                {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        private static int GetScanStartIndexByRt(float targetRt, ObservableCollection<RawSpectrum> spectrumCollection) {
            int startIndex = 0, endIndex = spectrumCollection.Count - 1;

            int counter = 0;
            int limit = spectrumCollection.Count > 50000 ? 20 : 10;
            while (counter < limit) {
                if (spectrumCollection[startIndex].ScanStartTime <= targetRt && targetRt < spectrumCollection[(startIndex + endIndex) / 2].ScanStartTime) {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (spectrumCollection[(startIndex + endIndex) / 2].ScanStartTime <= targetRt && targetRt < spectrumCollection[endIndex].ScanStartTime) {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        public static int GetPeakAreaIntensityListStartIndex(List<PeakAreaBean> peakAreaBeanList, float targetMass)
        {
            int startIndex = 0, endIndex = peakAreaBeanList.Count - 1;

            int counter = 0;
            while (counter < 5)
            {
                if (peakAreaBeanList[startIndex].AccurateMass <= targetMass && targetMass < peakAreaBeanList[(startIndex + endIndex) / 2].AccurateMass)
                {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (peakAreaBeanList[(startIndex + endIndex) / 2].AccurateMass <= targetMass && targetMass < peakAreaBeanList[endIndex].AccurateMass)
                {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        //public static List<AdductIonInformationBean> GetAdductIonInformationList(IonMode ionMode) {
        //    //Uri fileUri;
        //    //if (ionMode == IonMode.Positive)
        //    //    fileUri = new Uri("/Resources/AdductIonResource_Positive.txt", UriKind.Relative);
        //    //else
        //    //    fileUri = new Uri("/Resources/AdductIonResource_Negative.txt", UriKind.Relative);

        //    //var info = Application.GetResourceStream(fileUri);

        //    var adductListString = string.Empty;
        //    if (ionMode == IonMode.Positive)
        //        adductListString = Properties.Resources.AdductIonResource_Positive;
        //    else
        //        adductListString = Properties.Resources.AdductIonResource_Negative;
        //    var adductList = adductListString.Replace("\r\n", "\n").Split(new[] { '\n', '\r' });

        //    List<AdductIonInformationBean> adductIonInformationBeanList = new List<AdductIonInformationBean>();
        //    AdductIonInformationBean adductIonInformationBean = new AdductIonInformationBean();

        //    bool checker = true;

        //    for (int i = 1; i < adductList.Length; i++) {
        //        var line = adductList[i];
        //        if (line == "") break;
        //        var lineArray = line.Split('\t');

        //        adductIonInformationBean = new AdductIonInformationBean();
        //        adductIonInformationBean.AdductName = lineArray[0];
        //        adductIonInformationBean.Charge = int.Parse(lineArray[1]);
        //        adductIonInformationBean.AccurateMass = double.Parse(lineArray[2]);
        //        adductIonInformationBean.IonMode = ionMode;

        //        if (checker) { adductIonInformationBean.Included = true; checker = false; }
        //        else adductIonInformationBean.Included = false;

        //        adductIonInformationBeanList.Add(adductIonInformationBean);
        //    }

        //    //using (var sr = new StreamReader(info.Stream)) {
        //    //    string line;
        //    //    string[] lineArray;
        //    //    sr.ReadLine();
        //    //    while (sr.Peek() > -1) {
        //    //        line = sr.ReadLine();
        //    //        if (line == "") break;
        //    //        lineArray = line.Split('\t');

        //    //        adductIonInformationBean = new AdductIonInformationBean();
        //    //        adductIonInformationBean.AdductName = lineArray[0];
        //    //        adductIonInformationBean.Charge = int.Parse(lineArray[1]);
        //    //        adductIonInformationBean.AccurateMass = double.Parse(lineArray[2]);
        //    //        adductIonInformationBean.IonMode = ionMode;

        //    //        if (checker) { adductIonInformationBean.Included = true; checker = false; }
        //    //        else adductIonInformationBean.Included = false;

        //    //        adductIonInformationBeanList.Add(adductIonInformationBean);
        //    //    }
        //    //}

        //    return adductIonInformationBeanList;
        //}

        public static List<double[]> GetDriftChromatogramByScanRtMz(ObservableCollection<RawSpectrum> spectrumCollection,
           int scanID, float rt, float rtWidth, float mz, float mztol) {

            var driftBinToPeak = new Dictionary<int, double[]>();
            var driftBinToBasePeak = new Dictionary<int, double[]>();
            //accumulating peaks from peak top to peak left
            for (int i = scanID + 1; i >= 0; i--) {
                var spectrum = spectrumCollection[i];
                if (spectrum.MsLevel > 1) continue;
                var massSpectra = spectrum.Spectrum;
                var retention = spectrum.ScanStartTime;
                var driftTime = spectrum.DriftTime;
                var driftIndex = spectrum.OriginalIndex;
                var driftBin = (int)(driftTime * 1000);
                if (retention < rt - rtWidth * 0.5) break;

                var basepeakMz = 0.0;
                var basepeakIntensity = 0.0;
                var intensity = GetIonAbundanceOfMzInSpectrum(massSpectra, mz, mztol,
                    out basepeakMz, out basepeakIntensity);
                if (!driftBinToPeak.ContainsKey(driftBin)) {
                    driftBinToPeak[driftBin] = new double[4] { driftIndex, driftTime, basepeakMz, intensity };
                    driftBinToBasePeak[driftBin] = new double[2] { basepeakMz, basepeakIntensity };
                }
                else {
                    driftBinToPeak[driftBin][3] += intensity;
                    if (driftBinToBasePeak[driftBin][1] < basepeakIntensity) {
                        driftBinToBasePeak[driftBin][0] = basepeakMz;
                        driftBinToBasePeak[driftBin][1] = basepeakIntensity;
                        driftBinToPeak[driftBin][2] = basepeakMz;
                    }
                }
            }

            //accumulating peaks from peak top to peak right
            for (int i = scanID + 2; i < spectrumCollection.Count; i++) {
                var spectrum = spectrumCollection[i];
                if (spectrum.MsLevel > 1) continue;
                var massSpectra = spectrum.Spectrum;
                var retention = spectrum.ScanStartTime;
                var driftTime = spectrum.DriftTime;
                var driftIndex = spectrum.OriginalIndex;
                var driftBin = (int)(driftTime * 1000);
                if (retention > rt + rtWidth * 0.5) break;

                var basepeakMz = 0.0;
                var basepeakIntensity = 0.0;
                var intensity = GetIonAbundanceOfMzInSpectrum(massSpectra, mz, mztol,
                   out basepeakMz, out basepeakIntensity);
                if (!driftBinToPeak.ContainsKey(driftBin)) {
                    driftBinToPeak[driftBin] = new double[4] { driftIndex, driftTime, basepeakMz, intensity };
                    driftBinToBasePeak[driftBin] = new double[2] { basepeakMz, basepeakIntensity };
                }
                else {
                    driftBinToPeak[driftBin][3] += intensity;
                    if (driftBinToBasePeak[driftBin][1] < basepeakIntensity) {
                        driftBinToBasePeak[driftBin][0] = basepeakMz;
                        driftBinToBasePeak[driftBin][1] = basepeakIntensity;
                        driftBinToPeak[driftBin][2] = basepeakMz;
                    }
                }
            }

            var peaklist = new List<double[]>();
            foreach (var value in driftBinToPeak.Values) {
                peaklist.Add(value);
            }

            peaklist = peaklist.OrderBy(n => n[1]).ToList();
            return peaklist;
        }

        public static List<double[]> GetDriftChromatogramByRtMz(ObservableCollection<RawSpectrum> spectrumCollection,
           float rt, float rtWidth, float mz, float mztol, float minDt, float maxDt) {

            var startID = GetScanStartIndexByRt(rt - rtWidth * 0.5F, spectrumCollection);
            var driftBinToPeak = new Dictionary<int, double[]>();
            var driftBinToBasePeak = new Dictionary<int, double[]>();

            for (int i = startID; i < spectrumCollection.Count; i++) {
               
                var spectrum = spectrumCollection[i];
                var massSpectra = spectrum.Spectrum;
                var retention = spectrum.ScanStartTime;
                var driftTime = spectrum.DriftTime;
                var driftScan = spectrum.DriftScanNumber;
                var driftBin = (int)(driftTime * 1000);

                //if (i > 1213450) {
                //    Debug.WriteLine("id {0} rt {1}", i, spectrum.ScanStartTime);
                //}

                if (retention < rt - rtWidth * 0.5) continue;
                if (driftTime < minDt || driftTime > maxDt) continue; 
                if (retention > rt + rtWidth * 0.5) break;

                var basepeakMz = 0.0;
                var basepeakIntensity = 0.0;
                var intensity = GetIonAbundanceOfMzInSpectrum(massSpectra, mz, mztol,
                    out basepeakMz, out basepeakIntensity);
                if (!driftBinToPeak.ContainsKey(driftBin)) {
                    driftBinToPeak[driftBin] = new double[4] { driftScan, driftTime, basepeakMz, intensity };
                    driftBinToBasePeak[driftBin] = new double[2] { basepeakMz, basepeakIntensity };
                }
                else {
                    driftBinToPeak[driftBin][3] += intensity;
                    if (driftBinToBasePeak[driftBin][1] < basepeakIntensity) {
                        driftBinToBasePeak[driftBin][0] = basepeakMz;
                        driftBinToBasePeak[driftBin][1] = basepeakIntensity;
                        driftBinToPeak[driftBin][2] = basepeakMz;
                    }
                }
            }
            var peaklist = new List<double[]>();
            foreach (var value in driftBinToPeak.Values) {
                peaklist.Add(value);
            }

            peaklist = peaklist.OrderBy(n => n[1]).ToList();
            return peaklist;
        }

        public static List<double[]> CalcAccumulatedMs2Spectra(ObservableCollection<RawSpectrum> spectrumCollection,
        PeakAreaBean pSpot, DriftSpotBean dSpot, double mzTol)
        {
            var rt = pSpot.RtAtPeakTop;
            var rtLeft = pSpot.RtAtLeftPeakEdge;
            var rtRight = pSpot.RtAtRightPeakEdge;
            var rtRange = 1f;

            if(rtRight - rt > rtRange)
            {
                //Console.WriteLine("Peak: " + pSpot.PeakID + " has large peak width (left: " + rtLeft + ", top: " + rt + ", right: " + rtRight + ").");
                rtRight = rt + rtRange;
            }
            if (rt - rtLeft > rtRange)
            {
                //Console.WriteLine("Peak: " + pSpot.PeakID + " has large peak width (left: " + rtLeft + ", top: " + rt + ", right: " + rtRight + ").");
                rtLeft = rt - rtRange;
            }

            var mz = pSpot.AccurateMass;
            var scanID = dSpot.Ms1LevelDatapointNumber;
            var dataPointDriftBin = (int)(dSpot.DriftTimeAtPeakTop * 1000);

            var spectrumBin = new Dictionary<int, double[]>();
            //accumulating peaks from peak top to peak left
            for (int i = scanID; i >= 0; i--)
            {
                var spectrum = spectrumCollection[i];
                if (spectrum.MsLevel == 1) continue;

                var driftTime = spectrum.DriftTime;
                var driftBin = (int)(driftTime * 1000);
                if (driftBin != dataPointDriftBin) continue;

                var retention = spectrum.ScanStartTime;
                if (retention < rtLeft) break;

                var massSpectra = spectrum.Spectrum;
                foreach (var s in massSpectra)
                {
                    var massBin = (int)(s.Mz * 1000);
                    if (!spectrumBin.ContainsKey(massBin))
                    {
                        spectrumBin[massBin] = new double[3] { s.Mz, s.Intensity, s.Intensity };
                    }
                    else
                    {
                        spectrumBin[massBin][1] += s.Intensity;
                        if (spectrumBin[massBin][2] < s.Intensity)
                        {
                            spectrumBin[massBin][0] = s.Mz;
                            spectrumBin[massBin][2] = s.Intensity;
                        }
                    }
                }
            }

            for (int i = scanID + 1; i < spectrumCollection.Count; i++)
            {
                var spectrum = spectrumCollection[i];
                if (spectrum.MsLevel == 1) continue;

                var driftTime = spectrum.DriftTime;
                var driftBin = (int)(driftTime * 1000);
                if (driftBin != dataPointDriftBin) continue;

                var retention = spectrum.ScanStartTime;
                if (retention > rtRight) break;

                var massSpectra = spectrum.Spectrum;
                foreach (var s in massSpectra)
                {
                    var massBin = (int)(s.Mz * 1000);
                    if (!spectrumBin.ContainsKey(massBin))
                    {
                        // [accurate mass, intensity, max intensity]
                        spectrumBin[massBin] = new double[3] { s.Mz, s.Intensity, s.Intensity };
                    }
                    else
                    {
                        spectrumBin[massBin][1] += s.Intensity;
                        if (spectrumBin[massBin][2] < s.Intensity)
                        {
                            spectrumBin[massBin][0] = s.Mz;
                            spectrumBin[massBin][2] = s.Intensity;
                        }
                    }
                }
            }

            var peaklist = new List<double[]>();
            foreach (var value in spectrumBin.Values)
            {
                peaklist.Add(new double[] { value[0], value[1] });
            }
            peaklist = peaklist.OrderBy(n => n[1]).ToList();
            return peaklist;
        }

        public static List<double[]> GetAccumulatedMs2Spectra(Rfx.Riken.OsakaUniv.IonMobility.Spectra spectra, ObservableCollection<RawSpectrum> spectrumCollection,
            DriftSpotBean driftSpot, PeakAreaBean peakSpot, AnalysisParametersBean param, ProjectPropertyBean project)
        {
            if (spectra.SpectrumDic.ContainsKey(driftSpot.Ms1LevelDatapointNumber))
            {
                return spectra.SpectrumDic[(driftSpot.Ms1LevelDatapointNumber)].PeakList;
            }
            else
            {
                var centroid = GetAccumulatedMs2Spectra(spectrumCollection, driftSpot, peakSpot, param, project);
                spectra.SpectrumDic.Add(driftSpot.Ms1LevelDatapointNumber, new Rfx.Riken.OsakaUniv.IonMobility.Spectrum() { PeakList = centroid });
                return centroid;
            }
        }

        public static List<double[]> GetAccumulatedMs2Spectra(ObservableCollection<RawSpectrum> spectrumCollection,
            DriftSpotBean driftSpot, PeakAreaBean peakSpot, AnalysisParametersBean param, ProjectPropertyBean project)
        { 
            var massSpectrum = DataAccessLcUtility.CalcAccumulatedMs2Spectra(spectrumCollection, peakSpot, driftSpot, param.CentroidMs1Tolerance);
            var centroidSpectrum = massSpectrum;
            if (project.DataTypeMS2 == DataType.Profile && massSpectrum.Count > 0)
            {
                centroidSpectrum = SpectralCentroiding.PeakDetectionBasedCentroid(massSpectrum);
            }
            return centroidSpectrum;
        }


        // List<List<[driftTime, AccumulatedIntensity]> (mz<[driftTime, intensity]>)
        public static List<List<double[]>> GetAccumulatedMs2PeakListList(ObservableCollection<RawSpectrum> spectrumCollection,
             PeakAreaBean pSpot, List<double[]> curatedSpectrum, double minDriftTime, double maxDriftTime, IonMode ionMode)
        {
            var ms2peaklistlist = new List<List<double[]>>();
            var scanPolarity = ionMode == IonMode.Positive ? ScanPolarity.Positive : ScanPolarity.Negative;

            var rt = pSpot.RtAtPeakTop;
            var rtLeft = pSpot.RtAtLeftPeakEdge;
            var rtRight = pSpot.RtAtRightPeakEdge;

            var binMultiplyFactor = 1000;
            var accumulatedRtRange = 1f;
            if (rtRight - rt > accumulatedRtRange)
            {
                //Console.WriteLine("Peak: " + pSpot.PeakID + " has large peak width (left: " + rtLeft + ", top: " + rt + ", right: " + rtRight + ").");
                rtRight = rt + accumulatedRtRange;
            }
            if (rt - rtLeft > accumulatedRtRange)
            {
                //Console.WriteLine("Peak: " + pSpot.PeakID + " has large peak width (left: " + rtLeft + ", top: " + rt + ", right: " + rtRight + ").");
                rtLeft = rt - accumulatedRtRange;
            }

            var mz = pSpot.AccurateMass;
            var scanID = pSpot.Ms1LevelDatapointNumber;

            // <mzBin, <driftTimeIndex, [driftTimeBin, accumulatedIntensity]>>
            var chromatogramBin = new Dictionary<int, Dictionary<int, double[]>>();

            // <mzBin, <driftTimeBin, driftTimeIndex>>
            var driftTimeIndexDic = new Dictionary<int, Dictionary<int, int>>();

            // <mz, driftTimeIndex>
            var driftTimeCounter = new Dictionary<int, int>();

            var driftTimeBinSet = new HashSet<int>();
            //set initial mz
            foreach (var peak in curatedSpectrum)
            {
                var massBin = (int)(peak[0] * binMultiplyFactor + 0.5);
                var index = 0;
                driftTimeCounter.Add(massBin, index + 1);
            }

            //accumulating peaks from peak top to peak left
            for (int i = scanID; i >= 0; i--)
            {
                var spectrum = spectrumCollection[i];
                if (spectrum.ScanPolarity != scanPolarity) continue;
                if (spectrum.MsLevel <= 1) continue;
                if (spectrum.ScanStartTime < rtLeft) break;
                if (spectrum.DriftTime < minDriftTime || spectrum.DriftTime > maxDriftTime) continue;
                var massSpectra = spectrum.Spectrum;
                foreach (var s in massSpectra)
                {
                    var massBin = (int)(s.Mz * binMultiplyFactor + 0.5);
                    var driftBin = (int)(spectrum.DriftTime * binMultiplyFactor + 0.5);
                    
                    driftTimeBinSet.Add(driftBin);
                    if (!driftTimeCounter.ContainsKey(massBin)) continue;
                    if (driftTimeIndexDic.ContainsKey(massBin))
                    {
                        if (driftTimeIndexDic[massBin].ContainsKey(driftBin))
                        {
                            chromatogramBin[massBin][driftTimeIndexDic[massBin][driftBin]][1] += s.Intensity;
                        }
                        else
                        {
                            driftTimeIndexDic[massBin].Add(driftBin, driftTimeCounter[massBin]);
                            chromatogramBin[massBin][driftTimeIndexDic[massBin][driftBin]] = new double[] { driftBin, s.Intensity };
                            driftTimeCounter[massBin] += 1;
                        }
                    }
                    else
                    {
                        // <driftBint, driftTimeIndex>
                        var tmp1 = new Dictionary<int, int>();
                        tmp1.Add(driftBin, 0);
                        driftTimeIndexDic.Add(massBin, tmp1);

                        // <driftTimeIndex, [driftBin, intensity]>
                        var tmp2 = new Dictionary<int, double[]>();
                        tmp2.Add(0, new double[] { driftBin, s.Intensity });
                        chromatogramBin.Add(massBin, tmp2);
                    }
                }
            }

            for (int i = scanID + 1; i < spectrumCollection.Count; i++)
            {
                var spectrum = spectrumCollection[i];
                if (spectrum.ScanPolarity != scanPolarity) continue;
                if (spectrum.MsLevel == 1) continue;
                if (spectrum.DriftTime < minDriftTime || spectrum.DriftTime > maxDriftTime) continue;
                if (spectrum.ScanStartTime > rtRight) break;

                var massSpectra = spectrum.Spectrum;

                foreach (var s in massSpectra)
                {
                    var massBin = (int)(s.Mz * binMultiplyFactor + 0.5);
                    var driftBin = (int)(spectrum.DriftTime * binMultiplyFactor + 0.5);
                    driftTimeBinSet.Add(driftBin);
                    if (!driftTimeCounter.ContainsKey(massBin)) continue;

                    if (driftTimeIndexDic.ContainsKey(massBin))
                    {
                        if (driftTimeIndexDic[massBin].ContainsKey(driftBin))
                        {
                            chromatogramBin[massBin][driftTimeIndexDic[massBin][driftBin]][1] += s.Intensity;
                        }
                        else
                        {
                            driftTimeIndexDic[massBin].Add(driftBin, driftTimeCounter[massBin]);
                            chromatogramBin[massBin][driftTimeIndexDic[massBin][driftBin]] = new double[] { driftBin, s.Intensity };
                            driftTimeCounter[massBin] += 1;
                        }
                    }
                    else
                    {
                        // <driftBint, driftTimeIndex>
                        var tmp1 = new Dictionary<int, int>();
                        tmp1.Add(driftBin, 0);
                        driftTimeIndexDic.Add(massBin, tmp1);

                        // <driftTimeIndex, [driftBin, intensity]>
                        var tmp2 = new Dictionary<int, double[]>();
                        tmp2.Add(0, new double[] { driftBin, s.Intensity });
                        chromatogramBin.Add(massBin, tmp2);
                    }
                }
            }

            foreach (var mzBin in chromatogramBin.Keys)
            {
                var peaklist = new List<double[]>();
                var targetMz = Math.Round((double)mzBin / binMultiplyFactor, 3);
                // <driftTimeIndex, [driftBin, accumulatedIntensity]>
                var targetChromato = chromatogramBin[mzBin];
                var counter = 0;
                var tmpDriftTimeBinSet = new HashSet<int>();
                foreach(var values in targetChromato.Values)
                {
                    tmpDriftTimeBinSet.Add((int)(values[0] + 0.5));
                    var driftTime = Math.Round(values[0] / binMultiplyFactor, 3);
                    peaklist.Add(new double[] { 0, driftTime, targetMz, values[1] });
                }
                foreach(var df in driftTimeBinSet.Except(tmpDriftTimeBinSet))
                {
                    // add not detected driftTime
                    var driftTime = Math.Round((double)df / binMultiplyFactor, 3);
                    peaklist.Add(new double[] { 0, driftTime, targetMz, 0});
                }
                var sortedPeaklist = peaklist.OrderBy(n => n[1]).ToList();
                var ms2peaklist = new List<double[]>();
                foreach (var peaks in sortedPeaklist)
                {
                    ms2peaklist.Add(new double[] { counter++, peaks[1], peaks[2], peaks[3] });
                }
                ms2peaklistlist.Add(ms2peaklist);
            }
            return ms2peaklistlist;
        }

        public static int GetTotalPeakSpot(ObservableCollection<PeakAreaBean> spots) {
            var counter = 0;
            foreach (var spot in spots) {
                counter++;
                foreach (var drift in spot.DriftSpots) {
                    counter++;
                }
            }

            return counter;
        }

        public static int GetTotalPeakSpot(ObservableCollection<AlignmentPropertyBean> spots) {
            var counter = 0;
            foreach (var spot in spots) {
                counter++;
                foreach (var drift in spot.AlignedDriftSpots) {
                    counter++;
                }
            }

            return counter;
        }

        public static PeakAreaBean GetPeakAreaBean(ObservableCollection<AnalysisFileBean> files, int fileID, int peakID)
        {
            var analysisFile = files[fileID];
            DataStorageLcUtility.SetPeakAreaBeanCollection(analysisFile, analysisFile.AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);
            var peakareaBean = analysisFile.PeakAreaBeanCollection[peakID];
            DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(analysisFile);

            return peakareaBean;
        }
    }
}
