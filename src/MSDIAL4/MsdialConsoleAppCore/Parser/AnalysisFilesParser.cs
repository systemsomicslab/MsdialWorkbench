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
                        extension == ".raw" || extension == ".d" || extension == ".lrp") {
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
                    || extension == ".raw" || extension == ".d" || extension == ".lrp")
                    importableFiles.Add(file);
            }

            foreach (var file in folderpathes) {
                var extension = System.IO.Path.GetExtension(file).ToLower();
                if (extension == ".abf" || extension == ".cdf" || extension == ".mzml" || extension == ".ibf"
                    || extension == ".wiff" || extension == ".wiff2"
                    || extension == ".raw" || extension == ".d" || extension == ".lrp")
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

        public static List<AnalysisFileBean> ReadCsvContents(string filepath)
        {
            var csvData = new List<string[]>();
            // read csv file
            using (var sr = new StreamReader(filepath, System.Text.Encoding.ASCII))
            {
                string[] searchHeaderNames = { "file_path", "file_name", "type", "class_id", "batch", "analytical_order", "inject_volume" };
                var headerOrder = new List<int>();
                while (!sr.EndOfStream)
                {
                    var header = sr.ReadLine();
                    //if (header == null)
                    //    break;
                    // find first line that doesn't start with '#'
                    if (header.StartsWith('#'.ToString()))
                        continue;

                    Debug.WriteLine("Header: {0}", header, "");

                    var splitHeader = header.Split(',');
                    var headerFields = new List<string>();
                    foreach (var h in splitHeader)
                    {
                        var h1 = h.ToLowerInvariant();
                        var h2 = h1.Trim();
                        var h3 = h2.Replace(' ', '_');
                        headerFields.Add(h3);
                    }

                    for(var i = 0; i < searchHeaderNames.Length; i++)
                    {
                        var name = searchHeaderNames[i];
                        var index = headerFields.IndexOf(name);
                        Debug.WriteLine("  Index of {0}: {1}", name, index);
                        headerOrder.Add(index);
                    }

                    break;
                }
                while (!sr.EndOfStream)
                {
                    var line = sr.ReadLine();
                    //if (line == null)
                    //    break;
                    // skip lines that start with '#'
                    if (line.StartsWith('#'.ToString()))
                        continue;

                    var fields = line.Split(',');
                    var data = new List<string>();
                    for (var i = 0; i < searchHeaderNames.Length; i++)
                    {
                        var index = headerOrder[i];
                        if (index < 0)
                            data.Add(null);
                        else if (index >= fields.Length)
                            data.Add(null);
                        else
                            data.Add(fields[index]);
                    }
                    csvData.Add(data.ToArray());
                    Debug.WriteLine("File line: ->{0}<- converted to: ->{1}<-", line, string.Join("<=>", data));
                }
            }

            // create list of AnalysisFileBeans as 'createAnalysisFileBeans' would do but with more options
            var analysisFiles = new List<AnalysisFileBean>();
            int counter = 0;
            var dt = DateTime.Now;
            var dtString = dt.Year.ToString() + dt.Month.ToString() + dt.Day.ToString() + dt.Hour.ToString() + dt.Minute.ToString();

            foreach (var line in csvData)
            {
                // 0          , 1          , 2     , 3         , 4      , 5,                , 6
                // "file_path", "file_name", "type", "class_id", "batch", "analytical_order", "inject_volume"
                var afFilepath = Path.GetFullPath(Path.Combine(line[0], Path.GetDirectoryName(Path.GetFullPath(filepath))));
                Debug.WriteLine("afFilepath: {0}", afFilepath, "");
                var afFilename = line[1] ?? System.IO.Path.GetFileNameWithoutExtension(afFilepath);

                AnalysisFileType afType;
                var afTypeRes = Enum.TryParse(line[2], true, out afType);
                if (!afTypeRes)
                    afType = AnalysisFileType.Sample;
                
                var afClassId = line[3] ?? counter.ToString();

                int afBatch;
                var afBatchRes = int.TryParse(line[4], out afBatch);
                if (!afBatchRes)
                    afBatch = 1;

                int afAnalyticalOrder;
                var afAnalyticalOrderRes = int.TryParse(line[5], out afAnalyticalOrder);
                if (!afAnalyticalOrderRes)
                    afAnalyticalOrder = counter + 1;

                double afInjectVolume;
                var afInjectVolumeRes = double.TryParse(line[6], out afInjectVolume);
                if (!afInjectVolumeRes)
                    afInjectVolume = 1.0;

                var fileDir = System.IO.Path.GetDirectoryName(afFilepath);
                analysisFiles.Add(new AnalysisFileBean()
                {
                    AnalysisFilePropertyBean = new AnalysisFilePropertyBean()
                    {
                        AnalysisFileId = counter,
                        AnalysisFileIncluded = true,
                        AnalysisFileName = afFilename,
                        AnalysisFilePath = afFilepath,
                        AnalysisFileAnalyticalOrder = afAnalyticalOrder,
                        AnalysisFileClass = afClassId,
                        AnalysisFileType = afType,
                        DeconvolutionFilePath = Path.Combine(fileDir, afFilename + "_" + dtString + ".dcl"),
                        PeakAreaBeanInformationFilePath = Path.Combine(fileDir, afFilename + "_" + dtString + ".pai"),
                        AnalysisBatch = afBatch,
                        InjectionVolume = afInjectVolume
                    }
                });
                counter++;
            }

            return analysisFiles;
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
