using Msdial.Lcms.Dataprocess.Utility;
using Msdial.Lcms.DataProcess;
using Msdial.Common.Export;
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

namespace Riken.Metabolomics.MsdialConsoleApp.Process
{
    public class LcmsDiaProcess
    {
        public int Run(string inputFolder, string outputFolder, string methodFile, bool isProjectSaved, float targetMz, bool isAif)
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
            ConfigParser.SetLCMSAlignmentReferenceFileByFilename(methodFile, analysisFiles, lcmsParam);

            var alignmentFile = AlignmentResultParser.GetAlignmentFileBean(inputFolder);

            //check dda or dia
            projectProp.MethodType = MethodType.diMSMS;
            projectProp.Ionization = Ionization.ESI;
            projectProp.CheckAIF = isAif;

            //get file propeties in project prop
            projectProp = getFilePropertyDictionaryFromAnalysisFiles(projectProp, analysisFiles);

            //check adduct list
            lcmsParam.AdductIonInformationBeanList = AdductResourceParser.GetAdductIonInformationList(projectProp.IonMode);
            ConfigParser.ReadAdductIonInfo(lcmsParam.AdductIonInformationBeanList, methodFile);

            //check dia file list
            if (projectProp.ExperimentFilePath != null && projectProp.ExperimentFilePath != string.Empty) {
                if (isAif) {
                    if (!System.IO.File.Exists(projectProp.ExperimentFilePath))
                        return fileNoExistError(projectProp.ExperimentFilePath);
                    projectProp.ExperimentID_AnalystExperimentInformationBean = getDiaExperimentDictionaryAif(projectProp.ExperimentFilePath);
                    if (projectProp.ExperimentID_AnalystExperimentInformationBean == null)
                        return diaExperimentFileExceptionAif();
                    foreach (var value in projectProp.ExperimentID_AnalystExperimentInformationBean) { if (value.Value.CheckDecTarget == 1) { projectProp.Ms2LevelIdList.Add(value.Key); } }
                    projectProp.CollisionEnergyList = projectProp.ExperimentID_AnalystExperimentInformationBean.Select(x => x.Value.CollisionEnergy).ToList();

                    foreach (var file in analysisFiles) {
                        var decPathList = new List<string>();
                        var path = file.AnalysisFilePropertyBean.DeconvolutionFilePath;
                        for (var j = 0; j < projectProp.Ms2LevelIdList.Count; j++) {
                            decPathList.Add(Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "." + j + ".dcl"));

                        }
                        file.AnalysisFilePropertyBean.DeconvolutionFilePathList = decPathList;
                        file.AnalysisFilePropertyBean.DeconvolutionFilePath = decPathList[0];
                    }
                }
                else {
                    if (!System.IO.File.Exists(projectProp.ExperimentFilePath))
                        return fileNoExistError(projectProp.ExperimentFilePath);
                    projectProp.ExperimentID_AnalystExperimentInformationBean = getDiaExperimentDictionary(projectProp.ExperimentFilePath);
                    if (projectProp.ExperimentID_AnalystExperimentInformationBean == null)
                        return diaExperimentFileException();
                }
            } else
                return noDiaFileError();
            
            var iupacDB = IupacResourceParser.GetIupacReferenceBean();
            var mspDB = new List<MspFormatCompoundInformationBean>();
            if (projectProp.LibraryFilePath != null && projectProp.LibraryFilePath != string.Empty)
            {
                if (!System.IO.File.Exists(projectProp.LibraryFilePath))
                    return fileNoExistError(projectProp.LibraryFilePath);
                mspDB = DatabaseLcUtility.GetMspDbQueries(projectProp.LibraryFilePath, iupacDB);
                if (mspDB != null && mspDB.Count >= 0) {
                    mspDB = mspDB.OrderBy(n => n.PrecursorMz).ToList();
                    var counter = 0;
                    foreach (var query in mspDB) {
                        query.Id = counter; counter++;
                    }
                }
            }

            var txtDB = new List<PostIdentificatioinReferenceBean>();
            if (projectProp.PostIdentificationLibraryFilePath != null && projectProp.PostIdentificationLibraryFilePath != string.Empty)
            {
                if (!System.IO.File.Exists(projectProp.PostIdentificationLibraryFilePath))
                    return fileNoExistError(projectProp.PostIdentificationLibraryFilePath);
                txtDB = DatabaseLcUtility.GetTxtDbQueries(projectProp.PostIdentificationLibraryFilePath);
            }

            var error = string.Empty;
            if(projectProp.CompoundListInTargetModePath != null && projectProp.CompoundListInTargetModePath != string.Empty) {
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
            if(targetMz > 0 || (lcmsParam.CompoundListInTargetMode != null && lcmsParam.CompoundListInTargetMode.Count > 0)) {
                // add aducts in compound list
                var adductIons = ConverterUtility.GetAddutIonsFromName(lcmsParam, projectProp);
                var newCompoundList = new List<TextFormatCompoundInformationBean>();
                foreach (var target in lcmsParam.CompoundListInTargetMode) {
                    var exactMass = ConverterUtility.GetExactMass(projectProp, target.AccurateMass);
                    var adductMzList = ConverterUtility.ConvertAccurateMass2OtherAducts(adductIons, exactMass);
                    foreach(var adductMz in adductMzList) {
                        if(Math.Abs(target.AccurateMass - adductMz) > lcmsParam.CentroidMs1Tolerance) {
                            newCompoundList.Add(new TextFormatCompoundInformationBean() { AccurateMass = adductMz, MetaboliteName = "Target + adduct", AccurateMassTolerance = target.AccurateMassTolerance });
                        }
                        else { newCompoundList.Add(target); }
                    }
                }
                lcmsParam.CompoundListInTargetMode = newCompoundList;

                if (lcmsParam.RetentionTimeCorrectionCommon.StandardLibrary != null && lcmsParam.RetentionTimeCorrectionCommon.StandardLibrary.Count > 0 && lcmsParam.RetentionTimeCorrectionCommon.StandardLibrary.Count(x => x.IsTarget==true) > 0) {
                    foreach(var iSTD in lcmsParam.RetentionTimeCorrectionCommon.StandardLibrary) {
                        if (iSTD.IsTarget) {
                            lcmsParam.CompoundListInTargetMode.Add(new TextFormatCompoundInformationBean() { MetaboliteName = iSTD.MetaboliteName, AccurateMass = iSTD.AccurateMass, AccurateMassTolerance = iSTD.AccurateMassTolerance });
                        }
                    }
                }
            }

            Console.WriteLine("Start processing..");
            return Execute(projectProp, rdamProperty, analysisFiles, lcmsParam, mspDB, txtDB, 
                iupacDB, alignmentFile, outputFolder, isProjectSaved);
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
            #region Retention time correction

            if (lcmsParam.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExcuteRtCorrection == true && lcmsParam.RetentionTimeCorrectionCommon.StandardLibrary != null &&
                 lcmsParam.RetentionTimeCorrectionCommon.StandardLibrary.Count > 0 && lcmsParam.RetentionTimeCorrectionCommon.StandardLibrary.Count(x => x.IsTarget == true) > 0) {
                Console.WriteLine("Excute RT correction");
                foreach (var analysisFile in analysisFiles) {
                    analysisFile.RetentionTimeCorrectionBean = new Rfx.Riken.OsakaUniv.RetentionTimeCorrection.RetentionTimeCorrectionBean();
                    Msdial.Lcms.Dataprocess.Algorithm.RetentionTimeCorrection.Execute(projectProp, rdamProperty, analysisFile, lcmsParam, lcmsParam.RetentionTimeCorrectionCommon.StandardLibrary, lcmsParam.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam);
                }
                if (lcmsParam.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.RtDiffCalcMethod == Rfx.Riken.OsakaUniv.RetentionTimeCorrection.RtDiffCalcMethod.SampleMinusSampleAverage) {
                    var detectedStdCommonList = Msdial.Common.Utility.RtCorrection.MakeCommonStdList(new ObservableCollection<AnalysisFileBean>(analysisFiles), lcmsParam.RetentionTimeCorrectionCommon.StandardLibrary);
                    foreach (var f in analysisFiles) {
                        f.RetentionTimeCorrectionBean = Msdial.Lcms.Dataprocess.Algorithm.RetentionTimeCorrection.GetRetentionTimeCorrectionBean_SampleMinusAverage(lcmsParam.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam,
                            f.RetentionTimeCorrectionBean.StandardList, f.RetentionTimeCorrectionBean.OriginalRt.ToArray(), detectedStdCommonList);
                    }
                }
                else {
                    foreach (var f in analysisFiles)
                        f.RetentionTimeCorrectionBean = Msdial.Lcms.Dataprocess.Algorithm.RetentionTimeCorrection.GetRetentionTimeCorrectionBean_SampleMinusReference(lcmsParam.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam,
                            f.RetentionTimeCorrectionBean.StandardList, f.RetentionTimeCorrectionBean.OriginalRt.ToArray());
                }
                var StdCommonList = Msdial.Common.Utility.RtCorrection.MakeCommonStdList(new ObservableCollection<AnalysisFileBean>(analysisFiles), lcmsParam.RetentionTimeCorrectionCommon.StandardLibrary);
                var filePath = Path.Combine(projectProp.ProjectFolderPath, Path.GetFileNameWithoutExtension(projectProp.ProjectFilePath));
                //Msdial.Common.Export.DataExportAsPdf.ExportRetentionTimeCorrectionAll(filePath, analysisFiles, lcmsParam, lcmsParam.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam, StdCommonList);
                using (var sw = new StreamWriter(filePath + "RetentionTime.txt")) {
                    PrivateMethodTargetCompoundExport.ExportRetentionTimeCorrectionResults(sw, analysisFiles);
            } }
            #endregion 

            foreach (var file in analysisFiles)
            {
                if (mspDB != null || mspDB.Count >= 0)
                    mspDB = mspDB.OrderBy(n => n.PrecursorMz).ToList();

                var error = string.Empty;
                ProcessFile.Execute(projectProp, rdamProperty, file, lcmsParam, iupacDB, mspDB, txtDB, out error, null);
                if (error != string.Empty) {
                    Console.WriteLine(error);
                }

                if (mspDB != null || mspDB.Count >= 0)
                    mspDB = mspDB.OrderBy(n => n.Id).ToList();

                #region//export
                var filepath = file.AnalysisFilePropertyBean.AnalysisFilePath;
                var rdamID = rdamProperty.RdamFilePath_RdamFileID[filepath];
                var fileID = file.AnalysisFilePropertyBean.AnalysisFileId;
                var measurementID = rdamProperty.RdamFileContentBeanCollection[rdamID].FileID_MeasurementID[fileID];

                using (var rawDataAccess = new RawDataAccess(filepath, measurementID, false, false, true, file.RetentionTimeCorrectionBean.PredictedRt)) {
                    var raw_measurment = DataAccessLcUtility.GetRawDataMeasurement(rawDataAccess);
                    var spectrumCollection = DataAccessLcUtility.GetAllSpectrumCollection(raw_measurment);
                    var accumulatedMs1SpecCollection = DataAccessLcUtility.GetAccumulatedMs1SpectrumCollection(raw_measurment);
                   // var spectrumCollection = DataAccessLcUtility.GetRdamSpectrumCollection(rawDataAccess);
                    ResultExportForLC.ExportMs2DecResult(file, outputfolder, accumulatedMs1SpecCollection, spectrumCollection, mspDB, txtDB, lcmsParam, projectProp);
                }
                #endregion
                Console.WriteLine(file.AnalysisFilePropertyBean.AnalysisFilePath + " finished");
            }

            AlignmentResultBean alignmentResult = null;
            if (analysisFiles.Count > 1 && lcmsParam.TogetherWithAlignment)
            {
                alignmentResult = new AlignmentResultBean();
                ProcessJointAligner.Execute(rdamProperty, projectProp, new ObservableCollection<AnalysisFileBean>(analysisFiles), lcmsParam, alignmentResult, null);
                Console.WriteLine("Joint aligner finished");

                ProcessGapFilling.Execute(projectProp, rdamProperty, new ObservableCollection<AnalysisFileBean>(analysisFiles), alignmentFile, lcmsParam, iupacDB, alignmentResult, null);
                Console.WriteLine("Gap filling finished");

                if (projectProp.CheckAIF) {
                    alignmentFile.SpectraFilePath = Path.Combine(System.IO.Path.GetDirectoryName(alignmentFile.FilePath), alignmentFile.FileName + ".0.dcl");
                    for (var i = 0; i < projectProp.Ms2LevelIdList.Count; i++) {
                        var specFilePath = Path.Combine(System.IO.Path.GetDirectoryName(alignmentFile.FilePath), alignmentFile.FileName + "." + i + ".dcl");
                        ProcessAlignmentFinalization.Execute(new ObservableCollection<AnalysisFileBean>(analysisFiles), specFilePath,
                            alignmentResult, lcmsParam, projectProp, mspDB, null, null, i + 1);
                    }
                }
                else
                    ProcessAlignmentFinalization.Execute(new ObservableCollection<AnalysisFileBean>(analysisFiles), alignmentFile.SpectraFilePath, alignmentResult, lcmsParam, projectProp, mspDB, null, null);
                Console.WriteLine("Finalization finished");

                //export
                var outputFile = Path.Combine(outputfolder, alignmentFile.FileName + ".msdial");
                ResultExportForLC.ExportAlignmentResult(outputFile, alignmentFile, alignmentResult, mspDB, txtDB, analysisFiles, lcmsParam);
            }

            if(projectProp.CheckAIF && analysisFiles.Count > 6 && lcmsParam.AnalysisParamOfMsdialCorrDec != null && lcmsParam.AnalysisParamOfMsdialCorrDec.CanExcute && alignmentResult.AlignmentPropertyBeanCollection.Count > 0) {
                Console.WriteLine("Excute CorrDec");
                Msdial.Lcms.Dataprocess.Algorithm.CorrDecBase.CreateMs2SpectraGroup(projectProp, alignmentFile, new ObservableCollection<AnalysisFileBean>(analysisFiles), alignmentResult, lcmsParam, null);
                 System.Threading.Tasks.Parallel.For(0, projectProp.Ms2LevelIdList.Count, numDec => {
                    var decFilePath = Path.Combine(projectProp.ProjectFolderPath, alignmentFile.FileName + "_MsGrouping_Raw_" + numDec + ".mfg");
                    var filePath = Path.Combine(projectProp.ProjectFolderPath, alignmentFile.FileName + "_CorrelationBasedDecRes_Raw_" + numDec + ".cbd");
                    Msdial.Lcms.Dataprocess.Algorithm.CorrDecHandler.WriteCorrelationDecRes(lcmsParam.AnalysisParamOfMsdialCorrDec, projectProp, alignmentResult.AlignmentPropertyBeanCollection, analysisFiles.Count, filePath, decFilePath, null);
                });
            }

            if (isProjectStore) {
                Console.WriteLine("Now saving the project to be used in MSDIAL GUI...");
                ProjectSave.SaveForLcmsProject(projectProp, rdamProperty, mspDB,
                 iupacDB, lcmsParam, analysisFiles, alignmentFile, alignmentResult,
                 txtDB, new List<PostIdentificatioinReferenceBean>());

                if (lcmsParam.CompoundListInTargetMode != null && lcmsParam.CompoundListInTargetMode.Count > 0 && projectProp.IsLabPrivateVersionTada) { 
                    if (lcmsParam.RetentionTimeCorrectionCommon != null && lcmsParam.RetentionTimeCorrectionCommon.StandardLibrary != null && lcmsParam.RetentionTimeCorrectionCommon.StandardLibrary.Count > 0 && lcmsParam.RetentionTimeCorrectionCommon.StandardLibrary.Count(x => x.IsTarget == true) > 0) {
                        PrivateMethodTargetCompoundExport.ExportTargetResult(projectProp, rdamProperty, analysisFiles, alignmentFile, alignmentResult, mspDB, lcmsParam.RetentionTimeCorrectionCommon.StandardLibrary.Where(x => x.IsTarget == true).ToList(), lcmsParam, lcmsParam.CompoundListInTargetMode[0].AccurateMass, lcmsParam.AnalysisParamOfMsdialCorrDec.CanExcute);
                    }
                    else {
                        PrivateMethodTargetCompoundExport.ExportTargetResult(projectProp, rdamProperty, analysisFiles, alignmentFile, alignmentResult, mspDB, null, lcmsParam, lcmsParam.CompoundListInTargetMode[0].AccurateMass, lcmsParam.AnalysisParamOfMsdialCorrDec.CanExcute);

                    }
                }
            }


            Console.WriteLine("Finished");
            return 0;
        }

        private Dictionary<int, AnalystExperimentInformationBean> getDiaExperimentDictionary(string filepath)
        {
            var diaExperimentDict = new Dictionary<int, AnalystExperimentInformationBean>();
            string[] lines;
            int counter = 0;

            bool checker = true;
            int experimentID;
            float startMz, endMz;

            using (var sr = new StreamReader(filepath))
            {
                sr.ReadLine();

                while (sr.Peek() > -1)
                {
                    lines = sr.ReadLine().Split('\t');
                    if (lines.Length == 0) break;

                    if (lines.Length > 0 && lines.Length < 3) { checker = false; break; }

                    if (lines[1] == "SCAN")
                    {
                        if (int.TryParse(lines[0], out experimentID) && float.TryParse(lines[2], out startMz) && float.TryParse(lines[3], out endMz))
                            diaExperimentDict[counter] = new AnalystExperimentInformationBean() { ExperimentNumber = experimentID, MsType = MsType.SCAN, StartMz = startMz, EndMz = endMz };
                        else
                        {
                            checker = false;
                            break;
                        }
                    }
                    else if (lines[1] != "SCAN")
                    {
                        if (int.TryParse(lines[0], out experimentID) && float.TryParse(lines[2], out startMz) && float.TryParse(lines[3], out endMz))
                            diaExperimentDict[counter] = new AnalystExperimentInformationBean() { ExperimentNumber = experimentID, MsType = MsType.SWATH, StartMz = startMz, EndMz = endMz };
                        else
                        {
                            checker = false;
                            break;
                        }
                    }
                    else
                    {
                        checker = false;
                        break;
                    }
                    counter++;
                }
                if (checker == true)
                    return diaExperimentDict;
                else return null;
            }
        }

        // for AIF
        private Dictionary<int, AnalystExperimentInformationBean> getDiaExperimentDictionaryAif(string filepath) {
            var diaExperimentDict = new Dictionary<int, AnalystExperimentInformationBean>();
            string[] lines;
            int counter = 0;

            bool checkerAif = true;
            int experimentID;
            float startMz, endMz, ce;
            int check;

            using (var sr = new StreamReader(filepath)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    lines = sr.ReadLine().Split('\t');
                    if (lines.Length == 0) break;

                    if (lines.Length > 0 && lines.Length < 5) { checkerAif = false; break; }

                    if (lines.Length == 6) {    // w/o CE column            
                        if (lines[1] == "SCAN") {
                            if (int.TryParse(lines[0], out experimentID) && float.TryParse(lines[2], out startMz) && float.TryParse(lines[3], out endMz) && int.TryParse(lines[5], out check) && lines[4].Length > 0)
                                diaExperimentDict[counter] = new AnalystExperimentInformationBean() { ExperimentNumber = experimentID, MsType = MsType.SCAN, StartMz = startMz, EndMz = endMz, Name = lines[4].ToString(), CheckDecTarget = check };
                            else {
                                checkerAif = false;
                                break;
                            }
                        }
                        else if (lines[1] != "SCAN") {
                            if (int.TryParse(lines[0], out experimentID) && float.TryParse(lines[2], out startMz) && float.TryParse(lines[3], out endMz) && int.TryParse(lines[5], out check) && lines[4].Length > 0)
                                diaExperimentDict[counter] = new AnalystExperimentInformationBean() { ExperimentNumber = experimentID, MsType = MsType.AIF, StartMz = startMz, EndMz = endMz, Name = lines[4].ToString(), CheckDecTarget = check };
                            else {
                                checkerAif = false;
                                break;
                            }
                        }
                        else {
                            checkerAif = false;
                            break;
                        }
                        counter++;

                    }
                    else if (lines.Length == 7) { // w/ CE column
                        if (lines[1] == "SCAN") {
                            if (int.TryParse(lines[0], out experimentID) && float.TryParse(lines[2], out startMz) && float.TryParse(lines[3], out endMz) && float.TryParse(lines[5], out ce) && int.TryParse(lines[6], out check) && lines[4].Length > 0)
                                diaExperimentDict[counter] = new AnalystExperimentInformationBean() { ExperimentNumber = experimentID, MsType = MsType.SCAN, StartMz = startMz, EndMz = endMz, Name = lines[4].ToString(), CheckDecTarget = check, CollisionEnergy = ce };
                            else {
                                checkerAif = false;
                                break;
                            }
                        }
                        else if (lines[1] != "SCAN") {
                            if (int.TryParse(lines[0], out experimentID) && float.TryParse(lines[2], out startMz) && float.TryParse(lines[3], out endMz) && float.TryParse(lines[5], out ce) && int.TryParse(lines[6], out check) && lines[4].Length > 0)
                                diaExperimentDict[counter] = new AnalystExperimentInformationBean() { ExperimentNumber = experimentID, MsType = MsType.AIF, StartMz = startMz, EndMz = endMz, Name = lines[4].ToString(), CheckDecTarget = check, CollisionEnergy = ce };
                            else {
                                checkerAif = false;
                                break;
                            }
                        }
                        else {
                            checkerAif = false;
                            break;
                        }
                        counter++;
                    }
                }
                if (diaExperimentDict.Values.Count(x => x.CheckDecTarget == 1) == 0) checkerAif = false;
                if (checkerAif)
                    return diaExperimentDict;
                else return null;
            }
        }

        #region // error code
        private int fileNoExistError(string file)
        {
            var error = file + "\r\nThis file is not existed.";
            Console.WriteLine(error);

            return -1;
        }

        private int noDiaFileError()
        {
            var error = "The dia process requires the dia experiment file.";
            Console.WriteLine(error);

            return -1;
        }

        private int diaExperimentFileException()
        {
            string text = "Invalid analyst experiment information. Please confirm your file and prepare the following information.\r\n";
            text += "Experiment\tMS Type\tMin m/z\tMax m/z\r\n";
            text += "0\tSCAN\t100\t500\r\n";
            text += "1\tSWATH\t100\t125\r\n";
            text += "2\tSWATH\t125\t150\r\n";
            text += "3\tSWATH\t150\t175\r\n";
            text += "4\tSWATH\t175\t200\r\n";
            text += "5\tSWATH\t200\t225\r\n";
            text += "6\tSWATH\t225\t250\r\n";
            text += "7\tSWATH\t250\t275\r\n";
            text += "8\tSWATH\t275\t300\r\n";
            text += "9\tSWATH\t300\t325\r\n";
            text += "10\tSWATH\t325\t350\r\n";
            text += "11\tSWATH\t350\t375\r\n";
            text += "12\tSWATH\t375\t400\r\n";
            text += "13\tSWATH\t400\t425\r\n";
            text += "14\tSWATH\t425\t450\r\n";
            text += "14\tSWATH\t450\t475\r\n";
            text += "14\tSWATH\t475\t500\r\n";
            text += "This information should be found from Show->Sample information in PeakViewer (AB Sciex case).";

            Console.WriteLine(text);
            return -1;
        }

        private int diaExperimentFileExceptionAif() {
            string text = "Invalid analyst experiment information. Please confirm your file and prepare the following information.\r\n";
            text += "Experiment\tMS Type\tMin m/z\tMax m/z\tDisplay Name\tCollisionEnergy\tDeconvolution Target (1:Yes, 0:No)\r\n";
            text += "0\tAIF\t50\t1500\t10eV\t10\t1\r\n";
            text += "1\tAIF\t50\t1500\t30eV\t30\t1\r\n";
            text += "2\tSCAN\t50\t1500\t0eV\t0\t0\r\n";

            Console.WriteLine(text);
            return -1;
        }

        #endregion
    }
}
