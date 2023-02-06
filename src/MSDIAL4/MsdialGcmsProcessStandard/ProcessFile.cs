using Msdial.Gcms.Dataprocess.Algorithm;
using Msdial.Gcms.Dataprocess.Utility;
using Rfx.Riken.OsakaUniv;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using CompMs.Common.MessagePack;

namespace Msdial.Gcms.Dataprocess {
	public sealed class ProcessFile
    {
        private ProcessFile() { }

        public static void Execute(ProjectPropertyBean projectProperty, RdamPropertyBean rdamProperty, AnalysisFileBean analysisFile, AnalysisParamOfMsdialGcms param, 
            List<MspFormatCompoundInformationBean> mspDB, Action<int> reportAction, out string error)
        {
            error = string.Empty;
            var fileID = rdamProperty.RdamFilePath_RdamFileID[analysisFile.AnalysisFilePropertyBean.AnalysisFilePath];
            var measurementID = rdamProperty.RdamFileContentBeanCollection[fileID].FileID_MeasurementID[analysisFile.AnalysisFilePropertyBean.AnalysisFileId];

            using (var rawDataAccess = new RawDataAccess(analysisFile.AnalysisFilePropertyBean.AnalysisFilePath, measurementID, false, false, true))
            {
                var spectrumList = DataAccessGcUtility.GetRdamSpectrumList(rawDataAccess);
                if (spectrumList == null) return;

                var peakAreaList = new List<PeakAreaBean>();
                var ms1DecResults = new List<MS1DecResult>();

				try {
					if (param.ProcessOption == ProcessOption.All) {
	                    peakAreaList = PeakSpotting.GetPeakSpots(spectrumList, param, reportAction);
#if (DEBUG)
                        //Exporter.export(new ObservableCollection<RAW_Spectrum>(spectrumList), "msdial_rawdata.txt");
                        //Exporter.export(new ObservableCollection<PeakAreaBean>(peakAreaList), "msdial_GC_detectedPeaks.txt");
#endif
						ms1DecResults = Deconvolution.GetMS1DecResults(spectrumList, peakAreaList, param, reportAction);
#if (DEBUG)
						//Exporter.export(ms1DecResults, "msdial_GC_decResults.txt");
#endif
					} else if (param.ProcessOption == ProcessOption.IdentificationPlusAlignment) {
	                    peakAreaList = DataStorageGcUtility.GetPeakAreaList(analysisFile.AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);

						ms1DecResults = DataStorageGcUtility.ReadMS1DecResults(analysisFile.AnalysisFilePropertyBean.DeconvolutionFilePath);
						DataRefreshGcUtility.RefreshIdentificationProperties(ms1DecResults);
					}
				} 
                catch (Exception ex) {
                    ex.Message.ToString();
                    error = "We found an error reading the deconvolution files.\nThis is probably due to changes in the data format.\nPlease re-process your data.";
                    //Console.WriteLine(error);
                    //MessageBox.Show("We found an error reading the deconvolution files.\nThis is probably due to changes in the data format.\nPlease re-process your data.",
                    //    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
				}

				Identification.MainProcess(ms1DecResults, mspDB, param, analysisFile, reportAction);

                MessagePackHandler.SaveToFile<List<PeakAreaBean>>(peakAreaList, analysisFile.AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);

                //DataStorageGcUtility.SaveToXmlFile(peakAreaList, analysisFile.AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath, typeof(List<PeakAreaBean>));

                if (param.IsReplaceQuantmassByUserDefinedValue == true) {

                }

                DataStorageGcUtility.WriteMs1DecResults(analysisFile.AnalysisFilePropertyBean.DeconvolutionFilePath, ms1DecResults);
                reportAction?.Invoke(100);
            }
        }
	}
}
