using Msdial.Lcms.Dataprocess.Utility;
using Msdial.Lcms.DataProcess;
using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.MsdialConsoleApp.Export;
using Riken.Metabolomics.MsdialConsoleApp.Parser;
using Riken.Metabolomics.MsdialConsoleApp.SaveProject;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Riken.Metabolomics.MsdialConsoleApp.Process
{
    public class LcmsDdaProcess
    {
        public int Run(string inputFolder, string outputFolder, string methodFile, bool isProjectStore, float targetMz)
        {
            Console.WriteLine("Loading library files..");

            var analysisFiles = AnalysisFilesParser.ReadInput(inputFolder);
            if (analysisFiles == null || analysisFiles.Count == 0) {
                Console.WriteLine("There is no input file to be imported.");
                return -1;
            }

            var rdamProperty = AnalysisFilesParser.GetRdamProperty(analysisFiles);
            var lcmsParam = ConfigParser.ReadForLcmsParameter(methodFile);
            var projectProp = ConfigParser.ReadForLcmsProjectProperty(methodFile, inputFolder);
            var alignmentFile = AlignmentResultParser.GetAlignmentFileBean(inputFolder);

            //check dda or dia
            projectProp.Ionization = Ionization.ESI;
            projectProp.MethodType = MethodType.ddMSMS;

            //get file propeties in project prop
            projectProp = getFilePropertyDictionaryFromAnalysisFiles(projectProp, analysisFiles);

            //check adduct list
            lcmsParam.AdductIonInformationBeanList = AdductResourceParser.GetAdductIonInformationList(projectProp.IonMode);
            ConfigParser.ReadAdductIonInfo(lcmsParam.AdductIonInformationBeanList, methodFile);

            var iupacDB = IupacResourceParser.GetIupacReferenceBean();
            var mspDB = new List<MspFormatCompoundInformationBean>();
            if (projectProp.LibraryFilePath != null && projectProp.LibraryFilePath != string.Empty)
            {
				if (!System.IO.File.Exists(projectProp.LibraryFilePath)) {
					return fileNoExistError(projectProp.LibraryFilePath);
				} else {
					mspDB = DatabaseLcUtility.GetMspDbQueries(projectProp.LibraryFilePath, iupacDB);
				}
            }

            var txtDB = new List<PostIdentificatioinReferenceBean>();
            if (projectProp.PostIdentificationLibraryFilePath != null && projectProp.PostIdentificationLibraryFilePath != string.Empty)
            {
				if (!System.IO.File.Exists(projectProp.PostIdentificationLibraryFilePath)) {
					return fileNoExistError(projectProp.PostIdentificationLibraryFilePath);
				} else {
					txtDB = DatabaseLcUtility.GetTxtDbQueries(projectProp.PostIdentificationLibraryFilePath);
				}
            }

            var error = string.Empty;
            if (projectProp.CompoundListInTargetModePath != null && projectProp.CompoundListInTargetModePath != string.Empty) {
                if (!System.IO.File.Exists(projectProp.CompoundListInTargetModePath))
                    return fileNoExistError(projectProp.CompoundListInTargetModePath);
                lcmsParam.CompoundListInTargetMode = TextLibraryParcer.CompoundListInTargetModeReader(projectProp.CompoundListInTargetModePath, out error);
                if (error != string.Empty) {
                    Console.WriteLine(error);
                }
            }

            if (targetMz > 0) {
                if (lcmsParam.CompoundListInTargetMode == null || lcmsParam.CompoundListInTargetMode.Count == 0) {
                    lcmsParam.CompoundListInTargetMode = new List<TextFormatCompoundInformationBean>();
                }
                lcmsParam.CompoundListInTargetMode.Add(new TextFormatCompoundInformationBean() { MetaboliteName = "Target", AccurateMass = targetMz, AccurateMassTolerance = lcmsParam.MassSliceWidth });
            }

            // iSTDs are added to targeted compound list
            if (targetMz > 0 || (lcmsParam.CompoundListInTargetMode != null && lcmsParam.CompoundListInTargetMode.Count > 0)) {
                if (lcmsParam.RetentionTimeCorrectionCommon.StandardLibrary != null && lcmsParam.RetentionTimeCorrectionCommon.StandardLibrary.Count > 0 && lcmsParam.RetentionTimeCorrectionCommon.StandardLibrary.Count(x => x.IsTarget == true) > 0) {
                    foreach (var iSTD in lcmsParam.RetentionTimeCorrectionCommon.StandardLibrary) {
                        if (iSTD.IsTarget) {
                            lcmsParam.CompoundListInTargetMode.Add(new TextFormatCompoundInformationBean() { MetaboliteName = iSTD.MetaboliteName, AccurateMass = iSTD.AccurateMass, AccurateMassTolerance = iSTD.AccurateMassTolerance });
                        }
                    }
                }
            }

            Console.WriteLine("Start processing..");
            if (lcmsParam.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExcuteRtCorrection == true && lcmsParam.RetentionTimeCorrectionCommon.StandardLibrary != null &&
                lcmsParam.RetentionTimeCorrectionCommon.StandardLibrary.Count > 0 && lcmsParam.RetentionTimeCorrectionCommon.StandardLibrary.Count(x => x.IsTarget == true) > 0) {
                Console.WriteLine("Excute RT correction");
                foreach (var analysisFile in analysisFiles) {
                    Console.WriteLine(analysisFile.AnalysisFilePropertyBean.AnalysisFileName);
                    analysisFile.RetentionTimeCorrectionBean = new Rfx.Riken.OsakaUniv.RetentionTimeCorrection.RetentionTimeCorrectionBean();
                    Msdial.Lcms.Dataprocess.Algorithm.RetentionTimeCorrection.Execute(projectProp, rdamProperty, analysisFile, lcmsParam, lcmsParam.RetentionTimeCorrectionCommon.StandardLibrary, lcmsParam.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam);
                }
            }

            return Execute(projectProp, rdamProperty, analysisFiles, lcmsParam, mspDB,
                txtDB, iupacDB, alignmentFile, outputFolder, isProjectStore);
        }

        private ProjectPropertyBean getFilePropertyDictionaryFromAnalysisFiles(ProjectPropertyBean projectProp, List<AnalysisFileBean> analysisFiles)
        {
            for (int i = 0; i < analysisFiles.Count; i++)
            {
                projectProp.FileID_RdamID[i] = i;
                projectProp.FileID_ClassName[i] = analysisFiles[i].AnalysisFilePropertyBean.AnalysisFileClass;
                projectProp.FileID_AnalysisFileType[i] = analysisFiles[i].AnalysisFilePropertyBean.AnalysisFileType;
            }

            return projectProp;
        }

        private int Execute(ProjectPropertyBean projectProp, RdamPropertyBean rdamProperty, List<AnalysisFileBean> analysisFiles, 
            AnalysisParametersBean lcmsParam, List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> txtDB, 
            IupacReferenceBean iupacDB, AlignmentFileBean alignmentFile, string outputfolder, bool isProjectStore)
        {
            foreach (var file in analysisFiles) {
                var error = string.Empty;
                ProcessFile.Execute(projectProp, rdamProperty, file, lcmsParam, iupacDB, mspDB, txtDB, out error, null);
                if (error != string.Empty) {
                    Console.WriteLine(error);
                }

                #region//export
                var filepath = file.AnalysisFilePropertyBean.AnalysisFilePath;
                var rdamID = rdamProperty.RdamFilePath_RdamFileID[filepath];
                var fileID = file.AnalysisFilePropertyBean.AnalysisFileId;
                var measurementID = rdamProperty.RdamFileContentBeanCollection[rdamID].FileID_MeasurementID[fileID];

                Console.WriteLine("Exporting deconvolution results...");
                using (var rawDataAccess = new RawDataAccess(filepath, measurementID, true, file.RetentionTimeCorrectionBean.PredictedRt)) {
                    var raw_measurment = DataAccessLcUtility.GetRawDataMeasurement(rawDataAccess);
                    var spectrumCollection = DataAccessLcUtility.GetAllSpectrumCollection(raw_measurment);
                    var accumulatedMs1SpecCollection = DataAccessLcUtility.GetAccumulatedMs1SpectrumCollection(raw_measurment);
                    ResultExportForLC.ExportMs2DecResult(file, outputfolder, accumulatedMs1SpecCollection, spectrumCollection, mspDB, txtDB, lcmsParam, projectProp);
                }
                #endregion
                Console.WriteLine(file.AnalysisFilePropertyBean.AnalysisFilePath + " finished");
            }

            AlignmentResultBean alignmentResult = null;
            if (analysisFiles.Count > 1)
            {
                alignmentResult = new AlignmentResultBean();
                ProcessJointAligner.Execute(rdamProperty, projectProp, new ObservableCollection<AnalysisFileBean>(analysisFiles), 
                    lcmsParam, alignmentResult, null);
                Console.WriteLine("Joint aligner finished");

                ProcessGapFilling.Execute(projectProp, rdamProperty, new ObservableCollection<AnalysisFileBean>(analysisFiles), alignmentFile, lcmsParam, 
                    alignmentResult, null);
                Console.WriteLine("Gap filling finished");

                ProcessAlignmentFinalization.Execute(new ObservableCollection<AnalysisFileBean>(analysisFiles), alignmentFile.SpectraFilePath, 
                    alignmentResult, lcmsParam, projectProp, mspDB, null, null);
                Console.WriteLine("Finalization finished");

                //export
                var outputFile = outputfolder + "\\" + alignmentFile.FileName + ".msdial";
                ResultExportForLC.ExportAlignmentResult(outputFile, alignmentFile, alignmentResult, mspDB, txtDB, analysisFiles, lcmsParam);
            }

            if (isProjectStore) {
                Console.WriteLine("Now saving the project to be used in MSDIAL GUI...");
                ProjectSave.SaveForLcmsProject(projectProp, rdamProperty, mspDB,
                  iupacDB, lcmsParam, analysisFiles, alignmentFile, alignmentResult,
                  txtDB, new List<PostIdentificatioinReferenceBean>());
             
                ////just for Hiroshi use
                #region
                //var matFolder = outputfolder + "\\Mat files";
                //System.IO.Directory.CreateDirectory(matFolder);
                //ResultExportForLC.ExportIsotopeTrackingResultAsMatFormatFile(matFolder, projectProp, lcmsParam,
                //    mspDB, alignmentFile, alignmentResultBean);
                #endregion
            }

            return 0;
        }

        #region // error code
        private int fileNoExistError(string file)
        {
            var error = String.Format("\r\nThe file {0} does not exist.", file);
            Console.WriteLine(error);

            return -1;
        }
        #endregion
    }
}
