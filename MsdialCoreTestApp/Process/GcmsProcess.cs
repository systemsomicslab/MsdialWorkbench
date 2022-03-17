using CompMs.App.MsdialConsole.Export;
using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialGcMsApi.Algorithm.Alignment;
using CompMs.MsdialGcMsApi.DataObj;
using CompMs.MsdialGcMsApi.Process;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.App.MsdialConsole.Process
{
    public class GcmsProcess
    {
        public void ConvertRtToKovatRi(string alkaneDictPath, string rtListPath, string outputFilePath)
        {
            var alkaneDict = RetentionIndexHandler.GetRiDictionary(alkaneDictPath);

            using (StreamWriter sw = new StreamWriter(outputFilePath, false, Encoding.ASCII)) {
                using (var sr = new StreamReader(rtListPath, Encoding.ASCII)) {
                    while (sr.Peek() > -1) {
                        var line = sr.ReadLine();
                        if (line == string.Empty) continue;
                        var ri = RetentionIndexHandler.GetRetentionIndexByAlkanes(alkaneDict, float.Parse(line));

                        sw.WriteLine(ri);
                    }
                }
            }
        }

        public int Run(string inputFolder, string outputFolder, string methodFile, bool isProjectStore)
        {
            var param = ConfigParser.ReadForGcms(methodFile);
            var isCorrectlyImported = CommonProcess.SetProjectProperty(param, inputFolder, out List<AnalysisFileBean> analysisFiles, out AlignmentFileBean alignmentFile);
            if (!isCorrectlyImported) return -1;
            if (param.RiDictionaryFilePath != string.Empty)
            {
                var errorMessage = string.Empty;
                if (!File.Exists(param.RiDictionaryFilePath))
                    throw new FileNotFoundException(String.Format("File '{0}' does not exist.", param.RiDictionaryFilePath));
                if (!checkRiDicionaryFiles(analysisFiles, param.RiDictionaryFilePath, out errorMessage))
                    throw new FileNotFoundException(String.Format(errorMessage, param.RiDictionaryFilePath));

                //probably, at least in fiehn lab, this has to be automatically set from GCMS raw data.
                var flgIncorrectFormat = false;
                var flgIncorrectFiehnFormat = false;

                param.FileIdRiInfoDictionary = new Dictionary<int, RiDictionaryInfo>();
                foreach (var file in analysisFiles) {
                    param.FileIdRiInfoDictionary[file.AnalysisFileId] = new RiDictionaryInfo() {
                        DictionaryFilePath = file.RiDictionaryFilePath,
                        RiDictionary = RetentionIndexHandler.GetRiDictionary(file.RiDictionaryFilePath)
                    };

                    var dictionary = param.FileIdRiInfoDictionary[file.AnalysisFileId].RiDictionary;
                    if (dictionary == null || dictionary.Count == 0) flgIncorrectFormat = true;

                    if (param.RiCompoundType == RiCompoundType.Fames) {
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

            var mspDB = new List<MoleculeMsReference>();
            if (param.MspFilePath != string.Empty)
            {
                if (!File.Exists(param.MspFilePath))
					throw new FileNotFoundException(String.Format("File '{0}' does not exist.", param.MspFilePath));
				mspDB = LibraryHandler.ReadMspLibrary(param.MspFilePath);
                if (mspDB != null && mspDB.Count > 0) {
                    if (param.RetentionType == RetentionType.RT)
                        mspDB = mspDB.OrderBy(n => n.ChromXs.RT.Value).ToList();
                    else
                        mspDB = mspDB.OrderBy(n => n.ChromXs.RI.Value).ToList();
                    var counter = 0;
                    foreach (var query in mspDB) {
                        query.ScanID = counter; counter++;
                    }
                }
            }

            CommonProcess.ParseLibraries(param, -1, out IupacDatabase iupacDB, out _, out var txtDB, out var isotopeTextDB, out _);

            var container = new MsdialGcmsDataStorage() {
                AnalysisFiles = analysisFiles, AlignmentFiles = new List<AlignmentFileBean>() { alignmentFile },
                MspDB = mspDB, MsdialGcmsParameter = param, IupacDatabase = iupacDB, IsotopeTextDB = isotopeTextDB, TextDB = txtDB
            };

            Console.WriteLine("Start processing..");
			return Execute(container, outputFolder, isProjectStore);
		}

        private bool isFamesContanesMatch(Dictionary<int, float> riDictionary)
        {
            var fiehnFamesDictionary = RetentionIndexHandler.GetFiehnFamesDictionary();

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


        private bool checkRiDicionaryFiles(List<AnalysisFileBean> analysisFiles, string riDictionaryFile, out string errorMessage)
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
                        if (file.AnalysisFilePath == analysisFilePath) {
                            file.RiDictionaryFilePath = riFilePath;
                            break;
                        }
                    }
                }
            }

            //check if ri file exist correctly
            foreach (var file in analysisFiles) {
                if (file.RiDictionaryFilePath.IsEmptyOrNull()) {
                    errorMessage += file.AnalysisFilePath + "\r\n";
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

        private int Execute(MsdialGcmsDataStorage storage, string outputFolder, bool isProjectSaved) {
            var files = storage.AnalysisFiles;
            foreach (var file in files) {
                FileProcess.Run(file, storage);

                var chromPeakFeatures = MsdialPeakSerializer.LoadChromatogramPeakFeatures(file.PeakAreaBeanInformationFilePath);
                var msdecResults = MsdecResultsReader.ReadMSDecResults(file.DeconvolutionFilePath, out int dcl_version, out List<long> seekPoints);
                Console.WriteLine("DCL version: {0}", dcl_version);

                ResultExporter.ExportChromPeakFeatures(file, outputFolder, storage, null, chromPeakFeatures, msdecResults);
            }

            var alignmentFile = storage.AlignmentFiles.First();
            var factory = new GcmsAlignmentProcessFactory(files, storage, FacadeMatchResultEvaluator.FromDataBaseMapper(storage.DataBaseMapper));
            var aligner = factory.CreatePeakAligner();
            var result = aligner.Alignment(files, alignmentFile, null);

            Common.MessagePack.MessagePackHandler.SaveToFile(result, alignmentFile.FilePath);
            var streamManager = new DirectoryTreeStreamManager(storage.MsdialGcmsParameter.ProjectFolderPath);
            storage.SaveAsync(streamManager, storage.MsdialGcmsParameter.ProjectFileName, string.Empty).Wait();
            return 0;
        }

#region // error code

        private int ridictionaryError()
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

        private int famesIndexError()
        {
            var error = "If you use the FAMEs RI, you have to decide the retention times as minute for \r\n"
                            + "C8, C9, C10, C12, C14, C16, C18, C20, C22, C24, C26, C28, C30.";
            Console.WriteLine(error);
            return -1;
        }
#endregion
    }
}
