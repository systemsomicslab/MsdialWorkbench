using Msdial.Lcms.Dataprocess.Utility;
using Msdial.Lcms.Dataprocess.Algorithm;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace Msdial.Lcms.DataProcess
{
    public sealed class ProcessAlignmentFinalization
    {
        private ProcessAlignmentFinalization() { }

        private static int versionNum = 6;

        public static void Execute(ObservableCollection<AnalysisFileBean> analysisFiles, string spectraFilePath, 
            AlignmentResultBean alignmentResult, AnalysisParametersBean param, ProjectPropertyBean projectProperty,
            List<MspFormatCompoundInformationBean> mspDB,
            List<PostIdentificatioinReferenceBean> targetFormulaDB, Action<int> reportAction = null, int aifID = 0)
        {
            if (param.IsIonMobility) {
                ExecuteAtIonMobility(analysisFiles, spectraFilePath, alignmentResult, param, projectProperty, mspDB, targetFormulaDB, reportAction);
                return;
            }

            var seekPointer = new List<long>();
            var ms2DecResult = new MS2DecResult();
            var totalPeakNumber = alignmentResult.AlignmentPropertyBeanCollection.Count;
            var iniProgress = 0.0;
            var maxProgress = 0.0;
            if (aifID > 0) {
                var numDec = projectProperty.Ms2LevelIdList.Count;
                iniProgress = (double)((double)(aifID - 1) / (double)numDec * 100);
                maxProgress = (double)(1.0 / (double)numDec * 100);
            }

            using (var fs = File.Open(spectraFilePath, FileMode.Create, FileAccess.ReadWrite))
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

                //from third header
                for (int i = 0; i < alignmentResult.AlignmentPropertyBeanCollection.Count; i++)
                {
                    var spot = alignmentResult.AlignmentPropertyBeanCollection[i];
                    seekPointer.Add(fs.Position);
                    if(aifID == 0)
                        ms2DecResult = getMS2DecResult(analysisFiles, spot);
                    else
                        ms2DecResult = getMS2DecResult(analysisFiles, spot, aifID);
                    SpectralDeconvolution.WriteDeconvolutedSpectraContents(fs, ms2DecResult);

                    if (ms2DecResult.MassSpectra == null || ms2DecResult.MassSpectra.Count == 0)
                        spot.MsmsIncluded = false;

                    if(aifID == 0)
                        reportAction?.Invoke((int)((double)(i + 1) / (double)alignmentResult.AlignmentPropertyBeanCollection.Count * 100));
                    else
                        reportAction?.Invoke((int)(iniProgress + ((double)(i + 1) / (double)alignmentResult.AlignmentPropertyBeanCollection.Count) * maxProgress));
                }

                //Finalize
                fs.Seek(seekPointer[2], SeekOrigin.Begin);
                for (int i = 3; i < seekPointer.Count; i++)
                    fs.Write(BitConverter.GetBytes(seekPointer[i]), 0, 8);
            }

            if (param.TrackingIsotopeLabels)
            {
                if (param.UseTargetFormulaLibrary == true) IsotopeTracking.SetTargetFormulaInformation(alignmentResult, targetFormulaDB, param);
                IsotopeTracking.SetIsotopeTrackingID(alignmentResult, param, projectProperty, mspDB, reportAction);
            }
        }

        private static void ExecuteAtIonMobility(ObservableCollection<AnalysisFileBean> analysisFiles, string spectraFilePath, AlignmentResultBean alignmentResult, 
            AnalysisParametersBean param, ProjectPropertyBean projectProperty, List<MspFormatCompoundInformationBean> mspDB, 
            List<PostIdentificatioinReferenceBean> targetFormulaDB, Action<int> reportAction) {
            var seekPointer = new List<long>();
            var ms2DecResult = new MS2DecResult();
            var totalPeakNumber = alignmentResult.AlignmentIdNumber;

            using (var fs = File.Open(spectraFilePath, FileMode.Create, FileAccess.ReadWrite)) {
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

                //from third header
                var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection;
                for (int i = 0; i < alignedSpots.Count; i++) {
                    var spot = alignedSpots[i];

                    seekPointer.Add(fs.Position);
                    ms2DecResult = getMS2DecResult(analysisFiles, spot, true);

                    SpectralDeconvolution.WriteDeconvolutedSpectraContents(fs, ms2DecResult);

                    var isMsmsIncluded = false;
                    var driftSpots = spot.AlignedDriftSpots;
                    for (int j = 0; j < driftSpots.Count; j++) {

                        var dSpot = driftSpots[j];

                        seekPointer.Add(fs.Position);
                        ms2DecResult = getMS2DecResult(analysisFiles, dSpot);

                        SpectralDeconvolution.WriteDeconvolutedSpectraContents(fs, ms2DecResult);

                        if (ms2DecResult.MassSpectra == null || ms2DecResult.MassSpectra.Count == 0) {
                            dSpot.MsmsIncluded = false;
                        }
                        else {
                            isMsmsIncluded = true;
                        }
                    }
                    spot.MsmsIncluded = isMsmsIncluded;
                    reportAction?.Invoke((int)(((double)(i + 1) / (double)alignedSpots.Count) * 100));
                }

                //Finalize
                fs.Seek(seekPointer[2], SeekOrigin.Begin);
                for (int i = 3; i < seekPointer.Count; i++)
                    fs.Write(BitConverter.GetBytes(seekPointer[i]), 0, 8);
            }
        }

        private static MS2DecResult getMS2DecResult(ObservableCollection<AnalysisFileBean> files, AlignmentPropertyBean alignedSpot, bool isIonMobility = false)
        {
            var fileID = alignedSpot.RepresentativeFileID;
            var peakID = isIonMobility 
                ? alignedSpot.AlignedPeakPropertyBeanCollection[fileID].MasterPeakID 
                : alignedSpot.AlignedPeakPropertyBeanCollection[fileID].PeakID;
            var ms2DecResult = new MS2DecResult();

            using (FileStream fs = File.Open(files[fileID].AnalysisFilePropertyBean.DeconvolutionFilePath, FileMode.Open, FileAccess.ReadWrite))
            {
                var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);
                ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, peakID);
            }

            return ms2DecResult;
        }

        private static MS2DecResult getMS2DecResult(ObservableCollection<AnalysisFileBean> files, AlignedDriftSpotPropertyBean alignedSpot) {
            var fileID = alignedSpot.RepresentativeFileID;
            var peakID = alignedSpot.AlignedPeakPropertyBeanCollection[fileID].MasterPeakID;
            var ms2DecResult = new MS2DecResult();

            using (FileStream fs = File.Open(files[fileID].AnalysisFilePropertyBean.DeconvolutionFilePath, FileMode.Open, FileAccess.ReadWrite)) {
                var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);
                ms2DecResult = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, peakID);
            }

            return ms2DecResult;
        }


        private static MS2DecResult getMS2DecResult(ObservableCollection<AnalysisFileBean> analysisFileBeanCollection, AlignmentPropertyBean alignmentPropertyBean, int aifID) {
            var fileID = alignmentPropertyBean.RepresentativeFileID;
            var peakID = alignmentPropertyBean.AlignedPeakPropertyBeanCollection[fileID].PeakID;
            var deconvolutionResultBean = new MS2DecResult();

            using (FileStream fs = File.Open(analysisFileBeanCollection[fileID].AnalysisFilePropertyBean.DeconvolutionFilePathList[aifID-1], FileMode.Open, FileAccess.ReadWrite)) {
                var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);
                deconvolutionResultBean = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, peakID);
            }

            return deconvolutionResultBean;
        }
    }
}
