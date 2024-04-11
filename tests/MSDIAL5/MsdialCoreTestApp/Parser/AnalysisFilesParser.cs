using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace CompMs.App.MsdialConsole.Parser
{
	public sealed class AnalysisFilesParser
    {
        private AnalysisFilesParser() { }

        public static List<AnalysisFileBean> ReadInput(string input) {

            var analysisFiles = new List<AnalysisFileBean>();

            FileAttributes attributes = File.GetAttributes(input);
            if ((attributes & FileAttributes.Directory) == FileAttributes.Directory) {
                Debug.WriteLine(String.Format("{0} is folder", input));
                analysisFiles = ReadFolderContents(input);
            }
            else {
                Debug.WriteLine(String.Format("{0} is file", input));
                var list = new List<string>() { input };
                analysisFiles = CreateAnalysisFileBeans(list);
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
                var filepath = file.AnalysisFilePath;
                if (DataAccess.IsDataFormatSupported(filepath)) {
                    var extension = System.IO.Path.GetExtension(filepath).ToLower();
                    if (!extensions.Contains(extension)) {
                        extensions.Add(extension);
                    }
                }
            }

            if (extensions.Count > 1) return true;
            else return false;
        }

        public static List<AnalysisFileBean> ReadFolderContents(string folderpath)
        {
            var filepathes = Directory.GetFiles(folderpath, "*.*", SearchOption.TopDirectoryOnly);
            var importableFiles = new List<string>();

            foreach (var file in filepathes) {
                if (DataAccess.IsDataFormatSupported(file)) {
                    importableFiles.Add(file);
                }
            }

            return CreateAnalysisFileBeans(importableFiles);
        }

        public static List<AnalysisFileBean> CreateAnalysisFileBeans(List<string> filePaths)
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
                    AnalysisFileId = counter,
                    AnalysisFileIncluded = true,
                    AnalysisFileName = filename,
                    AnalysisFilePath = filepath,
                    AnalysisFileAnalyticalOrder = counter + 1,
                    AnalysisFileClass = counter.ToString(),
                    AnalysisFileType = AnalysisFileType.Sample,
                    DeconvolutionFilePath = fileDir + "\\" + filename + "_" + dtString + ".dcl",
                    PeakAreaBeanInformationFilePath = fileDir + "\\" + filename + "_" + dtString + ".pai",
                    RetentionTimeCorrectionBean = new RetentionTimeCorrectionBean(fileDir + "\\" + filename + "_" + dtString + ".rtc") 
                });
                counter++;
            }
            return analysisFiles;
        }
    }
}
