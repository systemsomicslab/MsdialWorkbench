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

        /// <summary>
        /// Reads the analysis files, asking the person at the console whether to go on when they mix raw data formats.
        /// Returns null only when that person answers N.
        /// </summary>
        public static List<AnalysisFileBean>? ReadInput(string input) {
            return ReadInput(input, Console.In, Console.Out, Console.IsInputRedirected);
        }

        /// <summary>
        /// Reads the analysis files, asking whether to go on when they mix raw data formats.
        /// </summary>
        /// <remarks>
        /// The question is only put to a person at the console. When standard input is redirected
        /// (a pipeline, a scheduler, a workflow that starts the Console as a child process) nobody
        /// can answer it: reading would either return null at once, which used to end the run with
        /// a NullReferenceException, or wait on a pipe that never closes. In that case, and when an
        /// interactive read reaches the end of input, the Console prints a warning naming the
        /// formats and continues. Returns null only when a person answers N.
        /// </remarks>
        internal static List<AnalysisFileBean>? ReadInput(string input, TextReader answers, TextWriter output, bool inputRedirected) {

            var analysisFiles = new List<AnalysisFileBean>();

            FileAttributes attributes = File.GetAttributes(input);
            if ((attributes & FileAttributes.Directory) == FileAttributes.Directory) {
                Debug.WriteLine(String.Format("{0} is folder", input));
                analysisFiles = ReadFolderContents(input);
            }
            else if (isCsv(input))
            {
                Debug.WriteLine(String.Format("{0} is CSV", input));
                analysisFiles = AnalysisFilesParser.ReadCsvContents(input);
            }
            else {
                Debug.WriteLine(String.Format("{0} is file", input));
                var list = new List<string>() { input };
                analysisFiles = CreateAnalysisFileBeans(list);
            }

            var formats = DataFormatsOf(analysisFiles);
            if (formats.Count > 1 && !ContinueWithMultipleFormats(formats, answers, output, inputRedirected)) {
                return null;
            }

            return analysisFiles;
        }

        /// <summary>
        /// Whether to process files of several raw data formats together. A person at the console is asked
        /// until they answer Y or N; without one, the formats are named in a warning and processing continues.
        /// </summary>
        internal static bool ContinueWithMultipleFormats(IReadOnlyList<string> formats, TextReader answers, TextWriter output, bool inputRedirected) {
            var named = string.Join(", ", formats);
            if (!inputRedirected) {
                while (true) {
                    output.WriteLine($"Your input contains several format files to be processed ({named}). Do you want to continue this process? Y/N");
                    var userResponse = answers.ReadLine();
                    if (userResponse is null) break;
                    var answer = userResponse.Trim().ToLowerInvariant();
                    if (answer == "n") return false;
                    if (answer == "y") return true;
                }
            }
            output.WriteLine($"Warning: the input mixes raw data formats ({named}). No answer can be read from the console, so all of them are processed.");
            return true;
        }

        /// <summary>
        /// The extensions of the supported raw data files among the analysis files, lower case and sorted.
        /// </summary>
        internal static List<string> DataFormatsOf(List<AnalysisFileBean> files) {
            var extensions = new List<string>();
            foreach (var file in files) {
                var filepath = file.AnalysisFilePath;
                if (DataAccess.IsDataFormatSupported(filepath)) {
                    var extension = System.IO.Path.GetExtension(filepath).ToLowerInvariant();
                    if (!extensions.Contains(extension)) {
                        extensions.Add(extension);
                    }
                }
            }
            extensions.Sort(StringComparer.Ordinal);
            return extensions;
        }

        public static bool isCsv(string input) {
            if (System.IO.File.Exists(input)) {
                var extension = System.IO.Path.GetExtension(input).ToLower();
                return extension == ".csv";
            } else {
                return false;
            }
        }

        public static List<AnalysisFileBean> ReadCsvContents(string filepath)
        {
            var csvData = new List<string[]>();
            // read csv file
            using (var sr = new StreamReader(filepath, System.Text.Encoding.ASCII))
            {
                string[] searchHeaderNames = { "file_path", "file_name", "file_type", "class_id", "acquisition_type", "batch_order", "analytical_order", "factor" };
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

                    for (var i = 0; i < searchHeaderNames.Length; i++)
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
                // 0          , 1          , 2          , 3         , 4                   5      , 6                 , 7
                // "file_path", "file_name", "file_type", "class_id", "acquisition_type", "batch", "analytical_order", "factor"
                //var afFilepath = Path.GetFullPath(Path.Combine(line[0], Path.GetDirectoryName(Path.GetFullPath(filepath))));
                var afFilepath = Path.GetFullPath(line[0]);
                Debug.WriteLine("afFilepath: {0}", afFilepath, "");
                if (!System.IO.File.Exists(afFilepath) && !System.IO.Directory.Exists(afFilepath))
                {
                    throw new FileNotFoundException("File not found: " + afFilepath);
                }

                var afFilename = line[1] ?? System.IO.Path.GetFileNameWithoutExtension(afFilepath);

                AnalysisFileType afType;
                var afTypeRes = Enum.TryParse(line[2], true, out afType);
                if (!afTypeRes)
                    afType = AnalysisFileType.Sample;

                var afClassId = line[3] ?? counter.ToString();

                AcquisitionType afAcqType;
                var afAcqTypeRes = Enum.TryParse(line[4], true, out afAcqType);
                if (!afAcqTypeRes)
                    afAcqType = AcquisitionType.DDA;

                int afBatch;
                var afBatchRes = int.TryParse(line[5], out afBatch);
                if (!afBatchRes)
                    afBatch = 1;

                int afAnalyticalOrder;
                var afAnalyticalOrderRes = int.TryParse(line[6], out afAnalyticalOrder);
                if (!afAnalyticalOrderRes)
                    afAnalyticalOrder = counter + 1;

                double afInjectVolume;
                var afInjectVolumeRes = double.TryParse(line[7], out afInjectVolume);
                if (!afInjectVolumeRes)
                    afInjectVolume = 1.0;

                var fileDir = System.IO.Path.GetDirectoryName(afFilepath);
                analysisFiles.Add(new AnalysisFileBean()
                {
                    AnalysisFileId = counter,
                    AnalysisFileIncluded = true,
                    AnalysisFileName = afFilename,
                    AnalysisFilePath = afFilepath,
                    AnalysisFileAnalyticalOrder = afAnalyticalOrder,
                    AnalysisFileClass = afClassId,
                    AnalysisFileType = afType,
                    AcquisitionType = afAcqType,
                    DeconvolutionFilePath = Path.Combine(fileDir, afFilename + "_" + dtString + ".dcl"),
                    PeakAreaBeanInformationFilePath = Path.Combine(fileDir, afFilename + "_" + dtString + ".pai"),
                    RetentionTimeCorrectionBean = new RetentionTimeCorrectionBean(fileDir + "\\" + afFilename + "_" + dtString + ".rtc"),
                    AnalysisBatch = afBatch,
                    DilutionFactor = afInjectVolume
                });
                counter++;
            }

            return analysisFiles;
        }

        public static bool isExistMultipleFormats(List<AnalysisFileBean> files) {
            return DataFormatsOf(files).Count > 1;
        }

        public static List<AnalysisFileBean> ReadFolderContents(string folderpath)
        {
            var importableFiles = new List<string>();

            foreach (var path in Directory.EnumerateFileSystemEntries(folderpath, "*", SearchOption.TopDirectoryOnly)) {
                var extension = Path.GetExtension(path).ToLowerInvariant();
                var isVendorDirectory = Directory.Exists(path) && (extension == ".raw" || extension == ".d");
                if (isVendorDirectory || DataAccess.IsDataFormatSupported(path)) {
                    importableFiles.Add(path);
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
                    DeconvolutionFilePath = Path.Combine(fileDir, filename + "_" + dtString + ".dcl"),
                    PeakAreaBeanInformationFilePath = Path.Combine(fileDir, filename + "_" + dtString + ".pai"),
                    RetentionTimeCorrectionBean = new RetentionTimeCorrectionBean(Path.Combine(fileDir, filename + "_" + dtString + ".rtc")) 
                });
                counter++;
            }
            return analysisFiles;
        }
    }
}
