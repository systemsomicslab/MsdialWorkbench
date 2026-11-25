using CompMs.App.MsdialConsole.Parser;
using CompMs.App.MsdialConsole.Process;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace MsdialCoreTestAppTests.Parser;

/// <summary>
/// How the Console reads, records and refuses the automatic alignment RT correction's method
/// keys, and the analysis file list the correction's Blank interpolation depends on (the final
/// review's CONF-30, CONF-31, CONF-32, CONF-57, CONF-61 and PLAU-3).
/// </summary>
[TestClass]
public sealed class AutomaticRtCorrectionMethodKeyTests
{
    // CONF-30: an unreadable value used to leave the setting at its default and the run went on.
    [TestMethod]
    [DataRow("Execute automatic RT correction for alignment: yes\n", "'Execute automatic RT correction for alignment'")]
    [DataRow("Execute automatic RT correction for alignment: True\nAutomatic RT correction anchor mode: User-Only\n", "'Automatic RT correction anchor mode'")]
    [DataRow("Execute automatic RT correction for alignment: True\nAutomatic RT correction anchor mode: Userly\n", "'Automatic RT correction anchor mode'")]
    [DataRow("Execute automatic RT correction for alignment: True\nAutomatic RT correction reference file ID: two\n", "'Automatic RT correction reference file ID'")]
    [DataRow("Execute automatic RT correction for alignment: True\nAutomatic RT correction reference file ID: 2.5\n", "'Automatic RT correction reference file ID'")]
    [DataRow("Execute automatic RT correction for alignment: True\nAutomatic RT correction outlier MAD threshold: high\n", "'Automatic RT correction outlier MAD threshold'")]
    public void LcmsProcess_RefusesAnUnreadableAutomaticRtCorrectionValueInsteadOfItsDefault(string lines, string key)
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile("method.txt", "Together with alignment: True\n" + lines);

        var (result, output, error) = RunLcmsOnEmptyInput(directory, methodFile);

        Assert.AreEqual(-1, result);
        StringAssert.Contains(error, key);
        StringAssert.Contains(error, "could not be read; the automatic alignment RT correction is refused rather than run on the built-in default");
        Assert.IsFalse(output.Contains("Loading analysis files"), "refused before the analysis files are read");
    }

    [TestMethod]
    public void LcmsProcess_AnUnreadableSettingOfACorrectionThatDoesNotRunIsOnlyReported()
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile(
            "method.txt",
            "Execute automatic RT correction for alignment: False\nAutomatic RT correction anchor mode: User-Only\n");

        var (_, output, error) = RunLcmsOnEmptyInput(directory, methodFile);

        Assert.IsFalse(error.Contains("could not be read"), error);
        StringAssert.Contains(output, "Loading analysis files");
        StringAssert.Contains(output, "'Automatic RT correction anchor mode: User-Only' could not be read");
    }

    // CONF-31: these keys were common keys, recorded as applied in modes that never use them.
    [TestMethod]
    public void OtherModes_RecordTheAutomaticRtCorrectionKeysAsNotApplicable()
    {
        var readers = new (string Mode, Action<string> Read)[] {
            ("gcms", path => ConfigParser.ReadForGcms(path)),
            ("dims", path => ConfigParser.ReadForDimsParameter(path)),
            ("imms", path => ConfigParser.ReadForImmsParameter(path)),
            ("lcimms", path => ConfigParser.ReadForLcImMsParameter(path)),
        };
        using var directory = new TemporaryDirectory();
        foreach (var (mode, read) in readers) {
            var methodFile = directory.CreateFile(
                $"{mode}.txt",
                """
                Execute automatic RT correction for alignment: True
                Automatic RT correction anchor mode: Hybrid
                Automatic RT correction preview only: False
                """);

            read(methodFile);

            var expected = new[] { "Execute automatic RT correction for alignment", "Automatic RT correction anchor mode", "Automatic RT correction preview only" };
            CollectionAssert.AreEqual(expected, RecordedKeys(methodFile, "not_applicable"), mode);
            Assert.IsFalse(RecordedKeys(methodFile, "applied").Intersect(expected).Any(), mode);
            Assert.IsFalse(RecordedKeys(methodFile, "unrecognised").Intersect(expected).Any(), mode);
        }
    }

    [TestMethod]
    [DataRow("True")]
    [DataRow("yes")]
    public void OtherModes_RefuseAPreviewTheyCannotRun(string value)
    {
        using var directory = new TemporaryDirectory();
        var readers = new Action<string>[] {
            path => ConfigParser.ReadForGcms(path),
            path => ConfigParser.ReadForDimsParameter(path),
            path => ConfigParser.ReadForImmsParameter(path),
            path => ConfigParser.ReadForLcImMsParameter(path),
        };
        var methodFile = directory.CreateFile("method.txt", $"Automatic RT correction preview only: {value}\n");

        foreach (var read in readers) {
            var error = Assert.ThrowsException<FormatException>(() => read(methodFile));
            StringAssert.Contains(error.Message, "only LC-MS runs have; the run is refused rather than run in full");
        }
    }

    [TestMethod]
    public void LcmsReader_StillAppliesTheAutomaticRtCorrectionKeys()
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile(
            "method.txt",
            """
            Execute automatic RT correction for alignment: True
            Automatic RT correction reference file ID: 3
            Automatic RT correction outlier MAD threshold: 4
            """);

        var parameter = ConfigParser.ReadForLcmsParameter(methodFile);

        Assert.IsTrue(parameter.AlignmentBaseParam.AutomaticRtCorrection.Execute);
        Assert.AreEqual(3, parameter.AlignmentBaseParam.AutomaticRtCorrection.ReferenceFileId);
        Assert.AreEqual(4f, parameter.AlignmentBaseParam.AutomaticRtCorrection.OutlierMadThreshold);
        CollectionAssert.AreEqual(
            new[] { "Execute automatic RT correction for alignment", "Automatic RT correction reference file ID", "Automatic RT correction outlier MAD threshold" },
            RecordedKeys(methodFile, "applied"));
        Assert.AreEqual(0, RecordedKeys(methodFile, "not_applicable").Count);
    }

    // CONF-32: the alignment's reference is the correction's, and the record said otherwise.
    [TestMethod]
    public void LcmsReader_RecordsTheAlignmentReferenceAsOverriddenWhenTheCorrectionRuns()
    {
        using var directory = new TemporaryDirectory();
        var on = directory.CreateFile("on.txt", "Execute automatic RT correction for alignment: True\nAlignment reference file ID: 5\n");
        var off = directory.CreateFile("off.txt", "Execute automatic RT correction for alignment: False\nAlignment reference file ID: 5\n");

        ConfigParser.ReadForLcmsParameter(on);
        ConfigParser.ReadForLcmsParameter(off);

        Assert.IsFalse(RecordedKeys(on, "applied").Contains("Alignment reference file ID"));
        var overridden = (JArray)Record(on)["overridden"]!;
        Assert.AreEqual(1, overridden.Count);
        Assert.AreEqual("Alignment reference file ID", (string)overridden[0]["key"]!);
        Assert.AreEqual("5", (string)overridden[0]["value"]!);
        StringAssert.Contains((string)overridden[0]["reason"]!, "automatic RT correction's reference file");
        CollectionAssert.Contains(RecordedKeys(off, "applied"), "Alignment reference file ID");
        Assert.AreEqual(0, ((JArray)Record(off)["overridden"]!).Count);
    }

    // PLAU-3: under tr-TR, "ID".ToLower() is "ıd" (dotless i), and the key was not recognised.
    [TestMethod]
    public void LcmsReader_RecognisesKeysWithAnIUnderATurkishCulture()
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile(
            "method.txt",
            """
            Ion mode: Negative
            Execute automatic RT correction for alignment: True
            Automatic RT correction reference file ID: 3
            Automatic RT correction minimum signal to noise: 5
            """);
        var original = Thread.CurrentThread.CurrentCulture;
        MsdialLcmsParameterProbe probe;
        try {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
            var parameter = ConfigParser.ReadForLcmsParameter(methodFile);
            probe = new MsdialLcmsParameterProbe(parameter.IonMode, parameter.AlignmentBaseParam.AutomaticRtCorrection.ReferenceFileId, parameter.AlignmentBaseParam.AutomaticRtCorrection.MinimumSignalToNoise);
        }
        finally {
            Thread.CurrentThread.CurrentCulture = original;
        }

        Assert.AreEqual(IonMode.Negative, probe.IonMode);
        Assert.AreEqual(3, probe.ReferenceFileId);
        Assert.AreEqual(5f, probe.MinimumSignalToNoise);
        Assert.AreEqual(0, RecordedKeys(methodFile, "unrecognised").Count);
    }

    private sealed record MsdialLcmsParameterProbe(IonMode IonMode, int ReferenceFileId, float MinimumSignalToNoise);

    // CONF-57: "2026"+"11"+"1"+"1"+"15", "2026"+"11"+"1"+"11"+"5" and "2026"+"11"+"11"+"1"+"5"
    // were one stamp.
    [TestMethod]
    public void RunStamp_IsZeroPaddedSoDifferentMinutesNeverShareIt_AndInteractiveStillReadsIt()
    {
        var stamps = new[] {
            new DateTime(2026, 11, 1, 1, 15, 0),
            new DateTime(2026, 11, 1, 11, 5, 0),
            new DateTime(2026, 11, 11, 1, 5, 0),
        }.Select(AnalysisFilesParser.RunStamp).ToList();

        CollectionAssert.AreEqual(new[] { "202611010115", "202611011105", "202611110105" }, stamps);
        // MS-DIAL Interactive's run_finalisation._TIMESTAMP.
        Assert.IsTrue(stamps.All(stamp => Regex.IsMatch(stamp, @"^20\d{6,10}$")));
    }

    // CONF-61: an order the CSV reader could not read became the row position without a word.
    [TestMethod]
    public void ReadCsvContents_SaysWhichOrdersItReplaced()
    {
        using var directory = new TemporaryDirectory();
        var names = new[] { "a", "b", "c", "d", "e", "f" };
        var paths = names.Select(name => directory.CreateFile(name + ".mzML")).ToArray();
        var csv = directory.CreateFile(
            "analysis_files.csv",
            "file_path,file_name,file_type,class_id,acquisition_type,batch_order,analytical_order,factor\n"
            + $"{paths[0]},a,Sample,S,DDA,1,1,1\n"
            + $"{paths[1]},b,Blank,B,DDA,1,2.5,1\n"
            + $"{paths[2]},c,Sample,S,DDA,1,0,1\n"
            + $"{paths[3]},d,Sample,S,DDA,1,,1\n"
            + $"{paths[4]},e,Sample,S,DDA,two,3.0,1\n"
            + $"{paths[5]},f,Sample,S,DDA,2,99999999999,1\n");
        var problems = new List<string>();

        var files = AnalysisFilesParser.ReadCsvContents(csv, problems);

        CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 3, 6 }, files.Select(file => file.AnalysisFileAnalyticalOrder).ToArray(), "row positions in place of the unreadable ones; 3.0 is 3");
        CollectionAssert.AreEqual(new[] { 1, 1, 1, 1, 1, 2 }, files.Select(file => file.AnalysisBatch).ToArray());
        Assert.AreEqual(5, problems.Count, string.Join("\n", problems));
        StringAssert.Contains(problems[0], "analytical_order '2.5' of b is not a whole number from 1");
        StringAssert.Contains(problems[1], "analytical_order '0' of c");
        StringAssert.Contains(problems[2], "analytical_order '' of d");
        StringAssert.Contains(problems[3], "batch_order 'two' of e is not a whole number; batch 1 is used instead");
        StringAssert.Contains(problems[4], "analytical_order '99999999999' of f");
        StringAssert.Matches(Path.GetFileName(files[0].RetentionTimeCorrectionBean.RetentionTimeCorrectionResultFilePath), new Regex(@"^a_20\d{10}\.rtc$"), "the zero-padded run stamp");
    }

    [TestMethod]
    public void RefuseBlankInterpolationOnReplacedOrders_OnlyWhenBlanksWouldBeInterpolatedOnThem()
    {
        var correction = new AutomaticAlignmentRetentionTimeCorrectionParameter { Execute = true, InterpolateBlankByAnalyticalOrder = true };
        var withBlank = new[] {
            new AnalysisFileBean { AnalysisFileId = 0, AnalysisFileType = AnalysisFileType.Sample },
            new AnalysisFileBean { AnalysisFileId = 1, AnalysisFileType = AnalysisFileType.Blank },
        };
        var withoutBlank = new[] { withBlank[0] };
        var problems = new[] { "analytical_order '2.5' of b is not a whole number from 1 to 2147483647; its row position 2 is used instead" };

        var refusal = LcmsProcess.RefuseBlankInterpolationOnReplacedOrders(correction, withBlank, problems);

        Assert.IsNotNull(refusal);
        StringAssert.Contains(refusal, "analytical_order '2.5' of b");
        StringAssert.Contains(refusal, "'Automatic RT correction interpolate blanks by analytical order: False'");
        Assert.IsNull(LcmsProcess.RefuseBlankInterpolationOnReplacedOrders(correction, withoutBlank, problems), "no Blank to place");
        Assert.IsNull(LcmsProcess.RefuseBlankInterpolationOnReplacedOrders(correction, withBlank, Array.Empty<string>()));
        Assert.IsNull(LcmsProcess.RefuseBlankInterpolationOnReplacedOrders(
            new AutomaticAlignmentRetentionTimeCorrectionParameter { Execute = true, InterpolateBlankByAnalyticalOrder = false }, withBlank, problems));
        Assert.IsNull(LcmsProcess.RefuseBlankInterpolationOnReplacedOrders(
            new AutomaticAlignmentRetentionTimeCorrectionParameter { Execute = false, InterpolateBlankByAnalyticalOrder = true }, withBlank, problems));
    }

    // CONF-57: an .rtc found under this run's name is an earlier run's, and is not read.
    [TestMethod]
    public void IgnoreEarlierRtCorrectionResults_DetachesAnRtcThatAlreadyExists()
    {
        using var directory = new TemporaryDirectory();
        var stale = directory.CreateFile("s1_202611010115.rtc", "an earlier run's legacy correction");
        var files = new[] {
            new AnalysisFileBean { AnalysisFileId = 0, RetentionTimeCorrectionBean = new RetentionTimeCorrectionBean(stale) },
            new AnalysisFileBean { AnalysisFileId = 1, RetentionTimeCorrectionBean = new RetentionTimeCorrectionBean(Path.Combine(directory.Path, "s2_202611010115.rtc")) },
        };
        var originalOut = Console.Out;
        var output = new StringWriter();
        try {
            Console.SetOut(output);
            LcmsProcess.IgnoreEarlierRtCorrectionResults(files);
        }
        finally {
            Console.SetOut(originalOut);
        }

        Assert.AreEqual(string.Empty, files[0].RetentionTimeCorrectionBean.RetentionTimeCorrectionResultFilePath);
        Assert.IsNull(files[0].RetentionTimeCorrectionBean.PredictedRt);
        StringAssert.EndsWith(files[1].RetentionTimeCorrectionBean.RetentionTimeCorrectionResultFilePath, "s2_202611010115.rtc", "a path with no file is left as it is");
        StringAssert.Contains(output.ToString(), "s1_202611010115.rtc holds an earlier run's legacy RT correction result");
        Assert.IsTrue(File.Exists(stale), "nothing beside the raw data is deleted");
    }

    private static (int Result, string Output, string Error) RunLcmsOnEmptyInput(TemporaryDirectory directory, string methodFile)
    {
        var input = Path.Combine(directory.Path, "empty-input");
        Directory.CreateDirectory(input);
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var output = new StringWriter();
        var error = new StringWriter();
        try {
            Console.SetOut(output);
            Console.SetError(error);
            var result = new LcmsProcess().Run(input, Path.Combine(directory.Path, "output"), methodFile, isProjectSaved: false, targetMz: -1f);
            return (result, output.ToString(), error.ToString());
        }
        finally {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private static JObject Record(string methodFile)
    {
        var record = Path.Combine(Path.GetDirectoryName(methodFile)!, Path.GetFileNameWithoutExtension(methodFile) + ".keys.json");
        return JObject.Parse(File.ReadAllText(record));
    }

    private static List<string> RecordedKeys(string methodFile, string list)
    {
        return Record(methodFile)[list]!.Select(item => (string)item!).ToList();
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MsdialCoreTestAppTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string CreateFile(string name, string content = "")
        {
            var path = System.IO.Path.Combine(Path, name);
            File.WriteAllText(path, content);
            return path;
        }

        public void Dispose()
        {
            if (Directory.Exists(Path)) {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
