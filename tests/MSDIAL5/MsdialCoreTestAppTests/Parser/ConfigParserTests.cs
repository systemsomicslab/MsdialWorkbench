using CompMs.App.MsdialConsole.Parser;
using CompMs.App.MsdialConsole.Process;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MsdialCoreTestAppTests.Parser;

[TestClass]
public sealed class ConfigParserTests
{
    [TestMethod]
    public void ReadCommonParameter_SampleMaxKeyComparesTheSampleMaxAtItsFoldChange()
    {
        var parameter = new MsdialLcmsParameter { BlankFiltering = BlankFiltering.SampleAveOverBlankAve };

        var result = ConfigParser.ReadCommonParameter(parameter, "sample max / blank average", "7");

        Assert.IsTrue(result.IsApplied);
        Assert.AreEqual(BlankFiltering.SampleMaxOverBlankAve, parameter.BlankFiltering);
        Assert.AreEqual(7f, parameter.FoldChangeForBlankFiltering);
    }

    [TestMethod]
    public void ReadCommonParameter_SampleAverageKeyComparesTheSampleAverageAtItsFoldChange()
    {
        // This key used to set a property blank filtering never reads, so it had no effect at all.
        var parameter = new MsdialLcmsParameter();

        var result = ConfigParser.ReadCommonParameter(parameter, "sample average / blank average", "7");

        Assert.IsTrue(result.IsApplied);
        Assert.AreEqual(BlankFiltering.SampleAveOverBlankAve, parameter.BlankFiltering);
        Assert.AreEqual(7f, parameter.FoldChangeForBlankFiltering);
    }

    [TestMethod]
    public void ReadForLcms_BlankFilteringAndItsFoldChangeAreSetByTheirOwnKeys()
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile(
            "method.txt",
            """
            Blank filtering: SampleAveOverBlankAve
            Fold change for blank filtering: 3
            """);

        var parameter = ConfigParser.ReadForLcmsParameter(methodFile);

        Assert.AreEqual(BlankFiltering.SampleAveOverBlankAve, parameter.BlankFiltering);
        Assert.AreEqual(3f, parameter.FoldChangeForBlankFiltering);
    }

    [TestMethod]
    public void ReadForLcms_BlankFilteringAloneKeepsTheDefaultFoldChange()
    {
        // The old reader accepted only SampleMaxOverBlankAve, so a sample-average mode was dropped.
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile("method.txt", "Blank filtering: sampleaveoverblankave");

        var parameter = ConfigParser.ReadForLcmsParameter(methodFile);

        Assert.AreEqual(BlankFiltering.SampleAveOverBlankAve, parameter.BlankFiltering);
        Assert.AreEqual(new MsdialLcmsParameter().FoldChangeForBlankFiltering, parameter.FoldChangeForBlankFiltering);
    }

    [TestMethod]
    public void ReadCommonParameter_BlankFilteringReportsAnUnknownModeAsUnusable()
    {
        var parameter = new MsdialLcmsParameter();

        var result = ConfigParser.ReadCommonParameter(parameter, "blank filtering", "SampleMedianOverBlankAve");

        Assert.IsTrue(result.IsUnusableValue);
        Assert.AreEqual(BlankFiltering.SampleMaxOverBlankAve, parameter.BlankFiltering);
    }

    [TestMethod]
    [DataRow("Sample max / blank average: 5\nSample average / blank average: 5", "choose different blank filtering comparisons", DisplayName = "both shorthands (earlier text exports)")]
    [DataRow("Blank filtering: SampleAveOverBlankAve\nSample max / blank average: 7", "choose different blank filtering comparisons", DisplayName = "average mode and sample max shorthand")]
    [DataRow("Sample average / blank average: 7\nBlank filtering: SampleMaxOverBlankAve", "choose different blank filtering comparisons", DisplayName = "sample average shorthand and max mode")]
    [DataRow("Fold change for blank filtering: 3\nSample max / blank average: 7", "set different fold changes", DisplayName = "fold change and shorthand value")]
    public void ReadForLcms_RefusesBlankFilteringLinesThatDisagree(string lines, string reason)
    {
        // Whichever line came last would otherwise decide, and the file would say two things.
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile("method.txt", lines);

        var error = Assert.ThrowsException<FormatException>(() => ConfigParser.ReadForLcmsParameter(methodFile));

        StringAssert.Contains(error.Message, "method.txt");
        StringAssert.Contains(error.Message, reason);
        Assert.IsTrue(File.Exists(Path.Combine(Path.GetDirectoryName(methodFile)!, "method.keys.json")),
            "the key record is still written, so every other finding is visible too");
    }

    [TestMethod]
    public void ReadForLcms_AcceptsAShorthandThatAgreesWithTheOtherKeys()
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile(
            "method.txt",
            """
            Blank filtering: SampleMaxOverBlankAve
            Fold change for blank filtering: 7.0
            Sample max / blank average: 7
            """);

        var parameter = ConfigParser.ReadForLcmsParameter(methodFile);

        Assert.AreEqual(BlankFiltering.SampleMaxOverBlankAve, parameter.BlankFiltering);
        Assert.AreEqual(7f, parameter.FoldChangeForBlankFiltering);
    }

    [TestMethod]
    public void ReadForLcms_OneBlankFilteringKeyWrittenTwiceIsNotAConflict()
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile(
            "method.txt",
            """
            Sample average / blank average: 5
            Sample average / blank average: 3
            Sample max / blank average:
            """);

        var parameter = ConfigParser.ReadForLcmsParameter(methodFile);

        Assert.AreEqual(BlankFiltering.SampleAveOverBlankAve, parameter.BlankFiltering);
        Assert.AreEqual(3f, parameter.FoldChangeForBlankFiltering);
    }

    [TestMethod]
    public void ReadForLcms_RunsWithTheBlankFilteringThatParametersAsTextWrote()
    {
        // The whole export is read back, so no other line it writes can contradict these two.
        var written = new MsdialLcmsParameter {
            BlankFiltering = BlankFiltering.SampleAveOverBlankAve,
            FoldChangeForBlankFiltering = 3.5f,
        };
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile("method.txt", string.Join(Environment.NewLine, written.ParametersAsText()));

        var read = ConfigParser.ReadForLcmsParameter(methodFile);

        CollectionAssert.IsSubsetOf(
            new[] { "Blank filtering: SampleAveOverBlankAve", "Fold change for blank filtering: 3.5" },
            written.ParametersAsText());
        Assert.AreEqual(BlankFiltering.SampleAveOverBlankAve, read.BlankFiltering);
        Assert.AreEqual(3.5f, read.FoldChangeForBlankFiltering);
    }

    [TestMethod]
    public void ReadCommonParameter_AcceptsAMinimumPeakHeightWrittenAsARealNumber()
    {
        // THE REGRESSION. MinimumAmplitude is a double, ParameterBase:589 writes it back as one,
        // and MS-DIAL Interactive writes it from a Python float, so every method file in the
        // reanalysis workspace carried "Minimum peak height: 500.0". The arm parsed it with
        // int.TryParse and returned true regardless, so the value was discarded, MinimumAmplitude
        // kept its built-in 1000, and the run recorded 500 in its audit trail. Every threshold the
        // contract's zero-threshold diagnostic produced was thrown away exactly this way.
        var parameter = new MsdialLcmsParameter();

        var result = ConfigParser.ReadCommonParameter(parameter, "minimum peak height", "500.0");

        Assert.IsTrue(result.IsApplied);
        Assert.AreEqual(500d, parameter.MinimumAmplitude);
    }

    [TestMethod]
    public void ReadCommonParameter_StillAcceptsAWholeNumberedThreshold()
    {
        var parameter = new MsdialLcmsParameter();

        Assert.IsTrue(ConfigParser.ReadCommonParameter(parameter, "minimum peak height", "0").IsApplied);
        Assert.AreEqual(0d, parameter.MinimumAmplitude, "the diagnostic run sets the threshold to zero");
    }

    [TestMethod]
    public void ReadCommonParameter_ReportsAValueItCannotReadInsteadOfClaimingItApplied()
    {
        // The distinction the old bool could not make. An unusable value is not an unknown key:
        // the key is spelled correctly, so nobody reading the method file would suspect it.
        var parameter = new MsdialLcmsParameter();

        var result = ConfigParser.ReadCommonParameter(parameter, "minimum peak height", "quite high");

        Assert.IsTrue(result.IsUnusableValue);
        Assert.IsFalse(result.IsApplied);
        Assert.IsFalse(result.IsUnknownKey);
        Assert.AreEqual(1000d, parameter.MinimumAmplitude, "the built-in default, untouched");
    }

    [TestMethod]
    public void ReadCommonParameter_KeepsAnUnknownKeyDistinctFromAnUnusableValue()
    {
        var parameter = new MsdialLcmsParameter();

        var result = ConfigParser.ReadCommonParameter(parameter, "a parameter that does not exist", "7");

        Assert.IsTrue(result.IsUnknownKey);
        Assert.IsFalse(result.IsUnusableValue);
    }

    [TestMethod]
    public void ReadCommonParameter_RefusesACountThatNamesNoWholeNumber()
    {
        // "5" and "5.0" are the same count because the writers disagree about which they emit.
        // "5.7" is not a count, and rounding it to 6 would hide that whoever wrote it believed
        // this parameter could express something it cannot.
        var parameter = new MsdialLcmsParameter();

        Assert.IsTrue(ConfigParser.ReadCommonParameter(parameter, "smoothing level", "5.0").IsApplied);
        Assert.AreEqual(5, parameter.SmoothingLevel);
        Assert.IsTrue(ConfigParser.ReadCommonParameter(parameter, "smoothing level", "5.7").IsUnusableValue);
        Assert.AreEqual(5, parameter.SmoothingLevel, "the refused value left the previous one alone");
    }

    [TestMethod]
    public void ReadCommonParameter_ReadsARealNumberTheSameWayOnEveryMachine()
    {
        // A method file is machine-written. It must mean the same thing where the decimal
        // separator is a comma, so the readers parse with the invariant culture.
        var parameter = new MsdialLcmsParameter();

        Assert.IsTrue(ConfigParser.ReadCommonParameter(parameter, "mass slice width", "0.1").IsApplied);
        Assert.AreEqual(0.1f, parameter.MassSliceWidth, 1e-7f);
    }

    [TestMethod]
    public void ReadCommonParameter_ReadsTheTwoKeysABadRenameHadMadeUnreachable()
    {
        // A `value` -> `valueLower` rename was applied inside the case labels themselves, so
        // "Sigma window value" and "Replace true zero values with 1/2 of minimum peak height over
        // all samples" could never be set from a method file however correctly they were spelled.
        // Both are written into every exported method file by ParameterBase.
        var parameter = new MsdialLcmsParameter();

        Assert.IsTrue(ConfigParser.ReadCommonParameter(parameter, "sigma window value", "0.7").IsApplied);
        Assert.AreEqual(0.7f, parameter.SigmaWindowValue, 1e-7f);

        var replace = ConfigParser.ReadCommonParameter(
            parameter, "replace true zero values with 1/2 of minimum peak height over all samples", "true");

        Assert.IsTrue(replace.IsApplied);
        Assert.IsTrue(parameter.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples);
    }

    [DataTestMethod]
    [DataRow("keep original precursor isotopes")]
    [DataRow("exclude after precursor")]
    [DataRow("corrdec execute")]
    [DataRow("is private version")]
    [DataRow("is private version of tada")]
    public void ReadCommonParameter_ReadsAnOnOffKeyInBothDirections(string key)
    {
        // THE REGRESSION. These arms assigned only one of the two values and returned true for
        // either, so the other value was reported as applied and silently ignored.
        // KeepOriginalPrecursorIsotopes defaults to false, so "Keep original precursor isotopes:
        // True" never took effect while the key record said it had.
        var parameter = new MsdialLcmsParameter();
        Func<bool> current = key switch
        {
            "keep original precursor isotopes" => () => parameter.KeepOriginalPrecursorIsotopes,
            "exclude after precursor" => () => parameter.RemoveAfterPrecursor,
            "corrdec execute" => () => parameter.CorrDecParam.CanExcute,
            "is private version" => () => parameter.IsLabPrivate,
            "is private version of tada" => () => parameter.IsLabPrivateVersionTada,
            _ => throw new ArgumentOutOfRangeException(nameof(key)),
        };

        AssertReadsBothDirections(value => ConfigParser.ReadCommonParameter(parameter, key, value), current);
    }

    [DataTestMethod]
    [DataRow("replace quant mass by user defined value")]
    [DataRow("is quant mass based on base peak mz")]
    public void ReadGcmsSpecificParameter_ReadsAnOnOffKeyInBothDirections(string key)
    {
        var parameter = new MsdialGcmsParameter();
        Func<bool> current = key switch
        {
            "replace quant mass by user defined value" => () => parameter.IsReplaceQuantmassByUserDefinedValue,
            "is quant mass based on base peak mz" => () => parameter.IsRepresentativeQuantMassBasedOnBasePeakMz,
            _ => throw new ArgumentOutOfRangeException(nameof(key)),
        };

        AssertReadsBothDirections(value => ConfigParser.ReadGcmsSpecificParameter(parameter, key, value), current);
    }

    [TestMethod]
    public void ReadLcImMsSpecificParameter_ReadsAccumulateMs2SpectraInBothDirections()
    {
        var parameter = new MsdialLcImMsParameter();

        AssertReadsBothDirections(
            value => ConfigParser.ReadLcImMsSpecificParameter(parameter, "accumulate ms2 spectra", value),
            () => parameter.IsAccumulateMS2Spectra);
    }

    [TestMethod]
    public void ReadCommonParameter_ReportsAnUnreadableOnOffValueOnAKeyThatWasAlreadyTwoWay()
    {
        // These arms already assigned both values, but still answered true for "yes", so the
        // key record said a value was applied that the run never saw.
        var parameter = new MsdialLcmsParameter();

        AssertReadsBothDirections(
            value => ConfigParser.ReadCommonParameter(parameter, "together with alignment", value),
            () => parameter.TogetherWithAlignment);
    }

    private static void AssertReadsBothDirections(Func<string, ConfigParser.MethodKeyOutcome> read, Func<bool> current)
    {
        Assert.IsTrue(read("True").IsApplied);
        Assert.IsTrue(current(), "True takes effect");

        Assert.IsTrue(read("False").IsApplied);
        Assert.IsFalse(current(), "False takes effect");

        Assert.IsTrue(read("TRUE").IsApplied);
        Assert.IsTrue(current(), "the letter case does not matter");

        var unusable = read("yes");
        Assert.IsTrue(unusable.IsUnusableValue, "a value that is neither true nor false is reported, not guessed at");
        Assert.IsTrue(current(), "the refused value left the previous one alone");
    }

    [TestMethod]
    public void ReadCommonParameter_SetsCcsFilteringForLbmAnnotationOnTheLbmParameter()
    {
        // This key used to set MspSearchParam, so it switched on CCS filtering for MSP-based
        // annotation and left LBM-based annotation untouched.
        var parameter = new MsdialLcmsParameter();
        Assert.IsFalse(parameter.LbmSearchParam.IsUseCcsForAnnotationFiltering);
        Assert.IsFalse(parameter.MspSearchParam.IsUseCcsForAnnotationFiltering);

        var result = ConfigParser.ReadCommonParameter(parameter, "use ccs for lbm-based annotation filtering", "true");

        Assert.IsTrue(result.IsApplied);
        Assert.IsTrue(parameter.LbmSearchParam.IsUseCcsForAnnotationFiltering);
        Assert.IsFalse(parameter.MspSearchParam.IsUseCcsForAnnotationFiltering);
    }

    [TestMethod]
    public void ReadCommonParameter_TreatsAThreadCountOutsideTheUsableRangeAsUnusable()
    {
        var parameter = new MsdialLcmsParameter();
        var before = parameter.NumThreads;

        var result = ConfigParser.ReadCommonParameter(parameter, "number of threads", "0");

        Assert.IsTrue(result.IsUnusableValue);
        Assert.AreEqual(before, parameter.NumThreads);
    }

    [TestMethod]
    public void ReadCommonParameter_ReadsAutomaticAlignmentRtCorrectionSettings()
    {
        var parameter = new MsdialLcmsParameter();
        var settings = new[] {
            ("execute automatic rt correction for alignment", "true"),
            ("automatic rt correction reference file id", "7"),
            ("automatic rt correction rt bin width", "0.4"),
            ("automatic rt correction match rt tolerance", "1.2"),
            ("automatic rt correction minimum anchors", "4"),
            ("automatic rt correction maximum anchors", "9"),
            ("automatic rt correction minimum sample coverage", "0.7"),
            ("automatic rt correction intensity quantile", "0.8"),
            ("automatic rt correction maximum peak width quantile", "0.6"),
            ("automatic rt correction minimum signal to noise", "5"),
            ("automatic rt correction minimum gaussian similarity", "0.3"),
            ("automatic rt correction minimum ideal slope", "0.4"),
            ("automatic rt correction outlier mad threshold", "4.5"),
            ("automatic rt correction reference centrality weight", "0.25"),
            ("automatic rt correction interpolate blanks by analytical order", "false"),
        };

        foreach (var (key, value) in settings) {
            Assert.IsTrue(ConfigParser.ReadCommonParameter(parameter, key, value).IsApplied, key);
        }

        var actual = parameter.AlignmentBaseParam.AutomaticRtCorrection;
        Assert.IsTrue(actual.Execute);
        Assert.AreEqual(7, actual.ReferenceFileId);
        Assert.AreEqual(0.4f, actual.RtBinWidth, 1e-7f);
        Assert.AreEqual(1.2f, actual.MatchRtTolerance, 1e-7f);
        Assert.AreEqual(4, actual.MinimumAnchorCount);
        Assert.AreEqual(9, actual.MaximumAnchorCount);
        Assert.AreEqual(0.7f, actual.MinimumSampleCoverage, 1e-7f);
        Assert.AreEqual(0.8f, actual.IntensityQuantile, 1e-7f);
        Assert.AreEqual(0.6f, actual.MaximumPeakWidthQuantile, 1e-7f);
        Assert.AreEqual(5f, actual.MinimumSignalToNoise, 1e-7f);
        Assert.AreEqual(0.3f, actual.MinimumGaussianSimilarity, 1e-7f);
        Assert.AreEqual(0.4f, actual.MinimumIdealSlope, 1e-7f);
        Assert.AreEqual(4.5f, actual.OutlierMadThreshold, 1e-7f);
        Assert.AreEqual(0.25f, actual.ReferenceCentralityWeight, 1e-7f);
        Assert.IsFalse(actual.InterpolateBlankByAnalyticalOrder);
    }

    [TestMethod]
    public void ReadForLcms_WritesWhatHappenedToEveryKeyBesideTheMethodFile()
    {
        // The Console said all of this on stdout and nowhere else. Stdout reaches a log the caller
        // keeps for as long as it keeps the job, and the contract's retained artifacts do not
        // include it, so an audit reading a unit's workspace could see the method file's declared
        // settings and could not see which of them the run had used.
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile(
            "method.txt",
            """
            Minimum peak height: 500.0
            Mass slice width: 0.1
            Sigma window value: 0.7
            A parameter that does not exist: 7
            Smoothing level: 5.7
            Msp file path:
            """);

        ConfigParser.ReadForLcmsParameter(methodFile);

        var record = Path.Combine(Path.GetDirectoryName(methodFile)!, "method.keys.json");
        Assert.IsTrue(File.Exists(record), "the key record must land beside the method file");
        var parsed = JObject.Parse(File.ReadAllText(record));

        Assert.AreEqual("msdial-method-file-keys.v1", (string?)parsed["schema"]);
        Assert.AreEqual("method.txt", (string?)parsed["method_file"]);
        Assert.AreEqual(64, ((string?)parsed["method_file_sha256"])!.Length, "sha256 of the file that was read");

        var applied = parsed["applied"]!.Select(item => (string)item!).ToList();
        var unrecognised = parsed["unrecognised"]!.Select(item => (string)item!).ToList();
        var unusable = parsed["unusable"]!.Select(item => (string)item!).ToList();
        var blank = parsed["blank"]!.Select(item => (string)item!).ToList();

        CollectionAssert.Contains(applied, "Minimum peak height");
        CollectionAssert.Contains(applied, "Mass slice width");
        CollectionAssert.Contains(applied, "Sigma window value");
        CollectionAssert.Contains(unrecognised, "A parameter that does not exist");
        CollectionAssert.Contains(unusable, "Smoothing level: 5.7");
        CollectionAssert.Contains(blank, "Msp file path");
    }

    [TestMethod]
    public void ReadForLcms_KeyRecordSeparatesAnUnusableValueFromAnUnknownKey()
    {
        // The two findings the old bool could not tell apart, now readable from the workspace.
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile(
            "method.txt",
            """
            Minimum peak height: quite high
            Nonexistent parameter: 1
            """);

        ConfigParser.ReadForLcmsParameter(methodFile);

        var parsed = JObject.Parse(
            File.ReadAllText(Path.Combine(Path.GetDirectoryName(methodFile)!, "method.keys.json")));

        CollectionAssert.Contains(parsed["unusable"]!.Select(i => (string)i!).ToList(), "Minimum peak height: quite high");
        CollectionAssert.Contains(parsed["unrecognised"]!.Select(i => (string)i!).ToList(), "Nonexistent parameter");
        CollectionAssert.DoesNotContain(parsed["applied"]!.Select(i => (string)i!).ToList(), "Minimum peak height");
    }

    /// <summary>
    /// The keys LcmsProcess reads for itself are recorded as applied, not as having no effect.
    /// </summary>
    /// <remarks>
    /// LcmsProcess reads these with readers of their own, after the key record has been written,
    /// and the record used to know only ReadCommonParameter. A repository run's record listed
    /// "MSP annotator settings file path", "LBM annotator priority" and "Alignment light mode" as
    /// unrecognised with NO EFFECT while each of them governed the run, and the reanalysis audits
    /// trust that record.
    /// </remarks>
    [TestMethod]
    public void ReadForLcms_KeyRecordListsTheKeysLcmsProcessReadsForItselfAsApplied()
    {
        using var directory = new TemporaryDirectory();
        var msp = directory.CreateFile("library.msp");
        var settings = directory.CreateFile(
            "msp_annotator_settings.tsv",
            $"annotator_id\tmsp_file_path\n" +
            $"library\t{msp}\n");
        var methodFile = directory.CreateFile(
            "method.txt",
            $"""
            MSP annotator settings file path: {settings}
            LBM annotator priority: 3
            Alignment light mode: True
            A parameter that does not exist: 7
            """);

        var (_, report) = ReadLcmsWithReport(methodFile);

        var applied = RecordedKeys(methodFile, "applied");
        foreach (var key in new[] { "MSP annotator settings file path", "LBM annotator priority", "Alignment light mode" }) {
            CollectionAssert.Contains(applied, key);
            Assert.IsFalse(report.Contains($"'{key}'"), $"'{key}' took effect and must not be reported as having none");
        }
        CollectionAssert.AreEqual(new[] { "A parameter that does not exist" }, RecordedKeys(methodFile, "unrecognised"));
        StringAssert.Contains(report, "'A parameter that does not exist' was not recognised and had NO EFFECT");

        // And the run really does take them: the record describes what the side readers do.
        Assert.AreEqual(1, ConfigParser.ReadMspAnnotatorSettings(methodFile, new MsdialLcmsParameter()).Count);
        Assert.AreEqual(3, ConfigParser.ReadLbmAnnotatorPriority(methodFile));
        Assert.IsTrue(ConfigParser.ReadAlignmentLightMode(methodFile));
    }

    [TestMethod]
    [DataRow("MSP annotator settings file path", "settings.tsv")]
    [DataRow("MSP annotation settings file path", "settings.tsv")]
    [DataRow("MSP search settings file path", "settings.tsv")]
    [DataRow("Text annotator settings file path", "settings.tsv")]
    [DataRow("Text library annotator settings file path", "settings.tsv")]
    [DataRow("Text DB annotator settings file path", "settings.tsv")]
    [DataRow("Text annotation settings file path", "settings.tsv")]
    [DataRow("LBM annotator priority", "2")]
    [DataRow("LBM annotation priority", "2")]
    [DataRow("Alignment light mode", "true")]
    [DataRow("Alignment light", "false")]
    [DataRow("Console alignment light mode", "true")]
    [DataRow("Detailed alignment provenance", "true")]
    [DataRow("Export detailed alignment provenance", "false")]
    [DataRow("Annotation candidates", "true")]
    [DataRow("Export annotation candidates", "false")]
    public void ReadForLcms_KeyRecordAcceptsEverySpellingTheSideReadersAccept(string key, string value)
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile("method.txt", $"{key}: {value}\n");

        ConfigParser.ReadForLcmsParameter(methodFile);

        CollectionAssert.AreEqual(new[] { key }, RecordedKeys(methodFile, "applied"));
        Assert.AreEqual(0, RecordedKeys(methodFile, "unrecognised").Count);
    }

    /// <summary>
    /// A value the side reader passes over is recorded as unusable, as the run used the default.
    /// </summary>
    /// <remarks>
    /// "2.0" is not a priority to the reader and "yes" is not a switch, so those lines are skipped
    /// and a later usable line, or the default, governs. Calling them applied would repeat the
    /// failure this record exists to end.
    /// </remarks>
    [TestMethod]
    public void ReadForLcms_KeyRecordCallsAValueTheSideReaderPassesOverUnusable()
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile(
            "method.txt",
            """
            Alignment light mode: yes
            LBM annotator priority: 2.0
            LBM annotator priority: 4
            """);

        ConfigParser.ReadForLcmsParameter(methodFile);

        CollectionAssert.AreEqual(
            new[] { "Alignment light mode: yes", "LBM annotator priority: 2.0" },
            RecordedKeys(methodFile, "unusable"));
        CollectionAssert.AreEqual(new[] { "LBM annotator priority" }, RecordedKeys(methodFile, "applied"));
        Assert.IsFalse(ConfigParser.ReadAlignmentLightMode(methodFile), "the default the run used");
        Assert.AreEqual(4, ConfigParser.ReadLbmAnnotatorPriority(methodFile), "the only usable line, which the run used");
    }

    /// <summary>
    /// A key written twice resolves to the later line in the side readers, as in the main ones.
    /// </summary>
    /// <remarks>
    /// These six readers used to stop at the first usable line while ReadCommonParameter kept the
    /// last, so one method file meant the later value for "Minimum peak height" and the earlier
    /// value for "LBM annotator priority", and the key record said both lines had applied.
    /// </remarks>
    [TestMethod]
    public void SideReaders_TakeTheLastLineOfAKeyWrittenTwice_AsTheMainReadersDo()
    {
        using var directory = new TemporaryDirectory();
        var msp = directory.CreateFile("library.msp");
        var text = directory.CreateFile("library.txt");
        var firstMsp = directory.CreateFile("first_msp.tsv", $"annotator_id\tmsp_file_path\nfirst\t{msp}\n");
        var lastMsp = directory.CreateFile("last_msp.tsv", $"annotator_id\tmsp_file_path\nlast\t{msp}\n");
        var firstText = directory.CreateFile("first_text.tsv", $"annotator_id\ttext_db_file_path\nfirst\t{text}\n");
        var lastText = directory.CreateFile("last_text.tsv", $"annotator_id\ttext_db_file_path\nlast\t{text}\n");
        var methodFile = directory.CreateFile(
            "method.txt",
            $"""
            Minimum peak height: 100
            Alignment light mode: True
            LBM annotator priority: 2
            Detailed alignment provenance: True
            Annotation candidates: True
            MSP annotator settings file path: {firstMsp}
            Text annotator settings file path: {firstText}
            Minimum peak height: 200
            Alignment light mode: False
            LBM annotator priority: 5
            Detailed alignment provenance: False
            Annotation candidates: False
            MSP annotator settings file path: {lastMsp}
            Text annotator settings file path: {lastText}
            """);

        var parameter = ConfigParser.ReadForLcmsParameter(methodFile);

        Assert.AreEqual(200d, parameter.MinimumAmplitude, "the main reader keeps the last line");
        Assert.IsFalse(ConfigParser.ReadAlignmentLightMode(methodFile));
        Assert.AreEqual(5, ConfigParser.ReadLbmAnnotatorPriority(methodFile));
        Assert.IsFalse(ConfigParser.ReadDetailedAlignmentProvenance(methodFile));
        Assert.IsFalse(ConfigParser.ReadAnnotationCandidateExport(methodFile));
        CollectionAssert.AreEqual(
            new[] { "last" },
            ConfigParser.ReadMspAnnotatorSettings(methodFile, parameter).Select(setting => setting.AnnotatorId).ToArray());
        CollectionAssert.AreEqual(
            new[] { "last" },
            ConfigParser.ReadTextAnnotatorSettings(methodFile, parameter).Select(setting => setting.AnnotatorId).ToArray());
    }

    /// <summary>
    /// An unusable or blank later line leaves the earlier value, in the side readers as in the main.
    /// </summary>
    /// <remarks>
    /// "Minimum peak height: quite high" leaves the earlier height in place, and "Msp file path:"
    /// does not clear an earlier path. The side readers skip such lines the same way. A blank
    /// settings-file path also used to end the search outright, so a real path after it was
    /// never read.
    /// </remarks>
    [TestMethod]
    public void SideReaders_SkipABlankOrUnusableLaterLine()
    {
        using var directory = new TemporaryDirectory();
        var msp = directory.CreateFile("library.msp");
        var settings = directory.CreateFile("msp.tsv", $"annotator_id\tmsp_file_path\nkept\t{msp}\n");
        var earlierThenBlank = directory.CreateFile(
            "earlier_then_blank.txt",
            $"""
            MSP annotator settings file path: {settings}
            MSP annotator settings file path:
            LBM annotator priority: 3
            LBM annotator priority: 2.0
            Alignment light mode: True
            Alignment light mode: yes
            """);
        var blankThenLater = directory.CreateFile(
            "blank_then_later.txt",
            $"""
            MSP annotator settings file path:
            MSP annotator settings file path: {settings}
            """);

        Assert.AreEqual(1, ConfigParser.ReadMspAnnotatorSettings(earlierThenBlank, new MsdialLcmsParameter()).Count);
        Assert.AreEqual(3, ConfigParser.ReadLbmAnnotatorPriority(earlierThenBlank));
        Assert.IsTrue(ConfigParser.ReadAlignmentLightMode(earlierThenBlank));
        Assert.AreEqual(1, ConfigParser.ReadMspAnnotatorSettings(blankThenLater, new MsdialLcmsParameter()).Count);
    }

    /// <summary>
    /// The key record names a key written on more than one line and the value the run used.
    /// </summary>
    /// <remarks>
    /// "applied" says a key took effect, not which of its lines did. The used value is the last
    /// line a reader accepted, so an unusable later line does not replace it. Keys are matched by
    /// spelling without regard to letter case, as the readers match them.
    /// </remarks>
    [TestMethod]
    public void ReadForLcms_KeyRecordNamesARepeatedKeyAndTheValueUsed()
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile(
            "method.txt",
            """
            Minimum peak height: 100
            Mass slice width: 0.1
            minimum peak height: 200
            LBM annotator priority: 3
            LBM annotator priority: 2.0
            Smoothing level: 5.5
            Smoothing level: 6.5
            """);

        var (parameter, report) = ReadLcmsWithReport(methodFile);

        var repeated = RepeatedKeys(methodFile);
        CollectionAssert.AreEqual(
            new[] { "Minimum peak height", "LBM annotator priority", "Smoothing level" },
            repeated.Select(entry => (string)entry["key"]!).ToArray(),
            "in order of first appearance; a key written once is not listed");
        Assert.AreEqual(2, (int)repeated[0]["lines"]!);
        Assert.AreEqual("200", (string?)repeated[0]["used"]);
        Assert.AreEqual(200d, parameter.MinimumAmplitude, "the record names the value the run used");
        Assert.AreEqual("3", (string?)repeated[1]["used"], "the unusable later line did not replace it");
        Assert.AreEqual(3, ConfigParser.ReadLbmAnnotatorPriority(methodFile));
        Assert.AreEqual(JTokenType.Null, repeated[2]["used"]!.Type, "neither line could be read");

        StringAssert.Contains(report, "'Minimum peak height' is written on 2 lines; the last line applied was used: '200'");
        StringAssert.Contains(report, "'Smoothing level' is written on 2 lines; no line was applied");
        Assert.IsFalse(report.Contains("'Mass slice width' is written"), report);
    }

    private static List<JObject> RepeatedKeys(string methodFile)
    {
        var record = Path.Combine(
            Path.GetDirectoryName(methodFile)!,
            Path.GetFileNameWithoutExtension(methodFile) + ".keys.json");
        return JObject.Parse(File.ReadAllText(record))["repeated"]!.Cast<JObject>().ToList();
    }

    /// <summary>
    /// Reading a method file does not load the RT correction library.
    /// </summary>
    /// <remarks>
    /// The reader used to open the library the moment it read the key, from the value as written:
    /// a relative value from the working directory, and before GC-MS resolved the path against
    /// the method file's folder, so the stored library and the stored path could name different
    /// files. RetentionTimeCorrectionProcess now loads it, once, from the final path.
    /// </remarks>
    [TestMethod]
    public void ReadForLcms_RecordsTheRtCorrectionLibraryPathWithoutLoadingIt()
    {
        using var directory = new TemporaryDirectory();
        var library = directory.CreateFile("anchors.txt", AnchorLibrary("Anchor A"));
        var methodFile = directory.CreateFile(
            "method.txt",
            $"Compounds library file path for RT correction: {library}\n");

        var parameter = ConfigParser.ReadForLcmsParameter(methodFile);

        Assert.AreEqual(library, parameter.CompoundListForRtCorrectionPath);
        Assert.AreEqual(0, parameter.RetentionTimeCorrectionCommon.StandardLibrary.Count);
        CollectionAssert.Contains(RecordedKeys(methodFile, "applied"), "Compounds library file path for RT correction");
    }

    [TestMethod]
    public void ReadForGcms_TheRtCorrectionLibraryIsLoadedFromThePathAfterItIsResolved()
    {
        // The value is relative, and the working directory is not the method file's folder, so
        // the old parse-time load could not have found it. The resolved path can.
        using var directory = new TemporaryDirectory();
        var library = directory.CreateFile("anchors.txt", AnchorLibrary("Anchor A", "Anchor B"));
        var methodFile = directory.CreateFile(
            "method.txt",
            "Compounds library file path for RT correction: anchors.txt\n");
        Assert.AreNotEqual(
            Path.GetFullPath(directory.Path).TrimEnd(Path.DirectorySeparatorChar),
            Path.GetFullPath(Environment.CurrentDirectory).TrimEnd(Path.DirectorySeparatorChar));

        var parameter = ConfigParser.ReadForGcms(methodFile);

        Assert.AreEqual(Path.GetFullPath(library), parameter.CompoundListForRtCorrectionPath);
        Assert.AreEqual(0, parameter.RetentionTimeCorrectionCommon.StandardLibrary.Count);
        CollectionAssert.AreEqual(
            new[] { "Anchor A", "Anchor B" },
            RetentionTimeCorrectionProcess.LoadStandards(parameter).Select(standard => standard.Name).ToArray());
    }

    [TestMethod]
    public void LoadStandards_RefusesAMalformedLibraryWithTheParserMessage()
    {
        using var directory = new TemporaryDirectory();
        var library = directory.CreateFile(
            "anchors.txt",
            "Name\tRT\tRT tolerance\tm/z\tm/z tolerance\tMinimum height\tInclude\n" +
            "Anchor A\tnot a time\t0.1\t100\t0.01\t1000\ttrue\n");
        var parameter = new MsdialLcmsParameter { CompoundListForRtCorrectionPath = library };

        var error = Assert.ThrowsException<InvalidDataException>(() => RetentionTimeCorrectionProcess.LoadStandards(parameter));

        StringAssert.Contains(error.Message, "non-numerical value for retention time");
    }

    /// <summary>
    /// A malformed RT correction library stops an LC-MS run before anything heavy is read.
    /// </summary>
    /// <remarks>
    /// The library is small and its format is easy to get wrong, so it is checked first: before
    /// the analysis files are imported, and so before the annotation libraries and the raw data.
    /// The input folder here is empty, so reaching the import would print "Loading analysis files".
    /// </remarks>
    [TestMethod]
    public void LcmsProcess_RefusesAMalformedRtCorrectionLibraryBeforeLoadingAnalysisFiles()
    {
        using var directory = new TemporaryDirectory();
        var library = directory.CreateFile(
            "anchors.txt",
            "Name\tRT\tRT tolerance\tm/z\tm/z tolerance\tMinimum height\tInclude\n" +
            "Anchor A\tnot a time\t0.1\t100\t0.01\t1000\ttrue\n");
        var methodFile = directory.CreateFile(
            "method.txt",
            $"""
            Execute RT correction: True
            Compounds library file path for RT correction: {library}
            """);

        var (result, output, error) = RunLcms(directory, methodFile);

        Assert.AreEqual(-1, result);
        StringAssert.Contains(error, "RT correction library could not be used");
        StringAssert.Contains(error, "non-numerical value for retention time");
        Assert.IsFalse(output.Contains("Loading analysis files"), output);
    }

    /// <summary>
    /// With RT correction off, a library path set in the method file is warned about, not read.
    /// </summary>
    /// <remarks>
    /// The library has no effect without the correction, so it is neither opened nor checked --
    /// the path here names no file at all. A path left in place usually means the switch was meant
    /// to be on, which is why it is still mentioned.
    /// </remarks>
    [TestMethod]
    public void LcmsProcess_WarnsWhenAnRtCorrectionLibraryIsSetButRtCorrectionIsOff()
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile(
            "method.txt",
            """
            Execute RT correction: False
            Compounds library file path for RT correction: no-such-anchors.txt
            """);

        var (_, output, error) = RunLcms(directory, methodFile);

        StringAssert.Contains(output, "'Execute RT correction' is False, so the library is not used");
        Assert.IsFalse(error.Contains("RT correction library could not be used"), error);
    }

    [TestMethod]
    public void LcmsProcess_SaysNothingAboutTheRtCorrectionLibraryWhenNoneIsSet()
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile("method.txt", "Execute RT correction: False\n");

        var (_, output, _) = RunLcms(directory, methodFile);

        Assert.IsFalse(output.Contains("Compounds library file path for RT correction"), output);
    }

    /// <summary>
    /// Run LC-MS on an empty input folder, so it stops at the analysis-file import.
    /// </summary>
    private static (int Result, string Output, string Error) RunLcms(TemporaryDirectory directory, string methodFile)
    {
        var input = System.IO.Path.Combine(directory.Path, "empty-input");
        Directory.CreateDirectory(input);
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var output = new StringWriter();
        var error = new StringWriter();
        try {
            Console.SetOut(output);
            Console.SetError(error);
            var result = new LcmsProcess().Run(
                input,
                System.IO.Path.Combine(directory.Path, "output"),
                methodFile,
                isProjectSaved: false,
                targetMz: -1f);
            return (result, output.ToString(), error.ToString());
        }
        finally {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private static string AnchorLibrary(params string[] names)
    {
        var text = new StringBuilder("Name\tRT\tRT tolerance\tm/z\tm/z tolerance\tMinimum height\tInclude\n");
        for (var i = 0; i < names.Length; i++) {
            text.Append($"{names[i]}\t{i + 1}.0\t0.1\t{100 * (i + 1)}.0\t0.01\t1000\ttrue\n");
        }
        return text.ToString();
    }

    /// <summary>
    /// Only LC-MS reads these keys, so every other mode still reports them as having no effect.
    /// </summary>
    [TestMethod]
    public void OtherModes_StillReportTheLcmsOnlyKeysAsUnrecognised()
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
                MSP annotator settings file path: settings.tsv
                LBM annotator priority: 3
                Alignment light mode: true
                """);

            read(methodFile);

            CollectionAssert.AreEqual(
                new[] { "MSP annotator settings file path", "LBM annotator priority", "Alignment light mode" },
                RecordedKeys(methodFile, "unrecognised"),
                mode);
        }
    }

    private static List<string> RecordedKeys(string methodFile, string list)
    {
        var record = Path.Combine(
            Path.GetDirectoryName(methodFile)!,
            Path.GetFileNameWithoutExtension(methodFile) + ".keys.json");
        return JObject.Parse(File.ReadAllText(record))[list]!.Select(item => (string)item!).ToList();
    }

    [TestMethod]
    public void ReadForGcms_AcceptsEqualsSyntaxQuotesAndGuiFieldNames()
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile(
            "method.txt",
            """
            Msp file path = "references\Fiehn Library.msp"
            RI dictionary file path: "ri\FAME Mapping.txt"
            RI compound type = Fames
            Retention type: RI
            Alignment index type = RI
            Retention index alignment tolerance: 1234.5
            Retention index tolerance for identification = 567.5
            Weighted dot product cutoff: 0.55
            Simple dot product cutoff = 0.56
            Reverse dot product cutoff: 0.57
            Matched peaks percentage cutoff = 0.58
            Minimum spectrum match: 4
            """);

        var parameter = ConfigParser.ReadForGcms(methodFile);

        Assert.AreEqual(
            Path.GetFullPath(Path.Combine(directory.Path, "references", "Fiehn Library.msp")),
            parameter.MspFilePath);
        Assert.AreEqual(
            Path.GetFullPath(Path.Combine(directory.Path, "ri", "FAME Mapping.txt")),
            parameter.RiDictionaryFilePath);
        Assert.AreEqual(RiCompoundType.Fames, parameter.RiCompoundType);
        Assert.AreEqual(RetentionType.RI, parameter.RetentionType);
        Assert.AreEqual(AlignmentIndexType.RI, parameter.AlignmentIndexType);
        Assert.AreEqual(1234.5f, parameter.RetentionIndexAlignmentTolerance);
        Assert.AreEqual(567.5f, parameter.MspSearchParam.RiTolerance);
        Assert.AreEqual(0.55f, parameter.MspSearchParam.WeightedDotProductCutOff);
        Assert.AreEqual(0.56f, parameter.MspSearchParam.SimpleDotProductCutOff);
        Assert.AreEqual(0.57f, parameter.MspSearchParam.ReverseDotProductCutOff);
        Assert.AreEqual(0.58f, parameter.MspSearchParam.MatchedPeaksPercentageCutOff);
        Assert.AreEqual(4f, parameter.MspSearchParam.MinimumSpectrumMatch);
    }

    [TestMethod]
    public void ReadForGcms_AcceptsLegacyRiPathAndAnnotationFieldNames()
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile(
            "legacy-method.txt",
            """
            RI index file pathes: dictionaries.txt
            RI tolerance for MSP-based annotation: 2000
            """);

        var parameter = ConfigParser.ReadForGcms(methodFile);

        Assert.AreEqual(
            Path.GetFullPath(Path.Combine(directory.Path, "dictionaries.txt")),
            parameter.RiDictionaryFilePath);
        Assert.AreEqual(2000f, parameter.MspSearchParam.RiTolerance);
    }

    [TestMethod]
    public void ReadMspAnnotatorSettings_UsesPerAnnotatorTargetOmics()
    {
        using var directory = new TemporaryDirectory();
        var msp = directory.CreateFile("library.msp");
        var settings = directory.CreateFile(
            "msp_annotator_settings.tsv",
            $"annotator_id\tmsp_file_path\tpriority\ttarget_omics\tms2_tolerance\n" +
            $"high\t{msp}\t2\tMetabolomics\t0.05\n" +
            $"inherit\t{msp}\t1\t\t0.25\n");
        var method = directory.CreateFile(
            "method.txt",
            $"Msp annotator settings file path: {settings}\n");

        var parsed = ConfigParser.ReadMspAnnotatorSettings(method, new MsdialLcmsParameter());

        Assert.AreEqual(2, parsed.Count);
        Assert.AreEqual(TargetOmics.Metabolomics, parsed[0].TargetOmics);
        Assert.IsNull(parsed[1].TargetOmics);
        Assert.AreEqual(0.05F, parsed[0].SearchParameter.Ms2Tolerance, 0.0001F);
        Assert.AreEqual(0.25F, parsed[1].SearchParameter.Ms2Tolerance, 0.0001F);
    }

    /// <summary>
    /// A run with no GUI can still say whether a library's spectra were acquired or computed.
    /// </summary>
    /// <remarks>
    /// The GUI asks this through the database-kind dropdown. The reanalysis pipeline never opens
    /// one, so without this the Console could only ever build DataBaseSource.Msp and an in-silico
    /// library -- NEIMS, CFM-ID, ICEBERG -- would be published as a reference-spectrum match.
    /// </remarks>
    [TestMethod]
    public void ReadMspAnnotatorSettings_ReadsTheLibraryKind()
    {
        using var directory = new TemporaryDirectory();
        var msp = directory.CreateFile("library.msp");
        var settings = directory.CreateFile(
            "msp_annotator_settings.tsv",
            $"annotator_id\tmsp_file_path\tlibrary_kind\n" +
            $"acquired\t{msp}\tacquired\n" +
            $"computed\t{msp}\tpredicted\n" +
            $"silent\t{msp}\t\n");
        var method = directory.CreateFile(
            "method.txt",
            $"Msp annotator settings file path: {settings}\n");

        var parsed = ConfigParser.ReadMspAnnotatorSettings(method, new MsdialLcmsParameter());

        Assert.AreEqual(3, parsed.Count);
        Assert.AreEqual(DataBaseSource.Msp, parsed[0].DataBaseSource);
        Assert.AreEqual(DataBaseSource.PredictedMsp, parsed[1].DataBaseSource);
        Assert.AreEqual(DataBaseSource.Msp, parsed[2].DataBaseSource,
            "silence means acquired, so an existing settings file runs unchanged");
    }

    [TestMethod]
    public void ReadMspAnnotatorSettings_AcceptsTheSpellingsPeopleWillActuallyType()
    {
        using var directory = new TemporaryDirectory();
        var msp = directory.CreateFile("library.msp");
        var settings = directory.CreateFile(
            "msp_annotator_settings.tsv",
            $"annotator_id\tmsp_file_path\tspectra_source\n" +
            $"a\t{msp}\tin silico\n" +
            $"b\t{msp}\tIn-Silico\n" +
            $"c\t{msp}\tGenerated\n" +
            $"d\t{msp}\tExperimental\n");
        var method = directory.CreateFile("method.txt", $"Msp annotator settings file path: {settings}\n");

        var parsed = ConfigParser.ReadMspAnnotatorSettings(method, new MsdialLcmsParameter());

        Assert.AreEqual(DataBaseSource.PredictedMsp, parsed[0].DataBaseSource);
        Assert.AreEqual(DataBaseSource.PredictedMsp, parsed[1].DataBaseSource);
        Assert.AreEqual(DataBaseSource.PredictedMsp, parsed[2].DataBaseSource);
        Assert.AreEqual(DataBaseSource.Msp, parsed[3].DataBaseSource);
    }

    /// <summary>
    /// A typo does not lose the run; it is reported and the library is treated as acquired.
    /// </summary>
    /// <remarks>
    /// The alternative -- failing the run -- costs a whole reanalysis for a misspelt word, and the
    /// alternative to THAT -- guessing "predicted" -- would publish an in-silico claim nobody made.
    /// </remarks>
    [TestMethod]
    public void ReadMspAnnotatorSettings_TreatsAnUnknownLibraryKindAsAcquired()
    {
        using var directory = new TemporaryDirectory();
        var msp = directory.CreateFile("library.msp");
        var settings = directory.CreateFile(
            "msp_annotator_settings.tsv",
            $"annotator_id\tmsp_file_path\tlibrary_kind\n" +
            $"typo\t{msp}\tpredicated\n");
        var method = directory.CreateFile("method.txt", $"Msp annotator settings file path: {settings}\n");

        var parsed = ConfigParser.ReadMspAnnotatorSettings(method, new MsdialLcmsParameter());

        Assert.AreEqual(1, parsed.Count);
        Assert.AreEqual(DataBaseSource.Msp, parsed[0].DataBaseSource);
    }

    /// <summary>
    /// A METHOD-FILE KEY THAT HAD NO EFFECT SAYS SO.
    /// </summary>
    /// <remarks>
    /// Every dispatcher used to discard the boolean saying whether a reader had claimed the line,
    /// under a comment reading "// write something if needed". So a misspelt key, a key from a newer
    /// MS-DIAL, or a key copied from another mode's template was read, matched nothing, and
    /// vanished; the run used the built-in default and the analyst had every reason to believe their
    /// value had been applied. For a reanalysis campaign that is a silently wrong result with a
    /// method file that appears to document it correctly.
    ///
    /// Reported rather than fatal, and that was measured: run against MS-DIAL's own shipped
    /// lipidomics template this finds twenty-seven such keys, so failing would reject every method
    /// file in existence.
    /// </remarks>
    [TestMethod]
    public void ReadForLcmsParameter_ReportsAKeyThatHadNoEffect()
    {
        using var directory = new TemporaryDirectory();
        var method = directory.CreateFile(
            "method.txt",
            "Mass slice width: 0.05" + "\n" +
            "Mass slize width: 0.5" + "\n");

        var (parameter, report) = ReadLcmsWithReport(method);

        Assert.AreEqual(0.05f, parameter.MassSliceWidth, 0.0001f, "the correctly spelled key applies");
        StringAssert.Contains(report, "Mass slize width");
        StringAssert.Contains(report, "NO EFFECT");
        Assert.IsFalse(report.Contains("'Mass slice width'"), "a key that worked is not reported");
    }

    [TestMethod]
    public void ReadForLcmsParameter_SummarizesManyIgnoredTemplateKeysButRetainsTheirAudit()
    {
        using var directory = new TemporaryDirectory();
        var method = directory.CreateFile(
            "method.txt",
            string.Join("\n", Enumerable.Range(1, 7).Select(index => $"Unsupported key {index}: 1")) + "\n");

        var (_, report) = ReadLcmsWithReport(method);
        var record = JObject.Parse(File.ReadAllText(Path.Combine(Path.GetDirectoryName(method), "method.keys.json")));

        StringAssert.Contains(report, "7 parameter(s) had NO EFFECT");
        StringAssert.Contains(report, "method.keys.json");
        Assert.IsFalse(report.Contains("'Unsupported key 1'"));
        Assert.AreEqual(7, record["unrecognised"]!.Count());
    }

    [TestMethod]
    public void ReadForLcmsParameter_SummarizesManyBlankTemplateKeysButRetainsTheirAudit()
    {
        using var directory = new TemporaryDirectory();
        var method = directory.CreateFile(
            "method.txt",
            string.Join("\n", Enumerable.Range(1, 7).Select(index => $"Unused path {index}:")) + "\n");

        var (_, report) = ReadLcmsWithReport(method);
        var record = JObject.Parse(File.ReadAllText(Path.Combine(Path.GetDirectoryName(method), "method.keys.json")));

        StringAssert.Contains(report, "7 parameter(s) were left blank");
        StringAssert.Contains(report, "method.keys.json");
        Assert.IsFalse(report.Contains("Unused path 1"));
        Assert.AreEqual(7, record["blank"]!.Count());
    }

    /// <summary>
    /// A blank value is reported separately, because it is how a method file says "none".
    /// </summary>
    /// <remarks>
    /// "Msp file path:" with nothing after it is how the templates say there is no MSP library, so
    /// it is not an error. It is still named, because from the analyst's side it looks identical to
    /// a value that failed to apply.
    /// </remarks>
    [TestMethod]
    public void ReadForLcmsParameter_SeparatesABlankValueFromAnUnrecognisedKey()
    {
        using var directory = new TemporaryDirectory();
        var method = directory.CreateFile(
            "method.txt",
            "Msp file path:" + "\n" +
            "Not a real setting: 3" + "\n");

        var (_, report) = ReadLcmsWithReport(method);

        StringAssert.Contains(report, "left blank");
        StringAssert.Contains(report, "Msp file path");
        StringAssert.Contains(report, "1 parameter(s) had no effect");
    }

    /// <summary>
    /// A method file whose keys are all understood says nothing.
    /// </summary>
    /// <remarks>
    /// The silence matters as much as the warning: a report that fires on every run is one nobody
    /// reads, and the point of this is that the twenty-seven in the template become visible.
    /// </remarks>
    [TestMethod]
    public void ReadForLcmsParameter_IsSilentWhenEveryKeyApplies()
    {
        using var directory = new TemporaryDirectory();
        var method = directory.CreateFile(
            "method.txt",
            "Mass slice width: 0.05" + "\n" +
            "# a comment is not a key" + "\n" +
            "Number of threads: 4" + "\n");

        var (_, report) = ReadLcmsWithReport(method);

        Assert.AreEqual(string.Empty, report.Trim(), report);
    }

    private static (MsdialLcmsParameter, string) ReadLcmsWithReport(string methodFile)
    {
        var original = Console.Out;
        var captured = new StringWriter();
        try {
            Console.SetOut(captured);
            var parameter = ConfigParser.ReadForLcmsParameter(methodFile);
            return (parameter, captured.ToString());
        }
        finally {
            Console.SetOut(original);
        }
    }

    [TestMethod]
    public void ReadLbmAnnotatorPriority_DefaultsToOneAndReadsConfiguredValue()
    {
        using var directory = new TemporaryDirectory();
        var defaultMethod = directory.CreateFile("default.txt", "Ion mode: Negative\n");
        var configuredMethod = directory.CreateFile("configured.txt", "LBM annotator priority: 3\n");

        Assert.AreEqual(1, ConfigParser.ReadLbmAnnotatorPriority(defaultMethod));
        Assert.AreEqual(3, ConfigParser.ReadLbmAnnotatorPriority(configuredMethod));
    }

    [TestMethod]
    public void ReadDetailedAlignmentProvenance_DefaultsToFalseAndAcceptsBothAliases()
    {
        using var directory = new TemporaryDirectory();
        var defaultMethod = directory.CreateFile("default.txt", "Ion mode: Negative\n");
        var shortAlias = directory.CreateFile("short.txt", "Detailed alignment provenance: True\n");
        var longAlias = directory.CreateFile("long.txt", "Export detailed alignment provenance: true\n");
        var explicitlyOff = directory.CreateFile("off.txt", "Detailed alignment provenance: FALSE\n");
        // A value that is neither true nor false must leave the default alone rather than throw: the
        // audit sidecar is optional, so an unparseable setting means "not requested", not "fail the run".
        var nonBoolean = directory.CreateFile("bad.txt", "Detailed alignment provenance: yes please\n");

        Assert.IsFalse(ConfigParser.ReadDetailedAlignmentProvenance(defaultMethod));
        Assert.IsTrue(ConfigParser.ReadDetailedAlignmentProvenance(shortAlias));
        Assert.IsTrue(ConfigParser.ReadDetailedAlignmentProvenance(longAlias));
        Assert.IsFalse(ConfigParser.ReadDetailedAlignmentProvenance(explicitlyOff));
        Assert.IsFalse(ConfigParser.ReadDetailedAlignmentProvenance(nonBoolean));
    }

    [TestMethod]
    public void ReadAnnotationCandidateExport_DefaultsToFalseAndAcceptsBothAliases()
    {
        using var directory = new TemporaryDirectory();
        var defaultMethod = directory.CreateFile("default.txt", "Ion mode: Negative\n");
        var shortAlias = directory.CreateFile("short.txt", "Annotation candidates: True\n");
        var longAlias = directory.CreateFile("long.txt", "Export annotation candidates: true\n");
        var explicitlyOff = directory.CreateFile("off.txt", "Annotation candidates: FALSE\n");
        var nonBoolean = directory.CreateFile("bad.txt", "Annotation candidates: all of them\n");

        Assert.IsFalse(ConfigParser.ReadAnnotationCandidateExport(defaultMethod));
        Assert.IsTrue(ConfigParser.ReadAnnotationCandidateExport(shortAlias));
        Assert.IsTrue(ConfigParser.ReadAnnotationCandidateExport(longAlias));
        Assert.IsFalse(ConfigParser.ReadAnnotationCandidateExport(explicitlyOff));
        Assert.IsFalse(ConfigParser.ReadAnnotationCandidateExport(nonBoolean));
    }

    /// <summary>
    /// The adducts a method file lists are the adducts it searches.
    /// </summary>
    /// <remarks>
    /// PeakCharacterEstimator keeps only adducts marked IsIncluded and otherwise falls back to the
    /// proton adduct. The reader stored AdductIon.GetAdductIon's shared instances, which are not
    /// included, so every console run searched [M+H]+ alone whatever the file listed, and the key
    /// report said the key had been applied.
    /// </remarks>
    [TestMethod]
    public void ReadForLcmsParameter_MarksTheListedAdductsIncluded()
    {
        using var directory = new TemporaryDirectory();
        var method = directory.CreateFile(
            "method.txt",
            "Searched adduct ions: [M+H]+,[M+Na]+, [M+NH4]+" + "\n");

        var (parameter, report) = ReadLcmsWithReport(method);

        CollectionAssert.AreEqual(
            new[] { "[M+H]+", "[M+Na]+", "[M+NH4]+" },
            parameter.SearchedAdductIons.Select(a => a.AdductIonName).ToArray(),
            "a space after the comma does not drop an adduct");
        Assert.IsTrue(parameter.SearchedAdductIons.All(a => a.IsIncluded));
        Assert.AreEqual(string.Empty, report.Trim(), report);
    }

    [TestMethod]
    public void ReadCommonParameter_MarksTheSharedAdductInstancesIncluded()
    {
        // The reader does not construct adducts of its own: it stores GetAdductIon's cached
        // instances and sets IsIncluded on them, as the GUI's adduct setting does.
        var parameter = new MsdialLcmsParameter();

        ConfigParser.ReadCommonParameter(parameter, "searched adduct ions", "[M+H]+,[M+Na]+");

        Assert.AreSame(AdductIon.GetAdductIon("[M+H]+"), parameter.SearchedAdductIons[0]);
        Assert.AreSame(AdductIon.GetAdductIon("[M+Na]+"), parameter.SearchedAdductIons[1]);
        Assert.IsTrue(AdductIon.GetAdductIon("[M+Na]+").IsIncluded);
    }

    [TestMethod]
    public void ReadCommonParameter_DropsAMistypedAdductAndKeepsTheRest()
    {
        var parameter = new MsdialLcmsParameter();

        var result = ConfigParser.ReadCommonParameter(parameter, "searched adduct ions", "[M+H]+,M+Na,[M+NH4]+");

        Assert.IsTrue(result.IsApplied);
        CollectionAssert.AreEqual(
            new[] { "[M+H]+", "[M+NH4]+" },
            parameter.SearchedAdductIons.Select(a => a.AdductIonName).ToArray());
    }

    [TestMethod]
    public void ReadCommonParameter_ReportsAnAdductListWithNothingUsableInIt()
    {
        var parameter = new MsdialLcmsParameter();
        var before = parameter.SearchedAdductIons;

        var result = ConfigParser.ReadCommonParameter(parameter, "searched adduct ions", "M+Na,sodium");

        Assert.IsTrue(result.IsUnusableValue);
        Assert.AreSame(before, parameter.SearchedAdductIons, "the built-in list, untouched");
    }

    [TestMethod]
    public void PeakCharacterEstimator_SearchesTheAdductsTheMethodFileListed()
    {
        var parameter = new MsdialLcmsParameter();
        ConfigParser.ReadCommonParameter(parameter, "searched adduct ions", "[M+H]+,[M+Na]+,[M+NH4]+");
        var estimator = new PeakCharacterEstimator(0, 0);

        estimator.Process(
            new AnalysisFileBean(),
            new EmptyDataProvider(),
            new List<ChromatogramPeakFeature>(),
            new List<MSDecResult>(),
            null!,
            parameter,
            null);

        CollectionAssert.AreEqual(
            new[] { "[M+H]+", "[M+Na]+", "[M+NH4]+" },
            estimator.SearchedAdducts.Select(a => a.AdductIonName).ToArray());
    }

    private sealed class EmptyDataProvider : IDataProvider
    {
        private static readonly ReadOnlyCollection<RawSpectrum> Empty = new List<RawSpectrum>().AsReadOnly();

        public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() => Empty;
        public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() => Empty;
        public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) => Empty;
        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) => Task.FromResult(Empty);
        public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) => Task.FromResult(Empty);
        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) => Task.FromResult(Empty);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "MsdialCoreTestAppTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string CreateFile(string name, string content = "")
        {
            var path = System.IO.Path.Combine(Path, name);
            File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            return path;
        }

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
