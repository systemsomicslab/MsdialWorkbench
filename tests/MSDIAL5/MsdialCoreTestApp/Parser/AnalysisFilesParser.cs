using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
            return ReadInput(input, Console.In, Console.Out, Console.IsInputRedirected, Console.IsOutputRedirected);
        }

        /// <summary>
        /// Reads the analysis files, asking whether to go on when they mix raw data formats.
        /// </summary>
        /// <remarks>
        /// The question is only put to a person at the console, which needs both standard input and
        /// standard output on it. When standard input is redirected (a pipeline, a scheduler, a workflow
        /// that starts the Console as a child process) nobody can answer: reading would either return
        /// null at once, which used to end the run with a NullReferenceException, or wait on a pipe that
        /// never closes. When standard output is redirected (a launcher that reads the Console's output
        /// into a log but leaves its standard input on a terminal) nobody sees the question, and reading
        /// would wait on that terminal. In either case, and when an interactive read reaches the end of
        /// input, the Console prints a warning naming the formats and continues. Returns null only when
        /// a person answers N.
        /// </remarks>
        internal static List<AnalysisFileBean>? ReadInput(string input, TextReader answers, TextWriter output, bool inputRedirected, bool outputRedirected) {
            return ReadInput(input, answers, output, inputRedirected, outputRedirected, null);
        }

        /// <summary>
        /// As <see cref="ReadInput(string)"/>, and adds to <paramref name="orderProblems"/> each
        /// analysis CSV row whose analytical_order or batch_order could not be read and was
        /// replaced (see <see cref="ReadCsvContents(string, List{string})"/>).
        /// </summary>
        public static List<AnalysisFileBean>? ReadInput(string input, List<string>? orderProblems) {
            return ReadInput(input, Console.In, Console.Out, Console.IsInputRedirected, Console.IsOutputRedirected, orderProblems);
        }

        internal static List<AnalysisFileBean>? ReadInput(string input, TextReader answers, TextWriter output, bool inputRedirected, bool outputRedirected, List<string>? orderProblems) {

            var analysisFiles = new List<AnalysisFileBean>();

            FileAttributes attributes = File.GetAttributes(input);
            if ((attributes & FileAttributes.Directory) == FileAttributes.Directory) {
                Debug.WriteLine(String.Format("{0} is folder", input));
                analysisFiles = ReadFolderContents(input);
            }
            else if (isCsv(input))
            {
                Debug.WriteLine(String.Format("{0} is CSV", input));
                analysisFiles = AnalysisFilesParser.ReadCsvContents(input, orderProblems);
            }
            else {
                Debug.WriteLine(String.Format("{0} is file", input));
                var list = new List<string>() { input };
                analysisFiles = CreateAnalysisFileBeans(list);
            }

            var formats = DataFormatsOf(analysisFiles);
            if (formats.Count > 1 && !ContinueWithMultipleFormats(formats, answers, output, inputRedirected, outputRedirected)) {
                return null;
            }

            return analysisFiles;
        }

        /// <summary>
        /// Whether to process files of several raw data formats together. A person at the console is asked
        /// until they answer Y or N; without one, the formats are named in a warning and processing continues.
        /// Nobody is asked when standard input or standard output is redirected.
        /// </summary>
        internal static bool ContinueWithMultipleFormats(IReadOnlyList<string> formats, TextReader answers, TextWriter output, bool inputRedirected, bool outputRedirected) {
            var named = string.Join(", ", formats);
            if (!inputRedirected && !outputRedirected) {
                while (true) {
                    output.WriteLine($"Your input contains several format files to be processed ({named}). Do you want to continue this process? Y/N");
                    var userResponse = answers.ReadLine();
                    if (userResponse is null) break;
                    var answer = userResponse.Trim().ToLowerInvariant();
                    if (answer == "n") return false;
                    if (answer == "y") return true;
                }
            }
            output.WriteLine($"Warning: the input mixes raw data formats ({named}). Nobody at the console can answer whether to go on, so all of them are processed.");
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

        /// <summary>
        /// The stamp a run's per-file intermediates (.dcl, .pai, .rtc) and its alignment result are
        /// named with: the minute the run read its files, as yyyyMMddHHmm.
        /// </summary>
        /// <remarks>
        /// The stamp used to be the year, month, day, hour and minute run together without zero
        /// padding, so different minutes gave one stamp: 2026-11-01 01:15, 2026-11-01 11:05 and
        /// 2026-11-11 01:05 were all "2026111115". A later run then found an earlier run's .rtc
        /// under its own name and read its legacy-corrected RTs. Zero padded, two runs share a
        /// stamp only within one minute. It is the GUI's stamp (DatasetFileSettingModel), and
        /// still twelve digits starting "20", which MS-DIAL Interactive's run_finalisation reads.
        /// </remarks>
        public static string RunStamp(DateTime time) {
            return time.ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture);
        }

        public static List<AnalysisFileBean> ReadCsvContents(string filepath) {
            return ReadCsvContents(filepath, null);
        }

        /// <summary>
        /// Reads the analysis files an analysis CSV lists.
        /// </summary>
        /// <remarks>
        /// An analytical_order that is not a whole number from 1 up (2.5, 0, an empty cell) is
        /// replaced by the row position, as a missing column is, and a batch_order that is not a
        /// whole number by 1. Both used to happen without a word, and the automatic RT
        /// correction interpolates a Blank's model from its neighbours by these numbers: a Blank
        /// given 2.5 to sit between orders 2 and 3, fourth in the list, became order 4 and copied
        /// the file after it. Each replacement is now said on the console and added to
        /// <paramref name="orderProblems"/>, so that LcmsProcess can refuse a run that would
        /// interpolate Blanks on it. A whole number written as a real number ("3.0") is read.
        /// </remarks>
        public static List<AnalysisFileBean> ReadCsvContents(string filepath, List<string>? orderProblems)
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
            var dtString = RunStamp(dt);

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
                if (line[5] is null) {
                    afBatch = 1;
                }
                else if (!TryReadWholeNumber(line[5], out afBatch)) {
                    afBatch = 1;
                    Replaced($"batch_order '{line[5]}' of {afFilename} is not a whole number; batch 1 is used instead");
                }

                int afAnalyticalOrder;
                if (line[6] is null) {
                    afAnalyticalOrder = counter + 1;
                }
                else if (!TryReadWholeNumber(line[6], out afAnalyticalOrder) || afAnalyticalOrder < 1) {
                    afAnalyticalOrder = counter + 1;
                    Replaced($"analytical_order '{line[6]}' of {afFilename} is not a whole number from 1 to {int.MaxValue}; its row position {counter + 1} is used instead");
                }

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
                    DilutionFactor = afInjectVolume,
                    // Recorded while the file is present, so mzTab-M can still name its format after the raw data are gone.
                    RawFileFormat = RawFileFormatClassifier.Classify(afFilepath),
                });
                counter++;
            }

            return analysisFiles;

            void Replaced(string problem) {
                Console.WriteLine($"Warning: {problem}.");
                orderProblems?.Add(problem);
            }
        }

        // A whole number, also written as a real number ("3.0"), in the invariant culture.
        private static bool TryReadWholeNumber(string text, out int value) {
            var trimmed = text.Trim();
            if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)) {
                return true;
            }
            if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var real)
                && Math.Abs(real - Math.Round(real)) < 1e-9
                && real >= int.MinValue && real <= int.MaxValue) {
                value = (int)Math.Round(real);
                return true;
            }
            value = 0;
            return false;
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
            var dtString = RunStamp(dt);

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
                    RetentionTimeCorrectionBean = new RetentionTimeCorrectionBean(Path.Combine(fileDir, filename + "_" + dtString + ".rtc")),
                    // Recorded while the file is present, so mzTab-M can still name its format after the raw data are gone.
                    RawFileFormat = RawFileFormatClassifier.Classify(filepath),
                });
                counter++;
            }
            return analysisFiles;
        }
    }
}
