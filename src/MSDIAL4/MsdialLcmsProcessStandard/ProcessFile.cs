using System;
using Msdial.Lcms.Dataprocess.Utility;
using Msdial.Lcms.Dataprocess.Algorithm;
using Rfx.Riken.OsakaUniv;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.ComponentModel;
using CompMs.RawDataHandler.Core;
using System.Windows;
using CompMs.Common.MessagePack;
using CompMs.Common.DataObj;

namespace Msdial.Lcms.DataProcess {
    public sealed class ProcessFile
    {
        private ProcessFile() { }

        public static void Execute(ProjectPropertyBean projectProperty, RdamPropertyBean rdamProperty, 
            AnalysisFileBean analysisFile, AnalysisParametersBean param, IupacReferenceBean iupac,
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> postIdentificationDB, out string error,
            Action<int> reportAction = null, CancellationToken token = new CancellationToken())
        {
            error = string.Empty;
            var fileID = rdamProperty.RdamFilePath_RdamFileID[analysisFile.AnalysisFilePropertyBean.AnalysisFilePath];
            var measurementID = rdamProperty.RdamFileContentBeanCollection[fileID].FileID_MeasurementID[analysisFile.AnalysisFilePropertyBean.AnalysisFileId];
            System.Console.WriteLine(analysisFile.AnalysisFilePropertyBean.AnalysisFilePath);
            var spectrumCollection = new ObservableCollection<RawSpectrum>();
            var accumulatedMs1SpecCollection = new ObservableCollection<RawSpectrum>();
            using (var rawDataAccess = new RawDataAccess(analysisFile.AnalysisFilePropertyBean.AnalysisFilePath, measurementID, false, false, true, analysisFile.RetentionTimeCorrectionBean.PredictedRt))
            {
                var raw_measurment = DataAccessLcUtility.GetRawDataMeasurement(rawDataAccess);
                spectrumCollection = DataAccessLcUtility.GetAllSpectrumCollection(raw_measurment);
                accumulatedMs1SpecCollection = DataAccessLcUtility.GetAccumulatedMs1SpectrumCollection(raw_measurment);
                var counter = 0;
                while (spectrumCollection == null)
                {
                    System.Threading.Thread.Sleep(2000);
                    raw_measurment = DataAccessLcUtility.GetRawDataMeasurement(rawDataAccess);
                    spectrumCollection = DataAccessLcUtility.GetAllSpectrumCollection(raw_measurment);
                    accumulatedMs1SpecCollection = DataAccessLcUtility.GetAccumulatedMs1SpectrumCollection(raw_measurment);
                    counter++;
                    System.Diagnostics.Debug.WriteLine("Cannot open this file: " + analysisFile.AnalysisFilePropertyBean.AnalysisFilePath);
                    if (counter > 5)
                    {
                        error = "Cannot open this file: " + analysisFile.AnalysisFilePropertyBean.AnalysisFilePath;
                        //MessageBox.Show("Cannot open this file: " + analysisFile.AnalysisFilePropertyBean.AnalysisFilePath, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                if (param.ProcessOption == ProcessOption.All) {
                    if (param.IsIonMobility)
                        analysisFile.PeakAreaBeanCollection = PeakSpotting.GetPeakAreaBeanCollectionAtIonMobilityData(accumulatedMs1SpecCollection, spectrumCollection, param, projectProperty, reportAction);
                    else
                        analysisFile.PeakAreaBeanCollection = PeakSpotting.GetPeakAreaBeanCollection(spectrumCollection, param, projectProperty, reportAction);

                    if (token.IsCancellationRequested) return;

                    System.Console.WriteLine("end peak picking");
                    IsotopeEstimator.SetIsotopeInformation(analysisFile.AnalysisFilePropertyBean.AnalysisFileId, analysisFile.PeakAreaBeanCollection, param, iupac);
                    DataSummarizer.SetDataSummary(analysisFile.DataSummaryBean, spectrumCollection, projectProperty, analysisFile.PeakAreaBeanCollection, param);

                    if (projectProperty.CheckAIF) {
                        for (var i = 1; i < projectProperty.Ms2LevelIdList.Count + 1; i++) {
                            SpectralDeconvolution.WriteMS2DecResult(spectrumCollection, analysisFile.AnalysisFilePropertyBean,
                                analysisFile.PeakAreaBeanCollection, param,
                                projectProperty, analysisFile.DataSummaryBean, iupac, reportAction, token, i);
                        }
                    }else {
                        SpectralDeconvolution.WriteMS2DecResult(spectrumCollection, analysisFile.AnalysisFilePropertyBean,
                            analysisFile.PeakAreaBeanCollection, param,
                            projectProperty, analysisFile.DataSummaryBean, iupac, reportAction, token);
                    }
                    if (token.IsCancellationRequested) return;
                }
                else if (param.ProcessOption == ProcessOption.IdentificationPlusAlignment) {
                    DataStorageLcUtility.SetPeakAreaBeanCollection(analysisFile, analysisFile.AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);
                }

                if (token.IsCancellationRequested)
                {
                    DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(analysisFile);
                    return;
                }

                DataRefreshLcUtility.RefreshIdentificationProperties(analysisFile.PeakAreaBeanCollection);

                var peakAreaList = new List<PeakAreaBean>(analysisFile.PeakAreaBeanCollection);

                if (projectProperty.CheckAIF) {
                    IdentificationForAif.CompoundIdentification(analysisFile.AnalysisFilePropertyBean.DeconvolutionFilePathList, spectrumCollection, peakAreaList, mspDB, param, projectProperty, reportAction);
                }
                else
                    Identification.CompoundIdentification(analysisFile.AnalysisFilePropertyBean.DeconvolutionFilePath, spectrumCollection, peakAreaList, mspDB, param, projectProperty, reportAction);

                Identification.PostCompoundIdentification(peakAreaList, postIdentificationDB, param, projectProperty, analysisFile);

                if (param.IsIonMobility)
                    new PeakCharacterEvaluator().Run(analysisFile, accumulatedMs1SpecCollection, peakAreaList,
                        mspDB, postIdentificationDB, param, projectProperty, reportAction);
                else
                    new PeakCharacterEvaluator().Run(analysisFile, spectrumCollection, peakAreaList,
                        mspDB, postIdentificationDB, param, projectProperty, reportAction);
                //AdductIonEstimator.SetAdductIonInformationFromIdentifiedCompound(peakAreaList, param, projectProperty.IonMode);

                //AdductIonEstimator.SetAdductIonInformation(peakAreaList, param, projectProperty.IonMode);

                MessagePackHandler.SaveToFile<ObservableCollection<PeakAreaBean>>(analysisFile.PeakAreaBeanCollection, analysisFile.AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);

                DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(analysisFile);
                reportAction?.Invoke(100);
            }
        }
    }
}
