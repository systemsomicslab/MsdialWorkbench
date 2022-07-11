using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Riken.Metabolomics.MsdialConsoleApp.Parser
{
	public sealed class AnalysisFilesParser
    {
        private AnalysisFilesParser() { }

        public static List<AnalysisFileBean> ReadInput(string input) {

            var analysisFiles = new List<AnalysisFileBean>();

            FileAttributes attributes = File.GetAttributes(input);
            if ((attributes & FileAttributes.Directory) == FileAttributes.Directory) {
                Debug.WriteLine(String.Format("{0} is folder", input));
                analysisFiles = AnalysisFilesParser.ReadFolderContents(input);
            } else if (isCsv(input)) {
                Debug.WriteLine(String.Format("{0} is CSV", input));
                analysisFiles = AnalysisFilesParser.ReadCsvContents(input);
            }
            else {
                Debug.WriteLine(String.Format("{0} is file", input));
                var list = new List<string>() { input };
                analysisFiles = AnalysisFilesParser.createAnalysisFileBeans(list);
            }

            if (isExistMultipleFormats(analysisFiles)) {
                while (true) {
                    Console.WriteLine("Your input contains several format files to be processed (abf, cdf, mzml, ibf, wiff)." +
                        " Do you want to continue this process? Y/N");
                    var userResponse = Console.ReadLine();
                    if (userResponse.ToLower() == "n") return null;
                    else if (userResponse.ToLower() == "y") break;
                }
            }

            return analysisFiles;
        }

        public static bool isExistMultipleFormats(List<AnalysisFileBean> files) {

            var extensions = new List<string>();
            foreach (var file in files) {
                var filepath = file.AnalysisFilePropertyBean.AnalysisFilePath;
                if (System.IO.File.Exists(filepath)) {
                    var extension = System.IO.Path.GetExtension(filepath).ToLower();
                    if (extension == ".abf" || extension == ".cdf" || extension == ".mzml" || 
                        extension == ".ibf" || extension == ".wiff" || extension == ".wiff2" ||
                        extension == ".raw" || extension == ".d") {
                        if (!extensions.Contains(extension)) {
                            extensions.Add(extension);
                        }
                    }
                }
            }

            if (extensions.Count > 1) return true;
            else return false;
        }

        public static bool isCsv(string input) {
            if (System.IO.File.Exists(input)) {
                var extension = System.IO.Path.GetExtension(input).ToLower();
                return extension == ".csv";
            } else {
                return false;
            }
        }

        public static List<AnalysisFileBean> ReadFolderContents(string folderpath)
        {
            var filepathes = Directory.GetFiles(folderpath, "*.*", SearchOption.TopDirectoryOnly);
            var folderpathes = Directory.GetDirectories(folderpath);
            var importableFiles = new List<string>();

            foreach (var file in filepathes) {
                var extension = System.IO.Path.GetExtension(file).ToLower();
                if (extension == ".abf" || extension == ".cdf" || extension == ".mzml" || extension == ".ibf" 
                    || extension == ".wiff" || extension == ".wiff2"
                    || extension == ".raw" || extension == ".d")
                    importableFiles.Add(file);
            }

            foreach (var file in folderpathes) {
                var extension = System.IO.Path.GetExtension(file).ToLower();
                if (extension == ".abf" || extension == ".cdf" || extension == ".mzml" || extension == ".ibf"
                    || extension == ".wiff" || extension == ".wiff2"
                    || extension == ".raw" || extension == ".d")
                    importableFiles.Add(file);
            }

            return createAnalysisFileBeans(importableFiles);
        }

        public static List<AnalysisFileBean> createAnalysisFileBeans(List<string> filePaths)
        {
            var analysisFiles = new List<AnalysisFileBean>();
            int counter = 0;
            var dt = DateTime.Now;
            var dtString = dt.Year.ToString() + dt.Month.ToString() + dt.Day.ToString() + dt.Hour.ToString() + dt.Minute.ToString();

            foreach (var filepath in filePaths)
            {
                var filename = System.IO.Path.GetFileNameWithoutExtension(filepath);
                var fileDir = System.IO.Path.GetDirectoryName(filepath);
                analysisFiles.Add(new AnalysisFileBean()
                {
                    AnalysisFilePropertyBean = new AnalysisFilePropertyBean()
                    {
                        AnalysisFileId = counter,
                        AnalysisFileIncluded = true,
                        AnalysisFileName = filename,
                        AnalysisFilePath = filepath,
                        AnalysisFileAnalyticalOrder = counter + 1,
                        AnalysisFileClass = counter.ToString(),
                        AnalysisFileType = AnalysisFileType.Sample,
                        DeconvolutionFilePath = Path.Combine(fileDir, filename + "_" + dtString + ".dcl"),
                        PeakAreaBeanInformationFilePath = Path.Combine(fileDir, filename + "_" + dtString + ".pai")
                    }
                });
                counter++;
            }
            return analysisFiles;
        }

        public static List<AnalysisFileBean> ReadCsvContents(string folderpath)
        {
            return null; // todo: fill in
        }

        public static RdamPropertyBean GetRdamProperty(List<AnalysisFileBean> analysisFiles)
        {
            int counter = 0;
            var rdamPropertyBean = new RdamPropertyBean();
            var rdamFileContentBean = new RdamFileContentBean();

            for (int i = 0; i < analysisFiles.Count; i++)
            {
                var filepath = analysisFiles[i].AnalysisFilePropertyBean.AnalysisFilePath;

                rdamPropertyBean.RdamFilePath_RdamFileID[filepath] = i;
                rdamPropertyBean.RdamFileID_RdamFilePath[i] = filepath;
                
                rdamFileContentBean = new RdamFileContentBean();
                rdamFileContentBean.MeasurementID_FileID[0] = counter;
                rdamFileContentBean.FileID_MeasurementID[counter] = 0;
                rdamFileContentBean.MeasurementNumber = 1;
                rdamFileContentBean.RdamFileID = i;
                rdamFileContentBean.RdamFileName = System.IO.Path.GetFileNameWithoutExtension(filepath);
                rdamFileContentBean.RdamFilePath = filepath;

                rdamPropertyBean.RdamFileContentBeanCollection.Add(rdamFileContentBean);

                counter++;
            }
            return rdamPropertyBean;
        }
    }
}
