using Msdial.Gcms.Dataprocess;
using Msdial.Gcms.Dataprocess.Algorithm;
using Msdial.Gcms.Dataprocess.Utility;
using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.MsdialConsoleApp.Export;
using Riken.Metabolomics.MsdialConsoleApp.Parser;
using Riken.Metabolomics.MsdialConsoleApp.SaveProject;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Linq;

namespace Riken.Metabolomics.MsdialConsoleApp.Process
{
	public class GcmsProcess
    {
        public static void ConvertRtToKovatRi(string alkaneDictPath, string rtListPath, string outputFilePath)
        {
            var alkaneDict = DatabaseGcUtility.GetRiDictionary(alkaneDictPath);

            using (StreamWriter sw = new StreamWriter(outputFilePath, false, Encoding.ASCII)) {
                using (var sr = new StreamReader(rtListPath, Encoding.ASCII)) {
                    while (sr.Peek() > -1) {
                        var line = sr.ReadLine();
                        if (line == string.Empty) continue;
                        var ri = GcmsScoring.GetRetentionIndexByAlkanes(alkaneDict, float.Parse(line));

                        sw.WriteLine(ri);
                    }
                }
            }
        }

        public static int Run(string inputFolder, string outputFolder, string methodFile, bool isProjectStore)
        {
            Console.WriteLine("Loading library files..");
            Debug.WriteLine(String.Format("inputFolder: {0} -- outputFolder: {1} -- method: {2}", inputFolder, outputFolder, methodFile));

            var dt = DateTime.Now;
            var projectFile = "Project-" + dt.Year.ToString() + dt.Month.ToString() + dt.Day.ToString() + dt.Hour.ToString() + dt.Minute.ToString();
            var projectProp = new ProjectPropertyBean() {
                ProjectDate = dt,
                ProjectFolderPath = inputFolder,
                ProjectFilePath = Path.Combine(inputFolder, projectFile + ".mtd"),
                Ionization = Ionization.EI
            };

            var analysisFiles = AnalysisFilesParser.ReadInput(inputFolder);
            if (analysisFiles == null || analysisFiles.Count == 0) {
                Console.WriteLine("There is no input file to be imported.");
                return -1;
            }

            var gcmsParam = ConfigParser.ReadForGcms(methodFile);
            ConfigParser.SetGCMSAlignmentReferenceFileByFilename(methodFile, analysisFiles, gcmsParam);

            var rdamProperty = AnalysisFilesParser.GetRdamProperty(analysisFiles);
            var alignmentFile = AlignmentResultParser.GetAlignmentFileBean(inputFolder);

            //get file properties in project prop
            projectProp = getFilePropertyDictionaryFromAnalysisFiles(projectProp, analysisFiles);

            if (gcmsParam.RiDictionaryFilePath != string.Empty)
            {
                var errorMessage = string.Empty;
                if (!File.Exists(gcmsParam.RiDictionaryFilePath))
                    throw new FileNotFoundException(String.Format("File '{0}' does not exist.", gcmsParam.RiDictionaryFilePath));
                if (!checkRiDicionaryFiles(analysisFiles, gcmsParam.RiDictionaryFilePath, out errorMessage))
                    throw new FileNotFoundException(String.Format(errorMessage, gcmsParam.RiDictionaryFilePath));

                //probably, at least in fiehn lab, this has to be automatically set from GCMS raw data.
                var flgIncorrectFormat = false;
                var flgIncorrectFiehnFormat = false;

                gcmsParam.FileIdRiInfoDictionary = new Dictionary<int, RiDictionaryInfo>();
                foreach (var analysisFile in analysisFiles) {
                    var file = analysisFile.AnalysisFilePropertyBean;
                    gcmsParam.FileIdRiInfoDictionary[file.AnalysisFileId] = new RiDictionaryInfo() {
                        DictionaryFilePath = file.RiDictionaryFilePath,
                        RiDictionary = DatabaseGcUtility.GetRiDictionary(file.RiDictionaryFilePath)
                    };

                    var dictionary = gcmsParam.FileIdRiInfoDictionary[file.AnalysisFileId].RiDictionary;
                    if (dictionary == null || dictionary.Count == 0) flgIncorrectFormat = true;

                    if (gcmsParam.RiCompoundType == RiCompoundType.Fames) {
                        if (!isFamesContanesMatch(dictionary)) flgIncorrectFiehnFormat = true;
                    }
                }
                if (flgIncorrectFormat == true) {
                    return ridictionaryError();
                }
                if (flgIncorrectFiehnFormat == true) {
                    return famesIndexError();
                }
            }

            var mspDB = new List<MspFormatCompoundInformationBean>();
            if (gcmsParam.MspFilePath != string.Empty)
            {
                if (!File.Exists(gcmsParam.MspFilePath))
					throw new FileNotFoundException(String.Format("File '{0}' does not exist.", gcmsParam.MspFilePath));
				mspDB = DatabaseGcUtility.GetMspDbQueries(gcmsParam.MspFilePath);
                if (mspDB != null && mspDB.Count > 0) {
                    if (gcmsParam.RetentionType == RetentionType.RT)
                        mspDB = mspDB.OrderBy(n => n.RetentionTime).ToList();
                    else
                        mspDB = mspDB.OrderBy(n => n.RetentionIndex).ToList();
                    var counter = 0;
                    foreach (var query in mspDB) {
                        query.Id = counter; counter++;
                    }
                }


            }

            Console.WriteLine("Start processing..");
			return Execute(projectProp, rdamProperty, analysisFiles, gcmsParam, 
                mspDB, alignmentFile, outputFolder, isProjectStore);
		}

        private static bool isFamesContanesMatch(Dictionary<int, float> riDictionary)
        {
            var fiehnFamesDictionary = MspFileParcer.GetFiehnFamesDictionary();

            if (fiehnFamesDictionary.Count != riDictionary.Count) return false;
            foreach (var fFame in fiehnFamesDictionary) {
                var fiehnCnumber = fFame.Key;
                var flg = false;
                foreach (var dict in riDictionary) {
                    if (fiehnCnumber == dict.Key) {
                        flg = true;
                        break;
                    }
                }
                if (flg == false) return false;
            }
            return true;
        }


        private static bool checkRiDicionaryFiles(List<AnalysisFileBean> analysisFiles, string riDictionaryFile, out string errorMessage)
        {
            errorMessage = string.Empty;
            using (var sr = new StreamReader(riDictionaryFile, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) continue;
                    var lineArray = line.Split('\t');
                    if (lineArray.Length < 2) continue;

                    var analysisFilePath = lineArray[0];
                    var riFilePath = lineArray[1];

                    foreach (var file in analysisFiles) {
                        if (file.AnalysisFilePropertyBean.AnalysisFilePath == analysisFilePath) {
                            file.AnalysisFilePropertyBean.RiDictionaryFilePath = riFilePath;
                            break;
                        }
                    }
                }
            }

            //check if ri file exist correctly
            foreach (var file in analysisFiles) {
                if (file.AnalysisFilePropertyBean.RiDictionaryFilePath == null || file.AnalysisFilePropertyBean.RiDictionaryFilePath == string.Empty) {
                    errorMessage += file.AnalysisFilePropertyBean.AnalysisFilePath + "\r\n";
                }
            }
            if (errorMessage != string.Empty) {
                errorMessage += "\r\n" + "The RI dictionary file of the above files is not set correctly. Set your RI dictionary file for all imported files.";
                return false;
            }
            else {
                return true;
            }
        }

        private static ProjectPropertyBean getFilePropertyDictionaryFromAnalysisFiles(ProjectPropertyBean projectProp, List<AnalysisFileBean> analysisFiles)
        {
            for (int i = 0; i < analysisFiles.Count; i++)
            {
                projectProp.FileID_RdamID[i] = i;
                projectProp.FileID_ClassName[i] = analysisFiles[i].AnalysisFilePropertyBean.AnalysisFileClass;
                projectProp.FileID_AnalysisFileType[i] = analysisFiles[i].AnalysisFilePropertyBean.AnalysisFileType;
            }

            return projectProp;
        }

        private static int Execute(ProjectPropertyBean projectProp, RdamPropertyBean rdamProperty, List<AnalysisFileBean> analysisFiles, 
            AnalysisParamOfMsdialGcms gcmsParam, List<MspFormatCompoundInformationBean> mspDB, 
            AlignmentFileBean alignmentFile, string outputfolder, bool isProjectStore)
        {
            for (int i = 0; i < analysisFiles.Count; i++)
            {
                var error = string.Empty;
                ProcessFile.Execute(projectProp, rdamProperty, analysisFiles[i], gcmsParam, mspDB, null, out error);
                if (error != string.Empty) {
                    Console.WriteLine(error);
                }

                //export
                ResultExportForGC.ExportMs1DecResult(analysisFiles[i], outputfolder, mspDB);
                Console.WriteLine(analysisFiles[i].AnalysisFilePropertyBean.AnalysisFilePath + " finished");
            }

            if (mspDB != null && mspDB.Count > 0) {
                mspDB = mspDB.OrderBy(n => n.Id).ToList();
            }

            AlignmentResultBean alignmentResult = null;
            if (analysisFiles.Count > 1 && gcmsParam.TogetherWithAlignment)
            {
                alignmentResult = new AlignmentResultBean();

                PeakAlignment.JointAligner(projectProp, new ObservableCollection<AnalysisFileBean>(analysisFiles), gcmsParam, alignmentResult, mspDB, null);
                Console.WriteLine("Joint aligner finished");

                PeakAlignment.QuantAndGapFilling(rdamProperty, new ObservableCollection<AnalysisFileBean>(analysisFiles), gcmsParam, alignmentResult, alignmentFile, projectProp, mspDB, null);
                Console.WriteLine("Gap filling finished");

                PeakAlignment.WriteAlignedSpotMs1DecResults(new ObservableCollection<AnalysisFileBean>(analysisFiles), alignmentFile.SpectraFilePath, alignmentResult, gcmsParam, mspDB, null);
                Console.WriteLine("All processing finished");

                //export
                var outputFile = Path.Combine(outputfolder, alignmentFile.FileName + ".msdial");
                var ms1DecResults = DataStorageGcUtility.ReadMS1DecResults(alignmentFile.SpectraFilePath);
                ResultExportForGC.ExportAlignmentResult(outputFile, alignmentResult, ms1DecResults, analysisFiles, mspDB, gcmsParam, "Height");
            }

            if (isProjectStore) {
                Console.WriteLine("Now saving the project to be used in MSDIAL GUI...");
                ProjectSave.SaveForGcmsProject(projectProp, rdamProperty, mspDB,
                   new IupacReferenceBean(), gcmsParam, new ObservableCollection<AnalysisFileBean>(analysisFiles),
                   alignmentFile, alignmentResult);
            }

            return 0;
        }

#region // error code

        private static int ridictionaryError()
        {
            string error = "Invalid RI information. Please check that your file follows the following format.\r\n";
            error += "Carbon number\tRT(min)\r\n";
            error += "10\t4.72\r\n";
            error += "11\t5.63\r\n";
            error += "12\t6.81\r\n";
            error += "13\t8.08\r\n";
            error += "14\t9.12\r\n";
            error += "15\t10.33\r\n";
            error += "16\t11.91\r\n";
            error += "18\t14.01\r\n";
            error += "20\t16.15\r\n";
            error += "22\t18.28\r\n";
            error += "24\t20.33\r\n";
            error += "26\t22.17\r\n";
            error += "\r\n";
            error += "This information is required for RI calculation.";

            Console.WriteLine(error);
            return -1;
        }

        private static int famesIndexError()
        {
            var error = "If you use the FAMEs RI, you have to decide the retention times as minute for \r\n"
                            + "C8, C9, C10, C12, C14, C16, C18, C20, C22, C24, C26, C28, C30.";
            Console.WriteLine(error);
            return -1;
        }
#endregion
    }
}
