using CompMs.Common.DataObj;
using Msdial.Lcms.Dataprocess.Algorithm;
using Msdial.Lcms.Dataprocess.Utility;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace Riken.Metabolomics.MsdialConsoleApp.Export
{
    public sealed class ResultExportForLC {
        private ResultExportForLC() { }

        public static void ExportMs2DecResult(AnalysisFileBean file, string outputFolder, ObservableCollection<RawSpectrum> accumulatedSpectra,
            ObservableCollection<RawSpectrum> spectrumCollection, List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> txtDB,
            AnalysisParametersBean param, ProjectPropertyBean projectProperty) {
            var outputfile = Path.Combine(outputFolder, file.AnalysisFilePropertyBean.AnalysisFileName + ".msdial");
            Console.WriteLine("Exporting peak list data: {0}", outputfile);

            using (StreamWriter sw = new StreamWriter(outputfile, false, Encoding.ASCII)) {
                using (var fs = File.Open(file.AnalysisFilePropertyBean.DeconvolutionFilePath, FileMode.Open, FileAccess.ReadWrite)) {
                    var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);

                    DataStorageLcUtility.SetPeakAreaBeanCollection(file, file.AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);

                    ResultExportLcUtility.WritePeaklistTextHeader(sw, param.IsIonMobility);

                    Console.WriteLine(String.Format("\nDeconvoluted peak count: {0}\n", file.PeakAreaBeanCollection.Count));

                    for (int i = 0; i < file.PeakAreaBeanCollection.Count; i++) {
                        if (param.IsIonMobility) {
                            var peakspot = file.PeakAreaBeanCollection[i];
                            var driftSpots = peakspot.DriftSpots;
                            ResultExportLcUtility.WriteMs2decResultAsTxt(sw, accumulatedSpectra, spectrumCollection, fs, seekpointList,
                                            peakspot, file.PeakAreaBeanCollection, null, peakspot.DriftSpots, mspDB, txtDB, param, projectProperty, 5.0F, true);
                            foreach (var drift in driftSpots) {
                                ResultExportLcUtility.WriteMs2decResultAsTxt(sw, accumulatedSpectra, spectrumCollection, fs, seekpointList,
                                            peakspot, file.PeakAreaBeanCollection, drift, peakspot.DriftSpots, mspDB, txtDB, param, projectProperty, 5.0F, true);
                            }
                        }
                        else {
                            ResultExportLcUtility.WriteMs2decResultAsTxt(sw, spectrumCollection,
                            fs, seekpointList, file.PeakAreaBeanCollection[i], file.PeakAreaBeanCollection, mspDB, txtDB, param, projectProperty, 5.0F, true);
                        }
                    }
                }
            }
        }

        public static void ExportAlignmentResult(string outputFile, AlignmentFileBean alignmentResultFile, AlignmentResultBean alignmentResultBean,
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> txtDB, List<AnalysisFileBean> analysisFiles, AnalysisParametersBean param) {
            using (var fs = File.Open(alignmentResultFile.SpectraFilePath, FileMode.Open, FileAccess.ReadWrite)) {
                var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);

                using (StreamWriter sw = new StreamWriter(outputFile, false, Encoding.ASCII)) {
                    //Header
                    ResultExportLcUtility.WriteDataMatrixHeader(sw, new ObservableCollection<AnalysisFileBean>(analysisFiles));

                    var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;
                    if (param.TrackingIsotopeLabels)
                        alignedSpots = ResultExportLcUtility.GetFilteredAlignedSpotsByIsotpeTrackingResult(alignedSpots, param, param.NonLabeledReferenceID);

                    //From the second
                    for (int i = 0; i < alignedSpots.Count; i++) {

                        if (param.IsRemoveFeatureBasedOnPeakHeightFoldChange && alignedSpots[i].IsBlankFiltered) continue;
                        ResultExportLcUtility.WriteDataMatrixMetaData(sw, alignedSpots[i], alignedSpots, mspDB, txtDB, fs, seekpointList, param);

                        for (int j = 0; j < alignedSpots[i].AlignedPeakPropertyBeanCollection.Count; j++) {
                            if (j == alignedSpots[i].AlignedPeakPropertyBeanCollection.Count - 1)
                                sw.WriteLine(alignedSpots[i].AlignedPeakPropertyBeanCollection[j].Variable);
                            else
                                sw.Write(alignedSpots[i].AlignedPeakPropertyBeanCollection[j].Variable + "\t");
                        }
                    }
                }
            }
        }

        public static void ExportAlignmentResultForIonMobilityData(string outputFile, AlignmentFileBean alignmentResultFile, AlignmentResultBean alignmentResultBean,
            List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> txtDB, List<AnalysisFileBean> analysisFiles, AnalysisParametersBean param) {
            using (var fs = File.Open(alignmentResultFile.SpectraFilePath, FileMode.Open, FileAccess.ReadWrite)) {
                var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);

                using (StreamWriter sw = new StreamWriter(outputFile, false, Encoding.ASCII)) {
                    //Header
                    ResultExportLcUtility.WriteDataMatrixHeaderAtIonMobility(sw, new ObservableCollection<AnalysisFileBean>(analysisFiles));

                    var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;
                    //From the second
                    for (int i = 0; i < alignedSpots.Count; i++) {

                        if (param.IsRemoveFeatureBasedOnPeakHeightFoldChange && alignedSpots[i].IsBlankFiltered) continue;
                        ResultExportLcUtility.WriteDataMatrixMetaDataAtIonMobility(sw, alignedSpots[i], new AlignedDriftSpotPropertyBean(), alignedSpots, mspDB, txtDB, fs, seekpointList, param);
                        ResultExportLcUtility.WriteData(sw, alignedSpots[i].AlignedPeakPropertyBeanCollection, "Height", false, 0.0F);


                        var driftSpots = alignedSpots[i].AlignedDriftSpots;
                        for (int j = 0; j < driftSpots.Count; j++) {
                            ResultExportLcUtility.WriteDataMatrixMetaDataAtIonMobility(sw, alignedSpots[i], driftSpots[j], alignedSpots, mspDB, txtDB, fs, seekpointList, param);

                            // Replace true zero values with 1/2 of minimum peak height over all samples
                            ResultExportLcUtility.WriteData(sw, driftSpots[j].AlignedPeakPropertyBeanCollection, "Height", false, 0.0F);
                        }
                    }
                }
            }
        }

        public static void ExportIsotopeTrackingResultAsMatFormatFile(string outputFolder,
           ProjectPropertyBean projectProp,
           AnalysisParametersBean param,
           List<MspFormatCompoundInformationBean> mspDB,
           AlignmentFileBean alignmentResultFile,
           AlignmentResultBean alignmentResult) {

            using (var fs = File.Open(alignmentResultFile.SpectraFilePath, FileMode.Open, FileAccess.ReadWrite)) {
                var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);
                ResultExportLcUtility.ExportIsotopeTrackingResultAsMatFormatFile(outputFolder, projectProp, param,
                    mspDB, alignmentResult, fs, seekpointList);
            }
        }
    }
}
