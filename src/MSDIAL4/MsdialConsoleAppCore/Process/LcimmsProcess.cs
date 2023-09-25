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
using System.IO;
using System.Linq;
using System.Text;

namespace Riken.Metabolomics.MsdialConsoleApp.Process {
    public class LcimmsProcess {
        public int Run(string inputFolder, string outputFolder, string methodFile, bool isProjectStore, bool isDIA = false) {
            Console.WriteLine("Loading library files..");

            var analysisFiles = AnalysisFilesParser.ReadInput(inputFolder);
            if (analysisFiles == null || analysisFiles.Count == 0) {
                Console.WriteLine("There is no input file to be imported.");
                return -1;
            }

            var rdamProperty = AnalysisFilesParser.GetRdamProperty(analysisFiles);
            var projectProp = ConfigParser.ReadForLcmsProjectProperty(methodFile, inputFolder);
            projectProp.SeparationType = SeparationType.IonMobility;
            projectProp.MethodType = isDIA ? MethodType.diMSMS : MethodType.ddMSMS;
            projectProp.Ionization = Ionization.ESI;

            var lcmsParam = ConfigParser.ReadForLcmsParameter(methodFile);
            lcmsParam.IsIonMobility = true;
            ConfigParser.SetLCMSAlignmentReferenceFileByFilename(methodFile, analysisFiles, lcmsParam);
            ConfigParser.SetCalibrateInformation(lcmsParam, analysisFiles);

            var alignmentFile = AlignmentResultParser.GetAlignmentFileBean(inputFolder);

            //get file propeties in project prop
            projectProp = getFilePropertyDictionaryFromAnalysisFiles(projectProp, analysisFiles);

            //check adduct list
            lcmsParam.AdductIonInformationBeanList = AdductResourceParser.GetAdductIonInformationList(projectProp.IonMode);
            ConfigParser.ReadAdductIonInfo(lcmsParam.AdductIonInformationBeanList, methodFile);

            var iupacDB = IupacResourceParser.GetIupacReferenceBean();
            var mspDB = new List<MspFormatCompoundInformationBean>();
            if (projectProp.LibraryFilePath != null && projectProp.LibraryFilePath != string.Empty) {
                if (!System.IO.File.Exists(projectProp.LibraryFilePath)) {
                    return fileNoExistError(projectProp.LibraryFilePath);
                }
                else {
                    mspDB = DatabaseLcUtility.GetMspDbQueries(projectProp.LibraryFilePath, iupacDB);
                    if (mspDB != null && mspDB.Count >= 0) {
                        mspDB = mspDB.OrderBy(n => n.PrecursorMz).ToList();
                        var counter = 0;
                        foreach (var query in mspDB) {
                            query.Id = counter; counter++;
                        }
                    }
                }
            }

            var txtDB = new List<PostIdentificatioinReferenceBean>();
            if (projectProp.PostIdentificationLibraryFilePath != null && projectProp.PostIdentificationLibraryFilePath != string.Empty) {
                if (!System.IO.File.Exists(projectProp.PostIdentificationLibraryFilePath)) {
                    return fileNoExistError(projectProp.PostIdentificationLibraryFilePath);
                }
                else {
                    txtDB = DatabaseLcUtility.GetTxtDbQueries(projectProp.PostIdentificationLibraryFilePath);
                }
            }

            Console.WriteLine("Start processing..");
            return Execute(projectProp, rdamProperty, analysisFiles, lcmsParam, mspDB,
                txtDB, iupacDB, alignmentFile, outputFolder, isProjectStore);
        }

        private ProjectPropertyBean getFilePropertyDictionaryFromAnalysisFiles(ProjectPropertyBean projectProp, List<AnalysisFileBean> analysisFiles) {
            for (int i = 0; i < analysisFiles.Count; i++) {
                projectProp.FileID_RdamID[i] = i;
                projectProp.FileID_ClassName[i] = analysisFiles[i].AnalysisFilePropertyBean.AnalysisFileClass;
                projectProp.FileID_AnalysisFileType[i] = analysisFiles[i].AnalysisFilePropertyBean.AnalysisFileType;
            }

            return projectProp;
        }

        private int Execute(ProjectPropertyBean projectProp, RdamPropertyBean rdamProperty, List<AnalysisFileBean> analysisFiles,
            AnalysisParametersBean lcmsParam, List<MspFormatCompoundInformationBean> mspDB, List<PostIdentificatioinReferenceBean> txtDB,
            IupacReferenceBean iupacDB, AlignmentFileBean alignmentFile, string outputfolder, bool isProjectStore) {
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
                using (var rawDataAccess = new RawDataAccess(filepath, measurementID, false, false, true, file.RetentionTimeCorrectionBean.PredictedRt)) {
                    var raw_measurment = DataAccessLcUtility.GetRawDataMeasurement(rawDataAccess);
                    var spectrumCollection = DataAccessLcUtility.GetAllSpectrumCollection(raw_measurment);
                    var accumulatedMs1SpecCollection = DataAccessLcUtility.GetAccumulatedMs1SpectrumCollection(raw_measurment);
                    ResultExportForLC.ExportMs2DecResult(file, outputfolder, accumulatedMs1SpecCollection, spectrumCollection, mspDB, txtDB, lcmsParam, projectProp);
                }
                #endregion
                Console.WriteLine(file.AnalysisFilePropertyBean.AnalysisFilePath + " finished");
            }

            AlignmentResultBean alignmentResult = null;
            if (analysisFiles.Count > 1 && lcmsParam.TogetherWithAlignment) {
                alignmentResult = new AlignmentResultBean();
                ProcessJointAligner.Execute(rdamProperty, projectProp, new ObservableCollection<AnalysisFileBean>(analysisFiles),
                    lcmsParam, alignmentResult, null);
                Console.WriteLine("Joint aligner finished");

                ProcessGapFilling.Execute(projectProp, rdamProperty, new ObservableCollection<AnalysisFileBean>(analysisFiles), alignmentFile, lcmsParam,
                    iupacDB, alignmentResult, null);
                Console.WriteLine("Gap filling finished");

                ProcessAlignmentFinalization.Execute(new ObservableCollection<AnalysisFileBean>(analysisFiles), alignmentFile.SpectraFilePath,
                    alignmentResult, lcmsParam, projectProp, mspDB, null, null);
                Console.WriteLine("Finalization finished");

                //export
                var outputFile = System.IO.Path.Combine(outputfolder, alignmentFile.FileName + ".msdial");
                ResultExportForLC.ExportAlignmentResultForIonMobilityData(outputFile, alignmentFile, alignmentResult, mspDB, txtDB, analysisFiles, lcmsParam);
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
        private int fileNoExistError(string file) {
            var error = String.Format("\r\nThe file {0} does not exist.", file);
            Console.WriteLine(error);

            return -1;
        }
        #endregion
    }
}
